using server.DTO;
using server.Extensions;
using server.Service.Setting;

namespace server.Service.Crypto;

public class CryptoPriceSnapshotService : ICryptoPriceSnapshotService
{
    private readonly ICryptoService _cryptoService;
    private readonly CoinGeckoSettings _coinGeckoSettings;

    public CryptoPriceSnapshotService(
        ICryptoService cryptoService,
        CoinGeckoSettings coinGeckoSettings)
    {
        _cryptoService = cryptoService;
        _coinGeckoSettings = coinGeckoSettings;
    }

    public Task BackfillHistoryAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task CaptureSnapshotAsync(bool force, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<CryptoAssetDto>> GetLatestAssetsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _cryptoService.GetAssetsAsync(cancellationToken);
        }
        catch (ExternalServiceException)
        {
            return _coinGeckoSettings.SupportedCoinIds
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(assetId => new CryptoAssetDto(assetId, assetId, assetId.ToUpperInvariant(), 0m))
                .ToArray();
        }
    }

    public async Task<IReadOnlyList<CryptoAssetPricePointDto>> GetAssetHistoryAsync(
        string assetId,
        CancellationToken cancellationToken = default)
    {
        var normalizedAssetId = NormalizeAssetId(assetId);

        try
        {
            return await _cryptoService.GetAssetHistoryAsync(
                normalizedAssetId,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow,
                cancellationToken);
        }
        catch (ExternalServiceException)
        {
            return Array.Empty<CryptoAssetPricePointDto>();
        }
    }

    private static string NormalizeAssetId(string assetId)
    {
        var normalizedAssetId = assetId.Trim();

        if (string.IsNullOrWhiteSpace(normalizedAssetId))
        {
            throw new ArgumentException("Asset id is required.", nameof(assetId));
        }

        return normalizedAssetId;
    }
}
