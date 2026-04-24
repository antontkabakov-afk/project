using Microsoft.EntityFrameworkCore;
using server.Models;

namespace server.Date;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshToken => Set<RefreshToken>();
    public DbSet<Session> Session => Set<Session>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<WalletSnapshot> WalletSnapshots => Set<WalletSnapshot>();
    public DbSet<CryptoPriceSnapshot> CryptoPriceSnapshots => Set<CryptoPriceSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Username).HasMaxLength(50);
            e.Property(x => x.WalletAddress).HasMaxLength(100);
            e.Property(x => x.WalletChain).HasMaxLength(50);

            e.HasMany(x => x.Sessions)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Transactions)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.WalletSnapshots)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasMany(x => x.RefreshTokens)
                .WithOne(x => x.Session)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();

            e.HasIndex(x => x.SessionId);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.AssetSymbol)
                .HasMaxLength(20)
                .IsRequired();

            e.Property(x => x.Amount)
                .HasPrecision(38, 18)
                .IsRequired();

            e.Property(x => x.PriceAtPurchase)
                .HasPrecision(38, 18)
                .IsRequired();

            e.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.Timestamp });
            e.HasIndex(x => new { x.UserId, x.AssetSymbol });
        });

        modelBuilder.Entity<WalletSnapshot>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.WalletAddress)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(x => x.Chain)
                .HasMaxLength(50)
                .IsRequired();

            e.Property(x => x.TotalValueUsd)
                .HasPrecision(38, 18)
                .IsRequired();

            e.Property(x => x.AssetsJson)
                .HasColumnType("text")
                .IsRequired();

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.Timestamp });
            e.HasIndex(x => new { x.UserId, x.WalletAddress, x.Chain, x.Timestamp });
        });

        modelBuilder.Entity<CryptoPriceSnapshot>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.AssetsJson)
                .HasColumnType("text")
                .IsRequired();

            e.HasIndex(x => x.Timestamp);
        });
    }
}
