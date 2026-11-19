using System.Net;
using System.Text.Json;

namespace MiniWms.Api.Tests.Locations;

/// <summary>
/// FEAT-001 AC 11 and 12: paging, sorting, filtering, search and the location tree, on real SQLite (the list and tree
/// queries run as SQL, not in memory).
/// </summary>
public sealed class LocationListApiTests : IDisposable
{
    private const string Zones = "/api/v1/zones";
    private const string Bins = "/api/v1/bins";
    private const string Tree = "/api/v1/locations/tree";

    private readonly LocationsHost host = new();

    public void Dispose() => host.Dispose();

    // Zones, in creation (id) order: ZA Charlie/Shipping, ZB Alpha/Receiving (Inactive), ZC Bravo/Storage, ZD Delta/Storage.
    private async Task SeedZonesAsync()
    {
        await host.CreateZoneAsync("ZA", "Charlie", "Shipping");
        await host.DeactivateZoneAsync(await host.CreateZoneAsync("ZB", "Alpha", "Receiving"));
        await host.CreateZoneAsync("ZC", "Bravo", "Storage");
        await host.CreateZoneAsync("ZD", "Delta", "Storage");
    }

    // Zones M (id 1) and K (id 2); bins in id order: B3 Delta/M, B1 Bravo/K (Inactive), B2 Alpha/M, B4 Charlie/K.
    private async Task<(int M, int K)> SeedBinsAsync()
    {
        var m = LocationsHost.Id(await host.CreateZoneAsync("M"));
        var k = LocationsHost.Id(await host.CreateZoneAsync("K"));
        await host.CreateBinAsync(m, "B3", "Delta");
        await host.DeactivateBinAsync(await host.CreateBinAsync(k, "B1", "Bravo"));
        await host.CreateBinAsync(m, "B2", "Alpha");
        await host.CreateBinAsync(k, "B4", "Charlie");
        return (m, k);
    }

    private async Task<string> CodesAsync(string url) => string.Join(",", LocationsHost.Codes(await host.GetJsonAsync(url)));

