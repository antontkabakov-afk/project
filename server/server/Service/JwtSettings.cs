namespace server.Service;

public class JwtSettings
{
    private const string DefaultAccessSecret = "ACCESS_SECRET_KEY_123ACCESS_SECRET_KEY_123";

    public string AccessSecret { get; init; } = DefaultAccessSecret;

    public static JwtSettings FromEnvironment()
    {
        return new JwtSettings
        {
            AccessSecret = Environment.GetEnvironmentVariable("JWT_ACCESS_SECRET")
                ?? DefaultAccessSecret
        };
    }
}
