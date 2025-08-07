using InputSystem.Cookbook.Settings;
using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Modules.UnifiedTestRunner;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Platforms;
using RecipeEngine.Unity.Abstractions.Packages;

namespace InputSystem.Cookbook.Recipes;

public class FullPackageTests: InputBaseRecipe
{
    public override string ProjectPath => ".";
    protected override IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityVersion)
    {
        var unityBranch = Settings.Wrench.EditorVersionToBranches[unityVersion];
        var netfxInstallCmd =
            "%GSUDO% choco install netfx-4.7.1-devpack -y --ignore-detected-reboot --ignore-package-codes";
        var doctoolsInstallCmd =
            "git clone --branch \"2.3.0-preview\" git@github.cds.internal.unity3d.com:unity/com.unity.package-manager-doctools.git Packages/com.unity.package-manager-doctools";
        
        IJobBuilder job = JobBuilder.Create(jobName)
            .WithDescription(jobName)
            .WithPlatform(platform)
            .WithCommands(c => c
                .Add(netfxInstallCmd)
                .Add(doctoolsInstallCmd)
                .Add(Utilities.GetEditorDownloadCommand(unityBranch, platform))
                //.Add($"upm-pvp create-test-project {ProjectPath} --packages \"upm-ci~/packages/*.tgz\" --unity .Editor")
                .Add(UtrCommand.Run(platform.System, b => b
                    .WithTestProject($"{ProjectPath}")
                    .WithEditor(".Editor")
                    .WithExtraArgs("--suite=Editor --suite=Playmode")
                    .WithCategory("!Performance")
                    .WithRerun(1, true)
                    .WithArtifacts("artifacts"))))
            //.WithDependencies(Settings.Wrench.WrenchJobs[InputSystemSettings.InputSystemPackageName][JobTypes.Pack])
            .WithArtifact(new Artifact("artifacts", "artifacts/**/*"));
        return job;
    }
}
