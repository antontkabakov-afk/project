using Microsoft.EntityFrameworkCore;
using server.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace server.Date;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshToken => Set<RefreshToken>();
    public DbSet<Session> Session => Set<Session>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.SessionId);
        });
    }
}
