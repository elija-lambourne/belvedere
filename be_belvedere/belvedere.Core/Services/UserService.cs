using belvedere.Persistence.Model;
using belvedere.Persistence.Util;
using OneOf.Types;
using OneOf;

namespace belvedere.Core.Services;

public interface IUserService
{
    public ValueTask<OneOf<User,NotFound>> GetUserByIdAsync(Guid id);
}

public class UserService(IUnitOfWork uow,ILogger<UserService> logger) : IUserService
{
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
