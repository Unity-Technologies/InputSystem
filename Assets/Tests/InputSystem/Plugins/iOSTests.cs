#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Plugins.iOS;
using UnityEngine.InputSystem.Plugins.iOS.LowLevel;
using UnityEngine.InputSystem.Processors;

internal class iOSTests : InputTestFixture
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

        Assert.That(device, Is.TypeOf<iOSGameController>());
        var controller = (iOSGameController)device;

        InputSystem.QueueStateEvent(controller,
            new iOSGameControllerState()
                .WithButton(iOSButton.LeftTrigger, true, 0.123f)
                .WithButton(iOSButton.RightTrigger, true, 0.456f)
                .WithAxis(iOSAxis.LeftStickX, 0.789f)
                .WithAxis(iOSAxis.LeftStickY, 0.987f)
                .WithAxis(iOSAxis.RightStickX, 0.654f)
                .WithAxis(iOSAxis.RightStickY, 0.321f));

        InputSystem.Update();

        var leftStickDeadzone = controller.leftStick.TryGetProcessor<StickDeadzoneProcessor>();
        var rightStickDeadzone = controller.leftStick.TryGetProcessor<StickDeadzoneProcessor>();

        Assert.That(controller.leftStick.ReadValue(), Is.EqualTo(leftStickDeadzone.Process(new Vector2(0.789f, 0.987f))));
        Assert.That(controller.rightStick.ReadValue(), Is.EqualTo(rightStickDeadzone.Process(new Vector2(0.654f, 0.321f))));
        Assert.That(controller.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(controller.rightTrigger.ReadValue(), Is.EqualTo(0.456).Within(0.000001));

        AssertButtonPress(controller, new iOSGameControllerState().WithButton(iOSButton.A), controller.buttonSouth);
        AssertButtonPress(controller, new iOSGameControllerState().WithButton(iOSButton.X), controller.buttonWest);
        AssertButtonPress(controller, new iOSGameControllerState().WithButton(iOSButton.Y), controller.buttonNorth);
        AssertButtonPress(controller, new iOSGameControllerState().WithButton(iOSButton.B), controller.buttonEast);
        AssertButtonPress(controller, new iOSGameControllerState().WithButton(iOSButton.LeftShoulder), controller.leftShoulder);
        AssertButtonPress(controller, new iOSGameControllerState().WithButton(iOSButton.RightShoulder), controller.rightShoulder);
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
