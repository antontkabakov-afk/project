using server.DTO;

namespace server.Service.Crypto;


public interface ICryptoService
{
    Task<IReadOnlyList<CryptoAssetDto>> GetAssetsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CryptoAssetSnapshotPoint>> GetHistoricalAssetsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CryptoAssetPricePointDto>> GetAssetHistoryAsync(
        string assetId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
