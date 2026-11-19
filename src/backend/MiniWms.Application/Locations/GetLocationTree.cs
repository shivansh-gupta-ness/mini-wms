namespace MiniWms.Application.Locations;

/// <param name="IsActive">Filters zones and bins independently; null returns everything.</param>
public record GetLocationTreeQuery(bool? IsActive = null);

/// <summary>Returns every zone with its bins (LOC-3.1), unpaged.</summary>
public class GetLocationTreeHandler(ILocationQueries queries)
{
    public Task<IReadOnlyList<ZoneNodeDto>> HandleAsync(GetLocationTreeQuery query, CancellationToken cancellationToken) =>
        queries.GetTreeAsync(query.IsActive, cancellationToken);
}
