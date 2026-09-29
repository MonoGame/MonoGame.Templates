namespace BuildScripts;

[TaskName("TestNuGetSummary")]
public sealed class TestNuGetSummaryTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        TestMonoGameTemplateTaskBase.DisplayTestSummary(context);

        // The test tasks record failures and carry on, so the run is failed here once everything has been reported.
        if (TestMonoGameTemplateTaskBase.HasFailures)
        {
            throw new Exception("One or more template tests failed.");
        }
    }
}
