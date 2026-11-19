using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using MiniWms.Application.Common;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>
/// A temporary SQLite database for one test: its own file (unique GUID name under the system temp folder),
/// built with the production <see cref="SqliteConnectionInterceptor"/>. Migrations are applied, and because there
/// are no migrations yet the schema of the test-only entity is created as well. Dispose disposes the contexts,
/// clears the connection pools and deletes the .db, -wal and -shm files.
/// </summary>
internal sealed class TemporaryDatabase : IDisposable
{
    // The interceptor is stateless; one instance keeps EF from building a new internal service provider per context.
    private static readonly SqliteConnectionInterceptor Interceptor = new();

    private readonly List<PersistenceTestDbContext> contexts = [];

    public TemporaryDatabase()
    {
        FilePath = TempDatabaseFiles.NewPath();
        ConnectionString = TempDatabaseFiles.ConnectionStringFor(FilePath);

        try
        {
            var context = CreateContext();
            context.Database.Migrate();
            // Migrate() has no migrations to apply yet, so create the test-only table directly.
            context.Database.GetService<IRelationalDatabaseCreator>().CreateTables();
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
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(ConnectionString)
            .AddInterceptors(Interceptor)
            .Options;

        var context = new PersistenceTestDbContext(
            options,
            clock ?? new FakeClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            currentUser ?? new FakeCurrentUser("fixture-user"));
        contexts.Add(context);
        return context;
    }

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
