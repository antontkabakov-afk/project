namespace server.DTO;

public record WalletAssetSnapshot(
    string AssetId,
    string AssetName,
    string AssetSymbol,
    string TokenAddress,
    decimal AmountHeld,
    string AmountHeldFormatted,
    decimal PriceUsd,
    decimal CurrentValue,
    bool IsNativeToken,
    string Chain,
    string? LogoUrl);

public record WalletSnapshotView(
    int Id,
    string WalletAddress,
    string Chain,
    DateTime Timestamp,
    decimal TotalValueUsd,
    IReadOnlyList<WalletAssetSnapshot> Assets);

public record PortfolioPerformanceSummary(
    DateTime? StartTimestampUtc,
    DateTime? EndTimestampUtc,
    decimal StartingValueUsd,
    decimal CurrentValueUsd,
    decimal ChangeValueUsd,
    decimal ChangePercentage);

public record HistoricalPerformancePoint(
    DateTime Timestamp,
    decimal TotalValueUsd,
    decimal ChangeValueUsd,
    decimal ChangePercentage);

public record AssetDistribution(
    string AssetSymbol,
    string AssetName,
    decimal AmountHeld,
    decimal CurrentValue,
    decimal Percentage);

public record AssetPerformance(
    string AssetSymbol,
    string AssetName,
    decimal AmountHeld,
    decimal CurrentValue,
    decimal ChangeValueUsd,
    decimal ChangePercentage);

public record PortfolioStatistics(
    PortfolioPerformanceSummary Performance,
    IReadOnlyList<HistoricalPerformancePoint> History,
    IReadOnlyList<AssetDistribution> Distribution,
    AssetPerformance? BestAsset,
    AssetPerformance? WorstAsset);
