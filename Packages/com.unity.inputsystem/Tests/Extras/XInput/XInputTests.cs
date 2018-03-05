using ISX;
using ISX.Plugins.XInput;
using NUnit.Framework;
using UnityEngine;

class XInputTests : InputTestFixture
{
    public override void Setup()
    {
        base.Setup();

        XInputSupport.Initialize();
    }

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
}
