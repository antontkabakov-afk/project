namespace server.Models;

public record CryptoAssetDto(
    string AssetId,
    string Name,
    string Symbol,
    decimal CurrentPrice);
