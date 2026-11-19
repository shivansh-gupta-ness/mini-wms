using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MiniWms.Domain.Common;

namespace MiniWms.Api.Common.Errors;

/// <summary>
/// Business-rule conflicts (<see cref="DomainException"/>) and stale versions (<see cref="DbUpdateConcurrencyException"/>)
/// become a 409 <c>conflict</c>. Both texts are written by our own code, so they are safe to show as <c>detail</c>.
/// </summary>
internal sealed class ConflictExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public const string StaleVersionMessage = "The item was changed by someone else. Reload and try again.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var detail = exception switch
        {
            DomainException domain => domain.Message,
            DbUpdateConcurrencyException => StaleVersionMessage,
            _ => null,
        };

        return detail is not null
            && await ProblemResponses.WriteConflict(httpContext, problems, exception, detail);
    }
}
