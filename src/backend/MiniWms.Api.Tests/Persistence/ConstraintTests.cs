using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWms.Domain.Locations;
using MiniWms.Domain.Products;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>TASK-005 AC3 (case-insensitive unique codes), AC4 (foreign keys) and AC5 (zone type), enforced by the database.</summary>
public class ConstraintTests
{
    // AC3
    [Fact]
    [Trait("AC", "TASK-005.3")]
    public void Zone_codes_that_differ_only_by_case_are_rejected()
    {
        using var database = new TemporaryDatabase();
        var warehouseId = SeedWarehouse(database);
        var first = database.CreateContext();
        first.Zones.Add(Zone.Create(warehouseId, "ZONE-A", "First", ZoneType.Storage));
        first.SaveChanges();
        var second = database.CreateContext();
        second.Zones.Add(Zone.Create(warehouseId, "zone-a", "Second", ZoneType.Storage));

        var error = Assert.Throws<DbUpdateException>(() => second.SaveChanges());

        Assert.Equal(SqliteConstraint.Unique, Sqlite(error).SqliteExtendedErrorCode);
    }

    [Fact]
    [Trait("AC", "TASK-005.3")]
    public void Distinct_zone_codes_are_accepted()
    {
        using var database = new TemporaryDatabase();
        var warehouseId = SeedWarehouse(database);
        var context = database.CreateContext();
        context.Zones.Add(Zone.Create(warehouseId, "ZONE-A", "First", ZoneType.Storage));
        context.Zones.Add(Zone.Create(warehouseId, "ZONE-B", "Second", ZoneType.Receiving));

        context.SaveChanges();

        Assert.Equal(2, database.CreateContext().Zones.Count());
    }

    [Fact]
    [Trait("AC", "TASK-005.3")]
    public void Bin_codes_that_differ_only_by_case_are_rejected_even_in_another_zone()
    {
        using var database = new TemporaryDatabase();
        var warehouseId = SeedWarehouse(database);
        var (zoneA, zoneB) = SeedTwoZones(database, warehouseId);
        var first = database.CreateContext();
        first.Bins.Add(Bin.Create(zoneA, "A-01-01", "First"));
        first.SaveChanges();
        var second = database.CreateContext();
        second.Bins.Add(Bin.Create(zoneB, "a-01-01", "Second"));

        var error = Assert.Throws<DbUpdateException>(() => second.SaveChanges());

        Assert.Equal(SqliteConstraint.Unique, Sqlite(error).SqliteExtendedErrorCode);
    }

    [Fact]
    [Trait("AC", "TASK-005.3")]
    public void Distinct_bin_codes_are_accepted()
    {
        using var database = new TemporaryDatabase();
        var warehouseId = SeedWarehouse(database);
        var (zoneA, zoneB) = SeedTwoZones(database, warehouseId);
        var context = database.CreateContext();
        context.Bins.Add(Bin.Create(zoneA, "A-01-01", "First"));
        context.Bins.Add(Bin.Create(zoneB, "B-01-01", "Second"));

        context.SaveChanges();

        Assert.Equal(2, database.CreateContext().Bins.Count());
    }

    [Fact]
    [Trait("AC", "TASK-005.3")]
    public void Product_skus_that_differ_only_by_case_are_rejected()
    {
        using var database = new TemporaryDatabase();
        var first = database.CreateContext();
        first.Products.Add(Product.Create("Widget-1", "Widget", "EA", 0));
        first.SaveChanges();
        var second = database.CreateContext();
        second.Products.Add(Product.Create("WIDGET-1", "Other widget", "EA", 0));

        var error = Assert.Throws<DbUpdateException>(() => second.SaveChanges());

        Assert.Equal(SqliteConstraint.Unique, Sqlite(error).SqliteExtendedErrorCode);
    }

    [Fact]
    [Trait("AC", "TASK-005.3")]
    public void Distinct_product_skus_are_accepted()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();
        context.Products.Add(Product.Create("WIDGET-1", "Widget", "EA", 0));
        context.Products.Add(Product.Create("WIDGET-2", "Widget", "EA", 0));

        context.SaveChanges();

