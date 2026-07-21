using System;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine;
using System.Collections;

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

internal class ProjectWideActionsBuildSetup : IPrebuildSetup, IPostBuildCleanup
{
    private const string kAssetPath = "Assets/ProjectWideInputActionAssetForPlayModeTesting.inputactions";
    private const string kCounterAssetPath = "Assets/ProjectWideInputActionAssetForPlayModeTesting.counter";
    #if UNITY_EDITOR
    private const string kSavedActionsObject = ProjectWideActionsBuildProvider.EditorBuildSettingsActionsConfigKey + ".testbackup";
    #endif

    private static void CreateAndAssignProjectWideTestAsset()
    {
#if UNITY_EDITOR
        // Preserve a backup of actual user configuration
        var userAsset = ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
        if (userAsset != null)
            EditorBuildSettings.AddConfigObject(name: kSavedActionsObject, obj: userAsset, overwrite: true);
        else
            EditorBuildSettings.RemoveConfigObject(name: kSavedActionsObject);

        // Create temporary asset and assign as setting
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath(kAssetPath);
        ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild = asset;
#endif
    }

    private static void CleanupProjectWideTestAsset()
    {
#if UNITY_EDITOR
        var testAsset = ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;

        // Restore users initial config and remove from settings
        if (EditorBuildSettings.TryGetConfigObject(name: kSavedActionsObject, out InputActionAsset userAsset))
            EditorBuildSettings.RemoveConfigObject(name: kSavedActionsObject);
        ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild = userAsset;

        // Remove temporary asset
        var path = AssetDatabase.GetAssetPath(testAsset);
        if (File.Exists(path))
            AssetDatabase.DeleteAsset(path);
#endif
    }

    // Runs before player build or before play-mode tests run, not to confuse with SetUp().
    // Runs before [OneTimeSetUp] and before [SetUp]
    #region IPrebuildSetup
    public void Setup() { CreateAndAssignProjectWideTestAsset(); }
    #endregion

    // Runs after player build, not to confuse with TearDown()
    // IMPORTANT: Does not run after editor play-mode tests if running with a filter (bug?),
    //            but do run as expected after filtered player test builds.
    //            Unclear if this is an issue in UTF or something wrong with this test.
    //            A workaround is provided via OneTimeTearDown() below.
    //            Seems to work fine when running all tests.
    #region IPostBuildCleanup
    public void Cleanup() { CleanupProjectWideTestAsset(); }
    #endregion
}

[TestFixture]
[PrebuildSetup(typeof(ProjectWideActionsBuildSetup))]
[PostBuildCleanup(typeof(ProjectWideActionsBuildSetup))]
internal class ProjectWideActionsTests : CoreTestsFixture
{
    const string TestCategory = "ProjectWideActions";

    [Test(Description = "Verifies that attempting to assign InputSystem.actions while in play-mode throws an exception.")]
    [Category(TestCategory)]
    public void ProjectWideActions_ThrowsException_WhenAssignedInPlayMode()
    {
        Assert.Throws<Exception>(() => InputSystem.actions = null);
    }

    [Test(Description = "Verifies that when entering play-mode InputSystem.actions is automatically assigned based on editor build configuration.")]
    [Category(TestCategory)]
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
    public void ProjectWideActions_AreEnabled_WhenEnteringPlayMode()
    {
        Assert.That(InputSystem.actions, Is.Not.Null);
        Assert.That(InputSystem.actions.enabled, Is.True);
    }

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_AppearInEnabledActions()
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
        InputSystem.actions.Disable();
        enabledActions = InputSystem.ListEnabledActions();
        Assert.That(enabledActions, Has.Count.EqualTo(1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action));

        // TODO Modifying the actions object after being assigned should also enable newly added actions?
    }

    [Category(TestCategory)]
    [Test]
    [TestCase("Player", true, Description = "PlayerInput using project-wide actions can have default action map set " +
            "enabled, and all others disabled.")]
    [TestCase(null, false, Description = "PlayerInput using project wide actions has all action maps of project-wide " +
            "actions disabled, if there is no default action map assigned.")]
    public void ProjectWideActions_CanEnableCurrentActionMapOfPlayerInput(string actionMapName, bool expectedResult)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var go = new GameObject("PlayerInput");

        go.SetActive(false);
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.actions = InputSystem.actions;
        // Default action map to be enabled. All others are expected to be disabled since InputSystem.actions.Disable()
        // will be called.
        playerInput.defaultActionMap = actionMapName;

        // PWA actions assigned to playerInput start enabled
        Assert.That(playerInput.actions.enabled, Is.True);
        Assert.That(ReferenceEquals(playerInput.actions, InputSystem.actions));

#if UNITY_EDITOR
        InputSystemEditorInitializer.OnPlayModeChange(PlayModeStateChange.ExitingEditMode);
#endif

        // This makes sure to call PlayerInput.OnEnable()
        go.SetActive(true);

        // This mimics usings to disabling actions on Start/Awake.
        InputSystem.actions.Disable();

        if (!string.IsNullOrEmpty(actionMapName))
            playerInput.SwitchCurrentActionMap("Player");

        Assert.That(playerInput.actions.enabled, Is.EqualTo(expectedResult));

        // We do this on the editor to make sure project-wide actions maintain the enabled state
        // after entering PlayMode.
#if UNITY_EDITOR
        InputSystemEditorInitializer.OnPlayModeChange(PlayModeStateChange.EnteredPlayMode);
        Assert.That(playerInput.actions.enabled, Is.EqualTo(expectedResult));
#endif

        if (!string.IsNullOrEmpty(actionMapName))
            Assert.That(playerInput.currentActionMap.enabled, Is.EqualTo(expectedResult));

        // Check state of all action maps in the asset
        Assert.That(InputSystem.actions.FindActionMap("Player").enabled, Is.EqualTo(expectedResult));
        Assert.That(InputSystem.actions.FindActionMap("UI").enabled, Is.False);

        //NOTE: Asset actions are considered enabled even if a single action map is enabled
        Assert.That(playerInput.actions.enabled, Is.EqualTo(expectedResult));
    }
}
