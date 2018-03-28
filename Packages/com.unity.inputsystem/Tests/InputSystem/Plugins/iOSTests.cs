#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using ISX;
using ISX.Plugins.iOS;
using ISX.Plugins.iOS.LowLevel;
using NUnit.Framework;

class iOSTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_SupportsiOSGamePad()
    {
        var device = InputSystem.AddDevice(
                new InputDeviceDescription
        {
            interfaceName = "iOS",
            deviceClass = "iOSGameController"    
        });

        Assert.That(device, Is.TypeOf<IOSGameController>());
        var controller = (IOSGameController)device;

        InputSystem.QueueStateEvent(controller,
            new IOSGameControllerState()
            .WithButton(IOSButton.LeftTrigger, true, 0.123f)
            .WithButton(IOSButton.RightTrigger, true, 0.456f)
            .WithAxis(IOSAxis.LeftStickX, 0.789f)
            .WithAxis(IOSAxis.LeftStickY, 0.987f)
            .WithAxis(IOSAxis.RightStickX, 0.654f)
            .WithAxis(IOSAxis.RightStickY, 0.321f));

        InputSystem.Update();

        Assert.That(controller.leftTrigger.value, Is.EqualTo(0.123).Within(0.000001));
        Assert.That(controller.rightTrigger.value, Is.EqualTo(0.456).Within(0.000001));
        Assert.That(controller.leftStick.x.value, Is.EqualTo(0.789).Within(0.000001));
        Assert.That(controller.leftStick.y.value, Is.EqualTo(0.987).Within(0.000001));
        Assert.That(controller.rightStick.x.value, Is.EqualTo(0.654).Within(0.000001));
        Assert.That(controller.rightStick.y.value, Is.EqualTo(0.321).Within(0.000001));

        /// FIXME, these will fail saying start/or select buttons were not expected to be pressed.
        /// The problem is iOSGameController doesn't have those, so mappings for those are not set.
        /// Because iOSGameController is derived from Gamepad, derived mappings are picked which are basically wrong
        /// Question: how to disable those controls?
        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.A), controller.buttonSouth);
        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.X), controller.buttonWest);
        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.Y), controller.buttonNorth);
        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.B), controller.buttonEast);
        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.LeftShoulder), controller.leftShoulder);
        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.RightShoulder), controller.rightShoulder);
    }

  

}
#endif // UNITY_EDITOR || UNITY_ANDROID
