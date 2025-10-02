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

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CheckUser_WithExistingUser_ReturnsExists()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Register a user first
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = "existing@test.com",
            Password = "Password123!"
        });

        // Act - Check if user exists
        var response = await client.PostAsJsonAsync("/api/auth/check", new CheckUserRequest
        {
            Username = "existing@test.com"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<CheckUserResponse>();
        content!.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task CheckUser_WithNonExistentUser_ReturnsNotExists()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Check if non-existent user exists
        var response = await client.PostAsJsonAsync("/api/auth/check", new CheckUserRequest
        {
            Username = "nonexistent@test.com"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<CheckUserResponse>();
        content!.Exists.Should().BeFalse();
    }

    [Fact]
    public async Task CheckUser_WithEmptyUsername_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/check", new CheckUserRequest
        {
            Username = ""
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.Should().Contain("required");
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Password = "TestPassword123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected OK but got {response.StatusCode}: {errorContent}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        content!.Success.Should().BeTrue();
        content.Username.Should().Be(request.Username);
    }

    [Fact]
    public async Task Register_WithExistingUsername_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest
        {
            Username = "duplicate",
            Password = "Password123!"
        };

        // First registration
        await client.PostAsJsonAsync("/api/auth/register", request);

        // Act - Try to register again with same username
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Register_WithMissingData_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest
        {
            Username = "",
            Password = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.Should().Contain("required");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Register first
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = "logintest",
            Password = "Password123!"
        });

        // Act - Login
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "logintest",
            Password = "Password123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content!.Success.Should().BeTrue();
        content.Username.Should().Be("logintest");
        content.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Register first
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = "testuser2",
            Password = "CorrectPassword"
        });

        // Act - Login with wrong password
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "testuser2",
            Password = "WrongPassword"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "nonexistent",
            Password = "SomePassword"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task GetApiKey_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/apikey");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Authentication required");
    }

    [Fact]
    public async Task GetApiKey_WithValidToken_ReturnsApiKey()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Register and login
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = "apikeyuser",
            Password = "Password123!"
        });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "apikeyuser",
            Password = "Password123!"
        });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var token = loginContent!.Token;

        // Save an API key first
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync("/api/auth/apikey", new SaveApiKeyRequest
        {
            ApiKey = "test-api-key-12345",
            Description = "Test Key"
        });

        // Act - Get the API key
        var response = await client.GetAsync("/api/auth/apikey");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiKeyResponse>();
        content!.ApiKey.Should().Be("test-api-key-12345");
    }

    [Fact]
    public async Task SaveApiKey_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/apikey", new SaveApiKeyRequest
        {
            ApiKey = "some-key",
            Description = "Test"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Authentication required");
    }

    [Fact]
    public async Task SaveApiKey_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/apikey", new SaveApiKeyRequest
        {
            ApiKey = "some-key",
            Description = "Test"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Authentication required");
    }

    [Fact]
    public async Task SaveApiKey_WithValidToken_DeactivatesOldKeys()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Register and login
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = "multikey",
            Password = "Password123!"
        });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "multikey",
            Password = "Password123!"
        });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var token = loginContent!.Token;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Save first API key
        await client.PostAsJsonAsync("/api/auth/apikey", new SaveApiKeyRequest
        {
            ApiKey = "first-key",
            Description = "First Key"
        });

        // Act - Save second API key (should deactivate first)
        var response = await client.PostAsJsonAsync("/api/auth/apikey", new SaveApiKeyRequest
        {
            ApiKey = "second-key",
            Description = "Second Key"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify only second key is returned
        var getResponse = await client.GetAsync("/api/auth/apikey");
        var content = await getResponse.Content.ReadFromJsonAsync<ApiKeyResponse>();
        content!.ApiKey.Should().Be("second-key");
    }

    [Fact]
    public async Task MultipleSessions_CanCoexist()
    {
        // Arrange
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        // Register user
        await client1.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = "multisession",
            Password = "Password123!"
        });

        // Login from first client
        var login1Response = await client1.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "multisession",
            Password = "Password123!"
        });
        var loginContent1 = await login1Response.Content.ReadFromJsonAsync<LoginResponse>();
        var token1 = loginContent1!.Token;

        // Login from second client
        var login2Response = await client2.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "multisession",
            Password = "Password123!"
        });
        var loginContent2 = await login2Response.Content.ReadFromJsonAsync<LoginResponse>();
        var token2 = loginContent2!.Token;

        // Set up auth headers
        client1.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);
        client2.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);

        // Act - Both sessions should be able to access API
        var response1 = await client1.GetAsync("/api/auth/apikey");
        var response2 = await client2.GetAsync("/api/auth/apikey");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        token1.Should().NotBe(token2); // Different tokens
    }
}