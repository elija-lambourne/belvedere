using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using OneOf.Types;
using OneOf;

namespace belvedere.Core.Services;

/// <summary>
/// Service interface for photo-related operations.
/// </summary>
/// <remarks>
/// Provides methods for retrieving and accessing photo information from the persistence layer.
/// </remarks>
public interface IPhotoService
{
    /// <summary>
    /// Retrieves a photo by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the photo to retrieve.</param>
    /// <returns>
    /// A <see cref="OneOf{Photo, NotFound}"/> containing either the photo if found,
    /// or a NotFound indicator if the photo does not exist.
    /// </returns>
    public ValueTask<OneOf<Photo, NotFound>> GetPhotoByIdAsync(Guid id);

    /// <summary>
    /// Retrieves the expanded metadata of a photo, including all its properties.
    /// </summary>
    /// <param name="id">The unique identifier of the photo to retrieve.</param>
    /// <returns>
    /// A <see cref="OneOf{Photo, NotFound}"/> containing the photo metadata if found.
    /// </returns>
    public ValueTask<OneOf<Photo, NotFound>> GetPhotoMetadataByIdAsync(Guid id);
}

/// <summary>
/// Service implementation for photo operations.
/// </summary>
/// <remarks>
/// Handles business logic for photo retrieval, including logging and error handling.
/// </remarks>
public class PhotoService(IUnitOfWork uow, ILogger<PhotoService> logger) : IPhotoService
{
    /// <summary>
    /// Retrieves a photo by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the photo to retrieve.</param>
    /// <returns>
    /// A <see cref="OneOf{Photo, NotFound}"/> containing either the photo if found,
    /// or a NotFound indicator if the photo does not exist.
    /// </returns>
    /// <remarks>
    /// Logs information about the success or failure of the retrieval operation.
    /// This method queries the repository and returns the result wrapped in a discriminated union type.
    /// </remarks>
    public async ValueTask<OneOf<Photo, NotFound>> GetPhotoByIdAsync(Guid id)
    {
        var photo = await uow.PhotoRepository.GetPhotoByIdAsync(id);
        if (photo is null)
        {
            logger.LogInformation("Photo with id {id} was not found", id);
            return new NotFound();
        }
        else
        {
            logger.LogInformation("Photo with id {id} was found", id);
            return photo;
        }
    }

    /// <summary>
    /// Retrieves the expanded metadata of a photo, including all its properties.
    /// </summary>
    /// <param name="id">The unique identifier of the photo to retrieve.</param>
    /// <returns>
    /// A <see cref="OneOf{Photo, NotFound}"/> containing the photo metadata if found.
    /// </returns>
    public async ValueTask<OneOf<Photo, NotFound>> GetPhotoMetadataByIdAsync(Guid id)
    {
        // Same as GetPhotoByIdAsync - the Photo model contains all metadata
        return await GetPhotoByIdAsync(id);
    }
}
