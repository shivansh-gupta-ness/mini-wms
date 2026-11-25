using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

namespace MiniWms.Application.Tests.Structure;

/// <summary>TASK-003: coverage tooling configuration and the coverage gate script (ADR-005).</summary>
public class CoverageToolingTests
{
    private const string ReportGeneratorPackage = "dotnet-reportgenerator-globaltool";

    private static readonly TimeSpan ScriptTimeout = TimeSpan.FromSeconds(60);

    private static readonly Lazy<ScriptRun> Run85 = new(() => RunGate("coverage-85.cobertura.xml"));
    private static readonly Lazy<ScriptRun> Run90 = new(() => RunGate("coverage-90.cobertura.xml"));
    private static readonly Lazy<ScriptRun> Run95 = new(() => RunGate("coverage-95.cobertura.xml"));
    private static readonly Lazy<ScriptRun> RunScope = new(() => RunGate("coverage-scope.cobertura.xml"));

    // AC1: the local tool manifest.
    [Fact]
    [Trait("AC", "TASK-003.1")]
    public void Tool_manifest_exists_in_the_repo_root() =>
        Assert.True(File.Exists(ManifestPath), $"{ManifestPath} is missing");

    [Fact]
    [Trait("AC", "TASK-003.1")]
    public void Tool_manifest_lists_reportgenerator_with_a_version()
    {
        var tool = ReadReportGeneratorTool();

        Assert.False(string.IsNullOrWhiteSpace(tool.GetProperty("version").GetString()));
    }

    [Fact]
    [Trait("AC", "TASK-003.1")]
    public void Tool_manifest_declares_the_reportgenerator_command()
    {
        var tool = ReadReportGeneratorTool();

        var commands = tool.GetProperty("commands").EnumerateArray().Select(c => c.GetString()).ToArray();

        Assert.Contains("reportgenerator", commands);
    }

    // AC2: coverlet and the runsettings.
    [Theory]
    [InlineData("MiniWms.Domain.Tests")]
    [InlineData("MiniWms.Application.Tests")]
    [InlineData("MiniWms.Api.Tests")]
    [Trait("AC", "TASK-003.2")]
    public void Test_project_references_coverlet_collector_without_a_version(string project)
    {
        var reference = XDocument.Load(RepoLocator.ProjectFile(project))
            .Descendants("PackageReference")
            .SingleOrDefault(e => (string?)e.Attribute("Include") == "coverlet.collector");

        Assert.NotNull(reference);
        Assert.Null(reference.Attribute("Version"));
        Assert.Null(reference.Attribute("VersionOverride"));
        Assert.Null(reference.Element("Version"));
    }

    [Fact]
    [Trait("AC", "TASK-003.2")]
    public void Directory_Packages_props_holds_the_coverlet_collector_version()
    {
        var version = XDocument.Load(Path.Combine(RepoLocator.BackendRoot, "Directory.Packages.props"))
            .Descendants("PackageVersion")
            .Single(e => (string?)e.Attribute("Include") == "coverlet.collector")
            .Attribute("Version");

        Assert.False(string.IsNullOrWhiteSpace(version?.Value));
    }

    [Fact]
    [Trait("AC", "TASK-003.2")]
    public void Runsettings_enables_the_XPlat_Code_Coverage_collector()
    {
        var collector = Collector();

        Assert.Equal("XPlat Code Coverage", (string?)collector.Attribute("friendlyName"));
    }

    [Fact]
    [Trait("AC", "TASK-003.2")]
    public void Runsettings_requests_the_cobertura_format() =>
        Assert.Equal("cobertura", Setting("Format"));

