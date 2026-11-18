using System.Xml.Linq;

namespace MiniWms.Application.Tests.Structure;

/// <summary>Finds files of the repository from the test output folder. The repo root is the folder that holds global.json.</summary>
internal static class RepoLocator
{
    public static readonly string[] SourceProjects =
    [
        "MiniWms.Domain",
        "MiniWms.Application",
        "MiniWms.Infrastructure",
        "MiniWms.Api",
    ];

    public static readonly string[] TestProjects =
    [
        "MiniWms.Domain.Tests",
        "MiniWms.Application.Tests",
        "MiniWms.Api.Tests",
    ];

    public static string RepoRoot { get; } = FindRepoRoot();

    public static string BackendRoot => Path.Combine(RepoRoot, "src", "backend");

    public static string ProjectFile(string project) =>
        Path.Combine(BackendRoot, project, project + ".csproj");

    /// <summary>Names of the projects referenced by a project through ProjectReference items.</summary>
    public static string[] ProjectReferences(string project) =>
        XDocument.Load(ProjectFile(project))
            .Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension((string?)e.Attribute("Include") ?? string.Empty))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "global.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("global.json was not found above " + AppContext.BaseDirectory);
    }
}
