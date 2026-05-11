using System.ComponentModel.DataAnnotations;

namespace belvedere.Persistence.Model;

public sealed class Album
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? CoverPhotoId { get; set; }
    [MaxLength(255)]
    public required string Title { get; set; }
    [MaxLength(2000)]
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    // Replace single share key with multiple share entries so each can have
    // an optional password and optional expiration.
    public ICollection<ShareKey> Shares { get; set; } = new List<ShareKey>();
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Photo? CoverPhoto { get; set; }
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
