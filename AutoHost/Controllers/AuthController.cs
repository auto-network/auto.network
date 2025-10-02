using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoHost.Data;
using AutoHost.Models;
using AutoHost.Services;
using System.Security.Cryptography;

namespace AutoHost.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;

    public AuthController(AppDbContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    [HttpPost("register", Name = "AuthRegister")]
    [ProducesResponseType(typeof(RegisterResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Username is required",
                ErrorCode = AuthErrorCode.UsernameRequired
            });
        }

        // Allow empty password for passkey-only accounts
        if (request.Password == null)
        {
            request.Password = "";
        }

        // Check if user already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (existingUser != null)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Username already exists",
                ErrorCode = AuthErrorCode.UsernameAlreadyExists
            });
        }

        // Create new user
        var user = new User
        {
            Username = request.Username,
            PasswordHash = _passwordService.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new RegisterResponse { Success = true, UserId = user.Id, Username = user.Username });
    }

    [HttpPost("check", Name = "AuthCheckUser")]
    [ProducesResponseType(typeof(CheckUserResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<CheckUserResponse>> CheckUser([FromBody] CheckUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Username is required",
                ErrorCode = AuthErrorCode.UsernameRequired
            });
        }

        var userExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
        return Ok(new CheckUserResponse { Exists = userExists });
    }

    [HttpPost("login", Name = "AuthLogin")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Username is required",
                ErrorCode = AuthErrorCode.UsernameRequired
            });
        }

        // Allow empty password for passkey-only accounts
        if (request.Password == null)
        {
            request.Password = "";
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Invalid username or password",
                ErrorCode = AuthErrorCode.InvalidCredentials
            });
        }

        // Check password
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            // User has no password (passkey-only account) - cannot login with password endpoint
            return Unauthorized(new ErrorResponse
            {
                Error = "This account requires passkey authentication",
                ErrorCode = AuthErrorCode.PasskeyRequired
            });
        }

        if (!_passwordService.VerifyPassword(user.PasswordHash, request.Password))
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Invalid username or password",
                ErrorCode = AuthErrorCode.InvalidCredentials
            });
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;

        // Generate token and hash
        var (token, tokenHash) = Helpers.TokenHelper.GenerateSessionToken();

        var session = new Session
        {
            UserId = user.Id,
            Token = tokenHash // Store the hash, not the actual token
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            Success = true,
            UserId = user.Id,
            Username = user.Username,
            Token = token // Return the actual token to the client
        });
    }

    [HttpGet("apikey", Name = "AuthGetApiKey")]
    [ProducesResponseType(typeof(ApiKeyResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<ActionResult<ApiKeyResponse>> GetApiKey()
    {
        // Get authenticated user ID from context
        var userId = HttpContext.Items["UserId"] as int?;
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Authentication required",
                ErrorCode = AuthErrorCode.AuthenticationRequired
            });
        }

        var apiKey = await _context.ApiKeys
            .Where(k => k.UserId == userId.Value && k.IsActive)
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync();

        if (apiKey == null)
        {
            return Ok(new ApiKeyResponse { ApiKey = "" });
        }

        // Update last used
        apiKey.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new ApiKeyResponse { ApiKey = apiKey.Key, Description = apiKey.Description });
    }

    [HttpPost("apikey", Name = "AuthSaveApiKey")]
    [ProducesResponseType(typeof(SaveApiKeyResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<ActionResult<SaveApiKeyResponse>> SaveApiKey([FromBody] SaveApiKeyRequest request)
    {
        // Get authenticated user ID from context
        var userId = HttpContext.Items["UserId"] as int?;
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Authentication required",
                ErrorCode = AuthErrorCode.AuthenticationRequired
            });
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "API key is required",
                ErrorCode = AuthErrorCode.ApiKeyRequired
            });
        }

        // Deactivate old keys for this user
        var oldKeys = await _context.ApiKeys
            .Where(k => k.UserId == userId.Value && k.IsActive)
            .ToListAsync();

        foreach (var oldKey in oldKeys)
        {
            oldKey.IsActive = false;
        }

        // Create new API key
        var apiKey = new ApiKey
        {
            UserId = userId.Value,
            Key = request.ApiKey,
            Description = request.Description ?? "OpenRouter API Key"
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        return Ok(new SaveApiKeyResponse { Success = true });
    }

    [HttpPost("password/create", Name = "AuthCreatePassword")]
    [ProducesResponseType(typeof(PasswordOperationResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<ActionResult<PasswordOperationResponse>> CreatePassword([FromBody] CreatePasswordRequest request)
    {
        // Get authenticated user ID from context
        var userId = HttpContext.Items["UserId"] as int?;
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Authentication required",
                ErrorCode = AuthErrorCode.AuthenticationRequired
            });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Password is required",
                ErrorCode = AuthErrorCode.PasswordRequired
            });
        }

        // Get the user
        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "User not found",
                ErrorCode = AuthErrorCode.UserNotFound
            });
        }

        // Check if user already has a password
        if (user.HasPassword)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Password already exists. Use change password instead.",
                ErrorCode = AuthErrorCode.PasswordAlreadyExists
            });
        }

        // Set the password
        user.PasswordHash = _passwordService.HashPassword(request.Password);
        await _context.SaveChangesAsync();

        return Ok(new PasswordOperationResponse { Success = true, Message = "Password created successfully" });
    }

    [HttpDelete("password", Name = "AuthRemovePassword")]
    [ProducesResponseType(typeof(PasswordOperationResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<ActionResult<PasswordOperationResponse>> RemovePassword()
    {
        // Get authenticated user ID from context
        var userId = HttpContext.Items["UserId"] as int?;
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Authentication required",
                ErrorCode = AuthErrorCode.AuthenticationRequired
            });
        }

        var user = await _context.Users
            .Include(u => u.Passkeys)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (user == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "User not found",
                ErrorCode = AuthErrorCode.UserNotFound
            });
        }

        // Check if user has at least one active passkey
        var hasActivePasskeys = user.Passkeys.Any(p => p.IsActive);
        if (!hasActivePasskeys)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Cannot remove password without at least one active passkey",
                ErrorCode = AuthErrorCode.CannotRemovePasswordWithoutPasskey
            });
        }

        // Remove the password by setting empty hash
        user.PasswordHash = "";
        await _context.SaveChangesAsync();

        return Ok(new PasswordOperationResponse { Success = true, Message = "Password removed successfully" });
    }
}

public class RegisterRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class CheckUserRequest
{
    public string Username { get; set; } = "";
}

public class SaveApiKeyRequest
{
    public string ApiKey { get; set; } = "";
    public string? Description { get; set; }
}

public class CreatePasswordRequest
{
    public string Password { get; set; } = "";
}

public class PasswordOperationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}