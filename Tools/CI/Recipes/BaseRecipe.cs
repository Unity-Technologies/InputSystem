using RecipeEngine.Api.Jobs;
using InputSystem.Cookbook.Settings;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Platforms;
using RecipeEngine.Platforms;
using RecipeEngine.Unity.Abstractions.Packages;

namespace InputSystem.Cookbook.Recipes;

public abstract class BaseRecipe : RecipeBase
{
    protected BaseRecipe()
    {
        Name = $"InputSystem-{GetType().Name}";
    }

    /// <summary>
    /// Implement this to specify the project path associated with the job.
    /// </summary>
    public abstract string ProjectPath { get; }

    public InputSystemSettings Settings => InputSystemSettings.Instance;

    public IEnumerable<Dependency> AsDependencies()
        => this.Jobs.ToDependencies(this);

    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetJobs()).SelectJobs();

    public virtual IEnumerable<IJobBuilder> GetJobs()
    {
        List<IJobBuilder> builders = new();

        var package = Settings.InputSystemPackage;
        foreach (var unityEditor in package.UnityEditors)
        {
            var version = unityEditor.Version.Version;
            foreach (var (platformType, editorPlatform) in unityEditor.EditorPlatforms.Items)
            {
                var platform = new Platform(editorPlatform.Agent, editorPlatform.System);
                var jobName = GetJobName(version, platformType);
                builders.Add(ProduceJob(jobName, package, platform, version));
            }
        }

        return builders;
    }

    /// <summary>
    /// Provide a job for the package in the specified unity version, on the specified system.
    /// It has a default implementation that uses a job name based on the unity version and system type.
    /// Override this method if you need additional logic before creating the job.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <param name="platform">The platform that the job should target.</param>
    /// <param name="unityVersion">The unity version that the job should target.</param>
    /// <returns>The job builder</returns>
    public virtual IJobBuilder ProduceJob(Package package, Platform platform, string unityVersion)
    {
        var jobName = GetJobName(unityVersion, platform.System);
        return ProduceJob(jobName, package, platform, unityVersion);
    }

    /// <summary>
    /// Implement this to provide a job for the package with the specified name, the specified unity version and the specified system.
    /// </summary>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="package">The package.</param>
    /// <param name="platform">The platform that the job should target.</param>
    /// <param name="unityVersion">The unity version that the job should target.</param>
    /// <returns>The job builder</returns>
    protected abstract IJobBuilder ProduceJob(string jobName, Package package, Platform platform, string unityVersion);

    /// <summary>
    /// Override to provide the platforms for a given UnityEditor instance.
    /// Desktop recipes use this to respect per-editor platform sets (e.g. a platform
    /// removed for a specific editor version will be absent here).
    /// </summary>
    /// <param name="unityEditor">The editor whose platforms should be returned.</param>
    /// <returns>The platforms for this editor.</returns>
    public virtual IEnumerable<Platform> GetJobPlatforms(UnityEditor unityEditor) => unityEditor.EditorPlatforms;

    /// <summary>
    /// Override to provide a fixed platform list regardless of editor version.
    /// Used by mobile recipes whose platforms come from config, not from UnityEditors.
    /// </summary>
    /// <param name="package"> The package that the job should target.</param>
    /// <returns>The system types.</returns>
    public virtual IEnumerable<Platform> GetJobPlatforms(WrenchPackage package) => package.UnityEditors[0].EditorPlatforms;

    protected virtual string GetName() => Name;
    protected string GetJobName(string editorVersion, EditorPlatformType platformType)
        => $"{GetName()} - {editorVersion} - {platformType}";
    protected string GetJobName(string editorVersion, SystemType systemType)
        => $"{GetName()} - {editorVersion} - {systemType}";
}
