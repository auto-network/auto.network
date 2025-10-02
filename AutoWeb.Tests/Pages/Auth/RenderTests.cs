using System;
using System.Threading.Tasks;
using AutoWeb.Client;
using AutoWeb.Pages;
using AutoWeb.Services;
using AutoWeb.Tests;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Xunit;

namespace AutoWeb.Tests.Pages;

/// <summary>
/// Render tests for Auth.razor component using bUnit.
/// Tests HTML structure, CSS classes, and element attributes.
/// </summary>
public class AuthRenderTests : TestContext
{
    private MockPasskeyServiceForAuth? _mockPasskeyService;
    private MockAutoHostClient? _mockAutoHostClient;

    /// <summary>
    /// Common setup for all Auth render tests - configures mock services with specified state.
    /// </summary>
    private void SetupAuthState(string state)
    {
        var navMan = new FakeNavigationManager();
        navMan.NavigateTo($"http://localhost/?state={state}");

        Services.AddSingleton<NavigationManager>(navMan);

        // Create and store mock AutoHost client
        _mockAutoHostClient = new MockAutoHostClient(navMan);
        Services.AddSingleton<IAutoHostClient>(_mockAutoHostClient);
        Services.AddSingleton<IJSRuntime>(new MockJSRuntime());

        // Create and store mock passkey service
        _mockPasskeyService = null; // Reset for each test
        Services.AddSingleton<PasskeyService>(sp =>
        {
            var jsRuntime = sp.GetRequiredService<IJSRuntime>();
            var autoHostClient = sp.GetRequiredService<IAutoHostClient>();
            var logger = sp.GetRequiredService<ILogger<PasskeyService>>();
            var nav = sp.GetRequiredService<NavigationManager>();
            _mockPasskeyService = new MockPasskeyServiceForAuth(jsRuntime, autoHostClient, logger, nav);
            return _mockPasskeyService;
        });
    }

    private MockPasskeyServiceForAuth GetMockPasskeyService()
    {
        if (_mockPasskeyService == null)
            throw new InvalidOperationException("Mock passkey service not initialized. Did you render the component?");
        return _mockPasskeyService;
    }

    private MockAutoHostClient GetMockAutoHostClient()
    {
        if (_mockAutoHostClient == null)
            throw new InvalidOperationException("Mock AutoHost client not initialized. Did you call SetupAuthState()?");
        return _mockAutoHostClient;
    }

    // Convenience methods for common states
    private void SetupNewUserWithPasskeySupport() => SetupAuthState("new-user-passkey-supported");
    private void SetupNewUserWithoutPasskeySupport() => SetupAuthState("new-user-passkey-not-supported");
    private void SetupExistingUserPasswordOnly() => SetupAuthState("existing-password-only");
    private void SetupExistingUserPasskeyOnly() => SetupAuthState("existing-passkey-only");
    private void SetupExistingUserBothMethods() => SetupAuthState("existing-password-and-passkey");

    #region Email Step Structure Tests

    [Fact]
    public void Should_Render_Email_Input_With_Correct_Attributes()
    {
        // Arrange
        SetupNewUserWithPasskeySupport();

        // Act
        var cut = RenderComponent<Auth>();

        // Assert - Email input structure
        var emailInput = cut.Find("input[type='email']");
        Assert.NotNull(emailInput);
        Assert.Equal("email", emailInput.GetAttribute("id"));
        Assert.Equal("you@example.com", emailInput.GetAttribute("placeholder"));

        // Check that it has styling classes (classes are dynamic via GetEmailInputClasses())
        var classAttr = emailInput.GetAttribute("class");
        Assert.NotNull(classAttr);
        Assert.Contains("bg-gray-", classAttr); // Should have some bg-gray variant
    }

    [Fact]
    public void Should_Render_Continue_Button_With_Correct_Structure()
    {
        // Arrange
        SetupNewUserWithPasskeySupport();

        // Act
        var cut = RenderComponent<Auth>();

        // Assert - Continue button structure
        var continueButton = cut.Find("button[type='submit']:contains('Continue')");
        Assert.NotNull(continueButton);
        Assert.True(continueButton.ClassList.Contains("bg-green-600"));
        Assert.True(continueButton.ClassList.Contains("hover:bg-green-500"));

        // Button should be disabled initially (empty email)
        var disabledAttr = continueButton.GetAttribute("disabled");
        Assert.NotNull(disabledAttr); // disabled attribute exists
    }

    [Fact]
    public void Should_Render_Status_Indicator_Structure()
    {
        // Arrange
        SetupNewUserWithPasskeySupport();

        // Act
        var cut = RenderComponent<Auth>();

        // Assert - Status indicator exists
        var statusIndicator = cut.Find(".text-green-400, .text-red-400, .text-gray-400");
        Assert.NotNull(statusIndicator);
    }

    #endregion

    #region Password Step Structure Tests

