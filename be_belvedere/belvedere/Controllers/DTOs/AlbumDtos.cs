namespace belvedere.Controllers.DTOs;

/// <summary>
///     Request model for creating a new album.
/// </summary>
public sealed record CreateAlbumRequest
{
    /// <summary>
    ///     The title of the album.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    ///     Optional description of the album.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     The unique identifier of the cover photo (if set).
    /// </summary>
    public Guid? CoverPhotoId { get; init; }

    /// <summary>
    ///     Whether the album is publicly visible.
    /// </summary>
    public bool IsPublic { get; init; }
}

/// <summary>
///     Response model for album information.
/// </summary>
public record AlbumDto
{
    /// <summary>
    ///     The unique identifier of the album.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     The title of the album.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    ///     Optional description of the album.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     The unique identifier of the cover photo (if set).
    /// </summary>
    public Guid? CoverPhotoId { get; init; }

    /// <summary>
    ///     Whether the album is publicly visible.
    /// </summary>
    public bool IsPublic { get; init; }

    /// <summary>
    ///     The date and time when the album was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    ///     The number of photos in the album.
    /// </summary>
    public int PhotoCount { get; init; }
}

/// <summary>
///     Response model for album with thumbnail photo details.
/// </summary>
public sealed record AlbumThumbnailDto : AlbumDto
{
    /// <summary>
    ///     The thumbnail photos in the album.
    /// </summary>
    public required List<PhotoThumbnailDto> Photos { get; init; }
}
