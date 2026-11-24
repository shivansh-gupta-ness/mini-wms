namespace MiniWms.Api.Common.Headers;

/// <summary>
/// Adds <c>X-Content-Type-Options: nosniff</c> and <c>Cache-Control: no-store</c> to every response (TASK-015).
/// </summary>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // OnStarting runs just before the response is sent, so it also covers what the exception handler and the
        // status-code pages write. The indexer overwrites any value set earlier on purpose: this API serves warehouse
        // data and none of it may be cached (a test pins this).
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Cache-Control"] = "no-store";
            return Task.CompletedTask;
        });

        await next(context);
    }
}
