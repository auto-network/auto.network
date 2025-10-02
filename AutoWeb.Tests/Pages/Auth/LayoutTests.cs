using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace AutoWeb.Tests.Pages;

/// <summary>
/// Playwright-based layout tests for Auth.razor component.
/// Tests visual rendering and layout correctness in a real browser.
/// </summary>
[Collection("Playwright")]
public class AuthLayoutTests
{
    private readonly ITestOutputHelper _output;
    private readonly PlaywrightFixture _fixture;

    public AuthLayoutTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    #region Email Step Layout Tests

    [Fact]
    public async Task EmailStep_ShouldRender_WithVisibleElements()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to test page with new-user-passkey-supported state
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=new-user-passkey-supported&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500); // Wait for Blazor WASM

            // Assert - Email input visible
            var emailInput = page.Locator("input[type='email']");
            await Assertions.Expect(emailInput).ToBeVisibleAsync();

            // Assert - Continue button visible
            var continueButton = page.Locator("button[type='submit']:has-text('Continue')");
            await Assertions.Expect(continueButton).ToBeVisibleAsync();

            // Assert - Continue button disabled (empty email)
            await Assertions.Expect(continueButton).ToBeDisabledAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EmailStep_ShouldEnableContinueButton_WithValidEmail()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to test page with new-user-passkey-supported state
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=new-user-passkey-supported&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email
            await page.FillAsync("input[type='email']", "test@example.com");

            // Assert - Continue button enabled
            var continueButton = page.Locator("button[type='submit']:has-text('Continue')");
            await Assertions.Expect(continueButton).ToBeEnabledAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EmailStep_ShouldShowError_WithInvalidEmail()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to test page with new-user-passkey-supported state
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=new-user-passkey-supported&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill invalid email
            await page.FillAsync("input[type='email']", "invalid-email");

            // Assert - Error message visible
            var errorMessage = page.Locator("p.text-red-400:has-text('valid email')");
            await Assertions.Expect(errorMessage).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    #endregion

    #region Password Step Layout Tests

    [Fact]
    public async Task PasswordStep_ShouldRender_ForExistingUser()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to test page with existing-password-only state
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=existing-password-only&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "test@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");
            await page.WaitForTimeoutAsync(500);

            // Assert - Password input visible
            var passwordInput = page.Locator("input[type='password']");
            await Assertions.Expect(passwordInput).ToBeVisibleAsync();

            // Assert - Sign In button visible and disabled (empty password)
            var signInButton = page.Locator("button[type='submit']:has-text('Sign In')");
            await Assertions.Expect(signInButton).ToBeVisibleAsync();
            await Assertions.Expect(signInButton).ToBeDisabledAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task PasswordStep_ShouldShowEmailWithChangeButton()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=existing-password-only&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "test@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");
            await page.WaitForTimeoutAsync(500);

            // Assert - Email displayed
            var emailDisplay = page.Locator("text=test@example.com");
            await Assertions.Expect(emailDisplay).ToBeVisibleAsync();

            // Assert - Change button visible
            var changeButton = page.Locator("button:has-text('Change')");
            await Assertions.Expect(changeButton).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task PasswordStep_ShouldEnableSubmit_WithPassword()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=existing-password-only&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "test@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");
            await page.WaitForTimeoutAsync(500);

            // Fill password
            await page.FillAsync("input[type='password']", "TestPassword123!");

            // Assert - Sign In button enabled
            var signInButton = page.Locator("button[type='submit']:has-text('Sign In')");
            await Assertions.Expect(signInButton).ToBeEnabledAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    #endregion

    #region Method Selection Layout Tests

    [Fact]
    public async Task MethodSelection_ShouldShowBothOptions_ForNewUser()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=new-user-passkey-supported&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "newuser@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");
            await page.WaitForTimeoutAsync(500);

            // Assert - Password button visible (use emoji to be specific)
            var passwordButton = page.Locator("button:has-text('🔑')");
            await Assertions.Expect(passwordButton).ToBeVisibleAsync();

            // Assert - Passkey button visible
            var passkeyButton = page.Locator("button:has-text('🔐')");
            await Assertions.Expect(passkeyButton).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task MethodSelection_ButtonsShouldBeClickable()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=new-user-passkey-supported&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "newuser@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");
            await page.WaitForTimeoutAsync(500);

            // Assert - Both buttons enabled (clickable) - use emojis for specificity
            var passwordButton = page.Locator("button:has-text('🔑')");
            var passkeyButton = page.Locator("button:has-text('🔐')");

            await Assertions.Expect(passwordButton).ToBeEnabledAsync();
            await Assertions.Expect(passkeyButton).ToBeEnabledAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    #endregion
}
