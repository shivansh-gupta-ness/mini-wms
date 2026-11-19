using System.Net;

namespace MiniWms.Api.Tests.Locations;

/// <summary>FEAT-001 AC 3, 4, 7 (zone part) and 10: create, validate, rename, change type, deactivate zones.</summary>
public sealed class ZoneApiTests : IDisposable
{
    private const string Zones = "/api/v1/zones";

    private readonly LocationsHost host = new();

    public void Dispose() => host.Dispose();

    // ---- AC 3

    [Theory]
    [InlineData("Receiving")]
    [InlineData("Storage")]
    [InlineData("Shipping")]
    [Trait("AC", "FEAT-001.3")]
    public async Task Create_with_a_valid_request_is_201_with_location_and_an_active_empty_zone(string type)
    {
        using var response = await host.PostAsync(Zones, new { code = "Z1", name = "First zone", type });
        var body = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"{Zones}/{LocationsHost.Id(body)}", response.Headers.Location?.ToString());
        Assert.Equal("Z1", LocationsHost.Str(body, "code"));
        Assert.Equal("First zone", LocationsHost.Str(body, "name"));
        Assert.Equal(type, LocationsHost.Str(body, "type"));
        Assert.True(body.GetProperty("isActive").GetBoolean());
        Assert.Equal(0, body.GetProperty("binCount").GetInt32());
        Assert.True(LocationsHost.Version(body) >= 1);
        Assert.True(body.GetProperty("warehouseId").GetInt32() > 0);
    }

    [Fact]
    [Trait("AC", "FEAT-001.3")]
    public async Task The_location_header_returns_the_created_zone()
    {
        using var response = await host.PostAsync(Zones, new { code = "Z1", name = "First zone", type = "Storage" });
        var created = await LocationsHost.JsonAsync(response);

        var fetched = await host.GetJsonAsync(response.Headers.Location!.ToString());

        Assert.Equal(created.GetRawText(), fetched.GetRawText());
    }

    [Fact]
    [Trait("AC", "FEAT-001.3")]
    public async Task Create_accepts_a_code_of_exactly_20_and_a_name_of_exactly_100_characters()
    {
        using var response = await host.PostAsync(Zones, new { code = new string('C', 20), name = new string('n', 100), type = "Storage" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    [Trait("AC", "FEAT-001.3")]
    public async Task Created_zone_belongs_to_the_seeded_warehouse()
    {
        var warehouse = await host.GetJsonAsync("/api/v1/warehouse");

        var zone = await host.CreateZoneAsync("Z1");

        Assert.Equal(LocationsHost.Id(warehouse), zone.GetProperty("warehouseId").GetInt32());
    }

    // ---- AC 4

    [Theory]
    [InlineData("""{"name":"Name","type":"Storage"}""", "code")]
    [InlineData("""{"code":"","name":"Name","type":"Storage"}""", "code")]
    [InlineData("""{"code":"   ","name":"Name","type":"Storage"}""", "code")]
    [InlineData("""{"code":"Z1","type":"Storage"}""", "name")]
    [InlineData("""{"code":"Z1","name":"  ","type":"Storage"}""", "name")]
    [InlineData("""{"code":"Z1","name":"Name"}""", "type")]
    [InlineData("""{"code":"Z1","name":"Name","type":""}""", "type")]
    [InlineData("""{"code":"Z1","name":"Name","type":"Cold"}""", "type")]
    [InlineData("""{"code":"Z1","name":"Name","type":"1"}""", "type")]
    [InlineData("""{"code":"Z1","name":"Name","type":"storage"}""", "type")]
    [Trait("AC", "FEAT-001.4")]
    public async Task Create_with_a_missing_or_invalid_field_is_400_under_that_field_and_nothing_is_saved(string json, string field)
    {
        using var response = await host.SendRawAsync(HttpMethod.Post, Zones, json);
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, field));
        Assert.Equal(0, await host.ZoneTotalAsync());
    }

    [Fact]
    [Trait("AC", "FEAT-001.4")]
    public async Task Create_with_a_code_over_20_characters_is_400_under_code_and_nothing_is_saved()
    {
        using var response = await host.PostAsync(Zones, new { code = new string('C', 21), name = "Name", type = "Storage" });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "code"));
        Assert.Equal(0, await host.ZoneTotalAsync());
    }

    [Fact]
    [Trait("AC", "FEAT-001.4")]
    public async Task Create_with_a_name_over_100_characters_is_400_under_name_and_nothing_is_saved()
    {
        using var response = await host.PostAsync(Zones, new { code = "Z1", name = new string('n', 101), type = "Storage" });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "name"));
        Assert.Equal(0, await host.ZoneTotalAsync());
    }

    [Fact]
    [Trait("AC", "FEAT-001.4")]
    public async Task Create_reports_every_failing_field_at_once()
    {
        using var response = await host.PostAsync(Zones, new { code = "", name = "", type = "Cold" });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "code"));
        Assert.NotEmpty(LocationsHost.Errors(problem, "name"));
        Assert.NotEmpty(LocationsHost.Errors(problem, "type"));
    }

    [Theory]
    [InlineData("A1", "a1")]
    [InlineData("A1", "A1")]
    [InlineData("a1", "A1")]
    [InlineData("A1", " A1 ")]
    [Trait("AC", "FEAT-001.4")]
    public async Task Create_with_a_duplicate_code_in_any_case_is_400_under_code_and_nothing_is_saved(string first, string second)
    {
        await host.CreateZoneAsync(first);

        using var response = await host.PostAsync(Zones, new { code = second, name = "Other", type = "Storage" });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "code"));
        Assert.Equal(1, await host.ZoneTotalAsync());
    }

    [Theory]
    [InlineData("""{"code":"Z1","name":"Name","type":1}""")]
    [InlineData("not json")]
    [InlineData("")]
    [Trait("AC", "FEAT-001.4")]
    public async Task Create_with_an_unreadable_body_is_a_400_problem_json_and_nothing_is_saved(string json)
    {
        using var response = await host.SendRawAsync(HttpMethod.Post, Zones, json);

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
        Assert.Equal(0, await host.ZoneTotalAsync());
    }

    // ---- AC 7 (zone)

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_renames_the_zone_keeps_the_code_and_increments_the_version()
    {
        var zone = await host.CreateZoneAsync("Z1", "Old name", "Storage");

        using var response = await host.PatchAsync($"{Zones}/{LocationsHost.Id(zone)}", new { name = "New name", version = LocationsHost.Version(zone) });
        var body = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("New name", LocationsHost.Str(body, "name"));
        Assert.Equal("Z1", LocationsHost.Str(body, "code"));
        Assert.Equal("Storage", LocationsHost.Str(body, "type"));
        Assert.Equal(LocationsHost.Version(zone) + 1, LocationsHost.Version(body));
        Assert.Equal("New name", LocationsHost.Str(await host.GetJsonAsync($"{Zones}/{LocationsHost.Id(zone)}"), "name"));
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_ignores_a_code_sent_in_the_body()
    {
        var zone = await host.CreateZoneAsync("Z1");

        using var response = await host.PatchAsync(
            $"{Zones}/{LocationsHost.Id(zone)}", new { code = "CHANGED", name = "New name", version = LocationsHost.Version(zone) });
        var body = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Z1", LocationsHost.Str(body, "code"));
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_changes_the_type_while_the_zone_has_no_bins()
    {
        var zone = await host.CreateZoneAsync("Z1", "Name", "Storage");

        using var response = await host.PatchAsync(
            $"{Zones}/{LocationsHost.Id(zone)}", new { name = "Name", type = "Shipping", version = LocationsHost.Version(zone) });
        var body = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Shipping", LocationsHost.Str(body, "type"));
        Assert.Equal(LocationsHost.Version(zone) + 1, LocationsHost.Version(body));
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_changing_the_type_of_a_zone_with_bins_is_409_with_a_message_and_nothing_changes()
    {
        var zone = await host.CreateZoneAsync("Z1", "Name", "Storage");
        await host.CreateBinAsync(LocationsHost.Id(zone), "B1");

        using var response = await host.PatchAsync(
            $"{Zones}/{LocationsHost.Id(zone)}", new { name = "Renamed", type = "Shipping", version = LocationsHost.Version(zone) });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "conflict");
        var after = await host.GetJsonAsync($"{Zones}/{LocationsHost.Id(zone)}");

        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
        Assert.Equal("Storage", LocationsHost.Str(after, "type"));
        Assert.Equal("Name", LocationsHost.Str(after, "name"));
        Assert.Equal(LocationsHost.Version(zone), LocationsHost.Version(after));
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_with_the_same_type_succeeds_for_a_zone_with_bins()
    {
        var zone = await host.CreateZoneAsync("Z1", "Name", "Storage");
        await host.CreateBinAsync(LocationsHost.Id(zone), "B1");

        using var response = await host.PatchAsync(
            $"{Zones}/{LocationsHost.Id(zone)}", new { name = "Renamed", type = "Storage", version = LocationsHost.Version(zone) });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_with_a_stale_version_is_409_conflict_and_nothing_changes()
    {
        var zone = await host.CreateZoneAsync("Z1", "Name");
        var path = $"{Zones}/{LocationsHost.Id(zone)}";
        using var first = await host.PatchAsync(path, new { name = "First", version = LocationsHost.Version(zone) });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var stale = await host.PatchAsync(path, new { name = "Second", version = LocationsHost.Version(zone) });
        await LocationsHost.AssertProblemAsync(stale, HttpStatusCode.Conflict, "conflict");

        Assert.Equal("First", LocationsHost.Str(await host.GetJsonAsync(path), "name"));
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_of_an_unknown_zone_is_404_not_found()
    {
        using var response = await host.PatchAsync($"{Zones}/9999", new { name = "Name", version = 1 });

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "not-found");
    }

    [Theory]
    [InlineData("""{"name":"","version":1}""", "name")]
    [InlineData("""{"name":"Name","type":"Cold","version":1}""", "type")]
    [InlineData("""{"name":"Name"}""", "version")]
    [Trait("AC", "FEAT-001.7")]
    public async Task Patch_with_an_invalid_field_is_400_under_that_field(string json, string field)
    {
        var zone = await host.CreateZoneAsync("Z1");

        using var response = await host.SendRawAsync(HttpMethod.Patch, $"{Zones}/{LocationsHost.Id(zone)}", json);
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, field));
    }

    [Fact]
    [Trait("AC", "FEAT-001.7")]
    public async Task Get_of_an_unknown_zone_is_404_not_found()
    {
        using var response = await host.GetAsync($"{Zones}/9999");

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "not-found");
    }

    // ---- AC 10

    [Fact]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivating_a_zone_with_an_active_bin_is_409_and_the_zone_stays_active()
    {
        var zone = await host.CreateZoneAsync("Z1");
        await host.CreateBinAsync(LocationsHost.Id(zone), "B1");

        using var response = await host.PostAsync($"{Zones}/{LocationsHost.Id(zone)}/deactivate", new { version = LocationsHost.Version(zone) });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "conflict");
        var after = await host.GetJsonAsync($"{Zones}/{LocationsHost.Id(zone)}");

        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
        Assert.True(after.GetProperty("isActive").GetBoolean());
        Assert.Equal(LocationsHost.Version(zone), LocationsHost.Version(after));
    }

    [Fact]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivating_a_zone_with_no_bins_is_200_and_inactive()
    {
        var zone = await host.CreateZoneAsync("Z1");

        using var response = await host.PostAsync($"{Zones}/{LocationsHost.Id(zone)}/deactivate", new { version = LocationsHost.Version(zone) });
        var body = await LocationsHost.JsonAsync(response);
        var after = await host.GetJsonAsync($"{Zones}/{LocationsHost.Id(zone)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(body.GetProperty("isActive").GetBoolean());
        Assert.False(after.GetProperty("isActive").GetBoolean());
        Assert.Equal(LocationsHost.Version(zone) + 1, LocationsHost.Version(body));
    }

    [Fact]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivating_a_zone_whose_bins_are_all_inactive_is_200_and_inactive()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = await host.CreateBinAsync(LocationsHost.Id(zone), "B1");
        await host.DeactivateBinAsync(bin);
        var current = await host.GetJsonAsync($"{Zones}/{LocationsHost.Id(zone)}");

        using var response = await host.PostAsync($"{Zones}/{LocationsHost.Id(zone)}/deactivate", new { version = LocationsHost.Version(current) });
        var body = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(body.GetProperty("isActive").GetBoolean());
        Assert.Equal(1, body.GetProperty("binCount").GetInt32());
    }

    [Fact]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivating_a_zone_that_still_has_one_active_bin_among_inactive_ones_is_409()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var inactive = await host.CreateBinAsync(LocationsHost.Id(zone), "B1");
        await host.CreateBinAsync(LocationsHost.Id(zone), "B2");
        await host.DeactivateBinAsync(inactive);

        using var response = await host.PostAsync($"{Zones}/{LocationsHost.Id(zone)}/deactivate", new { version = LocationsHost.Version(zone) });

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "conflict");
    }

    [Fact]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivating_an_inactive_zone_again_is_200_and_unchanged()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var inactive = await host.DeactivateZoneAsync(zone);

        using var response = await host.PostAsync($"{Zones}/{LocationsHost.Id(zone)}/deactivate", new { version = LocationsHost.Version(inactive) });
        var body = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(inactive.GetRawText(), body.GetRawText());
    }

    [Fact]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivating_a_zone_with_a_stale_version_is_409_and_the_zone_stays_active()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var path = $"{Zones}/{LocationsHost.Id(zone)}";
        using var rename = await host.PatchAsync(path, new { name = "Renamed", version = LocationsHost.Version(zone) });
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);

        using var response = await host.PostAsync($"{path}/deactivate", new { version = LocationsHost.Version(zone) });
        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "conflict");

        Assert.True((await host.GetJsonAsync(path)).GetProperty("isActive").GetBoolean());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not json")]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivating_a_zone_with_a_missing_or_invalid_body_is_400(string json)
    {
        var zone = await host.CreateZoneAsync("Z1");

        using var response = await host.SendRawAsync(HttpMethod.Post, $"{Zones}/{LocationsHost.Id(zone)}/deactivate", json);

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [Fact]
    [Trait("AC", "FEAT-001.10")]
    public async Task Deactivating_an_unknown_zone_is_404_not_found()
    {
        using var response = await host.PostAsync($"{Zones}/9999/deactivate", new { version = 1 });

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "not-found");
    }
}
