using belvedere.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace belvedere.Persistence.Repositories;

/// <summary>
/// Repository interface for album data access.
/// </summary>
/// <remarks>
/// Defines methods for retrieving album entities from the database.
/// </remarks>
public interface IAlbumRepository
{
    /// <summary>
    /// Retrieves an album by its unique identifier without its related photos.
    /// </summary>
    /// <param name="id">The unique identifier of the album to retrieve.</param>
    /// <returns>The album if found; otherwise null.</returns>
    /// <remarks>
    /// This method queries only the album's base properties and does not include the Photos collection.
    /// Use GetAlbumByIdWithPhotosAsync to retrieve an album with its related photos.
    /// </remarks>
    public ValueTask<Album?> GetAlbumByIdAsync(Guid id);

    /// <summary>
    /// Retrieves an album by its unique identifier including all related photos.
    /// </summary>
    /// <param name="id">The unique identifier of the album to retrieve.</param>
    /// <returns>The album with its Photos collection populated if found; otherwise null.</returns>
    /// <remarks>
    /// Uses Entity Framework Include to eagerly load related photos in a single query.
    /// Suitable for scenarios where all album photos need to be displayed or processed.
    /// </remarks>
    public ValueTask<Album?> GetAlbumByIdWithPhotosAsync(Guid id);

    /// <summary>
    /// Retrieves all albums for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A read-only list of albums owned by the user.</returns>
    public ValueTask<IReadOnlyList<Album>> GetAlbumsForUserAsync(Guid userId);

    /// <summary>
    /// Creates a new album and adds it to the repository.
    /// </summary>
    /// <param name="album">The album entity to create.</param>
    /// <returns>The created album entity.</returns>
    /// <remarks>
    /// The album will be tracked by Entity Framework. SaveChangesAsync must be called on the context to persist changes.
    /// </remarks>
    public Album CreateAlbum(Album album);

    /// <summary>
    /// Deletes an album by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the album to delete.</param>
    /// <returns>True if the album was deleted; false if the album was not found.</returns>
    public ValueTask<bool> DeleteAlbumAsync(Guid id);

    /// <summary>
    /// Adds a photo to an album's photo collection.
    /// </summary>
    /// <param name="albumId">The unique identifier of the album.</param>
    /// <param name="photoId">The unique identifier of the photo to add.</param>
    /// <returns>True if the photo was added; false if the album or photo was not found, or if the photo is already in the album.</returns>
    public ValueTask<bool> AddPhotoToAlbumAsync(Guid albumId, Guid photoId);

    /// <summary>
    /// Removes a photo from an album's photo collection.
    /// </summary>
    /// <param name="albumId">The unique identifier of the album.</param>
    /// <param name="photoId">The unique identifier of the photo to remove.</param>
    /// <returns>True if the photo was removed; false if the album or photo was not found, or if the photo is not in the album.</returns>
    public ValueTask<bool> RemovePhotoFromAlbumAsync(Guid albumId, Guid photoId);
}

/// <summary>
/// Repository implementation for album data access.
/// </summary>
/// <remarks>
/// Provides data access methods for Album entities using Entity Framework Core.
/// </remarks>
public class AlbumRepository(DbSet<Album> albums, DbSet<Photo> photos) : IAlbumRepository
{
    /// <summary>
    /// Retrieves an album by its unique identifier without its related photos.
    /// </summary>
    /// <param name="id">The unique identifier of the album to retrieve.</param>
    /// <returns>The album if found; otherwise null.</returns>
    /// <remarks>
    /// Uses FirstOrDefaultAsync to retrieve a single album matching the given ID.
    /// </remarks>
    public async ValueTask<Album?> GetAlbumByIdAsync(Guid id)
    {
        return await albums.FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <summary>
    /// Retrieves an album by its unique identifier including all related photos.
    /// </summary>
    /// <param name="id">The unique identifier of the album to retrieve.</param>
    /// <returns>The album with its Photos collection populated if found; otherwise null.</returns>
    /// <remarks>
    /// Eagerly loads the Photos collection to avoid N+1 queries when accessing photos.
    /// </remarks>
    public async ValueTask<Album?> GetAlbumByIdWithPhotosAsync(Guid id)
    {
        return await albums
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <summary>
    /// Retrieves all albums for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A read-only list of albums owned by the user.</returns>
    public async ValueTask<IReadOnlyList<Album>> GetAlbumsForUserAsync(Guid userId)
    {
        return await albums
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    /// <summary>
    /// Creates a new album and adds it to the repository.
    /// </summary>
    /// <param name="album">The album entity to create.</param>
    /// <returns>The created album entity.</returns>
    public Album CreateAlbum(Album album)
    {
        return albums.Add(album).Entity;
    }

    /// <summary>
    /// Deletes an album by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the album to delete.</param>
    /// <returns>True if the album was deleted; false if the album was not found.</returns>
    public async ValueTask<bool> DeleteAlbumAsync(Guid id)
    {
        var album = await albums.FirstOrDefaultAsync(a => a.Id == id);
        if (album is null)
            return false;

        albums.Remove(album);
        return true;
    }

    /// <summary>
    /// Adds a photo to an album's photo collection.
    /// </summary>
    /// <param name="albumId">The unique identifier of the album.</param>
    /// <param name="photoId">The unique identifier of the photo to add.</param>
    /// <returns>True if the photo was added; false if the album or photo was not found, or if the photo is already in the album.</returns>
    public async ValueTask<bool> AddPhotoToAlbumAsync(Guid albumId, Guid photoId)
    {
        var album = await albums.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == albumId);
        if (album is null)
            return false;

        // Check if photo is already in album
        if (album.Photos.Any(p => p.Id == photoId))
            return false;

        // Load the actual photo to add to the relationship
        var photo = await photos.FirstOrDefaultAsync(p => p.Id == photoId);
        if (photo is null)
            return false;

        album.Photos.Add(photo);
        return true;
    }

    /// <summary>
    /// Removes a photo from an album's photo collection.
    /// </summary>
    /// <param name="albumId">The unique identifier of the album.</param>
    /// <param name="photoId">The unique identifier of the photo to remove.</param>
    /// <returns>True if the photo was removed; false if the album or photo was not found, or if the photo is not in the album.</returns>
    public async ValueTask<bool> RemovePhotoFromAlbumAsync(Guid albumId, Guid photoId)
    {
        var album = await albums.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == albumId);
        if (album is null)
            return false;

        var photo = album.Photos.FirstOrDefault(p => p.Id == photoId);
        if (photo is null)
            return false;

        album.Photos.Remove(photo);
        return true;
    }
}
