using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiniWms.Api.Tests.Host;
using MiniWms.Application.Common;

namespace MiniWms.Api.Tests.Locations;

/// <summary>
/// The real API host on its own temporary SQLite database (migrations applied, MAIN warehouse seeded), plus small
/// helpers to call it and read the JSON back. One instance per test; dispose removes the database files.
/// </summary>
internal sealed class LocationsHost : IDisposable
{
    public const string Api = "/api/v1";

    private readonly ApiFactory factory;

    public LocationsHost(ApiFactory? factory = null)
    {
        this.factory = factory ?? new ApiFactory();
        Client = this.factory.CreateClient();
    }

    public HttpClient Client { get; }

    // ---- sending

    public Task<HttpResponseMessage> GetAsync(string path) => Client.GetAsync(path);

    public Task<HttpResponseMessage> PostAsync(string path, object? body = null) =>
        Client.PostAsync(path, JsonContent.Create(body ?? new { }));

    public Task<HttpResponseMessage> PatchAsync(string path, object body) =>
        Client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, path) { Content = JsonContent.Create(body) });

    public Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string path, string? body, string? contentType = "application/json")
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, contentType ?? "application/json");
        }

        return Client.SendAsync(request);
    }

    // ---- reading

    public static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public async Task<JsonElement> GetJsonAsync(string path)
    {
        using var response = await GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await JsonAsync(response);
    }

    public static int Id(JsonElement item) => item.GetProperty("id").GetInt32();

    public static int Version(JsonElement item) => item.GetProperty("version").GetInt32();

    public static string Str(JsonElement item, string property) => item.GetProperty(property).GetString()!;

    public static string[] Errors(JsonElement problem, string field) =>
        [.. problem.GetProperty("errors").GetProperty(field).EnumerateArray().Select(e => e.GetString()!)];

    public static string[] Codes(JsonElement page) =>
        [.. page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("code").GetString()!)];

    // ---- creating

    public async Task<JsonElement> CreateZoneAsync(string code, string name = "Zone name", string type = "Storage")
    {
        using var response = await PostAsync($"{Api}/zones", new { code, name, type });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await JsonAsync(response);
    }

    public async Task<JsonElement> CreateBinAsync(int zoneId, string code, string name = "Bin name")
    {
        using var response = await PostAsync($"{Api}/bins", new { zoneId, code, name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await JsonAsync(response);
    }

    public async Task<JsonElement> DeactivateBinAsync(JsonElement bin)
    {
        using var response = await PostAsync($"{Api}/bins/{Id(bin)}/deactivate", new { version = Version(bin) });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await JsonAsync(response);
    }

    public async Task<JsonElement> DeactivateZoneAsync(JsonElement zone)
    {
        using var response = await PostAsync($"{Api}/zones/{Id(zone)}/deactivate", new { version = Version(zone) });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await JsonAsync(response);
    }

    public async Task<int> ZoneTotalAsync() => (await GetJsonAsync($"{Api}/zones")).GetProperty("totalItems").GetInt32();

    public async Task<int> BinTotalAsync() => (await GetJsonAsync($"{Api}/bins")).GetProperty("totalItems").GetInt32();

    // ---- asserting

    /// <summary>Asserts the ADR-003 error shape and returns the body.</summary>
    public static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string type)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await JsonAsync(response);
        Assert.Equal(type, problem.GetProperty("type").GetString());
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        return problem;
    }

    public void Dispose()
    {
        Client.Dispose();
        factory.Dispose();
    }
}

/// <summary>
/// The real host with the stock query replaced by a fake: bins listed in <see cref="Stock"/> hold that quantity,
/// every other bin holds zero (the stand-in for the Inventory module, FEAT-001 AC 9).
/// </summary>
internal sealed class StockApiFactory : ApiFactory
{
    public ConcurrentDictionary<int, int> Stock { get; } = new();

    protected override void ConfigureTestHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IStockQuery>();
            services.AddSingleton<IStockQuery>(new FakeStockQuery(Stock));
        });

    private sealed class FakeStockQuery(ConcurrentDictionary<int, int> stock) : IStockQuery
    {
        public Task<int> BinOnHandAsync(int binId, CancellationToken cancellationToken) =>
            Task.FromResult(stock.GetValueOrDefault(binId));

        public Task<int> ProductOnHandAsync(int productId, CancellationToken cancellationToken) =>
            Task.FromResult(0);
    }
}
