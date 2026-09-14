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

public class StandalonePerformanceTests: BaseRecipe
{
    public override string ProjectPath => ".";
    protected override IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityVersion)
    {
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
                .Add(UtrCommand.Run(platform.System, b => b
                    .WithTestProject($"{ProjectPath}")
                    .WithEditor(".Editor")
                    .WithSuite(UtrTestSuiteType.Playmode)
                    .WithPlatform(platform.System)
                    .WithCategory("Performance")
                    .WithExtraArgs("--clean-library", "--api-profile=NET_4_6")
                    .WithRerun(1, true)
                    .WithPerformanceDataReporting(true)
                    .WithPerformanceProject("InputSystem")
                    .WithTimeout(3600)
                    .WithArtifacts("artifacts"))))
            .WithArtifact(new Artifact("artifacts", "artifacts/**/*"))
            .WithInfrastructureInstabilityDetection<WrenchExtensions.CustomScriptInfo>();

        return job;
    }
}