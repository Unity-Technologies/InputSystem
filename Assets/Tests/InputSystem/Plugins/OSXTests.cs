#if UNITY_EDITOR || UNITY_OSX

using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OSX;
using UnityEngine.InputSystem.OSX.LowLevel;
using UnityEngine.TestTools.Utils;

internal class OSXTests
{
    [Test]
    [Category("Devices")]
    [TestCase(0xd, 0x0)] // OSX dummy VID/PID
    public void Devices_SupportsNimbusPlusAsHID_WithProductNameAndPIDAndVID(int vendorId, int productId)
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "HID",
            product = "Nimbus+",
            capabilities = new HID.HIDDeviceDescriptor
            {
                vendorId = vendorId,
                productId = productId,
            }.ToJson()
        });

        Assert.That(device, Is.AssignableTo<NimbusGamepadHid>());
        Assert.That(device.displayName, Is.EqualTo("Nimbus+"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_DoNotSupportNimbusPlusAsHID_WithProductName()
    {
        Assert.Throws<System.ArgumentException>(() => InputSystem.AddDevice(new InputDeviceDescription
        {
            product = "Nimbus+",
            interfaceName = "HID",
        }));
    }

    [Test]
    [Category("Devices")]
    [TestCase(0xd, 0x0)] // OSX dummy VID/PID
    public void Devices_DoNotSupportNimbusPlusAsHID_WithOnlyPidAndVid(int vendorId, int productId)
    {
        Assert.Throws<System.ArgumentException>(() => InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "HID",
            capabilities = new HID.HIDDeviceDescriptor
            {
                vendorId = vendorId,
                productId = productId,
            }.ToJson()
        }));
    }

    [Test]
    [Category("Devices")]
    [TestCase(true)]
    [TestCase(false)]
    public void Devices_SupportsNimbusPlusAsHID(bool precompiled)
    {
        if (!precompiled)
            InputControlLayout.s_Layouts.precompiledLayouts.Clear();

        var gamepad = InputSystem.AddDevice<NimbusGamepadHid>();

        // Variance 1
        InputSystem.QueueStateEvent(gamepad, new NimbusPlusHIDInputReport() {});
        InputSystem.Update();

        var comparer = new Vector2EqualityComparer(0.04f);
        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(Vector2.zero).Using(comparer));
        Assert.That(gamepad.rightStick.ReadValue(), Is.EqualTo(Vector2.zero).Using(comparer));

        Assert.That(gamepad.leftTrigger.isPressed, Is.False);
        Assert.That(gamepad.rightTrigger.isPressed, Is.False);

        Assert.That(gamepad.dpad.up.isPressed, Is.False);
        Assert.That(gamepad.dpad.right.isPressed, Is.False);
        Assert.That(gamepad.dpad.left.isPressed, Is.False);
        Assert.That(gamepad.dpad.down.isPressed, Is.False);

        Assert.That(gamepad.buttonSouth.isPressed, Is.False);
        Assert.That(gamepad.buttonNorth.isPressed, Is.False);
        Assert.That(gamepad.buttonWest.isPressed, Is.False);
        Assert.That(gamepad.buttonEast.isPressed, Is.False);

        Assert.That(gamepad.leftShoulder.isPressed, Is.False);
        Assert.That(gamepad.rightShoulder.isPressed, Is.False);
        Assert.That(gamepad.leftStickButton.isPressed, Is.False);
        Assert.That(gamepad.rightStickButton.isPressed, Is.False);

        Assert.That(gamepad.homeButton.isPressed, Is.False);

        Assert.That(gamepad.startButton.isPressed, Is.False);
        Assert.That(gamepad.selectButton.isPressed, Is.False);

        Assert.That(gamepad.homeButton.isPressed, Is.False);

        // Variance 2
        InputSystem.QueueStateEvent(gamepad, new NimbusPlusHIDInputReport()
        {
            leftStickX = 127,
            leftStickY = 0,
            rightStickX = 0,
            rightStickY = -64,
            leftTrigger = 10,
            rightTrigger = 180,
            buttons1 = (1 << 4) | (1 << 7) | (1 << 3) | (1 << 2),
            buttons2 = (1 << 0) | (1 << 3) | (1 << 4) | (1 << 5)
        });
        InputSystem.Update();

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(new Vector2(1.0f, 0.0f)).Using(comparer));
        Assert.That(gamepad.rightStick.ReadValue(), Is.EqualTo(new Vector2(0.0f, -0.5f)).Using(comparer));

        Assert.That(gamepad.leftTrigger.isPressed, Is.False);
        Assert.That(gamepad.rightTrigger.isPressed, Is.True);

        Assert.That(gamepad.dpad.up.isPressed, Is.False);
        Assert.That(gamepad.dpad.right.isPressed, Is.False);
        Assert.That(gamepad.dpad.left.isPressed, Is.True);
        Assert.That(gamepad.dpad.down.isPressed, Is.True);

        Assert.That(gamepad.buttonSouth.isPressed, Is.True);
        Assert.That(gamepad.buttonNorth.isPressed, Is.True);
        Assert.That(gamepad.buttonWest.isPressed, Is.False);
        Assert.That(gamepad.buttonEast.isPressed, Is.False);

        Assert.That(gamepad.leftShoulder.isPressed, Is.True);
        Assert.That(gamepad.rightShoulder.isPressed, Is.False);
        Assert.That(gamepad.leftStickButton.isPressed, Is.False);
        Assert.That(gamepad.rightStickButton.isPressed, Is.True);

        Assert.That(gamepad.startButton.isPressed, Is.False);
        Assert.That(gamepad.selectButton.isPressed, Is.True);

        Assert.That(gamepad.homeButton.isPressed, Is.True);

        // Variance 3
        InputSystem.QueueStateEvent(gamepad, new NimbusPlusHIDInputReport()
        {
            leftStickX = 0,
            leftStickY = 127,
            rightStickX = -64,
            rightStickY = 0,
            leftTrigger = 130,
            rightTrigger = 5,
            buttons1 = (1 << 6) | (1 << 0) | (1 << 1),
            buttons2 = (1 << 1) | (1 << 2) | (1 << 6)
        });
        InputSystem.Update();

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(new Vector2(0.0f, 1.0f)).Using(comparer));
        Assert.That(gamepad.rightStick.ReadValue(), Is.EqualTo(new Vector2(-0.5f, 0.0f)).Using(comparer));

        Assert.That(gamepad.leftTrigger.isPressed, Is.True);
        Assert.That(gamepad.rightTrigger.isPressed, Is.False);

        Assert.That(gamepad.dpad.up.isPressed, Is.True);
        Assert.That(gamepad.dpad.right.isPressed, Is.True);
        Assert.That(gamepad.dpad.left.isPressed, Is.False);
        Assert.That(gamepad.dpad.down.isPressed, Is.False);

        Assert.That(gamepad.buttonSouth.isPressed, Is.False);
        Assert.That(gamepad.buttonNorth.isPressed, Is.False);
        Assert.That(gamepad.buttonWest.isPressed, Is.True);
        Assert.That(gamepad.buttonEast.isPressed, Is.False);

        Assert.That(gamepad.leftShoulder.isPressed, Is.False);
        Assert.That(gamepad.rightShoulder.isPressed, Is.True);
        Assert.That(gamepad.leftStickButton.isPressed, Is.True);
        Assert.That(gamepad.rightStickButton.isPressed, Is.False);

        Assert.That(gamepad.startButton.isPressed, Is.True);
        Assert.That(gamepad.selectButton.isPressed, Is.False);

        Assert.That(gamepad.homeButton.isPressed, Is.False);
    }
}

#endif // UNITY_EDITOR || UNITY_OSX
