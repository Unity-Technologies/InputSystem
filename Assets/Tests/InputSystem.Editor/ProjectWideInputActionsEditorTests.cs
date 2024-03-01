#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;

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

    const string TestCategory = "ProjectWideActions";
    const string m_AssetBackupDirectory = "Assets/~TestBackupFiles";
    const string s_DefaultProjectWideAssetBackupPath = "Assets/~TestBackupFilesDefaultProjectWideAssetBackup.json";

    private InputActionAsset savedUserActions;
    private InputActionAsset actions;
    private InputActionAsset otherActions;
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
    public void Setup()
    {
        callbackCount = 0;

        // In case project-wide actions have been configured, save a reference to the object to be able
        // to restore after test run.
        savedUserActions = InputSystem.actions;
        InputSystem.actions = null;
    }

    [TearDown]
    public void TearDown()
    {
        InputSystem.onActionsChange -= OnActionsChange;

        // Delete any default asset we may have created (backup is safe until test class is destroyed)
        AssetDatabase.DeleteAsset(ProjectWideActionsAsset.defaultAssetPath);

        // Clean-up objects created during test
        if (actions != null)
            Object.DestroyImmediate(actions);
        if (otherActions != null)
            Object.DestroyImmediate(otherActions);

        // Restore actions
        InputSystem.actions = savedUserActions;
    }

    private void GivenActionsCallback()
    {
        InputSystem.onActionsChange += OnActionsChange;
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

    private void OnActionsChange()
    {
        ++callbackCount;
    }

    // TODO This is useless when modified and tested in edit mode?!
    // In player the tests freshly created input assets is assigned
    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_AreNotSetByDefault()
    {
        Assert.That(InputSystem.actions, Is.Null);
    }

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActionsAsset_HasFilenameName()
    {
        // Expect asset name to be set to the file name
        var expectedName = Path.GetFileNameWithoutExtension(ProjectWideActionsAsset.defaultAssetPath);
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath();
        Assert.That(asset.name, Is.EqualTo(expectedName));

        // Expect JSON name to be set to the file name
        var json = EditorHelpers.ReadAllText(ProjectWideActionsAsset.defaultAssetPath);
        var parsedAsset = InputActionAsset.FromJson(json);
        Assert.That(parsedAsset.name, Is.EqualTo(expectedName));
        Object.DestroyImmediate(parsedAsset);
    }

    // This test is only relevant for the InputForUI module which native part was introduced in 2023.2
#if UNITY_2023_2_OR_NEWER
    [Test]
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

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenDifferent()
    {
        GivenActions();
        GivenOtherActions();
        GivenActionsCallback();

#if UNITY_EDITOR
        var expected = 0;
#else
        var expected = 1;
#endif

        // Can assign from null to null (no change)
        InputSystem.actions = null;
        Assert.That(callbackCount, Is.EqualTo(expected));

        // Can assign asset from null to instance (change)
        InputSystem.actions = actions;
        expected++;
        Assert.That(callbackCount, Is.EqualTo(expected));

        // Can assign from instance to same instance (no change)
        InputSystem.actions = actions;
        Assert.That(callbackCount, Is.EqualTo(expected));

        // Can assign another instance (change
        InputSystem.actions = otherActions;
        expected++;
        Assert.That(callbackCount, Is.EqualTo(expected));

        // Can assign asset from instance to null (change)
        InputSystem.actions = null;
        expected++;
        Assert.That(callbackCount, Is.EqualTo(expected));
    }

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenDifferent_WhenHavingDestroyedObjectAndAssignedNull()
    {
        GivenActions();
        GivenOtherActions();
        GivenActionsCallback();

        // Assign and make sure property returns the expected assigned value
        InputSystem.actions = actions;
        Assert.That(InputSystem.actions, Is.EqualTo(actions));
        Assert.That(callbackCount, Is.EqualTo(1));

        // Destroy the associated asset and make sure returned value evaluates to null (But actually Missing Reference).
        Object.DestroyImmediate(actions);
        Assert.That(actions == null, Is.True);    // sanity check that it was destroyed
        Assert.That(InputSystem.actions == null); // note: we want to avoid cast to object since it would use another Equals

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = null;
        Assert.That(InputSystem.actions == null);
        Assert.That(ReferenceEquals(InputSystem.actions, null)); // check its really null and not just Missing Reference.
        Assert.That(callbackCount, Is.EqualTo(2));
    }

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenDifferent_WhenAssignedDestroyedObject()
    {
        GivenActions();
        GivenOtherActions();
        GivenActionsCallback();

        // Destroy the associated asset and make sure returned value evaluates to null (But actually Missing Reference).
        Object.DestroyImmediate(actions);
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

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_CanBeAssignedAndFiresCallbackWhenDifferent_WhenHavingDestroyedObjectAndAssignedOther()
    {
        GivenActions();
        GivenOtherActions();
        GivenActionsCallback();

        // Assign and make sure property returns the expected assigned value
        InputSystem.actions = actions;
        Assert.That(InputSystem.actions, Is.EqualTo(actions));
        Assert.That(callbackCount, Is.EqualTo(1));

        // Destroy the associated asset and make sure returned value evaluates to null (But actually Missing Reference).
        Object.DestroyImmediate(actions);
        Assert.That(actions == null, Is.True);    // sanity check that it was destroyed
        Assert.That(InputSystem.actions == null); // note: we want to avoid cast to object since it would use another Equals

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = otherActions;
        Assert.That(InputSystem.actions, Is.EqualTo(otherActions));
        Assert.That(callbackCount, Is.EqualTo(2));
    }
}

#endif
