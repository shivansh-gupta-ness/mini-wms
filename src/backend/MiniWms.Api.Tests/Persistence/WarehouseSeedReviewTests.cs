using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>Seeder behaviour found in code review: spaces in the setting, a changed code and bad settings.</summary>
public class WarehouseSeedReviewTests
{
    private static IConfiguration Config(string? code, string? name = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:Warehouse:Code"] = code,
            ["Seed:Warehouse:Name"] = name,
        }).Build();

    [Fact]
    [Trait("AC", "TASK-005.2")]
    public void A_code_with_spaces_is_trimmed_and_a_second_start_does_not_fail()
    {
        using var database = new TemporaryDatabase();
        var configuration = Config(" MAIN ");

        WarehouseSeeder.Seed(database.CreateContext(), configuration);
        WarehouseSeeder.Seed(database.CreateContext(), configuration);

        Assert.Equal("MAIN", Assert.Single(database.CreateContext().Warehouses.AsNoTracking().ToList()).Code);
    }

    [Fact]
    [Trait("AC", "TASK-005.2")]
    public void Changing_the_configured_code_later_does_not_add_a_second_warehouse()
    {
        using var database = new TemporaryDatabase();

        WarehouseSeeder.Seed(database.CreateContext(), Config("MAIN"));
        WarehouseSeeder.Seed(database.CreateContext(), Config("WH1"));

        Assert.Equal("MAIN", Assert.Single(database.CreateContext().Warehouses.AsNoTracking().ToList()).Code);
    }

    [Theory]
    [InlineData("THIS-CODE-IS-MUCH-TOO-LONG", null)]
    [InlineData("MAIN", "x")]
    [Trait("AC", "TASK-005.2")]
    public void An_invalid_seed_setting_fails_with_a_message_that_names_the_setting(string code, string? name)
    {
        using var database = new TemporaryDatabase();
        var tooLongName = name is null ? null : new string(name[0], 101);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            WarehouseSeeder.Seed(database.CreateContext(), Config(code, tooLongName)));

        Assert.Contains("Seed:Warehouse", ex.Message);
    }

    [Fact]
    [Trait("AC", "TASK-005.2")]
    public void Blank_settings_fall_back_to_the_defaults()
    {
        using var database = new TemporaryDatabase();

        WarehouseSeeder.Seed(database.CreateContext(), Config("  ", ""));

        var warehouse = Assert.Single(database.CreateContext().Warehouses.AsNoTracking().ToList());
        Assert.Equal("MAIN", warehouse.Code);
        Assert.Equal("Main warehouse", warehouse.Name);
    }
}
