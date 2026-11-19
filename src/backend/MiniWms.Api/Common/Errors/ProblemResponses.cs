using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace MiniWms.Api.Common.Errors;

/// <summary>
/// Writes the 4xx bodies that our own code produces (ADR-003). The problem type and trace id are added by the
/// shared customizer in <c>Program.cs</c>; <c>detail</c> survives it only when it is marked here.
/// </summary>
internal static class ProblemResponses
{
    private const string AllowDetailKey = "MiniWms.AllowProblemDetail";

    /// <summary>Marks the response so the customizer keeps <c>detail</c> (4xx only).</summary>
    public static void AllowDetail(HttpContext context) => context.Items[AllowDetailKey] = true;

    public static bool IsDetailAllowed(HttpContext context) =>
        context.Items.TryGetValue(AllowDetailKey, out var value) && value is true;

    public static async ValueTask<bool> WriteValidation(
        HttpContext context, IProblemDetailsService problems, Exception exception, IDictionary<string, string[]> errors)
    {
        var problem = new HttpValidationProblemDetails(ToCamelCaseKeys(errors))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        };

        return await Write(context, problems, exception, problem);
    }

    public static async ValueTask<bool> WriteConflict(
        HttpContext context, IProblemDetailsService problems, Exception exception, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = detail,
        };

        AllowDetail(context);
        return await Write(context, problems, exception, problem);
    }

    private static async ValueTask<bool> Write(
        HttpContext context, IProblemDetailsService problems, Exception exception, ProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status!.Value;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    /// <summary>Field names in <c>errors</c> are camelCase; dotted paths are converted per segment.</summary>
    private static Dictionary<string, string[]> ToCamelCaseKeys(IDictionary<string, string[]> errors)
    {
        var result = new Dictionary<string, string[]>();
        foreach (var (field, messages) in errors)
        {
            var key = string.Join('.', field.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
            result[key] = result.TryGetValue(key, out var existing) ? [.. existing, .. messages] : messages;
        }

        return result;
    }
}
