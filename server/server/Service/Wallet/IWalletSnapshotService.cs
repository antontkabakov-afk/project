using server.DTO;

namespace server.Service.Wallet;

public interface IWalletSnapshotService
{
    Task<WalletSnapshotItemView> AddAsync(
        int authenticatedUserId,
        int walletId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WalletSnapshotItemView>> GetByWalletAsync(
        int authenticatedUserId,
        int walletId,
        CancellationToken cancellationToken = default);
}
