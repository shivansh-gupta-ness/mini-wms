using Microsoft.EntityFrameworkCore;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>TASK-004 AC7: each test gets its own temporary database with the schema; files are removed afterwards.</summary>
public class TemporaryDatabaseTests
{
    [Fact]
    [Trait("AC", "TASK-004.7")]
    public void Two_fixtures_get_two_different_absolute_files_under_the_temp_folder()
    {
        using var first = new TemporaryDatabase();
        using var second = new TemporaryDatabase();

        Assert.NotEqual(first.FilePath, second.FilePath);
        Assert.True(Path.IsPathRooted(first.FilePath));
        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetTempPath()), Path.GetDirectoryName(first.FilePath));
        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetTempPath()), Path.GetDirectoryName(second.FilePath));
    }

    [Fact]
    [Trait("AC", "TASK-004.7")]
    public void Each_fixture_creates_its_database_file_with_the_schema_applied()
    {
        using var first = new TemporaryDatabase();
        using var second = new TemporaryDatabase();

        var firstCount = first.CreateContext().Items.Count();
        var secondCount = second.CreateContext().Items.Count();

        Assert.True(File.Exists(first.FilePath));
        Assert.True(File.Exists(second.FilePath));
        Assert.Equal(0, firstCount);
        Assert.Equal(0, secondCount);
    }

    [Fact]
    [Trait("AC", "TASK-004.7")]
    public void Rows_written_in_one_fixture_are_not_visible_in_another()
    {
        using var first = new TemporaryDatabase();
        using var second = new TemporaryDatabase();
        var writer = first.CreateContext();
        writer.Items.Add(new PersistenceTestItem { Name = "only in first" });
        writer.SaveChanges();

        var secondCount = second.CreateContext().Items.Count();

        Assert.Equal(0, secondCount);
    }

    [Fact]
    [Trait("AC", "TASK-004.7")]
    public void Dispose_removes_the_db_wal_and_shm_files()
    {
        var database = new TemporaryDatabase();
        var writer = database.CreateContext();
        // Keep the connection in use (not idle in the pool): SqliteConnection.ClearAllPools() in a parallel test
        // closes idle pooled connections of any database, which would remove the sidecar files before the guard below.
        writer.Database.OpenConnection();
        writer.Items.Add(new PersistenceTestItem { Name = "creates wal" });
        writer.SaveChanges();
        // Guard: the sidecar files exist while a connection is open, so the test really exercises their removal.
        Assert.True(File.Exists(database.WalPath));
        Assert.True(File.Exists(database.ShmPath));

        database.Dispose();

        Assert.False(File.Exists(database.FilePath));
        Assert.False(File.Exists(database.WalPath));
        Assert.False(File.Exists(database.ShmPath));
    }
}
