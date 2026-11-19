using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MiniWms.Infrastructure.Common;

namespace MiniWms.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef` build the context from the Infrastructure project (no host needed).</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new AppDbContext(options, new SystemClock(), new DevelopmentCurrentUser());
    }
}
