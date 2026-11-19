namespace MiniWms.Application.Locations;

/// <summary>Returns the single warehouse (LOC-1.1), or null when none exists.</summary>
public class GetWarehouseHandler(ILocationQueries queries)
{
    public Task<WarehouseDto?> HandleAsync(CancellationToken cancellationToken) =>
        queries.GetWarehouseAsync(cancellationToken);
}
