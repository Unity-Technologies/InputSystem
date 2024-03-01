#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

// TODO Since we disallow assigning InputSystem.actions in play-mode the only scenario valid
//      to test for editor play-mode or player tests is with a fixed setup.
// TODO Solve issue where callbacks are not restored between tests

internal partial class CoreTests
{
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
#endif

        // Clean-up objects created during test
        if (actions != null)
            Object.Destroy(actions);
        if (otherActions != null)
            Object.Destroy(otherActions);

        base.TearDown();
    }

    private void GivenActions()
    {
        if (actions != null)
            return;

        // Create a small InputActionsAsset on the fly that we utilize for testing
        actions = ScriptableObject.CreateInstance<InputActionAsset>();
        actions.name = "TestAsset";
        var one = actions.AddActionMap("One");
        one.AddAction("A");
        one.AddAction("B");
        var two = actions.AddActionMap("Two");
        two.AddAction("C");
    }

    private void GivenOtherActions()
    {
        if (otherActions != null)
            return;

        // Create a small InputActionsAsset on the fly that we utilize for testing
        otherActions = ScriptableObject.CreateInstance<InputActionAsset>();
        otherActions.name = "OtherTestAsset";
        var three = otherActions.AddActionMap("Three");
        three.AddAction("D");
        three.AddAction("E");
    }
    
    // TODO Verify 

    [Test]
    [Ignore("Temporarily disabled until figured out how to mock this the best way")]
    [Category(TestCategory)]
    public void ProjectWideActions_AppearInEnabledActions() // TODO How is this really related to project-wide? Checking they are enabled?
    {
        GivenActions();

        // Setup project-wide actions
        InputSystem.actions = actions;

        // Assert that project-wide actions get enabled by default
        var actionCount = 3;
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
