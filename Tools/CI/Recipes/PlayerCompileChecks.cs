using InputSystem.Cookbook.Settings;
using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Modules.InfrastructureInstabilityDetection;
using RecipeEngine.Modules.Wrench.Helpers;
using RecipeEngine.Modules.Wrench.Platforms;
using RecipeEngine.Platforms;
using RecipeEngine.Unity.Abstractions.Packages;

namespace InputSystem.Cookbook.Recipes;

/// <summary>
/// Compiles the package's Player scripts in release and development builds, with and without
/// extra scripting defines such as UNITY_INCLUDE_TESTS, by running Assets/Editor/PlayerCompileCheck.cs.
/// Test jobs only build development Players, so they never compile the release-build combinations.
/// The check is about #if logic, not platform behavior, so it runs on one platform per editor version.
/// </summary>
public class PlayerCompileChecks : BaseRecipe
{
    public override string ProjectPath => ".";

    const EditorPlatformType k_PlatformType = EditorPlatformType.Ubuntu2204;

    public override IEnumerable<IJobBuilder> GetJobs()
    {
        var package = Settings.InputSystemPackage;
        foreach (var unityEditor in package.UnityEditors)
        {
            if (!unityEditor.EditorPlatforms.Items.TryGetValue(k_PlatformType, out var editorPlatform))
                continue;

            var version = unityEditor.Version.Version;
            var platform = new Platform(editorPlatform.Agent, editorPlatform.System);
            yield return ProduceJob(GetJobName(version, k_PlatformType), package, platform, version);
        }
    }

    protected override IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityVersion)
    {
        var unityBranch = Settings.Wrench.EditorVersionToBranches[unityVersion];

        return JobBuilder.Create(jobName)
            .WithDescription(jobName)
            .WithPlatform(platform)
            .WithCommands(c => c
                .Add(Utilities.GetEditorDownloadCommand(unityBranch, platform))
                .Add("mkdir -p artifacts")
                .Add($".Editor/Unity -projectPath {ProjectPath} -batchmode -nographics -executeMethod PlayerCompileCheck.Run -logFile artifacts/PlayerCompileCheck.log"))
            .WithArtifact(new Artifact("artifacts", "artifacts/**/*"))
            .WithInfrastructureInstabilityDetection<WrenchExtensions.CustomScriptInfo>();
    }
}
