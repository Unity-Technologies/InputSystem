#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using NUnit.Framework;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.iOS;
using UnityEngine.Experimental.Input.Plugins.iOS.LowLevel;

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

        Assert.That(controller.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(controller.rightTrigger.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
        Assert.That(controller.leftStick.x.ReadValue(), Is.EqualTo(0.789).Within(0.000001));
        Assert.That(controller.leftStick.y.ReadValue(), Is.EqualTo(0.987).Within(0.000001));
        Assert.That(controller.rightStick.x.ReadValue(), Is.EqualTo(0.654).Within(0.000001));
        Assert.That(controller.rightStick.y.ReadValue(), Is.EqualTo(0.321).Within(0.000001));

        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.A), controller.buttonSouth);
        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.X), controller.buttonWest);
        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.Y), controller.buttonNorth);
        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.B), controller.buttonEast);
        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.LeftShoulder), controller.leftShoulder);
        AssertButtonPress(controller, new IOSGameControllerState().WithButton(IOSButton.RightShoulder), controller.rightShoulder);
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
