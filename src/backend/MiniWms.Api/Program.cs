using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWms.Api.Common;
using MiniWms.Api.Common.Errors;
using MiniWms.Api.Common.RequestLimits;
using MiniWms.Api.Locations;
using MiniWms.Infrastructure;
using MiniWms.Infrastructure.Persistence;

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

        // detail is text written by our own code (business-rule 409), and only on 4xx. Never for 5xx, and never
        // exception text from unexpected exceptions: only our own handlers mark a response as allowed.
        var keepDetail = ProblemResponses.IsDetailAllowed(context.HttpContext)
            && problem.Status is >= 400 and < 500;
        if (!keepDetail)
        {
            problem.Detail = null;
        }

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

// Registered after AddProblemDetails so the default writer is consulted first; this one only takes over when it declines.
builder.Services.AddSingleton<IProblemDetailsWriter, AlwaysJsonProblemDetailsWriter>();

// Our own exception handlers run first, in this order; anything they do not handle stays a generic 500.
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<ConflictExceptionHandler>();
builder.Services.AddExceptionHandler<RequestBodyTooLargeExceptionHandler>();

// ADR-002: EF Core + SQLite; a relative database path resolves against the content root.
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddLocations();

var app = builder.Build();

// ADR-002: in Development only, create the database folder and apply pending migrations at startup.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var file = SqliteDataSource.FilePath(new SqliteConnectionStringBuilder(db.Database.GetConnectionString()), null);
    if (file is not null && Path.GetDirectoryName(file) is { } folder)
    {
        Directory.CreateDirectory(folder);
    }

    db.Database.Migrate();
    WarehouseSeeder.Seed(db, app.Configuration);
}

// CORS must wrap the error handlers so error responses keep their CORS headers.
app.UseCors(AngularDevCorsPolicy);
app.UseExceptionHandler();
app.UseStatusCodePages();

// TASK-014: after the error handlers (so the limit exception becomes a 413 problem), before every endpoint.
app.UseMiddleware<RequestBodyLimitMiddleware>();

// Every module registers through its own MapXxxEndpoints extension, called here.
app.MapHealthEndpoints();
app.MapLocationsEndpoints();

app.Run();

// Makes the entry point visible to the API integration tests (WebApplicationFactory).
public partial class Program;
