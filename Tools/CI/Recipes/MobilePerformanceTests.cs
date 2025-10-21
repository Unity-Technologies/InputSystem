using InputSystem.Cookbook.Recipes.Extensions;
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

public class MobilePerformanceBuildJobs: MobileBaseRecipe
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

        var utrCommand = UtrCommand.Run(platform.System, b => b
                .WithTestProject($"{ProjectPath}")
                .WithEditor(".Editor")
                .WithSuite(UtrTestSuiteType.Playmode)
                .WithCategory("Performance")
                .WithExtraArgs("--clean-library")
                .WithRerun(1, true)
                .WithBuildOnly()
                .WithPerformanceDataReporting(true)
                .WithPerformanceProject("InputSystem")
                .WithPlayerSavePath("build/players")
                .WithTimeout(3600)
                .WithArtifacts("build/logs"))
            .WithPlatform(platform);

        if (platform.System == SystemType.Android && jobName.Contains("il2cpp"))
            utrCommand = utrCommand.Concat("--scripting-backend=il2cpp");

        job.WithCommands(utrCommand)
            .WithArtifact(new Artifact("players", "build/players/**/*"),
                new Artifact("logs", "build/logs/**/*"))
            .WithInfrastructureInstabilityDetection<WrenchExtensions.CustomScriptInfo>();
        
        if (platform.System == SystemType.IOS && float.Parse(unityVersion) > 6000.2f)
            job.WithEnvironmentVariable("UNITY_HANDLEUIINTERRUPTIONS", 1);

        return job;
    }
}

public class MobilePerformanceTests: MobileBaseRecipe
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

        // For 6000.3+ versions, use iOS15 platform to run tests.
        if (platform.System == SystemType.IOS && float.Parse(unityVersion) > 6000.2f)
            platform = Settings.iOS15Platform;
        
        IJobBuilder job = JobBuilder.Create(jobName).WithDescription(jobName).WithPlatform(platform);
        var utrExecutable = PrepareUtrExecutable(job, platform.System);

        var utrCommand = UtrCommand.Run(platform.System, utrExecutable, b => b
                .WithSuite(UtrTestSuiteType.Playmode)
                .WithCategory("Performance")
                .WithRerun(1)
                .WithPerformanceDataReporting(true)
                .WithPerformanceProject("InputSystem")
                .WithPlayerLoadPath("build/players")
                .WithTimeout(3600)
                .WithArtifacts("build/test-results"))
            .WithPlatform(platform);

        job.WithCommands(utrCommand)
            .WithDependencies(buildJob)
            .WithArtifact(new Artifact("logs", "build/test-results/**/*"))
            .WithInfrastructureInstabilityDetection<WrenchExtensions.CustomScriptInfo>();

        return job;
    }
}