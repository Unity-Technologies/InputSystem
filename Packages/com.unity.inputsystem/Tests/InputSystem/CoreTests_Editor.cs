#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Composites;
using UnityEngine.Experimental.Input.Editor;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.HID;
using UnityEngine.TestTools;

partial class CoreTests
{
    [Serializable]
    struct PackageJson
    {
        public string version;
    }

    [Test]
    [Category("Editor")]
    public void Editor_PackageVersionAndAssemblyVersionAreTheSame()
    {
        var packageJsonFile = File.ReadAllText("Packages/com.unity.inputsystem/package.json");
        var packageJson = JsonUtility.FromJson<PackageJson>(packageJsonFile);

        // Snip -preview off the end. System.Version doesn't support semantic versioning.
        var versionString = packageJson.version;
        if (versionString.EndsWith("-preview"))
            versionString = versionString.Substring(0, versionString.Length - "-preview".Length);
        var version = new Version(versionString);

        Assert.That(InputSystem.version.Major, Is.EqualTo(version.Major));
        Assert.That(InputSystem.version.Minor, Is.EqualTo(version.Major));
        Assert.That(InputSystem.version.Build, Is.EqualTo(version.Build));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanSaveAndRestoreState()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterLayout(json);
        InputSystem.AddDevice("MyDevice");
        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            product = "Product",
            manufacturer = "Manufacturer",
            interfaceName = "Test"
        }.ToJson());
        InputSystem.Update();

        InputSystem.SaveAndReset();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(0));

        InputSystem.Restore();

        Assert.That(InputSystem.devices,
            Has.Exactly(1).With.Property("layout").EqualTo("MyDevice").And.TypeOf<Gamepad>());

        var unsupportedDevices = new List<InputDeviceDescription>();
        InputSystem.GetUnsupportedDevices(unsupportedDevices);

        Assert.That(unsupportedDevices.Count, Is.EqualTo(1));
        Assert.That(unsupportedDevices[0].product, Is.EqualTo("Product"));
        Assert.That(unsupportedDevices[0].manufacturer, Is.EqualTo("Manufacturer"));
        Assert.That(unsupportedDevices[0].interfaceName, Is.EqualTo("Test"));
    }

    // onFindLayoutForDevice allows dynamically injecting new layouts into the system that
    // are custom-tailored at runtime for the discovered device. Make sure that our domain
    // reload can restore these.
    [Test]
    [Category("Editor")]
    public void Editor_DomainReload_CanRestoreDevicesBuiltWithDynamicallyGeneratedLayouts()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = (int)HID.GenericDesktop.MultiAxisController,
            usagePage = HID.UsagePage.GenericDesktop,
            vendorId = 0x1234,
            productId = 0x5678,
            inputReportSize = 4,
            elements = new[]
            {
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 32 },
            }
        };

        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<HID>());

        InputSystem.SaveAndReset();

        Assert.That(InputSystem.devices, Is.Empty);

        var state = InputSystem.GetSavedState();
        var manager = InputSystem.s_Manager;

        manager.m_SavedAvailableDevices = state.managerState.availableDevices;
        manager.m_SavedDeviceStates = state.managerState.devices;

        manager.RestoreDevicesAfterDomainReload();

        Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<HID>());

        InputSystem.Restore();
    }

    [Test]
    [Category("Editor")]
    public void Editor_DomainReload_PreservesUsagesOnDevices()
    {
        var device = InputSystem.AddDevice<Gamepad>();
        InputSystem.SetDeviceUsage(device, CommonUsages.LeftHand);

        InputSystem.SaveAndReset();
        InputSystem.Restore();

        var newDevice = InputSystem.devices.First(x => x is Gamepad);

        Assert.That(newDevice.usages, Has.Count.EqualTo(1));
        Assert.That(newDevice.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
    }

    [Test]
    [Category("Editor")]
    public void Editor_DomainReload_PreservesUserInteractionFiltersOnDevice()
    {
        InputNoiseFilter filter = new InputNoiseFilter
        {
            elements = new InputNoiseFilter.FilterElement[]
            {
                new InputNoiseFilter.FilterElement
                {
                    controlIndex = 0,
                    type = InputNoiseFilter.ElementType.EntireControl
                }
            }
        };

        var device = InputSystem.AddDevice<Gamepad>();
        device.userInteractionFilter = filter;

        InputSystem.SaveAndReset();
        InputSystem.Restore();

        var newDevice = InputSystem.devices.First(x => x is Gamepad);

        Assert.That(newDevice.userInteractionFilter, Is.Not.Null);
        Assert.That(newDevice.userInteractionFilter.elements, Has.Length.EqualTo(1));
        Assert.That(newDevice.userInteractionFilter.elements[0].controlIndex, Is.EqualTo(0));
        Assert.That(newDevice.userInteractionFilter.elements[0].type, Is.EqualTo(InputNoiseFilter.ElementType.EntireControl));
    }

    [Test]
    [Category("Editor")]
    [Ignore("TODO")]
    public void TODO_Editor_DomainReload_PreservesVariantsOnDevices()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Editor")]
    [Ignore("TODO")]
    public void TODO_Editor_DomainReload_PreservesCurrentDevices()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Editor")]
    public void Editor_RestoringStateWillCleanUpEventHooks()
    {
        InputSystem.SaveAndReset();

        var receivedOnEvent = 0;
        var receivedOnDeviceChange = 0;

        InputSystem.onEvent += _ => ++ receivedOnEvent;
        InputSystem.onDeviceChange += (c, d) => ++ receivedOnDeviceChange;

        InputSystem.Restore();

        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();

        Assert.That(receivedOnEvent, Is.Zero);
        Assert.That(receivedOnDeviceChange, Is.Zero);
    }

    [Test]
    [Category("Editor")]
    public void Editor_RestoringStateWillRestoreObjectsOfLayoutBuilder()
    {
        var builder = new TestLayoutBuilder {layoutToLoad = "Gamepad"};
        InputSystem.RegisterLayoutBuilder(() => builder.DoIt(), "TestLayout");

        InputSystem.SaveAndReset();
        InputSystem.Restore();

        var device = InputSystem.AddDevice("TestLayout");

        Assert.That(device, Is.TypeOf<Gamepad>());
    }

    // Editor updates are confusing in that they denote just another point in the
    // application loop where we push out events. They do not mean that the events
    // we send necessarily go to the editor state buffers.
    [Test]
    [Category("Editor")]
    public void Editor_WhenPlaying_EditorUpdatesWriteEventIntoPlayerState()
    {
        InputConfiguration.LockInputToGame = true;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.25f});
        InputSystem.Update(InputUpdateType.Dynamic);

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.75f});
        InputSystem.Update(InputUpdateType.Editor);

        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.75).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadPreviousValue(), Is.EqualTo(0.25).Within(0.000001));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveActionMapThroughSerialization()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var obj = new SerializedObject(asset);

        InputActionSerializationHelpers.AddActionMap(obj);
        InputActionSerializationHelpers.AddActionMap(obj);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps, Has.Count.EqualTo(2));
        Assert.That(asset.actionMaps[0].name, Is.Not.Null.Or.Empty);
        Assert.That(asset.actionMaps[1].name, Is.Not.Null.Or.Empty);
        Assert.That(asset.actionMaps[0].m_Id, Is.Not.Empty);
        Assert.That(asset.actionMaps[1].m_Id, Is.Not.Empty);
        Assert.That(asset.actionMaps[0].name, Is.Not.EqualTo(asset.actionMaps[1].name));

        var actionMap2Name = asset.actionMaps[1].name;

        InputActionSerializationHelpers.DeleteActionMap(obj, 0);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps, Has.Count.EqualTo(1));
        Assert.That(asset.actionMaps[0].name, Is.EqualTo(actionMap2Name));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddActionMapFromSavedProperties()
    {
        var map = new InputActionMap("set");
        var binding = new InputBinding();
        binding.path = "some path";
        var action = map.AddAction("action");
        action.AddBinding(binding);

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var obj = new SerializedObject(asset);

        var parameters = new Dictionary<string, string>();
        parameters.Add("m_Name", "set");

        Assert.That(asset.actionMaps, Has.Count.EqualTo(0));

        InputActionSerializationHelpers.AddActionMapFromSavedProperties(obj, parameters);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps, Has.Count.EqualTo(1));
        Assert.That(asset.actionMaps[0].name, Is.EqualTo("set"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveActionThroughSerialization()
    {
        var map = new InputActionMap("set");
        map.AddAction(name: "action", binding: "/gamepad/leftStick");
        map.AddAction(name: "action1", binding: "/gamepad/rightStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AddAction(mapProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps[0].actions, Has.Count.EqualTo(3));
        Assert.That(asset.actionMaps[0].actions[2].name, Is.EqualTo("action2"));
        Assert.That(asset.actionMaps[0].actions[2].m_Id, Is.Not.Empty);
        Assert.That(asset.actionMaps[0].actions[2].bindings, Has.Count.Zero);

        InputActionSerializationHelpers.DeleteAction(mapProperty, 2);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps[0].actions, Has.Count.EqualTo(2));
        Assert.That(asset.actionMaps[0].actions[0].name, Is.EqualTo("action"));
        Assert.That(asset.actionMaps[0].actions[1].name, Is.EqualTo("action1"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveBindingThroughSerialization()
    {
        var map = new InputActionMap("set");
        map.AddAction(name: "action1", binding: "/gamepad/leftStick");
        map.AddAction(name: "action2", binding: "/gamepad/rightStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);
        var action1Property = mapProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AddBinding(action1Property, mapProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        // Maps and actions aren't UnityEngine.Objects so the modifications will not
        // be in-place. Look up the actions after each apply.
        var action1 = asset.actionMaps[0].TryGetAction("action1");
        var action2 = asset.actionMaps[0].TryGetAction("action2");

        Assert.That(action1.bindings, Has.Count.EqualTo(2));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action1.bindings[1].path, Is.EqualTo(""));
        Assert.That(action1.bindings[1].interactions, Is.EqualTo(""));
        Assert.That(action1.bindings[1].groups, Is.EqualTo(""));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));

        InputActionSerializationHelpers.RemoveBinding(action1Property, 1, mapProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        action1 = asset.actionMaps[0].TryGetAction("action1");
        action2 = asset.actionMaps[0].TryGetAction("action2");

        Assert.That(action1.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddBindingFromSavedProperties()
    {
        var map = new InputActionMap("set");
        map.AddAction(name: "action1");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);
        var action1Property = mapProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        var pathName = "/gamepad/leftStick";
        var name = "some name";
        var interactionsName = "someinteractions";
        var sourceActionName = "some action";
        var groupName = "group";
        var flags = 10;

        var parameters = new Dictionary<string, string>();
        parameters.Add("path", pathName);
        parameters.Add("name", name);
        parameters.Add("groups", groupName);
        parameters.Add("interactions", interactionsName);
        parameters.Add("flags", "" + flags);
        parameters.Add("action", sourceActionName);

        InputActionSerializationHelpers.AddBindingFromSavedProperties(parameters, action1Property, mapProperty);

        obj.ApplyModifiedPropertiesWithoutUndo();

        var action1 = asset.actionMaps[0].TryGetAction("action1");
        Assert.That(action1.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo(pathName));
        Assert.That(action1.bindings[0].action, Is.EqualTo("action1"));
        Assert.That(action1.bindings[0].groups, Is.EqualTo(groupName));
        Assert.That(action1.bindings[0].interactions, Is.EqualTo(interactionsName));
        Assert.That(action1.bindings[0].name, Is.EqualTo(name));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddCompositeBinding()
    {
        var map = new InputActionMap("set");
        map.AddAction(name: "action1");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);
        var action1Property = mapProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AddCompositeBinding(action1Property, mapProperty, "Axis", typeof(AxisComposite));
        obj.ApplyModifiedPropertiesWithoutUndo();

        var action1 = asset.actionMaps[0].TryGetAction("action1");
        Assert.That(action1.bindings, Has.Count.EqualTo(3));
        Assert.That(action1.bindings[0].path, Is.EqualTo("Axis"));
        Assert.That(action1.bindings, Has.Exactly(1).Matches((InputBinding x) => x.name == "positive"));
        Assert.That(action1.bindings, Has.Exactly(1).Matches((InputBinding x) => x.name == "negative"));
        Assert.That(action1.bindings[0].isComposite, Is.True);
        Assert.That(action1.bindings[0].isPartOfComposite, Is.False);
        Assert.That(action1.bindings[1].isComposite, Is.False);
        Assert.That(action1.bindings[1].isPartOfComposite, Is.True);
        Assert.That(action1.bindings[2].isComposite, Is.False);
        Assert.That(action1.bindings[2].isPartOfComposite, Is.True);
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanGenerateCodeWrapperForInputAsset()
    {
        var set1 = new InputActionMap("set1");
        set1.AddAction(name: "action1", binding: "/gamepad/leftStick");
        set1.AddAction(name: "action2", binding: "/gamepad/rightStick");
        var set2 = new InputActionMap("set2");
        set2.AddAction(name: "action1", binding: "/gamepad/buttonSouth");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(set1);
        asset.AddActionMap(set2);
        asset.name = "MyControls";

        var code = InputActionCodeGenerator.GenerateWrapperCode(asset,
            new InputActionCodeGenerator.Options {namespaceName = "MyNamespace", sourceAssetPath = "test"});

        // Our version of Mono doesn't implement the CodeDom stuff so all we can do here
        // is just perform some textual verification. Once we have the newest Mono, this should
        // use CSharpCodeProvider and at least parse if not compile and run the generated wrapper.

        Assert.That(code, Contains.Substring("namespace MyNamespace"));
        Assert.That(code, Contains.Substring("public class MyControls"));
        Assert.That(code, Contains.Substring("public InputActionMap Clone()"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanGenerateCodeWrapperForInputAsset_WhenAssetNameContainsSpacesAndSymbols()
    {
        var set1 = new InputActionMap("set1");
        set1.AddAction(name: "action ^&", binding: "/gamepad/leftStick");
        set1.AddAction(name: "1thing", binding: "/gamepad/leftStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(set1);
        asset.name = "New Controls (4)";

        var code = InputActionCodeGenerator.GenerateWrapperCode(asset,
            new InputActionCodeGenerator.Options {sourceAssetPath = "test"});

        Assert.That(code, Contains.Substring("class NewControls4"));
        Assert.That(code, Contains.Substring("public InputAction @action"));
        Assert.That(code, Contains.Substring("public InputAction @_1thing"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanRenameAction()
    {
        var map = new InputActionMap("set1");
        map.AddAction(name: "action", binding: "<Gamepad>/leftStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);
        var action1Property = mapProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.RenameAction(action1Property, mapProperty, "newAction");
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(map.actions[0].name, Is.EqualTo("newAction"));
        Assert.That(map.actions[0].bindings, Has.Count.EqualTo(1));
        Assert.That(map.actions[0].bindings[0].action, Is.EqualTo("newAction"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_RenamingAction_WillAutomaticallyEnsureUniqueNames()
    {
        var map = new InputActionMap("set1");
        map.AddAction("actionA", binding: "<Gamepad>/leftStick");
        map.AddAction("actionB");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);
        var action1Property = mapProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.RenameAction(action1Property, mapProperty, "actionB");
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(map.actions[1].name, Is.EqualTo("actionB"));
        Assert.That(map.actions[0].name, Is.EqualTo("actionB1"));
        Assert.That(map.actions[0].bindings, Has.Count.EqualTo(1));
        Assert.That(map.actions[0].bindings[0].action, Is.EqualTo("actionB1"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanRenameActionMap()
    {
        var map = new InputActionMap("oldName");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.RenameActionMap(mapProperty, "newName");
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(map.name, Is.EqualTo("newName"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_RenamingActionMap_WillAutomaticallyEnsureUniqueNames()
    {
        var map1 = new InputActionMap("mapA");
        var map2 = new InputActionMap("mapB");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map1);
        asset.AddActionMap(map2);

        var obj = new SerializedObject(asset);
        var map1Property = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.RenameActionMap(map1Property, "mapB");
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(map1.name, Is.EqualTo("mapB1"));
        Assert.That(map2.name, Is.EqualTo("mapB"));
    }

    // We don't want the game code's update mask affect editor code and vice versa.
    [Test]
    [Category("Editor")]
    public void Editor_UpdateMaskResetsWhenEnteringAndExitingPlayMode()
    {
        InputSystem.updateMask = InputUpdateType.Dynamic;

        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingEditMode);
        InputSystem.OnPlayModeChange(PlayModeStateChange.EnteredPlayMode);

        Assert.That(InputSystem.updateMask, Is.EqualTo(InputUpdateType.Default));

        InputSystem.updateMask = InputUpdateType.Dynamic;

        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingPlayMode);
        InputSystem.OnPlayModeChange(PlayModeStateChange.EnteredEditMode);

        Assert.That(InputSystem.updateMask, Is.EqualTo(InputUpdateType.Default));
    }

    [Test]
    [Category("Editor")]
    public void Editor_UpdateMaskResetsWhenEnteringAndExitingPlayMode_ButPreservesBeforeRenderState()
    {
        InputSystem.updateMask = InputUpdateType.Dynamic | InputUpdateType.BeforeRender;

        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingEditMode);
        InputSystem.OnPlayModeChange(PlayModeStateChange.EnteredPlayMode);

        Assert.That(InputSystem.updateMask, Is.EqualTo(InputUpdateType.Default | InputUpdateType.BeforeRender));

        InputSystem.updateMask = InputUpdateType.Dynamic | InputUpdateType.BeforeRender;

        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingPlayMode);
        InputSystem.OnPlayModeChange(PlayModeStateChange.EnteredEditMode);

        Assert.That(InputSystem.updateMask, Is.EqualTo(InputUpdateType.Default | InputUpdateType.BeforeRender));
    }

    [Test]
    [Category("Editor")]
    public void Editor_AlwaysKeepsEditorUpdatesEnabled()
    {
        InputSystem.updateMask = InputUpdateType.Dynamic;

        Assert.That(InputSystem.updateMask & InputUpdateType.Editor, Is.EqualTo(InputUpdateType.Editor));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanListDeviceMatchersForLayout()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout""
            }
        ";

        InputSystem.RegisterLayout(json);

        InputSystem.RegisterLayoutMatcher("TestLayout", new InputDeviceMatcher().WithProduct("A"));
        InputSystem.RegisterLayoutMatcher("TestLayout", new InputDeviceMatcher().WithProduct("B"));

        var matchers = EditorInputControlLayoutCache.GetDeviceMatchers("TestLayout").ToList();

        Assert.That(matchers, Has.Count.EqualTo(2));
        Assert.That(matchers[0], Is.EqualTo(new InputDeviceMatcher().WithProduct("A")));
        Assert.That(matchers[1], Is.EqualTo(new InputDeviceMatcher().WithProduct("B")));
    }

    private class TestEditorWindow : EditorWindow
    {
        public Vector2 mousePosition;

        public void OnGUI()
        {
            mousePosition = Mouse.current.position.ReadValue();
        }
    }

    [Test]
    [Category("Editor")]
    [Ignore("TODO")]
    public void TODO_Editor_PointerCoordinatesInEditorWindowOnGUI_AreInEditorWindowSpace()
    {
        Assert.Fail();
    }

    ////TODO: the following tests have to be edit mode tests but it looks like putting them into
    ////      Assembly-CSharp-Editor is the only way to mark them as such

    ////REVIEW: support actions in the editor at all?
    [UnityTest]
    [Category("Editor")]
    [Ignore("TODO")]
    public IEnumerator TODO_Editor_ActionSetUpInEditor_DoesNotTriggerInPlayMode()
    {
        throw new NotImplementedException();
    }

    [UnityTest]
    [Category("Editor")]
    [Ignore("TODO")]
    public IEnumerator TODO_Editor_PlayerActionDoesNotTriggerWhenGameViewIsNotFocused()
    {
        throw new NotImplementedException();
    }

    ////TODO: tests for InputAssetImporter; for this we need C# mocks to be able to cut us off from the actual asset DB
}
#endif // UNITY_EDITOR
