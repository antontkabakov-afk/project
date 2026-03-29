using server.Models;

namespace server.Date;
public class Session
{
    public int Id { get; set; } 

    public int UserId { get; set; }
    public User User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}