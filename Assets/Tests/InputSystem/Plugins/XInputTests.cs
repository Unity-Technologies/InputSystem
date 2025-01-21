using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Processors;

#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_XBOXONE || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
using UnityEngine.InputSystem.XInput.LowLevel;
#endif

internal class XInputTests : CoreTestsFixture
{
    ////TODO: refactor this into two tests that send actual state and test the wiring
    ////TODO: enable everything in the editor always and test
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
    /* ////Brute-forcing by commenting out of Devices_SupportsXInputDevicesOnPlatform
       ////since the test would still run while [Ignore] or UnityPlatform excluding it.
#endif
    [Test]
    [Category("Devices")]
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    [TestCase("Xbox One Wired Controller", "Microsoft", "HID", "XboxGamepadMacOS")]
    [TestCase("Xbox Series Wireless Controller", "Microsoft", "HID", "XboxGamepadMacOSWireless")]
#endif
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA
    [TestCase(null, null, "XInput", "XInputControllerWindows")]
#endif
    public void Devices_SupportsXInputDevicesOnPlatform(string product, string manufacturer, string interfaceName, string layoutName)
    {
        var description = new InputDeviceDescription
        {
            interfaceName = interfaceName,
            product = product,
            manufacturer = manufacturer
        };

        InputDevice device = null;
        Assert.That(() => device = InputSystem.AddDevice(description), Throws.Nothing);

        using (var matches = InputSystem.FindControls(string.Format("/<{0}>", layoutName)))
            Assert.That(matches, Has.Exactly(1).SameAs(device));

        Assert.That(device.name, Is.EqualTo(layoutName));
        Assert.That(device.description.manufacturer, Is.EqualTo(manufacturer));
        Assert.That(device.description.interfaceName, Is.EqualTo(interfaceName));
        Assert.That(device.description.product, Is.EqualTo(product));
    }
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
    */
#endif

    ////FIXME: we should not have tests that only run in players
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA
    [Test]
    [Category("Devices")]
    public void Devices_CanGetSubTypeOfXInputDevice()
    {
        var capabilities = new XInputController.Capabilities
        {
            subType = XInputController.DeviceSubType.ArcadePad
        };
        var description = new InputDeviceDescription
        {
            interfaceName = "XInput",
            capabilities = JsonUtility.ToJson(capabilities)
        };

        var device = (XInputController)InputSystem.AddDevice(description);

        Assert.That(device.subType, Is.EqualTo(XInputController.DeviceSubType.ArcadePad));
    }

#endif

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

