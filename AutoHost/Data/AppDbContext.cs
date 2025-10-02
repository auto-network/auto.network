using Microsoft.EntityFrameworkCore;
using AutoHost.Models;

namespace AutoHost.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<UserPasskey> UserPasskeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();

            entity.HasMany(e => e.ApiKeys)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Passkeys)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore the computed property
            entity.Ignore(e => e.HasPassword);
        });

        // Configure ApiKey entity
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
        });

        // Configure Session entity
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany(e => e.Sessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserPasskey entity
        modelBuilder.Entity<UserPasskey>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Index on UserId for quick lookups
            entity.HasIndex(e => e.UserId);

            // CredentialId must be unique across all users
            entity.HasIndex(e => e.CredentialId).IsUnique();

            // Configure byte array properties
            entity.Property(e => e.CredentialId)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.PublicKey)
                .IsRequired();

            // Configure optional string properties
            entity.Property(e => e.DeviceName)
                .HasMaxLength(100);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            // Ignore computed properties
            entity.Ignore(e => e.CredentialIdBase64);
            entity.Ignore(e => e.PublicKeyBase64);

            // Navigation
            entity.HasOne(e => e.User)
                .WithMany(e => e.Passkeys)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}