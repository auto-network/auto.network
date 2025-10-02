using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using AutoWeb.Services;
using AutoWeb.Pages;

namespace AutoWeb.Tests;

/// <summary>
/// Mock JSRuntime for both AuthenticationSettings and Auth.razor testing.
/// Implements sessionStorage and passkey operations.
/// </summary>
public class MockJSRuntime : IJSRuntime
{
    private readonly Dictionary<string, string> _sessionStorage = new();

    public MockJSRuntime()
    {
        // Pre-populate with default userEmail for existing tests
        _sessionStorage["userEmail"] = "test@example.com";
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Console.WriteLine($"[MockJSRuntime] InvokeAsync<{typeof(TValue).Name}>({identifier}, args={args?.Length ?? 0})");

        switch (identifier)
        {
            // sessionStorage.setItem
            case "sessionStorage.setItem":
                if (args != null && args.Length >= 2)
                {
                    var key = args[0]?.ToString() ?? "";
                    var value = args[1]?.ToString() ?? "";
                    _sessionStorage[key] = value;
                    Console.WriteLine($"  sessionStorage['{key}'] = '{value}'");
                }
                return new ValueTask<TValue>(default(TValue)!);

            // sessionStorage.getItem
            case "sessionStorage.getItem":
                if (args != null && args.Length >= 1)
                {
                    var key = args[0]?.ToString() ?? "";
                    var value = _sessionStorage.ContainsKey(key) ? _sessionStorage[key] : null;
                    Console.WriteLine($"  sessionStorage['{key}'] => '{value}'");
                    return new ValueTask<TValue>((TValue)(object)value!);
                }
                return new ValueTask<TValue>(default(TValue)!);

            // PasskeySupport.isSupported - should be handled by PasskeyService
            case "PasskeySupport.isSupported":
                Console.WriteLine("  PasskeySupport.isSupported => true");
                return new ValueTask<TValue>((TValue)(object)true);

            // PasskeySupport.createPasskey - return mock passkey creation data
            case "PasskeySupport.createPasskey":
                Console.WriteLine("  PasskeySupport.createPasskey => mock data");
                var mockCreation = new Auth.PasskeyCreationResult
                {
                    CredentialId = "mock-credential-id",
                    PublicKey = "mock-public-key",
                    AttestationObject = "mock-attestation",
                    ClientDataJSON = "mock-client-data",
                    UserHandle = "mock-user-handle"
                };
                return new ValueTask<TValue>((TValue)(object)mockCreation);

            // PasskeySupport.getPasskey - return mock passkey authentication data
            case "PasskeySupport.getPasskey":
                Console.WriteLine("  PasskeySupport.getPasskey => mock data");
                var mockAuth = new PasskeyAuthenticationResult
                {
                    CredentialId = "mock-credential-id",
                    AuthenticatorData = "mock-authenticator-data",
                    ClientDataJSON = "mock-client-data",
                    Signature = "mock-signature",
                    UserHandle = "mock-user-handle"
                };
                return new ValueTask<TValue>((TValue)(object)mockAuth);

            // eval - used for focus() calls, just no-op
            case "eval":
                Console.WriteLine("  eval => no-op");
                return new ValueTask<TValue>(default(TValue)!);

            default:
                Console.WriteLine($"  ⚠️  Unhandled JS call: {identifier}");
                return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
