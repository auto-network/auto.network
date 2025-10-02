using System.Security.Cryptography;

namespace AutoHost.Helpers;

public static class TokenHelper
{
    /// <summary>
    /// Generates a new session token and its hash for storage
    /// </summary>
    /// <returns>Tuple of (token to send to client, hash to store in database)</returns>
    public static (string Token, string TokenHash) GenerateSessionToken()
    {
        // Generate a cryptographically secure 32-byte token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes);

        using var sha256 = SHA256.Create();
        var tokenHash = Convert.ToBase64String(sha256.ComputeHash(tokenBytes));

        return (token, tokenHash);
    }

    /// <summary>
    /// Hashes a token for comparison with stored hash
    /// </summary>
    public static string HashToken(string token)
    {
        var tokenBytes = Convert.FromBase64String(token);
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(tokenBytes));
    }
}