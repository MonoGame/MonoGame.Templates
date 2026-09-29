using System.IO;
using IOPath = System.IO.Path;

namespace BuildScripts;

[TaskName("TestSpriteFontItem")]
public sealed class TestSpriteFontItemTask : TestMonoGameTemplateTaskBase
{
    private static readonly PlatformFamily[] _supportedPlatforms = { PlatformFamily.Windows, PlatformFamily.Linux, PlatformFamily.OSX };

    // Registers the new font in the host's Content.mgcb, so its .xnb is part of the content check.
    private static readonly string[] _mgcbEntry =
    {
        "",
        "#begin TestFont.spritefont",
        "/importer:FontDescriptionImporter",
        "/processor:FontDescriptionProcessor",
        "/processorParam:PremultiplyAlpha=True",
        "/processorParam:TextureFormat=Compressed",
        "/build:TestFont.spritefont"
    };

    protected override string TemplateName => "SpriteFont Item";
    protected override string ProjectFolderName => "spritefontitem";
    protected override string TemplateShortName => "mgsf";
    protected override string HostTemplateShortName => "mgdesktopgl";
    protected override PlatformFamily[] SupportedPlatforms => _supportedPlatforms;

    protected override bool PrepareTestProject(BuildContext context, TestPass pass, string projectDir)
    {
        var contentDir = IOPath.Combine(projectDir, "Content");
        if (!CreateTestProject(context, pass, "create item", TemplateShortName, "TestFont", contentDir, "--IncludeFont", "true"))
            return false;

        var mgcbPath = IOPath.Combine(contentDir, "Content.mgcb");
        if (!File.Exists(mgcbPath))
        {
            RecordFailure(context, pass, "register item", $"{mgcbPath} does not exist in the {HostTemplateShortName} project.");
            return false;
        }

        File.AppendAllLines(mgcbPath, _mgcbEntry);
        return true;
    }
}
