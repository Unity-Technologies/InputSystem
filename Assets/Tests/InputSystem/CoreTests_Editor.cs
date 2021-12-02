#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Scripting;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;

#pragma warning disable CS0649
partial class CoreTests
{
    [Serializable]
    internal struct PackageJson
    {
        public string version;

        public static Version ReadVersion()
        {
            var packageJsonFile = File.ReadAllText("Packages/com.unity.inputsystem/package.json");
            var packageJson = JsonUtility.FromJson<PackageJson>(packageJsonFile);

            // Snip -preview off the end. System.Version doesn't support semantic versioning.
            var versionString = packageJson.version;
            if (versionString.Contains("-preview"))
                versionString = versionString.Substring(0, versionString.IndexOf("-preview"));
            else if (versionString.Contains("-pre"))
                versionString = versionString.Substring(0, versionString.IndexOf("-pre"));
            return new Version(versionString);
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_PackageVersionAndAssemblyVersionAreTheSame()
    {
        var version = PackageJson.ReadVersion();

        Assert.That(InputSystem.version.Major, Is.EqualTo(version.Major));
        Assert.That(InputSystem.version.Minor, Is.EqualTo(version.Minor));
        Assert.That(InputSystem.version.Build, Is.EqualTo(version.Build));
    }

    [Test]
    [Category("Editor")]
    public void Editor_HelpUrlsPointToCurrentVersion()
    {
        var version = PackageJson.ReadVersion();

        Assert.That(InputSystem.kDocUrl, Does.Contain($"@{version.Major}.{version.Minor}"));
    }

    // upm-ci has this as a warning; turn it into an error in our CI.
    [Test]
    [Category("Editor")]
    public void Editor_ChangelogDatesConformToISO8601()
    {
        var changelog = File.ReadAllLines("Packages/com.unity.inputsystem/CHANGELOG.md");
        var regex = new Regex(@"^##\s+\[.*\]\s+-\s+(?<date>[0-9\-]+)");
        var dateRegex = new Regex(@"(?<year>[0-9][0-9][0-9][0-9])-(?<month>[0-9][0-9])-(?<day>[0-9][0-9])");

        DateTime? lastDate = null;
        foreach (var line in changelog)
        {
            var match = regex.Match(line);
            if (!match.Success)
                continue;

            var date = match.Groups["date"].Value;
            var dateMatch = dateRegex.Match(date);
            Assert.That(dateMatch.Success, Is.True, $"'{date}' in '{line}' is not in ISO 8601 format");

            Assert.That(int.Parse(dateMatch.Groups["year"].Value), Is.GreaterThanOrEqualTo(2018));
            Assert.That(int.Parse(dateMatch.Groups["month"].Value), Is.GreaterThanOrEqualTo(1).And.LessThanOrEqualTo(12));
            Assert.That(int.Parse(dateMatch.Groups["day"].Value), Is.GreaterThanOrEqualTo(1).And.LessThanOrEqualTo(31));

            // Also ensure dates are ordered.
            var dateTime = DateTime.ParseExact(date, "yyyy-MM-dd", null);
            if (lastDate != null)
                Assert.That(lastDate.Value, Is.GreaterThan(dateTime));

            lastDate = dateTime;
        }

        Assert.That(lastDate, Is.Not.Null, "Could not find any changelog dates in the changelog file");
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
        runtime.ReportNewInputDevice(new InputDeviceDescription
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

        runtime.ReportNewInputDevice(
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

        SimulateDomainReload();

        var newDevice = InputSystem.devices[0];

        Assert.That(newDevice.usages, Has.Count.EqualTo(1));
        Assert.That(newDevice.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
    }

    // We have code that will automatically query the enabled state of devices on creation
    // but if the IOCTL is not implemented, we still need to be able to maintain a device's
    // enabled state.
    [Test]
    [Category("Editor")]
    public void Editor_DomainReload_PreservesEnabledState()
    {
        var device = InputSystem.AddDevice<Gamepad>();
        InputSystem.DisableDevice(device);

        Assert.That(device.enabled, Is.False);

        SimulateDomainReload();

        var newDevice = InputSystem.devices[0];

        Assert.That(newDevice.enabled, Is.False);
    }

    [Test]
    [Category("Editor")]
    public void Editor_DomainReload_InputSystemInitializationCausesDevicesToBeRecreated()
    {
        InputSystem.AddDevice<Gamepad>();

        SimulateDomainReload();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices[0], Is.TypeOf<Gamepad>());
    }

    // https://fogbugz.unity3d.com/f/cases/1192379/
    [Test]
    [Category("Editor")]
    public void Editor_DomainReload_CustomDevicesAreRestoredAsLayoutsBecomeAvailable()
    {
        ////REVIEW: Consider switching away from explicit registration and switch to implicit discovery
        ////        through reflection. Explicit registration has proven surprisingly fickle and puts the
        ////        burden squarely on users.

        // We may have several [InitializeOnLoad] classes each registering a piece of data
        // with the input system. The first [InitializeOnLoad] code that gets picked by the
        // Unity runtime is the one that will trigger initialization of the input system.
        //
        // However, if we have a later one in the sequence registering a device layout, we
        // cannot successfully recreate devices using that layout until that code has executed,
        // too.
        //
        // What we do to solve this is to keep information on devices that we fail to restore
        // after a domain around until the very first full input update. At that point, we
        // warn about every

        const string kLayout = @"
            {
                ""name"" : ""CustomDevice"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterLayout(kLayout);
        InputSystem.AddDevice("CustomDevice");

        SimulateDomainReload();

        Assert.That(InputSystem.devices, Is.Empty);

        InputSystem.RegisterLayout(kLayout);

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices[0].layout, Is.EqualTo("CustomDevice"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_DomainReload_RetainsUnsupportedDevices()
    {
        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            interfaceName = "SomethingUnknown",
            product = "UnknownProduct"
        });
        InputSystem.Update();

        SimulateDomainReload();

        Assert.That(InputSystem.GetUnsupportedDevices(), Has.Count.EqualTo(1));
        Assert.That(InputSystem.GetUnsupportedDevices()[0].interfaceName, Is.EqualTo("SomethingUnknown"));
        Assert.That(InputSystem.GetUnsupportedDevices()[0].product, Is.EqualTo("UnknownProduct"));
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
    public void Editor_RestoringStateWillCleanUpEventHooks()
    {
        InputSystem.SaveAndReset();

        var receivedOnEvent = 0;
        var receivedOnDeviceChange = 0;

        InputSystem.onEvent += (e, d) => ++ receivedOnEvent;
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

    [Test]
    [Category("Editor")]
    public void Editor_WhenPlaying_EditorUpdatesKeepSeparateStateFromPlayerUpdates()
    {
        InputSystem.settings.editorInputBehaviorInPlayMode = default;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.25f});
        InputSystem.Update(InputUpdateType.Dynamic);
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.25).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadValueFromPreviousFrame(), Is.Zero.Within(0.000001));

        // Piping input into an editor update now should not result in it being consumed
        // as the game is running and has focus. We should see the blank state the editor
        // started with.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.75f});
        InputSystem.Update(InputUpdateType.Editor);
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.Zero.Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadValueFromPreviousFrame(), Is.Zero.Within(0.000001));

        // So running a player update now should make the input come through in player state.
        InputSystem.Update(InputUpdateType.Dynamic);
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.75).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadValueFromPreviousFrame(), Is.EqualTo(0.25).Within(0.000001));
    }

