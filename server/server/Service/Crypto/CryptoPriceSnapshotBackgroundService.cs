using server.Extensions;
using server.Service.Setting;

namespace server.Service.Crypto;

public class CryptoPriceSnapshotBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CryptoPriceSnapshotBackgroundService> _logger;
    private readonly CryptoPriceSnapshotSettings _settings;

    public CryptoPriceSnapshotBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CryptoPriceSnapshotBackgroundService> logger,
        CryptoPriceSnapshotSettings settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await BackfillHistoryAsync(stoppingToken);

        using var timer = new PeriodicTimer(_settings.SnapshotInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CaptureSnapshotAsync(stoppingToken);

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

    private async Task BackfillHistoryAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var snapshotService = scope.ServiceProvider.GetRequiredService<ICryptoPriceSnapshotService>();

        try
        {
            await snapshotService.BackfillHistoryAsync(cancellationToken);
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogWarning(
                ex,
                "Crypto price history backfill skipped because an upstream service failed: {Message}",
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crypto price history backfill failed.");
        }
    }

    private async Task CaptureSnapshotAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var snapshotService = scope.ServiceProvider.GetRequiredService<ICryptoPriceSnapshotService>();

        try
        {
            await snapshotService.CaptureSnapshotAsync(
                force: false,
                cancellationToken);
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogWarning(
                ex,
                "Crypto price snapshot capture skipped because an upstream service failed: {Message}",
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crypto price snapshot capture failed.");
        }
    }
}
