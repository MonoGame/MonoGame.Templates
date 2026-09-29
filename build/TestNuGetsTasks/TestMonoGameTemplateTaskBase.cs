using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.IO;
using IOPath = System.IO.Path;

namespace BuildScripts;

[IsDependentOn(typeof(TestNuGetSetupTask))]
public abstract class TestMonoGameTemplateTaskBase : FrostingTask<BuildContext>
{
    protected const string ProjectName = "TestProject";

    // Pre-compiled regex pattern that is narrower than MonoGame's pattern: MonoGame.Library.* and MonoGame.Tool.* packages have their own versions.
    private static readonly Regex PackageReferenceRegex = new(
        @"(?<partA><PackageReference\s+?Include=""(?<packageName>MonoGame\.Framework\.[^""]+?|MonoGame\.Content\.Builder\.Task)""\s+?Version="")(?<version>[^""]+?)(?<partB>"".*?>)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // Regex to match any path ending with MonoGame.Framework.dll
    private static readonly Regex MonoGameFrameworkPathRegex = new(
        @"([^\s]+MonoGame\.Framework\.dll)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly List<TestResult> TestResults = new();

    protected abstract string TemplateName { get; }
    protected abstract string ProjectFolderName { get; }
    protected abstract string TemplateShortName { get; }
    protected abstract PlatformFamily[] SupportedPlatforms { get; }

    /// <summary>
    /// The projects to build, publish and check, relative to the generated template folder.
    /// </summary>
    protected virtual TestPlatform[] Platforms=> new[] { new TestPlatform($"{ProjectName}.csproj", SupportedPlatforms) };

    /// <summary>
    /// The template "dotnet new" creates. Item template tests override this with their host template.
    /// </summary>
    protected virtual string HostTemplateShortName => TemplateShortName;

    /// <summary>
    /// One project to test, with the platforms that can build it.
    /// </summary>
    /// <param name="Project">Path to the .csproj, relative to the generated template folder.</param>
    /// <param name="SupportedPlatforms">Platforms that can build this project.</param>
    /// <param name="Workload">The dotnet workload the project needs, if any.</param>
    /// <param name="Packaging">Where the platform puts content: next to the executable, in an .apk/.aab, or in an .app bundle.</param>
    /// <param name="Publish">False for libraries and tools, which are only built.</param>
    protected sealed record TestPlatform(
        string Project,
        PlatformFamily[] SupportedPlatforms,
        string? Workload = null,
        PackagingKind Packaging = PackagingKind.Desktop,
        bool Publish = true);

    /// <summary>
    /// Path to a platform in a multi-project template, for example TestProject.DesktopGL/TestProject.DesktopGL.csproj.
    /// </summary>
    protected static string KitProject(string platform) => $"{ProjectName}.{platform}/{ProjectName}.{platform}.csproj";

    private class TestResult
    {
        public required string TemplateName { get; set; }
        public TestStatus Status { get; set; }
        public required string Message { get; set; }
        public PlatformFamily Platform { get; set; }
        public string Pass { get; set; } = "-";
        public string Project { get; set; } = "-";
        public string Step { get; set; } = "-";
    }

    private enum TestStatus
    {
        Success,
        Skipped,
        Failed
    }

    private sealed record OutputPaths(string TargetDir, string PublishDir);

    public static bool HasFailures => TestResults.Any(r => r.Status == TestStatus.Failed);

    public override bool ShouldRun(BuildContext context)
    {
        var currentPlatform = context.Environment.Platform.Family;
        var isSupported = SupportedPlatforms.Contains(currentPlatform);

        if (!isSupported)
        {
            context.Information($"⏭️ Skipping {TemplateName} test - not supported on {currentPlatform}");
            context.Information($"   Supported platforms: {string.Join(", ", SupportedPlatforms)}");

            // Record the skip result
            TestResults.Add(new TestResult
            {
                TemplateName = TemplateName,
                Status = TestStatus.Skipped,
                Message = $"Not supported on {currentPlatform}",
                Platform = currentPlatform
            });
        }

        return isSupported;
    }

    public override void Run(BuildContext context)
    {
        foreach (var pass in context.TestPasses)
        {
            context.Information("");
            context.Information($"=== {TemplateName} ({pass}) ===");

            var projectDir = CleanupPreviousTestRun(context, pass);

            if (!CreateTestProject(context, pass, "create", HostTemplateShortName, ProjectName, projectDir))
                continue;

            if (!PrepareTestProject(context, pass, projectDir))
                continue;

            UpdateProjectReferences(context, projectDir);
            UpdateDotnetToolsConfig(context, projectDir);

            if (context.RestoreSupportProjects)
                RestoreSupportProjects(context, pass, projectDir);

            foreach (var Platform in Platforms)
                TestPlatformProject(context, pass, projectDir, Platform);

            if (pass == TestPass.Artifacts)
                CheckNoStrayOutput(context, pass, projectDir);
        }
    }

    /// <summary>
    /// Runs after the template is created and before anything is built. Item template tests add their item here.
    /// </summary>
    protected virtual bool PrepareTestProject(BuildContext context, TestPass pass, string projectDir) => true;

    private string CleanupPreviousTestRun(BuildContext context, TestPass pass)
    {
        var projectDir = context.GetTemplateTestPath(pass, ProjectFolderName);
        context.Information($"🧹 Cleaning up previous {TemplateName} test run...");

        if (context.DirectoryExists(projectDir))
        {
            context.Information($"Removing existing test project: {projectDir}");
            context.DeleteDirectory(projectDir);
        }

        context.CreateDirectory(projectDir);

        // Written before "dotnet new" so the restore post action already uses the artifacts layout.
        if (pass == TestPass.Artifacts)
        {
            File.WriteAllText(IOPath.Combine(projectDir, "Directory.Build.props"),
                """
                <Project>
                  <PropertyGroup>
                    <UseArtifactsOutput>true</UseArtifactsOutput>
                  </PropertyGroup>
                </Project>
                """);
        }

        return projectDir;
    }

    protected bool CreateTestProject(BuildContext context, TestPass pass, string step, string shortName, string name, string outputDir, params string[] extraArgs)
    {
        context.Information($"Creating new MonoGame {shortName} project in: {outputDir}");
        context.CreateDirectory(outputDir);

        var args = new List<string>
        {
            "new", shortName,
            "-n", name,
            "-o", outputDir,
            "--debug:custom-hive", context.TemplateHiveDirectory,
            "--no-update-check"
        };
        args.AddRange(extraArgs);

        var result = RunStep(context, pass, step, args, outputDir);
        RecordResult(context, pass, "-", step, result.Succeeded ? TestStatus.Success : TestStatus.Failed,
            result.Succeeded ? $"dotnet new {shortName}" : Failure(result));
        return result.Succeeded;
    }

    /// <summary>
    /// Records a failure from a derived task, for example when an item cannot be registered.
    /// </summary>
    protected void RecordFailure(BuildContext context, TestPass pass, string step, string message) =>
        RecordResult(context, pass, "-", step, TestStatus.Failed, message);

    /// <summary>
    /// Restores the projects that are not platforms (Core, Content), which a single-Platform build does not restore on its own.
    /// </summary>
    private void RestoreSupportProjects(BuildContext context, TestPass pass, string projectDir)
    {
        var platforms = Platforms.Select(h => IOPath.GetFullPath(IOPath.Combine(projectDir, h.Project))).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var supportProjects = Directory.EnumerateFiles(projectDir, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !platforms.Contains(IOPath.GetFullPath(p)) && !ProjectInfo.IsBuildOutput(projectDir, p));

        foreach (var project in supportProjects)
            RestoreProject(context, pass, ProjectInfo.Load(project));
    }

    private void TestPlatformProject(BuildContext context, TestPass pass, string projectDir, TestPlatform Platform)
    {
        var currentPlatform = context.Environment.Platform.Family;
        var projectPath = IOPath.Combine(projectDir, Platform.Project);
        var projectName = IOPath.GetFileNameWithoutExtension(Platform.Project);

        if (!Platform.SupportedPlatforms.Contains(currentPlatform))
        {
            RecordResult(context, pass, projectName, "platform", TestStatus.Skipped,
                $"Not supported on {currentPlatform}. Supported platforms: {string.Join(", ", Platform.SupportedPlatforms)}");
            return;
        }

        if (!File.Exists(projectPath))
        {
            RecordResult(context, pass, projectName, "project", TestStatus.Failed,
                $"{Platform.Project} was not created. Update Platforms in {GetType().Name} if the template changed.");
            return;
        }

        if (Platform.Workload is { } workload && !context.IsWorkloadInstalled(workload))
        {
            RecordResult(context, pass, projectName, "workload", context.Strict ? TestStatus.Failed : TestStatus.Skipped,
                $"The '{workload}' workload is not installed. Run: dotnet workload install {workload}");
            return;
        }

        var project = ProjectInfo.Load(projectPath);
        var expectation = ContentExpectation.For(project);
        HashSet<string>? builtContent = null;

        foreach (var configuration in context.TestConfigurations)
        {
            if (!BuildProject(context, pass, project, configuration))
                continue;

            if (QueryOutputPaths(context, pass, project, configuration) is not { } paths)
                continue;

            if (pass == TestPass.Artifacts)
                CheckArtifactsPaths(context, pass, projectDir, project, configuration, paths, checkPublish: false);

            builtContent = CheckContent(context, pass, project, $"content {configuration}", expectation, Platform.Packaging, paths.TargetDir, reference: null)
                ?? builtContent;
        }

        if (!Platform.Publish || !context.RunPublish)
            return;

        if (Platform.Packaging == PackagingKind.iOS)
        {
            RecordResult(context, pass, project.Name, "publish Release", TestStatus.Skipped, "An iOS publish needs signing. The .app bundle from the build was checked instead.");
            return;
        }

        const string publishConfiguration = "Release";
        if (!PublishProject(context, pass, project, publishConfiguration))
            return;

        if (QueryOutputPaths(context, pass, project, publishConfiguration) is not { } publishPaths)
            return;

        if (pass == TestPass.Artifacts)
            CheckArtifactsPaths(context, pass, projectDir, project, publishConfiguration, publishPaths, checkPublish: true);

        CheckContent(context, pass, project, "content publish", expectation, Platform.Packaging, publishPaths.PublishDir, builtContent);
    }

    private void RestoreProject(BuildContext context, TestPass pass, ProjectInfo project)
    {
        context.Information($"Restoring project dependencies for {project.Name}...");
        var result = RunStep(context, pass, $"{project.Name}-restore", new[] { "restore", project.Path, "-nologo" }, project.Directory);
        RecordResult(context, pass, project.Name, "restore support project", result.Succeeded ? TestStatus.Success : TestStatus.Failed,
            result.Succeeded ? "Restored (--restore-support-projects)." : Failure(result));
    }

    private bool BuildProject(BuildContext context, TestPass pass, ProjectInfo project, string configuration)
    {
        context.Information($"Building {project.Name} ({configuration})...");

        var step = $"build {configuration}";
        var result = RunStep(context, pass, $"{project.Name}-{step}", new[]
        {
            "build", project.Path, "-c", configuration, "--verbosity", "normal", "-nologo", "-nr:false",
            $"-bl:{GetBinlogPath(context, pass, project, step)}"
        }, project.Directory);

        if (result.Succeeded)
            InspectNuGetPackagePaths(context, result.Output);

        RecordResult(context, pass, project.Name, step, result.Succeeded ? TestStatus.Success : TestStatus.Failed,
            result.Succeeded ? "Built." : Failure(result));
        return result.Succeeded;
    }

    private bool PublishProject(BuildContext context, TestPass pass, ProjectInfo project, string configuration)
    {
        context.Information($"Publishing {project.Name} ({configuration})...");

        var step = $"publish {configuration}";
        var result = RunStep(context, pass, $"{project.Name}-{step}", new[]
        {
            "publish", project.Path, "-c", configuration, "-nologo", "-nr:false",
            $"-bl:{GetBinlogPath(context, pass, project, step)}"
        }, project.Directory);

        RecordResult(context, pass, project.Name, step, result.Succeeded ? TestStatus.Success : TestStatus.Failed,
            result.Succeeded ? "Published." : Failure(result));
        return result.Succeeded;
    }

    private void InspectNuGetPackagePaths(BuildContext context, IEnumerable<string> buildOutput)
    {
        context.Information("🔍 Inspecting NuGet package paths for MonoGame.Framework.dll from build output...");

        var monoGameFrameworkPaths = new List<string>();

        foreach (var line in buildOutput)
        {
            var matches = MonoGameFrameworkPathRegex.Matches(line);
            foreach (Match match in matches)
            {
                var path = match.Groups[1].Value.Trim('"', ' ');
                monoGameFrameworkPaths.Add($"Path: {path}");
            }
        }

        if (monoGameFrameworkPaths.Count > 0)
        {
            context.Information($"📦 Found {monoGameFrameworkPaths.Count} MonoGame.Framework.dll references in build output:");
            foreach (var path in monoGameFrameworkPaths.Distinct())
            {
                context.Information($"  🔗 {path}");
            }
        }
        else
        {
            context.Warning("⚠️ No MonoGame.Framework.dll paths found in build output");
        }
    }

    /// <summary>
    /// Asks MSBuild where the output goes rather than assuming bin/Debug, so the same code handles the artifacts layout.
    /// </summary>
    private OutputPaths? QueryOutputPaths(BuildContext context, TestPass pass, ProjectInfo project, string configuration)
    {
        var result = RunStep(context, pass, $"{project.Name}-paths {configuration}", new[]
        {
            "msbuild", project.Path, $"-p:Configuration={configuration}", "-nologo",
            "-getProperty:TargetDir", "-getProperty:PublishDir"
        }, project.Directory);

        try
        {
            if (result.Succeeded)
            {
                var json = string.Join('\n', result.Output);
                var start = json.IndexOf('{');
                var end = json.LastIndexOf('}');
                var properties = JsonNode.Parse(json[start..(end + 1)])!["Properties"]!;

                string Resolve(string name) => IOPath.GetFullPath(IOPath.Combine(project.Directory, properties[name]!.GetValue<string>()));

                return new OutputPaths(Resolve("TargetDir"), Resolve("PublishDir"));
            }
        }
        catch (Exception ex) when (ex is JsonException or ArgumentOutOfRangeException or NullReferenceException)
        {
            // Reported below.
        }

        RecordResult(context, pass, project.Name, $"paths {configuration}", TestStatus.Failed, "Could not read TargetDir and PublishDir. " + Failure(result));
        return null;
    }

    /// <summary>
    /// Compares the content found in an output folder with what the Platform should ship.
    /// Returns the built content set for Content Builder Platforms, so publish can be compared against it.
    /// </summary>
    private HashSet<string>? CheckContent(BuildContext context, TestPass pass, ProjectInfo Platform, string step, ContentExpectation expectation,
        PackagingKind packaging, string outputDir, HashSet<string>? reference)
    {
        if (expectation.Kind == ContentKind.None)
            return null;

        var actual = ContentInspector.Read(packaging, outputDir);
        HashSet<string> expected;

        if (expectation.Kind == ContentKind.Mgcb)
        {
            if (expectation.Files.Count == 0)
            {
                RecordResult(context, pass, Platform.Name, step, TestStatus.Success, $"No items in {expectation.Source}. Nothing to check.");
                return null;
            }

            expected = expectation.Files;
        }
        else
        {
            // The Content Builder always writes to <OutputPath>/Content, whatever the platform packages it as.
            expected = reference ?? ContentInspector.ReadFolder(IOPath.Combine(outputDir, "Content")).Files;

            if (expectation.ExpectsContent && !expected.Any(f => f.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase)))
            {
                RecordResult(context, pass, Platform.Name, step, TestStatus.Failed, $"{expectation.Source} has source files but no .xnb files were produced.");
                return expected;
            }

            if (expected.Count == 0)
            {
                RecordResult(context, pass, Platform.Name, step, TestStatus.Success, $"{expectation.Source} has no content rules or files. Nothing to check.");
                return expected;
            }
        }

        var location = IOPath.GetRelativePath(context.TemplateTestsDirectory, actual.Location).Replace('\\', '/');
        var missing = expected.Where(f => !actual.Files.Contains(f)).OrderBy(f => f, StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
        {
            var where = actual.Found ? location : $"{location} (not found)";
            RecordResult(context, pass, Platform.Name, step, TestStatus.Failed,
                $"{missing.Count} of {expected.Count} content files missing from {where}. First missing: {string.Join(", ", missing.Take(10))}");
        }
        else
        {
            RecordResult(context, pass, Platform.Name, step, TestStatus.Success, $"{expected.Count} content files ({actual.XnbCount} .xnb) found in {location}");
        }

        return expectation.Kind == ContentKind.ContentBuilder ? expected : null;
    }

    private void CheckArtifactsPaths(BuildContext context, TestPass pass, string projectDir, ProjectInfo project, string configuration, OutputPaths paths, bool checkPublish)
    {
        var step = checkPublish ? "artifacts publish path" : $"artifacts path {configuration}";
        var (path, expectedRoot) = checkPublish
            ? (paths.PublishDir, IOPath.Combine(projectDir, "artifacts", "publish"))
            : (paths.TargetDir, IOPath.Combine(projectDir, "artifacts", "bin"));

        var isUnderArtifacts = IOPath.GetFullPath(path).StartsWith(IOPath.GetFullPath(expectedRoot) + IOPath.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        RecordResult(context, pass, project.Name, step, isUnderArtifacts ? TestStatus.Success : TestStatus.Failed,
            isUnderArtifacts ? IOPath.GetRelativePath(projectDir, path) : $"{path} is not under {expectedRoot}");
    }

    /// <summary>With UseArtifactsOutput on, nothing should write a bin or obj folder next to a project.</summary>
    private void CheckNoStrayOutput(BuildContext context, TestPass pass, string projectDir)
    {
        var artifactsDir = IOPath.Combine(projectDir, "artifacts");
        var stray = Directory.EnumerateDirectories(projectDir, "*", SearchOption.AllDirectories)
            .Where(d => IOPath.GetFileName(d) is "bin" or "obj")
            .Where(d => !d.StartsWith(artifactsDir + IOPath.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Select(d => IOPath.GetRelativePath(projectDir, d).Replace('\\', '/'))
            .ToList();

        RecordResult(context, pass, "-", "no bin/obj outside artifacts", stray.Count == 0 ? TestStatus.Success : TestStatus.Failed,
            stray.Count == 0 ? "Clean." : "Found: " + string.Join(", ", stray));
    }

    /// <summary>
    /// Optional: point the generated projects at a specific MonoGame version, for example freshly built packages.
    /// Off by default, so the test checks exactly what a user gets from "dotnet new".
    /// </summary>
    private void UpdateProjectReferences(BuildContext context, string projectDir)
    {
        if (string.IsNullOrEmpty(context.MonoGameVersion))
            return;

        context.Information($"Updating project references to version {context.MonoGameVersion} in: {projectDir}");

        var csprojFiles = Directory.GetFiles(projectDir, "*.csproj", SearchOption.AllDirectories);

        if (csprojFiles.Length == 0)
        {
            context.Warning($"No .csproj files found in directory: {projectDir}");
            return;
        }

        foreach (var csprojPath in csprojFiles)
        {
            UpdateProjectFile(context, csprojPath, context.MonoGameVersion);
        }
    }

    private void UpdateProjectFile(BuildContext context, string csprojPath, string version)
    {
        var csprojContent = File.ReadAllText(csprojPath);
        var newContent = PackageReferenceRegex.Replace(csprojContent, $"${{partA}}{version}${{partB}}");

        if (newContent != csprojContent)
        {
            File.WriteAllText(csprojPath, newContent);
            context.Information($"   ✅ Successfully updated {csprojPath} to version {version}");
        }
    }

    /// <summary>
    /// With --monogame-version set, points the MGCB tools at the same version.
    /// MonoGame's ReplaceDotnetToolsConfig rewrites the whole file per platform, which would hide problems in the shipped manifest.
    /// </summary>
    private void UpdateDotnetToolsConfig(BuildContext context, string projectDir)
    {
        if (string.IsNullOrEmpty(context.MonoGameVersion))
            return;

        foreach (var dotnetToolsPath in Directory.EnumerateFiles(projectDir, "dotnet-tools.json", SearchOption.AllDirectories))
        {
            var json = JsonNode.Parse(File.ReadAllText(dotnetToolsPath))!;
            if (json["tools"] is not JsonObject tools)
                continue;

            foreach (var (name, tool) in tools)
            {
                if (name.StartsWith("dotnet-mgcb", StringComparison.OrdinalIgnoreCase) && tool is JsonObject toolObject)
                    toolObject["version"] = context.MonoGameVersion;
            }

            File.WriteAllText(dotnetToolsPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private ProcessResult RunStep(BuildContext context, TestPass pass, string step, IEnumerable<string> args, string workingDir) =>
        context.RunDotNet(args, workingDir, context.GetTemplateLogPath(pass, ProjectFolderName, step));

    private string GetBinlogPath(BuildContext context, TestPass pass, ProjectInfo project, string step) =>
        IOPath.ChangeExtension(context.GetTemplateLogPath(pass, ProjectFolderName, $"{project.Name}-{step}"), ".binlog");

    /// <summary>The first error lines are usually more useful than the last ones, which are the MSBuild summary.</summary>
    private static string Failure(ProcessResult result)
    {
        var errors = result.Output
            .Where(l => l.Contains(": error ", StringComparison.OrdinalIgnoreCase) || l.Contains("[E]", StringComparison.Ordinal))
            .Distinct()
            .Take(3)
            .Select(l => l.Trim())
            .ToList();

        var summary = errors.Count > 0 ? string.Join(" ", errors) : result.Tail(3).ReplaceLineEndings(" ");
        return $"Exit code {result.ExitCode}. {summary} Log: {result.LogFile}";
    }

    private void RecordResult(BuildContext context, TestPass pass, string project, string step, TestStatus status, string message)
    {
        var result = new TestResult
        {
            TemplateName = TemplateName,
            Status = status,
            Message = message,
            Platform = context.Environment.Platform.Family,
            Pass = pass.ToString(),
            Project = project,
            Step = step
        };
        TestResults.Add(result);

        var line = $"{GetIcon(status)} {result.TemplateName} | {result.Pass} | {result.Project} | {result.Step}: {result.Message}";
        switch (status)
        {
            case TestStatus.Failed:
                context.Error(line);
                break;
            case TestStatus.Skipped:
                context.Warning(line);
                break;
            default:
                context.Information(line);
                break;
        }
    }

    private static string GetIcon(TestStatus status) => status switch
    {
        TestStatus.Success => "✅",
        TestStatus.Skipped => "⚠️",
        TestStatus.Failed => "❌",
        _ => "❓"
    };

    /// <summary>
    /// Display a summary report of all test results with status icons
    /// </summary>
    public static void DisplayTestSummary(BuildContext context)
    {
        if (TestResults.Count == 0)
        {
            context.Information("No test results to display.");
            return;
        }

        context.Information("");
        context.Information("🎯 MonoGame NuGet Template Test Summary");
        context.Information("==========================================");

        var successCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        foreach (var result in TestResults.OrderBy(r => r.TemplateName))
        {
            var icon = GetIcon(result.Status);

            context.Information($"{icon} {result.TemplateName} | {result.Pass} | {result.Project} | {result.Step} - {result.Message}");

            switch (result.Status)
            {
                case TestStatus.Success: successCount++; break;
                case TestStatus.Skipped: skippedCount++; break;
                case TestStatus.Failed: failedCount++; break;
            }
        }

        context.Information("");
        context.Information($"📊 Results: {successCount} successful, {skippedCount} skipped, {failedCount} failed");

        if (failedCount > 0)
        {
            context.Information($"❌ {failedCount} test(s) failed - check logs above for details");
        }
        else if (successCount > 0)
        {
            context.Information($"🎉 All {successCount} eligible test(s) completed successfully!");
        }

        WriteMarkdownSummary(context, successCount, skippedCount, failedCount);
    }

    /// <summary>
    /// Writes results.md next to the test projects and appends it to the GitHub job summary when running in Actions.
    /// </summary>
    private static void WriteMarkdownSummary(BuildContext context, int successCount, int skippedCount, int failedCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"## MonoGame NuGet Template Test Summary ({context.Environment.Platform.Family})");
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"{successCount} successful, {skippedCount} skipped, {failedCount} failed.");
        builder.AppendLine();
        builder.AppendLine("| Status | Template | Pass | Project | Step | Message |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

        // Failures first, then everything else in run order.
        foreach (var result in TestResults.OrderBy(r => r.Status == TestStatus.Failed ? 0 : 1))
        {
            var message = result.Message.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");
            builder.AppendLine(CultureInfo.InvariantCulture, $"| {GetIcon(result.Status)} | {result.TemplateName} | {result.Pass} | {result.Project} | {result.Step} | {message} |");
        }

        var markdown = builder.ToString();
        var resultsFile = IOPath.Combine(context.TemplateTestsDirectory, "results.md");
        File.WriteAllText(resultsFile, markdown);
        context.Information($"Results written to {resultsFile}");
        context.Information($"Logs and binlogs are in {context.TemplateLogsDirectory}");

        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            var stepSummary = context.EnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (!string.IsNullOrEmpty(stepSummary))
                File.AppendAllText(stepSummary, markdown + Environment.NewLine);
        }
    }

    /// <summary>
    /// Clear test results (useful for multiple test runs)
    /// </summary>
    public static void ClearTestResults()
    {
        TestResults.Clear();
    }
}
