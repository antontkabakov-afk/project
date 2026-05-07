namespace server.Service.Setting;

public class MoralisSettings
{
    private static readonly string[] DefaultSupportedChains =
    {
        "eth",
        "polygon",
        "base",
        "arbitrum",
        "optimism",
        "bsc",
        "avalanche"
    };

    public string BaseUrl { get; init; } = "https://deep-index.moralis.io/api/v2.2/";

    public string ApiKey { get; init; } = string.Empty;

    public string DefaultChain { get; init; } = "eth";

    public IReadOnlyList<string> SupportedChains { get; init; } = DefaultSupportedChains;

    public static MoralisSettings FromEnvironment()
    {
        var supportedChainsValue = Environment.GetEnvironmentVariable("MORALIS_SUPPORTED_CHAINS");
        var supportedChains = string.IsNullOrWhiteSpace(supportedChainsValue)
            ? DefaultSupportedChains
            : supportedChainsValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new MoralisSettings
        {
            BaseUrl = Environment.GetEnvironmentVariable("MORALIS_BASE_URL")
                ?? "https://deep-index.moralis.io/api/v2.2/",
            ApiKey = Environment.GetEnvironmentVariable("MORALIS_API_KEY") ?? string.Empty,
            DefaultChain = Environment.GetEnvironmentVariable("MORALIS_DEFAULT_CHAIN") ?? "eth",
            SupportedChains = supportedChains
        };
    }
}
