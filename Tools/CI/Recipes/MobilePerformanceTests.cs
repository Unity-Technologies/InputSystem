using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Dependencies;
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

public class MobilePerformanceBuildJobs: InputMobileBaseRecipe
{
    public override string ProjectPath => ".";
    public override IEnumerable<Platform> GetJobPlatforms(WrenchPackage package) => Settings.MobileBuildPlatforms.Values;

    protected override IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityVersion)
    {
        var unityBranch = Settings.Wrench.EditorVersionToBranches[unityVersion];
        IJobBuilder job = JobBuilder.Create(jobName)
            .WithDescription(jobName)
            .WithPlatform(platform)
            .WithCommands(Utilities.GetEditorDownloadCommand(unityBranch, platform));

        // Build job on Android with il2cpp scripting backend.
        if (platform.System == SystemType.Android && jobName.Contains("il2cpp"))
        {
            job.WithCommands(UtrCommand.Run(platform.System, b => b
                .WithTestProject($"{ProjectPath}")
                .WithEditor(".Editor")
                .WithSuite(UtrTestSuiteType.Playmode)
                .WithPlatform(platform.System)
                .WithCategory("Performance")
                .WithScriptingBackend(ScriptingBackendType.Il2Cpp)
                .WithExtraArgs("--clean-library")
                .WithRerun(1, true)
                .WithBuildOnly()
                .WithPerformanceDataReporting(true)
                .WithPerformanceProject("InputSystem")
                .WithPlayerSavePath("build/players")
                .WithArtifacts("build/logs")));
        }
        else
        {
            job.WithCommands(UtrCommand.Run(platform.System, b => b
                .WithTestProject($"{ProjectPath}")
                .WithEditor(".Editor")
                .WithSuite(UtrTestSuiteType.Playmode)
                .WithPlatform(platform.System)
                .WithCategory("Performance")
                .WithExtraArgs("--clean-library")
                .WithRerun(1, true)
                .WithBuildOnly()
                .WithPerformanceDataReporting(true)
                .WithPerformanceProject("InputSystem")
                .WithPlayerSavePath("build/players")
                .WithArtifacts("build/logs")));
        }

        job.WithArtifact(new Artifact("players", "build/players/**/*"),
                new Artifact("logs", "build/logs/**/*"))
            .WithInfrastructureInstabilityDetection<WrenchExtensions.CustomScriptInfo>();
        return job;
    }
}

public class MobilePerformanceTests: InputMobileBaseRecipe
{
    public override string ProjectPath => ".";
    public override IEnumerable<Platform> GetJobPlatforms(WrenchPackage package) => Settings.MobileTestPlatforms.Values;
    
    protected override IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityVersion)
    {
        IEnumerable<Dependency> buildJob = new MobilePerformanceBuildJobs().AsDependencies().Where(d =>
            d.JobId.Contains(platform.System.ToString()) && d.JobId.Contains(unityVersion));
        
        if (platform.System == SystemType.Android)
        {
            if (jobName.Contains("il2cpp"))
            {
                buildJob = new MobilePerformanceBuildJobs().AsDependencies().Where(d =>
                    d.JobId.Contains(platform.System.ToString()) && d.JobId.Contains(unityVersion) &&
                    d.JobId.Contains("il2cpp"));
            }
            else
            {
                buildJob = new MobilePerformanceBuildJobs().AsDependencies().Where(d =>
                    d.JobId.Contains(platform.System.ToString()) && d.JobId.Contains(unityVersion) &&
                    d.JobId.Contains("mono"));
            }
        }

        IJobBuilder job = JobBuilder.Create(jobName).WithDescription(jobName).WithPlatform(platform);

        if (platform.System == SystemType.Android)
            job.WithCommands(Settings.AndroidExtraCommands).WithAfterCommands(Settings.AndroidExtraAfterCommands);

        job.WithCommands(c => c
                .Add(UtrCommand.Run(platform.System, b => b
                    .WithSuite(UtrTestSuiteType.Playmode)
                    .WithPlatform(platform.System)
                    .WithCategory("Performance")
                    .WithRerun(1)
                    .WithPerformanceDataReporting(true)
                    .WithPerformanceProject("InputSystem")
                    .WithPlayerLoadPath("build/players")
                    .WithArtifacts("build/test-results"))))
            .WithDependencies(buildJob)
            .WithArtifact(new Artifact("logs", "build/test-results/**/*"))
            .WithInfrastructureInstabilityDetection<WrenchExtensions.CustomScriptInfo>();
        return job;
    }
}