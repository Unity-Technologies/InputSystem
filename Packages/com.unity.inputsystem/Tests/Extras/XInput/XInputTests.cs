using ISX;
using ISX.Plugins.XInput;
using ISX.Plugins.XInput.LowLevel;
using NUnit.Framework;
using UnityEngine;

class XInputTests : InputTestFixture
{
    public override void Setup()
    {
        base.Setup();

        XInputSupport.Initialize();
    }

    ////TODO: refactor this into two tests that send actual state and test the wiring
    [Test]
    [Category("Devices")]
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    [TestCase("Xbox One Wired Controller", "Microsoft", "HID", "XInputControllerOSX")]
#endif
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [TestCase(null, null, "XInput", "XInputControllerWindows")]
#endif
    public void Devices_SupportsXInputDevicesOnPlatform(string product, string manufacturer, string interfaceName, string templateName)
    {
        var description = new InputDeviceDescription
        {
            interfaceName = interfaceName,
            product = product,
            manufacturer = manufacturer
        };

        InputDevice device = null;
        Assert.That(() => device = InputSystem.AddDevice(description), Throws.Nothing);

        Assert.That(InputSystem.GetControls(string.Format("/<{0}>", templateName)), Has.Exactly(1).SameAs(device));
        Assert.That(device.name, Is.EqualTo(templateName));
        Assert.That(device.description.manufacturer, Is.EqualTo(manufacturer));
        Assert.That(device.description.interfaceName, Is.EqualTo(interfaceName));
        Assert.That(device.description.product, Is.EqualTo(product));
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
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

#if UNITY_EDITOR || UNITY_XBOXONE
    [Test]
    [Category("Devices")]
    public void Devices_SupportsControllerOnXbox()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            deviceClass = "XboxOneGamepad", ////REVIEW: this should be the product name instead
            interfaceName = "Xbox"
        });

        Assert.That(device, Is.AssignableTo<XInputController>());
        Assert.That(device, Is.AssignableTo<XboxOneGamepad>());
        var gamepad = (XboxOneGamepad)device;

        InputSystem.QueueStateEvent(gamepad,
            new XboxOneGamepadState
        {
            leftStick = new Vector2(0.123f, 0.456f),
            rightStick = new Vector2(0.789f, 0.234f),
            leftTrigger = 0.567f,
            rightTrigger = 0.891f,
            buttons = 0xffffffff
        });
        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.value, Is.EqualTo(0.123).Within(0.00001));
        Assert.That(gamepad.leftStick.y.value, Is.EqualTo(0.456).Within(0.00001));
        Assert.That(gamepad.rightStick.x.value, Is.EqualTo(0.789).Within(0.00001));
        Assert.That(gamepad.rightStick.y.value, Is.EqualTo(0.234).Within(0.00001));
        Assert.That(gamepad.leftTrigger.value, Is.EqualTo(0.567).Within(0.00001));
        Assert.That(gamepad.rightTrigger.value, Is.EqualTo(0.891).Within(0.00001));

        Assert.That(gamepad.menu.isPressed);
        Assert.That(gamepad.view.isPressed);
        Assert.That(gamepad.startButton.isPressed);
        Assert.That(gamepad.selectButton.isPressed);
        Assert.That(gamepad.aButton.isPressed);
        Assert.That(gamepad.bButton.isPressed);
        Assert.That(gamepad.xButton.isPressed);
        Assert.That(gamepad.yButton.isPressed);
        Assert.That(gamepad.buttonEast.isPressed);
        Assert.That(gamepad.buttonWest.isPressed);
        Assert.That(gamepad.buttonNorth.isPressed);
        Assert.That(gamepad.buttonSouth.isPressed);
        Assert.That(gamepad.paddle1.isPressed);
        Assert.That(gamepad.paddle2.isPressed);
        Assert.That(gamepad.paddle3.isPressed);
        Assert.That(gamepad.paddle4.isPressed);
        Assert.That(gamepad.leftShoulder.isPressed);
        Assert.That(gamepad.rightShoulder.isPressed);
    }

#endif
}
