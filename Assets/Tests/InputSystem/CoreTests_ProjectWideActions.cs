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
    public void OneTimeTearDown()
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
        InputSystem.onActionsChange -= OnActionsChange;

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

    private void Destroy(Object obj)
    {
#if UNITY_EDITOR
        Object.DestroyImmediate(obj);
#else
        Object.DestroyImmediate(actions);
#endif
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

    private void GivenActionsCallback()
    {
        InputSystem.onActionsChange += OnActionsChange;
    }

    private void OnActionsChange()
    {
        ++callbackCount;
    }

#if UNITY_EDITOR
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
        Object.Destroy(parsedAsset);
    }

#if UNITY_2023_2_OR_NEWER // This test is only relevant for the InputForUI module
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

#endif // UNITY_EDITOR

#if UNITY_EDITOR
    // In player the tests freshly created input assets assetis assigned
    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_AreNotSetByDefault()
    {
        Assert.That(InputSystem.actions, Is.Null);
    }
#endif

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
        Destroy(actions);
        Assert.That(actions == null, Is.True);    // sanity check that it was destroyed
        Assert.That(InputSystem.actions == null); // note: we want to avoid cast to object since it would use another Equals

        // Assert that property may be assigned to null reference since its different from missing reference.
        InputSystem.actions = otherActions;
        Assert.That(InputSystem.actions, Is.EqualTo(otherActions));
        Assert.That(callbackCount, Is.EqualTo(2));
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
        Destroy(actions);
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
        Destroy(actions);
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
    public void ProjectWideActions_SanityCheck()
    {
        InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
        Assert.False(asset == null);
        Assert.False(ReferenceEquals(asset, null));

        Object.DestroyImmediate(asset);
        Assert.True(asset == null);
        Assert.False(ReferenceEquals(asset, null));

        asset = null;
        Assert.True(asset == null);
        Assert.True(ReferenceEquals(asset, null));
    }

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_AppearInEnabledActions()
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

#if UNITY_EDITOR
    [Test]
    [Ignore("Reenable this test once clear how it relates or is specific to ProjectWideActions. Seems like this is rather testing something general. As a side-note likely no maps should be enabled in edit mode?!")]
    [Category(TestCategory)]
    public void ProjectWideActions_ThrowsWhenAddingOrRemovingWhileEnabled()
    {
        GivenActions();
        var asset = actions;

        // Verify adding ActionMap while enabled throws an exception
        Assert.Throws<InvalidOperationException>(() => asset.AddActionMap("AnotherMap").AddAction("AnotherAction"));

        asset.Disable();
        asset.AddActionMap("AnotherMap").AddAction("AnotherAction");

        // Verify enabled state reported correctly
        Assert.That(asset.enabled, Is.False);
        Assert.That(asset.FindActionMap("AnotherMap", true).enabled, Is.False);
        Assert.That(asset.FindAction("AnotherAction", true).enabled, Is.False);

        asset.Enable();

        Assert.That(asset.enabled, Is.True);
        Assert.That(asset.FindActionMap("AnotherMap", true).enabled, Is.True);
        Assert.That(asset.FindAction("AnotherAction", true).enabled, Is.True);

        // Verify adding/removing actions throws when ActionMap is enabled
        Assert.Throws<System.InvalidOperationException>(() => asset.FindActionMap("AnotherMap", true).AddAction("YetAnotherAction"));
        Assert.Throws<System.InvalidOperationException>(() => asset.RemoveAction("AnotherAction"));
        Assert.Throws<InvalidOperationException>(() => asset.RemoveActionMap("AnotherMap"));

        // Verify enabled state when enabling Action directly
        asset.Disable();
        asset.FindAction("AnotherAction", true).Enable();

        Assert.That(asset.FindActionMap("InitialActionMapOne", true).enabled, Is.False);
        Assert.That(asset.FindActionMap("AnotherMap", true).enabled, Is.True);
        Assert.That(asset.FindAction("AnotherAction", true).enabled, Is.True);

        // Verify removing any ActionMap throws if another one is enabled
        Assert.Throws<System.InvalidOperationException>(() => asset.RemoveActionMap("InitialActionMapOne"));
    }

#endif // UNITY_EDITOR
}
#endif
