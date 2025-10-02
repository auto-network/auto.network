using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoWeb.Client;
using AutoWeb.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using PasskeyInfo = AutoWeb.Client.PasskeyInfo;

namespace AutoWeb.Tests;

/// <summary>
/// Central registry of valid mock states for each component under test.
/// </summary>
public static class MockStates
{
    public static readonly Dictionary<string, string[]> ComponentStates = new()
    {
        ["AuthenticationSettings"] = new[]
        {
            "password-only",
            "single-passkey",
            "multiple-passkeys",
            "password-and-passkeys"
        },
        ["Auth"] = new[]
        {
            "new-user-passkey-supported",
            "new-user-passkey-not-supported",
            "existing-password-only",
            "existing-passkey-only",
            "existing-passkey-only-not-supported",
            "existing-password-and-passkey"
        }
    };

    public static string[] GetStatesForComponent(string componentName)
    {
        return ComponentStates.TryGetValue(componentName, out var states)
            ? states
            : Array.Empty<string>();
    }
}

public class MockAutoHostClient : IAutoHostClient
{
    private readonly NavigationManager _nav;
    private bool _hasPassword;
    private List<PasskeyInfo> _passkeys;
    private Dictionary<string, RegisteredUser> _registeredUsers = new();

    public MockAutoHostClient(NavigationManager nav)
    {
        _nav = nav;
        InitializeFromState();
    }

    private class RegisteredUser
    {
        public string Username { get; set; } = "";
        public bool HasPassword { get; set; }
        public bool HasPasskeys { get; set; }
        public int UserId { get; set; }
    }

    private void InitializeFromState()
    {
        var state = GetInitialState();
        Console.WriteLine($"[MockAutoHostClient] InitializeFromState() => {state}");

        switch (state)
        {
            // AuthenticationSettings states
            case "password-only":
                _hasPassword = true;
                _passkeys = new List<PasskeyInfo>();
                break;
            case "single-passkey":
                _hasPassword = false;
                _passkeys = new List<PasskeyInfo> {
                    new() { Id = 1, DeviceName = "iPhone 15 Pro", CreatedAt = DateTimeOffset.UtcNow.AddDays(-30), LastUsedAt = DateTimeOffset.UtcNow.AddHours(-2) }
                };
                break;
            case "multiple-passkeys":
                _hasPassword = false;
                _passkeys = new List<PasskeyInfo> {
                    new() { Id = 1, DeviceName = "iPhone 15 Pro", CreatedAt = DateTimeOffset.UtcNow.AddDays(-30), LastUsedAt = DateTimeOffset.UtcNow.AddHours(-2) },
                    new() { Id = 2, DeviceName = "MacBook Pro M3", CreatedAt = DateTimeOffset.UtcNow.AddDays(-20), LastUsedAt = DateTimeOffset.UtcNow.AddDays(-1) },
                    new() { Id = 3, DeviceName = "Windows Desktop", CreatedAt = DateTimeOffset.UtcNow.AddDays(-10), LastUsedAt = DateTimeOffset.UtcNow.AddHours(-5) }
                };
                break;
            case "password-and-passkeys":
                _hasPassword = true;
                _passkeys = new List<PasskeyInfo> {
                    new() { Id = 1, DeviceName = "iPhone 15 Pro", CreatedAt = DateTimeOffset.UtcNow.AddDays(-30), LastUsedAt = DateTimeOffset.UtcNow.AddHours(-2) },
                    new() { Id = 2, DeviceName = "MacBook Pro M3", CreatedAt = DateTimeOffset.UtcNow.AddDays(-20), LastUsedAt = DateTimeOffset.UtcNow.AddDays(-1) },
                    new() { Id = 3, DeviceName = "Windows Desktop", CreatedAt = DateTimeOffset.UtcNow.AddDays(-10), LastUsedAt = DateTimeOffset.UtcNow.AddHours(-5) }
                };
                break;

            // Auth.razor states - new users
            case "new-user-passkey-supported":
                _hasPassword = false;
                _passkeys = new List<PasskeyInfo>();
                // No registered users
                break;
            case "new-user-passkey-not-supported":
                _hasPassword = false;
                _passkeys = new List<PasskeyInfo>();
                // No registered users
                break;

            // Auth.razor states - existing users with password only
            case "existing-password-only":
                _hasPassword = true;
                _passkeys = new List<PasskeyInfo>();
                _registeredUsers["test@example.com"] = new RegisteredUser
                {
                    Username = "test@example.com",
                    HasPassword = true,
                    HasPasskeys = false,
                    UserId = 1
                };
                break;

            // Auth.razor states - existing users with passkey only
            case "existing-passkey-only":
            case "existing-passkey-only-not-supported":
                _hasPassword = false;
                _passkeys = new List<PasskeyInfo> {
                    new() { Id = 1, DeviceName = "iPhone 15 Pro", CreatedAt = DateTimeOffset.UtcNow.AddDays(-30), LastUsedAt = DateTimeOffset.UtcNow.AddHours(-2) }
                };
                _registeredUsers["test@example.com"] = new RegisteredUser
                {
                    Username = "test@example.com",
                    HasPassword = false,
                    HasPasskeys = true,
                    UserId = 1
                };
                break;

            // Auth.razor states - existing users with both
            case "existing-password-and-passkey":
                _hasPassword = true;
                _passkeys = new List<PasskeyInfo> {
                    new() { Id = 1, DeviceName = "iPhone 15 Pro", CreatedAt = DateTimeOffset.UtcNow.AddDays(-30), LastUsedAt = DateTimeOffset.UtcNow.AddHours(-2) }
                };
                _registeredUsers["test@example.com"] = new RegisteredUser
                {
                    Username = "test@example.com",
                    HasPassword = true,
                    HasPasskeys = true,
                    UserId = 1
                };
                break;

            default:
                _hasPassword = true;
                _passkeys = new List<PasskeyInfo>();
                break;
        }
    }

