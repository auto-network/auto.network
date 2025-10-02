using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using AutoHost.Controllers;

namespace AutoHost.Tests;

public class PasskeyControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PasskeyControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetChallenge_ReturnsChallenge()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/passkey/challenge");

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("challenge", out var challenge));
        Assert.NotEqual(string.Empty, challenge.GetString());
    }

    [Fact]
    public async Task RegisterPasskey_RequiresAuthentication()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterPasskeyRequest
        {
            CredentialId = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            AttestationObject = Convert.ToBase64String(new byte[] { 4, 5, 6 }),
            ClientDataJson = Convert.ToBase64String(new byte[] { 7, 8, 9 }),
            DeviceName = "Test Device"
        };

        // Act - use enroll endpoint which requires authentication
        var response = await client.PostAsJsonAsync("/api/passkey/enroll", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CheckUser_ReturnsNotExistsForNonexistentUser()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CheckUserRequest
        {
            Username = "nonexistent@example.com"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/passkey/check-user", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CheckUserPasskeyResponse>();
        Assert.NotNull(result);
        Assert.False(result.Exists);
    }

    [Fact]
    public async Task DeletePasskey_RequiresAuthentication()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/passkey/123");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticateWithPasskey_RejectsInvalidCredential()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new AuthenticatePasskeyRequest
        {
            CredentialId = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            AuthenticatorData = Convert.ToBase64String(new byte[] { 4, 5, 6 }),
            ClientDataJson = Convert.ToBase64String(new byte[] { 7, 8, 9 }),
            Signature = Convert.ToBase64String(new byte[] { 10, 11, 12 })
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/passkey/authenticate", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}