        Assert.Equal(2, database.CreateContext().Products.Count());
    }

    // AC4
    [Fact]
    [Trait("AC", "TASK-005.4")]
    public void Bin_with_a_missing_zone_is_rejected_by_the_foreign_key()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();
        context.Bins.Add(Bin.Create(9999, "A-01-01", "Orphan"));

        var error = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

        Assert.Equal(SqliteConstraint.ForeignKey, Sqlite(error).SqliteExtendedErrorCode);
    }

    [Fact]
    [Trait("AC", "TASK-005.4")]
    public void Zone_with_a_missing_warehouse_is_rejected_by_the_foreign_key()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext();
        context.Zones.Add(Zone.Create(9999, "ZONE-A", "Orphan", ZoneType.Storage));

        var error = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

        Assert.Equal(SqliteConstraint.ForeignKey, Sqlite(error).SqliteExtendedErrorCode);
    }

    [Fact]
    [Trait("AC", "TASK-005.4")]
    public void Bin_with_an_existing_zone_is_accepted()
    {
        using var database = new TemporaryDatabase();
        var warehouseId = SeedWarehouse(database);
        var (zoneA, _) = SeedTwoZones(database, warehouseId);
        var context = database.CreateContext();
        context.Bins.Add(Bin.Create(zoneA, "A-01-01", "Bin"));

        context.SaveChanges();

        Assert.Equal(1, database.CreateContext().Bins.Count());
    }

    // AC5
    [Theory]
    [InlineData("Cold")]
    [InlineData("")]
    [InlineData("storage")]
    [InlineData("Staging")]
    [Trait("AC", "TASK-005.5")]
    public void Zone_with_a_type_outside_the_three_allowed_values_is_rejected_by_the_database(string type)
    {
        using var database = new TemporaryDatabase();
        var warehouseId = SeedWarehouse(database);
        var context = database.CreateContext();

        var error = Assert.Throws<SqliteException>(() => InsertZone(context, warehouseId, "Z1", type));

        Assert.Equal(SqliteConstraint.Check, error.SqliteExtendedErrorCode);
    }

    [Theory]
    [InlineData("Receiving")]
    [InlineData("Storage")]
    [InlineData("Shipping")]
    [Trait("AC", "TASK-005.5")]
    public void Zone_with_an_allowed_type_is_accepted_by_the_database(string type)
    {
        using var database = new TemporaryDatabase();
        var warehouseId = SeedWarehouse(database);
        var context = database.CreateContext();

        InsertZone(context, warehouseId, "Z1", type);

        Assert.Equal(1, context.Zones.Count());
    }

    [Theory]
    [InlineData(ZoneType.Receiving)]
    [InlineData(ZoneType.Storage)]
    [InlineData(ZoneType.Shipping)]
    [Trait("AC", "TASK-005.5")]
    public void Zone_type_is_stored_as_its_name_and_read_back(ZoneType type)
    {
        using var database = new TemporaryDatabase();
        var warehouseId = SeedWarehouse(database);
        var writer = database.CreateContext();
        writer.Zones.Add(Zone.Create(warehouseId, "Z1", "Zone", type));
        writer.SaveChanges();

        var stored = SchemaQuery.Run(database.CreateContext(), "SELECT Type FROM Zones").Single()[0];
        var read = database.CreateContext().Zones.AsNoTracking().Single().Type;

        Assert.Equal(type.ToString(), stored);
        Assert.Equal(type, read);
    }

    private static SqliteException Sqlite(DbUpdateException error) =>
        Assert.IsType<SqliteException>(error.InnerException);

    private static int SeedWarehouse(TemporaryDatabase database)
    {
        var context = database.CreateContext();
        var warehouse = Warehouse.Create("MAIN", "Main warehouse");
        context.Warehouses.Add(warehouse);
        context.SaveChanges();
        return warehouse.Id;
    }

    private static (int ZoneA, int ZoneB) SeedTwoZones(TemporaryDatabase database, int warehouseId)
    {
        var context = database.CreateContext();
        var a = Zone.Create(warehouseId, "ZONE-A", "Zone A", ZoneType.Storage);
        var b = Zone.Create(warehouseId, "ZONE-B", "Zone B", ZoneType.Storage);
        context.Zones.AddRange(a, b);
        context.SaveChanges();
        return (a.Id, b.Id);
    }

    private static void InsertZone(DbContext context, int warehouseId, string code, string type) =>
        context.Database.ExecuteSqlRaw(
            "INSERT INTO Zones (WarehouseId, Code, Name, Type, IsActive, CreatedAt, CreatedBy, Version) VALUES ({0}, {1}, 'Z', {2}, 1, 0, 'sql', 1)",
            warehouseId, code, type);
}
