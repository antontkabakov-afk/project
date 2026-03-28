namespace server.Models;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    public string Username { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastSeanUtc { get; set; } = default;

    public ICollection<RefreshToken> Servers { get; set; } = new List<RefreshToken>();
}
