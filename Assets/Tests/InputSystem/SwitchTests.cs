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
using UnityEngine.TestTools.Utils;

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
                leftStickX = 0x10,
                leftStickY = 0x10,
                rightStickX = 0x80,
                rightStickY = 0xf2,
            });
        InputSystem.Update();

        var leftStickDeadzone = controller.leftStick.TryGetProcessor<StickDeadzoneProcessor>();
        var rightStickDeadzone = controller.rightStick.TryGetProcessor<StickDeadzoneProcessor>();

        var currentLeft = controller.leftStick.ReadValue();
        var expectedLeft = leftStickDeadzone.Process(new Vector2(-1.0f, 1.0f));

        var currentRight = controller.rightStick.ReadValue();
        var expectedRight = rightStickDeadzone.Process(new Vector2(0.0f, -1.0f));

        Assert.That(currentLeft, Is.EqualTo(expectedLeft).Using(Vector2EqualityComparer.Instance));
        Assert.That(currentRight, Is.EqualTo(expectedRight).Using(new Vector2EqualityComparer(0.01f)));

        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.A), controller.buttonEast);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.B), controller.buttonSouth);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.X), controller.buttonNorth);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.Y), controller.buttonWest);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.StickL), controller.leftStickButton);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.StickR), controller.rightStickButton);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.L), controller.leftShoulder);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.R), controller.rightShoulder);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.ZL), controller.leftTrigger);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.ZR), controller.rightTrigger);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.Plus), controller.startButton);
        AssertButtonPress(controller, StateWithButton(SwitchProControllerHIDInputState.Button.Minus), controller.selectButton);
    }

    private static SwitchProControllerHIDInputState StateWithButton(SwitchProControllerHIDInputState.Button button)
    {
        return new SwitchProControllerHIDInputState
        {
            leftStickX = 0x7f,
            leftStickY = 0x7f,
            rightStickX = 0x7f,
            rightStickY = 0x7f,
        }.WithButton(button);
    }

#endif
}
#endif
