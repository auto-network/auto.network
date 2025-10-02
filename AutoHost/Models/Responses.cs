namespace AutoHost.Models;

public class RegisterResponse
{
    public bool Success { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class CheckUserResponse
{
    public bool Exists { get; set; }
}

public class LoginResponse
{
    public bool Success { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class ApiKeyResponse
{
    public string ApiKey { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class SaveApiKeyResponse
{
    public bool Success { get; set; }
}

public class VersionResponse
{
    public string Version { get; set; } = "1.0.0";
    public string Status { get; set; } = "ok";
}

public class ErrorResponse
{
    /// <summary>Human-readable error message (for backwards compatibility and debugging)</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>Machine-readable error code for proper error handling and i18n</summary>
    public AuthErrorCode ErrorCode { get; set; } = AuthErrorCode.None;
}