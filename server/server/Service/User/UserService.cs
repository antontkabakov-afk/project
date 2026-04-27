using Microsoft.EntityFrameworkCore;
using server.Date;
using server.DTO;
using server.Service.Wallet;

namespace server.Service.User;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserView> GetByIdAsync(
        int authenticatedUserId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (authenticatedUserId != userId)
        {
            throw new UnauthorizedAccessException("You cannot access another user.");
        }

        var user = await _db.Users
            .AsNoTracking()
            .Include(item => item.Wallets)
            .ThenInclude(item => item.Snapshots)
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {userId} was not found.");

        var wallets = user.Wallets
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Chain)
            .ThenBy(item => item.Address)
            .Select(wallet => WalletService.ToWalletView(wallet, wallet.Snapshots))
            .ToList();

        return new UserView(
            user.Id,
            user.Username,
            user.Email,
            wallets);
    }
}
