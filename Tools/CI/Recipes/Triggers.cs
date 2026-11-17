using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Api.Triggers;
using InputSystem.Cookbook.Settings;
using RecipeEngine.Api.Triggers.Recurring;

namespace InputSystem.Cookbook.Recipes;

public class Triggers : RecipeBase
{
    IEnumerable<Dependency> allEditorFunctionalTests = new EditorFunctionalTests().AsDependencies();
    IEnumerable<Dependency> allStandaloneFunctionalTests = new StandaloneFunctionalTests().AsDependencies();
    IEnumerable<Dependency> allStandaloneIl2CppFunctionalTests = new StandaloneIl2CppFunctionalTests().AsDependencies();
    // Run functional tests in all Unity versions on all mobile platforms except TvOS.
    IEnumerable<Dependency> allMobileFunctionalTests = new MobileFunctionalTests().AsDependencies().Where(d => !d.JobId.Contains("TvOS"));
    // Run functional build jobs on TvOS for all Unity versions.
    IEnumerable<Dependency> allTvOSFunctionalBuildJobs = new MobileFunctionalBuildJobs().AsDependencies().Where(d => d.JobId.Contains("TvOS"));

    IEnumerable<Dependency> allEditorPerformanceTests = new EditorPerformanceTests().AsDependencies();
    IEnumerable<Dependency> allStandalonePerformanceTests = new StandalonePerformanceTests().AsDependencies();
    IEnumerable<Dependency> allStandaloneIl2CppPerformanceTests = new StandaloneIl2CppPerformanceTests().AsDependencies();
    // Run performance tests in all Unity versions on all mobile platforms except TvOS.
    IEnumerable<Dependency> allMobilePerformanceTests = new MobilePerformanceTests().AsDependencies().Where(d => !d.JobId.Contains("TvOS"));
    // Run performance build jobs on TvOS for all Unity versions.
    IEnumerable<Dependency> allTvOSPerformanceBuildJobs = new MobilePerformanceBuildJobs().AsDependencies().Where(d => d.JobId.Contains("TvOS"));

    // Wrench's "Validate - inputsystem - <editor> - <platform>" jobs, which run the PVP checks with our
    // .yamato/wrench/pvp-exemptions.json. Wrench doesn't expose these jobs publicly, so their ids are built
    // the same way Wrench names them. If the naming changes, regenerating logs a YAM002 warning
    // ("A dependency references a job that does not exist").
    IEnumerable<Dependency> allValidationJobs = InputSystemSettings.Instance.InputSystemPackage.UnityEditors
        .SelectMany(editor => editor.EditorPlatforms.Items.Keys.Select(platformType =>
            new Dependency("wrench/validation-jobs",
                $"validate_-_inputsystem_-_{editor.Version.Version.Replace('.', '_')}_-_{platformType.ToString().ToLowerInvariant()}")))
        .ToList();

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
                    .WithDependencies(allStandaloneIl2CppFunctionalTests.Where(d => !d.JobId.Contains("Ubuntu")))
                    .WithDependencies(allMobileFunctionalTests)
                    .WithDependencies(allTvOSFunctionalBuildJobs)
                    // Gate on Validate rather than on "Publish Dry Run": a dry run has the same requirements
                    // as a real publish, so it always fails while the changelog has an [Unreleased] section.
                    .WithDependencies(allValidationJobs)
                    .WithPullRequestTrigger(pr => pr.ExcludeDraft().And().WithTargetBranch(InputSystemSettings.BranchName).And().WithoutChanges("**/*.md"), true, CancelLeftoverJobs.Always),

                JobBuilder.Create("Nightly trigger")
                    .WithDependencies(allStandaloneIl2CppFunctionalTests.Where(d => d.JobId.Contains("Ubuntu")))
                    .WithDependencies(new Dependency("triggers", "all_performance_tests"))
                    .WithScheduleTrigger(Schedule.RunDaily(InputSystemSettings.BranchName))
            ];
        return builders;
    }
}
