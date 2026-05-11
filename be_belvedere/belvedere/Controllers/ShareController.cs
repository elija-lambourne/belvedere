using belvedere.Controllers.DTOs;
using belvedere.Core.Services;
using belvedere.Core.Util;
using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using belvedere.Util;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;

namespace belvedere.Controllers;

/// <summary>
///     API controller for managing share links for photos and albums.
/// </summary>
/// <remarks>
///     This controller is part of the Backend-for-Frontend (BFF) pattern and provides endpoints for:
///     - Authenticated users to create and manage share links for their photos and albums
///     - Public retrieval of share link information via share key (with optional password protection)
///     
///     Share creation requires user authentication via Keycloak OIDC. Share retrieval ("resolve") is public
///     and supports optional share-key password protection and expiration time checking.
/// </remarks>
[Route("api/shares")]
[ApiController]
public sealed class ShareController(
    ITransactionProvider transaction,
    IUnitOfWork uow,
    IPhotoService photoService,
    IAlbumService albumService,
    IShareService shareService,
    IOptions<Settings> settings,
    ILogger<ShareController> logger) : BaseController
{
    /// <summary>
    ///     Creates a new share link for a photo or album with optional password protection and expiration.
    /// </summary>
    /// <param name="request">The create share request containing target type, ID, password, and expiration details.</param>
    /// <returns>
    ///     A <see cref="CreateShareResponse" /> containing the generated share key, shareable URL, and expiration time.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This endpoint requires user authentication via Keycloak OIDC. CSRF token must be provided in the X-XSRF-TOKEN header.
    ///         The authenticated user must be the owner of the photo or album being shared.
    ///     </para>
    ///     <para>
    ///         The share key is generated using cryptographically secure randomness and is guaranteed to be unique.
    ///         If a password is provided, it will be hashed using PBKDF2-SHA256 before storage.
    ///         
    ///         Changes are committed within a database transaction; if any error occurs, the transaction is rolled back.
    ///     </para>
    ///     <para>
    ///         Share links support both public (no password) and password-protected access patterns, and can optionally
    ///         expire at a specified time. Once expired, the share returns HTTP 410 Gone.
    ///     </para>
    /// </remarks>
    /// <response code="201">Share link created successfully.</response>
    /// <response code="400">Invalid request data (validation failed or invalid target type).</response>
    /// <response code="401">User is not authenticated or external sub claim is missing.</response>
    /// <response code="404">The photo/album does not exist or the user does not own it.</response>
    [HttpPost]
    [Route("")]
    [Authorize]
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
            var targetType = request.TargetType;
            switch (targetType)
            {
                case ResourceType.Photo:
                {
                    OneOf<Photo, NotFound> photoResult = await photoService.GetPhotoByIdAsync(request.TargetId);
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

                    break;
                }
                case ResourceType.Album:
                {
                    OneOf<Album, NotFound> albumResult = await albumService.GetAlbumByIdAsync(request.TargetId);
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

                    break;
                }
                default:
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
    ///     Resolves a share key to its target resource identifier and type.
    /// </summary>
    /// <param name="key">The unique share key identifying the shared resource.</param>
    /// <param name="password">Optional password for password-protected shares.</param>
    /// <returns>
    ///     A <see cref="ShareResolutionResponse" /> containing the target type and target ID.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This endpoint is public and does not require authentication. It allows anyone to resolve a share key
    ///         to determine what resource (photo or album) it points to.
    ///     </para>
    ///     <para>
    ///         If the share is password-protected, the correct password must be provided in the query string.
    ///         If the share has an expiration date and that date has passed, an HTTP 410 Gone status is returned.
    ///     </para>
    /// </remarks>
    /// <response code="200">The shared resource was resolved successfully.</response>
    /// <response code="401">The share is password-protected and the password is incorrect or missing.</response>
    /// <response code="404">The share key does not exist.</response>
    /// <response code="410">The share link has expired.</response>
    [HttpGet]
    [Route("{key}")]
    [AllowAnonymous]
    [ProducesResponseType<ShareResolutionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async ValueTask<ActionResult<ShareResolutionResponse>> GetShareByKey(
        [FromRoute] string key, [FromQuery] string? password = null)
    {
        OneOf<ShareKey, NotFound, IShareService.ShareUnauthorized, IShareService.Expired> resolution
            = await shareService.ResolveShareAsync(key, password);

        return resolution.Match<ActionResult<ShareResolutionResponse>>(shareKey =>
         {
             if (shareKey.PhotoId is not null)
             {
                 return Ok(new ShareResolutionResponse
                 {
                     TargetType = ResourceType.Photo,
                     TargetId = shareKey.PhotoId.Value
                 });
             }

             if (shareKey.AlbumId is not null)
             {
                 return Ok(new ShareResolutionResponse
                 {
                     TargetType = ResourceType.Album,
                     TargetId = shareKey.AlbumId.Value
                 });
             }

             return NotFound();
         },
         notFound => NotFound(),
         unauthorized => Unauthorized(),
         expired => StatusCode(StatusCodes.Status410Gone));
    }

    /// <summary>
    ///     Builds the complete shareable URL for a given share key.
    /// </summary>
    /// <param name="key">The share key to include in the URL.</param>
    /// <returns>A complete URL to the shared resource that can be sent to other users.</returns>
    /// <remarks>
    ///     Constructs a client-side URL by appending the share key to the configured client origin.
    ///     The client origin is trimmed to remove trailing slashes before concatenation.
    /// </remarks>
    private string BuildShareUrl(string key)
    {
        string origin = settings.Value.ClientOrigin.TrimEnd('/');

        return $"{origin}/share/{key}";
    }
}

/// <summary>
///     Request model for creating a new share link for a photo or album.
/// </summary>
/// <remarks>
///     Used by the CreateShare endpoint to specify what resource to share, with optional password protection
///     and expiration constraints.
/// </remarks>
public sealed record CreateShareRequest(ResourceType TargetType, Guid TargetId, string? Password, DateTime? ExpiresAt)
{
    /// <summary>
    ///     Validator for the CreateShareRequest model using FluentValidation.
    /// </summary>
    /// <remarks>
    ///     Validates that:
    ///     - TargetType is not empty and is either "photo" or "album"
    ///     - TargetId is not empty
    ///     - Password maximum length does not exceed 256 characters
    ///     - ExpiresAt (if provided) is a future date/time
    /// </remarks>
    public sealed class Validator : AbstractValidator<CreateShareRequest>
    {
        public Validator()
        {
            RuleFor(x => x.TargetType)
                .NotEmpty()
                .Must(value => value == ResourceType.Photo ||
                               value == ResourceType.Album)
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
///     Response model sent when a share link is created successfully.
/// </summary>
/// <remarks>
///     Contains the generated share key, the full shareable URL, and optional expiration information.
/// </remarks>
public sealed class CreateShareResponse(string shareKey, string shareUrl, DateTime? expiresAt)
{
    /// <summary>
    ///     Gets the unique share key generated for this share.
    /// </summary>
    /// <remarks>
    ///     This key is used to identify and access the shared resource.
    /// </remarks>
    public string ShareKey { get; } = shareKey;

    /// <summary>
    ///     Gets the complete URL that can be shared with others to access the shared resource.
    /// </summary>
    public string ShareUrl { get; } = shareUrl;

    /// <summary>
    ///     Gets the optional expiration date and time for this share link.
    /// </summary>
    /// <remarks>
    ///     If null, the share link does not expire.
    ///     After this time, the share link becomes inaccessible and returns HTTP 410 Gone.
    /// </remarks>
    public DateTime? ExpiresAt { get; } = expiresAt;
}