    [Test]
    [Category("Devices")]
    public void Devices_SupportXboxControllerOnOSX()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "HID",
            product = "Xbox One Wired Controller",
            manufacturer = "Microsoft"
        });

        Assert.That(device, Is.AssignableTo<XInputController>());
        Assert.That(device, Is.AssignableTo<XboxGamepadMacOS>());
        var gamepad = (XboxGamepadMacOS)device;

        InputSystem.QueueStateEvent(gamepad,
            new XInputControllerOSXState
            {
                leftStickX = 32767,
                leftStickY = -32767,
                rightStickX = 32767,
                rightStickY = -32767,
                leftTrigger = 255,
                rightTrigger = 255,
            });

        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.up.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.down.ReadValue(), Is.EqualTo(0.0).Within(0.001));
        Assert.That(gamepad.leftStick.right.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.left.ReadValue(), Is.EqualTo(0.0).Within(0.001));

        Assert.That(gamepad.rightStick.x.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.y.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.up.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.down.ReadValue(), Is.EqualTo(0.0).Within(0.001));
        Assert.That(gamepad.rightStick.right.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.left.ReadValue(), Is.EqualTo(0.0).Within(0.001));

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(1));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(1));

        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.A), gamepad.aButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.A), gamepad.buttonSouth);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.B), gamepad.bButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.B), gamepad.buttonEast);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.X), gamepad.xButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.X), gamepad.buttonWest);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Y), gamepad.yButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Y), gamepad.buttonNorth);

        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.DPadDown), gamepad.dpad.down);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.DPadUp), gamepad.dpad.up);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.DPadLeft), gamepad.dpad.left);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.DPadRight), gamepad.dpad.right);

        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.LeftThumbstickPress), gamepad.leftStickButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.RightThumbstickPress), gamepad.rightStickButton);

        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.LeftShoulder), gamepad.leftShoulder);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.RightShoulder), gamepad.rightShoulder);

        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Start), gamepad.menu);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Start), gamepad.startButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Select), gamepad.view);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Select), gamepad.selectButton);
    }

    [TestCase(0x045E, 0x02E0, 16, 11)] // Xbox One Wireless Controller
    [TestCase(0x045E, 0x0B20, 10, 11)] // Xbox Series X|S Wireless Controller
    // This test is used to establish the correct button map layout based on the PID and VIDs. The usual difference
    // is around the select and start button bits.
    // If the layout is changed this test will fail and will need to be adapted either with a new device/layout or
    // a new button map.
    public void Devices_SupportWirelessXboxOneAndSeriesControllerOnOSX(int vendorId, int productId, int selectBit, int startBit)
    {
        // Fake a real Xbox Wireless Controller
        var xboxGamepad = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "HID",
            product = "Xbox Wireless Controller",
            manufacturer = "Microsoft",
            capabilities = new HID.HIDDeviceDescriptor
            {
                vendorId = vendorId,
                productId = productId,
            }.ToJson()
        });


        Assert.That(xboxGamepad, Is.AssignableTo<XInputController>());

        var gamepad = (XInputController)xboxGamepad;
        Assert.That(gamepad.selectButton.isPressed, Is.False);

        // Check if the controller is an Xbox One from a particular type where we know the select and start buttons are
        // different
        if (productId == 0x02e0)
        {
            Assert.That(xboxGamepad, Is.AssignableTo<XboxOneGampadMacOSWireless>());

            InputSystem.QueueStateEvent(gamepad,
                new XInputControllerWirelessOSXState
                {
                    buttons = (uint)(1 << selectBit |
                        1 << startBit)
                });
            InputSystem.Update();
        }
        else
        {
            Assert.That(xboxGamepad, Is.AssignableTo<XboxGamepadMacOSWireless>());

            InputSystem.QueueStateEvent(gamepad,
                new XInputControllerWirelessOSXState
                {
                    buttons = (uint)(1 << selectBit |
                        1 << startBit)
                });
            InputSystem.Update();
        }

        Assert.That(gamepad.selectButton.isPressed);
        Assert.That(gamepad.startButton.isPressed);
    }

