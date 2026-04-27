namespace server.DTO;

public record CryptoAssetDto(
    string AssetId,
    string Name,
    string Symbol,
    decimal CurrentPrice);
