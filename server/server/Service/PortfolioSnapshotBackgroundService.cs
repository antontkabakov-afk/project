using Microsoft.EntityFrameworkCore;
using server.Date;

namespace server.Service;

public class PortfolioSnapshotBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PortfolioSnapshotBackgroundService> _logger;
    private readonly PortfolioSnapshotSettings _settings;

    public PortfolioSnapshotBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PortfolioSnapshotBackgroundService> logger,
        PortfolioSnapshotSettings settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_settings.SnapshotInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CaptureSnapshotsAsync(stoppingToken);

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CaptureSnapshotsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var snapshotService = scope.ServiceProvider.GetRequiredService<IPortfolioSnapshotService>();

        var wallets = await db.Users
            .AsNoTracking()
            .Where(user => user.WalletAddress != null)
            .Select(user => new
            {
                user.Id,
                user.WalletAddress,
                user.WalletChain
            })
            .ToListAsync(cancellationToken);

        foreach (var wallet in wallets)
        {
            try
            {
                await snapshotService.CreateSnapshotAsync(
                    wallet.Id,
                    wallet.WalletAddress!,
                    wallet.WalletChain,
                    force: false,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Snapshot capture failed for user {UserId} and wallet {WalletAddress}",
                    wallet.Id,
                    wallet.WalletAddress);
            }
        }
    }
}
