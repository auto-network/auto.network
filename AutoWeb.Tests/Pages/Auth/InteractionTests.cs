using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace AutoWeb.Tests.Pages;

/// <summary>
/// Playwright-based interaction tests for Auth.razor component.
/// Tests complete end-to-end authentication workflows in a real browser.
/// </summary>
[Collection("Playwright")]
public class AuthInteractionTests
{
    private readonly ITestOutputHelper _output;
    private readonly PlaywrightFixture _fixture;

    public AuthInteractionTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task NewUser_PasswordRegistration_Success()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to test page with new-user-passkey-supported state
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=new-user-passkey-supported&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "newuser@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");
            await page.WaitForTimeoutAsync(500);

            // Verify method selection appears
            var passwordButton = page.Locator("button:has-text('🔑')");
            await Assertions.Expect(passwordButton).ToBeVisibleAsync();

            // Click Password option
            await passwordButton.ClickAsync();
            await page.WaitForTimeoutAsync(500);

            // Verify password creation form appears
            await Assertions.Expect(page.Locator("input[type='password']").First).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("button[type='submit']:has-text('Create Account')")).ToBeVisibleAsync();

            // Fill password and confirm password
            var passwordInputs = await page.Locator("input[type='password']").AllAsync();
            await passwordInputs[0].FillAsync("TestPassword123!");
            await passwordInputs[1].FillAsync("TestPassword123!");

            // Click Create Account
            await page.ClickAsync("button[type='submit']:has-text('Create Account')");
            await page.WaitForTimeoutAsync(1000);

            // Verify sessionStorage has authToken (indicates success)
            var token = await page.EvaluateAsync<string>("sessionStorage.getItem('authToken')");
            var userId = await page.EvaluateAsync<string>("sessionStorage.getItem('userId')");
            var userEmail = await page.EvaluateAsync<string>("sessionStorage.getItem('userEmail')");

            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.NotNull(userId);
            Assert.Equal("newuser@example.com", userEmail);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task NewUser_PasskeyRegistration_Success()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to test page with new-user-passkey-supported state
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=new-user-passkey-supported&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "newuser@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");
            await page.WaitForTimeoutAsync(500);

            // Verify method selection appears
            var passkeyButton = page.Locator("button:has-text('🔐')");
            await Assertions.Expect(passkeyButton).ToBeVisibleAsync();

            // Click Passkey option
            await passkeyButton.ClickAsync();

            // Wait for authentication to complete
            await page.WaitForTimeoutAsync(1500);

            // Verify sessionStorage has authToken (automated=true triggers mock success)
            var token = await page.EvaluateAsync<string>("sessionStorage.getItem('authToken')");
            var userId = await page.EvaluateAsync<string>("sessionStorage.getItem('userId')");
            var userEmail = await page.EvaluateAsync<string>("sessionStorage.getItem('userEmail')");

            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.NotNull(userId);
            Assert.Equal("newuser@example.com", userEmail);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task NewUser_NoPasskeySupport_PasswordOnly()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to test page with new-user-passkey-not-supported state
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=new-user-passkey-not-supported&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "newuser@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");
            await page.WaitForTimeoutAsync(500);

            // Verify password creation form appears (no method selection)
            await Assertions.Expect(page.Locator("input[type='password']").First).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("button[type='submit']:has-text('Create Account')")).ToBeVisibleAsync();

            // Fill password and confirm password
            var passwordInputs = await page.Locator("input[type='password']").AllAsync();
            await passwordInputs[0].FillAsync("TestPassword123!");
            await passwordInputs[1].FillAsync("TestPassword123!");

            // Click Create Account
            await page.ClickAsync("button[type='submit']:has-text('Create Account')");
            await page.WaitForTimeoutAsync(1000);

            // Verify sessionStorage has authToken
            var token = await page.EvaluateAsync<string>("sessionStorage.getItem('authToken')");
            var userId = await page.EvaluateAsync<string>("sessionStorage.getItem('userId')");
            var userEmail = await page.EvaluateAsync<string>("sessionStorage.getItem('userEmail')");

            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.NotNull(userId);
            Assert.Equal("newuser@example.com", userEmail);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ExistingUser_PasswordLogin_Success()
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

            // Verify password login form appears (no method selection)
            await Assertions.Expect(page.Locator("input[type='password']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("button[type='submit']:has-text('Sign In')")).ToBeVisibleAsync();

            // Fill password
            await page.FillAsync("input[type='password']", "password123");

            // Click Sign In
            await page.ClickAsync("button[type='submit']:has-text('Sign In')");
            await page.WaitForTimeoutAsync(1000);

            // Verify sessionStorage has authToken
            var token = await page.EvaluateAsync<string>("sessionStorage.getItem('authToken')");
            var userId = await page.EvaluateAsync<string>("sessionStorage.getItem('userId')");
            var userEmail = await page.EvaluateAsync<string>("sessionStorage.getItem('userEmail')");

            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.NotNull(userId);
            Assert.Equal("test@example.com", userEmail);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ExistingUser_PasskeyAutoTrigger_Success()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to test page with existing-passkey-only state
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=existing-passkey-only&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "test@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");

            // Passkey should auto-trigger, wait for completion
            await page.WaitForTimeoutAsync(1500);

            // Verify sessionStorage has authToken
            var token = await page.EvaluateAsync<string>("sessionStorage.getItem('authToken')");
            var userEmail = await page.EvaluateAsync<string>("sessionStorage.getItem('userEmail')");

            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.Equal("test@example.com", userEmail);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ExistingUser_MethodSelection_ChoosePassword_Success()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to test page with existing-password-and-passkey state
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=existing-password-and-passkey&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "test@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");
            await page.WaitForTimeoutAsync(500);

            // Verify method selection appears
            var passwordButton = page.Locator("button:has-text('🔑')");
            await Assertions.Expect(passwordButton).ToBeVisibleAsync();

            // Click Password option
            await passwordButton.ClickAsync();
            await page.WaitForTimeoutAsync(500);

            // Verify password login form appears
            await Assertions.Expect(page.Locator("input[type='password']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("button[type='submit']:has-text('Sign In')")).ToBeVisibleAsync();

            // Fill password and sign in
            await page.FillAsync("input[type='password']", "password123");
            await page.ClickAsync("button[type='submit']:has-text('Sign In')");
            await page.WaitForTimeoutAsync(1000);

            // Verify sessionStorage has authToken
            var token = await page.EvaluateAsync<string>("sessionStorage.getItem('authToken')");
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ExistingUser_MethodSelection_ChoosePasskey_Success()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to test page with existing-password-and-passkey state
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=existing-password-and-passkey&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "test@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");
            await page.WaitForTimeoutAsync(500);

            // Verify method selection appears
            var passkeyButton = page.Locator("button:has-text('🔐')");
            await Assertions.Expect(passkeyButton).ToBeVisibleAsync();

            // Click Passkey option
            await passkeyButton.ClickAsync();

            // Wait for authentication to complete
            await page.WaitForTimeoutAsync(1500);

            // Verify sessionStorage has authToken
            var token = await page.EvaluateAsync<string>("sessionStorage.getItem('authToken')");
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ExistingUser_PasswordAndPasskey_NoPasskeySupport_PasswordOnly()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to test page with existing-password-and-passkey-not-supported state
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=Auth&state=existing-password-and-passkey-not-supported&automated=true",
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1500);

            // Fill email and continue
            await page.FillAsync("input[type='email']", "test@example.com");
            await page.ClickAsync("button[type='submit']:has-text('Continue')");
            await page.WaitForTimeoutAsync(500);

            // Verify password login form appears directly (no method selection, no passkey option)
            await Assertions.Expect(page.Locator("input[type='password']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("button[type='submit']:has-text('Sign In')")).ToBeVisibleAsync();

            // Verify passkey button does NOT appear
            var passkeyButton = page.Locator("button:has-text('🔐')");
            await Assertions.Expect(passkeyButton).Not.ToBeVisibleAsync();

            // Fill password and sign in
            await page.FillAsync("input[type='password']", "password123");
            await page.ClickAsync("button[type='submit']:has-text('Sign In')");
            await page.WaitForTimeoutAsync(1000);

            // Verify sessionStorage has authToken
            var token = await page.EvaluateAsync<string>("sessionStorage.getItem('authToken')");
            var userId = await page.EvaluateAsync<string>("sessionStorage.getItem('userId')");
            var userEmail = await page.EvaluateAsync<string>("sessionStorage.getItem('userEmail')");

            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.NotNull(userId);
            Assert.Equal("test@example.com", userEmail);
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
