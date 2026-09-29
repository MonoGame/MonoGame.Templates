
namespace BuildScripts;

[TaskName("Build dotnet templates")]
public sealed class BuildDotNetTemplatesTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                context.DotNetPack(context.GetProjectPath(ProjectType.Templates, "MonoGame.Templates.CSharp"), context.DotNetPackSettings);
                return;
            }
            catch (CakeException) when (attempt < 3)
            {
                context.Warning($"dotnet pack attempt {attempt} failed, retrying...");
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }
    }
}
