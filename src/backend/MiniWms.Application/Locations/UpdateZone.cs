using FluentValidation;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Locations;

/// <param name="Id">From the route.</param>
/// <param name="Type">Optional; changes the type only when it differs and the zone has no bins.</param>
public record UpdateZoneCommand(int Id, string? Name, string? Type, int Version);

public class UpdateZoneValidator : AbstractValidator<UpdateZoneCommand>
{
    public UpdateZoneValidator()
    {
        RuleFor(c => c.Name).RequiredText("Name", Zone.NameMaxLength);
        RuleFor(c => c.Type).ValidZoneType(required: false);
        RuleFor(c => c.Version).ValidVersion();
    }
}

/// <summary>Renames a zone and, while it has no bins, changes its type (LOC-2.1). Returns null when not found.</summary>
public class UpdateZoneHandler(ILocationStore store, ILocationQueries queries)
{
    public async Task<ZoneDto?> HandleAsync(UpdateZoneCommand command, CancellationToken cancellationToken)
    {
        await new UpdateZoneValidator().ValidateAndThrowAsync(command, cancellationToken);

        var zone = await store.FindZoneAsync(command.Id, cancellationToken);
        if (zone is null)
        {
            return null;
        }

        zone.Rename(command.Name);
        if (command.Type is not null)
        {
            LocationRules.TryParseZoneType(command.Type, out var type);
            var hasBins = type != zone.Type && await store.ZoneHasBinsAsync(zone.Id, cancellationToken);
            zone.ChangeType(type, hasBins);
        }

        await store.SaveAsync(zone, command.Version, cancellationToken);
        return await queries.GetZoneAsync(zone.Id, cancellationToken);
    }
}
