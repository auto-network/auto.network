using Microsoft.JSInterop;
using AutoWeb.Client;

namespace AutoWeb.Services;

public class PasskeyService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IAutoHostClient _apiClient;
    private readonly ILogger<PasskeyService> _logger;

    public PasskeyService(IJSRuntime jsRuntime, IAutoHostClient apiClient, ILogger<PasskeyService> logger)
    {
        _jsRuntime = jsRuntime;
        _apiClient = apiClient;
        _logger = logger;
    }

    public virtual async Task<bool> IsSupported()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("PasskeySupport.isSupported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking passkey support");
            return false;
        }
    }

    public async Task<(bool Success, string? Error)> RegisterPasskeyAsync(string? deviceName = null)
    {
        try
        {
            // Get challenge from server
            var challengeResponse = await _apiClient.PasskeyChallengeAsync();
            if (string.IsNullOrEmpty(challengeResponse.Challenge))
            {
                return (false, "Failed to get challenge from server");
            }

            // Get current user info (we should have this from the auth state)
            var user = await _apiClient.AuthGetApiKeyAsync();
            if (string.IsNullOrEmpty(user.ApiKey))
            {
                return (false, "User not authenticated");
            }

            // Create passkey via WebAuthn
            var passkeyData = await _jsRuntime.InvokeAsync<PasskeyCreationResult?>(
                "PasskeySupport.createPasskey",
                user.ApiKey, // Using email as username for now
                challengeResponse.Challenge,
                "localhost");

            if (passkeyData == null)
            {
                return (false, "Failed to create passkey");
            }

            // Register with server
            var request = new RegisterPasskeyRequest
            {
                CredentialId = passkeyData.CredentialId,
                AttestationObject = passkeyData.AttestationObject,
                ClientDataJson = passkeyData.ClientDataJSON,
                DeviceName = deviceName
            };

            var result = await _apiClient.PasskeyEnrollAsync(request);
            return (result.Success, result.Success ? null : "Failed to register passkey");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering passkey");
            return (false, ex.Message);
        }
    }

    public virtual async Task<(bool Success, string? Token, AuthErrorCode? ErrorCode, string? Error)> AuthenticateWithPasskeyAsync(string username)
    {
        try
        {
            // Check if user has passkeys
            var checkUserResponse = await _apiClient.AuthCheckUserAsync(new CheckUserRequest { Username = username });

            if (!checkUserResponse.Exists)
            {
                return (false, null, AuthErrorCode.UserNotFound, "No passkeys registered for this user");
            }

            // Get challenge from server
            var challengeResponse = await _apiClient.PasskeyChallengeAsync();
            if (string.IsNullOrEmpty(challengeResponse.Challenge))
            {
                return (false, null, AuthErrorCode.InvalidOrExpiredChallenge, "Failed to get challenge from server");
            }

            // Get passkey via WebAuthn
            var passkeyData = await _jsRuntime.InvokeAsync<PasskeyAuthenticationResult?>(
                "PasskeySupport.getPasskey",
                challengeResponse.Challenge,
                Array.Empty<string>(), // Let browser choose from available credentials
                "localhost");

            if (passkeyData == null)
            {
                return (false, null, AuthErrorCode.PasskeyAuthenticationFailed, "Failed to get passkey");
            }

            // Authenticate with server
            var request = new AuthenticatePasskeyRequest
            {
                CredentialId = passkeyData.CredentialId,
                AuthenticatorData = passkeyData.AuthenticatorData,
                ClientDataJson = passkeyData.ClientDataJSON,
                Signature = passkeyData.Signature,
                UserHandle = passkeyData.UserHandle
            };

            var result = await _apiClient.PasskeyAuthenticateAsync(request);
            return (result.Success, result.Token, result.Success ? null : AuthErrorCode.PasskeyAuthenticationFailed, result.Success ? null : "Authentication failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with passkey");

            // Parse browser/JS errors to error codes
            var errorCode = ex.Message.Contains("User denied", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("canceled", StringComparison.OrdinalIgnoreCase)
                ? AuthErrorCode.AuthenticationCancelled
                : AuthErrorCode.PasskeyAuthenticationFailed;

            return (false, null, errorCode, ex.Message);
        }
    }

    public async Task<List<PasskeyInfo>> GetUserPasskeysAsync()
    {
        try
        {
            var response = await _apiClient.PasskeyListAsync();
            // For now, return empty list since Passkeys is an object type
            // This needs to be fixed in the backend to return a proper list
            return new List<PasskeyInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user passkeys");
            return new List<PasskeyInfo>();
        }
    }

    public async Task<bool> DeletePasskeyAsync(int id)
    {
        try
        {
            var result = await _apiClient.PasskeyDeleteAsync(id);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting passkey");
            return false;
        }
    }
}

// Data models for JavaScript interop
public class PasskeyCreationResult
{
    public string CredentialId { get; set; } = "";
    public string PublicKey { get; set; } = "";
    public string AttestationObject { get; set; } = "";
    public string ClientDataJSON { get; set; } = "";
    public string UserHandle { get; set; } = "";
}

public class PasskeyAuthenticationResult
{
    public string CredentialId { get; set; } = "";
    public string AuthenticatorData { get; set; } = "";
    public string ClientDataJSON { get; set; } = "";
    public string Signature { get; set; } = "";
    public string? UserHandle { get; set; }
}

public class PasskeyInfo
{
    public int Id { get; set; }
    public string DeviceName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}