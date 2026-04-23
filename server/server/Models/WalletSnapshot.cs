namespace server.Models;

public class WalletSnapshot
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string WalletAddress { get; set; } = string.Empty;

    public string Chain { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public decimal TotalValueUsd { get; set; }

    public string AssetsJson { get; set; } = "[]";
}
