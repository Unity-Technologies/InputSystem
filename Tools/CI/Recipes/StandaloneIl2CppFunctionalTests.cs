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

public class StandaloneIl2CppFunctionalTests: BaseRecipe
{
    public override string ProjectPath => ".";
    protected override IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityVersion)
    {
        var unityBranch = Settings.Wrench.EditorVersionToBranches[unityVersion];

        IJobBuilder job = JobBuilder.Create(jobName)
            .WithDescription(jobName)
            .WithPlatform(platform);

        if (platform.System == SystemType.Windows)
        {
            job.WithCommands(c => c.Add(InputSystemSettings.NetfxInstallCmd));
        }

        job.WithCommands(c => c
                .Add(InputSystemSettings.DoctoolsInstallCmd)
                .Add(Utilities.GetEditorDownloadCommand(unityBranch, platform, "Il2Cpp"))
                .Add(UtrCommand.Run(platform.System, b => b
                    .WithTestProject($"{ProjectPath}")
                    .WithEditor(".Editor")
                    .WithSuite(UtrTestSuiteType.Playmode)
                    .WithPlatform(platform.System)
                    .WithScriptingBackend(ScriptingBackendType.Il2Cpp)
                    .WithCategory("!Performance")
                    .WithExtraArgs("--clean-library", "--api-profile=NET_4_6")
                    .WithRerun(1, true)
                    .WithTimeout(3600)
                    .WithArtifacts("artifacts"))))
            .WithArtifact(new Artifact("artifacts", "artifacts/**/*"))
            .WithInfrastructureInstabilityDetection<WrenchExtensions.CustomScriptInfo>();

        return job;
    }
}