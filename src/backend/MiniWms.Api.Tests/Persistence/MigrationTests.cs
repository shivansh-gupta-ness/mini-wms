using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MiniWms.Api.Tests.Host;
using MiniWms.Infrastructure.Persistence;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>TASK-005 AC8 (migration and snapshot committed, no pending model changes) and AC7 (proving tests carry their trait).</summary>
public class MigrationTests
{
    private static string MigrationsFolder =>
        Path.Combine(RepoLocator.RepoRoot, "src", "backend", "MiniWms.Infrastructure", "Migrations");

    [Fact]
    [Trait("AC", "TASK-005.8")]
    public void Migrations_folder_contains_the_initial_migration_and_the_model_snapshot()
    {
        var migrations = Directory.GetFiles(MigrationsFolder, "*_InitialLocationsProducts.cs");
        var designers = Directory.GetFiles(MigrationsFolder, "*_InitialLocationsProducts.Designer.cs");

        Assert.Single(migrations.Except(designers));
        Assert.Single(designers);
        Assert.True(File.Exists(Path.Combine(MigrationsFolder, "AppDbContextModelSnapshot.cs")));
    }

    [Fact]
    [Trait("AC", "TASK-005.8")]
    public void Infrastructure_assembly_lists_the_initial_migration()
    {
        using var context = CreateProductionContext("Data Source=:memory:");

        var migrations = context.Database.GetMigrations().ToList();

        Assert.EndsWith("_InitialLocationsProducts", Assert.Single(migrations));
    }

    [Fact]
    [Trait("AC", "TASK-005.8")]
    public void Model_has_no_pending_changes_compared_with_the_snapshot()
    {
        using var context = CreateProductionContext("Data Source=:memory:");

        var pending = context.Database.HasPendingModelChanges();

        Assert.False(pending);
    }

    [Fact]
    [Trait("AC", "TASK-005.8")]
    public void Applying_the_migrations_to_a_new_database_leaves_no_pending_migration()
    {
        using var database = new TemporaryDatabase();
        using var context = CreateProductionContext(database.ConnectionString);

        var applied = context.Database.GetAppliedMigrations().ToList();
        var pending = context.Database.GetPendingMigrations().ToList();

        Assert.Single(applied);
        Assert.Empty(pending);
    }

    // AC7
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(8)]
    [Trait("AC", "TASK-005.7")]
    public void This_assembly_has_proving_tests_tagged_for_the_acceptance_criterion(int ac)
    {
        var expected = $"TASK-005.{ac}";

        var tagged = typeof(MigrationTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(m => m.CustomAttributes)
            .Count(a => a.AttributeType.Name == "TraitAttribute"
                && (string?)a.ConstructorArguments[0].Value == "AC"
                && (string?)a.ConstructorArguments[1].Value == expected);

        Assert.True(tagged > 0, $"No test is tagged [Trait(\"AC\", \"{expected}\")].");
    }

    // The production context: migrations and the snapshot belong to AppDbContext's assembly, not to the test-only derived context.
    private static AppDbContext CreateProductionContext(string connectionString) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connectionString).Options,
            new FakeClock(default), new FakeCurrentUser("migration-check"));
}
