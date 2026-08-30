using Aukaria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aukaria.Infrastructure;

public sealed class AukariaDbContextFactory : IDesignTimeDbContextFactory<AukariaDbContext>
{
    public AukariaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AukariaDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=aukaria_dev;Username=postgres;Password=postgres")
            .Options;

        return new AukariaDbContext(options);
    }
}
