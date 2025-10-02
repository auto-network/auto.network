using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace AutoWeb.Tests;

/// <summary>
/// Shared Playwright and AutoWeb server instance for all tests in a class.
/// Starts once per test class, not per test.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    public const int AutoWebPort = 6200;
    public static readonly string BaseUrl = $"http://localhost:{AutoWebPort}";

    private Process? _autoWebProcess;
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Console.WriteLine("Starting AutoWeb server (once per test class)...");

        // Kill any existing process on our port
        KillProcessOnPort(AutoWebPort);

        // Start AutoWeb
        var autoWebDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(PlaywrightFixture).Assembly.Location) ?? "",
            "..", "..", "..", "..", "AutoWeb"
        ));

        _autoWebProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --urls {BaseUrl}",  // Removed --no-build to ensure AutoWeb is built
                WorkingDirectory = autoWebDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _autoWebProcess.Start();

        // Wait for server to start
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(30);
        var ready = false;

        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    ready = true;
                    break;
                }
            }
            catch
            {
                // Still starting
            }

            await Task.Delay(500);
        }

        if (!ready)
        {
            throw new Exception($"AutoWeb failed to start on {BaseUrl} within {timeout.TotalSeconds}s");
        }

        Console.WriteLine($"AutoWeb ready at {BaseUrl}");

        // Initialize Playwright
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });

        Console.WriteLine("Playwright browser initialized");
    }

    public async Task DisposeAsync()
    {
        if (Browser != null)
        {
            await Browser.CloseAsync();
            await Browser.DisposeAsync();
        }

        _playwright?.Dispose();

        if (_autoWebProcess != null && !_autoWebProcess.HasExited)
        {
            _autoWebProcess.Kill(entireProcessTree: true);
            _autoWebProcess.Dispose();
        }

        Console.WriteLine("PlaywrightFixture cleanup complete");
    }

    private void KillProcessOnPort(int port)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "lsof",
                    Arguments = $"-ti:{port}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
            {
                foreach (var pid in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    Process.Start("kill", $"-9 {pid}");
                }
                System.Threading.Thread.Sleep(1000);
            }
        }
        catch
        {
            // Ignore errors
        }
    }
}
