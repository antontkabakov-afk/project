using server.Date;

namespace server.Models;

public class RefreshToken
{
    public int Id { get; set; } 

    public int SessionId { get; set; }
    public Session Session { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenId { get; set; }
}