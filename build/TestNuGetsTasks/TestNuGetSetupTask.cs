using System.IO;
using IOPath = System.IO.Path;

namespace BuildScripts;

[TaskName("TestNuGetSetup")]
public sealed class TestNuGetSetupTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        TestMonoGameTemplateTaskBase.ClearTestResults();
        context.Information("Initialized MonoGame NuGet template testing...");

        var nugetSourcePath = IOPath.GetFullPath(context.NuGetsDirectory).TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar);
        var testsPath = context.TemplateTestsDirectory;

        CleanupPreviousTestRun(context, testsPath);

        SetupNuGetSource(context, testsPath);

        var templateVersion = GetTemplateVersionFromNuGet(context, nugetSourcePath);
        context.Information($"Detected MonoGame template version: {templateVersion}");

        InstallTemplates(context, templateVersion, nugetSourcePath);
    }

    private void CleanupPreviousTestRun(BuildContext context, string testsPath)
    {
        context.Information($"Cleaning up previous test run: {testsPath}");

        context.RunDotNet(new[] { "build-server", "shutdown" }, context.ShellWorkingDir, IOPath.GetFullPath(context.GetOutputPath("build-server-shutdown.log")));

        if (context.DirectoryExists(testsPath))
        {
            context.DeleteDirectory(testsPath);
        }

        context.CreateDirectory(testsPath);
    }

    /// <summary>
    /// MSBuild and NuGet search parent folders for their settings. These files stop that search at the test root,
    /// so a Directory.Build.props or nuget.config in a parent checkout cannot change the result.
    /// </summary>
    /// <remarks>
    /// MonoGame's version runs "dotnet nuget add source", which changes the user's NuGet config.
    /// A nuget.config in the test folder gives the same result without touching anything outside it.
    /// </remarks>
    private void SetupNuGetSource(BuildContext context, string testsPath)
    {
        const string emptyProject = "<Project>\n  <!-- Stops MSBuild from importing files from parent folders. -->\n</Project>\n";

        File.WriteAllText(IOPath.Combine(testsPath, "Directory.Build.props"), emptyProject);
        File.WriteAllText(IOPath.Combine(testsPath, "Directory.Build.targets"), emptyProject);
        File.WriteAllText(IOPath.Combine(testsPath, "Directory.Packages.props"), emptyProject);

        // Analyzer rules from a parent .editorconfig (MonoGame's makes CA1050 an error) would otherwise apply to the generated projects.
        File.WriteAllText(IOPath.Combine(testsPath, ".editorconfig"), "root = true\n");

        var extraSource = string.IsNullOrEmpty(context.PackageSource)
            ? ""
            : $"\n    <add key=\"MonoGameTestSource\" value=\"{System.Security.SecurityElement.Escape(IOPath.GetFullPath(context.PackageSource))}\" />";

        File.WriteAllText(IOPath.Combine(testsPath, "nuget.config"),
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />{extraSource}
              </packageSources>
            </configuration>
            """);

        if (context.FileExists("global.json"))
        {
            context.CopyFile("global.json", IOPath.Combine(testsPath, "global.json"));
        }

        context.Information($"Successfully set up NuGet sources for: {testsPath}");
    }

    private string GetTemplateVersionFromNuGet(BuildContext context, string nugetPath)
    {
        var normalizedPath = nugetPath.TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar);
        var searchPattern = System.IO.Path.Combine(normalizedPath, "MonoGame.Templates.CSharp.*.nupkg");
        var templateFiles = context.GetFiles(searchPattern);

        if (!templateFiles.Any())
        {
            throw new FileNotFoundException($"No MonoGame.Templates.CSharp NuGet package found in {nugetPath}. Run the Default target first.");
        }

        var latestTemplateFile = templateFiles.OrderByDescending(f => f.GetFilename().ToString()).First();
        var fileName = latestTemplateFile.GetFilenameWithoutExtension().ToString();

        const string prefix = "MonoGame.Templates.CSharp.";
        return fileName.Substring(prefix.Length);
    }

    /// <summary>
    /// Installs into a private template hive, so there is nothing to uninstall first and the user's own templates are untouched.
    /// </summary>
    private void InstallTemplates(BuildContext context, string templateVersion, string nugetSourcePath)
    {
        context.Information($"Installing MonoGame templates version {templateVersion}...");

        var result = context.RunDotNet(new[]
        {
            "new", "install", $"MonoGame.Templates.CSharp::{templateVersion}",
            "--nuget-source", nugetSourcePath,
            "--debug:custom-hive", context.TemplateHiveDirectory
        }, context.TemplateTestsDirectory, IOPath.Combine(context.TemplateLogsDirectory, "install.log"));

        if (!result.Succeeded)
        {
            context.Error(result.Tail());
            throw new Exception($"Failed to install MonoGame.Templates.CSharp {templateVersion}. Log: {result.LogFile}");
        }

        // The hive was just emptied, so "already installed" means the install went to the user's own template list instead.
        if (result.Output.Any(line => line.Contains("already installed", StringComparison.OrdinalIgnoreCase)))
        {
            throw new Exception($"The templates were not installed into the private hive {context.TemplateHiveDirectory}. Check the user's template list with 'dotnet new uninstall'. Log: {result.LogFile}");
        }
    }
}
