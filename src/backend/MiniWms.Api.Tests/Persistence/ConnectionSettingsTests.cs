using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniWms.Infrastructure;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>
/// TASK-004 AC2: connection settings on every connection. These tests use the production wiring
/// (<c>AddInfrastructure</c> with an in-memory configuration), not hand-built options.
/// </summary>
public class ConnectionSettingsTests
{
    [Theory]
    [Trait("AC", "TASK-004.2")]
    [InlineData("foreign_keys", "1")]
    [InlineData("journal_mode", "wal")]
    [InlineData("busy_timeout", "5000")]
    public void Pragma_has_the_ADR_002_value_on_a_connection_opened_through_the_context(string pragma, string expected)
    {
        using var database = new TemporaryDatabase();
        using var provider = BuildProvider(database);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Database.OpenConnection();
        var actual = ReadPragma(context.Database.GetDbConnection(), pragma);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [Trait("AC", "TASK-004.2")]
    [InlineData("foreign_keys", "1")]
    [InlineData("journal_mode", "wal")]
    [InlineData("busy_timeout", "5000")]
    public async Task Pragma_has_the_ADR_002_value_on_a_connection_opened_asynchronously(string pragma, string expected)
    {
        using var database = new TemporaryDatabase();
        await using var provider = BuildProvider(database);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.OpenConnectionAsync();
        var actual = ReadPragma(context.Database.GetDbConnection(), pragma);

        Assert.Equal(expected, actual);
    }

    private static ServiceProvider BuildProvider(TemporaryDatabase database)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = database.ConnectionString })
            .Build();

        return new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
    }

    private static string ReadPragma(DbConnection connection, string pragma)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA {pragma}";
        return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture)!;
    }
}
