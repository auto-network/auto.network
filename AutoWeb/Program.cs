using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AutoWeb;
using AutoWeb.Client;
using AutoWeb.Services;
using AutoWeb.Tests;
using Microsoft.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Check if we're in mock mode (environment variable or development)
var enableMocks = Environment.GetEnvironmentVariable("ENABLE_MOCKS") == "true" ||
                  builder.HostEnvironment.IsDevelopment();

Console.WriteLine($"[Program.cs] EnableMocks: {enableMocks}");

// Register authorization handler
builder.Services.AddScoped<AuthorizationMessageHandler>();

// Register AutoHost client
if (enableMocks)
{
    Console.WriteLine("[Program.cs] Registering MockAutoHostClient");
    builder.Services.AddScoped<IAutoHostClient>(sp =>
    {
        var nav = sp.GetRequiredService<NavigationManager>();
        return new MockAutoHostClient(nav);
    });
}
else
{
    Console.WriteLine("[Program.cs] Registering real AutoHostClient");
    builder.Services.AddScoped<IAutoHostClient>(sp =>
    {
        var currentUrl = builder.HostEnvironment.BaseAddress;
        var autoHostUrl = currentUrl.Contains(":6100")
            ? "http://localhost:6050"  // Test AutoHost
            : "http://localhost:5050"; // Production AutoHost

        var jsRuntime = sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
        var authHandler = new AuthorizationMessageHandler(jsRuntime)
        {
            InnerHandler = new HttpClientHandler()
        };
        var httpClient = new HttpClient(authHandler) { BaseAddress = new Uri(autoHostUrl) };

        return new AutoHostClient(httpClient);
    });
}

// Register PasskeyService
if (enableMocks)
{
    builder.Services.AddScoped<PasskeyService>(sp =>
    {
        var jsRuntime = sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
        var autoHostClient = sp.GetRequiredService<IAutoHostClient>();
        var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PasskeyService>>();
        var nav = sp.GetRequiredService<NavigationManager>();

        // Use MockPasskeyServiceForAuth which reads state from query string
        // NOTE: For Auth tests, MockJSRuntime should be provided by test setup, not registered here
        return new MockPasskeyServiceForAuth(jsRuntime, autoHostClient, logger, nav);
    });
}
else
{
    builder.Services.AddScoped<PasskeyService>();
}

await builder.Build().RunAsync();
