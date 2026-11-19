using MiniWms.Application.Locations;
using MiniWms.Domain.Common;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Tests.Locations;

/// <summary>FEAT-001 AC 2 (warehouse rename), AC 3 and 4 (zone create), AC 7 (zone update) at use-case level.</summary>
public class WarehouseAndZoneUseCaseTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    // ---- AC 2: warehouse rename

    [Fact]
    [Trait("AC", "FEAT-001.2")]
    public async Task Rename_warehouse_saves_the_new_name_and_keeps_the_code()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var dto = await new RenameWarehouseHandler(store, queries).HandleAsync(new RenameWarehouseCommand(" Central ", 3), Ct);

        Assert.NotNull(dto);
        Assert.Equal("Central", dto.Name);
        Assert.Equal("MAIN", dto.Code);
        Assert.True(store.SaveCalled);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("AC", "FEAT-001.2")]
    public async Task Rename_warehouse_with_a_blank_name_is_rejected_under_name_and_not_saved(string? name)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var fields = await Failures.FieldsAsync(() => new RenameWarehouseHandler(store, queries).HandleAsync(new RenameWarehouseCommand(name, 3), Ct));

        Assert.Equal(["name"], fields);
        Assert.False(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.2")]
    public async Task Rename_warehouse_with_a_name_over_100_characters_is_rejected_under_name_and_not_saved()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var fields = await Failures.FieldsAsync(() => new RenameWarehouseHandler(store, queries).HandleAsync(new RenameWarehouseCommand(new string('N', 101), 3), Ct));

        Assert.Equal(["name"], fields);
        Assert.False(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.2")]
    public async Task Rename_warehouse_accepts_a_name_of_exactly_100_characters()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var dto = await new RenameWarehouseHandler(store, queries).HandleAsync(new RenameWarehouseCommand(new string('N', 100), 3), Ct);

        Assert.Equal(100, dto!.Name.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("AC", "FEAT-001.2")]
    public async Task Rename_warehouse_without_a_version_is_rejected_under_version(int version)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var fields = await Failures.FieldsAsync(() => new RenameWarehouseHandler(store, queries).HandleAsync(new RenameWarehouseCommand("Central", version), Ct));

        Assert.Equal(["version"], fields);
        Assert.False(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.2")]
    public async Task Rename_warehouse_returns_null_when_there_is_no_warehouse()
    {
        var store = new FakeLocationStore();

        var dto = await new RenameWarehouseHandler(store, new FakeLocationQueries(store)).HandleAsync(new RenameWarehouseCommand("Central", 1), Ct);

        Assert.Null(dto);
        Assert.False(store.SaveCalled);
    }

    // ---- AC 3: zone create, happy path

    [Theory]
    [InlineData("Receiving", ZoneType.Receiving)]
    [InlineData("Storage", ZoneType.Storage)]
    [InlineData("Shipping", ZoneType.Shipping)]
    [Trait("AC", "FEAT-001.3")]
    public async Task Create_zone_returns_an_Active_zone_with_the_trimmed_values(string type, ZoneType expected)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var dto = await new CreateZoneHandler(store, queries).HandleAsync(new CreateZoneCommand("  Z1 ", "  Zone one ", type), Ct);

        Assert.Equal("Z1", dto.Code);
        Assert.Equal("Zone one", dto.Name);
        Assert.Equal(expected.ToString(), dto.Type);
        Assert.True(dto.IsActive);
        Assert.Equal(1, dto.WarehouseId);
        Assert.Equal(0, dto.BinCount);
        Assert.Same(store.Zones.Single(), store.Saves.Single().Entity);
    }

    // ---- AC 4: zone create, rejections

    [Theory]
    [InlineData(null, "Name", "Storage", "code")]
    [InlineData("  ", "Name", "Storage", "code")]
    [InlineData("Z1", null, "Storage", "name")]
    [InlineData("Z1", " ", "Storage", "name")]
    [InlineData("Z1", "Name", null, "type")]
    [InlineData("Z1", "Name", "", "type")]
    [InlineData("Z1", "Name", "Cold", "type")]
    [InlineData("Z1", "Name", "1", "type")]
    [InlineData("Z1", "Name", "storage", "type")]
    [InlineData("Z1", "Name", "SHIPPING", "type")]
    [InlineData("123456789012345678901", "Name", "Storage", "code")]
    [Trait("AC", "FEAT-001.4")]
    public async Task Create_zone_with_a_bad_field_is_rejected_under_that_field_and_nothing_is_saved(
        string? code, string? name, string? type, string field)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var fields = await Failures.FieldsAsync(() => new CreateZoneHandler(store, queries).HandleAsync(new CreateZoneCommand(code, name, type), Ct));

        Assert.Equal([field], fields);
        Assert.False(store.SaveCalled);
        Assert.Empty(store.Zones);
    }

    [Fact]
    [Trait("AC", "FEAT-001.4")]
    public async Task Create_zone_with_all_fields_missing_reports_each_field()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var fields = await Failures.FieldsAsync(() => new CreateZoneHandler(store, queries).HandleAsync(new CreateZoneCommand(null, null, null), Ct));

        Assert.Equal(["code", "name", "type"], fields.Order());
    }

    [Fact]
    [Trait("AC", "FEAT-001.3")]
    [Trait("AC", "FEAT-001.4")]
    public async Task Create_zone_accepts_a_code_of_exactly_20_characters()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var dto = await new CreateZoneHandler(store, queries).HandleAsync(new CreateZoneCommand(new string('C', 20), "Name", "Storage"), Ct);

        Assert.Equal(20, dto.Code.Length);
    }

    [Theory]
    [InlineData("Z1")]
    [InlineData("z1")]
    [InlineData(" z1 ")]
    [Trait("AC", "FEAT-001.4")]
    public async Task Create_zone_with_a_code_already_used_without_regard_to_case_is_rejected_under_code(string code)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        store.SeedZone("Z1");

        var fields = await Failures.FieldsAsync(() => new CreateZoneHandler(store, queries).HandleAsync(new CreateZoneCommand(code, "Other", "Storage"), Ct));

        Assert.Equal(["code"], fields);
        Assert.False(store.SaveCalled);
        Assert.Single(store.Zones);
    }

    [Fact]
    [Trait("AC", "FEAT-001.4")]
    public async Task Create_zone_code_clashing_with_an_Inactive_zone_is_still_rejected()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        store.SeedZone("Z1", active: false);

        var fields = await Failures.FieldsAsync(() => new CreateZoneHandler(store, queries).HandleAsync(new CreateZoneCommand("Z1", "Other", "Storage"), Ct));

        Assert.Equal(["code"], fields);
    }

    // ---- AC 7: zone update

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Update_zone_renames_it_and_keeps_the_code()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1");

        var dto = await new UpdateZoneHandler(store, queries).HandleAsync(new UpdateZoneCommand(zone.Id, " New name ", null, 1), Ct);

        Assert.Equal("New name", dto!.Name);
        Assert.Equal("Z1", dto.Code);
        Assert.True(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Update_zone_changes_the_type_while_it_has_no_bins()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1", ZoneType.Storage);

        var dto = await new UpdateZoneHandler(store, queries).HandleAsync(new UpdateZoneCommand(zone.Id, "Name", "Shipping", 1), Ct);

        Assert.Equal("Shipping", dto!.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("AC", "FEAT-001.7")]
    public async Task Update_zone_type_change_with_bins_is_a_DomainException_whether_the_bins_are_active_or_not(bool binActive)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1", ZoneType.Storage);
        store.SeedBin(zone, "B1", binActive);

        await Assert.ThrowsAsync<DomainException>(() =>
            new UpdateZoneHandler(store, queries).HandleAsync(new UpdateZoneCommand(zone.Id, "Name", "Shipping", 1), Ct));

        Assert.Equal(ZoneType.Storage, zone.Type);
        Assert.False(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Update_zone_with_the_same_type_is_a_no_op_even_with_bins()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1", ZoneType.Storage);
        store.SeedBin(zone, "B1");

        var dto = await new UpdateZoneHandler(store, queries).HandleAsync(new UpdateZoneCommand(zone.Id, "Renamed", "Storage", 1), Ct);

        Assert.Equal("Storage", dto!.Type);
        Assert.Equal("Renamed", dto.Name);
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Update_zone_returns_null_when_the_zone_does_not_exist()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var dto = await new UpdateZoneHandler(store, queries).HandleAsync(new UpdateZoneCommand(999, "Name", null, 1), Ct);

        Assert.Null(dto);
        Assert.False(store.SaveCalled);
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Update_zone_without_a_version_is_rejected_under_version()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1");

        var fields = await Failures.FieldsAsync(() => new UpdateZoneHandler(store, queries).HandleAsync(new UpdateZoneCommand(zone.Id, "Name", null, 0), Ct));

        Assert.Equal(["version"], fields);
        Assert.False(store.SaveCalled);
    }

    [Theory]
    [InlineData("1", "type")]
    [InlineData("storage", "type")]
    [InlineData(null, "name")]
    [Trait("AC", "FEAT-001.7")]
    public async Task Update_zone_with_a_bad_field_is_rejected_under_that_field(string? typeOrNull, string field)
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1");
        var command = field == "name"
            ? new UpdateZoneCommand(zone.Id, typeOrNull, null, 1)
            : new UpdateZoneCommand(zone.Id, "Name", typeOrNull, 1);

        var fields = await Failures.FieldsAsync(() => new UpdateZoneHandler(store, queries).HandleAsync(command, Ct));

        Assert.Equal([field], fields);
    }
}
