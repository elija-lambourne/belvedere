using belvedere.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace belvedere.Persistence.Repositories;

public interface IAlbumRepository
{
    public ValueTask<Album?> GetAlbumByIdAsync(Guid id);
}
public class AlbumRepository(DbSet<Album> albums) : IAlbumRepository
{
    public async ValueTask<Album?> GetAlbumByIdAsync(Guid id)
    {
        return await albums.FirstOrDefaultAsync(a => a.Id == id);
    }
}
