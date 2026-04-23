using System.Text.Json.Serialization;

namespace server.Models;

// Deprecated legacy enum retained for existing transaction rows.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    [JsonStringEnumMemberName("BUY")]
    Buy,

    [JsonStringEnumMemberName("SELL")]
    Sell
}
