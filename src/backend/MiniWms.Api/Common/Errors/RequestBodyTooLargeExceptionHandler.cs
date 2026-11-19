using Microsoft.AspNetCore.Diagnostics;

namespace MiniWms.Api.Common.Errors;

/// <summary>
/// A request body over the size limit (a 413 <see cref="BadHttpRequestException"/>, thrown by the limiting stream or
/// by Kestrel) becomes a 413 <c>error</c> problem with no <c>detail</c>. Any other exception is left to the next handler.
/// </summary>
internal sealed class RequestBodyTooLargeExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken) =>
        exception is BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge }
        && await ProblemResponses.WriteStatus(
            httpContext, problems, exception, StatusCodes.Status413PayloadTooLarge, "Request body too large.");
}
