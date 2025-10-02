using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace AutoWeb.Tests.Components.ConnectionHub;

[Collection("Playwright")]
public class ConnectionHubLayoutTests
{
    private readonly PlaywrightFixture _fixture;

    public ConnectionHubLayoutTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Empty_State_Renders_Correctly()
    {
        var page = await _fixture.Browser.NewPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=ConnectionHub&state=empty&automated=true");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify empty state message is visible
            var emptyMessage = page.Locator("text=No connections configured");
            await emptyMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

            // Verify Add Connection button is visible
            var addButton = page.Locator("button:has-text('Add Connection')");
            await addButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Single_Connection_Renders_Correctly()
    {
        var page = await _fixture.Browser.NewPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=ConnectionHub&state=single-connection&automated=true");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify connection item is visible
            var connectionItem = page.Locator(".bg-gray-700.p-3.rounded").First;
            await connectionItem.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

            // Verify service name is visible (use more specific selector to avoid multiple matches)
            var serviceName = page.Locator(".text-white.font-medium:has-text('OpenRouter')");
            await serviceName.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

            // Verify buttons are visible
            var showButton = page.Locator("button:has-text('Show')");
            await showButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });

            var deleteButton = page.Locator("button:has-text('Delete')");
            await deleteButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Multiple_Connections_Render_Correctly()
    {
        var page = await _fixture.Browser.NewPageAsync();
        try
        {
            await page.GotoAsync($"{PlaywrightFixture.BaseUrl}/test?component=ConnectionHub&state=multiple-connections&automated=true");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify all 3 connections are visible
            var connectionItems = page.Locator(".bg-gray-700.p-3.rounded");
            var count = await connectionItems.CountAsync();
            Assert.Equal(3, count);

            // Verify different service types are shown (use specific selectors)
            await page.Locator(".text-white.font-medium:has-text('OpenRouter')").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });
            await page.Locator(".text-white.font-medium:has-text('OpenAI')").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });
            await page.Locator(".text-white.font-medium:has-text('Anthropic')").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 200 });
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
