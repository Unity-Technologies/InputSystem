#if DEVELOPMENT_BUILD || UNITY_EDITOR
using ISX;
using ISX.XInput;
using NUnit.Framework;

public class XInputTests : InputTestFixture
{
    public override void Setup()
    {
        base.Setup();

        XInputSupport.Initialize();
    }

    [Test]
    [Category("Devices")]
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    [TestCase("Xbox One Wired Controller", "Microsoft", "HID", "Gamepad")]
#endif
    public void Devices_SupportsXInputDevicesOnPlatform(string product, string manufacturer, string interfaceName, string baseTemplate)
    {
        var description = new InputDeviceDescription
        {
            interfaceName = interfaceName,
            product = product,
            manufacturer = manufacturer
        };

        InputDevice device = null;
        Assert.That(() => device = InputSystem.AddDevice(description), Throws.Nothing);

        Assert.That(InputSystem.GetControls(string.Format("/<{0}>", baseTemplate)), Has.Exactly(1).SameAs(device));
        Assert.That(device.name, Is.EqualTo(baseTemplate));
        Assert.That(device.description.manufacturer, Is.EqualTo(manufacturer));
        Assert.That(device.description.interfaceName, Is.EqualTo(interfaceName));
        Assert.That(device.description.product, Is.EqualTo(product));
    }
}
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
