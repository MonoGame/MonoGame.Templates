using System.Xml.Linq;
using IOPath = System.IO.Path;

namespace BuildScripts;

/// <summary>
/// .csproj Reading definition.
/// </summary>
public sealed class ProjectInfo
{
    public required string Path { get; init; }
    public required string Directory { get; init; }
    public required string Name { get; init; }
    public List<string> PackageReferences { get; init; } = new();

    /// <summary>
    /// Full paths of every Import in the project.
    /// </summary>
    public List<string> Imports { get; init; } = new();

    /// <summary>
    /// Full paths of every explicit MonoGameContentReference in the project.
    /// </summary>
    public List<string> ContentReferences { get; init; } = new();

    public bool References(string package) =>
        PackageReferences.Contains(package, StringComparer.OrdinalIgnoreCase);

    public static ProjectInfo Load(string path)
    {
        var directory = IOPath.GetDirectoryName(path)!;
        var document = XDocument.Load(path);
        var elements = document.Descendants().ToList();

        string ResolvePath(string value) =>
            IOPath.GetFullPath(IOPath.Combine(directory, value.Replace('\\', IOPath.DirectorySeparatorChar)));

        return new ProjectInfo
        {
            Path = path,
            Directory = directory,
            Name = IOPath.GetFileNameWithoutExtension(path),
            PackageReferences = elements
                .Where(e => e.Name.LocalName == "PackageReference")
                .Select(e => (string?)e.Attribute("Include"))
                .OfType<string>()
                .ToList(),
            Imports = elements
                .Where(e => e.Name.LocalName == "Import")
                .Select(e => (string?)e.Attribute("Project"))
                .OfType<string>()
                .Where(p => !p.Contains("$("))
                .Select(ResolvePath)
                .ToList(),
            ContentReferences = elements
                .Where(e => e.Name.LocalName == "MonoGameContentReference")
                .Select(e => (string?)e.Attribute("Include"))
                .OfType<string>()
                .Where(p => !p.Contains('*'))
                .Select(ResolvePath)
                .ToList()
        };
    }

    /// <summary>
    /// Determines if the specified path is within a build output folder (bin, obj, or artifacts).
    /// </summary>
    /// <param name="root">The root directory to consider as the base for relative paths.</param>
    /// <param name="path">The path to check if it is within a build output folder.</param>
    /// <returns>True if the path is within a build output folder; otherwise, false.</returns>
    public static bool IsBuildOutput(string root, string path)
    {
        var segments = IOPath.GetRelativePath(root, path).Split(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar);
        return segments.Any(s => s is "bin" or "obj" or "artifacts");
    }
}
