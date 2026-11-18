using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;

namespace MiniWms.Api.Tests.Host;

public class ApiHostTests
{
    private const string HealthPath = "/api/v1/health";

    // AC1: health returns 200 and {"status":"ok"} with no session.
    [Fact]
    [Trait("AC", "TASK-002.1")]
    public async Task Health_returns_200_with_status_ok_json_and_no_auth()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(HealthPath);
        var json = await ReadJson(response);

        Assert.Null(client.DefaultRequestHeaders.Authorization);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("ok", json.GetProperty("status").GetString());
        Assert.Single(json.EnumerateObject());
    }

    // AC2: unknown route is a 404 problem+json without stack trace.
    [Fact]
    [Trait("AC", "TASK-002.2")]
    public async Task Unknown_route_returns_404_problem_json()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/does-not-exist");
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, json.GetProperty("status").GetInt32());
        Assert.Equal("not-found", json.GetProperty("type").GetString());
    }

    [Fact]
    [Trait("AC", "TASK-002.2")]
    public async Task Unknown_route_body_has_no_detail_and_no_stack_trace()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/does-not-exist");
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;

        Assert.False(json.TryGetProperty("detail", out _));
        Assert.DoesNotContain(" at ", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", body, StringComparison.Ordinal);
    }

    // AC3: an unhandled exception is a generic 500 problem+json.
    [Fact]
    [Trait("AC", "TASK-002.3")]
    public async Task Unhandled_exception_returns_500_problem_json_with_generic_title()
    {
        using var factory = new ThrowingApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(ThrowingApiFactory.ThrowPath);
        var json = await ReadJson(response);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(500, json.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.", json.GetProperty("title").GetString());
        Assert.Equal("error", json.GetProperty("type").GetString());
    }

    [Fact]
    [Trait("AC", "TASK-002.3")]
    public async Task Unhandled_exception_body_has_no_detail_and_no_exception_text()
    {
        using var factory = new ThrowingApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(ThrowingApiFactory.ThrowPath);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;

        Assert.False(json.TryGetProperty("detail", out _));
        Assert.DoesNotContain(ThrowingApiFactory.SecretText, body, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", body, StringComparison.Ordinal);
    }

    // AC4: CORS allows only the Angular dev server.
    [Fact]
    [Trait("AC", "TASK-002.4")]
    public async Task Angular_dev_origin_is_allowed()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, HealthPath);
        request.Headers.Add("Origin", "http://localhost:4200");

        using var response = await client.SendAsync(request);

        Assert.Equal(["http://localhost:4200"], response.Headers.GetValues("Access-Control-Allow-Origin"));
    }

    [Fact]
    [Trait("AC", "TASK-002.4")]
    public async Task Other_origin_is_not_allowed()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, HealthPath);
        request.Headers.Add("Origin", "http://evil.example");

        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    // AC5: default launch profile and base path.
    [Fact]
    [Trait("AC", "TASK-002.5")]
    public void Http_launch_profile_listens_on_localhost_5080()
    {
        var path = Path.Combine(RepoLocator.ApiProjectFolder, "Properties", "launchSettings.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));

        var url = doc.RootElement.GetProperty("profiles").GetProperty("http").GetProperty("applicationUrl").GetString();

        Assert.Equal("http://localhost:5080", url);
    }

    [Fact]
    [Trait("AC", "TASK-002.5")]
    public async Task Health_is_served_under_the_api_v1_base_path()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(HealthPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("AC", "TASK-002.5")]
    public async Task Health_without_the_base_path_is_404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // AC6: endpoint modules register through MapXxxEndpoints called from Program.cs.
    [Fact]
    [Trait("AC", "TASK-002.6")]
    public void HealthEndpoints_has_a_public_static_MapHealthEndpoints_extension()
    {
        var method = typeof(MiniWms.Api.Common.HealthEndpoints).GetMethod("MapHealthEndpoints");

        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsPublic);
        Assert.True(method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false));
        Assert.Equal(typeof(IEndpointRouteBuilder), method.GetParameters()[0].ParameterType);
    }

    [Fact]
    [Trait("AC", "TASK-002.6")]
    public void Program_calls_MapHealthEndpoints()
    {
        var text = File.ReadAllText(Path.Combine(RepoLocator.ApiProjectFolder, "Program.cs"));

        Assert.Contains("MapHealthEndpoints(", text, StringComparison.Ordinal);
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
