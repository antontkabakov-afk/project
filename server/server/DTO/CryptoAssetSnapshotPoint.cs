namespace server.DTO;

public record CryptoAssetSnapshotPoint(
    DateTime Timestamp,
    IReadOnlyList<CryptoAssetDto> Assets);
