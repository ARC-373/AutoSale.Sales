using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoSale.Infrastructure.Persistence;

public sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set DATABASE_CONNECTION_STRING before running Entity Framework Core design-time commands.");
        }

        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new SalesDbContext(options);
    }
}
