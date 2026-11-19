namespace MiniWms.Application.Locations;

/// <summary>Returns one bin with its path, or null when it does not exist (the endpoint answers 404).</summary>
public class GetBinHandler(ILocationQueries queries)
{
    public Task<BinDto?> HandleAsync(int id, CancellationToken cancellationToken) =>
        queries.GetBinAsync(id, cancellationToken);
}
