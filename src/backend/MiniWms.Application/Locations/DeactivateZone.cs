using FluentValidation;

namespace MiniWms.Application.Locations;

/// <param name="Id">From the route.</param>
public record DeactivateZoneCommand(int Id, int Version);

public class DeactivateZoneValidator : AbstractValidator<DeactivateZoneCommand>
{
    public DeactivateZoneValidator()
    {
        RuleFor(c => c.Version).ValidVersion();
    }
}

/// <summary>
/// Deactivates a zone with no Active bin (LOC-2.4). An already Inactive zone is returned unchanged. Returns null
/// when not found.
/// </summary>
public class DeactivateZoneHandler(ILocationStore store, ILocationQueries queries)
{
    public async Task<ZoneDto?> HandleAsync(DeactivateZoneCommand command, CancellationToken cancellationToken)
    {
        await new DeactivateZoneValidator().ValidateAndThrowAsync(command, cancellationToken);

        var zone = await store.FindZoneAsync(command.Id, cancellationToken);
        if (zone is null)
        {
            return null;
        }

        if (zone.IsActive)
        {
            zone.Deactivate(await store.ZoneHasActiveBinsAsync(zone.Id, cancellationToken));
            await store.SaveAsync(zone, command.Version, cancellationToken);
        }

        return await queries.GetZoneAsync(zone.Id, cancellationToken);
    }
}
