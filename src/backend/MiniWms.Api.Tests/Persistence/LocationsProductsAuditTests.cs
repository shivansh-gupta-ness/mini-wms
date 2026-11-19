using Microsoft.EntityFrameworkCore;
using MiniWms.Domain.Locations;
using MiniWms.Domain.Products;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>The new entities derive from AuditableEntity: SaveChanges stamps audit fields and Version (data-model conventions).</summary>
public class LocationsProductsAuditTests
{
    private static readonly DateTimeOffset CreateTime = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdateTime = new(2026, 3, 2, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    [Trait("AC", "TASK-005.1")]
    public void Adding_warehouse_zone_bin_and_product_stamps_created_fields_and_Version_1()
    {
        using var database = new TemporaryDatabase();
        var context = database.CreateContext(new FakeClock(CreateTime), new FakeCurrentUser("creator"));
        var warehouse = Warehouse.Create("MAIN", "Main warehouse");
        context.Warehouses.Add(warehouse);
        context.SaveChanges();
        var zone = Zone.Create(warehouse.Id, "ZONE-A", "Zone A", ZoneType.Storage);
        context.Zones.Add(zone);
        context.SaveChanges();
        var bin = Bin.Create(zone.Id, "A-01", "Bin");
        var product = Product.Create("SKU-1", "Widget", "EA", 5);
        context.AddRange(bin, product);

        context.SaveChanges();

        foreach (var entity in new Domain.Common.AuditableEntity[] { warehouse, zone, bin, product })
        {
            Assert.Equal(CreateTime, entity.CreatedAt);
            Assert.Equal("creator", entity.CreatedBy);
            Assert.Equal(1, entity.Version);
            Assert.Null(entity.UpdatedAt);
            Assert.Null(entity.UpdatedBy);
        }
    }

    [Fact]
    [Trait("AC", "TASK-005.1")]
    public void Updating_warehouse_zone_bin_and_product_stamps_updated_fields_and_Version_2()
    {
        using var database = new TemporaryDatabase();
        var seed = database.CreateContext(new FakeClock(CreateTime), new FakeCurrentUser("creator"));
        var warehouse = Warehouse.Create("MAIN", "Main warehouse");
        seed.Warehouses.Add(warehouse);
        seed.SaveChanges();
        seed.Zones.Add(Zone.Create(warehouse.Id, "ZONE-A", "Zone A", ZoneType.Storage));
        seed.SaveChanges();
        seed.Bins.Add(Bin.Create(seed.Zones.Single().Id, "A-01", "Bin"));
        seed.Products.Add(Product.Create("SKU-1", "Widget", "EA", 5));
        seed.SaveChanges();

        var context = database.CreateContext(new FakeClock(UpdateTime), new FakeCurrentUser("editor"));
        context.Warehouses.Single().Rename("Renamed warehouse");
        context.Zones.Single().Rename("Renamed zone");
        context.Bins.Single().Rename("Renamed bin");
        context.Products.Single().Update("Renamed product", "BOX", 7);
        context.SaveChanges();

        var read = database.CreateContext();
        var stored = new Domain.Common.AuditableEntity[]
        {
            read.Warehouses.AsNoTracking().Single(),
            read.Zones.AsNoTracking().Single(),
            read.Bins.AsNoTracking().Single(),
            read.Products.AsNoTracking().Single(),
        };
        foreach (var entity in stored)
        {
            Assert.Equal(CreateTime, entity.CreatedAt);
            Assert.Equal("creator", entity.CreatedBy);
            Assert.Equal(UpdateTime, entity.UpdatedAt);
            Assert.Equal("editor", entity.UpdatedBy);
            Assert.Equal(2, entity.Version);
        }
    }

    [Fact]
    [Trait("AC", "TASK-005.1")]
    public void Saving_a_stale_product_raises_DbUpdateConcurrencyException()
    {
        using var database = new TemporaryDatabase();
        var seed = database.CreateContext();
        seed.Products.Add(Product.Create("SKU-1", "Widget", "EA", 0));
        seed.SaveChanges();
        var first = database.CreateContext();
        var stale = database.CreateContext();
        var firstProduct = first.Products.Single();
        var staleProduct = stale.Products.Single();
        firstProduct.Update("First", "EA", 0);
        first.SaveChanges();

        staleProduct.Update("Stale", "EA", 0);

        Assert.Throws<DbUpdateConcurrencyException>(() => stale.SaveChanges());
    }
}
