namespace BuildScripts;

[TaskName("TestBlankMGCBStarterKit")]
public sealed class TestBlankMGCBStarterKitTask : TestMonoGameTemplateTaskBase
{
    private static readonly PlatformFamily[] _supportedPlatforms = { PlatformFamily.Windows, PlatformFamily.Linux, PlatformFamily.OSX };

    protected override string TemplateName => "Blank Content Builder Starter Kit";
    protected override string ProjectFolderName => "blankmgcbstartkit";
    protected override string TemplateShortName => "mgblankmgcbstartkit";
    protected override PlatformFamily[] SupportedPlatforms => _supportedPlatforms;
    protected override TestPlatform[] Platforms=> new[]
    {
        new TestPlatform(KitProject("DesktopGL"), new[] { PlatformFamily.Windows, PlatformFamily.Linux, PlatformFamily.OSX }),
        new TestPlatform(KitProject("DesktopVK"), new[] { PlatformFamily.Windows, PlatformFamily.Linux }),
        new TestPlatform(KitProject("WindowsDX12"), new[] { PlatformFamily.Windows }),
        new TestPlatform(KitProject("Android"), new[] { PlatformFamily.Windows, PlatformFamily.Linux }, "android", PackagingKind.Android),
        new TestPlatform(KitProject("iOS"), new[] { PlatformFamily.OSX }, "ios", PackagingKind.iOS)
    };
}
