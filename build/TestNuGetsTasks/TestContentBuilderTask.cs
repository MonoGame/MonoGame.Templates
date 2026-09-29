namespace BuildScripts;

[TaskName("TestContentBuilder")]
public sealed class TestContentBuilderTask : TestMonoGameTemplateTaskBase
{
    private static readonly PlatformFamily[] _supportedPlatforms = { PlatformFamily.Windows, PlatformFamily.Linux, PlatformFamily.OSX };

    protected override string TemplateName => "Content Builder";
    protected override string ProjectFolderName => "contentbuilder";
    protected override string TemplateShortName => "mgcb";
    protected override PlatformFamily[] SupportedPlatforms => _supportedPlatforms;
    protected override TestPlatform[] Platforms=> new[]
    {
        new TestPlatform($"{ProjectName}.csproj", _supportedPlatforms, Publish: false)
    };
}
