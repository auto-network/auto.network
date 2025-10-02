using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Xunit;
using AutoWeb.Client;
using AutoWeb.Components;

namespace AutoWeb.Tests.Components.ConnectionHub;

public class ConnectionHubRenderTests : TestContext
{
    private readonly Mock<IAutoHostClient> _mockAutoHostClient;
    private readonly Mock<IJSRuntime> _mockJSRuntime;

    public ConnectionHubRenderTests()
    {
        _mockAutoHostClient = new Mock<IAutoHostClient>();
        _mockJSRuntime = new Mock<IJSRuntime>();

        Services.AddSingleton(_mockAutoHostClient.Object);
        Services.AddSingleton(_mockJSRuntime.Object);
    }

    private void SetupRegistry()
    {
        _mockAutoHostClient.Setup(x => x.ConnectionsGetRegistryAsync())
            .ReturnsAsync(new ServiceRegistryResponse
            {
                Services = new List<ServiceDefinition>
                {
                    new ServiceDefinition
                    {
                        Type = ServiceType.OpenRouter,
                        DisplayName = "OpenRouter",
                        Description = "Multi-model aggregator",
                        SupportedProtocols = new[] { ProtocolType.OpenAICompatible },
                        DefaultProtocol = ProtocolType.OpenAICompatible
                    }
                },
                Protocols = new List<ProtocolDefinition>
                {
                    new ProtocolDefinition
                    {
                        Type = ProtocolType.OpenAICompatible,
                        DisplayName = "OpenAI Compatible",
                        Description = "Standard OpenAI API format"
                    }
                }
            });
    }

    private void SetupEmptyConnections()
    {
        _mockAutoHostClient.Setup(x => x.ConnectionsGetAsync())
            .ReturnsAsync(new ConnectionsListResponse { Connections = new List<ConnectionInfo>() });
    }

    private void SetupConnections(int count)
    {
        var connections = new List<ConnectionInfo>();
        for (int i = 1; i <= count; i++)
        {
            connections.Add(new ConnectionInfo
            {
                Id = i,
                ServiceType = ServiceType.OpenRouter,
                Protocol = ProtocolType.OpenAICompatible,
                Key = $"sk-test-key-{i}",
                Description = $"Connection {i}",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-i),
                LastUsedAt = DateTimeOffset.UtcNow.AddHours(-i)
            });
        }

