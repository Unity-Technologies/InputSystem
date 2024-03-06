#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

internal class ProjectWideInputActionsEditorTests : TestFixtureBase
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

    private InputActionAsset savedUserActions;
    private InputActionAsset actions;
    private bool actionsArePersisted;
    private InputActionAsset otherActions;
    private bool otherActionsArePersisted;
    private int callbackCount;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Avoid overwriting any default asset already in /Assets folder by making a backup file not visible to AssetDatabase.
        // This is for verifying the default output of templated actions from editor tools.
        if (File.Exists(ProjectWideActionsAsset.defaultAssetPath))
        {
            if (!Directory.Exists(m_AssetBackupDirectory))
                Directory.CreateDirectory(m_AssetBackupDirectory);
            AssetDatabase.MoveAsset(oldPath: ProjectWideActionsAsset.defaultAssetPath,
                newPath: s_DefaultProjectWideAssetBackupPath);
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
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
    }

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

        callbackCount = 0;

        // In case project-wide actions have been configured, save a reference to the object to be able
        // to restore after test run.
        savedUserActions = InputSystem.actions;
        InputSystem.actions = null;
    }

    [TearDown]
    public override void TearDown()
    {
        InputSystem.onActionsChange -= OnActionsChange;

        // Delete any default asset we may have created (backup is safe until test class is destroyed)
        AssetDatabase.DeleteAsset(ProjectWideActionsAsset.defaultAssetPath);

        // Clean-up objects created during test
        if (actions != null && !actionsArePersisted)
            Object.DestroyImmediate(actions);
        if (otherActions != null && !otherActionsArePersisted)
            Object.DestroyImmediate(otherActions);

        // Restore actions
        InputSystem.actions = savedUserActions;

        base.TearDown();
    }

    private void GivenActionsCallback()
    {
        InputSystem.onActionsChange += OnActionsChange;
    }

    private void GivenActions(bool persisted = false)
    {
        // Create a small InputActionsAsset on the fly that we utilize for testing
        actions = ScriptableObject.CreateInstance<InputActionAsset>();
        actions.name = "TestAsset";
        var one = actions.AddActionMap("One");
        one.AddAction("A");
        one.AddAction("B");
        var two = actions.AddActionMap("Two");
        two.AddAction("C");

        if (persisted)
        {
            var json = actions.ToJson();
            Object.DestroyImmediate(actions);
            actions = AssetDatabaseUtils.CreateAsset<InputActionAsset>(content: json);

            actionsArePersisted = true;
        }
    }

    private void GivenOtherActions(bool persisted = false)
    {
        // Create a small InputActionsAsset on the fly that we utilize for testing
        otherActions = ScriptableObject.CreateInstance<InputActionAsset>();
        otherActions.name = "OtherTestAsset";
        var three = otherActions.AddActionMap("Three");
        three.AddAction("D");
        three.AddAction("E");

        if (persisted)
        {
            var json = otherActions.ToJson();
            Object.DestroyImmediate(otherActions);
            otherActions = AssetDatabaseUtils.CreateAsset<InputActionAsset>(content: json);

            otherActionsArePersisted = true;
        }
    }

    private void OnActionsChange()
    {
        ++callbackCount;
    }

    [Test(Description = "Verifies that project-wide actions are not set by default")]
    [Category(TestCategory)]
    public void ProjectWideActions_AreNotSetByDefault()
    {
        Assert.That(InputSystem.actions, Is.Null);
    }

    [Test(Description = "Verifies that project-wide actions defaults are constructed as an asset on the default asset path")]
    [Category(TestCategory)]
    public void ProjectWideActionsAsset_DefaultAssetFileHasDefaultContent()
    {
        // Expect asset name to be set to the file name
        var expectedName = Path.GetFileNameWithoutExtension(ProjectWideActionsAsset.defaultAssetPath);
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath();
        Assert.That(asset.name, Is.EqualTo(expectedName));

        // Expect JSON name to be set to the file name
        var json = File.ReadAllText(EditorHelpers.GetPhysicalPath(ProjectWideActionsAsset.defaultAssetPath));
        var parsedAsset = InputActionAsset.FromJson(json);
        Assert.That(parsedAsset.name, Is.EqualTo(expectedName));
        Object.DestroyImmediate(parsedAsset);
    }

    // This test is only relevant for the InputForUI module which native part was introduced in 2023.2
#if UNITY_2023_2_OR_NEWER
    [Test(Description = "Verifies that modifying the default project-wide action UI map generates console warnings")]
    [Category(TestCategory)]
    public void ProjectWideActions_ShowsErrorWhenUIActionMapHasNameChanges()
    {
        // Create a default template asset that we then modify to generate various warnings
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath();

        var indexOf = asset.m_ActionMaps.IndexOf(x => x.name == "UI");
        var uiMap = asset.m_ActionMaps[indexOf];

        // Change the name of the UI action map
        uiMap.m_Name = "UI2";

        ProjectWideActionsAsset.CheckForDefaultUIActionMapChanges(asset);

        LogAssert.Expect(LogType.Warning, new Regex("The action map named 'UI' does not exist"));

        // Change the name of some UI map back to default and change the name of the actions
        uiMap.m_Name = "UI";
        var defaultActionName0 = uiMap.m_Actions[0].m_Name;
        var defaultActionName1 = uiMap.m_Actions[1].m_Name;

        uiMap.m_Actions[0].Rename("Navigation");
        uiMap.m_Actions[1].Rename("Show");

        ProjectWideActionsAsset.CheckForDefaultUIActionMapChanges(asset);

        LogAssert.Expect(LogType.Warning, new Regex($"The UI action '{defaultActionName0}' name has been modified"));
        LogAssert.Expect(LogType.Warning, new Regex($"The UI action '{defaultActionName1}' name has been modified"));
    }

