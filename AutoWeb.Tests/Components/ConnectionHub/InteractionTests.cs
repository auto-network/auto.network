using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace AutoWeb.Tests.Components.ConnectionHub;

[Collection("Playwright")]
public class ConnectionHubInteractionTests
{
    private readonly PlaywrightFixture _fixture;

    public ConnectionHubInteractionTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_Toggle_API_Key_Visibility()
    {
        var page = await _fixture.Browser.NewPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=ConnectionHub&state=single-connection&automated=true");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Initially, key should not be visible
            var keyDisplay = page.Locator(".font-mono.bg-gray-600");
            await keyDisplay.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 200 });

            // Click Show button
            var showButton = page.Locator("button:has-text('Show')");
            await showButton.ClickAsync();

            // Key should now be visible
            await keyDisplay.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

            // Button text should change to Hide
            var hideButton = page.Locator("button:has-text('Hide')");
            await hideButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

            // Click Hide button
            await hideButton.ClickAsync();

            // Key should be hidden again
            await keyDisplay.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 200 });
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Can_Delete_Connection()
    {
        var page = await _fixture.Browser.NewPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=ConnectionHub&state=single-connection&automated=true");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify connection exists
            var connectionItem = page.Locator(".bg-gray-700.p-3.rounded").First;
            await connectionItem.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

            // Click Delete button
            var deleteButton = page.Locator("button:has-text('Delete')");
            await deleteButton.ClickAsync();

            // Wait for success message
            var successMessage = page.Locator("text=deleted successfully");
            await successMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

            // Verify empty state message appears
            var emptyMessage = page.Locator("text=No connections configured");
            await emptyMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Can_Create_New_Connection()
    {
        var page = await _fixture.Browser.NewPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=ConnectionHub&state=empty&automated=true");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Click Add Connection button
            var addButton = page.Locator("button:has-text('Add Connection')");
            await addButton.ClickAsync();

            // Form should be visible
            var form = page.Locator(".bg-gray-700.p-4.rounded-lg");
            await form.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

            // Fill in API key and trigger change event
            var keyInput = page.Locator("input[type='password']");
            await keyInput.PressSequentiallyAsync("sk-test-new-key");
            await keyInput.PressAsync("Tab"); // Blur the input to trigger @bind change event

            // Click Save button (should be enabled now)
            var saveButton = page.Locator("button:has-text('Save')").First;
            await saveButton.ClickAsync(new() { Timeout = 200 });

            // Wait for success message
            var successMessage = page.Locator("text=successfully");
            await successMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

            // Verify connection appears in list
            var connectionItem = page.Locator(".bg-gray-700.p-3.rounded").First;
            await connectionItem.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
