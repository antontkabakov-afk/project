using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace server.Date;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("PG_CONNECTION") ??
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException(
                "PG_CONNECTION (or ConnectionStrings__DefaultConnection) is not set.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(cs);

        return new AppDbContext(optionsBuilder.Options);
    }
}
