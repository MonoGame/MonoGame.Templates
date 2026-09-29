namespace BuildScripts;

[TaskName("TestAndroid")]
public sealed class TestAndroidTask : TestMonoGameTemplateTaskBase
{
    private static readonly PlatformFamily[] _supportedPlatforms = { PlatformFamily.Windows, PlatformFamily.Linux };
    
    protected override string TemplateName => "Android";
    protected override string ProjectFolderName => "android";
    protected override string TemplateShortName => "mgandroid";
    protected override PlatformFamily[] SupportedPlatforms => _supportedPlatforms;
    protected override TestPlatform[] Platforms=> new[]
    {
        new TestPlatform($"{ProjectName}.csproj", _supportedPlatforms, "android", PackagingKind.Android)
    };
}
