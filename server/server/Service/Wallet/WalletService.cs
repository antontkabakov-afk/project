using Microsoft.EntityFrameworkCore;
using server.Date;
using server.DTO;

namespace server.Service.Wallet;

public class WalletService : IWalletService
{
    private readonly AppDbContext _db;
    private readonly IMoralisService _moralisService;

    public WalletService(AppDbContext db, IMoralisService moralisService)
    {
        _db = db;
        _moralisService = moralisService;
    }
    public bool IsDummyAccount(int? id)
    {
        return !_db.Users.Where(u => u.Id == id).Where(u => u.Email == "demo@crypto.local").Any();
    }

    public async Task<WalletView> CreateAsync(
        int authenticatedUserId,
        int userId,
        string name,
        string address,
        string chain,
        CancellationToken cancellationToken = default)
    {
        EnsureOwnership(authenticatedUserId, userId);

        var normalizedName = (name ?? string.Empty).Trim();
        var normalizedAddress = _moralisService.NormalizeWalletAddress(address);
        var requestedChain = (chain ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedChain = _moralisService.NormalizeChain(requestedChain);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Wallet name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(normalizedAddress))
        {
            throw new ArgumentException("Wallet address is required.", nameof(address));
        }

        if (!_moralisService.IsValidWalletAddress(normalizedAddress))
        {
            throw new ArgumentException("Wallet address is invalid.", nameof(address));
        }

        if (!string.IsNullOrWhiteSpace(requestedChain) && requestedChain != normalizedChain)
        {
            throw new ArgumentException("Wallet chain is not supported.", nameof(chain));
        }

        var userExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(item => item.Id == userId, cancellationToken);

        if (!userExists)
        {
            throw new KeyNotFoundException($"User {userId} was not found.");
        }

        var walletExists = await _db.Wallets
            .AsNoTracking()
            .AnyAsync(
                item => item.UserId == userId &&
                    item.Address == normalizedAddress &&
                    item.Chain == normalizedChain,
                cancellationToken);

        if (walletExists)
        {
            throw new ArgumentException("This wallet already exists for the user on the selected chain.");
        }

        await _moralisService.GetWalletBalanceAsync(
            normalizedAddress,
            normalizedChain,
            cancellationToken);

        var wallet = new Models.Wallet
        {
            Name = normalizedName,
            Address = normalizedAddress,
            Chain = normalizedChain,
            UserId = userId
        };

        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync(cancellationToken);

        return ToWalletView(wallet, []);
    }

    public async Task<IReadOnlyList<WalletView>> GetByUserAsync(
        int authenticatedUserId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        EnsureOwnership(authenticatedUserId, userId);

        var userExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(item => item.Id == userId, cancellationToken);

        if (!userExists)
        {
            throw new KeyNotFoundException($"User {userId} was not found.");
        }

        var wallets = await _db.Wallets
            .AsNoTracking()
            .Include(item => item.Snapshots)
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Chain)
            .ThenBy(item => item.Address)
            .ToListAsync(cancellationToken);

        return wallets
            .Select(wallet => ToWalletView(wallet, wallet.Snapshots))
            .ToList();
    }

    public async Task DeleteAsync(
        int authenticatedUserId,
        int walletId,
        CancellationToken cancellationToken = default)
    {
        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(item => item.Id == walletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet {walletId} was not found.");

        if (wallet.UserId != authenticatedUserId)
        {
            throw new UnauthorizedAccessException("You cannot access another user's wallet.");
        }

        _db.Wallets.Remove(wallet);
        await _db.SaveChangesAsync(cancellationToken);
    }

    internal static WalletView ToWalletView(
        Models.Wallet wallet,
        IEnumerable<Models.WalletSnapshot> snapshots)
    {
        var snapshotViews = snapshots
            .OrderByDescending(item => item.Timestamp)
            .ThenByDescending(item => item.Id)
            .Select(WalletSnapshotService.ToSnapshotView)
            .ToList();

        return new WalletView(
            wallet.Id,
            wallet.Name,
            wallet.Address,
            wallet.Chain,
            wallet.UserId,
            snapshotViews);
    }

    private static void EnsureOwnership(int authenticatedUserId, int userId)
    {
        if (authenticatedUserId != userId)
        {
            throw new UnauthorizedAccessException("You cannot access another user's wallets.");
        }
    }
}
