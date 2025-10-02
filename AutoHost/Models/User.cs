using System.ComponentModel.DataAnnotations;

namespace AutoHost.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    // Password is now optional - users can use passkeys only
    public string? PasswordHash { get; set; }

    // Track if user has a password set
    public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

    // Auth method preference: "password", "passkey", or "both"
    [MaxLength(20)]
    public string PreferredAuthMethod { get; set; } = "password";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public List<ApiKey> ApiKeys { get; set; } = new();
    public List<Session> Sessions { get; set; } = new();
    public List<UserPasskey> Passkeys { get; set; } = new();
}