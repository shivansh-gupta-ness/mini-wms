using System.Diagnostics;
using System.Text.Json;
using MiniWms.Api.Tests.Host;

namespace MiniWms.Api.Tests.Persistence;

/// <summary>
/// TASK-004 AC1 (local tool manifest) and AC6 (git ignores the database files). Whether <c>dotnet tool restore</c>
/// really installs the tool needs the network, so that part is proven by a command, not by these tests.
/// </summary>
public class RepositoryConfigurationTests
{
    private const string GitCmdFolder = @"C:\Program Files\Git\cmd";
    private const string DatabaseFolder = "src/backend/MiniWms.Api/App_Data/";
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(60);

    // AC1
    [Fact]
    [Trait("AC", "TASK-004.1")]
    public void Tool_manifest_pins_dotnet_ef_to_a_10_x_version()
    {
        var version = ReadTools().GetProperty("dotnet-ef").GetProperty("version").GetString();

        Assert.StartsWith("10.", version, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "TASK-004.1")]
    public void Tool_manifest_exposes_the_dotnet_ef_command()
    {
        var commands = ReadTools().GetProperty("dotnet-ef").GetProperty("commands").EnumerateArray()
            .Select(command => command.GetString());

        Assert.Contains("dotnet-ef", commands);
    }

    [Fact]
    [Trait("AC", "TASK-004.1")]
    public void Tool_manifest_still_contains_reportgenerator()
    {
        var found = ReadTools().TryGetProperty("dotnet-reportgenerator-globaltool", out _);

        Assert.True(found);
    }

    // AC6
    [Theory]
    [Trait("AC", "TASK-004.6")]
    [InlineData("miniwms.db")]
    [InlineData("miniwms.db-wal")]
    [InlineData("miniwms.db-shm")]
    public void Git_ignores_the_database_file_and_its_wal_sidecar_files(string fileName)
    {
        var exitCode = RunGitCheckIgnore(DatabaseFolder + fileName);

        // git check-ignore -q: exit code 0 = ignored, 1 = not ignored.
        Assert.Equal(0, exitCode);
    }

    private static JsonElement ReadTools()
    {
        var path = Path.Combine(RepoLocator.RepoRoot, ".config", "dotnet-tools.json");
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("tools");
    }

    private static int RunGitCheckIgnore(string relativePath)
    {
        var gitExe = Path.Combine(GitCmdFolder, "git.exe");
        var startInfo = new ProcessStartInfo(File.Exists(gitExe) ? gitExe : "git")
        {
            WorkingDirectory = RepoLocator.RepoRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("check-ignore");
        startInfo.ArgumentList.Add("-q");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(relativePath);

        using var process = Process.Start(startInfo)!;
        _ = process.StandardOutput.ReadToEndAsync();
        _ = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(GitTimeout))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"git check-ignore did not finish within {GitTimeout}.");
        }

        return process.ExitCode;
    }
}
