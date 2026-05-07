using server.DTO;

namespace server.Service.Portfolio;

public interface IPortfolioSnapshotService
{
    Task<IReadOnlyList<WalletSnapshotView>> GetSnapshotsAsync(
        int userId,
        int? walletId = null,
        int? days = null,
        CancellationToken cancellationToken = default);

    Task<WalletSnapshotView?> GetLatestSnapshotAsync(
        int userId,
        int? walletId = null,
        CancellationToken cancellationToken = default);

    Task<PortfolioStatistics> CalculateHistoricalPerformanceAsync(
        int userId,
        int? walletId = null,
        int? days = null,
        CancellationToken cancellationToken = default);

}
