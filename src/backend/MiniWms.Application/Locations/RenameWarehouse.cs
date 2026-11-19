using FluentValidation;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Locations;

public record RenameWarehouseCommand(string? Name, int Version);

public class RenameWarehouseValidator : AbstractValidator<RenameWarehouseCommand>
{
    public RenameWarehouseValidator()
    {
        RuleFor(c => c.Name).RequiredText("Name", Warehouse.NameMaxLength);
        RuleFor(c => c.Version).ValidVersion();
    }
}

/// <summary>Renames the warehouse (LOC-2.1); the code never changes.</summary>
public class RenameWarehouseHandler(ILocationStore store, ILocationQueries queries)
{
    public async Task<WarehouseDto?> HandleAsync(RenameWarehouseCommand command, CancellationToken cancellationToken)
    {
        await new RenameWarehouseValidator().ValidateAndThrowAsync(command, cancellationToken);

        var warehouse = await store.GetWarehouseAsync(cancellationToken);
        if (warehouse is null)
        {
            return null;
        }

        warehouse.Rename(command.Name);
        await store.SaveAsync(warehouse, command.Version, cancellationToken);
        return await queries.GetWarehouseAsync(cancellationToken);
    }
}
