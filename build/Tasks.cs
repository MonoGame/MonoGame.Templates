
namespace BuildScripts;

[TaskName("Build Templates")]
[IsDependentOn(typeof(BuildDotNetTemplatesTask))]
public sealed class BuildTemplatesTask : FrostingTask<BuildContext> { }

// MonoGame.Library.Shared.CSharp (mgshared) is obsolete and has no test task.
[TaskName("TestNuGet")]
[IsDependentOn(typeof(TestNuGetSetupTask))]
[IsDependentOn(typeof(TestDesktopGLTask))]
[IsDependentOn(typeof(TestWindowsDXTask))]
[IsDependentOn(typeof(TestAndroidTask))]
[IsDependentOn(typeof(TestiOSTask))]
[IsDependentOn(typeof(TestBlank2DStarterKitTask))]
[IsDependentOn(typeof(TestFull2DStarterKitTask))]
[IsDependentOn(typeof(TestDesktopVKTask))]
[IsDependentOn(typeof(TestWindowsDX12Task))]
[IsDependentOn(typeof(TestBlankMGCBStarterKitTask))]
[IsDependentOn(typeof(TestFull2DMGCBStarterKitTask))]
[IsDependentOn(typeof(TestContentBuilderTask))]
[IsDependentOn(typeof(TestLibraryTask))]
[IsDependentOn(typeof(TestPipelineExtensionTask))]
[IsDependentOn(typeof(TestSpriteFontItemTask))]
[IsDependentOn(typeof(TestPipelineExtensionItemTask))]
[IsDependentOn(typeof(TestNuGetSummaryTask))]
public sealed class TestNuGetTask : FrostingTask<BuildContext> { }

[TaskName("Default")]
[IsDependentOn(typeof(BuildTemplatesTask))]
public sealed class DefaultTask : FrostingTask<BuildContext> { }