    private string GetInitialState()
    {
        var uri = new Uri(_nav.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var state = query["state"] ?? "password-only";
        Console.WriteLine($"[MockAutoHostClient] GetInitialState() => {state} (from URI: {uri})");
        return state;
    }

    public Task<CheckUserPasskeyResponse> PasskeyCheckUserAsync(CheckUserRequest request)
    {
        var exists = _registeredUsers.ContainsKey(request.Username);
        Console.WriteLine($"[MockAutoHostClient] PasskeyCheckUserAsync({request.Username}) => exists={exists}, hasPassword={_hasPassword}, hasPasskeys={_passkeys.Count > 0}");

        return Task.FromResult(new CheckUserPasskeyResponse
        {
            Exists = exists,
            HasPassword = _hasPassword,
            HasPasskeys = _passkeys.Count > 0
        });
    }

    public Task<CheckUserPasskeyResponse> PasskeyCheckUserAsync(CheckUserRequest request, CancellationToken cancellationToken)
        => PasskeyCheckUserAsync(request);

    public Task<PasskeyListResponse> PasskeyListAsync()
    {
        return Task.FromResult(new PasskeyListResponse { Passkeys = _passkeys.ToList() });
    }

    public Task<PasskeyListResponse> PasskeyListAsync(CancellationToken cancellationToken) => PasskeyListAsync();

    public Task<DeletePasskeyResponse> PasskeyDeleteAsync(int id)
    {
        var passkey = _passkeys.FirstOrDefault(p => p.Id == id);
        if (passkey != null)
        {
            _passkeys.Remove(passkey);
            Console.WriteLine($"[MockAutoHostClient] Deleted passkey ID={id}, remaining count={_passkeys.Count}");
        }
        return Task.FromResult(new DeletePasskeyResponse { Success = true });
    }

    public Task<DeletePasskeyResponse> PasskeyDeleteAsync(int id, CancellationToken cancellationToken) => PasskeyDeleteAsync(id);

    public Task<PasswordOperationResponse> AuthCreatePasswordAsync(CreatePasswordRequest request)
    {
        _hasPassword = true;
        Console.WriteLine($"[MockAutoHostClient] Created password, hasPassword={_hasPassword}");
        return Task.FromResult(new PasswordOperationResponse { Success = true, Message = "Password created successfully" });
    }

    public Task<PasswordOperationResponse> AuthCreatePasswordAsync(CreatePasswordRequest request, CancellationToken cancellationToken) => AuthCreatePasswordAsync(request);

    public Task<PasswordOperationResponse> AuthRemovePasswordAsync()
    {
        _hasPassword = false;
        Console.WriteLine($"[MockAutoHostClient] Removed password, hasPassword={_hasPassword}");
        return Task.FromResult(new PasswordOperationResponse { Success = true, Message = "Password removed successfully" });
    }

    public Task<PasswordOperationResponse> AuthRemovePasswordAsync(CancellationToken cancellationToken) => AuthRemovePasswordAsync();

    public Task<CheckUserResponse> AuthCheckUserAsync(CheckUserRequest request)
    {
        var exists = _registeredUsers.ContainsKey(request.Username);
        Console.WriteLine($"[MockAutoHostClient] AuthCheckUserAsync({request.Username}) => exists={exists}");

        return Task.FromResult(new CheckUserResponse
        {
            Exists = exists
        });
    }

    public Task<CheckUserResponse> AuthCheckUserAsync(CheckUserRequest request, CancellationToken cancellationToken)
        => AuthCheckUserAsync(request);

    public Task<RegisterResponse> AuthRegisterAsync(RegisterRequest request)
    {
        if (_registeredUsers.ContainsKey(request.Username))
        {
            Console.WriteLine($"[MockAutoHostClient] AuthRegisterAsync({request.Username}) => user already exists");
            return Task.FromResult(new RegisterResponse { Success = false });
        }

        var userId = _registeredUsers.Count + 1;
        _registeredUsers[request.Username] = new RegisteredUser
        {
            Username = request.Username,
            HasPassword = true,
            HasPasskeys = false,
            UserId = userId
        };
        _hasPassword = true;

        Console.WriteLine($"[MockAutoHostClient] AuthRegisterAsync({request.Username}) => success, userId={userId}");
        return Task.FromResult(new RegisterResponse
        {
            Success = true,
            UserId = userId,
            Username = request.Username
        });
    }

    public Task<RegisterResponse> AuthRegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
        => AuthRegisterAsync(request);

    public Task<LoginResponse> AuthLoginAsync(LoginRequest request)
    {
        if (!_registeredUsers.ContainsKey(request.Username))
        {
            Console.WriteLine($"[MockAutoHostClient] AuthLoginAsync({request.Username}) => user not found");
            return Task.FromResult(new LoginResponse { Success = false });
        }

        var user = _registeredUsers[request.Username];
        if (!user.HasPassword)
        {
            Console.WriteLine($"[MockAutoHostClient] AuthLoginAsync({request.Username}) => no password set");
            return Task.FromResult(new LoginResponse { Success = false });
        }

        Console.WriteLine($"[MockAutoHostClient] AuthLoginAsync({request.Username}) => success");
        return Task.FromResult(new LoginResponse
        {
            Success = true,
            UserId = user.UserId,
            Username = user.Username,
            Token = $"mock-token-{user.UserId}"
        });
    }

    public Task<LoginResponse> AuthLoginAsync(LoginRequest request, CancellationToken cancellationToken)
        => AuthLoginAsync(request);

    // Other methods throw NotImplementedException
    public void Dispose() { }
    public Task<ApiKeyResponse> AuthGetApiKeyAsync() => throw new NotImplementedException();
    public Task<ApiKeyResponse> AuthGetApiKeyAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<SaveApiKeyResponse> AuthSaveApiKeyAsync(SaveApiKeyRequest request) => throw new NotImplementedException();
    public Task<SaveApiKeyResponse> AuthSaveApiKeyAsync(SaveApiKeyRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<ChallengeResponse> PasskeyChallengeAsync() => Task.FromResult(new ChallengeResponse { Challenge = "mock-challenge" });
    public Task<ChallengeResponse> PasskeyChallengeAsync(CancellationToken cancellationToken) => PasskeyChallengeAsync();

    public Task<RegisterPasskeyResponse> PasskeyRegisterAsync(RegisterNewUserPasskeyRequest request)
    {
        // Register a new user with passkey (no existing account)
        if (_registeredUsers.ContainsKey(request.Username))
        {
            Console.WriteLine($"[MockAutoHostClient] PasskeyRegisterAsync({request.Username}) => user already exists");
            return Task.FromResult(new RegisterPasskeyResponse { Success = false });
        }

        var userId = _registeredUsers.Count + 1;
        _registeredUsers[request.Username] = new RegisteredUser
        {
            Username = request.Username,
            HasPassword = false,
            HasPasskeys = true,
            UserId = userId
        };

        var passkeyId = _passkeys.Count + 1;
        _passkeys.Add(new PasskeyInfo
        {
            Id = passkeyId,
            DeviceName = request.DeviceName ?? "New Device",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUsedAt = DateTimeOffset.UtcNow
        });

        Console.WriteLine($"[MockAutoHostClient] PasskeyRegisterAsync({request.Username}) => success, userId={userId}, passkeyId={passkeyId}");
        return Task.FromResult(new RegisterPasskeyResponse
        {
            Success = true,
            PasskeyId = passkeyId,
            DeviceName = request.DeviceName ?? "New Device",
            Token = $"mock-token-{userId}",
            UserId = userId
        });
    }

    public Task<RegisterPasskeyResponse> PasskeyRegisterAsync(RegisterNewUserPasskeyRequest request, CancellationToken cancellationToken)
        => PasskeyRegisterAsync(request);

    public Task<RegisterPasskeyResponse> PasskeyEnrollAsync(RegisterPasskeyRequest request)
    {
        // Add passkey to existing user account (already logged in)
        var passkeyId = _passkeys.Count + 1;
        _passkeys.Add(new PasskeyInfo
        {
            Id = passkeyId,
            DeviceName = request.DeviceName ?? "New Device",
            CreatedAt = DateTimeOffset.UtcNow,
            LastUsedAt = DateTimeOffset.UtcNow
        });

        Console.WriteLine($"[MockAutoHostClient] PasskeyEnrollAsync() => success, passkeyId={passkeyId}");
        return Task.FromResult(new RegisterPasskeyResponse
        {
            Success = true,
            PasskeyId = passkeyId,
            DeviceName = request.DeviceName ?? "New Device"
        });
    }

    public Task<RegisterPasskeyResponse> PasskeyEnrollAsync(RegisterPasskeyRequest request, CancellationToken cancellationToken)
        => PasskeyEnrollAsync(request);

    public Task<AuthenticatePasskeyResponse> PasskeyAuthenticateAsync(AuthenticatePasskeyRequest request)
    {
        // AuthenticatePasskeyRequest doesn't have Username - it uses UserHandle from WebAuthn
        // For mock purposes, we'll just succeed if any user with passkeys exists
        var userWithPasskeys = _registeredUsers.Values.FirstOrDefault(u => u.HasPasskeys);

        if (userWithPasskeys == null)
        {
            Console.WriteLine($"[MockAutoHostClient] PasskeyAuthenticateAsync() => no users with passkeys");
            return Task.FromResult(new AuthenticatePasskeyResponse { Success = false });
        }

        Console.WriteLine($"[MockAutoHostClient] PasskeyAuthenticateAsync() => success for user {userWithPasskeys.Username}");
        return Task.FromResult(new AuthenticatePasskeyResponse
        {
            Success = true,
            Token = $"mock-token-{userWithPasskeys.UserId}",
            UserId = userWithPasskeys.UserId,
            Username = userWithPasskeys.Username
        });
    }

    public Task<AuthenticatePasskeyResponse> PasskeyAuthenticateAsync(AuthenticatePasskeyRequest request, CancellationToken cancellationToken)
        => PasskeyAuthenticateAsync(request);
    public Task<VersionResponse> GetVersionAsync() => Task.FromResult(new VersionResponse { Version = "Test-1.0.0" });
    public Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken) => GetVersionAsync();
}

public class MockPasskeyService : PasskeyService
{
    public MockPasskeyService(IJSRuntime jsRuntime, IAutoHostClient autoHostClient, ILogger<PasskeyService> logger)
        : base(jsRuntime, autoHostClient, logger)
    {
    }

