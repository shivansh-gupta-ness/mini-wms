using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MiniWms.Domain.Common;

namespace MiniWms.Infrastructure.Persistence;

/// <summary>
/// Optimistic concurrency for updates (ADR-003). <c>AppDbContext.ApplyAudit</c> overwrites <c>Version</c> on save,
/// so a client-supplied version must go in as the <em>original</em> value: it is then the value in the WHERE clause,
/// and a stale version updates zero rows and raises <see cref="DbUpdateConcurrencyException"/>.
/// </summary>
public static class VersionedUpdate
{
    /// <summary>
    /// Call after loading the entity and applying the changes, before <c>SaveChanges</c>. The entry becomes
    /// Modified so the audit fields and the version increment (client version + 1) are written even when no
    /// property value changed.
    /// </summary>
    public static EntityEntry<TEntity> UseClientVersion<TEntity>(
        this AppDbContext context, TEntity entity, int clientVersion)
        where TEntity : AuditableEntity
    {
        var entry = context.Entry(entity);
        var version = entry.Property(e => e.Version);
        version.OriginalValue = clientVersion;
        version.IsModified = true; // moves the entry to Modified without rewriting unchanged columns
        return entry;
    }
}
