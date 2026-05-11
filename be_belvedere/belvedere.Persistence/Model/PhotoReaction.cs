namespace belvedere.Persistence.Model;

public sealed class PhotoReaction
{
    public long Id { get; set; }
    public Guid PhotoId { get; set; }
    public Guid UserId { get; set; }
    public ReactionType Reaction { get; set; }
    public DateTime CreatedAt { get; set; }

    public Photo Photo { get; set; } = null!;
    public User User { get; set; } = null!;
}

