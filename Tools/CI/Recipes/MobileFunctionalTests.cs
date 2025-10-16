using InputSystem.Cookbook.Recipes.Extensions;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Modules.UnifiedTestRunner;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Platforms;
using RecipeEngine.Unity.Abstractions.Packages;
using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Modules.InfrastructureInstabilityDetection;
using RecipeEngine.Modules.Wrench.Helpers;

namespace InputSystem.Cookbook.Recipes;

public class MobileFunctionalBuildJobs: MobileBaseRecipe
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
                .WithCategory("!Performance")
                .WithExtraArgs("--clean-library")
                .WithRerun(1, true)
                .WithBuildOnly()
                .WithPlayerSavePath("build/players")
                .WithTimeout(3600)
                .WithArtifacts("build/logs"))
            .WithPlatform(platform);

        if (platform.System == SystemType.Android && jobName.Contains("il2cpp"))
            utrCommand = utrCommand.Concat("--scripting-backend=il2cpp");

        job.WithCommands(utrCommand)
            .WithArtifact(new Artifact("players", "build/players/**/*"), new Artifact("logs", "build/logs/**/*"))
            .WithInfrastructureInstabilityDetection<WrenchExtensions.CustomScriptInfo>();

        // Bokken iPhones that run iOS 15 and above should have UNITY_HANDLEUIINTERRUPTIONS env var set to 1
        // 6000.3+ versions support iOS 15, so apply this only for those versions and above.
        if (platform.System == SystemType.IOS && float.Parse(unityVersion) > 6000.2f)
            job.WithEnvironmentVariable("UNITY_HANDLEUIINTERRUPTIONS", 1);

        return job;
    }
}

public class MobileFunctionalTests: MobileBaseRecipe
{
    public override string ProjectPath => ".";
    public override IEnumerable<Platform> GetJobPlatforms(WrenchPackage package) => Settings.MobileTestPlatforms.Values;
    
    protected override IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityVersion)
    {
        IEnumerable<Dependency> buildJob = new MobileFunctionalBuildJobs().AsDependencies().Where(d =>
            d.JobId.Contains(platform.System.ToString()) && d.JobId.Contains(unityVersion));

        if (platform.System == SystemType.Android)
        {
            if (jobName.Contains("il2cpp"))
            {
                buildJob = new MobileFunctionalBuildJobs().AsDependencies().Where(d =>
                    d.JobId.Contains(platform.System.ToString()) && d.JobId.Contains(unityVersion) &&
                    d.JobId.Contains("il2cpp"));
            }
            else
            {
                buildJob = new MobileFunctionalBuildJobs().AsDependencies().Where(d =>
                    d.JobId.Contains(platform.System.ToString()) && d.JobId.Contains(unityVersion) &&
                    d.JobId.Contains("mono"));
            }
        }

        // For 6000.3+ versions, use iOS15 platform to run tests.
        if (platform.System == SystemType.IOS && float.Parse(unityVersion) > 6000.2f)
            platform = Settings.iOS15Platform;

        IJobBuilder job = JobBuilder.Create(jobName).WithDescription(jobName).WithPlatform(platform);
        
        if (platform.System == SystemType.Android)
            job.WithCommands(Settings.AndroidExtraCommands).WithAfterCommands(Settings.AndroidExtraAfterCommands);

        var utrCommand = UtrCommand.Run(platform.System, b => b
                .WithSuite(UtrTestSuiteType.Playmode)
                .WithCategory("!Performance")
                .WithRerun(1)
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