namespace server.Models;

public record CryptoAssetSnapshotPoint(
    DateTime Timestamp,
    IReadOnlyList<CryptoAssetDto> Assets);
