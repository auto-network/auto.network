using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoHost.Data;
using AutoHost.Models;
using AutoHost.Services;
using System.Text;
using System.Text.Json;

namespace AutoHost.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasskeyController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasskeyService _passkeyService;

    public PasskeyController(AppDbContext context, PasskeyService passkeyService)
    {
        _context = context;
        _passkeyService = passkeyService;
    }

    private User? GetAuthenticatedUser()
    {
        return HttpContext.Items.TryGetValue("User", out var userObj) && userObj is User user
            ? user
            : null;
    }

    private byte[]? ExtractAndValidateChallenge(byte[] clientDataJson)
    {
        var challenge = _passkeyService.ExtractChallengeFromClientData(clientDataJson);
        if (challenge == null)
            return null;

        // Validate that we generated this challenge
        if (!_passkeyService.ValidateChallenge(challenge))
            return null;

        return challenge;
    }

    [HttpGet("challenge", Name = "PasskeyChallenge")]
    [ProducesResponseType(typeof(ChallengeResponse), 200)]
    public IActionResult GetChallenge()
    {
        var user = GetAuthenticatedUser();

        // Generate challenge for authenticated user (adding passkey) or anonymous (registration)
        var challenge = _passkeyService.GenerateChallenge(user?.Id);
        return Ok(new ChallengeResponse { Challenge = Convert.ToBase64String(challenge) });
    }

    [HttpPost("register", Name = "PasskeyRegister")]
    [ProducesResponseType(typeof(RegisterPasskeyResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> RegisterWithPasskey([FromBody] RegisterNewUserPasskeyRequest request)
    {
        // Check if user already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (existingUser != null)
            return BadRequest(new ErrorResponse
            {
                Error = "Username already exists",
                ErrorCode = AuthErrorCode.UsernameAlreadyExists
            });

        // Convert base64 strings to byte arrays
        var credentialId = Convert.FromBase64String(request.CredentialId);
        var attestationObject = Convert.FromBase64String(request.AttestationObject);
        var clientDataJson = Convert.FromBase64String(request.ClientDataJson);

        // Extract and validate challenge
        var challenge = ExtractAndValidateChallenge(clientDataJson);
        if (challenge == null)
            return BadRequest(new ErrorResponse
            {
                Error = "Invalid or expired challenge",
                ErrorCode = AuthErrorCode.InvalidOrExpiredChallenge
            });

        // Create new user with no password
        var user = new User
        {
            Username = request.Username,
            PasswordHash = "", // Empty password hash for passkey-only account
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Now register the passkey for this user
        var (success, error, passkey) = await _passkeyService.CreatePasskeyAsync(
            user.Id,
            user.Username,
            credentialId,
            attestationObject,
            clientDataJson,
            request.DeviceName,
            challenge); // Pass the already-validated challenge

        if (!success)
        {
            // Rollback user creation if passkey registration fails
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            Console.WriteLine($"[PASSKEY] CreatePasskeyAsync failed: {error}");
            return BadRequest(new ErrorResponse
            {
                Error = error ?? "Passkey registration failed",
                ErrorCode = AuthErrorCode.PasskeyRegistrationFailed
            });
        }

        // Create a session for the new user
        var (sessionToken, tokenHash) = Helpers.TokenHelper.GenerateSessionToken();

        var session = new Session
        {
            UserId = user.Id,
            Token = tokenHash,  // Store the hash
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return Ok(new RegisterPasskeyResponse
        {
            Success = true,
            PasskeyId = passkey!.Id,
            DeviceName = passkey.DeviceName ?? "Unknown Device",
            Token = sessionToken,
            UserId = user.Id,
            Username = user.Username
        });
    }

    [HttpPost("enroll", Name = "PasskeyEnroll")]
    [ProducesResponseType(typeof(RegisterPasskeyResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> EnrollPasskey([FromBody] RegisterPasskeyRequest request)
    {
        var user = GetAuthenticatedUser();
        if (user == null)
            return Unauthorized(new { error = "Authentication required" });

        // Convert base64 strings to byte arrays
        var credentialId = Convert.FromBase64String(request.CredentialId);
        var attestationObject = Convert.FromBase64String(request.AttestationObject);
        var clientDataJson = Convert.FromBase64String(request.ClientDataJson);

        // Extract and validate challenge
        var challenge = ExtractAndValidateChallenge(clientDataJson);
        if (challenge == null)
            return BadRequest(new ErrorResponse
            {
                Error = "Invalid or expired challenge",
                ErrorCode = AuthErrorCode.InvalidOrExpiredChallenge
            });

        // Register the passkey with the validated challenge
        var (success, error, passkey) = await _passkeyService.CreatePasskeyAsync(
            user.Id,
            user.Username,
            credentialId,
            attestationObject,
            clientDataJson,
            request.DeviceName,
            challenge);

        if (!success)
            return BadRequest(new ErrorResponse
            {
                Error = error ?? "Registration failed",
                ErrorCode = AuthErrorCode.PasskeyRegistrationFailed
            });

        return Ok(new RegisterPasskeyResponse
        {
            Success = true,
            PasskeyId = passkey!.Id,
            DeviceName = passkey.DeviceName ?? "Unknown Device"
        });
    }

    [HttpGet("list", Name = "PasskeyList")]
    [ProducesResponseType(typeof(PasskeyListResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> GetUserPasskeys()
    {
        var user = GetAuthenticatedUser();
        if (user == null)
            return Unauthorized(new { error = "Authentication required" });

        var passkeys = await _context.UserPasskeys
            .Where(p => p.UserId == user.Id && p.IsActive)
            .Select(p => new PasskeyInfo
            {
                Id = p.Id,
                DeviceName = p.DeviceName,
                CreatedAt = p.CreatedAt,
                LastUsedAt = p.LastUsedAt,
                IsActive = p.IsActive
            })
            .ToListAsync();

        return Ok(new PasskeyListResponse { Passkeys = passkeys });
    }

    [HttpDelete("{id}", Name = "PasskeyDelete")]
    [ProducesResponseType(typeof(DeletePasskeyResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> DeletePasskey(int id)
    {
        var user = GetAuthenticatedUser();
        if (user == null)
            return Unauthorized(new { error = "Authentication required" });

        var passkey = await _context.UserPasskeys
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

        if (passkey == null)
            return NotFound(new ErrorResponse
            {
                Error = "Passkey not found",
                ErrorCode = AuthErrorCode.PasskeyNotFound
            });

        // Check if user has other auth methods before deleting
        var hasPassword = user.HasPassword;

        var otherPasskeys = await _context.UserPasskeys
            .CountAsync(p => p.UserId == user.Id && p.IsActive && p.Id != id);

        if (!hasPassword && otherPasskeys == 0)
            return BadRequest(new ErrorResponse
            {
                Error = "Cannot remove last authentication method",
                ErrorCode = AuthErrorCode.CannotRemoveLastAuthMethod
            });

        passkey.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new DeletePasskeyResponse { Success = true });
    }

    [HttpPost("check-user", Name = "PasskeyCheckUser")]
    [ProducesResponseType(typeof(CheckUserPasskeyResponse), 200)]
    public async Task<IActionResult> CheckUserExists([FromBody] CheckUserRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Passkeys)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
        {
            return Ok(new CheckUserPasskeyResponse
            {
                Exists = false,
                HasPassword = false,
                HasPasskeys = false
            });
        }

        var hasActivePasskeys = user.Passkeys.Any(p => p.IsActive);

        return Ok(new CheckUserPasskeyResponse
        {
            Exists = true,
            HasPassword = user.HasPassword,
            HasPasskeys = hasActivePasskeys
        });
    }

    [HttpPost("authenticate", Name = "PasskeyAuthenticate")]
    [ProducesResponseType(typeof(AuthenticatePasskeyResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> AuthenticateWithPasskey([FromBody] AuthenticatePasskeyRequest request)
    {
        // Convert base64 strings to byte arrays
        var credentialId = Convert.FromBase64String(request.CredentialId);
        var authenticatorData = Convert.FromBase64String(request.AuthenticatorData);
        var clientDataJson = Convert.FromBase64String(request.ClientDataJson);
        var signature = Convert.FromBase64String(request.Signature);
        byte[]? userHandle = null;

        if (!string.IsNullOrEmpty(request.UserHandle))
        {
            userHandle = Convert.FromBase64String(request.UserHandle);
        }

        // Verify the passkey and create session
        var (success, error, user, token) = await _passkeyService.VerifyPasskeyAsync(
            credentialId,
            authenticatorData,
            clientDataJson,
            signature,
            userHandle);

        if (!success)
            return Unauthorized(new ErrorResponse
            {
                Error = error ?? "Authentication failed",
                ErrorCode = AuthErrorCode.PasskeyAuthenticationFailed
            });

        return Ok(new AuthenticatePasskeyResponse
        {
            Success = true,
            Token = token!,
            UserId = user!.Id,
            Username = user.Username
        });
    }
}

// DTOs for requests
public class RegisterPasskeyRequest
{
    public required string CredentialId { get; set; }
    public required string AttestationObject { get; set; }
    public required string ClientDataJson { get; set; }
    public string? DeviceName { get; set; }
}

public class RegisterNewUserPasskeyRequest
{
    public required string Username { get; set; }
    public required string CredentialId { get; set; }
    public required string AttestationObject { get; set; }
    public required string ClientDataJson { get; set; }
    public string? DeviceName { get; set; }
}

public class AuthenticatePasskeyRequest
{
    public required string CredentialId { get; set; }
    public required string AuthenticatorData { get; set; }
    public required string ClientDataJson { get; set; }
    public required string Signature { get; set; }
    public string? UserHandle { get; set; }
}

// Response DTOs
public class ChallengeResponse
{
    public required string Challenge { get; set; }
}

public class RegisterPasskeyResponse
{
    public bool Success { get; set; }
    public int PasskeyId { get; set; }
    public required string DeviceName { get; set; }
    // Include session info for new user registration
    public string? Token { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
}

public class PasskeyInfo
{
    public int Id { get; set; }
    public string? DeviceName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
}

public class PasskeyListResponse
{
    public required List<PasskeyInfo> Passkeys { get; set; }
}

public class DeletePasskeyResponse
{
    public bool Success { get; set; }
}

public class CheckUserPasskeyResponse
{
    public bool Exists { get; set; }
    public bool HasPassword { get; set; }
    public bool HasPasskeys { get; set; }
}

public class AuthenticatePasskeyResponse
{
    public bool Success { get; set; }
    public required string Token { get; set; }
    public int UserId { get; set; }
    public required string Username { get; set; }
}