    // AC3: the report command writes and prints a per-assembly summary.
    [Fact]
    [Trait("AC", "TASK-003.3")]
    public void Report_command_writes_Summary_txt_under_the_report_folder()
    {
        var run = Run90.Value;

        Assert.False(run.TimedOut, run.Describe());
        Assert.True(run.SummaryExists, run.Describe());
        Assert.Contains("Covered lines", run.SummaryText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("MiniWms.Domain")]
    [InlineData("MiniWms.Application")]
    [InlineData("MiniWms.Api")]
    [Trait("AC", "TASK-003.3")]
    public void Report_command_prints_line_coverage_for_each_assembly(string assembly)
    {
        var run = Run90.Value;

        Assert.False(run.TimedOut, run.Describe());
        Assert.Contains($"  {assembly}: ", run.Stdout, StringComparison.Ordinal);
    }

    // AC4: scope.
    [Fact]
    [Trait("AC", "TASK-003.4")]
    public void Runsettings_includes_only_Domain_Application_and_Api() =>
        Assert.Equal("[MiniWms.Domain]*,[MiniWms.Application]*,[MiniWms.Api]*", Setting("Include"));

    [Theory]
    [InlineData("**/Program.cs")]
    [InlineData("**/Migrations/**")]
    [InlineData("**/*.g.cs")]
    [Trait("AC", "TASK-003.4")]
    public void Runsettings_excludes_file_pattern(string pattern) =>
        Assert.Contains(pattern, Setting("ExcludeByFile").Split(','));

    [Theory]
    [InlineData("GeneratedCodeAttribute")]
    [InlineData("CompilerGeneratedAttribute")]
    [Trait("AC", "TASK-003.4")]
    public void Runsettings_excludes_generated_code_attribute(string attribute) =>
        Assert.Contains(attribute, Setting("ExcludeByAttribute").Split(','));

    [Fact]
    [Trait("AC", "TASK-003.4")]
    public void Gate_passes_when_only_an_out_of_scope_assembly_is_uncovered()
    {
        var run = RunScope.Value;

        Assert.False(run.TimedOut, run.Describe());
        Assert.Equal(0, run.ExitCode);
    }

    [Fact]
    [Trait("AC", "TASK-003.4")]
    public void Out_of_scope_assembly_is_not_listed_in_the_output()
    {
        var run = RunScope.Value;

        Assert.False(run.TimedOut, run.Describe());
        Assert.DoesNotContain("MiniWms.Infrastructure", run.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TASK-003.4")]
    public void Out_of_scope_assembly_is_not_counted_in_the_combined_figure()
    {
        var run = RunScope.Value;

        Assert.False(run.TimedOut, run.Describe());
        Assert.Contains("Combined line coverage: 100% (20 of 20 lines)", run.Stdout, StringComparison.Ordinal);
    }

    // AC5: the 90% gate.
    [Fact]
    [Trait("AC", "TASK-003.5")]
    public void Gate_fails_with_exit_code_1_at_85_percent()
    {
        var run = Run85.Value;

        Assert.False(run.TimedOut, run.Describe());
        Assert.Equal(1, run.ExitCode);
    }

    [Fact]
    [Trait("AC", "TASK-003.5")]
    public void Gate_failure_names_the_percentage_the_threshold_and_the_shortfall()
    {
        var run = Run85.Value;

        Assert.False(run.TimedOut, run.Describe());
        Assert.Contains(
            "GATE FAILED: line coverage 85% is below the 90% threshold (shortfall 5 percentage points)",
            run.Stdout,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TASK-003.5")]
    public void Gate_passes_with_exit_code_0_at_exactly_90_percent()
    {
        var run = Run90.Value;

        Assert.False(run.TimedOut, run.Describe());
        Assert.Equal(0, run.ExitCode);
    }

    [Fact]
    [Trait("AC", "TASK-003.5")]
    public void Gate_passes_with_exit_code_0_at_95_percent()
    {
        var run = Run95.Value;

        Assert.False(run.TimedOut, run.Describe());
        Assert.Equal(0, run.ExitCode);
    }

    // AC7: documentation.
    [Theory]
    [InlineData("dotnet tool restore")]
    [InlineData("XPlat Code Coverage")]
    [InlineData("coverage.ps1")]
    [InlineData("-ExecutionPolicy Bypass")]
    [InlineData("--filter \"AC=")]
    [Trait("AC", "TASK-003.7")]
    public void Backend_REPO_CONTEXT_lists_the_command(string expected) =>
        Assert.Contains(expected, File.ReadAllText(BackendContextPath), StringComparison.Ordinal);

    [Fact]
    [Trait("AC", "TASK-003.7")]
    [Trait("AC", "TASK-016.1")]
    public void Backend_REPO_CONTEXT_stays_within_500_lines() =>
        Assert.True(
            File.ReadAllLines(BackendContextPath).Length <= 500,
            "REPO_CONTEXT.md is over 500 lines: split it into child REPO_CONTEXT.md files next to the code they describe and list them in the parent");

    private static string ManifestPath => Path.Combine(RepoLocator.RepoRoot, ".config", "dotnet-tools.json");

    private static string BackendContextPath => Path.Combine(RepoLocator.BackendRoot, "REPO_CONTEXT.md");

    private static string RunsettingsPath => Path.Combine(RepoLocator.BackendRoot, "coverage.runsettings");

    private static JsonElement ReadReportGeneratorTool()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ManifestPath));
        return doc.RootElement.GetProperty("tools").GetProperty(ReportGeneratorPackage).Clone();
    }

    private static XElement Collector() =>
        XDocument.Load(RunsettingsPath).Descendants("DataCollector").Single();

    private static string Setting(string name) =>
        Collector().Descendants(name).Single().Value;

    private static ScriptRun RunGate(string fixtureName)
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);
        var reportDir = Path.Combine(Path.GetTempPath(), "miniwms-cov-" + Guid.NewGuid().ToString("N"));
        try
        {
            var script = Path.Combine(RepoLocator.BackendRoot, "coverage.ps1");
            var info = new ProcessStartInfo("powershell")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var arg in new[]
                     {
                         "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script,
                         "-Cobertura", fixture, "-ReportDir", reportDir,
                     })
            {
                info.ArgumentList.Add(arg);
            }

            using var process = Process.Start(info) ?? throw new InvalidOperationException("powershell did not start");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(ScriptTimeout))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                return new ScriptRun(true, -1, stdout.Result, stderr.Result, false, string.Empty);
            }

            var summary = Path.Combine(reportDir, "Summary.txt");
            var exists = File.Exists(summary);
            return new ScriptRun(
                false,
                process.ExitCode,
                stdout.Result,
                stderr.Result,
                exists,
                exists ? File.ReadAllText(summary) : string.Empty);
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, recursive: true);
            }
        }
    }

    private sealed record ScriptRun(
        bool TimedOut,
        int ExitCode,
        string Stdout,
        string Stderr,
        bool SummaryExists,
        string SummaryText)
    {
        public string Describe() =>
            $"timedOut={TimedOut} exitCode={ExitCode}{Environment.NewLine}stdout:{Environment.NewLine}{Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{Stderr}";
    }
}
