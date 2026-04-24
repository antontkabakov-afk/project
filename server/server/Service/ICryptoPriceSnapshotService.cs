using server.Models;

namespace server.Service;

public interface ICryptoPriceSnapshotService
{
    Task BackfillHistoryAsync(
        CancellationToken cancellationToken = default);

    Task CaptureSnapshotAsync(
        bool force,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CryptoAssetDto>> GetLatestAssetsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CryptoAssetPricePointDto>> GetAssetHistoryAsync(
        string assetId,
        CancellationToken cancellationToken = default);
}
