using belvedere.Controllers.DTOs;
using belvedere.Core.Services;
using belvedere.Core.Util;
using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using belvedere.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using SkiaSharp;

namespace belvedere.Controllers;

/// <summary>
///     API controller for managing photo access and retrieving presigned URLs.
/// </summary>
/// <remarks>
///     This controller is part of the Backend-for-Frontend (BFF) pattern and provides endpoints for:
///     - Authenticated users to access and manage their photos
///     - Public read-only access to photos in public albums
///     - Share-key access to photos via secure, password-protected share links
///     
///     Most endpoints require Keycloak OIDC authentication via session cookies.
///     Some endpoints support optional share-key access for public sharing scenarios.
/// </remarks>
[Route("api/photos")]
[ApiController]
public sealed class PhotoController(
    IUnitOfWork uow,
    IPhotoService photoService,
    IStorageService storageService,
    IShareService shareService,
    ILogger<PhotoController> logger) : BaseController
{
    /// <summary>
    ///     Retrieves a temporary presigned URL for accessing a specific photo.
    /// </summary>
    /// <param name="id">The GUID identifier of the photo to retrieve a presigned URL for.</param>
    /// <param name="shareKey">Optional share key for access to non-public photos. Required if accessing a non-public photo without authentication.</param>
    /// <param name="sharePassword">Optional password if the share key is password-protected.</param>
    /// <returns>
    ///     A <see cref="PhotoSignedUrlResponse" /> containing a temporary presigned URL that expires in 5 minutes.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Access is allowed in the following scenarios:
    ///         1. User is authenticated and owns the photo
    ///         2. The photo is in a public album (no authentication required)
    ///         3. A valid, non-expired share key is provided (optionally password-protected)
    ///     </para>
    ///     <para>
    ///         The returned URL is valid for 5 minutes and can be used to download/view the photo from S3 storage.
    ///         After expiration, a new URL must be requested via this endpoint.
    ///     </para>
    /// </remarks>
    /// <response code="200">Returns the presigned URL successfully.</response>
    /// <response code="400">The photo ID is empty (Guid.Empty).</response>
    /// <response code="401">The share key requires a password (only sent with key, no password provided).</response>
    /// <response code="404">The photo does not exist or the user does not have access to it.</response>
    /// <response code="410">The share key has expired.</response>
    [HttpGet]
    [Route("{id:guid}/signed-url")]
    [AllowAnonymous]
    [ProducesResponseType<PhotoSignedUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async ValueTask<ActionResult<PhotoSignedUrlResponse>> GetSignedUrl(
        [FromRoute] Guid id,
        [FromQuery] string? shareKey = null,
        [FromQuery] string? sharePassword = null)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        OneOf<Photo, NotFound> photoResult = await photoService.GetPhotoByIdAsync(id);

        return await photoResult.Match<ValueTask<ActionResult<PhotoSignedUrlResponse>>>(async photo =>
        {
            if (!await HasPhotoAccessAsync(photo, shareKey, sharePassword))
            {
                logger.LogInformation("User tried to access photo {PhotoId} without access", id);

                return Unauthorized();
            }

            string temporaryUrl = await storageService.GetPresignedUrlAsync(photo.StorageKey, TimeSpan.FromMinutes(5));

            return Ok(new PhotoSignedUrlResponse
            {
                TemporaryUrl = temporaryUrl
            });
        }, _ => ValueTask.FromResult<ActionResult<PhotoSignedUrlResponse>>(NotFound()));
    }

    /// <summary>
    ///     Retrieves the expanded metadata of a specific photo.
    /// </summary>
    /// <param name="id">The GUID identifier of the photo to retrieve metadata for.</param>
    /// <param name="shareKey">Optional share key for access to non-public photos. Required if accessing a non-public photo without authentication.</param>
    /// <param name="sharePassword">Optional password if the share key is password-protected.</param>
    /// <returns>
    ///     A <see cref="PhotoMetaDataDto" /> containing all metadata of the photo including EXIF data.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This endpoint returns comprehensive metadata about a photo including EXIF data, location, dimensions, etc.
    ///     </para>
    ///     <para>
    ///         Access is allowed in the following scenarios:
    ///         1. User is authenticated and owns the photo
    ///         2. The photo is in a public album (no authentication required)
    ///         3. A valid, non-expired share key is provided (optionally password-protected)
    ///     </para>
    /// </remarks>
    /// <response code="200">Returns the photo metadata successfully.</response>
    /// <response code="400">The photo ID is empty (Guid.Empty).</response>
    /// <response code="401">The share key requires a password (only sent with key, no password provided).</response>
    /// <response code="404">The photo does not exist or the user does not have access to it.</response>
    /// <response code="410">The share key has expired.</response>
    [HttpGet]
    [Route("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<PhotoMetaDataDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async ValueTask<ActionResult<PhotoMetaDataDto>> GetPhotoMetadata(
        [FromRoute] Guid id,
        [FromQuery] string? shareKey = null,
        [FromQuery] string? sharePassword = null)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        OneOf<Photo, NotFound> photoResult = await photoService.GetPhotoByIdAsync(id);

        return await photoResult.Match<ValueTask<ActionResult<PhotoMetaDataDto>>>(async photo =>
        {
            if (!await HasPhotoAccessAsync(photo, shareKey, sharePassword))
            {
                logger.LogInformation("User tried to access photo {PhotoId} without proper access", id);

                return Unauthorized();
            }

            return Ok(MapPhotoToMetadata(photo));
        }, _ => ValueTask.FromResult<ActionResult<PhotoMetaDataDto>>(NotFound()));
    }

    /// <summary>
    ///     Maps a Photo model to a PhotoMetadataResponse DTO.
    /// </summary>
    private PhotoMetaDataDto MapPhotoToMetadata(Photo photo)
    {
        return new PhotoMetaDataDto
        {
            Id = photo.Id,
            Title = photo.Title,
            Description = photo.Description,
            MimeType = photo.MimeType,
            Width = photo.Width,
            Height = photo.Height,
            FileSize = photo.FileSize,
            CreatedAt = photo.CreatedAt,
            CapturedAt = photo.CapturedAt,
            Make = photo.Make,
            Model = photo.Model,
            ExposureTime = photo.ExposureTime,
            FNumber = photo.FNumber,
            Iso = photo.Iso,
            Latitude = photo.Latitude,
            Longitude = photo.Longitude,
            CountryCode = photo.CountryCode,
            City = photo.City,
            IsLivePhoto = photo.IsLivePhoto,
            Reactions = photo.Reactions.GroupBy(r => r.Reaction).ToDictionary(g => g.Key, g => (uint) g.Count()),
            FileName = photo.FileName
        };
    }

    /// <summary>
    ///     Checks whether the user has access to the specified photo.
    /// </summary>
    private async ValueTask<bool> HasPhotoAccessAsync(Photo photo, string? shareKey,
                                                      string? sharePassword)
    {
        bool isAuthenticated = User.Identity?.IsAuthenticated == true;
        if (isAuthenticated)
        {
            string? externalSub = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(externalSub))
            {
                var currentUser = await uow.UserRepository.GetUserByExternalSubAsync(externalSub);
                if (currentUser?.Id == photo.UserId)
                {
                    return true;
                }
            }
        }

        var photoWithAlbums = await uow.PhotoRepository.GetPhotoByIdWithAlbumsAsync(photo.Id);
        if (photoWithAlbums?.Albums.Any(a => a.IsPublic) == true)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(shareKey))
        {
            return false;
        }

        {
            OneOf<ShareKey, NotFound, IShareService.ShareUnauthorized, IShareService.Expired> resolution
                = await shareService.ResolveShareAsync(shareKey, sharePassword);

            return
                resolution.Match(validKey => validKey.PhotoId == photo.Id || (validKey.AlbumId is not null &&
                                                                              photoWithAlbums?.Albums.Any(a => a.Id ==
                                                                                       validKey.AlbumId) == true),
                                 _ => false,
                                 _ => false,
                                 _ => false);
        }

    }

    /// <summary>
    ///     Uploads a new photo for the authenticated user.
    /// </summary>
    /// <param name="request">The photo upload request containing the file and metadata.</param>
    /// <returns>
    ///     A <see cref="CreatePhotoResponse" /> containing the created photo's metadata and a presigned URL.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This endpoint requires user authentication via Keycloak OIDC. The authenticated user becomes the owner
    ///         of the uploaded photo. CSRF token must be provided in the X-XSRF-TOKEN header.
    ///     </para>
    ///     <para>
    ///         The file is processed asynchronously:
    ///         1. File is uploaded to S3-compatible storage
    ///         2. Thumbnail is generated for fast preview loading
    ///         3. EXIF metadata is extracted (if available)
    ///         4. Photo metadata is stored in the database
    ///         5. A 5-minute presigned URL is returned for immediate access
    ///     </para>
    ///     <para>
    ///         Supported image formats: JPEG, PNG, WebP, GIF, BMP, and HEIC/HEIF (Apple formats).
    ///         Maximum file size and other constraints should be validated by clients before upload.
    ///     </para>
    /// </remarks>
    /// <response code="201">Photo uploaded and created successfully.</response>
    /// <response code="400">Invalid request data (missing file or validation failed).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="413">File is too large for upload.</response>
    /// <response code="415">File type is not supported.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType<CreatePhotoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async ValueTask<ActionResult<CreatePhotoResponse>> CreatePhoto([FromForm] CreatePhotoRequest request)
    {
        if (request?.File == null || request.File.Length == 0)
        {
            return BadRequest("No file provided");
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

        // 1. Basic validation already done above
        // Read file into memory so we can both generate thumbnail/blurhash and upload
        await using var fullStream = new MemoryStream();
        await request.File.CopyToAsync(fullStream);
        byte[] originalBytes = fullStream.ToArray();

        // 2. Create a FormFile for the storage service to reuse existing SaveFileAsync logic
        var originalFormFile = new FormFile(new MemoryStream(originalBytes), 0, originalBytes.Length, request.File.Name, request.File.FileName)
        {
            Headers = request.File.Headers,
            ContentType = request.File.ContentType
        };

        // 3. Upload original and extract metadata
        var origMeta = await storageService.SaveFileAsync(originalFormFile, request.Title, request.Description);

        // 4. Generate thumbnail and blurhash using SkiaSharp so the container only needs Linux native assets
        using var sourceBitmap = SKBitmap.Decode(originalBytes);
        if (sourceBitmap is null)
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        const int ThumbMax = 400;
        float scale = Math.Min((float) ThumbMax / sourceBitmap.Width, (float) ThumbMax / sourceBitmap.Height);
        scale = Math.Min(scale, 1f);

        int thumbWidth = Math.Max(1, (int) Math.Round(sourceBitmap.Width * scale));
        int thumbHeight = Math.Max(1, (int) Math.Round(sourceBitmap.Height * scale));

        using var thumbnailBitmap = new SKBitmap(new SKImageInfo(thumbWidth, thumbHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(thumbnailBitmap);
        using var paint = new SKPaint
        {
            IsAntialias = true
        };
        using var image = SKImage.FromBitmap(thumbnailBitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawImage(image, new SKRect(0,0,thumbWidth, thumbHeight),new SKSamplingOptions(SKCubicResampler.Mitchell),paint);
        canvas.Flush();

        // Encode thumbnail to JPEG
        using var thumbnailImage = SKImage.FromBitmap(thumbnailBitmap);
        using SKData thumbData = thumbnailImage.Encode(SKEncodedImageFormat.Jpeg, 75);
        byte[] thumbBytes = thumbData.ToArray();

        // 5. Upload thumbnail bytes
        var thumbMeta = await storageService.SaveBytesAsync(thumbBytes, "thumb_" + request.File.FileName, "image/jpeg", null, null);

        // 6. Persist Photo entity in DB
        var photoEntity = new Photo
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            Title = request.Title,
            FileName = origMeta.FileName,
            Description = request.Description,
            StorageKey = origMeta.StorageKey,
            ThumbKey = thumbMeta.StorageKey,
            MimeType = origMeta.MimeType,
            Width = origMeta.Width,
            Height = origMeta.Height,
            FileSize = origMeta.FileSize,
            CreatedAt = origMeta.CreatedAt,
            CapturedAt = origMeta.CapturedAt,
            Make = origMeta.Make,
            Model = origMeta.Model,
            ExposureTime = origMeta.ExposureTime ?? 0,
            FNumber = origMeta.FNumber ?? 0,
            FocalLength = origMeta.FocalLength ?? 0,
            Iso = origMeta.Iso ?? 0,
            Latitude = origMeta.Latitude,
            Longitude = origMeta.Longitude,
            IsLivePhoto = origMeta.IsLivePhoto
        };

        await uow.PhotoRepository.AddPhotoAsync(photoEntity);
        await uow.SaveChangesAsync();

        // 7. Generate temporary URL for the created photo
        string temporaryUrl = await storageService.GetPresignedUrlAsync(photoEntity.StorageKey, TimeSpan.FromMinutes(5));

        // 8. Build DTOs
        var photoDto = new PhotoDto
        {
            Id = photoEntity.Id,
            Title = photoEntity.Title,
            FileName = photoEntity.FileName,
            Description = photoEntity.Description,
            Width = photoEntity.Width,
            Height = photoEntity.Height,
            MimeType = photoEntity.MimeType,
            CreatedAt = photoEntity.CreatedAt
        };

        var thumbnailDto = new PhotoThumbnailDto
        {
            Id = photoEntity.Id,
            Title = photoEntity.Title,
            FileName = photoEntity.FileName,
            Description = photoEntity.Description,
            Width = photoEntity.Width,
            Height = photoEntity.Height,
            MimeType = photoEntity.MimeType,
            CreatedAt = photoEntity.CreatedAt,
            FileSize = photoEntity.FileSize,
            Make = photoEntity.Make,
            Model = photoEntity.Model,
            ExposureTime = photoEntity.ExposureTime,
            FocalLength = photoEntity.FocalLength,
            FNumber = photoEntity.FNumber,
            Iso = photoEntity.Iso,
            City = photoEntity.City,
            IsLivePhoto = photoEntity.IsLivePhoto,
            ThumbnailUrl = await storageService.GetPresignedUrlAsync(photoEntity.ThumbKey, TimeSpan.FromMinutes(5))
        };

        var metadataDto = MapPhotoToMetadata(photoEntity);

        var createResponse = new CreatePhotoResponse
        {
            Photo = photoDto,
            Thumbnail = thumbnailDto,
            Metadata = metadataDto,
            TemporaryUrl = temporaryUrl
        };

        return CreatedAtAction(nameof(GetPhotoMetadata), new { id = photoEntity.Id }, createResponse);
    }
}