// Disable tests in standalone builds from 2022.1+ see UUM-19622
#if !UNITY_STANDALONE_OSX || !TEMP_DISABLE_STANDALONE_OSX_XINPUT_TEST
    [Test]
    [Category("Devices")]
    public void Devices_SupportXboxWirelessControllerOnOSX()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "HID",
            product = "Xbox One Wireless Controller",
            manufacturer = "Microsoft",
            capabilities = new HID.HIDDeviceDescriptor
            {
                vendorId = 0x045E,
                productId = 0x02E0,
            }.ToJson()
        });

        Assert.That(device, Is.AssignableTo<XInputController>());
        Assert.That(device, Is.AssignableTo<XboxOneGampadMacOSWireless>());
        var gamepad = (XboxOneGampadMacOSWireless)device;

        InputSystem.QueueStateEvent(gamepad,
            new XInputControllerWirelessOSXState
            {
                leftStickX = 65535,
                leftStickY = 0,
                rightStickX = 65535,
                rightStickY = 0,
                leftTrigger = 1023,
                rightTrigger = 1023,
            });

        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.up.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.down.ReadValue(), Is.EqualTo(0.0).Within(0.001));
        Assert.That(gamepad.leftStick.right.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.left.ReadValue(), Is.EqualTo(0.0).Within(0.001));

        Assert.That(gamepad.rightStick.x.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.y.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.up.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.down.ReadValue(), Is.EqualTo(0.0).Within(0.001));
        Assert.That(gamepad.rightStick.right.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.left.ReadValue(), Is.EqualTo(0.0).Within(0.001));

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(1));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(1));

        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.A), gamepad.aButton);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.A), gamepad.buttonSouth);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.B), gamepad.bButton);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.B), gamepad.buttonEast);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.X), gamepad.xButton);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.X), gamepad.buttonWest);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.Y), gamepad.yButton);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.Y), gamepad.buttonNorth);

        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithDpad(5), gamepad.dpad.down);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithDpad(1), gamepad.dpad.up);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithDpad(7), gamepad.dpad.left);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithDpad(3), gamepad.dpad.right);

        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.LeftThumbstickPress), gamepad.leftStickButton);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.RightThumbstickPress), gamepad.rightStickButton);

        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.LeftShoulder), gamepad.leftShoulder);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.RightShoulder), gamepad.rightShoulder);

        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.Start), gamepad.menu);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.Start), gamepad.startButton);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.Select), gamepad.view);
        AssertButtonPress(gamepad, XInputControllerWirelessOSXState.defaultState.WithButton(XInputControllerWirelessOSXState.Button.Select), gamepad.selectButton);

        // Test to make sure that the default state is not set to input values of 0, but to the center of the sticks
        InputSystem.QueueStateEvent(gamepad,
            new XInputControllerWirelessOSXState
            {
                leftStickX = 0,
                leftStickY = 0,
                rightStickX = 0,
                rightStickY = 0,
                leftTrigger = 0,
                rightTrigger = 0,
            });
        InputSystem.Update();
        Assert.That(gamepad.leftStick.IsActuated());
        Assert.That(gamepad.leftStick.x.IsActuated());
        Assert.That(gamepad.leftStick.CheckStateIsAtDefault(), Is.False);
        Assert.That(gamepad.leftStick.x.CheckStateIsAtDefault(), Is.False);
        Assert.That(gamepad.leftTrigger.IsActuated(), Is.False);
        Assert.That(gamepad.leftTrigger.CheckStateIsAtDefault());
    }

#endif // TEMP_DISABLE_STANDALONE_OSX_XINPUT_TEST

#endif


#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA
    [Test]
    [Category("Devices")]
    public void Devices_SupportXboxControllerOnWindows()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "XInput"
        });

        Assert.That(device, Is.AssignableTo<XInputController>());
        Assert.That(device, Is.AssignableTo<XInputControllerWindows>());
        var gamepad = (XInputControllerWindows)device;

        InputSystem.QueueStateEvent(gamepad,
            new XInputControllerWindowsState
            {
                leftStickX = 32767,
                leftStickY = 32767,
                rightStickX = 32767,
                rightStickY = 32767,
                leftTrigger = 255,
                rightTrigger = 255,
            });

        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.x.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.y.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(1));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(1));

        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.A), gamepad.aButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.A), gamepad.buttonSouth);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.B), gamepad.bButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.B), gamepad.buttonEast);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.X), gamepad.xButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.X), gamepad.buttonWest);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Y), gamepad.yButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Y), gamepad.buttonNorth);

        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.DPadDown), gamepad.dpad.down);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.DPadUp), gamepad.dpad.up);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.DPadLeft), gamepad.dpad.left);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.DPadRight), gamepad.dpad.right);

        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.LeftThumbstickPress), gamepad.leftStickButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.RightThumbstickPress), gamepad.rightStickButton);

        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.LeftShoulder), gamepad.leftShoulder);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.RightShoulder), gamepad.rightShoulder);

        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Start), gamepad.menu);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Start), gamepad.startButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Select), gamepad.view);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Select), gamepad.selectButton);
    }

#endif
}
