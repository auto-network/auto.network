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

    // Note: API key management tests moved to ConnectionsControllerTests.cs
    // The /api/auth/apikey endpoints have been replaced by /api/connections endpoints
}