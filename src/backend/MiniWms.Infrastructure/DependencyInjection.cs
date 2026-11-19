using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiniWms.Application.Common;
using MiniWms.Application.Locations;
using MiniWms.Infrastructure.Common;
using MiniWms.Infrastructure.Locations;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Infrastructure;

public static class DependencyInjection
{
    /// <param name="basePath">Folder against which a relative database path is resolved (the API content root).</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string? basePath = null)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ICurrentUser, DevelopmentCurrentUser>();
        // The connection string is read when the context is first built, so test overrides apply.
        services.AddDbContext<AppDbContext>(options => options
            .UseSqlite(ResolveConnectionString(configuration, basePath))
            .AddInterceptors(new SqliteConnectionInterceptor()));

        services.AddScoped<ILocationStore, LocationStore>();
        services.AddScoped<ILocationQueries, LocationQueries>();
        // Default until the Inventory module registers the real one; TryAdd lets it (or a test) replace it.
        services.TryAddSingleton<IStockQuery, ZeroStockQuery>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, string? basePath)
    {
        var raw = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        var builder = new SqliteConnectionStringBuilder(raw);
        if (basePath is not null && SqliteDataSource.FilePath(builder, basePath) is { } file)
        {
            builder.DataSource = file;
        }

        return builder.ToString();
    }
}
