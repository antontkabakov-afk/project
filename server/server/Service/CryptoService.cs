using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using server.Models;

namespace server.Service;

public class CryptoService : ICryptoService
{
    private const int MaxRateLimitAttempts = 4;
    private const string SupportedCoinPricesCacheKey = "coingecko-supported-coin-prices";
    private const string UsdCurrencyCode = "usd";
    private const string RateLimitMessage = "CoinGecko rate limit reached. Please try again shortly.";

    private static readonly TimeSpan HistoricalRequestPacingDelay = TimeSpan.FromMilliseconds(750);
    private static readonly TimeSpan SupportedCoinPricesCacheDuration = TimeSpan.FromMinutes(1);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, (string Name, string Symbol)> SupportedCoinMetadata =
        new Dictionary<string, (string Name, string Symbol)>(StringComparer.OrdinalIgnoreCase)
        {
            ["bitcoin"] = ("Bitcoin", "BTC"),
            ["ethereum"] = ("Ethereum", "ETH"),
            ["solana"] = ("Solana", "SOL"),
            ["ripple"] = ("Ripple", "XRP"),
            ["cardano"] = ("Cardano", "ADA"),
            ["dogecoin"] = ("Dogecoin", "DOGE"),
            ["tron"] = ("TRON", "TRX"),
            ["avalanche-2"] = ("Avalanche", "AVAX"),
            ["polkadot"] = ("Polkadot", "DOT"),
            ["chainlink"] = ("Chainlink", "LINK")
        };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CryptoService> _logger;
    private readonly IReadOnlyList<SupportedCoin> _supportedCoins;

