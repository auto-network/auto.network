using System.ComponentModel.DataAnnotations;

namespace AutoHost.Models;

public class Session
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Token { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    public DateTime? LastAccessedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property
    public User User { get; set; } = null!;
}