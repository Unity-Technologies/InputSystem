#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

internal partial class CoreTests
{
    const string TestCategory = "ProjectWideActions";
    const string TestAssetPath = "Assets/TestInputManager.asset";
    string m_TemplateAssetPath;

#if UNITY_EDITOR
    const int initialTotalActionCount = 12;
    const int initialMapCount = 2;
    const int initialFirstActionMapCount = 2;
#else
    const int initialTotalActionCount = 19;
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

        var defaultUIMapTemplate = ProjectWideActionsAsset.GetDefaultUIActionMap();

        // Create a template `InputActionAsset` containing some test actions.
        // This will then be used to populate the initially empty `TestActionsAsset` when it is first acessed.
        var templateActions = ScriptableObject.CreateInstance<InputActionAsset>();
        templateActions.name = "TestAsset";
        var map = templateActions.AddActionMap("InitialActionMapOne");
        map.AddAction("InitialActionOne");
        map.AddAction("InitialActionTwo");

        // Add the default UI map to the template
        templateActions.AddActionMap(defaultUIMapTemplate);

        m_TemplateAssetPath = Path.Combine(Environment.CurrentDirectory, "Assets/ProjectWideActionsTemplate.inputactions");
        File.WriteAllText(m_TemplateAssetPath, templateActions.ToJson());

        ProjectWideActionsAsset.TestHook_SetAssetPaths(m_TemplateAssetPath, TestAssetPath);
#endif

        base.Setup();
    }

    [TearDown]
    public override void TearDown()
    {
#if UNITY_EDITOR
        ProjectWideActionsAsset.TestHook_Reset();

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
        var asset = ProjectWideActionsAsset.instance;

        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.actionMaps.Count, Is.EqualTo(initialMapCount));
        Assert.That(asset.actionMaps[0].actions.Count, Is.EqualTo(initialFirstActionMapCount));
        Assert.That(asset.actionMaps[0].actions[0].name, Is.EqualTo("InitialActionOne"));
    }

    [Test]
    [Category(TestCategory)]
    public void ProjectWideActionsAsset_CanModifySaveAndLoadAsset()
    {
        var asset = ProjectWideActionsAsset.instance;

        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.actionMaps.Count, Is.EqualTo(initialMapCount));
        Assert.That(asset.actionMaps[0].actions.Count, Is.EqualTo(initialFirstActionMapCount));
        Assert.That(asset.actionMaps[0].actions[0].name, Is.EqualTo("InitialActionOne"));

        asset.Disable(); // Cannot modify active actions

        // Add more actions
        asset.actionMaps[0].AddAction("ActionTwo");
        asset.actionMaps[0].AddAction("ActionThree");

        // Modify existing
        asset.actionMaps[0].actions[0].Rename("FirstAction");

        // Add another map
        asset.AddActionMap("ActionMapThree").AddAction("AnotherAction");

        // Save
        AssetDatabase.SaveAssets();

        // Reload
        asset = ProjectWideActionsAsset.instance;

        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.actionMaps.Count, Is.EqualTo(initialMapCount + 1));
        Assert.That(asset.actionMaps[0].actions.Count, Is.EqualTo(initialFirstActionMapCount + 2));
        Assert.That(asset.actionMaps[1].actions.Count, Is.EqualTo(10));
        Assert.That(asset.actionMaps[0].actions[0].name, Is.EqualTo("FirstAction"));
        Assert.That(asset.actionMaps[2].actions[0].name, Is.EqualTo("AnotherAction"));
    }

    #if UNITY_2023_2_OR_NEWER
    [Test]
    [Category(TestCategory)]
    public void ProjectWideActions_ShowsErrorWhenUIActionMapHasNameChanges()  // This test is only relevant for the InputForUI module
    {
        var asset = ProjectWideActionsAsset.instance;
        var indexOf = asset.m_ActionMaps.IndexOf(x => x.name == "UI");
        var uiMap = asset.m_ActionMaps[indexOf];

        // Change the name of the UI action map
        uiMap.m_Name = "UI2";

        ProjectWideActionsAsset.CheckForDefaultUIActionMapChanges();

        LogAssert.Expect(LogType.Warning, new Regex("The action map named 'UI' does not exist"));

        // Change the name of some UI map back to default and change the name of the actions
        uiMap.m_Name = "UI";
        var defaultActionName0 = uiMap.m_Actions[0].m_Name;
        var defaultActionName1 = uiMap.m_Actions[1].m_Name;

        uiMap.m_Actions[0].Rename("Navigation");
        uiMap.m_Actions[1].Rename("Show");

        ProjectWideActionsAsset.CheckForDefaultUIActionMapChanges();

        LogAssert.Expect(LogType.Warning, new Regex($"The UI action '{defaultActionName0}' name has been modified"));
        LogAssert.Expect(LogType.Warning, new Regex($"The UI action '{defaultActionName1}' name has been modified"));
    }

    #endif

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
        Assert.That(InputSystem.actions.actionMaps[0].actions.Count, Is.EqualTo(initialFirstActionMapCount));
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
        Assert.That(enabledActions, Has.Count.EqualTo(initialTotalActionCount));

        // Add more actions also work
        var action = new InputAction(name: "standaloneAction");
        action.Enable();

        enabledActions = InputSystem.ListEnabledActions();
        Assert.That(enabledActions, Has.Count.EqualTo(initialTotalActionCount + 1));
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
        Assert.That(enabledActions, Has.Count.EqualTo(initialTotalActionCount));

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
