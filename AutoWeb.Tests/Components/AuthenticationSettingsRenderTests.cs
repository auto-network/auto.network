using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace AutoWeb.Tests.Components;

/// <summary>
/// Browser-based layout rendering tests for AuthenticationSettings component.
/// Uses Playwright to capture actual rendered layout information (LayoutML).
/// </summary>
[Collection("Playwright")]
public class AuthenticationSettingsRenderTests
{
    private readonly ITestOutputHelper _output;
    private readonly PlaywrightFixture _fixture;
    private const int AutoWebPort = 6200; // Dedicated port for render tests
    private static readonly string BaseUrl = $"http://localhost:{AutoWebPort}";

    private static readonly string BaselineDir = Path.Combine(
        Path.GetDirectoryName(typeof(AuthenticationSettingsRenderTests).Assembly.Location) ?? "",
        "..", "..", "..", "baseline", "AuthenticationSettings", "layout"
    );

    public AuthenticationSettingsRenderTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }


    public static IEnumerable<object[]> ComponentStates()
    {
        yield return new object[] { "password-only", "Password only, no passkeys (can't remove password)" };
        yield return new object[] { "single-passkey", "Single passkey only, no password (can't delete last passkey)" };
        yield return new object[] { "multiple-passkeys", "Multiple passkeys, no password (can delete any)" };
        yield return new object[] { "password-and-passkeys", "Both password and passkeys (full flexibility)" };
    }

    [Theory]
    [MemberData(nameof(ComponentStates))]
    public async Task RenderLayout_CaptureLayoutML(string stateName, string description)
    {
        var page = await _fixture.Browser.NewPageAsync();

        // Capture console messages
        page.Console += (_, msg) => _output.WriteLine($"[Browser Console] {msg.Type}: {msg.Text}");

        try
        {
            _output.WriteLine($"\n=== Testing state: {stateName} ===");
            _output.WriteLine($"Description: {description}");

            // Navigate to base URL first to set sessionStorage
            await page.GotoAsync(PlaywrightFixture.BaseUrl);
            await page.EvaluateAsync("sessionStorage.setItem('userEmail', 'test@example.com')");

            // Now navigate to test page with state (automated=true hides test harness UI)
            var url = $"{PlaywrightFixture.BaseUrl}/test?state={stateName}&automated=true";
            _output.WriteLine($"Navigating to: {url}");
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Debug: Check what's on the page
            var pageContent = await page.ContentAsync();
            _output.WriteLine($"Page loaded, length: {pageContent.Length}");

            // Wait for Blazor to initialize
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1000); // Wait for Blazor WASM to boot

            // Wait specifically for component to finish loading (look for non-loading state)
            await page.WaitForSelectorAsync(".bg-gray-800 button:not(:disabled)", new() { Timeout = 5000 });
            await page.WaitForTimeoutAsync(500); // Extra buffer

            // Check if component rendered
            var hasComponent = await page.Locator(".bg-gray-800").CountAsync() > 0;
            _output.WriteLine($"Component rendered: {hasComponent}");

            // DEBUG: Check what actual button text is on the page
            var buttonTexts = await page.Locator(".bg-gray-800 button").AllTextContentsAsync();
            _output.WriteLine($"Actual buttons on page: {string.Join(", ", buttonTexts.Select(t => $"'{t.Trim()}'"))}");

            // Check for error messages
            var pageText = await page.InnerTextAsync("body");
            if (pageText.Contains("Failed") || pageText.Contains("Error"))
            {
                _output.WriteLine($"⚠️  Page contains error text: {pageText.Substring(0, Math.Min(500, pageText.Length))}");
            }

            if (!hasComponent)
            {
                // Take screenshot for debugging
                await page.ScreenshotAsync(new() { Path = $"/tmp/debug-{stateName}.png" });
                _output.WriteLine($"Screenshot saved to /tmp/debug-{stateName}.png");
                var title = await page.TitleAsync();
                _output.WriteLine($"Page title: {title}");
            }

            // Capture LayoutML
            var layoutML = await CaptureLayoutML(page);

            // Output to test results
            _output.WriteLine($"\nCaptured {layoutML.Elements.Count} elements");
            _output.WriteLine($"Viewport: {layoutML.Viewport.Width}x{layoutML.Viewport.Height}");

            // DEBUG: Show ALL elements
            _output.WriteLine("\n=== ALL CAPTURED ELEMENTS ===");
            foreach (var el in layoutML.Elements)
            {
                _output.WriteLine($"  {el.Tag} [{el.Selector}] text='{el.Text.Substring(0, Math.Min(50, el.Text.Length))}'");
            }

            // Validate basic layout expectations
            ValidateLayout(stateName, layoutML);

            // Save baseline if directory exists (manual baseline capture)
            if (Directory.Exists(BaselineDir))
            {
                var baselinePath = Path.Combine(BaselineDir, $"{stateName}.json");
                var json = JsonSerializer.Serialize(layoutML, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(baselinePath, json);
                _output.WriteLine($"Saved baseline: {baselinePath}");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task<LayoutMLData> CaptureLayoutML(IPage page)
    {
        try
        {
            var json = await page.EvaluateAsync<string>(@"() => {
            const viewport = {
                width: window.innerWidth,
                height: window.innerHeight
            };

            function isElementVisible(el) {
                const style = window.getComputedStyle(el);
                if (style.display === 'none' || style.visibility === 'hidden') return false;
                if (parseFloat(style.opacity) === 0) return false;

                const rect = el.getBoundingClientRect();
                return rect.width > 0 && rect.height > 0;
            }

            function captureElement(el, path = '') {
                const rect = el.getBoundingClientRect();
                const style = window.getComputedStyle(el);

                return {
                    tag: el.tagName.toLowerCase(),
                    selector: path,
                    text: el.textContent?.trim().substring(0, 100) || '',
                    bounds: {
                        x: rect.x,
                        y: rect.y,
                        width: rect.width,
                        height: rect.height,
                        top: rect.top,
                        left: rect.left,
                        right: rect.right,
                        bottom: rect.bottom
                    },
                    style: {
                        display: style.display,
                        visibility: style.visibility,
                        opacity: style.opacity,
                        position: style.position,
                        zIndex: style.zIndex
                    },
                    isVisible: isElementVisible(el),
                    classes: Array.from(el.classList)
                };
            }

            // Capture the component container and key elements
            const elements = [];

            // Get the main component container
            const container = document.querySelector('.space-y-4');
            if (container) {
                elements.push(captureElement(container, '.space-y-4'));
            }

            // Capture all sections
            const sections = document.querySelectorAll('.bg-gray-800');
            sections.forEach((el, idx) => {
                elements.push(captureElement(el, `.bg-gray-800:nth-child(${idx + 1})`));

                // Capture all buttons within each section
                el.querySelectorAll('button').forEach((btn, btnIdx) => {
                    const button = captureElement(btn, `.bg-gray-800:nth-child(${idx + 1}) button:nth-child(${btnIdx + 1})`);
                    button.disabled = btn.disabled;
                    button.text = btn.textContent?.trim() || '';
                    elements.push(button);
                });
            });

            // Capture passkey items
            document.querySelectorAll('.bg-gray-700.p-3').forEach((el, idx) => {
                elements.push(captureElement(el, `.bg-gray-700.p-3:nth-child(${idx + 1})`));
            });

            return JSON.stringify({
                viewport: viewport,
                elements: elements,
                timestamp: new Date().toISOString()
            });
        }");

            _output.WriteLine($"Captured JSON length: {json?.Length ?? 0}");
            if (!string.IsNullOrEmpty(json))
            {
                var result = JsonSerializer.Deserialize<LayoutMLData>(json) ?? new LayoutMLData();
                _output.WriteLine($"Deserialized: {result.Elements.Count} elements, viewport {result.Viewport.Width}x{result.Viewport.Height}");
                return result;
            }
            return new LayoutMLData();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error capturing LayoutML: {ex.Message}");
            _output.WriteLine($"Stack: {ex.StackTrace}");
            throw;
        }
    }

    private void ValidateLayout(string stateName, LayoutMLData layout)
    {
        _output.WriteLine("\n=== Layout Validation ===");

        // All states should have visible sections
        var sections = layout.Elements.Where(e => e.Classes.Contains("bg-gray-800")).ToList();
        Assert.True(sections.Count >= 2, $"Expected at least 2 sections, found {sections.Count}");
        _output.WriteLine($"✓ Found {sections.Count} main sections");

        // All sections should be visible and have size
        foreach (var section in sections)
        {
            Assert.True(section.IsVisible, $"Section {section.Selector} should be visible");
            Assert.True(section.Bounds.Width > 0, $"Section {section.Selector} should have width");
            Assert.True(section.Bounds.Height > 0, $"Section {section.Selector} should have height");
            _output.WriteLine($"✓ Section {section.Selector}: {section.Bounds.Width}x{section.Bounds.Height}");
        }

        // Validate buttons based on state
        var buttons = layout.Elements.Where(e => e.Tag == "button").ToList();
        _output.WriteLine($"Found {buttons.Count} buttons");

        // DEBUG: Show ALL button info
        foreach (var btn in buttons)
        {
            _output.WriteLine($"  Button: text='{btn.Text}', disabled={btn.Disabled}, visible={btn.IsVisible}, bounds={btn.Bounds.Width}x{btn.Bounds.Height}");
        }

        switch (stateName)
        {
            case "password-only":
                // Should have disabled Remove Password button (no passkeys as backup)
                // The button text shows "Add passkey first" to indicate why it's disabled
                var passwordOnlyBtn = buttons.FirstOrDefault(b => b.Text.Contains("Add passkey first") || b.Text.Contains("Remove"));
                Assert.NotNull(passwordOnlyBtn);
                Assert.True(passwordOnlyBtn.Disabled, "Remove Password button should be disabled when no passkeys exist");
                _output.WriteLine("✓ Password-only state: Remove button correctly disabled");
                break;

            case "single-passkey":
                // Should have Create Password button (enabled)
                // Delete button should be disabled (can't delete last auth method)
                var createPwdBtn = buttons.FirstOrDefault(b => b.Text.Contains("Create") && b.Text.Contains("Password"));
                Assert.NotNull(createPwdBtn);
                Assert.False(createPwdBtn.Disabled, "Create Password button should be enabled");
                _output.WriteLine("✓ Single-passkey state: Create Password button available");
                break;

            case "multiple-passkeys":
                // Should have Create Password button (enabled)
                // All Delete buttons should be enabled (can delete any passkey when there are multiples)
                var createBtn = buttons.FirstOrDefault(b => b.Text.Contains("Create") && b.Text.Contains("Password"));
                Assert.NotNull(createBtn);
                var deleteButtons = buttons.Where(b => b.Text.Contains("Delete")).ToList();
                Assert.True(deleteButtons.Count > 0, "Should have Delete buttons");
                Assert.All(deleteButtons, btn => Assert.False(btn.Disabled, "Delete buttons should be enabled"));
                _output.WriteLine($"✓ Multiple-passkeys state: {deleteButtons.Count} delete buttons enabled");
                break;

            case "password-and-passkeys":
                // Remove Password should be enabled (has passkeys as backup)
                // Delete buttons should be enabled
                var removePwdBtn = buttons.FirstOrDefault(b => b.Text.Contains("Remove"));
                Assert.NotNull(removePwdBtn);
                Assert.False(removePwdBtn.Disabled, "Remove Password button should be enabled when passkeys exist");
                _output.WriteLine("✓ Password-and-passkeys state: Remove Password enabled");
                break;
        }

        _output.WriteLine("✓ All layout validations passed");
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

public class LayoutMLData
{
    [JsonPropertyName("viewport")]
    public ViewportInfo Viewport { get; set; } = new();
    [JsonPropertyName("elements")]
    public List<ElementInfo> Elements { get; set; } = new();
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = "";
}

public class ViewportInfo
{
    [JsonPropertyName("width")]
    public int Width { get; set; }
    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public class ElementInfo
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "";
    [JsonPropertyName("selector")]
    public string Selector { get; set; } = "";
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
    [JsonPropertyName("bounds")]
    public BoundsInfo Bounds { get; set; } = new();
    [JsonPropertyName("style")]
    public StyleInfo Style { get; set; } = new();
    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; }
    [JsonPropertyName("classes")]
    public List<string> Classes { get; set; } = new();
    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }
}

public class BoundsInfo
{
    [JsonPropertyName("x")]
    public double X { get; set; }
    [JsonPropertyName("y")]
    public double Y { get; set; }
    [JsonPropertyName("width")]
    public double Width { get; set; }
    [JsonPropertyName("height")]
    public double Height { get; set; }
    [JsonPropertyName("top")]
    public double Top { get; set; }
    [JsonPropertyName("left")]
    public double Left { get; set; }
    [JsonPropertyName("right")]
    public double Right { get; set; }
    [JsonPropertyName("bottom")]
    public double Bottom { get; set; }
}

public class StyleInfo
{
    [JsonPropertyName("display")]
    public string Display { get; set; } = "";
    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "";
    [JsonPropertyName("opacity")]
    public string Opacity { get; set; } = "";
    [JsonPropertyName("position")]
    public string Position { get; set; } = "";
    [JsonPropertyName("zIndex")]
    public string ZIndex { get; set; } = "";
}
