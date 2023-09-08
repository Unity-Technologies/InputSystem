#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.IO;
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
    const string TestAssetPath = "Assets/TestInputManager.asset";
    string m_TemplateAssetPath;

#if UNITY_EDITOR
    const int initialActionCount = 2;
    const int initialMapCount = 1;
#else
    const int initialActionCount = 17;
    const int initialMapCount = 2;
#endif

    [SetUp]
    public override void Setup()
    {
        // @TODO: Currently we can only inject the TestActionsAsset in PlayMode tests.
        // It would be nice to be able to inject it as a Preloaded asset into the Player tests so
        // we don't need different tests for the player.
        // This also means these tests are dependant on the content of InputManager.asset not being changed.
#if UNITY_EDITOR
        // This asset takes the place of ProjectSettings/InputManager.asset for the sake of testing, as we don't
        // really want to go changing that asset in every test.
        // This is used as a backing for `InputSystem.actions` in PlayMode tests.
        var testAsset = ScriptableObject.CreateInstance<TestActionsAsset>();
        AssetDatabase.CreateAsset(testAsset, TestAssetPath);

        // Create a template `InputActionAsset` containing some test actions.
        // This will then be used to populate the initially empty `TestActionsAsset` when it is first acessed.
        var templateActions = ScriptableObject.CreateInstance<InputActionAsset>();
        templateActions.name = "TestAsset";
        var map = templateActions.AddActionMap("InitialActionMapOne");
        map.AddAction("InitialActionOne");
        map.AddAction("InitialActionTwo");

        m_TemplateAssetPath = Path.Combine(Environment.CurrentDirectory, "Assets/ProjectWideActionsTemplate.inputactions");
        File.WriteAllText(m_TemplateAssetPath, templateActions.ToJson());

        ProjectWideActionsAsset.SetAssetPaths(m_TemplateAssetPath, TestAssetPath);
#endif

        base.Setup();
    }

    [TearDown]
    public override void TearDown()
    {
#if UNITY_EDITOR
        ProjectWideActionsAsset.Reset();

        if (File.Exists(m_TemplateAssetPath))
            File.Delete(m_TemplateAssetPath);

        AssetDatabase.DeleteAsset(TestAssetPath);
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

#if UNITY_EDITOR
        Assert.That(InputSystem.actions.actionMaps[0].actions.Count, Is.EqualTo(initialActionCount));
        Assert.That(InputSystem.actions.actionMaps[0].actions[0].name, Is.EqualTo("InitialActionOne"));
#else
        Assert.That(InputSystem.actions.actionMaps[0].actions.Count, Is.EqualTo(9));
        Assert.That(InputSystem.actions.actionMaps[0].actions[0].name, Is.EqualTo("Move"));
#endif
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
        InputSystem.actions.Disable();
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
