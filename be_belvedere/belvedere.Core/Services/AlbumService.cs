using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using OneOf;
using OneOf.Types;

namespace belvedere.Core.Services;

/// <summary>
/// Represents an unauthorized access result.
/// </summary>
public sealed class Unauthorized;


/// <summary>
/// Service interface for album-related operations.
/// </summary>
/// <remarks>
/// Provides methods for retrieving album information from the persistence layer.
/// </remarks>
public interface IAlbumService
{
    /// <summary>
    /// Retrieves an album by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the album to retrieve.</param>
    /// <returns>
    /// A <see cref="OneOf{Album, NotFound}"/> containing either the album if found,
    /// or a NotFound indicator if the album does not exist.
    /// </returns>
    public ValueTask<OneOf<Album, NotFound>> GetAlbumByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all albums for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A read-only list of albums owned by the user.</returns>
    public ValueTask<IReadOnlyList<Album>> GetAlbumsForUserAsync(Guid userId);

    /// <summary>
    /// Creates a new album for the specified user.
    /// </summary>
    /// <param name="album">The album to create (UserId will be overwritten).</param>
    /// <param name="userId">The user ID of the album owner.</param>
    /// <returns>The created album.</returns>
    public ValueTask<Album> CreateAlbumAsync(Album album, Guid userId);

    /// <summary>
    /// Deletes an album by its unique identifier, ensuring the current user owns it.
    /// </summary>
    /// <param name="id">The unique identifier of the album to delete.</param>
    /// <param name="currentUserId">The ID of the user requesting the deletion.</param>
    /// <returns>
    /// A <see cref="OneOf"/> containing None if successful, NotFound if album doesn't exist, or Unauthorized if user doesn't own it.
    /// </returns>
    public ValueTask<OneOf<None, NotFound, Unauthorized>> DeleteAlbumAsync(Guid id, Guid currentUserId);

    /// <summary>
    /// Adds a photo to an album, ensuring the current user owns the album.
    /// </summary>
    /// <param name="albumId">The unique identifier of the album.</param>
    /// <param name="photoId">The unique identifier of the photo to add.</param>
    /// <param name="currentUserId">The ID of the user requesting the operation.</param>
    /// <returns>
    /// A <see cref="OneOf"/> containing None if successful, NotFound if album or photo doesn't exist, or Unauthorized if user doesn't own the album.
    /// </returns>
    public ValueTask<OneOf<None, NotFound, Unauthorized>> AddPhotoToAlbumAsync(Guid albumId, Guid photoId, Guid currentUserId);

    /// <summary>
    /// Removes a photo from an album, ensuring the current user owns the album.
    /// </summary>
    /// <param name="albumId">The unique identifier of the album.</param>
    /// <param name="photoId">The unique identifier of the photo to remove.</param>
    /// <param name="currentUserId">The ID of the user requesting the operation.</param>
    /// <returns>
    /// A <see cref="OneOf"/> containing None if successful, NotFound if album or photo doesn't exist, or Unauthorized if user doesn't own the album.
    /// </returns>
    public ValueTask<OneOf<None, NotFound, Unauthorized>> RemovePhotoFromAlbumAsync(Guid albumId, Guid photoId, Guid currentUserId);
}

/// <summary>
/// Service implementation for album operations.
/// </summary>
/// <remarks>
/// Handles business logic for album retrieval, including logging and error handling.
/// </remarks>
public class AlbumService(IUnitOfWork uow, ILogger<AlbumService> logger) : IAlbumService
{
    /// <summary>
    /// Retrieves an album by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the album to retrieve.</param>
    /// <returns>
    /// A <see cref="OneOf{Album, NotFound}"/> containing either the album if found,
    /// or a NotFound indicator if the album does not exist.
    /// </returns>
    /// <remarks>
    /// Logs information about the success or failure of the retrieval operation.
    /// This method queries the repository and returns the result wrapped in a discriminated union type.
    /// </remarks>
    public async ValueTask<OneOf<Album, NotFound>> GetAlbumByIdAsync(Guid id)
    {
        var album = await uow.AlbumRepository.GetAlbumByIdAsync(id);

        if (album is null)
        {
            logger.LogInformation("Album with id {id} was not found", id);
            return new NotFound();
        }
        else
        {
            logger.LogInformation("Album with id {id} was found", id);
            return album;
        }
    }

    /// <summary>
    /// Retrieves all albums for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A read-only list of albums owned by the user.</returns>
    public async ValueTask<IReadOnlyList<Album>> GetAlbumsForUserAsync(Guid userId)
    {
        logger.LogInformation("Fetching albums for user {userId}", userId);
        return await uow.AlbumRepository.GetAlbumsForUserAsync(userId);
    }

