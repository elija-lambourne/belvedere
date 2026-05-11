using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using OneOf.Types;
using OneOf;

namespace belvedere.Core.Services;

/// <summary>
/// Service interface for user-related operations.
/// </summary>
/// <remarks>
/// Provides methods for retrieving user information from the persistence layer.
/// </remarks>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to retrieve.</param>
    /// <returns>
    /// A <see cref="OneOf{User, NotFound}"/> containing either the user if found,
    /// or a NotFound indicator if the user does not exist.
    /// </returns>
    public ValueTask<OneOf<User, NotFound>> GetUserByIdAsync(Guid id);
}

/// <summary>
/// Service implementation for user operations.
/// </summary>
/// <remarks>
/// Handles business logic for user retrieval, including logging and error handling.
/// </remarks>
public class UserService(IUnitOfWork uow, ILogger<UserService> logger) : IUserService
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to retrieve.</param>
    /// <returns>
    /// A <see cref="OneOf{User, NotFound}"/> containing either the user if found,
    /// or a NotFound indicator if the user does not exist.
    /// </returns>
    /// <remarks>
    /// Logs information about the success or failure of the retrieval operation.
    /// This method queries the repository and returns the result wrapped in a discriminated union type.
    /// </remarks>
    public async ValueTask<OneOf<User, NotFound>> GetUserByIdAsync(Guid id)
    {
        var user = await uow.UserRepository.GetUserByIdAsync(id);

        if (user is null)
        {
            logger.LogInformation("User with id {id} was not found", id);
            return new NotFound();
        }
        else
        {
            logger.LogInformation("User with id {id} was found", id);
            return user;
        }
    }
}
