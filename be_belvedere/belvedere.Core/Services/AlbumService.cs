using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using OneOf.Types;
using OneOf;

namespace belvedere.Core.Services;
public interface IAlbumService
{
    public ValueTask<OneOf<Album, NotFound>> GetAlbumByIdAsync(Guid id); 
}

public class AlbumService(IUnitOfWork uow,ILogger<AlbumService> logger) : IAlbumService
{
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
}
