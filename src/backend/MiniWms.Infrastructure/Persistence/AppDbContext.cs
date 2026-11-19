using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MiniWms.Application.Common;
using MiniWms.Domain.Common;
using MiniWms.Domain.Locations;
using MiniWms.Domain.Products;

namespace MiniWms.Infrastructure.Persistence;

/// <summary>
/// Context base (ADR-002). Module tables are added by module tickets; this class applies the
/// conventions for every <see cref="AuditableEntity"/> and stamps audit fields in SaveChanges.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, IClock clock, ICurrentUser currentUser)
    : DbContext(options)
{
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<Zone> Zones => Set<Zone>();

    public DbSet<Bin> Bins => Set<Bin>();

    public DbSet<Product> Products => Set<Product>();

    // The SQLite provider cannot order or compare DateTimeOffset values; store them as UTC ticks instead.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(AuditableEntity.Version))
                    .IsConcurrencyToken();
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAudit();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAudit();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAudit()
    {
        var now = clock.UtcNow;
        var user = currentUser.UserName;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = user;
                    entry.Entity.Version = 1;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = user;
                    // The original value is what the caller read; it goes into the WHERE clause,
                    // so a stale Version updates zero rows and raises DbUpdateConcurrencyException.
                    var version = entry.Property(e => e.Version);
                    version.CurrentValue = version.OriginalValue + 1;
                    break;
            }
        }
    }
}
