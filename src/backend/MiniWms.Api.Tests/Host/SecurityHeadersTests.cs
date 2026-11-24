using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MiniWms.Api.Tests.Host;

/// <summary>TASK-015: X-Content-Type-Options: nosniff and Cache-Control: no-store on every response.</summary>
public class SecurityHeadersTests
{
    private const string AngularOrigin = "http://localhost:4200";
    private const string ZonesPath = "/api/v1/zones";
    private const string HealthPath = "/api/v1/health";
    private const string UnknownPath = "/api/v1/does-not-exist";
    private const string ValidZone = """{"code":"SEC1","name":"Security zone","type":"Storage"}""";
    private const int Limit = 65_536;

    // AC1 and AC2: success, 404 and 400 (kind: 200, 404, 400)
    [Theory]
    [Trait("AC", "TASK-015.1")]
    [InlineData(200)]
    [InlineData(404)]
    [InlineData(400)]
    public async Task Responses_carry_nosniff(int expected)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await SendKindAsync(client, expected);

        Assert.Equal(expected, (int)response.StatusCode);
        AssertNosniff(response);
    }

    [Theory]
    [Trait("AC", "TASK-015.2")]
    [InlineData(200)]
    [InlineData(404)]
    [InlineData(400)]
    public async Task Responses_carry_no_store(int expected)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await SendKindAsync(client, expected);

        Assert.Equal(expected, (int)response.StatusCode);
        AssertNoStore(response);
    }

    // A value set by an endpoint is replaced on purpose: the API serves warehouse data and none of it may be cached.
    // A future route that needs caching must change the middleware (and this test) deliberately.
    [Fact]
    [Trait("AC", "TASK-015.2")]
    public async Task A_Cache_Control_value_set_by_an_endpoint_is_replaced_by_no_store()
    {
        using var factory = new EndpointCacheControlApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(EndpointCacheControlApiFactory.Path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoStore(response);
        Assert.Null(response.Headers.CacheControl?.MaxAge);
        Assert.False(response.Headers.CacheControl?.Public);
    }

    // AC3: error responses carry both headers and the problem body is unchanged
    [Fact]
    [Trait("AC", "TASK-015.1")]
    [Trait("AC", "TASK-015.2")]
    [Trait("AC", "TASK-015.3")]
    public async Task A_400_validation_problem_has_both_headers_and_the_unchanged_body()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(ZonesPath, Json("{}"));
        var root = await ReadRoot(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertBothHeaders(response);
        AssertProblem(response, root, "validation", 400);
    }

    [Fact]
    [Trait("AC", "TASK-015.1")]
    [Trait("AC", "TASK-015.2")]
    [Trait("AC", "TASK-015.3")]
    public async Task A_404_problem_has_both_headers_and_the_unchanged_body()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(UnknownPath);
        var root = await ReadRoot(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertBothHeaders(response);
        AssertProblem(response, root, "not-found", 404);
        Assert.False(root.TryGetProperty("detail", out _));
    }

    [Fact]
    [Trait("AC", "TASK-015.3")]
    public async Task A_409_conflict_problem_has_both_headers_and_the_unchanged_body()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        // A zone that has a bin cannot be deactivated: the API answers 409.
        using var zoneResponse = await client.PostAsync(ZonesPath, Json(ValidZone));
        Assert.Equal(HttpStatusCode.Created, zoneResponse.StatusCode);
        using var zone = JsonDocument.Parse(await zoneResponse.Content.ReadAsStringAsync());
        var id = zone.RootElement.GetProperty("id").GetInt32();
        var version = zone.RootElement.GetProperty("version").GetInt32();
        using var bin = await client.PostAsync(
            "/api/v1/bins", Json($$"""{"zoneId":{{id}},"code":"B1","name":"Bin name"}"""));
        Assert.Equal(HttpStatusCode.Created, bin.StatusCode);

        using var response = await client.PostAsync(
            $"{ZonesPath}/{id}/deactivate", Json($$"""{"version":{{version}}}"""));
        var root = await ReadRoot(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        AssertBothHeaders(response);
        AssertProblem(response, root, "conflict", 409);
    }

    [Fact]
    [Trait("AC", "TASK-015.3")]
    public async Task A_status_code_page_409_has_both_headers_and_the_unchanged_body()
    {
        using var factory = new ErrorRoutesApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(ErrorRoutesApiFactory.StatusPathPrefix + "409");
        var root = await ReadRoot(response);

        AssertBothHeaders(response);
        AssertProblem(response, root, "conflict", 409);
        Assert.False(root.TryGetProperty("detail", out _));
    }

    [Fact]
    [Trait("AC", "TASK-015.3")]
    public async Task A_413_problem_has_both_headers_and_the_unchanged_body()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, ZonesPath) { Content = Bytes(Limit + 1) };

        using var response = await client.SendAsync(request);
        var root = await ReadRoot(response);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        AssertBothHeaders(response);
        Assert.Equal(
            ["status", "title", "traceId", "type"],
            root.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.Equal("Request body too large.", root.GetProperty("title").GetString());
    }

    [Fact]
    [Trait("AC", "TASK-015.3")]
    public async Task The_generic_500_has_both_headers_and_the_unchanged_body()
    {
        using var factory = new ThrowingApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(ThrowingApiFactory.ThrowPath);
        var text = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        AssertBothHeaders(response);
        AssertProblem(response, root, "error", 500);
        Assert.Equal("An unexpected error occurred.", root.GetProperty("title").GetString());
        Assert.False(root.TryGetProperty("detail", out _));
        Assert.DoesNotContain(ThrowingApiFactory.SecretText, text);
    }

    [Fact]
    [Trait("AC", "TASK-015.3")]
    public async Task The_Development_500_has_both_headers_and_the_unchanged_generic_body()
    {
        using var factory = new DevelopmentErrorApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(DevelopmentErrorApiFactory.ThrowPath);
        var text = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        AssertBothHeaders(response);
        AssertProblem(response, root, "error", 500);
        Assert.False(root.TryGetProperty("detail", out _));
        Assert.DoesNotContain(DevelopmentErrorApiFactory.Secret, text);
    }

    // AC4: CORS still works and the new headers are added
    [Fact]
    [Trait("AC", "TASK-015.4")]
    public async Task A_CORS_preflight_still_allows_the_angular_origin_and_carries_both_headers()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, ZonesPath);
        request.Headers.Add("Origin", AngularOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal([AngularOrigin], response.Headers.GetValues("Access-Control-Allow-Origin"));
        AssertBothHeaders(response);
    }

    [Fact]
    [Trait("AC", "TASK-015.4")]
    public async Task A_GET_from_the_angular_origin_keeps_Access_Control_Allow_Origin_and_carries_both_headers()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, HealthPath);
        request.Headers.Add("Origin", AngularOrigin);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([AngularOrigin], response.Headers.GetValues("Access-Control-Allow-Origin"));
        AssertBothHeaders(response);
    }

    // AC5
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [Trait("AC", "TASK-015.5")]
    public void This_assembly_has_proving_tests_tagged_for_the_acceptance_criterion(int ac) =>
        Assert.True(TaggedCount(typeof(SecurityHeadersTests), $"TASK-015.{ac}") > 0,
            $"No test is tagged [Trait(\"AC\", \"TASK-015.{ac}\")].");

    private static int TaggedCount(Type type, string expected) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .SelectMany(m => m.CustomAttributes)
            .Count(a => a.AttributeType.Name == "TraitAttribute"
                && (string?)a.ConstructorArguments[0].Value == "AC"
                && (string?)a.ConstructorArguments[1].Value == expected);

    private static Task<HttpResponseMessage> SendKindAsync(HttpClient client, int kind) => kind switch
    {
        200 => client.GetAsync(HealthPath),
        404 => client.GetAsync(UnknownPath),
        _ => client.PostAsync(ZonesPath, Json("{}")),
    };

    private static void AssertNosniff(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var values));
        Assert.Equal(["nosniff"], values);
    }

    private static void AssertNoStore(HttpResponseMessage response)
    {
        Assert.True(response.Headers.CacheControl?.NoStore == true);
        Assert.True(response.Headers.TryGetValues("Cache-Control", out var values));
        Assert.Equal(["no-store"], values);
    }

    private static void AssertBothHeaders(HttpResponseMessage response)
    {
        AssertNosniff(response);
        AssertNoStore(response);
    }

    private static void AssertProblem(HttpResponseMessage response, JsonElement root, string type, int status)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(type, root.GetProperty("type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.Equal(status, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("traceId").GetString()));
    }

    // The document is disposed; Clone() gives an element that stays valid after that.
    private static async Task<JsonElement> ReadRoot(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private sealed class EndpointCacheControlApiFactory : ApiFactory
    {
        public const string Path = "/__test/cache";

        protected override void ConfigureTestHost(IWebHostBuilder builder) =>
            builder.ConfigureServices(services => services.AddTransient<IStartupFilter, CacheStartupFilter>());

        private sealed class CacheStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
                app =>
                {
                    next(app);
                    app.Use((context, nextMiddleware) =>
                    {
                        if (context.Request.Path != Path)
                        {
                            return nextMiddleware(context);
                        }

                        context.Response.Headers["Cache-Control"] = "public, max-age=3600";
                        context.Response.StatusCode = 200;
                        return Task.CompletedTask;
                    });
                };
        }
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static ByteArrayContent Bytes(int length)
    {
        var content = new ByteArrayContent(Enumerable.Repeat((byte)'x', length).ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }
}
