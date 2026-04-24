namespace server.Service;

public interface IMoralisService
{
    bool IsValidWalletAddress(string walletAddress);

    string NormalizeWalletAddress(string walletAddress);

    string NormalizeChain(string? chain);

    Task<IReadOnlyList<MoralisWalletToken>> GetWalletTokensAsync(
        string walletAddress,
        string? chain,
        CancellationToken cancellationToken = default);

    Task<MoralisNativeBalance> GetWalletBalanceAsync(
        string walletAddress,
        string? chain,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoralisWalletActivity>> GetWalletHistoryAsync(
        string walletAddress,
        string? chain,
        int pageSize,
        CancellationToken cancellationToken = default);
}
