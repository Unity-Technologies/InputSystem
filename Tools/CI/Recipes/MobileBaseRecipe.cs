using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Modules.UnifiedTestRunner;
using RecipeEngine.Platforms;
using RecipeEngine.Unity.Abstractions.Packages;

namespace InputSystem.Cookbook.Recipes;

public abstract class MobileBaseRecipe: BaseRecipe
{
    public override IEnumerable<IJobBuilder> GetJobs()
    {
        List<IJobBuilder> builders = new();

        var package = Settings.InputSystemPackage;
        var platforms = GetJobPlatforms(package);
        foreach (var platform in platforms)
        {
            var supportedVersions = package.SupportedEditorVersions;
            foreach (var version in supportedVersions)
            {
                if (platform.System == SystemType.Android)
                {
                    builders.AddRange(ProduceJobsForAndroid(package, platform, version));
                }
                else
                {
                    builders.Add(ProduceJob(package, platform, version));
                }
            }
        }

        return builders;
    }

    // Produces jobs for Android platform with different scripting backends.
    IEnumerable<IJobBuilder> ProduceJobsForAndroid(Package package, Platform platform, string unityVersion)
    {
        List<IJobBuilder> builders = new();
        string jobName = "";
        foreach (var backend in (List<string>)["mono", "il2cpp"])
        {
            jobName = GetJobName(unityVersion, platform.System) + $" - {backend}";
            builders.Add(ProduceJob(jobName, package, platform, unityVersion));
        }

        return builders;
    }

    protected string PrepareUtrExecutable(IJobBuilder job, SystemType systemType)
    {
        var executableName = "utr.bat";
        var utrDownloadCommand = UtrCommand.Download(systemType, executableName);
        switch (systemType)
        {
            case SystemType.Android:
                job.WithCommands(Settings.AndroidExtraCommands).WithAfterCommands(Settings.AndroidExtraAfterCommands);
                job.WithCommands(utrDownloadCommand);
                return executableName;
            case SystemType.IOS:
                job.WithCommands(utrDownloadCommand);
                job.WithEnvironmentVariable("UTR_VERSION", "1.42.0");
                return executableName;
            default:
                return "UnifiedTestRunner";
        }
    }
}