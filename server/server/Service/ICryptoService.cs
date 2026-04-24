using server.Models;

namespace server.Service;

public interface ICryptoService
{
    Task<IReadOnlyList<CryptoAssetDto>> GetAssetsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CryptoAssetSnapshotPoint>> GetHistoricalAssetsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
