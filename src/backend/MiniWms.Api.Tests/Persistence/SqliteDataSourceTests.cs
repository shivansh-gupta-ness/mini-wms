using Microsoft.Data.Sqlite;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Api.Tests.Persistence;

public class SqliteDataSourceTests
{
    [Theory]
    [InlineData("Data Source=:memory:")]
    [InlineData("Data Source=file:memdb1?mode=memory&cache=shared")]
    [InlineData("Data Source=x.db;Mode=Memory")]
    [InlineData("")]
    [Trait("AC", "TASK-004.5")]
    public void Non_file_sources_have_no_file_path(string connectionString) =>
        Assert.Null(SqliteDataSource.FilePath(new SqliteConnectionStringBuilder(connectionString), Path.GetTempPath()));

    [Fact]
    [Trait("AC", "TASK-004.5")]
    public void Relative_path_resolves_against_the_base_path()
    {
        var baseDir = Path.GetTempPath();
        var path = SqliteDataSource.FilePath(new SqliteConnectionStringBuilder("Data Source=App_Data/miniwms.db"), baseDir);
        Assert.Equal(Path.GetFullPath(Path.Combine(baseDir, "App_Data", "miniwms.db")), path);
    }
}
