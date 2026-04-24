namespace server.Service;

public class CorsSettings
{
    private static readonly string[] DefaultAllowedOrigins =
    {
        "http://localhost:5173",
        "http://localhost:4173",
        "http://localhost:8080",
        "http://localhost:5500"
    };

    public IReadOnlyList<string> AllowedOrigins { get; init; } = DefaultAllowedOrigins;

    public static CorsSettings FromEnvironment()
    {
        var originsValue = Environment.GetEnvironmentVariable("CLIENT_ORIGINS");
        var origins = string.IsNullOrWhiteSpace(originsValue)
            ? DefaultAllowedOrigins
            : originsValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return new CorsSettings
        {
            AllowedOrigins = origins.Length == 0 ? DefaultAllowedOrigins : origins
        };
    }
}
