using belvedere.Controllers.DTOs;
using belvedere.Core.Services;
using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using belvedere.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace belvedere.Controllers;

/// <summary>
///     API controller for managing albums and their photos.
/// </summary>
/// <remarks>
///     This controller is part of the Backend-for-Frontend (BFF) pattern and provides endpoints for:
///     - Authenticated users to manage albums and organize their photos
///     - Public read-only access to albums marked as public
///     - Share-key access to non-public albums via secure, password-protected share links
///     
///     Most endpoints require Keycloak OIDC authentication via session cookies.
///     Album modification endpoints (create, update, delete) require user authentication.
///     Read endpoints support both authenticated and share-key based access.
/// </remarks>
[Route("api/albums")]
[ApiController]
public sealed class AlbumController(
    IUnitOfWork uow,
    IAlbumService albumService,
    ITransactionProvider transactionProvider,
    IShareService shareService,
    IStorageService storageService,
    ILogger<AlbumController> logger) : BaseController
{
    /// <summary>
    ///     Retrieves all albums for the authenticated user.
    /// </summary>
    /// <returns>A list of albums owned by the current user.</returns>
    /// <remarks>
    ///     This endpoint requires user authentication via Keycloak OIDC. Only returns albums owned by the authenticated user.
    ///     Albums are returned with basic information including photo count and cover photo reference.
    /// </remarks>
    /// <response code="200">Returns the list of albums successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType<List<AlbumDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async ValueTask<ActionResult<List<AlbumDto>>> GetAlbums()
    {
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

        IReadOnlyList<Album> albums = await albumService.GetAlbumsForUserAsync(currentUser.Id);
        var response = albums.Select(a => new AlbumDto
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            CoverPhotoId = a.CoverPhotoId,
            IsPublic = a.IsPublic,
            CreatedAt = a.CreatedAt,
            PhotoCount = a.Photos.Count
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    ///     Retrieves a specific album with all its photos.
    /// </summary>
    /// <param name="id">The unique identifier of the album.</param>
    /// <param name="shareKey">Optional share key for access to non-public albums. Required if accessing a non-public album without authentication.</param>
    /// <param name="sharePassword">Optional password if the share key is password-protected.</param>
    /// <returns>The album with all its photos.</returns>
    /// <remarks>
    ///     <para>
    ///         Access is allowed in the following scenarios:
    ///         1. User is authenticated and owns the album
    ///         2. The album is marked as public (no authentication required)
    ///         3. A valid, non-expired share key is provided (optionally password-protected)
    ///     </para>
    ///     <para>
    ///         Returns full photo objects with blur hash for placeholder rendering.
    ///     </para>
    /// </remarks>
    /// <response code="200">Returns the album and its photos successfully.</response>
    /// <response code="401">The share key requires a password (only sent with key, no password provided).</response>
    /// <response code="404">The album does not exist or the user does not have access to it.</response>
    /// <response code="410">The share key has expired.</response>
    [HttpGet("{id:guid}/preload")]
    [AllowAnonymous]
    [ProducesResponseType<AlbumExtendedDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async ValueTask<ActionResult<AlbumExtendedDto>> PreloadAlbum(
        [FromRoute] Guid id,
        [FromQuery] string? shareKey = null,
        [FromQuery] string? sharePassword = null)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        var album = await uow.AlbumRepository.GetAlbumByIdWithPhotosAsync(id);
        if (album is null)
        {
            return NotFound();
        }

        if (!await HasAlbumAccessAsync(album, shareKey, sharePassword))
        {
            return NotFound();
        }

        var response = new AlbumExtendedDto
        {
            Id = album.Id,
            Title = album.Title,
            Description = album.Description,
            CoverPhotoId = album.CoverPhotoId,
            IsPublic = album.IsPublic,
            CreatedAt = album.CreatedAt,
            PhotoCount = album.Photos.Count,
            Photos = album.Photos.Select(p => new PhotoBlurDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                FileName = p.FileName,
                BlurHash = p.BlurHash,
                Width = p.Width,
                Height = p.Height,
                MimeType = p.MimeType,
                CreatedAt = p.CreatedAt
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    ///     Retrieves a specific album with all its photo thumbnails (including presigned URLs).
    /// </summary>
    /// <param name="id">The unique identifier of the album.</param>
    /// <param name="shareKey">Optional share key for access to non-public albums. Required if accessing a non-public album without authentication.</param>
    /// <param name="sharePassword">Optional password if the share key is password-protected.</param>
    /// <returns>The album with all its photo thumbnails including presigned URLs.</returns>
    /// <remarks>
    ///     <para>
    ///         This endpoint is typically used for loading album preview/gallery views with fast image loading.
    ///         Each photo includes a 5-minute presigned URL for viewing the thumbnail from S3 storage.
    ///     </para>
    ///     <para>
    ///         Access is allowed in the following scenarios:
    ///         1. User is authenticated and owns the album
    ///         2. The album is marked as public (no authentication required)
    ///         3. A valid, non-expired share key is provided (optionally password-protected)
    ///     </para>
    /// </remarks>
    /// <response code="200">Returns the album and its photo thumbnails successfully.</response>
    /// <response code="401">The share key requires a password (only sent with key, no password provided).</response>
    /// <response code="404">The album does not exist or the user does not have access to it.</response>
    /// <response code="410">The share key has expired.</response>
    [HttpGet("{id:guid}/thumbnails")]
    [AllowAnonymous]
    [ProducesResponseType<AlbumThumbnailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async ValueTask<ActionResult<AlbumThumbnailDto>> GetAlbumThumbnails(
        [FromRoute] Guid id,
        [FromQuery] string? shareKey = null,
        [FromQuery] string? sharePassword = null)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        var album = await uow.AlbumRepository.GetAlbumByIdWithPhotosAsync(id);
        if (album is null)
        {
            return NotFound();
        }

        if (!await HasAlbumAccessAsync(album, shareKey, sharePassword))
        {
            return NotFound();
        }

        var photoThumbnails = new List<PhotoThumbnailDto>();
        foreach (var p in album.Photos)
        {
            string thumbnailUrl = await storageService.GetPresignedUrlAsync(p.ThumbKey, TimeSpan.FromMinutes(5));
            photoThumbnails.Add(new PhotoThumbnailDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                FileName = p.FileName,
                Width = p.Width,
                Height = p.Height,
                MimeType = p.MimeType,
                CreatedAt = p.CreatedAt,
                FileSize = p.FileSize,
                Make = p.Make,
                Model = p.Model,
                ExposureTime = p.ExposureTime,
                FNumber = p.FNumber,
                Iso = p.Iso,
                City = p.City,
                IsLivePhoto = p.IsLivePhoto,
                ThumbnailUrl = thumbnailUrl
            });
        }

        var response = new AlbumThumbnailDto
        {
            Id = album.Id,
            Title = album.Title,
            Description = album.Description,
            CoverPhotoId = album.CoverPhotoId,
            IsPublic = album.IsPublic,
            CreatedAt = album.CreatedAt,
            PhotoCount = album.Photos.Count,
            Photos = photoThumbnails
        };

        return Ok(response);
    }

    /// <summary>
    ///     Creates a new album for the authenticated user.
    /// </summary>
    /// <param name="request">The album creation request.</param>
    /// <returns>The created album.</returns>
    /// <remarks>
    ///     This endpoint requires user authentication via Keycloak OIDC. CSRF token must be provided in the X-XSRF-TOKEN header.
    ///     The authenticated user becomes the owner of the newly created album.
    /// </remarks>
    /// <response code="201">Album created successfully.</response>
    /// <response code="400">Invalid request (missing title or other validation failed).</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType<AlbumDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async ValueTask<ActionResult<AlbumDto>> CreateAlbum([FromBody] CreateAlbumRequest request)
    {
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

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required");
        }

        var album = new Album
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            IsPublic = request.IsPublic
        };

        var createdAlbum = await albumService.CreateAlbumAsync(album, currentUser.Id);

        var response = new AlbumDto
        {
            Id = createdAlbum.Id,
            Title = createdAlbum.Title,
            Description = createdAlbum.Description,
            CoverPhotoId = createdAlbum.CoverPhotoId,
            IsPublic = createdAlbum.IsPublic,
            CreatedAt = createdAlbum.CreatedAt,
            PhotoCount = 0
        };

        return CreatedAtAction(nameof(PreloadAlbum), new { id = createdAlbum.Id }, response);
    }

    /// <summary>
    ///     Deletes an album (owner only).
    /// </summary>
    /// <param name="id">The unique identifier of the album to delete.</param>
    /// <returns>No content on success.</returns>
    /// <remarks>
    ///     This endpoint requires user authentication via Keycloak OIDC. CSRF token must be provided in the X-XSRF-TOKEN header.
    ///     Only the album owner can delete the album. The user is identified via the "sub" claim from the OIDC token.
    /// </remarks>
    /// <response code="204">Album deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not own this album.</response>
    /// <response code="404">Album not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeleteAlbum([FromRoute] Guid id)
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

        await transactionProvider.BeginTransactionAsync();
        try
        {
            OneOf<None, NotFound, Unauthorized> result = await albumService.DeleteAlbumAsync(id, currentUser.Id);

            if (result.IsT0)
            {
                await transactionProvider.CommitAsync();

                return NoContent();
            }

            if (result.IsT1)
            {
                return NotFound();
            }

            logger.LogWarning("User {UserId} unauthorized to delete album {AlbumId}", currentUser.Id, id);

            return Forbid();
        }
        catch
        {
            await transactionProvider.RollbackAsync();

            throw;
        }
    }

    /// <summary>
    ///     Adds a photo to an album (owner only).
    /// </summary>
    /// <param name="albumId">The unique identifier of the album.</param>
    /// <param name="photoId">The unique identifier of the photo to add.</param>
    /// <returns>No content on success.</returns>
    /// <remarks>
    ///     This endpoint requires user authentication via Keycloak OIDC. CSRF token must be provided in the X-XSRF-TOKEN header.
    ///     Only the album owner can add photos to the album. Both the album and photo must exist and be owned by the authenticated user.
    /// </remarks>
    /// <response code="204">Photo added to album successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not own this album.</response>
    /// <response code="404">Album or photo not found.</response>
    [HttpPost("{albumId:guid}/photos/{photoId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> AddPhotoToAlbum([FromRoute] Guid albumId, [FromRoute] Guid photoId)
    {
        if (albumId == Guid.Empty || photoId == Guid.Empty)
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

        await transactionProvider.BeginTransactionAsync();
        try
        {
            OneOf<None, NotFound, Unauthorized> result
                = await albumService.AddPhotoToAlbumAsync(albumId, photoId, currentUser.Id);

            if (result.IsT0)
            {
                await transactionProvider.CommitAsync();

                return NoContent();
            }

            if (result.IsT1)
            {
                return NotFound();
            }

            logger.LogWarning("User {UserId} unauthorized to add photo to album {AlbumId}", currentUser.Id, albumId);

            return Forbid();
        }
        catch
        {
            await transactionProvider.RollbackAsync();

            throw;
        }
    }

    /// <summary>
    ///     Removes a photo from an album (owner only).
    /// </summary>
    /// <param name="albumId">The unique identifier of the album.</param>
    /// <param name="photoId">The unique identifier of the photo to remove.</param>
    /// <returns>No content on success.</returns>
    /// <remarks>
    ///     This endpoint requires user authentication via Keycloak OIDC. CSRF token must be provided in the X-XSRF-TOKEN header.
    ///     Only the album owner can remove photos from the album. Both the album and photo must exist and the album must be owned by the authenticated user.
    /// </remarks>
    /// <response code="204">Photo removed from album successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not own this album.</response>
    /// <response code="404">Album or photo not found.</response>
    [HttpDelete("{albumId:guid}/photos/{photoId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> RemovePhotoFromAlbum([FromRoute] Guid albumId, [FromRoute] Guid photoId)
    {
        if (albumId == Guid.Empty || photoId == Guid.Empty)
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

        await transactionProvider.BeginTransactionAsync();
        try
        {
            OneOf<None, NotFound, Unauthorized> result
                = await albumService.RemovePhotoFromAlbumAsync(albumId, photoId, currentUser.Id);

            if (result.IsT0)
            {
                await transactionProvider.CommitAsync();

                return NoContent();
            }

            if (result.IsT1)
            {
                return NotFound();
            }

            logger.LogWarning("User {UserId} unauthorized to remove photo from album {AlbumId}", currentUser.Id,
                              albumId);

            return Forbid();
        }
        catch
        {
            await transactionProvider.RollbackAsync();

            throw;
        }
    }

    /// <summary>
    ///     Gets the current authenticated user from the claims.
    /// </summary>
    private async ValueTask<User?> GetCurrentUserAsync()
    {
        string? externalSub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(externalSub))
        {
            return null;
        }

        return await uow.UserRepository.GetUserByExternalSubAsync(externalSub);
    }

    /// <summary>
    ///     Checks whether the user has access to the specified album.
    /// </summary>
    private async ValueTask<bool> HasAlbumAccessAsync(Album album, string? shareKey, string? sharePassword)
    {
        bool isAuthenticated = User.Identity?.IsAuthenticated == true;
        var currentUser = isAuthenticated ? await GetCurrentUserAsync() : null;
        if (currentUser?.Id == album.UserId || album.IsPublic)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(shareKey))
        {
            OneOf<ShareKey, NotFound, IShareService.ShareUnauthorized, IShareService.Expired> resolution
                = await shareService.ResolveShareAsync(shareKey, sharePassword);

            return resolution.Match(validKey => validKey.AlbumId == album.Id,
                                    _ => false,
                                    _ => false,
                                    _ => false);
        }

        return false;
    }
}
