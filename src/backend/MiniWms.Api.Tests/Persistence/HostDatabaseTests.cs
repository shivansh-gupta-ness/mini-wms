using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniWms.Api.Tests.Host;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>
/// TASK-004 AC5 (the host creates and migrates the database in Development) and AC9 (every test factory uses its own
/// temporary database, never App_Data/miniwms.db). Nothing here writes into the real src/backend/MiniWms.Api/App_Data.
/// </summary>
public class HostDatabaseTests
{
    private const string RelativeConnectionString = "Data Source=App_Data/miniwms.db";

    // AC5
    [Fact]
    [Trait("AC", "TASK-004.5")]
    public void Appsettings_default_connection_string_is_the_relative_App_Data_path()
    {
        var path = Path.Combine(RepoLocator.ApiProjectFolder, "appsettings.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));

        var value = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("Default").GetString();

        Assert.Equal(RelativeConnectionString, value);
    }

    [Fact]
    [Trait("AC", "TASK-004.5")]
    public void Development_start_creates_the_database_file_under_App_Data_of_the_content_root_and_applies_migrations()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), $"miniwms-host-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRoot);
        try
        {
            using (var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseContentRoot(contentRoot);
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = RelativeConnectionString,
                    }));
            }))
            {
                using var scope = factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var pending = context.Database.GetPendingMigrations().ToList();

                Assert.True(File.Exists(Path.Combine(contentRoot, "App_Data", "miniwms.db")));
                Assert.Empty(pending);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    // AC9
    [Fact]
    [Trait("AC", "TASK-004.9")]
    public void Two_factory_instances_get_two_different_database_files_under_the_temp_folder()
    {
        using var first = new ApiFactory();
        using var second = new ApiFactory();

        Assert.NotEqual(first.DatabasePath, second.DatabasePath);
        Assert.Equal(TempFolder, Path.GetDirectoryName(first.DatabasePath));
        Assert.Equal(TempFolder, Path.GetDirectoryName(second.DatabasePath));
    }

    [Fact]
    [Trait("AC", "TASK-004.9")]
    public void ApiFactory_host_uses_its_temporary_database_and_creates_the_file()
    {
        using var factory = new ApiFactory();

        var dataSource = HostDataSource(factory);

        Assert.Equal(factory.DatabasePath, dataSource);
        Assert.True(File.Exists(factory.DatabasePath));
    }

    [Fact]
    [Trait("AC", "TASK-004.9")]
    public void ThrowingApiFactory_host_uses_its_temporary_database_and_creates_the_file()
    {
        using var factory = new ThrowingApiFactory();

        var dataSource = HostDataSource(factory);

        Assert.Equal(TempFolder, Path.GetDirectoryName(factory.DatabasePath));
        Assert.Equal(factory.DatabasePath, dataSource);
        Assert.True(File.Exists(factory.DatabasePath));
    }

    [Fact]
    [Trait("AC", "TASK-004.9")]
    public void ErrorRoutesApiFactory_host_uses_its_temporary_database_and_creates_the_file()
    {
        using var factory = new ErrorRoutesApiFactory();

        var dataSource = HostDataSource(factory);

        Assert.Equal(TempFolder, Path.GetDirectoryName(factory.DatabasePath));
        Assert.Equal(factory.DatabasePath, dataSource);
        Assert.True(File.Exists(factory.DatabasePath));
    }

    [Fact]
    [Trait("AC", "TASK-004.9")]
    public void Factory_host_does_not_use_the_real_App_Data_database()
    {
        using var factory = new ApiFactory();
        var realAppData = Path.Combine(RepoLocator.ApiProjectFolder, "App_Data");

        var dataSource = HostDataSource(factory);

        Assert.DoesNotContain(realAppData, dataSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("AC", "TASK-004.9")]
    public void Disposing_a_factory_removes_its_db_wal_and_shm_files()
    {
        var factory = new ApiFactory();
        _ = HostDataSource(factory);
        var path = factory.DatabasePath;
        // Guard: the file exists while the host is running, so the test really exercises the removal.
        Assert.True(File.Exists(path));

        factory.Dispose();

        Assert.False(File.Exists(path));
        Assert.False(File.Exists(path + "-wal"));
        Assert.False(File.Exists(path + "-shm"));
    }

    private static string TempFolder => Path.TrimEndingDirectorySeparator(Path.GetTempPath());

    /// <summary>Starts the host, then returns the database file the host's context really points at.</summary>
    private static string HostDataSource(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return new SqliteConnectionStringBuilder(context.Database.GetConnectionString()).DataSource;
    }
}
