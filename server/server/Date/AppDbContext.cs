using Microsoft.EntityFrameworkCore;
using server.Models;

namespace server.Date;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletSnapshot> WalletSnapshots => Set<WalletSnapshot>();

    public DbSet<RefreshToken> RefreshToken => Set<RefreshToken>();
    public DbSet<Session> Session => Set<Session>();
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

            e.HasMany(x => x.Sessions)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Wallets)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Wallet>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(x => x.Address)
                .HasMaxLength(255)
                .IsRequired();

            e.Property(x => x.Chain)
                .HasMaxLength(32)
                .HasDefaultValue("eth")
                .IsRequired();

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.Address, x.Chain }).IsUnique();

            e.HasMany(x => x.Snapshots)
                .WithOne(x => x.Wallet)
                .HasForeignKey(x => x.WalletId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WalletSnapshot>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.TotalValue)
                .HasPrecision(18, 2)
                .IsRequired();

            e.Property(x => x.Currency)
                .HasMaxLength(16);

            e.Property(x => x.Notes)
                .HasMaxLength(500);

            e.Property(x => x.AssetsJson)
                .HasColumnType("text")
                .HasDefaultValue("[]")
                .IsRequired();

            e.HasIndex(x => x.WalletId);
            e.HasIndex(x => new { x.WalletId, x.Timestamp });
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
