using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AutoHost.Data;
using AutoHost.Controllers;
using AutoHost.Models;
using ErrorResponse = AutoHost.Models.ErrorResponse;

namespace AutoHost.Tests;

public class ConnectionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConnectionsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, string token, int userId)> CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var username = $"testuser_{Guid.NewGuid():N}";

        // Register
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = username,
            Password = "Password123!"
        });

        var registerContent = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();

        // Login
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = username,
            Password = "Password123!"
        });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var token = loginContent!.Token;

        // Set auth header
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return (client, token, registerContent!.UserId);
    }

    #region List Endpoint Tests

    [Fact]
    public async Task GetConnections_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/connections");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.Should().Contain("Authentication required");
        content.ErrorCode.Should().Be(AuthErrorCode.AuthenticationRequired);
    }

    [Fact]
    public async Task GetConnections_WithNoConnections_ReturnsEmptyList()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/connections");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ConnectionsListResponse>();
        content!.Connections.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConnections_WithSingleConnection_ReturnsSingleConnection()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Create a connection
        await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "test-key-123",
            Description = "Test Connection",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        // Act
        var response = await client.GetAsync("/api/connections");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ConnectionsListResponse>();
        content!.Connections.Should().HaveCount(1);
        content.Connections[0].Key.Should().Be("test-key-123");
        content.Connections[0].Description.Should().Be("Test Connection");
        content.Connections[0].ServiceType.Should().Be(ServiceType.OpenRouter);
        content.Connections[0].Protocol.Should().Be(ProtocolType.OpenAICompatible);
    }

    [Fact]
    public async Task GetConnections_WithMultipleConnections_ReturnsAllActive()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Create multiple connections
        await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "openrouter-key",
            Description = "OpenRouter",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "openai-key",
            Description = "OpenAI",
            ServiceType = ServiceType.OpenAI,
            Protocol = ProtocolType.OpenAICompatible
        });

        await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "anthropic-key",
            Description = "Anthropic",
            ServiceType = ServiceType.Anthropic,
            Protocol = ProtocolType.AnthropicAPI
        });

        // Act
        var response = await client.GetAsync("/api/connections");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ConnectionsListResponse>();
        content!.Connections.Should().HaveCount(3);
        content.Connections.Select(c => c.ServiceType).Should().Contain(new[]
        {
            ServiceType.OpenRouter,
            ServiceType.OpenAI,
            ServiceType.Anthropic
        });
    }

    [Fact]
    public async Task GetConnections_OnlyReturnsActiveConnections()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Create connection
        var createResponse = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "test-key",
            Description = "Test",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        var createContent = await createResponse.Content.ReadFromJsonAsync<CreateConnectionResponse>();
        var connectionId = createContent!.ConnectionId!.Value;

        // Delete connection (soft delete)
        await client.DeleteAsync($"/api/connections/{connectionId}");

        // Act
        var response = await client.GetAsync("/api/connections");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ConnectionsListResponse>();
        content!.Connections.Should().BeEmpty();
    }

    #endregion

    #region Registry Endpoint Tests

    [Fact]
    public async Task GetRegistry_ReturnsAllServices()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/connections/registry");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ServiceRegistryResponse>();
        content!.Services.Should().HaveCount(4);
        content.Services.Select(s => s.Type).Should().Contain(new[]
        {
            ServiceType.OpenRouter,
            ServiceType.OpenAI,
            ServiceType.Anthropic,
            ServiceType.Grok
        });
    }

    [Fact]
    public async Task GetRegistry_ReturnsAllProtocols()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/connections/registry");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ServiceRegistryResponse>();
        content!.Protocols.Should().HaveCount(2);
        content.Protocols.Select(p => p.Type).Should().Contain(new[]
        {
            ProtocolType.OpenAICompatible,
            ProtocolType.AnthropicAPI
        });
    }

    [Fact]
    public async Task GetRegistry_ReturnsCorrectMetadata()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/connections/registry");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ServiceRegistryResponse>();

        var openRouter = content!.Services.First(s => s.Type == ServiceType.OpenRouter);
        openRouter.DisplayName.Should().Be("OpenRouter");
        openRouter.Description.Should().NotBeNullOrEmpty();
        openRouter.SupportedProtocols.Should().Contain(ProtocolType.OpenAICompatible);
        openRouter.DefaultProtocol.Should().Be(ProtocolType.OpenAICompatible);

        var anthropic = content.Services.First(s => s.Type == ServiceType.Anthropic);
        anthropic.DisplayName.Should().Be("Anthropic");
        anthropic.SupportedProtocols.Should().Contain(ProtocolType.AnthropicAPI);
        anthropic.DefaultProtocol.Should().Be(ProtocolType.AnthropicAPI);
    }

    #endregion

    #region Create Endpoint Tests

    [Fact]
    public async Task CreateConnection_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "test-key",
            Description = "Test",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.ErrorCode.Should().Be(AuthErrorCode.AuthenticationRequired);
    }

    [Fact]
    public async Task CreateConnection_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "test-key-123",
            Description = "Test Connection",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<CreateConnectionResponse>();
        content!.Success.Should().BeTrue();
        content.ConnectionId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateConnection_AllowsMultipleActiveConnections()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Act - Create two connections
        var response1 = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "first-key",
            Description = "First",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        var response2 = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "second-key",
            Description = "Second",
            ServiceType = ServiceType.OpenAI,
            Protocol = ProtocolType.OpenAICompatible
        });

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify both exist
        var listResponse = await client.GetAsync("/api/connections");
        var listContent = await listResponse.Content.ReadFromJsonAsync<ConnectionsListResponse>();
        listContent!.Connections.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateConnection_ReturnsConnectionId()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "test-key",
            Description = "Test",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        // Assert
        var content = await response.Content.ReadFromJsonAsync<CreateConnectionResponse>();
        content!.ConnectionId.Should().NotBeNull();
        content.ConnectionId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateConnection_WithEmptyApiKey_ReturnsBadRequest()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "",
            Description = "Test",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.Should().Contain("API key is required");
        content.ErrorCode.Should().Be(AuthErrorCode.ApiKeyRequired);
    }

    [Fact]
    public async Task CreateConnection_WithInvalidServiceProtocolMapping_ReturnsBadRequest()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Act - Try to use OpenAICompatible protocol with Anthropic service (invalid)
        var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "test-key",
            Description = "Test",
            ServiceType = ServiceType.Anthropic,
            Protocol = ProtocolType.OpenAICompatible
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.Should().Contain("Invalid protocol");
        content.Error.Should().Contain("Anthropic");
        content.Error.Should().Contain("OpenAICompatible");
        content.ErrorCode.Should().Be(AuthErrorCode.InvalidServiceProtocol);
    }

    [Fact]
    public async Task CreateConnection_WithValidServiceProtocolMappings_ReturnsSuccess()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Act - Test all valid mappings
        var validMappings = new[]
        {
            (ServiceType.OpenRouter, ProtocolType.OpenAICompatible),
            (ServiceType.OpenAI, ProtocolType.OpenAICompatible),
            (ServiceType.Anthropic, ProtocolType.AnthropicAPI),
            (ServiceType.Grok, ProtocolType.OpenAICompatible)
        };

        foreach (var (service, protocol) in validMappings)
        {
            var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
            {
                ApiKey = $"test-key-{service}",
                Description = $"Test {service}",
                ServiceType = service,
                Protocol = protocol
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"{service} + {protocol} should be valid");
        }
    }

    #endregion

    #region Delete Endpoint Tests

    [Fact]
    public async Task DeleteConnection_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/connections/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.ErrorCode.Should().Be(AuthErrorCode.AuthenticationRequired);
    }

    [Fact]
    public async Task DeleteConnection_SoftDeletesConnection()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Create connection
        var createResponse = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "test-key",
            Description = "Test",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        var createContent = await createResponse.Content.ReadFromJsonAsync<CreateConnectionResponse>();
        var connectionId = createContent!.ConnectionId!.Value;

        // Act - Delete connection
        var deleteResponse = await client.DeleteAsync($"/api/connections/{connectionId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteContent = await deleteResponse.Content.ReadFromJsonAsync<DeleteConnectionResponse>();
        deleteContent!.Success.Should().BeTrue();

        // Verify connection no longer appears in list
        var listResponse = await client.GetAsync("/api/connections");
        var listContent = await listResponse.Content.ReadFromJsonAsync<ConnectionsListResponse>();
        listContent!.Connections.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteConnection_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Act
        var response = await client.DeleteAsync("/api/connections/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.Should().Contain("not found");
        content.ErrorCode.Should().Be(AuthErrorCode.ConnectionNotFound);
    }

    [Fact]
    public async Task DeleteConnection_OfAnotherUser_ReturnsForbidden()
    {
        // Arrange
        var (client1, _, _) = await CreateAuthenticatedClient();
        var (client2, _, _) = await CreateAuthenticatedClient();

        // User 1 creates connection
        var createResponse = await client1.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "test-key",
            Description = "Test",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        var createContent = await createResponse.Content.ReadFromJsonAsync<CreateConnectionResponse>();
        var connectionId = createContent!.ConnectionId!.Value;

        // Act - User 2 tries to delete User 1's connection
        var response = await client2.DeleteAsync($"/api/connections/{connectionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.Should().Contain("permission");
        content.ErrorCode.Should().Be(AuthErrorCode.Forbidden);
    }

    [Fact]
    public async Task DeleteConnection_AlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Create connection
        var createResponse = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "test-key",
            Description = "Test",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        var createContent = await createResponse.Content.ReadFromJsonAsync<CreateConnectionResponse>();
        var connectionId = createContent!.ConnectionId!.Value;

        // Delete once
        await client.DeleteAsync($"/api/connections/{connectionId}");

        // Act - Try to delete again
        var response = await client.DeleteAsync($"/api/connections/{connectionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.ErrorCode.Should().Be(AuthErrorCode.ConnectionNotFound);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Integration_CreateAndList_ReturnsCreatedConnection()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Act - Create connection
        var createResponse = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "integration-key",
            Description = "Integration Test",
            ServiceType = ServiceType.OpenAI,
            Protocol = ProtocolType.OpenAICompatible
        });

        var createContent = await createResponse.Content.ReadFromJsonAsync<CreateConnectionResponse>();

        // List connections
        var listResponse = await client.GetAsync("/api/connections");
        var listContent = await listResponse.Content.ReadFromJsonAsync<ConnectionsListResponse>();

        // Assert
        listContent!.Connections.Should().HaveCount(1);
        listContent.Connections[0].Id.Should().Be(createContent!.ConnectionId!.Value);
        listContent.Connections[0].Key.Should().Be("integration-key");
        listContent.Connections[0].Description.Should().Be("Integration Test");
    }

    [Fact]
    public async Task Integration_CreateAndDelete_RemovesFromList()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Create connection
        var createResponse = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "test-key",
            Description = "Test",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        var createContent = await createResponse.Content.ReadFromJsonAsync<CreateConnectionResponse>();
        var connectionId = createContent!.ConnectionId!.Value;

        // Act - Delete connection
        await client.DeleteAsync($"/api/connections/{connectionId}");

        // List connections
        var listResponse = await client.GetAsync("/api/connections");
        var listContent = await listResponse.Content.ReadFromJsonAsync<ConnectionsListResponse>();

        // Assert
        listContent!.Connections.Should().BeEmpty();
    }

    [Fact]
    public async Task Integration_MultipleServices_AllProtocolsWork()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Act - Create connections for all services
        await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "openrouter-key",
            Description = "OpenRouter",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "openai-key",
            Description = "OpenAI",
            ServiceType = ServiceType.OpenAI,
            Protocol = ProtocolType.OpenAICompatible
        });

        await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "anthropic-key",
            Description = "Anthropic",
            ServiceType = ServiceType.Anthropic,
            Protocol = ProtocolType.AnthropicAPI
        });

        await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "grok-key",
            Description = "Grok",
            ServiceType = ServiceType.Grok,
            Protocol = ProtocolType.OpenAICompatible
        });

        // List connections
        var listResponse = await client.GetAsync("/api/connections");
        var listContent = await listResponse.Content.ReadFromJsonAsync<ConnectionsListResponse>();

        // Assert
        listContent!.Connections.Should().HaveCount(4);
        listContent.Connections.Should().Contain(c => c.ServiceType == ServiceType.OpenRouter && c.Protocol == ProtocolType.OpenAICompatible);
        listContent.Connections.Should().Contain(c => c.ServiceType == ServiceType.OpenAI && c.Protocol == ProtocolType.OpenAICompatible);
        listContent.Connections.Should().Contain(c => c.ServiceType == ServiceType.Anthropic && c.Protocol == ProtocolType.AnthropicAPI);
        listContent.Connections.Should().Contain(c => c.ServiceType == ServiceType.Grok && c.Protocol == ProtocolType.OpenAICompatible);
    }

    [Fact]
    public async Task Integration_CreateListDeleteList_Roundtrip()
    {
        // Arrange
        var (client, _, _) = await CreateAuthenticatedClient();

        // Create 3 connections
        var createResponse1 = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "key1",
            Description = "Connection 1",
            ServiceType = ServiceType.OpenRouter,
            Protocol = ProtocolType.OpenAICompatible
        });

        var createResponse2 = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "key2",
            Description = "Connection 2",
            ServiceType = ServiceType.OpenAI,
            Protocol = ProtocolType.OpenAICompatible
        });

        var createResponse3 = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest
        {
            ApiKey = "key3",
            Description = "Connection 3",
            ServiceType = ServiceType.Anthropic,
            Protocol = ProtocolType.AnthropicAPI
        });

        // List - should have 3
        var listResponse1 = await client.GetAsync("/api/connections");
        var listContent1 = await listResponse1.Content.ReadFromJsonAsync<ConnectionsListResponse>();
        listContent1!.Connections.Should().HaveCount(3);

        // Delete middle one
        var createContent2 = await createResponse2.Content.ReadFromJsonAsync<CreateConnectionResponse>();
        await client.DeleteAsync($"/api/connections/{createContent2!.ConnectionId!.Value}");

        // List - should have 2
        var listResponse2 = await client.GetAsync("/api/connections");
        var listContent2 = await listResponse2.Content.ReadFromJsonAsync<ConnectionsListResponse>();
        listContent2!.Connections.Should().HaveCount(2);
        listContent2.Connections.Should().Contain(c => c.Key == "key1");
        listContent2.Connections.Should().Contain(c => c.Key == "key3");
        listContent2.Connections.Should().NotContain(c => c.Key == "key2");
    }

    #endregion
}
