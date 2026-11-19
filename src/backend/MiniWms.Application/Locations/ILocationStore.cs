using MiniWms.Domain.Common;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Locations;

/// <summary>Write side of Locations: tracked entities in, saved entities out. Implemented in Infrastructure.</summary>
public interface ILocationStore
{
    Task<Warehouse?> GetWarehouseAsync(CancellationToken cancellationToken);

    Task<Zone?> FindZoneAsync(int id, CancellationToken cancellationToken);

    Task<Bin?> FindBinAsync(int id, CancellationToken cancellationToken);

    /// <summary>Whether any zone, active or not, has this code; compared without regard to case.</summary>
    Task<bool> ZoneCodeExistsAsync(string code, CancellationToken cancellationToken);

    /// <summary>Whether any bin, active or not, has this code; compared without regard to case.</summary>
    Task<bool> BinCodeExistsAsync(string code, CancellationToken cancellationToken);

    /// <summary>Whether the zone has any bin, active or not.</summary>
    Task<bool> ZoneHasBinsAsync(int zoneId, CancellationToken cancellationToken);

    Task<bool> ZoneHasActiveBinsAsync(int zoneId, CancellationToken cancellationToken);

    void Add(Zone zone);

    void Add(Bin bin);

    /// <summary>
    /// Saves the entity. When <paramref name="clientVersion"/> is given the stale-version check uses it (a mismatch
    /// surfaces as a concurrency conflict, 409). A unique-code race maps to the same <c>code</c> field error as the
    /// use-case check.
    /// </summary>
    Task SaveAsync(AuditableEntity entity, int? clientVersion, CancellationToken cancellationToken);
}
