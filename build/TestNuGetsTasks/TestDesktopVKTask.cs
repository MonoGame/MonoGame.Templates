namespace BuildScripts;

[TaskName("TestDesktopVK")]
public sealed class TestDesktopVKTask : TestMonoGameTemplateTaskBase
{
    private static readonly PlatformFamily[] _supportedPlatforms = { PlatformFamily.Windows, PlatformFamily.Linux };

    protected override string TemplateName => "DesktopVK";
    protected override string ProjectFolderName => "desktopvk";
    protected override string TemplateShortName => "mgdesktopvk";
    protected override PlatformFamily[] SupportedPlatforms => _supportedPlatforms;
}