    // ---- paging

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task Zones_default_to_page_1_of_25_and_return_exactly_the_four_paging_fields()
    {
        for (var i = 1; i <= 26; i++)
        {
            await host.CreateZoneAsync($"Z{i:00}");
        }

        var page = await host.GetJsonAsync(Zones);

        Assert.Equal(["items", "page", "pageSize", "totalItems"], page.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(1, page.GetProperty("page").GetInt32());
        Assert.Equal(25, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(26, page.GetProperty("totalItems").GetInt32());
        Assert.Equal(25, page.GetProperty("items").GetArrayLength());
    }

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task Bins_default_to_page_1_of_25_and_the_second_page_holds_the_rest()
    {
        var zone = LocationsHost.Id(await host.CreateZoneAsync("Z1"));
        for (var i = 1; i <= 26; i++)
        {
            await host.CreateBinAsync(zone, $"B{i:00}");
        }

        var first = await host.GetJsonAsync(Bins);
        var second = await host.GetJsonAsync($"{Bins}?page=2");

        Assert.Equal(["items", "page", "pageSize", "totalItems"], first.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(25, first.GetProperty("pageSize").GetInt32());
        Assert.Equal(25, first.GetProperty("items").GetArrayLength());
        Assert.Equal(26, first.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, second.GetProperty("page").GetInt32());
        Assert.Equal(["B26"], LocationsHost.Codes(second));
    }

    [Theory]
    [InlineData("zones")]
    [InlineData("bins")]
    [Trait("AC", "FEAT-001.11")]
    public async Task A_page_size_of_100_is_accepted_and_is_echoed(string list)
    {
        var page = await host.GetJsonAsync($"/api/v1/{list}?pageSize=100");

        Assert.Equal(100, page.GetProperty("pageSize").GetInt32());
    }

    [Theory]
    [InlineData("zones", "pageSize=101", "pageSize")]
    [InlineData("zones", "pageSize=0", "pageSize")]
    [InlineData("zones", "pageSize=abc", "pageSize")]
    [InlineData("zones", "page=0", "page")]
    [InlineData("zones", "page=-1", "page")]
    [InlineData("zones", "page=abc", "page")]
    [InlineData("bins", "pageSize=101", "pageSize")]
    [InlineData("bins", "pageSize=0", "pageSize")]
    [InlineData("bins", "pageSize=abc", "pageSize")]
    [InlineData("bins", "page=0", "page")]
    [InlineData("bins", "page=-1", "page")]
    [InlineData("bins", "page=abc", "page")]
    [Trait("AC", "FEAT-001.11")]
    public async Task A_bad_page_or_page_size_is_400_under_that_field(string list, string query, string field)
    {
        using var response = await host.GetAsync($"/api/v1/{list}?{query}");
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, field));
    }

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task A_page_past_the_end_is_200_with_no_items_and_the_real_total()
    {
        await SeedZonesAsync();

        var page = await host.GetJsonAsync($"{Zones}?page=3&pageSize=2");

        Assert.Equal(0, page.GetProperty("items").GetArrayLength());
        Assert.Equal(4, page.GetProperty("totalItems").GetInt32());
    }

    // ---- sorting

    [Theory]
    [InlineData("code", "ZA,ZB,ZC,ZD")]
    [InlineData("-code", "ZD,ZC,ZB,ZA")]
    [InlineData("name", "ZB,ZC,ZA,ZD")]
    [InlineData("-name", "ZD,ZA,ZC,ZB")]
    [InlineData("type", "ZB,ZC,ZD,ZA")]
    [InlineData("-type", "ZA,ZC,ZD,ZB")]
    [InlineData("isActive", "ZB,ZA,ZC,ZD")]
    [InlineData("-isActive", "ZA,ZC,ZD,ZB")]
    [Trait("AC", "FEAT-001.11")]
    public async Task Zones_sort_by_every_allowed_field_with_the_id_as_tie_break(string sort, string expected)
    {
        await SeedZonesAsync();

        Assert.Equal(expected, await CodesAsync($"{Zones}?sort={sort}"));
    }

    [Theory]
    [InlineData("code", "B1,B2,B3,B4")]
    [InlineData("-code", "B4,B3,B2,B1")]
    [InlineData("name", "B2,B1,B4,B3")]
    [InlineData("-name", "B3,B4,B1,B2")]
    [InlineData("zone", "B1,B4,B3,B2")]
    [InlineData("-zone", "B3,B2,B1,B4")]
    [InlineData("isActive", "B1,B3,B2,B4")]
    [InlineData("-isActive", "B3,B2,B4,B1")]
    [Trait("AC", "FEAT-001.11")]
    public async Task Bins_sort_by_every_allowed_field_with_the_id_as_tie_break(string sort, string expected)
    {
        await SeedBinsAsync();

        Assert.Equal(expected, await CodesAsync($"{Bins}?sort={sort}"));
    }

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task Without_a_sort_the_lists_come_back_in_id_order()
    {
        await SeedZonesAsync();
        await SeedBinsAsync();

        Assert.Equal("ZA,ZB,ZC,ZD,M,K", await CodesAsync($"{Zones}?pageSize=100"));
        Assert.Equal("B3,B1,B2,B4", await CodesAsync(Bins));
    }

    [Theory]
    [InlineData("zones", "bogus")]
    [InlineData("zones", "-bogus")]
    [InlineData("zones", "zone")]
    [InlineData("zones", "code,name")]
    [InlineData("bins", "bogus")]
    [InlineData("bins", "type")]
    [InlineData("bins", "code,name")]
    [Trait("AC", "FEAT-001.11")]
    public async Task An_unknown_sort_field_is_400_under_sort(string list, string sort)
    {
        using var response = await host.GetAsync($"/api/v1/{list}?sort={Uri.EscapeDataString(sort)}");
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "sort"));
    }

    // ---- filters

    [Theory]
    [InlineData("isActive=true", "ZA,ZC,ZD", 3)]
    [InlineData("isActive=false", "ZB", 1)]
    [InlineData("type=Storage", "ZC,ZD", 2)]
    [InlineData("type=Receiving", "ZB", 1)]
    [InlineData("type=Shipping&isActive=true", "ZA", 1)]
    [InlineData("type=Storage&isActive=false", "", 0)]
    [Trait("AC", "FEAT-001.11")]
    public async Task Zones_filter_by_isActive_and_type_and_the_total_follows_the_filter(string query, string expected, int total)
    {
        await SeedZonesAsync();

        var page = await host.GetJsonAsync($"{Zones}?{query}");

        Assert.Equal(expected, string.Join(",", LocationsHost.Codes(page)));
        Assert.Equal(total, page.GetProperty("totalItems").GetInt32());
    }

    [Theory]
    [InlineData("zones", "isActive=maybe", "isActive")]
    [InlineData("zones", "type=Cold", "type")]
    [InlineData("zones", "type=1", "type")]
    [InlineData("bins", "isActive=maybe", "isActive")]
    [InlineData("bins", "zoneId=abc", "zoneId")]
    [Trait("AC", "FEAT-001.11")]
    public async Task A_bad_filter_value_is_400_under_that_filter(string list, string query, string field)
    {
        using var response = await host.GetAsync($"/api/v1/{list}?{query}");
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, field));
    }

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task Bins_filter_by_isActive_and_zoneId()
    {
        var (m, k) = await SeedBinsAsync();

        Assert.Equal("B3,B2", await CodesAsync($"{Bins}?zoneId={m}"));
        Assert.Equal("B1,B4", await CodesAsync($"{Bins}?zoneId={k}"));
        Assert.Equal("B3,B2,B4", await CodesAsync($"{Bins}?isActive=true"));
        Assert.Equal("B1", await CodesAsync($"{Bins}?isActive=false"));
        Assert.Equal("B4", await CodesAsync($"{Bins}?zoneId={k}&isActive=true"));
        Assert.Equal(string.Empty, await CodesAsync($"{Bins}?zoneId=9999"));
    }

    // ---- unknown ids

    [Theory]
    [InlineData("/api/v1/zones/9999")]
    [InlineData("/api/v1/bins/9999")]
    [InlineData("/api/v1/zones/abc")]
    [InlineData("/api/v1/bins/abc")]
    [Trait("AC", "FEAT-001.11")]
    public async Task An_unknown_id_is_404(string path)
    {
        using var response = await host.GetAsync(path);

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "not-found");
    }

    // ---- tree

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task The_tree_of_an_empty_warehouse_is_an_empty_array()
    {
        var tree = await host.GetJsonAsync(Tree);

        Assert.Equal(JsonValueKind.Array, tree.ValueKind);
        Assert.Equal(0, tree.GetArrayLength());
    }

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task The_tree_is_a_plain_array_of_zones_each_with_its_bins_and_a_bin_count_of_all_bins()
    {
        await SeedBinsAsync();
        await host.DeactivateZoneAsync(await host.CreateZoneAsync("N", "Empty zone", "Shipping"));

        var tree = await host.GetJsonAsync(Tree);
        var zones = tree.EnumerateArray().ToArray();

        Assert.Equal(JsonValueKind.Array, tree.ValueKind);
        Assert.Equal(["K", "M", "N"], zones.Select(z => LocationsHost.Str(z, "code")).ToArray());
        Assert.Equal(["B1", "B4"], zones[0].GetProperty("bins").EnumerateArray().Select(b => LocationsHost.Str(b, "code")).ToArray());
        Assert.Equal(["B2", "B3"], zones[1].GetProperty("bins").EnumerateArray().Select(b => LocationsHost.Str(b, "code")).ToArray());
        Assert.Equal(0, zones[2].GetProperty("bins").GetArrayLength());
        Assert.Equal([2, 2, 0], zones.Select(z => z.GetProperty("binCount").GetInt32()).ToArray());
        Assert.Equal("Shipping", LocationsHost.Str(zones[2], "type"));
        Assert.False(zones[2].GetProperty("isActive").GetBoolean());
    }

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task Each_tree_bin_has_the_bin_fields_and_its_zone_id()
    {
        var (m, _) = await SeedBinsAsync();

        var tree = await host.GetJsonAsync(Tree);
        var zoneM = tree.EnumerateArray().Single(z => LocationsHost.Id(z) == m);
        var bin = zoneM.GetProperty("bins").EnumerateArray().First();

        Assert.Equal("B2", LocationsHost.Str(bin, "code"));
        Assert.Equal("Alpha", LocationsHost.Str(bin, "name"));
        Assert.Equal("M/B2", LocationsHost.Str(bin, "path"));
        Assert.Equal("M", LocationsHost.Str(bin, "zoneCode"));
        Assert.Equal(m, bin.GetProperty("zoneId").GetInt32());
        Assert.True(bin.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task The_tree_isActive_filter_applies_to_zones_and_bins_independently()
    {
        await SeedBinsAsync();
        await host.DeactivateZoneAsync(await host.CreateZoneAsync("N"));

        var active = (await host.GetJsonAsync($"{Tree}?isActive=true")).EnumerateArray().ToArray();
        var inactive = (await host.GetJsonAsync($"{Tree}?isActive=false")).EnumerateArray().ToArray();

        Assert.Equal(["K", "M"], active.Select(z => LocationsHost.Str(z, "code")).ToArray());
        Assert.Equal(["B4"], active[0].GetProperty("bins").EnumerateArray().Select(b => LocationsHost.Str(b, "code")).ToArray());
        Assert.Equal(["B2", "B3"], active[1].GetProperty("bins").EnumerateArray().Select(b => LocationsHost.Str(b, "code")).ToArray());
        Assert.Equal([2, 2], active.Select(z => z.GetProperty("binCount").GetInt32()).ToArray());
        Assert.Equal(["N"], inactive.Select(z => LocationsHost.Str(z, "code")).ToArray());
    }

    [Fact]
    [Trait("AC", "FEAT-001.11")]
    public async Task The_tree_with_a_bad_isActive_is_400_under_isActive()
    {
        using var response = await host.GetAsync($"{Tree}?isActive=maybe");
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "isActive"));
    }

    // ---- AC 12: search

    // Zones in id order: ALPHA-1 "Receiving dock", BETA_2 "Cold store", GAMMA "100% fresh", DELTA "Under_score name".
    private async Task SeedSearchZonesAsync()
    {
        await host.CreateZoneAsync("ALPHA-1", "Receiving dock");
        await host.CreateZoneAsync("BETA_2", "Cold store");
        await host.CreateZoneAsync("GAMMA", "100% fresh");
        await host.CreateZoneAsync("DELTA", "Under_score name");
    }

    [Theory]
    [InlineData("alp", "ALPHA-1")]
    [InlineData("ALP", "ALPHA-1")]
    [InlineData("pha-", "ALPHA-1")]
    [InlineData("DOCK", "ALPHA-1")]
    [InlineData("dock", "ALPHA-1")]
    [InlineData("cold ST", "BETA_2")]
    [InlineData("mm", "GAMMA")]
    [InlineData("a", "ALPHA-1,BETA_2,GAMMA,DELTA")]
    [InlineData("nothing-like-this", "")]
    [Trait("AC", "FEAT-001.12")]
    public async Task Zones_match_q_against_part_of_the_code_or_name_without_regard_to_case(string q, string expected)
    {
        await SeedSearchZonesAsync();

        Assert.Equal(expected, await CodesAsync($"{Zones}?q={Uri.EscapeDataString(q)}"));
    }

    [Theory]
    [InlineData("_", "BETA_2,DELTA")]
    [InlineData("A_", "BETA_2")]
    [InlineData("%", "GAMMA")]
    [InlineData("0%", "GAMMA")]
    [InlineData("\\", "")]
    [Trait("AC", "FEAT-001.12")]
    public async Task Zones_treat_percent_underscore_and_backslash_in_q_literally(string q, string expected)
    {
        await SeedSearchZonesAsync();

        Assert.Equal(expected, await CodesAsync($"{Zones}?q={Uri.EscapeDataString(q)}"));
    }

    [Fact]
    [Trait("AC", "FEAT-001.12")]
    public async Task A_percent_in_q_returns_nothing_when_no_code_or_name_contains_one()
    {
        await host.CreateZoneAsync("ALPHA", "Receiving");
        await host.CreateZoneAsync("BETA_2", "Cold");

        var page = await host.GetJsonAsync($"{Zones}?q=%25");

        Assert.Equal(0, page.GetProperty("totalItems").GetInt32());
        Assert.Equal(0, page.GetProperty("items").GetArrayLength());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("AC", "FEAT-001.12")]
    public async Task A_blank_q_returns_everything(string q)
    {
        await SeedSearchZonesAsync();
        await SeedBinsAsync();

        Assert.Equal("ALPHA-1,BETA_2,GAMMA,DELTA,M,K", await CodesAsync($"{Zones}?q={Uri.EscapeDataString(q)}"));
        Assert.Equal("B3,B1,B2,B4", await CodesAsync($"{Bins}?q={Uri.EscapeDataString(q)}"));
    }

    [Fact]
    [Trait("AC", "FEAT-001.12")]
    public async Task The_total_follows_q_and_q_combines_with_the_other_filters_and_paging()
    {
        await SeedSearchZonesAsync();

        var first = await host.GetJsonAsync($"{Zones}?q=a&pageSize=3&sort=-code");
        var second = await host.GetJsonAsync($"{Zones}?q=a&pageSize=3&page=2&sort=-code");
        var filtered = await host.GetJsonAsync($"{Zones}?q=a&type=Storage");

        Assert.Equal(4, first.GetProperty("totalItems").GetInt32());
        Assert.Equal(["GAMMA", "DELTA", "BETA_2"], LocationsHost.Codes(first));
        Assert.Equal(["ALPHA-1"], LocationsHost.Codes(second));
        Assert.Equal(4, filtered.GetProperty("totalItems").GetInt32());
    }

    [Theory]
    [InlineData("b1", "B1")]
    [InlineData("alph", "B2")]
    [InlineData("DELTA", "B3")]
    [InlineData("b", "B3,B1,B2,B4")]
    [InlineData("zzz", "")]
    [Trait("AC", "FEAT-001.12")]
    public async Task Bins_match_q_against_part_of_the_code_or_name_without_regard_to_case(string q, string expected)
    {
        await SeedBinsAsync();

        Assert.Equal(expected, await CodesAsync($"{Bins}?q={Uri.EscapeDataString(q)}"));
    }

    [Fact]
    [Trait("AC", "FEAT-001.12")]
    public async Task Bins_treat_percent_and_underscore_in_q_literally_and_q_combines_with_zoneId()
    {
        var zone = LocationsHost.Id(await host.CreateZoneAsync("Z1"));
        var other = LocationsHost.Id(await host.CreateZoneAsync("Z2"));
        await host.CreateBinAsync(zone, "B_1", "Plain");
        await host.CreateBinAsync(zone, "B2", "50% off");
        await host.CreateBinAsync(other, "B3", "Shelf one");

        Assert.Equal("B_1", await CodesAsync($"{Bins}?q=_"));
        Assert.Equal("B2", await CodesAsync($"{Bins}?q=%25"));
        Assert.Equal("B3", await CodesAsync($"{Bins}?q=SHELF"));
        Assert.Equal("B_1,B2", await CodesAsync($"{Bins}?q=b&zoneId={zone}"));
        Assert.Equal("B3", await CodesAsync($"{Bins}?q=b&zoneId={other}"));
    }

    [Theory]
    [InlineData("zones")]
    [InlineData("bins")]
    [Trait("AC", "FEAT-001.12")]
    public async Task A_q_of_exactly_100_characters_is_accepted(string list)
    {
        using var response = await host.GetAsync($"/api/v1/{list}?q={new string('x', 100)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("zones")]
    [InlineData("bins")]
    [Trait("AC", "FEAT-001.12")]
    public async Task A_q_over_100_characters_is_400_under_q(string list)
    {
        using var response = await host.GetAsync($"/api/v1/{list}?q={new string('x', 101)}");
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "q"));
    }
}
