using Microsoft.EntityFrameworkCore;
using MiniWms.Application.Common;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>
/// A temporary SQLite database for one test: its own file (unique GUID name under the system temp folder),
/// built with the production <see cref="SqliteConnectionInterceptor"/>. Migrations are applied (all module tables), and
/// the table of the test-only entity is created on top. Dispose disposes the contexts,
/// clears the connection pools and deletes the .db, -wal and -shm files.
/// </summary>
internal sealed class TemporaryDatabase : IDisposable
{
    // The interceptor is stateless; one instance keeps EF from building a new internal service provider per context.
    private static readonly SqliteConnectionInterceptor Interceptor = new();

    // The module tables come from the migrations; this is the one table that exists only in tests.
    private const string CreateItemsTable = """
        CREATE TABLE "Items" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_Items" PRIMARY KEY AUTOINCREMENT,
            "Name" TEXT NOT NULL,
            "CreatedAt" INTEGER NOT NULL,
            "CreatedBy" TEXT NOT NULL,
            "UpdatedAt" INTEGER NULL,
            "UpdatedBy" TEXT NULL,
            "Version" INTEGER NOT NULL
        )
        """;

    private readonly List<PersistenceTestDbContext> contexts = [];

    public TemporaryDatabase()
    {
        FilePath = TempDatabaseFiles.NewPath();
        ConnectionString = TempDatabaseFiles.ConnectionStringFor(FilePath);

        try
        {
            // Migrations belong to AppDbContext (the derived test context has a different type), so apply them
            // through the production context. They create every module table; only the test-only table is added here.
            using (var production = new AppDbContext(CreateOptions(), new FakeClock(default), new FakeCurrentUser("migrator")))
            {
                production.Database.Migrate();
            }

            CreateContext().Database.ExecuteSqlRaw(CreateItemsTable);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public string FilePath { get; }

    public string WalPath => FilePath + "-wal";

    public string ShmPath => FilePath + "-shm";

    public string ConnectionString { get; }

    public PersistenceTestDbContext CreateContext(IClock? clock = null, ICurrentUser? currentUser = null)
    {
        var context = new PersistenceTestDbContext(
            CreateOptions(),
            clock ?? new FakeClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            currentUser ?? new FakeCurrentUser("fixture-user"));
        contexts.Add(context);
        return context;
    }

    private DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(ConnectionString)
            .AddInterceptors(Interceptor)
            .Options;

    public void Dispose()
    {
        foreach (var context in contexts)
        {
            context.Dispose();
        }

        contexts.Clear();
        TempDatabaseFiles.Delete(FilePath);
    }
}
