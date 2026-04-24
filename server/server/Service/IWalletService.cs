using server.Models;

namespace server.Service;

public interface IWalletService
{
    Task<WalletConnectionView> GetWalletConnectionAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<WalletConnectionView> ConnectWalletAsync(
        int userId,
        string walletAddress,
        string? chain,
        CancellationToken cancellationToken = default);
}
