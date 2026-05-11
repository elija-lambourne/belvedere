using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using OneOf.Types;
using OneOf;

namespace belvedere.Core.Services;

public interface IPhotoService
{
    public ValueTask<OneOf<Photo, NotFound>> GetPhotoByIdAsync(Guid id);
}

public class PhotoService(IUnitOfWork uow,ILogger<PhotoService> logger) : IPhotoService
{
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
}
