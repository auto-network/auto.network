using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moq;
using Xunit;
using AutoWeb.Client;
using AutoWeb.Components;

namespace AutoWeb.Tests.Components.ConnectionHub;

public class ConnectionHubTests : TestContext
{
    private readonly Mock<IAutoHostClient> _mockAutoHostClient;
    private readonly Mock<IJSRuntime> _mockJSRuntime;

    public ConnectionHubTests()
    {
        _mockAutoHostClient = new Mock<IAutoHostClient>();
        _mockJSRuntime = new Mock<IJSRuntime>();

        Services.AddSingleton(_mockAutoHostClient.Object);
        Services.AddSingleton(_mockJSRuntime.Object);
    }

    private void SetupRegistry()
    {
        var services = new List<ServiceDefinition>
        {
            new ServiceDefinition
            {
                Type = ServiceType.OpenRouter,
                DisplayName = "OpenRouter",
                Description = "Multi-model aggregator",
                SupportedProtocols = new[] { ProtocolType.OpenAICompatible },
                DefaultProtocol = ProtocolType.OpenAICompatible
            },
            new ServiceDefinition
            {
                Type = ServiceType.OpenAI,
                DisplayName = "OpenAI",
                Description = "ChatGPT and GPT-4",
                SupportedProtocols = new[] { ProtocolType.OpenAICompatible },
                DefaultProtocol = ProtocolType.OpenAICompatible
            },
            new ServiceDefinition
            {
                Type = ServiceType.Anthropic,
                DisplayName = "Anthropic",
                Description = "Claude models",
                SupportedProtocols = new[] { ProtocolType.AnthropicAPI },
                DefaultProtocol = ProtocolType.AnthropicAPI
            },
            new ServiceDefinition
            {
                Type = ServiceType.Grok,
                DisplayName = "Grok (xAI)",
                Description = "Grok models",
                SupportedProtocols = new[] { ProtocolType.OpenAICompatible },
                DefaultProtocol = ProtocolType.OpenAICompatible
            }
        };

        var protocols = new List<ProtocolDefinition>
        {
            new ProtocolDefinition
            {
                Type = ProtocolType.OpenAICompatible,
                DisplayName = "OpenAI Compatible",
                Description = "Standard OpenAI API format"
            },
            new ProtocolDefinition
            {
                Type = ProtocolType.AnthropicAPI,
                DisplayName = "Anthropic API",
                Description = "Anthropic's native API"
            }
        };

        _mockAutoHostClient.Setup(x => x.ConnectionsGetRegistryAsync())
            .ReturnsAsync(new ServiceRegistryResponse
            {
                Services = services,
                Protocols = protocols
            });
    }

    private void SetupEmptyConnections()
    {
        _mockAutoHostClient.Setup(x => x.ConnectionsGetAsync())
            .ReturnsAsync(new ConnectionsListResponse
            {
                Connections = new List<ConnectionInfo>()
            });
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
            .ReturnsAsync(new ConnectionsListResponse
            {
                Connections = connections
            });
    }