    /// <summary>
    /// Creates a new album for the specified user.
    /// </summary>
    /// <param name="album">The album to create (UserId will be overwritten).</param>
    /// <param name="userId">The user ID of the album owner.</param>
    /// <returns>The created album.</returns>
    public async ValueTask<Album> CreateAlbumAsync(Album album, Guid userId)
    {
        album.UserId = userId;
        album.CreatedAt = DateTime.UtcNow;
        var createdAlbum = uow.AlbumRepository.CreateAlbum(album);
        await uow.SaveChangesAsync();
        logger.LogInformation("Album {albumId} created for user {userId}", album.Id, userId);
        return createdAlbum;
    }

    /// <summary>
    /// Deletes an album by its unique identifier, ensuring the current user owns it.
    /// </summary>
    /// <param name="id">The unique identifier of the album to delete.</param>
    /// <param name="currentUserId">The ID of the user requesting the deletion.</param>
    /// <returns>
    /// A <see cref="OneOf"/> containing None if successful, NotFound if album doesn't exist, or Unauthorized if user doesn't own it.
    /// </returns>
    public async ValueTask<OneOf<None, NotFound, Unauthorized>> DeleteAlbumAsync(Guid id, Guid currentUserId)
    {
        var album = await uow.AlbumRepository.GetAlbumByIdAsync(id);
        if (album is null)
        {
            logger.LogInformation("Album {albumId} not found for deletion", id);
            return new NotFound();
        }

        if (album.UserId != currentUserId)
        {
            logger.LogWarning("User {userId} attempted to delete album {albumId} they don't own", currentUserId, id);
            return new Unauthorized();
        }

        await uow.AlbumRepository.DeleteAlbumAsync(id);
        await uow.SaveChangesAsync();
        logger.LogInformation("Album {albumId} deleted by user {userId}", id, currentUserId);
        return new None();
    }

    /// <summary>
    /// Adds a photo to an album, ensuring the current user owns the album.
    /// </summary>
    /// <param name="albumId">The unique identifier of the album.</param>
    /// <param name="photoId">The unique identifier of the photo to add.</param>
    /// <param name="currentUserId">The ID of the user requesting the operation.</param>
    /// <returns>
    /// A <see cref="OneOf"/> containing None if successful, NotFound if album or photo doesn't exist, or Unauthorized if user doesn't own the album.
    /// </returns>
    public async ValueTask<OneOf<None, NotFound, Unauthorized>> AddPhotoToAlbumAsync(Guid albumId, Guid photoId, Guid currentUserId)
    {
        var album = await uow.AlbumRepository.GetAlbumByIdAsync(albumId);
        if (album is null)
        {
            logger.LogInformation("Album {albumId} not found when adding photo", albumId);
            return new NotFound();
        }

        if (album.UserId != currentUserId)
        {
            logger.LogWarning("User {userId} attempted to add photo to album {albumId} they don't own", currentUserId, albumId);
            return new Unauthorized();
        }

        var photo = await uow.PhotoRepository.GetPhotoByIdAsync(photoId);
        if (photo is null)
        {
            logger.LogInformation("Photo {photoId} not found when adding to album", photoId);
            return new NotFound();
        }

        var added = await uow.AlbumRepository.AddPhotoToAlbumAsync(albumId, photoId);
        if (!added)
        {
            logger.LogInformation("Photo {photoId} could not be added to album {albumId} (possibly already in album)", photoId, albumId);
            return new NotFound();
        }

        await uow.SaveChangesAsync();
        logger.LogInformation("Photo {photoId} added to album {albumId} by user {userId}", photoId, albumId, currentUserId);
        return new None();
    }

    /// <summary>
    /// Removes a photo from an album, ensuring the current user owns the album.
    /// </summary>
    /// <param name="albumId">The unique identifier of the album.</param>
    /// <param name="photoId">The unique identifier of the photo to remove.</param>
    /// <param name="currentUserId">The ID of the user requesting the operation.</param>
    /// <returns>
    /// A <see cref="OneOf"/> containing None if successful, NotFound if album or photo doesn't exist, or Unauthorized if user doesn't own the album.
    /// </returns>
    public async ValueTask<OneOf<None, NotFound, Unauthorized>> RemovePhotoFromAlbumAsync(Guid albumId, Guid photoId, Guid currentUserId)
    {
        var album = await uow.AlbumRepository.GetAlbumByIdAsync(albumId);
        if (album is null)
        {
            logger.LogInformation("Album {albumId} not found when removing photo", albumId);
            return new NotFound();
        }

        if (album.UserId != currentUserId)
        {
            logger.LogWarning("User {userId} attempted to remove photo from album {albumId} they don't own", currentUserId, albumId);
            return new Unauthorized();
        }

        var removed = await uow.AlbumRepository.RemovePhotoFromAlbumAsync(albumId, photoId);
        if (!removed)
        {
            logger.LogInformation("Photo {photoId} not found in album {albumId} for removal", photoId, albumId);
            return new NotFound();
        }

        await uow.SaveChangesAsync();
        logger.LogInformation("Photo {photoId} removed from album {albumId} by user {userId}", photoId, albumId, currentUserId);
        return new None();
    }
}
