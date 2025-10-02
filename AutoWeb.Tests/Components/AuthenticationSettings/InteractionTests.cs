using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace AutoWeb.Tests.Components;

/// <summary>
/// Interaction tests for AuthenticationSettings component.
/// Tests user workflows like adding/removing passwords and passkeys.
/// </summary>
[Collection("Playwright")]
public class AuthenticationSettingsInteractionTests
{
    private readonly ITestOutputHelper _output;
    private readonly PlaywrightFixture _fixture;

    public AuthenticationSettingsInteractionTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task CreatePassword_ThenDeletePasskey_ShouldShowNoPasskeysMessage()
    {
        var page = await _fixture.Browser.NewPageAsync();
        page.Console += (_, msg) => _output.WriteLine($"[Browser {msg.Type}] {msg.Text}");
        page.PageError += (_, err) => _output.WriteLine($"[Browser ERROR] {err}");

        try
        {
            _output.WriteLine("\n=== Test: Create Password then Delete Passkey ===");

            // Navigate to base URL to set sessionStorage
            await page.GotoAsync(PlaywrightFixture.BaseUrl);
            await page.EvaluateAsync("sessionStorage.setItem('userEmail', 'test@example.com')");

            // Navigate to single-passkey state
            var url = $"{PlaywrightFixture.BaseUrl}/test?state=single-passkey&automated=true";
            _output.WriteLine($"Navigating to: {url}");
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Wait for component to load
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1000);

            // Verify initial state: single passkey, no password
            _output.WriteLine("\n1. Verifying initial state (single passkey, no password)...");
            var createPasswordBtn = page.Locator("button:has-text('Create Password')");
            await createPasswordBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            _output.WriteLine("✓ 'Create Password' button found");

            var passkeyItem = page.Locator(".bg-gray-700.p-3:has-text('iPhone 15 Pro')");
            await passkeyItem.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            _output.WriteLine("✓ Passkey 'iPhone 15 Pro' is visible");

            // Click "Create Password"
            _output.WriteLine("\n2. Clicking 'Create Password' button...");
            await createPasswordBtn.ClickAsync();
            await page.WaitForTimeoutAsync(500);

            // Verify password dialog appeared
            var passwordInput = page.Locator("input[type='password']").First;
            await passwordInput.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            _output.WriteLine("✓ Password dialog appeared");

            // Fill in password - use PressSequentially instead of Fill for Blazor binding
            _output.WriteLine("\n3. Filling in password fields...");
            await passwordInput.PressSequentiallyAsync("TestPassword123!");
            var confirmInput = page.Locator("input[type='password']").Nth(1);
            await confirmInput.PressSequentiallyAsync("TestPassword123!");

            // Verify fields actually have values
            var pwValue = await passwordInput.InputValueAsync();
            var confirmValue = await confirmInput.InputValueAsync();
            _output.WriteLine($"Password field value: '{pwValue}'");
            _output.WriteLine($"Confirm field value: '{confirmValue}'");

            if (pwValue != "TestPassword123!" || confirmValue != "TestPassword123!")
            {
                throw new Exception($"Fields not filled correctly! pw='{pwValue}', confirm='{confirmValue}'");
            }
            _output.WriteLine("✓ Password fields filled and verified");

            // Click "Create" button in dialog (use exact text match to avoid "Create Password" button)
            _output.WriteLine("\n4. Clicking 'Create' button in dialog...");
            var createBtn = page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true });

            // Click the button
            await createBtn.ClickAsync();
            _output.WriteLine("✓ Clicked Create button");

            // Verify we now have "Remove Password" button (not "Create Password")
            _output.WriteLine("\n5. Verifying password was created...");
            var removePasswordBtn = page.Locator("button:has-text('Remove Password')");
            await removePasswordBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 500 });
            _output.WriteLine("✓ 'Remove Password' button now visible");

            // Verify Delete button is enabled (we have password as backup)
            var deleteBtn = page.Locator("button:has-text('Delete')").First;
            var deleteBtnDisabled = await deleteBtn.IsDisabledAsync();
            Assert.False(deleteBtnDisabled, "Delete button should be enabled when password exists");
            _output.WriteLine("✓ 'Delete' button is enabled");

            // Click Delete on the passkey
            _output.WriteLine("\n6. Clicking 'Delete' on passkey...");
            await deleteBtn.ClickAsync();

            // Verify success message
            var removedMsg = page.Locator("text=Passkey removed successfully");
            await removedMsg.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });
            _output.WriteLine("✓ 'Passkey removed successfully' message appeared");

            // Verify passkey is removed from list
            _output.WriteLine("\n7. Verifying passkey was removed...");
            await passkeyItem.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 200 });
            _output.WriteLine("✓ Passkey item removed from DOM");

            // Verify "No passkeys registered yet" message appears
            var noPasskeysMsg = page.Locator("text=No passkeys registered yet");
            await noPasskeysMsg.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });
            _output.WriteLine("✓ 'No passkeys registered yet' message appeared");

            // Verify password section now shows "Add passkey first" (can't remove password when no passkeys exist)
            var addPasskeyFirstBtn = page.Locator("button:has-text('Add passkey first')");
            await addPasskeyFirstBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });
            var isDisabledFinal = await addPasskeyFirstBtn.IsDisabledAsync();
            Assert.True(isDisabledFinal, "Button should be disabled - can't remove password without passkeys");
            _output.WriteLine("✓ Password section shows 'Add passkey first' button (disabled)");

            _output.WriteLine("\n✓✓✓ Test PASSED ✓✓✓");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"\n❌ Test FAILED: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");

            // Take screenshot on failure
            await page.ScreenshotAsync(new() { Path = "/tmp/test-failure.png" });
            _output.WriteLine($"Screenshot saved to /tmp/test-failure.png");

            throw;
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
