namespace server.Service;

public interface IPriceService
{
    Task<IReadOnlyDictionary<string, TokenPriceQuote>> GetTokenPricesAsync(
        IReadOnlyList<MoralisWalletToken> tokens,
        CancellationToken cancellationToken = default);
}
