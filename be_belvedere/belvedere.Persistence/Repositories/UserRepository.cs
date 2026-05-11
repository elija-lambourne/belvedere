using belvedere.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace belvedere.Persistence.Repositories;

public interface IUserRepository
{
    public ValueTask<User?> GetUserByIdAsync(Guid id);
}
public class UserRepository(DbSet<User> users) : IUserRepository
{
    public async ValueTask<User?> GetUserByIdAsync(Guid id)
    {
        return await users.FirstOrDefaultAsync(u => u.Id == id);
    }
}
