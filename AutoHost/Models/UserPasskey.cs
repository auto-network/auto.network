namespace AutoHost.Models;

public class UserPasskey
{
    public int Id { get; set; }

    public int UserId { get; set; }

    // Store as byte array (will be indexed unique)
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();

    // Public key for verification
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();

    // Counter for replay attack prevention
    public uint SignCount { get; set; } = 0;

    // Optional metadata
    public string? DeviceName { get; set; }
    public string? UserAgent { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Navigation property
    public User User { get; set; } = null!;

    // Helper properties for base64 encoding (like ChatAIze pattern)
    public string CredentialIdBase64 => Convert.ToBase64String(CredentialId);
    public string PublicKeyBase64 => Convert.ToBase64String(PublicKey);
}