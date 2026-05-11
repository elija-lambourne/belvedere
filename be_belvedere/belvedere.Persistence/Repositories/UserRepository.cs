using belvedere.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace belvedere.Persistence.Repositories;

/// <summary>
/// Repository interface for user data access.
/// </summary>
/// <remarks>
/// Defines methods for retrieving user entities from the database.
/// </remarks>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the user to retrieve.</param>
    /// <returns>The user if found; otherwise null.</returns>
    /// <remarks>
    /// Returns the user with all their properties populated, such as external subject identifier and creation information.
    /// </remarks>
    public ValueTask<User?> GetUserByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a user by their external subject identifier from the authentication provider.
    /// </summary>
    /// <param name="externalSub">The external subject identifier from the OAuth/OpenID provider (typically from the "sub" claim).</param>
    /// <returns>The user if found; otherwise null.</returns>
    /// <remarks>
    /// This method is used during authentication to look up users by their provider-assigned identifier.
    /// The externalSub value typically comes from the "sub" (subject) claim in JWT tokens.
    /// This is the primary method for authenticating users and linking them to their internal ID.
    /// </remarks>
    public ValueTask<User?> GetUserByExternalSubAsync(string externalSub);
}

/// <summary>
/// Repository implementation for user data access.
/// </summary>
/// <remarks>
/// Provides data access methods for User entities using Entity Framework Core.
/// </remarks>
public class UserRepository(DbSet<User> users) : IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to retrieve.</param>
    /// <returns>The user if found; otherwise null.</returns>
    /// <remarks>
    /// Uses FirstOrDefaultAsync to asynchronously retrieve a single user matching the given ID.
    /// </remarks>
    public async ValueTask<User?> GetUserByIdAsync(Guid id)
    {
        return await users.FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>
    /// Retrieves a user by their external subject identifier from the authentication provider.
    /// </summary>
    /// <param name="externalSub">The external subject identifier to search for.</param>
    /// <returns>The user if found; otherwise null.</returns>
    /// <remarks>
    /// Uses FirstOrDefaultAsync to asynchronously retrieve a single user matching the given external subject ID.
    /// This is typically used during authentication to find users by their OAuth/OpenID provider subject.
    /// </remarks>
    public async ValueTask<User?> GetUserByExternalSubAsync(string externalSub)
    {
        return await users.FirstOrDefaultAsync(u => u.ExternalSub == externalSub);
    }
}
