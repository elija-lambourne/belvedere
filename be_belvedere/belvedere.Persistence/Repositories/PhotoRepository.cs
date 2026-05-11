using belvedere.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace belvedere.Persistence.Repositories;

public interface IPhotoRepository
{
    public ValueTask<Photo?> GetPhotoByIdAsync(Guid id);
}

public class PhotoRepository(DbSet<Photo> photos) : IPhotoRepository
{
    public async ValueTask<Photo?> GetPhotoByIdAsync(Guid id)
    {
        return await photos.FirstOrDefaultAsync(p => p.Id == id);
    }
}
