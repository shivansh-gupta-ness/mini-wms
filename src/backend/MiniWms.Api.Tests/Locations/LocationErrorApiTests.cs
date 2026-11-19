using System.Net;
using System.Text.Json;
using MiniWms.Api.Tests.Host;

namespace MiniWms.Api.Tests.Locations;

/// <summary>FEAT-001 AC 13: every failing Locations request is application/problem+json in the ADR-003 shape.</summary>
public sealed class LocationErrorApiTests : IDisposable
{
    private readonly LocationsHost host = new();

    public void Dispose() => host.Dispose();

    [Fact]
    [Trait("AC", "FEAT-001.13")]
    public async Task A_400_is_problem_json_with_type_title_status_trace_id_and_errors_of_string_arrays()
    {
        using var response = await host.PostAsync("/api/v1/zones", new { code = "", name = "", type = "Cold" });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");

        var errors = problem.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.All(errors.EnumerateObject(), field =>
        {
            Assert.Equal(JsonValueKind.Array, field.Value.ValueKind);
            Assert.All(field.Value.EnumerateArray(), message => Assert.False(string.IsNullOrWhiteSpace(message.GetString())));
        });
    }

    [Fact]
    [Trait("AC", "FEAT-001.13")]
    public async Task A_404_is_problem_json_of_type_not_found()
    {
        using var response = await host.GetAsync("/api/v1/zones/9999");

        await LocationsHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "not-found");
    }

    [Fact]
    [Trait("AC", "FEAT-001.13")]
    public async Task A_409_is_problem_json_of_type_conflict_with_a_detail_and_no_errors()
    {
        var zone = await host.CreateZoneAsync("Z1");
        await host.CreateBinAsync(LocationsHost.Id(zone), "B1");

        using var response = await host.PostAsync($"/api/v1/zones/{LocationsHost.Id(zone)}/deactivate", new { version = LocationsHost.Version(zone) });
        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "conflict");

        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
        Assert.False(problem.TryGetProperty("errors", out _));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    [InlineData("PUT")]
    [Trait("AC", "FEAT-001.13")]
    public async Task A_405_is_problem_json_with_a_status_and_a_trace_id(string method)
    {
        using var response = await host.SendRawAsync(new HttpMethod(method), "/api/v1/warehouse", "{}");
        var problem = await LocationsHost.JsonAsync(response);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(405, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    [Trait("AC", "FEAT-001.13")]
    public async Task A_405_on_the_warehouse_names_the_allowed_methods(string method)
    {
        using var response = await host.SendRawAsync(new HttpMethod(method), "/api/v1/warehouse", "{}");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(["GET", "PATCH"], response.Content.Headers.Allow.Order().ToArray());
    }

    // Every update goes through the versioned-update helper: a stale version is a 409 on each of them.
    [Theory]
    [InlineData("warehouse")]
    [InlineData("zone-patch")]
    [InlineData("zone-deactivate")]
    [InlineData("bin-patch")]
    [InlineData("bin-deactivate")]
    [Trait("AC", "FEAT-001.13")]
    public async Task A_stale_version_is_409_conflict_on_every_update_endpoint(string endpoint)
    {
        var zone = await host.CreateZoneAsync("Z1");
        var bin = endpoint.StartsWith("bin", StringComparison.Ordinal)
            ? await host.CreateBinAsync(LocationsHost.Id(zone), "B1")
            : zone;
        var warehouse = await host.GetJsonAsync("/api/v1/warehouse");
        var (path, current, isPatch) = endpoint switch
        {
            "warehouse" => ("/api/v1/warehouse", warehouse, true),
            "zone-patch" => ($"/api/v1/zones/{LocationsHost.Id(zone)}", zone, true),
            "zone-deactivate" => ($"/api/v1/zones/{LocationsHost.Id(zone)}/deactivate", zone, false),
            "bin-patch" => ($"/api/v1/bins/{LocationsHost.Id(bin)}", bin, true),
            _ => ($"/api/v1/bins/{LocationsHost.Id(bin)}/deactivate", bin, false),
        };
        var stale = LocationsHost.Version(current) + 5;

        using var response = isPatch
            ? await host.PatchAsync(path, new { name = "Renamed", version = stale })
            : await host.PostAsync(path, new { version = stale });

        var problem = await LocationsHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "conflict");
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
    }

    // The unique index is the safety net behind the duplicate-code check: whoever loses a race still gets the 400.
    [Fact]
    [Trait("AC", "FEAT-001.4")]
    public async Task Racing_creates_of_the_same_zone_code_give_one_201_and_400s_under_code_never_a_500()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
            host.PostAsync("/api/v1/zones", new { code = "RACE", name = "Racer", type = "Storage" })));

        try
        {
            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
            Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest));
            foreach (var rejected in responses.Where(r => r.StatusCode == HttpStatusCode.BadRequest))
            {
                Assert.NotEmpty(LocationsHost.Errors(await LocationsHost.JsonAsync(rejected), "code"));
            }

            Assert.Equal(1, await host.ZoneTotalAsync());
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    [Trait("AC", "FEAT-001.6")]
    public async Task Racing_creates_of_the_same_bin_code_give_one_201_and_400s_under_code_never_a_500()
    {
        var zone = await host.CreateZoneAsync("Z1");
        var zoneId = LocationsHost.Id(zone);

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
            host.PostAsync("/api/v1/bins", new { zoneId, code = "RACE", name = "Racer" })));

        try
        {
            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
            Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest));
            foreach (var rejected in responses.Where(r => r.StatusCode == HttpStatusCode.BadRequest))
            {
                Assert.NotEmpty(LocationsHost.Errors(await LocationsHost.JsonAsync(rejected), "code"));
            }

            Assert.Equal(1, await host.BinTotalAsync());
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }
}

/// <summary>FEAT-001 AC 13: an unexpected failure on the real host never leaks detail.</summary>
public sealed class LocationServerErrorApiTests : IDisposable
{
    private readonly LocationsHost host = new(new ThrowingApiFactory());

    public void Dispose() => host.Dispose();

    [Fact]
    [Trait("AC", "FEAT-001.13")]
    public async Task A_500_is_problem_json_with_a_trace_id_and_no_detail()
    {
        using var response = await host.GetAsync(ThrowingApiFactory.ThrowPath);
        var problem = await LocationsHost.JsonAsync(response);
        var text = problem.GetRawText();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.False(problem.TryGetProperty("detail", out _));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        Assert.DoesNotContain(ThrowingApiFactory.SecretText, text, StringComparison.Ordinal);
    }
}
