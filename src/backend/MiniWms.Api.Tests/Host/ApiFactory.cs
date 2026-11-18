using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MiniWms.Api.Tests.Host;

/// <summary>The real API host, started in memory.</summary>
internal class ApiFactory : WebApplicationFactory<Program>
{
}

/// <summary>
/// The real host plus a test-only route that throws. A startup filter adds the route after the host's own
/// pipeline, so the exception passes through the host's UseExceptionHandler.
/// </summary>
internal sealed class ThrowingApiFactory : ApiFactory
{
    public const string ThrowPath = "/__test/throw";
    public const string SecretText = "secret detail";

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
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
