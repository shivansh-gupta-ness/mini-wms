using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniWms.Domain.Common;
using MiniWms.Domain.Locations;

namespace MiniWms.Infrastructure.Persistence;

/// <summary>Creates the single warehouse (LOC-1.1) if it is missing. Safe to run on every start.</summary>
public static class WarehouseSeeder
{
    public const string DefaultCode = "MAIN";
    public const string DefaultName = "Main warehouse";

    // A blank value counts as missing; surrounding spaces are removed the same way the Domain does.
    private static string Setting(IConfiguration configuration, string key, string fallback) =>
        configuration[key] is { } value && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;

    public static void Seed(AppDbContext db, IConfiguration configuration)
    {
        // LOC-1.1: exactly one warehouse exists. If any warehouse is there, the seed does nothing,
        // so changing the configured code later cannot add a second one.
        if (db.Warehouses.Any())
        {
            return;
        }

        var code = Setting(configuration, "Seed:Warehouse:Code", DefaultCode);
        var name = Setting(configuration, "Seed:Warehouse:Name", DefaultName);

        Warehouse warehouse;
        try
        {
            warehouse = Warehouse.Create(code, name);
        }
        catch (DomainException ex)
        {
            throw new InvalidOperationException(
                $"The warehouse seed configuration (Seed:Warehouse:Code / Name) is invalid: {ex.Message}", ex);
        }

        db.Warehouses.Add(warehouse);
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException) when (db.Warehouses.AsNoTracking().Any())
        {
            // Another instance seeded between our check and our insert; the warehouse exists, which is the goal.
        }
    }
}
