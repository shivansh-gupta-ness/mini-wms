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

        switch (problem.Status)
        {
            case StatusCodes.Status500InternalServerError:
                problem.Type = "error";
                problem.Title = "An unexpected error occurred.";
                break;
            case StatusCodes.Status404NotFound:
                problem.Type = "not-found";
                problem.Title = "Not Found";
                break;
            default:
                problem.Type = "error";
                break;
        }
    });

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(AngularDevCorsPolicy);

// Every module registers through its own MapXxxEndpoints extension, called here.
app.MapHealthEndpoints();

app.Run();

// Makes the entry point visible to the API integration tests (WebApplicationFactory).
public partial class Program;
