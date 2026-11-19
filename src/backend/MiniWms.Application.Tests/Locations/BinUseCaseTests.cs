using MiniWms.Application.Locations;
using MiniWms.Domain.Common;

namespace MiniWms.Application.Tests.Locations;

/// <summary>FEAT-001 AC 5 and 6 (bin create), AC 7 (bin update), AC 8 and 9 (bin deactivate), AC 10 (zone deactivate).</summary>
public class BinUseCaseTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    // ---- AC 5

    [Fact]
    [Trait("AC", "FEAT-001.5")]
    public async Task Create_bin_returns_an_Active_bin_with_zoneCode_and_the_path()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1");

        var dto = await new CreateBinHandler(store, queries).HandleAsync(new CreateBinCommand(zone.Id, " A-01 ", " First "), Ct);

        Assert.Equal("A-01", dto.Code);
        Assert.Equal("First", dto.Name);
        Assert.Equal(zone.Id, dto.ZoneId);
        Assert.Equal("Z1", dto.ZoneCode);
        Assert.Equal("Z1/A-01", dto.Path);
        Assert.True(dto.IsActive);
        Assert.Same(store.Bins.Single(), store.Saves.Single().Entity);
    }

    [Fact]
    [Trait("AC", "FEAT-001.5")]
    public async Task Create_bin_accepts_a_code_of_exactly_30_characters()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1");

        var dto = await new CreateBinHandler(store, queries).HandleAsync(new CreateBinCommand(zone.Id, new string('B', 30), "Name"), Ct);

        Assert.Equal(30, dto.Code.Length);
    }

    // ---- AC 6

    [Theory]
    [InlineData(null, "Name", "code")]
    [InlineData(" ", "Name", "code")]
    [InlineData("A-01", null, "name")]
    [InlineData("A-01", "  ", "name")]
    [InlineData("1234567890123456789012345678901", "Name", "code")]
    [InlineData("A-01", "NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNN", "name")]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_bin_with_a_bad_field_is_rejected_under_that_field_and_nothing_is_saved(string? code, string? name, string field)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1");

        var fields = await Failures.FieldsAsync(() => new CreateBinHandler(store, queries).HandleAsync(new CreateBinCommand(zone.Id, code, name), Ct));

        Assert.Equal([field], fields);
        Assert.False(store.SaveCalled);
        Assert.Empty(store.Bins);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(999)]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_bin_with_a_missing_or_unknown_zone_is_rejected_under_zoneId(int zoneId)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        store.SeedZone("Z1");

        var fields = await Failures.FieldsAsync(() => new CreateBinHandler(store, queries).HandleAsync(new CreateBinCommand(zoneId, "A-01", "Name"), Ct));

        Assert.Equal(["zoneId"], fields);
        Assert.False(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_bin_under_an_Inactive_zone_is_rejected_under_zoneId()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1", active: false);

        var fields = await Failures.FieldsAsync(() => new CreateBinHandler(store, queries).HandleAsync(new CreateBinCommand(zone.Id, "A-01", "Name"), Ct));

        Assert.Equal(["zoneId"], fields);
        Assert.False(store.SaveCalled);
        Assert.Empty(store.Bins);
    }

    [Theory]
    [InlineData("A-01")]
    [InlineData("a-01")]
    [InlineData(" a-01 ")]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_bin_with_a_code_used_in_another_zone_of_the_warehouse_is_rejected_under_code_without_regard_to_case(string code)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone1 = store.SeedZone("Z1");
        var zone2 = store.SeedZone("Z2");
        store.SeedBin(zone1, "A-01");

        var fields = await Failures.FieldsAsync(() => new CreateBinHandler(store, queries).HandleAsync(new CreateBinCommand(zone2.Id, code, "Name"), Ct));

        Assert.Equal(["code"], fields);
        Assert.False(store.SaveCalled);
        Assert.Single(store.Bins);
    }

    // ---- AC 7: bin update

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Update_bin_renames_it_and_keeps_the_code_and_path()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var bin = store.SeedBin(store.SeedZone("Z1"), "A-01");

        var dto = await new UpdateBinHandler(store, queries).HandleAsync(new UpdateBinCommand(bin.Id, " Renamed ", 1), Ct);

        Assert.Equal("Renamed", dto!.Name);
        Assert.Equal("A-01", dto.Code);
        Assert.Equal("Z1/A-01", dto.Path);
        Assert.True(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Update_bin_returns_null_when_the_bin_does_not_exist()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var dto = await new UpdateBinHandler(store, queries).HandleAsync(new UpdateBinCommand(999, "Name", 1), Ct);

        Assert.Null(dto);
        Assert.False(store.SaveCalled);
    }

    [Theory]
    [InlineData(null, 1, "name")]
    [InlineData("Name", 0, "version")]
    [Trait("AC", "FEAT-001.7")]
    public async Task Update_bin_with_a_missing_name_or_version_is_rejected_under_that_field(string? name, int version, string field)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var bin = store.SeedBin(store.SeedZone("Z1"), "A-01");

        var fields = await Failures.FieldsAsync(() => new UpdateBinHandler(store, queries).HandleAsync(new UpdateBinCommand(bin.Id, name, version), Ct));

        Assert.Equal([field], fields);
        Assert.False(store.SaveCalled);
    }

    // ---- AC 8 and 9: bin deactivate

    [Fact]
    [Trait("AC", "FEAT-001.8")]
    public async Task Deactivate_bin_with_no_stock_makes_it_Inactive_and_saves()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var bin = store.SeedBin(store.SeedZone("Z1"), "A-01");

        var dto = await new DeactivateBinHandler(store, queries, new FakeStockQuery()).HandleAsync(new DeactivateBinCommand(bin.Id, 1), Ct);

        Assert.False(dto!.IsActive);
        Assert.False(bin.IsActive);
        Assert.True(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.9")]
    public async Task Deactivate_bin_with_stock_on_hand_is_a_DomainException_naming_the_quantity_and_is_not_saved()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var bin = store.SeedBin(store.SeedZone("Z1"), "A-01");
        var stock = new FakeStockQuery { BinStock = { [bin.Id] = 42 } };

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            new DeactivateBinHandler(store, queries, stock).HandleAsync(new DeactivateBinCommand(bin.Id, 1), Ct));

        Assert.Contains("42", ex.Message);
        Assert.True(bin.IsActive);
        Assert.False(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.8")]
    [Trait("AC", "FEAT-001.9")]
    public async Task Deactivate_bin_asks_the_stock_query_about_that_bin_only()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1");
        var bin = store.SeedBin(zone, "A-01");
        var other = store.SeedBin(zone, "A-02");
        var stock = new FakeStockQuery { BinStock = { [other.Id] = 5 } };

        var dto = await new DeactivateBinHandler(store, queries, stock).HandleAsync(new DeactivateBinCommand(bin.Id, 1), Ct);

        Assert.False(dto!.IsActive);
        Assert.Equal(1, stock.BinCalls);
    }

    [Fact]
    [Trait("AC", "FEAT-001.8")]
    public async Task Deactivate_bin_that_is_already_Inactive_is_returned_unchanged_without_a_save_or_a_stock_check()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var bin = store.SeedBin(store.SeedZone("Z1"), "A-01", active: false);
        var stock = new FakeStockQuery { BinStock = { [bin.Id] = 9 } };

        var dto = await new DeactivateBinHandler(store, queries, stock).HandleAsync(new DeactivateBinCommand(bin.Id, 1), Ct);

        Assert.False(dto!.IsActive);
        Assert.False(store.SaveCalled);
        Assert.Equal(0, stock.BinCalls);
    }

    [Fact]
    [Trait("AC", "FEAT-001.8")]
    public async Task Deactivate_bin_returns_null_when_missing_and_rejects_a_missing_version()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var bin = store.SeedBin(store.SeedZone("Z1"), "A-01");
        var handler = new DeactivateBinHandler(store, queries, new FakeStockQuery());

        var missing = await handler.HandleAsync(new DeactivateBinCommand(999, 1), Ct);
        var fields = await Failures.FieldsAsync(() => handler.HandleAsync(new DeactivateBinCommand(bin.Id, 0), Ct));

        Assert.Null(missing);
        Assert.Equal(["version"], fields);
        Assert.False(store.SaveCalled);
    }

    // ---- AC 10: zone deactivate

    [Fact]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivate_zone_with_an_Active_bin_is_a_DomainException_and_is_not_saved()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1");
        store.SeedBin(zone, "A-01");

        await Assert.ThrowsAsync<DomainException>(() =>
            new DeactivateZoneHandler(store, queries).HandleAsync(new DeactivateZoneCommand(zone.Id, 1), Ct));

        Assert.True(zone.IsActive);
        Assert.False(store.SaveCalled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivate_zone_with_no_bins_or_only_Inactive_bins_makes_it_Inactive(bool withInactiveBin)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1");
        if (withInactiveBin)
        {
            store.SeedBin(zone, "A-01", active: false);
        }

        var dto = await new DeactivateZoneHandler(store, queries).HandleAsync(new DeactivateZoneCommand(zone.Id, 1), Ct);

        Assert.False(dto!.IsActive);
        Assert.True(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivate_zone_that_is_already_Inactive_is_returned_unchanged_without_a_save()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1", active: false);

        var dto = await new DeactivateZoneHandler(store, queries).HandleAsync(new DeactivateZoneCommand(zone.Id, 1), Ct);

        Assert.False(dto!.IsActive);
        Assert.False(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivate_zone_returns_null_when_missing_and_rejects_a_missing_version()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1");
        var handler = new DeactivateZoneHandler(store, queries);

        var missing = await handler.HandleAsync(new DeactivateZoneCommand(999, 1), Ct);
        var fields = await Failures.FieldsAsync(() => handler.HandleAsync(new DeactivateZoneCommand(zone.Id, 0), Ct));

        Assert.Null(missing);
        Assert.Equal(["version"], fields);
    }
}
