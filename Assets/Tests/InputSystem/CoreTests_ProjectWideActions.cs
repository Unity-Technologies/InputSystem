#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

internal partial class CoreTests
{
    const string TestCategory = "ProjectWideActions";

    const int initialActionCount = 2;
    const int initialMapCount = 1;

    [SetUp]
    public override void Setup()
    {
        // This asset takes the place of the project wide actions asset for the sake of testing, as we don't
        // really want to go changing that asset in every test.
        // This is used as a backing for `InputSystem.actions` in PlayMode tests.
        var testAsset = ScriptableObject.CreateInstance<InputActionAsset>();
        testAsset.name = InputSystem.kProjectWideActionsAssetName;

        var map = testAsset.AddActionMap("InitialActionMapOne");
        map.AddAction("InitialActionOne");
        map.AddAction("InitialActionTwo");

#if UNITY_EDITOR
        ProjectWideActionsAsset.testAsset = testAsset;
#else
        InputSystem.actions = testAsset;
#endif

        base.Setup();
    }

    [TearDown]
    public override void TearDown()
    {
#if UNITY_EDITOR
        ProjectWideActionsAsset.testAsset = null;
#endif

        base.TearDown();
    }

#if UNITY_EDITOR
    [Test]
    [Category(TestCategory)]
    public void ProjectWideActionsAsset_TemplateAssetIsInstalledOnFirstUse()
    {
        var asset = ProjectWideActionsAsset.GetOrCreate();

        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.actionMaps.Count, Is.EqualTo(initialMapCount));
        Assert.That(asset.actionMaps[0].actions.Count, Is.EqualTo(initialActionCount));
        Assert.That(asset.actionMaps[0].actions[0].name, Is.EqualTo("InitialActionOne"));
    }

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActionsAsset_CanModifySaveAndLoadAsset()
    {
        var asset = ProjectWideActionsAsset.GetOrCreate();

        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.actionMaps.Count, Is.EqualTo(initialMapCount));
        Assert.That(asset.actionMaps[0].actions.Count, Is.EqualTo(initialActionCount));
        Assert.That(asset.actionMaps[0].actions[0].name, Is.EqualTo("InitialActionOne"));

        asset.Disable(); // Cannot modify active actions

        // Add more actions
        asset.actionMaps[0].AddAction("ActionTwo");
        asset.actionMaps[0].AddAction("ActionThree");

        // Modify existing
        asset.actionMaps[0].actions[0].Rename("FirstAction");

        // Add another map
        asset.AddActionMap("ActionMapTwo").AddAction("AnotherAction");

        // Save
        AssetDatabase.SaveAssets();

        // Reload
        asset = ProjectWideActionsAsset.GetOrCreate();

        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.actionMaps.Count, Is.EqualTo(initialMapCount + 1));
        Assert.That(asset.actionMaps[0].actions.Count, Is.EqualTo(initialActionCount + 2));
        Assert.That(asset.actionMaps[1].actions.Count, Is.EqualTo(1));
        Assert.That(asset.actionMaps[0].actions[0].name, Is.EqualTo("FirstAction"));
        Assert.That(asset.actionMaps[1].actions[0].name, Is.EqualTo("AnotherAction"));
    }

#endif

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_AreEnabledByDefault()
    {
        Assert.That(InputSystem.actions, Is.Not.Null);
        Assert.That(InputSystem.actions.enabled, Is.True);
    }

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_ContainsTemplateActions()
    {
        Assert.That(InputSystem.actions, Is.Not.Null);
        Assert.That(InputSystem.actions.actionMaps.Count, Is.EqualTo(initialMapCount));

        Assert.That(InputSystem.actions.actionMaps[0].actions.Count, Is.EqualTo(initialActionCount));
        Assert.That(InputSystem.actions.actionMaps[0].actions[0].name, Is.EqualTo("InitialActionOne"));
    }

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_AppearInEnabledActions()
    {
        var enabledActions = InputSystem.ListEnabledActions();
        Assert.That(enabledActions, Has.Count.EqualTo(initialActionCount));

        // Add more actions also work
        var action = new InputAction(name: "standaloneAction");
        action.Enable();

        enabledActions = InputSystem.ListEnabledActions();
        Assert.That(enabledActions, Has.Count.EqualTo(initialActionCount + 1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action));

        // Disabling works
        InputSystem.actions?.Disable();
        enabledActions = InputSystem.ListEnabledActions();
        Assert.That(enabledActions, Has.Count.EqualTo(1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action));
    }

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_CanReplaceExistingActions()
    {
        // Initial State
        Assert.That(InputSystem.actions, Is.Not.Null);
        Assert.That(InputSystem.actions.enabled, Is.True);
        var enabledActions = InputSystem.ListEnabledActions();
        Assert.That(enabledActions, Has.Count.EqualTo(initialActionCount));

        // Build new asset
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = new InputActionMap("replacedMap1");
        var map2 = new InputActionMap("replacedMap2");
        var action1 = map1.AddAction("replacedAction1", InputActionType.Button);
        var action2 = map1.AddAction("replacedAction2", InputActionType.Button);
        var action3 = map1.AddAction("replacedAction3", InputActionType.Button);
        var action4 = map2.AddAction("replacedAction4", InputActionType.Button);

        action1.AddBinding("<Gamepad>/buttonSouth");
        action2.AddBinding("<Gamepad>/buttonWest");
        action3.AddBinding("<Gamepad>/buttonNorth");
        action4.AddBinding("<Gamepad>/buttonEast");
        asset.AddActionMap(map1);
        asset.AddActionMap(map2);

        // Replace project-wide actions
        InputSystem.actions = asset;

        // State after replacing
        Assert.That(InputSystem.actions, Is.Not.Null);
        Assert.That(InputSystem.actions.enabled, Is.True);
        enabledActions = InputSystem.ListEnabledActions();
        Assert.That(enabledActions, Has.Count.EqualTo(4));

        Assert.That(InputSystem.actions.actionMaps.Count, Is.EqualTo(2));
        Assert.That(InputSystem.actions.actionMaps[0].actions.Count, Is.EqualTo(3));
        Assert.That(InputSystem.actions.actionMaps[0].actions[0].name, Is.EqualTo("replacedAction1"));
        Assert.That(InputSystem.actions.actionMaps[1].actions.Count, Is.EqualTo(1));
        Assert.That(InputSystem.actions.actionMaps[1].actions[0].name, Is.EqualTo("replacedAction4"));
    }
}

#endif
