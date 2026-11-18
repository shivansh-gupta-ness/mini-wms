namespace MiniWms.Api.Common;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1");

        api.MapGet("/health", () => Results.Ok(new HealthResponse("ok")));

        return app;
    }

    private sealed record HealthResponse(string Status);
}
