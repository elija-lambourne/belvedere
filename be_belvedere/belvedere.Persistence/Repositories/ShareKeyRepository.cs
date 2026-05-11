using belvedere.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace belvedere.Persistence.Repositories;

/// <summary>
/// Repository interface for share key data access.
/// </summary>
/// <remarks>
/// Defines methods for retrieving and persisting share key entities in the database.
/// Share keys provide temporary secure access to photos and albums.
/// </remarks>
public interface IShareKeyRepository
{
    /// <summary>
    /// Retrieves a share key by its unique key string.
    /// </summary>
    /// <param name="key">The unique share key string (URL-safe base64 encoded token).</param>
    /// <returns>The share key entity if found; otherwise null.</returns>
    /// <remarks>
    /// This method looks up a share key by its unique identifying string.
    /// The returned share key contains information about the shared resource (album or photo),
    /// password hash (if protected), expiration time, and creation timestamp.
    /// </remarks>
    public ValueTask<ShareKey?> GetShareKeyByKeyAsync(string key);

    /// <summary>
    /// Adds a new share key to the repository.
    /// </summary>
    /// <param name="shareKey">The share key entity to add.</param>
    /// <returns>The added share key entity with any database-generated values populated.</returns>
    /// <remarks>
    /// Registers the share key with the Entity Framework context.
    /// Changes must be persisted by calling SaveChangesAsync on the unit of work.
    /// </remarks>
    public ShareKey AddShareKey(ShareKey shareKey);
}

/// <summary>
/// Repository implementation for share key data access.
/// </summary>
/// <remarks>
/// Provides data access methods for ShareKey entities using Entity Framework Core.
/// </remarks>
public class ShareKeyRepository(DbSet<ShareKey> shareKeys) : IShareKeyRepository
{
    /// <summary>
    /// Retrieves a share key by its unique key string.
    /// </summary>
    /// <param name="key">The unique share key string to look up.</param>
    /// <returns>The share key entity if found; otherwise null.</returns>
    /// <remarks>
    /// Uses FirstOrDefaultAsync to asynchronously retrieve a single share key matching the key string.
    /// </remarks>
    public async ValueTask<ShareKey?> GetShareKeyByKeyAsync(string key)
    {
        return await shareKeys.FirstOrDefaultAsync(s => s.Key == key);
    }

    /// <summary>
    /// Adds a new share key to the repository.
    /// </summary>
    /// <param name="shareKey">The share key entity to add.</param>
    /// <returns>The added share key entity.</returns>
    /// <remarks>
    /// Registers the share key with Entity Framework's change tracking.
    /// The entity will be inserted into the database when SaveChangesAsync is called.
    /// </remarks>
    public ShareKey AddShareKey(ShareKey shareKey)
    {
        return shareKeys.Add(shareKey).Entity;
    }
}
