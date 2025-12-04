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
    // Run functional tests in all Unity versions on all mobile platforms except TvOS.
    IEnumerable<Dependency> allMobileFunctionalTests = new MobileFunctionalTests().AsDependencies().Where( d => !d.JobId.Contains("TvOS"));
    // Run functional build jobs on TvOS for all Unity versions.
    IEnumerable<Dependency> allTvOSFunctionalBuildJobs = new MobileFunctionalBuildJobs().AsDependencies().Where(d=> d.JobId.Contains("TvOS"));
    
    IEnumerable<Dependency> allEditorPerformanceTests = new EditorPerformanceTests().AsDependencies();
    IEnumerable<Dependency> allStandalonePerformanceTests = new StandalonePerformanceTests().AsDependencies();
    IEnumerable<Dependency> allStandaloneIl2CppPerformanceTests = new StandaloneIl2CppPerformanceTests().AsDependencies();
    // Run performance tests in all Unity versions on all mobile platforms except TvOS.
    IEnumerable<Dependency> allMobilePerformanceTests = new MobilePerformanceTests().AsDependencies().Where( d => !d.JobId.Contains("TvOS"));
    // Run performance build jobs on TvOS for all Unity versions.
    IEnumerable<Dependency> allTvOSPerformanceBuildJobs = new MobilePerformanceBuildJobs().AsDependencies().Where(d=> d.JobId.Contains("TvOS"));
        
    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetTriggers()).SelectJobs();

    private ISet<IJobBuilder> GetTriggers()
    {
        var allPerformanceTests = JobBuilder.Create("All Performance Tests")
            .WithDependencies(allEditorPerformanceTests)
            .WithDependencies(allStandalonePerformanceTests)
            .WithDependencies(allStandaloneIl2CppPerformanceTests)
            .WithDependencies(allMobilePerformanceTests)
            .WithDependencies(allTvOSPerformanceBuildJobs);
        
        HashSet<IJobBuilder> builders =
        [
            allPerformanceTests,

            JobBuilder.Create("All Functional Tests")
                .WithDependencies(allEditorFunctionalTests)
                .WithDependencies(allStandaloneFunctionalTests)
                // Exclude standalone-il2cpp tests on Ubuntu from PR trigger as they are unstable, run them nightly instead.
                .WithDependencies(allStandaloneIl2CppFunctionalTests.Where(d => !d.JobId.Contains("Ubuntu")) )
                .WithDependencies(allMobileFunctionalTests)
                .WithDependencies(allTvOSFunctionalBuildJobs)
                .WithDependencies(new Dependency("wrench/promotion-jobs", "publish_dry_run_inputsystem"))
                .WithPullRequestTrigger(pr => pr.ExcludeDraft().And().WithTargetBranch(InputSystemSettings.BranchName).And().WithoutChanges("**/*.md"), true, CancelLeftoverJobs.Always),

            JobBuilder.Create("Nightly trigger")
                .WithDependencies(allStandaloneIl2CppFunctionalTests.Where(d => d.JobId.Contains("Ubuntu")))
                .WithDependencies(new Dependency("triggers", "all_performance_tests"))
                .WithScheduleTrigger(Schedule.RunDaily(InputSystemSettings.BranchName)),
        ];
        return builders;
    }
}