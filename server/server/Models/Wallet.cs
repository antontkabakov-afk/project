namespace server.Models;

public class Wallet
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Chain { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<WalletSnapshot> Snapshots { get; set; } = new List<WalletSnapshot>();
}
