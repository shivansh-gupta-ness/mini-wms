using Microsoft.Data.Sqlite;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>Names and removes temporary SQLite database files. Shared by the fixture and the API test factories.</summary>
internal static class TempDatabaseFiles
{
    private static readonly string[] SidecarSuffixes = ["", "-wal", "-shm"];

    /// <summary>A unique absolute path under the system temp folder. The file is not created.</summary>
    public static string NewPath() =>
        Path.Combine(Path.GetTempPath(), $"miniwms-test-{Guid.NewGuid():N}.db");

    public static string ConnectionStringFor(string path) => $"Data Source={path}";

    /// <summary>
    /// Closes every pooled connection first (Windows keeps the files locked while a pooled connection is open),
    /// then deletes the database file and its WAL sidecar files.
    /// </summary>
    public static void Delete(string path)
    {
        SqliteConnection.ClearAllPools();

        foreach (var suffix in SidecarSuffixes)
        {
            try
            {
                File.Delete(path + suffix);
            }
            catch (IOException)
            {
                // A temp file that stays behind must not fail the test that used it.
            }
        }
    }
}
