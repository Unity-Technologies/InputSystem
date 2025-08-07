using InputSystem.Cookbook.Settings;
using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Modules.InfrastructureInstabilityDetection;
using RecipeEngine.Modules.UnifiedTestRunner;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Platforms;
using RecipeEngine.Unity.Abstractions.Packages;

namespace InputSystem.Cookbook.Recipes;

public class FullPackageTests: InputBaseRecipe
{
    public override string ProjectPath => ".";
    protected override IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityBranch)
    {
        IJobBuilder job = JobBuilder.Create(jobName)
            .WithDescription(jobName)
            .WithPlatform(platform);

        if (platform.System == SystemType.Windows)
        {
            job.WithCommands(c => c.Add(InputSystemSettings.NetfxInstallCmd));
        }

        job.WithCommands(c => c
                .Add(InputSystemSettings.DoctoolsInstallCmd)
                .Add(Utilities.GetEditorDownloadCommand(unityBranch, platform))
                //.Add($"upm-pvp create-test-project {ProjectPath} --packages \"upm-ci~/packages/*.tgz\" --unity .Editor")
                .Add(UtrCommand.Run(platform.System, b => b
                    .WithTestProject($"{ProjectPath}")
                    .WithEditor(".Editor")
                    .WithExtraArgs("--suite=Editor --suite=Playmode")
                    .WithCategory("!Performance")
                    .WithRerun(1, true)
                    .WithExtraArgs("--clean-library", "--api-profile=NET_4_6")
                    .WithArtifacts("artifacts"))))
            .WithArtifact(new Artifact("artifacts", "artifacts/**/*"))
            .WithInfrastructureInstabilityDetection();

        return job;
    }
}
