using FluentValidation;
using MiniWms.Domain.Common;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Locations;

/// <param name="Type">One of Receiving, Storage, Shipping (exact spelling).</param>
public record CreateZoneCommand(string? Code, string? Name, string? Type);

public class CreateZoneValidator : AbstractValidator<CreateZoneCommand>
{
    public CreateZoneValidator()
    {
        RuleFor(c => c.Code).RequiredText("Code", Zone.CodeMaxLength);
        RuleFor(c => c.Name).RequiredText("Name", Zone.NameMaxLength);
        RuleFor(c => c.Type).ValidZoneType(required: true);
    }
}

/// <summary>Creates an Active zone (LOC-1.2). A duplicate code is a 400 under <c>code</c> (LOC-1.4).</summary>
public class CreateZoneHandler(ILocationStore store, ILocationQueries queries)
{
    public async Task<ZoneDto> HandleAsync(CreateZoneCommand command, CancellationToken cancellationToken)
    {
        await new CreateZoneValidator().ValidateAndThrowAsync(command, cancellationToken);

        var warehouse = await store.GetWarehouseAsync(cancellationToken)
            ?? throw new InvalidOperationException("The warehouse does not exist.");

        var code = command.Code!.Trim();
        if (await store.ZoneCodeExistsAsync(code, cancellationToken))
        {
            throw LocationRules.DuplicateCode();
        }

        LocationRules.TryParseZoneType(command.Type, out var type);
        var zone = Zone.Create(warehouse.Id, code, command.Name, type);
        store.Add(zone);
        await store.SaveAsync(zone, null, cancellationToken);

        return await queries.GetZoneAsync(zone.Id, cancellationToken)
            ?? throw new InvalidOperationException("The saved zone could not be read.");
    }
}
