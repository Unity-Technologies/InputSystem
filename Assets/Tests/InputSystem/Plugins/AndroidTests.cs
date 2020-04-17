#if UNITY_EDITOR || UNITY_ANDROID
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Android;
using UnityEngine.InputSystem.Android.LowLevel;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.TestTools.Utils;

internal class AndroidTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_CanDifferentiateAndroidGamepadFromJoystick()
    {
        var gamepad = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidGameController",
            capabilities = new AndroidDeviceCapabilities
            {
                inputSources = AndroidInputSource.Gamepad | AndroidInputSource.Joystick,
            }.ToJson()
        });
        var joystick = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidGameController",
            capabilities = new AndroidDeviceCapabilities
            {
                inputSources = AndroidInputSource.Joystick
            }.ToJson()
        });

        Assert.That(gamepad, Is.TypeOf<AndroidGamepad>());
        Assert.That(joystick, Is.TypeOf<AndroidJoystick>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsAndroidGamepad()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidGameController",
            capabilities = new AndroidDeviceCapabilities
            {
                inputSources = AndroidInputSource.Gamepad | AndroidInputSource.Joystick,
            }.ToJson()
        });

        Assert.That(device, Is.TypeOf<AndroidGamepad>());
        var controller = (AndroidGamepad)device;

        var leftStick = new Vector2(0.789f, 0.987f);
        var rightStick = new Vector2(0.654f, 0.321f);
        // Note: Regarding triggers, android sends different events depending to which device the controller is connected.
        //       For ex., when NVIDIA shield controller when connected to Shield Console, triggers generate events:
        //            Left Trigger -> AndroidAxis.Brake, AndroidAxis.LtTrigger
        //            Right Trigger -> AndroidAxis.Gas, AndroidAxis.RtTrigger
        //       BUT
        //           when NVIDIA shield controller when connected to Samsung phone, triggers generate events:
        //            Left Trigger -> AndroidAxis.Brake
        //            Right Trigger -> AndroidAxis.Gas
        //
        //       This is why we're only reading and validating that events are correctly processed from AndroidAxis.Brake & AndroidAxis.Gas
        InputSystem.QueueStateEvent(controller,
            new AndroidGameControllerState()
                .WithAxis(AndroidAxis.Brake, 0.123f)
                .WithAxis(AndroidAxis.Gas, 0.456f)
                .WithAxis(AndroidAxis.X, leftStick.x)
                .WithAxis(AndroidAxis.Y, leftStick.y)
                .WithAxis(AndroidAxis.Z, rightStick.x)
                .WithAxis(AndroidAxis.Rz, rightStick.y));

        InputSystem.Update();

        var leftStickDeadzone = controller.leftStick.TryGetProcessor<StickDeadzoneProcessor>();
        var rightStickDeadzone = controller.leftStick.TryGetProcessor<StickDeadzoneProcessor>();

        // Y is upside down on Android.
        Assert.That(controller.leftStick.ReadValue(), Is.EqualTo(leftStickDeadzone.Process(new Vector2(leftStick.x, -leftStick.y))));
        Assert.That(controller.rightStick.ReadValue(), Is.EqualTo(rightStickDeadzone.Process(new Vector2(rightStick.x, -rightStick.y))));

        Assert.That(controller.leftStick.left.ReadValue(), Is.EqualTo(0.0f));
        Assert.That(controller.leftStick.right.ReadValue(), Is.EqualTo(new AxisDeadzoneProcessor().Process(leftStick.x)));
        Assert.That(controller.leftStick.up.ReadValue(), Is.EqualTo(0.0f));
        Assert.That(controller.leftStick.down.ReadValue(), Is.EqualTo(new AxisDeadzoneProcessor().Process(leftStick.y)));

        Assert.That(controller.rightStick.left.ReadValue(), Is.EqualTo(0.0f));
        Assert.That(controller.rightStick.right.ReadValue(), Is.EqualTo(new AxisDeadzoneProcessor().Process(rightStick.x)));
        Assert.That(controller.rightStick.up.ReadValue(), Is.EqualTo(0.0f));
        Assert.That(controller.rightStick.down.ReadValue(), Is.EqualTo(new AxisDeadzoneProcessor().Process(rightStick.y)));

        Assert.That(controller.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(controller.rightTrigger.ReadValue(), Is.EqualTo(0.456).Within(0.000001));

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

        // Move sticks to opposite directions
        InputSystem.QueueStateEvent(controller,
            new AndroidGameControllerState()
                .WithAxis(AndroidAxis.X, -leftStick.x)
                .WithAxis(AndroidAxis.Y, -leftStick.y)
                .WithAxis(AndroidAxis.Z, -rightStick.x)
                .WithAxis(AndroidAxis.Rz, -rightStick.y));

        InputSystem.Update();

        Assert.That(controller.leftStick.left.ReadValue(), Is.EqualTo(new AxisDeadzoneProcessor().Process(leftStick.x)));
        Assert.That(controller.leftStick.right.ReadValue(), Is.EqualTo(0.0f));
        Assert.That(controller.leftStick.up.ReadValue(), Is.EqualTo(new AxisDeadzoneProcessor().Process(leftStick.y)));
        Assert.That(controller.leftStick.down.ReadValue(), Is.EqualTo(0.0f));

        Assert.That(controller.rightStick.left.ReadValue(), Is.EqualTo(new AxisDeadzoneProcessor().Process(rightStick.x)));
        Assert.That(controller.rightStick.right.ReadValue(), Is.EqualTo(0.0f));
        Assert.That(controller.rightStick.up.ReadValue(), Is.EqualTo(new AxisDeadzoneProcessor().Process(rightStick.y)));
        Assert.That(controller.rightStick.down.ReadValue(), Is.EqualTo(0.0f));
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsAndroidGamepad_WithAxisDpad()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidGameController",
            capabilities = new AndroidDeviceCapabilities
            {
                inputSources = AndroidInputSource.Gamepad | AndroidInputSource.Joystick,
                motionAxes = new[]
                {
                    AndroidAxis.Generic1, // Noise
                    AndroidAxis.HatX,
                    AndroidAxis.Generic2, // Noise
                    AndroidAxis.HatY
                }
            }.ToJson()
        });

        // HatX is -1 (left) to 1 (right)
        // HatY is -1 (up) to 1 (down)

        InputSystem.QueueStateEvent(gamepad,
            new AndroidGameControllerState()
                .WithAxis(AndroidAxis.HatX, 1)
                .WithAxis(AndroidAxis.HatY, 1));
        InputSystem.Update();

        Assert.That(gamepad.dpad.left.isPressed, Is.False);
        Assert.That(gamepad.dpad.right.isPressed, Is.True);
        Assert.That(gamepad.dpad.up.isPressed, Is.False);
        Assert.That(gamepad.dpad.down.isPressed, Is.True);

        InputSystem.QueueStateEvent(gamepad,
            new AndroidGameControllerState()
                .WithAxis(AndroidAxis.HatX, -1)
                .WithAxis(AndroidAxis.HatY, -1));
        InputSystem.Update();

        Assert.That(gamepad.dpad.left.isPressed, Is.True);
        Assert.That(gamepad.dpad.right.isPressed, Is.False);
        Assert.That(gamepad.dpad.up.isPressed, Is.True);
        Assert.That(gamepad.dpad.down.isPressed, Is.False);

        InputSystem.QueueStateEvent(gamepad,
            new AndroidGameControllerState()
                .WithAxis(AndroidAxis.HatX, 0)
                .WithAxis(AndroidAxis.HatY, 0));
        InputSystem.Update();

        Assert.That(gamepad.dpad.left.isPressed, Is.False);
        Assert.That(gamepad.dpad.right.isPressed, Is.False);
        Assert.That(gamepad.dpad.up.isPressed, Is.False);
        Assert.That(gamepad.dpad.down.isPressed, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsAndroidGamepad_WithButtonDpad()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidGameController",
            capabilities = new AndroidDeviceCapabilities
            {
                inputSources = AndroidInputSource.Gamepad | AndroidInputSource.Joystick,
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

    [Test]
    [Category("Devices")]
    public void Devices_SupportsAndroidXboxConttroller()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidGameController",
            capabilities = new AndroidDeviceCapabilities
            {
                inputSources = AndroidInputSource.Gamepad | AndroidInputSource.Joystick,
                // http://www.linux-usb.org/usb.ids
                vendorId = 0x045e,
                productId = 0x02dd,
                motionAxes = new[]
                {
                    AndroidAxis.Rx,
                    AndroidAxis.Ry,
                    AndroidAxis.Z,
                    AndroidAxis.Rz,
                    AndroidAxis.HatX,
                    AndroidAxis.HatY
                }
            }.ToJson()
        });

        Assert.That(gamepad.name, Is.EqualTo("AndroidGamepadXboxController"));

        // Check if normalization works correctly
        InputSystem.QueueStateEvent(gamepad,
            new AndroidGameControllerState()
                .WithAxis(AndroidAxis.Z, -1)
                .WithAxis(AndroidAxis.Rz, -1));

        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.0f).Within(0.000001));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(0.0f).Within(0.000001));

        InputSystem.QueueStateEvent(gamepad,
            new AndroidGameControllerState()
                .WithAxis(AndroidAxis.Z, 1)
                .WithAxis(AndroidAxis.Rz, 1)
                .WithAxis(AndroidAxis.Rx, 0.123f)
                .WithAxis(AndroidAxis.Ry, -0.456f));


        InputSystem.Update();

        var rightStickDeadzone = gamepad.leftStick.TryGetProcessor<StickDeadzoneProcessor>();
        Assert.That(gamepad.rightStick.ReadValue(), Is.EqualTo(rightStickDeadzone.Process(new Vector2(0.123f, 0.456f))));

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(1.0f).Within(0.000001));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(1.0f).Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_DualshockTriggersHaveCorrectDefaultValues()
    {
        // Trigger on Dualshock has -1.0 value when in rest mode (not touched by user)
        // But when input system reads data from state struct, it reads 0.0, after normalization this becomes 0.5
        // This is incorrect... We need somekind of attribute which would allow us to specify default value in state struct
        // Update: using defaultState attribute now to have value as -1.0 by default
        var gamepad = (Gamepad)InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidGameController",
            capabilities = new AndroidDeviceCapabilities
            {
                inputSources = AndroidInputSource.Gamepad | AndroidInputSource.Joystick,
                // http://www.linux-usb.org/usb.ids
                vendorId = 0x054c,
                productId = 0x09cc,
                motionAxes = new[]
                {
                    AndroidAxis.Rx,
                    AndroidAxis.Ry,
                    AndroidAxis.Z,
                    AndroidAxis.Rz,
                    AndroidAxis.HatX,
                    AndroidAxis.HatY
                }
            }.ToJson()
        });

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.0f).Within(0.000001));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(0.0f).Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    [TestCase(typeof(AndroidAccelerometer))]
    [TestCase(typeof(AndroidMagneticFieldSensor))]
    [TestCase(typeof(AndroidGyroscope))]
    [TestCase(typeof(AndroidLightSensor))]
    [TestCase(typeof(AndroidPressureSensor))]
    [TestCase(typeof(AndroidProximity))]
    [TestCase(typeof(AndroidGravitySensor))]
    [TestCase(typeof(AndroidLinearAccelerationSensor))]
    [TestCase(typeof(AndroidRotationVector))]
    [TestCase(typeof(AndroidRelativeHumidity))]
    [TestCase(typeof(AndroidAmbientTemperature))]
    [TestCase(typeof(AndroidStepCounter))]
    public void Devices_CanCreateAndroidSensors(Type type)
    {
        var device = InputSystem.AddDevice(type.Name);

        Assert.That(device, Is.AssignableTo<Sensor>());
        Assert.That(device, Is.TypeOf(type));
    }

    [Test]
    [Category("Devices")]
    [TestCase("AndroidAmbientTemperature", "ambientTemperature")]
    [TestCase("AndroidLightSensor", "lightLevel")]
    [TestCase("AndroidPressureSensor", "atmosphericPressure")]
    [TestCase("AndroidProximity", "distance")]
    [TestCase("AndroidRelativeHumidity", "relativeHumidity")]
    [TestCase("AndroidAmbientTemperature", "ambientTemperature")]
    public void Devices_SupportSensorsWithAxisControl(string layoutName, string controlName)
    {
        var device = InputSystem.AddDevice(layoutName);
        var control = (AxisControl)device[controlName];

        using (StateEvent.From(device, out var stateEventPtr))
        {
            control.WriteValueIntoEvent(0.123f, stateEventPtr);

            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.Update();

            Assert.That(control.ReadValue(), Is.EqualTo(0.123f).Within(0.0000001));
        }
    }

    [Test]
    [Category("Devices")]
    [TestCase("AndroidStepCounter", "stepCounter")]
    public void Devices_SupportSensorsWithIntegerControl(string layoutName, string controlName)
    {
        var device = InputSystem.AddDevice(layoutName);
        var control = (IntegerControl)device[controlName];

        using (StateEvent.From(device, out var stateEventPtr))
        {
            control.WriteValueIntoEvent(5, stateEventPtr);

            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.Update();

            Assert.That(control.ReadValue(), Is.EqualTo(5));
        }
    }

    [Test]
    [Category("Devices")]
    [TestCase("AndroidAccelerometer", "acceleration", true, true)]
    [TestCase("AndroidMagneticFieldSensor", "magneticField", false, false)]
    [TestCase("AndroidGyroscope", "angularVelocity", false, true)]
    public void Devices_SupportSensorsWithVector3Control(string layoutName, string controlName, bool isAffectedByGravity, bool isAffectedByOrientation)
    {
        const float kSensorStandardGravity = 9.80665f;
        const float kMultiplier = -1.0f / kSensorStandardGravity;

        var device = InputSystem.AddDevice(layoutName);
        var control = (Vector3Control)device[controlName];

        using (StateEvent.From(device, out var stateEventPtr))
        {
            var value = new Vector3(0.1f, 0.2f, 0.3f);
            ////FIXME: Seems like written value doesn't through processor, for ex, AndroidCompensateDirectionProcessor, thus we need to manually apply preprocessing
            if (isAffectedByGravity)
                control.WriteValueIntoEvent(value / kMultiplier, stateEventPtr);
            else
                control.WriteValueIntoEvent(value, stateEventPtr);

            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.Update();

            runtime.screenOrientation = ScreenOrientation.LandscapeLeft;
            InputSystem.settings.compensateForScreenOrientation = false;
            Assert.That(control.ReadValue(), Is.EqualTo(value).Using(Vector3EqualityComparer.Instance));

            InputSystem.settings.compensateForScreenOrientation = true;
            Assert.That(control.ReadValue(), Is.EqualTo(isAffectedByOrientation ? new Vector3(-value.y, value.x, value.z) : value).Using(Vector3EqualityComparer.Instance));
        }
    }

    [Test]
    [Category("Devices")]
    [TestCase("AndroidRotationVector", "attitude")]
    public void Devices_SupportSensorsWithQuaternionControl(string layoutName, string controlName)
    {
        var device = InputSystem.AddDevice(layoutName);
        var control = (QuaternionControl)device[controlName];

        using (StateEvent.From(device, out var stateEventPtr))
        {
            var rotation = new Vector3(5.0f, 12.0f, 16.0f);
            var q = Quaternion.Euler(rotation);

            // The 4th value is ignored and is calculated from other three
            control.WriteValueIntoEvent(new Quaternion(q.x, q.y, q.z, 1234567.0f), stateEventPtr);

            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.QueueEvent(stateEventPtr);
            InputSystem.Update();

            runtime.screenOrientation = ScreenOrientation.LandscapeLeft;
            InputSystem.settings.compensateForScreenOrientation = false;
            Assert.That(control.ReadValue(), Is.EqualTo(q).Within(0.01));
            Assert.That(control.ReadValue().eulerAngles, Is.EqualTo(rotation).Using(Vector3EqualityComparer.Instance));

            InputSystem.settings.compensateForScreenOrientation = true;
            Assert.That(control.ReadValue().eulerAngles, Is.EqualTo(new Vector3(rotation.x, rotation.y, Mathf.Repeat(rotation.z - 90.0f, 360.0f))).Using(Vector3EqualityComparer.Instance));
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanOverrideAndroidGamepadLayouts()
    {
        const string json = @"
            {
                ""name"" : ""CustomDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""interface"" : ""Android"",
                    ""deviceClass"" : ""AndroidGameController"",
                    ""product"" : ""MyProduct""
                }
            }
        ";

        InputSystem.RegisterLayout(json);

        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidGameController",
            product = "MyProduct",
            capabilities = new AndroidDeviceCapabilities
            {
                productId = 0x12345,
                vendorId = 0x53421
            }.ToJson()
        });
        InputSystem.Update();

        Assert.That(InputSystem.devices[0].layout, Is.EqualTo("CustomDevice"));
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
