namespace server.Models;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Username { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; }

    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
}
