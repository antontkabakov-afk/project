using server.DTO;

namespace server.Service.User;

public interface IUserService
{
    Task<UserView> GetByIdAsync(
        int authenticatedUserId,
        int userId,
        CancellationToken cancellationToken = default);
}
