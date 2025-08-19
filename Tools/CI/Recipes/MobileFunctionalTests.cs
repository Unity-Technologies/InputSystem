using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Modules.UnifiedTestRunner;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Platforms;
using RecipeEngine.Unity.Abstractions.Packages;
using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Modules.InfrastructureInstabilityDetection;
using RecipeEngine.Modules.Wrench.Helpers;

namespace InputSystem.Cookbook.Recipes;

public class MobileFunctionalBuildJobs: InputBaseRecipe
{
    public override string ProjectPath => ".";

    public override IEnumerable<Platform> GetJobPlatforms(WrenchPackage package) => Settings.MobileBuildPlatforms.Values;
    protected override IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityVersion)
    {
        var unityBranch = Settings.Wrench.EditorVersionToBranches[unityVersion];
        IJobBuilder job = JobBuilder.Create(jobName)
            .WithDescription(jobName)
            .WithPlatform(platform)
            .WithCommands(c => c
                .Add(Utilities.GetEditorDownloadCommand(unityBranch, platform))
                .Add(UtrCommand.Run(platform.System, b => b
                    .WithTestProject($"{ProjectPath}")
                    .WithEditor(".Editor")
                    .WithSuite(UtrTestSuiteType.Playmode)
                    .WithPlatform(platform.System)
                    .WithCategory("!Performance")
                    .WithExtraArgs("--clean-library")
                    .WithRerun(1, true)
                    .WithBuildOnly()
                    .WithPlayerSavePath("build/players")
                    .WithArtifacts("build/logs"))))
            .WithArtifact(new Artifact("players", "build/players/**/*"), 
                new Artifact("logs", "build/logs/**/*"))
            .WithInfrastructureInstabilityDetection<WrenchExtensions.CustomScriptInfo>();
        return job;
    }
}

public class MobileFunctionalTests: InputBaseRecipe
{
    public override string ProjectPath => ".";
    
    public override IEnumerable<Platform> GetJobPlatforms(WrenchPackage package) => Settings.MobileTestPlatforms.Values;
    protected override IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityVersion)
    {
        var buildJob = new MobileFunctionalBuildJobs().AsDependencies().Where(d =>
            d.JobId.Contains(platform.System.ToString()) && d.JobId.Contains(unityVersion));

        IJobBuilder job = JobBuilder.Create(jobName)
            .WithDescription(jobName)
            .WithPlatform(platform);
        
        if (platform.System == SystemType.Android)
        {
            job.WithCommands(c => c
                    //Set the IP of the device. In case device gets lost, UTR will try to recconect to ANDROID_DEVICE_CONNECTION
                    .Add("set ANDROID_DEVICE_CONNECTION=%BOKKEN_DEVICE_IP%")
                    //Establish an ADB connection with the device
                    .Add("start %ANDROID_SDK_ROOT%\\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%")
                    //List the connected devices
                    .Add("start %ANDROID_SDK_ROOT%\\platform-tools\\adb.exe devices"))
                .WithAfterCommands(c=> c
                    .Add("start %ANDROID_SDK_ROOT%\\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%")
                    .Add("if not exist build\\test-results mkdir build\\test-results")
                    .Add("powershell %ANDROID_SDK_ROOT%\\platform-tools\\adb.exe logcat -d > build/test-results/device_log.txt"));
        }

        job.WithCommands(c => c
                .Add(UtrCommand.Run(platform.System, b => b
                    .WithSuite(UtrTestSuiteType.Playmode)
                    .WithPlatform(platform.System)
                    .WithCategory("!Performance")
                    .WithRerun(1)
                    .WithPlayerLoadPath("build/players")
                    .WithArtifacts("build/test-results"))))
            .WithDependencies(buildJob)
            .WithArtifact(new Artifact("logs", "build/test-results/**/*"))
            .WithInfrastructureInstabilityDetection<WrenchExtensions.CustomScriptInfo>();
        return job;
    }
}