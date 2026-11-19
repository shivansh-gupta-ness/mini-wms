using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MiniWms.Api.Tests.Host;

/// <summary>
/// The real host plus a probe in front of the whole pipeline that records what the server sees of the request
/// framing (declared Content-Length, Transfer-Encoding), so a test can tell a streamed body from a declared one.
/// </summary>
internal sealed class FramingProbeApiFactory : ApiFactory
{
    public long? SeenContentLength { get; private set; }

    public string? SeenTransferEncoding { get; private set; }

    /// <summary>How many body bytes the server side pulled from the request body (counted in front of the limit).</summary>
    public long ServerBytesRead => _counter?.Total ?? 0;

    private CountingBody? _counter;

    protected override void ConfigureTestHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new ProbeStartupFilter(this)));

    private sealed class CountingBody(Stream inner) : Stream
    {
        private long _total;

        public long Total => Interlocked.Read(ref _total);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => Add(inner.Read(buffer, offset, count));

        public override int Read(Span<byte> buffer) => Add(inner.Read(buffer));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Add(await inner.ReadAsync(buffer, cancellationToken));

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int Add(int read)
        {
            Interlocked.Add(ref _total, read);
            return read;
        }
    }

    private sealed class ProbeStartupFilter(FramingProbeApiFactory owner) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.Use((context, nextMiddleware) =>
                {
                    owner.SeenContentLength = context.Request.ContentLength;
                    owner.SeenTransferEncoding = context.Request.Headers.TransferEncoding.ToString();
                    owner._counter = new CountingBody(context.Request.Body);
                    context.Request.Body = owner._counter;
                    return nextMiddleware(context);
                });
                next(app);
            };
    }
}

/// <summary>
/// The real host running in the Development environment (TASK-014 AC3) plus a test-only route that throws an
/// exception carrying a secret text and a stack. The route is added after the host's own pipeline, so the exception
/// passes through the host's UseExceptionHandler.
/// </summary>
internal sealed class DevelopmentErrorApiFactory : ApiFactory
{
    public const string ThrowPath = "/__test/t14/throw";
    public const string Secret = "SECRET-dev-only-message";

    protected override void ConfigureTestHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services => services.AddTransient<IStartupFilter, ThrowStartupFilter>());
    }

    private sealed class ThrowStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                next(app);
                app.Use((context, nextMiddleware) =>
                    context.Request.Path == ThrowPath ? Throw() : nextMiddleware(context));
            };

        private static Task Throw() => throw new SecretDevelopmentException(Secret);
    }

    /// <summary>A distinctive exception type name, so a leak of the type name is detectable.</summary>
    internal sealed class SecretDevelopmentException(string message) : Exception(message);
}
