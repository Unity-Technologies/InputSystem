using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.DualShock;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using Gyroscope = UnityEngine.Experimental.Input.Gyroscope;

#if UNITY_2018_3_OR_NEWER
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
#endif

////TODO: test that device re-creation doesn't lose flags and such

partial class CoreTests
{
    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDevice_FromLayout()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var device = setup.Finish();

        Assert.That(device, Is.TypeOf<Gamepad>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftStick"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDevice_WithNestedState()
    {
        InputSystem.RegisterLayout<CustomDevice>();
        var setup = new InputDeviceBuilder("CustomDevice");
        var device = setup.Finish();

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
        var leftyGamepadSetup = new InputDeviceBuilder("Gamepad", variants: "Lefty");
        var leftyGamepadPrimary2DMotion = leftyGamepadSetup.GetControl("{Primary2DMotion}");
        var leftyGamepadSecondary2DMotion = leftyGamepadSetup.GetControl("{Secondary2DMotion}");
        //var leftyGamepadPrimaryTrigger = leftyGamepadSetup.GetControl("{PrimaryTrigger}");
        //var leftyGamepadSecondaryTrigger = leftyGamepadSetup.GetControl("{SecondaryTrigger}");
        //shoulder?

        var defaultGamepadSetup = new InputDeviceBuilder("Gamepad");
        var defaultGamepadPrimary2DMotion = defaultGamepadSetup.GetControl("{Primary2DMotion}");
        var defaultGamepadSecondary2DMotion = defaultGamepadSetup.GetControl("{Secondary2DMotion}");
        //var defaultGamepadPrimaryTrigger = defaultGamepadSetup.GetControl("{PrimaryTrigger}");
        //var defaultGamepadSecondaryTrigger = defaultGamepadSetup.GetControl("{SecondaryTrigger}");

        var leftyGamepad = (Gamepad)leftyGamepadSetup.Finish();
        var defaultGamepad = (Gamepad)defaultGamepadSetup.Finish();

        Assert.That(leftyGamepad.variants, Is.EqualTo("Lefty"));
        Assert.That(leftyGamepadPrimary2DMotion, Is.SameAs(leftyGamepad.rightStick));
        Assert.That(leftyGamepadSecondary2DMotion, Is.SameAs(leftyGamepad.leftStick));

        Assert.That(defaultGamepadPrimary2DMotion, Is.SameAs(defaultGamepad.leftStick));
        Assert.That(defaultGamepadSecondary2DMotion, Is.SameAs(defaultGamepad.rightStick));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CannotChangeSetupOfDeviceWhileAddedToSystem()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        Assert.That(() => new InputDeviceBuilder("Keyboard", existingDevice: device), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanChangeControlSetupAfterCreation()
    {
        const string initialJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"" },
                    { ""name"" : ""second"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(initialJson);

        // Create initial version of device.
        var initialSetup = new InputDeviceBuilder("MyDevice");
        var initialFirstControl = initialSetup.GetControl("first");
        var initialSecondControl = initialSetup.GetControl("second");
        var initialDevice = initialSetup.Finish();

        // Change layout.
        const string modifiedJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"" },
                    { ""name"" : ""second"", ""layout"" : ""Axis"" },
                    { ""name"" : ""third"", ""layout"" : ""Button"" }
                ]
            }
        ";
        InputSystem.RegisterLayout(modifiedJson);

        // Modify device.
        var modifiedSetup = new InputDeviceBuilder("MyDevice", existingDevice: initialDevice);
        var modifiedFirstControl = modifiedSetup.GetControl("first");
        var modifiedSecondControl = modifiedSetup.GetControl("second");
        var modifiedThirdControl = modifiedSetup.GetControl("third");
        var modifiedDevice = modifiedSetup.Finish();

        Assert.That(modifiedDevice, Is.SameAs(initialDevice));
        Assert.That(modifiedFirstControl, Is.SameAs(initialFirstControl));
        Assert.That(initialFirstControl, Is.TypeOf<ButtonControl>());
        Assert.That(modifiedSecondControl, Is.Not.SameAs(initialSecondControl));
        Assert.That(initialSecondControl, Is.TypeOf<ButtonControl>());
        Assert.That(modifiedSecondControl, Is.TypeOf<AxisControl>());
        Assert.That(modifiedThirdControl, Is.TypeOf<ButtonControl>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanChangeDeviceTypeAfterCreation()
    {
        // Device layout for a generic InputDevice.
        const string initialJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""buttonSouth"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(initialJson);

        // Create initial version of device.
        var initialSetup = new InputDeviceBuilder("MyDevice");
        var initialButton = initialSetup.GetControl<ButtonControl>("buttonSouth");
        var initialDevice = initialSetup.Finish();

        // Change layout to now be a gamepad.
        const string modifiedJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad""
            }
        ";
        InputSystem.RegisterLayout(modifiedJson);

        // Modify device.
        var modifiedSetup = new InputDeviceBuilder("MyDevice", existingDevice: initialDevice);
        var modifiedButton = modifiedSetup.GetControl<ButtonControl>("buttonSouth");
        var modifiedDevice = modifiedSetup.Finish();

        Assert.That(modifiedDevice, Is.Not.SameAs(initialDevice));
        Assert.That(modifiedDevice, Is.TypeOf<Gamepad>());
        Assert.That(initialDevice, Is.TypeOf<InputDevice>());
        Assert.That(modifiedButton, Is.SameAs(initialButton)); // Button survives.
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

        var setup = new InputDeviceBuilder("Gamepad");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.dpad.up.path, Is.EqualTo("/Gamepad/dpad/up"));

        InputSystem.AddDevice(device);

        Assert.That(device.dpad.up.path, Is.EqualTo("/Gamepad1/dpad/up"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDevice_MarksItAdded()
    {
        var device = new InputDeviceBuilder("Gamepad").Finish();

        Assert.That(device.added, Is.False);

        InputSystem.AddDevice(device);

        Assert.That(device.added, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceByType_IfTypeIsNotKnownAsLayout_AutomaticallyRegistersControlLayout()
    {
        Assert.That(() => InputSystem.AddDevice<TestDeviceWithDefaultState>(), Throws.Nothing);
        Assert.That(InputSystem.TryLoadLayout("TestDeviceWithDefaultState"), Is.Not.Null);
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

        Assert.That(InputSystem.updateMask & InputUpdateType.BeforeRender, Is.EqualTo((InputUpdateType)0));

        InputSystem.AddDevice("CustomGamepad");

        Assert.That(InputSystem.updateMask & InputUpdateType.BeforeRender, Is.EqualTo(InputUpdateType.BeforeRender));
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

        Assert.That(InputSystem.updateMask & InputUpdateType.BeforeRender, Is.EqualTo(InputUpdateType.BeforeRender));

        InputSystem.RemoveDevice(device1);

        Assert.That(InputSystem.updateMask & InputUpdateType.BeforeRender, Is.EqualTo(InputUpdateType.BeforeRender));

        InputSystem.RemoveDevice(device2);

        Assert.That(InputSystem.updateMask & InputUpdateType.BeforeRender, Is.EqualTo((InputUpdateType)0));
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

        testRuntime.ReportNewInputDevice(json);
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

        testRuntime.ReportNewInputDevice(json);
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
    public void Devices_CanLookUpDeviceByItsIdAfterItHasBeenAdded()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        Assert.That(InputSystem.GetDeviceById(device.id), Is.SameAs(device));
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
        InputSystem.AddDevice<Keyboard>(); // Noise.
        var gamepad = InputSystem.AddDevice<DualShockGamepad>();

        Assert.That(InputSystem.GetDevice<Gamepad>(), Is.SameAs(gamepad));
        Assert.That(InputSystem.GetDevice<DualShockGamepad>(), Is.SameAs(gamepad));
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

        Assert.That(gamepad1.id, Is.Not.EqualTo(gamepad2.id));
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

        testRuntime.ReportNewInputDevice(new InputDeviceDescription
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
        InputDevice receivedDevice = null;
        InputDeviceChange? receivedDeviceChange = null;

        InputSystem.onDeviceChange +=
            (d, c) =>
        {
            ++receivedCalls;
            receivedDevice = d;
            receivedDeviceChange = c;
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0.5f) });
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad));
        Assert.That(receivedDeviceChange, Is.EqualTo(InputDeviceChange.StateChanged));
    }

    private class TestDeviceThatResetsStateInCallback : InputDevice, IInputStateCallbackReceiver
    {
        public ButtonControl button { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            button = builder.GetControl<ButtonControl>(this, "button");
            base.FinishSetup(builder);
        }

        public bool OnCarryStateForward(IntPtr statePtr)
        {
            button.WriteValueInto(statePtr, 1);
            return true;
        }

        public void OnBeforeWriteNewState(IntPtr oldStatePtr, IntPtr newStatePtr)
        {
        }

        public bool OnReceiveStateWithDifferentFormat(IntPtr statePtr, FourCC stateFormat, uint stateSize,
            ref uint offsetToStoreAt)
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
        InputDevice receivedDevice = null;
        InputDeviceChange? receivedDeviceChange = null;

        InputSystem.onDeviceChange +=
            (d, c) =>
        {
            ++receivedCalls;
            receivedDevice = d;
            receivedDeviceChange = c;
        };

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(device));
        Assert.That(receivedDeviceChange, Is.EqualTo(InputDeviceChange.StateChanged));
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingStateOfDevice_MarksDeviceAsUpdatedThisFrame()
    {
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

        public FourCC GetFormat()
        {
            return new FourCC("PART");
        }
    }

    private unsafe struct TestDeviceFullState : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis", arraySize = 5)]
        public fixed float axis[5];

        public FourCC GetFormat()
        {
            return new FourCC("FULL");
        }
    }

    [InputControlLayout(stateType = typeof(TestDeviceFullState))]
    private class TestDeviceDecidingWhereToIntegrateState : InputDevice, IInputStateCallbackReceiver
    {
        public bool OnCarryStateForward(IntPtr statePtr)
        {
            return false;
        }

        public void OnBeforeWriteNewState(IntPtr oldStatePtr, IntPtr newStatePtr)
        {
        }

        public unsafe bool OnReceiveStateWithDifferentFormat(IntPtr statePtr, FourCC stateFormat, uint stateSize,
            ref uint offsetToStoreAt)
        {
            Assert.That(stateFormat, Is.EqualTo(new FourCC("PART")));
            Assert.That(stateSize, Is.EqualTo(UnsafeUtility.SizeOf<TestDevicePartialState>()));

            var values = (float*)currentStatePtr.ToPointer();
            for (var i = 0; i < 5; ++i)
                if (Mathf.Approximately(values[i], 0))
                {
                    offsetToStoreAt = (uint)i * sizeof(float);
                    return true;
                }

            Assert.Fail();
            return false;
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_DeviceWithStateCallback_CanDecideHowToIntegrateState()
    {
        InputSystem.RegisterLayout<TestDeviceDecidingWhereToIntegrateState>();
        var device = InputSystem.AddDevice<TestDeviceDecidingWhereToIntegrateState>();

        InputSystem.QueueStateEvent(device, new TestDevicePartialState { axis = 0.123f });
        InputSystem.Update();

        Assert.That(device["axis0"].ReadValueAsObject(), Is.EqualTo(0.123).Within(0.00001));

        InputSystem.QueueStateEvent(device, new TestDevicePartialState { axis = 0.234f });
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
        testRuntime.ReportNewInputDevice(new InputDeviceDescription { product = "MyController" }.ToJson());
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
        testRuntime.ReportNewInputDevice(new InputDeviceDescription { deviceClass = "Touchscreen" }.ToJson());
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<Touchscreen>());

        // Should not try to use a control layout.
        testRuntime.ReportNewInputDevice(new InputDeviceDescription { deviceClass = "Touch" }.ToJson());
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

        var inputEvent = DeviceRemoveEvent.Create(gamepad1.id, testRuntime.currentTime);
        InputSystem.QueueEvent(ref inputEvent);
        InputSystem.Update();

        Assert.That(gamepad1.added, Is.False);
        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad2));
        Assert.That(Gamepad.current, Is.Not.SameAs(gamepad1));
        Assert.That(gamepad1WasRemoved, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_WhenRemovedThroughEvent_AndDeviceIsNative_DeviceIsMovedToDisconnectedDeviceList()
    {
        ////REVIEW: should the system mandate more info in the description in order to retain a device?
        var description =
            new InputDeviceDescription
        {
            deviceClass = "Gamepad",
        };

        var originalDeviceId = testRuntime.ReportNewInputDevice(description);
        InputSystem.AddDevice<Keyboard>(); // Noise.
        InputSystem.Update();
        var originalGamepad = (Gamepad)InputSystem.GetDeviceById(originalDeviceId);

        var receivedChanges = new List<KeyValuePair<InputDevice, InputDeviceChange>>();
        InputSystem.onDeviceChange +=
            (device, change) =>
        {
            receivedChanges.Add(new KeyValuePair<InputDevice, InputDeviceChange>(device, change));
        };

        var inputEvent = DeviceRemoveEvent.Create(originalGamepad.id, testRuntime.currentTime);
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
        var newDeviceId = testRuntime.ReportNewInputDevice(description);
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
        Assert.That(originalGamepad.id, Is.EqualTo(newDeviceId));
        Assert.That(InputSystem.disconnectedDevices, Has.Count.Zero);
    }

    //Keep weak ref to device when getting disconnect event
    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_WhenRemovedThroughEvent_AreReusedWhenReconnectedAndNotReclaimedYet()
    {
        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            deviceClass = "Gamepad"
        }.ToJson());
        InputSystem.Update();

        var gamepad = (Gamepad)InputSystem.devices[0];

        var inputEvent = DeviceRemoveEvent.Create(gamepad.id, testRuntime.currentTime);
        InputSystem.QueueEvent(ref inputEvent);
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.Zero);

        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void Devices_WhenRemoved_DoNotEmergeOnUnsupportedList()
    {
        // Devices added directly via AddDevice() don't end up on the list of
        // available devices. Devices reported by the runtime do.
        testRuntime.ReportNewInputDevice(@"
            {
                ""type"" : ""Gamepad""
            }
        ");

        InputSystem.Update();
        var device = InputSystem.devices[0];

        var inputEvent = DeviceRemoveEvent.Create(device.id, testRuntime.currentTime);
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
        // Create an isolated runtime + input manager.
        using (var runtime = new InputTestRuntime())
        {
            var manager = new InputManager();

            manager.InitializeData();
            manager.InstallRuntime(runtime);

            // Create a device layout that will fail to instantiate.
            const string layout = @"
                {
                    ""name"" : ""TestDevice"",
                    ""controls"" : [
                        { ""name"" : ""test"", ""layout"" : ""DoesNotExist"" }
                    ]
                }
            ";
            manager.RegisterControlLayout(layout);

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

            Assert.That(() => manager.Update(), Throws.Nothing);

            // Make sure InputManager kept the gamepad.
            Assert.That(manager.devices.Count, Is.EqualTo(1));
            Assert.That(manager.devices, Has.Exactly(1).TypeOf<Gamepad>());
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeDisabledAndReEnabled()
    {
        var device = InputSystem.AddDevice<Mouse>();

        bool? disabled = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(device.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == DisableDeviceCommand.Type)
                    {
                        Assert.That(disabled, Is.Null);
                        disabled = true;
                        return InputDeviceCommand.kGenericSuccess;
                    }

                    if (commandPtr->type == EnableDeviceCommand.Type)
                    {
                        Assert.That(disabled, Is.Null);
                        disabled = false;
                        return InputDeviceCommand.kGenericSuccess;
                    }

                    return InputDeviceCommand.kGenericFailure;
                });
        }

        Assert.That(device.enabled, Is.True);
        Assert.That(disabled, Is.Null);

        InputSystem.DisableDevice(device);

        Assert.That(device.enabled, Is.False);
        Assert.That(disabled.HasValue, Is.True);
        Assert.That(disabled.Value, Is.True);

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

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_WhenDisabled_StateIsReset()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_WhenDisabled_RefreshActions()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void Devices_ThatHaveNoKnownLayout_AreDisabled()
    {
        var deviceId = testRuntime.AllocateDeviceId();
        testRuntime.ReportNewInputDevice(new InputDeviceDescription { deviceClass = "TestThing" }.ToJson(), deviceId);

        bool? wasDisabled = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == DisableDeviceCommand.Type)
                    {
                        Assert.That(wasDisabled, Is.Null);
                        wasDisabled = true;
                        return InputDeviceCommand.kGenericSuccess;
                    }

                    Assert.Fail("Should not get other IOCTLs");
                    return InputDeviceCommand.kGenericFailure;
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
        var deviceId = testRuntime.AllocateDeviceId();
        testRuntime.ReportNewInputDevice(new InputDeviceDescription { deviceClass = "TestThing" }.ToJson(), deviceId);
        InputSystem.Update();

        bool? wasEnabled = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == EnableDeviceCommand.Type)
                    {
                        Assert.That(wasEnabled, Is.Null);
                        wasEnabled = true;
                        return InputDeviceCommand.kGenericSuccess;
                    }

