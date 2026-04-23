namespace server.Models;

// Deprecated legacy model retained for existing rows; the active portfolio flow uses wallet snapshots.
public class Transaction
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string AssetSymbol { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal PriceAtPurchase { get; set; }

    public TransactionType Type { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
