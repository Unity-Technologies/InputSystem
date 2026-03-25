#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.iOS;
using UnityEngine.InputSystem.iOS.LowLevel;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

internal class iOSTests : CoreTestsFixture
{
    [Test]
    [Category("Devices")]
    [TestCase(null, typeof(iOSGameController), typeof(Gamepad))]
    [TestCase("Xbox Wireless Controller", typeof(XboxOneGampadiOS), typeof(XInputController))]
    [TestCase("DUALSHOCK 4 Wireless Controller", typeof(DualShock4GampadiOS), typeof(DualShockGamepad))]
    [TestCase("DualSense Wireless Controller", typeof(DualSenseGampadiOS), typeof(DualShockGamepad))]
    public void Devices_SupportsiOSGamePad(string product, Type deviceType, Type parentType)
    {
        var device = InputSystem.AddDevice(
            new InputDeviceDescription
            {
                interfaceName = "iOS",
                deviceClass = "iOSGameController",
                product = product
            });

        Assert.That(device, Is.TypeOf(deviceType));
        Assert.That(device, Is.InstanceOf(parentType));

        var gamepad = (Gamepad)device;

        InputSystem.QueueStateEvent(gamepad,
            new iOSGameControllerState()
                .WithButton(iOSButton.LeftTrigger, true, 0.123f)
                .WithButton(iOSButton.RightTrigger, true, 0.456f)
                .WithAxis(iOSAxis.LeftStickX, 0.789f)
                .WithAxis(iOSAxis.LeftStickY, 0.987f)
                .WithAxis(iOSAxis.RightStickX, 0.654f)
                .WithAxis(iOSAxis.RightStickY, 0.321f));

        InputSystem.Update();

        var leftStickDeadzone = gamepad.leftStick.TryGetProcessor<StickDeadzoneProcessor>();
        var rightStickDeadzone = gamepad.leftStick.TryGetProcessor<StickDeadzoneProcessor>();

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(leftStickDeadzone.Process(new Vector2(0.789f, 0.987f))));
        Assert.That(gamepad.rightStick.ReadValue(), Is.EqualTo(rightStickDeadzone.Process(new Vector2(0.654f, 0.321f))));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(0.456).Within(0.000001));

        AssertButtonPress(gamepad, new iOSGameControllerState().WithButton(iOSButton.A), gamepad.buttonSouth);
        AssertButtonPress(gamepad, new iOSGameControllerState().WithButton(iOSButton.A), gamepad.aButton);
        AssertButtonPress(gamepad, new iOSGameControllerState().WithButton(iOSButton.X), gamepad.buttonWest);
        AssertButtonPress(gamepad, new iOSGameControllerState().WithButton(iOSButton.X), gamepad.xButton);
        AssertButtonPress(gamepad, new iOSGameControllerState().WithButton(iOSButton.Y), gamepad.buttonNorth);
        AssertButtonPress(gamepad, new iOSGameControllerState().WithButton(iOSButton.Y), gamepad.yButton);
        AssertButtonPress(gamepad, new iOSGameControllerState().WithButton(iOSButton.B), gamepad.buttonEast);
        AssertButtonPress(gamepad, new iOSGameControllerState().WithButton(iOSButton.B), gamepad.bButton);
        AssertButtonPress(gamepad, new iOSGameControllerState().WithButton(iOSButton.LeftShoulder), gamepad.leftShoulder);
        AssertButtonPress(gamepad, new iOSGameControllerState().WithButton(iOSButton.RightShoulder), gamepad.rightShoulder);
    }

    [Test]
    [Category("Devices")]
    // this is a new test, as we need to assert the Nintendo layout (e.g. buttonSouth == B button)
    public void Devices_SupportsSwitchProControlleriOS()
    {
        var device = InputSystem.AddDevice(
            new InputDeviceDescription
            {
                interfaceName = "iOS",
                deviceClass = "iOSGameController",
                product = "Pro Controller"
            });
        Assert.That(device, Is.TypeOf(typeof(SwitchProControlleriOS)));
        Assert.That(device, Is.InstanceOf(typeof(SwitchProController)));
        Assert.That(device, Is.InstanceOf(typeof(Gamepad)));

        var gamepad = (SwitchProControlleriOS)device;

        InputSystem.QueueStateEvent(gamepad,
            new iOSGameControllerStateSwappedFaceButtons()
                .WithButton(iOSButton.LeftTrigger, true, 0.123f)
                .WithButton(iOSButton.RightTrigger, true, 0.456f)
                .WithAxis(iOSAxis.LeftStickX, 0.789f)
                .WithAxis(iOSAxis.LeftStickY, 0.987f)
                .WithAxis(iOSAxis.RightStickX, 0.654f)
                .WithAxis(iOSAxis.RightStickY, 0.321f));
        InputSystem.Update();

        var leftStickDeadzone = gamepad.leftStick.TryGetProcessor<StickDeadzoneProcessor>();
        var rightStickDeadzone = gamepad.leftStick.TryGetProcessor<StickDeadzoneProcessor>();

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(leftStickDeadzone.Process(new Vector2(0.789f, 0.987f))));
        Assert.That(gamepad.rightStick.ReadValue(), Is.EqualTo(rightStickDeadzone.Process(new Vector2(0.654f, 0.321f))));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
        // testing for Pro Controller layout...
        AssertButtonPress(gamepad, new iOSGameControllerStateSwappedFaceButtons().WithButton(iOSButton.A), gamepad.buttonEast);
        AssertButtonPress(gamepad, new iOSGameControllerStateSwappedFaceButtons().WithButton(iOSButton.A), gamepad.aButton);
        AssertButtonPress(gamepad, new iOSGameControllerStateSwappedFaceButtons().WithButton(iOSButton.X), gamepad.buttonNorth);
        AssertButtonPress(gamepad, new iOSGameControllerStateSwappedFaceButtons().WithButton(iOSButton.X), gamepad.xButton);
        AssertButtonPress(gamepad, new iOSGameControllerStateSwappedFaceButtons().WithButton(iOSButton.Y), gamepad.buttonWest);
        AssertButtonPress(gamepad, new iOSGameControllerStateSwappedFaceButtons().WithButton(iOSButton.Y), gamepad.yButton);
        AssertButtonPress(gamepad, new iOSGameControllerStateSwappedFaceButtons().WithButton(iOSButton.B), gamepad.buttonSouth);
        AssertButtonPress(gamepad, new iOSGameControllerStateSwappedFaceButtons().WithButton(iOSButton.B), gamepad.bButton);
        AssertButtonPress(gamepad, new iOSGameControllerStateSwappedFaceButtons().WithButton(iOSButton.LeftShoulder), gamepad.leftShoulder);
        AssertButtonPress(gamepad, new iOSGameControllerStateSwappedFaceButtons().WithButton(iOSButton.RightShoulder), gamepad.rightShoulder);
        Assert.Contains("Submit", gamepad.buttonEast.usages.m_Array);
        Assert.Contains("PrimaryAction", gamepad.aButton.usages.m_Array);
        Assert.Contains("Back", gamepad.bButton.usages.m_Array);
        Assert.Contains("Cancel", gamepad.bButton.usages.m_Array);
        Assert.Contains("SecondaryAction", gamepad.yButton.usages.m_Array);
    }

    [Test]
    [Category("Devices")]
    [TestCase("Gravity", typeof(GravitySensor))]
    [TestCase("Attitude", typeof(AttitudeSensor))]
    [TestCase("LinearAcceleration", typeof(LinearAccelerationSensor))]
    public void Devices_SupportsiOSSensors(string deviceClass, Type sensorType)
    {
        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            interfaceName = "iOS",
            deviceClass = deviceClass
        });

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices[0], Is.TypeOf(sensorType));
        Assert.That(InputSystem.devices[0].description.interfaceName, Is.EqualTo("iOS"));
        Assert.That(InputSystem.devices[0].description.deviceClass, Is.EqualTo(deviceClass));
    }

#if UNITY_EDITOR || UNITY_IOS
    [Test]
    [Category("Devices")]
    public void Devices_SupportsiOSStepCounter()
    {
        var device = InputSystem.AddDevice<iOSStepCounter>();
        LogAssert.Expect(LogType.Error, "Please enable Motion Usage in Input Settings before using Step Counter.");
        InputSystem.EnableDevice(device);

        InputSystem.settings.iOS.motionUsage.enabled = true;
        InputSystem.EnableDevice(device);
        InputSystem.QueueStateEvent(device, new iOSStepCounterState(){stepCounter = 5});
        InputSystem.Update();
        Assert.That(device.stepCounter.ReadValue(), Is.EqualTo(5));
    }

#endif
}
#endif // UNITY_EDITOR || UNITY_ANDROID
