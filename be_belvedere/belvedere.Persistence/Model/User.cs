using System.ComponentModel.DataAnnotations;

namespace belvedere.Persistence.Model;

public sealed class User
{
    public Guid Id { get; set; }
    [MaxLength(255)]
    public required string ExternalSub { get; set; }
    [MaxLength(320)]
    public required string Email { get; set; }
    public DateTime LastLogin { get; set; }
    public long StorageUsed { get; set; }

    public ICollection<belvedere.Persistence.Model.Photo> Photos { get; set; } = new List<belvedere.Persistence.Model.Photo>();
    public ICollection<belvedere.Persistence.Model.Album> Albums { get; set; } = new List<belvedere.Persistence.Model.Album>();
    public ICollection<belvedere.Persistence.Model.PhotoReaction> Reactions { get; set; } = new List<belvedere.Persistence.Model.PhotoReaction>();
}



