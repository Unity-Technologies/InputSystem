using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Modules.UnifiedTestRunner;
using RecipeEngine.Platforms;
using RecipeEngine.Unity.Abstractions.Packages;

namespace InputSystem.Cookbook.Recipes;

public abstract class MobileBaseRecipe : BaseRecipe
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
        switch (systemType)
        {
            case SystemType.Android:
                // For build jobs on Android, we still use the built-in UTR and the extra commands are not needed.
                if (job.Name.Contains("BuildJobs"))
                    break;
                job.WithCommands(Settings.AndroidExtraCommands).WithAfterCommands(Settings.AndroidExtraAfterCommands);
                job.WithCommands(UtrCommand.Download(systemType, "utr.bat"));
                // Yet another temporary fix. UTR 1.43.0 was failing on Android builds due to some internal issue so
                // we are forcing UTR version 1.42.0 for Android platform.
                job.WithEnvironmentVariable("UTR_VERSION", "1.42.0");
                return "utr.bat";
            case SystemType.IOS:
                job.WithCommands(UtrCommand.Download(systemType, "utr"));
                // A temporary fix for the issue where UTR 1.41.0 cannot handle new signing for 6000.5 on iOS platform.
                // It can be removed once https://artifactory.prd.it.unity3d.com/ui/native/unity-tools-local/utr-standalone/utr has been bumped to 1.42.0 or above.
                job.WithEnvironmentVariable("UTR_VERSION", "1.42.0");
                return "./utr";
        }

        return "UnifiedTestRunner";
    }
}