    // Test 1: Should load registry on initialization
    [Fact]
    public async Task Should_Load_Registry_On_Init()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        // Assert
        _mockAutoHostClient.Verify(x => x.ConnectionsGetRegistryAsync(), Times.Once);
    }

    // Test 2: Should load connections on initialization
    [Fact]
    public async Task Should_Load_Connections_On_Init()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        // Assert
        _mockAutoHostClient.Verify(x => x.ConnectionsGetAsync(), Times.Once);
    }

    // Test 3: Should show empty state when no connections
    [Fact]
    public async Task Should_Show_Empty_State()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        // Assert
        var emptyMessage = component.Find(".text-gray-400");
        Assert.Contains("No connections configured", emptyMessage.TextContent);
    }

    // Test 4: Should show Add Connection button
    [Fact]
    public async Task Should_Show_Add_Connection_Button()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        // Assert
        var addButton = component.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Add Connection"));
        Assert.NotNull(addButton);
    }

    // Test 5: Should open add connection form when button clicked
    [Fact]
    public async Task Should_Open_Add_Form_When_Button_Clicked()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var addButton = component.FindAll("button")
            .First(b => b.TextContent.Contains("Add Connection"));
        await addButton.ClickAsync(new MouseEventArgs());

        // Assert - form should be visible
        var form = component.Find(".bg-gray-700.p-4.rounded-lg.mb-4");
        Assert.NotNull(form);
    }

    // Test 6: Should enable Save button when API key is provided
    [Fact]
    public async Task Should_Enable_Save_When_Key_Provided()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var addButton = component.FindAll("button")
            .First(b => b.TextContent.Contains("Add Connection"));
        await addButton.ClickAsync(new MouseEventArgs());

        // For @bind elements, use .Change() synchronously
        var keyInput = component.Find("input[type='password']");
        keyInput.Change("sk-test-key");

        // Assert
        var saveButton = component.FindAll("button")
            .FirstOrDefault(b => b.TextContent == "Save");
        Assert.NotNull(saveButton);
        Assert.False(saveButton.HasAttribute("disabled"));
    }

    // Test 7: Should call API to create connection when Save clicked
    [Fact]
    public async Task Should_Call_API_To_Create_Connection()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        _mockAutoHostClient.Setup(x => x.ConnectionsCreateAsync(It.IsAny<CreateConnectionRequest>()))
            .ReturnsAsync(new CreateConnectionResponse { Success = true, ConnectionId = 1 });

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var addButton = component.FindAll("button")
            .First(b => b.TextContent.Contains("Add Connection"));
        await addButton.ClickAsync(new MouseEventArgs());

        var keyInput = component.Find("input[type='password']");
        keyInput.Change("sk-test-key");

        var saveButton = component.FindAll("button")
            .First(b => b.TextContent == "Save");
        await saveButton.ClickAsync(new MouseEventArgs());
        await Task.Delay(50);

        // Assert
        _mockAutoHostClient.Verify(x => x.ConnectionsCreateAsync(It.Is<CreateConnectionRequest>(
            req => req.ApiKey == "sk-test-key" &&
                   req.ServiceType == ServiceType.OpenRouter &&
                   req.Protocol == ProtocolType.OpenAICompatible
        )), Times.Once);
    }

    // Test 8: Should show success message after connection created
    [Fact]
    public async Task Should_Show_Success_After_Create()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        _mockAutoHostClient.Setup(x => x.ConnectionsCreateAsync(It.IsAny<CreateConnectionRequest>()))
            .ReturnsAsync(new CreateConnectionResponse { Success = true, ConnectionId = 1 });

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var addButton = component.FindAll("button")
            .First(b => b.TextContent.Contains("Add Connection"));
        await addButton.ClickAsync(new MouseEventArgs());

        var keyInput = component.Find("input[type='password']");
        keyInput.Change("sk-test-key");

        var saveButton = component.FindAll("button")
            .First(b => b.TextContent == "Save");
        await saveButton.ClickAsync(new MouseEventArgs());
        await Task.Delay(50);

        // Assert
        var successDiv = component.Find(".border-green-500");
        Assert.NotNull(successDiv);
        Assert.Contains("successfully", successDiv.TextContent);
    }

    // Test 9: Should show error message if creation fails
    [Fact]
    public async Task Should_Show_Error_If_Create_Fails()
    {
        // Arrange
        SetupRegistry();
        SetupEmptyConnections();

        _mockAutoHostClient.Setup(x => x.ConnectionsCreateAsync(It.IsAny<CreateConnectionRequest>()))
            .ThrowsAsync(new ApiException<ErrorResponse>("Failed", 400, "Failed", null,
                new ErrorResponse { Error = "Invalid API key format" }, null));

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var addButton = component.FindAll("button")
            .First(b => b.TextContent.Contains("Add Connection"));
        await addButton.ClickAsync(new MouseEventArgs());

        var keyInput = component.Find("input[type='password']");
        keyInput.Change("invalid-key");

        var saveButton = component.FindAll("button")
            .First(b => b.TextContent == "Save");
        await saveButton.ClickAsync(new MouseEventArgs());
        await Task.Delay(50);

        // Assert
        var errorDiv = component.Find(".border-red-500");
        Assert.NotNull(errorDiv);
        Assert.Contains("Invalid API key format", errorDiv.TextContent);
    }

    // Test 10: Should display list of connections
    [Fact]
    public async Task Should_Display_Connection_List()
    {
        // Arrange
        SetupRegistry();
        SetupConnections(3);

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        // Assert
        var connectionItems = component.FindAll(".bg-gray-700.p-3.rounded.flex");
        Assert.Equal(3, connectionItems.Count);
    }

    // Test 11: Should toggle API key visibility when Show clicked
    [Fact]
    public async Task Should_Toggle_Key_Visibility()
    {
        // Arrange
        SetupRegistry();
        SetupConnections(1);

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        // Key should not be visible initially
        var keyDisplayBefore = component.FindAll(".font-mono.bg-gray-600")
            .FirstOrDefault(e => e.TextContent.Contains("sk-test-key"));
        Assert.Null(keyDisplayBefore);

        var showButton = component.FindAll("button")
            .First(b => b.TextContent.Contains("Show"));
        await showButton.ClickAsync(new MouseEventArgs());

        // Assert - key should now be visible
        var keyDisplayAfter = component.FindAll(".font-mono.bg-gray-600")
            .FirstOrDefault(e => e.TextContent.Contains("sk-test-key"));
        Assert.NotNull(keyDisplayAfter);
    }

    // Test 12: Should call API to delete connection when Delete clicked
    [Fact]
    public async Task Should_Call_API_To_Delete()
    {
        // Arrange
        SetupRegistry();
        SetupConnections(1);

        _mockAutoHostClient.Setup(x => x.ConnectionsDeleteAsync(1))
            .ReturnsAsync(new DeleteConnectionResponse { Success = true });

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var deleteButton = component.FindAll("button")
            .First(b => b.TextContent.Contains("Delete"));
        await deleteButton.ClickAsync(new MouseEventArgs());
        await Task.Delay(50);

        // Assert
        _mockAutoHostClient.Verify(x => x.ConnectionsDeleteAsync(1), Times.Once);
    }

    // Test 13: Should show success message after deletion
    [Fact]
    public async Task Should_Show_Success_After_Delete()
    {
        // Arrange
        SetupRegistry();
        SetupConnections(1);

        _mockAutoHostClient.Setup(x => x.ConnectionsDeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(new DeleteConnectionResponse { Success = true });

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var deleteButton = component.FindAll("button")
            .First(b => b.TextContent.Contains("Delete"));
        await deleteButton.ClickAsync(new MouseEventArgs());
        await Task.Delay(50);

        // Assert
        var successDiv = component.Find(".border-green-500");
        Assert.NotNull(successDiv);
        Assert.Contains("deleted successfully", successDiv.TextContent);
    }

    // Test 14: Should show error message if deletion fails
    [Fact]
    public async Task Should_Show_Error_If_Delete_Fails()
    {
        // Arrange
        SetupRegistry();
        SetupConnections(1);

        _mockAutoHostClient.Setup(x => x.ConnectionsDeleteAsync(It.IsAny<int>()))
            .ThrowsAsync(new ApiException<ErrorResponse>("Failed", 400, "Failed", null,
                new ErrorResponse { Error = "Cannot delete connection" }, null));

        // Act
        var component = RenderComponent<AutoWeb.Components.ConnectionHub>();
        await Task.Delay(50);

        var deleteButton = component.FindAll("button")
            .First(b => b.TextContent.Contains("Delete"));
        await deleteButton.ClickAsync(new MouseEventArgs());
        await Task.Delay(50);

        // Assert
        var errorDiv = component.Find(".border-red-500");
        Assert.NotNull(errorDiv);
        Assert.Contains("Cannot delete connection", errorDiv.TextContent);
    }
}
