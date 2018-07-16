#if UNITY_EDITOR || UNITY_ANDROID
using System.Linq;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.Android.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.Android
{
    /// <summary>
    /// Initializes custom android devices.
    /// You can use 'adb shell dumpsys input' from terminal to output information about all input devices.
    /// </summary>
    public static class AndroidSupport
    {
        public const string kAndroidInterface = "Android";

        public static void Initialize()
        {
            InputSystem.RegisterControlLayout<AndroidGamepad>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidGameController"));
            InputSystem.RegisterControlLayout<AndroidJoystick>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidGameController"));

            ////TODO: capability matching does not yet support bitmasking so these remain handled by OnFindControlLayoutForDevice for now

            const string kDpadSettings = @"
        { ""name"" : ""dpad"", ""offset"" : 88, ""format"" : ""VEC2"", ""sizeInBits"" : 64 },
        { ""name"" : ""dpad/right"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=0,clampMax=1"" },
        { ""name"" : ""dpad/left"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=-1,clampMax=0,invert"" },
        { ""name"" : ""dpad/down"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=0,clampMax=1"" },
        { ""name"" : ""dpad/up"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=-1,clampMax=0,invert"" }
";
            InputSystem.RegisterControlLayout(@"
{
    ""name"" : ""AndroidGamepadWithDpadAxes"",
    ""extend"" : ""AndroidGamepad"",
    ""controls"" : [
    " + kDpadSettings + @"
    ]
}
            ");
            InputSystem.RegisterControlLayout(@"
{
    ""name"" : ""AndroidGamepadWithDpadButtons"",
    ""extend"" : ""AndroidGamepad"",
    ""controls"" : [
        { ""name"" : ""dpad"", ""offset"" : 0, ""bit"" : 19, ""sizeInBits"" : 4 },
        { ""name"" : ""dpad/left"", ""bit"" : 21 },
        { ""name"" : ""dpad/right"", ""bit"" : 22 },
        { ""name"" : ""dpad/up"", ""bit"" : 19 },
        { ""name"" : ""dpad/down"", ""bit"" : 20 }
    ]
}
            ");

            // TODO: why do I have to set layout here for leftTrigger, shouldn't it come from child control ?
            InputSystem.RegisterControlLayout(string.Format(@"
{{
    ""name"" : ""AndroidGamepadXbox"",
    ""extend"" : ""AndroidGamepad"",
    ""controls"" : [
        {0},
        {{ ""name"" : ""leftTrigger"", ""layout"" : ""Button"", ""offset"" : {1}, ""format"" : ""FLT"", ""parameters"" : ""normalize=true,normalizeMin=-1,normalizeMax=1,normalizeZero=-1"", ""variant"" : ""{4}"" }},
        {{ ""name"" : ""rightTrigger"", ""layout"" : ""Button"", ""offset"" : {2}, ""format"" : ""FLT"", ""parameters"" : ""normalize=true,normalizeMin=-1,normalizeMax=1,normalizeZero=-1"", ""variant"" : ""{4}"" }},
        {{ ""name"" : ""rightStick"", ""layout"" : ""Stick"", ""offset"" : {3}, ""format"" : ""VEC2"", ""variant"" : ""{4}"" }},
        {{ ""name"" : ""rightStick/x"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""variant"" : ""{4}""  }},
        {{ ""name"" : ""rightStick/y"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""variant"" : ""{4}"" }}
    ]
}}"
                , kDpadSettings
                , (uint)AndroidAxis.Z * sizeof(float) + AndroidGameControllerState.kAxisOffset
                , (uint)AndroidAxis.Rz * sizeof(float) + AndroidGameControllerState.kAxisOffset
                , (uint)AndroidAxis.Rx * sizeof(float) + AndroidGameControllerState.kAxisOffset
                , AndroidGameControllerState.kVariantGamepad));

            InputSystem.RegisterControlProcessor<AndroidCompensateDirectionProcessor>();

            // Add sensors
            InputSystem.RegisterControlLayout<AndroidAccelerometer>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Accelerometer));
            InputSystem.RegisterControlLayout<AndroidMagneticField>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.MagneticField));
            InputSystem.RegisterControlLayout<AndroidGyroscope>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Gyroscope));
            InputSystem.RegisterControlLayout<AndroidLight>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Light));
            InputSystem.RegisterControlLayout<AndroidPressure>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Pressure));
            InputSystem.RegisterControlLayout<AndroidProximity>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Proximity));
            InputSystem.RegisterControlLayout<AndroidGravity>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Gravity));
            InputSystem.RegisterControlLayout<AndroidLinearAcceleration>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.LinearAcceleration));
            InputSystem.RegisterControlLayout<AndroidRotationVector>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.RotationVector));
            InputSystem.RegisterControlLayout<AndroidRelativeHumidity>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.RelativeHumidity));
            InputSystem.RegisterControlLayout<AndroidAmbientTemperature>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.AmbientTemperature));
            InputSystem.RegisterControlLayout<AndroidStepCounter>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.StepCounter));

            InputSystem.onFindControlLayoutForDevice += OnFindControlLayoutForDevice;
        }

        internal static string OnFindControlLayoutForDevice(int deviceId, ref InputDeviceDescription description,
            string matchedTemplate, IInputRuntime runtime)
        {
            if (description.interfaceName != "Android" || string.IsNullOrEmpty(description.capabilities))
                return null;

            ////TODO: these should just be Controller and Sensor; the interface is already Android
            switch (description.deviceClass)
            {
                case "AndroidGameController":
                {
                    var caps = AndroidDeviceCapabilities.FromJson(description.capabilities);
                    if ((caps.inputSources & AndroidInputSource.Gamepad) == AndroidInputSource.Gamepad)
                    {
                        // Most of the gamepads:
                        // - NVIDIA Controller v01.03/v01.04
                        // - ELAN PLAYSTATION(R)3 Controller
                        // - My-Power CO.,LTD. PS(R) Controller Adaptor
                        // - (Add more)
                        // map buttons in the following way:
                        //  Left Stick -> AXIS_X(0) / AXIS_Y(1)
                        //  Right Stick -> AXIS_Z (11) / AXIS_RZ(14)
                        //  Right Thumb -> KEYCODE_BUTTON_THUMBR(107)
                        //  Left Thumb -> KEYCODE_BUTTON_THUMBL(106)
                        //  L1 (Left shoulder) -> KEYCODE_BUTTON_L1(102)
                        //  R1 (Right shoulder) -> KEYCODE_BUTTON_R1(103)
                        //  L2 (Left trigger) -> AXIS_LTRIGGER(17)
                        //  R2 (Right trigger) -> AXIS_RTRIGGER(18)
                        //  X -> KEYCODE_BUTTON_X(99)
                        //  Y -> KEYCODE_BUTTON_Y(100)
                        //  B -> KEYCODE_BUTTON_B(97)
                        //  A -> KEYCODE_BUTTON_A(96)
                        //  DPAD -> AXIS_HAT_X(15),AXIS_HAT_Y(16) or KEYCODE_DPAD_LEFT(21), KEYCODE_DPAD_RIGHT(22), KEYCODE_DPAD_UP(19), KEYCODE_DPAD_DOWN(20),

                        // There's also Xbox Gamepad (for ex., Microsoft X-Box One pad (Firmware 2015)) mapping (Note mapping: L2/R2/Right Stick)
                        //  Left Stick -> AXIS_X(0) / AXIS_Y(1)
                        //  Right Stick -> AXIS_RX (12) / AXIS_RY(13)
                        //  Right Thumb -> KEYCODE_BUTTON_THUMBR(107)
                        //  Left Thumb -> KEYCODE_BUTTON_THUMBL(106)
                        //  L1 (Left shoulder) -> KEYCODE_BUTTON_L1(102)
                        //  R1 (Right shoulder) -> KEYCODE_BUTTON_R1(103)
                        //  L2 (Left trigger) -> AXIS_Z(11)
                        //  R2 (Right trigger) -> AXIS_RZ(14)
                        //  X -> KEYCODE_BUTTON_X(99)
                        //  Y -> KEYCODE_BUTTON_Y(100)
                        //  B -> KEYCODE_BUTTON_B(97)
                        //  A -> KEYCODE_BUTTON_A(96)
                        //  DPAD -> AXIS_HAT_X(15),AXIS_HAT_Y(16)

                        if (caps.motionAxes != null)
                        {
                            if (caps.motionAxes.Contains(AndroidAxis.Rx) &&
                                caps.motionAxes.Contains(AndroidAxis.Ry) &&
                                caps.motionAxes.Contains(AndroidAxis.HatX) &&
                                caps.motionAxes.Contains(AndroidAxis.HatY))
                            {
                                return "AndroidGamepadXbox";
                            }
                            else if (caps.motionAxes.Contains(AndroidAxis.HatX) &&
                                     caps.motionAxes.Contains(AndroidAxis.HatY))
                            {
                                return "AndroidGamepadWithDpadAxes";
                            }
                        }
                        return "AndroidGamepadWithDpadButtons";
                    }

                    return "AndroidJoystick";
                }
                default:
                    return null;
            }
        }
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
