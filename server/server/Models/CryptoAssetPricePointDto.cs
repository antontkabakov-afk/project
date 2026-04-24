namespace server.Models;

public record CryptoAssetPricePointDto(
    DateTime Timestamp,
    decimal CurrentPrice);
