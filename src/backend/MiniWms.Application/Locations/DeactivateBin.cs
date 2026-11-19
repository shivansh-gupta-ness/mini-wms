using FluentValidation;
using MiniWms.Application.Common;

namespace MiniWms.Application.Locations;

/// <param name="Id">From the route.</param>
public record DeactivateBinCommand(int Id, int Version);

public class DeactivateBinValidator : AbstractValidator<DeactivateBinCommand>
{
    public DeactivateBinValidator()
    {
        RuleFor(c => c.Version).ValidVersion();
    }
}

/// <summary>
/// Deactivates a bin that holds no stock (LOC-2.2, LOC-2.3); the quantity comes from <see cref="IStockQuery"/>.
/// An already Inactive bin is returned unchanged. Returns null when not found.
/// </summary>
public class DeactivateBinHandler(ILocationStore store, ILocationQueries queries, IStockQuery stock)
{
    public async Task<BinDto?> HandleAsync(DeactivateBinCommand command, CancellationToken cancellationToken)
    {
        await new DeactivateBinValidator().ValidateAndThrowAsync(command, cancellationToken);

        var bin = await store.FindBinAsync(command.Id, cancellationToken);
        if (bin is null)
        {
            return null;
        }

        if (bin.IsActive)
        {
            bin.Deactivate(await stock.BinOnHandAsync(bin.Id, cancellationToken));
            await store.SaveAsync(bin, command.Version, cancellationToken);
        }

        return await queries.GetBinAsync(bin.Id, cancellationToken);
    }
}
