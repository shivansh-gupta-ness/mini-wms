using MiniWms.Application.Locations;

namespace MiniWms.Api.Locations;

public static class LocationsEndpoints
{
    private const string Api = "/api/v1";

    public static IServiceCollection AddLocations(this IServiceCollection services)
    {
        services.AddScoped<GetWarehouseHandler>();
        services.AddScoped<RenameWarehouseHandler>();
        services.AddScoped<ListZonesHandler>();
        services.AddScoped<CreateZoneHandler>();
        services.AddScoped<GetZoneHandler>();
        services.AddScoped<UpdateZoneHandler>();
        services.AddScoped<DeactivateZoneHandler>();
        services.AddScoped<ListBinsHandler>();
        services.AddScoped<CreateBinHandler>();
        services.AddScoped<GetBinHandler>();
        services.AddScoped<UpdateBinHandler>();
        services.AddScoped<DeactivateBinHandler>();
        services.AddScoped<GetLocationTreeHandler>();
        return services;
    }

    public static IEndpointRouteBuilder MapLocationsEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(Api);

        // Warehouse: one row, seeded; it can be read and renamed, never created or deleted (LOC-1.1).
        api.MapGet("/warehouse", async (GetWarehouseHandler handler, CancellationToken ct) =>
            await handler.HandleAsync(ct) is { } warehouse ? Results.Ok(warehouse) : Results.NotFound());

        api.MapPatch("/warehouse", async (HttpRequest request, RenameWarehouseHandler handler, CancellationToken ct) =>
        {
            var body = await LocationRequests.ReadBodyAsync<RenameBody>(request, ct);
            var result = await handler.HandleAsync(new RenameWarehouseCommand(body.Name, body.Version ?? 0), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        api.MapMethods("/warehouse", ["POST", "DELETE"], (HttpContext context) =>
        {
            context.Response.Headers.Allow = "GET, PATCH";
            return Results.StatusCode(StatusCodes.Status405MethodNotAllowed);
        });

        // Zones
        api.MapGet("/zones", async (HttpRequest request, ListZonesHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(LocationRequests.ZoneListQuery(request.Query), ct)));

        api.MapPost("/zones", async (HttpRequest request, CreateZoneHandler handler, CancellationToken ct) =>
        {
            var body = await LocationRequests.ReadBodyAsync<CreateZoneBody>(request, ct);
            var zone = await handler.HandleAsync(new CreateZoneCommand(body.Code, body.Name, body.Type), ct);
            return Results.Created($"{Api}/zones/{zone.Id}", zone);
        });

        api.MapGet("/zones/{id:int}", async (int id, GetZoneHandler handler, CancellationToken ct) =>
            await handler.HandleAsync(id, ct) is { } zone ? Results.Ok(zone) : Results.NotFound());

        api.MapPatch("/zones/{id:int}", async (int id, HttpRequest request, UpdateZoneHandler handler, CancellationToken ct) =>
        {
            var body = await LocationRequests.ReadBodyAsync<UpdateZoneBody>(request, ct);
            var result = await handler.HandleAsync(new UpdateZoneCommand(id, body.Name, body.Type, body.Version ?? 0), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        api.MapPost("/zones/{id:int}/deactivate", async (int id, HttpRequest request, DeactivateZoneHandler handler, CancellationToken ct) =>
        {
            var body = await LocationRequests.ReadBodyAsync<VersionBody>(request, ct);
            var result = await handler.HandleAsync(new DeactivateZoneCommand(id, body.Version ?? 0), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        // Bins
        api.MapGet("/bins", async (HttpRequest request, ListBinsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(LocationRequests.BinListQuery(request.Query), ct)));

        api.MapPost("/bins", async (HttpRequest request, CreateBinHandler handler, CancellationToken ct) =>
        {
            var body = await LocationRequests.ReadBodyAsync<CreateBinBody>(request, ct);
            var bin = await handler.HandleAsync(new CreateBinCommand(body.ZoneId ?? 0, body.Code, body.Name), ct);
            return Results.Created($"{Api}/bins/{bin.Id}", bin);
        });

        api.MapGet("/bins/{id:int}", async (int id, GetBinHandler handler, CancellationToken ct) =>
            await handler.HandleAsync(id, ct) is { } bin ? Results.Ok(bin) : Results.NotFound());

        api.MapPatch("/bins/{id:int}", async (int id, HttpRequest request, UpdateBinHandler handler, CancellationToken ct) =>
        {
            var body = await LocationRequests.ReadBodyAsync<RenameBody>(request, ct);
            var result = await handler.HandleAsync(new UpdateBinCommand(id, body.Name, body.Version ?? 0), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        api.MapPost("/bins/{id:int}/deactivate", async (int id, HttpRequest request, DeactivateBinHandler handler, CancellationToken ct) =>
        {
            var body = await LocationRequests.ReadBodyAsync<VersionBody>(request, ct);
            var result = await handler.HandleAsync(new DeactivateBinCommand(id, body.Version ?? 0), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        // Tree
        api.MapGet("/locations/tree", async (HttpRequest request, GetLocationTreeHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(LocationRequests.TreeQuery(request.Query), ct)));

        return app;
    }
}
