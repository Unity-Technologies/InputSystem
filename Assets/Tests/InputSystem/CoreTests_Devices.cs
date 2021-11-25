using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Quaternion = UnityEngine.Quaternion;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

////TODO: set display name from product string only for layout builders (and give them control); for other layouts, let the layout dictate the display name

////TODO: test that device re-creation doesn't lose flags and such

partial class CoreTests
{
    [Test]
    [Category("Devices")]
    public void Devices_CanGetAllDevices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        Assert.That(InputSystem.devices, Is.EquivalentTo(new InputDevice[] { gamepad1, gamepad2, keyboard, mouse }));

        InputSystem.RemoveDevice(keyboard);

        Assert.That(InputSystem.devices, Is.EquivalentTo(new InputDevice[] { gamepad1, gamepad2, mouse }));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDevice_FromLayout()
    {
        var device = InputDevice.Build<InputDevice>("Gamepad");

        Assert.That(device, Is.TypeOf<Gamepad>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftStick"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDevice_WithNestedState()
    {
        InputSystem.RegisterLayout<CustomDevice>();
        var device = InputDevice.Build<CustomDevice>();

        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("button1"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDevice_FromLayoutMatchedByDeviceDescription()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""interface"" : ""AA|BB"",
                    ""product"" : ""Shtabble""
                }
            }
        ";

        InputSystem.RegisterLayout(json);

        var description = new InputDeviceDescription
        {
            interfaceName = "BB",
            product = "Shtabble"
        };

        var device = InputSystem.AddDevice(description);

        Assert.That(device.layout, Is.EqualTo("MyDevice"));
        Assert.That(device, Is.TypeOf<Gamepad>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDevice_AndGiveItACustomName()
    {
        var device1 = InputSystem.AddDevice<Gamepad>("TestGamepad");
        var device2 = InputSystem.AddDevice<Gamepad>("TestGamepad");

        Assert.That(device1.name, Is.EqualTo("TestGamepad"));
        Assert.That(device2.name, Is.EqualTo("TestGamepad1"));
    }

    ////TODO: add base score to matchers
    // Sometimes we don't want a device to be picked up by the input system. Forcing
    // it's layout to "None" tells the system that we don't want to instantiate a
    // layout for the device.
    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_CanSuppressCreationOfDevice()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void Devices_DeviceCreatedFromDeviceDescriptionStoresDescriptionOnDevice()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""Product""
                }
            }
        ";

        InputSystem.RegisterLayout(json);

        var description = new InputDeviceDescription
        {
            interfaceName = "Interface",
            product = "Product",
            manufacturer = "Manufacturer",
            deviceClass = "DeviceClass",
            version = "Version",
            serial = "Serial"
        };

        var device = InputSystem.AddDevice(description);

        Assert.That(device.description, Is.EqualTo(description));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDevice_FromLayoutVariant()
    {
        const string json = @"
            {
                ""name"" : ""TestDevice"",
                ""controls"" : [
                    { ""name"" : ""VariantAControl"", ""variants"" : ""A"", ""layout"" : ""Button"" },
                    { ""name"" : ""VariantBControl"", ""variants"" : ""B"", ""layout"" : ""Button"" },
                    { ""name"" : ""VariantCControl"", ""variants"" : ""C"", ""layout"" : ""Button"" },
                    { ""name"" : ""VariantABControl"", ""variants"" : ""A;B"", ""layout"" : ""Button"" },
                    { ""name"" : ""NoVariantControl"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var variantA = InputDevice.Build<InputDevice>(layoutName: "TestDevice", layoutVariants: "A");
        var variantB = InputDevice.Build<InputDevice>(layoutName: "TestDevice", layoutVariants: "B");
        var noVariant = InputDevice.Build<InputDevice>(layoutName: "TestDevice");

        Assert.That(variantA.TryGetChildControl("VariantAControl"), Is.Not.Null);
        Assert.That(variantA.TryGetChildControl("VariantBControl"), Is.Null);
        Assert.That(variantA.TryGetChildControl("VariantCControl"), Is.Null);
        Assert.That(variantA.TryGetChildControl("VariantABControl"), Is.Not.Null);
        Assert.That(variantA.TryGetChildControl("NoVariantControl"), Is.Not.Null);

        Assert.That(variantB.TryGetChildControl("VariantAControl"), Is.Null);
        Assert.That(variantB.TryGetChildControl("VariantBControl"), Is.Not.Null);
        Assert.That(variantB.TryGetChildControl("VariantCControl"), Is.Null);
        Assert.That(variantB.TryGetChildControl("VariantABControl"), Is.Not.Null);
        Assert.That(variantB.TryGetChildControl("NoVariantControl"), Is.Not.Null);

        Assert.That(noVariant.TryGetChildControl("VariantAControl"), Is.Null);
        Assert.That(noVariant.TryGetChildControl("VariantBControl"), Is.Null);
        Assert.That(noVariant.TryGetChildControl("VariantCControl"), Is.Null);
        Assert.That(noVariant.TryGetChildControl("VariantABControl"), Is.Null);
        Assert.That(noVariant.TryGetChildControl("NoVariantControl"), Is.Not.Null);
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingUsageOfDevice_SendsDeviceChangeNotification()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        InputDevice receivedDevice = null;
        InputDeviceChange? receivedDeviceChange = null;

        InputSystem.onDeviceChange +=
            (d, c) =>
        {
            receivedDevice = d;
            receivedDeviceChange = c;
        };

        InputSystem.SetDeviceUsage(device, CommonUsages.LeftHand);

        Assert.That(receivedDevice, Is.SameAs(device));
        Assert.That(receivedDeviceChange, Is.EqualTo(InputDeviceChange.UsageChanged));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDeviceByUsage()
    {
        InputSystem.AddDevice<Gamepad>();
        var device = InputSystem.AddDevice<Keyboard>();

        InputSystem.SetDeviceUsage(device, CommonUsages.LeftHand);

        using (var controls = InputSystem.FindControls("/{LeftHand}"))
        {
            Assert.That(controls, Has.Count.EqualTo(1));
            Assert.That(controls, Has.Exactly(1).SameAs(device));
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanSetUsagesOnDevices()
    {
        var device = InputSystem.AddDevice<Mouse>();

        InputSystem.AddDeviceUsage(device, "First");

        Assert.That(device.usages, Has.Count.EqualTo(1));
        Assert.That(device.usages[0], Is.EqualTo(new InternedString("First")));

        InputSystem.AddDeviceUsage(device, "second");

        Assert.That(device.usages, Has.Count.EqualTo(2));
        Assert.That(device.usages[0], Is.EqualTo(new InternedString("First")));
        Assert.That(device.usages[1], Is.EqualTo(new InternedString("Second")));

        InputSystem.RemoveDeviceUsage(device, "First");

        Assert.That(device.usages, Has.Count.EqualTo(1));
        Assert.That(device.usages[0], Is.EqualTo(new InternedString("Second")));

        InputSystem.AddDeviceUsage(device, "Third");
        InputSystem.SetDeviceUsage(device, "Fourth");

        Assert.That(device.usages, Has.Count.EqualTo(1));
        Assert.That(device.usages[0], Is.EqualTo(new InternedString("Fourth")));

        InputSystem.SetDeviceUsage(device, null);

        Assert.That(device.usages, Is.Empty);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDeviceByMultipleUsages()
    {
        InputSystem.AddDevice<Gamepad>();
        var device = InputSystem.AddDevice<Gamepad>();

        InputSystem.SetDeviceUsage(device, CommonUsages.LeftHand);
        InputSystem.AddDeviceUsage(device, CommonUsages.Vertical);

        // Device should be found even if the one of the usages is specified
        using (var controls = InputSystem.FindControls("/{LeftHand}"))
        {
            Assert.That(controls, Has.Count.EqualTo(1));
            Assert.That(controls, Has.Exactly(1).SameAs(device));
        }

        using (var controls = InputSystem.FindControls("/{Vertical}"))
        {
            Assert.That(controls, Has.Count.EqualTo(1));
            Assert.That(controls, Has.Exactly(1).SameAs(device));
        }

        // And with both of the usages
        using (var controls = InputSystem.FindControls("/{LeftHand}{Vertical}"))
        {
            Assert.That(controls, Has.Count.EqualTo(1));
            Assert.That(controls, Has.Exactly(1).SameAs(device));
        }

        // Even with any order of usages
        using (var controls = InputSystem.FindControls("/{Vertical}{LeftHand}"))
        {
            Assert.That(controls, Has.Count.EqualTo(1));
            Assert.That(controls, Has.Exactly(1).SameAs(device));
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDeviceByUsageAndLayout()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.SetDeviceUsage(gamepad, CommonUsages.LeftHand);

        var keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.SetDeviceUsage(keyboard, CommonUsages.LeftHand);

        using (var controls = InputSystem.FindControls("/<Keyboard>{LeftHand}"))
        {
            Assert.That(controls, Has.Count.EqualTo(1));
            Assert.That(controls, Has.Exactly(1).SameAs(keyboard));
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAddDeviceFromLayout()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAddDeviceFromLayout_LookedUpFromType()
    {
        // Register layout with name different from name of type
        // so that trying to find the layout using the type name
        // would fail.
        InputSystem.RegisterLayout<CustomDevice>("MyDevice");

        var device = InputSystem.AddDevice<CustomDevice>();

        Assert.That(device, Is.TypeOf<CustomDevice>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceTwiceIsIgnored()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        InputSystem.onDeviceChange +=
            (d, c) => Assert.Fail("Shouldn't send notification for duplicate adding of device.");

        InputSystem.AddDevice(device);

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAddAndRemoveDeviceChangeListeners()
    {
        var receivedCallCount1 = 0;
        InputDevice receivedDevice1 = null;
        InputDeviceChange? receiveDeviceChange1 = null;

        Action<InputDevice, InputDeviceChange> listener1 =
            (device, change) =>
        {
            ++receivedCallCount1;
            receivedDevice1 = device;
            receiveDeviceChange1 = change;
        };

        var receivedCallCount2 = 0;
        InputDevice receivedDevice2 = null;
        InputDeviceChange? receiveDeviceChange2 = null;

        Action<InputDevice, InputDeviceChange> listener2 =
            (device, change) =>
        {
            ++receivedCallCount2;
            receivedDevice2 = device;
            receiveDeviceChange2 = change;
        };

        InputSystem.onDeviceChange += listener1;
        InputSystem.onDeviceChange += listener2;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(receivedCallCount1, Is.EqualTo(1));
        Assert.That(receivedDevice1, Is.SameAs(gamepad));
        Assert.That(receiveDeviceChange1, Is.EqualTo(InputDeviceChange.Added));
        Assert.That(receivedCallCount2, Is.EqualTo(1));
        Assert.That(receivedDevice2, Is.SameAs(gamepad));
        Assert.That(receiveDeviceChange2, Is.EqualTo(InputDeviceChange.Added));

        receivedCallCount1 = 0;
        receivedDevice1 = null;
        receiveDeviceChange1 = null;
        receivedCallCount2 = 0;
        receivedDevice2 = null;
        receiveDeviceChange2 = null;

        // Remove one listener.
        InputSystem.onDeviceChange -= listener2;

        InputSystem.RemoveDevice(gamepad);

        Assert.That(receivedCallCount1, Is.EqualTo(1));
        Assert.That(receivedDevice1, Is.SameAs(gamepad));
        Assert.That(receiveDeviceChange1, Is.EqualTo(InputDeviceChange.Removed));
        Assert.That(receivedCallCount2, Is.Zero);
        Assert.That(receivedDevice2, Is.Null);
        Assert.That(receiveDeviceChange2, Is.Null);
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDevice_TriggersNotification()
    {
        var receivedCallCount = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receiveDeviceChange = null;

        InputSystem.onDeviceChange +=
            (device, change) =>
        {
            ++receivedCallCount;
            receivedDevice = device;
            receiveDeviceChange = change;
        };

        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(receivedCallCount, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad));
        Assert.That(receiveDeviceChange, Is.EqualTo(InputDeviceChange.Added));
    }

    ////TODO: refine this behavior such that this only occurs if the given device is the first of its kind
    [Test]
    [Category("Devices")]
    public void Devices_AddingDevice_MakesItCurrent()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(Gamepad.current, Is.SameAs(gamepad));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDevice_DoesNotCauseExistingDevicesToForgetTheirState()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.5f });
        InputSystem.Update();

        InputSystem.AddDevice("Keyboard");

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDevice_AffectsControlPaths()
    {
        InputSystem.AddDevice(
            "Gamepad"); // Add a gamepad so that when we add another, its name will have to get adjusted.

        var device = InputDevice.Build<Gamepad>();

        Assert.That(device.dpad.up.path, Is.EqualTo("/Gamepad/dpad/up"));

        InputSystem.AddDevice(device);

        Assert.That(device.dpad.up.path, Is.EqualTo("/Gamepad1/dpad/up"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDevice_MarksItAdded()
    {
        var device = InputDevice.Build<Gamepad>();

        Assert.That(device.added, Is.False);

        InputSystem.AddDevice(device);

        Assert.That(device.added, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceByType_IfTypeIsNotKnownAsLayout_AutomaticallyRegistersControlLayout()
    {
        Assert.That(() => InputSystem.AddDevice<TestDeviceWithDefaultState>(), Throws.Nothing);
        Assert.That(InputSystem.LoadLayout("TestDeviceWithDefaultState"), Is.Not.Null);
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDevice_SetsItIntoDefaultState()
    {
        var device = InputSystem.AddDevice<TestDeviceWithDefaultState>();

        Assert.That(device["control"].ReadValueAsObject(), Is.EqualTo(0.1234).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_AddingDevice_RequestsSyncFromDevice()
    {
        var deviceId = runtime.AllocateDeviceId();
        runtime.ReportNewInputDevice<Gamepad>(deviceId);

        var receivedSyncRequest = false;
        runtime.SetDeviceCommandCallback(deviceId,
            (id, command) =>
            {
                if (command->type == RequestSyncCommand.Type)
                {
                    Assert.That(receivedSyncRequest, Is.False);
                    Assert.That(id, Is.EqualTo(deviceId));
                    receivedSyncRequest = true;
                    return InputDeviceCommand.GenericSuccess;
                }
                return InputDeviceCommand.GenericFailure;
            });

        InputSystem.Update();

        Assert.That(receivedSyncRequest, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceThatUsesBeforeRenderUpdates_CausesBeforeRenderUpdatesToBeEnabled()
    {
        const string deviceJson = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterLayout(deviceJson);

        Assert.That(InputSystem.s_Manager.updateMask & InputUpdateType.BeforeRender, Is.EqualTo((InputUpdateType)0));

        InputSystem.AddDevice("CustomGamepad");

        Assert.That(InputSystem.s_Manager.updateMask & InputUpdateType.BeforeRender, Is.EqualTo(InputUpdateType.BeforeRender));
    }

    [Test]
    [Category("Devices")]
    public void Devices_RemovingLastDeviceThatUsesBeforeRenderUpdates_CausesBeforeRenderUpdatesToBeDisabled()
    {
        const string deviceJson = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterLayout(deviceJson);

        var device1 = InputSystem.AddDevice("CustomGamepad");
        var device2 = InputSystem.AddDevice("CustomGamepad");

        Assert.That(InputSystem.s_Manager.updateMask & InputUpdateType.BeforeRender, Is.EqualTo(InputUpdateType.BeforeRender));

        InputSystem.RemoveDevice(device1);

        Assert.That(InputSystem.s_Manager.updateMask & InputUpdateType.BeforeRender, Is.EqualTo(InputUpdateType.BeforeRender));

        InputSystem.RemoveDevice(device2);

        Assert.That(InputSystem.s_Manager.updateMask & InputUpdateType.BeforeRender, Is.EqualTo((InputUpdateType)0));
    }

    private class TestDeviceReceivingAddAndRemoveNotification : Mouse
    {
        public int addedCount;
        public int removedCount;

        protected override void OnAdded()
        {
            ++addedCount;
        }

        protected override void OnRemoved()
        {
            ++removedCount;
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingAndRemovingDevice_InvokesNotificationOnDeviceItself()
    {
        InputSystem.RegisterLayout<TestDeviceReceivingAddAndRemoveNotification>();

        var device = InputSystem.AddDevice<TestDeviceReceivingAddAndRemoveNotification>();

        Assert.That(device.addedCount, Is.EqualTo(1));
        Assert.That(device.removedCount, Is.Zero);

        InputSystem.RemoveDevice(device);

        Assert.That(device.addedCount, Is.EqualTo(1));
        Assert.That(device.removedCount, Is.EqualTo(1));
    }

    [Test]
    [Category("Devices")]
    public void Devices_UnsupportedDevices_AreAddedToList()
    {
        const string json = @"
            {
                ""interface"" : ""TestInterface"",
                ""product"" : ""TestProduct"",
                ""manufacturer"" : ""TestManufacturer""
            }
        ";

        runtime.ReportNewInputDevice(json);
        InputSystem.Update();

        var unsupportedDevices = new List<InputDeviceDescription>();
        var count = InputSystem.GetUnsupportedDevices(unsupportedDevices);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(unsupportedDevices.Count, Is.EqualTo(1));
        Assert.That(unsupportedDevices[0].interfaceName, Is.EqualTo("TestInterface"));
        Assert.That(unsupportedDevices[0].product, Is.EqualTo("TestProduct"));
        Assert.That(unsupportedDevices[0].manufacturer, Is.EqualTo("TestManufacturer"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_UnsupportedDevices_AreRemovedFromList_WhenMatchingLayoutIsAdded()
    {
        const string json = @"
            {
                ""interface"" : ""TestInterface"",
                ""product"" : ""TestProduct"",
                ""manufacturer"" : ""TestManufacturer""
            }
        ";

        runtime.ReportNewInputDevice(json);
        InputSystem.Update();

        InputSystem.RegisterLayout<TestLayoutType>(
            matches: new InputDeviceMatcher()
                .WithInterface("TestInterface"));

        var unsupportedDevices = new List<InputDeviceDescription>();
        var count = InputSystem.GetUnsupportedDevices(unsupportedDevices);

        Assert.That(count, Is.Zero);
        Assert.That(unsupportedDevices.Count, Is.Zero);
        Assert.That(InputSystem.devices.Count, Is.EqualTo(1));
        Assert.That(InputSystem.devices[0].description.interfaceName, Is.EqualTo("TestInterface"));
        Assert.That(InputSystem.devices[0].description.product, Is.EqualTo("TestProduct"));
        Assert.That(InputSystem.devices[0].description.manufacturer, Is.EqualTo("TestManufacturer"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanRestrictSetOfSupportedDevices()
    {
        // Add native devices.
        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            deviceClass = "Keyboard",
        });
        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            deviceClass = "Gamepad",
        });

        InputSystem.Update();

        var keyboardId = ((Keyboard)InputSystem.devices[0]).deviceId;
        var gamepadId = ((Gamepad)InputSystem.devices[1]).deviceId;

        // Add manually added device.
        var mouseId = InputSystem.AddDevice<Mouse>().deviceId;

        // We don't mandate that the system reuses the same device instances it had created before.
        // Makes our checks here a little contrived. Can't use just IsEquivalentTo() as the device
        // instances may change.

        Assert.That(InputSystem.devices, Has.Count.EqualTo(3));
        Assert.That(InputSystem.devices[0], Is.InstanceOf<Keyboard>());
        Assert.That(InputSystem.devices[0].deviceId, Is.EqualTo(keyboardId));
        Assert.That(InputSystem.devices[1], Is.InstanceOf<Gamepad>());
        Assert.That(InputSystem.devices[1].deviceId, Is.EqualTo(gamepadId));
        Assert.That(InputSystem.devices[2], Is.InstanceOf<Mouse>());
        Assert.That(InputSystem.devices[2].deviceId, Is.EqualTo(mouseId));

        bool? receivedSettingsChange = null;
        var receivedDeviceChanges = new List<InputDeviceChange>();
        var receivedDevices = new List<InputDevice>();

        InputSystem.onSettingsChange +=
            () =>
        {
            Assert.That(receivedSettingsChange, Is.Null);
            receivedSettingsChange = true;
        };

        InputSystem.onDeviceChange +=
            (device, change) =>
        {
            receivedDeviceChanges.Add(change);
            receivedDevices.Add(device);
        };

        // Restrict to just gamepads.
        InputSystem.settings.supportedDevices = new[] {"Gamepad"};

        // Keyboard should have been removed as it comes from the runtime. Mouse should have been
        // kept as it has been explicitly added in code. Gamepad should have been kept as it
        // is explicitly listed as supported.
        Assert.That(InputSystem.devices, Has.Count.EqualTo(2));
        Assert.That(InputSystem.devices[0], Is.InstanceOf<Gamepad>());
        Assert.That(InputSystem.devices[0].deviceId, Is.EqualTo(gamepadId));
        Assert.That(InputSystem.devices[1], Is.InstanceOf<Mouse>());
        Assert.That(InputSystem.devices[1].deviceId, Is.EqualTo(mouseId));
        Assert.That(receivedSettingsChange, Is.True);
        Assert.That(InputSystem.settings.supportedDevices, Is.EquivalentTo(new[] { "Gamepad" }));
        Assert.That(receivedDeviceChanges, Is.EquivalentTo(new[] {InputDeviceChange.Removed}));
        Assert.That(receivedDevices[0].deviceId, Is.EqualTo(keyboardId));

        receivedSettingsChange = null;
        receivedDevices.Clear();
        receivedDeviceChanges.Clear();

        // Switch set of supported devices to mouse&keyboard.
        InputSystem.settings.supportedDevices = new[] {"Keyboard", "Mouse"};

        // Keyboard should have been re-added. Gamepad should have been removed.
        Assert.That(InputSystem.devices, Has.Count.EqualTo(2));
        Assert.That(InputSystem.devices[0], Is.InstanceOf<Mouse>());
        Assert.That(InputSystem.devices[0].deviceId, Is.EqualTo(mouseId));
        Assert.That(InputSystem.devices[1], Is.InstanceOf<Keyboard>());
        Assert.That(InputSystem.devices[1].deviceId, Is.EqualTo(keyboardId));
        Assert.That(receivedSettingsChange, Is.True);
        Assert.That(InputSystem.settings.supportedDevices, Is.EquivalentTo(new[] { "Keyboard", "Mouse" }));
        Assert.That(receivedDeviceChanges, Is.EquivalentTo(new[] {InputDeviceChange.Added, InputDeviceChange.Removed}));
        Assert.That(receivedDevices[0].deviceId, Is.EqualTo(keyboardId));
        Assert.That(receivedDevices[1].deviceId, Is.EqualTo(gamepadId));

        receivedSettingsChange = null;
        receivedDevices.Clear();
        receivedDeviceChanges.Clear();

        // Setting to same value should result in no change.
        InputSystem.settings.supportedDevices = new[] {"Keyboard", "Mouse"};

        Assert.That(receivedSettingsChange, Is.Null);
        Assert.That(receivedDeviceChanges, Is.Empty);
        Assert.That(receivedDevices, Is.Empty);

        // Clearing should restore gamepad.
        InputSystem.settings.supportedDevices = new ReadOnlyArray<string>();

        // Keyboard should have been re-added. Gamepad should have been removed.
        Assert.That(InputSystem.devices, Has.Count.EqualTo(3));
        Assert.That(InputSystem.devices[0], Is.InstanceOf<Mouse>());
        Assert.That(InputSystem.devices[0].deviceId, Is.EqualTo(mouseId));
        Assert.That(InputSystem.devices[1], Is.InstanceOf<Keyboard>());
        Assert.That(InputSystem.devices[1].deviceId, Is.EqualTo(keyboardId));
        Assert.That(InputSystem.devices[2], Is.InstanceOf<Gamepad>());
        Assert.That(InputSystem.devices[2].deviceId, Is.EqualTo(gamepadId));

        Assert.That(receivedSettingsChange, Is.True);
        Assert.That(InputSystem.settings.supportedDevices, Is.Empty);
        Assert.That(receivedDeviceChanges, Is.EquivalentTo(new[] {InputDeviceChange.Added}));
        Assert.That(receivedDevices[0].deviceId, Is.EqualTo(gamepadId));
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsAllRecognizedDefaultsByDefault()
    {
        Assert.That(InputSystem.settings.supportedDevices, Is.Empty);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanTellIfDeviceHasNoisyControls()
    {
        const string layout = @"
            {
                ""name"" : ""TestDevice"",
                ""controls"" : [
                    { ""name"" : ""notNoisy"", ""layout"" : ""axis"" },
                    { ""name"" : ""noisy"", ""layout"" : ""axis"", ""noisy"" : true }
                ]
            }
        ";

        InputSystem.RegisterLayout(layout);
        var device = InputSystem.AddDevice("TestDevice");

        Assert.That(device.noisy, Is.True);
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_NoisyControlsAreToggledOffInNoiseMask()
    {
        InputSystem.AddDevice<Mouse>(); // Noise.

        const string layout = @"
            {
                ""name"" : ""TestDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""noisyControl"", ""layout"" : ""axis"", ""noisy"" : true }
                ]
            }
        ";

        InputSystem.RegisterLayout(layout);
        var device = (Gamepad)InputSystem.AddDevice("TestDevice");

        var noiseMaskPtr = (byte*)device.noiseMaskPtr;

        const int kNumButtons = 14; // Buttons without left and right trigger which aren't stored in the buttons field.

        // All the gamepads buttons should have the flag on as they aren't noise. However, the leftover
        // bits in the "buttons" field should be marked as noise as they are not actively used by any control.
        Assert.That(*(uint*)(noiseMaskPtr + device.stateBlock.byteOffset), Is.EqualTo((1 << kNumButtons) - 1));

        // The noisy control we added should be flagged as noise by having their bits off.
        Assert.That(*(uint*)(noiseMaskPtr + device["noisyControl"].stateBlock.byteOffset), Is.Zero);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByItsIdAfterItHasBeenAdded()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        Assert.That(InputSystem.GetDeviceById(device.deviceId), Is.SameAs(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByLayout()
    {
        var device = InputSystem.AddDevice("Gamepad", "test"); // Give name to make sure we're not matching by name.
        var result = InputSystem.GetDevice("Gamepad");

        Assert.That(result, Is.SameAs(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByType()
    {
        Assert.That(InputSystem.GetDevice<Keyboard>(), Is.Null);

        InputSystem.AddDevice<Keyboard>(); // Noise.
        var gamepad = InputSystem.AddDevice<DualShockGamepad>();

        Assert.That(InputSystem.GetDevice<Gamepad>(), Is.SameAs(gamepad));
        Assert.That(InputSystem.GetDevice<DualShockGamepad>(), Is.SameAs(gamepad));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByType_ReturnsLastActiveDevice()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        Assert.That(InputSystem.GetDevice<Gamepad>(), Is.SameAs(gamepad1));

        runtime.currentTime += 1;
        InputSystem.QueueStateEvent(gamepad2, new GamepadState()); // Any update will do, even if not changing anything.
        InputSystem.Update();

        Assert.That(InputSystem.GetDevice<Gamepad>(), Is.SameAs(gamepad2));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByTypeAndUsage()
    {
        var leftHand = InputSystem.AddDevice<Gamepad>();
        var rightHand = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Gamepad>(); // Noise.

        InputSystem.SetDeviceUsage(leftHand, CommonUsages.LeftHand);
        InputSystem.SetDeviceUsage(rightHand, CommonUsages.RightHand);

        Assert.That(InputSystem.GetDevice<Gamepad>(CommonUsages.LeftHand), Is.SameAs(leftHand));
        Assert.That(InputSystem.GetDevice<Gamepad>(CommonUsages.RightHand), Is.SameAs(rightHand));
    }

    [Test]
    [Category("Devices")]
    public void Devices_EnsuresDeviceNamesAreUnique()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad1.name, Is.Not.EqualTo(gamepad2.name));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AssignsUniqueNumericIdToDevices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad1.deviceId, Is.Not.EqualTo(gamepad2.deviceId));
    }

    [Test]
    [Category("Devices")]
    public void Devices_NameDefaultsToNameOfLayout()
    {
        var device = InputSystem.AddDevice<Mouse>();

        Assert.That(device.name, Is.EqualTo("Mouse"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_NameDefaultsToNameOfTemplate_AlsoWhenProductNameIsNotSupplied()
    {
        InputSystem.RegisterLayout(@"
            {
                ""name"" : ""TestTemplate"",
                ""device"" : { ""interface"" : ""TEST"" },
                ""controls"" : [
                    { ""name"" : ""button"", ""layout"" : ""Button"" }
                ]
            }
        ");

        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            interfaceName = "TEST",
        }.ToJson());
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Exactly(1).With.Property("name").EqualTo("TestTemplate"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingConfigurationOfDevice_TriggersNotification()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedCalls = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receivedDeviceChange = null;

        InputSystem.onDeviceChange +=
            (d, c) =>
        {
            ++receivedCalls;
            receivedDevice = d;
            receivedDeviceChange = c;
        };

        InputSystem.QueueConfigChangeEvent(gamepad);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad));
        Assert.That(receivedDeviceChange, Is.EqualTo(InputDeviceChange.ConfigurationChanged));
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingStateOfDevice_TriggersNotification()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedCalls = 0;
        InputDevice receivedDevice = default;
        InputEventPtr receivedEventPtr = default;

        InputState.onChange +=
            (d, e) =>
        {
            ++receivedCalls;
            receivedDevice = d;
            receivedEventPtr = e;
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0.5f) });
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad));
        Assert.That(receivedEventPtr.valid, Is.True);
        Assert.That(receivedEventPtr.deviceId, Is.EqualTo(gamepad.deviceId));
        Assert.That(receivedEventPtr.IsA<StateEvent>(), Is.True);
    }

    private class TestDeviceThatResetsStateInCallback : InputDevice, IInputStateCallbackReceiver
    {
        [InputControl(format = "FLT")]
        public ButtonControl button { get; private set; }

        protected override void FinishSetup()
        {
            button = GetChildControl<ButtonControl>("button");
            base.FinishSetup();
        }

        public void OnNextUpdate()
        {
            InputState.Change(button, 1);
        }

        public void OnStateEvent(InputEventPtr eventPtr)
        {
            InputState.Change(this, eventPtr);
        }

        public bool GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
        {
            return false;
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingStateOfDevice_InStateCallback_TriggersNotification()
    {
        InputSystem.RegisterLayout<TestDeviceThatResetsStateInCallback>();
        var device = InputSystem.AddDevice<TestDeviceThatResetsStateInCallback>();

        var receivedCalls = 0;
        InputDevice receivedDevice = default;
        InputEventPtr receivedEventPtr = default;

        InputState.onChange +=
            (d, e) =>
        {
            ++receivedCalls;
            receivedDevice = d;
            receivedEventPtr = e;
        };

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(device));
        Assert.That(receivedEventPtr.valid, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingStateOfDevice_MarksDeviceAsUpdatedThisFrame()
    {
        // If there hasn't been an update yet, the device has the same (default) update
        // count as the system and is thus considered updated. Given this case won't matter
        // in practice in the player and editor, we don't bother accounting for it.
        InputSystem.Update();

        var device = InputSystem.AddDevice<Gamepad>();

        Assert.That(device.wasUpdatedThisFrame, Is.False);

        InputSystem.QueueStateEvent(device, new GamepadState { rightTrigger = 0.5f });
        InputSystem.Update();

        Assert.That(device.wasUpdatedThisFrame, Is.True);

        InputSystem.Update();

        Assert.That(device.wasUpdatedThisFrame, Is.False);
    }

    private struct TestDevicePartialState : IInputStateTypeInfo
    {
        public float axis;

        public FourCC format => new FourCC("PART");
    }

    private unsafe struct TestDeviceFullState : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis", arraySize = 5)]
        public fixed float axis[5];

        public FourCC format => new FourCC("FULL");
    }

    [InputControlLayout(stateType = typeof(TestDeviceFullState))]
    private class TestDeviceIntegratingStateItself : InputDevice, IInputStateCallbackReceiver
    {
        public void OnNextUpdate()
        {
        }

        public unsafe void OnStateEvent(InputEventPtr eventPtr)
        {
            // Ignore anything but TestDevicePartialState events.
            if (eventPtr.stateFormat != new FourCC("PART"))
                return;

            Assert.That(eventPtr.stateSizeInBytes, Is.EqualTo(UnsafeUtility.SizeOf<TestDevicePartialState>()));

            var values = (float*)currentStatePtr;
            var newValue = (TestDevicePartialState*)StateEvent.From(eventPtr)->state;
            for (var i = 0; i < 5; ++i)
                if (Mathf.Approximately(values[i], 0))
                {
                    InputState.Change(this["axis" + i], newValue->axis, eventPtr: eventPtr);
                    return;
                }

            Assert.Fail();
        }

        public bool GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
        {
            return false;
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_DeviceWithStateCallback_IntegratesStateItself()
    {
        InputSystem.RegisterLayout<TestDeviceIntegratingStateItself>();
        var device = InputSystem.AddDevice<TestDeviceIntegratingStateItself>();

        InputSystem.QueueStateEvent(device, new TestDevicePartialState { axis = 0.123f });
        InputSystem.Update();

        Assert.That(device["axis0"].ReadValueAsObject(), Is.EqualTo(0.123).Within(0.00001));

        InputSystem.QueueStateEvent(device, new TestDevicePartialState { axis = 0.234f });
        InputSystem.Update();

        Assert.That(device["axis0"].ReadValueAsObject(), Is.EqualTo(0.123).Within(0.00001));
        Assert.That(device["axis1"].ReadValueAsObject(), Is.EqualTo(0.234).Within(0.00001));

        InputSystem.QueueStateEvent(device, new TestDeviceFullState());
        InputSystem.Update();

        Assert.That(device["axis0"].ReadValueAsObject(), Is.EqualTo(0.123).Within(0.00001));
        Assert.That(device["axis1"].ReadValueAsObject(), Is.EqualTo(0.234).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanReadStateOfDeviceAsByteArray()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(device, new GamepadState { leftStick = new Vector2(0.123f, 0.456f) });
        InputSystem.Update();

        var state = device.ReadValueAsObject();

        Assert.That(state, Is.TypeOf<byte[]>());
        var buffer = (byte[])state;

        Assert.That(buffer.Length, Is.EqualTo(Marshal.SizeOf(typeof(GamepadState))));

        unsafe
        {
            fixed(byte* bufferPtr = buffer)
            {
                var statePtr = (GamepadState*)bufferPtr;
                Assert.That(statePtr->leftStick.x, Is.EqualTo(0.123).Within(0.00001));
                Assert.That(statePtr->leftStick.y, Is.EqualTo(0.456).Within(0.00001));
            }
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAddLayoutForDeviceThatsAlreadyBeenReported()
    {
        runtime.ReportNewInputDevice(new InputDeviceDescription { product = "MyController" }.ToJson());
        InputSystem.Update();

        var json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""MyController""
                }
            }
        ";

        InputSystem.RegisterLayout(json);

        Assert.That(InputSystem.devices,
            Has.Exactly(1).With.Property("layout").EqualTo("CustomGamepad").And.TypeOf<Gamepad>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanMatchLayoutByDeviceClass()
    {
        runtime.ReportNewInputDevice(new InputDeviceDescription { deviceClass = "Touchscreen" }.ToJson());
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Exactly(1).InstanceOf<Touchscreen>());

        // Should not try to use a control layout.
        runtime.ReportNewInputDevice(new InputDeviceDescription { deviceClass = "Touch" }.ToJson());
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
    }

    // For some devices, we need to discover their setup at runtime and cannot create layouts
    // in advance. HID joysticks are one such case. We want to be able to turn any HID joystick
    // into a Joystick device and accurately represent all the axes and buttons the device
    // actually has. If we couldn't make up layouts on the fly, we would have to have a fallback
    // joystick layout that simply has N buttons and M axes.
    //
    // So, instead we have a callback that tells us when a device has been discovered. We can use
    // this callback to generate a layout on the fly.
    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_CanDetermineWhichLayoutIsChosenOnDeviceDiscovery()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeRemoved()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        var gamepad3 = InputSystem.AddDevice<Gamepad>();

        var gamepad2Offset = gamepad2.stateBlock.byteOffset;

        var receivedCalls = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receivedChange = null;

        InputSystem.onDeviceChange +=
            (device, change) =>
        {
            ++receivedCalls;
            receivedDevice = device;
            receivedChange = change;
        };

        InputSystem.RemoveDevice(gamepad2);

        Assert.That(gamepad2.added, Is.False);
        Assert.That(InputSystem.devices, Has.Count.EqualTo(2));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad1));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad3));
        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad2));
        Assert.That(receivedChange, Is.EqualTo(InputDeviceChange.Removed));
        Assert.That(gamepad2.stateBlock.byteOffset, Is.EqualTo(0)); // Should have lost its offset into state buffers.
        Assert.That(gamepad3.stateBlock.byteOffset,
            Is.EqualTo(gamepad2Offset)); // 3 should have moved into 2's position.
        Assert.That(gamepad2.leftStick.stateBlock.byteOffset,
            Is.EqualTo(Marshal.OffsetOf(typeof(GamepadState), "leftStick")
                .ToInt32())); // Should have unbaked offsets in control hierarchy.
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeRemoved_ThroughEvents()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var gamepad1WasRemoved = false;
        InputSystem.onDeviceChange +=
            (device, change) =>
        {
            if (device == gamepad1)
                gamepad1WasRemoved = true;
        };

        var inputEvent = DeviceRemoveEvent.Create(gamepad1.deviceId, runtime.currentTime);
        InputSystem.QueueEvent(ref inputEvent);
        InputSystem.Update();

        Assert.That(gamepad1.added, Is.False);
        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad2));
        Assert.That(Gamepad.current, Is.Not.SameAs(gamepad1));
        Assert.That(gamepad1WasRemoved, Is.True);
    }

    ////FIXME: for this to work reliably, a device must not retain one-time configuration and must be able
    ////       to respond to the descriptor getting updated
    [Test]
    [Category("Devices")]
    public void Devices_WhenRemovedThroughEvent_AndDeviceIsNative_DeviceIsMovedToDisconnectedDeviceList()
    {
        ////REVIEW: should the system mandate more info in the description in order to retain a device?
        var description =
            new InputDeviceDescription
        {
            deviceClass = "Gamepad"
        };

        var originalDeviceId = runtime.ReportNewInputDevice(description);
        InputSystem.AddDevice<Keyboard>(); // Noise.
        InputSystem.Update();
        var originalGamepad = (Gamepad)InputSystem.GetDeviceById(originalDeviceId);

        var receivedChanges = new List<KeyValuePair<InputDevice, InputDeviceChange>>();
        InputSystem.onDeviceChange +=
            (device, change) =>
        {
            receivedChanges.Add(new KeyValuePair<InputDevice, InputDeviceChange>(device, change));
        };

        var inputEvent = DeviceRemoveEvent.Create(originalGamepad.deviceId, runtime.currentTime);
        InputSystem.QueueEvent(ref inputEvent);
        InputSystem.Update();

        // Two notifications: first removed, then disconnect.
        Assert.That(receivedChanges, Has.Count.EqualTo(2));
        Assert.That(receivedChanges[0].Key, Is.SameAs(originalGamepad));
        Assert.That(receivedChanges[0].Value, Is.EqualTo(InputDeviceChange.Removed));
        Assert.That(receivedChanges[1].Key, Is.SameAs(originalGamepad));
        Assert.That(receivedChanges[1].Value, Is.EqualTo(InputDeviceChange.Disconnected));

        Assert.That(originalGamepad.added, Is.False);
        Assert.That(InputSystem.disconnectedDevices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.disconnectedDevices, Has.Exactly(1).SameAs(originalGamepad));

        receivedChanges.Clear();

        // Add it back.
        var newDeviceId = runtime.ReportNewInputDevice(description);
        InputSystem.Update();
        var newGamepad = (Gamepad)InputSystem.GetDeviceById(newDeviceId);

        Assert.That(newGamepad, Is.SameAs(originalGamepad));

        // Two notifications: first added, then reconnect.
        Assert.That(receivedChanges, Has.Count.EqualTo(2));
        Assert.That(receivedChanges[0].Key, Is.SameAs(originalGamepad));
        Assert.That(receivedChanges[0].Value, Is.EqualTo(InputDeviceChange.Added));
        Assert.That(receivedChanges[1].Key, Is.SameAs(originalGamepad));
        Assert.That(receivedChanges[1].Value, Is.EqualTo(InputDeviceChange.Reconnected));

        Assert.That(originalGamepad.added, Is.True);
        Assert.That(originalGamepad.deviceId, Is.EqualTo(newDeviceId));
        Assert.That(InputSystem.disconnectedDevices, Has.Count.Zero);
    }

    [Test]
    [Category("Devices")]
    public void Devices_WhenRetainedOnDisconnectedList_CanBePurgedManually()
    {
        var deviceId = runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                deviceClass = "Gamepad"
            });

        InputSystem.Update();
        var device = InputSystem.GetDeviceById(deviceId);

        Assert.That(device, Is.Not.Null);
        Assert.That(InputSystem.devices, Is.EquivalentTo(new[] { device }));
        Assert.That(InputSystem.disconnectedDevices, Is.Empty);

        var inputEvent = DeviceRemoveEvent.Create(deviceId, runtime.currentTime);
        InputSystem.QueueEvent(ref inputEvent);
        InputSystem.Update();

        Assert.That(InputSystem.devices, Is.Empty);
        Assert.That(InputSystem.disconnectedDevices, Is.EquivalentTo(new[] { device }));

        InputSystem.FlushDisconnectedDevices();

        Assert.That(InputSystem.devices, Is.Empty);
        Assert.That(InputSystem.disconnectedDevices, Is.Empty);
    }

    [Test]
    [Category("Devices")]
    [TestCase("Gamepad", "TestDevice")]
    [TestCase("Gamepad", "TestDevice", "caps")]
    [TestCase("Gamepad", "TestDevice", null, "1234")]
    [TestCase("Gamepad", "TestDevice", "caps", "1234")]
    public void Devices_WhenRetainedOnDisconnectedList_CanBeReconnected(string deviceClass, string product, string capabilities = null, string serial = null)
    {
        var deviceId = runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                deviceClass = deviceClass,
                product = product,
                capabilities = capabilities,
                serial = serial
            });

        InputSystem.Update();

        var device = InputSystem.GetDeviceById(deviceId);

        runtime.ReportInputDeviceRemoved(device);
        InputSystem.Update();

        Assert.That(InputSystem.devices, Is.Empty);
        Assert.That(InputSystem.disconnectedDevices, Is.EquivalentTo(new[] { device }));

        if (!string.IsNullOrEmpty(capabilities))
        {
            // Report device with different caps and make sure we don't reuse the instance from before.
            runtime.ReportNewInputDevice(
                new InputDeviceDescription
                {
                    deviceClass = deviceClass,
                    product = product,
                    capabilities = capabilities + "foo"
                });
            InputSystem.Update();

            Assert.That(InputSystem.disconnectedDevices, Is.EquivalentTo(new[] { device }));
            Assert.That(device.added, Is.False);
        }

        if (!string.IsNullOrEmpty(serial))
        {
            // Report device with different serial and make sure we don't reuse the instance from before.
            runtime.ReportNewInputDevice(
                new InputDeviceDescription
                {
                    deviceClass = deviceClass,
                    product = product,
                    capabilities = capabilities,
                    serial = serial + "foo"
                });
            InputSystem.Update();

            Assert.That(InputSystem.disconnectedDevices, Is.EquivalentTo(new[] { device }));
            Assert.That(device.added, Is.False);
        }

        // Bring the device back.
        var newDeviceId = runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                deviceClass = deviceClass,
                product = product,
                capabilities = capabilities,
                serial = serial
            });
        InputSystem.Update();

        Assert.That(newDeviceId, Is.Not.EqualTo(deviceId));
        Assert.That(device.added, Is.True);
        Assert.That(InputSystem.disconnectedDevices, Is.Empty);
    }

    [Test]
    [Category("Devices")]
    public void Devices_WhenRemoved_DoNotEmergeOnUnsupportedList()
    {
        // Devices added directly via AddDevice() don't end up on the list of
        // available devices. Devices reported by the runtime do.
        runtime.ReportNewInputDevice(@"
            {
                ""type"" : ""Gamepad""
            }
        ");

        InputSystem.Update();
        var device = InputSystem.devices[0];

        var inputEvent = DeviceRemoveEvent.Create(device.deviceId, runtime.currentTime);
        InputSystem.QueueEvent(ref inputEvent);
        InputSystem.Update();

        var unsupportedDevices = new List<InputDeviceDescription>();
        InputSystem.GetUnsupportedDevices(unsupportedDevices);

        ////TODO: also make sure that when the layout support it is removed, the device goes back on the unsupported list

        Assert.That(unsupportedDevices.Count, Is.Zero);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeReadded()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice("Keyboard");

        InputSystem.RemoveDevice(gamepad);
        InputSystem.AddDevice(gamepad);

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.5f });
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5f).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_WhenCreationFails_SystemRecoversGracefully()
    {
        // Create a device layout that will fail to instantiate.
        const string layout = @"
            {
                ""name"" : ""TestDevice"",
                ""controls"" : [
                    { ""name"" : ""test"", ""layout"" : ""DoesNotExist"" }
                ]
            }
        ";
        InputSystem.RegisterLayout(layout);

        // Report two devices, one that will fail creation and one that shouldn't.
        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            deviceClass = "TestDevice"
        }.ToJson());
        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            deviceClass = "Gamepad"
        }.ToJson());

        LogAssert.Expect(LogType.Error,
            new Regex(".*Could not create a device for 'TestDevice'.*Cannot find layout 'DoesNotExist'.*"));

        Assert.That(() => InputSystem.Update(), Throws.Nothing);

        // Make sure InputManager kept the gamepad.
        Assert.That(InputSystem.devices.Count, Is.EqualTo(1));
        Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<Gamepad>());

        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanBeDisabledAndReEnabled()
    {
        var device = InputSystem.AddDevice<Mouse>();
        var nativeBackendDisableWasCalled = false;
        var nativeBackendEnableWasCalled = false;

        bool? disabled = null;
        runtime.SetDeviceCommandCallback(device.deviceId,
            (id, commandPtr) =>
            {
                if (commandPtr->type == DisableDeviceCommand.Type)
                {
                    Assert.That(disabled, Is.Null);
                    disabled = true;
                    nativeBackendDisableWasCalled = true;
                    return InputDeviceCommand.GenericSuccess;
                }

                if (commandPtr->type == EnableDeviceCommand.Type)
                {
                    Assert.That(disabled, Is.Null);
                    disabled = false;
                    nativeBackendEnableWasCalled = true;
                    return InputDeviceCommand.GenericSuccess;
                }

                return InputDeviceCommand.GenericFailure;
            });

        Assert.That(device.enabled, Is.True);
        Assert.That(disabled, Is.Null);

        InputSystem.DisableDevice(device);

        Assert.That(device.enabled, Is.False);
        Assert.That(disabled.HasValue, Is.True);
        Assert.That(disabled.Value, Is.True);
        Assert.That(nativeBackendDisableWasCalled, Is.True);

        // Make sure that state sent against the device is ignored.
        InputSystem.QueueStateEvent(device, new MouseState { buttons = 0xffff });
        InputSystem.Update();

        Assert.That(device.CheckStateIsAtDefault(), Is.True);

        // Re-enable device.
        disabled = null;
        InputSystem.EnableDevice(device);

        Assert.That(device.enabled, Is.True);
        Assert.That(disabled.HasValue, Is.True);
        Assert.That(disabled.Value, Is.False);
        Assert.That(nativeBackendEnableWasCalled, Is.True);
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanBeDisabled_WhileLettingItKeepSendingEvents()
    {
        var device = InputSystem.AddDevice<Mouse>();

        runtime.SetDeviceCommandCallback(device.deviceId,
            (id, commandPtr) =>
            {
                if (commandPtr->type == DisableDeviceCommand.Type)
                    Assert.Fail("Device should not receive a DisableDeviceCommand");

                return InputDeviceCommand.GenericFailure;
            });

        InputSystem.DisableDevice(device, keepSendingEvents: true);

        Assert.That(device.enabled, Is.False);

        var receivedEvent = false;
        InputSystem.onEvent +=
            (eventPtr, d) =>
        {
            Assert.That(d, Is.SameAs(device));
            Assert.That(receivedEvent, Is.False);
            receivedEvent = true;
        };

        InputSystem.QueueStateEvent(device, new MouseState());
        InputSystem.Update();

        Assert.That(receivedEvent, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_WhenDisabledOrReEnabled_TriggersNotification()
    {
        InputDevice receivedDevice = null;
        InputDeviceChange? receivedChange = null;

        InputSystem.onDeviceChange +=
            (device, change) =>
        {
            receivedDevice = device;
            receivedChange = change;
        };

        var mouse = InputSystem.AddDevice<Mouse>();

        InputSystem.DisableDevice(mouse);

        Assert.That(receivedDevice, Is.SameAs(mouse));
        Assert.That(receivedChange.Value, Is.EqualTo(InputDeviceChange.Disabled));

        receivedDevice = null;
        receivedChange = null;

        InputSystem.EnableDevice(mouse);

        Assert.That(receivedDevice, Is.SameAs(mouse));
        Assert.That(receivedChange.Value, Is.EqualTo(InputDeviceChange.Enabled));
    }

    // When a device is reset, it won't receive any more state updates. So, if we don't reset it, actions tied to
    // the device may get stuck. Thus we reset devices when we disable them.
    [Test]
    [Category("Devices")]
    public void Devices_WhenDisabled_StateIsReset()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        Set(mouse.position, new Vector2(123, 234));
        Press(mouse.leftButton);

        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
        Assert.That(mouse.leftButton.ReadValue(), Is.EqualTo(1));
        Assert.That(mouse.rightButton.ReadValue(), Is.Zero);

        InputSystem.DisableDevice(mouse);

        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
        Assert.That(mouse.leftButton.ReadValue(), Is.Zero);
        Assert.That(mouse.rightButton.ReadValue(), Is.Zero);
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_WhenEnabled_StateIsSynced()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var receivedSyncRequest = false;
        runtime.SetDeviceCommandCallback(mouse,
            (id, command) =>
            {
                if (command->type == RequestSyncCommand.Type)
                {
                    Assert.That(receivedSyncRequest, Is.False);
                    receivedSyncRequest = true;
                    return InputDeviceCommand.GenericSuccess;
                }
                return InputDeviceCommand.GenericFailure;
            });

        InputSystem.DisableDevice(mouse);

        Assert.That(receivedSyncRequest, Is.False);

        InputSystem.EnableDevice(mouse);

        Assert.That(receivedSyncRequest, Is.True);
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanRequestSync()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var receivedSync = false;
        runtime.SetDeviceCommandCallback(gamepad2,
            (id, command) =>
            {
                if (command->type == RequestSyncCommand.Type)
                {
                    Assert.That(receivedSync, Is.False);
                    receivedSync = true;
                    return InputDeviceCommand.GenericSuccess;
                }

                return InputDeviceCommand.GenericFailure;
            });

        Assert.That(InputSystem.TrySyncDevice(gamepad1), Is.False);
        Assert.That(InputSystem.TrySyncDevice(gamepad2), Is.True);
        Assert.That(receivedSync, Is.True);
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanResetDevice()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var touch = InputSystem.AddDevice<Touchscreen>();
        var gyro = InputSystem.AddDevice<Gyroscope>();

        const string noisyGamepadJson = @"
            {
                ""name"" : ""NoisyGamepad"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""gyro"", ""noisy"" : true, ""layout"" : ""Quaternion"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(noisyGamepadJson);
        var noisyGamepad = (Gamepad)InputSystem.AddDevice("NoisyGamepad");

        var receivedGamepadReset = false;
        var receivedMouseReset = false;
        var receivedTouchReset = false;
        var receivedKeyboardReset = false;
        var receivedGyroReset = false;
        var receivedNoisyGamepadReset = false;

        long DeviceCallback(int deviceId, InputDeviceCommand* command, ref bool receivedResetCommand)
        {
            if (command->type == RequestResetCommand.Type)
            {
                Assert.That(receivedResetCommand, Is.False);
                receivedResetCommand = true;
                return InputDeviceCommand.GenericSuccess;
            }
            return InputDeviceCommand.GenericFailure;
        }

        runtime.SetDeviceCommandCallback(gamepad, (id, command) => DeviceCallback(id, command, ref receivedGamepadReset));
        runtime.SetDeviceCommandCallback(mouse, (id, command) => DeviceCallback(id, command, ref receivedMouseReset));
        runtime.SetDeviceCommandCallback(touch, (id, command) => DeviceCallback(id, command, ref receivedTouchReset));
        runtime.SetDeviceCommandCallback(keyboard, (id, command) => DeviceCallback(id, command, ref receivedKeyboardReset));
        runtime.SetDeviceCommandCallback(gyro, (id, command) => DeviceCallback(id, command, ref receivedGyroReset));
        runtime.SetDeviceCommandCallback(noisyGamepad, (id, command) => DeviceCallback(id, command, ref receivedNoisyGamepadReset));

        Set(mouse.position, new Vector2(123, 234), queueEventOnly: true);
        Set(mouse.scroll, new Vector2(9, 8), queueEventOnly: true);
        Press(mouse.leftButton, queueEventOnly: true);
        Set(gamepad.leftStick, new Vector2(0.123f, 0.234f), queueEventOnly: true);
        Set(gamepad.leftTrigger, 0.75f, queueEventOnly: true);
        Press(gamepad.buttonSouth, queueEventOnly: true);
        BeginTouch(1, new Vector2(234, 345), queueEventOnly: true);
        BeginTouch(2, new Vector2(345, 456), queueEventOnly: true);
        Press(keyboard.spaceKey, queueEventOnly: true);
        Set(gyro.angularVelocity, new Vector3(456, 567, 678), queueEventOnly: true);
        Set((QuaternionControl)noisyGamepad["gyro"], Quaternion.Euler(1, 2, 3), queueEventOnly: true);
        Press(noisyGamepad.buttonSouth, queueEventOnly: true);
        InputSystem.Update();

        InputDeviceChange? receivedResetDeviceChange = null;
        InputSystem.onDeviceChange += (d, c) =>
        {
            if (c == InputDeviceChange.SoftReset || c == InputDeviceChange.HardReset)
            {
                Assert.That(receivedResetDeviceChange, Is.Null);
                receivedResetDeviceChange = c;
            }
        };

        // "Soft" reset mouse.
        InputSystem.ResetDevice(mouse);

        Assert.That(receivedResetDeviceChange, Is.EqualTo(InputDeviceChange.SoftReset));
        Assert.That(receivedMouseReset, Is.False);
        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
        Assert.That(mouse.scroll.ReadValue(), Is.EqualTo(Vector2.zero));
        Assert.That(mouse.leftButton.ReadValue(), Is.Zero);

        receivedMouseReset = false;
        receivedResetDeviceChange = null;

        // "Hard" reset mouse.
        InputSystem.ResetDevice(mouse, alsoResetDontResetControls: true);

        Assert.That(receivedResetDeviceChange, Is.EqualTo(InputDeviceChange.HardReset));
        Assert.That(receivedMouseReset, Is.True);
        Assert.That(mouse.position.ReadValue(), Is.EqualTo(Vector2.zero));
        Assert.That(mouse.scroll.ReadValue(), Is.EqualTo(Vector2.zero));
        Assert.That(mouse.leftButton.ReadValue(), Is.Zero);

        receivedResetDeviceChange = null;

        // Reset keyboard. There's no dontReset controls on the device so there's no difference
        // between a soft and a hard reset.
        InputSystem.ResetDevice(keyboard);

        Assert.That(receivedResetDeviceChange, Is.EqualTo(InputDeviceChange.HardReset));
        Assert.That(receivedKeyboardReset, Is.True);
        Assert.That(keyboard.allControls, Has.All.Matches((InputControl control) => control.CheckStateIsAtDefault()));
        Assert.That(keyboard.allControls.OfType<KeyControl>(), Has.All.Matches((KeyControl key) => !key.isPressed));
        Assert.That(keyboard.CheckStateIsAtDefault(), Is.True);

        receivedResetDeviceChange = null;

        // Reset gamepad.
        InputSystem.ResetDevice(gamepad);

        Assert.That(receivedResetDeviceChange, Is.EqualTo(InputDeviceChange.HardReset));
        Assert.That(receivedGamepadReset, Is.True);
        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(Vector2.zero));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.Zero);
        Assert.That(gamepad.buttonSouth.ReadValue(), Is.Zero);

        receivedResetDeviceChange = null;

        // "Soft" reset noisy gamepad.
        InputSystem.ResetDevice(noisyGamepad);

        Assert.That(receivedResetDeviceChange, Is.EqualTo(InputDeviceChange.SoftReset));
        Assert.That(receivedNoisyGamepadReset, Is.False);
        Assert.That(noisyGamepad.buttonSouth.ReadValue(), Is.Zero);
        Assert.That(noisyGamepad["gyro"].ReadValueAsObject(), Is.EqualTo(Quaternion.Euler(1, 2, 3)));

        receivedResetDeviceChange = null;

        // "Hard" reset noisy gamepad.
        InputSystem.ResetDevice(noisyGamepad, alsoResetDontResetControls: true);

        Assert.That(receivedResetDeviceChange, Is.EqualTo(InputDeviceChange.HardReset));
        Assert.That(receivedNoisyGamepadReset, Is.True);
        Assert.That(noisyGamepad.buttonSouth.ReadValue(), Is.Zero);
        Assert.That(noisyGamepad["gyro"].ReadValueAsObject(), Is.EqualTo(default(Quaternion)));

        receivedResetDeviceChange = null;

        // "Soft" reset touch.
        InputSystem.ResetDevice(touch);

        Assert.That(receivedResetDeviceChange, Is.EqualTo(InputDeviceChange.SoftReset));
        Assert.That(receivedTouchReset, Is.False);
        Assert.That(touch.position.ReadValue(), Is.EqualTo(new Vector2(234, 345)));
        Assert.That(touch.primaryTouch.touchId.ReadValue(), Is.EqualTo(1));
        Assert.That(touch.primaryTouch.position.ReadValue(), Is.EqualTo(new Vector2(234, 345)));
        Assert.That(touch.primaryTouch.press.ReadValue(), Is.Zero);
        Assert.That(touch.primaryTouch.isInProgress, Is.False);
        Assert.That(touch.touches[0].touchId.ReadValue(), Is.EqualTo(1));
        Assert.That(touch.touches[0].position.ReadValue(), Is.EqualTo(new Vector2(234, 345)));
        Assert.That(touch.touches[0].press.ReadValue(), Is.Zero);
        Assert.That(touch.touches[0].isInProgress, Is.False);
        Assert.That(touch.touches[1].touchId.ReadValue(), Is.EqualTo(2));
        Assert.That(touch.touches[1].position.ReadValue(), Is.EqualTo(new Vector2(345, 456)));
        Assert.That(touch.touches[1].press.ReadValue(), Is.Zero);
        Assert.That(touch.touches[1].isInProgress, Is.False);

        receivedResetDeviceChange = null;

        // "Hard" reset touch.
        InputSystem.ResetDevice(touch, alsoResetDontResetControls: true);

        Assert.That(receivedResetDeviceChange, Is.EqualTo(InputDeviceChange.HardReset));
        Assert.That(receivedTouchReset, Is.True);
        Assert.That(touch.CheckStateIsAtDefault(), Is.True);
        Assert.That(touch.primaryTouch.touchId.ReadValue(), Is.Zero);
        Assert.That(touch.primaryTouch.position.ReadValue(), Is.EqualTo(Vector2.zero));
        Assert.That(touch.primaryTouch.press.ReadValue(), Is.Zero);
        Assert.That(touch.primaryTouch.isInProgress, Is.False);
        Assert.That(touch.touches[0].touchId.ReadValue(), Is.Zero);
        Assert.That(touch.touches[0].position.ReadValue(), Is.EqualTo(Vector2.zero));
        Assert.That(touch.touches[0].press.ReadValue(), Is.Zero);
        Assert.That(touch.touches[0].isInProgress, Is.False);
        Assert.That(touch.touches[1].touchId.ReadValue(), Is.Zero);
        Assert.That(touch.touches[1].position.ReadValue(), Is.EqualTo(Vector2.zero));
        Assert.That(touch.touches[1].press.ReadValue(), Is.Zero);
        Assert.That(touch.touches[1].isInProgress, Is.False);

        receivedResetDeviceChange = null;

        // "Soft" reset gyro.
        InputSystem.ResetDevice(gyro);

        Assert.That(receivedResetDeviceChange, Is.EqualTo(InputDeviceChange.SoftReset));
        Assert.That(receivedGyroReset, Is.False);
        Assert.That(gyro.CheckStateIsAtDefault(), Is.False);
        Assert.That(gyro.CheckStateIsAtDefaultIgnoringNoise(), Is.True);
        Assert.That(gyro.angularVelocity.ReadValue(), Is.EqualTo(new Vector3(456, 567, 678)));

        receivedResetDeviceChange = null;

        // "Hard" reset gyro.
        InputSystem.ResetDevice(gyro, alsoResetDontResetControls: true);

        Assert.That(receivedResetDeviceChange, Is.EqualTo(InputDeviceChange.HardReset));
        Assert.That(receivedGyroReset, Is.True);
        Assert.That(gyro.CheckStateIsAtDefault(), Is.True);
        Assert.That(gyro.CheckStateIsAtDefaultIgnoringNoise(), Is.True);
        Assert.That(gyro.angularVelocity.ReadValue(), Is.EqualTo(Vector3.zero));
    }

    class DeviceWithCustomReset : InputDevice, ICustomDeviceReset
    {
        [InputControl]
        public AxisControl axis { get; private set; }

        protected override void FinishSetup()
        {
            axis = GetChildControl<AxisControl>("axis");
        }

        public unsafe void Reset()
        {
            InputState.Change(axis, 1f);
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCustomizeResetOfDevice()
    {
        var device = InputSystem.AddDevice<DeviceWithCustomReset>();

        Set(device.axis, 0.45f);

        InputSystem.ResetDevice(device);

        Assert.That(device.axis.ReadValue(), Is.EqualTo(1f));

        InputSystem.ResetDevice(device, alsoResetDontResetControls: true);

        Assert.That(device.axis.ReadValue(), Is.Zero);
    }

    [Test]
    [Category("Devices")]
    public void Devices_WhenDeviceIsReset_AndResetsAreObservableStateChanges()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var changeMonitorTriggered = false;
        InputState.AddChangeMonitor(gamepad.buttonSouth,
            (control, d, arg3, arg4) => changeMonitorTriggered = true);

        Press(gamepad.buttonSouth);

        Assert.That(changeMonitorTriggered, Is.True);

        changeMonitorTriggered = false;

        InputSystem.ResetDevice(gamepad);

        Assert.That(changeMonitorTriggered, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_ThatHaveNoKnownLayout_AreDisabled()
    {
        var deviceId = runtime.AllocateDeviceId();
        runtime.ReportNewInputDevice(new InputDeviceDescription { deviceClass = "TestThing" }.ToJson(), deviceId);

        bool? wasDisabled = null;
        unsafe
        {
            runtime.SetDeviceCommandCallback(deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == DisableDeviceCommand.Type)
                    {
                        Assert.That(wasDisabled, Is.Null);
                        wasDisabled = true;
                        return InputDeviceCommand.GenericSuccess;
                    }

                    Assert.Fail("Should not get other IOCTLs");
                    return InputDeviceCommand.GenericFailure;
                });
        }

        InputSystem.Update();

        Assert.That(wasDisabled.HasValue);
        Assert.That(wasDisabled.Value, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_ThatHadNoKnownLayout_AreReEnabled_WhenLayoutBecomesKnown()
    {
        var deviceId = runtime.AllocateDeviceId();
        runtime.ReportNewInputDevice(new InputDeviceDescription { deviceClass = "TestThing" }.ToJson(), deviceId);
        InputSystem.Update();

        bool? wasEnabled = null;
        unsafe
        {
            runtime.SetDeviceCommandCallback(deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == EnableDeviceCommand.Type)
                    {
                        Assert.That(wasEnabled, Is.Null);
                        wasEnabled = true;
                        return InputDeviceCommand.GenericSuccess;
                    }

                    return InputDeviceCommand.GenericSuccess;
                });
        }

        InputSystem.RegisterLayout<Mouse>(matches: new InputDeviceMatcher().WithDeviceClass("TestThing"));

        Assert.That(wasEnabled.HasValue);
        Assert.That(wasEnabled.Value, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_QueryTheirEnabledStateFromRuntime()
    {
        var deviceId = runtime.AllocateDeviceId();

        var queryEnabledStateResult = false;
        bool? receivedQueryEnabledStateCommand = null;
        unsafe
        {
            runtime.SetDeviceCommandCallback(deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == QueryEnabledStateCommand.Type)
                    {
                        Assert.That(receivedQueryEnabledStateCommand, Is.Null);
                        receivedQueryEnabledStateCommand = true;
                        ((QueryEnabledStateCommand*)commandPtr)->isEnabled = queryEnabledStateResult;
                        return InputDeviceCommand.GenericSuccess;
                    }

                    Assert.Fail("Should not get other IOCTLs");
                    return InputDeviceCommand.GenericFailure;
                });
        }

        runtime.ReportNewInputDevice(new InputDeviceDescription { deviceClass = "Mouse" }.ToJson(), deviceId);
        InputSystem.Update();
        var device = InputSystem.devices.First(x => x.deviceId == deviceId);

        var isEnabled = device.enabled;

        Assert.That(isEnabled, Is.False);
        Assert.That(receivedQueryEnabledStateCommand, Is.Not.Null);
        Assert.That(receivedQueryEnabledStateCommand.Value, Is.True);

        receivedQueryEnabledStateCommand = null;
        queryEnabledStateResult = true;

        // A configuration change event should cause the cached state to become invalid
        // and thus cause InputDevice.enabled to issue another IOCTL.
        InputSystem.QueueConfigChangeEvent(device);
        InputSystem.Update();

        isEnabled = device.enabled;

        Assert.That(isEnabled, Is.True);
        Assert.That(receivedQueryEnabledStateCommand, Is.Not.Null);
        Assert.That(receivedQueryEnabledStateCommand.Value, Is.True);

        // Make sure that querying the state *again* does not lead to another IOCTL.

        receivedQueryEnabledStateCommand = null;

        isEnabled = device.enabled;

        Assert.That(isEnabled, Is.True);
        Assert.That(receivedQueryEnabledStateCommand, Is.Null);
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanMarkDeviceAsBeingAbleToRunInBackground()
    {
        var device1Id = runtime.ReportNewInputDevice<Gamepad>();
        var device2Id = runtime.ReportNewInputDevice<Mouse>();

        var receivedCanRunInBackgroundForDevice1 = false;
        var receivedCanRunInBackgroundForDevice2 = false;

        runtime.SetDeviceCommandCallback(device1Id,
            (id, command) =>
            {
                if (command->type != QueryCanRunInBackground.Type)
                    return InputDeviceCommand.GenericFailure;

                Assert.That(id, Is.EqualTo(device1Id));
                Assert.That(command->payloadSizeInBytes, Is.EqualTo(UnsafeUtility.SizeOf<bool>()));

                receivedCanRunInBackgroundForDevice1 = true;
                ((QueryCanRunInBackground*)command)->canRunInBackground = true;
                return InputDeviceCommand.GenericSuccess;
            });
        runtime.SetDeviceCommandCallback(device2Id,
            (id, command) =>
            {
                if (command->type != QueryCanRunInBackground.Type)
                    return InputDeviceCommand.GenericFailure;

                Assert.That(id, Is.EqualTo(device2Id));
                Assert.That(command->payloadSizeInBytes, Is.EqualTo(UnsafeUtility.SizeOf<bool>()));

                receivedCanRunInBackgroundForDevice2 = true;
                ((QueryCanRunInBackground*)command)->canRunInBackground = false;
                return InputDeviceCommand.GenericSuccess;
            });

        InputSystem.Update();

        // Just adding the device should not have triggered a query for canRunInBackground.
        Assert.That(receivedCanRunInBackgroundForDevice1, Is.False);
        Assert.That(receivedCanRunInBackgroundForDevice2, Is.False);

        var device1 = InputSystem.GetDeviceById(device1Id);
        var device2 = InputSystem.GetDeviceById(device2Id);

        var device1CanRunInBackground = device1.canRunInBackground;
        var device2CanRunInBackground = device2.canRunInBackground;

        Assert.That(receivedCanRunInBackgroundForDevice1, Is.True);
        Assert.That(receivedCanRunInBackgroundForDevice2, Is.True);
        Assert.That(device1CanRunInBackground, Is.True);
        Assert.That(device2CanRunInBackground, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_NativeDevicesAreFlaggedAsSuch()
    {
        var description = new InputDeviceDescription { deviceClass = "Gamepad" };
        var deviceId = runtime.ReportNewInputDevice(description.ToJson());

        InputSystem.Update();

        var device = InputSystem.GetDeviceById(deviceId);

        Assert.That(device, Is.Not.Null);
        Assert.That(device.native, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_DisplayNameDefaultsToProductName()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            deviceClass = "Gamepad",
            product = "Product Name"
        });

        Assert.That(device.displayName, Is.EqualTo("Product Name"));
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanPauseResumeAndResetHapticsOnAllDevices()
    {
        InputSystem.AddDevice<Gamepad>();
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Keyboard>();

        gamepad.SetMotorSpeeds(0.1234f, 0.5678f);

        DualMotorRumbleCommand? receivedCommand = null;
        runtime.SetDeviceCommandCallback(gamepad.deviceId,
            (deviceId, command) =>
            {
                if (command->type == DualMotorRumbleCommand.Type)
                {
                    Assert.That(receivedCommand.HasValue, Is.False);
                    receivedCommand = *((DualMotorRumbleCommand*)command);
                    return 1;
                }

                Assert.Fail();
                return InputDeviceCommand.GenericFailure;
            });

        InputSystem.PauseHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.Zero.Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.Zero.Within(0.000001));

        receivedCommand = null;
        InputSystem.ResumeHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.EqualTo(0.1234).Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.EqualTo(0.5678).Within(0.000001));

        receivedCommand = null;
        InputSystem.ResetHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.Zero.Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.Zero.Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanRumbleGamepad()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        DualMotorRumbleCommand? receivedCommand = null;
        runtime.SetDeviceCommandCallback(gamepad.deviceId,
            (deviceId, command) =>
            {
                if (command->type == DualMotorRumbleCommand.Type)
                {
                    Assert.That(receivedCommand.HasValue, Is.False);
                    receivedCommand = *((DualMotorRumbleCommand*)command);
                    return 1;
                }

                Assert.Fail();
                return InputDeviceCommand.GenericFailure;
            });

        gamepad.SetMotorSpeeds(0.1234f, 0.5678f);

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.EqualTo(0.1234).Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.EqualTo(0.5678).Within(0.000001));

        receivedCommand = null;
        gamepad.PauseHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.Zero.Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.Zero.Within(0.000001));

        receivedCommand = null;
        gamepad.ResumeHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.EqualTo(0.1234).Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.EqualTo(0.5678).Within(0.000001));

        receivedCommand = null;
        gamepad.ResetHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.Zero.Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.Zero.Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanQueryAllGamepadsWithSimpleGetter()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Keyboard>();

        Assert.That(Gamepad.all, Has.Count.EqualTo(2));
        Assert.That(Gamepad.all, Has.Exactly(1).SameAs(gamepad1));
        Assert.That(Gamepad.all, Has.Exactly(1).SameAs(gamepad2));

        var gamepad3 = InputSystem.AddDevice<Gamepad>();

        Assert.That(Gamepad.all, Has.Count.EqualTo(3));
        Assert.That(Gamepad.all, Has.Exactly(1).SameAs(gamepad3));

        InputSystem.RemoveDevice(gamepad2);

        Assert.That(Gamepad.all, Has.Count.EqualTo(2));
        Assert.That(Gamepad.all, Has.None.SameAs(gamepad2));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanQueryAllJoysticksWithSimpleGetter()
    {
        var joystick1 = InputSystem.AddDevice<Joystick>();
        var joystick2 = InputSystem.AddDevice<Joystick>();
        InputSystem.AddDevice<Keyboard>();

        Assert.That(Joystick.all, Has.Count.EqualTo(2));
        Assert.That(Joystick.all, Has.Exactly(1).SameAs(joystick1));
        Assert.That(Joystick.all, Has.Exactly(1).SameAs(joystick2));

        var joystick3 = InputSystem.AddDevice<Joystick>();

        Assert.That(Joystick.all, Has.Count.EqualTo(3));
        Assert.That(Joystick.all, Has.Exactly(1).SameAs(joystick3));

        InputSystem.RemoveDevice(joystick2);

        Assert.That(Joystick.all, Has.Count.EqualTo(2));
        Assert.That(Joystick.all, Has.None.SameAs(joystick2));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetButtonOnGamepadUsingEnum()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad[GamepadButton.North], Is.SameAs(gamepad.buttonNorth));
        Assert.That(gamepad[GamepadButton.South], Is.SameAs(gamepad.buttonSouth));
        Assert.That(gamepad[GamepadButton.East], Is.SameAs(gamepad.buttonEast));
        Assert.That(gamepad[GamepadButton.West], Is.SameAs(gamepad.buttonWest));
        Assert.That(gamepad[GamepadButton.Start], Is.SameAs(gamepad.startButton));
        Assert.That(gamepad[GamepadButton.Select], Is.SameAs(gamepad.selectButton));
        Assert.That(gamepad[GamepadButton.LeftShoulder], Is.SameAs(gamepad.leftShoulder));
        Assert.That(gamepad[GamepadButton.RightShoulder], Is.SameAs(gamepad.rightShoulder));
        Assert.That(gamepad[GamepadButton.LeftStick], Is.SameAs(gamepad.leftStickButton));
        Assert.That(gamepad[GamepadButton.RightStick], Is.SameAs(gamepad.rightStickButton));
        Assert.That(gamepad[GamepadButton.DpadUp], Is.SameAs(gamepad.dpad.up));
        Assert.That(gamepad[GamepadButton.DpadDown], Is.SameAs(gamepad.dpad.down));
        Assert.That(gamepad[GamepadButton.DpadLeft], Is.SameAs(gamepad.dpad.left));
        Assert.That(gamepad[GamepadButton.DpadRight], Is.SameAs(gamepad.dpad.right));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateGenericJoystick()
    {
        var json = @"
            {
                ""name"" : ""MyJoystick"",
                ""extend"" : ""Joystick"",
                ""controls"" : [
                    { ""name"" : ""button1"", ""layout"" : ""Button"" },
                    { ""name"" : ""button2"", ""layout"" : ""Button"" },
                    { ""name"" : ""axis1"", ""layout"" : ""Axis"" },
                    { ""name"" : ""axis2"", ""layout"" : ""Axis"" },
                    { ""name"" : ""discrete"", ""layout"" : ""Digital"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var device = InputSystem.AddDevice("MyJoystick");

        Assert.That(device, Is.TypeOf<Joystick>());
        Assert.That(Joystick.current, Is.SameAs(device));

        var joystick = (Joystick)device;
        Assert.That(joystick.stick, Is.Not.Null);
        Assert.That(joystick.trigger, Is.Not.Null);
    }

    [Test]
    [Category("Devices")]
    public void Devices_JoysticksHaveDeadzonesOnStick()
    {
        var joystick = InputSystem.AddDevice<Joystick>();

        InputSystem.QueueStateEvent(joystick, new JoystickState {stick = new Vector2(0.001f, 0.002f)});
        InputSystem.Update();

        Assert.That(joystick.stick.ReadValue(), Is.EqualTo(Vector2.zero));
    }

    [Test]
    [Category("Devices")]
    public void Devices_PointerDeltasDoNotAccumulateFromPreviousFrame()
    {
        var pointer = InputSystem.AddDevice<Pointer>();

        InputSystem.QueueStateEvent(pointer, new PointerState { delta = new Vector2(0.5f, 0.5f) });
        InputSystem.Update();

        Assert.That(pointer.delta.x.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(pointer.delta.y.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));

        InputSystem.QueueStateEvent(pointer, new PointerState { delta = new Vector2(0.5f, 0.5f) });
        InputSystem.Update();

        Assert.That(pointer.delta.x.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(pointer.delta.y.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    [TestCase("Pointer", "delta")]
    [TestCase("Mouse", "delta")]
    [TestCase("Mouse", "scroll")]
    public void Devices_DeltaControlsAccumulateBetweenUpdates(string layoutName, string controlName)
    {
        var device = InputSystem.AddDevice(layoutName);
        var deltaControl = (Vector2Control)device[controlName];
        Debug.Assert(deltaControl != null);

        using (StateEvent.From(device, out var stateEventPtr))
        {
            deltaControl.WriteValueIntoEvent(new Vector2(0.5f, 0.5f), stateEventPtr);

            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.Update();

            Assert.That(deltaControl.x.ReadValue(), Is.EqualTo(1).Within(0.0000001));
            Assert.That(deltaControl.y.ReadValue(), Is.EqualTo(1).Within(0.0000001));
        }
    }

    [Test]
    [Category("Devices")]
    [TestCase("Pointer", "delta")]
    [TestCase("Mouse", "delta")]
    [TestCase("Mouse", "scroll")]
    public void Devices_DeltaControlsResetBetweenUpdates(string layoutName, string controlName)
    {
        var device = InputSystem.AddDevice(layoutName);
        var deltaControl = (Vector2Control)device[controlName];
        Debug.Assert(deltaControl != null);

        using (StateEvent.From(device, out var stateEventPtr))
        {
            deltaControl.WriteValueIntoEvent(new Vector2(0.5f, 0.5f), stateEventPtr);
            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.Update();

            Assert.That(deltaControl.x.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
            Assert.That(deltaControl.y.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));

            InputSystem.Update();

            Assert.That(deltaControl.x.ReadValue(), Is.Zero);
            Assert.That(deltaControl.y.ReadValue(), Is.Zero);
        }
    }

    [Test]
    [Category("Devices")]
    [TestCase("Gamepad", typeof(Gamepad))]
    [TestCase("Keyboard", typeof(Keyboard))]
    [TestCase("Pointer", typeof(Pointer))]
    [TestCase("Mouse", typeof(Mouse))]
    [TestCase("Pen", typeof(Pen))]
    [TestCase("Touchscreen", typeof(Touchscreen))]
    [TestCase("Joystick", typeof(Joystick))]
    [TestCase("Accelerometer", typeof(Accelerometer))]
    [TestCase("Gyroscope", typeof(Gyroscope))]
    public void Devices_CanCreateDevice(string layout, Type type)
    {
        var device = InputSystem.AddDevice(layout);

        Assert.That(device, Is.InstanceOf(type));
        Assert.That(device.layout, Is.EqualTo(layout));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCheckAnyKeyOnKeyboard()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
        InputSystem.Update();

        Assert.That(keyboard.anyKey.isPressed, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_AnyKeyOnKeyboard_DoesNotReactToIMESelected()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.IMESelected));
        InputSystem.Update();

        Assert.That(keyboard.anyKey.isPressed, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetTextInputFromKeyboard()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var textReceived = "";
        keyboard.onTextInput += ch => textReceived += ch;

        InputSystem.QueueTextEvent(keyboard, 'a');
        InputSystem.QueueTextEvent(keyboard, 'b');
        InputSystem.QueueTextEvent(keyboard, 'c');

        InputSystem.Update();

        Assert.That(textReceived, Is.EqualTo("abc"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanHandleUTF32CharactersInTextInputOnKeyboard()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var charsReceived = new List<char>();
        keyboard.onTextInput += ch => charsReceived.Add(ch);

        const int highBits = 0x12;
        const int lowBits = 0x21;

        var inputEvent = TextEvent.Create(keyboard.deviceId, 0x10000 + (highBits << 10 | lowBits));
        InputSystem.QueueEvent(ref inputEvent);
        InputSystem.Update();

        Assert.That(charsReceived, Has.Count.EqualTo(2));
        Assert.That(charsReceived[0], Is.EqualTo(0xD800 + highBits));
        Assert.That(charsReceived[1], Is.EqualTo(0xDC00 + lowBits));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetDisplayNameFromKeyboardKey()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var currentLayoutName = "default";
        unsafe
        {
            runtime.SetDeviceCommandCallback(keyboard.deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == QueryKeyNameCommand.Type)
                    {
                        var keyNameCommand = (QueryKeyNameCommand*)commandPtr;

                        var scanCode = 0x02;
                        var name = "other";

                        if (keyNameCommand->scanOrKeyCode == (int)Key.A)
                        {
                            scanCode = 0x01;
                            name = currentLayoutName == "default" ? "M" : "RIGHT CTRL";
                        }

                        keyNameCommand->scanOrKeyCode = scanCode;
                        StringHelpers.WriteStringToBuffer(name, (IntPtr)keyNameCommand->nameBuffer,
                            QueryKeyNameCommand.kMaxNameLength);

                        return QueryKeyNameCommand.kSize;
                    }

                    return InputDeviceCommand.GenericFailure;
                });
        }

        Assert.That(keyboard.aKey.displayName, Is.EqualTo("M"));
        Assert.That(keyboard.bKey.displayName, Is.EqualTo("Other"));

        // Change layout.
        currentLayoutName = "other";
        InputSystem.QueueConfigChangeEvent(keyboard);
        InputSystem.Update();

        Assert.That(keyboard.aKey.displayName, Is.EqualTo("Right Ctrl"));
        Assert.That(keyboard.bKey.displayName, Is.EqualTo("Other"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetNameOfCurrentKeyboardLayout()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var currentLayoutName = "default";
        unsafe
        {
            runtime.SetDeviceCommandCallback(keyboard.deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == QueryKeyboardLayoutCommand.Type)
                    {
                        var layoutCommand = (QueryKeyboardLayoutCommand*)commandPtr;
                        if (StringHelpers.WriteStringToBuffer(currentLayoutName, (IntPtr)layoutCommand->nameBuffer,
                            QueryKeyboardLayoutCommand.kMaxNameLength))
                            return QueryKeyboardLayoutCommand.kMaxNameLength;
                    }

                    return InputDeviceCommand.GenericFailure;
                });
        }

        Assert.That(keyboard.keyboardLayout, Is.EqualTo("default"));

        currentLayoutName = "new";
        InputSystem.QueueConfigChangeEvent(keyboard);
        InputSystem.Update();

        Assert.That(keyboard.keyboardLayout, Is.EqualTo("new"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetKeyCodeFromKeyboardKey()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        Assert.That(keyboard.aKey.keyCode, Is.EqualTo(Key.A));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpKeyFromKeyboardUsingKeyCode()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        Assert.That(keyboard[Key.A], Is.SameAs(keyboard.aKey));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpKeyFromKeyboardUsingDisplayName()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        SetKeyInfo(Key.A, "q");
        SetKeyInfo(Key.Q, "a");

        Assert.That(keyboard.FindKeyOnCurrentKeyboardLayout("a"), Is.SameAs(keyboard.qKey));
        Assert.That(keyboard.FindKeyOnCurrentKeyboardLayout("q"), Is.SameAs(keyboard.aKey));
    }

    [Test]
    [Category("Devices")]
    public void Devices_KeyboardsHaveSyntheticCombinedModifierKeys()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        Assert.That(keyboard.shiftKey.synthetic, Is.True);
        Assert.That(keyboard.ctrlKey.synthetic, Is.True);
        Assert.That(keyboard.altKey.synthetic, Is.True);

        Assert.That(keyboard.shiftKey.isPressed, Is.False);
        Assert.That(keyboard.ctrlKey.isPressed, Is.False);
        Assert.That(keyboard.altKey.isPressed, Is.False);

        Press(keyboard.leftAltKey);
        Press(keyboard.leftShiftKey);
        Press(keyboard.leftCtrlKey);

        Assert.That(keyboard.shiftKey.isPressed, Is.True);
        Assert.That(keyboard.ctrlKey.isPressed, Is.True);
        Assert.That(keyboard.altKey.isPressed, Is.True);

        Assert.That(keyboard.shiftKey.ReadValue(), Is.EqualTo(1).Within(0.00001));
        Assert.That(keyboard.ctrlKey.ReadValue(), Is.EqualTo(1).Within(0.00001));
        Assert.That(keyboard.altKey.ReadValue(), Is.EqualTo(1).Within(0.00001));

        Release(keyboard.leftAltKey);
        Release(keyboard.leftShiftKey);
        Release(keyboard.leftCtrlKey);

        Assert.That(keyboard.shiftKey.isPressed, Is.False);
        Assert.That(keyboard.ctrlKey.isPressed, Is.False);
        Assert.That(keyboard.altKey.isPressed, Is.False);

        Press(keyboard.rightAltKey);
        Press(keyboard.rightShiftKey);
        Press(keyboard.rightCtrlKey);

        Assert.That(keyboard.shiftKey.isPressed, Is.True);
        Assert.That(keyboard.ctrlKey.isPressed, Is.True);
        Assert.That(keyboard.altKey.isPressed, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanPerformHorizontalAndVerticalScrollWithMouse()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        InputSystem.QueueDeltaStateEvent(mouse.scroll, new Vector2(10, 12));
        InputSystem.Update();

        Assert.That(mouse.scroll.x.ReadValue(), Is.EqualTo(10).Within(0.0000001));
        Assert.That(mouse.scroll.y.ReadValue(), Is.EqualTo(12).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanWarpMousePosition()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        WarpMousePositionCommand? receivedCommand = null;
        unsafe
        {
            runtime.SetDeviceCommandCallback(mouse.deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == WarpMousePositionCommand.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = *((WarpMousePositionCommand*)commandPtr);
                        return 1;
                    }

                    Assert.Fail();
                    return InputDeviceCommand.GenericFailure;
                });
        }

        mouse.WarpCursorPosition(new Vector2(0.1234f, 0.5678f));

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.warpPositionInPlayerDisplaySpace.x, Is.EqualTo(0.1234).Within(0.000001));
        Assert.That(receivedCommand.Value.warpPositionInPlayerDisplaySpace.y, Is.EqualTo(0.5678).Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanUseMouseAsPointer()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        InputSystem.QueueStateEvent(mouse,
            new MouseState
            {
                position = new Vector2(0.123f, 0.456f),
            }.WithButton(MouseButton.Left));
        InputSystem.Update();

        Assert.That(mouse.position.x.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(mouse.position.y.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
        Assert.That(mouse.press.isPressed, Is.True);

        Assert.That(InputControlPath.TryFindControls(mouse, "*/{PrimaryAction}"), Is.EquivalentTo(new[] { mouse.leftButton }));
        Assert.That(InputControlPath.TryFindControls(mouse, "*/{SecondaryAction}"), Is.EquivalentTo(new[] { mouse.rightButton }));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanDetectIfPenIsInRange()
    {
        var pen = InputSystem.AddDevice<Pen>();

        Assert.That(pen.inRange.ReadValue(), Is.EqualTo(0).Within(0.00001));

        InputSystem.QueueStateEvent(pen, new PenState().WithButton(PenButton.InRange));
        InputSystem.Update();

        Assert.That(pen.inRange.ReadValue(), Is.EqualTo(1).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_PenPrimaryActionIsTip()
    {
        var pen = InputSystem.AddDevice<Pen>();
        Assert.That(pen["{PrimaryAction}"], Is.SameAs(pen.tip));
        Assert.That(pen.allControls, Has.Exactly(1).With.Property("usages").Contains("PrimaryAction"));
        Assert.That(new InputAction(binding: "<Pen>/{PrimaryAction}").controls, Is.EquivalentTo(new[] { pen.tip }));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanUsePenAsPointer()
    {
        var pen = InputSystem.AddDevice<Pen>();

        Assert.That(pen.position.ReadValue(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        Assert.That(pen.delta.ReadValue(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        Assert.That(pen.pressure.ReadValue(), Is.Zero);
        Assert.That(pen.press.isPressed, Is.False);
        Assert.That(pen.tip.isPressed, Is.False);
        Assert.That(pen.eraser.isPressed, Is.False);
        Assert.That(pen.firstBarrelButton.isPressed, Is.False);
        Assert.That(pen.secondBarrelButton.isPressed, Is.False);
        Assert.That(pen.thirdBarrelButton.isPressed, Is.False);
        Assert.That(pen.fourthBarrelButton.isPressed, Is.False);
        Assert.That(pen.inRange.isPressed, Is.False);
        Assert.That(pen.twist.ReadValue(), Is.Zero);
        Assert.That(pen.tilt.ReadValue(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

        InputSystem.QueueStateEvent(pen, new PenState
        {
            position = new Vector2(0.123f, 0.234f),
            delta = new Vector2(0.11f, 0.22f),
            pressure = 0.345f,
            twist = 0.456f,
            tilt = new Vector2(0.567f, 0.678f),
        });
        InputSystem.Update();

        Assert.That(pen.position.ReadValue(), Is.EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(pen.delta.ReadValue(), Is.EqualTo(new Vector2(0.11f, 0.22f)).Using(Vector2EqualityComparer.Instance));
        Assert.That(pen.pressure.ReadValue(), Is.EqualTo(0.345).Within(0.00001));
        Assert.That(pen.twist.ReadValue(), Is.EqualTo(0.456).Within(0.00001));
        Assert.That(pen.tilt.ReadValue(), Is.EqualTo(new Vector2(0.567f, 0.678f)).Using(Vector2EqualityComparer.Instance));

        AssertButtonPress(pen, new PenState().WithButton(PenButton.Tip), pen.tip, pen.press);
        AssertButtonPress(pen, new PenState().WithButton(PenButton.Eraser), pen.eraser);
        AssertButtonPress(pen, new PenState().WithButton(PenButton.BarrelFirst), pen.firstBarrelButton);
        AssertButtonPress(pen, new PenState().WithButton(PenButton.BarrelSecond), pen.secondBarrelButton);
        AssertButtonPress(pen, new PenState().WithButton(PenButton.BarrelThird), pen.thirdBarrelButton);
        AssertButtonPress(pen, new PenState().WithButton(PenButton.BarrelFourth), pen.fourthBarrelButton);
    }

    // This test makes sure that Touchscreen correctly synthesizes primaryTouch in a way that makes the controls
    // inherited from Pointer operate in a fashion equivalent to other types of pointers.
    //
    // NOTE: The behavior here where primary touch stays ongoing and in place for as long as there is any other
    //       active touch on the screen seems consistent with observed behavior on iOS and Android.
    [Test]
    [Category("Devices")]
    public void Devices_CanUseTouchscreenAsPointer()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        // First finger goes down.
        BeginTouch(4, new Vector2(0.123f, 0.456f), time: 0);

        Assert.That(device.primaryTouch.touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.position.x.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(device.position.y.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
        Assert.That(device.delta.x.ReadValue(), Is.Zero.Within(0.000001));
        Assert.That(device.delta.y.ReadValue(), Is.Zero.Within(0.000001));
        Assert.That(device.press.isPressed, Is.True);
        Assert.That(device.press.wasPressedThisFrame, Is.True);
        Assert.That(device.press.wasReleasedThisFrame, Is.False);

        // First finger moves.
        MoveTouch(4, new Vector2(0.234f, 0.345f), time: 0.1);

        Assert.That(device.primaryTouch.touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.position.x.ReadValue(), Is.EqualTo(0.234).Within(0.000001));
        Assert.That(device.position.y.ReadValue(), Is.EqualTo(0.345).Within(0.000001));
        Assert.That(device.delta.x.ReadValue(), Is.EqualTo(0.111).Within(0.000001));
        Assert.That(device.delta.y.ReadValue(), Is.EqualTo(-0.111).Within(0.000001));
        Assert.That(device.press.isPressed, Is.True);
        Assert.That(device.press.wasPressedThisFrame, Is.False);
        Assert.That(device.press.wasReleasedThisFrame, Is.False);

        // Second finger goes down. No effect.
        BeginTouch(5, new Vector2(0.111f, 0.222f), time: 0.2);

        Assert.That(device.primaryTouch.touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.position.x.ReadValue(), Is.EqualTo(0.234).Within(0.000001));
        Assert.That(device.position.y.ReadValue(), Is.EqualTo(0.345).Within(0.000001));
        Assert.That(device.delta.x.ReadValue(), Is.Zero.Within(0.000001));
        Assert.That(device.delta.y.ReadValue(), Is.Zero.Within(0.000001));
        Assert.That(device.press.isPressed, Is.True);
        Assert.That(device.press.wasPressedThisFrame, Is.False);
        Assert.That(device.press.wasReleasedThisFrame, Is.False);

        // First finger goes up. Primary touch moves to final position but does NOT
        // end yet.
        EndTouch(4, new Vector2(0.345f, 0.456f), time: 0.3);

        Assert.That(device.primaryTouch.touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.position.x.ReadValue(), Is.EqualTo(0.345).Within(0.000001));
        Assert.That(device.position.y.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
        Assert.That(device.delta.x.ReadValue(), Is.EqualTo(0.111).Within(0.000001));
        Assert.That(device.delta.y.ReadValue(), Is.EqualTo(0.111).Within(0.000001));
        Assert.That(device.press.isPressed, Is.True);
        Assert.That(device.press.wasPressedThisFrame, Is.False);
        Assert.That(device.press.wasReleasedThisFrame, Is.False);

        // Second finger moves. No effect on primary touch.
        MoveTouch(5, new Vector2(0.456f, 0.567f), time: 0.4);

        Assert.That(device.primaryTouch.touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.position.x.ReadValue(), Is.EqualTo(0.345).Within(0.000001));
        Assert.That(device.position.y.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
        Assert.That(device.delta.x.ReadValue(), Is.Zero.Within(0.000001));
        Assert.That(device.delta.y.ReadValue(), Is.Zero.Within(0.000001));
        Assert.That(device.press.isPressed, Is.True);
        Assert.That(device.press.wasPressedThisFrame, Is.False);
        Assert.That(device.press.wasReleasedThisFrame, Is.False);

        // Second finger goes up. Primary touch now ends.
        EndTouch(5, new Vector2(0.777f, 0.888f), time: 0.4);

        Assert.That(device.primaryTouch.touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.position.x.ReadValue(), Is.EqualTo(0.345).Within(0.000001));
        Assert.That(device.position.y.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
        Assert.That(device.delta.x.ReadValue(), Is.Zero.Within(0.000001));
        Assert.That(device.delta.y.ReadValue(), Is.Zero.Within(0.000001));
        Assert.That(device.press.isPressed, Is.False);
        Assert.That(device.press.wasPressedThisFrame, Is.False);
        Assert.That(device.press.wasReleasedThisFrame, Is.True);
    }

    // Touchscreen is somewhat special in that treats its available TouchState slots like a pool
    // from which it dynamically assigns entries to track individual touches.
    [Test]
    [Category("Devices")]
    public void Devices_TouchscreenDynamicallyAllocatesTouchStates()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 4,
            });
        InputSystem.Update();

        Assert.That(device.touches[0].touchId.ReadValue(), Is.EqualTo(4));

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 5,
            });
        InputSystem.Update();

        Assert.That(device.touches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.touches[1].touchId.ReadValue(), Is.EqualTo(5));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchStaysOnSameControlForDurationOfTouch()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        // Begin touch.
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 4,
            });
        InputSystem.Update();

        Assert.That(device.touches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.touches[0].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));

        // Don't move.
        InputSystem.Update();

        Assert.That(device.touches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.touches[0].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));

        // Move.
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Moved,
                touchId = 4,
            });
        InputSystem.Update();

        Assert.That(device.touches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.touches[0].phase.ReadValue(), Is.EqualTo(TouchPhase.Moved));

        // Don't move.
        InputSystem.Update();

        Assert.That(device.touches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.touches[0].phase.ReadValue(), Is.EqualTo(TouchPhase.Moved));

        // Random unrelated touch.
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 5,
            });
        InputSystem.Update();

        Assert.That(device.touches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.touches[0].phase.ReadValue(), Is.EqualTo(TouchPhase.Moved));

        // End.
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Ended,
                touchId = 4,
            });
        InputSystem.Update();

        Assert.That(device.touches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.touches[0].phase.ReadValue(), Is.EqualTo(TouchPhase.Ended));
    }

    // Touchscreen does NOT make use of TouchPhase.Stationary. The rationale here is that for actions,
    // the activity on the touch controls is just noise -- actions care about input signaling changes,
    // not about input signaling "no changes" (which is what Stationary is about). And for polling touch
    // directly, Touchscreen makes for a lousy API overall so even if it did the Stationary thing, it
    // would do little for improving its ability to function as a polling API. That part is really
    // EnhancedTouchSupport's job.
    [Test]
    [Category("Devices")]
    public void Devices_TouchesDoNotBecomeStationaryWhenNotMovedInFrame()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 4,
            });
        InputSystem.Update();

        Assert.That(device.touches[0].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));

        InputSystem.Update();

        Assert.That(device.touches[0].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetStartTimeOfTouches()
    {
        var touchscreen = InputSystem.AddDevice<Touchscreen>();

        BeginTouch(4, new Vector2(0.123f, 0.234f), time: 0.1);
        BeginTouch(5, new Vector2(0.234f, 0.345f), time: 0.2);

        Assert.That(touchscreen.touches[0].startTime.ReadValue(), Is.EqualTo(0.1));
        Assert.That(touchscreen.touches[1].startTime.ReadValue(), Is.EqualTo(0.2));

        MoveTouch(4, new Vector2(0.345f, 0.456f), time: 0.3);
        MoveTouch(4, new Vector2(0.456f, 0.567f), time: 0.3);
        MoveTouch(5, new Vector2(0.567f, 0.678f), time: 0.4);

        Assert.That(touchscreen.touches[0].startTime.ReadValue(), Is.EqualTo(0.1));
        Assert.That(touchscreen.touches[1].startTime.ReadValue(), Is.EqualTo(0.2));

        EndTouch(4, new Vector2(0.123f, 0.234f), time: 0.5);
        EndTouch(5, new Vector2(0.234f, 0.345f), time: 0.5);

        Assert.That(touchscreen.touches[0].startTime.ReadValue(), Is.EqualTo(0.1));
        Assert.That(touchscreen.touches[1].startTime.ReadValue(), Is.EqualTo(0.2));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanDetectTouchTaps()
    {
        // Give us known tap settings.
        InputSystem.settings.defaultTapTime = 0.5f;
        InputSystem.settings.tapRadius = 5;

        var touchscreen = InputSystem.AddDevice<Touchscreen>();

        // Each TouchControl has a tap and then there's a tap on the screen as a whole which
        // in turn is wired to the tap
        using (var allTouchTaps = new InputStateHistory<float>("<Touchscreen>/touch*/tap"))
        using (var primaryTouchTap = new InputStateHistory<float>(touchscreen.primaryTouch.tap))
        {
            allTouchTaps.StartRecording();
            primaryTouchTap.StartRecording();

            BeginTouch(4, new Vector2(0.123f, 0.234f), time: 0.1);
            BeginTouch(5, new Vector2(0.234f, 0.345f), time: 0.2);

            Assert.That(allTouchTaps, Is.Empty);
            Assert.That(primaryTouchTap, Is.Empty);

            EndTouch(4, new Vector2(1, 2), time: 0.3);
            EndTouch(5, new Vector2(2, 3), time: 0.3);

            // Both touches should have seen a tap.
            Assert.That(allTouchTaps, Has.Count.EqualTo(4));
            Assert.That(allTouchTaps[0].control, Is.SameAs(touchscreen.touches[0].tap));
            Assert.That(allTouchTaps[1].control, Is.SameAs(touchscreen.touches[0].tap));
            Assert.That(allTouchTaps[0].ReadValue(), Is.EqualTo(1));
            Assert.That(allTouchTaps[1].ReadValue(), Is.EqualTo(0));
            Assert.That(allTouchTaps[0].time, Is.EqualTo(0.3));
            Assert.That(allTouchTaps[1].time, Is.EqualTo(0.3));
            Assert.That(allTouchTaps[2].control, Is.SameAs(touchscreen.touches[1].tap));
            Assert.That(allTouchTaps[3].control, Is.SameAs(touchscreen.touches[1].tap));
            Assert.That(allTouchTaps[2].ReadValue(), Is.EqualTo(1));
            Assert.That(allTouchTaps[3].ReadValue(), Is.EqualTo(0));
            Assert.That(allTouchTaps[2].time, Is.EqualTo(0.3));
            Assert.That(allTouchTaps[3].time, Is.EqualTo(0.3));

            // The primary touch switched from touch #0 to touch #1 when we released
            // touch #0 while touch #1 was still ongoing. Even though touch #1 then
            // released within defaultTapTime, the fact we had to switch from one touch
            // to another on primaryTouch means we don't trigger a tap.
            Assert.That(primaryTouchTap, Is.Empty);

            allTouchTaps.Clear();

            // Run a touch that exceeds the max tap radius.
            BeginTouch(4, new Vector2(1, 2), time: 0.4);
            MoveTouch(4, new Vector2(10, 20), time: 0.5);
            EndTouch(4, new Vector2(10, 20), time: 0.6);

            Assert.That(allTouchTaps, Is.Empty);
            Assert.That(primaryTouchTap, Is.Empty);

            // Run a single finger tap.
            BeginTouch(4, new Vector2(1, 2), time: 0.6);
            EndTouch(4, new Vector2(3, 4), time: 0.8);

            Assert.That(allTouchTaps, Has.Count.EqualTo(2));
            Assert.That(allTouchTaps[0].control, Is.SameAs(touchscreen.touches[0].tap));
            Assert.That(allTouchTaps[1].control, Is.SameAs(touchscreen.touches[0].tap));
            Assert.That(allTouchTaps[0].ReadValue(), Is.EqualTo(1));
            Assert.That(allTouchTaps[1].ReadValue(), Is.EqualTo(0));
            Assert.That(allTouchTaps[0].time, Is.EqualTo(0.8));
            Assert.That(allTouchTaps[1].time, Is.EqualTo(0.8));

            Assert.That(primaryTouchTap, Has.Count.EqualTo(2));
            Assert.That(primaryTouchTap[0].control, Is.SameAs(touchscreen.primaryTouch.tap));
            Assert.That(primaryTouchTap[1].control, Is.SameAs(touchscreen.primaryTouch.tap));
            Assert.That(primaryTouchTap[0].ReadValue(), Is.EqualTo(1));
            Assert.That(primaryTouchTap[1].ReadValue(), Is.EqualTo(0));
            Assert.That(primaryTouchTap[0].time, Is.EqualTo(0.8));
            Assert.That(primaryTouchTap[1].time, Is.EqualTo(0.8));
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanDetectTouchTaps_AndKeepTrackOfTapCounts()
    {
        // Give us known tap settings.
        InputSystem.settings.defaultTapTime = 0.5f;
        InputSystem.settings.tapRadius = 5;
        InputSystem.settings.multiTapDelayTime = 5;

        var touchscreen = InputSystem.AddDevice<Touchscreen>();

        BeginTouch(1, new Vector2(0.123f, 0.234f), time: 1);
        EndTouch(1, new Vector2(0.123f, 0.234f), time: 1);

        Assert.That(touchscreen.touches[0].tapCount.ReadValue(), Is.EqualTo(1));
        Assert.That(touchscreen.primaryTouch.tapCount.ReadValue(), Is.EqualTo(1));

        BeginTouch(1, new Vector2(0.123f, 0.234f), time: 2);
        EndTouch(1, new Vector2(0.123f, 0.234f), time: 2);

        Assert.That(touchscreen.touches[0].tapCount.ReadValue(), Is.EqualTo(2));
        Assert.That(touchscreen.primaryTouch.tapCount.ReadValue(), Is.EqualTo(2));

        runtime.currentTime = 10;
        InputSystem.Update();

        Assert.That(touchscreen.touches[0].tapCount.ReadValue(), Is.Zero);
        Assert.That(touchscreen.primaryTouch.tapCount.ReadValue(), Is.Zero);
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchscreenSupports10ConcurrentTouchesByDefault()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        Assert.That(device.touches, Has.Count.EqualTo(10));

        for (var i = 0; i < 10; ++i)
            InputSystem.QueueStateEvent(device, new TouchState { touchId = i + 1, phase = TouchPhase.Began });

        InputSystem.Update();

        Assert.That(device.touches[0].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(device.touches[0].touchId.ReadValue(), Is.EqualTo(1));
        Assert.That(device.touches[1].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(device.touches[1].touchId.ReadValue(), Is.EqualTo(2));
        Assert.That(device.touches[2].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(device.touches[2].touchId.ReadValue(), Is.EqualTo(3));
        Assert.That(device.touches[3].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(device.touches[3].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.touches[4].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(device.touches[4].touchId.ReadValue(), Is.EqualTo(5));
        Assert.That(device.touches[5].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(device.touches[5].touchId.ReadValue(), Is.EqualTo(6));
        Assert.That(device.touches[6].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(device.touches[6].touchId.ReadValue(), Is.EqualTo(7));
        Assert.That(device.touches[7].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(device.touches[7].touchId.ReadValue(), Is.EqualTo(8));
        Assert.That(device.touches[8].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(device.touches[8].touchId.ReadValue(), Is.EqualTo(9));
        Assert.That(device.touches[9].phase.ReadValue(), Is.EqualTo(TouchPhase.Began));
        Assert.That(device.touches[9].touchId.ReadValue(), Is.EqualTo(10));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateTouchscreenWithCustomTouchCount()
    {
        // NOTE: ATM we have pretty tight restrictions on the upper limit of the number
        //       of controls and the state offsets and sizes of controls. This comes down
        //       to state offset tables in device (InputDevice.m_StateOffsetToControlMap)
        //       packing everything into 32-bit entries. If the current limits turn out
        //       to be too low, we can bump this to 64 bits.

        // Create a touchscreen that has 16 concurrent touches instead of 10.
        const string json = @"
            {
                ""name"" : ""CustomTouchscreen"",
                ""extend"" : ""Touchscreen"",
                ""controls"" : [
                    { ""name"" : ""touch"", ""arraySize"" : 16 }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = (Touchscreen)InputSystem.AddDevice("CustomTouchscreen");

        Assert.That(device.touches, Has.Count.EqualTo(16));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchscreenStateLayoutCorrespondsToStruct()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        Assert.That(device.primaryTouch.stateBlock.byteOffset, Is.Zero);
        Assert.That(device.touches[0].stateBlock.byteOffset, Is.EqualTo(TouchscreenState.kTouchDataOffset));
        Assert.That(device.touches[1].stateBlock.byteOffset, Is.EqualTo(TouchscreenState.kTouchDataOffset * 2));
        Assert.That(device.touches[2].stateBlock.byteOffset, Is.EqualTo(TouchscreenState.kTouchDataOffset * 3));
        Assert.That(device.touches[3].stateBlock.byteOffset, Is.EqualTo(TouchscreenState.kTouchDataOffset * 4));
        Assert.That(device.touches[4].stateBlock.byteOffset, Is.EqualTo(TouchscreenState.kTouchDataOffset * 5));
        Assert.That(device.touches[5].stateBlock.byteOffset, Is.EqualTo(TouchscreenState.kTouchDataOffset * 6));
        Assert.That(device.touches[6].stateBlock.byteOffset, Is.EqualTo(TouchscreenState.kTouchDataOffset * 7));
        Assert.That(device.touches[7].stateBlock.byteOffset, Is.EqualTo(TouchscreenState.kTouchDataOffset * 8));
        Assert.That(device.touches[8].stateBlock.byteOffset, Is.EqualTo(TouchscreenState.kTouchDataOffset * 9));
        Assert.That(device.touches[9].stateBlock.byteOffset, Is.EqualTo(TouchscreenState.kTouchDataOffset * 10));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanReadTouchStateFromTouchControl()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        BeginTouch(1, new Vector2(123, 234));

        var touch = device.touches[0].ReadValue();

        Assert.That(touch.touchId, Is.EqualTo(1));
        Assert.That(touch.phase, Is.EqualTo(TouchPhase.Began));
        Assert.That(touch.position, Is.EqualTo(new Vector2(123, 234)).Using(Vector2EqualityComparer.Instance));
        Assert.That(touch.startPosition, Is.EqualTo(new Vector2(123, 234)).Using(Vector2EqualityComparer.Instance));
        Assert.That(touch.tapCount, Is.Zero);
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchTimestampsFromDifferentIdsDontAffectEachOther()
    {
        // On iOS and probably Android, when you're touching the screen with two fingers. Touches with different ids can come in different order.
        // Here's an example, in what order OS sends us touches
        // NewInput: Touch Moved 2227.000000 x 1214.000000, id = 5, time = 24.478610
        // NewInput: Touch Moved 1828.000000 x 1156.000000, id = 6, time = 24.478610
        // NewInput: Touch Moved 2227.000000 x 1290.000000, id = 5, time = 24.494703
        // NewInput: Touch Moved 1818.000000 x 1231.000000, id = 6, time = 24.494702
        //
        // Notice, last event has lower timestamp than previous events, but these are two different touches so they shouldn't affect each other.
        // Sadly currently there's a bug in managed side, where Input System will ignore events with lower timestamp than previous event

        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 4,
                position = new Vector2(1, 2)
            },
            1.0);
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 5,
                position = new Vector2(3, 4)
            },
            0.9);
        InputSystem.Update();

        Assert.That(device.touches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.touches[1].touchId.ReadValue(), Is.EqualTo(5));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchDeltasAreComputedAutomatically()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 4,
                position = new Vector2(10, 20)
            });
        InputSystem.Update();

        Assert.That(device.touches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.touches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Moved,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.Update();

        Assert.That(device.touches[0].delta.x.ReadValue(), Is.EqualTo(10).Within(0.00001));
        Assert.That(device.touches[0].delta.y.ReadValue(), Is.EqualTo(20).Within(0.00001));

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Ended,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.Update();

        Assert.That(device.touches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.touches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFlagTouchAsIndirect()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 1,
                isIndirectTouch = true,
            });

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 2,
            });
        InputSystem.Update();

        Assert.That(device.touches[0].indirectTouch.ReadValue(), Is.EqualTo(1));
        Assert.That(device.touches[1].indirectTouch.ReadValue(), Is.EqualTo(0));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchDeltasResetWhenTouchIsStationary()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 4,
                position = new Vector2(10, 20)
            });
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Moved,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.Update();

        Assert.That(device.touches[0].delta.x.ReadValue(), Is.EqualTo(10).Within(0.00001));
        Assert.That(device.touches[0].delta.y.ReadValue(), Is.EqualTo(20).Within(0.00001));

        InputSystem.Update();

        Assert.That(device.touches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.touches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchDeltasResetWhenTouchIsMovingInPlace()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 4,
                position = new Vector2(10, 20)
            });
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Moved,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.Update();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Moved,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.Update();

        Assert.That(device.touches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.touches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchesAccumulateDeltasWithinFrame()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Began,
                touchId = 4,
                position = new Vector2(10, 20)
            });
        InputSystem.Update();

        Assert.That(device.touches[0].delta.ReadValue(), Is.EqualTo(Vector2.zero));

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Moved,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = TouchPhase.Moved,
                touchId = 4,
                position = new Vector2(30, 50)
            });
        InputSystem.Update();

        Assert.That(device.touches[0].delta.x.ReadValue(), Is.EqualTo(20).Within(0.00001));
        Assert.That(device.touches[0].delta.y.ReadValue(), Is.EqualTo(30).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_TouchControlCanReadTouchStateEventForTouchscreen()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_CannotChangeStateLayoutOfTouchControls()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_CannotChangeStateLayoutOfTouchscreen()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDisabledSensorMakesItCurrent()
    {
        var deviceId = runtime.ReportNewInputDevice<Accelerometer>();
        runtime.SetDeviceCommandCallback(deviceId,
            new QueryEnabledStateCommand
            {
                baseCommand = new InputDeviceCommand(QueryEnabledStateCommand.Type, QueryEnabledStateCommand.kSize),
                isEnabled = false
            });

        InputSystem.Update();

        Assert.That(Accelerometer.current, Is.Not.Null);
        Assert.That(Accelerometer.current.enabled, Is.False);

        InputSystem.EnableDevice(Accelerometer.current);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetSensorSamplingFrequency()
    {
        var sensor = InputSystem.AddDevice<Accelerometer>();

        bool? receivedQueryFrequencyCommand = null;
        unsafe
        {
            runtime.SetDeviceCommandCallback(sensor.deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == QuerySamplingFrequencyCommand.Type)
                    {
                        Assert.That(receivedQueryFrequencyCommand, Is.Null);
                        receivedQueryFrequencyCommand = true;
                        ((QuerySamplingFrequencyCommand*)commandPtr)->frequency = 120.0f;
                        return InputDeviceCommand.GenericSuccess;
                    }

                    return InputDeviceCommand.GenericFailure;
                });
        }

        Assert.That(sensor.samplingFrequency, Is.EqualTo(120.0).Within(0.000001));
        Assert.That(receivedQueryFrequencyCommand, Is.Not.Null);
        Assert.That(receivedQueryFrequencyCommand.Value, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanSetSensorSamplingFrequency()
    {
        var sensor = InputSystem.AddDevice<Accelerometer>();

        bool? receivedSetFrequencyCommand = null;
        unsafe
        {
            runtime.SetDeviceCommandCallback(sensor.deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == SetSamplingFrequencyCommand.Type)
                    {
                        Assert.That(receivedSetFrequencyCommand, Is.Null);
                        receivedSetFrequencyCommand = true;
                        return InputDeviceCommand.GenericSuccess;
                    }

                    return InputDeviceCommand.GenericFailure;
                });
        }

        sensor.samplingFrequency = 30.0f;

        Assert.That(receivedSetFrequencyCommand, Is.Not.Null);
        Assert.That(receivedSetFrequencyCommand.Value, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetAccelerometerReading()
    {
        var accelerometer = InputSystem.AddDevice<Accelerometer>();
        var value = new Vector3(0.123f, 0.456f, 0.789f);
        InputSystem.QueueStateEvent(accelerometer, new AccelerometerState { acceleration = value });
        InputSystem.Update();

        Assert.That(accelerometer.acceleration.ReadValue(), Is.EqualTo(value).Within(0.00001));
        Assert.That(Accelerometer.current, Is.SameAs(accelerometer));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetGyroReading()
    {
        var gyro = InputSystem.AddDevice<Gyroscope>();
        var value = new Vector3(0.987f, 0.654f, 0.321f);
        InputSystem.QueueStateEvent(gyro, new GyroscopeState { angularVelocity = value });
        InputSystem.Update();

        Assert.That(gyro.angularVelocity.ReadValue(), Is.EqualTo(value).Within(0.00001));
        Assert.That(Gyroscope.current, Is.SameAs(gyro));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetGravityReading()
    {
        var sensor = InputSystem.AddDevice<GravitySensor>();
        var value = new Vector3(0.987f, 0.654f, 0.321f);
        InputSystem.QueueStateEvent(sensor, new GravityState { gravity = value });
        InputSystem.Update();

        Assert.That(sensor.gravity.ReadValue(), Is.EqualTo(value).Within(0.00001));
        Assert.That(GravitySensor.current, Is.SameAs(sensor));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetAttitudeReading()
    {
        var sensor = InputSystem.AddDevice<AttitudeSensor>();
        var value = Quaternion.Euler(10, 20, 30);
        InputSystem.QueueStateEvent(sensor, new AttitudeState { attitude = value });
        InputSystem.Update();

        Assert.That(sensor.attitude.ReadValue(), Is.EqualTo(value).Within(0.00001));
        Assert.That(AttitudeSensor.current, Is.SameAs(sensor));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetLinearAccelerationReading()
    {
        var sensor = InputSystem.AddDevice<LinearAccelerationSensor>();
        var value = new Vector3(0.987f, 0.654f, 0.321f);
        InputSystem.QueueStateEvent(sensor, new LinearAccelerationState { acceleration = value });
        InputSystem.Update();

        Assert.That(sensor.acceleration.ReadValue(), Is.EqualTo(value).Within(0.00001));
        Assert.That(LinearAccelerationSensor.current, Is.SameAs(sensor));
    }

    [Test]
    [Category("Devices")]
    [TestCase("Accelerometer", "acceleration")]
    [TestCase("Gyroscope", "angularVelocity")]
    [TestCase("GravitySensor", "gravity")]
    public void Devices_CanCompensateSensorDirectionValues(string layoutName, string controlName)
    {
        var sensor = InputSystem.AddDevice(layoutName);
        var value = new Vector3(0.123f, 0.456f, 0.789f);
        var directionControl = (Vector3Control)sensor[controlName];

        using (StateEvent.From(sensor, out var stateEventPtr))
        {
            directionControl.WriteValueIntoEvent(value, stateEventPtr);
            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.Update();

            InputSystem.settings.compensateForScreenOrientation = true;

            runtime.screenOrientation = ScreenOrientation.LandscapeLeft;
            Assert.That(directionControl.ReadValue(),
                Is.EqualTo(new Vector3(-value.y, value.x, value.z)).Using(Vector3EqualityComparer.Instance));

            runtime.screenOrientation = ScreenOrientation.PortraitUpsideDown;
            Assert.That(directionControl.ReadValue(),
                Is.EqualTo(new Vector3(-value.x, -value.y, value.z)).Using(Vector3EqualityComparer.Instance));

            runtime.screenOrientation = ScreenOrientation.LandscapeRight;
            Assert.That(directionControl.ReadValue(),
                Is.EqualTo(new Vector3(value.y, -value.x, value.z)).Using(Vector3EqualityComparer.Instance));

            runtime.screenOrientation = ScreenOrientation.Portrait;
            Assert.That(directionControl.ReadValue(), Is.EqualTo(value).Using(Vector3EqualityComparer.Instance));

            InputSystem.settings.compensateForScreenOrientation = false;
            Assert.That(directionControl.ReadValue(), Is.EqualTo(value).Using(Vector3EqualityComparer.Instance));
        }
    }

    [Test]
    [Category("Devices")]
    [TestCase("AttitudeSensor", "attitude")]
    public void Devices_CanCompensateSensorRotationValues(string layoutName, string controlName)
    {
        var sensor = InputSystem.AddDevice(layoutName);
        var angles = new Vector3(11, 22, 33);
        var rotationControl = (QuaternionControl)sensor[controlName];

        using (StateEvent.From(sensor, out var stateEventPtr))
        {
            rotationControl.WriteValueIntoEvent(Quaternion.Euler(angles), stateEventPtr);
            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.Update();

            InputSystem.settings.compensateForScreenOrientation = true;
            runtime.screenOrientation = ScreenOrientation.LandscapeLeft;
            Assert.That(rotationControl.ReadValue().eulerAngles,
                Is.EqualTo(new Vector3(angles.x, angles.y, angles.z + 270)).Using(Vector3EqualityComparer.Instance));

            runtime.screenOrientation = ScreenOrientation.PortraitUpsideDown;
            Assert.That(rotationControl.ReadValue().eulerAngles,
                Is.EqualTo(new Vector3(angles.x, angles.y, angles.z + 180)).Using(Vector3EqualityComparer.Instance));

            runtime.screenOrientation = ScreenOrientation.LandscapeRight;
            Assert.That(rotationControl.ReadValue().eulerAngles,
                Is.EqualTo(new Vector3(angles.x, angles.y, angles.z + 90)).Using(Vector3EqualityComparer.Instance));

            runtime.screenOrientation = ScreenOrientation.Portrait;
            Assert.That(rotationControl.ReadValue().eulerAngles,
                Is.EqualTo(angles).Using(Vector3EqualityComparer.Instance));

            InputSystem.settings.compensateForScreenOrientation = false;
            Assert.That(rotationControl.ReadValue().eulerAngles,
                Is.EqualTo(angles).Using(Vector3EqualityComparer.Instance));
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanMatchDeviceDescriptions()
    {
        var matchOne = new InputDeviceMatcher()
            .WithInterface("TestInterface");
        var matchTwo = new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithDeviceClass("TestDeviceClass");
        var matchOneWithRegex = new InputDeviceMatcher()
            .WithInterface(".*Interface");
        var matchAll = new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithDeviceClass("TestDeviceClass")
            .WithManufacturer("TestManufacturer")
            .WithProduct("TestProduct")
            .WithVersion(@"1\.0");
        var matchNone = new InputDeviceMatcher()
            .WithInterface("Test")
            .WithDeviceClass("Test")
            .WithManufacturer("Test");
        var matchMoreThanItHas = new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithCapability("canDo", true);

        var description = new InputDeviceDescription
        {
            interfaceName = "TestInterface",
            deviceClass = "TestDeviceClass",
            product = "TestProduct",
            manufacturer = "TestManufacturer",
            version = "1.0",
        };

        Assert.That(matchOne.MatchPercentage(description), Is.EqualTo(1.0f / 5).Within(0.0001));
        Assert.That(matchTwo.MatchPercentage(description), Is.EqualTo(1.0f / 5 * 2).Within(0.0001));
        Assert.That(matchOneWithRegex.MatchPercentage(description), Is.EqualTo(1.0f / 5).Within(0.0001));
        Assert.That(matchAll.MatchPercentage(description), Is.EqualTo(1).Within(0.0001));
        Assert.That(matchNone.MatchPercentage(description), Is.EqualTo(0).Within(0.0001));
        Assert.That(matchMoreThanItHas.MatchPercentage(description), Is.EqualTo(0).Within(0.0001));
    }

    [Serializable]
    private struct TestDeviceCapabilities
    {
        public string stringCap;
        public int intCap;
        public float floatCap;
        public float floatCapWithExponent;
        public bool boolCap;
        public NestedCaps nestedCap;
        public string[] arrayCap;
        public EnumCaps enumCap;

        [Serializable]
        public struct NestedCaps
        {
            public string value;
        }

        [Serializable]
        public enum EnumCaps
        {
            First,
            Second
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanMatchDeviceDescriptions_WithCapabilities()
    {
        var matchOneAndOneCap = new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithCapability("stringCap", "string");
        var matchOneCapInArray = new InputDeviceMatcher()
            .WithCapability("arrayCap[]", "second");
        var matchOneIntAndOneFloatCap = new InputDeviceMatcher()
            .WithCapability("floatCap", 1.234f)
            .WithCapability("intCap", 1234);
        var matchBoolCap = new InputDeviceMatcher()
            .WithCapability("boolCap", true);
        var matchEnumCap = new InputDeviceMatcher()
            .WithCapability("enumCap", TestDeviceCapabilities.EnumCaps.Second);
        var matchNestedCap = new InputDeviceMatcher()
            .WithCapability("nestedCap/value", "value");
        var matchStringCapWithRegex = new InputDeviceMatcher()
            .WithCapability("stringCap", ".*ng$");
        var matchIntCapWithRegex = new InputDeviceMatcher()
            .WithCapability("intCap", "1.*4");
        var matchIntCapWithString = new InputDeviceMatcher()
            .WithCapability("intCap", "1234");
        var matchFloatCapWithString = new InputDeviceMatcher()
            .WithCapability("floatCap", "1.234");
        var matchFloatWithExponentCapWithString = new InputDeviceMatcher()
            .WithCapability("floatCapWithExponent", "1.234e-10");
        var matchBoolCapWithString = new InputDeviceMatcher()
            .WithCapability("boolCap", "true");
        var matchNone = new InputDeviceMatcher()
            .WithCapability("intCap", 4567);

        var description = new InputDeviceDescription
        {
            interfaceName = "TestInterface",
            capabilities = new TestDeviceCapabilities
            {
                stringCap = "string",
                intCap = 1234,
                floatCap = 1.234f,
                floatCapWithExponent = 1.234e-10f,
                boolCap = true,
                nestedCap = new TestDeviceCapabilities.NestedCaps
                {
                    value = "value"
                },
                arrayCap = new[] { "first", "second" },
                enumCap = TestDeviceCapabilities.EnumCaps.Second,
            }.ToJson()
        };

        Assert.That(matchOneAndOneCap.MatchPercentage(description), Is.EqualTo(1).Within(0.0001));
        Assert.That(matchOneCapInArray.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchOneIntAndOneFloatCap.MatchPercentage(description), Is.EqualTo(1).Within(0.0001));
        Assert.That(matchBoolCap.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchEnumCap.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchNestedCap.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchStringCapWithRegex.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchIntCapWithRegex.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchIntCapWithString.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchFloatCapWithString.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchFloatWithExponentCapWithString.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchBoolCapWithString.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchNone.MatchPercentage(description), Is.EqualTo(0).Within(0.0001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDevicesByLayouts()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice("Keyboard");

        using (var matches = InputSystem.FindControls("/<gamepad>"))
        {
            Assert.That(matches, Has.Count.EqualTo(2));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad1));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad2));
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_RemovingDevice_UpdatesInternalDevicesIndices()
    {
        var device1 = InputSystem.AddDevice<Gamepad>();
        var device2 = InputSystem.AddDevice<Mouse>();
        var device3 = InputSystem.AddDevice<Keyboard>();

        InputSystem.RemoveDevice(device2);

        Assert.That(device1.m_DeviceIndex, Is.EqualTo(0));
        Assert.That(device2.m_DeviceIndex, Is.EqualTo(InputDevice.kInvalidDeviceIndex));
        Assert.That(device3.m_DeviceIndex, Is.EqualTo(1));
    }

    [Test]
    [Category("Devices")]
    public void Devices_RemovingDevice_CleansUpUpdateCallback()
    {
        var device = InputSystem.AddDevice<CustomDeviceWithUpdate>();
        InputSystem.RemoveDevice(device);

        InputSystem.Update();

        Assert.That(device.onUpdateCallCount, Is.Zero);
    }

    // Sadly, while this one is a respectable effort on InputManager's part, in practice it is limited in usefulness
    // by the fact that when native sends us the descriptor string, that very string will lead to a GC allocation and
    // thus already cause garbage (albeit a very small amount). At least InputManager isn't adding any to it, though.
    [Test]
    [Category("Devices")]
    [Retry(2)] // Warm up JIT
    public void Devices_RemovingAndReaddingDevice_DoesNotAllocateMemory()
    {
        var description =
            new InputDeviceDescription
        {
            deviceClass = "Gamepad",
            product = "TestProduct",
            manufacturer = "TestManufacturer"
        };

        var deviceId = runtime.ReportNewInputDevice(description);
        InputSystem.Update();

        // We allow the system to allocate memory the first time the removal happens. In particular,
        // the array we use to hold removed devices we only allocate the first time we need to put
        // something in it so we need one run to warm up the system. However, even the first re-adding
        // should not allocate.
        var removeEvent1 = DeviceRemoveEvent.Create(deviceId);
        InputSystem.QueueEvent(ref removeEvent1);
        InputSystem.Update();

        // Avoid GC hit from string allocation.
        var kProfilerRegion = "Devices_RemovingAndReaddingDevice_DoesNotAllocateMemory";

        // We don't want a GC hit from the InputDescription->JSON conversion we get from the test runtime.
        // Doesn't happen when a native backend reports a device.
        var descriptionJson = description.ToJson();

        Assert.That(() =>
        {
            Profiler.BeginSample(kProfilerRegion);

            // "Plug" it back in.
            deviceId = runtime.ReportNewInputDevice(descriptionJson);
            InputSystem.Update();

            // "Unplug" device.
            var removeEvent2 = DeviceRemoveEvent.Create(deviceId);
            InputSystem.QueueEvent(ref removeEvent2);
            InputSystem.Update();

            // "Plug" it back in.
            runtime.ReportNewInputDevice(descriptionJson);
            InputSystem.Update();

            Profiler.EndSample();
        }, Is.Not.AllocatingGCMemory());
    }

    [Test]
    [Category("Devices")]
    public void Devices_AreMadeCurrentWhenReceivingStateEvent()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        Assert.That(Gamepad.current, Is.Not.SameAs(gamepad1));

        InputSystem.QueueStateEvent(gamepad1, new GamepadState().WithButton(GamepadButton.A));
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad1));

        // Sending event that isn't a state event should result in no change.
        InputSystem.QueueConfigChangeEvent(gamepad2);
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad1));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AreNotMadeCurrentWhenReceivingStateEventWithNoControlsChanged()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad1, new GamepadState().WithButton(GamepadButton.A));
        InputSystem.Update();
        Assert.That(Gamepad.current, Is.SameAs(gamepad1));

        InputSystem.QueueStateEvent(gamepad2, new GamepadState().WithButton(GamepadButton.B));
        InputSystem.Update();
        Assert.That(Gamepad.current, Is.SameAs(gamepad2));

        InputSystem.QueueStateEvent(gamepad1, new GamepadState().WithButton(GamepadButton.A));
        InputSystem.Update();

        // If none of the controls changed, a state event shouldn't switch current gamepad.
        Assert.That(Gamepad.current, Is.SameAs(gamepad2));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFilterNoiseOnCurrent()
    {
        // Make left trigger on gamepads noisy.
        InputSystem.RegisterLayoutOverride(@"
            {
                ""name"" : ""LeftTriggerIsNoisy"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""leftTrigger"", ""noisy"" : true }
                ]
            }
        ");

        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        // Make sure the layout override came through okay.
        Assert.That(gamepad1.noisy, Is.True);
        Assert.That(gamepad1.leftTrigger.noisy, Is.True);
        Assert.That(gamepad1.rightTrigger.noisy, Is.False);
        Assert.That(Gamepad.current, Is.SameAs(gamepad2));

        // Send delta state without noise on first gamepad.
        InputSystem.QueueDeltaStateEvent(gamepad1.leftStick, new Vector2(0.123f, 0.234f));
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad1));

        // Send full state without noise on second gamepad.
        InputSystem.QueueStateEvent(gamepad2, new GamepadState {rightStick = new Vector2(0.123f, 0.234f)});
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad2));

        // Send delta state with only noise on first gamepad.
        InputSystem.QueueDeltaStateEvent(gamepad1.leftTrigger, 0.345f);
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad2)); // Should be unchanged.

        // Send full state with only noise on first gamepad.
        // NOTE: We already have non-default state on leftStick which we need to preserve.
        InputSystem.QueueStateEvent(gamepad1, new GamepadState { leftStick = new Vector2(0.123f, 0.234f), leftTrigger = 0.567f});
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad2)); // Should be unchanged.

        // Send full state with some noise on first gamepad.
        // NOTE: We already have non-default state on leftStick which we need to preserve.
        InputSystem.QueueStateEvent(gamepad1, new GamepadState { leftStick = new Vector2(0.345f, 0.456f), leftTrigger = 0.567f});
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad1));
    }

    // We currently do not read out actual values during noise detection. This means that any state change on a control
    // that isn't marked as noisy will pass the noise filter. If, for example, the sticks are wiggled but they are still
    // below deadzone threshold, they will still classify as carrying signal. To do that differently, we would have to
    // mirror processors back onto state or actually invoke the processors during noise filtering.
    [Test]
    [Category("Devices")]
    public void Devices_FilteringNoiseOnCurrentDoesNotTakeProcessorsIntoAccount()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Gamepad>();

        // Actuate leftStick below deadzone threshold.
        InputSystem.QueueStateEvent(gamepad1, new GamepadState { leftStick = new Vector2(0.001f, 0.001f)});
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad1));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AreUpdatedWithTimestampOfLastEvent()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        runtime.currentTime = 1234;
        runtime.currentTimeOffsetToRealtimeSinceStartup = 1123;

        // This can be anything above and beyond the simple default gamepad state.
        InputSystem.QueueStateEvent(device, new GamepadState { leftStick = Vector2.one });
        InputSystem.Update();

        // Externally visible time must be offset according to currentTimeOffsetToRealtimeSinceStartup.
        // Internal time is not offset.
        Assert.That(device.lastUpdateTime, Is.EqualTo(111).Within(0.00001));
        Assert.That(device.m_LastUpdateTimeInternal, Is.EqualTo(1234).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanSetPollingFrequency()
    {
        InputSystem.pollingFrequency = 120;

        Assert.That(runtime.pollingFrequency, Is.EqualTo(120).Within(0.000001));
        Assert.That(InputSystem.pollingFrequency, Is.EqualTo(120).Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_PollingFrequencyIs60HzByDefault()
    {
        Assert.That(InputSystem.pollingFrequency, Is.EqualTo(60).Within(0.000001));
        // Make sure InputManager passed the frequency on to the runtime.
        Assert.That(runtime.pollingFrequency, Is.EqualTo(60).Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanInterceptAndHandleDeviceCommands()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputDevice receivedDevice = null;
        FourCC? receivedCommandType = null;

        InputSystem.onDeviceCommand +=
            (device, commandPtr) =>
        {
            // Only handle first call.
            if (receivedDevice != null)
                return null;

            receivedDevice = device;
            receivedCommandType = commandPtr->type;

            // If we don't return null, should be considered handled.
            return InputDeviceCommand.GenericFailure;
        };

        var receivedRuntimeCommand = false;
        runtime.SetDeviceCommandCallback(gamepad,
            (id, command) =>
            {
                if (command->type == DualMotorRumbleCommand.Type)
                    receivedRuntimeCommand = true;
                return InputDeviceCommand.GenericFailure;
            });

        gamepad.SetMotorSpeeds(1, 1);

        Assert.That(receivedDevice, Is.SameAs(gamepad));
        Assert.That(receivedCommandType, Is.EqualTo(DualMotorRumbleCommand.Type));
        Assert.That(receivedRuntimeCommand, Is.False);

        gamepad.SetMotorSpeeds(0.5f, 0.5f);

        Assert.That(receivedRuntimeCommand, Is.True);
    }

    ////TODO: make this an interface that you can register *against* a device to take over its I/O handling
    // Ability to completely replace the data stream for an existing device. Suppresses all events
    // coming from the runtime.
    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_CanHijackEventStreamOfDevice()
    {
        //sends disable (or some other IOCTL?) to device that is hijacked
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]

    [TestCase(true, InputSettings.BackgroundBehavior.IgnoreFocus, InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)]
    [TestCase(true, InputSettings.BackgroundBehavior.ResetAndDisableAllDevices, InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)]
    [TestCase(true, InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices, InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)]

    [TestCase(false, InputSettings.BackgroundBehavior.IgnoreFocus, InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)]
    [TestCase(false, InputSettings.BackgroundBehavior.ResetAndDisableAllDevices, InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)]
    [TestCase(false, InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices, InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)]

    [TestCase(true, InputSettings.BackgroundBehavior.IgnoreFocus, InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)]
    [TestCase(true, InputSettings.BackgroundBehavior.ResetAndDisableAllDevices, InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)]
    [TestCase(true, InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices, InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)]

    [TestCase(false, InputSettings.BackgroundBehavior.IgnoreFocus, InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)]
    [TestCase(false, InputSettings.BackgroundBehavior.ResetAndDisableAllDevices, InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)]
    [TestCase(false, InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices, InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)]

    [TestCase(true, InputSettings.BackgroundBehavior.IgnoreFocus, InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)]
    [TestCase(true, InputSettings.BackgroundBehavior.ResetAndDisableAllDevices, InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)]
    [TestCase(true, InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices, InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)]

    [TestCase(false, InputSettings.BackgroundBehavior.IgnoreFocus, InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)]
    [TestCase(false, InputSettings.BackgroundBehavior.ResetAndDisableAllDevices, InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)]
    [TestCase(false, InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices, InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)]
    public unsafe void Devices_CanHandleFocusChanges(bool appRunInBackground, InputSettings.BackgroundBehavior backgroundBehavior, InputSettings.EditorInputBehaviorInPlayMode editorInputBehaviorInPlayMode)
    {
        // The constant leads to "Unreachable code detected" warnings.
        #pragma warning disable CS0162

        const bool kIsEditor =
        #if UNITY_EDITOR
            true;
        #else
            false;
        #endif

        // runInBackground=false in UNITY_EDITOR without gameViewFocus=ExactlyAsInPlayer is undefined as
        // we consider runInBackground to always be on in the editor.
        if (!appRunInBackground && kIsEditor && editorInputBehaviorInPlayMode != InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
            return;

        runtime.runInBackground = appRunInBackground;

        InputSystem.settings.backgroundBehavior = backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehaviorInPlayMode;

        // Add a device that is disabled from the get-go and should remain disabled.
        var sensorId = runtime.ReportNewInputDevice<Gyroscope>();
        runtime.SetDeviceCommandCallback(sensorId, (_, commandPtr) =>
        {
            if (commandPtr->type == QueryEnabledStateCommand.Type)
            {
                var queryPtr = (QueryEnabledStateCommand*)commandPtr;
                queryPtr->isEnabled = false;
                return InputDeviceCommand.GenericSuccess;
            }
            if (commandPtr->type == EnableDeviceCommand.Type)
                Assert.Fail("Device should never be enabled");
            return InputDeviceCommand.GenericFailure;
        });

        // Create one device through the native codepath.
        runtime.ReportNewInputDevice<TrackedDevice>();
        InputSystem.Update();

        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var joystick = InputSystem.AddDevice<Joystick>();
        var trackedDevice = InputSystem.GetDevice<TrackedDevice>();
        var sensor = InputSystem.GetDevice<Gyroscope>();

        // Add a device that we force-disable. Should remain disabled throughout.
        var disabledDevice = InputSystem.AddDevice<Pen>();
        InputSystem.DisableDevice(disabledDevice);

        // Put devices into some non-default state.
        Set(mouse.position, new Vector2(123, 234));
        Press(mouse.leftButton);
        Set(trackedDevice.devicePosition, new Vector3(234, 345, 456));
        Press(trackedDevice.isTracked);
        Press(keyboard.spaceKey);
        Set(gamepad.leftTrigger, 0.5f);
        Press(joystick.trigger);

        var commands = new List<string>();
        long DeviceCallback(string device, InputDeviceCommand* command, bool supportSyncCommand, bool canRunInBackground)
        {
            if (command->type == QueryCanRunInBackground.Type)
            {
                ((QueryCanRunInBackground*)command)->canRunInBackground = canRunInBackground;
                return InputDeviceCommand.GenericSuccess;
            }
            if (command->type == RequestSyncCommand.Type)
            {
                commands.Add("Sync " + device);
                return supportSyncCommand ? InputDeviceCommand.GenericSuccess : InputDeviceCommand.GenericFailure;
            }
            if (command->type == RequestResetCommand.Type)
            {
                commands.Add("Reset " + device);
                return InputDeviceCommand.GenericSuccess;
            }
            if (command->type == EnableDeviceCommand.Type)
            {
                commands.Add("Enable " + device);
                return InputDeviceCommand.GenericSuccess;
            }
            if (command->type == DisableDeviceCommand.Type)
            {
                commands.Add("Disable " + device);
                return InputDeviceCommand.GenericSuccess;
            }
            return InputDeviceCommand.GenericFailure;
        }

        runtime.SetDeviceCommandCallback(trackedDevice,
            (id, command) =>
                DeviceCallback("TrackedDevice", command, true, true));
        runtime.SetDeviceCommandCallback(mouse,
            (id, command) =>
                // Pretend we don't support syncs.
                DeviceCallback("Mouse", command, false, false));
        runtime.SetDeviceCommandCallback(gamepad,
            (id, command) =>
                DeviceCallback("Gamepad", command, true, true));
        runtime.SetDeviceCommandCallback(joystick,
            (id, command) =>
                DeviceCallback("Joystick", command, false, false));
        runtime.SetDeviceCommandCallback(keyboard,
            (id, command) =>
                DeviceCallback("Keyboard", command, true, false));

        var changes = new List<string>();
        void DeviceChangeCallback(InputDevice device, InputDeviceChange change)
        {
            var devicesOfType = InputSystem.devices.Where(d => d.GetType() == device.GetType());
            var name = devicesOfType.Count() > 1 ? $"{device.layout}{devicesOfType.IndexOf(device)+1}" : device.layout;
            changes.Add($"{change} {name}");
        }

        InputSystem.onDeviceChange += DeviceChangeCallback;

        var eventCount = 0;
        InputSystem.onEvent += (eventPtr, _) => ++ eventCount;

        Assert.That(trackedDevice.enabled, Is.True);
        Assert.That(mouse.enabled, Is.True);
        Assert.That(keyboard.enabled, Is.True);
        Assert.That(gamepad.enabled, Is.True);
        Assert.That(joystick.enabled, Is.True);
        Assert.That(sensor.enabled, Is.False);
        Assert.That(disabledDevice.enabled, Is.False);

        if (kIsEditor && editorInputBehaviorInPlayMode != InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
        {
            // In the editor, canRunInBackground is automatically adapted to account both for the fact that "background" means
            // "Game View not focused (but Unity is still running)" and that "Run In Background" is essentially always on.
            switch (editorInputBehaviorInPlayMode)
            {
                case InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus:
                    Assert.That(trackedDevice.canRunInBackground, Is.True);
                    Assert.That(mouse.canRunInBackground, Is.False);
                    Assert.That(gamepad.canRunInBackground, Is.True);
                    Assert.That(joystick.canRunInBackground, Is.True);
                    Assert.That(keyboard.canRunInBackground, Is.False);
                    break;

                case InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus:
                    Assert.That(trackedDevice.canRunInBackground, Is.False);
                    Assert.That(mouse.canRunInBackground, Is.False);
                    Assert.That(gamepad.canRunInBackground, Is.False);
                    Assert.That(joystick.canRunInBackground, Is.False);
                    Assert.That(keyboard.canRunInBackground, Is.False);
                    break;
            }
        }
        else
        {
            Assert.That(trackedDevice.canRunInBackground, Is.True);
            Assert.That(mouse.canRunInBackground, Is.False);
            Assert.That(gamepad.canRunInBackground, Is.True);
            Assert.That(joystick.canRunInBackground, Is.False);
            Assert.That(keyboard.canRunInBackground, Is.False);
        }

        // Lose focus.
        runtime.PlayerFocusLost();

        Assert.That(sensor.enabled, Is.False);
        Assert.That(disabledDevice.enabled, Is.False);

        if (!appRunInBackground && (!kIsEditor || editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView))
        {
            // No change on focus loss.

            Assert.That(trackedDevice.enabled, Is.True);
            Assert.That(mouse.enabled, Is.True);
            Assert.That(keyboard.enabled, Is.True);
            Assert.That(gamepad.enabled, Is.True);
            Assert.That(joystick.enabled, Is.True);

            Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
            Assert.That(mouse.leftButton.isPressed, Is.True);
            Assert.That(keyboard.spaceKey.isPressed, Is.True);
            Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5f).Using(FloatEqualityComparer.Instance));
            Assert.That(joystick.trigger.isPressed, Is.True);
            Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(234, 345, 456)));
            Assert.That(trackedDevice.isTracked.isPressed, Is.True);

            Assert.That(changes, Is.Empty);
            Assert.That(commands, Is.Empty);
        }
        else
        {
            switch (backgroundBehavior)
            {
                case InputSettings.BackgroundBehavior.IgnoreFocus:
                    Assert.That(trackedDevice.enabled, Is.True);
                    Assert.That(mouse.enabled, Is.True);
                    Assert.That(keyboard.enabled, Is.True);
                    Assert.That(gamepad.enabled, Is.True);
                    Assert.That(joystick.enabled, Is.True);

                    Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                    Assert.That(mouse.leftButton.isPressed, Is.True);
                    Assert.That(keyboard.spaceKey.isPressed, Is.True);
                    Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5f).Using(FloatEqualityComparer.Instance));
                    Assert.That(joystick.trigger.isPressed, Is.True);
                    Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(234, 345, 456)));
                    Assert.That(trackedDevice.isTracked.isPressed, Is.True);

                    Assert.That(changes, Is.Empty);
                    Assert.That(commands, Is.Empty);

                    break;

                case InputSettings.BackgroundBehavior.ResetAndDisableAllDevices:
                    Assert.That(trackedDevice.enabled, Is.False);
                    Assert.That(mouse.enabled, Is.False);
                    Assert.That(keyboard.enabled, Is.False);
                    Assert.That(gamepad.enabled, Is.False);
                    Assert.That(joystick.enabled, Is.False);

                    Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                    Assert.That(mouse.leftButton.isPressed, Is.False);
                    Assert.That(keyboard.spaceKey.isPressed, Is.False);
                    Assert.That(gamepad.leftTrigger.ReadValue(), Is.Zero);
                    Assert.That(joystick.trigger.isPressed, Is.False);
                    Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(234, 345, 456)));
                    Assert.That(trackedDevice.isTracked.isPressed, Is.False);

                    // Reset device -> Disable Device, Reset device -> disable device..., but also in a completely different order per
                    // device than in the expected list.
                    Assert.That(changes, Is.EqualTo(new[]
                    {
                        "SoftReset TrackedDevice", "Disabled TrackedDevice",
                        "SoftReset Mouse", "Disabled Mouse",
                        "HardReset Keyboard", "Disabled Keyboard",
                        "HardReset Gamepad", "Disabled Gamepad",
                        "HardReset Joystick", "Disabled Joystick"
                    }));
                    if (!kIsEditor || editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
                        Assert.That(commands, Is.EquivalentTo(new[] { "Disable Mouse", "Disable Keyboard", "Disable Gamepad", "Disable Joystick", "Disable TrackedDevice" }));
                    else
                        Assert.That(commands, Is.Empty); // We don't actually disable them in the backend as the editor will continue to receive data.

                    break;

                case InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices:
                    if (!kIsEditor || editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
                    {
                        Assert.That(trackedDevice.enabled, Is.True);
                        Assert.That(mouse.enabled, Is.False);
                        Assert.That(keyboard.enabled, Is.False);
                        Assert.That(gamepad.enabled, Is.True);
                        Assert.That(joystick.enabled, Is.False);

                        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                        Assert.That(mouse.leftButton.isPressed, Is.False);
                        Assert.That(keyboard.spaceKey.isPressed, Is.False);
                        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5f).Using(FloatEqualityComparer.Instance));
                        Assert.That(joystick.trigger.isPressed, Is.False);
                        Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(234, 345, 456)));
                        Assert.That(trackedDevice.isTracked.isPressed, Is.True);

                        Assert.That(changes, Is.EquivalentTo(new[]
                        {
                            "SoftReset Mouse", "HardReset Keyboard", "HardReset Joystick",
                            "Disabled Mouse", "Disabled Keyboard", "Disabled Joystick"
                        }));
                        if (!kIsEditor || editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
                            Assert.That(commands, Is.EquivalentTo(new[] { "Disable Mouse", "Disable Keyboard", "Disable Joystick" }));
                        else
                            Assert.That(commands, Is.Empty);
                    }

                    if (kIsEditor && editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)
                    {
                        Assert.That(trackedDevice.enabled, Is.True);
                        Assert.That(mouse.enabled, Is.False);
                        Assert.That(keyboard.enabled, Is.False);
                        Assert.That(gamepad.enabled, Is.True);
                        Assert.That(joystick.enabled, Is.True);

                        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                        Assert.That(mouse.leftButton.isPressed, Is.False);
                        Assert.That(keyboard.spaceKey.isPressed, Is.False);
                        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5f).Using(FloatEqualityComparer.Instance));
                        Assert.That(joystick.trigger.isPressed, Is.True);
                        Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(234, 345, 456)));
                        Assert.That(trackedDevice.isTracked.isPressed, Is.True);

                        Assert.That(changes, Is.EquivalentTo(new[]
                        {
                            "SoftReset Mouse", "HardReset Keyboard",
                            "Disabled Mouse", "Disabled Keyboard"
                        }));
                        Assert.That(commands, Is.Empty); // No actual disabling in backend.
                    }

                    if (kIsEditor && editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)
                    {
                        Assert.That(trackedDevice.enabled, Is.False);
                        Assert.That(mouse.enabled, Is.False);
                        Assert.That(keyboard.enabled, Is.False);
                        Assert.That(gamepad.enabled, Is.False);
                        Assert.That(joystick.enabled, Is.False);

                        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                        Assert.That(mouse.leftButton.isPressed, Is.False);
                        Assert.That(keyboard.spaceKey.isPressed, Is.False);
                        Assert.That(gamepad.leftTrigger.ReadValue(), Is.Zero);
                        Assert.That(joystick.trigger.isPressed, Is.False);
                        Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(234, 345, 456)));
                        Assert.That(trackedDevice.isTracked.isPressed, Is.False);

                        Assert.That(changes, Is.EquivalentTo(new[]
                        {
                            "SoftReset Mouse", "HardReset Keyboard", "HardReset Gamepad", "HardReset Joystick", "SoftReset TrackedDevice",
                            "Disabled Mouse", "Disabled Keyboard", "Disabled Gamepad", "Disabled Joystick", "Disabled TrackedDevice"
                        }));
                        Assert.That(commands, Is.Empty); // We don't actually disable them in the backend as the editor will continue to receive data.
                    }

                    break;
            }
        }

        changes.Clear();
        commands.Clear();

        InputDevice mouse2 = null;
        InputDevice mouse3 = null;
        InputDevice trackedDevice2 = null;

        // If we're set up to process input even when in the background, make sure
        // this is intact.
        if (appRunInBackground || kIsEditor)
        {
            // Queue a change on each device.
            Set(mouse.position, new Vector2(333, 444), queueEventOnly: true);
            Set(trackedDevice.devicePosition, new Vector3(444, 555, 666), queueEventOnly: true);
            Set(joystick.stick, new Vector2(0.666f, 0.777f), queueEventOnly: true);
            Press(gamepad.buttonNorth, queueEventOnly: true);
            Press(keyboard.aKey, queueEventOnly: true);

            // runInBackground=true, ResetAndDisableAllDevices, OnlyPointerAndKeyboard
            // (a) why don't we early out of event processing?
            // (b) why are the devices not disabled?

            // Run one player update.
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Dynamic),
                "Expecting to run a player update");

            Assert.That(changes, Is.Empty);
            Assert.That(commands, Is.Empty);

            switch (backgroundBehavior)
            {
                case InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices:

                    if (appRunInBackground && (!kIsEditor || editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView))
                    {
                        // Only background devices should have been affected.
                        Assert.That(eventCount, Is.EqualTo(2));
                        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                        Assert.That(keyboard.aKey.isPressed, Is.False);
                        Assert.That(gamepad.buttonNorth.isPressed, Is.True); // canRunInBackground
                        Assert.That(joystick.stick.ReadValue(), Is.EqualTo(Vector2.zero));
                        Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(444, 555, 666))); // canRunInBackground
                    }

                    if (kIsEditor && editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)
                    {
                        // All devices should be unaffected.
                        Assert.That(eventCount, Is.Zero);
                        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                        Assert.That(keyboard.aKey.isPressed, Is.False);
                        Assert.That(gamepad.buttonNorth.isPressed, Is.False);
                        Assert.That(joystick.stick.ReadValue(), Is.EqualTo(Vector2.zero));
                        Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(234, 345, 456)));
                    }

                    if (kIsEditor && editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)
                    {
                        // All devices except for mouse and keyboard should be affected.
                        Assert.That(eventCount, Is.EqualTo(3));
                        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                        Assert.That(keyboard.aKey.isPressed, Is.False);
                        Assert.That(gamepad.buttonNorth.isPressed, Is.True);
                        Assert.That(joystick.stick.ReadValue(),
                            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.666f, 0.777f))));
                        Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(444, 555, 666)));
                    }

                    break;

                case InputSettings.BackgroundBehavior.ResetAndDisableAllDevices:

                    // All devices should be unaffected.
                    Assert.That(eventCount, Is.Zero);
                    Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                    Assert.That(keyboard.aKey.isPressed, Is.False);
                    Assert.That(gamepad.buttonNorth.isPressed, Is.False);
                    Assert.That(joystick.stick.ReadValue(), Is.EqualTo(Vector2.zero));
                    Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(234, 345, 456)));

                    break;

                case InputSettings.BackgroundBehavior.IgnoreFocus:

                    if (!kIsEditor || editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
                    {
                        if (appRunInBackground)
                        {
                            // All devices should have been affected.
                            Assert.That(eventCount, Is.EqualTo(5));
                            Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(333, 444)));
                            Assert.That(keyboard.aKey.isPressed, Is.True);
                            Assert.That(gamepad.buttonNorth.isPressed, Is.True);
                            Assert.That(joystick.stick.ReadValue(),
                                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.666f, 0.777f))));
                            Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(444, 555, 666)));
                        }
                        else
                        {
                            // All devices should be unaffected.
                            Assert.That(eventCount, Is.Zero);
                            Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                            Assert.That(keyboard.aKey.isPressed, Is.False);
                            Assert.That(gamepad.buttonNorth.isPressed, Is.False);
                            Assert.That(joystick.stick.ReadValue(), Is.EqualTo(Vector2.zero));
                            Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(234, 345, 456)));
                        }
                    }

                    if (kIsEditor && editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)
                    {
                        // All devices should be unaffected.
                        Assert.That(eventCount, Is.Zero);
                        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                        Assert.That(keyboard.aKey.isPressed, Is.False);
                        Assert.That(gamepad.buttonNorth.isPressed, Is.False);
                        Assert.That(joystick.stick.ReadValue(), Is.EqualTo(Vector2.zero));
                        Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(234, 345, 456)));
                    }

                    if (kIsEditor && editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)
                    {
                        // All devices except for mouse and keyboard should be affected.
                        Assert.That(eventCount, Is.EqualTo(3));
                        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
                        Assert.That(keyboard.aKey.isPressed, Is.False);
                        Assert.That(gamepad.buttonNorth.isPressed, Is.True);
                        Assert.That(joystick.stick.ReadValue(),
                            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.666f, 0.777f))));
                        Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(444, 555, 666)));
                    }

                    break;
            }

            // In the editor, some of the input may have been deferred to the next editor update.
            // Make sure we see the input coming through.
            if (kIsEditor)
            {
                // Run another empty player update to make sure we don't end up losing the input we've deferred.
                eventCount = 0;
                InputSystem.Update(InputUpdateType.Dynamic);
                Assert.That(eventCount, Is.Zero); // No further events should have been processed.

                // Now run an editor update to force the input through.
                eventCount = 0;
                InputSystem.Update(InputUpdateType.Editor);

                Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Editor));

                switch (editorInputBehaviorInPlayMode)
                {
                    case InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus:
                        if (backgroundBehavior == InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices ||
                            backgroundBehavior == InputSettings.BackgroundBehavior.IgnoreFocus)
                        {
                            // Pointer and mouse input should have gone to editor.
                            Assert.That(eventCount, Is.EqualTo(2));
                            Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(333, 444)));
                            Assert.That(keyboard.aKey.isPressed, Is.True);
                            Assert.That(gamepad.buttonNorth.isPressed, Is.False);
                            Assert.That(joystick.stick.ReadValue(), Is.EqualTo(Vector2.zero));
                            Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(Vector3.zero));
                        }
                        else
                        {
                            // All input should have gone to editor.
                            Assert.That(eventCount, Is.EqualTo(5));
                            Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(333, 444)));
                            Assert.That(keyboard.aKey.isPressed, Is.True);
                            Assert.That(gamepad.buttonNorth.isPressed, Is.True);
                            Assert.That(joystick.stick.ReadValue(),
                                Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.666f, 0.777f))));
                            Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(444, 555, 666)));
                        }
                        break;

                    case InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus:
                        // All input should have gone to editor.
                        Assert.That(eventCount, Is.EqualTo(5));
                        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(333, 444)));
                        Assert.That(keyboard.aKey.isPressed, Is.True);
                        Assert.That(gamepad.buttonNorth.isPressed, Is.True);
                        Assert.That(joystick.stick.ReadValue(),
                            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.666f, 0.777f))));
                        Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(444, 555, 666)));
                        break;

                    case InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView:
                        // No input should have been deferred.
                        Assert.That(eventCount, Is.Zero);
                        break;
                }
            }

            // If we're in the background, make sure we handle adding and removing devices properly.

            // Add a pointer device that can run in the background. Need to do this view ReportNewInputDevice
            // as we have to have the command callback in place by the time we add the device.
            var mouse2Id = runtime.ReportNewInputDevice<Mouse>();
            runtime.SetDeviceCommandCallback(mouse2Id,
                (id, command) =>
                    DeviceCallback("Mouse2", command, true, true));
            InputSystem.Update(InputUpdateType.Dynamic);
            mouse2 = (Mouse)InputSystem.GetDeviceById(mouse2Id);

            // Add device that can't run in the background.
            mouse3 = InputSystem.AddDevice<Mouse>();
            runtime.SetDeviceCommandCallback(mouse3,
                (id, command) =>
                    DeviceCallback("Mouse3", command, false, false));

            // Add another device that can run in the background.
            var trackedDevice2Id = runtime.ReportNewInputDevice<TrackedDevice>();
            runtime.SetDeviceCommandCallback(trackedDevice2Id,
                (id, command) =>
                    DeviceCallback("TrackedDevice2", command, false, true));
            InputSystem.Update(InputUpdateType.Dynamic);
            trackedDevice2 = InputSystem.GetDeviceById(trackedDevice2Id);

            if (!appRunInBackground)
            {
                Assert.That(mouse2.enabled, Is.True);
                Assert.That(mouse3.enabled, Is.True);
                Assert.That(trackedDevice2.enabled, Is.True);
            }
            else
            {
                switch (backgroundBehavior)
                {
                    case InputSettings.BackgroundBehavior.IgnoreFocus:
                        Assert.That(mouse2.enabled, Is.True);
                        Assert.That(mouse3.enabled, Is.True);
                        Assert.That(trackedDevice2.enabled, Is.True);
                        break;

                    case InputSettings.BackgroundBehavior.ResetAndDisableAllDevices:
                        Assert.That(mouse2.enabled, Is.False);
                        Assert.That(mouse3.enabled, Is.False);
                        Assert.That(trackedDevice2.enabled, Is.False);
                        break;

                    case InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices:
                        if (!kIsEditor || editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
                        {
                            Assert.That(mouse2.enabled, Is.True);
                            Assert.That(mouse3.enabled, Is.False);
                            Assert.That(trackedDevice2.enabled, Is.True);
                        }
                        if (kIsEditor && editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)
                        {
                            Assert.That(mouse2.enabled, Is.False);
                            Assert.That(mouse3.enabled, Is.False);
                            Assert.That(trackedDevice2.enabled, Is.False);
                        }
                        if (kIsEditor && editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)
                        {
                            Assert.That(mouse2.enabled, Is.False);
                            Assert.That(mouse3.enabled, Is.False);
                            Assert.That(trackedDevice2.enabled, Is.True);
                        }
                        break;
                }
            }

            ////TODO: test manually enabling disabled device
        }

        changes.Clear();
        commands.Clear();

        // Regain focus.
        runtime.PlayerFocusGained();

        Assert.That(sensor.enabled, Is.False);
        Assert.That(disabledDevice.enabled, Is.False);

        if (!appRunInBackground && (!kIsEditor || editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView))
        {
            // No change on focus gain.

            Assert.That(trackedDevice.enabled, Is.True);
            Assert.That(mouse.enabled, Is.True);
            Assert.That(keyboard.enabled, Is.True);
            Assert.That(gamepad.enabled, Is.True);
            Assert.That(joystick.enabled, Is.True);

            if (kIsEditor)
            {
                Assert.That(trackedDevice2.enabled, Is.True);
                Assert.That(mouse2.enabled, Is.True);
                Assert.That(mouse3.enabled, Is.True);
            }

            Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(123, 234)));
            Assert.That(mouse.leftButton.isPressed, Is.False);
            Assert.That(joystick.trigger.isPressed, Is.False);
            Assert.That(keyboard.spaceKey.isPressed, Is.True);
            Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5f).Using(FloatEqualityComparer.Instance));
            Assert.That(trackedDevice.devicePosition.ReadValue(), Is.EqualTo(new Vector3(234, 345, 456)));
            Assert.That(trackedDevice.isTracked.isPressed, Is.True);

            // Enabled devices that do not support syncs will have seen resets.
            if (kIsEditor)
                Assert.That(changes, Is.EquivalentTo(new[] { "SoftReset Mouse1", "SoftReset TrackedDevice2", "SoftReset Mouse3", "HardReset Joystick" }));
            else
                Assert.That(changes, Is.EquivalentTo(new[] { "SoftReset Mouse", "HardReset Joystick" }));

            // All enabled devices should have received sync requests.
            if (kIsEditor)
                Assert.That(commands,
                    Is.EquivalentTo(new[]
                    {
                        "Sync Mouse", "Sync Mouse2", "Sync Mouse3", "Sync TrackedDevice", "Sync TrackedDevice2",
                        "Sync Keyboard", "Sync Gamepad", "Sync Joystick",
                        "Reset Joystick"
                    }));
            else
                Assert.That(commands,
                    Is.EquivalentTo(new[]
                    {
                        "Sync Mouse", "Sync TrackedDevice",
                        "Sync Keyboard", "Sync Gamepad", "Sync Joystick",
                        "Reset Joystick"
                    }));
        }
        else
        {
            Assert.That(trackedDevice.enabled, Is.True);
            Assert.That(trackedDevice2.enabled, Is.True);
            Assert.That(mouse.enabled, Is.True);
            Assert.That(mouse2.enabled, Is.True);
            Assert.That(mouse3.enabled, Is.True);
            Assert.That(keyboard.enabled, Is.True);
            Assert.That(gamepad.enabled, Is.True);
            Assert.That(joystick.enabled, Is.True);

            switch (backgroundBehavior)
            {
                case InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices:

                    if (!kIsEditor || editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
                    {
                        // All non-canRunInBackground devices are re-enabled and receive a sync request.

                        Assert.That(commands,
                            Is.EquivalentTo(new[]
                            {
                                // Going all the way to the backend so we're seeing all this in actual
                                // commands here.
                                "Enable Mouse", "Sync Mouse",
                                "Enable Mouse3", "Sync Mouse3",
                                "Enable Keyboard", "Sync Keyboard",
                                "Enable Joystick", "Sync Joystick",
                                "Reset Joystick"
                            }));

                        Assert.That(changes, Is.EquivalentTo(new[]
                        {
                            // Enabled devices that don't support syncs get reset.
                            "SoftReset Mouse1", "SoftReset Mouse3", "HardReset Joystick",

                            "Enabled Mouse1", "Enabled Mouse3",
                            "Enabled Keyboard", "Enabled Joystick"
                        }));
                    }

                    if (kIsEditor && editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus)
                    {
                        // Pointers and keyboards are re-enabled and receive a sync request.

                        Assert.That(commands,
                            Is.EquivalentTo(new[]
                            {
                                "Sync Mouse", "Sync Mouse2", "Sync Mouse3",
                                "Sync Keyboard",
                            }));

                        Assert.That(changes, Is.EquivalentTo(new[]
                        {
                            // Enabled devices that don't support syncs get reset.
                            "SoftReset Mouse1", "SoftReset Mouse3",

                            "Enabled Mouse1", "Enabled Mouse2", "Enabled Mouse3", "Enabled Keyboard"
                        }));
                    }

                    if (kIsEditor && editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus)
                    {
                        // All devices re-enabled and receive a sync request.

                        Assert.That(commands,
                            Is.EquivalentTo(new[]
                            {
                                "Sync Gamepad", "Sync Joystick",
                                "Sync TrackedDevice", "Sync TrackedDevice2",
                                "Sync Mouse", "Sync Mouse2", "Sync Mouse3",
                                "Sync Keyboard"
                            }));

                        Assert.That(changes, Is.EquivalentTo(new[]
                        {
                            // Enabled devices that don't support syncs get reset.
                            "SoftReset Mouse1", "SoftReset Mouse3", "HardReset Joystick", "SoftReset TrackedDevice2",

                            "Enabled Gamepad", "Enabled Joystick",
                            "Enabled TrackedDevice1", "Enabled TrackedDevice2",
                            "Enabled Mouse1", "Enabled Mouse2", "Enabled Mouse3",
                            "Enabled Keyboard"
                        }));
                    }

                    break;

                case InputSettings.BackgroundBehavior.ResetAndDisableAllDevices:

                    // All devices re-enabled and receive a sync request.

                    if (!kIsEditor || editorInputBehaviorInPlayMode == InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
                    {
                        Assert.That(commands,
                            Is.EquivalentTo(new[]
                            {
                                "Sync Gamepad", "Sync Joystick",
                                "Sync TrackedDevice", "Sync TrackedDevice2",
                                "Sync Mouse", "Sync Mouse2", "Sync Mouse3",
                                "Sync Keyboard",

                                "Enable Gamepad", "Enable Joystick",
                                "Enable TrackedDevice", "Enable TrackedDevice2",
                                "Enable Mouse", "Enable Mouse2", "Enable Mouse3",
                                "Enable Keyboard",

                                // Devices that don't support syncs and don't have dontReset controls should have seen resets.
                                "Reset Joystick"
                            }));
                    }
                    else
                    {
                        Assert.That(commands,
                            Is.EquivalentTo(new[]
                            {
                                "Sync Gamepad", "Sync Joystick",
                                "Sync TrackedDevice", "Sync TrackedDevice2",
                                "Sync Mouse", "Sync Mouse2", "Sync Mouse3",
                                "Sync Keyboard"
                            }));
                    }

                    Assert.That(changes, Is.EquivalentTo(new[]
                    {
                        // Enabled devices that don't support syncs get reset.
                        "SoftReset Mouse1", "SoftReset Mouse3", "HardReset Joystick", "SoftReset TrackedDevice2",

                        "Enabled Gamepad", "Enabled Joystick",
                        "Enabled TrackedDevice1", "Enabled TrackedDevice2",
                        "Enabled Mouse1", "Enabled Mouse2", "Enabled Mouse3",
                        "Enabled Keyboard"
                    }));

                    break;

                case InputSettings.BackgroundBehavior.IgnoreFocus:
                    Assert.That(changes, Is.Empty);
                    Assert.That(commands, Is.Empty);
                    break;
            }
        }

        #pragma warning restore CS0162
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanMarkDeviceAsBeingAbleToRunInBackground_ThroughIOCTL()
    {
        var deviceId = runtime.ReportNewInputDevice<Gamepad>();
        runtime.SetCanRunInBackground(deviceId);

        InputSystem.Update();

        var gamepad = (Gamepad)InputSystem.GetDeviceById(deviceId);
        Assert.That(gamepad.canRunInBackground, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanMarkDeviceAsBeingAbleToRunInBackground_ThroughLayout()
    {
        const string json = @"
            {
                ""name"" : ""TestDevice"",
                ""extend"" : ""Gamepad"",
                ""runInBackground"" : ""enabled""
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputSystem.AddDevice("TestDevice");
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(device.canRunInBackground, Is.True);
        Assert.That(gamepad.canRunInBackground, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanListenForIMECompositionEvents()
    {
        const string imeCompositionCharacters = "CompositionTestCharacters! ɝ";
        var callbackWasCalled = false;

        var keyboard = InputSystem.AddDevice<Keyboard>();
        keyboard.onIMECompositionChange += composition =>
        {
            Assert.That(callbackWasCalled, Is.False);
            callbackWasCalled = true;
            Assert.AreEqual(composition.ToString(), imeCompositionCharacters);
        };

        var inputEvent = IMECompositionEvent.Create(keyboard.deviceId, imeCompositionCharacters,
            InputRuntime.s_Instance.currentTime);
        InputSystem.QueueEvent(ref inputEvent);
        InputSystem.Update();

        Assert.That(callbackWasCalled, Is.True);
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanEnableAndDisableIME()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        bool? receivedIMEEnabledValue = null;
        runtime.SetDeviceCommandCallback(keyboard.deviceId,
            (id, commandPtr) =>
            {
                if (commandPtr->type == EnableIMECompositionCommand.Type)
                {
                    Assert.That(receivedIMEEnabledValue, Is.Null);
                    receivedIMEEnabledValue = ((EnableIMECompositionCommand*)commandPtr)->imeEnabled;
                    return InputDeviceCommand.GenericSuccess;
                }

                return InputDeviceCommand.GenericFailure;
            });

        keyboard.SetIMEEnabled(true);

        Assert.That(receivedIMEEnabledValue, Is.True);

        receivedIMEEnabledValue = null;

        keyboard.SetIMEEnabled(false);

        Assert.That(receivedIMEEnabledValue, Is.False);
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanSetIMECursorPositionOnKeyboard()
    {
        var commandWasSent = false;

        var keyboard = InputSystem.AddDevice<Keyboard>();

        runtime.SetDeviceCommandCallback(keyboard.deviceId,
            (id, commandPtr) =>
            {
                if (commandPtr->type == SetIMECursorPositionCommand.Type)
                {
                    Assert.That(commandWasSent, Is.False);
                    commandWasSent = true;

                    var command = *(SetIMECursorPositionCommand*)commandPtr;
                    Assert.AreEqual(Vector2.one, command.position);
                    return InputDeviceCommand.GenericSuccess;
                }

                return InputDeviceCommand.GenericFailure;
            });

        ////REVIEW: should this require IME to be enabled?
        keyboard.SetIMECursorPosition(Vector2.one);
        Assert.That(commandWasSent, Is.True);
    }

    // https://fogbugz.unity3d.com/f/cases/1340793/
    [Test]
    [Category("Devices")]
    public void Devices_CanWriteStateOfKeyboardSyntheticModifierKeys()
    {
        // DiscreteButtons (which, amongst other things, we use for the keyboard's ctrl/shift/alt
        // combined left+right modifiers) are generally not easily writable as their on and off
        // states may correspond to multiple possible input states. However, for the keyboard, that
        // is not really the case as we can simply consider
        //
        //   0 ==> both left and right key are set to 0
        //   1 ==> both left and right key are set to 1

        var keyboard = InputSystem.AddDevice<Keyboard>();

        Press(keyboard.ctrlKey);
        Press(keyboard.shiftKey);
        Press(keyboard.altKey);

        Assert.That(keyboard.leftCtrlKey.isPressed, Is.True);
        Assert.That(keyboard.rightCtrlKey.isPressed, Is.True);
        Assert.That(keyboard.leftShiftKey.isPressed, Is.True);
        Assert.That(keyboard.rightShiftKey.isPressed, Is.True);
        Assert.That(keyboard.leftAltKey.isPressed, Is.True);
        Assert.That(keyboard.rightAltKey.isPressed, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_RemovingKeyboardMakesNextKeyboardCurrent()
    {
        var keyboard1 = InputSystem.AddDevice<Keyboard>();
        Press(keyboard1.spaceKey);
        Assert.That(Keyboard.current, Is.EqualTo(keyboard1));

        var keyboard2 = InputSystem.AddDevice<Keyboard>();
        Press(keyboard2.spaceKey);
        Assert.That(Keyboard.current, Is.EqualTo(keyboard2));

        InputSystem.RemoveDevice(keyboard2);
        Assert.That(Keyboard.current, Is.EqualTo(keyboard1));
    }

    [Test]
    [Category("Devices")]
    public void Devices_RemovingDevice_MakesNextDeviceOfTypeCurrent()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        Press(mouse.leftButton);
        Assert.That(Pointer.current, Is.EqualTo(mouse));

        var pointer = InputSystem.AddDevice<Pointer>();
        Move(pointer.position, Vector2.right);
        Assert.That(Pointer.current, Is.EqualTo(pointer));

        InputSystem.RemoveDevice(pointer);
        Assert.That(Pointer.current, Is.EqualTo(mouse));
    }

    [Test]
    [Category("Devices")]
    public void Devices_EventMerging_CanMergeMouseMoveEvents()
    {
        var moveAction = new InputAction("Move", binding: "<Mouse>/position");
        int performedCount = 0;
        Vector2 mousePosition = Vector2.zero;
        moveAction.performed += ctx =>
        {
            performedCount++;
            mousePosition = ctx.ReadValue<Vector2>();
        };
        moveAction.Enable();

        var mouse = InputSystem.AddDevice<Mouse>();

        InputSystem.QueueStateEvent(mouse,
            new MouseState
            {
                position = new Vector2(1.0f, 2.0f),
                delta = new Vector2(1.0f, 1.0f),
            }, time: 1);
        InputSystem.QueueStateEvent(mouse,
            new MouseState
            {
                position = new Vector2(2.0f, 3.0f),
                delta = new Vector2(1.0f, 1.0f),
            }, time: 2);
        InputSystem.QueueStateEvent(mouse,
            new MouseState
            {
                position = new Vector2(3.0f, 4.0f),
                delta = new Vector2(1.0f, 1.0f),
            }, time: 3);

        InputSystem.Update();

        Assert.That(performedCount, Is.EqualTo(1));
        Assert.That(mousePosition, Is.EqualTo(new Vector2(3, 4)));
        Assert.That(Mouse.current.delta.ReadValue(), Is.EqualTo(new Vector2(3, 3)));
    }

    [Test]
    [Category("Devices")]
    public void Devices_EventMerging_OnlyConsecutiveMoveEventsAreMerged()
    {
        var moveAction = new InputAction("Move", binding: "<Mouse>/position");
        var clickAction = new InputAction("Click", binding: "<Mouse>/leftButton");
        var keyPressAction = new InputAction("KeyPress", binding: "<Keyboard>/space");

        var movePerformedCount = 0;
        var clickPerformedCount = 0;
        var keyPressPerformedCount = 0;

        var mousePositionAtClickEvent = Vector2.zero;
        var mouseDeltaAtClickEvent = Vector2.zero;
        var mouseScrollAtClickEvent = Vector2.zero;

        var mousePositionAtKeyPressEvent = Vector2.zero;
        var mouseDeltaAtKeyPressEvent = Vector2.zero;
        var mouseScrollAtKeyPressEvent = Vector2.zero;

        moveAction.performed += ctx =>
        {
            movePerformedCount++;
        };
        clickAction.performed += ctx =>
        {
            clickPerformedCount++;
            mousePositionAtClickEvent = Mouse.current.position.ReadValue();
            mouseDeltaAtClickEvent = Mouse.current.delta.ReadValue();
            mouseScrollAtClickEvent = Mouse.current.scroll.ReadValue();
        };
        keyPressAction.performed += ctx =>
        {
            keyPressPerformedCount++;
            mousePositionAtKeyPressEvent = Mouse.current.position.ReadValue();
            mouseDeltaAtKeyPressEvent = Mouse.current.delta.ReadValue();
            mouseScrollAtKeyPressEvent = Mouse.current.scroll.ReadValue();
        };

        moveAction.Enable();
        clickAction.Enable();
        keyPressAction.Enable();

        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        InputSystem.QueueStateEvent(mouse,
            new MouseState
            {
                position = new Vector2(1.0f, 2.0f),
                delta = Vector2.one,
                scroll = Vector2.one
            }, time: 1);
        InputSystem.QueueStateEvent(mouse,
            new MouseState
            {
                position = new Vector2(2.0f, 3.0f),
                delta = Vector2.one,
                scroll = Vector2.one
            }, time: 2);

        // inject a click event.
        InputSystem.QueueStateEvent(mouse,
            new MouseState
            {
                position = new Vector2(3.0f, 4.0f),
                delta = Vector2.one,
                scroll = Vector2.one
            }.WithButton(MouseButton.Left), time: 3);
        InputSystem.QueueStateEvent(mouse,
            new MouseState
            {
                position = new Vector2(4.0f, 5.0f),
                delta = Vector2.one,
                scroll = Vector2.one,
            }, time: 4);
        InputSystem.QueueStateEvent(mouse,
            new MouseState
            {
                position = new Vector2(5.0f, 6.0f),
                delta = Vector2.one,
                scroll = Vector2.one,
            }, time: 5);

        // inject a keyboard press event
        InputSystem.QueueStateEvent(keyboard,
            new KeyboardState(Key.Space), time: 6);
        InputSystem.QueueStateEvent(mouse,
            new MouseState
            {
                position = new Vector2(6.0f, 7.0f),
                delta = Vector2.one,
                scroll = Vector2.one,
            }, time: 7);
        InputSystem.QueueStateEvent(mouse,
            new MouseState
            {
                position = new Vector2(7.0f, 8.0f),
                delta = Vector2.one,
                scroll = Vector2.one,
            }, time: 8);

        InputSystem.Update();

        Assert.That(movePerformedCount, Is.EqualTo(4));
        Assert.That(clickPerformedCount, Is.EqualTo(1));
        Assert.That(keyPressPerformedCount, Is.EqualTo(1));

        Assert.That(Mouse.current.position.ReadValue(), Is.EqualTo(new Vector2(7, 8)));
        Assert.That(Mouse.current.delta.ReadValue(), Is.EqualTo(new Vector2(7, 7)));
        Assert.That(Mouse.current.scroll.ReadValue(), Is.EqualTo(new Vector2(7, 7)));

        Assert.That(mousePositionAtClickEvent, Is.EqualTo(new Vector2(3, 4)));
        Assert.That(mouseDeltaAtClickEvent, Is.EqualTo(new Vector2(3, 3)));
        Assert.That(mouseScrollAtClickEvent, Is.EqualTo(new Vector2(3, 3)));

        Assert.That(mousePositionAtKeyPressEvent, Is.EqualTo(new Vector2(5, 6)));
        Assert.That(mouseDeltaAtKeyPressEvent, Is.EqualTo(new Vector2(5, 5)));
        Assert.That(mouseScrollAtKeyPressEvent, Is.EqualTo(new Vector2(5, 5)));
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PreProcessorTestDeviceState : IInputStateTypeInfo
    {
        public static FourCC Format => new FourCC('P', 'P', 'T', 'D');
        public FourCC format => Format;

        [InputControl(layout = "Integer")] public int value1;
        [InputControl(layout = "Integer")] public int value2;
    }

    [InputControlLayout(stateType = typeof(PreProcessorTestDeviceState))]
    public class PreProcessorTestDevice : InputDevice, IEventPreProcessor
    {
        public enum Behavior
        {
            PreserveEventsAsIs,
            SkipEvents,
            ConvertEventsInPlace,
            ShrinkEventsInPlace,
            GrowEventsInPlace
        }

        public Behavior behavior = Behavior.PreserveEventsAsIs;

        public IntegerControl value1 { get; private set; }
        public IntegerControl value2 { get; private set; }

        protected override void FinishSetup()
        {
            value1 = GetChildControl<IntegerControl>("value1");
            value2 = GetChildControl<IntegerControl>("value2");
            base.FinishSetup();
        }

        unsafe bool IEventPreProcessor.PreProcessEvent(InputEventPtr currentEventPtr)
        {
            var inputEvent = (InputEvent*)currentEventPtr;
            var stateEvent = StateEvent.FromUnchecked(inputEvent);
            var size = stateEvent->stateSizeInBytes;
            if (stateEvent->stateFormat != PreProcessorTestDeviceState.Format || size < sizeof(PreProcessorTestDeviceState))
                return false;

            var state = (PreProcessorTestDeviceState*)stateEvent->state;
            switch (behavior)
            {
                case Behavior.SkipEvents:
                    return false;
                case Behavior.PreserveEventsAsIs:
                    return true;
                case Behavior.ConvertEventsInPlace:
                    state->value1 = -state->value1;
                    state->value2 = -state->value2;
                    return true;
                case Behavior.ShrinkEventsInPlace:
                    state->value1 = -state->value1;
                    state->value2 = int.MaxValue; // This change should not be visible in device state

                    // This code shrinks down a state event in-place without changing a type signature,
                    // it relies on a lose definition of state updating in InputManager.UpdateState where passing
                    // a state event of size smaller than device state size will only update first N bytes,
                    // in a way working similarly to delta state events with offset=0.
                    //
                    // In case if we move to strongly typed events this will not work and will need to be redesigned,
                    // one option could be having two different FourCC state types and shrinking will convert to another type.
                    inputEvent->sizeInBytes -= 4;
                    return true;
                case Behavior.GrowEventsInPlace:
                    state->value1 = -state->value1;
                    state->value2 = -state->value2;

                    // Potentially we're creating out-of-bounds memory access here,
                    // but we rely on a size check following PreProcessEvent to early out our test with an exception.
                    inputEvent->sizeInBytes += 1;
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    };

    [Test]
    [Category("Devices")]
    [TestCase(PreProcessorTestDevice.Behavior.PreserveEventsAsIs, 10, 20)]
    [TestCase(PreProcessorTestDevice.Behavior.SkipEvents, 0, 0)]
    [TestCase(PreProcessorTestDevice.Behavior.ConvertEventsInPlace, -10, -20)]
    [TestCase(PreProcessorTestDevice.Behavior.ShrinkEventsInPlace, -10, 0)]
#if UNITY_EDITOR
    [TestCase(PreProcessorTestDevice.Behavior.GrowEventsInPlace, 0, 0, true)]
#endif
    public void Devices_EventPreProcessor_Can(PreProcessorTestDevice.Behavior secondEventBehavior, int expectedValue1, int expectedValue2, bool expectAccessViolationException = false)
    {
        var device = InputSystem.AddDevice<PreProcessorTestDevice>();
        device.behavior = secondEventBehavior;

        InputSystem.QueueStateEvent(device, new PreProcessorTestDeviceState() {value1 = 10, value2 = 20});
        if (!expectAccessViolationException)
            InputSystem.Update();
        else
        {
            LogAssert.Expect(LogType.Exception, "AccessViolationException: 'PreProcessorTestDevice:/PreProcessorTestDevice'.PreProcessEvent tries to grow an event from 32 bytes to 33 bytes, this will potentially corrupt events after the current event and/or cause out-of-bounds memory access.");
            LogAssert.Expect(LogType.Error, "AccessViolationException during event processing of Dynamic update; resetting event buffer");
            Assert.That(() => InputSystem.Update(), Throws.TypeOf<AccessViolationException>());
        }

        Assert.That(device.value1.ReadValue(), Is.EqualTo(expectedValue1));
        Assert.That(device.value2.ReadValue(), Is.EqualTo(expectedValue2));
    }
}
