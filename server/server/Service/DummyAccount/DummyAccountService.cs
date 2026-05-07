using server.Date;
using server.Models;
using server.Service.Setting;

namespace server.Service.DummyAccount;

public class DummyAccountService
{
    private readonly AppDbContext _db;

    public DummyAccountService(AppDbContext db, DBContextStting _dbContext)
    {
        _db = db;

        if (_dbContext.IsDummyInfo)
        {
            Seed();
        }
    }

    private void Seed()
    {
        if (_db.Users.Any())
            return; 

        var user = CreateUser();
        var wallets = CreateWallets(user);

        user.Wallets = wallets;

        _db.Users.Add(user);
        _db.SaveChanges();
    }

    private Models.User CreateUser()
    {
        return new Models.User
        {
            Email = "demo@crypto.local",
            Username = "demo_user",
            PasswordHash = "dummy_hash",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-30),
            LastSeenUtc = DateTime.UtcNow
        };
    }

    private List<Models.Wallet> CreateWallets(Models.User user)
    {
        var wallets = new List<Models.Wallet>();

        for (int w = 0; w < 2; w++)
        {
            var wallet = new Models.Wallet
            {
                Name = $"Wallet {w + 1}",
                Address = $"0x{Guid.NewGuid():N}",
                Chain = w == 0 ? "Ethereum" : "Bitcoin",
                User = user
            };

            wallet.Snapshots = CreateSnapshots(wallet);

            wallets.Add(wallet);
        }

        return wallets;
    }

    private List<WalletSnapshot> CreateSnapshots(Models.Wallet wallet)
    {
        var snapshots = new List<WalletSnapshot>();

        var baseValue = 5000m + (wallet.Chain == "Ethereum" ? 2000 : 3000);

        for (int i = 30; i >= 0; i--)
        {
            snapshots.Add(new WalletSnapshot
            {
                Wallet = wallet,
                Timestamp = DateTime.UtcNow.AddDays(-i),
                TotalValue = baseValue + (i * 120),
                Currency = "USD",
                AssetsJson = "{\"BTC\":0.5,\"ETH\":1.2}"
            });
        }

        return snapshots;
    }
}