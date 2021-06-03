#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Switch.LowLevel;
using UnityEngine.InputSystem.Processors;

internal class SwitchTests : CoreTestsFixture
{
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA
    [Test]
    [Category("Devices")]
    public void Devices_SupportsHIDNpad()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            vendorId = 0x57e,
            productId = 0x2009,
        };

        var device = InputSystem.AddDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            });

        Assert.That(device, Is.TypeOf<SwitchProControllerHID>());
        var controller = (SwitchProControllerHID)device;

        InputSystem.QueueStateEvent(controller,
            new SwitchProControllerHIDInputState
            {
                leftStickX = 0x1000,
                leftStickY = 0x1000,
                rightStickX = 0x7fff,
                rightStickY = 0xefff,
            });
        InputSystem.Update();

        var leftStickDeadzone = controller.leftStick.TryGetProcessor<StickDeadzoneProcessor>();
        var rightStickDeadzone = controller.rightStick.TryGetProcessor<StickDeadzoneProcessor>();

        Assert.That(Vector2.Distance(controller.leftStick.ReadValue(), leftStickDeadzone.Process(new Vector2(-1.0f, 1.0f))), Is.LessThan(0.0001f));
        Assert.That(Vector2.Distance(controller.rightStick.ReadValue(), rightStickDeadzone.Process(new Vector2(0.0f, -1.0f))), Is.LessThan(0.0001f));

        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.A), controller.buttonEast);
        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.B), controller.buttonSouth);
        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.X), controller.buttonNorth);
        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.Y), controller.buttonWest);
        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.StickL), controller.leftStickButton);
        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.StickR), controller.rightStickButton);
        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.L), controller.leftShoulder);
        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.R), controller.rightShoulder);
        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.ZL), controller.leftTrigger);
        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.ZR), controller.rightTrigger);
        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.Plus), controller.startButton);
        AssertButtonPress(controller, new SwitchProControllerHIDInputState().WithButton(SwitchProControllerHIDInputState.Button.Minus), controller.selectButton);
    }

#endif
}
#endif
