using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using MiniWms.Domain.Common;

namespace MiniWms.Api.Common.Errors;

/// <summary>FluentValidation failures and Domain field-rule failures become a 400 with typed <c>errors</c>.</summary>
internal sealed class ValidationExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        IDictionary<string, string[]> errors;
        switch (exception)
        {
            case ValidationException fluent:
                errors = fluent.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
                break;
            case ValidationFailedException domain:
                errors = domain.Errors.ToDictionary(p => p.Key, p => p.Value);
                break;
            default:
                return false;
        }

        return await ProblemResponses.WriteValidation(httpContext, problems, exception, errors);
    }
}
