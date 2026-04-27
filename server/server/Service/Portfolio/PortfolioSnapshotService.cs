using System.Globalization;
using Microsoft.EntityFrameworkCore;
using server.Date;
using server.DTO;
using server.Service.Wallet;

namespace server.Service.Portfolio;

public class PortfolioSnapshotService : IPortfolioSnapshotService
{
    private readonly AppDbContext _db;

    public PortfolioSnapshotService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WalletSnapshotView>> GetSnapshotsAsync(
        int userId,
        int? walletId = null,
        int? days = null,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await GetSnapshotRecordsAsync(userId, walletId, cancellationToken);
        var cutoff = GetCutoff(days);

        return snapshots
            .Where(item => !cutoff.HasValue || item.Timestamp >= cutoff.Value)
            .OrderByDescending(item => item.Timestamp)
            .ThenByDescending(item => item.Id)
            .Select(ToView)
            .ToList();
    }

    public async Task<WalletSnapshotView?> GetLatestSnapshotAsync(
        int userId,
        int? walletId = null,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await GetSnapshotRecordsAsync(userId, walletId, cancellationToken);

        if (snapshots.Count == 0)
        {
            return null;
        }

        if (walletId.HasValue)
        {
            var latestSnapshot = snapshots
                .OrderByDescending(item => item.Timestamp)
                .ThenByDescending(item => item.Id)
                .First();

            return ToView(latestSnapshot);
        }

        var latestSnapshotsByWallet = snapshots
            .GroupBy(item => item.WalletId)
            .Select(group => group
                .OrderByDescending(item => item.Timestamp)
                .ThenByDescending(item => item.Id)
                .First())
            .ToList();

        return ToAggregateView(latestSnapshotsByWallet, latestSnapshotsByWallet.Max(item => item.Timestamp));
    }

    public async Task<PortfolioStatistics> CalculateHistoricalPerformanceAsync(
        int userId,
        int? walletId = null,
        int? days = null,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await GetSnapshotRecordsAsync(userId, walletId, cancellationToken);

        return walletId.HasValue
            ? CalculateWalletStatistics(snapshots, days)
            : CalculateAccountStatistics(snapshots, days);
    }

    private async Task<List<SnapshotRecord>> GetSnapshotRecordsAsync(
        int userId,
        int? walletId,
        CancellationToken cancellationToken)
    {
        if (walletId.HasValue)
        {
            await EnsureWalletOwnershipAsync(userId, walletId.Value, cancellationToken);
        }

        var query = _db.WalletSnapshots
            .AsNoTracking()
            .Include(item => item.Wallet)
            .Where(item => item.Wallet.UserId == userId);

        if (walletId.HasValue)
        {
            query = query.Where(item => item.WalletId == walletId.Value);
        }

        var snapshots = await query
            .OrderBy(item => item.Timestamp)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return snapshots
            .Select(ToRecord)
            .ToList();
    }

    private async Task EnsureWalletOwnershipAsync(
        int userId,
        int walletId,
        CancellationToken cancellationToken)
    {
        var wallet = await _db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == walletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet {walletId} was not found.");

        if (wallet.UserId != userId)
        {
            throw new UnauthorizedAccessException("You cannot access another user's wallet.");
        }
    }

    private static PortfolioStatistics CalculateWalletStatistics(
        IReadOnlyList<SnapshotRecord> snapshots,
        int? days)
    {
        var cutoff = GetCutoff(days);
        var visibleSnapshots = snapshots
            .Where(item => !cutoff.HasValue || item.Timestamp >= cutoff.Value)
            .OrderBy(item => item.Timestamp)
            .ThenBy(item => item.Id)
            .ToList();

        if (visibleSnapshots.Count == 0)
        {
            return CreateEmptyStatistics();
        }

        var firstSnapshot = visibleSnapshots.First();
        var latestSnapshot = visibleSnapshots.Last();
        var aggregateSnapshots = visibleSnapshots
            .Select(item => new AggregateSnapshot(
                item.Timestamp,
                item.TotalValueUsd,
                item.Assets))
            .ToList();
        var performance = CreatePerformanceSummary(aggregateSnapshots.First(), aggregateSnapshots.Last());
        var history = CreateHistory(aggregateSnapshots);
        var distribution = CreateDistribution(latestSnapshot.Assets);
        var (bestAsset, worstAsset) = CreateAssetLeaders(firstSnapshot.Assets, latestSnapshot.Assets);

        return new PortfolioStatistics(
            performance,
            history,
            distribution,
            bestAsset,
            worstAsset);
    }

