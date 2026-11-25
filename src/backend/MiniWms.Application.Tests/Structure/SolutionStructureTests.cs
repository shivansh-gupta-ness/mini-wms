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
    [Trait("AC", "TASK-016.1")]
    public void Backend_repo_context_is_short_and_lists_layout_and_commands()
    {
        var path = Path.Combine(RepoLocator.BackendRoot, "REPO_CONTEXT.md");
        var text = File.ReadAllText(path);

        Assert.True(
            File.ReadAllLines(path).Length <= 500,
            "REPO_CONTEXT.md is over 500 lines: split it into child REPO_CONTEXT.md files next to the code they describe and list them in the parent");
        Assert.Contains("MiniWms.sln", text, StringComparison.Ordinal);
        Assert.Contains("dotnet build src/backend", text, StringComparison.Ordinal);
        Assert.Contains("dotnet test src/backend", text, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project src/backend/MiniWms.Api", text, StringComparison.Ordinal);
        Assert.Contains("ADR-001", text, StringComparison.Ordinal);
    }

    // TASK-016 AC5: every REPO_CONTEXT.md is within 500 lines and listed by its nearest parent REPO_CONTEXT.md.
    [Fact]
    [Trait("AC", "TASK-016.5")]
    public void Real_backend_tree_has_no_REPO_CONTEXT_problems()
    {
        var root = RepoLocator.BackendRoot;

        Assert.True(File.Exists(Path.Combine(root, "REPO_CONTEXT.md")), "src/backend/REPO_CONTEXT.md is missing");
        var problems = RepoContextProblems(root, "src/backend");
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Theory]
    [InlineData("MiniWms.Api/REPO_CONTEXT.md")]
    [InlineData("./MiniWms.Api/REPO_CONTEXT.md")]
    [InlineData("src/backend/MiniWms.Api/REPO_CONTEXT.md")]
    [InlineData("See `src/backend/MiniWms.Api/REPO_CONTEXT.md`.")]
    [Trait("AC", "TASK-016.5")]
    public void Sample_child_listed_with_a_dot_slash_or_repo_relative_path_passes(string listing)
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, listing));
        tree.Write("MiniWms.Api/REPO_CONTEXT.md", Lines(5));

        Assert.Empty(RepoContextProblems(tree.Root, "src/backend"));
    }

    [Theory]
    [InlineData("Other/MiniWms.Api/REPO_CONTEXT.md")]
    [InlineData("../MiniWms.Api/REPO_CONTEXT.md")]
    [InlineData("src/frontend/MiniWms.Api/REPO_CONTEXT.md")]
    [InlineData("xsrc/backend/MiniWms.Api/REPO_CONTEXT.md")]
    [Trait("AC", "TASK-016.5")]
    public void Sample_child_listed_with_a_wrong_prefix_fails_and_names_the_child(string listing)
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, listing));
        tree.Write("MiniWms.Api/REPO_CONTEXT.md", Lines(5));

        var problem = Assert.Single(RepoContextProblems(tree.Root, "src/backend"));
        Assert.StartsWith("MiniWms.Api/REPO_CONTEXT.md", problem, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("src/backend/MiniWms.Api/Deep/REPO_CONTEXT.md", true)]
    [InlineData("./Deep/REPO_CONTEXT.md", true)]
    [InlineData("src/backend/Deep/REPO_CONTEXT.md", false)]
    [Trait("AC", "TASK-016.5")]
    public void Sample_grandchild_listed_in_its_parent_with_a_repo_relative_path(string listing, bool passes)
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, "MiniWms.Api/REPO_CONTEXT.md"));
        tree.Write("MiniWms.Api/REPO_CONTEXT.md", Lines(5, listing));
        tree.Write("MiniWms.Api/Deep/REPO_CONTEXT.md", Lines(5));

        var problems = RepoContextProblems(tree.Root, "src/backend");

        Assert.Equal(passes, problems.Count == 0);
    }

    [Fact]
    [Trait("AC", "TASK-016.5")]
    public void Sample_child_of_500_lines_plus_a_final_newline_passes()
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, "Api/REPO_CONTEXT.md"));
        tree.Write("Api/REPO_CONTEXT.md", Lines(500) + "\n");

        Assert.Empty(RepoContextProblems(tree.Root));
    }

    [Fact]
    [Trait("AC", "TASK-016.5")]
    public void Sample_child_of_501_lines_plus_a_final_newline_fails_and_names_the_child()
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, "Api/REPO_CONTEXT.md"));
        tree.Write("Api/REPO_CONTEXT.md", Lines(501) + "\n");

        var problem = Assert.Single(RepoContextProblems(tree.Root));
        Assert.StartsWith("Api/REPO_CONTEXT.md", problem, StringComparison.Ordinal);
        Assert.Contains("split it into child REPO_CONTEXT.md files next to the code they describe and list them in the parent", problem, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Foo/REPO_CONTEXT.md.bak")]
    [InlineData("Foo/REPO_CONTEXT.mdx")]
    [Trait("AC", "TASK-016.5")]
    public void Sample_child_listed_only_with_a_longer_file_name_fails_and_names_the_child(string listing)
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, listing));
        tree.Write("Foo/REPO_CONTEXT.md", Lines(5));

        var problem = Assert.Single(RepoContextProblems(tree.Root));
        Assert.StartsWith("Foo/REPO_CONTEXT.md", problem, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("See Foo/REPO_CONTEXT.md.")]
    [InlineData("See `Foo/REPO_CONTEXT.md`")]
    [InlineData("[Foo](Foo/REPO_CONTEXT.md)")]
    [InlineData("Foo/REPO_CONTEXT.md, and more")]
    [InlineData("Foo/REPO_CONTEXT.md: the foo layout")]
    [InlineData("| Foo/REPO_CONTEXT.md | foo |")]
    [InlineData("Foo/REPO_CONTEXT.md")]
    [Trait("AC", "TASK-016.5")]
    public void Sample_child_listed_with_punctuation_after_the_name_passes(string listing)
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, listing));
        tree.Write("Foo/REPO_CONTEXT.md", Lines(5));

        Assert.Empty(RepoContextProblems(tree.Root));
    }

    [Fact]
    [Trait("AC", "TASK-016.5")]
    public void Sample_tree_with_parent_child_and_grandchild_passes()
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, "- `Api/REPO_CONTEXT.md`"));
        tree.Write("Api/REPO_CONTEXT.md", Lines(5, "- `Deep/REPO_CONTEXT.md`"));
        tree.Write("Api/Deep/REPO_CONTEXT.md", Lines(5));

        Assert.Empty(RepoContextProblems(tree.Root));
    }

    [Fact]
    [Trait("AC", "TASK-016.5")]
    public void Sample_child_of_501_lines_fails_and_names_the_child()
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, "Api/REPO_CONTEXT.md"));
        tree.Write("Api/REPO_CONTEXT.md", Lines(501));

        var problem = Assert.Single(RepoContextProblems(tree.Root));
        Assert.Contains("Api/REPO_CONTEXT.md", problem, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TASK-016.5")]
    public void Sample_child_of_exactly_500_lines_passes()
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, "Api/REPO_CONTEXT.md"));
        tree.Write("Api/REPO_CONTEXT.md", Lines(500));

        Assert.Empty(RepoContextProblems(tree.Root));
    }

    [Fact]
    [Trait("AC", "TASK-016.5")]
    public void Sample_unlisted_child_fails_and_names_the_child()
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5));
        tree.Write("Api/REPO_CONTEXT.md", Lines(5));

        var problem = Assert.Single(RepoContextProblems(tree.Root));
        Assert.Contains("Api/REPO_CONTEXT.md", problem, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TASK-016.5")]
    public void Sample_child_listed_only_by_a_longer_folder_name_fails()
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, "OtherFoo/REPO_CONTEXT.md"));
        tree.Write("Foo/REPO_CONTEXT.md", Lines(5));
        tree.Write("OtherFoo/REPO_CONTEXT.md", Lines(5));

        var problem = Assert.Single(RepoContextProblems(tree.Root));
        Assert.StartsWith("Foo/REPO_CONTEXT.md", problem, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TASK-016.5")]
    public void Sample_grandchild_listed_only_in_the_top_file_fails_and_names_the_grandchild()
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5, "Api/REPO_CONTEXT.md", "Api/Deep/REPO_CONTEXT.md"));
        tree.Write("Api/REPO_CONTEXT.md", Lines(5));
        tree.Write("Api/Deep/REPO_CONTEXT.md", Lines(5));

        var problem = Assert.Single(RepoContextProblems(tree.Root));
        Assert.StartsWith("Api/Deep/REPO_CONTEXT.md", problem, StringComparison.Ordinal);
        Assert.Contains(" in Api/REPO_CONTEXT.md", problem, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData("TestResults")]
    [InlineData("node_modules")]
    [InlineData("dist")]
    [InlineData("coverage")]
    [InlineData(".angular")]
    [Trait("AC", "TASK-016.5")]
    public void Sample_REPO_CONTEXT_inside_a_skipped_folder_is_ignored(string skipped)
    {
        using var tree = new TempTree();
        tree.Write("REPO_CONTEXT.md", Lines(5));
        tree.Write($"Api/{skipped}/REPO_CONTEXT.md", Lines(600));

        Assert.Empty(RepoContextProblems(tree.Root));
    }

    private static readonly string[] SkippedFolders = ["bin", "obj", "TestResults", "node_modules", "dist", "coverage", ".angular"];

    private static string Lines(int count, params string[] listed) =>
        string.Join('\n', listed.Concat(Enumerable.Repeat("line", Math.Max(0, count - listed.Length))));

    // prefix is the root folder's repo-relative path (for example src/backend); empty for a sample tree.
    private static List<string> RepoContextProblems(string root, string prefix = "")
    {
        var problems = new List<string>();
        var files = new List<string>();
        Collect(root, files);

        foreach (var file in files.Order(StringComparer.Ordinal))
        {
            var relativeToRoot = Path.GetRelativePath(root, file).Replace('\\', '/');

            if (File.ReadAllLines(file).Length > 500)
            {
                problems.Add($"{relativeToRoot} is over 500 lines: split it into child REPO_CONTEXT.md files next to the code they describe and list them in the parent");
            }

            var parent = NearestParent(root, file);
            if (parent is null)
            {
                continue;
            }

            var parentFolder = Path.GetDirectoryName(parent)!;
            var relativeToParent = Path.GetRelativePath(parentFolder, file).Replace('\\', '/');
            var parentFolderFromRoot = Path.GetRelativePath(root, parentFolder).Replace('\\', '/');
            var parentPrefix = string.Join('/', new[] { prefix, parentFolderFromRoot == "." ? string.Empty : parentFolderFromRoot }.Where(s => s.Length > 0));
            var prefixForms = parentPrefix.Length > 0
                ? @"(?:\./|" + System.Text.RegularExpressions.Regex.Escape(parentPrefix + "/") + ")?"
                : @"(?:\./)?";
            var listed = new System.Text.RegularExpressions.Regex(
                @"(?<![A-Za-z0-9_.\-/])" + prefixForms + System.Text.RegularExpressions.Regex.Escape(relativeToParent) + @"(?![A-Za-z0-9_\-/])(?!\.[A-Za-z0-9_\-])");
            if (!listed.IsMatch(File.ReadAllText(parent)))
            {
                problems.Add($"{relativeToRoot} is not listed as {relativeToParent} in {Path.GetRelativePath(root, parent).Replace('\\', '/')}");
            }
        }

        return problems;
    }

    private static void Collect(string folder, List<string> files)
    {
        var file = Path.Combine(folder, "REPO_CONTEXT.md");
        if (File.Exists(file))
        {
            files.Add(file);
        }

        foreach (var sub in Directory.GetDirectories(folder))
        {
            if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0)
            {
                continue;
            }

            if (!SkippedFolders.Contains(Path.GetFileName(sub), StringComparer.Ordinal))
            {
                Collect(sub, files);
            }
        }
    }

    /// <summary>The nearest REPO_CONTEXT.md above the file inside the root; null for the file directly in the root.</summary>
    private static string? NearestParent(string root, string file)
    {
        var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        var dir = Path.GetDirectoryName(Path.GetFullPath(file))!;
        if (string.Equals(dir, rootFull, StringComparison.Ordinal))
        {
            return null;
        }

        for (var up = Path.GetDirectoryName(dir); up is not null; up = Path.GetDirectoryName(up))
        {
            var candidate = Path.Combine(up, "REPO_CONTEXT.md");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            if (string.Equals(up, rootFull, StringComparison.Ordinal))
            {
                break;
            }
        }

        return null;
    }

    private sealed class TempTree : IDisposable
    {
        public TempTree()
        {
            Root = Path.Combine(Path.GetTempPath(), "miniwms-ctx-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Write(string relative, string content)
        {
            var path = Path.Combine(Root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    private static string? Property(XDocument doc, string name) =>
        doc.Descendants(name).FirstOrDefault()?.Value.Trim();

    private static string[] PackageReferences(string project) =>
        XDocument.Load(RepoLocator.ProjectFile(project))
            .Descendants("PackageReference")
            .Select(e => (string?)e.Attribute("Include") ?? string.Empty)
            .ToArray();
}
