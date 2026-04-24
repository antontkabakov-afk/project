using server.Models;

namespace server.Service;

public interface IPortfolioSnapshotService
{
    Task<WalletSnapshotView> CreateSnapshotAsync(
        int userId,
        string walletAddress,
        string? chain,
        bool force,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WalletSnapshotView>> GetSnapshotsAsync(
        int userId,
        int? days = null,
        CancellationToken cancellationToken = default);

    Task<WalletSnapshotView?> GetLatestSnapshotAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<PortfolioStatistics> CalculateHistoricalPerformanceAsync(
        int userId,
        int? days = null,
        CancellationToken cancellationToken = default);
}
