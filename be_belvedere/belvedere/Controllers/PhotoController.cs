using belvedere.Controllers.DTOs;
using belvedere.Core.Services;
using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using belvedere.Util;
using Microsoft.AspNetCore.Mvc;

namespace belvedere.Controllers;

/// <summary>
/// API controller for managing photo access and retrieving presigned URLs for authenticated users.
/// </summary>
/// <remarks>
/// Provides endpoints for users to obtain temporary signed URLs for accessing their photos stored in S3 storage.
/// All endpoints require authentication via the "sub" claim in the user's identity.
/// </remarks>
[Route("api/photos")]
public sealed class PhotoController(
    IUnitOfWork uow,
    IPhotoService photoService,
    IStorageService storageService,
    ILogger<PhotoController> logger) : BaseController
{
    /// <summary>
    /// Retrieves a temporary presigned URL for accessing a specific photo.
    /// </summary>
    /// <param name="id">The GUID identifier of the photo to retrieve a presigned URL for.</param>
    /// <returns>
    /// A <see cref="PhotoSignedUrlResponse"/> containing a temporary presigned URL that expires in 5 minutes.
    /// </returns>
    /// <remarks>
    /// This endpoint requires user authentication. The user can only retrieve signed URLs for photos they own.
    /// The returned URL is valid for 5 minutes.
    /// </remarks>
    /// <response code="200">Returns the presigned URL successfully.</response>
    /// <response code="400">The photo ID is empty (Guid.Empty).</response>
    /// <response code="401">User is not authenticated or external sub claim is missing.</response>
    /// <response code="404">The photo does not exist or the user does not have access to it.</response>
    [HttpGet]
    [Route("{id:guid}/signed-url")]
    [ProducesResponseType<PhotoSignedUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<PhotoSignedUrlResponse>> GetSignedUrl([FromRoute] Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
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

        var photoResult = await photoService.GetPhotoByIdAsync(id);

        return await photoResult.Match<ValueTask<ActionResult<PhotoSignedUrlResponse>>>(async photo =>
        {
            if (photo.UserId != currentUser.Id)
            {
                logger.LogInformation("User {UserId} tried to access photo {PhotoId} without access", currentUser.Id,
                                      id);

                return NotFound();
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
    /// <returns>
    /// A <see cref="PhotoExtendedDto"/> containing all metadata of the photo.
    /// </returns>
    /// <remarks>
    /// This endpoint returns comprehensive metadata about a photo including EXIF data, location, dimensions, etc.
    /// The user must either own the photo or the photo must be in a public album or accessed via a valid share key.
    /// </remarks>
    /// <response code="200">Returns the photo metadata successfully.</response>
    /// <response code="400">The photo ID is empty (Guid.Empty).</response>
    /// <response code="404">The photo does not exist or the user does not have access to it.</response>
    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType<PhotoMetaDataDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<PhotoMetaDataDto>> GetPhotoMetadata([FromRoute] Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        var photoResult = await photoService.GetPhotoByIdAsync(id);

        return await photoResult.Match<ValueTask<ActionResult<PhotoMetaDataDto>>>(async photo =>
        {
            // Check if user has access to this photo
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            if (isAuthenticated)
            {
                string? externalSub = User.FindFirst("sub")?.Value;
                if (!string.IsNullOrWhiteSpace(externalSub))
                {
                    var currentUser = await uow.UserRepository.GetUserByExternalSubAsync(externalSub);
                    if (currentUser?.Id == photo.UserId)
                    {
                        // Owner can always access their own photos
                        return Ok(this.MapPhotoToMetadata(photo));
                    }
                }
            }

            // Check if photo is in any public album
            var photoWithAlbums = await uow.PhotoRepository.GetPhotoByIdWithAlbumsAsync(id);
            if (photoWithAlbums?.Albums.Any(a => a.IsPublic) == true)
            {
                return Ok(this.MapPhotoToMetadata(photo));
            }

            // TODO: In the future, check share keys here for additional access

            logger.LogInformation("User tried to access photo {PhotoId} without proper access", id);
            return NotFound();
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
            Reactions = photo.Reactions.GroupBy(r => r.Reaction).ToDictionary(g => g.Key, g => (uint)g.Count()),
            FileName = photo.FileName
        };
    }
}
