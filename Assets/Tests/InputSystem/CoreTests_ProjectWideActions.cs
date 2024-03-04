#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif // UNITY_EDITOR

internal partial class CoreTests
{
#if UNITY_EDITOR
    // Allows including a default project-wide asset into player tests which overrides user configured asset
    private class ProjectWideInputActionsForTest : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private InputActionAsset m_StoredActions;
        private InputActionAsset m_AssetForTesting;
        private Object m_Asset;

        public int callbackOrder => 1000; // Larger than ProjectWideActionsBuilderProvider to override

        private const string kAssetPath = "Assets/ProjectWideInputActionAssetForTesting.inputactions";

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Assert(!File.Exists(kAssetPath));

            // Store whatever setting exist before build
            m_StoredActions = InputSystem.actions;

            // Create an asset for testing
            var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath(kAssetPath);

            // Remove any "real" preloaded assets
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            for (var i = preloadedAssets.Length - 1; i >= 0; --i)
            {
                var preloadedAsset = preloadedAssets[i] as InputActionAsset;
                if (preloadedAsset != null)
                {
                    ArrayHelpers.EraseAt(ref preloadedAssets, i);
                }
            }

            PlayerSettings.SetPreloadedAssets(preloadedAssets);

            // Mark test asset to be included in build
            asset.m_IsProjectWide = true;

            // Add asset
            m_Asset = BuildProviderHelpers.PreProcessSinglePreloadedAsset(asset);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            BuildProviderHelpers.PostProcessSinglePreloadedAsset(ref m_Asset);

            InputSystem.actions = m_StoredActions;

            // Remove the test-only asset after build
            AssetDatabase.DeleteAsset(kAssetPath);
        }
    }
#endif // UNITY_EDITOR

    // Note that only a selected few tests verifies the behavior associated with the editor support for
    // creating a dedicated asset. For all other logical tests we are better off constructing an asset on
    // the fly for functional tests to avoid differences between editor and playmode tests.
    //
    // Note that player tests are currently lacking since it would require a proper asset to be configured
    // during edit mode and then built and then loaded indirectly via config object / resources.
    //
    // Note that any existing default created asset is preserved during test run by moving it via ADB.

    const string TestCategory = "ProjectWideActions";
    const string m_AssetBackupDirectory = "Assets/~TestBackupFiles";
    const string s_DefaultProjectWideAssetBackupPath = "Assets/~TestBackupFilesDefaultProjectWideAssetBackup.json";

    private InputActionAsset actions;
    private InputActionAsset otherActions;
    private int callbackCount;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
#if UNITY_EDITOR
        // Avoid overwriting any default asset already in /Assets folder by making a backup file not visible to AssetDatabase.
        // This is for verifying the default output of templated actions from editor tools.
        if (File.Exists(ProjectWideActionsAsset.defaultAssetPath))
        {
            if (!Directory.Exists(m_AssetBackupDirectory))
                Directory.CreateDirectory(m_AssetBackupDirectory);
            AssetDatabase.MoveAsset(oldPath: ProjectWideActionsAsset.defaultAssetPath,
                newPath: s_DefaultProjectWideAssetBackupPath);
        }
#endif // UNITY_EDITOR
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() // TODO Remove
    {
#if UNITY_EDITOR
        // Restore default asset if we made a backup copy of it during setup
        if (File.Exists(s_DefaultProjectWideAssetBackupPath))
        {
            if (File.Exists(ProjectWideActionsAsset.defaultAssetPath))
                AssetDatabase.DeleteAsset(ProjectWideActionsAsset.defaultAssetPath);
            AssetDatabase.MoveAsset(oldPath: s_DefaultProjectWideAssetBackupPath,
                newPath: ProjectWideActionsAsset.defaultAssetPath);
            Directory.Delete("Assets/~TestBackupFiles");
            File.Delete("Assets/~TestBackupFiles.meta");
        }
#endif // UNITY_EDITOR
    }

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        callbackCount = 0;
    }

    [TearDown]
    public override void TearDown()
    {
#if UNITY_EDITOR
        // Delete any default asset we may have created (backup is safe until test class is destroyed)
        AssetDatabase.DeleteAsset(ProjectWideActionsAsset.defaultAssetPath);
#endif // UNITY_EDITOR

        // Clean-up objects created during test
        if (actions != null)
            Object.Destroy(actions);
        if (otherActions != null)
            Object.Destroy(otherActions);

        base.TearDown();
    }

// These are play-mode tests valid for editor play-mode or player play-mode
    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_ThrowsException_WhenAssignedInPlayMode()
    {
        Assert.Throws<Exception>(() => InputSystem.actions = null);
    }

// These are player tests of project-wide actions
#if !UNITY_EDITOR
    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_IsAutomaticallyAssignedFromPersistedAsset_WhenRunningInPlayer()
    {
        Assert.That(InputSystem.actions, Is.Not.Null); // Verify that we have asset for testing
        Assert.That(InputSystem.actions.name, Is.EqualTo("ProjectWideInputActionAssetForTesting"));
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
        InputSystem.actions?.Disable();
        enabledActions = InputSystem.ListEnabledActions();
        Assert.That(enabledActions, Has.Count.EqualTo(1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action));

        // TODO Modifying the actions object after being assigned should also enable newly added actions?
    }

#endif // !UNITY_EDITOR
}

#endif
