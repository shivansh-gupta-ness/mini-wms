using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MiniWms.Api.Tests.Host;

/// <summary>A non-seekable read-only stream of <c>length</c> bytes that records how many bytes were read from it.</summary>
internal sealed class CountingSourceStream(long length) : Stream
{
    private long _position;

    public long BytesRead => Interlocked.Read(ref _position);

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Fill(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer) => Fill(buffer);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        new(Fill(buffer.Span));

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    // The body starts as a valid JSON object with an endless string value, so the JSON reader keeps reading
    // (it would otherwise answer 400 on the first bad byte, before the limit is reached).
    private static readonly byte[] Prefix = "{\"padding\":\""u8.ToArray();

    private int Fill(Span<byte> buffer)
    {
        var n = (int)Math.Min(buffer.Length, length - _position);
        for (var i = 0; i < n; i++)
        {
            buffer[i] = _position + i < Prefix.Length ? Prefix[_position + i] : (byte)'x';
        }

        Interlocked.Add(ref _position, n);
        return n;
    }
}

/// <summary>TASK-014: the 64 KB request body limit (AC1, AC2), the Development 500 (AC3) and the trait check (AC5).</summary>
public class RequestBodyLimitTests
{
    private const int Limit = 65_536;
    private const string ProblemJson = "application/problem+json";
    private const string AngularOrigin = "http://localhost:4200";
    private const string ZonesPath = "/api/v1/zones";
    private const string StackFrameText = "   at ";

