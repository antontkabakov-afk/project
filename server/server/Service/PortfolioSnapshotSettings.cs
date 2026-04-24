namespace server.Service;

public class PortfolioSnapshotSettings
{
    public TimeSpan SnapshotInterval { get; init; } = TimeSpan.FromHours(1);

    public static PortfolioSnapshotSettings FromEnvironment()
    {
        var intervalMinutesValue = Environment.GetEnvironmentVariable("PORTFOLIO_SNAPSHOT_INTERVAL_MINUTES");
        var intervalMinutes = int.TryParse(intervalMinutesValue, out var parsedMinutes) && parsedMinutes > 0
            ? parsedMinutes
            : 60;

        return new PortfolioSnapshotSettings
        {
            SnapshotInterval = TimeSpan.FromMinutes(intervalMinutes)
        };
    }
}
