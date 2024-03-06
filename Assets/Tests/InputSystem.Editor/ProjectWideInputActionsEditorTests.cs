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

internal class ProjectWideInputActionsEditorTests
{
    // Note that only a selected few tests verifies the behavior associated with the editor support for
    // creating a dedicated asset. For all other logical tests we are better off constructing an asset on
    // the fly for functional tests to avoid differences between editor and playmode tests.
    //
    // Note that player tests are currently lacking since it would require a proper asset to be configured
    // during edit mode and then built and then loaded indirectly via config object / resources.
    //
    // Note that any existing default created asset is preserved during test run by moving it via ADB.

    const string kTestCategory = "ProjectWideActions";
    const string kAssetDirectory = "Assets";
    const string kAssetBackupDirectory = kAssetDirectory + "/TestBackupFiles";
    const string kDefaultProjectWideAssetBackupPath = kAssetBackupDirectory + "/DefaultProjectWideAssetBackup.json";

    private string m_SavedAssetPath;

    private InputActionAsset m_Actions;
    private bool m_ActionsArePersisted;

    private InputActionAsset m_OtherActions;
    private bool m_OtherActionsArePersisted;

    private int m_CallbackCount;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Avoid changing the editor setting for project-wide input actions (if any). To achieve this we
        // store the asset path since we cannot only keep a reference in case its equal to moved asset.
        // If not equal to moved asset above a reference would be fine, but might as well use a more robust
        // method.
        m_SavedAssetPath = AssetDatabase.GetAssetPath(InputSystem.actions);

