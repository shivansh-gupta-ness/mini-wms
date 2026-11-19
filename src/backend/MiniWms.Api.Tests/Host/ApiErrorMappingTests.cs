using System.Net;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniWms.Application.Common.Paging;
using MiniWms.Application.Common.Sorting;
using MiniWms.Application.Common.Validation;
using MiniWms.Domain.Common;
using MiniWms.Domain.Products;

namespace MiniWms.Api.Tests.Host;

/// <summary>
/// The real host plus test-only routes under /__test/t6/ that throw the exceptions the shared building blocks map.
/// The routes sit after the host's own pipeline, so the exceptions pass through its UseExceptionHandler.
/// </summary>
internal sealed class ErrorMappingApiFactory : ApiFactory
{
    public const string Secret = "SECRET-connection-string-and-stack";
    public const string BusinessMessage = "The bin still holds stock.";

    protected override void ConfigureTestHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services => services.AddTransient<IStartupFilter, RoutesStartupFilter>());

    private sealed class RoutesStartupFilter : IStartupFilter
    {
        private static readonly SortFields<Product> ProductSortFields =
            new SortFields<Product>().Add("name", p => p.Name).Add("sku", p => p.Sku);

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                next(app);
                app.Use((context, nextMiddleware) =>
                {
                    switch (context.Request.Path.Value)
                    {
                        case "/__test/t6/fluent":
                            throw new ValidationException(
                            [
                                new ValidationFailure("UnitOfMeasure", "Unit of measure is required."),
                                new ValidationFailure("ReorderLevel", "Reorder level must not be negative."),
                                new ValidationFailure("ReorderLevel", "Reorder level must be a whole number."),
                            ]);
                        case "/__test/t6/domain-validation":
                            throw new ValidationFailedException(
                                new Dictionary<string, string[]> { ["Name"] = ["Name is required."] });
                        case "/__test/t6/paged":
                            var query = context.Request.Query;
                            var paged = new PagedQuery(
                                int.Parse(query["page"].FirstOrDefault() ?? "1"),
                                int.Parse(query["pageSize"].FirstOrDefault() ?? "25"),
                                query["sort"].FirstOrDefault());
                            new PagedQueryValidator<Product>(ProductSortFields).ValidateAndThrow(paged);
                            context.Response.StatusCode = StatusCodes.Status204NoContent;
                            return Task.CompletedTask;
                        case "/__test/t6/conflict":
                            throw new DomainException(BusinessMessage);
                        case "/__test/t6/concurrency":
                            throw new DbUpdateConcurrencyException(Secret);
                        case "/__test/t6/generic":
                            throw new InvalidOperationException(Secret);
                        case "/__test/t6/status/404":
                            context.Response.StatusCode = StatusCodes.Status404NotFound;
                            return Task.CompletedTask;
                        case "/__test/t6/status/401":
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        default:
                            return nextMiddleware(context);
                    }
                });
            };
    }
}

/// <summary>TASK-006 AC4 to AC6 (API part): validation 400, conflict 409 and stale version 409, with the BUG-001 guarantees kept.</summary>
public class ApiErrorMappingTests
{
    private const string ProblemJson = "application/problem+json";
    private static readonly string[] AllowedProperties = ["type", "title", "status", "detail", "traceId", "errors"];

