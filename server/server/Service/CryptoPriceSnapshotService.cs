using System.Text.Json;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using server.Date;
using server.Models;

namespace server.Service;

public class CryptoPriceSnapshotService : ICryptoPriceSnapshotService
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, (string Name, string Symbol)> FallbackAssetMetadata =
        new Dictionary<string, (string Name, string Symbol)>(StringComparer.OrdinalIgnoreCase)
        {
            ["bitcoin"] = ("Bitcoin", "BTC"),
            ["ethereum"] = ("Ethereum", "ETH"),
            ["solana"] = ("Solana", "SOL"),
            ["ripple"] = ("Ripple", "XRP"),
            ["cardano"] = ("Cardano", "ADA"),
            ["dogecoin"] = ("Dogecoin", "DOGE"),
            ["tron"] = ("TRON", "TRX"),
            ["avalanche-2"] = ("Avalanche", "AVAX"),
            ["polkadot"] = ("Polkadot", "DOT"),
            ["chainlink"] = ("Chainlink", "LINK")
        };

    private readonly AppDbContext _db;
    private readonly ICryptoService _cryptoService;
    private readonly CoinGeckoSettings _coinGeckoSettings;
    private readonly CryptoPriceSnapshotSettings _settings;
    private readonly ILogger<CryptoPriceSnapshotService> _logger;

    public CryptoPriceSnapshotService(
        AppDbContext db,
        ICryptoService cryptoService,
        CoinGeckoSettings coinGeckoSettings,
        CryptoPriceSnapshotSettings settings,
        ILogger<CryptoPriceSnapshotService> logger)
    {
        _db = db;
        _cryptoService = cryptoService;
        _coinGeckoSettings = coinGeckoSettings;
        _settings = settings;
        _logger = logger;
    }

    public async Task CaptureSnapshotAsync(
        bool force,
        CancellationToken cancellationToken = default)
    {
        var latestSnapshot = await GetLatestSnapshotEntityAsync(cancellationToken);
        await CaptureSnapshotEntityAsync(latestSnapshot, force, cancellationToken);
    }

    public async Task BackfillHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        if (_settings.BackfillDays <= 0)
        {
            return;
        }

        var backfillStartUtc = DateTime.UtcNow.AddDays(-_settings.BackfillDays);
        var oldestSnapshot = await _db.CryptoPriceSnapshots
            .AsNoTracking()
            .OrderBy(snapshot => snapshot.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (oldestSnapshot is not null && oldestSnapshot.Timestamp <= backfillStartUtc)
        {
            return;
        }

        var backfillEndUtc = oldestSnapshot?.Timestamp ?? DateTime.UtcNow;

        if (backfillEndUtc <= backfillStartUtc)
        {
            return;
        }

        var historicalSnapshots = await _cryptoService.GetHistoricalAssetsAsync(
            backfillStartUtc,
            backfillEndUtc,
            cancellationToken);

        if (historicalSnapshots.Count == 0)
        {
            return;
        }

        var existingTimestamps = await _db.CryptoPriceSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Timestamp >= backfillStartUtc && snapshot.Timestamp <= backfillEndUtc)
            .Select(snapshot => snapshot.Timestamp)
            .ToListAsync(cancellationToken);
        var existingTimestampSet = existingTimestamps.ToHashSet();

        var snapshotsToAdd = historicalSnapshots
            .Where(snapshot => snapshot.Assets.Count > 0 && !existingTimestampSet.Contains(snapshot.Timestamp))
            .Select(snapshot => new CryptoPriceSnapshot
            {
                Timestamp = snapshot.Timestamp,
                AssetsJson = JsonSerializer.Serialize(snapshot.Assets, SnapshotJsonOptions)
            })
            .OrderBy(snapshot => snapshot.Timestamp)
            .ToList();

        if (snapshotsToAdd.Count == 0)
        {
            return;
        }

        _db.CryptoPriceSnapshots.AddRange(snapshotsToAdd);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Backfilled {Count} crypto price snapshots covering the last {Days} days.",
            snapshotsToAdd.Count,
            _settings.BackfillDays);
    }

    public async Task<IReadOnlyList<CryptoAssetDto>> GetLatestAssetsAsync(
        CancellationToken cancellationToken = default)
    {
        var latestSnapshot = await GetLatestSnapshotEntityAsync(cancellationToken);

        try
        {
            var snapshot = await CaptureSnapshotEntityAsync(
                latestSnapshot,
                force: false,
                cancellationToken);

            return DeserializeAssets(snapshot.AssetsJson);
        }
        catch (ExternalServiceException ex) when (latestSnapshot is not null)
        {
            _logger.LogWarning(
                ex,
                "Falling back to the latest stored crypto price snapshot captured at {Timestamp}",
                latestSnapshot.Timestamp);

            return DeserializeAssets(latestSnapshot.AssetsJson);
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogWarning(
                ex,
                "Returning configured crypto assets without live prices because no stored snapshot is available.");

            return BuildConfiguredAssetFallback();
        }
    }

    public async Task<IReadOnlyList<CryptoAssetPricePointDto>> GetAssetHistoryAsync(
        string assetId,
        CancellationToken cancellationToken = default)
    {
        var normalizedAssetId = NormalizeAssetId(assetId);

        var snapshots = await _db.CryptoPriceSnapshots
            .AsNoTracking()
            .OrderBy(snapshot => snapshot.Timestamp)
            .ToListAsync(cancellationToken);

        if (snapshots.Count == 0)
        {
            return Array.Empty<CryptoAssetPricePointDto>();
        }

        var history = new List<CryptoAssetPricePointDto>(snapshots.Count);

        foreach (var snapshot in snapshots)
        {
            var asset = DeserializeAssets(snapshot.AssetsJson)
                .FirstOrDefault(item => string.Equals(
                    item.AssetId,
                    normalizedAssetId,
                    StringComparison.OrdinalIgnoreCase));

            if (asset is null)
            {
                continue;
            }

            history.Add(new CryptoAssetPricePointDto(
                snapshot.Timestamp,
                asset.CurrentPrice));
        }

        return history;
    }

    private async Task<CryptoPriceSnapshot> CaptureSnapshotEntityAsync(
        CryptoPriceSnapshot? latestSnapshot,
        bool force,
        CancellationToken cancellationToken)
    {
        if (!force &&
            latestSnapshot is not null &&
            DateTime.UtcNow - latestSnapshot.Timestamp < _settings.SnapshotInterval)
        {
            return latestSnapshot;
        }

        var assets = await _cryptoService.GetAssetsAsync(cancellationToken);
        var snapshot = new CryptoPriceSnapshot
        {
            Timestamp = DateTime.UtcNow,
            AssetsJson = JsonSerializer.Serialize(assets, SnapshotJsonOptions)
        };

        _db.CryptoPriceSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);

        return snapshot;
    }

    private async Task<CryptoPriceSnapshot?> GetLatestSnapshotEntityAsync(
        CancellationToken cancellationToken)
    {
        return await _db.CryptoPriceSnapshots
            .AsNoTracking()
            .OrderByDescending(snapshot => snapshot.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IReadOnlyList<CryptoAssetDto> DeserializeAssets(string assetsJson)
    {
        if (string.IsNullOrWhiteSpace(assetsJson))
        {
            return Array.Empty<CryptoAssetDto>();
        }

        return JsonSerializer.Deserialize<List<CryptoAssetDto>>(assetsJson, SnapshotJsonOptions)
            ?? [];
    }

    private IReadOnlyList<CryptoAssetDto> BuildConfiguredAssetFallback()
    {
        return _coinGeckoSettings.SupportedCoinIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(CreateFallbackAsset)
            .ToArray();
    }

    private static CryptoAssetDto CreateFallbackAsset(string assetId)
    {
        if (FallbackAssetMetadata.TryGetValue(assetId, out var metadata))
        {
            return new CryptoAssetDto(assetId, metadata.Name, metadata.Symbol, 0m);
        }

        var normalizedName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
            assetId.Replace('-', ' ').Trim().ToLowerInvariant());
        var normalizedSymbol = new string(
            assetId.Where(char.IsLetterOrDigit).Take(6).ToArray()).ToUpperInvariant();

        return new CryptoAssetDto(
            assetId,
            string.IsNullOrWhiteSpace(normalizedName) ? assetId : normalizedName,
            string.IsNullOrWhiteSpace(normalizedSymbol) ? assetId.ToUpperInvariant() : normalizedSymbol,
            0m);
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
