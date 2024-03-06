#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine.InputSystem.Editor;
#endif // UNITY_EDITOR

// Note that editor edit mode behavior is tested in a dedicated test suite in editor test assembly.
//
// Note that play-mode and player tests both use a dedicated asset setup via build hooks so that the
// editor build configuration for preloaded Project-wide Input Actions asset may be temporarily replaced.
// Note that the play mode tests in this file rely on an asset stored in a random file to avoid any
// collisions with assets that may have been created by the user. These are automatically removed on
// test termination.

[TestFixture]
[PrebuildSetup(typeof(ProjectWideActionsTests.BuildSetup))]
[PostBuildCleanup(typeof(ProjectWideActionsTests.BuildSetup))]
internal class ProjectWideActionsTests : CoreTestsFixture
{
    private const string kAssetPath = "Assets/ProjectWideInputActionAssetForPlayModeTesting.inputactions";
    private static InputActionAsset s_ActionsToIncludeInPlayerBuildBeforeSetup;

    private static void CreateAndAssignProjectWideTestAsset()
    {
#if UNITY_EDITOR
        // Create a temporary asset for testing and assign it as the asset to include in player build while
        // we at the same time preserve the current configuration so we can restore it after the test build.
        s_ActionsToIncludeInPlayerBuildBeforeSetup = ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath(kAssetPath);
        ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild = asset;
#endif
    }

    private static void CleanupProjectWideTestAsset()
    {
#if UNITY_EDITOR
        // Restore setting
        var testAsset = ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
        ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild = s_ActionsToIncludeInPlayerBuildBeforeSetup;
        s_ActionsToIncludeInPlayerBuildBeforeSetup = null;

        // Remove asset
        var path = AssetDatabase.GetAssetPath(testAsset);
        if (File.Exists(path))
            AssetDatabase.DeleteAsset(path);
#endif
    }

    private class BuildSetup : IPrebuildSetup, IPostBuildCleanup
    {
        // Runs before player build or before play-mode tests run, not to confuse with SetUp().
        // Runs before [OneTimeSetUp] and before [SetUp]
        #region IPrebuildSetup
        public void Setup() { CreateAndAssignProjectWideTestAsset(); }
        #endregion

        // Runs after player build, not to confuse with TearDown()
        // IMPORTANT: Does not run after editor play-mode tests, but do run as expected after player test builds.
        //            Unclear if this is an issue in UTF or something wrong with this test.
        //            A workaround is provided via OneTimeTearDown() below.
        #region IPostBuildCleanup
        public void Cleanup() { CleanupProjectWideTestAsset(); }
        #endregion
    }

    // Note that this is mainly a workaround for editor play-mode tests since IPostBuildCleanup doesn't
    // seem to be called. This may be removed if IPostBuildCleanup is invoked according to what is stated
    // in UTF documentation.
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        CleanupProjectWideTestAsset();
    }

    const string TestCategory = "ProjectWideActions";

    [Test(Description = "Verifies that attempting to assign InputSystem.actions while in play-mode throws an exception.")]
    [NUnit.Framework.Category(TestCategory)]
    public void ProjectWideActions_ThrowsException_WhenAssignedInPlayMode()
    {
        Assert.Throws<Exception>(() => InputSystem.actions = null);
    }

    [Test(Description = "Verifies that when entering play-mode InputSystem.actions is automatically assigned based on editor build configuration.")]
    [NUnit.Framework.Category(TestCategory)]
    public void ProjectWideActions_IsAutomaticallyAssignedFromPersistedAsset_WhenRunningInPlayer()
    {
        // Regardless if editor play-mode or standalone player build we should always have project-wide input actions
        // asset for the scenario setup, derived from editor build settings or preloaded assets.
        Assert.That(InputSystem.actions, Is.Not.Null);

        // In editor play-mode we may as well verify that the asset has the expected name
        #if UNITY_EDITOR
        var expectedName = InputActionImporter.NameFromAssetPath(AssetDatabase.GetAssetPath(InputSystem.actions));
        Assert.That(InputSystem.actions.name, Is.EqualTo(expectedName));
        #endif
    }

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_AreEnabledByDefaultInPlayModeAndAppearInEnabledActions()
    {
        // Assert that project-wide actions get enabled by default
        var actionCount = 19;
        var enabledActions = InputSystem.ListEnabledActions();
        Assert.That(enabledActions, Has.Count.EqualTo(actionCount));

        // Adding more actions also work
        var action = new InputAction(name: "standaloneAction");
        action.Enable();

        enabledActions = InputSystem.ListEnabledActions();
        Assert.That(enabledActions, Has.Count.EqualTo(actionCount + 1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action));

        // Disabling works
        InputSystem.actions?.Disable();
        enabledActions = InputSystem.ListEnabledActions();
        Assert.That(enabledActions, Has.Count.EqualTo(1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action));

        // TODO Modifying the actions object after being assigned should also enable newly added actions?
    }
}

#endif
