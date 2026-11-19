namespace MiniWms.Application.Locations;

/// <summary>Returns one zone, or null when it does not exist (the endpoint answers 404).</summary>
public class GetZoneHandler(ILocationQueries queries)
{
    public Task<ZoneDto?> HandleAsync(int id, CancellationToken cancellationToken) =>
        queries.GetZoneAsync(id, cancellationToken);
}
