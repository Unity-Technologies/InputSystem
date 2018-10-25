#if UNITY_EDITOR || UNITY_SWITCH
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
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

        InputSystem.QueueStateEvent(controller,
            new NPadInputState
            {
                leftStick = new Vector2(0.123f, 0.456f),
                rightStick = new Vector2(0.789f, 0.987f),
                acceleration = new Vector3(0.987f, 0.654f, 0.321f),
                attitude = new Quaternion(0.111f, 0.222f, 0.333f, 0.444f),
                angularVelocity = new Vector3(0.444f, 0.555f, 0.666f),
            });
        InputSystem.Update();

        Assert.That(controller.leftStick.x.ReadValue(), Is.EqualTo(0.123).Within(0.00001));
        Assert.That(controller.leftStick.y.ReadValue(), Is.EqualTo(0.456).Within(0.00001));
        Assert.That(controller.rightStick.x.ReadValue(), Is.EqualTo(0.789).Within(0.00001));
        Assert.That(controller.rightStick.y.ReadValue(), Is.EqualTo(0.987).Within(0.00001));

        Assert.That(controller.acceleration.x.ReadValue(), Is.EqualTo(0.987).Within(0.00001));
        Assert.That(controller.acceleration.y.ReadValue(), Is.EqualTo(0.654).Within(0.00001));
        Assert.That(controller.acceleration.z.ReadValue(), Is.EqualTo(0.321).Within(0.00001));

        Quaternion attitude = controller.attitude.ReadValue();

        Assert.That(attitude.x, Is.EqualTo(0.111).Within(0.00001));
        Assert.That(attitude.y, Is.EqualTo(0.222).Within(0.00001));
        Assert.That(attitude.z, Is.EqualTo(0.333).Within(0.00001));
        Assert.That(attitude.w, Is.EqualTo(0.444).Within(0.00001));

        Assert.That(controller.angularVelocity.x.ReadValue(), Is.EqualTo(0.444).Within(0.00001));
        Assert.That(controller.angularVelocity.y.ReadValue(), Is.EqualTo(0.555).Within(0.00001));
        Assert.That(controller.angularVelocity.z.ReadValue(), Is.EqualTo(0.666).Within(0.00001));

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

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanUpdateStatus()
    {
        var controller = InputSystem.AddDevice<NPad>();

        NPadStatusReport? receivedCommand = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(controller.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == NPadStatusReport.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = *((NPadStatusReport*)commandPtr);
                        ((NPadStatusReport*)commandPtr)->npadId = NPad.NpadId.Handheld;
                        ((NPadStatusReport*)commandPtr)->orientation = NPad.Orientation.Vertical;
                        ((NPadStatusReport*)commandPtr)->styleMask = NPad.NpadStyle.Handheld;

                        ((NPadStatusReport*)commandPtr)->colorLeftMain = ColorToNNColor(Color.red);
                        ((NPadStatusReport*)commandPtr)->colorLeftSub = ColorToNNColor(Color.black);
                        ((NPadStatusReport*)commandPtr)->colorRightMain = ColorToNNColor(Color.cyan);
                        ((NPadStatusReport*)commandPtr)->colorRightSub = ColorToNNColor(Color.gray);
                        return 1;
                    }
                    else if (commandPtr->type == QueryUserIdCommand.Type)
                    {
                        // Sending this command happens before refreshing NPad status
                        return 1;
                    }

                    Assert.Fail("Received wrong type of command, " + commandPtr->type);
                    return InputDeviceCommand.kGenericFailure;
                });
        }
        Assert.That(controller.npadId, Is.EqualTo(NPad.NpadId.Handheld));
        Assert.That(controller.orientation, Is.EqualTo(NPad.Orientation.Vertical));
        Assert.That(controller.styleMask, Is.EqualTo(NPad.NpadStyle.Handheld));
        Assert.That(controller.leftControllerColor.Main, Is.EqualTo((Color32)Color.red));
        Assert.That(controller.leftControllerColor.Sub, Is.EqualTo((Color32)Color.black));
        Assert.That(controller.rightControllerColor.Main, Is.EqualTo((Color32)Color.cyan));
        Assert.That(controller.rightControllerColor.Sub, Is.EqualTo((Color32)Color.gray));
    }

    private int ColorToNNColor(Color color)
    {
        Color32 color32 = color;

        return (int)color32.r | ((int)color32.g << 8) | ((int)color32.b << 16) | ((int)color32.a << 24);
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanSetControllerOrientation()
    {
        var controller = InputSystem.AddDevice<NPad>();

        NpadDeviceIOCTLSetOrientation? receivedCommand = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(controller.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == NpadDeviceIOCTLSetOrientation.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = *((NpadDeviceIOCTLSetOrientation*)commandPtr);
                        return 1;
                    }

                    Assert.Fail("Received wrong type of command");
                    return InputDeviceCommand.kGenericFailure;
                });
        }
        controller.SetOrientationToSingleJoyCon(NPad.Orientation.Horizontal);

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.orientation, Is.EqualTo(NPad.Orientation.Horizontal));

        receivedCommand = null;
        controller.SetOrientationToSingleJoyCon(NPad.Orientation.Vertical);

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.orientation, Is.EqualTo(NPad.Orientation.Vertical));
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanStartSixAxisSensors()
    {
        var controller = InputSystem.AddDevice<NPad>();

        NpadDeviceIOCTLStartSixAxisSensor? receivedCommand = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(controller.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == NpadDeviceIOCTLStartSixAxisSensor.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = *((NpadDeviceIOCTLStartSixAxisSensor*)commandPtr);
                        return 1;
                    }

                    Assert.Fail("Received wrong type of command");
                    return InputDeviceCommand.kGenericFailure;
                });
        }
        controller.StartSixAxisSensor();

        Assert.That(receivedCommand.HasValue, Is.True);
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanStopSixAxisSensors()
    {
        var controller = InputSystem.AddDevice<NPad>();

        NpadDeviceIOCTLStopSixAxisSensor? receivedCommand = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(controller.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == NpadDeviceIOCTLStopSixAxisSensor.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = *((NpadDeviceIOCTLStopSixAxisSensor*)commandPtr);
                        return 1;
                    }

                    Assert.Fail("Received wrong type of command");
                    return InputDeviceCommand.kGenericFailure;
                });
        }
        controller.StopSixAxisSensor();

        Assert.That(receivedCommand.HasValue, Is.True);
    }
}
#endif
