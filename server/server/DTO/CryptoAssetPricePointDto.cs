namespace server.DTO;

public record CryptoAssetPricePointDto(
    DateTime Timestamp,
    decimal CurrentPrice);
