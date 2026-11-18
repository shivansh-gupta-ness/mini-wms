using System.Text.Json;
using System.Xml.Linq;

namespace MiniWms.Application.Tests.Structure;

public class SolutionStructureTests
{
    private static readonly string[] Modules = ["Locations", "Products", "Inventory", "Inbound", "Outbound", "Auth"];

    // AC1: the solution contains the seven projects.
    [Fact]
    [Trait("AC", "TASK-001.1")]
    public void Solution_contains_the_seven_projects()
    {
        var sln = File.ReadAllText(Path.Combine(RepoLocator.BackendRoot, "MiniWms.sln"));

        foreach (var project in RepoLocator.SourceProjects.Concat(RepoLocator.TestProjects))
        {
            Assert.Contains($"\"{project}\"", sln, StringComparison.Ordinal);
            Assert.True(File.Exists(RepoLocator.ProjectFile(project)), $"{project}.csproj is missing");
        }
    }

    // AC2: ADR-001 reference rules.
    [Fact]
    [Trait("AC", "TASK-001.2")]
    public void Domain_references_nothing()
    {
        Assert.Empty(RepoLocator.ProjectReferences("MiniWms.Domain"));
        Assert.Empty(PackageReferences("MiniWms.Domain"));
    }

    [Fact]
    [Trait("AC", "TASK-001.2")]
    public void Application_references_only_Domain() =>
        Assert.Equal(["MiniWms.Domain"], RepoLocator.ProjectReferences("MiniWms.Application"));

    [Fact]
    [Trait("AC", "TASK-001.2")]
    public void Infrastructure_references_only_Application_and_Domain() =>
        Assert.Equal(["MiniWms.Application", "MiniWms.Domain"], RepoLocator.ProjectReferences("MiniWms.Infrastructure"));

    [Fact]
    [Trait("AC", "TASK-001.2")]
    public void Api_references_only_Application_and_Infrastructure() =>
        Assert.Equal(["MiniWms.Application", "MiniWms.Infrastructure"], RepoLocator.ProjectReferences("MiniWms.Api"));

    [Fact]
    [Trait("AC", "TASK-001.2")]
    public void Source_projects_never_reference_test_projects()
    {
        foreach (var project in RepoLocator.SourceProjects)
        {
            Assert.DoesNotContain(RepoLocator.ProjectReferences(project), r => r.EndsWith(".Tests", StringComparison.Ordinal));
        }
    }

    // AC3: SDK pin.
    [Fact]
    [Trait("AC", "TASK-001.3")]
    public void Global_json_pins_the_dotnet_10_sdk()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoLocator.RepoRoot, "global.json")));
        var sdk = doc.RootElement.GetProperty("sdk");

        Assert.Equal("10.0.300", sdk.GetProperty("version").GetString());
        Assert.Equal("latestFeature", sdk.GetProperty("rollForward").GetString());
    }

    // AC4: shared build settings.
    [Fact]
    [Trait("AC", "TASK-001.4")]
    public void Build_props_turn_on_nullable_implicit_usings_and_warnings_as_errors()
    {
        var props = XDocument.Load(Path.Combine(RepoLocator.BackendRoot, "Directory.Build.props"));

        Assert.Equal("enable", Property(props, "Nullable"));
        Assert.Equal("enable", Property(props, "ImplicitUsings"));
        Assert.Equal("true", Property(props, "TreatWarningsAsErrors"));
        Assert.Equal("net10.0", Property(props, "TargetFramework"));
    }

    [Fact]
    [Trait("AC", "TASK-001.4")]
    public void Editorconfig_sits_in_src_backend()
    {
        var path = Path.Combine(RepoLocator.BackendRoot, ".editorconfig");

        Assert.True(File.Exists(path));
        Assert.Contains("root = true", File.ReadAllText(path), StringComparison.Ordinal);
    }

    // AC5: central package management.
    [Fact]
    [Trait("AC", "TASK-001.5")]
    public void Package_versions_are_managed_centrally()
    {
        var props = XDocument.Load(Path.Combine(RepoLocator.BackendRoot, "Directory.Packages.props"));

        Assert.Equal("true", Property(props, "ManagePackageVersionsCentrally"));
        Assert.NotEmpty(props.Descendants("PackageVersion"));
    }

    [Fact]
    [Trait("AC", "TASK-001.5")]
    public void No_project_file_carries_a_package_version()
    {
        foreach (var project in RepoLocator.SourceProjects.Concat(RepoLocator.TestProjects))
        {
            foreach (var reference in XDocument.Load(RepoLocator.ProjectFile(project)).Descendants("PackageReference"))
            {
                Assert.Null(reference.Attribute("Version"));
                Assert.Null(reference.Attribute("VersionOverride"));
            }
        }
    }

    // AC6: Common/ in every source project; the module folders in Domain, where the entities and rules start.
    // Application, Infrastructure and Api get their module folders with each module's own ticket (keeps this PR under the 30-file limit).
    [Fact]
    [Trait("AC", "TASK-001.6")]
    public void Every_source_project_has_a_Common_folder()
    {
        foreach (var project in RepoLocator.SourceProjects)
        {
            var path = Path.Combine(RepoLocator.BackendRoot, project, "Common");
            Assert.True(Directory.Exists(path), $"{project}/Common is missing");
        }
    }

    [Fact]
    [Trait("AC", "TASK-001.6")]
    public void Domain_has_the_six_module_folders()
    {
        foreach (var module in Modules)
        {
            var path = Path.Combine(RepoLocator.BackendRoot, "MiniWms.Domain", module);
            Assert.True(Directory.Exists(path), $"MiniWms.Domain/{module} is missing");
        }
    }

    // AC8: backend repository context.
    [Fact]
    [Trait("AC", "TASK-001.8")]
    public void Backend_repo_context_is_short_and_lists_layout_and_commands()
    {
        var path = Path.Combine(RepoLocator.BackendRoot, "REPO_CONTEXT.md");
        var text = File.ReadAllText(path);

        Assert.True(File.ReadAllLines(path).Length < 80, "REPO_CONTEXT.md must be under 80 lines");
        Assert.Contains("MiniWms.sln", text, StringComparison.Ordinal);
        Assert.Contains("dotnet build src/backend", text, StringComparison.Ordinal);
        Assert.Contains("dotnet test src/backend", text, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project src/backend/MiniWms.Api", text, StringComparison.Ordinal);
        Assert.Contains("ADR-001", text, StringComparison.Ordinal);
    }

    private static string? Property(XDocument doc, string name) =>
        doc.Descendants(name).FirstOrDefault()?.Value.Trim();

    private static string[] PackageReferences(string project) =>
        XDocument.Load(RepoLocator.ProjectFile(project))
            .Descendants("PackageReference")
            .Select(e => (string?)e.Attribute("Include") ?? string.Empty)
            .ToArray();
}
