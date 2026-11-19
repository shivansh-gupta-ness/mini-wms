using MiniWms.Application.Common;

namespace MiniWms.Infrastructure.Locations;

/// <summary>Default <see cref="IStockQuery"/> until the Inventory module exists: nothing is ever in stock.</summary>
internal sealed class ZeroStockQuery : IStockQuery
{
    public Task<int> BinOnHandAsync(int binId, CancellationToken cancellationToken) => Task.FromResult(0);

    public Task<int> ProductOnHandAsync(int productId, CancellationToken cancellationToken) => Task.FromResult(0);
}
