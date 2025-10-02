using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moq;
using Xunit;
using Xunit.Abstractions;
using AutoWeb.Client;
using AutoWeb.Components;
using AutoWeb.Services;

namespace AutoWeb.Tests.Components;

/// <summary>
/// Layout validation tests for AuthenticationSettings component.
/// These tests render the component in various states and validate the layout/UI structure.
/// </summary>
public class AuthenticationSettingsLayoutTests : TestContext
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IAutoHostClient> _mockAutoHostClient;
    private readonly Mock<IJSRuntime> _mockJSRuntime;
    private readonly PasskeyService _passkeyService;

    public AuthenticationSettingsLayoutTests(ITestOutputHelper output)
    {
        _output = output;
        _mockAutoHostClient = new Mock<IAutoHostClient>();
        _mockJSRuntime = new Mock<IJSRuntime>();

        // Create real PasskeyService with mocked dependencies
        var mockLogger = new Mock<ILogger<PasskeyService>>();
        _passkeyService = new PasskeyService(
            _mockJSRuntime.Object,
            _mockAutoHostClient.Object,
            mockLogger.Object);

        Services.AddSingleton(_mockAutoHostClient.Object);
        Services.AddSingleton(_passkeyService);
        Services.AddSingleton(_mockJSRuntime.Object);

        // Setup sessionStorage mock for userEmail
        _mockJSRuntime.Setup(x => x.InvokeAsync<string?>("sessionStorage.getItem", It.Is<object[]>(args =>
            args.Length == 1 && (string)args[0] == "userEmail")))
            .ReturnsAsync("test@example.com");
    }

    [Fact]
    public async Task Layout_NoAuth_ShowsCreatePasswordOption()
    {
        // Arrange
        SetupNoAuthState();

        // Act
        var component = RenderComponent<AuthenticationSettings>();
        await Task.Delay(50);
        var layout = CaptureLayout(component);

        // Assert
        _output.WriteLine("No Auth Layout:");
        _output.WriteLine(layout);

        Assert.Contains("Create Password", layout);
        Assert.DoesNotContain("Remove Password", layout);
        Assert.Contains("No password set", layout);
        Assert.Contains("No passkeys registered", layout);
    }

    [Fact]
    public async Task Layout_PasswordOnly_ShowsDisabledRemoveButton()
    {
        // Arrange
        SetupPasswordOnlyState();

        // Act
        var component = RenderComponent<AuthenticationSettings>();
        await Task.Delay(50);
        var layout = CaptureLayout(component);

        // Assert
        _output.WriteLine("Password Only Layout:");
        _output.WriteLine(layout);

        Assert.Contains("Password authentication is enabled", layout);
        Assert.Contains("Add passkey first", layout);
        Assert.Contains("disabled", component.Find("button[disabled]").ToMarkup());
    }

    [Fact]
    public async Task Layout_SinglePasskeyOnly_ShowsLastOneProtection()
    {
        // Arrange
        SetupSinglePasskeyOnlyState();

        // Act
        var component = RenderComponent<AuthenticationSettings>();
        await Task.Delay(50);
        var layout = CaptureLayout(component);

        // Assert
        _output.WriteLine("Single Passkey Only Layout:");
        _output.WriteLine(layout);

        Assert.Contains("Create Password", layout);
        Assert.Contains("Last one", layout);
        Assert.Contains("iPhone 15 Pro", layout);
    }

    [Fact]
    public async Task Layout_MultiplePasskeysOnly_AllDeletable()
    {
        // Arrange
        SetupMultiplePasskeysOnlyState();

        // Act
        var component = RenderComponent<AuthenticationSettings>();
        await Task.Delay(50);
        var layout = CaptureLayout(component);

        // Assert
        _output.WriteLine("Multiple Passkeys Only Layout:");
        _output.WriteLine(layout);

        Assert.Contains("Create Password", layout);
        Assert.Contains("Delete", layout);
        Assert.DoesNotContain("Last one", layout);

        var deleteButtons = component.FindAll(".bg-gray-700.p-3.rounded.flex button");
        Assert.All(deleteButtons, btn => Assert.False(btn.HasAttribute("disabled")));
    }

    [Fact]
    public async Task Layout_PasswordAndMultiplePasskeys_FullFeatures()
    {
        // Arrange
        SetupPasswordAndMultiplePasskeysState();

        // Act
        var component = RenderComponent<AuthenticationSettings>();
        await Task.Delay(50);
        var layout = CaptureLayout(component);

        // Assert
        _output.WriteLine("Password + Multiple Passkeys Layout:");
        _output.WriteLine(layout);

        Assert.Contains("Remove Password", layout);
        Assert.DoesNotContain("Add passkey first", layout);
        Assert.Contains("Delete", layout);

        // Remove password button should be enabled
        var removeButton = component.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Remove Password"));
        Assert.NotNull(removeButton);
        Assert.False(removeButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Layout_PasskeysNotSupported_ShowsNotSupportedButton()
    {
        // Arrange
        SetupPasskeysNotSupportedState();

        // Act
        var component = RenderComponent<AuthenticationSettings>();
        await Task.Delay(50);
        var layout = CaptureLayout(component);

        // Assert
        _output.WriteLine("Passkeys Not Supported Layout:");
        _output.WriteLine(layout);

        Assert.Contains("Passkeys Not Supported", layout);

        var addButton = component.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Passkeys Not Supported"));
        Assert.NotNull(addButton);
        Assert.True(addButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Layout_LoadingState_ShowsLoadingIndicator()
    {
        // Arrange
        var tcs = new TaskCompletionSource<PasskeyListResponse>();
        _mockAutoHostClient.Setup(x => x.PasskeyListAsync())
            .Returns(tcs.Task);

        _mockAutoHostClient.Setup(x => x.PasskeyCheckUserAsync(It.IsAny<CheckUserRequest>()))
            .ReturnsAsync(new CheckUserPasskeyResponse
            {
                Exists = true,
                HasPassword = true,
                HasPasskeys = false
            });

        SetupPasskeySupport(true);

        // Act
        var component = RenderComponent<AuthenticationSettings>();
        // Don't wait - check immediately for loading state
        var layout = CaptureLayout(component);

        // Assert
        _output.WriteLine("Loading State Layout:");
        _output.WriteLine(layout);

        Assert.Contains("Loading passkeys...", layout);

        // Complete loading
        tcs.SetResult(new PasskeyListResponse { Passkeys = new List<AutoWeb.Client.PasskeyInfo>() });
    }

    [Theory]
    [InlineData("no-auth", "Create Password", "No password set")]
    [InlineData("password-only", "Add passkey first", "Password authentication is enabled")]
    [InlineData("single-passkey", "Last one", "iPhone 15 Pro")]
    [InlineData("multiple-passkeys", "Delete", "MacBook Pro")]
    public async Task Layout_VariousStates_ContainExpectedElements(string stateName, string expectedElement1, string expectedElement2)
    {
        // Arrange
        switch (stateName)
        {
            case "no-auth":
                SetupNoAuthState();
                break;
            case "password-only":
                SetupPasswordOnlyState();
                break;
            case "single-passkey":
                SetupSinglePasskeyOnlyState();
                break;
            case "multiple-passkeys":
                SetupMultiplePasskeysOnlyState();
                break;
        }

        // Act
        var component = RenderComponent<AuthenticationSettings>();
        await Task.Delay(50);
        var layout = CaptureLayout(component);

        // Assert
        _output.WriteLine($"Layout for {stateName}:");
        _output.WriteLine(layout);
        Assert.Contains(expectedElement1, layout);
        Assert.Contains(expectedElement2, layout);
    }

    // Helper method to capture simplified layout
    private string CaptureLayout(IRenderedComponent<AuthenticationSettings> component)
    {
        var sb = new StringBuilder();

        // Extract text content from the component
        var passwordSection = component.FindAll(".bg-gray-800").FirstOrDefault();
        if (passwordSection != null)
        {
            sb.AppendLine("=== Password Section ===");
            var passwordText = passwordSection.TextContent.Trim();
            sb.AppendLine(passwordText);
        }

        var passkeysSection = component.FindAll(".bg-gray-800").Skip(1).FirstOrDefault();
        if (passkeysSection != null)
        {
            sb.AppendLine("\n=== Passkeys Section ===");
            var passkeysText = passkeysSection.TextContent.Trim();
            sb.AppendLine(passkeysText);
        }

        // Check button states
        sb.AppendLine("\n=== Buttons ===");
        var buttons = component.FindAll("button");
        foreach (var button in buttons)
        {
            var text = button.TextContent.Trim();
            var disabled = button.HasAttribute("disabled") ? " [DISABLED]" : " [ENABLED]";
            sb.AppendLine($"- {text}{disabled}");
        }

        return sb.ToString();
    }

    private string ExtractKeyInfo(string htmlLine)
    {
        if (htmlLine.Contains("<button"))
        {
            var disabled = htmlLine.Contains("disabled") ? " [DISABLED]" : "";
            var content = ExtractTextContent(htmlLine);
            return $"Button: {content}{disabled}";
        }
        if (htmlLine.Contains("bg-gray-800"))
        {
            return "Section: Main Container";
        }
        if (htmlLine.Contains("bg-gray-700"))
        {
            return "Section: Sub Container";
        }
        return "";
    }

    private string ExtractTextContent(string htmlLine)
    {
        var start = htmlLine.IndexOf('>');
        var end = htmlLine.LastIndexOf('<');
        if (start >= 0 && end > start)
        {
            return htmlLine.Substring(start + 1, end - start - 1).Trim();
        }
        return "";
    }

    // Setup methods for different states
    private void SetupNoAuthState()
    {
        _mockAutoHostClient.Setup(x => x.PasskeyCheckUserAsync(It.IsAny<CheckUserRequest>()))
            .ReturnsAsync(new CheckUserPasskeyResponse
            {
                Exists = true,
                HasPassword = false,
                HasPasskeys = false
            });

        _mockAutoHostClient.Setup(x => x.PasskeyListAsync())
            .ReturnsAsync(new PasskeyListResponse { Passkeys = new List<AutoWeb.Client.PasskeyInfo>() });

        SetupPasskeySupport(true);
    }

    private void SetupPasswordOnlyState()
    {
        _mockAutoHostClient.Setup(x => x.PasskeyCheckUserAsync(It.IsAny<CheckUserRequest>()))
            .ReturnsAsync(new CheckUserPasskeyResponse
            {
                Exists = true,
                HasPassword = true,
                HasPasskeys = false
            });

        _mockAutoHostClient.Setup(x => x.PasskeyListAsync())
            .ReturnsAsync(new PasskeyListResponse { Passkeys = new List<AutoWeb.Client.PasskeyInfo>() });

        SetupPasskeySupport(true);
    }

    private void SetupSinglePasskeyOnlyState()
    {
        var passkeys = new List<AutoWeb.Client.PasskeyInfo>
        {
            new AutoWeb.Client.PasskeyInfo
            {
                Id = 1,
                DeviceName = "iPhone 15 Pro",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
                LastUsedAt = DateTimeOffset.UtcNow.AddHours(-2)
            }
        };

        _mockAutoHostClient.Setup(x => x.PasskeyCheckUserAsync(It.IsAny<CheckUserRequest>()))
            .ReturnsAsync(new CheckUserPasskeyResponse
            {
                Exists = true,
                HasPassword = false,
                HasPasskeys = true
            });

        _mockAutoHostClient.Setup(x => x.PasskeyListAsync())
            .ReturnsAsync(new PasskeyListResponse { Passkeys = passkeys });

        SetupPasskeySupport(true);
    }

    private void SetupMultiplePasskeysOnlyState()
    {
        var passkeys = new List<AutoWeb.Client.PasskeyInfo>
        {
            new AutoWeb.Client.PasskeyInfo
            {
                Id = 1,
                DeviceName = "iPhone 15 Pro",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
                LastUsedAt = DateTimeOffset.UtcNow.AddHours(-2)
            },
            new AutoWeb.Client.PasskeyInfo
            {
                Id = 2,
                DeviceName = "MacBook Pro M3",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-20),
                LastUsedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockAutoHostClient.Setup(x => x.PasskeyCheckUserAsync(It.IsAny<CheckUserRequest>()))
            .ReturnsAsync(new CheckUserPasskeyResponse
            {
                Exists = true,
                HasPassword = false,
                HasPasskeys = true
            });

        _mockAutoHostClient.Setup(x => x.PasskeyListAsync())
            .ReturnsAsync(new PasskeyListResponse { Passkeys = passkeys });

        SetupPasskeySupport(true);
    }

    private void SetupPasswordAndMultiplePasskeysState()
    {
        var passkeys = new List<AutoWeb.Client.PasskeyInfo>
        {
            new AutoWeb.Client.PasskeyInfo
            {
                Id = 1,
                DeviceName = "iPhone 15 Pro",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
                LastUsedAt = DateTimeOffset.UtcNow.AddHours(-2)
            },
            new AutoWeb.Client.PasskeyInfo
            {
                Id = 2,
                DeviceName = "MacBook Pro M3",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-20),
                LastUsedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockAutoHostClient.Setup(x => x.PasskeyCheckUserAsync(It.IsAny<CheckUserRequest>()))
            .ReturnsAsync(new CheckUserPasskeyResponse
            {
                Exists = true,
                HasPassword = true,
                HasPasskeys = true
            });

        _mockAutoHostClient.Setup(x => x.PasskeyListAsync())
            .ReturnsAsync(new PasskeyListResponse { Passkeys = passkeys });

        SetupPasskeySupport(true);
    }

    private void SetupPasskeysNotSupportedState()
    {
        _mockAutoHostClient.Setup(x => x.PasskeyCheckUserAsync(It.IsAny<CheckUserRequest>()))
            .ReturnsAsync(new CheckUserPasskeyResponse
            {
                Exists = true,
                HasPassword = true,
                HasPasskeys = false
            });

        _mockAutoHostClient.Setup(x => x.PasskeyListAsync())
            .ReturnsAsync(new PasskeyListResponse { Passkeys = new List<AutoWeb.Client.PasskeyInfo>() });

        SetupPasskeySupport(false); // Not supported
    }

    private void SetupPasskeySupport(bool isSupported)
    {
        _mockJSRuntime.Setup(x => x.InvokeAsync<bool>("PasskeySupport.isSupported", It.IsAny<object[]>()))
            .ReturnsAsync(isSupported);
    }

    // ============================================================
    // BASELINE CAPTURE AND COMPARISON TESTS
    // ============================================================

    private static readonly string BaselineDir = Path.Combine(
        Path.GetDirectoryName(typeof(AuthenticationSettingsLayoutTests).Assembly.Location) ?? "",
        "..", "..", "..", "baseline", "AuthenticationSettings"
    );

    public static IEnumerable<object[]> LayoutStates()
    {
        yield return new object[] { "no-auth", "No password, no passkeys" };
        yield return new object[] { "password-only", "Password but no passkeys" };
        yield return new object[] { "single-passkey", "Single passkey only" };
        yield return new object[] { "multiple-passkeys", "Multiple passkeys" };
        yield return new object[] { "password-and-passkeys", "Both password and passkeys" };
    }

    /// <summary>
    /// Captures baseline HTML for all states.
    /// Run this manually to regenerate baselines after intentional UI changes.
    /// Usage: dotnet test --filter "FullyQualifiedName~CaptureBaseline"
    /// </summary>
    [Fact(Skip = "Manual test - run explicitly to capture baselines")]
    public async Task CaptureBaseline_AllStates()
    {
        _output.WriteLine($"Capturing baselines to: {BaselineDir}");
        Directory.CreateDirectory(BaselineDir);

        foreach (var stateData in LayoutStates())
        {
            var stateName = (string)stateData[0];
            var description = (string)stateData[1];

            _output.WriteLine($"\nCapturing: {stateName} - {description}");

            // Setup mock for this state
            SetupMockForState(stateName);

            // Render component
            var component = RenderComponent<AuthenticationSettings>();
            await Task.Delay(50);

            // Normalize and save HTML
            var html = NormalizeHtml(component.Markup);
            var filepath = Path.Combine(BaselineDir, $"{stateName}.html");
            File.WriteAllText(filepath, html);

            _output.WriteLine($"  Saved: {filepath}");
            _output.WriteLine($"  Length: {html.Length} chars");
        }

        _output.WriteLine($"\n✓ Captured {LayoutStates().Count()} baselines");
    }

    /// <summary>
    /// Compares current component rendering against baseline HTML.
    /// Fails if layout has changed from baseline.
    /// </summary>
    [Theory]
    [MemberData(nameof(LayoutStates))]
    public async Task Layout_MatchesBaseline(string stateName, string description)
    {
        // Arrange
        SetupMockForState(stateName);
        var baselinePath = Path.Combine(BaselineDir, $"{stateName}.html");

        // Skip if baseline doesn't exist
        if (!File.Exists(baselinePath))
        {
            _output.WriteLine($"⚠️  Baseline not found: {baselinePath}");
            _output.WriteLine("   Run CaptureBaseline_AllStates to generate it");
            return; // Skip instead of fail
        }

        // Act
        var component = RenderComponent<AuthenticationSettings>();
        await Task.Delay(50);

        var actualHtml = NormalizeHtml(component.Markup);
        var baselineHtml = File.ReadAllText(baselinePath);

        // Assert
        if (actualHtml != baselineHtml)
        {
            _output.WriteLine($"\n❌ Layout mismatch for state: {stateName}");
            _output.WriteLine($"   Description: {description}");
            _output.WriteLine($"\nExpected (baseline):");
            _output.WriteLine(baselineHtml);
            _output.WriteLine($"\nActual (current):");
            _output.WriteLine(actualHtml);
            _output.WriteLine($"\nDifference:");
            ShowDiff(baselineHtml, actualHtml);
        }

        Assert.Equal(baselineHtml, actualHtml);
    }

    private string NormalizeHtml(string html)
    {
        // Remove Blazor internal attributes (b-xxxxx)
        html = Regex.Replace(html, @"\s*b-[a-z0-9]+=""[^""]*""", "");

        // Remove Blazor diff attributes
        html = Regex.Replace(html, @"\s*_bl_[a-z0-9]+=""[^""]*""", "");

        // Normalize blazor:onclick IDs (these change between renders)
        html = Regex.Replace(html, @"blazor:onclick=""\d+""", @"blazor:onclick=""X""");

        // Normalize whitespace
        html = Regex.Replace(html, @"\s+", " ");
        html = Regex.Replace(html, @">\s+<", "><");
        html = Regex.Replace(html, @"\s+>", ">");
        html = Regex.Replace(html, @"<\s+", "<");

        return html.Trim();
    }

    private void SetupMockForState(string stateName)
    {
        switch (stateName)
        {
            case "no-auth":
                SetupNoAuthState();
                break;
            case "password-only":
                SetupPasswordOnlyState();
                break;
            case "single-passkey":
                SetupSinglePasskeyOnlyState();
                break;
            case "multiple-passkeys":
                SetupMultiplePasskeysOnlyState();
                break;
            case "password-and-passkeys":
                SetupPasswordAndMultiplePasskeysState();
                break;
            default:
                throw new ArgumentException($"Unknown state: {stateName}");
        }
    }

    private void ShowDiff(string expected, string actual)
    {
        var expectedLines = expected.Split(new[] { '<' }, StringSplitOptions.RemoveEmptyEntries);
        var actualLines = actual.Split(new[] { '<' }, StringSplitOptions.RemoveEmptyEntries);

        int maxLines = Math.Max(expectedLines.Length, actualLines.Length);
        for (int i = 0; i < maxLines && i < 20; i++) // Show first 20 differences
        {
            var exp = i < expectedLines.Length ? expectedLines[i].Substring(0, Math.Min(80, expectedLines[i].Length)) : "(missing)";
            var act = i < actualLines.Length ? actualLines[i].Substring(0, Math.Min(80, actualLines[i].Length)) : "(missing)";

            if (exp != act)
            {
                _output.WriteLine($"  Line {i}:");
                _output.WriteLine($"    Expected: <{exp}");
                _output.WriteLine($"    Actual:   <{act}");
            }
        }
    }
}