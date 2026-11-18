namespace MiniWms.Api.Tests.Host;

/// <summary>Finds files of the repository from the test output folder. The repo root is the folder that holds global.json.</summary>
internal static class RepoLocator
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string ApiProjectFolder => Path.Combine(RepoRoot, "src", "backend", "MiniWms.Api");

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
