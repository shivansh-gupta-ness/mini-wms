var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Host behaviour (health endpoint, error handling, CORS) is added in TASK-002.
app.Run();

// Makes the entry point visible to the API integration tests (WebApplicationFactory) added in TASK-002.
public partial class Program;