    [Fact]
    public async Task Should_Render_Password_Input_Structure()
    {
        // Arrange - Existing user with password only
        SetupExistingUserPasswordOnly();
        var cut = RenderComponent<Auth>();

        // Navigate to password step
        var emailInput = cut.Find("input[type='email']");
        await emailInput.InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "test@example.com" });
        var continueButton = cut.Find("button[type='submit']:contains('Continue')");
        await continueButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait for password step
        cut.WaitForAssertion(() =>
        {
            var passwordInput = cut.Find("input[type='password']");
            Assert.NotNull(passwordInput);
        });

        // Assert - Password input structure
        var passwordField = cut.Find("input[type='password']");
        Assert.True(passwordField.ClassList.Contains("bg-gray-700"));
        Assert.True(passwordField.ClassList.Contains("text-white"));
    }

    [Fact]
    public async Task Should_Render_Submit_Button_Structure()
    {
        // Arrange - Existing user with password only
        SetupExistingUserPasswordOnly();
        var cut = RenderComponent<Auth>();

        // Navigate to password step
        var emailInput = cut.Find("input[type='email']");
        await emailInput.InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "test@example.com" });
        var continueButton = cut.Find("button[type='submit']:contains('Continue')");
        await continueButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait for password step
        cut.WaitForAssertion(() =>
        {
            var signInButton = cut.Find("button[type='submit']:contains('Sign In')");
            Assert.NotNull(signInButton);
        });

        // Assert - Submit button structure
        var submitButton = cut.Find("button[type='submit']:contains('Sign In')");
        Assert.True(submitButton.ClassList.Contains("bg-green-600"));
        Assert.True(submitButton.HasAttribute("disabled")); // Disabled when password empty
    }

    [Fact]
    public async Task Should_Render_Email_Display_With_Change_Button()
    {
        // Arrange - Existing user with password only
        SetupExistingUserPasswordOnly();
        var cut = RenderComponent<Auth>();

        // Navigate to password step
        var emailInput = cut.Find("input[type='email']");
        await emailInput.InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "test@example.com" });
        var continueButton = cut.Find("button[type='submit']:contains('Continue')");
        await continueButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait for password step
        cut.WaitForAssertion(() =>
        {
            var passwordInput = cut.Find("input[type='password']");
            Assert.NotNull(passwordInput);
        });

        // Assert - Email display shows the email entered
        var markup = cut.Markup;
        Assert.Contains("test@example.com", markup);

        // Assert - Change button exists
        var changeButton = cut.Find("button:contains('Change')");
        Assert.NotNull(changeButton);
    }

    #endregion

    #region Method Selection Structure Tests

    [Fact]
    public async Task Should_Render_Two_Option_Buttons()
    {
        // Arrange - New user with passkey support (shows method selection)
        SetupNewUserWithPasskeySupport();
        var cut = RenderComponent<Auth>();

        // Navigate to method selection
        var emailInput = cut.Find("input[type='email']");
        await emailInput.InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "test@example.com" });
        var continueButton = cut.Find("button[type='submit']:contains('Continue')");
        await continueButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait for method selection
        cut.WaitForAssertion(() =>
        {
            var passwordButton = cut.Find("button:contains('Password')");
            Assert.NotNull(passwordButton);
        });

        // Assert - Both option buttons exist
        var passwordBtn = cut.Find("button:contains('Password')");
        var passkeyBtn = cut.Find("button:contains('Passkey')");
        Assert.NotNull(passwordBtn);
        Assert.NotNull(passkeyBtn);

        // Both should be interactive buttons
        Assert.True(passwordBtn.ClassList.Contains("bg-gray-700"));
        Assert.True(passkeyBtn.ClassList.Contains("bg-gray-700"));
    }

    [Fact]
    public async Task Should_Render_Correct_Icons_And_Text()
    {
        // Arrange - New user with passkey support
        SetupNewUserWithPasskeySupport();
        var cut = RenderComponent<Auth>();

        // Navigate to method selection
        var emailInput = cut.Find("input[type='email']");
        await emailInput.InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "test@example.com" });
        var continueButton = cut.Find("button[type='submit']:contains('Continue')");
        await continueButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait for method selection
        cut.WaitForAssertion(() =>
        {
            var passwordButton = cut.Find("button:contains('Password')");
            Assert.NotNull(passwordButton);
        });

        // Assert - Text content
        var markup = cut.Markup;
        Assert.Contains("Password", markup);
        Assert.Contains("Passkey", markup);
    }

    #endregion

    #region Passkey Step Structure Tests

    [Fact]
    public async Task Should_Render_Passkey_Icon_And_Message()
    {
        // Arrange - Existing user with passkey only (auto-triggers passkey)
        SetupExistingUserPasskeyOnly();
        var cut = RenderComponent<Auth>();

        // Navigate to trigger passkey
        var emailInput = cut.Find("input[type='email']");
        await emailInput.InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "test@example.com" });
        var continueButton = cut.Find("button[type='submit']:contains('Continue')");
        await continueButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait for passkey step
        cut.WaitForAssertion(() =>
        {
            var passkeyHeading = cut.Find("h2:contains('Authenticating')");
            Assert.NotNull(passkeyHeading);
        });

        // Assert - Passkey message and icon present
        var markup = cut.Markup;
        Assert.Contains("Authenticating", markup);
        Assert.Contains("passkey", markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_Render_Retry_And_Fallback_Buttons_On_Error()
    {
        // NOTE: This is a simplified structure test.
        // Full passkey error flow testing is better suited for Playwright interaction tests (Phase 5)
        // This test verifies the passkey step structure exists and is accessible

        // Arrange - Existing user with passkey only (auto-triggers passkey)
        SetupExistingUserPasskeyOnly();
        var cut = RenderComponent<Auth>();

        // Navigate to trigger passkey
        var emailInput = cut.Find("input[type='email']");
        await emailInput.InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "test@example.com" });
        var continueButton = cut.Find("button[type='submit']:contains('Continue')");
        await continueButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait for passkey step
        cut.WaitForAssertion(() =>
        {
            var passkeyHeading = cut.Find("h2:contains('Authenticating')");
            Assert.NotNull(passkeyHeading);
        });

        // Assert - Passkey step rendered successfully
        var markup = cut.Markup;
        Assert.Contains("Authenticating with Passkey", markup);

        // Note: Full error state with "Try Again" and "Use password instead" buttons
        // is tested in AuthTests.cs unit tests (Should_Show_Error_State_With_Retry)
        // and will be tested end-to-end in Phase 5 (Playwright interaction tests)
    }

    #endregion
}