    // AC4
    [Theory]
    [Trait("AC", "TASK-006.4")]
    [InlineData("/__test/t6/fluent")]
    [InlineData("/__test/t6/domain-validation")]
    [InlineData("/__test/t6/paged?pageSize=101")]
    public async Task Validation_failures_are_400_problem_json_of_type_validation(string path)
    {
        var (response, json, _) = await Get(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("validation", json.GetProperty("type").GetString());
        Assert.Equal(400, json.GetProperty("status").GetInt32());
    }

    [Fact]
    [Trait("AC", "TASK-006.4")]
    public async Task A_FluentValidation_exception_keys_errors_by_camelCase_field_and_groups_messages()
    {
        var (_, json, _) = await Get("/__test/t6/fluent");

        var errors = json.GetProperty("errors");
        Assert.Equal(["unitOfMeasure", "reorderLevel"], errors.EnumerateObject().Select(p => p.Name));
        Assert.Equal(
            ["Unit of measure is required."],
            errors.GetProperty("unitOfMeasure").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(2, errors.GetProperty("reorderLevel").GetArrayLength());
    }

    [Fact]
    [Trait("AC", "TASK-006.4")]
    public async Task A_Domain_ValidationFailedException_keys_errors_by_camelCase_field()
    {
        var (_, json, _) = await Get("/__test/t6/domain-validation");

        var errors = json.GetProperty("errors");
        Assert.Equal(["name"], errors.EnumerateObject().Select(p => p.Name));
        Assert.Equal("Name is required.", errors.GetProperty("name")[0].GetString());
    }

    [Fact]
    [Trait("AC", "TASK-006.4")]
    public async Task A_real_PagedQueryValidator_failure_reports_every_bad_field_by_camelCase_key()
    {
        var (response, json, _) = await Get("/__test/t6/paged?page=0&pageSize=101&sort=colour");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var keys = json.GetProperty("errors").EnumerateObject().Select(p => p.Name).Order().ToArray();
        Assert.Equal(["page", "pageSize", "sort"], keys);
    }

    [Fact]
    [Trait("AC", "TASK-006.1")]
    public async Task A_pageSize_above_100_through_the_pipeline_is_a_field_level_400_on_pageSize()
    {
        var (response, json, _) = await Get("/__test/t6/paged?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["pageSize"], json.GetProperty("errors").EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    [Trait("AC", "TASK-006.2")]
    public async Task An_unknown_sort_field_through_the_pipeline_is_a_field_level_400_on_sort()
    {
        var (response, json, _) = await Get("/__test/t6/paged?sort=colour");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["sort"], json.GetProperty("errors").EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    [Trait("AC", "TASK-006.2")]
    public async Task Allowed_paging_and_sort_pass_the_pipeline()
    {
        var (response, _, _) = await Get("/__test/t6/paged?page=2&pageSize=100&sort=-name");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [Trait("AC", "TASK-006.4")]
    [InlineData("/__test/t6/fluent")]
    [InlineData("/__test/t6/domain-validation")]
    [InlineData("/__test/t6/paged?pageSize=0")]
    public async Task Validation_responses_have_a_trace_id_and_no_instance_and_no_stack_trace(string path)
    {
        var (_, json, body) = await Get(path);

        Assert.False(string.IsNullOrEmpty(json.GetProperty("traceId").GetString()));
        Assert.False(json.TryGetProperty("instance", out _));
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", body, StringComparison.Ordinal);
    }

    // AC5
    [Fact]
    [Trait("AC", "TASK-006.5")]
    public async Task A_DomainException_is_409_conflict_with_its_message_as_detail()
    {
        var (response, json, _) = await Get("/__test/t6/conflict");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("conflict", json.GetProperty("type").GetString());
        Assert.Equal(409, json.GetProperty("status").GetInt32());
        Assert.Equal(ErrorMappingApiFactory.BusinessMessage, json.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("traceId").GetString()));
        Assert.False(json.TryGetProperty("instance", out _));
    }

    [Fact]
    [Trait("AC", "TASK-006.5")]
    public async Task A_generic_exception_is_still_500_with_the_generic_title_and_no_detail()
    {
        var (response, json, body) = await Get("/__test/t6/generic");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("error", json.GetProperty("type").GetString());
        Assert.Equal("An unexpected error occurred.", json.GetProperty("title").GetString());
        Assert.False(json.TryGetProperty("detail", out _));
        Assert.DoesNotContain(ErrorMappingApiFactory.Secret, body, StringComparison.Ordinal);
    }

    [Theory]
    [Trait("AC", "TASK-006.5")]
    [InlineData("/__test/t6/status/404")]
    [InlineData("/__test/t6/status/401")]
    public async Task A_status_only_response_has_no_detail(string path)
    {
        var (_, json, _) = await Get(path);

        Assert.False(json.TryGetProperty("detail", out _));
        Assert.False(json.TryGetProperty("instance", out _));
    }

    [Theory]
    [Trait("AC", "TASK-006.5")]
    [InlineData("/__test/t6/fluent")]
    [InlineData("/__test/t6/domain-validation")]
    [InlineData("/__test/t6/conflict")]
    [InlineData("/__test/t6/concurrency")]
    [InlineData("/__test/t6/generic")]
    [InlineData("/__test/t6/status/404")]
    [InlineData("/__test/t6/status/401")]
    public async Task Every_error_body_keeps_to_the_allow_listed_properties(string path)
    {
        var (_, json, _) = await Get(path);

        Assert.All(json.EnumerateObject().Select(p => p.Name), name => Assert.Contains(name, AllowedProperties));
    }

    [Fact]
    [Trait("AC", "TASK-006.5")]
    public async Task Detail_is_sent_only_on_the_conflict_responses()
    {
        var withDetail = new List<string>();
        foreach (var path in new[] { "/__test/t6/fluent", "/__test/t6/domain-validation", "/__test/t6/conflict", "/__test/t6/concurrency", "/__test/t6/generic" })
        {
            var (_, json, _) = await Get(path);
            if (json.TryGetProperty("detail", out _))
            {
                withDetail.Add(path);
            }
        }

        Assert.Equal(["/__test/t6/conflict", "/__test/t6/concurrency"], withDetail);
    }

    // AC6 (API part)
    [Fact]
    [Trait("AC", "TASK-006.6")]
    public async Task A_DbUpdateConcurrencyException_is_409_conflict_with_the_fixed_reload_text()
    {
        var (response, json, _) = await Get("/__test/t6/concurrency");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("conflict", json.GetProperty("type").GetString());
        Assert.Equal("The item was changed by someone else. Reload and try again.", json.GetProperty("detail").GetString());
    }

    [Fact]
    [Trait("AC", "TASK-006.6")]
    public async Task The_concurrency_response_does_not_leak_the_exception_text()
    {
        var (_, _, body) = await Get("/__test/t6/concurrency");

        Assert.DoesNotContain(ErrorMappingApiFactory.Secret, body, StringComparison.Ordinal);
        Assert.DoesNotContain("DbUpdateConcurrencyException", body, StringComparison.Ordinal);
    }

    private static async Task<(HttpResponseMessage Response, JsonElement Json, string Body)> Get(string path)
    {
        using var factory = new ErrorMappingApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        var json = string.IsNullOrEmpty(body) ? default : JsonDocument.Parse(body).RootElement;
        return (response, json, body);
    }
}
