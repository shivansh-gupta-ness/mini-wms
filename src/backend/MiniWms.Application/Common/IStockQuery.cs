namespace MiniWms.Application.Common;

/// <summary>
/// Stock facts that belong to the Inventory module (ADR-001). Other modules ask through this interface; until
/// Inventory exists the registered implementation returns zero.
/// </summary>
public interface IStockQuery
{
    /// <summary>Total quantity held in the bin across all products.</summary>
    Task<int> BinOnHandAsync(int binId, CancellationToken cancellationToken);

    /// <summary>Total quantity of the product across all bins.</summary>
    Task<int> ProductOnHandAsync(int productId, CancellationToken cancellationToken);
}
