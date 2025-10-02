using System.Text.Json.Serialization;

namespace AutoHost.Models;

/// <summary>
/// Error codes for authentication and authorization operations.
/// These codes enable proper error handling, i18n support, and eliminate fragile string matching.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuthErrorCode
{
    /// <summary>No error occurred</summary>
    None = 0,

    // Validation errors (1000-1999)
    /// <summary>Username/email is required but not provided</summary>
    UsernameRequired = 1000,
    /// <summary>Password is required but not provided</summary>
    PasswordRequired = 1001,
    /// <summary>API key is required but not provided</summary>
    ApiKeyRequired = 1002,
    /// <summary>Invalid email format</summary>
    InvalidEmail = 1003,

    // Authentication errors (2000-2999)
    /// <summary>Invalid username or password</summary>
    InvalidCredentials = 2000,
    /// <summary>User cancelled the authentication request (e.g., dismissed passkey prompt)</summary>
    AuthenticationCancelled = 2001,
    /// <summary>Authentication challenge has expired or is invalid</summary>
    InvalidOrExpiredChallenge = 2002,
    /// <summary>User is not authenticated (missing or invalid token)</summary>
    AuthenticationRequired = 2003,
    /// <summary>Account requires passkey authentication (password login disabled)</summary>
    PasskeyRequired = 2004,
    /// <summary>Passkey authentication failed</summary>
    PasskeyAuthenticationFailed = 2005,
    /// <summary>Wrong passkey selected (doesn't belong to this user)</summary>
    WrongPasskeySelected = 2006,

    // Authorization errors (3000-3999)
    /// <summary>User not found in database</summary>
    UserNotFound = 3000,
    /// <summary>Passkey not found or doesn't belong to user</summary>
    PasskeyNotFound = 3001,
    /// <summary>Connection not found or has been deleted</summary>
    ConnectionNotFound = 3002,
    /// <summary>User does not have permission to access this resource</summary>
    Forbidden = 3003,

    // Registration errors (4000-4999)
    /// <summary>Username/email already exists in database</summary>
    UsernameAlreadyExists = 4000,
    /// <summary>User already has a password (use change password instead)</summary>
    PasswordAlreadyExists = 4001,
    /// <summary>Passkey registration failed</summary>
    PasskeyRegistrationFailed = 4002,

    // Business logic errors (5000-5999)
    /// <summary>Cannot remove last authentication method (user would be locked out)</summary>
    CannotRemoveLastAuthMethod = 5000,
    /// <summary>Cannot remove password without at least one active passkey</summary>
    CannotRemovePasswordWithoutPasskey = 5001,
    /// <summary>Invalid service type and protocol combination</summary>
    InvalidServiceProtocol = 5002,

    // Browser/client errors (6000-6999)
    /// <summary>Browser doesn't support passkeys (WebAuthn not available)</summary>
    PasskeyNotSupported = 6000,
    /// <summary>Cannot connect to server</summary>
    CannotConnect = 6001,

    // Generic errors (9000-9999)
    /// <summary>An unexpected error occurred</summary>
    UnknownError = 9000,
    /// <summary>Operation failed for unspecified reason</summary>
    OperationFailed = 9001
}
