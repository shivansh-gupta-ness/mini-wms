using MiniWms.Api.Common;

const string AngularDevCorsPolicy = "AngularDevServer";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddPolicy(AngularDevCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

// ADR-003: every error is application/problem+json; 500 is generic in every environment (NFR-3).
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        var problem = context.ProblemDetails;
        problem.Extensions.Clear();
        problem.Detail = null;
        problem.Instance = null;
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        problem.Type = problem.Status switch
        {
            StatusCodes.Status400BadRequest => "validation",
            StatusCodes.Status401Unauthorized => "unauthorized",
            StatusCodes.Status403Forbidden => "forbidden",
            StatusCodes.Status404NotFound => "not-found",
            StatusCodes.Status409Conflict => "conflict",
            _ => "error",
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            problem.Title = "An unexpected error occurred.";
        }
    });

var app = builder.Build();

// CORS must wrap the error handlers so error responses keep their CORS headers.
app.UseCors(AngularDevCorsPolicy);
app.UseExceptionHandler();
app.UseStatusCodePages();

// Every module registers through its own MapXxxEndpoints extension, called here.
app.MapHealthEndpoints();

app.Run();

// Makes the entry point visible to the API integration tests (WebApplicationFactory).
public partial class Program;
