using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace MiniWms.Api.Common.Errors;

/// <summary>
/// Fallback writer (ADR-003: every error is application/problem+json). The framework's default writer declines
/// when the request's <c>Accept</c> header does not allow JSON (for example only <c>text/html</c>), which would
/// leave an empty body. It is registered after the default one, so it only runs when the default declines.
/// </summary>
internal sealed class AlwaysJsonProblemDetailsWriter(
    IOptions<ProblemDetailsOptions> problemOptions, IOptions<JsonOptions> jsonOptions) : IProblemDetailsWriter
{
    public bool CanWrite(ProblemDetailsContext context) => true;

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        problem.Status ??= context.HttpContext.Response.StatusCode;

        // Same customizer the default writer applies (type, traceId, detail rule, 500 title).
        problemOptions.Value.CustomizeProblemDetails?.Invoke(context);

        return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(
            problem, problem.GetType(), jsonOptions.Value.SerializerOptions, "application/problem+json"));
    }
}
