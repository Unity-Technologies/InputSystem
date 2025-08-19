using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Api.Triggers;
using RecipeEngine.Modules.Wrench.Models;
using InputSystem.Cookbook.Settings;
using RecipeEngine.Api.Triggers.Recurring;

namespace InputSystem.Cookbook.Recipes;

public class Triggers: RecipeBase
{
    InputSystemSettings settings = InputSystemSettings.Instance;
    IEnumerable<Dependency> allEditorFunctionalTests = new EditorFunctionalTests().AsDependencies();
    IEnumerable<Dependency> allStandaloneFunctionalTests = new StandaloneFunctionalTests().AsDependencies();
    IEnumerable<Dependency> allStandaloneIl2CppFunctionalTests = new StandaloneIl2CppFunctionalTests().AsDependencies();
    // Run functional tests in all Unity versions on all mobile platforms except for TvOS, which is only run in 2021.3.
    IEnumerable<Dependency> allMobileFunctionalTests = new MobileFunctionalTests().AsDependencies().Where( d => !d.JobId.Contains("TvOS") || d.JobId.Contains("2021.3"));
    // Run build jobs on TvOS for all Unity versions.
    IEnumerable<Dependency> allTvOSBuildJobs = new MobileFunctionalBuildJobs().AsDependencies().Where(d=> d.JobId.Contains("TvOS"));
        
    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetTriggers()).SelectJobs();

    private ISet<IJobBuilder> GetTriggers()
    {
        HashSet<IJobBuilder> builders =
        [
            JobBuilder.Create("All Functional Tests")
                .WithDependencies(allEditorFunctionalTests)
                .WithDependencies(allStandaloneFunctionalTests)
                .WithDependencies(allStandaloneIl2CppFunctionalTests)
                .WithDependencies(allMobileFunctionalTests)
                .WithDependencies(allTvOSBuildJobs)
                .WithPullRequestTrigger(pr => pr.ExcludeDraft().And().WithTargetBranch(InputSystemSettings.BranchName).And().WithoutChanges("**/*.md"), true, CancelLeftoverJobs.Always)
            
        ];
        return builders;
    }
}