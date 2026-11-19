using System.Net;

namespace MiniWms.Api.Tests.Locations;

/// <summary>FEAT-001 AC 1 and 2: the single seeded warehouse can be read and renamed, never created or deleted.</summary>
public sealed class WarehouseApiTests : IDisposable
{
    private const string Path = "/api/v1/warehouse";

    private readonly LocationsHost host = new();

    public void Dispose() => host.Dispose();

    // ---- AC 1

    [Fact]
    [Trait("AC", "FEAT-001.1")]
    public async Task Get_returns_the_seeded_MAIN_warehouse_with_all_fields()
    {
        using var response = await host.GetAsync(Path);
        var body = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("MAIN", LocationsHost.Str(body, "code"));
        Assert.False(string.IsNullOrWhiteSpace(LocationsHost.Str(body, "name")));
        Assert.True(body.GetProperty("id").GetInt32() > 0);
        Assert.True(body.GetProperty("isActive").GetBoolean());
        Assert.True(LocationsHost.Version(body) >= 1);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    [InlineData("PUT")]
    [Trait("AC", "FEAT-001.1")]
    public async Task Create_delete_and_replace_are_405_and_the_warehouse_is_unchanged(string method)
    {
        var before = await host.GetJsonAsync(Path);

        using var response = await host.SendRawAsync(new HttpMethod(method), Path, """{"code":"X","name":"Other"}""");
        var after = await host.GetJsonAsync(Path);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(before.GetRawText(), after.GetRawText());
    }

    [Theory]
    [InlineData("POST", "/api/v1/warehouse/1")]
    [InlineData("DELETE", "/api/v1/warehouse/1")]
    [InlineData("POST", "/api/v1/warehouses")]
    [InlineData("DELETE", "/api/v1/warehouses/1")]
    [Trait("AC", "FEAT-001.1")]
    public async Task No_other_route_creates_or_deletes_a_warehouse(string method, string path)
    {
        using var response = await host.SendRawAsync(new HttpMethod(method), path, """{"code":"X","name":"Other"}""");

        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            $"{method} {path} returned {(int)response.StatusCode}");
        Assert.Equal("MAIN", LocationsHost.Str(await host.GetJsonAsync(Path), "code"));
    }

    // ---- AC 2

    [Fact]
    [Trait("AC", "FEAT-001.2")]
    public async Task Patch_saves_the_new_name_keeps_the_code_and_increments_the_version()
    {
        var before = await host.GetJsonAsync(Path);

        using var response = await host.PatchAsync(Path, new { name = "Renamed warehouse", version = LocationsHost.Version(before) });
        var body = await LocationsHost.JsonAsync(response);
        var reread = await host.GetJsonAsync(Path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Renamed warehouse", LocationsHost.Str(body, "name"));
        Assert.Equal("MAIN", LocationsHost.Str(body, "code"));
        Assert.Equal(LocationsHost.Id(before), LocationsHost.Id(body));
        Assert.Equal(LocationsHost.Version(before) + 1, LocationsHost.Version(body));
        Assert.Equal(body.GetRawText(), reread.GetRawText());
    }

    [Fact]
    [Trait("AC", "FEAT-001.2")]
    public async Task Patch_accepts_a_name_of_exactly_100_characters()
    {
        var before = await host.GetJsonAsync(Path);

        using var response = await host.PatchAsync(Path, new { name = new string('n', 100), version = LocationsHost.Version(before) });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("AC", "FEAT-001.2")]
    public async Task Patch_with_a_blank_name_is_400_under_name_and_nothing_changes(string name)
    {
        var before = await host.GetJsonAsync(Path);

        using var response = await host.PatchAsync(Path, new { name, version = LocationsHost.Version(before) });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
        var after = await host.GetJsonAsync(Path);

        Assert.NotEmpty(LocationsHost.Errors(problem, "name"));
        Assert.Equal(before.GetRawText(), after.GetRawText());
    }

    [Fact]
    [Trait("AC", "FEAT-001.2")]
    public async Task Patch_with_a_101_character_name_is_400_under_name_and_nothing_changes()
    {
        var before = await host.GetJsonAsync(Path);

        using var response = await host.PatchAsync(Path, new { name = new string('n', 101), version = LocationsHost.Version(before) });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
        var after = await host.GetJsonAsync(Path);

        Assert.NotEmpty(LocationsHost.Errors(problem, "name"));
        Assert.Equal(before.GetRawText(), after.GetRawText());
    }

    [Fact]
    [Trait("AC", "FEAT-001.2")]
    public async Task Patch_with_a_stale_version_is_409_conflict_and_nothing_changes()
    {
        var before = await host.GetJsonAsync(Path);
        using var first = await host.PatchAsync(Path, new { name = "First", version = LocationsHost.Version(before) });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var stale = await host.PatchAsync(Path, new { name = "Second", version = LocationsHost.Version(before) });
        var problem = await LocationsHost.AssertProblemAsync(stale, HttpStatusCode.Conflict, "conflict");
        var after = await host.GetJsonAsync(Path);

        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
        Assert.Equal("First", LocationsHost.Str(after, "name"));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"name":"No version"}""")]
    [InlineData("""{"name":"Zero version","version":0}""")]
    [Trait("AC", "FEAT-001.2")]
    public async Task Patch_without_a_usable_version_is_400_under_version(string json)
    {
        using var response = await host.SendRawAsync(HttpMethod.Patch, Path, json);
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "version"));
    }

    [Theory]
    [InlineData(null, "application/json")]
    [InlineData("", "application/json")]
    [InlineData("{not json", "application/json")]
    [InlineData("""{"name":"x","version":"abc"}""", "application/json")]
    [InlineData("""{"name":"x","version":1}""", "text/plain")]
    [Trait("AC", "FEAT-001.2")]
    public async Task Patch_with_a_missing_or_invalid_body_is_a_400_problem_json(string? json, string contentType)
    {
        using var response = await host.SendRawAsync(HttpMethod.Patch, Path, json, contentType);
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        Assert.NotEmpty(LocationsHost.Errors(problem, "body"));
    }
}
