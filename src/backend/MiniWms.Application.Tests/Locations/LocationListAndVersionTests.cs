using System.Reflection;
using MiniWms.Application.Common;
using MiniWms.Application.Common.Paging;
using MiniWms.Application.Locations;
using MiniWms.Domain.Locations;

namespace MiniWms.Application.Tests.Locations;

/// <summary>FEAT-001 AC 11 and 12 (list queries), AC 13 (client version reaches the store).</summary>
public class LocationListAndVersionTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private static (ListZonesHandler Handler, FakeLocationQueries Queries) ZoneLister()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        return (new ListZonesHandler(queries), queries);
    }

    private static (ListBinsHandler Handler, FakeLocationQueries Queries) BinLister()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        return (new ListBinsHandler(queries), queries);
    }

    // ---- AC 11 / 12: rejections

    [Theory]
    [InlineData(0, 25, null, null, "page")]
    [InlineData(-1, 25, null, null, "page")]
    [InlineData(1, 0, null, null, "pageSize")]
    [InlineData(1, 101, null, null, "pageSize")]
    [InlineData(1, 25, "color", null, "sort")]
    [InlineData(1, 25, "code,name", null, "sort")]
    [InlineData(1, 25, "zone", null, "sort")]
    [InlineData(1, 25, null, "Cold", "type")]
    [InlineData(1, 25, null, "1", "type")]
    [InlineData(1, 25, null, "storage", "type")]
    [Trait("AC", "FEAT-001.11")]
    public async Task List_zones_with_a_bad_parameter_is_rejected_under_that_field_and_the_query_is_not_run(
        int page, int pageSize, string? sort, string? type, string field)
    {
        var (handler, queries) = ZoneLister();

        var fields = await Failures.FieldsAsync(() => handler.HandleAsync(new ListZonesQuery(page, pageSize, sort, null, type, null), Ct));

        Assert.Equal([field], fields);
        Assert.Null(queries.ZoneList);
    }

    [Theory]
    [InlineData(0, 25, null, null, "page")]
    [InlineData(1, 0, null, null, "pageSize")]
    [InlineData(1, 101, null, null, "pageSize")]
    [InlineData(1, 25, "type", null, "sort")]
    [InlineData(1, 25, "code,zone", null, "sort")]
    [InlineData(1, 25, null, 0, "zoneId")]
    [InlineData(1, 25, null, -4, "zoneId")]
    [Trait("AC", "FEAT-001.11")]
    public async Task List_bins_with_a_bad_parameter_is_rejected_under_that_field_and_the_query_is_not_run(
        int page, int pageSize, string? sort, int? zoneId, string field)
    {
        var (handler, queries) = BinLister();

        var fields = await Failures.FieldsAsync(() => handler.HandleAsync(new ListBinsQuery(page, pageSize, sort, null, zoneId, null), Ct));

        Assert.Equal([field], fields);
        Assert.Null(queries.BinList);
    }

    [Fact]
    [Trait("AC", "FEAT-001.12")]
    public async Task List_zones_and_bins_with_q_over_100_characters_are_rejected_under_q()
    {
        var (zones, zoneQueries) = ZoneLister();
        var (bins, binQueries) = BinLister();
        var tooLong = new string('x', 101);

        var zoneFields = await Failures.FieldsAsync(() => zones.HandleAsync(new ListZonesQuery(Q: tooLong), Ct));
        var binFields = await Failures.FieldsAsync(() => bins.HandleAsync(new ListBinsQuery(Q: tooLong), Ct));

        Assert.Equal(["q"], zoneFields);
        Assert.Equal(["q"], binFields);
        Assert.Null(zoneQueries.ZoneList);
        Assert.Null(binQueries.BinList);
    }

    // ---- AC 11 / 12: parsed values reach the queries

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task List_zones_passes_paging_filters_and_the_parsed_sort_to_the_query()
    {
        var (handler, queries) = ZoneLister();

        var result = await handler.HandleAsync(new ListZonesQuery(2, 50, "-Type", true, "Shipping", null), Ct);

        var call = queries.ZoneList!;
        Assert.Equal(2, call.Paging.Page);
        Assert.Equal(50, call.Paging.PageSize);
        Assert.Equal("type", call.Sort!.Field);
        Assert.True(call.Sort.Descending);
        Assert.True(call.IsActive);
        Assert.Equal(ZoneType.Shipping, call.Type);
        Assert.Null(call.Q);
        Assert.Equal(2, result.Page);
        Assert.Equal(50, result.PageSize);
    }

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task List_zones_without_options_uses_the_defaults_and_no_sort_or_filters()
    {
        var (handler, queries) = ZoneLister();

        await handler.HandleAsync(new ListZonesQuery(), Ct);

        var call = queries.ZoneList!;
        Assert.Equal(1, call.Paging.Page);
        Assert.Equal(25, call.Paging.PageSize);
        Assert.Null(call.Sort);
        Assert.Null(call.IsActive);
        Assert.Null(call.Type);
    }

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task List_bins_passes_paging_filters_and_the_parsed_sort_to_the_query()
    {
        var (handler, queries) = BinLister();

        await handler.HandleAsync(new ListBinsQuery(3, 100, "-ZONE", false, 7, null), Ct);

        var call = queries.BinList!;
        Assert.Equal(3, call.Paging.Page);
        Assert.Equal(100, call.Paging.PageSize);
        Assert.Equal("zone", call.Sort!.Field);
        Assert.True(call.Sort.Descending);
        Assert.False(call.IsActive);
        Assert.Equal(7, call.ZoneId);
    }

    [Theory]
    [InlineData("code")]
    [InlineData("name")]
    [InlineData("type")]
    [InlineData("isActive")]
    [InlineData("ISACTIVE")]
    [Trait("AC", "FEAT-001.11")]
    public async Task List_zones_accepts_each_allowed_sort_field_in_either_direction_and_any_case(string field)
    {
        var (handler, queries) = ZoneLister();

        await handler.HandleAsync(new ListZonesQuery(Sort: field), Ct);
        Assert.False(queries.ZoneList!.Sort!.Descending);
        await handler.HandleAsync(new ListZonesQuery(Sort: "-" + field), Ct);

        Assert.True(queries.ZoneList.Sort!.Descending);
        Assert.Equal(field, queries.ZoneList.Sort.Field, ignoreCase: true);
    }

    [Theory]
    [InlineData("code")]
    [InlineData("name")]
    [InlineData("zone")]
    [InlineData("isActive")]
    [InlineData("CODE")]
    [Trait("AC", "FEAT-001.11")]
    public async Task List_bins_accepts_each_allowed_sort_field_in_either_direction_and_any_case(string field)
    {
        var (handler, queries) = BinLister();

        await handler.HandleAsync(new ListBinsQuery(Sort: field), Ct);
        Assert.False(queries.BinList!.Sort!.Descending);
        await handler.HandleAsync(new ListBinsQuery(Sort: "-" + field), Ct);

        Assert.True(queries.BinList.Sort!.Descending);
        Assert.Equal(field, queries.BinList.Sort.Field, ignoreCase: true);
    }

    [Theory]
    [InlineData("  tool ", "tool")]
    [InlineData("tool", "tool")]
    [InlineData("   ", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    [Trait("AC", "FEAT-001.12")]
    public async Task List_zones_and_bins_pass_q_trimmed_and_blank_q_as_null(string? q, string? expected)
    {
        var (zones, zoneQueries) = ZoneLister();
        var (bins, binQueries) = BinLister();

        await zones.HandleAsync(new ListZonesQuery(Q: q), Ct);
        await bins.HandleAsync(new ListBinsQuery(Q: q), Ct);

        Assert.Equal(expected, zoneQueries.ZoneList!.Q);
        Assert.Equal(expected, binQueries.BinList!.Q);
    }

    [Fact]
    [Trait("AC", "FEAT-001.12")]
    public async Task List_accepts_q_of_exactly_100_characters()
    {
        var (zones, queries) = ZoneLister();

        await zones.HandleAsync(new ListZonesQuery(Q: new string('x', 100)), Ct);

        Assert.Equal(100, queries.ZoneList!.Q!.Length);
    }

    // ---- AC 13: the client version reaches the store

    [Fact]
    [Trait("AC", "FEAT-001.13")]
    public async Task Every_update_and_deactivate_passes_the_client_version_to_SaveAsync()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();
        var zone = store.SeedZone("Z1");
        var zone2 = store.SeedZone("Z2");
        var bin = store.SeedBin(zone, "A-01");
        var bin2 = store.SeedBin(zone, "A-02");

        await new RenameWarehouseHandler(store, queries).HandleAsync(new RenameWarehouseCommand("Central", 11), Ct);
        await new UpdateZoneHandler(store, queries).HandleAsync(new UpdateZoneCommand(zone.Id, "Name", null, 12), Ct);
        await new UpdateBinHandler(store, queries).HandleAsync(new UpdateBinCommand(bin.Id, "Name", 13), Ct);
        await new DeactivateBinHandler(store, queries, new FakeStockQuery()).HandleAsync(new DeactivateBinCommand(bin2.Id, 14), Ct);
        await new DeactivateZoneHandler(store, queries).HandleAsync(new DeactivateZoneCommand(zone2.Id, 15), Ct);

        Assert.Equal([11, 12, 13, 14, 15], store.Saves.Select(s => s.ClientVersion));
        Assert.Same(store.Warehouse, store.Saves[0].Entity);
        Assert.Same(zone, store.Saves[1].Entity);
        Assert.Same(bin, store.Saves[2].Entity);
        Assert.Same(bin2, store.Saves[3].Entity);
        Assert.Same(zone2, store.Saves[4].Entity);
    }

    [Fact]
    [Trait("AC", "FEAT-001.13")]
    public async Task Create_zone_and_create_bin_pass_no_client_version_to_SaveAsync()
    {
        var (store, queries) = Seed.WarehouseWithVersion3();

        var zone = await new CreateZoneHandler(store, queries).HandleAsync(new CreateZoneCommand("Z1", "Zone", "Storage"), Ct);
        await new CreateBinHandler(store, queries).HandleAsync(new CreateBinCommand(zone.Id, "A-01", "Bin"), Ct);

        Assert.Equal(2, store.Saves.Count);
        Assert.All(store.Saves, s => Assert.Null(s.ClientVersion));
    }

    // ---- trait presence

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [Trait("AC", "FEAT-001.15")]
    public void This_assembly_has_proving_tests_tagged_for_the_acceptance_criterion(int ac)
    {
        var expected = $"FEAT-001.{ac}";

        var tagged = typeof(LocationListAndVersionTests).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(LocationListAndVersionTests).Namespace)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(m => m.CustomAttributes)
            .Count(a => a.AttributeType.Name == "TraitAttribute"
                && (string?)a.ConstructorArguments[0].Value == "AC"
                && (string?)a.ConstructorArguments[1].Value == expected);

        Assert.True(tagged > 0, $"No test in the Locations folder is tagged [Trait(\"AC\", \"{expected}\")].");
    }
}
