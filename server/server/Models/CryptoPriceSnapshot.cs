namespace server.Models;

public class CryptoPriceSnapshot
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string AssetsJson { get; set; } = "[]";
}
