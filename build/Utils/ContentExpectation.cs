using IOPath = System.IO.Path;

namespace BuildScripts;

public enum ContentKind
{
    /// <summary>The Platform has no content project, so there is nothing to check.</summary>
    None,

    /// <summary>Content is built by MonoGame.Content.Builder.Task from one or more .mgcb files.</summary>
    Mgcb,

    /// <summary>Content is built by a Content Builder project through BuildContent.targets.</summary>
    ContentBuilder
}

/// <summary>
/// What content a Platform project should end up with.
/// </summary>
public sealed class ContentExpectation
{
    public ContentKind Kind { get; private init; }

    /// <summary>For MGCB Platforms, the output files listed by the .mgcb files, relative to the Content folder.</summary>
    public HashSet<string> Files { get; private init; } = NewFileSet();

    /// <summary>For Content Builder Platforms, true when the Assets folder holds source files, so .xnb output is required.</summary>
    public bool ExpectsContent { get; private init; }

    public string Source { get; private init; } = "";

    public static HashSet<string> NewFileSet() => new(StringComparer.OrdinalIgnoreCase);

    public static ContentExpectation For(ProjectInfo Platform)
    {
        var mgcbFiles = FindMgcbFiles(Platform);
        if (mgcbFiles.Count > 0)
        {
            var files = NewFileSet();
            foreach (var mgcb in mgcbFiles)
                files.UnionWith(ParseMgcb(mgcb));

            return new ContentExpectation
            {
                Kind = ContentKind.Mgcb,
                Files = files,
                Source = string.Join(", ", mgcbFiles.Select(IOPath.GetFileName))
            };
        }

        var buildContentTargets = Platform.Imports.FirstOrDefault(i =>
            IOPath.GetFileName(i).Equals("BuildContent.targets", StringComparison.OrdinalIgnoreCase));
        if (buildContentTargets is not null)
        {
            var assets = IOPath.Combine(IOPath.GetDirectoryName(buildContentTargets)!, "Assets");
            var hasSources = Directory.Exists(assets) && Directory
                .EnumerateFiles(assets, "*", SearchOption.AllDirectories)
                .Any(f => !IOPath.GetFileName(f).Equals("readme.txt", StringComparison.OrdinalIgnoreCase));

            return new ContentExpectation
            {
                Kind = ContentKind.ContentBuilder,
                ExpectsContent = hasSources,
                Source = IOPath.GetRelativePath(Platform.Directory, assets)
            };
        }

        return new ContentExpectation { Kind = ContentKind.None };
    }

    /// <summary>
    /// Explicit MonoGameContentReference items, plus any .mgcb in the project folder.
    /// </summary>
    private static List<string> FindMgcbFiles(ProjectInfo Platform)
    {
        var files = Platform.ContentReferences.Where(File.Exists).ToList();

        if (Platform.References("MonoGame.Content.Builder.Task"))
        {
            files.AddRange(Directory
                .EnumerateFiles(Platform.Directory, "*.mgcb", SearchOption.AllDirectories)
                .Where(f => !ProjectInfo.IsBuildOutput(Platform.Directory, f)));
        }

        return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Each "/build:source[;destination]" produces an .xnb and each "/copy:source[;destination]" copies the file as is.
    /// </summary>
    public static IEnumerable<string> ParseMgcb(string mgcbPath)
    {
        foreach (var rawLine in File.ReadLines(mgcbPath))
        {
            var line = rawLine.Trim();
            string? value = null;
            var isBuild = false;

            if (line.StartsWith("/build:", StringComparison.OrdinalIgnoreCase))
            {
                value = line["/build:".Length..];
                isBuild = true;
            }
            else if (line.StartsWith("/copy:", StringComparison.OrdinalIgnoreCase))
            {
                value = line["/copy:".Length..];
            }

            if (string.IsNullOrWhiteSpace(value))
                continue;

            var parts = value.Split(';');
            var output = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : parts[0];
            output = output.Trim().Replace('\\', '/');

            yield return isBuild ? IOPath.ChangeExtension(output, ".xnb").Replace('\\', '/') : output;
        }
    }
}
