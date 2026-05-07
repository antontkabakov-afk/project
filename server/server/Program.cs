using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Scalar.AspNetCore;
using server.Date;
using server.Models;
using server.Service;
using server.Service.Crypto;
using server.Service.DummyAccount;
using server.Service.Portfolio;
using server.Service.Setting;
using server.Service.User;
using server.Service.Wallet;
using System.Net;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connStr = DBContextStting.FromEnvironment();
var jwtSettings = JwtSettings.FromEnvironment();
var coinGeckoSettings = CoinGeckoSettings.FromEnvironment();
var cryptoPriceSnapshotSettings = CryptoPriceSnapshotSettings.FromEnvironment();
var moralisSettings = MoralisSettings.FromEnvironment();
var authCookieSettings = AuthCookieSettings.FromEnvironment(builder.Environment);
var corsSettings = CorsSettings.FromEnvironment();

void ConfigureCoinGeckoHttpClient(HttpClient client)
{
    client.BaseAddress = new Uri(coinGeckoSettings.BaseUrl);
    client.DefaultRequestVersion = HttpVersion.Version11;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("crypto-tracker/1.0");

    if (!string.IsNullOrWhiteSpace(coinGeckoSettings.DemoApiKey))
    {
        client.DefaultRequestHeaders.Add("x-cg-demo-api-key", coinGeckoSettings.DemoApiKey);
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connStr.ConnStr,
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));
builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddMemoryCache();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(coinGeckoSettings);
builder.Services.AddSingleton(cryptoPriceSnapshotSettings);
builder.Services.AddSingleton(moralisSettings);
builder.Services.AddSingleton(authCookieSettings);
builder.Services.AddSingleton(corsSettings);
builder.Services.AddSingleton(connStr);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var key = Encoding.UTF8.GetBytes(jwtSettings.AccessSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(corsSettings.AllowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<ICryptoPriceSnapshotService, CryptoPriceSnapshotService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IWalletSnapshotService, WalletSnapshotService>();
builder.Services.AddScoped<IPortfolioSnapshotService, PortfolioSnapshotService>();
builder.Services.AddScoped<DummyAccountService>();

builder.Services.AddHttpClient<ICryptoService, CryptoService>(ConfigureCoinGeckoHttpClient);
builder.Services.AddHttpClient<IPriceService, PriceService>(ConfigureCoinGeckoHttpClient);
builder.Services.AddHttpClient<IMoralisService, MoralisService>(client =>
{
    client.BaseAddress = new Uri(moralisSettings.BaseUrl);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("crypto-tracker/1.0");

    if (!string.IsNullOrWhiteSpace(moralisSettings.ApiKey))
    {
        client.DefaultRequestHeaders.Add("X-API-Key", moralisSettings.ApiKey);
    }
});
builder.Services.AddHostedService<CryptoPriceSnapshotBackgroundService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();

var app = builder.Build();
var startupLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("Startup");

await MigrateDatabaseAsync(app.Services, startupLogger);

using (var scope = app.Services.CreateScope())
{
    var conn = scope.ServiceProvider.GetRequiredService<DBContextStting>();

    if (conn.IsDummyInfo)
    {
        scope.ServiceProvider.GetRequiredService<DummyAccountService>();
    }
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();
app.MapGet(
        "/health/ready",
        async (AppDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? Results.Ok(new { status = "ok" })
                : Results.Problem(
                    detail: "The API cannot connect to PostgreSQL.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Database unavailable");
        })
    .AllowAnonymous();
app.MapControllers();

app.Run();

static async Task MigrateDatabaseAsync(IServiceProvider services, ILogger logger)
{
    const int maxAttempts = 10;

    for (var attempt = 1; attempt <= maxAttempts; attempt += 1)
    {
        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await InitializeDatabaseAsync(dbContext, logger);
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts && IsTransientStartupFailure(ex))
        {
            var delay = TimeSpan.FromSeconds(Math.Min(attempt * 2, 15));

            logger.LogWarning(
                ex,
                "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds} seconds.",
                attempt,
                maxAttempts,
                delay.TotalSeconds);

            await Task.Delay(delay);
        }
    }

    using var finalScope = services.CreateScope();
    var finalDbContext = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await InitializeDatabaseAsync(finalDbContext, logger);
}

static async Task InitializeDatabaseAsync(AppDbContext dbContext, ILogger logger)
{
    var hasMigrations = dbContext.Database.GetMigrations();

    if (!hasMigrations.Any())
    {
        await dbContext.Database.EnsureCreatedAsync();
        logger.LogInformation("No EF Core migrations were found. Database schema was created with EnsureCreated.");
    }
    else
    {
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }

    await ApplySchemaUpgradesAsync(dbContext, logger);
}

static bool IsTransientStartupFailure(Exception exception)
{
    return exception is NpgsqlException or TimeoutException ||
        exception.InnerException is not null && IsTransientStartupFailure(exception.InnerException);
}

static async Task ApplySchemaUpgradesAsync(AppDbContext dbContext, ILogger logger)
{
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE "Wallets"
            ADD COLUMN IF NOT EXISTS "Chain" character varying(32) NOT NULL DEFAULT 'eth';

        ALTER TABLE "WalletSnapshots"
            ADD COLUMN IF NOT EXISTS "AssetsJson" text NOT NULL DEFAULT '[]';

        ALTER TABLE "Wallets"
            ALTER COLUMN "Chain" SET DEFAULT 'eth';

        ALTER TABLE "WalletSnapshots"
            ALTER COLUMN "AssetsJson" SET DEFAULT '[]';

        DROP INDEX IF EXISTS "IX_Wallets_UserId_Address";
        DROP INDEX IF EXISTS "IX_Wallets_UserId_Address_Chain";

        CREATE UNIQUE INDEX "IX_Wallets_UserId_Address_Chain"
            ON "Wallets" ("UserId", "Address", "Chain");

        CREATE INDEX IF NOT EXISTS "IX_Wallets_UserId"
            ON "Wallets" ("UserId");

        CREATE INDEX IF NOT EXISTS "IX_WalletSnapshots_WalletId"
            ON "WalletSnapshots" ("WalletId");

        CREATE INDEX IF NOT EXISTS "IX_WalletSnapshots_WalletId_Timestamp"
            ON "WalletSnapshots" ("WalletId", "Timestamp");
        """);

    logger.LogInformation("Wallet schema upgrades were applied successfully.");
}
