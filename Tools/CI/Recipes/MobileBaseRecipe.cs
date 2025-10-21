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
                // Skip tests on 2021.3 as it is longer supported
                if (version == "2021.3")
                    continue;

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
        if (systemType == SystemType.Android)
        {
            var executableName = "utr.bat";
            job.WithCommands(Settings.AndroidExtraCommands).WithAfterCommands(Settings.AndroidExtraAfterCommands);
            var utrDownloadCommand = UtrCommand.Download(systemType, executableName);
            job.WithCommands(utrDownloadCommand);
            return executableName;
        }

        return "UnifiedTestRunner";
    }
}