    public CryptoService(
        HttpClient httpClient,
        IMemoryCache memoryCache,
        ILogger<CryptoService> logger,
        CoinGeckoSettings coinGeckoSettings)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        _logger = logger;
        _supportedCoins = coinGeckoSettings.SupportedCoinIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(coinId => coinId.Trim())
            .Where(coinId => !string.IsNullOrWhiteSpace(coinId))
            .Select(CreateSupportedCoin)
            .ToArray();
    }

    public async Task<IReadOnlyList<CryptoAssetDto>> GetAssetsAsync(
        CancellationToken cancellationToken = default)
    {
        var supportedCoins = _supportedCoins;

        if (supportedCoins.Count == 0)
        {
            return [];
        }

        var priceLookup = await GetPriceLookupAsync(supportedCoins, cancellationToken);
        var assets = new List<CryptoAssetDto>(supportedCoins.Count);

        foreach (var supportedCoin in supportedCoins)
        {
            if (!priceLookup.TryGetValue(supportedCoin.Id, out var currentPrice))
            {
                continue;
            }

            assets.Add(new CryptoAssetDto(
                supportedCoin.Id,
                supportedCoin.Name,
                supportedCoin.Symbol,
                currentPrice));
        }

        if (assets.Count == 0)
        {
            throw new ExternalServiceException("CoinGecko returned an invalid price response.");
        }

        return assets;
    }

    public async Task<IReadOnlyList<CryptoAssetSnapshotPoint>> GetHistoricalAssetsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var supportedCoins = _supportedCoins;

        if (supportedCoins.Count == 0)
        {
            return [];
        }

        var normalizedFromUtc = NormalizeUtc(fromUtc);
        var normalizedToUtc = NormalizeUtc(toUtc);

        if (normalizedToUtc <= normalizedFromUtc)
        {
            return [];
        }

        var snapshotsByTimestamp = new SortedDictionary<DateTime, Dictionary<string, CryptoAssetDto>>();

        for (var index = 0; index < supportedCoins.Count; index += 1)
        {
            var supportedCoin = supportedCoins[index];
            var pricePoints = await GetHistoricalPricePointsAsync(
                supportedCoin,
                normalizedFromUtc,
                normalizedToUtc,
                cancellationToken);

            foreach (var (timestamp, currentPrice) in pricePoints)
            {
                var normalizedTimestamp = NormalizeHistoricalTimestamp(timestamp);

                if (!snapshotsByTimestamp.TryGetValue(normalizedTimestamp, out var assetsById))
                {
                    assetsById = new Dictionary<string, CryptoAssetDto>(StringComparer.OrdinalIgnoreCase);
                    snapshotsByTimestamp[normalizedTimestamp] = assetsById;
                }

                assetsById[supportedCoin.Id] = new CryptoAssetDto(
                    supportedCoin.Id,
                    supportedCoin.Name,
                    supportedCoin.Symbol,
                    currentPrice);
            }

            if (index < supportedCoins.Count - 1)
            {
                await Task.Delay(HistoricalRequestPacingDelay, cancellationToken);
            }
        }

        return snapshotsByTimestamp
            .Select(item => new CryptoAssetSnapshotPoint(
                item.Key,
                item.Value.Values
                    .OrderBy(asset => asset.Symbol, StringComparer.OrdinalIgnoreCase)
                    .ToArray()))
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<string, decimal>> GetPriceLookupAsync(
        IReadOnlyList<SupportedCoin> supportedCoins,
        CancellationToken cancellationToken)
    {
        return await _memoryCache.GetOrCreateAsync(
            SupportedCoinPricesCacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = SupportedCoinPricesCacheDuration;

                var joinedIds = Uri.EscapeDataString(string.Join(",", supportedCoins.Select(coin => coin.Id)));
                var relativeUrl = $"simple/price?ids={joinedIds}&vs_currencies={UsdCurrencyCode}";
                var payload = await GetRequiredPayloadAsync<Dictionary<string, SimplePriceItem>>(
                    relativeUrl,
                    invalidResponseMessage: "CoinGecko returned an invalid price response.",
                    failureMessage: "Unable to retrieve crypto prices right now.",
                    cancellationToken);

                return payload
                    .Where(item => item.Value.Usd.HasValue)
                    .ToDictionary(
                        item => item.Key,
                        item => item.Value.Usd!.Value,
                        StringComparer.OrdinalIgnoreCase);
            })
            ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyList<(DateTime Timestamp, decimal CurrentPrice)>> GetHistoricalPricePointsAsync(
        SupportedCoin supportedCoin,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var fromUnixSeconds = new DateTimeOffset(fromUtc).ToUnixTimeSeconds();
        var toUnixSeconds = new DateTimeOffset(toUtc).ToUnixTimeSeconds();
        var relativeUrl =
            $"coins/{Uri.EscapeDataString(supportedCoin.Id)}/market_chart/range?vs_currency={UsdCurrencyCode}&from={fromUnixSeconds}&to={toUnixSeconds}";
        var payload = await GetRequiredPayloadAsync<MarketChartRangeResponse>(
            relativeUrl,
            invalidResponseMessage: "CoinGecko returned an invalid price history response.",
            failureMessage: "Unable to retrieve crypto price history right now.",
            cancellationToken);

        return payload.Prices
            .Where(point => point.Count >= 2)
            .Select(point => (
                DateTimeOffset.FromUnixTimeMilliseconds(decimal.ToInt64(point[0])).UtcDateTime,
                point[1]))
            .ToArray();
    }

    private async Task<T> GetRequiredPayloadAsync<T>(
        string relativeUrl,
        string invalidResponseMessage,
        string failureMessage,
        CancellationToken cancellationToken)
        where T : class
    {
        for (var attempt = 1; attempt <= MaxRateLimitAttempts; attempt += 1)
        {
            try
            {
                using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

                if ((int)response.StatusCode == 429)
                {
                    if (attempt == MaxRateLimitAttempts)
                    {
                        throw new ExternalServiceException(RateLimitMessage, statusCode: 429);
                    }

                    var delay = GetRateLimitDelay(response, attempt);
                    _logger.LogWarning(
                        "CoinGecko rate limited {RelativeUrl}. Retrying in {DelaySeconds} seconds (attempt {Attempt}/{MaxAttempts}).",
                        relativeUrl,
                        delay.TotalSeconds,
                        attempt,
                        MaxRateLimitAttempts);

                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var payload = await JsonSerializer.DeserializeAsync<T>(
                    stream,
                    SerializerOptions,
                    cancellationToken);

                return payload ?? throw new ExternalServiceException(invalidResponseMessage);
            }
            catch (ExternalServiceException)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogError(ex, "CoinGecko request failed for {RelativeUrl}", relativeUrl);
                throw new ExternalServiceException(failureMessage, ex);
            }
        }

        throw new ExternalServiceException(RateLimitMessage, statusCode: 429);
    }

    private sealed record SupportedCoin(
        string Id,
        string Name,
        string Symbol);

    private sealed record SimplePriceItem(
        [property: JsonPropertyName("usd")] decimal? Usd);

    private sealed record MarketChartRangeResponse(
        [property: JsonPropertyName("prices")] List<List<decimal>> Prices);

    private static SupportedCoin CreateSupportedCoin(string coinId)
    {
        if (SupportedCoinMetadata.TryGetValue(coinId, out var metadata))
        {
            return new SupportedCoin(coinId, metadata.Name, metadata.Symbol);
        }

        var normalizedName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
            coinId.Replace('-', ' ').Trim().ToLowerInvariant());
        var normalizedSymbol = new string(
            coinId.Where(char.IsLetterOrDigit).Take(6).ToArray()).ToUpperInvariant();

        return new SupportedCoin(
            coinId,
            string.IsNullOrWhiteSpace(normalizedName) ? coinId : normalizedName,
            string.IsNullOrWhiteSpace(normalizedSymbol) ? coinId.ToUpperInvariant() : normalizedSymbol);
    }

    private static DateTime NormalizeHistoricalTimestamp(DateTime timestampUtc)
    {
        var normalizedTimestampUtc = NormalizeUtc(timestampUtc);

        return new DateTime(
            normalizedTimestampUtc.Year,
            normalizedTimestampUtc.Month,
            normalizedTimestampUtc.Day,
            normalizedTimestampUtc.Hour,
            0,
            0,
            DateTimeKind.Utc);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static TimeSpan GetRateLimitDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is TimeSpan retryAfterDelta && retryAfterDelta > TimeSpan.Zero)
        {
            return retryAfterDelta;
        }

        if (retryAfter?.Date is DateTimeOffset retryAfterDate)
        {
            var delay = retryAfterDate - DateTimeOffset.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                return delay;
            }
        }

        return TimeSpan.FromSeconds(Math.Min(5 * attempt, 20));
    }
}
