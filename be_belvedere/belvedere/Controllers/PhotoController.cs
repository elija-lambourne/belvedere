using belvedere.Controllers.DTOs;
using belvedere.Core.Services;
using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using belvedere.Util;
using Microsoft.AspNetCore.Mvc;

namespace belvedere.Controllers;

/// <summary>
/// API controller for managing photo access and retrieving presigned URLs.
/// </summary>
[Route("api/photos")]
public sealed class PhotoController(
    IUnitOfWork uow,
    IPhotoService photoService,
    IStorageService storageService,
    IShareService shareService,
    ILogger<PhotoController> logger) : BaseController
{
    /// <summary>
    /// Retrieves a temporary presigned URL for accessing a specific photo.
    /// </summary>
    /// <param name="id">The GUID identifier of the photo to retrieve a presigned URL for.</param>
    /// <param name="shareKey">Optional share key for access to non-public photos.</param>
    /// <param name="sharePassword">Optional password if the share key is password-protected.</param>
    /// <returns>
    /// A <see cref="PhotoSignedUrlResponse"/> containing a temporary presigned URL that expires in 5 minutes.
    /// </returns>
    /// <remarks>
    /// The user must either own the photo, the photo must be in a public album, or accessed via a valid share key.
    /// The returned URL is valid for 5 minutes.
    /// </remarks>
    /// <response code="200">Returns the presigned URL successfully.</response>
    /// <response code="400">The photo ID is empty (Guid.Empty).</response>
    /// <response code="404">The photo does not exist or the user does not have access to it.</response>
    /// <response code="401">The user is not authenticated.</response>
    [HttpGet]
    [Route("{id:guid}/signed-url")]
    [ProducesResponseType<PhotoSignedUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async ValueTask<ActionResult<PhotoSignedUrlResponse>> GetSignedUrl(
        [FromRoute] Guid id,
        [FromQuery] string? shareKey = null,
        [FromQuery] string? sharePassword = null)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        var photoResult = await photoService.GetPhotoByIdAsync(id);

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
    /// Retrieves the expanded metadata of a specific photo.
    /// </summary>
    /// <param name="id">The GUID identifier of the photo to retrieve metadata for.</param>
    /// <param name="shareKey">Optional share key for access to non-public photos.</param>
    /// <param name="sharePassword">Optional password if the share key is password-protected.</param>
    /// <returns>
    /// A <see cref="PhotoMetaDataDto"/> containing all metadata of the photo.
    /// </returns>
    /// <remarks>
    /// This endpoint returns comprehensive metadata about a photo including EXIF data, location, dimensions, etc.
    /// The user must either own the photo or the photo must be in a public album or accessed via a valid share key.
    /// </remarks>
    /// <response code="200">Returns the photo metadata successfully.</response>
    /// <response code="400">The photo ID is empty (Guid.Empty).</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="404">The photo does not exist or the user does not have access to it.</response>
    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType<PhotoMetaDataDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<PhotoMetaDataDto>> GetPhotoMetadata(
        [FromRoute] Guid id,
        [FromQuery] string? shareKey = null,
        [FromQuery] string? sharePassword = null)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        var photoResult = await photoService.GetPhotoByIdAsync(id);

        return await photoResult.Match<ValueTask<ActionResult<PhotoMetaDataDto>>>(async photo =>
        {
            if (!await HasPhotoAccessAsync(photo, shareKey, sharePassword))
            {
                logger.LogInformation("User tried to access photo {PhotoId} without proper access", id);

                return Unauthorized();
            }

            return Ok(this.MapPhotoToMetadata(photo));
        }, _ => ValueTask.FromResult<ActionResult<PhotoMetaDataDto>>(NotFound()));
    }

    /// <summary>
    /// Maps a Photo model to a PhotoMetadataResponse DTO.
    /// </summary>
    private PhotoMetaDataDto MapPhotoToMetadata(belvedere.Persistence.Model.Photo photo)
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
    /// Checks whether the user has access to the specified photo.
    /// </summary>
    private async ValueTask<bool> HasPhotoAccessAsync(belvedere.Persistence.Model.Photo photo, string? shareKey,
                                                      string? sharePassword)
    {
        var isAuthenticated = User.Identity?.IsAuthenticated == true;
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

        if (!string.IsNullOrWhiteSpace(shareKey))
        {
            var resolution = await shareService.ResolveShareAsync(shareKey, sharePassword);

            return
                resolution.Match(validKey => validKey.PhotoId == photo.Id || (validKey.AlbumId is not null && photoWithAlbums?.Albums.Any(a => a.Id == validKey.AlbumId) == true),
                                 _ => false,
                                 _ => false,
                                 _ => false);
        }

        return false;
    }
}
