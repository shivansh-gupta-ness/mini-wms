using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniWms.Domain.Locations;

namespace MiniWms.Infrastructure.Persistence;

/// <summary>Creates the single warehouse (LOC-1.1) if it is missing. Safe to run on every start.</summary>
public static class WarehouseSeeder
{
    public const string DefaultCode = "MAIN";
    public const string DefaultName = "Main warehouse";

    public static void Seed(AppDbContext db, IConfiguration configuration)
    {
        var code = configuration["Seed:Warehouse:Code"] is { Length: > 0 } c ? c : DefaultCode;
        var name = configuration["Seed:Warehouse:Name"] is { Length: > 0 } n ? n : DefaultName;

        // The Code column is NOCASE, so this comparison is case-insensitive in SQLite.
        if (db.Warehouses.Any(w => w.Code == code))
        {
            return;
        }

        db.Warehouses.Add(Warehouse.Create(code, name));
        db.SaveChanges();
    }
}