    public override Task<bool> IsSupported() => Task.FromResult(true);
}

public class MockPasskeyServiceForAuth : PasskeyService
{
    private readonly NavigationManager _nav;

    // Configurable behavior for testing
    public bool? OverrideIsSupported { get; set; }
    public (bool Success, string? Token, AutoWeb.Client.AuthErrorCode? ErrorCode, string? Error)? OverrideAuthResult { get; set; }

    public MockPasskeyServiceForAuth(IJSRuntime jsRuntime, IAutoHostClient autoHostClient, ILogger<PasskeyService> logger, NavigationManager nav)
        : base(jsRuntime, autoHostClient, logger)
    {
        _nav = nav;
    }

    private string GetState()
    {
        var uri = new Uri(_nav.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["state"] ?? "new-user-passkey-supported";
    }

    public override Task<bool> IsSupported()
    {
        // Allow test to override
        if (OverrideIsSupported.HasValue)
        {
            Console.WriteLine($"[MockPasskeyServiceForAuth] IsSupported() => {OverrideIsSupported.Value} (overridden)");
            return Task.FromResult(OverrideIsSupported.Value);
        }

        var state = GetState();

        // States where passkeys are NOT supported
        var isSupported = state != "new-user-passkey-not-supported"
                       && state != "existing-passkey-only-not-supported";

        Console.WriteLine($"[MockPasskeyServiceForAuth] IsSupported() => {isSupported} (state={state})");
        return Task.FromResult(isSupported);
    }

    public override Task<(bool Success, string? Token, AutoWeb.Client.AuthErrorCode? ErrorCode, string? Error)> AuthenticateWithPasskeyAsync(string username)
    {
        Console.WriteLine($"[MockPasskeyServiceForAuth] AuthenticateWithPasskeyAsync({username})");

        // Allow test to override
        if (OverrideAuthResult.HasValue)
        {
            Console.WriteLine($"  => Overridden: Success={OverrideAuthResult.Value.Success}, ErrorCode={OverrideAuthResult.Value.ErrorCode}");
            return Task.FromResult(OverrideAuthResult.Value);
        }

        // Default: simulate successful authentication
        var result = (true, $"mock-token-{username}", (AutoWeb.Client.AuthErrorCode?)null, (string?)null);
        Console.WriteLine($"  => Default success: Token={result.Item2}");
        return Task.FromResult(result);
    }
}