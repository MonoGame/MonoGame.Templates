using IOPath = System.IO.Path;

namespace BuildScripts;

[TaskName("TestPipelineExtensionItem")]
public sealed class TestPipelineExtensionItemTask : TestMonoGameTemplateTaskBase
{
    private static readonly PlatformFamily[] _supportedPlatforms = { PlatformFamily.Windows, PlatformFamily.Linux, PlatformFamily.OSX };

    protected override string TemplateName => "Pipeline Extension Item";
    protected override string ProjectFolderName => "pipelineextensionitem";
    protected override string TemplateShortName => "mgpipelineitem";
    protected override string HostTemplateShortName => "mgpipeline";
    protected override PlatformFamily[] SupportedPlatforms => _supportedPlatforms;
    protected override TestPlatform[] Platforms=> new[]
    {
        new TestPlatform($"{ProjectName}.csproj", _supportedPlatforms, Publish: false)
    };

    // A sub folder, because the host project already has an Importer1.cs and Processor1.cs.
    protected override bool PrepareTestProject(BuildContext context, TestPass pass, string projectDir) =>
        CreateTestProject(context, pass, "create item", TemplateShortName, "Custom", IOPath.Combine(projectDir, "Extensions"));
}
