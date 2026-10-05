using InputSystem.Cookbook.Settings;
using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Modules.InfrastructureInstabilityDetection;
using RecipeEngine.Modules.UnifiedTestRunner;
using RecipeEngine.Modules.Wrench.Helpers;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Platforms;
using RecipeEngine.Unity.Abstractions.Packages;

namespace InputSystem.Cookbook.Recipes;

public class EditorFunctionalTests: BaseRecipe
{
    public override string ProjectPath => ".";
    protected override IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityVersion)
    {
        var yamatoSourceDir = platform.System == SystemType.Windows ? "%YAMATO_SOURCE_DIR%" : "$YAMATO_SOURCE_DIR";
        var unityBranch = Settings.Wrench.EditorVersionToBranches[unityVersion];
        
        IJobBuilder job = JobBuilder.Create(jobName)
            .WithDescription(jobName)
            .WithPlatform(platform);

        job.WithCommands(c => c.Add(platform.System == SystemType.Windows
            ? InputSystemSettings.DocfxInstallCmdWindows
            : InputSystemSettings.DocfxInstallCmdUnix));

        job.WithCommands(c => c
                .Add(InputSystemSettings.DoctoolsInstallCmd)
                .Add(Utilities.GetEditorDownloadCommand(unityBranch, platform))
                .Add(GetSyncSolutionCommand(platform))
                .Add(UtrCommand.Run(platform.System, b => b
                    .WithTestProject($"{ProjectPath}")
                    .WithEditor(".Editor")
                    .WithExtraArgs("--suite=Editor --suite=Playmode")
                    .WithCategory("!Performance")
                    .WithExtraArgs("--clean-library", "--api-profile=NET_4_6")
                    .WithRerun(1, true)
                    .WithExtraArgs("--enable-code-coverage", 
                        "--coverage-options=\"generateAdditionalMetrics;generateHtmlReport;" + 
                        $"assemblyFilters:+Unity.InputSystem*;pathReplacePatterns:@*,,**/PackageCache/,;sourcePaths:{yamatoSourceDir}/Packages;\"",
                        $"--coverage-results-path={yamatoSourceDir}/upm-ci~/CodeCoverage",
                        $"--coverage-upload-options=\"reportsDir:upm-ci~/CodeCoverage;name:inputsystem_{platform.System.ToString()}_{unityVersion}_project;flags:inputsystem_{platform.System.ToString()}_{unityVersion}_project\"")
                    .WithTimeout(3600)
                    .WithArtifacts("artifacts"))
                ))
            .WithArtifact(new Artifact("artifacts", "artifacts/**/*"))
            .WithInfrastructureInstabilityDetection<WrenchExtensions.CustomScriptInfo>();

        return job;
    }

    // Generates .sln/.csproj files via the Rider editor package so that PMDT 3.x can find
    // them on CI where no IDE is configured and SyncAll() would otherwise be a no-op.
    private static string GetSyncSolutionCommand(Platform platform) => platform.System switch
    {
        SystemType.Windows =>
            @".Editor\Unity.exe -projectPath . -batchmode -nographics -quit -executeMethod Packages.Rider.Editor.RiderScriptEditor.SyncSolution -logFile -",
        SystemType.MacOS =>
            ".Editor/Unity.app/Contents/MacOS/Unity -projectPath . -batchmode -nographics -quit -executeMethod Packages.Rider.Editor.RiderScriptEditor.SyncSolution -logFile -",
        _ =>
            ".Editor/Unity -projectPath . -batchmode -nographics -quit -executeMethod Packages.Rider.Editor.RiderScriptEditor.SyncSolution -logFile -"
    };
}
