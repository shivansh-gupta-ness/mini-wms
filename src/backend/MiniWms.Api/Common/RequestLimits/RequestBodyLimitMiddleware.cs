using MiniWms.Api.Common.Errors;

namespace MiniWms.Api.Common.RequestLimits;

/// <summary>
/// Caps every request body at <see cref="MaxRequestBodyBytes"/> (TASK-014). A declared <c>Content-Length</c> over the
/// limit is answered with 413 without reading the body; otherwise the body stream is wrapped so that reading past the
/// limit throws a 413 <see cref="BadHttpRequestException"/>, which <c>RequestBodyTooLargeExceptionHandler</c> maps.
/// </summary>
internal sealed class RequestBodyLimitMiddleware(RequestDelegate next, IProblemDetailsService problems)
{
    public const int MaxRequestBodyBytes = 65_536;

    public const string Title = "Request body too large.";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > MaxRequestBodyBytes)
        {
            await ProblemResponses.WriteStatus(
                context, problems, null, StatusCodes.Status413PayloadTooLarge, Title);
            return;
        }

        context.Request.Body = new LimitedReadStream(context.Request.Body, MaxRequestBodyBytes);
        await next(context);
    }

    private sealed class LimitedReadStream(Stream inner, long limit) : Stream
    {
        private long _total;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            Count(inner.Read(buffer, offset, count));

        public override int Read(Span<byte> buffer) => Count(inner.Read(buffer));

        public override async Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Count(await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken));

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Count(await inner.ReadAsync(buffer, cancellationToken));

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int Count(int read)
        {
            _total += read;
            if (_total > limit)
            {
                throw new BadHttpRequestException(Title, StatusCodes.Status413PayloadTooLarge);
            }

            return read;
        }
    }
}
