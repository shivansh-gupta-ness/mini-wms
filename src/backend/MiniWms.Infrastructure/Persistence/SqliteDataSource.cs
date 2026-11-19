using Microsoft.Data.Sqlite;

namespace MiniWms.Infrastructure.Persistence;

/// <summary>Finds the database file behind a SQLite connection string; null when it is not a plain file path.</summary>
public static class SqliteDataSource
{
    public static string? FilePath(SqliteConnectionStringBuilder builder, string? basePath)
    {
        var source = builder.DataSource;
        if (string.IsNullOrEmpty(source)
            || source == ":memory:"
            || source.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            || builder.Mode == SqliteOpenMode.Memory)
        {
            return null;
        }

        return Path.IsPathRooted(source) || basePath is null
            ? Path.GetFullPath(source)
            : Path.GetFullPath(Path.Combine(basePath, source));
    }
}