        _mockAutoHostClient.Setup(x => x.ConnectionsGetAsync())
            .ReturnsAsync(new ConnectionsListResponse { Connections = connections });
    }

    // Test 1: Should render main container with correct structure
    [Fact]
    public async Task Should_Render_Main_Container_Structure()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        // Assert
        var container = component.Find(".bg-gray-800.rounded-lg.p-6");
        Assert.NotNull(container);

        // Should have header with title and button
        var header = container.QuerySelector(".flex.justify-between.items-center.mb-4");
        Assert.NotNull(header);

        var title = header.QuerySelector("h3.text-lg.font-semibold.text-white");
        Assert.NotNull(title);
        Assert.Equal("Connection Hub", title.TextContent);
    }

    // Test 2: Should render Add Connection button with correct styling
    [Fact]
    public async Task Should_Render_Add_Button_With_Correct_Styling()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        // Assert
        var addButton = component.Find("button.bg-green-600.text-white.rounded");
        Assert.NotNull(addButton);
        Assert.Contains("Add Connection", addButton.TextContent);
        Assert.True(addButton.ClassList.Contains("hover:bg-green-500"));
        Assert.True(addButton.ClassList.Contains("transition-colors"));
    }

    // Test 3: Should render empty state with correct message
    [Fact]
    public async Task Should_Render_Empty_State_Message()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        // Assert
        var emptyMessage = component.Find(".text-gray-400");
        Assert.NotNull(emptyMessage);
        Assert.Equal("No connections configured.", emptyMessage.TextContent);
    }

    // Test 4: Should render add form with correct structure when opened
    [Fact]
    public async Task Should_Render_Add_Form_Structure()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var addButton = component.Find("button:contains('Add Connection')");
        addButton.Click();

        // Assert
        var form = component.Find(".bg-gray-700.p-4.rounded-lg.mb-4");
        Assert.NotNull(form);

        // Should have service dropdown
        var serviceSelect = form.QuerySelector("select");
        Assert.NotNull(serviceSelect);
        var labels = form.QuerySelectorAll("label");
        Assert.True(labels.Any(l => l.TextContent.Contains("Service")));

        // Should have protocol dropdown
        var selects = form.QuerySelectorAll("select");
        Assert.Equal(2, selects.Length);

        // Should have description input (may not have explicit type="text" attribute)
        var inputs = form.QuerySelectorAll("input");
        var descInput = inputs.FirstOrDefault(i => i.GetAttribute("placeholder")?.Contains("Production") == true);
        Assert.NotNull(descInput);

        // Should have API key input
        var keyInput = form.QuerySelector("input[type='password']");
        Assert.NotNull(keyInput);
        Assert.Equal("sk-...", keyInput.GetAttribute("placeholder"));

        // Should have Save and Cancel buttons
        var buttons = form.QuerySelectorAll("button");
        Assert.True(buttons.Length >= 2);
        Assert.Contains(buttons, b => b.TextContent.Contains("Save"));
        Assert.Contains(buttons, b => b.TextContent.Contains("Cancel"));
    }

    // Test 5: Should render connection list items with correct structure
    [Fact]
    public async Task Should_Render_Connection_List_Items()
    {
        // Arrange
        SetupRegistry();
        SetupConnections(2);

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        // Assert
        var connectionItems = component.FindAll(".bg-gray-700.p-3.rounded.flex");
        Assert.Equal(2, connectionItems.Count);

        var firstItem = connectionItems[0];

        // Should have service name
        var serviceName = firstItem.QuerySelector(".text-white.font-medium");
        Assert.NotNull(serviceName);

        // Should have protocol info
        var protocolInfo = firstItem.QuerySelector(".text-gray-500.text-xs");
        Assert.NotNull(protocolInfo);
        Assert.Contains("via", protocolInfo.TextContent);

        // Should have metadata (creation date, last used)
        var metadata = firstItem.QuerySelector(".text-sm.text-gray-400");
        Assert.NotNull(metadata);
        // Check for date-related content (format is "Created MMM d, yyyy")
        Assert.True(metadata.TextContent.Length > 0, "Metadata should have content");

        // Should have Show and Delete buttons
        var buttons = firstItem.QuerySelectorAll("button");
        Assert.True(buttons.Length >= 2);
        Assert.Contains(buttons, b => b.TextContent.Contains("Show"));
        Assert.Contains(buttons, b => b.TextContent.Contains("Delete"));
    }

    // Test 6: Should apply disabled attribute correctly to Save button
    [Fact]
    public async Task Should_Apply_Disabled_Attribute_To_Save_Button()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var addButton = component.Find("button:contains('Add Connection')");
        addButton.Click();

        // Assert - Save button should be disabled when API key is empty
        var saveButton = component.Find("button:contains('Save')");
        Assert.True(saveButton.HasAttribute("disabled"));
        Assert.True(saveButton.ClassList.Contains("disabled:bg-gray-500"));
    }

    // Test 7: Should render success message with correct styling
    [Fact]
    public async Task Should_Render_Success_Message_Styling()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        _mockAutoHostClient.Setup(x => x.ConnectionsCreateAsync(It.IsAny<CreateConnectionRequest>()))
            .ReturnsAsync(new CreateConnectionResponse { Success = true, ConnectionId = 1 });

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var addButton = component.Find("button:contains('Add Connection')");
        addButton.Click();

        var keyInput = component.Find("input[type='password']");
        keyInput.Change("sk-test-key");

        var saveButton = component.Find("button:contains('Save')");
        saveButton.Click();
        await Task.Delay(50);

        // Assert
        var successDiv = component.Find(".bg-green-900.bg-opacity-50.border.border-green-500.rounded");
        Assert.NotNull(successDiv);

        var successText = successDiv.QuerySelector(".text-sm.text-green-200");
        Assert.NotNull(successText);
        Assert.Contains("successfully", successText.TextContent);
    }

    // Test 8: Should render error message with correct styling
    [Fact]
    public async Task Should_Render_Error_Message_Styling()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        _mockAutoHostClient.Setup(x => x.ConnectionsCreateAsync(It.IsAny<CreateConnectionRequest>()))
            .ThrowsAsync(new ApiException<ErrorResponse>("Failed", 400, "Failed", null,
                new ErrorResponse { Error = "Invalid API key" }, null));

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var addButton = component.Find("button:contains('Add Connection')");
        addButton.Click();

        var keyInput = component.Find("input[type='password']");
        keyInput.Change("invalid-key");

        var saveButton = component.Find("button:contains('Save')");
        saveButton.Click();
        await Task.Delay(50);

        // Assert
        var errorDiv = component.Find(".bg-red-900.bg-opacity-50.border.border-red-500.rounded");
        Assert.NotNull(errorDiv);

        var errorText = errorDiv.QuerySelector(".text-sm.text-red-200");
        Assert.NotNull(errorText);
        Assert.Contains("Invalid API key", errorText.TextContent);
    }
}
