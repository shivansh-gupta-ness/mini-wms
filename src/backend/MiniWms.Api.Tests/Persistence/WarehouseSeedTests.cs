using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>TASK-005 AC2: the Development host seeds exactly one warehouse MAIN, and starting again does not add a second.</summary>
public class WarehouseSeedTests
{
    [Fact]
    [Trait("AC", "TASK-005.2")]
    public void Fresh_host_creates_exactly_one_warehouse_MAIN_named_Main_warehouse()
    {
        using var factory = new Host.ApiFactory();

        var warehouses = ReadWarehouses(factory);

        var warehouse = Assert.Single(warehouses);
        Assert.Equal("MAIN", warehouse.Code);
        Assert.Equal("Main warehouse", warehouse.Name);
        Assert.True(warehouse.IsActive);
    }

    [Fact]
    [Trait("AC", "TASK-005.2")]
    public void Seeded_warehouse_has_audit_fields_and_Version_1()
    {
        using var factory = new Host.ApiFactory();

        var warehouse = Assert.Single(ReadWarehouses(factory));

        Assert.Equal(1, warehouse.Version);
        Assert.False(string.IsNullOrWhiteSpace(warehouse.CreatedBy));
        Assert.NotEqual(default, warehouse.CreatedAt);
    }

    [Fact]
    [Trait("AC", "TASK-005.2")]
    public void Starting_a_second_host_on_the_same_database_file_does_not_add_a_second_warehouse()
    {
        var path = TempDatabaseFiles.NewPath();
        try
        {
            using (var first = CreateFactory(path))
            {
                Assert.Single(ReadWarehouses(first));
            }

            using var second = CreateFactory(path);

            var warehouse = Assert.Single(ReadWarehouses(second));
            Assert.Equal("MAIN", warehouse.Code);
        }
        finally
        {
            TempDatabaseFiles.Delete(path);
        }
    }

    [Fact]
    [Trait("AC", "TASK-005.2")]
    public void Running_the_seeder_twice_keeps_one_warehouse()
    {
        using var database = new TemporaryDatabase();
        var configuration = new ConfigurationBuilder().Build();

        WarehouseSeeder.Seed(database.CreateContext(), configuration);
        WarehouseSeeder.Seed(database.CreateContext(), configuration);

        var warehouse = Assert.Single(database.CreateContext().Warehouses.AsNoTracking().ToList());
        Assert.Equal("MAIN", warehouse.Code);
        Assert.Equal("Main warehouse", warehouse.Name);
    }

    [Fact]
    [Trait("AC", "TASK-005.2")]
    public void Seeder_does_not_duplicate_an_existing_warehouse_whose_code_differs_only_by_case()
    {
        using var database = new TemporaryDatabase();
        SchemaTests.InsertWarehouse(database.CreateContext(), "main");

        WarehouseSeeder.Seed(database.CreateContext(), new ConfigurationBuilder().Build());

        var warehouse = Assert.Single(database.CreateContext().Warehouses.AsNoTracking().ToList());
        Assert.Equal("main", warehouse.Code);
    }

    private static WebApplicationFactory<Program> CreateFactory(string databasePath) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = TempDatabaseFiles.ConnectionStringFor(databasePath),
                }));
        });

    private static List<MiniWms.Domain.Locations.Warehouse> ReadWarehouses<T>(WebApplicationFactory<T> factory)
        where T : class
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return context.Warehouses.AsNoTracking().ToList();
    }
}
