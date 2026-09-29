namespace BuildScripts;

[TaskName("TestWindowsDX12")]
public sealed class TestWindowsDX12Task : TestMonoGameTemplateTaskBase
{
    private static readonly PlatformFamily[] _supportedPlatforms = { PlatformFamily.Windows };

    protected override string TemplateName => "WindowsDX12";
    protected override string ProjectFolderName => "windowsdx12";
    protected override string TemplateShortName => "mgwindowsdx12";
    protected override PlatformFamily[] SupportedPlatforms => _supportedPlatforms;
}
