namespace BuildScripts;

public enum ProjectType
{
    Templates
}

public enum TestPass
{
    /// <summary>
    /// Generates projects as-is, using bin/ and obj/ next to each project.
    /// </summary>
    Default,

    /// <summary>
    /// The same projects with a Directory.Build.props that turns on `UseArtifactsOutput`.
    /// </summary>
    Artifacts
}

public class BuildContext : FrostingContext
{
    public static string VersionBase = "3.8.5.1";

    public BuildContext(ICakeContext context) : base(context)
    {
        var buildConfiguration = context.Argument("build-configuration", "Release");
        BuildOutput = context.Argument("build-output", "Artifacts");
        NuGetsDirectory = $"{BuildOutput}/NuGet/";

        // The template version is stamped into every dotnet-tools.json, so it must match a published MGCB tool.
        Version = context.Argument("build-version", context.EnvironmentVariable("TEMPLATE_VERSION") ?? VersionBase);

        DotNetMSBuildSettings = new DotNetMSBuildSettings();
        DotNetMSBuildSettings.WithProperty("Version", Version);

        DotNetPackSettings = new DotNetPackSettings
        {
            MSBuildSettings = DotNetMSBuildSettings,
            Verbosity = DotNetVerbosity.Minimal,
            OutputDirectory = NuGetsDirectory,
            Configuration = buildConfiguration,
            WorkingDirectory = this.ShellWorkingDir
        };

        // Template test settings.
        TemplateTestsDirectory = System.IO.Path.GetFullPath(GetOutputPath("TemplateTests"));
        TestConfigurations = SplitList(context.Argument("configurations", "Debug,Release"));
        TestPasses = SplitList(context.Argument("passes", "Default,Artifacts"))
            .Select(p => Enum.Parse<TestPass>(p, ignoreCase: true))
            .ToList();
        RunPublish = context.Argument("publish", true);
        Strict = context.Argument("strict", false);
        RestoreSupportProjects = context.Argument("restore-support-projects", false);
        PackageSource = context.Argument<string?>("package-source", null);
        MonoGameVersion = context.Argument<string?>("monogame-version", null);

        Console.WriteLine($"Version: {Version}");
        Console.WriteLine($"BuildConfiguration: {buildConfiguration}");

        context.CreateDirectory(BuildOutput);
    }

    public string Version { get; }

    public string BuildOutput { get; }

    public string NuGetsDirectory { get; }

    public DotNetMSBuildSettings DotNetMSBuildSettings { get; }

    public DotNetPackSettings DotNetPackSettings { get; }

