namespace BuildScripts;

[TaskName("TestPipelineExtension")]
public sealed class TestPipelineExtensionTask : TestMonoGameTemplateTaskBase
{
    private static readonly PlatformFamily[] _supportedPlatforms = { PlatformFamily.Windows, PlatformFamily.Linux, PlatformFamily.OSX };

    protected override string TemplateName => "Pipeline Extension";
    protected override string ProjectFolderName => "pipelineextension";
    protected override string TemplateShortName => "mgpipeline";
    protected override PlatformFamily[] SupportedPlatforms => _supportedPlatforms;
    protected override TestPlatform[] Platforms=> new[]
    {
        new TestPlatform($"{ProjectName}.csproj", _supportedPlatforms, Publish: false)
    };
}
