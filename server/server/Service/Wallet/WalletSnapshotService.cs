using Microsoft.EntityFrameworkCore;
using server.Date;
using server.DTO;
using server.Service.Crypto;

namespace server.Service.Wallet;

public class WalletSnapshotService : IWalletSnapshotService
{
    private readonly AppDbContext _db;
    private readonly IMoralisService _moralisService;
    private readonly IPriceService _priceService;

    public WalletSnapshotService(
        AppDbContext db,
        IMoralisService moralisService,
        IPriceService priceService)
    {
        _db = db;
        _moralisService = moralisService;
        _priceService = priceService;
    }

    public async Task<WalletSnapshotItemView> AddAsync(
        int authenticatedUserId,
        int walletId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var wallet = await _db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == walletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet {walletId} was not found.");

        if (wallet.UserId != authenticatedUserId)
        {
            throw new UnauthorizedAccessException("You cannot access another user's wallet.");
        }

        var tokens = await _moralisService.GetWalletTokensAsync(
            wallet.Address,
            wallet.Chain,
            cancellationToken);

        var activeTokens = tokens
            .Where(item => !item.IsSpam && item.Balance > 0m)
            .ToList();

        var priceQuotes = await _priceService.GetTokenPricesAsync(activeTokens, cancellationToken);
        var assets = activeTokens
            .Select(token =>
            {
                priceQuotes.TryGetValue(token.AssetId, out var quote);

                var priceUsd = quote?.PriceUsd ?? 0m;
                var currentValue = decimal.Round(
                    token.Balance * priceUsd,
                    2,
                    MidpointRounding.AwayFromZero);

                return new WalletAssetSnapshot(
                    token.AssetId,
                    token.Name,
                    token.Symbol,
                    token.TokenAddress,
                    token.Balance,
                    token.BalanceFormatted,
                    priceUsd,
                    currentValue,
                    token.IsNativeToken,
                    token.Chain,
                    token.LogoUrl);
            })
            .OrderByDescending(item => item.CurrentValue)
            .ThenBy(item => item.AssetSymbol)
            .ToList();

        var totalValue = decimal.Round(
            assets.Sum(item => item.CurrentValue),
            2,
            MidpointRounding.AwayFromZero);

        var snapshot = new Models.WalletSnapshot
        {
            WalletId = walletId,
            Timestamp = DateTime.UtcNow,
            TotalValue = totalValue,
            Currency = "USD",
            Notes = NormalizeOptionalValue(notes),
            AssetsJson = WalletSnapshotAssetSerializer.Serialize(assets)
        };

        _db.WalletSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);

        return ToSnapshotView(snapshot);
    }

    public async Task<IReadOnlyList<WalletSnapshotItemView>> GetByWalletAsync(
        int authenticatedUserId,
        int walletId,
        CancellationToken cancellationToken = default)
    {
        var wallet = await _db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == walletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet {walletId} was not found.");

        if (wallet.UserId != authenticatedUserId)
        {
            throw new UnauthorizedAccessException("You cannot access another user's wallet.");
        }

        var snapshots = await _db.WalletSnapshots
            .AsNoTracking()
            .Where(item => item.WalletId == walletId)
            .OrderByDescending(item => item.Timestamp)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        return snapshots
            .Select(ToSnapshotView)
            .ToList();
    }

    internal static WalletSnapshotItemView ToSnapshotView(Models.WalletSnapshot snapshot)
    {
        return new WalletSnapshotItemView(
            snapshot.Id,
            snapshot.WalletId,
            snapshot.Timestamp,
            snapshot.TotalValue,
            snapshot.Currency,
            snapshot.Notes);
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }
}
