namespace BuildScripts;

[TaskName("TestLibrary")]
public sealed class TestLibraryTask : TestMonoGameTemplateTaskBase
{
    private static readonly PlatformFamily[] _supportedPlatforms = { PlatformFamily.Windows, PlatformFamily.Linux, PlatformFamily.OSX };

    protected override string TemplateName => "Library";
    protected override string ProjectFolderName => "library";
    protected override string TemplateShortName => "mglib";
    protected override PlatformFamily[] SupportedPlatforms => _supportedPlatforms;
    protected override TestPlatform[] Platforms=> new[]
    {
        new TestPlatform($"{ProjectName}.csproj", _supportedPlatforms, Publish: false)
    };
}
