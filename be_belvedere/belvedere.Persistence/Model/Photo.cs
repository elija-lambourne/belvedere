using System.ComponentModel.DataAnnotations;

namespace belvedere.Persistence.Model;

public sealed class Photo
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    [MaxLength(255)]
    public string? Title { get; set; }
    public required string FileName { get; init; }
    [MaxLength(2000)]
    public string? Description { get; set; }
    [MaxLength(1024)]
    public required string StorageKey { get; set; }
    [MaxLength(1024)]
    public required string ThumbKey { get; set; }
    [MaxLength(255)]
    public required string MimeType { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime CapturedAt { get; set; }
    [MaxLength(255)]
    public string? Make { get; set; }
    [MaxLength(255)]
    public string? Model { get; set; }
    public double? ExposureTime { get; set; }
    public double? FocalLength { get; init; }
    public double? FNumber { get; set; }
    public int? Iso { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    [MaxLength(3)]
    public string? CountryCode { get; set; }
    [MaxLength(255)]
    public string? City { get; set; }
    public bool IsLivePhoto { get; set; }
    
    public ICollection<ShareKey> Shares { get; set; } = new List<ShareKey>();
    public User User { get; set; } = null!;
    public ICollection<Album> Albums { get; set; } = new List<Album>();
    public ICollection<PhotoReaction> Reactions { get; set; } = new List<PhotoReaction>();
}

