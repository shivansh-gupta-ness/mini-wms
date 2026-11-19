using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MiniWms.Infrastructure.Persistence;

/// <summary>Applies the ADR-002 connection settings every time a connection opens.</summary>
public sealed class SqliteConnectionInterceptor : DbConnectionInterceptor
{
    private const string Pragmas =
        "PRAGMA busy_timeout = 5000; PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
