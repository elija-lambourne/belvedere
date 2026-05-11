using belvedere.Persistence.Model;

namespace belvedere.Controllers.DTOs;

/// <summary>
///     Response model containing a temporary presigned URL for accessing a photo.
/// </summary>
/// <remarks>
///     This response object is returned by the GetSignedUrl endpoint and contains a temporary, time-limited URL
///     that can be used to access the photo from S3 storage without requiring authentication.
/// </remarks>
public sealed class PhotoSignedUrlResponse
{
    /// <summary>
    ///     Gets the temporary presigned URL for accessing the photo.
    /// </summary>
    /// <remarks>
    ///     This URL is valid for 5 minutes and can be used to download or view the photo without authentication.
    ///     After expiration, a new presigned URL must be requested.
    /// </remarks>
    public required string TemporaryUrl { get; init; }
}

public record PhotoDto
{
    /// <summary>
    ///     The unique identifier of the photo.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     The title of the photo.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     The filename of the photo
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    ///     Optional description of the photo.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     The width of the photo in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     The height of the photo in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     The MIME type of the photo.
    /// </summary>
    public required string MimeType { get; init; }

    /// <summary>
    ///     The date and time when the photo was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
///     Response model for a photo within an album context.
/// </summary>
public sealed record PhotoBlurDto : PhotoDto
{
    /// <summary>
    ///     The blurhash of the photo (for placeholder rendering).
    /// </summary>
    public required string BlurHash { get; init; }
}

/// <summary>
///     Response model for expanded photo metadata.
/// </summary>
public sealed record PhotoThumbnailDto : PhotoDto
{
    /// <summary>
    ///     The file size in bytes.
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    ///     Camera make (EXIF data).
    /// </summary>
    public string? Make { get; init; }

    /// <summary>
    ///     Camera model (EXIF data).
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    ///     Exposure time in seconds.
    /// </summary>
    public double? ExposureTime { get; init; }

    /// <summary>
    ///     F-number of the photo.
    /// </summary>
    public double? FNumber { get; init; }

    /// <summary>
    ///     ISO sensitivity value.
    /// </summary>
    public int? Iso { get; init; }

    /// <summary>
    ///     City where the photo was taken.
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    ///     Whether the photo is a Live Photo.
    /// </summary>
    public bool IsLivePhoto { get; init; }

    /// <summary>
    ///     The temporary signed URL for the thumbnail
    /// </summary>
    public required string ThumbnailUrl { get; init; }
}

public sealed record PhotoMetaDataDto : PhotoDto
{
    /// <summary>
    ///     Latitude of where the photo was taken.
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    ///     Longitude of where the photo was taken.
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    ///     Country code where the photo was taken.
    /// </summary>
    public string? CountryCode { get; init; }

    /// <summary>
    ///     The date and time when the photo was captured.
    /// </summary>
    public DateTime CapturedAt { get; init; }

    /// <summary>
    ///     The file size in bytes.
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    ///     Camera make (EXIF data).
    /// </summary>
    public string? Make { get; init; }

    /// <summary>
    ///     Camera model (EXIF data).
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    ///     Exposure time in seconds.
    /// </summary>
    public double? ExposureTime { get; init; }

    /// <summary>
    ///     F-number of the photo.
    /// </summary>
    public double? FNumber { get; init; }

    /// <summary>
    ///     ISO sensitivity value.
    /// </summary>
    public int? Iso { get; init; }

    /// <summary>
    ///     City where the photo was taken.
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    ///     Whether the photo is a Live Photo.
    /// </summary>
    public bool IsLivePhoto { get; init; }

    public required Dictionary<ReactionType, uint> Reactions { get; init; }
}
