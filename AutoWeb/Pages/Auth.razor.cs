using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AutoWeb.Client;
using AutoWeb.Helpers;
using AutoWeb.Services;

namespace AutoWeb.Pages;

public partial class Auth : ComponentBase, IDisposable
{
    [Inject] private IAutoHostClient AutoHostClient { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private PasskeyService PasskeyService { get; set; } = default!;

    private enum AuthStep
    {
        Email,
        MethodSelection,  // For users with multiple auth options
        Password,
        Passkey
    }

    private AuthStep authStep = AuthStep.Email;
    private string email = "";
    private string password = "";
    private string confirmPassword = "";
    private bool isNewUser = false;
    private bool hasPassword = false;
    private bool hasPasskeys = false;
    private bool isLoading = false;
    private string errorMessage = "";
    private AutoWeb.Client.AuthErrorCode? errorCode = null;
    private bool passkeyFailed = false;
    private string? authToken = null;
    private bool isConnected = false;
    private string apiVersion = "";
    private bool passkeySupported = false;
    private System.Threading.Timer? connectionCheckTimer;

    private string GetEmailInputClasses()
    {
        var baseClasses = "w-full p-3 bg-gray-700 text-white rounded border focus:outline-none transition-colors";
        var isInvalid = !string.IsNullOrEmpty(email) && !ValidationHelper.IsValidEmail(email);
        var borderClasses = isInvalid
            ? "border-red-500 focus:border-red-400"
            : "border-gray-600 focus:border-green-400";
        return $"{baseClasses} {borderClasses}";
    }

    private bool CanSubmitPassword()
    {
        if (authStep != AuthStep.Password || string.IsNullOrWhiteSpace(password))
            return false;

        if (isNewUser && password != confirmPassword)
            return false;

        return true;
    }

    private async Task CheckEmail()
    {
        if (!ValidationHelper.IsValidEmail(email))
        {
            errorMessage = "Please enter a valid email address";
            return;
        }

        errorMessage = "";
        isLoading = true;

        try
        {
            // Check if user exists and their auth methods
            var checkRequest = new CheckUserRequest { Username = email };
            var response = await AutoHostClient.PasskeyCheckUserAsync(checkRequest);
            isNewUser = !response.Exists;
            hasPassword = response.HasPassword;
            hasPasskeys = response.HasPasskeys;
        }
        catch (Exception)
        {
            // If AutoHost is not running, assume new user
            isNewUser = true;
            hasPassword = false;
            hasPasskeys = false;
        }
        finally
        {
            isLoading = false;
        }

        // Determine next step based on user's auth methods
        if (isNewUser)
        {
            // New user - show method selection ONLY if passkeys are supported
            if (passkeySupported)
            {
                authStep = AuthStep.MethodSelection;
            }
            else
            {
                // No passkey support, go straight to password creation
                authStep = AuthStep.Password;
                await Task.Delay(150);
                await JS.InvokeVoidAsync("eval", "document.getElementById('password')?.focus()");
            }
        }
        else
        {
            // Existing user - determine available auth methods
            if (!hasPassword && !hasPasskeys)
            {
                // Invalid state - user should have at least one auth method
                errorMessage = "Account configuration error. Please contact support.";
                authStep = AuthStep.Email;
                return;
            }

            if (!hasPassword && hasPasskeys && !passkeySupported)
            {
                // User locked out - only has passkeys but browser doesn't support them
                errorMessage = "Your account uses passkeys, but your browser doesn't support them. Please use a compatible browser.";
                errorCode = AutoWeb.Client.AuthErrorCode.PasskeyNotSupported;
                authStep = AuthStep.Email;
                return;
            }

            // Determine which step to show
            if (hasPassword && hasPasskeys && passkeySupported)
            {
                // Both methods available - show selection
                authStep = AuthStep.MethodSelection;
            }
            else if (hasPasskeys && !hasPassword && passkeySupported)
            {
                // Only passkeys (and they're supported) - auto-trigger
                authStep = AuthStep.Passkey;
                await Task.Delay(50);
                await AuthenticateWithPasskey();
            }
            else if (hasPassword)
            {
                // Has password (might also have unusable passkeys) - go to password
                authStep = AuthStep.Password;
                await Task.Delay(150);
                await JS.InvokeVoidAsync("eval", "document.getElementById('password')?.focus()");
            }
        }

        await Task.Delay(50); // Small delay for smooth animation
    }

    private async Task Authenticate()
    {
        if (!CanSubmitPassword())
            return;

        errorMessage = "";
        isLoading = true;

        try
        {
            if (isNewUser)
            {
                // Register new user
                var registerRequest = new RegisterRequest { Username = email, Password = password };
                await AutoHostClient.AuthRegisterAsync(registerRequest);
            }

            // Login
            var loginRequest = new LoginRequest { Username = email, Password = password };
            var loginResponse = await AutoHostClient.AuthLoginAsync(loginRequest);

            authToken = loginResponse.Token;
            var userId = loginResponse.UserId;

            // Store auth info in session storage
            await JS.InvokeVoidAsync("sessionStorage.setItem", "authToken", authToken);
            await JS.InvokeVoidAsync("sessionStorage.setItem", "userId", userId.ToString());
            await JS.InvokeVoidAsync("sessionStorage.setItem", "userEmail", email);

            // Navigate to main app
            Navigation.NavigateTo("/");
        }
        catch (ApiException<ErrorResponse> apiEx)
        {
            errorMessage = apiEx.Result?.Error ?? "An error occurred";
            errorCode = apiEx.Result?.ErrorCode;
            if (apiEx.StatusCode == 401)
            {
                password = "";
                confirmPassword = "";
            }
        }
        catch (HttpRequestException)
        {
            errorMessage = "Cannot connect to AutoHost. Please ensure it's running on port 5050.";
        }
        catch (Exception ex)
        {
            errorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ResetToEmail()
    {
        authStep = AuthStep.Email;
        password = "";
        confirmPassword = "";
        errorMessage = "";
        errorCode = null;
        passkeyFailed = false;
    }

    private void SelectPasswordMethod()
    {
        authStep = AuthStep.Password;
        errorMessage = "";
        errorCode = null;
        passkeyFailed = false;
        InvokeAsync(async () =>
        {
            await Task.Delay(150);
            await JS.InvokeVoidAsync("eval", "document.getElementById('password')?.focus()");
        });
    }

    private void SelectPasskeyMethod()
    {
        authStep = AuthStep.Passkey;
        errorMessage = "";
        errorCode = null;
        passkeyFailed = false;
        InvokeAsync(async () =>
        {
            await Task.Delay(50);
            await AuthenticateWithPasskey();
        });
    }

    private async Task AuthenticateWithPasskey()
    {
        isLoading = true;
        errorMessage = "";
        errorCode = null;
        passkeyFailed = false;

        try
        {
            var (success, token, code, error) = await PasskeyService.AuthenticateWithPasskeyAsync(email);

            if (success && !string.IsNullOrEmpty(token))
            {
                authToken = token;

                // Store auth info in session storage
                await JS.InvokeVoidAsync("sessionStorage.setItem", "authToken", authToken);
                await JS.InvokeVoidAsync("sessionStorage.setItem", "userEmail", email);

                // Navigate to main app
                Navigation.NavigateTo("/");
            }
            else
            {
                errorMessage = error ?? "Passkey authentication failed";
                errorCode = code;
                passkeyFailed = true;
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            errorCode = null; // JS/Network errors don't have error codes
            passkeyFailed = true;
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task RetryPasskey()
    {
        passkeyFailed = false;
        errorMessage = "";
        errorCode = null;
        if (isNewUser)
        {
            await RegisterWithPasskey();
        }
        else
        {
            await AuthenticateWithPasskey();
        }
    }

    private string GetUserFriendlyErrorTitle(AutoWeb.Client.AuthErrorCode? errorCode = null)
    {
        if (errorCode == null)
        {
            // Fallback for network/connection errors only (not API errors)
            if (errorMessage.Contains("Cannot connect", StringComparison.OrdinalIgnoreCase))
            {
                return "Connection Error";
            }
            return "Authentication Failed";
        }

        return errorCode.Value switch
        {
            AutoWeb.Client.AuthErrorCode.AuthenticationCancelled => "Authentication Cancelled",
            AutoWeb.Client.AuthErrorCode.WrongPasskeySelected => "Wrong Account Selected",
            AutoWeb.Client.AuthErrorCode.InvalidOrExpiredChallenge => "Session Expired",
            AutoWeb.Client.AuthErrorCode.InvalidCredentials => "Invalid Credentials",
            AutoWeb.Client.AuthErrorCode.CannotConnect => "Connection Error",
            AutoWeb.Client.AuthErrorCode.UsernameAlreadyExists => "Account Already Exists",
            AutoWeb.Client.AuthErrorCode.PasskeyNotSupported => "Browser Not Supported",
            _ => "Authentication Failed"
        };
    }

    private string GetUserFriendlyErrorMessage(AutoWeb.Client.AuthErrorCode? errorCode = null)
    {
        if (errorCode == null)
        {
            // Fallback for network/connection errors only (not API errors)
            if (errorMessage.Contains("Cannot connect", StringComparison.OrdinalIgnoreCase))
            {
                return "Unable to connect to the server. Please ensure AutoHost is running on port 5050.";
            }
            return errorMessage;
        }

        return errorCode.Value switch
        {
            AutoWeb.Client.AuthErrorCode.AuthenticationCancelled => "You cancelled the authentication request. Click 'Try Again' to retry.",
            AutoWeb.Client.AuthErrorCode.WrongPasskeySelected => $"The passkey you selected doesn't belong to {email}. Please select the correct passkey or use a different sign-in method.",
            AutoWeb.Client.AuthErrorCode.InvalidOrExpiredChallenge => "Your session has expired. Please try again.",
            AutoWeb.Client.AuthErrorCode.InvalidCredentials => "The password you entered is incorrect. Please try again.",
            AutoWeb.Client.AuthErrorCode.CannotConnect => "Unable to connect to the server. Please ensure AutoHost is running on port 5050.",
            AutoWeb.Client.AuthErrorCode.UsernameAlreadyExists => "An account with this email already exists. Please sign in or use a different email.",
            AutoWeb.Client.AuthErrorCode.PasskeyNotSupported => "Your account uses passkeys for authentication, but your current browser doesn't support them. Please use Chrome, Safari, Edge, or another browser with passkey support.",
            AutoWeb.Client.AuthErrorCode.PasskeyAuthenticationFailed => "Unable to authenticate with your passkey. You can try again or use a different sign-in method.",
            _ => "Something went wrong. Please try again or contact support if the problem persists."
        };
    }

    private async Task RegisterWithPasskey()
    {
        isLoading = true;
        errorMessage = "";
        errorCode = null;
        passkeyFailed = false;

        try
        {
            // Get challenge from server
            var challengeResponse = await AutoHostClient.PasskeyChallengeAsync();
            if (string.IsNullOrEmpty(challengeResponse.Challenge))
            {
                errorMessage = "Failed to get challenge from server";
                passkeyFailed = true;
                return;
            }

            // Create passkey via WebAuthn
            var passkeyData = await JS.InvokeAsync<PasskeyCreationResult?>(
                "PasskeySupport.createPasskey",
                email,
                challengeResponse.Challenge,
                "localhost");

            if (passkeyData == null)
            {
                errorMessage = "Failed to create passkey";
                passkeyFailed = true;
                return;
            }

            // Register new user with passkey
            var request = new RegisterNewUserPasskeyRequest
            {
                Username = email,
                CredentialId = passkeyData.CredentialId,
                AttestationObject = passkeyData.AttestationObject,
                ClientDataJson = passkeyData.ClientDataJSON,
                DeviceName = "Browser Passkey"
            };

            var result = await AutoHostClient.PasskeyRegisterAsync(request);

            if (result.Success && !string.IsNullOrEmpty(result.Token))
            {
                // Store auth info
                await JS.InvokeVoidAsync("sessionStorage.setItem", "authToken", result.Token);
                await JS.InvokeVoidAsync("sessionStorage.setItem", "userId", result.UserId.ToString());
                await JS.InvokeVoidAsync("sessionStorage.setItem", "userEmail", email);

                // Navigate to main app
                Navigation.NavigateTo("/");
            }
            else
            {
                errorMessage = "Failed to register with passkey";
                passkeyFailed = true;
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            passkeyFailed = true;
        }
        finally
        {
            isLoading = false;
        }
    }

    // Data model for JavaScript interop
    public class PasskeyCreationResult
    {
        public string CredentialId { get; set; } = "";
        public string PublicKey { get; set; } = "";
        public string AttestationObject { get; set; } = "";
        public string ClientDataJSON { get; set; } = "";
        public string UserHandle { get; set; } = "";
    }

    protected override async Task OnInitializedAsync()
    {
        // Check if passkeys are supported
        passkeySupported = await PasskeyService.IsSupported();

        // Start checking connection status
        await CheckConnection();

        // Set up periodic connection checks every 3 seconds
        connectionCheckTimer = new System.Threading.Timer(
            async _ => await InvokeAsync(async () =>
            {
                await CheckConnection();
                StateHasChanged();
            }),
            null,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(3)
        );
    }

    private async Task CheckConnection()
    {
        try
        {
            var response = await AutoHostClient.GetVersionAsync();
            isConnected = true;
            apiVersion = response.Version;

            // Stop polling once connected
            if (connectionCheckTimer != null)
            {
                connectionCheckTimer.Dispose();
                connectionCheckTimer = null;
            }
        }
        catch
        {
            isConnected = false;
            apiVersion = "";
        }
    }

    public void Dispose()
    {
        connectionCheckTimer?.Dispose();
    }
}