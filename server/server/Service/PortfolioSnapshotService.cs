using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.Date;
using server.Models;

namespace server.Service;

public class PortfolioSnapshotService : IPortfolioSnapshotService
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly IMoralisService _moralisService;
    private readonly IPriceService _priceService;
    private readonly PortfolioSnapshotSettings _settings;

    public PortfolioSnapshotService(
        AppDbContext db,
        IMoralisService moralisService,
        IPriceService priceService,
        PortfolioSnapshotSettings settings)
    {
        _db = db;
        _moralisService = moralisService;
        _priceService = priceService;
        _settings = settings;
    }

    public async Task<WalletSnapshotView> CreateSnapshotAsync(
        int userId,
        string walletAddress,
        string? chain,
        bool force,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = _moralisService.NormalizeWalletAddress(walletAddress);
        var normalizedChain = _moralisService.NormalizeChain(chain);

        var latestSnapshot = await GetLatestSnapshotEntityAsync(
            userId,
            normalizedAddress,
            normalizedChain,
            cancellationToken);

        if (!force &&
            latestSnapshot is not null &&
            DateTime.UtcNow - latestSnapshot.Timestamp < _settings.SnapshotInterval)
        {
            return ToView(latestSnapshot);
        }

        var walletTokens = await _moralisService.GetWalletTokensAsync(
            normalizedAddress,
            normalizedChain,
            cancellationToken);

        var currentPriceLookup = await _priceService.GetTokenPricesAsync(walletTokens, cancellationToken);
        var previousPriceLookup = latestSnapshot is null
            ? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            : DeserializeAssets(latestSnapshot.AssetsJson)
                .ToDictionary(asset => asset.AssetId, asset => asset.PriceUsd, StringComparer.OrdinalIgnoreCase);

        var assets = walletTokens
            .Where(token => !token.IsSpam && token.Balance > 0m)
            .Select(token =>
            {
                var priceUsd = currentPriceLookup.TryGetValue(token.AssetId, out var currentQuote) &&
                    currentQuote.PriceUsd > 0m
                    ? currentQuote.PriceUsd
                    : previousPriceLookup.GetValueOrDefault(token.AssetId);

                return new WalletAssetSnapshot(
                    token.AssetId,
                    token.Name,
                    token.Symbol,
                    token.TokenAddress,
                    token.Balance,
                    token.BalanceFormatted,
                    priceUsd,
                    token.Balance * priceUsd,
                    token.IsNativeToken,
                    normalizedChain,
                    token.LogoUrl);
            })
            .OrderByDescending(asset => asset.CurrentValue)
            .ThenBy(asset => asset.AssetSymbol)
            .ToList();

        var snapshot = new WalletSnapshot
        {
            UserId = userId,
            WalletAddress = normalizedAddress,
            Chain = normalizedChain,
            Timestamp = DateTime.UtcNow,
            TotalValueUsd = assets.Sum(asset => asset.CurrentValue),
            AssetsJson = JsonSerializer.Serialize(assets, SnapshotJsonOptions)
        };

        _db.WalletSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);

        return ToView(snapshot, assets);
    }

    public async Task<IReadOnlyList<WalletSnapshotView>> GetSnapshotsAsync(
        int userId,
        int? days = null,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await BuildSnapshotQuery(userId, days)
            .OrderByDescending(snapshot => snapshot.Timestamp)
            .ToListAsync(cancellationToken);

        return snapshots
            .Select(ToView)
            .ToList();
    }

    public async Task<WalletSnapshotView?> GetLatestSnapshotAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _db.WalletSnapshots
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        return snapshot is null ? null : ToView(snapshot);
    }

    public async Task<PortfolioStatistics> CalculateHistoricalPerformanceAsync(
        int userId,
        int? days = null,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await BuildSnapshotQuery(userId, days)
            .OrderBy(snapshot => snapshot.Timestamp)
            .ToListAsync(cancellationToken);

        if (snapshots.Count == 0)
        {
            return new PortfolioStatistics(
                new PortfolioPerformanceSummary(null, null, 0m, 0m, 0m, 0m),
                Array.Empty<HistoricalPerformancePoint>(),
                Array.Empty<AssetDistribution>(),
                null,
                null);
        }

        var snapshotViews = snapshots
            .Select(ToView)
            .ToList();

        var firstSnapshot = snapshotViews.First();
        var latestSnapshot = snapshotViews.Last();
        var changeValueUsd = latestSnapshot.TotalValueUsd - firstSnapshot.TotalValueUsd;
        var changePercentage = firstSnapshot.TotalValueUsd <= 0m
            ? 0m
            : changeValueUsd / firstSnapshot.TotalValueUsd * 100m;

        var performance = new PortfolioPerformanceSummary(
            firstSnapshot.Timestamp,
            latestSnapshot.Timestamp,
            firstSnapshot.TotalValueUsd,
            latestSnapshot.TotalValueUsd,
            changeValueUsd,
            changePercentage);

        var history = snapshotViews
            .Select(snapshot =>
            {
                var snapshotChangeValue = snapshot.TotalValueUsd - firstSnapshot.TotalValueUsd;
                var snapshotChangePercentage = firstSnapshot.TotalValueUsd <= 0m
                    ? 0m
                    : snapshotChangeValue / firstSnapshot.TotalValueUsd * 100m;

                return new HistoricalPerformancePoint(
                    snapshot.Timestamp,
                    snapshot.TotalValueUsd,
                    snapshotChangeValue,
                    snapshotChangePercentage);
            })
            .ToList();

        var latestAssets = latestSnapshot.Assets
            .Where(asset => asset.CurrentValue > 0m)
            .ToList();

        var latestTotalValue = latestAssets.Sum(asset => asset.CurrentValue);
        var distribution = latestAssets
            .Select(asset => new AssetDistribution(
                asset.AssetSymbol,
                asset.AssetName,
                asset.AmountHeld,
                asset.CurrentValue,
                latestTotalValue <= 0m ? 0m : asset.CurrentValue / latestTotalValue * 100m))
            .OrderByDescending(asset => asset.CurrentValue)
            .ToList();

        var assetPerformance = BuildAssetPerformance(snapshotViews, latestSnapshot);

        return new PortfolioStatistics(
            performance,
            history,
            distribution,
            assetPerformance.FirstOrDefault(),
            assetPerformance.LastOrDefault());
    }

    private static List<AssetPerformance> BuildAssetPerformance(
        IReadOnlyList<WalletSnapshotView> snapshots,
        WalletSnapshotView latestSnapshot)
    {
        var performance = snapshots
            .SelectMany(snapshot => snapshot.Assets.Select(asset => new { snapshot.Timestamp, Asset = asset }))
            .GroupBy(item => item.Asset.AssetId, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var firstSeenAsset = group
                    .OrderBy(item => item.Timestamp)
                    .Select(item => item.Asset)
                    .First();

                var latestAsset = latestSnapshot.Assets
                    .FirstOrDefault(asset => string.Equals(asset.AssetId, group.Key, StringComparison.OrdinalIgnoreCase));

                var currentValue = latestAsset?.CurrentValue ?? 0m;
                var currentAmount = latestAsset?.AmountHeld ?? 0m;
                var changeValueUsd = currentValue - firstSeenAsset.CurrentValue;
                var changePercentage = firstSeenAsset.CurrentValue <= 0m
                    ? 0m
                    : changeValueUsd / firstSeenAsset.CurrentValue * 100m;

                var referenceAsset = latestAsset ?? firstSeenAsset;

                return new AssetPerformance(
                    referenceAsset.AssetSymbol,
                    referenceAsset.AssetName,
                    currentAmount,
                    currentValue,
                    changeValueUsd,
                    changePercentage);
            })
            .OrderByDescending(item => item.ChangeValueUsd)
            .ThenBy(item => item.AssetSymbol)
            .ToList();

        return performance;
    }

    private IQueryable<WalletSnapshot> BuildSnapshotQuery(int userId, int? days)
    {
        var query = _db.WalletSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.UserId == userId);

        if (days.HasValue)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days.Value);
            query = query.Where(snapshot => snapshot.Timestamp >= cutoff);
        }

        return query;
    }

    private async Task<WalletSnapshot?> GetLatestSnapshotEntityAsync(
        int userId,
        string walletAddress,
        string chain,
        CancellationToken cancellationToken)
    {
        return await _db.WalletSnapshots
            .AsNoTracking()
            .Where(snapshot =>
                snapshot.UserId == userId &&
                snapshot.WalletAddress == walletAddress &&
                snapshot.Chain == chain)
            .OrderByDescending(snapshot => snapshot.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static WalletSnapshotView ToView(WalletSnapshot snapshot)
    {
        return ToView(snapshot, DeserializeAssets(snapshot.AssetsJson));
    }

    private static WalletSnapshotView ToView(
        WalletSnapshot snapshot,
        IReadOnlyList<WalletAssetSnapshot> assets)
    {
        return new WalletSnapshotView(
            snapshot.Id,
            snapshot.WalletAddress,
            snapshot.Chain,
            snapshot.Timestamp,
            snapshot.TotalValueUsd,
            assets);
    }

    private static IReadOnlyList<WalletAssetSnapshot> DeserializeAssets(string assetsJson)
    {
        if (string.IsNullOrWhiteSpace(assetsJson))
        {
            return Array.Empty<WalletAssetSnapshot>();
        }

        return JsonSerializer.Deserialize<List<WalletAssetSnapshot>>(assetsJson, SnapshotJsonOptions)
            ?? [];
    }
}
