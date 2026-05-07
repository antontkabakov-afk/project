using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using server.DTO;
using server.Extensions;

namespace server.Service.Crypto;

public class PriceService : IPriceService
{
    private const string AssetPlatformsCacheKey = "coingecko-asset-platforms";

    private static readonly TimeSpan AssetPlatformsCacheDuration = TimeSpan.FromHours(6);
    private static readonly TimeSpan TokenPriceCacheDuration = TimeSpan.FromMinutes(2);

    private static readonly IReadOnlyDictionary<string, int> MoralisChainIds =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["eth"] = 1,
            ["polygon"] = 137,
            ["bsc"] = 56,
            ["avalanche"] = 43114,
            ["arbitrum"] = 42161,
            ["optimism"] = 10,
            ["base"] = 8453
        };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<PriceService> _logger;

    public PriceService(
        HttpClient httpClient,
        IMemoryCache memoryCache,
        ILogger<PriceService> logger)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, TokenPriceQuote>> GetTokenPricesAsync(
        IReadOnlyList<MoralisWalletToken> tokens,
        CancellationToken cancellationToken = default)
    {
        if (tokens.Count == 0)
        {
            return new Dictionary<string, TokenPriceQuote>(StringComparer.OrdinalIgnoreCase);
        }

        var assetPlatforms = await GetAssetPlatformsAsync(cancellationToken);
        var results = new Dictionary<string, TokenPriceQuote>(StringComparer.OrdinalIgnoreCase);

        var nativeTokenGroups = new Dictionary<string, List<MoralisWalletToken>>(StringComparer.OrdinalIgnoreCase);
        var contractTokenGroups = new Dictionary<string, List<MoralisWalletToken>>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in tokens)
        {
            if (TryGetCachedQuote(token, assetPlatforms, out var cachedQuote))
            {
                results[token.AssetId] = cachedQuote;
                continue;
            }

            if (!TryResolvePlatform(token.Chain, assetPlatforms, out var platform))
            {
                continue;
            }

            if (token.IsNativeToken)
            {
                if (string.IsNullOrWhiteSpace(platform.NativeCoinId))
                {
                    continue;
                }

                if (!nativeTokenGroups.TryGetValue(platform.NativeCoinId, out var nativeTokens))
                {
                    nativeTokens = [];
                    nativeTokenGroups[platform.NativeCoinId] = nativeTokens;
                }

                nativeTokens.Add(token);
                continue;
            }

            if (string.IsNullOrWhiteSpace(token.TokenAddress) || token.TokenAddress.StartsWith("native:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!contractTokenGroups.TryGetValue(platform.Id, out var contractTokens))
            {
                contractTokens = [];
                contractTokenGroups[platform.Id] = contractTokens;
            }

            contractTokens.Add(token);
        }

        if (nativeTokenGroups.Count > 0)
        {
            var priceQuotes = await GetNativePricesAsync(
                nativeTokenGroups.Keys.ToArray(),
                cancellationToken);

            foreach (var nativeGroup in nativeTokenGroups)
            {
                if (!priceQuotes.TryGetValue(nativeGroup.Key, out var quote))
                {
                    continue;
                }

                foreach (var token in nativeGroup.Value)
                {
                    results[token.AssetId] = quote;
                    CacheQuote(token, assetPlatforms, quote);
                }
            }
        }

        foreach (var contractGroup in contractTokenGroups)
        {
            var lookup = await GetContractPricesAsync(contractGroup.Key, contractGroup.Value, cancellationToken);

            foreach (var token in contractGroup.Value)
            {
                if (!lookup.TryGetValue(token.TokenAddress, out var quote))
                {
                    continue;
                }

                results[token.AssetId] = quote;
                CacheQuote(token, assetPlatforms, quote);
            }
        }

        return results;
    }

    private async Task<IReadOnlyList<AssetPlatformItem>> GetAssetPlatformsAsync(CancellationToken cancellationToken)
    {
        return await _memoryCache.GetOrCreateAsync(
            AssetPlatformsCacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = AssetPlatformsCacheDuration;

                using var response = await _httpClient.GetAsync("asset_platforms", cancellationToken);

                if ((int)response.StatusCode == 429)
                {
                    throw new ExternalServiceException(
                        "CoinGecko rate limit reached. Please try again shortly.",
                        statusCode: 429);
                }

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var payload = await JsonSerializer.DeserializeAsync<List<AssetPlatformItem>>(
                    stream,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    cancellationToken);

                return payload ?? [];
            })
            ?? [];
    }

    private async Task<IReadOnlyDictionary<string, TokenPriceQuote>> GetNativePricesAsync(
        IReadOnlyList<string> nativeCoinIds,
        CancellationToken cancellationToken)
    {
        var joinedIds = Uri.EscapeDataString(string.Join(",", nativeCoinIds));
        var relativeUrl = $"simple/price?ids={joinedIds}&vs_currencies=usd&include_last_updated_at=true";

        using var payload = await GetJsonDocumentAsync(relativeUrl, cancellationToken);
        var result = new Dictionary<string, TokenPriceQuote>(StringComparer.OrdinalIgnoreCase);

        foreach (var nativeCoinId in nativeCoinIds)
        {
            if (!payload.RootElement.TryGetProperty(nativeCoinId, out var coinNode))
            {
                continue;
            }

            var price = GetDecimal(coinNode, "usd");
            var lastUpdatedAt = GetUnixTimestamp(coinNode, "last_updated_at");

            if (!price.HasValue)
            {
                continue;
            }

            result[nativeCoinId] = new TokenPriceQuote(
                price.Value,
                lastUpdatedAt ?? DateTime.UtcNow);
        }

        return result;
    }

    private async Task<IReadOnlyDictionary<string, TokenPriceQuote>> GetContractPricesAsync(
        string platformId,
        IReadOnlyList<MoralisWalletToken> tokens,
        CancellationToken cancellationToken)
    {
        var contractAddresses = tokens
            .Select(token => token.TokenAddress.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (contractAddresses.Length == 0)
        {
            return new Dictionary<string, TokenPriceQuote>(StringComparer.OrdinalIgnoreCase);
        }

        var joinedAddresses = Uri.EscapeDataString(string.Join(",", contractAddresses));
        var relativeUrl =
            $"simple/token_price/{Uri.EscapeDataString(platformId)}?contract_addresses={joinedAddresses}&vs_currencies=usd&include_last_updated_at=true";

        using var payload = await GetJsonDocumentAsync(relativeUrl, cancellationToken);
        var result = new Dictionary<string, TokenPriceQuote>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in payload.RootElement.EnumerateObject())
        {
            var price = GetDecimal(property.Value, "usd");
            var lastUpdatedAt = GetUnixTimestamp(property.Value, "last_updated_at");

            if (!price.HasValue)
            {
                continue;
            }

            result[property.Name.ToLowerInvariant()] = new TokenPriceQuote(
                price.Value,
                lastUpdatedAt ?? DateTime.UtcNow);
        }

        return result;
    }

    private async Task<JsonDocument> GetJsonDocumentAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

            if ((int)response.StatusCode == 429)
            {
                throw new ExternalServiceException(
                    "CoinGecko rate limit reached. Please try again shortly.",
                    statusCode: 429);
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (ExternalServiceException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "CoinGecko request failed for {RelativeUrl}", relativeUrl);
            throw new ExternalServiceException("Unable to retrieve token prices right now.", ex);
        }
    }

    private bool TryGetCachedQuote(
        MoralisWalletToken token,
        IReadOnlyList<AssetPlatformItem> assetPlatforms,
        out TokenPriceQuote quote)
    {
        var cacheKey = GetCacheKey(token, assetPlatforms);

        if (!string.IsNullOrWhiteSpace(cacheKey) &&
            _memoryCache.TryGetValue<TokenPriceQuote>(cacheKey, out var cachedQuote) &&
            cachedQuote is not null)
        {
            quote = cachedQuote;
            return true;
        }

        quote = default!;
        return false;
    }

    private void CacheQuote(
        MoralisWalletToken token,
        IReadOnlyList<AssetPlatformItem> assetPlatforms,
        TokenPriceQuote quote)
    {
        var cacheKey = GetCacheKey(token, assetPlatforms);

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return;
        }

        _memoryCache.Set(cacheKey, quote, TokenPriceCacheDuration);
    }

    private static string? GetCacheKey(
        MoralisWalletToken token,
        IReadOnlyList<AssetPlatformItem> assetPlatforms)
    {
        if (!TryResolvePlatform(token.Chain, assetPlatforms, out var platform))
        {
            return null;
        }

        if (token.IsNativeToken)
        {
            return string.IsNullOrWhiteSpace(platform.NativeCoinId)
                ? null
                : $"coingecko:native:{platform.NativeCoinId}";
        }

        return string.IsNullOrWhiteSpace(token.TokenAddress) || token.TokenAddress.StartsWith("native:", StringComparison.OrdinalIgnoreCase)
            ? null
            : $"coingecko:token:{platform.Id}:{token.TokenAddress.ToLowerInvariant()}";
    }

    private static bool TryResolvePlatform(
        string chain,
        IReadOnlyList<AssetPlatformItem> assetPlatforms,
        out AssetPlatformItem platform)
    {
        platform = default!;

        if (!MoralisChainIds.TryGetValue(chain, out var chainId))
        {
            return false;
        }

        var match = assetPlatforms.FirstOrDefault(item => item.ChainIdentifier == chainId);

        if (match is null)
        {
            return false;
        }

        platform = match;
        return true;
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.String when decimal.TryParse(property.GetString(), out var parsedValue) => parsedValue,
            _ => null
        };
    }

    private static DateTime? GetUnixTimestamp(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var unixTimestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }

        if (property.ValueKind == JsonValueKind.String && long.TryParse(property.GetString(), out unixTimestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }

        return null;
    }

    private sealed record AssetPlatformItem(
        [property: System.Text.Json.Serialization.JsonPropertyName("id")] string Id,
        [property: System.Text.Json.Serialization.JsonPropertyName("chain_identifier")] int? ChainIdentifier,
        [property: System.Text.Json.Serialization.JsonPropertyName("native_coin_id")] string? NativeCoinId);
}
