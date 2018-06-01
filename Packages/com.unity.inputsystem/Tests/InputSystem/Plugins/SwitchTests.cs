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
            interfaceName = "Switch",
            manufacturer = "Nintendo",
            product = "Wireless Controller",
        });

        Assert.That(device, Is.TypeOf<NPad>());
        var controller = (NPad)device;

        InputSystem.Update();

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
    }
}
#endif
