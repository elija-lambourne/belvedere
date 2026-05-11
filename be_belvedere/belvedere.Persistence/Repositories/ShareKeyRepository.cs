using belvedere.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace belvedere.Persistence.Repositories;

public interface IShareKeyRepository
{
    public ValueTask<ShareKey?> GetShareKeyByKeyAsync(string key);
}

public class ShareKeyRepository(DbSet<ShareKey> shareKeys) : IShareKeyRepository
{
    public async ValueTask<ShareKey?> GetShareKeyByKeyAsync(string key)
    {
        return await shareKeys.FirstOrDefaultAsync(s => s.Key == key);
    }
}
