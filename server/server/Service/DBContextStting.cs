namespace server.Service;

public class DBContextStting
{
    public string ConnStr { set; get; } = string.Empty;

    public static DBContextStting FromEnvironment()
    {
        var connectionString = Environment.GetEnvironmentVariable("PG_CONNECTION") ??
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PG_CONNECTION (or ConnectionStrings__DefaultConnection) must be set.");
        }

        return new DBContextStting
        {
            ConnStr = connectionString
        };
    }
}
