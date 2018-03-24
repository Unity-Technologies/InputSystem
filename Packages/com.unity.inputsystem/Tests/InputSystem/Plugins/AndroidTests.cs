#if UNITY_EDITOR || UNITY_ANDROID
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Android;
using UnityEngine.Experimental.Input.Plugins.Android.LowLevel;
using NUnit.Framework;

class AndroidTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_SupportsAndroidGamepad()
    {
        var device = InputSystem.AddDevice(
                new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidGameController"     ////TODO: have backend report this as just "Gamepad" or "Controller"
        });

        Assert.That(device, Is.TypeOf<AndroidGameController>());
        var controller = (AndroidGameController)device;

        InputSystem.QueueStateEvent(controller,
            new AndroidGameControllerState()
            .WithAxis(AndroidAxis.Ltrigger, 0.123f)
            .WithAxis(AndroidAxis.Rtrigger, 0.456f)
            .WithAxis(AndroidAxis.X, 0.789f)
            .WithAxis(AndroidAxis.Y, 0.987f)
            .WithAxis(AndroidAxis.Z, 0.654f)
            .WithAxis(AndroidAxis.Rx, 0.321f));

        InputSystem.Update();

        Assert.That(controller.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(controller.rightTrigger.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
        Assert.That(controller.leftStick.x.ReadValue(), Is.EqualTo(0.789).Within(0.000001));
        Assert.That(controller.leftStick.y.ReadValue(), Is.EqualTo(0.987).Within(0.000001));
        Assert.That(controller.rightStick.x.ReadValue(), Is.EqualTo(0.654).Within(0.000001));
        Assert.That(controller.rightStick.y.ReadValue(), Is.EqualTo(0.321).Within(0.000001));

        AssertButtonPress(controller, new AndroidGameControllerState().WithButton(AndroidKeyCode.ButtonA), controller.buttonSouth);
        AssertButtonPress(controller, new AndroidGameControllerState().WithButton(AndroidKeyCode.ButtonX), controller.buttonWest);
        AssertButtonPress(controller, new AndroidGameControllerState().WithButton(AndroidKeyCode.ButtonY), controller.buttonNorth);
        AssertButtonPress(controller, new AndroidGameControllerState().WithButton(AndroidKeyCode.ButtonB), controller.buttonEast);
        AssertButtonPress(controller, new AndroidGameControllerState().WithButton(AndroidKeyCode.ButtonThumbl), controller.leftStickButton);
        AssertButtonPress(controller, new AndroidGameControllerState().WithButton(AndroidKeyCode.ButtonThumbr), controller.rightStickButton);
        AssertButtonPress(controller, new AndroidGameControllerState().WithButton(AndroidKeyCode.ButtonL1), controller.leftShoulder);
        AssertButtonPress(controller, new AndroidGameControllerState().WithButton(AndroidKeyCode.ButtonR1), controller.rightShoulder);
        AssertButtonPress(controller, new AndroidGameControllerState().WithButton(AndroidKeyCode.ButtonStart), controller.startButton);
        AssertButtonPress(controller, new AndroidGameControllerState().WithButton(AndroidKeyCode.ButtonSelect), controller.selectButton);
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsAndroidGamepad_WithAxisDpad()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice(
                new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidGameController",
            capabilities = new AndroidDeviceCapabilities
            {
                motionAxes = new[]
                {
                    AndroidAxis.Generic1, // Noise
                    AndroidAxis.HatX,
                    AndroidAxis.Generic2, // Noise
                    AndroidAxis.HatY
                }
            }.ToJson()
        });

        InputSystem.QueueStateEvent(gamepad,
            new AndroidGameControllerState()
            .WithAxis(AndroidAxis.HatX, 0.789f)
            .WithAxis(AndroidAxis.HatY, 0.987f));
        InputSystem.Update();

        Assert.That(gamepad.dpad.left.isPressed, Is.False);
        Assert.That(gamepad.dpad.right.isPressed, Is.True);
        Assert.That(gamepad.dpad.up.isPressed, Is.True);
        Assert.That(gamepad.dpad.down.isPressed, Is.False);

        InputSystem.QueueStateEvent(gamepad,
            new AndroidGameControllerState()
            .WithAxis(AndroidAxis.HatX, 0.123f)
            .WithAxis(AndroidAxis.HatY, 0.456f));
        InputSystem.Update();

        Assert.That(gamepad.dpad.left.isPressed, Is.True);
        Assert.That(gamepad.dpad.right.isPressed, Is.False);
        Assert.That(gamepad.dpad.up.isPressed, Is.False);
        Assert.That(gamepad.dpad.down.isPressed, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsAndroidGamepad_WithButtonDpad()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice(
                new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidGameController",
            capabilities = new AndroidDeviceCapabilities
            {
                motionAxes = new[]
                {
                    AndroidAxis.Generic1, // Noise
                    AndroidAxis.Generic2, // Noise
                }
            }.ToJson()
        });

        AssertButtonPress(gamepad, new AndroidGameControllerState().WithButton(AndroidKeyCode.DpadDown), gamepad.dpad.down);
        AssertButtonPress(gamepad, new AndroidGameControllerState().WithButton(AndroidKeyCode.DpadUp), gamepad.dpad.up);
        AssertButtonPress(gamepad, new AndroidGameControllerState().WithButton(AndroidKeyCode.DpadLeft), gamepad.dpad.left);
        AssertButtonPress(gamepad, new AndroidGameControllerState().WithButton(AndroidKeyCode.DpadRight), gamepad.dpad.right);
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
