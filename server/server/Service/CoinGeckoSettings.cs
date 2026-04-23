namespace server.Service;

public class CoinGeckoSettings
{
    private static readonly string[] DefaultSupportedCoinIds =
    {
        "bitcoin",
        "ethereum",
        "solana",
        "ripple",
        "cardano",
        "dogecoin",
        "tron",
        "avalanche-2",
        "polkadot",
        "chainlink"
    };

    public string BaseUrl { get; init; } = "https://api.coingecko.com/api/v3/";

    public string? DemoApiKey { get; init; }

    public IReadOnlyList<string> SupportedCoinIds { get; init; } = DefaultSupportedCoinIds;

    public static CoinGeckoSettings FromEnvironment()
    {
        var supportedCoinsValue = Environment.GetEnvironmentVariable("COINGECKO_SUPPORTED_COINS");
        var supportedCoinIds = string.IsNullOrWhiteSpace(supportedCoinsValue)
            ? DefaultSupportedCoinIds
            : supportedCoinsValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new CoinGeckoSettings
        {
            BaseUrl = Environment.GetEnvironmentVariable("COINGECKO_BASE_URL")
                ?? "https://api.coingecko.com/api/v3/",
            DemoApiKey = Environment.GetEnvironmentVariable("COINGECKO_DEMO_API_KEY"),
            SupportedCoinIds = supportedCoinIds
        };
    }
}
