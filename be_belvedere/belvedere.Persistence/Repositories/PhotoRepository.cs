using belvedere.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace belvedere.Persistence.Repositories;

/// <summary>
/// Repository interface for photo data access.
/// </summary>
/// <remarks>
/// Defines methods for retrieving photo entities from the database.
/// </remarks>
public interface IPhotoRepository
{
    /// <summary>
    /// Retrieves a photo by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the photo to retrieve.</param>
    /// <returns>The photo if found; otherwise null.</returns>
    /// <remarks>
    /// Returns the photo with all its properties populated, such as title, storage key, and ownership information.
    /// </remarks>
    public ValueTask<Photo?> GetPhotoByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a photo with all its related albums.
    /// </summary>
    /// <param name="id">The unique identifier of the photo to retrieve.</param>
    /// <returns>The photo with its Albums collection populated if found; otherwise null.</returns>
    public ValueTask<Photo?> GetPhotoByIdWithAlbumsAsync(Guid id);

    /// <summary>
    /// Adds a new photo to the underlying DbSet and returns the added entity.
    /// </summary>
    public ValueTask<Photo> AddPhotoAsync(Photo photo);
}

/// <summary>
/// Repository implementation for photo data access.
/// </summary>
/// <remarks>
/// Provides data access methods for Photo entities using Entity Framework Core.
/// </remarks>
public class PhotoRepository(DbSet<Photo> photos) : IPhotoRepository
{
    /// <summary>
    /// Retrieves a photo by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the photo to retrieve.</param>
    /// <returns>The photo if found; otherwise null.</returns>
    /// <remarks>
    /// Uses FirstOrDefaultAsync to asynchronously retrieve a single photo matching the given ID.
    /// </remarks>
    public async ValueTask<Photo?> GetPhotoByIdAsync(Guid id)
    {
        return await photos.FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Retrieves a photo with all its related albums.
    /// </summary>
    /// <param name="id">The unique identifier of the photo to retrieve.</param>
    /// <returns>The photo with its Albums collection populated if found; otherwise null.</returns>
    public async ValueTask<Photo?> GetPhotoByIdWithAlbumsAsync(Guid id)
    {
        return await photos
            .Include(p => p.Albums)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async ValueTask<Photo> AddPhotoAsync(Photo photo)
    {
        var entry = await photos.AddAsync(photo);
        return entry.Entity;
    }
}
