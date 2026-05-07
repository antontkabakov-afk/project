using System.Text.Json;
using server.DTO;

namespace server.Service.Wallet;

internal static class WalletSnapshotAssetSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(IReadOnlyList<WalletAssetSnapshot> assets)
    {
        return JsonSerializer.Serialize(assets, JsonOptions);
    }

    public static IReadOnlyList<WalletAssetSnapshot> Deserialize(string? assetsJson)
    {
        if (string.IsNullOrWhiteSpace(assetsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<WalletAssetSnapshot>>(assetsJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
