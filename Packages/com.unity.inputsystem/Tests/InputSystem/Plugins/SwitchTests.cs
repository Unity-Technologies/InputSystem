#if UNITY_EDITOR || UNITY_SWITCH
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Switch;
using UnityEngine.Experimental.Input.Plugins.Switch.LowLevel;

public class SwitchTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_SupportsSwitchNpad()
    {
        var device = InputSystem.AddDevice(
                new InputDeviceDescription
        {
            interfaceName = "NPad",
            manufacturer = "Nintendo",
            product = "Wireless Controller",
        });

        Assert.That(device, Is.TypeOf<NPad>());
        var controller = (NPad)device;

        InputSystem.QueueStateEvent(controller,
            new NPadInputState()
            .WithLeftStick(new Vector2(0.123f, 0.456f))
            .WithRightStick(new Vector2(0.789f, 0.987f)));
        InputSystem.Update();

        Assert.That(controller.leftStick.x.ReadValue(), Is.EqualTo(0.123).Within(0.00001));
        Assert.That(controller.leftStick.y.ReadValue(), Is.EqualTo(0.456).Within(0.00001));
        Assert.That(controller.rightStick.x.ReadValue(), Is.EqualTo(0.789).Within(0.00001));
        Assert.That(controller.rightStick.y.ReadValue(), Is.EqualTo(0.987).Within(0.00001));

        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.A), controller.buttonEast);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.B), controller.buttonSouth);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.X), controller.buttonNorth);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.Y), controller.buttonWest);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.StickL), controller.leftStickButton);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.StickR), controller.rightStickButton);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.L), controller.leftShoulder);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.R), controller.rightShoulder);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.ZL), controller.leftTrigger);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.ZR), controller.rightTrigger);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.Plus), controller.startButton);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.Minus), controller.selectButton);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.LSL), controller.leftSL);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.LSR), controller.leftSR);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.RSL), controller.rightSL);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.RSR), controller.rightSR);

        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.VKey_LUp), controller.leftVK.up);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.VKey_LDown), controller.leftVK.down);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.VKey_LLeft), controller.leftVK.left);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.VKey_LRight), controller.leftVK.right);

        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.VKey_RUp), controller.rightVK.up);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.VKey_RDown), controller.rightVK.down);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.VKey_RLeft), controller.rightVK.left);
        AssertButtonPress(controller, new NPadInputState().WithButton(NPadInputState.Button.VKey_RRight), controller.rightVK.right);
    }
}
#endif
