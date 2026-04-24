namespace server.Service;

public record MoralisWalletToken(
    string AssetId,
    string Name,
    string Symbol,
    string TokenAddress,
    decimal Balance,
    string BalanceFormatted,
    int Decimals,
    bool IsNativeToken,
    bool IsSpam,
    string Chain,
    string? LogoUrl);

public record MoralisNativeBalance(
    string WalletAddress,
    string Chain,
    decimal Balance,
    string BalanceFormatted);

public record MoralisWalletActivity(
    string Hash,
    string Category,
    string Summary,
    DateTime Timestamp);

public record TokenPriceQuote(
    decimal PriceUsd,
    DateTime LastUpdatedAtUtc);
