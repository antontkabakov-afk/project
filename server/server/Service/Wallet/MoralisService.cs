using server.Extensions;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using server.Service.Setting;
using server.DTO;

namespace server.Service.Wallet;


public class MoralisService : IMoralisService
{
    private static readonly Regex WalletAddressRegex = new(
        "^0x[a-fA-F0-9]{40}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HttpClient _httpClient;
    private readonly ILogger<MoralisService> _logger;
    private readonly MoralisSettings _settings;

    public MoralisService(
        HttpClient httpClient,
        ILogger<MoralisService> logger,
        MoralisSettings settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings;
    }

    public bool IsValidWalletAddress(string walletAddress)
    {
        return WalletAddressRegex.IsMatch((walletAddress ?? string.Empty).Trim());
    }

    public string NormalizeWalletAddress(string walletAddress)
    {
        return (walletAddress ?? string.Empty).Trim().ToLowerInvariant();
    }

    public string NormalizeChain(string? chain)
    {
        var normalizedChain = (chain ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedChain))
        {
            normalizedChain = _settings.DefaultChain;
        }

        return _settings.SupportedChains.Contains(normalizedChain, StringComparer.OrdinalIgnoreCase)
            ? normalizedChain
            : _settings.DefaultChain;
    }

    public async Task<IReadOnlyList<MoralisWalletToken>> GetWalletTokensAsync(
        string walletAddress,
        string? chain,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = NormalizeWalletAddress(walletAddress);
        var normalizedChain = NormalizeChain(chain);

        var relativeUrl = $"wallets/{normalizedAddress}/tokens?chain={Uri.EscapeDataString(normalizedChain)}";
        var response = await GetFromMoralisAsync<TokenBalancesResponse>(relativeUrl, cancellationToken);

        return response.Result
            .Select(token => MapToken(token, normalizedChain))
            .Where(token => token.Balance > 0m)
            .OrderByDescending(token => token.IsNativeToken)
            .ThenBy(token => token.Symbol)
            .ToList();
    }

    public async Task<MoralisNativeBalance> GetWalletBalanceAsync(
        string walletAddress,
        string? chain,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = NormalizeWalletAddress(walletAddress);
        var normalizedChain = NormalizeChain(chain);

        var relativeUrl = $"{normalizedAddress}/balance?chain={Uri.EscapeDataString(normalizedChain)}";
        var response = await GetFromMoralisAsync<NativeBalanceResponse>(relativeUrl, cancellationToken);
        var rawBalance = TryParseDecimal(response.Balance) ?? 0m;
        var balance = AdjustRawBalance(rawBalance, 18);
        var formattedBalance = balance.ToString(CultureInfo.InvariantCulture);

        return new MoralisNativeBalance(
            normalizedAddress,
            normalizedChain,
            balance,
            formattedBalance);
    }

    public async Task<IReadOnlyList<MoralisWalletActivity>> GetWalletHistoryAsync(
        string walletAddress,
        string? chain,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = NormalizeWalletAddress(walletAddress);
        var normalizedChain = NormalizeChain(chain);
        var size = Math.Clamp(pageSize, 1, 100);

        var relativeUrl = $"wallets/{normalizedAddress}/history?chain={Uri.EscapeDataString(normalizedChain)}&limit={size}";
        var response = await GetFromMoralisAsync<WalletHistoryResponse>(relativeUrl, cancellationToken);

        return response.Result
            .Select(item => new MoralisWalletActivity(
                item.Hash ?? string.Empty,
                item.Category ?? string.Empty,
                item.Summary ?? string.Empty,
                item.BlockTimestamp))
            .OrderByDescending(item => item.Timestamp)
            .ToList();
    }

    private async Task<T> GetFromMoralisAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new ExternalServiceException("Moralis API key is not configured.");
        }

        try
        {
            using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

            if ((int)response.StatusCode == 429)
            {
                throw new ExternalServiceException(
                    "Moralis rate limit reached. Please try again shortly.",
                    statusCode: 429);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new ExternalServiceException(
                    "Moralis authorization failed. Verify MORALIS_API_KEY.",
                    statusCode: 503);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                throw new ExternalServiceException(
                    "Moralis rejected the wallet request. Check the wallet address and chain.",
                    statusCode: 400);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new ExternalServiceException(
                    "Moralis wallet endpoint was not found.",
                    statusCode: 503);
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken);

            if (payload is null)
            {
                throw new ExternalServiceException("Moralis returned an empty response.");
            }

            return payload;
        }
        catch (ExternalServiceException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Moralis request failed for {RelativeUrl}", relativeUrl);
            throw new ExternalServiceException("Unable to retrieve wallet data right now.", ex);
        }
    }

    private static MoralisWalletToken MapToken(TokenBalanceItem token, string chain)
    {
        var symbol = (token.Symbol ?? string.Empty).Trim().ToUpperInvariant();
        var tokenAddress = token.NativeToken
            ? $"native:{chain}"
            : (token.TokenAddress ?? string.Empty).Trim().ToLowerInvariant();

        var balance = TryParseDecimal(token.BalanceFormatted);
        var rawBalance = TryParseDecimal(token.Balance);

        if (!balance.HasValue && rawBalance.HasValue)
        {
            balance = AdjustRawBalance(rawBalance.Value, token.Decimals);
        }

        var balanceFormatted = !string.IsNullOrWhiteSpace(token.BalanceFormatted)
            ? token.BalanceFormatted
            : (balance ?? 0m).ToString(CultureInfo.InvariantCulture);

        return new MoralisWalletToken(
            AssetId: $"{chain}:{tokenAddress}",
            Name: (token.Name ?? symbol).Trim(),
            Symbol: symbol,
            TokenAddress: tokenAddress,
            Balance: balance ?? 0m,
            BalanceFormatted: balanceFormatted,
            Decimals: token.Decimals,
            IsNativeToken: token.NativeToken,
            IsSpam: token.PossibleSpam,
            Chain: chain,
            LogoUrl: token.Logo);
    }

    private static decimal? TryParseDecimal(string? value)
    {
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal AdjustRawBalance(decimal rawBalance, int decimals)
    {
        if (rawBalance <= 0m || decimals <= 0)
        {
            return rawBalance;
        }

        var adjustedBalance = rawBalance;

        for (var index = 0; index < Math.Min(decimals, 28); index += 1)
        {
            adjustedBalance /= 10m;
        }

        return adjustedBalance;
    }

    private sealed record TokenBalancesResponse(
        [property: JsonPropertyName("result")] List<TokenBalanceItem> Result);

    private sealed record TokenBalanceItem(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("symbol")] string? Symbol,
        [property: JsonPropertyName("decimals")] int Decimals,
        [property: JsonPropertyName("balance")] string? Balance,
        [property: JsonPropertyName("balance_formatted")] string? BalanceFormatted,
        [property: JsonPropertyName("possible_spam")] bool PossibleSpam,
        [property: JsonPropertyName("native_token")] bool NativeToken,
        [property: JsonPropertyName("token_address")] string? TokenAddress,
        [property: JsonPropertyName("logo")] string? Logo);

    private sealed record NativeBalanceResponse(
        [property: JsonPropertyName("balance")] string? Balance);

    private sealed record WalletHistoryResponse(
        [property: JsonPropertyName("result")] List<WalletHistoryItem> Result);

    private sealed record WalletHistoryItem(
        [property: JsonPropertyName("hash")] string? Hash,
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("block_timestamp")] DateTime BlockTimestamp);
}
