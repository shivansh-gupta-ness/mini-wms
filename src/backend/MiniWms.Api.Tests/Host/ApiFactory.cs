using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniWms.Api.Tests.Persistence;

namespace MiniWms.Api.Tests.Host;

/// <summary>
/// The real API host, started in memory. Every factory gets its own temporary database file through a
/// ConnectionStrings:Default override (TASK-004 AC9), never App_Data/miniwms.db. The override sits in a sealed
/// method, so a derived factory cannot skip it; derived factories add their services in <see cref="ConfigureTestHost"/>.
/// The database file and its -wal and -shm files are removed on dispose, after the connection pools are cleared.
/// </summary>
internal class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Absolute path of this factory's own database file.</summary>
    public string DatabasePath { get; } = TempDatabaseFiles.NewPath();

    protected sealed override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = TempDatabaseFiles.ConnectionStringFor(DatabasePath),
            }));

        ConfigureTestHost(builder);
    }

    /// <summary>Extension point for derived factories; the database override has already been applied.</summary>
    protected virtual void ConfigureTestHost(IWebHostBuilder builder)
    {
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            TempDatabaseFiles.Delete(DatabasePath);
        }
    }
}

/// <summary>
/// The real host plus a test-only route that throws. A startup filter adds the route after the host's own
/// pipeline, so the exception passes through the host's UseExceptionHandler.
/// </summary>
internal sealed class ThrowingApiFactory : ApiFactory
{
    public const string ThrowPath = "/__test/throw";
    public const string SecretText = "secret detail";

    protected override void ConfigureTestHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services => services.AddTransient<IStartupFilter, ThrowRouteStartupFilter>());

    private sealed class ThrowRouteStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                next(app);
                app.Use((context, nextMiddleware) =>
                    context.Request.Path == ThrowPath
                        ? throw new InvalidOperationException(SecretText)
                        : nextMiddleware(context));
            };
    }
}

/// <summary>
/// The real host plus test-only routes: the throwing route, and /__test/status/{code} routes that set the status
/// code and write no body, so the host's own status-code handling writes the problem+json.
/// </summary>
internal sealed class ErrorRoutesApiFactory : ApiFactory
{
    public const string StatusPathPrefix = "/__test/status/";

    protected override void ConfigureTestHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services => services.AddTransient<IStartupFilter, ErrorRoutesStartupFilter>());

    private sealed class ErrorRoutesStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                next(app);
                app.Use((context, nextMiddleware) =>
                {
                    if (context.Request.Path == ThrowingApiFactory.ThrowPath)
                    {
                        throw new InvalidOperationException(ThrowingApiFactory.SecretText);
                    }

                    if (context.Request.Path.StartsWithSegments(StatusPathPrefix.TrimEnd('/'), out var rest)
                        && int.TryParse(rest.Value?.TrimStart('/'), out var code))
                    {
                        context.Response.StatusCode = code;
                        return Task.CompletedTask;
                    }

                    return nextMiddleware(context);
                });
            };
    }
}