    private static PortfolioStatistics CalculateAccountStatistics(
        IReadOnlyList<SnapshotRecord> snapshots,
        int? days)
    {
        var cutoff = GetCutoff(days);
        var latestSnapshotsByWallet = new Dictionary<int, SnapshotRecord>();
        var aggregateSnapshots = new List<AggregateSnapshot>();

        foreach (var snapshot in snapshots)
        {
            latestSnapshotsByWallet[snapshot.WalletId] = snapshot;

            if (cutoff.HasValue && snapshot.Timestamp < cutoff.Value)
            {
                continue;
            }

            aggregateSnapshots.Add(ToAggregateSnapshot(latestSnapshotsByWallet.Values, snapshot.Timestamp));
        }

        if (aggregateSnapshots.Count == 0)
        {
            return CreateEmptyStatistics();
        }

        var firstSnapshot = aggregateSnapshots.First();
        var latestSnapshot = aggregateSnapshots.Last();
        var performance = CreatePerformanceSummary(firstSnapshot, latestSnapshot);
        var history = CreateHistory(aggregateSnapshots);
        var distribution = CreateDistribution(latestSnapshot.Assets);
        var (bestAsset, worstAsset) = CreateAssetLeaders(firstSnapshot.Assets, latestSnapshot.Assets);

        return new PortfolioStatistics(
            performance,
            history,
            distribution,
            bestAsset,
            worstAsset);
    }

    private static PortfolioPerformanceSummary CreatePerformanceSummary(
        AggregateSnapshot firstSnapshot,
        AggregateSnapshot latestSnapshot)
    {
        var changeValueUsd = latestSnapshot.TotalValueUsd - firstSnapshot.TotalValueUsd;
        var changePercentage = firstSnapshot.TotalValueUsd <= 0m
            ? (latestSnapshot.TotalValueUsd > 0m ? 100m : 0m)
            : changeValueUsd / firstSnapshot.TotalValueUsd * 100m;

        return new PortfolioPerformanceSummary(
            firstSnapshot.Timestamp,
            latestSnapshot.Timestamp,
            firstSnapshot.TotalValueUsd,
            latestSnapshot.TotalValueUsd,
            changeValueUsd,
            changePercentage);
    }

    private static IReadOnlyList<HistoricalPerformancePoint> CreateHistory(
        IReadOnlyList<AggregateSnapshot> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return [];
        }

        var firstSnapshot = snapshots.First();

