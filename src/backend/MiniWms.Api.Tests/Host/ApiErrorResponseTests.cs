using System.Net;
using System.Text.Json;

namespace MiniWms.Api.Tests.Host;

/// <summary>BUG-001 regression tests: CORS on error responses, problem types and the correlation id.</summary>
public class ApiErrorResponseTests
{
    private const string AngularOrigin = "http://localhost:4200";
    private const string OtherOrigin = "http://evil.example";
    private const string HealthPath = "/api/v1/health";
    private const string UnknownPath = "/api/v1/does-not-exist";
    private const string ThrowPath = ThrowingApiFactory.ThrowPath;
    private const string GenericTitle = "An unexpected error occurred.";

    // AC1
    [Fact]
    [Trait("AC", "BUG-001.1")]
    public async Task Unknown_route_with_angular_origin_is_404_problem_json_with_cors_header()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await Send(client, HttpMethod.Get, UnknownPath, AngularOrigin);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal([AngularOrigin], AllowOrigin(response));
    }

    // AC2
    [Fact]
    [Trait("AC", "BUG-001.2")]
    public async Task Unhandled_exception_with_angular_origin_is_500_problem_json_with_cors_header_and_generic_title()
    {
        using var factory = new ThrowingApiFactory();
        using var client = factory.CreateClient();

        using var response = await Send(client, HttpMethod.Get, ThrowPath, AngularOrigin);
        var json = await ReadJson(response);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(GenericTitle, json.GetProperty("title").GetString());
        Assert.Equal([AngularOrigin], AllowOrigin(response));
    }

    // AC3
    [Fact]
    [Trait("AC", "BUG-001.3")]
    public async Task Unknown_route_from_other_origin_has_no_cors_header()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await Send(client, HttpMethod.Get, UnknownPath, OtherOrigin);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    [Trait("AC", "BUG-001.3")]
    public async Task Unhandled_exception_from_other_origin_has_no_cors_header()
    {
        using var factory = new ThrowingApiFactory();
        using var client = factory.CreateClient();

        using var response = await Send(client, HttpMethod.Get, ThrowPath, OtherOrigin);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    // AC4
    [Fact]
    [Trait("AC", "BUG-001.4")]
    public async Task Preflight_from_angular_origin_succeeds_with_allow_origin_methods_and_headers()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await SendPreflight(client, AngularOrigin);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal([AngularOrigin], AllowOrigin(response));
        Assert.True(response.Headers.Contains("Access-Control-Allow-Methods"));
        Assert.True(response.Headers.Contains("Access-Control-Allow-Headers"));
    }

    [Fact]
    [Trait("AC", "BUG-001.4")]
    public async Task Preflight_from_other_origin_has_no_allow_origin()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await SendPreflight(client, OtherOrigin);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    // AC5
    [Theory]
    [Trait("AC", "BUG-001.5")]
    [InlineData("/__test/status/400", "validation")]
    [InlineData("/__test/status/401", "unauthorized")]
    [InlineData("/__test/status/403", "forbidden")]
    [InlineData(UnknownPath, "not-found")]
    [InlineData("/__test/status/409", "conflict")]
    [InlineData(ThrowPath, "error")]
    public async Task Error_type_follows_adr_003_by_status(string path, string expectedType)
    {
        using var factory = new ErrorRoutesApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);
        var json = await ReadJson(response);

        Assert.Equal(expectedType, json.GetProperty("type").GetString());
    }

    [Theory]
    [Trait("AC", "BUG-001.5")]
    [InlineData("/__test/status/400", 400)]
    [InlineData("/__test/status/401", 401)]
    [InlineData("/__test/status/403", 403)]
    [InlineData(UnknownPath, 404)]
    [InlineData("/__test/status/409", 409)]
    [InlineData(ThrowPath, 500)]
    [InlineData("/__test/status/418", 418)]
    public async Task Error_status_property_equals_the_http_status(string path, int expectedStatus)
    {
        using var factory = new ErrorRoutesApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);
        var json = await ReadJson(response);

        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Equal(expectedStatus, json.GetProperty("status").GetInt32());
    }

    [Fact]
    [Trait("AC", "BUG-001.5")]
    public async Task Status_without_an_adr_003_type_has_type_error()
    {
        using var factory = new ErrorRoutesApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/__test/status/418");
        var json = await ReadJson(response);

        Assert.Equal("error", json.GetProperty("type").GetString());
    }

    // AC6
    [Theory]
    [Trait("AC", "BUG-001.6")]
    [InlineData("/__test/status/400")]
    [InlineData("/__test/status/401")]
    [InlineData("/__test/status/403")]
    [InlineData(UnknownPath)]
    [InlineData("/__test/status/409")]
    [InlineData(ThrowPath)]
    public async Task Error_body_has_a_non_empty_trace_id(string path)
    {
        using var factory = new ErrorRoutesApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);
        var json = await ReadJson(response);

        Assert.Equal(JsonValueKind.String, json.GetProperty("traceId").ValueKind);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("traceId").GetString()));
    }

    [Theory]
    [Trait("AC", "BUG-001.6")]
    [InlineData("/__test/status/400")]
    [InlineData("/__test/status/401")]
    [InlineData("/__test/status/403")]
    [InlineData(UnknownPath)]
    [InlineData("/__test/status/409")]
    [InlineData(ThrowPath)]
    public async Task Error_body_has_no_detail_and_no_instance(string path)
    {
        using var factory = new ErrorRoutesApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);
        var json = await ReadJson(response);

        Assert.False(json.TryGetProperty("detail", out _));
        Assert.False(json.TryGetProperty("instance", out _));
    }

    [Fact]
    [Trait("AC", "BUG-001.6")]
    public async Task Server_error_title_stays_generic()
    {
        using var factory = new ErrorRoutesApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(ThrowPath);
        var json = await ReadJson(response);

        Assert.Equal(GenericTitle, json.GetProperty("title").GetString());
    }

    private static string[] AllowOrigin(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.ToArray() : [];

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, string origin)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Origin", origin);
        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendPreflight(HttpClient client, string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, HealthPath);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "x-test");
        return client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