    [Test]
    [Category("Editor")]
    // Case 1368559
    // Case 1367556
    // Case 1372830
    public void Editor_WhenPlaying_ItsPossibleToQueryPlayerStateAfterEditorUpdate()
    {
        InputSystem.settings.editorInputBehaviorInPlayMode = default;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        // ----------------- Engine frame 1

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.25f});
        InputSystem.Update(InputUpdateType.Dynamic);
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.25).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadValueFromPreviousFrame(), Is.Zero.Within(0.000001));

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.75f});
        InputSystem.Update(InputUpdateType.Editor);
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.Zero.Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadValueFromPreviousFrame(), Is.Zero.Within(0.000001));

        // ----------------- Engine frame 2

        // Simulate early player loop callback
        runtime.onPlayerLoopInitialization();

        // This code might be running in EarlyUpdate or FixedUpdate, _before_ Dynamic update is invoked.
        // We should read values from last player update, meaning we report values from last frame.
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.25).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadValueFromPreviousFrame(), Is.Zero.Within(0.000001));

        // Running a player update now should make the input come through in player state.
        InputSystem.Update(InputUpdateType.Dynamic);
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.75).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadValueFromPreviousFrame(), Is.EqualTo(0.25).Within(0.000001));
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

        InputActionSerializationHelpers.DeleteActionMap(obj, asset.actionMaps[0].id);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps, Has.Count.EqualTo(1));
        Assert.That(asset.actionMaps[0].name, Is.EqualTo(actionMap2Name));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveElementThroughSerialization()
    {
        var map = new InputActionMap("map");
        var action1 = map.AddAction(name: "action1", binding: "<Gamepad>/leftStick");
        var action2 = map.AddAction(name: "action2", binding: "<Gamepad>/rightStick");
        action2.AddBinding("<Gamepad>/dpad");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var mapId = map.id;
        var action1Id = action1.id;
        var action2Id = action2.id;
        var binding1Id = map.bindings[0].id;
        var binding2Id = map.bindings[1].id;
        var binding3Id = map.bindings[2].id;

        var obj = new SerializedObject(asset);

        var maps = obj.FindProperty("m_ActionMaps");
        InputActionSerializationHelpers.AddElement(maps, "new map", 0);

        var actions = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(1).FindPropertyRelative("m_Actions");
        var bindings = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(1).FindPropertyRelative("m_Bindings");
        InputActionSerializationHelpers.AddElement(actions, "new action", 1);
        InputActionSerializationHelpers.AddElement(bindings, "new binding", 1);

        obj.ApplyModifiedPropertiesWithoutUndo();

        // By the nature of Unity serialization, only the connection to UnityEngine.Objects is maintained
        // for C# objects. So map, action1, and action2 are all no longer the objects inside the asset.
        map = asset.actionMaps[1];

        Assert.That(asset.actionMaps.Count, Is.EqualTo(2));
        Assert.That(asset.actionMaps[0].name, Is.EqualTo("new map"));
        Assert.That(asset.actionMaps[1].name, Is.EqualTo("map"));
        Assert.That(asset.actionMaps[0].id, Is.Not.EqualTo(mapId));
        Assert.That(asset.actionMaps[1].id, Is.EqualTo(mapId));

        Assert.That(map.actions, Has.Count.EqualTo(3));
        Assert.That(map.actions[0].name, Is.EqualTo("action1"));
        Assert.That(map.actions[1].name, Is.EqualTo("new action"));
        Assert.That(map.actions[2].name, Is.EqualTo("action2"));
        Assert.That(map.actions[0].id, Is.EqualTo(action1Id));
        Assert.That(map.actions[1].id, Is.Not.EqualTo(action1Id));
        Assert.That(map.actions[1].id, Is.Not.EqualTo(action2Id));
        Assert.That(map.actions[2].id, Is.EqualTo(action2Id));
        Assert.That(map.actions[0].bindings, Has.Count.EqualTo(1));
        Assert.That(map.actions[1].bindings, Has.Count.Zero);
        Assert.That(map.actions[2].bindings, Has.Count.EqualTo(2));
        Assert.That(map.actions[0].bindings[0].path, Is.EqualTo("<Gamepad>/leftStick"));
        Assert.That(map.actions[2].bindings[0].path, Is.EqualTo("<Gamepad>/rightStick"));
        Assert.That(map.actions[2].bindings[1].path, Is.EqualTo("<Gamepad>/dpad"));

        Assert.That(map.bindings, Has.Count.EqualTo(4));
        Assert.That(map.bindings[0].id, Is.EqualTo(binding1Id));
        Assert.That(map.bindings[1].id, Is.Not.EqualTo(binding1Id));
        Assert.That(map.bindings[1].id, Is.Not.EqualTo(binding2Id));
        Assert.That(map.bindings[1].id, Is.Not.EqualTo(binding3Id));
        Assert.That(map.bindings[2].id, Is.EqualTo(binding2Id));
        Assert.That(map.bindings[3].id, Is.EqualTo(binding3Id));
        Assert.That(map.bindings[0].name, Is.Not.EqualTo("new binding"));
        Assert.That(map.bindings[1].name, Is.EqualTo("new binding"));
        Assert.That(map.bindings[2].name, Is.Not.EqualTo("new binding"));
        Assert.That(map.bindings[3].name, Is.Not.EqualTo("new binding"));
        Assert.That(map.bindings[0].path, Is.EqualTo("<Gamepad>/leftStick"));
        Assert.That(map.bindings[1].path, Is.Empty);
        Assert.That(map.bindings[2].path, Is.EqualTo("<Gamepad>/rightStick"));
        Assert.That(map.bindings[3].path, Is.EqualTo("<Gamepad>/dpad"));
        Assert.That(map.bindings[0].action, Is.EqualTo("action1"));
        Assert.That(map.bindings[1].action, Is.Empty);
        Assert.That(map.bindings[2].action, Is.EqualTo("action2"));
        Assert.That(map.bindings[3].action, Is.EqualTo("action2"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveActionThroughSerialization()
    {
        var map = new InputActionMap("set");
        map.AddAction(name: "action1", binding: "/gamepad/leftStick");
        var action2 = map.AddAction(name: "action2", binding: "/gamepad/rightStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AddAction(mapProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps[0].actions, Has.Count.EqualTo(3));
        Assert.That(asset.actionMaps[0].actions[2].name, Is.EqualTo("New action"));
        Assert.That(asset.actionMaps[0].actions[2].type, Is.EqualTo(InputActionType.Button));
        Assert.That(asset.actionMaps[0].actions[2].expectedControlType, Is.EqualTo("Button"));
        Assert.That(asset.actionMaps[0].actions[2].m_Id, Is.Not.Empty);
        Assert.That(asset.actionMaps[0].actions[2].bindings, Has.Count.Zero);

        InputActionSerializationHelpers.DeleteActionAndBindings(mapProperty, action2.id);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps[0].actions, Has.Count.EqualTo(2));
        Assert.That(asset.actionMaps[0].actions[0].name, Is.EqualTo("action1"));
        Assert.That(asset.actionMaps[0].actions[1].name, Is.EqualTo("New action"));
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
        var action1 = asset.actionMaps[0].FindAction("action1");
        var action2 = asset.actionMaps[0].FindAction("action2");

        Assert.That(action1.bindings, Has.Count.EqualTo(2));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action1.bindings[1].path, Is.EqualTo(""));
        Assert.That(action1.bindings[1].interactions, Is.EqualTo(""));
        Assert.That(action1.bindings[1].groups, Is.EqualTo(""));
        Assert.That(action1.bindings[1].m_Id, Is.Not.Null);
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));

        InputActionSerializationHelpers.DeleteBinding(mapProperty.FindPropertyRelative("m_Bindings"),
            action1.bindings[1].id);
        obj.ApplyModifiedPropertiesWithoutUndo();

        action1 = asset.actionMaps[0].FindAction("action1");
        action2 = asset.actionMaps[0].FindAction("action2");

        Assert.That(action1.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddCompositeBindingThroughSerialization()
    {
        var map = new InputActionMap("map");
        map.AddAction("action1");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);
        var action1Property = mapProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AddCompositeBinding(action1Property, mapProperty, "Axis", typeof(AxisComposite));
        obj.ApplyModifiedPropertiesWithoutUndo();

        var action1 = asset.actionMaps[0].FindAction("action1");
        Assert.That(action1.bindings, Has.Count.EqualTo(3));
        Assert.That(action1.bindings[0].path, Is.EqualTo("Axis"));
        Assert.That(action1.bindings, Has.Exactly(1).Matches((InputBinding x) =>
            string.Equals(x.name, "positive", StringComparison.InvariantCultureIgnoreCase)));
        Assert.That(action1.bindings, Has.Exactly(1).Matches((InputBinding x) =>
            string.Equals(x.name, "negative", StringComparison.InvariantCultureIgnoreCase)));
        Assert.That(action1.bindings[0].isComposite, Is.True);
        Assert.That(action1.bindings[0].isPartOfComposite, Is.False);
        Assert.That(action1.bindings[1].isComposite, Is.False);
        Assert.That(action1.bindings[1].isPartOfComposite, Is.True);
        Assert.That(action1.bindings[2].isComposite, Is.False);
        Assert.That(action1.bindings[2].isPartOfComposite, Is.True);
        Assert.That(action1.bindings[0].m_Id, Is.Not.Null.And.Not.Empty);
        Assert.That(action1.bindings[1].m_Id, Is.Not.Null.And.Not.Empty);
        Assert.That(action1.bindings[2].m_Id, Is.Not.Null.And.Not.Empty);
        Assert.That(action1.bindings[0].m_Id, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanChangeCompositeType()
    {
        var map = new InputActionMap("map");
        map.AddAction(name: "action1");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);
        var action1Property = mapProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        // Add an axis composite with a positive and negative binding in place.
        var composite = InputActionSerializationHelpers.AddCompositeBinding(action1Property, mapProperty, "Axis",
            addPartBindings: false);
        InputActionSerializationHelpers.AddBinding(action1Property, mapProperty, path: "<Gamepad>/buttonWest",
            name: "Negative", processors: "normalize", interactions: "tap", flags: InputBinding.Flags.PartOfComposite);
        InputActionSerializationHelpers.AddBinding(action1Property, mapProperty, path: "<Gamepad>/buttonEast",
            name: "Positive", processors: "clamp", interactions: "slowtap", flags: InputBinding.Flags.PartOfComposite);

        // Noise.
        InputActionSerializationHelpers.AddBinding(action1Property, mapProperty, path: "foobar");

        // Change to vector2 composite and make sure that we've added two more bindings, changed the names
        // of bindings accordingly, and preserved the existing binding paths and such.
        InputActionSerializationHelpers.ChangeCompositeBindingType(composite,
            NameAndParameters.Parse("Dpad(normalize=false)"));
        obj.ApplyModifiedPropertiesWithoutUndo();

        var action1 = asset.actionMaps[0].FindAction("action1");
        Assert.That(action1.bindings, Has.Count.EqualTo(6)); // Composite + 4 parts + noise added above.
        Assert.That(action1.bindings[0].path, Is.EqualTo("Dpad(normalize=false)"));
        Assert.That(action1.bindings, Has.None.Matches((InputBinding x) =>
            string.Equals(x.name, "positive", StringComparison.InvariantCultureIgnoreCase)));
        Assert.That(action1.bindings, Has.None.Matches((InputBinding x) =>
            string.Equals(x.name, "negative", StringComparison.InvariantCultureIgnoreCase)));
        Assert.That(action1.bindings, Has.Exactly(1).Matches((InputBinding x) =>
            string.Equals(x.name, "up", StringComparison.InvariantCultureIgnoreCase)));
        Assert.That(action1.bindings, Has.Exactly(1).Matches((InputBinding x) =>
            string.Equals(x.name, "down", StringComparison.InvariantCultureIgnoreCase)));
        Assert.That(action1.bindings, Has.Exactly(1).Matches((InputBinding x) =>
            string.Equals(x.name, "left", StringComparison.InvariantCultureIgnoreCase)));
        Assert.That(action1.bindings, Has.Exactly(1).Matches((InputBinding x) =>
            string.Equals(x.name, "right", StringComparison.InvariantCultureIgnoreCase)));
        Assert.That(action1.bindings[0].isComposite, Is.True);
        Assert.That(action1.bindings[0].isPartOfComposite, Is.False);
        Assert.That(action1.bindings[1].isComposite, Is.False);
        Assert.That(action1.bindings[1].isPartOfComposite, Is.True);
        Assert.That(action1.bindings[2].isComposite, Is.False);
        Assert.That(action1.bindings[2].isPartOfComposite, Is.True);
        Assert.That(action1.bindings[3].isComposite, Is.False);
        Assert.That(action1.bindings[3].isPartOfComposite, Is.True);
        Assert.That(action1.bindings[4].isComposite, Is.False);
        Assert.That(action1.bindings[4].isPartOfComposite, Is.True);
        Assert.That(action1.bindings[1].path, Is.EqualTo("<Gamepad>/buttonWest"));
        Assert.That(action1.bindings[2].path, Is.EqualTo("<Gamepad>/buttonEast"));
        Assert.That(action1.bindings[1].interactions, Is.EqualTo("tap"));
        Assert.That(action1.bindings[2].interactions, Is.EqualTo("slowtap"));
        Assert.That(action1.bindings[1].processors, Is.EqualTo("normalize"));
        Assert.That(action1.bindings[2].processors, Is.EqualTo("clamp"));
        Assert.That(action1.bindings[3].path, Is.Empty);
        Assert.That(action1.bindings[4].path, Is.Empty);
        Assert.That(action1.bindings[3].interactions, Is.Empty);
        Assert.That(action1.bindings[4].interactions, Is.Empty);
        Assert.That(action1.bindings[3].processors, Is.Empty);
        Assert.That(action1.bindings[4].processors, Is.Empty);
        Assert.That(action1.bindings[5].path, Is.EqualTo("foobar"));
        Assert.That(action1.bindings[5].name, Is.Empty);
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanReplaceBindingGroupThroughSerialization()
    {
        var map = new InputActionMap("map");
        var action = map.AddAction(name: "action1");
        action.AddBinding("Foo", groups: "A");
        action.AddBinding("Bar", groups: "B");
        action.AddBinding("Flub", groups: "A;B");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        InputActionSerializationHelpers.ReplaceBindingGroup(obj, "A", "C");
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(action.bindings[0].groups, Is.EqualTo("C"));
        Assert.That(action.bindings[1].groups, Is.EqualTo("B"));
        Assert.That(action.bindings[2].groups, Is.EqualTo("C;B"));

        InputActionSerializationHelpers.ReplaceBindingGroup(obj, "C", "");
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(action.bindings[0].groups, Is.EqualTo(""));
        Assert.That(action.bindings[1].groups, Is.EqualTo("B"));
        Assert.That(action.bindings[2].groups, Is.EqualTo("B"));

        InputActionSerializationHelpers.ReplaceBindingGroup(obj, "B", "", deleteOrphanedBindings: true);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(map.bindings, Has.Count.EqualTo(1));
        Assert.That(map.bindings[0].groups, Is.EqualTo(""));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputActionAssetManager_CanMoveAssetOnDisk()
    {
        const string kAssetPath = "Assets/DirectoryBeforeRename/InputAsset." + InputActionAsset.Extension;
        const string kAssetPathAfterMove = "Assets/DirectoryAfterRename/InputAsset." + InputActionAsset.Extension;
        const string kDefaultContents = "{}";

        AssetDatabase.CreateFolder("Assets", "DirectoryBeforeRename");
        File.WriteAllText(kAssetPath, kDefaultContents);
        AssetDatabase.ImportAsset(kAssetPath);

        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(kAssetPath);
        Assert.NotNull(asset, "Could not load asset: " + kAssetPath);

        var inputActionAssetManager = new InputActionAssetManager(asset);
        inputActionAssetManager.Initialize();
        inputActionAssetManager.onDirtyChanged = (bool dirty) => {};

        FileUtil.MoveFileOrDirectory("Assets/DirectoryBeforeRename", "Assets/DirectoryAfterRename");
        AssetDatabase.Refresh();

        Assert.DoesNotThrow(() => inputActionAssetManager.SaveChangesToAsset());

        var fileContents = File.ReadAllText(kAssetPathAfterMove);
        Assert.AreNotEqual(kDefaultContents, fileContents, "Expected file contents to have been modified after SaveChangesToAsset was called.");

        AssetDatabase.DeleteAsset("Assets/DirectoryAfterRename");
    }

    private class MonoBehaviourWithEmbeddedAction : MonoBehaviour
    {
        public InputAction action;
    }

    private class MonoBehaviourWithEmbeddedActionMap : MonoBehaviour
    {
        public InputActionMap actionMap;
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanShowBindingsFromEmbeddedActions()
    {
        var go = new GameObject();
        var component = go.AddComponent<MonoBehaviourWithEmbeddedAction>();
        component.action = new InputAction("action");
        component.action.AddBinding("<Gamepad>/buttonSouth");
        component.action.AddBinding("<Gamepad>/buttonNorth");

        var so = new SerializedObject(component);
        var actionProperty = so.FindProperty("action");

        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildWithJustBindingsFromAction(actionProperty)
        };
        tree.Reload();

        Assert.That(tree.rootItem, Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children, Has.Count.EqualTo(2));
        Assert.That(tree.rootItem.children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree.rootItem.children[1], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree.rootItem.children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/buttonSouth"));
        Assert.That(tree.rootItem.children[1].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/buttonNorth"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanShowActionsAndBindingsFromEmbeddedActionMap()
    {
        var go = new GameObject();
        var component = go.AddComponent<MonoBehaviourWithEmbeddedActionMap>();
        component.actionMap = new InputActionMap("map");
        var action1 = component.actionMap.AddAction("action1");
        var action2 = component.actionMap.AddAction("action2");
        action1.AddBinding("<Gamepad>/buttonSouth");
        action2.AddBinding("<Gamepad>/buttonNorth");

        var so = new SerializedObject(component);
        var actionMapProperty = so.FindProperty("actionMap");

        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildWithJustActionsAndBindingsFromMap(actionMapProperty)
        };
        tree.Reload();

        Assert.That(tree.rootItem, Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children, Has.Count.EqualTo(2));
        Assert.That(tree.rootItem.children[0], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children[1], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children[0].As<ActionTreeItem>().displayName, Is.EqualTo("action1"));
        Assert.That(tree.rootItem.children[1].As<ActionTreeItem>().displayName, Is.EqualTo("action2"));
        Assert.That(tree.rootItem.children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[1].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree.rootItem.children[1].children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree.rootItem.children[0].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/buttonSouth"));
        Assert.That(tree.rootItem.children[1].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/buttonNorth"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanShowJustActionMapsFromAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map1");
        var map2 = asset.AddActionMap("map2");
        map1.AddAction("action1");
        map2.AddAction("action2");

        var so = new SerializedObject(asset);

        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildWithJustActionMapsFromAsset(so)
        };
        tree.Reload();

        Assert.That(tree.rootItem, Is.TypeOf<InputActionTreeView.ActionMapListItem>());
        Assert.That(tree.rootItem.children, Has.Count.EqualTo(2));
        Assert.That(tree.rootItem.children[0], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children[1], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children[0].As<ActionMapTreeItem>().displayName, Is.EqualTo("map1"));
        Assert.That(tree.rootItem.children[1].As<ActionMapTreeItem>().displayName, Is.EqualTo("map2"));
        Assert.That(tree.rootItem.children[0].As<ActionMapTreeItem>().property.propertyPath, Is.EqualTo("m_ActionMaps.Array.data[0]"));
        Assert.That(tree.rootItem.children[1].As<ActionMapTreeItem>().property.propertyPath, Is.EqualTo("m_ActionMaps.Array.data[1]"));
        Assert.That(tree.rootItem.children[0].children, Is.Null);
        Assert.That(tree.rootItem.children[1].children, Is.Null);
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanShowActionsAndBindingsFromActionMapInAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map1");
        var map2 = asset.AddActionMap("map2");
        var action1 = map1.AddAction("action1");
        var action2 = map1.AddAction("action2");
        var action3 = map2.AddAction("action3");
        var action4 = map2.AddAction("action4");
        action1.AddBinding("<Gamepad>/leftStick");
        action1.AddBinding("<Keyboard>/a");
        action2.AddBinding("<Gamepad>/rightStick");
        action3.AddBinding("<Gamepad>/buttonSouth");
        action4.AddBinding("<Gamepad>/buttonNorth");

        var so = new SerializedObject(asset);
        var actionMapArrayProperty = so.FindProperty("m_ActionMaps");
        var actionMapProperty = actionMapArrayProperty.GetArrayElementAtIndex(1);

        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildWithJustActionsAndBindingsFromMap(actionMapProperty)
        };
        tree.Reload();

        Assert.That(tree.rootItem, Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children, Has.Count.EqualTo(2));
        Assert.That(tree.rootItem.children[0], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children[1], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children[0].As<ActionTreeItem>().displayName, Is.EqualTo("action3"));
        Assert.That(tree.rootItem.children[1].As<ActionTreeItem>().displayName, Is.EqualTo("action4"));
        Assert.That(tree.rootItem.children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[1].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree.rootItem.children[1].children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree.rootItem.children[0].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/buttonSouth"));
        Assert.That(tree.rootItem.children[1].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/buttonNorth"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CompositesAreShownAsSubtrees()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var action = map.AddAction("action");
        action.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/b");

        var so = new SerializedObject(asset);

        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so)
        };
        tree.Reload();

        var actionItem = tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Actions.Array.data[0]");
        Assert.That(actionItem, Is.Not.Null);

        Assert.That(actionItem.children, Is.Not.Null);
        Assert.That(actionItem.children, Has.Count.EqualTo(1));
        Assert.That(actionItem.children[0], Is.TypeOf<CompositeBindingTreeItem>());
        Assert.That(actionItem.children[0].displayName, Is.EqualTo("Axis"));
        Assert.That(actionItem.children[0].children, Is.Not.Null);
        Assert.That(actionItem.children[0].children, Has.Count.EqualTo(2));
        Assert.That(actionItem.children[0].children[0], Is.TypeOf<PartOfCompositeBindingTreeItem>());
        Assert.That(actionItem.children[0].children[1], Is.TypeOf<PartOfCompositeBindingTreeItem>());
        Assert.That(actionItem.children[0].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Keyboard>/a"));
        Assert.That(actionItem.children[0].children[1].As<BindingTreeItem>().path, Is.EqualTo("<Keyboard>/b"));
        Assert.That(actionItem.children[0].children[0].As<BindingTreeItem>().name, Is.EqualTo("Positive"));
        Assert.That(actionItem.children[0].children[1].As<BindingTreeItem>().name, Is.EqualTo("Negative"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanSelectToplevelItemByName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map1");
        var map2 = asset.AddActionMap("map2");
        map1.AddAction("action1");
        map2.AddAction("action2");

        var so = new SerializedObject(asset);

        var selectionChanged = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSelectionChanged = () => selectionChanged = true,
        };
        tree.Reload();

        Assert.That(selectionChanged, Is.False);

        tree.SelectItem("map2");

        Assert.That(selectionChanged, Is.True);
        Assert.That(tree.GetSelectedItems(), Is.EquivalentTo(new[] { tree.rootItem.children[1] }));
        Assert.That(tree.GetSelectedItems().OfType<ActionMapTreeItem>().First().displayName, Is.EqualTo("map2"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanAddActionMap()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map1");
        map1.AddAction("action1");

        using (var so = new SerializedObject(asset))
        {
            var renameItem = (ActionTreeItemBase)null;
            var tree = new InputActionTreeView(so)
            {
                onBuildTree = () => InputActionTreeView.BuildFullTree(so),
                onBeginRename = item =>
                {
                    Assert.That(renameItem, Is.Null);
                    renameItem = item;
                }
            };
            tree.Reload();

            tree.AddNewActionMap();

            Assert.That(tree.rootItem.children, Has.Count.EqualTo(2));
            Assert.That(tree.rootItem.children[1], Is.TypeOf<ActionMapTreeItem>());

            var newActionMapItem = (ActionMapTreeItem)tree.rootItem.children[1];
            Assert.That(newActionMapItem.displayName, Is.EqualTo("New action map"));
            Assert.That(renameItem, Is.SameAs(newActionMapItem));
            Assert.That(tree.GetSelectedItems(), Is.EquivalentTo(new[] { newActionMapItem }));
            Assert.That(tree.IsExpanded(newActionMapItem.id), Is.True);
            Assert.That(newActionMapItem.children, Is.Not.Null);
            Assert.That(newActionMapItem.children, Has.Count.EqualTo(1));
            Assert.That(newActionMapItem.children[0], Is.TypeOf<ActionTreeItem>());
            Assert.That(newActionMapItem.children[0].displayName, Is.EqualTo("New action"));
            Assert.That(tree.IsExpanded(newActionMapItem.children[0].id), Is.True);
            Assert.That(newActionMapItem.children[0].children, Has.Count.EqualTo(1));
            Assert.That(newActionMapItem.children[0].children[0], Is.TypeOf<BindingTreeItem>());
            Assert.That(newActionMapItem.children[0].children[0].As<BindingTreeItem>().path, Is.EqualTo(""));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanAddAction()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map1");
        map.AddAction("action1");

        using (var so = new SerializedObject(asset))
        {
            var renameItem = (ActionTreeItemBase)null;
            var selectionChanged = false;
            var tree = new InputActionTreeView(so)
            {
                onBuildTree = () => InputActionTreeView.BuildFullTree(so),
                onBeginRename = item =>
                {
                    Assert.That(renameItem, Is.Null);
                    renameItem = item;
                },
                onSelectionChanged = () =>
                {
                    Assert.That(selectionChanged, Is.False);
                    selectionChanged = true;
                }
            };
            tree.Reload();
            tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Actions.Array.data[0]"));
            selectionChanged = false;
            tree.AddNewAction();

            Assert.That(tree.rootItem.children[0].children, Has.Count.EqualTo(2));
            Assert.That(tree.rootItem.children[0].children[1], Is.TypeOf<ActionTreeItem>());

            var newActionItem = (ActionTreeItem)tree.rootItem.children[0].children[1];
            Assert.That(newActionItem.displayName, Is.EqualTo("New action"));
            Assert.That(renameItem, Is.SameAs(newActionItem));
            Assert.That(selectionChanged, Is.True);
            Assert.That(tree.GetSelectedItems(), Is.EquivalentTo(new[] {newActionItem}));
            Assert.That(tree.IsExpanded(newActionItem.id), Is.True);
            Assert.That(newActionItem.children, Is.Not.Null);
            Assert.That(newActionItem.children, Has.Count.EqualTo(1));
            Assert.That(newActionItem.children[0], Is.TypeOf<BindingTreeItem>());
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCopyPasteActionMap()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map1");
        var map2 = asset.AddActionMap("map2");
        var action1 = map1.AddAction("action1");
        var action2 = map1.AddAction("action2");
        var action3 = map2.AddAction("action3");
        var action4 = map2.AddAction("action4");
        action1.AddBinding("<Gamepad>/leftStick");
        action1.AddBinding("<Keyboard>/a");
        action2.AddBinding("<Gamepad>/rightStick");
        action3.AddBinding("<Gamepad>/buttonSouth");
        action4.AddBinding("<Gamepad>/buttonNorth");

        var serializedObject = new SerializedObject(asset);
        var tree = new InputActionTreeView(serializedObject)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(serializedObject)
        };

        tree.Reload();
        tree.SelectItem("map1");

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.CopySelectedItemsToClipboard();
            Assert.That(EditorHelpers.GetSystemCopyBufferContents(), Does.StartWith(InputActionTreeView.k_CopyPasteMarker));
            tree.PasteDataFromClipboard();

            Assert.That(tree.rootItem.children, Has.Count.EqualTo(3));
            Assert.That(tree.rootItem.children[1], Is.TypeOf<ActionMapTreeItem>());
            Assert.That(tree.rootItem.children[1].displayName, Is.EqualTo("map3"));
            Assert.That(tree.rootItem.children[1].As<ActionMapTreeItem>().guid, Is.Not.EqualTo(map1.id));
            Assert.That(tree.rootItem.children[1].children, Is.Not.Null);
            Assert.That(tree.rootItem.children[1].children, Has.Count.EqualTo(2));
            Assert.That(tree.rootItem.children[1].children[0], Is.TypeOf<ActionTreeItem>());
            Assert.That(tree.rootItem.children[1].children[1], Is.TypeOf<ActionTreeItem>());
            Assert.That(tree.rootItem.children[1].children[0].displayName, Is.EqualTo("action1"));
            Assert.That(tree.rootItem.children[1].children[1].displayName, Is.EqualTo("action2"));
            Assert.That(tree.rootItem.children[1].children[0].children, Is.Not.Null);
            Assert.That(tree.rootItem.children[1].children[1].children, Is.Not.Null);
            Assert.That(tree.rootItem.children[1].children[0].children, Has.Count.EqualTo(2));
            Assert.That(tree.rootItem.children[1].children[1].children, Has.Count.EqualTo(1));
            Assert.That(tree.rootItem.children[1].children[0].children[0], Is.TypeOf<BindingTreeItem>());
            Assert.That(tree.rootItem.children[1].children[0].children[1], Is.TypeOf<BindingTreeItem>());
            Assert.That(tree.rootItem.children[1].children[1].children[0], Is.TypeOf<BindingTreeItem>());
            Assert.That(tree.rootItem.children[1].children[0].children[0].As<BindingTreeItem>().path,
                Is.EqualTo("<Gamepad>/leftStick"));
            Assert.That(tree.rootItem.children[1].children[0].children[1].As<BindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree.rootItem.children[1].children[1].children[0].As<BindingTreeItem>().path,
                Is.EqualTo("<Gamepad>/rightStick"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCopyPasteAction_IntoSameActionMap()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map1");
        var action1 = map1.AddAction("action1");
        var action2 = map1.AddAction("action2");
        action1.AddBinding("<Gamepad>/leftStick");
        action1.AddBinding("<Keyboard>/a");
        action2.AddBinding("<Gamepad>/rightStick");

        var serializedObject = new SerializedObject(asset);
        var tree = new InputActionTreeView(serializedObject)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(serializedObject),
        };

        tree.Reload();
        tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Actions.Array.data[1]"));

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.CopySelectedItemsToClipboard();
            Assert.That(EditorHelpers.GetSystemCopyBufferContents(), Does.StartWith(InputActionTreeView.k_CopyPasteMarker));
            tree.PasteDataFromClipboard();

            Assert.That(tree.rootItem.children[0].children, Has.Count.EqualTo(3));
            Assert.That(tree.rootItem.children[0].children[2], Is.TypeOf<ActionTreeItem>());
            Assert.That(tree.rootItem.children[0].children[2].displayName, Is.EqualTo("action3"));
            Assert.That(tree.rootItem.children[0].children[2].children, Is.Not.Null);
            Assert.That(tree.rootItem.children[0].children[2].children, Has.Count.EqualTo(1));
            Assert.That(tree.rootItem.children[0].children[2].children[0], Is.TypeOf<BindingTreeItem>());
            Assert.That(tree.rootItem.children[0].children[2].children[0].As<BindingTreeItem>().path,
                Is.EqualTo("<Gamepad>/rightStick"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCopyPasteAction_IntoDifferentActionMap()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map1");
        var map2 = asset.AddActionMap("map2");
        var action1 = map1.AddAction("action1");
        var action2 = map2.AddAction("action2");
        action1.AddBinding("<Gamepad>/leftStick");
        action1.AddBinding("<Keyboard>/a");
        action2.AddBinding("<Gamepad>/rightStick");

        var serializedObject = new SerializedObject(asset);
        var tree = new InputActionTreeView(serializedObject)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(serializedObject),
        };
        tree.Reload();

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Actions.Array.data[0]"));
            tree.CopySelectedItemsToClipboard();
            tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[1].m_Actions.Array.data[0]"));
            tree.PasteDataFromClipboard();

            Assert.That(tree["map1"].children, Has.Count.EqualTo(1));
            Assert.That(tree["map2"].children, Has.Count.EqualTo(2));
            Assert.That(tree["map2"].children[0].displayName, Is.EqualTo("action2"));
            Assert.That(tree["map2"].children[1], Is.TypeOf<ActionTreeItem>());
            Assert.That(tree["map2"].children[1].displayName, Is.EqualTo("action1"));
            Assert.That(tree["map2"].children[1].children, Is.Not.Null);
            Assert.That(tree["map2"].children[1].children, Has.Count.EqualTo(2));
            Assert.That(tree["map2"].children[1].children[0].As<BindingTreeItem>().path,
                Is.EqualTo("<Gamepad>/leftStick"));
            Assert.That(tree.rootItem.children[1].children[1].children[1].As<BindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/a"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCopyPasteBinding_IntoSameAction()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map1");
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");
        action1.AddBinding("<Gamepad>/leftStick");
        action1.AddBinding("<Keyboard>/a");
        action2.AddBinding("<Gamepad>/rightStick");

        var serializedObject = new SerializedObject(asset);
        var tree = new InputActionTreeView(serializedObject)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(serializedObject),
        };
        tree.Reload();

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[0]"));
            tree.CopySelectedItemsToClipboard();
            tree.PasteDataFromClipboard();

            Assert.That(tree["map1/action1"].children, Has.Count.EqualTo(3));
            Assert.That(tree["map1/action2"].children, Has.Count.EqualTo(1));
            Assert.That(tree["map1/action1"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/leftStick"));
            Assert.That(tree["map1/action1"].children[1].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/leftStick"));
            Assert.That(tree["map1/action1"].children[2].As<BindingTreeItem>().path, Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree["map1/action2"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/rightStick"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCopyPasteBinding_IntoDifferentAction()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map1");
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");
        action1.AddBinding("<Gamepad>/leftStick");
        action1.AddBinding("<Keyboard>/a");
        action2.AddBinding("<Gamepad>/rightStick");

        var serializedObject = new SerializedObject(asset);
        var tree = new InputActionTreeView(serializedObject)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(serializedObject),
        };
        tree.Reload();

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[0]"));
            tree.CopySelectedItemsToClipboard();
            tree.SelectItem("map1/action2");
            tree.PasteDataFromClipboard();

            Assert.That(tree["map1/action1"].children, Has.Count.EqualTo(2));
            Assert.That(tree["map1/action2"].children, Has.Count.EqualTo(2));
            Assert.That(tree["map1/action1"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/leftStick"));
            Assert.That(tree["map1/action1"].children[1].As<BindingTreeItem>().path, Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree["map1/action2"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/rightStick"));
            Assert.That(tree["map1/action2"].children[1].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/leftStick"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCopyPasteBinding_IntoDifferentAsset()
    {
        var asset1 = ScriptableObject.CreateInstance<InputActionAsset>();
        asset1.AddControlScheme("Gamepad").WithRequiredDevice<Gamepad>();
        asset1.AddControlScheme("Keyboard").WithRequiredDevice<Keyboard>();

        var map1 = asset1.AddActionMap("map");
        var action1 = map1.AddAction("actionOnlyInFirstAsset");
        var action2 = map1.AddAction("actionInBothAssets");
        action1.AddBinding("<Gamepad>/leftStick", groups: "Gamepad");
        action1.AddBinding("<Keyboard>/a", groups: "Keyboard");
        action2.AddBinding("*/{Back}", groups: "Gamepad;Keyboard");

        var asset2 = ScriptableObject.CreateInstance<InputActionAsset>();
        asset2.AddControlScheme("Gamepad").WithRequiredDevice<Gamepad>();
        asset2.AddControlScheme("Mouse").WithRequiredDevice<Mouse>();

        var map2 = asset2.AddActionMap("map");
        map2.AddAction("actionOnlyInSecondAsset");
        map2.AddAction("actionInBothAssets");

        var serializedObject1 = new SerializedObject(asset1);
        var tree1 = new InputActionTreeView(serializedObject1)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(serializedObject1),
        };
        tree1.Reload();

        var serializedObject2 = new SerializedObject(asset2);
        var tree2 = new InputActionTreeView(serializedObject2)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(serializedObject2),
            onBindingAdded = prop => InputActionSerializationHelpers.RemoveUnusedBindingGroups(prop, asset2.controlSchemes)
        };
        tree2.Reload();

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            // Copy <Gamepad>/leftStick binging from first asset.
            tree1.SelectItem(tree1.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[0]"));
            tree1.CopySelectedItemsToClipboard();

            // Paste it onto actionOnlyInSecondAsset.
            tree2.SelectItem("map/actionOnlyInSecondAsset");
            tree2.PasteDataFromClipboard();

            Assert.That(tree2["map/actionOnlyInSecondAsset"].children, Has.Count.EqualTo(1));
            Assert.That(tree2["map/actionOnlyInSecondAsset"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/leftStick"));
            Assert.That(tree2["map/actionOnlyInSecondAsset"].children[0].As<BindingTreeItem>().groups, Is.EqualTo("Gamepad"));

            // Copy <Keyboard>/a binging from first asset.
            tree1.SelectItem(tree1.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[1]"));
            tree1.CopySelectedItemsToClipboard();

            // Paste it onto actionOnlyInSecondAsset in second asset.
            tree2.SelectItem("map/actionOnlyInSecondAsset");
            tree2.PasteDataFromClipboard();

            Assert.That(tree2["map/actionOnlyInSecondAsset"].children, Has.Count.EqualTo(2));
            Assert.That(tree2["map/actionOnlyInSecondAsset"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/leftStick"));
            Assert.That(tree2["map/actionOnlyInSecondAsset"].children[0].As<BindingTreeItem>().groups, Is.EqualTo("Gamepad"));
            Assert.That(tree2["map/actionOnlyInSecondAsset"].children[1].As<BindingTreeItem>().path, Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree2["map/actionOnlyInSecondAsset"].children[1].As<BindingTreeItem>().groups, Is.EqualTo(""));

            // Copy */{Back} binging from first asset.
            tree1.SelectItem(tree1.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[2]"));
            tree1.CopySelectedItemsToClipboard();

            // Paste it onto actionInBothAssets in second asset.
            // NOTE: Apparently, we don't currently support just pasting it straight onto the map.
            tree2.SelectItem("map/actionInBothAssets");
            tree2.PasteDataFromClipboard();

            Assert.That(tree2["map/actionInBothAssets"].children, Has.Count.EqualTo(1));
            Assert.That(tree2["map/actionInBothAssets"].children[0].As<BindingTreeItem>().path, Is.EqualTo("*/{Back}"));
            Assert.That(tree2["map/actionInBothAssets"].children[0].As<BindingTreeItem>().groups, Is.EqualTo("Gamepad"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CannotCopyPasteBinding_IntoActionMap()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        map.AddAction("action", binding: "<Gamepad>leftStick");

        var so = new SerializedObject(asset);
        var selectionChanged = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSelectionChanged = () =>
            {
                Assert.That(selectionChanged, Is.False);
                selectionChanged = true;
            }
        };
        tree.Reload();

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[0]"));
            selectionChanged = false;
            tree.CopySelectedItemsToClipboard();
            tree.SelectItem("map");
            selectionChanged = false;
            tree.PasteDataFromClipboard();

            Assert.That(selectionChanged, Is.False);
            Assert.That(so.FindProperty("m_ActionMaps.Array.data[0].m_Bindings").arraySize, Is.EqualTo(1));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCopyPasteCompositeBinding_IntoSameAction()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var action = map.AddAction("action");
        action.AddBinding("<Gamepad>/leftStick/x");
        action.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/b");
        action.AddBinding("<Gamepad>/leftStick/y");

        var so = new SerializedObject(asset);
        var selectionChanged = false;
        var serializedObjectModified = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSelectionChanged = () =>
            {
                Assert.That(selectionChanged, Is.False);
                selectionChanged = true;
            },
            onSerializedObjectModified = () =>
            {
                Assert.That(serializedObjectModified, Is.False);
                serializedObjectModified = true;
            }
        };
        tree.Reload();

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[1]"));
            selectionChanged = false;
            tree.CopySelectedItemsToClipboard();
            tree.PasteDataFromClipboard();

            Assert.That(selectionChanged, Is.True);
            Assert.That(tree["map/action"].children, Has.Count.EqualTo(4));
            Assert.That(tree["map/action"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/leftStick/x"));
            Assert.That(tree["map/action"].children[1], Is.TypeOf<CompositeBindingTreeItem>());
            Assert.That(tree["map/action"].children[2], Is.TypeOf<CompositeBindingTreeItem>());
            Assert.That(tree["map/action"].children[3].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/leftStick/y"));
            Assert.That(tree["map/action"].children[1].children, Has.Count.EqualTo(2));
            Assert.That(tree["map/action"].children[2].children, Has.Count.EqualTo(2));
            Assert.That(tree["map/action"].children[1].children[0].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree["map/action"].children[1].children[1].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/b"));
            Assert.That(tree["map/action"].children[2].children[0].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree["map/action"].children[2].children[1].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/b"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCopyPasteCompositeBinding_IntoDifferentAction()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var action = map.AddAction("action");
        var action2 = map.AddAction("action2");
        action.AddBinding("<Gamepad>/leftStick/x");
        action.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/b");
        action2.AddBinding("<Gamepad>/leftStick/x");

        var so = new SerializedObject(asset);
        var selectionChanged = false;
        var serializedObjectModified = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSelectionChanged = () =>
            {
                Assert.That(selectionChanged, Is.False);
                selectionChanged = true;
            },
            onSerializedObjectModified = () =>
            {
                Assert.That(serializedObjectModified, Is.False);
                serializedObjectModified = true;
            }
        };
        tree.Reload();

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[1]"));
            selectionChanged = false;
            tree.CopySelectedItemsToClipboard();
            tree.SelectItem("map/action2");
            selectionChanged = false;
            tree.PasteDataFromClipboard();

            Assert.That(selectionChanged, Is.True);
            Assert.That(tree["map/action"].children, Has.Count.EqualTo(2));
            Assert.That(tree["map/action"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/leftStick/x"));
            Assert.That(tree["map/action"].children[1], Is.TypeOf<CompositeBindingTreeItem>());
            Assert.That(tree["map/action"].children[1].children, Has.Count.EqualTo(2));
            Assert.That(tree["map/action"].children[1].children[0].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree["map/action"].children[1].children[1].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/b"));

            Assert.That(tree["map/action2"].children, Has.Count.EqualTo(2));
            Assert.That(tree["map/action2"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/leftStick/x"));
            Assert.That(tree["map/action2"].children[1], Is.TypeOf<CompositeBindingTreeItem>());
            Assert.That(tree["map/action2"].children[1].children, Has.Count.EqualTo(2));
            Assert.That(tree["map/action2"].children[1].children[0].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree["map/action2"].children[1].children[1].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/b"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCopyPastePartOfCompositeBinding_IntoSameComposite()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var action = map.AddAction("action");
        action.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/b");
        action.AddBinding("<Gamepad>/rightTrigger");

        var so = new SerializedObject(asset);
        var selectionChanged = false;
        var serializedObjectModified = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSelectionChanged = () =>
            {
                Assert.That(selectionChanged, Is.False);
                selectionChanged = true;
            },
            onSerializedObjectModified = () =>
            {
                Assert.That(serializedObjectModified, Is.False);
                serializedObjectModified = true;
            }
        };
        tree.Reload();

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[1]"));
            tree.CopySelectedItemsToClipboard();
            selectionChanged = false;
            tree.PasteDataFromClipboard();

            Assert.That(selectionChanged, Is.True);
            Assert.That(tree.GetSelectedItems(),
                Is.EquivalentTo(new[]
                    {tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[2]")}));
            Assert.That(serializedObjectModified, Is.True);
            Assert.That(tree["map/action/Axis"], Is.TypeOf<CompositeBindingTreeItem>());
            Assert.That(tree["map/action/Axis"].children, Has.Count.EqualTo(3));
            Assert.That(tree["map/action/Axis"].children[0], Is.TypeOf<PartOfCompositeBindingTreeItem>());
            Assert.That(tree["map/action/Axis"].children[1], Is.TypeOf<PartOfCompositeBindingTreeItem>());
            Assert.That(tree["map/action/Axis"].children[2], Is.TypeOf<PartOfCompositeBindingTreeItem>());
            Assert.That(tree["map/action/Axis"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree["map/action/Axis"].children[1].As<BindingTreeItem>().path, Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree["map/action/Axis"].children[2].As<BindingTreeItem>().path, Is.EqualTo("<Keyboard>/b"));
            Assert.That(tree["map/action/Axis"].children[0].As<BindingTreeItem>().name, Is.EqualTo("Positive"));
            Assert.That(tree["map/action/Axis"].children[1].As<BindingTreeItem>().name, Is.EqualTo("Positive"));
            Assert.That(tree["map/action/Axis"].children[2].As<BindingTreeItem>().name, Is.EqualTo("Negative"));
            Assert.That(tree["map/action"].children[1].As<BindingTreeItem>().path,
                Is.EqualTo("<Gamepad>/rightTrigger"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCopyPastePartOfCompositeBinding_IntoDifferentComposite()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var action = map.AddAction("action");
        action.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/b");
        action.AddCompositeBinding("Axis")
            .With("Positive", "<Gamepad>/buttonEast")
            .With("Negative", "<Gamepad>/buttonWest");

        var so = new SerializedObject(asset);
        var selectionChanged = false;
        var serializedObjectModified = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSelectionChanged = () =>
            {
                Assert.That(selectionChanged, Is.False);
                selectionChanged = true;
            },
            onSerializedObjectModified = () =>
            {
                Assert.That(serializedObjectModified, Is.False);
                serializedObjectModified = true;
            }
        };
        tree.Reload();

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[2]"));
            tree.CopySelectedItemsToClipboard();
            selectionChanged = false;
            tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[3]"));
            selectionChanged = false;
            tree.PasteDataFromClipboard();

            Assert.That(selectionChanged, Is.True);
            Assert.That(tree.GetSelectedItems(),
                Is.EquivalentTo(new[]
                    {tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[6]")}));
            Assert.That(serializedObjectModified, Is.True);
            Assert.That(tree["map/action"].children[0], Is.TypeOf<CompositeBindingTreeItem>());
            Assert.That(tree["map/action"].children[1], Is.TypeOf<CompositeBindingTreeItem>());
            Assert.That(tree["map/action"].children[0].children, Has.Count.EqualTo(2));
            Assert.That(tree["map/action"].children[1].children, Has.Count.EqualTo(3));
            Assert.That(tree["map/action"].children[0].children[0].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree["map/action"].children[0].children[1].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/b"));
            Assert.That(tree["map/action"].children[1].children[0].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Gamepad>/buttonEast"));
            Assert.That(tree["map/action"].children[1].children[1].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Gamepad>/buttonWest"));
            Assert.That(tree["map/action"].children[1].children[2].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/b"));
            Assert.That(tree["map/action"].children[1].children[0].As<PartOfCompositeBindingTreeItem>().name,
                Is.EqualTo("Positive"));
            Assert.That(tree["map/action"].children[1].children[1].As<PartOfCompositeBindingTreeItem>().name,
                Is.EqualTo("Negative"));
            Assert.That(tree["map/action"].children[1].children[2].As<PartOfCompositeBindingTreeItem>().name,
                Is.EqualTo("Negative"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCutAndPasteAction()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map1");
        var map2 = asset.AddActionMap("map2");
        var action1 = map1.AddAction("action1");
        action1.AddBinding("<Gamepad>/leftStick/x");
        action1.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/b");
        map2.AddAction("action2", binding: "<Keyboard>/space");

        var so = new SerializedObject(asset);
        var selectionChanged = false;
        var serializedObjectModified = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSelectionChanged = () =>
            {
                Assert.That(selectionChanged, Is.False);
                selectionChanged = true;
            },
            onSerializedObjectModified = () =>
            {
                Assert.That(serializedObjectModified, Is.False);
                serializedObjectModified = true;
            }
        };
        tree.Reload();

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.SelectItem("map1/action1");
            selectionChanged = false;
            tree.HandleCopyPasteCommandEvent(EditorGUIUtility.CommandEvent(InputActionTreeView.k_CutCommand));

            Assert.That(selectionChanged, Is.True);
            Assert.That(serializedObjectModified, Is.True);
            Assert.That(tree.GetSelectedItems(), Is.Empty);
            Assert.That(tree.FindItemByPath("map1/action1"), Is.Null);
            Assert.That(tree["map1"].children, Is.Null.Or.Empty);
            Assert.That(EditorHelpers.GetSystemCopyBufferContents(), Does.StartWith(InputActionTreeView.k_CopyPasteMarker));

            selectionChanged = false;
            serializedObjectModified = false;

            tree.SelectItem("map2");
            selectionChanged = false;
            tree.HandleCopyPasteCommandEvent(EditorGUIUtility.CommandEvent(InputActionTreeView.k_PasteCommand));

            Assert.That(selectionChanged, Is.True);
            Assert.That(tree.FindItemByPath("map2/action1"), Is.Not.Null);
            Assert.That(tree.GetSelectedItems(), Is.EquivalentTo(new[] { tree.FindItemByPath("map2/action1")}));
            Assert.That(tree["map2"].children, Has.Count.EqualTo(2));
            Assert.That(tree["map2"].children[0].As<ActionTreeItem>().displayName, Is.EqualTo("action2"));
            Assert.That(tree["map2"].children[1].As<ActionTreeItem>().displayName, Is.EqualTo("action1"));
            Assert.That(tree["map2/action1"].children, Has.Count.EqualTo(2));
            Assert.That(tree["map2/action1"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/leftStick/x"));
            Assert.That(tree["map2/action1"].children[1].As<CompositeBindingTreeItem>().path, Is.EqualTo("Axis"));
            Assert.That(tree["map2/action1"].children[1].children, Has.Count.EqualTo(2));
            Assert.That(tree["map2/action1"].children[1].children[0].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/a"));
            Assert.That(tree["map2/action1"].children[1].children[1].As<PartOfCompositeBindingTreeItem>().path,
                Is.EqualTo("<Keyboard>/b"));
            Assert.That(tree["map2/action1"].children[1].children[0].As<PartOfCompositeBindingTreeItem>().name,
                Is.EqualTo("Positive"));
            Assert.That(tree["map2/action1"].children[1].children[1].As<PartOfCompositeBindingTreeItem>().name,
                Is.EqualTo("Negative"));
        }
    }

    // https://fogbugz.unity3d.com/f/cases/1228000/
    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanCutAndPasteAction_WithControlSchemeFilterActive()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var action1 = map.AddAction("action1");
        map.AddAction("action2");
        asset.AddControlScheme("scheme1");
        asset.AddControlScheme("scheme2");
        action1.AddBinding("<Gamepad>/buttonSouth", groups: "scheme1");
        action1.AddBinding("<Keyboard>/space", groups: "scheme2");

        var so = new SerializedObject(asset);
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
        };
        tree.SetItemSearchFilterAndReload("g:scheme1");

        using (new EditorHelpers.FakeSystemCopyBuffer())
        {
            tree.SelectItem("map/action1");
            tree.HandleCopyPasteCommandEvent(EditorGUIUtility.CommandEvent(InputActionTreeView.k_CutCommand));
            tree.SelectItem("map/action2");
            tree.HandleCopyPasteCommandEvent(EditorGUIUtility.CommandEvent(InputActionTreeView.k_PasteCommand));

            Assert.That(tree.FindItemByPath("map/action1"), Is.Not.Null);
            Assert.That(tree["map/action1"].childrenIncludingHidden.Count(), Is.EqualTo(2));
            Assert.That(tree["map/action1"].childrenIncludingHidden.ToList()[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(tree["map/action1"].childrenIncludingHidden.ToList()[1].As<BindingTreeItem>().path, Is.EqualTo("<Keyboard>/space"));
            Assert.That(tree["map/action1"].childrenIncludingHidden.ToList()[0].As<BindingTreeItem>().groups, Is.EqualTo("scheme1"));
            Assert.That(tree["map/action1"].childrenIncludingHidden.ToList()[1].As<BindingTreeItem>().groups, Is.EqualTo("scheme2"));
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanFilterItems()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map1");
        var action1 = map1.AddAction("AAAA");
        var action2 = map1.AddAction("AABB");
        action1.AddBinding("<Gamepad>/leftStick", groups: "BB");
        action2.AddBinding("<Gamepad>/rightStick", groups: "B");
        var map2 = asset.AddActionMap("map2");
        var action3 = map2.AddAction("CCAA");
        action3.AddBinding("<Keyboard>/a", groups: "BB;B");
        action3.AddCompositeBinding("Axis")
            .With("Positive", "<Gamepad>/buttonSouth", groups: "BB")
            .With("Negative", "<Gamepad>/buttonNorth", groups: "BB");

        var so = new SerializedObject(asset);
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so)
        };

        // Filter by just name.
        tree.SetItemSearchFilterAndReload("cc");

        Assert.That(tree.rootItem.children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children[0].displayName, Is.EqualTo("map2"));
        Assert.That(tree.rootItem.children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[0], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children[0].children[0].displayName, Is.EqualTo("CCAA"));
        Assert.That(tree.rootItem.children[0].children[0].children, Has.Count.EqualTo(2));
        Assert.That(tree.rootItem.children[0].children[0].children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree.rootItem.children[0].children[0].children[1], Is.TypeOf<CompositeBindingTreeItem>());
        Assert.That(tree.rootItem.children[0].children[0].children[1].children, Has.Count.EqualTo(2));

        // Filter by binding group.
        // NOTE: This should match by the *complete* group name, not just by substring.
        tree.SetItemSearchFilterAndReload("g:B");

        Assert.That(tree.rootItem.children, Has.Count.EqualTo(2));
        Assert.That(tree.rootItem.children[0], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children[1], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children[0].displayName, Is.EqualTo("map1"));
        Assert.That(tree.rootItem.children[1].displayName, Is.EqualTo("map2"));
        Assert.That(tree.rootItem.children[0].children, Has.Count.EqualTo(2));
        Assert.That(tree.rootItem.children[0].children[0], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children[0].children[1], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children[0].children[0].displayName, Is.EqualTo("AAAA"));
        Assert.That(tree.rootItem.children[0].children[1].displayName, Is.EqualTo("AABB"));
        Assert.That(tree.rootItem.children[0].children[0].children, Is.Empty);
        Assert.That(tree.rootItem.children[0].children[1].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[1].children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree.rootItem.children[0].children[1].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/rightStick"));
        Assert.That(tree.rootItem.children[1].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[1].children[0], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children[1].children[0].displayName, Is.EqualTo("CCAA"));
        Assert.That(tree.rootItem.children[1].children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[1].children[0].children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree.rootItem.children[1].children[0].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Keyboard>/a"));

        // Filter by device layout.
        tree.SetItemSearchFilterAndReload("d:Gamepad");

        Assert.That(tree.rootItem.children, Has.Count.EqualTo(2));
        Assert.That(tree.rootItem.children[0], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children[1], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children[0].displayName, Is.EqualTo("map1"));
        Assert.That(tree.rootItem.children[1].displayName, Is.EqualTo("map2"));
        Assert.That(tree.rootItem.children[0].children, Has.Count.EqualTo(2));
        Assert.That(tree.rootItem.children[0].children[0], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children[0].children[1], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children[0].children[0].displayName, Is.EqualTo("AAAA"));
        Assert.That(tree.rootItem.children[0].children[1].displayName, Is.EqualTo("AABB"));
        Assert.That(tree.rootItem.children[0].children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[0].children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree.rootItem.children[0].children[0].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/leftStick"));
        Assert.That(tree.rootItem.children[0].children[1].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[1].children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree.rootItem.children[0].children[1].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/rightStick"));
        Assert.That(tree.rootItem.children[1].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[1].children[0], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree.rootItem.children[1].children[0].displayName, Is.EqualTo("CCAA"));
        Assert.That(tree.rootItem.children[1].children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[1].children[0].children[0], Is.TypeOf<CompositeBindingTreeItem>());
        Assert.That(tree.rootItem.children[1].children[0].children[0].children, Has.Count.EqualTo(2));
        Assert.That(tree.rootItem.children[1].children[0].children[0].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/buttonSouth"));
        Assert.That(tree.rootItem.children[1].children[0].children[0].children[1].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/buttonNorth"));

        // Filter that matches nothing.
        tree.SetItemSearchFilterAndReload("matchesNothing");

        Assert.That(tree.rootItem.children, Is.Empty);
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanHaveWhitespaceInSearchFilter()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map");
        var action = map1.AddAction("action");
        action.AddBinding("<Gamepad>/buttonNorth", groups: "Other");
        action.AddBinding("<Gamepad>/buttonSouth", groups: "Binding(Group\"With)  Spaces");

        using (var so = new SerializedObject(asset))
        {
            var tree = new InputActionTreeView(so)
            {
                onBuildTree = () => InputActionTreeView.BuildFullTree(so)
            };

            tree.SetItemSearchFilterAndReload("\"g:Binding(Group\\\"With)  Spaces\"");

            Assert.That(tree["map"].children, Has.Count.EqualTo(1));
            Assert.That(tree["map/action"].children, Has.Count.EqualTo(1));
            Assert.That(tree["map/action"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/buttonSouth"));
        }
    }

    // Bindings that have no associated binding group (i.e. aren't part of any control scheme), will not be constrained
    // by a binding mask. Means they will be active regardless of which binding group / control scheme is chosen. To
    // make this more visible in the tree, we display those items as "{GLOBAL}" when filtering by binding group.
    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_WhenFilteringByBindingGroup_ItemsNotInAnyGroup_AreShownAsGlobal()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map1");
        var action = map.AddAction("action");
        action.AddBinding("<Gamepad>/leftStick", groups: "A"); // In group.
        action.AddBinding("<Gamepad>/rightStick"); // Not in group.

        var so = new SerializedObject(asset);
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so)
        };

        tree.SetItemSearchFilterAndReload("g:A");

        var actionItem = tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Actions.Array.data[0]");
        Assert.That(actionItem, Is.Not.Null);

        Assert.That(actionItem.children, Has.Count.EqualTo(2));
        Assert.That(actionItem.children[0].displayName, Does.Not.Contain("{GLOBAL}"));
        Assert.That(actionItem.children[1].displayName, Does.Contain("{GLOBAL}"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanDeleteActionMaps()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map1");
        var map2 = asset.AddActionMap("map2");
        asset.AddActionMap("map3");
        var action1 = map1.AddAction("action1");
        var action2 = map2.AddAction("action2");
        action1.AddBinding("<Gamepad>/leftStick");
        action2.AddBinding("<Gamepad>/rightStick");

        var so = new SerializedObject(asset);
        var modified = false;
        var selectionChanged = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSerializedObjectModified = () =>
            {
                Assert.That(modified, Is.False);
                modified = true;
            },
            onSelectionChanged = () =>
            {
                Assert.That(selectionChanged, Is.False);
                selectionChanged = true;
            }
        };
        tree.Reload();
        tree.SelectItem("map1");
        selectionChanged = false;
        tree.SelectItem("map3", additive: true);
        selectionChanged = false;
        tree.DeleteDataOfSelectedItems();

        Assert.That(selectionChanged, Is.True);
        Assert.That(modified, Is.True);
        Assert.That(tree.HasSelection, Is.False);
        Assert.That(tree.rootItem.children, Is.Not.Null);
        Assert.That(tree.rootItem.children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children[0].displayName, Is.EqualTo("map2"));
        Assert.That(tree.rootItem.children[0].children, Is.Not.Null);
        Assert.That(tree.rootItem.children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[0].displayName, Is.EqualTo("action2"));
        Assert.That(tree.rootItem.children[0].children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[0].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/rightStick"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanDeleteActions()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map1");
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");
        map.AddAction("action3");
        action1.AddBinding("<Gamepad>/leftStick");
        action2.AddBinding("<Gamepad>/rightStick");

        var so = new SerializedObject(asset);
        var modified = false;
        var selectionChanged = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSerializedObjectModified = () =>
            {
                Assert.That(modified, Is.False);
                modified = true;
            },
            onSelectionChanged = () =>
            {
                Assert.That(selectionChanged, Is.False);
                selectionChanged = true;
            }
        };
        tree.Reload();
        tree.SelectItem("map1/action1");
        selectionChanged = false;
        tree.SelectItem("map1/action3", additive: true);
        selectionChanged = false;
        tree.DeleteDataOfSelectedItems();

        Assert.That(selectionChanged, Is.True);
        Assert.That(modified, Is.True);
        Assert.That(tree.HasSelection, Is.False);
        Assert.That(tree.rootItem.children, Is.Not.Null);
        Assert.That(tree.rootItem.children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children[0].displayName, Is.EqualTo("map1"));
        Assert.That(tree.rootItem.children[0].children, Is.Not.Null);
        Assert.That(tree.rootItem.children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[0].displayName, Is.EqualTo("action2"));
        Assert.That(tree.rootItem.children[0].children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[0].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/rightStick"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanDeleteBindings()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map1");
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");
        action1.AddBinding("<Gamepad>/leftStick");
        action1.AddBinding("<Gamepad>/buttonSouth");
        action1.AddBinding("<Gamepad>/dpad");
        action2.AddBinding("<Gamepad>/rightStick");

        var so = new SerializedObject(asset);
        var modified = false;
        var selectionChanged = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSerializedObjectModified = () =>
            {
                Assert.That(modified, Is.False);
                modified = true;
            },
            onSelectionChanged = () =>
            {
                Assert.That(selectionChanged, Is.False);
                selectionChanged = true;
            }
        };
        tree.Reload();
        tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[1]"));
        selectionChanged = false;
        tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[2]"),
            additive: true);
        selectionChanged = false;
        tree.DeleteDataOfSelectedItems();

        Assert.That(selectionChanged, Is.True);
        Assert.That(modified, Is.True);
        Assert.That(tree.HasSelection, Is.False);
        Assert.That(tree.rootItem.children, Is.Not.Null);
        Assert.That(tree.rootItem.children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children[0].displayName, Is.EqualTo("map1"));
        Assert.That(tree.rootItem.children[0].children, Is.Not.Null);
        Assert.That(tree.rootItem.children[0].children, Has.Count.EqualTo(2));
        Assert.That(tree.rootItem.children[0].children[0].displayName, Is.EqualTo("action1"));
        Assert.That(tree.rootItem.children[0].children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[0].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/leftStick"));
        Assert.That(tree.rootItem.children[0].children[1].displayName, Is.EqualTo("action2"));
        Assert.That(tree.rootItem.children[0].children[1].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[1].children[0].As<BindingTreeItem>().path,
            Is.EqualTo("<Gamepad>/rightStick"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanDeleteMultipleBindings()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map1 = asset.AddActionMap("map1");
        var action1 = map1.AddAction("action1");
        action1.AddBinding("<Gamepad>/leftStick");
        action1.AddBinding("<Gamepad>/buttonSouth");
        action1.AddBinding("<Gamepad>/dpad");

        var so = new SerializedObject(asset);
        var modified = false;
        var selectionChanged = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSerializedObjectModified = () =>
            {
                Assert.That(modified, Is.False);
                modified = true;
            },
            onSelectionChanged = () =>
            {
                Assert.That(selectionChanged, Is.False);
                selectionChanged = true;
            }
        };
        tree.Reload();
        selectionChanged = false;
        tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[0]"));
        selectionChanged = false;
        tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[1]"), true);
        selectionChanged = false;
        tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[2]"), true);
        selectionChanged = false;
        tree.DeleteDataOfSelectedItems();

        Assert.That(selectionChanged, Is.True);
        Assert.That(modified, Is.True);
        Assert.That(tree.HasSelection, Is.False);
        Assert.That(tree.rootItem.children, Is.Not.Null);
        Assert.That(tree.rootItem.children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree.rootItem.children[0].displayName, Is.EqualTo("map1"));
        Assert.That(tree.rootItem.children[0].children, Is.Not.Null);
        Assert.That(tree.rootItem.children[0].children, Has.Count.EqualTo(1));
        Assert.That(tree.rootItem.children[0].children[0].displayName, Is.EqualTo("action1"));
        Assert.That(tree.rootItem.children[0].children[0].children, Is.Null);
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CanDeleteComposite()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map1");
        var action1 = map.AddAction("action1");
        var action2 = map.AddAction("action2");
        action1.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/b");
        action1.AddBinding("<Gamepad>/dpad");
        action2.AddBinding("<Gamepad>/rightStick");

        var so = new SerializedObject(asset);
        var modified = false;
        var selectionChanged = false;
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so),
            onSerializedObjectModified = () =>
            {
                Assert.That(modified, Is.False);
                modified = true;
            },
            onSelectionChanged = () =>
            {
                Assert.That(selectionChanged, Is.False);
                selectionChanged = true;
            }
        };
        tree.Reload();
        tree.SelectItem(tree.FindItemByPropertyPath("m_ActionMaps.Array.data[0].m_Bindings.Array.data[0]"));
        selectionChanged = false;
        tree.DeleteDataOfSelectedItems();

        Assert.That(selectionChanged, Is.True);
        Assert.That(modified, Is.True);
        Assert.That(tree.HasSelection, Is.False);
        Assert.That(tree["map1"], Is.TypeOf<ActionMapTreeItem>());
        Assert.That(tree["map1"].children, Has.Count.EqualTo(2));
        Assert.That(tree["map1/action1"], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree["map1/action2"], Is.TypeOf<ActionTreeItem>());
        Assert.That(tree["map1/action1"].children, Has.Count.EqualTo(1));
        Assert.That(tree["map1/action2"].children, Has.Count.EqualTo(1));
        Assert.That(tree["map1/action1"].children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree["map1/action2"].children[0], Is.TypeOf<BindingTreeItem>());
        Assert.That(tree["map1/action1"].children[0].children, Is.Null);
        Assert.That(tree["map1/action2"].children[0].children, Is.Null);
        Assert.That(tree["map1/action1"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/dpad"));
        Assert.That(tree["map1/action2"].children[0].As<BindingTreeItem>().path, Is.EqualTo("<Gamepad>/rightStick"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_ActionTree_CompositesAreShownWithNiceNames()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var action = map.AddAction("action");
        action.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/b");

        // Wipe name that AddCompositeBinding assigned.
        action.ChangeBinding(0).WithName(null);

        var so = new SerializedObject(asset);
        var tree = new InputActionTreeView(so)
        {
            onBuildTree = () => InputActionTreeView.BuildFullTree(so)
        };
        tree.Reload();

        Assert.That(tree["map/action"].children[0].displayName, Is.EqualTo("1D Axis"));
    }

    #if UNITY_STANDALONE // CodeDom API not available in most players. We only build and run this in the editor but we're
                         // still affected by the current platform.
#if !TEMP_DISABLE_EDITOR_TESTS_ON_TRUNK // Temporary: Disables tests while net-profile passed from UTR to trunk is overridden to netstandard (missing CodeDom)
    [Test]
    [Category("Editor")]
    [TestCase("MyControls (2)", "MyNamespace", "", "MyNamespace.MyControls2")]
    [TestCase("MyControls (2)", "MyNamespace", "MyClassName", "MyNamespace.MyClassName")]
    [TestCase("MyControls", "", "MyClassName", "MyClassName")]
    [TestCase("interface", "", "class", "class")] // Make sure we can deal with C# reserved keywords.
    public void Editor_CanGenerateCodeWrapperForInputAsset(string assetName, string namespaceName, string className, string typeName)
    {
        var map1 = new InputActionMap("set1");
        map1.AddAction("action1", binding: "/gamepad/leftStick");
        map1.AddAction("action2", binding: "/gamepad/rightStick");
        var map2 = new InputActionMap("set2");
        map2.AddAction("action1", binding: "/gamepad/buttonSouth");
        // Add an action that has a C# reserved keyword name.
        map2.AddAction("return");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map1);
        asset.AddActionMap(map2);
        asset.name = assetName;

        var code = InputActionCodeGenerator.GenerateWrapperCode(asset,
            new InputActionCodeGenerator.Options {namespaceName = namespaceName, className = className, sourceAssetPath = "test"});

        var type = Compile(code, typeName);

        var set1Property = type.GetProperty("set1");
        Assert.That(set1Property, Is.Not.Null);
        var set1MapGetter = set1Property.PropertyType.GetMethod("Get");
        var instance = Activator.CreateInstance(type);
        Assert.That(instance, Is.Not.Null);
        var set1Instance = set1Property.GetValue(instance);
        Assert.That(set1Instance, Is.Not.Null);
        var set1map = set1MapGetter.Invoke(set1Instance, null) as InputActionMap;
        Assert.That(set1map, Is.Not.Null);
        Assert.That(typeof(IInputActionCollection2).IsAssignableFrom(type));

        Assert.That(set1map.ToJson(), Is.EqualTo(map1.ToJson()));
    }

#endif
#endif

    // Can take any given registered layout and generate a cross-platform C# struct for it
    // that collects all the control values from both proper and optional controls (based on
    // all derived layouts).
    [Test]
    [Category("Editor")]
    [Ignore("TODO")]
    public void TODO_Editor_CanGenerateStateStructForLayout()
    {
        Assert.Fail();
    }

    // Can take any given registered layout and generate a piece of code that takes as input
    // memory in the state format of the layout and generates as output state in the cross-platform
    // C# struct format.
    [Test]
    [Category("Editor")]
    [Ignore("TODO")]
    public void TODO_Editor_CanGenerateStateStructConversionCodeForLayout()
    {
        Assert.Fail();
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

    [Test]
    [Category("Editor")]
    [Ignore("TODO")]
    public void TODO_Editor_SettingsModifiedInPlayMode_AreRestoredWhenReEnteringEditMode()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Editor")]
    public void Editor_AlwaysKeepsEditorUpdatesEnabled()
    {
        Assert.That(InputSystem.s_Manager.updateMask & InputUpdateType.Editor, Is.EqualTo(InputUpdateType.Editor));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanGetValueTypeOfLayout()
    {
        Assert.That(EditorInputControlLayoutCache.GetValueType("Axis"), Is.SameAs(typeof(float)));
        Assert.That(EditorInputControlLayoutCache.GetValueType("Button"), Is.SameAs(typeof(float)));
        Assert.That(EditorInputControlLayoutCache.GetValueType("Stick"), Is.SameAs(typeof(Vector2)));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanGetValueTypeOfProcessor()
    {
        Assert.That(InputProcessor.GetValueTypeFromType(typeof(StickDeadzoneProcessor)), Is.SameAs(typeof(Vector2)));
        Assert.That(InputProcessor.GetValueTypeFromType(typeof(ScaleProcessor)), Is.SameAs(typeof(float)));
    }

    private class TestInteractionWithValueType : IInputInteraction<float>
    {
        public void Process(ref InputInteractionContext context)
        {
        }

        public void Reset()
        {
        }
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanGetValueTypeOfInteraction()
    {
        InputSystem.RegisterInteraction<TestInteractionWithValueType>();
        Assert.That(InputInteraction.GetValueType(typeof(TestInteractionWithValueType)), Is.SameAs(typeof(float)));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanGetParameterEditorFromInteractionType()
    {
        Assert.That(InputParameterEditor.LookupEditorForType(typeof(HoldInteraction)),
            Is.SameAs(typeof(HoldInteractionEditor)));
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

    [Test]
    [Category("Editor")]
    public void Editor_CanListOptionalControlsForLayout()
    {
        const string baseLayout = @"
            {
                ""name"" : ""Base"",
                ""controls"" : [
                    { ""name"" : ""controlFromBase"", ""layout"" : ""Button"" }
                ]
            }
        ";
        const string firstDerived = @"
            {
                ""name"" : ""FirstDerived"",
                ""extend"" : ""Base"",
                ""controls"" : [
                    { ""name"" : ""controlFromFirstDerived"", ""layout"" : ""Axis"" }
                ]
            }
        ";
        const string secondDerived = @"
            {
                ""name"" : ""SecondDerived"",
                ""extend"" : ""FirstDerived"",
                ""controls"" : [
                    { ""name"" : ""controlFromSecondDerived"", ""layout"" : ""Vector2"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(baseLayout);
        InputSystem.RegisterLayout(firstDerived);
        InputSystem.RegisterLayout(secondDerived);

        var optionalControlsForBase =
            EditorInputControlLayoutCache.GetOptionalControlsForLayout("Base").ToList();
        var optionalControlsForFirstDerived =
            EditorInputControlLayoutCache.GetOptionalControlsForLayout("FirstDerived").ToList();
        var optionalControlsForSecondDerived =
            EditorInputControlLayoutCache.GetOptionalControlsForLayout("SecondDerived").ToList();

        Assert.That(optionalControlsForBase, Has.Count.EqualTo(2));
        Assert.That(optionalControlsForBase[0].name, Is.EqualTo(new InternedString("controlFromFirstDerived")));
        Assert.That(optionalControlsForBase[0].layout, Is.EqualTo(new InternedString("Axis")));
        Assert.That(optionalControlsForBase[1].name, Is.EqualTo(new InternedString("controlFromSecondDerived")));
        Assert.That(optionalControlsForBase[1].layout, Is.EqualTo(new InternedString("Vector2")));

        Assert.That(optionalControlsForFirstDerived, Has.Count.EqualTo(1));
        Assert.That(optionalControlsForFirstDerived[0].name, Is.EqualTo(new InternedString("controlFromSecondDerived")));
        Assert.That(optionalControlsForFirstDerived[0].layout, Is.EqualTo(new InternedString("Vector2")));

        Assert.That(optionalControlsForSecondDerived, Is.Empty);
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanIconsForLayouts()
    {
        const string kIconPath = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/";
        var skinPrefix = EditorGUIUtility.isProSkin ? "d_" : "";
        var scale = Mathf.Clamp((int)EditorGUIUtility.pixelsPerPoint, 0, 4);
        var scalePostFix = scale > 1 ? $"@{scale}x" : "";

        Assert.That(EditorInputControlLayoutCache.GetIconForLayout("Button"),
            Is.SameAs(AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath + skinPrefix + "Button" + scalePostFix + ".png")));
        Assert.That(EditorInputControlLayoutCache.GetIconForLayout("Axis"),
            Is.SameAs(AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath + skinPrefix + "Axis" + scalePostFix + ".png")));
        Assert.That(EditorInputControlLayoutCache.GetIconForLayout("Key"),
            Is.SameAs(AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath + skinPrefix + "Button" + scalePostFix + ".png")));
        Assert.That(EditorInputControlLayoutCache.GetIconForLayout("DualShockGamepad"),
            Is.SameAs(AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath + skinPrefix + "Gamepad" + scalePostFix + ".png")));
        Assert.That(EditorInputControlLayoutCache.GetIconForLayout("Pen"),
            Is.SameAs(AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath + skinPrefix + "Pen" + scalePostFix + ".png")));
    }

    [Test]
    [Category("Editor")]
    public void Editor_AddingAndRemovingLayoutRefreshesLayoutCache()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout""
            }
        ";

        InputSystem.RegisterLayout(json);

        Assert.That(EditorInputControlLayoutCache.allLayouts, Has.Exactly(1).With.Property("name").EqualTo(new InternedString("TestLayout")));

        InputSystem.RemoveLayout("TestLayout");

        Assert.That(EditorInputControlLayoutCache.allLayouts, Has.None.With.Property("name").EqualTo(new InternedString("TestLayout")));
    }

    private class TestEditorWindow : EditorWindow
    {
        public Vector2 mousePosition;

        public void OnGUI()
        {
            mousePosition = InputSystem.GetDevice<Mouse>().position.ReadValue();
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

    [Test]
    [Category("Editor")]
    public void Editor_CanForceKeyboardAndMouseInputToGameViewWithoutFocus()
    {
        runtime.runInBackground = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        runtime.PlayerFocusLost();

        Assert.That(keyboard.enabled, Is.True);
        Assert.That(mouse.enabled, Is.True);

        Press(keyboard.spaceKey, queueEventOnly: true);
        Press(mouse.leftButton, queueEventOnly: true);

        // First make sure the editor is *not* eating this input.
        var eventCountBefore = InputSystem.metrics.totalEventCount;
        InputSystem.Update(InputUpdateType.Editor);
        Assert.That(InputSystem.metrics.totalEventCount, Is.EqualTo(eventCountBefore));

        Assert.That(keyboard.spaceKey.isPressed, Is.False);
        Assert.That(mouse.leftButton.isPressed, Is.False);

        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(keyboard.spaceKey.isPressed, Is.True);
        Assert.That(mouse.leftButton.isPressed, Is.True);
    }

    [Test]
    [Category("Editor")]
    public void Editor_WhenNotInPlayMode_AllInputGoesToEditor()
    {
        // Give us a setting where in play mode, gamepad input would go to the game
        // regardless of focus.
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus;

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();
        runtime.isInPlayMode = false;

        Press(gamepad.buttonSouth);
        Press(mouse.leftButton);

        Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Editor));
        Assert.That(gamepad.buttonSouth.isPressed, Is.True);

        Set(gamepad.leftTrigger, 0.5f, queueEventOnly: true);
        Set(mouse.position, new Vector2(123, 234), queueEventOnly: true);

        // Try running a dynamic update. Outside of play mode, this should do nothing.
        var eventCountBefore = InputSystem.metrics.totalEventCount;
        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Editor));
        Assert.That(InputSystem.metrics.totalEventCount, Is.EqualTo(eventCountBefore));
        Assert.That(gamepad.buttonSouth.isPressed, Is.True);
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.Zero);
        Assert.That(mouse.position.ReadValue(), Is.EqualTo(default(Vector2)));

        // Running the editor update now, we should see the event for the gamepad popping up.
        InputSystem.Update();

        Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Editor));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5f));
        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
    }

    [Test]
    [Category("Editor")]
    public unsafe void Editor_WhenEditorIsActivated_AllDevicesAreSynced()
    {
        runtime.isInPlayMode = false;

        var mouse = InputSystem.AddDevice<Mouse>();

        var receivedMouseSync = false;
        runtime.SetDeviceCommandCallback(mouse, (id, commandPtr) =>
        {
            if (commandPtr->type == RequestSyncCommand.Type)
                receivedMouseSync = true;
            return InputDeviceCommand.GenericFailure;
        });

        runtime.isEditorActive = false;
        InputSystem.Update();

        Assert.That(receivedMouseSync, Is.False);

        runtime.isEditorActive = true;
        InputSystem.Update();

        Assert.That(receivedMouseSync, Is.True);
    }

    // While going into play mode, the editor will be unresponsive. So what will happen is that when the user clicks the
    // play mode button and then moves the mouse around while Unity is busy going into play mode, the game will receive
    // a huge pointer motion delta in one of its first frames. If pointer motion is tied to camera motion, for example,
    // this will usually lead to the camera looking down at the ground because after clicking the play mode button at the
    // top of the UI, the user will likely move the pointer down towards the game view area thus generating a large down
    // motion delta.
    //
    // What we do to counter this is to record the time of when we enter play mode and then record the time again when
    // we have fully entered play mode. All the events in-between we discard.
    [Test]
    [Category("Editor")]
    public void Editor_InputEventsOccurringWhileGoingIntoPlayMode_AreDiscarded()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        // We need to actually pass time and have a non-zero start time for this to work.
        currentTime = 1;
        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingEditMode);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(234, 345) });
        currentTime = 2;
        InputSystem.OnPlayModeChange(PlayModeStateChange.EnteredPlayMode);

        InputSystem.Update();

        Assert.That(mouse.position.ReadValue(), Is.EqualTo(default(Vector2)));

        // Make sure the event was not left in the buffer.
        Assert.That(runtime.m_EventCount, Is.EqualTo(0));
    }

    [Test]
    [Category("Editor")]
    [TestCase(InputSettings.UpdateMode.ProcessEventsManually, InputUpdateType.Manual)]
    [TestCase(InputSettings.UpdateMode.ProcessEventsInDynamicUpdate, InputUpdateType.Dynamic)]
    [TestCase(InputSettings.UpdateMode.ProcessEventsInFixedUpdate, InputUpdateType.Fixed)]
    public void Editor_WhenRunUpdatesInEditModeIsEnabled_PlayerUpdatesRunOutsideOfPlayMode(InputSettings.UpdateMode updateMode, InputUpdateType updateType)
    {
        runtime.isInPlayMode = false;
        InputSystem.settings.updateMode = updateMode;

        var updates = new List<InputUpdateType>();
        InputSystem.onBeforeUpdate += () => updates.Add(InputState.currentUpdateType);

        InputSystem.Update(InputUpdateType.Editor);
        InputSystem.Update(updateType);

        Assert.That(updates, Is.EqualTo(new[] { InputUpdateType.Editor }));

        InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kRunPlayerUpdatesInEditMode, true);

        updates.Clear();

        InputSystem.Update(InputUpdateType.Editor);
        InputSystem.Update(updateType);

        Assert.That(updates, Is.EqualTo(new[] { InputUpdateType.Editor, updateType }));
    }

    [Test]
    [Category("Editor")]
    public void Editor_WhenRunUpdatesInEditModeIsEnabled_InputActionsTriggerInEditMode()
    {
        runtime.isInPlayMode = false;
        InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kRunPlayerUpdatesInEditMode, true);

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/leftTrigger");

        var performedCallCount = 0;
        action.performed += context => performedCallCount++;
        action.Enable();

        Set(gamepad.leftTrigger, 0f);
        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(performedCallCount, Is.EqualTo(0));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(0));

        Set(gamepad.leftTrigger, 0.75f, queueEventOnly: true);
        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(performedCallCount, Is.EqualTo(1));
        Assert.That(action.ReadValue<float>(), Is.EqualTo(0.75f).Within(0.00001f));
    }

    [Test]
    [Category("Editor")]
    public void Editor_LeavingPlayMode_DestroysAllActionStates()
    {
        InputSystem.AddDevice<Gamepad>();

        // Enter play mode.
        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingEditMode);
        InputSystem.OnPlayModeChange(PlayModeStateChange.EnteredPlayMode);

        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        action.Enable();

        Assert.That(InputActionState.s_GlobalState.globalList.length, Is.EqualTo(1));
        Assert.That(InputSystem.s_Manager.m_StateChangeMonitors.Length, Is.GreaterThan(0));
        Assert.That(InputSystem.s_Manager.m_StateChangeMonitors[0].count, Is.EqualTo(1));

        // Exit play mode.
        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingPlayMode);
        InputSystem.OnPlayModeChange(PlayModeStateChange.EnteredEditMode);

        Assert.That(InputActionState.s_GlobalState.globalList.length, Is.Zero);
        Assert.That(InputSystem.s_Manager.m_StateChangeMonitors[0].listeners[0].control, Is.Null); // Won't get removed, just cleared.
    }

    [Test]
    [Category("Editor")]
    public void Editor_LeavingPlayMode_RemovesAllInputUsersAndStopsListeningForUnpairedDeviceActivity()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Enter play mode.
        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingEditMode);
        InputSystem.OnPlayModeChange(PlayModeStateChange.EnteredPlayMode);

        var user = InputUser.PerformPairingWithDevice(gamepad);
        ++InputUser.listenForUnpairedDeviceActivity;
        InputUser.onUnpairedDeviceUsed += (control, ptr) => {};

        Assert.That(user.valid, Is.True);
        Assert.That(InputUser.all, Has.Count.EqualTo(1));

        // Exit play mode.
        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingPlayMode);
        InputSystem.OnPlayModeChange(PlayModeStateChange.EnteredEditMode);

        Assert.That(user.valid, Is.False);
        Assert.That(InputUser.all, Has.Count.Zero);

        // Send an event to make sure InputUser removed its event hook.
        Press(gamepad.buttonSouth);
    }

    [Test]
    [Category("Editor")]
    public void Editor_LeavingPlayMode_ReenablesAllDevicesTemporarilyDisabledDueToFocus()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.ResetAndDisableAllDevices;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus;

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();
        Set(mouse.position, new Vector2(123, 234));
        Press(gamepad.buttonSouth);

        runtime.PlayerFocusLost();

        Assert.That(gamepad.enabled, Is.False);

        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingPlayMode);

        Assert.That(gamepad.enabled, Is.True);
        Assert.That(gamepad.disabledWhileInBackground, Is.False);
        Assert.That(mouse.position.ReadValue(), Is.EqualTo(default(Vector2)));
        Assert.That(gamepad.buttonSouth.isPressed, Is.False);
    }

    [Test]
    [Category("Editor")]
    public void Editor_LeavingPlayMode_DiscardsInputActionAssetChanges()
    {
        // Control schemes
        AssertAssetIsUnmodifiedAfterExitingPlayMode(asset => asset
            .AddControlScheme("AddedControlScheme"), "Add control scheme");
        AssertAssetIsUnmodifiedAfterExitingPlayMode(asset => asset
            .RemoveControlScheme("ControlSchemeToRemove"), "Remove control scheme");

        // Action maps
        AssertAssetIsUnmodifiedAfterExitingPlayMode(asset => asset
            .AddActionMap("NewActionMap"), "Add action map");
        AssertAssetIsUnmodifiedAfterExitingPlayMode(asset => asset
            .RemoveActionMap("ActionMapToRemove"), "Remove action map");
        AssertAssetIsUnmodifiedAfterExitingPlayMode(asset => asset
            .FindActionMap("ActionMapToModify")
            .AddAction("NewAction"), "Add action");

        // Actions
        AssertAssetIsUnmodifiedAfterExitingPlayMode(asset => asset
            .FindActionMap("DefaultActionMap")
            .FindAction("DefaultAction")
            .RemoveAction(), "Remove action");
        AssertAssetIsUnmodifiedAfterExitingPlayMode(asset => asset
            .FindActionMap("DefaultActionMap")
            .FindAction("DefaultAction")
            .Rename("New Action"), "Modify action");

        // Bindings
        AssertAssetIsUnmodifiedAfterExitingPlayMode(asset => asset
            .FindActionMap("ActionMapToModify")
            .AddBinding("<Gamepad>/buttonNorth"), "Add new binding");

        AssertAssetIsUnmodifiedAfterExitingPlayMode(asset => asset
            .FindActionMap("DefaultActionMap")
            .FindAction("DefaultAction")
            .ApplyBindingOverride("<Gamepad>/buttonNorth"), "Modify binding");
    }

    private void AssertAssetIsUnmodifiedAfterExitingPlayMode(Action<InputActionAsset> action, string message = "")
    {
        var m_TestAssetPath = $"Assets/__TestInputAsset.{InputActionAsset.Extension}";

        var inputActionMap = new InputActionMap("DefaultActionMap");
        var inputAction = inputActionMap.AddAction("DefaultAction");
        inputAction.AddBinding("<Gamepad>/buttonSouth");

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(inputActionMap);

        asset.AddActionMap("ActionMapToRemove");
        asset.AddActionMap("ActionMapToModify");
        asset.AddControlScheme("ControlSchemeToRemove");

        File.WriteAllText(m_TestAssetPath, asset.ToJson());
        AssetDatabase.ImportAsset(m_TestAssetPath);
        asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(m_TestAssetPath);
        var originalJson = asset.ToJson();
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var assetGuid, out long _);

        // Enter play mode.
        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingEditMode);
        InputSystem.OnPlayModeChange(PlayModeStateChange.EnteredPlayMode);

        asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(m_TestAssetPath);
        action?.Invoke(asset);

        // Exit play mode.
        InputSystem.OnPlayModeChange(PlayModeStateChange.ExitingPlayMode);
        InputSystem.OnPlayModeChange(PlayModeStateChange.EnteredEditMode);

        var actualAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(m_TestAssetPath);
        Assert.That(actualAsset.ToJson(), Is.EqualTo(originalJson), message);
    }

#if UNITY_STANDALONE // CodeDom API not available in most players.
#if !TEMP_DISABLE_EDITOR_TESTS_ON_TRUNK // Temporary: Disables tests while net-profile passed from UTR to trunk is overridden to netstandard (missing CodeDom)
    [Test]
    [Category("Editor")]
    [TestCase("Mouse", typeof(Mouse))]
    [TestCase("Pen", typeof(Pen))]
    [TestCase("Keyboard", typeof(Keyboard))]
    [TestCase("Gamepad", typeof(Gamepad))]
    [TestCase("Touchscreen", typeof(Touchscreen))]
    [TestCase("DualShock4GamepadHID", typeof(DualShock4GamepadHID))]
    public void Editor_CanGenerateCodeForInputDeviceLayout(string layoutName, Type deviceType)
    {
        var code = InputLayoutCodeGenerator.GenerateCodeForDeviceLayout(layoutName, "FIRST", @namespace: "TestNamespace");

        var type = Compile(code,  "TestNamespace.Fast" + deviceType.Name, options: "-define:FIRST -define:UNITY_EDITOR");
        var device = (InputDevice)Activator.CreateInstance(type);

        Assert.That(device, Is.InstanceOf(deviceType));

        // Compare the mouse to one created by InputDeviceBuilder.
        var original = InputDevice.Build<InputDevice>(layoutName, noPrecompiledLayouts: true);

        Assert.That(device.name, Is.EqualTo(original.name));
        Assert.That(device.displayName, Is.EqualTo(original.displayName));
        Assert.That(device.shortDisplayName, Is.EqualTo(original.shortDisplayName));
        Assert.That(device.layout, Is.EqualTo(original.layout));
        Assert.That(device.noisy, Is.EqualTo(original.noisy));
        Assert.That(device.synthetic, Is.False);
        Assert.That(device.isSetupFinished, Is.True);
        Assert.That(device.stateBlock.format, Is.EqualTo(original.stateBlock.format));
        Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(original.stateBlock.sizeInBits));
        Assert.That(device.usages, Is.EquivalentTo(original.usages));
        Assert.That(device.aliases, Is.EquivalentTo(original.aliases));
        Assert.That(device.parent, Is.Null);
        Assert.That(device.device, Is.SameAs(device));
        Assert.That(device.children.Select(x => x.path), Is.EquivalentTo(original.children.Select(x => x.path)));
        Assert.That(device.hasControlsWithDefaultState, Is.EqualTo(original.hasControlsWithDefaultState));
        Assert.That(device.hasDontResetControls, Is.EqualTo(original.hasDontResetControls));

        Assert.That(device.allControls.Count, Is.EqualTo(original.allControls.Count));
        Assert.That(device.allControls.Select(x => x.name), Is.EquivalentTo(original.allControls.Select(x => x.name)));
        Assert.That(device.allControls.Select(x => x.displayName), Is.EquivalentTo(original.allControls.Select(x => x.displayName)));
        Assert.That(device.allControls.Select(x => x.shortDisplayName), Is.EquivalentTo(original.allControls.Select(x => x.shortDisplayName)));
        Assert.That(device.allControls.Select(x => x.path), Is.EquivalentTo(original.allControls.Select(x => x.path)));
        Assert.That(device.allControls.Select(x => x.parent.path), Is.EquivalentTo(original.allControls.Select(x => x.parent.path)));
        Assert.That(device.allControls.Select(x => x.parent != device ? x.parent.GetType() : null),
            Is.EquivalentTo(original.allControls.Select(x => x.parent != original ? x.parent.GetType() : null)));
        Assert.That(device.allControls.SelectMany(x => x.children.Select(c => c.path)),
            Is.EquivalentTo(original.allControls.SelectMany(x => x.children.Select(c => c.path))));
        Assert.That(device.allControls.Select(x => x.stateBlock.format), Is.EquivalentTo(original.allControls.Select(x => x.stateBlock.format)));
        Assert.That(device.allControls.Select(x => x.stateBlock.sizeInBits), Is.EquivalentTo(original.allControls.Select(x => x.stateBlock.sizeInBits)));
        Assert.That(device.allControls.Select(x => x.stateBlock.byteOffset), Is.EquivalentTo(original.allControls.Select(x => x.stateBlock.byteOffset)));
        Assert.That(device.allControls.Select(x => x.stateBlock.bitOffset), Is.EquivalentTo(original.allControls.Select(x => x.stateBlock.bitOffset)));
        Assert.That(device.allControls.Select(x => x.noisy), Is.EquivalentTo(original.allControls.Select(x => x.noisy)));
        Assert.That(device.allControls.Select(x => x.synthetic), Is.EquivalentTo(original.allControls.Select(x => x.synthetic)));
        Assert.That(device.allControls.Select(x => x.isSetupFinished), Is.EquivalentTo(original.allControls.Select(x => x.isSetupFinished)));
        Assert.That(device.allControls.Select(x => x.usages), Is.EquivalentTo(original.allControls.Select(x => x.usages)));
        Assert.That(device.allControls.Select(x => x.aliases), Is.EquivalentTo(original.allControls.Select(x => x.aliases)));
        Assert.That(device.allControls.Select(x => x.dontReset), Is.EquivalentTo(original.allControls.Select(x => x.dontReset)));

        // Check that all InputControl getters were initialized correctly.
        Assert.That(
            device.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x =>
                typeof(InputControl).IsAssignableFrom(x.PropertyType) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0 &&
                x.Name != "device" && x.Name != "parent")
                .Select(x => ((InputControl)(x.GetValue(device)))?.path),
            Is.EqualTo(original.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x =>
                typeof(InputControl).IsAssignableFrom(x.PropertyType) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0 &&
                x.Name != "device" && x.Name != "parent")
                .Select(x => ((InputControl)(x.GetValue(original)))?.path)));
        Assert.That(
            device.allControls.Select(c => c.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x =>
                typeof(InputControl).IsAssignableFrom(x.PropertyType) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0 &&
                x.Name != "device" && x.Name != "parent")
                .Select(x => ((InputControl)(x.GetValue(c)))?.path)),
            Is.EqualTo(original.allControls.Select(c => c.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x =>
                typeof(InputControl).IsAssignableFrom(x.PropertyType) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0 &&
                x.Name != "device" && x.Name != "parent")
                .Select(x => ((InputControl)(x.GetValue(c)))?.path))));

        // Check children array of each control.
        Assert.That(
            device.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x =>
                x.PropertyType.IsArray && typeof(InputControl).IsAssignableFrom(x.PropertyType.GetElementType()) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0)
                .Select(x => ((Array)(x.GetValue(device)))?.Length),
            Is.EqualTo(original.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x =>
                x.PropertyType.IsArray && typeof(InputControl).IsAssignableFrom(x.PropertyType.GetElementType()) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0)
                .Select(x => ((Array)(x.GetValue(original)))?.Length)));
        Assert.That(
            device.allControls.Select(c => GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x =>
                x.PropertyType.IsArray && typeof(InputControl).IsAssignableFrom(x.PropertyType.GetElementType()) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0)
                .Select(x => ((Array)(x.GetValue(c)))?.Length)),
            Is.EqualTo(original.allControls.Select(c => c.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x =>
                x.PropertyType.IsArray && typeof(InputControl).IsAssignableFrom(x.PropertyType.GetElementType()) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0)
                .Select(x => ((Array)(x.GetValue(c)))?.Length))));
        Assert.That(
            device.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x =>
                x.PropertyType.IsArray && typeof(InputControl).IsAssignableFrom(x.PropertyType.GetElementType()) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0)
                .Select(x => ((InputControl[])(x.GetValue(device))).Select(v => v?.path)),
            Is.EqualTo(original.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x =>
                x.PropertyType.IsArray && typeof(InputControl).IsAssignableFrom(x.PropertyType.GetElementType()) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0)
                .Select(x => ((InputControl[])(x.GetValue(original))).Select(v => v?.path))));
        Assert.That(
            device.allControls.Select(c => GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x =>
                x.PropertyType.IsArray && typeof(InputControl).IsAssignableFrom(x.PropertyType.GetElementType()) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0)
                .Select(x => ((InputControl[])(x.GetValue(c)))?.Select(v => v?.path))),
            Is.EqualTo(original.allControls.Select(c => GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x =>
                x.PropertyType.IsArray && typeof(InputControl).IsAssignableFrom(x.PropertyType.GetElementType()) && x.CanRead && x.GetIndexParameters().LengthSafe() == 0)
                .Select(x => ((InputControl[])(x.GetValue(c)))?.Select(v => v?.path)))));

        // Check processors on each control.
        Assert.That(
            device.allControls.Select(c => c.GetProcessors()).Where(l => l.Count() != 0).SelectMany(l => l.Select(p => p.ToString())),
            Is.EquivalentTo(original.allControls.Select(c => c.GetProcessors()).Where(l => l.Count() != 0)
                .SelectMany(l => l.Select(p => p.ToString()))));

        // Check AxisControl parameters (assumption here is that if those parameters are correct, parameters
        // on other control types are correct, too).
        Assert.That(
            device.allControls.OfType<AxisControl>().Select(c =>
                $"{c.clamp},{c.clampMin},{c.clampMax},{c.clampConstant},{c.invert},{c.normalize},{c.normalizeMin},{c.normalizeMax},{c.normalizeZero},{c.scale},{c.scaleFactor}"),
            Is.EquivalentTo(
                original.allControls.OfType<AxisControl>().Select(c =>
                    $"{c.clamp},{c.clampMin},{c.clampMax},{c.clampConstant},{c.invert},{c.normalize},{c.normalizeMin},{c.normalizeMax},{c.normalizeZero},{c.scale},{c.scaleFactor}")));

        // Check min and max values.
        Assert.That(device.allControls.Select(c => $"{c.m_MinValue},{c.m_MaxValue}"),
            Is.EquivalentTo(original.allControls.Select(c => $"{c.m_MinValue},{c.m_MaxValue}")));

        // Check that all KeyControls have key codes.
        Assert.That(device.allControls.OfType<KeyControl>().Select(x => x.keyCode),
            Is.EquivalentTo(original.allControls.OfType<KeyControl>().Select(x => x.keyCode)));

        // Check that all DpadAxisControls have "component" indices.
        Assert.That(device.allControls.OfType<DpadControl.DpadAxisControl>().Select(x => x.component),
            Is.EquivalentTo(original.allControls.OfType<DpadControl.DpadAxisControl>().Select(x => x.component)));

        // Add both the original and the code-generated device.
        InputSystem.AddDevice(original);
        InputSystem.AddDevice(device);

        // Press every non-synthetic button on the device.
        var allNonSyntheticButtons = device.allControls.OfType<ButtonControl>().Where(x => !x.synthetic).ToList();
        foreach (var button in allNonSyntheticButtons)
        {
            using (StateEvent.FromDefaultStateFor(device, out var eventPtr))
            {
                unsafe
                {
                    var statePtr = StateEvent.From(eventPtr)->stateData;
                    try
                    {
                        button.WriteValueIntoEvent(1f, eventPtr);
                    }
                    // Some controls don't support writing.
                    catch (NotSupportedException)
                    {
                        continue;
                    }

                    // Make sure it leads to only a single button being pressed in the event.
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    Assert.That(allNonSyntheticButtons.Where(b => b.ReadValueFromEvent(eventPtr) != 0f),
                        Is.EquivalentTo(new[] { button }));
                }

                InputSystem.QueueEvent(eventPtr);
                InputSystem.Update();

                // Make sure the button is pressed and is the only one on the device.
                Assert.That(allNonSyntheticButtons.Where(b => b.isPressed),
                    Is.EquivalentTo(new[] { button }));
            }
        }
    }

    internal static Type Compile(string code, string typeName, string options = null)
    {
        var codeProvider = CodeDomProvider.CreateProvider("CSharp");
        var cp = new CompilerParameters();
        cp.CompilerOptions = options;
        cp.ReferencedAssemblies.Add($"{EditorApplication.applicationContentsPath}/Managed/UnityEngine/UnityEngine.CoreModule.dll");
        cp.ReferencedAssemblies.Add("Library/ScriptAssemblies/Unity.InputSystem.dll");
        var cr = codeProvider.CompileAssemblyFromSource(cp, code);
        Assert.That(cr.Errors, Is.Empty);
        var assembly = cr.CompiledAssembly;
        Assert.That(assembly, Is.Not.Null);
        var type = assembly.GetType(typeName);
        Assert.That(type, Is.Not.Null);
        return type;
    }

#endif
#endif

    [Test]
    [Category("Editor")]
    public void Editor_CanRestartEditorThroughReflection()
    {
        EditorHelpers.RestartEditorAndRecompileScripts(dryRun: true);
    }

    [Test]
    [Category("Editor")]
    public void Editor_AfterUpdateCallbackIsNotCalledDuringEditorUpdates()
    {
        var receivedCalls = 0;
        InputSystem.onAfterUpdate += () => ++ receivedCalls;

        InputSystem.Update(InputUpdateType.Editor);

        Assert.That(receivedCalls, Is.Zero);
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputControlPicker_TouchscreenPickerContainsSingleAndMultiTouchControls()
    {
        var dropdown = new StubInputControlPickerDropdown(new InputControlPickerState(), _ => {});
        var root = dropdown.BuildRoot();

        Assert.That(() =>
        {
            var touchscreen = root.children.FirstOrDefault(c => c.name == "Touchscreen");
            if (touchscreen == null)
                return false;

            return touchscreen.children.Any(c => c.name == "Press (Single touch)") &&
            touchscreen.children.Any(c => c.name == "Press (Multi-touch)");
        });
    }

    internal class StubInputControlPickerDropdown : InputControlPickerDropdown
    {
        public StubInputControlPickerDropdown(InputControlPickerState state, Action<string> onPickCallback,
                                              InputControlPicker.Mode mode = InputControlPicker.Mode.PickControl)
            : base(state, onPickCallback, mode)
        {
        }

        #pragma warning disable CS0114
        public UnityEngine.InputSystem.Editor.AdvancedDropdownItem BuildRoot()
        {
            return base.BuildRoot();
        }
    }

    ////TODO: tests for InputAssetImporter; for this we need C# mocks to be able to cut us off from the actual asset DB
}
#endif // UNITY_EDITOR
