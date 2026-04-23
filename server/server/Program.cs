using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using server.Date;
using server.Models;
using server.Service;
using System.Text;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var connStr = Environment.GetEnvironmentVariable("PG_CONNECTION")
              ?? throw new Exception("PG_CONNECTION not set in .env or environment");
var jwtSettings = JwtSettings.FromEnvironment();
var coinGeckoSettings = CoinGeckoSettings.FromEnvironment();
var moralisSettings = MoralisSettings.FromEnvironment();
var portfolioSnapshotSettings = PortfolioSnapshotSettings.FromEnvironment();
var cryptoPriceSnapshotSettings = CryptoPriceSnapshotSettings.FromEnvironment();

void ConfigureCoinGeckoHttpClient(HttpClient client)
{
    client.BaseAddress = new Uri(coinGeckoSettings.BaseUrl);

    if (!string.IsNullOrWhiteSpace(coinGeckoSettings.DemoApiKey))
    {
        client.DefaultRequestHeaders.Add("x-cg-demo-api-key", coinGeckoSettings.DemoApiKey);
    }
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connStr));
builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(coinGeckoSettings);
builder.Services.AddSingleton(moralisSettings);
builder.Services.AddSingleton(portfolioSnapshotSettings);
builder.Services.AddSingleton(cryptoPriceSnapshotSettings);

var key = Encoding.UTF8.GetBytes(jwtSettings.AccessSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;

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
            .WithOrigins("http://localhost:5173", "http://localhost:5500")//temp
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<ICryptoPriceSnapshotService, CryptoPriceSnapshotService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IPortfolioSnapshotService, PortfolioSnapshotService>();
builder.Services.AddHttpClient<ICryptoService, CryptoService>(ConfigureCoinGeckoHttpClient);
builder.Services.AddHttpClient<IMoralisService, MoralisService>(client =>
{
    var settings = MoralisSettings.FromEnvironment();

    Console.WriteLine($"Moralis BaseUrl: {settings.BaseUrl}");
    Console.WriteLine($"Moralis DefaultChain: {settings.DefaultChain}");
    Console.WriteLine($"Moralis ApiKey loaded: {!string.IsNullOrWhiteSpace(settings.ApiKey)}");
    Console.WriteLine($"Moralis Chains: {string.Join(", ", settings.SupportedChains)}");

    client.BaseAddress = new Uri(moralisSettings.BaseUrl);

    if (!string.IsNullOrWhiteSpace(moralisSettings.ApiKey))
    {
        client.DefaultRequestHeaders.Add("X-API-Key", moralisSettings.ApiKey);
    }
});
builder.Services.AddHttpClient<IPriceService, PriceService>(ConfigureCoinGeckoHttpClient);
builder.Services.AddHostedService<CryptoPriceSnapshotBackgroundService>();
builder.Services.AddHostedService<PortfolioSnapshotBackgroundService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
