using server.DTO;

namespace server.Service.Crypto;


public interface IPriceService
{
    Task<IReadOnlyDictionary<string, TokenPriceQuote>> GetTokenPricesAsync(
        IReadOnlyList<MoralisWalletToken> tokens,
        CancellationToken cancellationToken = default);
}