                    Assert.Fail("Should not get other IOCTLs");
                    return InputDeviceCommand.kGenericFailure;
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
        var deviceId = testRuntime.AllocateDeviceId();

        var queryEnabledStateResult = false;
        bool? receivedQueryEnabledStateCommand = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == QueryEnabledStateCommand.Type)
                    {
                        Assert.That(receivedQueryEnabledStateCommand, Is.Null);
                        receivedQueryEnabledStateCommand = true;
                        ((QueryEnabledStateCommand*)commandPtr)->isEnabled = queryEnabledStateResult;
                        return InputDeviceCommand.kGenericSuccess;
                    }

                    Assert.Fail("Should not get other IOCTLs");
                    return InputDeviceCommand.kGenericFailure;
                });
        }

        testRuntime.ReportNewInputDevice(new InputDeviceDescription { deviceClass = "Mouse" }.ToJson(), deviceId);
        InputSystem.Update();
        var device = InputSystem.devices.First(x => x.id == deviceId);

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
    public void Devices_NativeDevicesAreFlaggedAsSuch()
    {
        var description = new InputDeviceDescription { deviceClass = "Gamepad" };
        var deviceId = testRuntime.ReportNewInputDevice(description.ToJson());

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
    public void Devices_CanAssociateUserIdWithDevice()
    {
        var device = InputSystem.AddDevice<Gamepad>();
        string userId = null;

        unsafe
        {
            testRuntime.SetDeviceCommandCallback(device.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == QueryUserIdCommand.Type)
                    {
                        var queryUserIdPtr = (QueryUserIdCommand*)commandPtr;
                        StringHelpers.WriteStringToBuffer(userId, new IntPtr(queryUserIdPtr->idBuffer),
                            QueryUserIdCommand.kMaxIdLength);
                        return 1;
                    }

                    return InputDeviceCommand.kGenericFailure;
                });
        }

        Assert.That(device.userId, Is.Null);

        InputSystem.QueueConfigChangeEvent(device);
        InputSystem.Update();
        Assert.That(device.userId, Is.Null);

        userId = "testId";
        InputSystem.QueueConfigChangeEvent(device);
        InputSystem.Update();
        Assert.That(device.userId, Is.EqualTo(userId));
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
        testRuntime.SetDeviceCommandCallback(gamepad.id,
            (deviceId, command) =>
            {
                if (command->type == DualMotorRumbleCommand.Type)
                {
                    Assert.That(receivedCommand.HasValue, Is.False);
                    receivedCommand = *((DualMotorRumbleCommand*)command);
                    return 1;
                }

                Assert.Fail();
                return InputDeviceCommand.kGenericFailure;
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
        testRuntime.SetDeviceCommandCallback(gamepad.id,
            (deviceId, command) =>
            {
                if (command->type == DualMotorRumbleCommand.Type)
                {
                    Assert.That(receivedCommand.HasValue, Is.False);
                    receivedCommand = *((DualMotorRumbleCommand*)command);
                    return 1;
                }

                Assert.Fail();
                return InputDeviceCommand.kGenericFailure;
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

        Assert.That(joystick.axes, Has.Count.EqualTo(4)); // Includes stick.
        Assert.That(joystick.buttons, Has.Count.EqualTo(3)); // Includes trigger.
        Assert.That(joystick.trigger.name, Is.EqualTo("trigger"));
        Assert.That(joystick.stick.name, Is.EqualTo("stick"));
    }

    // The whole dynamic vs fixed vs before-render vs editor update mechanic is a can of worms. In the
    // ECS version, all this should be thrown out entirely.
    //
    // This test here is another peculiarity we need to watch out for. Events received by the system will
    // get written into either player or editor buffers. If written into player buffers, they get written
    // into *both* dynamic and fixed update buffers. However, depending on what the *current* update is
    // (fixed or dynamic) this means we are writing state into the *next* update of the other type. E.g.
    // when receiving state during fixed update, we write it both into the current fixed update and into
    // the *next* dynamic update.
    //
    // For the reset logic, this means we have to be extra careful to not overwrite that state we've written
    // "for the future".
    [Test]
    [Category("Devices")]
    public void Devices_PointerDeltaUpdatedInFixedUpdate_DoesNotGetResetInDynamicUpdate()
    {
        var pointer = InputSystem.AddDevice<Pointer>();

        InputSystem.QueueStateEvent(pointer, new PointerState { delta = new Vector2(0.5f, 0.5f) });
        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(pointer.delta.x.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(pointer.delta.y.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));

        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(pointer.delta.x.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(pointer.delta.y.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_PointerDeltasDoNotAccumulateFromPreviousFrame()
    {
        var pointer = InputSystem.AddDevice<Pointer>();

        InputSystem.QueueStateEvent(pointer, new PointerState { delta = new Vector2(0.5f, 0.5f) });
        InputSystem.Update(InputUpdateType.Fixed);
        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(pointer.delta.x.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(pointer.delta.y.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));

        InputSystem.QueueStateEvent(pointer, new PointerState { delta = new Vector2(0.5f, 0.5f) });
        InputSystem.Update(InputUpdateType.Fixed);
        InputSystem.Update(InputUpdateType.Dynamic);

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

        InputEventPtr stateEventPtr;
        using (StateEvent.From(device, out stateEventPtr))
        {
            deltaControl.WriteValueInto(stateEventPtr, new Vector2(0.5f, 0.5f));

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

        InputEventPtr stateEventPtr;
        using (StateEvent.From(device, out stateEventPtr))
        {
            deltaControl.WriteValueInto(stateEventPtr, new Vector2(0.5f, 0.5f));
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
    public void Devices_CanAdjustSensitivityOnPointerDeltas()
    {
        var pointer = InputSystem.AddDevice<Pointer>();

        const float kWindowWidth = 640f;
        const float kWindowHeight = 480f;
        const float kSensitivity = 6f;

        InputConfiguration.PointerDeltaSensitivity = kSensitivity;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(pointer.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == QueryDimensionsCommand.Type)
                    {
                        var windowDimensionsCommand = (QueryDimensionsCommand*)commandPtr;
                        windowDimensionsCommand->outDimensions = new Vector2(kWindowWidth, kWindowHeight);
                        return InputDeviceCommand.kGenericSuccess;
                    }

                    return InputDeviceCommand.kGenericFailure;
                });
        }

        InputSystem.QueueStateEvent(pointer, new PointerState { delta = new Vector2(32f, 64f) });
        InputSystem.Update();

        // NOTE: Whereas the tests above access .delta.x.value and .delta.y.value, here we access
        //       delta.value.x and delta.value.y. This is because the sensitivity processor sits
        //       on the vector control and not on the individual component axes.

        Assert.That(pointer.delta.ReadValue().x, Is.EqualTo(32 / kWindowWidth * kSensitivity).Within(0.00001));
        Assert.That(pointer.delta.ReadValue().y, Is.EqualTo(64 / kWindowHeight * kSensitivity).Within(0.00001));
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

        Assert.That(device, Is.InstanceOf<InputDevice>());
        Assert.That(device.layout, Is.EqualTo(layout));
        Assert.That(device, Is.TypeOf(type));
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

        var inputEvent = TextEvent.Create(keyboard.id, 0x10000 + (highBits << 10 | lowBits), 123);
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
            testRuntime.SetDeviceCommandCallback(keyboard.id,
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
                            name = currentLayoutName == "default" ? "m" : "q";
                        }

                        keyNameCommand->scanOrKeyCode = scanCode;
                        StringHelpers.WriteStringToBuffer(name, (IntPtr)keyNameCommand->nameBuffer,
                            QueryKeyNameCommand.kMaxNameLength);

                        return QueryKeyNameCommand.kSize;
                    }

                    return InputDeviceCommand.kGenericFailure;
                });
        }

        Assert.That(keyboard.aKey.displayName, Is.EqualTo("m"));
        Assert.That(keyboard.bKey.displayName, Is.EqualTo("other"));

        // Change layout.
        currentLayoutName = "other";
        InputSystem.QueueConfigChangeEvent(keyboard);
        InputSystem.Update();

        Assert.That(keyboard.aKey.displayName, Is.EqualTo("q"));
        Assert.That(keyboard.bKey.displayName, Is.EqualTo("other"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetNameOfCurrentKeyboardLayout()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var currentLayoutName = "default";
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(keyboard.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == QueryKeyboardLayoutCommand.Type)
                    {
                        var layoutCommand = (QueryKeyboardLayoutCommand*)commandPtr;
                        if (StringHelpers.WriteStringToBuffer(currentLayoutName, (IntPtr)layoutCommand->nameBuffer,
                            QueryKeyboardLayoutCommand.kMaxNameLength))
                            return QueryKeyboardLayoutCommand.kMaxNameLength;
                    }

                    return InputDeviceCommand.kGenericFailure;
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
            testRuntime.SetDeviceCommandCallback(mouse.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == WarpMousePositionCommand.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = *((WarpMousePositionCommand*)commandPtr);
                        return 1;
                    }

                    Assert.Fail();
                    return InputDeviceCommand.kGenericFailure;
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
        var device = InputSystem.AddDevice<Mouse>();

        InputSystem.QueueStateEvent(device,
            new MouseState
            {
                position = new Vector2(0.123f, 0.456f),
            });
        InputSystem.Update();

        Assert.That(device.position.x.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(device.position.y.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
        ////TODO: mouse phase should be driven by Mouse device automatically
        Assert.That(device.phase.ReadValue(), Is.EqualTo(PointerPhase.None));
        ////TODO: pointer ID etc.
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanDetectIfPenInRange()
    {
        var pen = InputSystem.AddDevice<Pen>();

        Assert.That(pen.inRange.ReadValue(), Is.EqualTo(0).Within(0.00001));

        InputSystem.QueueStateEvent(pen, new PenState().WithButton(PenState.Button.InRange));
        InputSystem.Update();

        Assert.That(pen.inRange.ReadValue(), Is.EqualTo(1).Within(0.00001));
    }

    ////FIXME: this needs to be overhauled; functioning of Touchscreen as Pointer is currently broken
    [Test]
    [Category("Devices")]
    public void Devices_CanUseTouchscreenAsPointer()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        // Primary touch functions as pointing element of touchscreen.

        InputSystem.QueueDeltaStateEvent(device.primaryTouch,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 4,
                position = new Vector2(0.123f, 0.456f)
            });
        InputSystem.Update();

        Assert.That(device.pointerId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.position.x.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(device.position.y.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
        Assert.That(device.phase.ReadValue(), Is.EqualTo(PointerPhase.Began));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchscreenReturnsActiveAndJustEndedTouches()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        Assert.That(device.activeTouches.Count, Is.Zero);
        Assert.That(device.allTouchControls.Count, Is.EqualTo(TouchscreenState.kMaxTouches));

        InputSystem.QueueDeltaStateEvent(device.allTouchControls[0],
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 4,
                position = new Vector2(0.123f, 0.456f)
            });
        InputSystem.Update();

        Assert.That(device.activeTouches.Count, Is.EqualTo(1));
        Assert.That(device.activeTouches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.activeTouches[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Began));
        Assert.That(device.activeTouches[0].position.x.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(device.activeTouches[0].position.y.ReadValue(), Is.EqualTo(0.456).Within(0.000001));

        InputSystem.QueueDeltaStateEvent(device.allTouchControls[0],
            new TouchState
            {
                phase = PointerPhase.Moved,
                touchId = 4,
                position = new Vector2(0.123f, 0.456f)
            });
        InputSystem.QueueDeltaStateEvent(device.allTouchControls[1],
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 5,
                position = new Vector2(0.789f, 0.123f)
            });
        InputSystem.Update();

        Assert.That(device.activeTouches.Count, Is.EqualTo(2));
        Assert.That(device.activeTouches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.activeTouches[1].touchId.ReadValue(), Is.EqualTo(5));
        Assert.That(device.activeTouches[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Moved));
        Assert.That(device.activeTouches[1].phase.ReadValue(), Is.EqualTo(PointerPhase.Began));

        // No change. Touches should become stationary and stay in list.
        InputSystem.Update();

        Assert.That(device.activeTouches.Count, Is.EqualTo(2));
        Assert.That(device.activeTouches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.activeTouches[1].touchId.ReadValue(), Is.EqualTo(5));
        Assert.That(device.activeTouches[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));
        Assert.That(device.activeTouches[1].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));

        InputSystem.QueueDeltaStateEvent(device.allTouchControls[0],
            new TouchState
            {
                phase = PointerPhase.Ended,
                touchId = 4,
            });
        InputSystem.QueueDeltaStateEvent(device.allTouchControls[1],
            new TouchState
            {
                phase = PointerPhase.Cancelled,
                touchId = 5,
            });
        InputSystem.Update();

        // For one frame, the ended and cancelled touches should stick around on the active touches list

        Assert.That(device.activeTouches.Count, Is.EqualTo(2));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Ended));
        Assert.That(device.allTouchControls[1].phase.ReadValue(), Is.EqualTo(PointerPhase.Cancelled));

        // But then they should disappear from the list.

        InputSystem.Update();

        Assert.That(device.activeTouches.Count, Is.Zero);
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.None));
        Assert.That(device.allTouchControls[1].phase.ReadValue(), Is.EqualTo(PointerPhase.None));
    }

    ////REVIEW: if we allow this, InputControl.ReadValueFrom() is in trouble
    ////        (actually, is this true? TouchControl should be able to read a state event like here just fine)
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
                phase = PointerPhase.Began,
                touchId = 4,
            });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 5,
            });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[1].touchId.ReadValue(), Is.EqualTo(5));
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
                phase = PointerPhase.Began,
                touchId = 4,
            });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Began));

        // Don't move.
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));

        // Move.
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Moved,
                touchId = 4,
            });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Moved));

        // Don't move.
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));

        // Random unrelated touch.
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 5,
            });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));

        // End.
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Ended,
                touchId = 4,
            });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Ended));

        // Release.
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.None));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchesBecomeStationaryWhenNotMovedInFrame()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 4,
            });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Began));

        InputSystem.Update();

        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_TouchesWithSameIdDontGetStuck_FIXME()
    {
        ////FIXME: Fails - touches stuck in Stationary phase
        /// While it's not recommended for two different touches to share an id, it shoudn't get stuck in Stationary phase
        /// Can we add checks for Development build ?
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 0,
            });

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Ended,
                touchId = 0,
            });

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 0,
            });

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Ended,
                touchId = 0,
            });
        InputSystem.Update();
        InputSystem.Update();

        Assert.That(device.activeTouches.Count, Is.EqualTo(0));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.None));
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_TouchesWithWrongTimestampCorrectlyRecognized_FIXME()
    {
        ////FIXME: fails - events which have timestamp which is less than previous event are ignored implictly
        /// Can we add checks for Development build ?
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 0,
            }, 1.0);

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Ended,
                touchId = 0,
            }, 0.9);

        InputSystem.Update();
        InputSystem.Update();

        Assert.That(device.activeTouches.Count, Is.EqualTo(0));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.None));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchDeltasAreComputedAutomatically()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 4,
                position = new Vector2(10, 20)
            });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Moved,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(10).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(20).Within(0.00001));

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Ended,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchFlagsWorkCorrectly()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 1,
                flags = 1 << (int)TouchFlags.IndirectTouch
            });

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 2,
                flags = 0
            });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].indirectTouch.ReadValue(), Is.EqualTo(1));
        Assert.That(device.activeTouches[1].indirectTouch.ReadValue(), Is.EqualTo(0));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchDeltasResetWhenTouchIsStationary()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 4,
                position = new Vector2(10, 20)
            });
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Moved,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(10).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(20).Within(0.00001));

        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchDeltasResetWhenTouchIsMovingInPlace()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 4,
                position = new Vector2(10, 20)
            });
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Moved,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.Update();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Moved,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchesAccumulateDeltasWithinFrame()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 4,
                position = new Vector2(10, 20)
            });
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Moved,
                touchId = 4,
                position = new Vector2(20, 40)
            });
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Moved,
                touchId = 4,
                position = new Vector2(30, 50)
            });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(20).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(30).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanKeepTrackOfMultipleConcurrentTouches()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 92,
            });
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Moved,
                touchId = 92,
            });

        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(92));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Moved));
        Assert.That(device.activeTouches.Count, Is.EqualTo(1));

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Ended,
                touchId = 92,
            });
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Began,
                touchId = 93,
            });
        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Moved,
                touchId = 93,
            });

        InputSystem.Update();

        ////FIXME: this test exposes a current weakness of how OnCarryStateForward() is implemented; the fact
        ////       that Touchscreen blindly overwrites state is visible not just to actions but also when
        ////       looking at values from the last frame which get destroyed by Touchscreen

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(92));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Ended));
        Assert.That(device.allTouchControls[0].touchId.ReadPreviousValue(), Is.EqualTo(92));
        Assert.That(device.allTouchControls[0].phase.ReadPreviousValue(), Is.EqualTo(PointerPhase.Stationary));
        //Assert.That(device.allTouchControls[0].phase.ReadPreviousValue(), Is.EqualTo(PointerPhase.Moved));
        Assert.That(device.allTouchControls[1].touchId.ReadValue(), Is.EqualTo(93));
        Assert.That(device.allTouchControls[1].phase.ReadValue(), Is.EqualTo(PointerPhase.Moved));
        Assert.That(device.activeTouches.Count, Is.EqualTo(2));

        InputSystem.QueueStateEvent(device,
            new TouchState
            {
                phase = PointerPhase.Ended,
                touchId = 93,
            });

        InputSystem.Update();

        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.None));
        Assert.That(device.allTouchControls[0].phase.ReadPreviousValue(), Is.EqualTo(PointerPhase.None));
        //Assert.That(device.allTouchControls[0].phase.ReadPreviousValue(), Is.EqualTo(PointerPhase.Ended));
        Assert.That(device.allTouchControls[1].touchId.ReadValue(), Is.EqualTo(93));
        Assert.That(device.allTouchControls[1].phase.ReadValue(), Is.EqualTo(PointerPhase.Ended));
        Assert.That(device.allTouchControls[1].touchId.ReadPreviousValue(), Is.EqualTo(93));
        Assert.That(device.allTouchControls[1].phase.ReadPreviousValue(), Is.EqualTo(PointerPhase.Stationary));
        //Assert.That(device.allTouchControls[1].phase.ReadPreviousValue(), Is.EqualTo(PointerPhase.Moved));
        Assert.That(device.activeTouches.Count, Is.EqualTo(1));

        InputSystem.Update();

        Assert.That(device.allTouchControls[1].phase.ReadValue(), Is.EqualTo(PointerPhase.None));
        Assert.That(device.allTouchControls[1].phase.ReadPreviousValue(), Is.EqualTo(PointerPhase.Stationary));
        //Assert.That(device.allTouchControls[1].phase.ReadPreviousValue(), Is.EqualTo(PointerPhase.Ended));
        Assert.That(device.activeTouches.Count, Is.EqualTo(0));
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
    public void Devices_CanGetSensorSamplingFrequency()
    {
        var sensor = InputSystem.AddDevice<Accelerometer>();

        bool? receivedQueryFrequencyCommand = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(sensor.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == QuerySamplingFrequencyCommand.Type)
                    {
                        Assert.That(receivedQueryFrequencyCommand, Is.Null);
                        receivedQueryFrequencyCommand = true;
                        ((QuerySamplingFrequencyCommand*)commandPtr)->frequency = 120.0f;
                        return InputDeviceCommand.kGenericSuccess;
                    }

                    return InputDeviceCommand.kGenericFailure;
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
            testRuntime.SetDeviceCommandCallback(sensor.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == SetSamplingFrequencyCommand.Type)
                    {
                        Assert.That(receivedSetFrequencyCommand, Is.Null);
                        receivedSetFrequencyCommand = true;
                        return InputDeviceCommand.kGenericSuccess;
                    }

                    return InputDeviceCommand.kGenericFailure;
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

        Assert.That(Accelerometer.current, Is.SameAs(accelerometer));

        Assert.That(accelerometer.acceleration.ReadValue(), Is.EqualTo(value).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetGyroReading()
    {
        var gyro = InputSystem.AddDevice<Gyroscope>();
        var value = new Vector3(0.987f, 0.654f, 0.321f);
        InputSystem.QueueStateEvent(gyro, new GyroscopeState { angularVelocity = value });
        InputSystem.Update();

        Assert.That(Gyroscope.current, Is.SameAs(gyro));
        Assert.That(gyro.angularVelocity.ReadValue(), Is.EqualTo(value).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetGravityReading()
    {
        var sensor = InputSystem.AddDevice<Gravity>();
        var value = new Vector3(0.987f, 0.654f, 0.321f);
        InputSystem.QueueStateEvent(sensor, new GravityState { gravity = value });
        InputSystem.Update();

        Assert.That(Gravity.current, Is.SameAs(sensor));
        Assert.That(sensor.gravity.ReadValue(), Is.EqualTo(value).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetAttitudeReading()
    {
        var sensor = InputSystem.AddDevice<Attitude>();
        var value = Quaternion.Euler(10, 20, 30);
        InputSystem.QueueStateEvent(sensor, new AttitudeState { attitude = value });
        InputSystem.Update();

        Assert.That(Attitude.current, Is.SameAs(sensor));
        Assert.That(sensor.attitude.ReadValue(), Is.EqualTo(value).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetLinearAccelerationReading()
    {
        var sensor = InputSystem.AddDevice<LinearAcceleration>();
        var value = new Vector3(0.987f, 0.654f, 0.321f);
        InputSystem.QueueStateEvent(sensor, new LinearAccelerationState { acceleration = value });
        InputSystem.Update();

        Assert.That(LinearAcceleration.current, Is.SameAs(sensor));
        Assert.That(sensor.acceleration.ReadValue(), Is.EqualTo(value).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    [TestCase("Accelerometer", "acceleration")]
    [TestCase("Gyroscope", "angularVelocity")]
    [TestCase("Gravity", "gravity")]
    public void Devices_CanCompensateSensorDirectionValues(string layoutName, string controlName)
    {
        var sensor = InputSystem.AddDevice(layoutName);
        var value = new Vector3(0.123f, 0.456f, 0.789f);
        var directionControl = (Vector3Control)sensor[controlName];

        InputEventPtr stateEventPtr;
        using (StateEvent.From(sensor, out stateEventPtr))
        {
            directionControl.WriteValueInto(stateEventPtr, value);
            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.Update();

            InputConfiguration.CompensateSensorsForScreenOrientation = true;

            testRuntime.screenOrientation = ScreenOrientation.LandscapeLeft;
            Assert.That(directionControl.ReadValue(),
                Is.EqualTo(new Vector3(-value.y, value.x, value.z)).Using(Vector3EqualityComparer.Instance));

            testRuntime.screenOrientation = ScreenOrientation.PortraitUpsideDown;
            Assert.That(directionControl.ReadValue(),
                Is.EqualTo(new Vector3(-value.x, -value.y, value.z)).Using(Vector3EqualityComparer.Instance));

            testRuntime.screenOrientation = ScreenOrientation.LandscapeRight;
            Assert.That(directionControl.ReadValue(),
                Is.EqualTo(new Vector3(value.y, -value.x, value.z)).Using(Vector3EqualityComparer.Instance));

            testRuntime.screenOrientation = ScreenOrientation.Portrait;
            Assert.That(directionControl.ReadValue(), Is.EqualTo(value).Using(Vector3EqualityComparer.Instance));

            InputConfiguration.CompensateSensorsForScreenOrientation = false;
            Assert.That(directionControl.ReadValue(), Is.EqualTo(value).Using(Vector3EqualityComparer.Instance));
        }
    }

    [Test]
    [Category("Devices")]
    [TestCase("Attitude", "attitude")]
    public void Devices_CanCompensateSensorRotationValues(string layoutName, string controlName)
    {
        var sensor = InputSystem.AddDevice(layoutName);
        var angles = new Vector3(11, 22, 33);
        var rotationControl = (QuaternionControl)sensor[controlName];

        InputEventPtr stateEventPtr;
        using (StateEvent.From(sensor, out stateEventPtr))
        {
            rotationControl.WriteValueInto(stateEventPtr, Quaternion.Euler(angles));
            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.Update();

            InputConfiguration.CompensateSensorsForScreenOrientation = true;
            testRuntime.screenOrientation = ScreenOrientation.LandscapeLeft;
            Assert.That(rotationControl.ReadValue().eulerAngles,
                Is.EqualTo(new Vector3(angles.x, angles.y, angles.z + 270)).Using(Vector3EqualityComparer.Instance));

            testRuntime.screenOrientation = ScreenOrientation.PortraitUpsideDown;
            Assert.That(rotationControl.ReadValue().eulerAngles,
                Is.EqualTo(new Vector3(angles.x, angles.y, angles.z + 180)).Using(Vector3EqualityComparer.Instance));

            testRuntime.screenOrientation = ScreenOrientation.LandscapeRight;
            Assert.That(rotationControl.ReadValue().eulerAngles,
                Is.EqualTo(new Vector3(angles.x, angles.y, angles.z + 90)).Using(Vector3EqualityComparer.Instance));

            testRuntime.screenOrientation = ScreenOrientation.Portrait;
            Assert.That(rotationControl.ReadValue().eulerAngles,
                Is.EqualTo(angles).Using(Vector3EqualityComparer.Instance));

            InputConfiguration.CompensateSensorsForScreenOrientation = false;
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
    public void Devices_RemovingDeviceCleansUpUpdateCallback()
    {
        InputSystem.RegisterLayout<CustomDeviceWithUpdate>();
        var device = (CustomDeviceWithUpdate)InputSystem.AddDevice("CustomDeviceWithUpdate");
        InputSystem.RemoveDevice(device);

        InputSystem.Update();

        Assert.That(device.onUpdateCallCount, Is.Zero);
    }

    #if UNITY_2018_3_OR_NEWER
    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_RemovingAndReaddingDevice_DoesNotAllocateMemory()
    {
        var description =
            new InputDeviceDescription
        {
            deviceClass = "Gamepad",
            product = "TestProduct",
            manufacturer = "TestManufacturer"
        };

        var deviceId = testRuntime.ReportNewInputDevice(description);
        InputSystem.Update();

        Assert.That(() =>
        {
            // "Unplug" device.
            var removeEvent = DeviceRemoveEvent.Create(deviceId, 0.123);
            InputSystem.QueueEvent(ref removeEvent);
            InputSystem.Update();

            // "Plug" it back in.
            testRuntime.ReportNewInputDevice(description);
            InputSystem.Update();
        }, Is.Not.AllocatingGCMemory());
    }

    #endif

    [Test]
    [Category("Devices")]
    public void Devices_AreUpdatedWithTimestampOfLastEvent()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        testRuntime.currentTime = 1234;
        testRuntime.currentTimeOffsetToRealtimeSinceStartup = 1123;

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

        Assert.That(testRuntime.pollingFrequency, Is.EqualTo(120).Within(0.000001));
        Assert.That(InputSystem.pollingFrequency, Is.EqualTo(120).Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_PollingFrequencyIs60HzByDefault()
    {
        Assert.That(InputSystem.pollingFrequency, Is.EqualTo(60).Within(0.000001));
        // Make sure InputManager passed the frequency on to the runtime.
        Assert.That(testRuntime.pollingFrequency, Is.EqualTo(60).Within(0.000001));
    }

    //This could be the first step towards being able to simulate input well.
    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_CanCreateVirtualDevices()
    {
        //layout has one or more binding paths on controls instead of associated memory
        //sets up state monitors
        //virtual device is of a device type determined by base template (e.g. Gamepad)
        //can associate additional processing logic with controls
        //state changes for virtual devices are accumulated as separate buffer of events that is flushed out in a post-step
        //performed as a loop so virtual devices can feed into other virtual devices
        //virtual devices are marked with flag
        Assert.Fail();
    }

    [InputControlLayout]
    public class NoisyInputDevice : InputDevice
    {
        public static NoisyInputDevice current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct NoisyInputEventState : IInputStateTypeInfo
    {
        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }

        [FieldOffset(0)] public short button1;
        [FieldOffset(2)] public short button2;
    }

    [Test]
    [Category("Devices")]
    public void Devices_NoisyControlDoesNotUpdateCurrentDevice()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""SHRT"", ""offset"" : 0, ""noisy"" : ""true"" },
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""SHRT"", ""offset"" : 2 }
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");
        var lastUpdateTime = device1.lastUpdateTime;

        Assert.That(NoisyInputDevice.current == device2);

        InputSystem.QueueStateEvent(device1, new NoisyInputEventState { button1 = short.MaxValue, button2 = 0 });
        InputSystem.Update();

        Assert.AreEqual(lastUpdateTime, device1.lastUpdateTime);
        Assert.AreEqual(NoisyInputDevice.current, device2);
    }

    [Test]
    [Category("Devices")]
    public void Devices_NonNoisyControlDoesUpdateCurrentDevice()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""SHRT"", ""offset"" : 0, ""noisy"" : ""true"" },
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""SHRT"", ""offset"" : 2 }
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        Assert.AreEqual(NoisyInputDevice.current, device2);

        InputSystem.QueueStateEvent(device1, new NoisyInputEventState { button1 = short.MaxValue, button2 = short.MaxValue });
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device1);
    }

    [Test]
    [Category("Devices")]
    public void Devices_NoisyDeadzonesAffectCurrentDevice()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""processors"" : ""deadzone(min=0.5,max=0.9)""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        Assert.AreEqual(Gamepad.current, device2);

        InputSystem.QueueStateEvent(device1, new GamepadState { leftStick = new Vector2(0.1f, 0.1f) });
        InputSystem.Update();

        Assert.AreEqual(Gamepad.current, device2);

        InputSystem.QueueStateEvent(device1, new GamepadState { leftStick = new Vector2(1.0f, 1.0f) });
        InputSystem.Update();

        Assert.AreEqual(Gamepad.current, device1);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanDetectNoisyControlsOnDeltaStateEvents()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""SHRT""},
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""SHRT"", ""noisy"" : ""true"" }
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        Assert.That(NoisyInputDevice.current == device2);

        InputSystem.QueueDeltaStateEvent(device1["first"], short.MaxValue);
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device1);

        InputSystem.QueueDeltaStateEvent(device2["second"], short.MaxValue);
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device1);
    }

    [Test]
    [Category("Devices")]
    public void Devices_SettingBlankFilterSkipsNoiseFiltering()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""SHRT"", ""noisy"" : ""true""},
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""SHRT""}
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        device1.userInteractionFilter = new InputNoiseFilter();

        Assert.That(NoisyInputDevice.current == device2);

        InputSystem.QueueDeltaStateEvent(device1["first"], short.MaxValue);
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device1);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanOverrideDefaultNoiseFilter()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""SHRT""},
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""SHRT""}
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        // Tag the entire device as noisy
        device1.userInteractionFilter = new InputNoiseFilter
        {
            elements = new[]
            {
                new InputNoiseFilter.FilterElement
                {
                    controlIndex = 0,
                    type = InputNoiseFilter.ElementType.EntireControl
                },
                new InputNoiseFilter.FilterElement
                {
                    controlIndex = 1,
                    type = InputNoiseFilter.ElementType.EntireControl
                },
            }
        };

        Assert.That(NoisyInputDevice.current == device2);

        InputSystem.QueueStateEvent(device1, new NoisyInputEventState { button1 = short.MaxValue, button2 = short.MaxValue });
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device2);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanUseNoiseFilterWithMultipleProcessors()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""NoisyInputDevice"",
                ""format"" : ""TEST"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"", ""format"" : ""FLT"", ""processors"" : ""Invert(),Clamp(min=0.0,max=0.9)""},
                    { ""name"" : ""second"", ""layout"" : ""Button"", ""format"" : ""FLT"", ""processors"" : ""Clamp(min=0.0,max=0.9),Invert()""}
                ]
            }
        ";

        InputSystem.RegisterLayout<NoisyInputDevice>();
        InputSystem.RegisterLayout(json);

        var device1 = InputSystem.AddDevice("MyDevice");
        var device2 = InputSystem.AddDevice("MyDevice");

        Assert.That(NoisyInputDevice.current == device2);

        InputSystem.QueueDeltaStateEvent(device1["first"], 1.0f);
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device2);

        InputSystem.QueueDeltaStateEvent(device1["second"], 1.0f);
        InputSystem.Update();

        Assert.AreEqual(NoisyInputDevice.current, device1);
    }
}