        // Avoid overwriting any default asset already in /Assets folder by making a backup file not visible to
        // AssetDatabase. This is for verifying the default output of templated actions from editor tools.
        if (File.Exists(ProjectWideActionsAsset.defaultAssetPath))
        {
            if (!Directory.Exists(kAssetBackupDirectory))
                Directory.CreateDirectory(kAssetBackupDirectory);
            File.Copy(sourceFileName: ProjectWideActionsAsset.defaultAssetPath,
                destFileName: kDefaultProjectWideAssetBackupPath, overwrite: true);
            var wasDeleted = AssetDatabase.DeleteAsset(ProjectWideActionsAsset.defaultAssetPath);
            Assert.That(wasDeleted, Is.True);
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Restore default asset if we made a backup copy of it during setup
        if (File.Exists(kDefaultProjectWideAssetBackupPath))
        {
            if (File.Exists(ProjectWideActionsAsset.defaultAssetPath))
                AssetDatabase.DeleteAsset(ProjectWideActionsAsset.defaultAssetPath);
            File.Copy(sourceFileName: kDefaultProjectWideAssetBackupPath,
                destFileName: ProjectWideActionsAsset.defaultAssetPath);
            AssetDatabase.ImportAsset(ProjectWideActionsAsset.defaultAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabaseUtils.ExternalDeleteFileOrDirectory(kAssetBackupDirectory);
        }

        // Restore users project-wide input actions setting
        var asset = m_SavedAssetPath != null ? AssetDatabase.LoadAssetAtPath<InputActionAsset>(m_SavedAssetPath) : null;
        InputSystem.actions = asset;
    }

    [SetUp]
    public void Setup()
    {
        TestUtils.MockDialogs();

        m_CallbackCount = 0;

        // Always start with action null since this represents a fresh project
        InputSystem.actions = null;
    }

    [TearDown]
    public void TearDown()
    {
        InputSystem.onActionsChange -= OnActionsChange;

        // Delete any default asset we may have created (backup is safe until test class is destroyed)
        AssetDatabase.DeleteAsset(ProjectWideActionsAsset.defaultAssetPath);

        // Clean-up objects created during test
        if (m_Actions != null && !m_ActionsArePersisted)
            Object.DestroyImmediate(m_Actions);
        if (m_OtherActions != null && !m_OtherActionsArePersisted)
            Object.DestroyImmediate(m_OtherActions);

        InputSystem.actions = null;

        TestUtils.RestoreDialogs();
        AssetDatabaseUtils.Restore();
    }

    private void GivenActionsCallback()
    {
        InputSystem.onActionsChange += OnActionsChange;
    }

    private void GivenActions(bool persisted = false)
    {
        // Create a small InputActionsAsset on the fly that we utilize for testing
        m_Actions = ScriptableObject.CreateInstance<InputActionAsset>();
        m_Actions.name = "TestAsset";
        var one = m_Actions.AddActionMap("One");
        one.AddAction("A");
        one.AddAction("B");
        var two = m_Actions.AddActionMap("Two");
        two.AddAction("C");

        if (persisted)
        {
            var json = m_Actions.ToJson();
            Object.DestroyImmediate(m_Actions);
            m_Actions = AssetDatabaseUtils.CreateAsset<InputActionAsset>(content: json);

            m_ActionsArePersisted = true;
        }
    }

    private void GivenOtherActions(bool persisted = false)
    {
        // Create a small InputActionsAsset on the fly that we utilize for testing
        m_OtherActions = ScriptableObject.CreateInstance<InputActionAsset>();
        m_OtherActions.name = "OtherTestAsset";
        var three = m_OtherActions.AddActionMap("Three");
        three.AddAction("D");
        three.AddAction("E");

        if (persisted)
        {
            var json = m_OtherActions.ToJson();
            Object.DestroyImmediate(m_OtherActions);
            m_OtherActions = AssetDatabaseUtils.CreateAsset<InputActionAsset>(content: json);

            m_OtherActionsArePersisted = true;
        }
    }

    private void OnActionsChange()
    {
        ++m_CallbackCount;
    }

    [Test(Description = "Verifies that project-wide actions are not set by default")]
    [Category(kTestCategory)]
    public void ProjectWideActions_AreNotSetByDefault()
    {
        Assert.That(InputSystem.actions, Is.Null);
    }

    [Test(Description = "Verifies that project-wide actions defaults are constructed as an asset on the default asset path")]
    [Category(kTestCategory)]
    public void ProjectWideActionsAsset_DefaultAssetFileHasDefaultContent()
    {
        // Expect asset name to be set to the file name
        var expectedName = Path.GetFileNameWithoutExtension(ProjectWideActionsAsset.defaultAssetPath);
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath();
        Assert.That(asset.name, Is.EqualTo(expectedName));

        // Expect JSON name to be set to the file name
        var json = File.ReadAllText(EditorHelpers.GetPhysicalPath(ProjectWideActionsAsset.defaultAssetPath));
        var parsedAsset = InputActionAsset.FromJson(json);
        var parsedAssetName = parsedAsset.name;
        Object.DestroyImmediate(parsedAsset);
        Assert.That(parsedAssetName, Is.EqualTo(expectedName));
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
    [Category(kTestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenDifferent()
    {
        GivenActions(persisted: true);
        GivenOtherActions(persisted: true);
        GivenActionsCallback();

        // Can assign from null to null (no change)
        InputSystem.actions = null;
        Assert.That(m_CallbackCount, Is.EqualTo(0));

        // Can assign asset from null to instance (change)
        InputSystem.actions = m_Actions;
        Assert.That(m_CallbackCount, Is.EqualTo(1));

        // Can assign from instance to same instance (no change)
        InputSystem.actions = m_Actions;
        Assert.That(m_CallbackCount, Is.EqualTo(1)); // no callback expected

        // Can assign another instance (change
        InputSystem.actions = m_OtherActions;
        Assert.That(m_CallbackCount, Is.EqualTo(2));

        // Can assign asset from instance to null (change)
        InputSystem.actions = null;
        Assert.That(m_CallbackCount, Is.EqualTo(3));
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions in edit-mode, build settings are updated")]
    [Category(kTestCategory)]
    public void ProjectWideActions_WillUpdateBuildSettingsWhenChanged()
    {
        GivenActions(persisted: true);
        GivenOtherActions(persisted: true);
        GivenActionsCallback();

        Debug.Assert(EditorUtility.IsPersistent(m_Actions));

        // Can assign from null to null (no change)
        InputSystem.actions = null;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(null));

        // Can assign asset from null to instance (change)
        InputSystem.actions = m_Actions;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(m_Actions));

        // Can assign from instance to same instance (no change)
        InputSystem.actions = m_Actions;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(m_Actions));

        // Can assign another instance (change
        InputSystem.actions = m_OtherActions;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(m_OtherActions));

        // Can assign asset from instance to null (change)
        InputSystem.actions = null;
        Assert.That(ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild, Is.EqualTo(null));
    }

    [Test(Description =
            "Verifies that when assigning InputSystem.actions in edit-mode with a temporary object not persisted on disc, an exception is thrown")]
    [Category(kTestCategory)]
    public void ProjectWideActions_ThrowsArgumentException_WhenAssignedFromNonPersistedObject()
    {
        GivenActions();

        Assert.Throws<ArgumentException>(() => InputSystem.actions = m_Actions);
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions a callback is fired when currently being assigned to a deleted asset (destroyed object) and then assigning null")]
    [Category(kTestCategory)]
    public void ProjectWideActions_CanBeAssignedNullAndFiresCallback_WhenHavingDestroyedObjectAndAssignedNull()
    {
        GivenActions(persisted: true);
        //GivenOtherActions(persisted: true);
        GivenActionsCallback();

        // Assign and make sure property returns the expected assigned value
        InputSystem.actions = m_Actions;
        Assert.That(InputSystem.actions, Is.EqualTo(m_Actions));
        Assert.That(m_CallbackCount, Is.EqualTo(1));

        // Delete the associated asset make sure returned value evaluates to null (But actually Missing Reference).
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Actions));
        Assert.That(m_Actions == null, Is.True);    // sanity check that it was destroyed
        Assert.That(InputSystem.actions == null); // note: we want to avoid cast to object since it would use another Equals

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = null;
        Assert.That(InputSystem.actions == null);
        Assert.That(ReferenceEquals(InputSystem.actions, null)); // check its really null and not just Missing Reference.
        Assert.That(m_CallbackCount, Is.EqualTo(2));
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions a callback is fired when the previously assigned asset has been destroyed object")]
    [Category(kTestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenDifferent_WhenAssignedDestroyedObject()
    {
        GivenActions(persisted: true);
        //GivenOtherActions();
        GivenActionsCallback();

        // Destroy the associated asset and make sure returned value evaluates to null (But actually Missing Reference).
        //Object.DestroyImmediate(actions);
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Actions));
        Assert.That(m_Actions == null, Is.True);       // sanity check that it was destroyed

        // Assert that we can assign a destroyed object
        InputSystem.actions = m_Actions;
        Assert.That(InputSystem.actions == m_Actions); // note: we want to avoid cast to object since it would use another Equals
        Assert.That(!ReferenceEquals(InputSystem.actions, null)); // expecting missing reference
        Assert.That(m_CallbackCount, Is.EqualTo(1));

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = null;
        Assert.That(InputSystem.actions == null);
        Assert.That(ReferenceEquals(InputSystem.actions, null)); // check its really null and not just Missing Reference.
        Assert.That(m_CallbackCount, Is.EqualTo(2));
    }

    [Test(Description = "Verifies that when assigning InputSystem.actions a callback is fired when assigning and current object has been destroyed")]
    [Category(kTestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenAssignedAndDifferent_WhenHavingDestroyedObjectAndAssignedOther()
    {
        GivenActions(persisted: true);
        GivenOtherActions(persisted: true);
        GivenActionsCallback();

        // Assign and make sure property returns the expected assigned value
        InputSystem.actions = m_Actions;
        Assert.That(InputSystem.actions, Is.EqualTo(m_Actions));
        Assert.That(m_CallbackCount, Is.EqualTo(1));

        // Destroy the associated asset and make sure returned value evaluates to null (But actually Missing Reference).
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Actions));
        Assert.That(m_Actions == null, Is.True);    // sanity check that it was destroyed
        Assert.That(InputSystem.actions == null); // note: we want to avoid cast to object since it would use another Equals

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = m_OtherActions;
        Assert.That(InputSystem.actions, Is.EqualTo(m_OtherActions));
        Assert.That(m_CallbackCount, Is.EqualTo(2));
    }
}

#endif