    public string ShellWorkingDir { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// Test projects, the private template hive and logs folder.
    /// </summary>
    public string TemplateTestsDirectory { get; }

    public IReadOnlyList<string> TestConfigurations { get; }

    public IReadOnlyList<TestPass> TestPasses { get; }

    public bool RunPublish { get; }

    /// <summary>
    /// When true, a platform that should build on this OS but is missing its workload fails instead of being skipped.
    /// </summary>
    public bool Strict { get; }

    /// <summary>
    /// When true, projects that are not (Core, Content) are restored before the platform is built.
    /// Off by default: a user building a single platform gets no such restore, so the test should not either.
    /// </summary>
    public bool RestoreSupportProjects { get; }

    /// <summary>
    /// An extra NuGet source, for example a folder of freshly built MonoGame packages.
    /// </summary>
    public string? PackageSource { get; }

    /// <summary>
    /// When set, MonoGame package and tool versions in generated projects are rewritten to this version.
    /// </summary>
    public string? MonoGameVersion { get; }

    public string TemplateHiveDirectory => System.IO.Path.Combine(TemplateTestsDirectory, "hive");

    public string TemplateLogsDirectory => System.IO.Path.Combine(TemplateTestsDirectory, "logs");

    public string GetProjectPath(ProjectType type, string id = "") => type switch
    {
        ProjectType.Templates => $"CSharp/{id}.csproj",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public string GetOutputPath(string path)
    {
        if (System.IO.Path.IsPathRooted(path) || path.StartsWith(BuildOutput))
        {
            return path;
        }

        return System.IO.Path.Combine(BuildOutput, path);
    }

    public bool IsWorkloadInstalled(string workload)
    {
        this.StartProcess(
            "dotnet",
            new ProcessSettings()
            {
                Arguments = $"workload list",
                RedirectStandardOutput = true,
                WorkingDirectory = this.ShellWorkingDir,
            },
            out IEnumerable<string> processOutput
        );

        return processOutput.Any(match => match.StartsWith($"{workload} "));
    }

    public string GetTemplateTestPath(TestPass pass, string name) =>
        System.IO.Path.Combine(TemplateTestsDirectory, pass.ToString().ToLowerInvariant(), name);

    public string GetTemplateLogPath(TestPass pass, string label, string step) =>
        System.IO.Path.Combine(TemplateLogsDirectory, pass.ToString().ToLowerInvariant(), Sanitize(label), Sanitize(step) + ".log");

    private static List<string> SplitList(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string Sanitize(string value) =>
        string.Concat(value.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_'));
}

public sealed record ProcessResult(int ExitCode, IReadOnlyList<string> Output, string LogFile)
{
    public bool Succeeded => ExitCode == 0;

    public string Tail(int lines = 25) => string.Join(Environment.NewLine, Output.TakeLast(lines));
}

public static class BuildContextExtensions
{
    public static void DeleteDirectory(this BuildContext context, DirectoryPath fullPath)
    {
        context.DeleteDirectory(fullPath, new DeleteDirectorySettings { Recursive = true, Force = true });
    }

    /// <summary>
    /// Runs a dotnet command without failing the build, and writes everything it output to a log file.
    /// </summary>
    public static ProcessResult RunDotNet(this BuildContext context, IEnumerable<string> arguments, string workingDirectory, string logFile)
    {
        context.CreateDirectory(System.IO.Path.GetDirectoryName(logFile)!);

        var argumentBuilder = new ProcessArgumentBuilder();
        foreach (var argument in arguments)
            argumentBuilder.Append(QuoteArgument(argument));

        var processSettings = new ProcessSettings
        {
            Arguments = argumentBuilder,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // Stop MSBuild nodes from outliving the command and keeping the redirected output open.
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
                ["DOTNET_NOLOGO"] = "1",
                ["DOTNET_CLI_UI_LANGUAGE"] = "en",
                ["MSBUILDDISABLENODEREUSE"] = "1",
                ["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0"
            }
        };

        var exitCode = context.StartProcess("dotnet", processSettings, out IEnumerable<string> standardOutput, out IEnumerable<string> standardError);
        var output = standardOutput.Concat(standardError).ToList();

        File.WriteAllLines(logFile, new[] { $"> dotnet {argumentBuilder.Render()}", $"> in {workingDirectory}" }.Concat(output));

        return new ProcessResult(exitCode, output, logFile);
    }

    /// <summary>
    /// Quotes one argument using the Windows command line rules, which .NET also applies on Linux and macOS.
    /// </summary>
    /// <remarks>
    /// Cake's AppendQuoted does not double trailing backslashes, so a folder path ending in "\" escapes its closing quote
    /// and swallows the arguments after it.
    /// </remarks>
    private static string QuoteArgument(string argument)
    {
        if (argument.Length > 0 && argument.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
            return argument;

        var builder = new System.Text.StringBuilder("\"");
        var backslashes = 0;

        foreach (var c in argument)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }

            // Backslashes are only special when they come before a quote.
            builder.Append('\\', c == '"' ? backslashes * 2 + 1 : backslashes);
            builder.Append(c);
            backslashes = 0;
        }

        builder.Append('\\', backslashes * 2);
        builder.Append('"');
        return builder.ToString();
    }
}
