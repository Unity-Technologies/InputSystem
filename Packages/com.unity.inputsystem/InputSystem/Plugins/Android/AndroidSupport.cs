#if UNITY_EDITOR || UNITY_ANDROID
using System.Linq;
using UnityEngine.Experimental.Input.Layouts;
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
            InputSystem.RegisterLayout<AndroidGamepad>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidGameController"));
            InputSystem.RegisterLayout<AndroidJoystick>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidGameController"));

            ////TODO: capability matching does not yet support bitmasking so these remain handled by OnFindLayoutForDevice for now

            const string kDpadHatSettings = @"
        { ""name"" : ""dpad"", ""offset"" : 88, ""format"" : ""VEC2"", ""sizeInBits"" : 64 },
        { ""name"" : ""dpad/right"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=0,clampMax=1"" },
        { ""name"" : ""dpad/left"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=-1,clampMax=0,invert"" },
        { ""name"" : ""dpad/down"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=0,clampMax=1"" },
        { ""name"" : ""dpad/up"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=-1,clampMax=0,invert"" }
";
            InputSystem.RegisterLayout(@"
{
    ""name"" : ""AndroidGamepadWithDpadAxes"",
    ""extend"" : ""AndroidGamepad"",
    ""controls"" : [
    " + kDpadHatSettings + @"
    ]
}
            ");
            InputSystem.RegisterLayout(@"
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

            ////TODO: why do I have to set layout here for leftTrigger, shouldn't it come from child control ?
            InputSystem.RegisterLayout(string.Format(@"
{{
    ""name"" : ""AndroidGamepadXboxController"",
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
                , kDpadHatSettings
                , (uint)AndroidAxis.Z * sizeof(float) + AndroidGameControllerState.kAxisOffset
                , (uint)AndroidAxis.Rz * sizeof(float) + AndroidGameControllerState.kAxisOffset
                , (uint)AndroidAxis.Rx * sizeof(float) + AndroidGameControllerState.kAxisOffset
                , AndroidGameControllerState.kVariantGamepad));


            InputSystem.RegisterLayout(string.Format(@"
{{
    ""name"" : ""AndroidGamepadDualShock"",
    ""extend"" : ""AndroidGamepad"",
    ""controls"" : [
        {0},
        {{ ""name"" : ""leftTrigger"", ""layout"" : ""Button"", ""offset"" : {2}, ""format"" : ""FLT"", ""parameters"" : ""normalize,normalizeMin=-1,normalizeMax=1,normalizeZero=-1"", ""defaultState"" : -1, ""variant"" : ""{1}"" }},
        {{ ""name"" : ""rightTrigger"", ""layout"" : ""Button"", ""offset"" : {3}, ""format"" : ""FLT"", ""parameters"" : ""normalize,normalizeMin=-1,normalizeMax=1,normalizeZero=-1"", ""defaultState"" : -1, ""variant"" : ""{1}"" }},
        {{ ""name"" : ""leftShoulder"", ""layout"" : ""Button"", ""offset"" : 0, ""bit"" : {4}, ""variant"" : ""{1}"" }},
        {{ ""name"" : ""rightShoulder"", ""layout"" : ""Button"", ""offset"" : 0, ""bit"" : {5}, ""variant"" : ""{1}"" }},
        {{ ""name"" : ""buttonSouth"", ""layout"" : ""Button"", ""offset"" : 0, ""bit"" : {6}, ""variant"" : ""{1}"" }},
        {{ ""name"" : ""buttonWest"", ""layout"" : ""Button"", ""offset"" : 0, ""bit"" : {7}, ""variant"" : ""{1}"" }},
        {{ ""name"" : ""buttonNorth"", ""layout"" : ""Button"", ""offset"" : 0, ""bit"" : {8}, ""variant"" : ""{1}"" }},
        {{ ""name"" : ""buttonEast"", ""layout"" : ""Button"", ""offset"" : 0, ""bit"" : {9}, ""variant"" : ""{1}"" }}
    ]
}}"
                , kDpadHatSettings
                , AndroidGameControllerState.kVariantGamepad
                , (uint)AndroidAxis.Rx * sizeof(float) + AndroidGameControllerState.kAxisOffset
                , (uint)AndroidAxis.Ry * sizeof(float) + AndroidGameControllerState.kAxisOffset
                , (uint)AndroidKeyCode.ButtonY
                , (uint)AndroidKeyCode.ButtonZ
                , (uint)AndroidKeyCode.ButtonB
                , (uint)AndroidKeyCode.ButtonA
                , (uint)AndroidKeyCode.ButtonX
                , (uint)AndroidKeyCode.ButtonC));


            InputSystem.RegisterControlProcessor<AndroidCompensateDirectionProcessor>();
            InputSystem.RegisterControlProcessor<AndroidCompensateRotationProcessor>();

            // Add sensors
            InputSystem.RegisterLayout<AndroidAccelerometer>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Accelerometer));
            InputSystem.RegisterLayout<AndroidMagneticField>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.MagneticField));
            InputSystem.RegisterLayout<AndroidGyroscope>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Gyroscope));
            InputSystem.RegisterLayout<AndroidLight>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Light));
            InputSystem.RegisterLayout<AndroidPressure>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Pressure));
            InputSystem.RegisterLayout<AndroidProximity>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Proximity));
            InputSystem.RegisterLayout<AndroidGravity>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Gravity));
            InputSystem.RegisterLayout<AndroidLinearAcceleration>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.LinearAcceleration));
            InputSystem.RegisterLayout<AndroidRotationVector>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.RotationVector));
            InputSystem.RegisterLayout<AndroidRelativeHumidity>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.RelativeHumidity));
            InputSystem.RegisterLayout<AndroidAmbientTemperature>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.AmbientTemperature));
            InputSystem.RegisterLayout<AndroidStepCounter>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.StepCounter));

            InputSystem.onFindLayoutForDevice += OnFindLayoutForDevice;
        }

        internal static string OnFindLayoutForDevice(int deviceId, ref InputDeviceDescription description,
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

                    // Note: Gamepads have both AndroidInputSource.Gamepad and AndroidInputSource.Joystick in input source, while
                    //       Joysticks don't have AndroidInputSource.Gamepad in their input source
                    if ((caps.inputSources & AndroidInputSource.Gamepad) != AndroidInputSource.Gamepad)
                        return "AndroidJoystick";

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
                    //  L2 (Left trigger) -> AXIS_BRAKE(23)
                    //  R2 (Right trigger) -> AXIS_GAS(22)
                    //  X -> KEYCODE_BUTTON_X(99)
                    //  Y -> KEYCODE_BUTTON_Y(100)
                    //  B -> KEYCODE_BUTTON_B(97)
                    //  A -> KEYCODE_BUTTON_A(96)
                    //  DPAD -> AXIS_HAT_X(15),AXIS_HAT_Y(16) or KEYCODE_DPAD_LEFT(21), KEYCODE_DPAD_RIGHT(22), KEYCODE_DPAD_UP(19), KEYCODE_DPAD_DOWN(20),

                    // Note: On Nvidia Shield Console, L2/R2 additionally invoke key events for AXIS_LTRIGGER, AXIS_RTRIGGER (in addition to AXIS_BRAKE, AXIS_GAS)
                    //       If you connect gamepad to a phone for L2/R2 only AXIS_BRAKE/AXIS_GAS come. AXIS_LTRIGGER, AXIS_RTRIGGER are not invoked.
                    //       That's why we map triggers only to AXIS_BRAKE/AXIS_GAS


                    // Other exotic gamepads have different mappings
                    //  Xbox Gamepad (for ex., Microsoft X-Box One pad (Firmware 2015)) mapping (Note mapping: L2/R2/Right Stick)
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

                    //  Sony's Dualshock
                    //  Left Stick -> AXIS_X(0) / AXIS_Y(1)
                    //  Right Stick -> AXIS_Z(11) / AXIS_RZ(14)
                    //  Right Thumb -> KEYCODE_BUTTON_START(108)
                    //  Left Thumb -> KEYCODE_BUTTON_SELECT(109)
                    //  X -> KEYCODE_BUTTON_A(96),
                    //  Y -> KEYCODE_BUTTON_X(99)
                    //  B -> KEYCODE_BUTTON_C(98),
                    //  A -> KEYCODE_BUTTON_B(97)
                    //  L1 -> KEYCODE_BUTTON_Y(100)
                    //  R1 -> KEYCODE_BUTTON_Z(101)
                    //  L2 -> KEYCODE_BUTTON_L1(102), AXIS_RX(12),
                    //  R2 -> KEYCODE_BUTTON_R1(103), AXIS_RY(13),
                    //  DPAD -> AXIS_HAT_X(15),AXIS_HAT_Y(16),
                    //  Share -> KEYCODE_BUTTON_L2(104)
                    //  Options -> KEYCODE_BUTTON_R2(105),
                    //  Click on Touchpad -> KEYCODE_BUTTON_THUMBL(106)


                    if (caps.motionAxes == null)
                        return "AndroidGamepadWithDpadButtons";

                    // Vendor Ids, Product Ids can be found here http://www.linux-usb.org/usb.ids
                    const int kVendorMicrosoft = 0x045e;

                    const int kVendorSonyCorp = 0x54c;
                    const int kDualShock4CUHZCT1x = 0x05c4;
                    const int kDualShock4CUHZCT2x = 0x09cc;


                    if (caps.vendorId == kVendorMicrosoft &&
                        caps.motionAxes != null &&
                        caps.motionAxes.Contains(AndroidAxis.Rx) &&
                        caps.motionAxes.Contains(AndroidAxis.Ry) &&
                        caps.motionAxes.Contains(AndroidAxis.HatX) &&
                        caps.motionAxes.Contains(AndroidAxis.HatY))
                        return "AndroidGamepadXboxController";

                    if (caps.vendorId == kVendorSonyCorp && (caps.productId == kDualShock4CUHZCT1x || caps.productId == kDualShock4CUHZCT2x))
                        return "AndroidGamepadDualShock";

                    // Fallback to generic gamepads
                    if (caps.motionAxes.Contains(AndroidAxis.HatX) &&
                        caps.motionAxes.Contains(AndroidAxis.HatY))
                        return "AndroidGamepadWithDpadAxes";

                    return "AndroidGamepadWithDpadButtons";
                }
                default:
                    return null;
            }
        }
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