#endif // UNITY_2023_2_OR_NEWER

    [Test(Description = "Verifies that when assigning InputSystem.actions a callback is fired if value is different but not when value is not different")]
    [Category(TestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenDifferent()
    {
        GivenActions(persisted: true);
        GivenOtherActions(persisted: true);
        GivenActionsCallback();

        // Can assign from null to null (no change)
        InputSystem.actions = null;
        Assert.That(callbackCount, Is.EqualTo(0));

        // Can assign asset from null to instance (change)
        InputSystem.actions = actions;
        Assert.That(callbackCount, Is.EqualTo(1));

        // Can assign from instance to same instance (no change)
        InputSystem.actions = actions;
        Assert.That(callbackCount, Is.EqualTo(1)); // no callback expected

        // Can assign another instance (change
        InputSystem.actions = otherActions;
        Assert.That(callbackCount, Is.EqualTo(2));

        // Can assign asset from instance to null (change)
        InputSystem.actions = null;
        Assert.That(callbackCount, Is.EqualTo(3));
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions in edit-mode, build settings are updated")]
    [Category(TestCategory)]
    public void ProjectWideActions_WillUpdateBuildSettingsWhenChanged()
    {
        GivenActions(persisted: true);
        GivenOtherActions(persisted: true);
        GivenActionsCallback();

        Debug.Assert(EditorUtility.IsPersistent(actions));

        // Can assign from null to null (no change)
        InputSystem.actions = null;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(null));

        // Can assign asset from null to instance (change)
        InputSystem.actions = actions;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(actions));

        // Can assign from instance to same instance (no change)
        InputSystem.actions = actions;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(actions));

        // Can assign another instance (change
        InputSystem.actions = otherActions;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(otherActions));

        // Can assign asset from instance to null (change)
        InputSystem.actions = null;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(null));
    }

    [Test(Description =
            "Verifies that when assigning InputSystem.actions in edit-mode with a temporary object not persisted on disc, an exception is thrown")]
    [Category(TestCategory)]
    public void ProjectWideActions_ThrowsArgumentException_WhenAssignedFromNonPersistedObject()
    {
        GivenActions();

        Assert.Throws<ArgumentException>(() => InputSystem.actions = actions);
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions a callback is fired when currently being assigned to a deleted asset (destroyed object) and then assigning null")]
    [Category(TestCategory)]
    public void ProjectWideActions_CanBeAssignedNullAndFiresCallback_WhenHavingDestroyedObjectAndAssignedNull()
    {
        GivenActions(persisted: true);
        //GivenOtherActions(persisted: true);
        GivenActionsCallback();

        // Assign and make sure property returns the expected assigned value
        InputSystem.actions = actions;
        Assert.That(InputSystem.actions, Is.EqualTo(actions));
        Assert.That(callbackCount, Is.EqualTo(1));

        // Delete the associated asset make sure returned value evaluates to null (But actually Missing Reference).
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(actions));
        Assert.That(actions == null, Is.True);    // sanity check that it was destroyed
        Assert.That(InputSystem.actions == null); // note: we want to avoid cast to object since it would use another Equals

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = null;
        Assert.That(InputSystem.actions == null);
        Assert.That(ReferenceEquals(InputSystem.actions, null)); // check its really null and not just Missing Reference.
        Assert.That(callbackCount, Is.EqualTo(2));
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions a callback is fired when the previously assigned asset has been destroyed object")]
    [Category(TestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenDifferent_WhenAssignedDestroyedObject()
    {
        GivenActions(persisted: true);
        //GivenOtherActions();
        GivenActionsCallback();

        // Destroy the associated asset and make sure returned value evaluates to null (But actually Missing Reference).
        //Object.DestroyImmediate(actions);
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(actions));
        Assert.That(actions == null, Is.True);       // sanity check that it was destroyed

        // Assert that we can assign a destroyed object
        InputSystem.actions = actions;
        Assert.That(InputSystem.actions == actions); // note: we want to avoid cast to object since it would use another Equals
        Assert.That(!ReferenceEquals(InputSystem.actions, null)); // expecting missing reference
        Assert.That(callbackCount, Is.EqualTo(1));

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = null;
        Assert.That(InputSystem.actions == null);
        Assert.That(ReferenceEquals(InputSystem.actions, null)); // check its really null and not just Missing Reference.
        Assert.That(callbackCount, Is.EqualTo(2));
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions a callback is fired when assigning and current object has been destroyed")]
    [Category(TestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenAssignedAndDifferent_WhenHavingDestroyedObjectAndAssignedOther()
    {
        GivenActions(persisted: true);
        GivenOtherActions(persisted: true);
        GivenActionsCallback();

        // Assign and make sure property returns the expected assigned value
        InputSystem.actions = actions;
        Assert.That(InputSystem.actions, Is.EqualTo(actions));
        Assert.That(callbackCount, Is.EqualTo(1));

        // Destroy the associated asset and make sure returned value evaluates to null (But actually Missing Reference).
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(actions));
        Assert.That(actions == null, Is.True);    // sanity check that it was destroyed
        Assert.That(InputSystem.actions == null); // note: we want to avoid cast to object since it would use another Equals

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = otherActions;
        Assert.That(InputSystem.actions, Is.EqualTo(otherActions));
        Assert.That(callbackCount, Is.EqualTo(2));
    }
}

#endif
