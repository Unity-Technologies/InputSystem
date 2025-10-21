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

        if (platform.System == SystemType.Windows)
        {
            job.WithCommands(c => c.Add(InputSystemSettings.NetfxInstallCmd));
        }

        job.WithCommands(c => c
                .Add(InputSystemSettings.DoctoolsInstallCmd)
                .Add(Utilities.GetEditorDownloadCommand(unityBranch, platform))
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
}
