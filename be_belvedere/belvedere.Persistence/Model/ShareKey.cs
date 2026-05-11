using System.ComponentModel.DataAnnotations;

namespace belvedere.Persistence.Model;

public sealed class ShareKey
{
    public Guid Id { get; set; }
    public Guid? AlbumId { get; set; }
    public Guid? PhotoId { get; set; }
    
    [MaxLength(128)]
    public required string Key { get; set; }

    // Optional password (store hashed value). Nullable when no password is set for this share.
    [MaxLength(512)]
    public string? PasswordHash { get; set; }

    // Optional expiration - when null the share does not expire.
    public DateTime? ExpiresAt { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Album? Album { get; set; }
    public Photo? Photo { get; set; }
}




