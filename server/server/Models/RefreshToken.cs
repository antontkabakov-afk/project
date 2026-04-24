using System.Security.Cryptography;
using System.Text;

namespace server.Models;

public class RefreshToken
{
    public int Id { get; set; }

    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenId { get; set; }
}
public static class TokenHasher
{
    public static string Hash(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
