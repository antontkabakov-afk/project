namespace server.Service;

public class CryptoPriceSnapshotSettings
{
    public TimeSpan SnapshotInterval { get; init; } = TimeSpan.FromMinutes(15);
    public int BackfillDays { get; init; } = 30;

    public static CryptoPriceSnapshotSettings FromEnvironment()
    {
        var intervalMinutesValue = Environment.GetEnvironmentVariable("CRYPTO_PRICE_SNAPSHOT_INTERVAL_MINUTES");
        var intervalMinutes = int.TryParse(intervalMinutesValue, out var parsedMinutes) && parsedMinutes > 0
            ? parsedMinutes
            : 15;
        var backfillDaysValue = Environment.GetEnvironmentVariable("CRYPTO_PRICE_BACKFILL_DAYS");
        var backfillDays = int.TryParse(backfillDaysValue, out var parsedBackfillDays) && parsedBackfillDays >= 0
            ? parsedBackfillDays
            : 30;

        return new CryptoPriceSnapshotSettings
        {
            SnapshotInterval = TimeSpan.FromMinutes(intervalMinutes),
            BackfillDays = backfillDays
        };
    }
}
