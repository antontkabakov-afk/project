namespace server.Models;

public class WalletSnapshot
{
    public int Id { get; set; }

    public int WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public decimal TotalValue { get; set; }

    public string? Currency { get; set; }

    public string? Notes { get; set; }

    public string AssetsJson { get; set; } = "[]";
}
