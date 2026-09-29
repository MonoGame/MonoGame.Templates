using System.IO.Compression;
using IOPath = System.IO.Path;

namespace BuildScripts;

public enum PackagingKind
{
    /// <summary>
    /// Content is copied next to the executable in a Content folder.
    /// </summary>
    Desktop,

    /// <summary>
    /// Content is packed into the .apk or .aab under assets/Content.
    /// </summary>
    Android,

    /// <summary>
    /// Content is copied into the .app bundle under Content.
    /// </summary>
    iOS
}

/// <summary>
/// The content files found in a build or publish output, relative to the Content root.
/// </summary>
public sealed record ContentListing(bool Found, HashSet<string> Files, string Location)
{
    public int XnbCount => Files.Count(f => f.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Reads content back out of a build or publish folder, following how each platform packages it.
/// </summary>
public static class ContentInspector
{
    private static readonly string[] AndroidContentPrefixes = { "assets/Content/", "base/assets/Content/" };

    public static ContentListing Read(PackagingKind packaging, string outputDirectory) => packaging switch
    {
        PackagingKind.Android => ReadAndroidPackage(outputDirectory),
        PackagingKind.iOS => ReadAppBundle(outputDirectory),
        _ => ReadFolder(IOPath.Combine(outputDirectory, "Content"))
    };

    public static ContentListing ReadFolder(string contentDirectory)
    {
        var files = ContentExpectation.NewFileSet();
        if (!Directory.Exists(contentDirectory))
            return new ContentListing(false, files, contentDirectory);

        foreach (var file in Directory.EnumerateFiles(contentDirectory, "*", SearchOption.AllDirectories))
            files.Add(IOPath.GetRelativePath(contentDirectory, file).Replace('\\', '/'));

        return new ContentListing(true, files, contentDirectory);
    }

    private static ContentListing ReadAndroidPackage(string outputDirectory)
    {
        var files = ContentExpectation.NewFileSet();
        if (!Directory.Exists(outputDirectory))
            return new ContentListing(false, files, outputDirectory);

        var package = Directory.EnumerateFiles(outputDirectory, "*-Signed.apk").FirstOrDefault()
            ?? Directory.EnumerateFiles(outputDirectory, "*.apk").FirstOrDefault()
            ?? Directory.EnumerateFiles(outputDirectory, "*.aab").FirstOrDefault();

        if (package is null)
            return new ContentListing(false, files, IOPath.Combine(outputDirectory, "*.apk|*.aab"));

        using var archive = ZipFile.OpenRead(package);
        foreach (var entry in archive.Entries)
        {
            var prefix = AndroidContentPrefixes.FirstOrDefault(p => entry.FullName.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            if (prefix is not null && entry.FullName.Length > prefix.Length && !entry.FullName.EndsWith('/'))
                files.Add(entry.FullName[prefix.Length..]);
        }

        return new ContentListing(true, files, package);
    }

    private static ContentListing ReadAppBundle(string outputDirectory)
    {
        var bundle = Directory.Exists(outputDirectory)
            ? Directory.EnumerateDirectories(outputDirectory, "*.app", SearchOption.AllDirectories).FirstOrDefault()
            : null;

        if (bundle is null)
            return new ContentListing(false, ContentExpectation.NewFileSet(), IOPath.Combine(outputDirectory, "*.app"));

        return ReadFolder(IOPath.Combine(bundle, "Content"));
    }
}
