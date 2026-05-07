using server.DTO;

namespace server.Service.Wallet;

public interface IWalletService
{
    Task<WalletView> CreateAsync(
        int authenticatedUserId,
        int userId,
        string name,
        string address,
        string chain,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WalletView>> GetByUserAsync(
        int authenticatedUserId,
        int userId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int authenticatedUserId,
        int walletId,
        CancellationToken cancellationToken = default);

    bool IsDummyAccount(int? id);

}
