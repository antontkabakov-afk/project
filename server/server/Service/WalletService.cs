using Microsoft.EntityFrameworkCore;
using server.Date;
using server.Models;

namespace server.Service;

public class WalletService : IWalletService
{
    private readonly AppDbContext _db;
    private readonly IMoralisService _moralisService;
    private readonly IPortfolioSnapshotService _portfolioSnapshotService;

    public WalletService(
        AppDbContext db,
        IMoralisService moralisService,
        IPortfolioSnapshotService portfolioSnapshotService)
    {
        _db = db;
        _moralisService = moralisService;
        _portfolioSnapshotService = portfolioSnapshotService;
    }

    public async Task<WalletConnectionView> GetWalletConnectionAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} was not found.");

        var latestSnapshot = await _db.WalletSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.UserId == userId)
            .OrderByDescending(snapshot => snapshot.Timestamp)
            .Select(snapshot => new { snapshot.Timestamp, snapshot.TotalValueUsd })
            .FirstOrDefaultAsync(cancellationToken);

        return new WalletConnectionView(
            !string.IsNullOrWhiteSpace(user.WalletAddress),
            user.WalletAddress,
            user.WalletChain ?? _moralisService.NormalizeChain(null),
            user.WalletConnectedAtUtc,
            latestSnapshot?.Timestamp,
            latestSnapshot?.TotalValueUsd);
    }

    public async Task<WalletConnectionView> ConnectWalletAsync(
        int userId,
        string walletAddress,
        string? chain,
        CancellationToken cancellationToken = default)
    {
        if (!_moralisService.IsValidWalletAddress(walletAddress))
        {
            throw new ArgumentException("Invalid wallet address.", nameof(walletAddress));
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} was not found.");

        var normalizedAddress = _moralisService.NormalizeWalletAddress(walletAddress);
        var normalizedChain = _moralisService.NormalizeChain(chain);
        WalletSnapshotView? snapshot = null;
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            user.WalletAddress = normalizedAddress;
            user.WalletChain = normalizedChain;
            user.WalletConnectedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            snapshot = await _portfolioSnapshotService.CreateSnapshotAsync(
                userId,
                normalizedAddress,
                normalizedChain,
                force: true,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        return new WalletConnectionView(
            true,
            normalizedAddress,
            normalizedChain,
            user.WalletConnectedAtUtc,
            snapshot?.Timestamp,
            snapshot?.TotalValueUsd);
    }
}
