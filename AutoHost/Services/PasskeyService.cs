using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoHost.Data;
using AutoHost.Models;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AutoHost.Services;

public class PasskeyService
{
    private readonly AppDbContext _context;
    private readonly IFido2 _fido2;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;

    public PasskeyService(AppDbContext context, IFido2 fido2, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
    {
        _context = context;
        _fido2 = fido2;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }

    public byte[] GenerateChallenge(int? userId = null)
    {
        var challenge = RandomNumberGenerator.GetBytes(32);

        // ALWAYS store challenge keyed by itself for stateless validation
        // This works for all scenarios: registration, authentication, and adding passkeys
        var challengeKey = Convert.ToBase64String(challenge);
        var cacheKey = $"challenge_{challengeKey}";
        _cache.Set(cacheKey, challenge, TimeSpan.FromMinutes(5));
        Console.WriteLine($"[CHALLENGE] Generated and stored with key: {cacheKey}");

        return challenge;
    }

    // DEPRECATED: No longer using user-specific challenge storage
    // Keeping for backward compatibility but should not be used
    public byte[]? RecallChallenge(int userId)
    {
        return null; // Always return null since we don't store by userId anymore
    }

    public bool ValidateChallenge(byte[] challenge)
    {
        var challengeKey = Convert.ToBase64String(challenge);
        var cacheKey = $"challenge_{challengeKey}";
        Console.WriteLine($"[CHALLENGE] Looking for key: {cacheKey}");
        var cached = _cache.Get<byte[]>(cacheKey);
        if (cached != null)
        {
            Console.WriteLine($"[CHALLENGE] Found and removing key: {cacheKey}");
            _cache.Remove(cacheKey); // Use once
            return true;
        }
        Console.WriteLine($"[CHALLENGE] NOT FOUND: {cacheKey}");
        return false;
    }

    public byte[]? ExtractChallengeFromClientData(byte[] clientDataJson)
    {
        try
        {
            var clientDataText = Encoding.UTF8.GetString(clientDataJson);
            var clientData = JsonSerializer.Deserialize<JsonElement>(clientDataText);
            var challengeBase64 = clientData.GetProperty("challenge").GetString();

            // Convert from base64url to bytes
            return Convert.FromBase64String(
                challengeBase64!.Replace('-', '+').Replace('_', '/').PadRight((challengeBase64.Length + 3) & ~3, '='));
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Error, UserPasskey? Passkey)> CreatePasskeyAsync(
        int userId,
        string userName,
        byte[] credentialId,
        byte[] attestationObject,
        byte[] clientDataJson,
        string? deviceName = null,
        byte[]? expectedChallenge = null)
    {
        try
        {
            // Check if credential already exists
            var exists = await _context.UserPasskeys
                .AnyAsync(p => p.CredentialId == credentialId);

            if (exists)
                return (false, "Credential already registered", null);

            // Create the attestation response for validation
            var response = new AuthenticatorAttestationRawResponse
            {
                Id = credentialId,
                RawId = credentialId,
                Type = PublicKeyCredentialType.PublicKey,
                Response = new AuthenticatorAttestationRawResponse.ResponseData
                {
                    AttestationObject = attestationObject,
                    ClientDataJson = clientDataJson
                }
            };

            // Challenge should always be passed in after validation
            // We no longer store/recall by userId
            if (expectedChallenge == null)
                return (false, "Challenge must be provided", null);
            var challenge = expectedChallenge;

            // Create options that were used on client
            // IMPORTANT: User ID must match what JavaScript uses (username, not numeric ID)
            var user = new Fido2User
            {
                Id = Encoding.UTF8.GetBytes(userName),  // Use username to match client
                Name = userName,
                DisplayName = userName
            };

            var credentialCreateOptions = new CredentialCreateOptions
            {
                Challenge = challenge,
                User = user,
                Rp = new PublicKeyCredentialRpEntity("localhost", "Auto", null),
                PubKeyCredParams = new List<PubKeyCredParam>
                {
                    new PubKeyCredParam(COSE.Algorithm.ES256)
                },
                Attestation = AttestationConveyancePreference.None,
                AuthenticatorSelection = new AuthenticatorSelection
                {
                    AuthenticatorAttachment = AuthenticatorAttachment.Platform,
                    UserVerification = UserVerificationRequirement.Preferred
                }
            };

            // Validate the credential
            var credentialResult = await _fido2.MakeNewCredentialAsync(
                response,
                credentialCreateOptions,
                async (args, cancellationToken) =>
                {
                    // Verify credential ID is unique
                    var credExists = await _context.UserPasskeys
                        .AnyAsync(p => p.CredentialId == args.CredentialId, cancellationToken);
                    return !credExists;
                });

            if (credentialResult.Status != "ok" || credentialResult.Result == null)
                return (false, credentialResult.ErrorMessage ?? "Registration failed", null);

            // Get user agent for device info
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

            // Save the credential
            var passkey = new UserPasskey
            {
                UserId = userId,
                CredentialId = credentialResult.Result.CredentialId,
                PublicKey = credentialResult.Result.PublicKey,
                SignCount = credentialResult.Result.Counter,
                DeviceName = deviceName ?? GetDeviceNameFromUserAgent(userAgent),
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.UserPasskeys.Add(passkey);
            await _context.SaveChangesAsync();

            return (true, null, passkey);
        }
        catch (Exception ex)
        {
            return (false, $"Registration error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string? Error, User? User, string? Token)> VerifyPasskeyAsync(
        byte[] credentialId,
        byte[] authenticatorData,
        byte[] clientDataJson,
        byte[] signature,
        byte[]? userHandle = null)
    {
        try
        {
            // Find the passkey
            var passkey = await _context.UserPasskeys
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.CredentialId == credentialId && p.IsActive);

            if (passkey == null)
                return (false, "Credential not found", null, null);

            // Extract and validate challenge
            var challenge = ExtractChallengeFromClientData(clientDataJson);
            if (challenge == null || !ValidateChallenge(challenge))
                return (false, "Invalid or expired challenge", null, null);

            // Create the assertion response
            var response = new AuthenticatorAssertionRawResponse
            {
                Id = credentialId,
                RawId = credentialId,
                Type = PublicKeyCredentialType.PublicKey,
                Response = new AuthenticatorAssertionRawResponse.AssertionResponse
                {
                    AuthenticatorData = authenticatorData,
                    ClientDataJson = clientDataJson,
                    Signature = signature,
                    UserHandle = userHandle
                }
            };

            var assertionOptions = new AssertionOptions
            {
                Challenge = challenge,
                RpId = "localhost"
            };

            // Verify the assertion
            var result = await _fido2.MakeAssertionAsync(
                response,
                assertionOptions,
                passkey.PublicKey,
                passkey.SignCount,
                (args, cancellationToken) =>
                {
                    // Verify the user handle matches if provided
                    // User handle is the username (email), not the numeric ID
                    if (userHandle != null)
                    {
                        var expectedUserHandle = Encoding.UTF8.GetBytes(passkey.User.Username);
                        return Task.FromResult(args.UserHandle.SequenceEqual(expectedUserHandle));
                    }
                    return Task.FromResult(true);
                });

            if (result.Status != "ok")
                return (false, result.ErrorMessage ?? "Authentication failed", null, null);

            // Update passkey metadata
            passkey.SignCount = result.Counter;
            passkey.LastUsedAt = DateTime.UtcNow;

            // Create session
            var (sessionToken, tokenHash) = Helpers.TokenHelper.GenerateSessionToken();
            var session = new Session
            {
                UserId = passkey.UserId,
                Token = tokenHash,  // Store hash, not raw token
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return (true, null, passkey.User, sessionToken);
        }
        catch (Exception ex)
        {
            return (false, $"Authentication error: {ex.Message}", null, null);
        }
    }

    private static string GetDeviceNameFromUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown Device";

        // Simple parsing - in production you'd use a proper UA parser
        if (userAgent.Contains("iPhone"))
            return "iPhone";
        if (userAgent.Contains("Android"))
            return "Android Device";
        if (userAgent.Contains("Windows"))
            return "Windows PC";
        if (userAgent.Contains("Mac"))
            return "Mac";
        if (userAgent.Contains("Linux"))
            return "Linux PC";

        return "Web Browser";
    }
}