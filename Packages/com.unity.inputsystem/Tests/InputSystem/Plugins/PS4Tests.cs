using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.DualShock;
using UnityEngine.Experimental.Input.Plugins.DualShock.LowLevel;
using UnityEngine.Experimental.Input.Processors;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.TestTools.Utils;


public class PS4Tests : InputTestFixture
{
#if UNITY_EDITOR || UNITY_PS4

    [Test]
    [Category("Devices")]
    public void Devices_SupportsMoveOnPS4()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            deviceClass = "PS4MoveController",
            interfaceName = "PS4"
        });

        Assert.That(device, Is.AssignableTo<MoveControllerPS4>());
        var move = (MoveControllerPS4)device;

        InputSystem.QueueStateEvent(move,
            new MoveControllerStatePS4
            {
                buttons = 0xffffffff,
                trigger = 0.567f,
                accelerometer = new Vector3(0.987f, 0.654f, 0.321f),
                gyro = new Vector3(0.444f, 0.555f, 0.666f),
            });

        InputSystem.Update();

        Assert.That(move.squareButton.isPressed);
        Assert.That(move.triangleButton.isPressed);
        Assert.That(move.circleButton.isPressed);
        Assert.That(move.crossButton.isPressed);
        Assert.That(move.selectButton.isPressed);
        Assert.That(move.triggerButton.isPressed);
        Assert.That(move.moveButton.isPressed);
        Assert.That(move.startButton.isPressed);

        Assert.That(move.trigger.ReadValue(), Is.EqualTo(0.567).Within(0.00001));

        Assert.That(move.accelerometer.x.ReadValue(), Is.EqualTo(0.987).Within(0.00001));
        Assert.That(move.accelerometer.y.ReadValue(), Is.EqualTo(0.654).Within(0.00001));
        Assert.That(move.accelerometer.z.ReadValue(), Is.EqualTo(0.321).Within(0.00001));

        Assert.That(move.gyro.x.ReadValue(), Is.EqualTo(0.444).Within(0.00001));
        Assert.That(move.gyro.y.ReadValue(), Is.EqualTo(0.555).Within(0.00001));
        Assert.That(move.gyro.z.ReadValue(), Is.EqualTo(0.666).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanSetLightColorAndMotorSpeedsOnMoveController()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            deviceClass = "PS4MoveController",
            interfaceName = "PS4"
        });

        Assert.That(device, Is.AssignableTo<MoveControllerPS4>());
        var move = (MoveControllerPS4)device;

        MoveControllerPS4OuputCommand? receivedCommand = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(move.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == MoveControllerPS4OuputCommand.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = *((MoveControllerPS4OuputCommand*)commandPtr);
                        return 1;
                    }

                    Assert.Fail("Received wrong type of command");
                    return InputDeviceCommand.kGenericFailure;
                });
        }
        move.SetMotorSpeed(0.1234f);

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.motorSpeed, Is.EqualTo((byte)(0.1234 * 255)));

        receivedCommand = null;
        move.SetLightSphereColor(new Color(0.123f, 0.456f, 0.789f));

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.redColor, Is.EqualTo((byte)(0.123f * 255)));
        Assert.That(receivedCommand.Value.greenColor, Is.EqualTo((byte)(0.456f * 255)));
        Assert.That(receivedCommand.Value.blueColor, Is.EqualTo((byte)(0.789f * 255)));
    }

#endif
}
