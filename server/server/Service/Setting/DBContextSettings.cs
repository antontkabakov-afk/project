namespace server.Service.Setting;

public class DBContextStting
{
    public string ConnStr { set; get; } = string.Empty;

    public bool IsDummyInfo { set; get; } = false;

    public static DBContextStting FromEnvironment()
    {
        var connectionString = Environment.GetEnvironmentVariable("PG_CONNECTION") ??
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        var isDummyInfoStr = Environment.GetEnvironmentVariable("IS_DUMMY_INFO");

        var isDummyInfo = false;

        if (!string.IsNullOrWhiteSpace(connectionString) || isDummyInfoStr == "true")
        {
            isDummyInfo = true;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PG_CONNECTION (or ConnectionStrings__DefaultConnection) must be set.");
        }

        return new DBContextStting
        {
            ConnStr = connectionString,
            IsDummyInfo = isDummyInfo
        };
    }
}
