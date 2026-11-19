using System.Net;

namespace MiniWms.Api.Tests.Locations;

/// <summary>FEAT-001 AC 5, 6, 7 (bin part) and 8: create, validate, rename and deactivate bins (zero stock).</summary>
public sealed class BinApiTests : IDisposable
{
    private const string Bins = "/api/v1/bins";

    private readonly LocationsHost host = new();

    public void Dispose() => host.Dispose();

    // ---- AC 5

    [Fact]
    [Trait("AC", "FEAT-001.5")]
    public async Task Create_in_an_active_zone_is_201_with_location_zone_code_and_path()
    {
        var zone = await host.CreateZoneAsync("Z1");

        using var response = await host.PostAsync(Bins, new { zoneId = LocationsHost.Id(zone), code = "B1", name = "First bin" });
        var body = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"{Bins}/{LocationsHost.Id(body)}", response.Headers.Location?.ToString());
        Assert.Equal(LocationsHost.Id(zone), body.GetProperty("zoneId").GetInt32());
        Assert.Equal("Z1", LocationsHost.Str(body, "zoneCode"));
        Assert.Equal("B1", LocationsHost.Str(body, "code"));
        Assert.Equal("First bin", LocationsHost.Str(body, "name"));
        Assert.Equal("Z1/B1", LocationsHost.Str(body, "path"));
        Assert.True(body.GetProperty("isActive").GetBoolean());
        Assert.True(LocationsHost.Version(body) >= 1);
    }

    [Fact]
    [Trait("AC", "FEAT-001.5")]
    public async Task The_location_header_returns_the_created_bin_and_the_zone_counts_it()
    {
        var zone = await host.CreateZoneAsync("Z1");

        using var response = await host.PostAsync(Bins, new { zoneId = LocationsHost.Id(zone), code = "B1", name = "First bin" });
        var created = await LocationsHost.JsonAsync(response);
        var fetched = await host.GetJsonAsync(response.Headers.Location!.ToString());
        var zoneAfter = await host.GetJsonAsync($"/api/v1/zones/{LocationsHost.Id(zone)}");

        Assert.Equal(created.GetRawText(), fetched.GetRawText());
        Assert.Equal(1, zoneAfter.GetProperty("binCount").GetInt32());
    }

    [Fact]
    [Trait("AC", "FEAT-001.5")]
    public async Task Create_accepts_a_code_of_exactly_30_and_a_name_of_exactly_100_characters()
    {
        var zone = await host.CreateZoneAsync("Z1");

        using var response = await host.PostAsync(Bins, new { zoneId = LocationsHost.Id(zone), code = new string('B', 30), name = new string('n', 100) });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ---- AC 6

    [Theory]
    [InlineData("code")]
    [InlineData("name")]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_with_a_missing_or_blank_code_or_name_is_400_under_that_field_and_nothing_is_saved(string field)
    {
        var zone = await host.CreateZoneAsync("Z1");
        var body = new Dictionary<string, object?> { ["zoneId"] = LocationsHost.Id(zone), ["code"] = "B1", ["name"] = "Name" };

        body[field] = "   ";
        using var blank = await host.PostAsync(Bins, body);
        var blankProblem = await LocationsHost.AssertProblemAsync(blank, HttpStatusCode.BadRequest, "validation");

        body.Remove(field);
        using var missing = await host.PostAsync(Bins, body);
        var missingProblem = await LocationsHost.AssertProblemAsync(missing, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(blankProblem, field));
        Assert.NotEmpty(LocationsHost.Errors(missingProblem, field));
        Assert.Equal(0, await host.BinTotalAsync());
    }

    [Fact]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_without_a_zone_is_400_under_zoneId_and_nothing_is_saved()
    {
        using var response = await host.PostAsync(Bins, new { code = "B1", name = "Name" });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "zoneId"));
        Assert.Equal(0, await host.BinTotalAsync());
    }

    [Fact]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_in_an_unknown_zone_is_400_under_zoneId_and_nothing_is_saved()
    {
        using var response = await host.PostAsync(Bins, new { zoneId = 9999, code = "B1", name = "Name" });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "zoneId"));
        Assert.Equal(0, await host.BinTotalAsync());
    }

    [Fact]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_in_an_inactive_zone_is_400_under_zoneId_and_nothing_is_saved()
    {
        var zone = await host.CreateZoneAsync("Z1");
        await host.DeactivateZoneAsync(zone);

        using var response = await host.PostAsync(Bins, new { zoneId = LocationsHost.Id(zone), code = "B1", name = "Name" });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "zoneId"));
        Assert.Equal(0, await host.BinTotalAsync());
    }

    [Fact]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_with_a_code_over_30_characters_is_400_under_code_and_nothing_is_saved()
    {
        var zone = await host.CreateZoneAsync("Z1");

        using var response = await host.PostAsync(Bins, new { zoneId = LocationsHost.Id(zone), code = new string('B', 31), name = "Name" });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "code"));
        Assert.Equal(0, await host.BinTotalAsync());
    }

    [Fact]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_with_a_name_over_100_characters_is_400_under_name_and_nothing_is_saved()
    {
        var zone = await host.CreateZoneAsync("Z1");

        using var response = await host.PostAsync(Bins, new { zoneId = LocationsHost.Id(zone), code = "B1", name = new string('n', 101) });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "name"));
        Assert.Equal(0, await host.BinTotalAsync());
    }

    [Theory]
    [InlineData("b1", false)]
    [InlineData("B1", false)]
    [InlineData("b1", true)]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_with_a_code_used_in_the_warehouse_in_any_case_is_400_under_code_and_nothing_is_saved(
        string duplicate, bool inAnotherZone)
    {
        var zone = await host.CreateZoneAsync("Z1");
        var other = await host.CreateZoneAsync("Z2");
        await host.CreateBinAsync(LocationsHost.Id(zone), "B1");

        var target = inAnotherZone ? other : zone;
        using var response = await host.PostAsync(Bins, new { zoneId = LocationsHost.Id(target), code = duplicate, name = "Name" });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "code"));
        Assert.Equal(1, await host.BinTotalAsync());
    }

    [Theory]
    [InlineData("""{"zoneId":"x","code":"B1","name":"Name"}""")]
    [InlineData("not json")]
    [InlineData("")]
    [Trait("AC", "FEAT-001.6")]
    public async Task Create_with_an_unreadable_body_is_a_400_problem_json_and_nothing_is_saved(string json)
    {
        using var response = await host.SendRawAsync(HttpMethod.Post, Bins, json);

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
        Assert.Equal(0, await host.BinTotalAsync());
    }

    // ---- AC 7 (bin)

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_renames_the_bin_keeps_the_code_and_increments_the_version()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = await host.CreateBinAsync(LocationsHost.Id(zone), "B1", "Old name");

        using var response = await host.PatchAsync($"{Bins}/{LocationsHost.Id(bin)}", new { name = "New name", version = LocationsHost.Version(bin) });
        var body = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("New name", LocationsHost.Str(body, "name"));
        Assert.Equal("B1", LocationsHost.Str(body, "code"));
        Assert.Equal("Z1/B1", LocationsHost.Str(body, "path"));
        Assert.Equal(LocationsHost.Version(bin) + 1, LocationsHost.Version(body));
        Assert.Equal("New name", LocationsHost.Str(await host.GetJsonAsync($"{Bins}/{LocationsHost.Id(bin)}"), "name"));
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_with_a_stale_version_is_409_conflict_and_nothing_changes()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = await host.CreateBinAsync(LocationsHost.Id(zone), "B1", "Name");
        var path = $"{Bins}/{LocationsHost.Id(bin)}";
        using var first = await host.PatchAsync(path, new { name = "First", version = LocationsHost.Version(bin) });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var stale = await host.PatchAsync(path, new { name = "Second", version = LocationsHost.Version(bin) });
        await LocationsHost.AssertProblemAsync(stale, HttpStatusCode.Conflict, "conflict");

        Assert.Equal("First", LocationsHost.Str(await host.GetJsonAsync(path), "name"));
    }

    [Theory]
    [InlineData("""{"name":"","version":1}""", "name")]
    [InlineData("""{"name":"Name"}""", "version")]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_with_an_invalid_field_is_400_under_that_field(string json, string field)
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = await host.CreateBinAsync(LocationsHost.Id(zone), "B1");

        using var response = await host.SendRawAsync(HttpMethod.Patch, $"{Bins}/{LocationsHost.Id(bin)}", json);
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, field));
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_of_an_unknown_bin_is_404_not_found()
    {
        using var response = await host.PatchAsync($"{Bins}/9999", new { name = "Name", version = 1 });

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "not-found");
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Get_of_an_unknown_bin_is_404_not_found()
    {
        using var response = await host.GetAsync($"{Bins}/9999");

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "not-found");
    }

    // ---- AC 8

    [Fact]
    [Trait("AC", "FEAT-001.8")]
    public async Task Deactivating_a_bin_with_no_stock_is_200_and_inactive()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = await host.CreateBinAsync(LocationsHost.Id(zone), "B1");

        using var response = await host.PostAsync($"{Bins}/{LocationsHost.Id(bin)}/deactivate", new { version = LocationsHost.Version(bin) });
        var body = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(body.GetProperty("isActive").GetBoolean());
        Assert.Equal("B1", LocationsHost.Str(body, "code"));
        Assert.Equal(LocationsHost.Version(bin) + 1, LocationsHost.Version(body));
    }

    [Fact]
    [Trait("AC", "FEAT-001.8")]
    public async Task An_inactive_bin_is_still_returned_by_id_and_in_the_list_but_not_when_filtered_to_active()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = await host.CreateBinAsync(LocationsHost.Id(zone), "B1");
        await host.CreateBinAsync(LocationsHost.Id(zone), "B2");
        await host.DeactivateBinAsync(bin);

        var byId = await host.GetJsonAsync($"{Bins}/{LocationsHost.Id(bin)}");
        var all = await host.GetJsonAsync(Bins);
        var active = await host.GetJsonAsync($"{Bins}?isActive=true");

        Assert.False(byId.GetProperty("isActive").GetBoolean());
        Assert.Equal(["B1", "B2"], LocationsHost.Codes(all));
        Assert.Equal(["B2"], LocationsHost.Codes(active));
    }

    [Fact]
    [Trait("AC", "FEAT-001.8")]
    public async Task Deactivating_an_inactive_bin_again_is_200_and_unchanged()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = await host.CreateBinAsync(LocationsHost.Id(zone), "B1");
        var inactive = await host.DeactivateBinAsync(bin);

        using var response = await host.PostAsync($"{Bins}/{LocationsHost.Id(bin)}/deactivate", new { version = LocationsHost.Version(inactive) });
        var body = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(inactive.GetRawText(), body.GetRawText());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"version":0}""")]
    [InlineData("""{"version":"abc"}""")]
    [InlineData("not json")]
    [InlineData("")]
    [Trait("AC", "FEAT-001.8")]
    public async Task Deactivating_with_a_missing_or_invalid_body_is_400_and_the_bin_stays_active(string json)
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = await host.CreateBinAsync(LocationsHost.Id(zone), "B1");

        using var response = await host.SendRawAsync(HttpMethod.Post, $"{Bins}/{LocationsHost.Id(bin)}/deactivate", json);

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
        Assert.True((await host.GetJsonAsync($"{Bins}/{LocationsHost.Id(bin)}")).GetProperty("isActive").GetBoolean());
    }

    [Fact]
    [Trait("AC", "FEAT-001.8")]
    public async Task Deactivating_with_a_stale_version_is_409_and_the_bin_stays_active()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = await host.CreateBinAsync(LocationsHost.Id(zone), "B1");
        var path = $"{Bins}/{LocationsHost.Id(bin)}";
        using var rename = await host.PatchAsync(path, new { name = "Renamed", version = LocationsHost.Version(bin) });
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);

        using var response = await host.PostAsync($"{path}/deactivate", new { version = LocationsHost.Version(bin) });
        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "conflict");

        Assert.True((await host.GetJsonAsync(path)).GetProperty("isActive").GetBoolean());
    }

    [Fact]
    [Trait("AC", "FEAT-001.8")]
    public async Task Deactivating_an_unknown_bin_is_404_not_found()
    {
        using var response = await host.PostAsync($"{Bins}/9999/deactivate", new { version = 1 });

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "not-found");
    }
}

/// <summary>FEAT-001 AC 9: a bin that holds stock cannot be deactivated; the stock comes from a fake IStockQuery.</summary>
public sealed class BinStockApiTests : IDisposable
{
    private readonly StockApiFactory factory = new();
    private readonly LocationsHost host;

    public BinStockApiTests() => host = new LocationsHost(factory);

    public void Dispose() => host.Dispose();

    [Fact]
    [Trait("AC", "FEAT-001.9")]
    public async Task Deactivating_a_bin_that_holds_stock_is_409_naming_the_quantity_and_the_bin_stays_active()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = await host.CreateBinAsync(LocationsHost.Id(zone), "B1");
        factory.Stock[LocationsHost.Id(bin)] = 37;

        using var response = await host.PostAsync($"/api/v1/bins/{LocationsHost.Id(bin)}/deactivate", new { version = LocationsHost.Version(bin) });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "conflict");
        var after = await host.GetJsonAsync($"/api/v1/bins/{LocationsHost.Id(bin)}");

        Assert.Contains("37", problem.GetProperty("detail").GetString());
        Assert.True(after.GetProperty("isActive").GetBoolean());
        Assert.Equal(LocationsHost.Version(bin), LocationsHost.Version(after));
    }

    [Fact]
    [Trait("AC", "FEAT-001.9")]
    public async Task Only_the_bin_that_holds_stock_is_refused_while_an_empty_bin_deactivates()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var full = await host.CreateBinAsync(LocationsHost.Id(zone), "B1");
        var empty = await host.CreateBinAsync(LocationsHost.Id(zone), "B2");
        factory.Stock[LocationsHost.Id(full)] = 5;

        using var refused = await host.PostAsync($"/api/v1/bins/{LocationsHost.Id(full)}/deactivate", new { version = LocationsHost.Version(full) });
        using var accepted = await host.PostAsync($"/api/v1/bins/{LocationsHost.Id(empty)}/deactivate", new { version = LocationsHost.Version(empty) });

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Fact]
    [Trait("AC", "FEAT-001.9")]
    public async Task A_bin_can_be_deactivated_once_its_stock_is_gone()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = await host.CreateBinAsync(LocationsHost.Id(zone), "B1");
        var path = $"/api/v1/bins/{LocationsHost.Id(bin)}/deactivate";
        factory.Stock[LocationsHost.Id(bin)] = 1;
        using var refused = await host.PostAsync(path, new { version = LocationsHost.Version(bin) });
        factory.Stock[LocationsHost.Id(bin)] = 0;

        using var accepted = await host.PostAsync(path, new { version = LocationsHost.Version(bin) });

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }
}