        return snapshots
            .Select(snapshot =>
            {
                var changeValueUsd = snapshot.TotalValueUsd - firstSnapshot.TotalValueUsd;
                var changePercentage = firstSnapshot.TotalValueUsd <= 0m
                    ? (snapshot.TotalValueUsd > 0m ? 100m : 0m)
                    : changeValueUsd / firstSnapshot.TotalValueUsd * 100m;

                return new HistoricalPerformancePoint(
                    snapshot.Timestamp,
                    snapshot.TotalValueUsd,
                    changeValueUsd,
                    changePercentage);
            })
            .ToList();
    }

    private static IReadOnlyList<AssetDistribution> CreateDistribution(
        IReadOnlyList<WalletAssetSnapshot> assets)
    {
        var totalValue = assets.Sum(item => item.CurrentValue);

        return assets
            .Where(item => item.CurrentValue > 0m)
            .OrderByDescending(item => item.CurrentValue)
            .Select(item => new AssetDistribution(
                item.AssetSymbol,
                item.AssetName,
                item.AmountHeld,
                item.CurrentValue,
                totalValue <= 0m ? 0m : item.CurrentValue / totalValue * 100m))
            .ToList();
    }

    private static (AssetPerformance? BestAsset, AssetPerformance? WorstAsset) CreateAssetLeaders(
        IReadOnlyList<WalletAssetSnapshot> startingAssets,
        IReadOnlyList<WalletAssetSnapshot> endingAssets)
    {
        var startingLookup = startingAssets.ToDictionary(item => item.AssetId, StringComparer.OrdinalIgnoreCase);
        var endingLookup = endingAssets.ToDictionary(item => item.AssetId, StringComparer.OrdinalIgnoreCase);

        var performances = startingLookup.Keys
            .Union(endingLookup.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(assetId =>
            {
                startingLookup.TryGetValue(assetId, out var startingAsset);
                endingLookup.TryGetValue(assetId, out var endingAsset);

                var assetName = endingAsset?.AssetName ?? startingAsset?.AssetName ?? assetId;
                var assetSymbol = endingAsset?.AssetSymbol ?? startingAsset?.AssetSymbol ?? assetId;
                var amountHeld = endingAsset?.AmountHeld ?? 0m;
                var currentValue = endingAsset?.CurrentValue ?? 0m;
                var startingValue = startingAsset?.CurrentValue ?? 0m;
                var changeValueUsd = currentValue - startingValue;
                var changePercentage = startingValue <= 0m
                    ? (currentValue > 0m ? 100m : 0m)
                    : changeValueUsd / startingValue * 100m;

                return new AssetPerformance(
                    assetSymbol,
                    assetName,
                    amountHeld,
                    currentValue,
                    changeValueUsd,
                    changePercentage);
            })
            .Where(item => item.CurrentValue > 0m || item.ChangeValueUsd != 0m)
            .ToList();

        return (
            performances
                .OrderByDescending(item => item.ChangePercentage)
                .ThenByDescending(item => item.ChangeValueUsd)
                .FirstOrDefault(),
            performances
                .OrderBy(item => item.ChangePercentage)
                .ThenBy(item => item.ChangeValueUsd)
                .FirstOrDefault());
    }

    private static AggregateSnapshot ToAggregateSnapshot(
        IEnumerable<SnapshotRecord> snapshots,
        DateTime timestamp)
    {
        var snapshotList = snapshots.ToList();

        return new AggregateSnapshot(
            timestamp,
            decimal.Round(
                snapshotList.Sum(item => item.TotalValueUsd),
                2,
                MidpointRounding.AwayFromZero),
            AggregateAssets(snapshotList.SelectMany(item => item.Assets)));
    }

    private static IReadOnlyList<WalletAssetSnapshot> AggregateAssets(
        IEnumerable<WalletAssetSnapshot> assets)
    {
        return assets
            .GroupBy(item => item.AssetId, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var firstAsset = group.First();
                var amountHeld = group.Sum(item => item.AmountHeld);
                var currentValue = decimal.Round(
                    group.Sum(item => item.CurrentValue),
                    2,
                    MidpointRounding.AwayFromZero);
                var priceUsd = amountHeld <= 0m
                    ? group.LastOrDefault(item => item.PriceUsd > 0m)?.PriceUsd ?? 0m
                    : currentValue / amountHeld;

                return new WalletAssetSnapshot(
                    firstAsset.AssetId,
                    firstAsset.AssetName,
                    firstAsset.AssetSymbol,
                    firstAsset.TokenAddress,
                    amountHeld,
                    amountHeld.ToString(CultureInfo.InvariantCulture),
                    priceUsd,
                    currentValue,
                    firstAsset.IsNativeToken,
                    firstAsset.Chain,
                    firstAsset.LogoUrl);
            })
            .OrderByDescending(item => item.CurrentValue)
            .ThenBy(item => item.AssetSymbol)
            .ToList();
    }

    private static WalletSnapshotView ToView(SnapshotRecord snapshot)
    {
        return new WalletSnapshotView(
            snapshot.Id,
            snapshot.WalletAddress,
            snapshot.Chain,
            snapshot.Timestamp,
            snapshot.TotalValueUsd,
            snapshot.Assets);
    }

    private static WalletSnapshotView ToAggregateView(
        IReadOnlyList<SnapshotRecord> snapshots,
        DateTime timestamp)
    {
        var aggregateSnapshot = ToAggregateSnapshot(snapshots, timestamp);

        return new WalletSnapshotView(
            0,
            string.Empty,
            "account",
            aggregateSnapshot.Timestamp,
            aggregateSnapshot.TotalValueUsd,
            aggregateSnapshot.Assets);
    }

    private static SnapshotRecord ToRecord(Models.WalletSnapshot snapshot)
    {
        return new SnapshotRecord(
            snapshot.Id,
            snapshot.WalletId,
            snapshot.Wallet.Address,
            snapshot.Wallet.Chain,
            snapshot.Timestamp,
            snapshot.TotalValue,
            WalletSnapshotAssetSerializer.Deserialize(snapshot.AssetsJson));
    }

    private static DateTime? GetCutoff(int? days)
    {
        return days.HasValue
            ? DateTime.UtcNow.AddDays(-days.Value)
            : null;
    }

    private static PortfolioStatistics CreateEmptyStatistics()
    {
        return new PortfolioStatistics(
            new PortfolioPerformanceSummary(null, null, 0m, 0m, 0m, 0m),
            [],
            [],
            null,
            null);
    }

    private sealed record SnapshotRecord(
        int Id,
        int WalletId,
        string WalletAddress,
        string Chain,
        DateTime Timestamp,
        decimal TotalValueUsd,
        IReadOnlyList<WalletAssetSnapshot> Assets);

    private sealed record AggregateSnapshot(
        DateTime Timestamp,
        decimal TotalValueUsd,
        IReadOnlyList<WalletAssetSnapshot> Assets);
}
