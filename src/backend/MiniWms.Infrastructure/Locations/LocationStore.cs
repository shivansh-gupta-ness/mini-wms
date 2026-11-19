using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWms.Application.Locations;
using MiniWms.Domain.Common;
using MiniWms.Domain.Locations;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Infrastructure.Locations;

/// <summary>EF Core implementation of the Locations write side. Code columns are NOCASE, so equality ignores case.</summary>
internal sealed class LocationStore(AppDbContext context) : ILocationStore
{
    // SQLite extended result code SQLITE_CONSTRAINT_UNIQUE.
    private const int UniqueConstraintExtendedCode = 2067;

    public Task<Warehouse?> GetWarehouseAsync(CancellationToken cancellationToken) =>
        context.Warehouses.OrderBy(w => w.Id).FirstOrDefaultAsync(cancellationToken);

    public Task<Zone?> FindZoneAsync(int id, CancellationToken cancellationToken) =>
        context.Zones.FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

    public Task<Bin?> FindBinAsync(int id, CancellationToken cancellationToken) =>
        context.Bins.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> ZoneCodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        var trimmed = code.Trim();
        return context.Zones.AnyAsync(z => z.Code == trimmed, cancellationToken);
    }

    public Task<bool> BinCodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        var trimmed = code.Trim();
        return context.Bins.AnyAsync(b => b.Code == trimmed, cancellationToken);
    }

    public Task<bool> ZoneHasBinsAsync(int zoneId, CancellationToken cancellationToken) =>
        context.Bins.AnyAsync(b => b.ZoneId == zoneId, cancellationToken);

    public Task<bool> ZoneHasActiveBinsAsync(int zoneId, CancellationToken cancellationToken) =>
        context.Bins.AnyAsync(b => b.ZoneId == zoneId && b.IsActive, cancellationToken);

    public void Add(Zone zone) => context.Zones.Add(zone);

    public void Add(Bin bin) => context.Bins.Add(bin);

    public async Task SaveAsync(AuditableEntity entity, int? clientVersion, CancellationToken cancellationToken)
    {
        if (clientVersion is { } version)
        {
            context.UseClientVersion(entity, version);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DuplicateCodeColumn(ex) is not null)
        {
            // Lost a race with another request that used the same code; same answer as the use-case check.
            throw new ValidationFailedException("code", "Code is already used in the warehouse.");
        }
    }

    private static string? DuplicateCodeColumn(DbUpdateException ex)
    {
        if (ex.InnerException is not SqliteException { SqliteExtendedErrorCode: UniqueConstraintExtendedCode } sqlite)
        {
            return null;
        }

        foreach (var column in new[] { "Zones.Code", "Bins.Code" })
        {
            if (sqlite.Message.Contains(column, StringComparison.Ordinal))
            {
                return column;
            }
        }

        return null;
    }
}