    // AC1 (a)
    [Theory]
    [Trait("AC", "TASK-014.1")]
    [InlineData("POST", ZonesPath)]
    [InlineData("PUT", "/api/v1/zones/1")]
    [InlineData("PATCH", "/api/v1/zones/1")]
    [InlineData("GET", ZonesPath)]
    public async Task A_declared_body_over_the_limit_is_413_problem_json_without_a_stack_trace(string method, string path)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = Bytes(Limit + 1) };

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();

        AssertTooLargeProblem(response, text);
        Assert.DoesNotContain(StackFrameText, text);
        Assert.DoesNotContain("Exception", text);
    }

    // AC1 (b)
    [Theory]
    [Trait("AC", "TASK-014.1")]
    [InlineData("POST", ZonesPath)]
    [InlineData("PATCH", "/api/v1/zones/1")]
    public async Task A_streamed_body_without_Content_Length_over_the_limit_is_the_same_413_problem(string method, string path)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var source = new CountingSourceStream(Limit + 1000);
        using var content = new StreamContent(source);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = content };
        request.Headers.TransferEncodingChunked = true;
        Assert.Null(request.Content.Headers.ContentLength);

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();

        AssertTooLargeProblem(response, text);
        Assert.DoesNotContain(StackFrameText, text);
    }

    // AC1 (b): proves the server really sees a chunked body with no Content-Length (TestServer does send it that way).
    [Fact]
    [Trait("AC", "TASK-014.1")]
    public async Task A_streamed_body_reaches_the_server_chunked_with_no_Content_Length()
    {
        using var factory = new FramingProbeApiFactory();
        using var client = factory.CreateClient();
        using var content = new StreamContent(new CountingSourceStream(Limit + 1000));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, ZonesPath) { Content = content };
        request.Headers.TransferEncodingChunked = true;

        using var response = await client.SendAsync(request);

        Assert.Null(factory.SeenContentLength);
        Assert.Equal("chunked", factory.SeenTransferEncoding);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    // AC1 (c), streamed: the server side stops pulling bytes just past the limit. The count is taken on the server
    // side of the pipe (in front of the limit), because TestServer's client side pumps the whole source regardless.
    [Fact]
    [Trait("AC", "TASK-014.1")]
    public async Task A_streamed_body_over_the_limit_is_not_read_to_the_end_by_the_server()
    {
        using var factory = new FramingProbeApiFactory();
        using var client = factory.CreateClient();
        using var content = new StreamContent(new CountingSourceStream(50_000_000));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, ZonesPath) { Content = content };
        request.Headers.TransferEncodingChunked = true;

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.InRange(factory.ServerBytesRead, Limit + 1, Limit + 65_536);
    }

    // AC1 (c), declared: the server never reads the body at all.
    [Fact]
    [Trait("AC", "TASK-014.1")]
    public async Task A_declared_body_over_the_limit_is_not_read_by_the_server()
    {
        using var factory = new FramingProbeApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, ZonesPath) { Content = Bytes(Limit + 1) };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal(Limit + 1, factory.SeenContentLength);
        Assert.Equal(0, factory.ServerBytesRead);
    }

    // AC1 (d)
    [Theory]
    [Trait("AC", "TASK-014.1")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task The_413_carries_the_CORS_header_for_an_allowed_origin(bool declaredLength)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using HttpContent content = declaredLength ? Bytes(Limit + 1) : new StreamContent(new CountingSourceStream(Limit + 1));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, ZonesPath) { Content = content };
        request.Headers.Add("Origin", AngularOrigin);
        if (!declaredLength)
        {
            request.Headers.TransferEncodingChunked = true;
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal([AngularOrigin], response.Headers.GetValues("Access-Control-Allow-Origin"));
    }

    // AC1 (e)
    [Fact]
    [Trait("AC", "TASK-014.1")]
    public async Task The_413_has_exactly_type_title_status_and_traceId()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, ZonesPath) { Content = Bytes(Limit + 1) };

        using var response = await client.SendAsync(request);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(
            ["status", "title", "traceId", "type"],
            json.RootElement.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
    }

    // AC2
    [Fact]
    [Trait("AC", "TASK-014.2")]
    public async Task A_body_of_exactly_the_limit_with_a_Content_Length_is_accepted()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, ZonesPath) { Content = ZoneBodyOfExactlyTheLimit() };
        Assert.Equal(Limit, request.Content.Headers.ContentLength);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    [Trait("AC", "TASK-014.2")]
    public async Task A_streamed_body_of_exactly_the_limit_without_Content_Length_is_accepted()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var bytes = await ZoneBodyOfExactlyTheLimit().ReadAsByteArrayAsync();
        using var content = new StreamContent(new NonSeekableStream(bytes));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, ZonesPath) { Content = content };
        request.Headers.TransferEncodingChunked = true;
        Assert.Null(request.Content.Headers.ContentLength);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    [Trait("AC", "TASK-014.2")]
    [Trait("AC", "TASK-014.4")]
    public async Task A_normal_small_request_still_works()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent(
            """{"code":"SMALL","name":"Small zone","type":"Storage"}""", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(ZonesPath, content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // AC3
    [Theory]
    [Trait("AC", "TASK-014.3")]
    [InlineData(null)]
    [InlineData("text/html")]
    public async Task In_Development_an_unhandled_exception_is_still_the_generic_500_problem(string? accept)
    {
        using var factory = new DevelopmentErrorApiFactory();
        using var client = factory.CreateClient();
        Assert.True(factory.Services.GetRequiredService<IHostEnvironment>().IsDevelopment());
        using var request = new HttpRequestMessage(HttpMethod.Get, DevelopmentErrorApiFactory.ThrowPath);
        if (accept is not null)
        {
            request.Headers.Accept.ParseAdd(accept);
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(text);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("An unexpected error occurred.", json.RootElement.GetProperty("title").GetString());
        Assert.False(json.RootElement.TryGetProperty("detail", out _));
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain(DevelopmentErrorApiFactory.Secret, text);
        Assert.DoesNotContain(StackFrameText, text);
        Assert.DoesNotContain("SecretDevelopmentException", text);
        Assert.DoesNotContain("Exception", text);
    }

    [Fact]
    [Trait("AC", "TASK-014.3")]
    public void The_Development_factory_really_runs_in_the_Development_environment()
    {
        using var factory = new DevelopmentErrorApiFactory();

        var environment = factory.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal("Development", environment.EnvironmentName);
    }

    // AC4 is proved by running the unchanged existing suite (no existing test file is edited).

    // AC5
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [Trait("AC", "TASK-014.5")]
    public void This_assembly_has_proving_tests_tagged_for_the_acceptance_criterion(int ac) =>
        Assert.True(TaggedCount(typeof(RequestBodyLimitTests).Assembly, $"TASK-014.{ac}") > 0,
            $"No test is tagged [Trait(\"AC\", \"TASK-014.{ac}\")].");

    private static int TaggedCount(Assembly assembly, string expected) =>
        assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(m => m.CustomAttributes)
            .Count(a => a.AttributeType.Name == "TraitAttribute"
                && (string?)a.ConstructorArguments[0].Value == "AC"
                && (string?)a.ConstructorArguments[1].Value == expected);

    private static void AssertTooLargeProblem(HttpResponseMessage response, string text)
    {
        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("error", root.GetProperty("type").GetString());
        Assert.Equal("Request body too large.", root.GetProperty("title").GetString());
        Assert.Equal(413, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("traceId").GetString()));
        Assert.False(root.TryGetProperty("detail", out _));
        Assert.False(root.TryGetProperty("errors", out _));
    }

    private static ByteArrayContent Bytes(int length)
    {
        var content = new ByteArrayContent(Enumerable.Repeat((byte)'x', length).ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    private static ByteArrayContent ZoneBodyOfExactlyTheLimit()
    {
        const string prefix = """{"code":"EDGE","name":"Edge zone","type":"Storage","padding":""";
        var paddingLength = Limit - prefix.Length - "\"\"}".Length;
        var json = prefix + "\"" + new string('x', paddingLength) + "\"}";
        var bytes = Encoding.UTF8.GetBytes(json);
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    private sealed class NonSeekableStream(byte[] bytes) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
