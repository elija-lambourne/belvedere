using belvedere.Core.Services;
using belvedere.Core.Util;
using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using belvedere.Util;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;

namespace belvedere.Controllers;

/// <summary>
/// API controller for managing share links for photos and albums.
/// </summary>
/// <remarks>
/// Provides endpoints for authenticated users to create and retrieve share links for their photos and albums.
/// Share links can be optionally password-protected and can have expiration times.
/// Provides both authenticated share creation and public share retrieval endpoints.
/// </remarks>
[Route("api/shares")]
public sealed class ShareController(
    ITransactionProvider transaction,
    IUnitOfWork uow,
    IPhotoService photoService,
    IAlbumService albumService,
    IShareService shareService,
    IStorageService storageService,
    IOptions<Settings> settings,
    ILogger<ShareController> logger) : BaseController
{
    /// <summary>
    /// Creates a new share link for a photo or album with optional password protection and expiration.
    /// </summary>
    /// <param name="request">The create share request containing target type, ID, password, and expiration details.</param>
    /// <returns>
    /// A <see cref="CreateShareResponse"/> containing the generated share key, shareable URL, and expiration time.
    /// </returns>
    /// <remarks>
    /// This endpoint requires user authentication. The authenticated user must be the owner of the photo or album
    /// being shared. The share key is generated using cryptographically secure randomness and is unique.
    /// If a password is provided, it will be hashed using PBKDF2-SHA256 before storage.
    /// Changes are committed within a database transaction; if any error occurs, the transaction is rolled back.
    /// </remarks>
    /// <response code="201">Share link created successfully.</response>
    /// <response code="400">Invalid request data (validation failed or invalid target type).</response>
    /// <response code="401">User is not authenticated or external sub claim is missing.</response>
    /// <response code="404">The photo/album does not exist or the user does not own it.</response>
    [HttpPost]
    [Route("")]
    [ProducesResponseType<CreateShareResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> CreateShare([FromBody] CreateShareRequest request)
    {
        if (!ValidateRequest<CreateShareRequest.Validator, CreateShareRequest>(request, out string[]? validationErrors))
        {
            return BadRequest(validationErrors);
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        string? externalSub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(externalSub))
        {
            return Unauthorized();
        }

        var currentUser = await uow.UserRepository.GetUserByExternalSubAsync(externalSub);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            ShareKey shareKey;
            string targetType = request.TargetType.Trim().ToLowerInvariant();
            if (targetType == "photo")
            {
                var photoResult = await photoService.GetPhotoByIdAsync(request.TargetId);
                if (photoResult.IsT1)
                {
                    await transaction.RollbackAsync();

                    return NotFound();
                }

                var photo = photoResult.AsT0;
                if (photo.UserId != currentUser.Id)
                {
                    await transaction.RollbackAsync();

                    return NotFound();
                }

                shareKey = await shareService.CreateShareAsync(null, photo.Id, request.Password, request.ExpiresAt);
            }
            else if (targetType == "album")
            {
                var albumResult = await albumService.GetAlbumByIdAsync(request.TargetId);
                if (albumResult.IsT1)
                {
                    await transaction.RollbackAsync();

                    return NotFound();
                }

                var album = albumResult.AsT0;
                if (album.UserId != currentUser.Id)
                {
                    await transaction.RollbackAsync();

                    return NotFound();
                }

                shareKey = await shareService.CreateShareAsync(album.Id, null, request.Password, request.ExpiresAt);
            }
            else
            {
                await transaction.RollbackAsync();

                return BadRequest(new { error = "TargetType must be either 'photo' or 'album'" });
            }

            await transaction.CommitAsync();

            var response = new CreateShareResponse(shareKey.Key,
                                                   BuildShareUrl(shareKey.Key),
                                                   shareKey.ExpiresAt);

            return CreatedAtAction(nameof(GetShareByKey), new { key = shareKey.Key }, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create share");
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    /// <summary>
    /// Retrieves shared content (photo or album) using a share key and optional password.
    /// </summary>
    /// <param name="key">The unique share key identifying the shared resource.</param>
    /// <param name="password">Optional password for password-protected shares.</param>
    /// <returns>
    /// A <see cref="SharedResourceResponse"/> containing either the shared photo or album with presigned URLs
    /// for accessing the actual content from S3 storage.
    /// </returns>
    /// <remarks>
    /// This endpoint is public and does not require authentication.
    /// If the share is password-protected, the correct password must be provided in the query string.
    /// Returned URLs for photos are valid for 5 minutes.
    /// If the share has an expiration date and that date has passed, an HTTP 410 Gone status is returned.
    /// </remarks>
    /// <response code="200">The shared resource was retrieved successfully.</response>
    /// <response code="401">The share is password-protected and the password is incorrect or missing.</response>
    /// <response code="404">The share key does not exist.</response>
    /// <response code="410">The share link has expired.</response>
    [HttpGet]
    [Route("{key}")]
    [ProducesResponseType<SharedResourceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async ValueTask<ActionResult<SharedResourceResponse>> GetShareByKey(
        [FromRoute] string key, [FromQuery] string? password = null)
    {
        var resolution = await shareService.ResolveShareAsync(key, password);

        return await resolution.Match<ValueTask<ActionResult<SharedResourceResponse>>>(async shareKey =>
         {
             if (shareKey.PhotoId is not null)
             {
                 var photoResult = await photoService.GetPhotoByIdAsync(shareKey.PhotoId.Value);
                 if (photoResult.IsT1)
                 {
                     return NotFound();
                 }

                 var photo = photoResult.AsT0;
                 string temporaryUrl
                     = await storageService.GetPresignedUrlAsync(photo.StorageKey, TimeSpan.FromMinutes(5));

                 return Ok(new SharedResourceResponse("photo",
                                                      new SharedPhotoResponse(photo.Id, photo.Title, temporaryUrl),
                                                      null));
             }

             if (shareKey.AlbumId is not null)
             {
                 Album? album = await uow.AlbumRepository.GetAlbumByIdWithPhotosAsync(shareKey.AlbumId.Value);
                 if (album is null)
                 {
                     return NotFound();
                 }

                 var photos = new List<SharedPhotoResponse>();
                 foreach (Photo photo in album.Photos.OrderBy(p => p.CreatedAt))
                 {
                     string temporaryUrl
                         = await storageService.GetPresignedUrlAsync(photo.StorageKey, TimeSpan.FromMinutes(5));
                     photos.Add(new SharedPhotoResponse(photo.Id, photo.Title, temporaryUrl));
                 }

                 return Ok(new SharedResourceResponse("album",
                                                      null,
                                                      new SharedAlbumResponse(album.Id, album.Title,
                                                                              album.Description, photos)));
             }

             return NotFound();
          },
          notFound => ValueTask.FromResult<ActionResult<SharedResourceResponse>>(NotFound()),
          unauthorized => ValueTask.FromResult<ActionResult<SharedResourceResponse>>(Unauthorized()),
          expired => ValueTask.FromResult<ActionResult<SharedResourceResponse>>(StatusCode(StatusCodes.Status410Gone)));
     }

    /// <summary>
    /// Builds the complete shareable URL for a given share key.
    /// </summary>
    /// <param name="key">The share key to include in the URL.</param>
    /// <returns>A complete URL to the shared resource that can be sent to other users.</returns>
    /// <remarks>
    /// Constructs a client-side URL by appending the share key to the configured client origin.
    /// The client origin is trimmed to remove trailing slashes before concatenation.
    /// </remarks>
    private string BuildShareUrl(string key)
    {
        string origin = settings.Value.ClientOrigin.TrimEnd('/');

        return $"{origin}/share/{key}";
    }
}

/// <summary>
/// Request model for creating a new share link for a photo or album.
/// </summary>
/// <remarks>
/// Used by the CreateShare endpoint to specify what resource to share, with optional password protection
/// and expiration constraints.
/// </remarks>
public sealed class CreateShareRequest(string targetType, Guid targetId, string? password, DateTime? expiresAt)
{
    /// <summary>
    /// Gets the type of resource being shared ("photo" or "album").
    /// </summary>
    public string TargetType { get; } = targetType;

    /// <summary>
    /// Gets the unique identifier of the resource being shared.
    /// </summary>
    public Guid TargetId { get; } = targetId;

    /// <summary>
    /// Gets the optional password to protect the share link.
    /// </summary>
    /// <remarks>
    /// If provided, users accessing the share must provide this password.
    /// Maximum length is 256 characters.
    /// </remarks>
    public string? Password { get; } = password;

    /// <summary>
    /// Gets the optional expiration date and time for the share link.
    /// </summary>
    /// <remarks>
    /// If provided, the share link becomes invalid after this date/time.
    /// Must be in the future relative to the current UTC time.
    /// </remarks>
    public DateTime? ExpiresAt { get; } = expiresAt;

    /// <summary>
    /// Validator for the CreateShareRequest model using FluentValidation.
    /// </summary>
    /// <remarks>
    /// Validates that:
    /// - TargetType is not empty and is either "photo" or "album"
    /// - TargetId is not empty
    /// - Password maximum length does not exceed 256 characters
    /// - ExpiresAt (if provided) is a future date/time
    /// </remarks>
    public sealed class Validator : AbstractValidator<CreateShareRequest>
    {
        public Validator()
        {
            RuleFor(x => x.TargetType)
                .NotEmpty()
                .Must(value => value.Equals("photo", StringComparison.OrdinalIgnoreCase) ||
                               value.Equals("album", StringComparison.OrdinalIgnoreCase))
                .WithMessage("TargetType must be either 'photo' or 'album'");

            RuleFor(x => x.TargetId)
                .NotEmpty();

            RuleFor(x => x.Password)
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.Password));

            RuleFor(x => x.ExpiresAt)
                .Must(value => value is null || value.Value > DateTime.UtcNow)
                .WithMessage("ExpiresAt must be in the future")
                .When(x => x.ExpiresAt is not null);
        }
    }
}

/// <summary>
/// Response model sent when a share link is created successfully.
/// </summary>
/// <remarks>
/// Contains the generated share key, the full shareable URL, and optional expiration information.
/// </remarks>
public sealed class CreateShareResponse(string shareKey, string shareUrl, DateTime? expiresAt)
{
    /// <summary>
    /// Gets the unique share key generated for this share.
    /// </summary>
    /// <remarks>
    /// This key is used to identify and access the shared resource.
    /// </remarks>
    public string ShareKey { get; } = shareKey;

    /// <summary>
    /// Gets the complete URL that can be shared with others to access the shared resource.
    /// </summary>
    public string ShareUrl { get; } = shareUrl;

    /// <summary>
    /// Gets the optional expiration date and time for this share link.
    /// </summary>
    /// <remarks>
    /// If null, the share link does not expire.
    /// After this time, the share link becomes inaccessible and returns HTTP 410 Gone.
    /// </remarks>
    public DateTime? ExpiresAt { get; } = expiresAt;
}

/// <summary>
/// Response model for retrieving a shared resource (photo or album).
/// </summary>
/// <remarks>
/// Contains either a shared photo or a shared album with all necessary information
/// including presigned URLs for accessing the actual content.
/// </remarks>
public sealed class SharedResourceResponse(string targetType, SharedPhotoResponse? photo, SharedAlbumResponse? album)
{
    /// <summary>
    /// Gets the type of shared resource ("photo" or "album").
    /// </summary>
    public string TargetType { get; } = targetType;

    /// <summary>
    /// Gets the shared photo details if this is a photo share.
    /// </summary>
    /// <remarks>
    /// This is non-null only when TargetType is "photo".
    /// </remarks>
    public SharedPhotoResponse? Photo { get; } = photo;

    /// <summary>
    /// Gets the shared album details if this is an album share.
    /// </summary>
    /// <remarks>
    /// This is non-null only when TargetType is "album".
    /// Includes all photos in the album with their presigned URLs.
    /// </remarks>
    public SharedAlbumResponse? Album { get; } = album;
}

/// <summary>
/// Response model containing details about a shared photo.
/// </summary>
/// <remarks>
/// Includes the photo's metadata and a temporary presigned URL for accessing the image from storage.
/// </remarks>
public sealed class SharedPhotoResponse(Guid id, string title, string temporaryUrl)
{
    /// <summary>
    /// Gets the unique identifier of the photo.
    /// </summary>
    public Guid Id { get; } = id;

    /// <summary>
    /// Gets the title or name of the photo.
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// Gets the temporary presigned URL for accessing the photo from S3 storage.
    /// </summary>
    /// <remarks>
    /// This URL is valid for 5 minutes. After expiration, a new share request must be made
    /// to obtain a new presigned URL.
    /// </remarks>
    public string TemporaryUrl { get; } = temporaryUrl;
}

/// <summary>
/// Response model containing details about a shared album and its photos.
/// </summary>
/// <remarks>
/// Includes the album's metadata and a collection of all photos in the album,
/// each with their own presigned URLs for viewing.
/// </remarks>
public sealed class SharedAlbumResponse(
    Guid id,
    string title,
    string? description,
    IReadOnlyList<SharedPhotoResponse> photos)
{
    /// <summary>
    /// Gets the unique identifier of the album.
    /// </summary>
    public Guid Id { get; } = id;

    /// <summary>
    /// Gets the title or name of the album.
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// Gets the optional description or notes about the album.
    /// </summary>
    public string? Description { get; } = description;

    /// <summary>
    /// Gets the collection of photos contained in this album.
    /// </summary>
    /// <remarks>
    /// Photos are ordered by creation date in ascending order.
    /// Each photo includes a temporary presigned URL valid for 5 minutes.
    /// </remarks>
    public IReadOnlyList<SharedPhotoResponse> Photos { get; } = photos;
}
