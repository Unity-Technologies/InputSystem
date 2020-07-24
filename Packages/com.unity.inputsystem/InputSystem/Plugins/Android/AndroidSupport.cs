#if UNITY_EDITOR || UNITY_ANDROID
using System.Linq;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Android.LowLevel;

namespace UnityEngine.InputSystem.Android
{
    /// <summary>
    /// Initializes custom android devices.
    /// You can use 'adb shell dumpsys input' from terminal to output information about all input devices.
    /// </summary>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    class AndroidSupport
    {
        internal const string kAndroidInterface = "Android";

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
            InputSystem.RegisterLayout<DualShock4GamepadAndroid>();
            InputSystem.RegisterLayout<XboxOneGamepadAndroid>();

            ////TODO: capability matching does not yet support bitmasking so these remain handled by OnFindLayoutForDevice for now

            const string kDpadHatSettings = @"
        { ""name"" : ""dpad"", ""offset"" : 88, ""format"" : ""VEC2"", ""sizeInBits"" : 64 },
        { ""name"" : ""dpad/right"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clamp=3,clampConstant=0,clampMin=0,clampMax=1"" },
        { ""name"" : ""dpad/left"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clamp=3,clampConstant=0,clampMin=-1,clampMax=0,invert"" },
        { ""name"" : ""dpad/down"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clamp=3,clampConstant=0,clampMin=0,clampMax=1"" },
        { ""name"" : ""dpad/up"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clamp=3,clampConstant=0,clampMin=-1,clampMax=0,invert"" }
";
            InputSystem.RegisterLayout(@"
{
    ""name"" : ""AndroidGamepadWithDpadAxes"",
    ""extend"" : ""AndroidGamepad"",
    ""hideInUI"" : true,
    ""controls"" : [
    " + kDpadHatSettings + @"
    ]
}
            ");
            InputSystem.RegisterLayout(@"
{
    ""name"" : ""AndroidGamepadWithDpadButtons"",
    ""extend"" : ""AndroidGamepad"",
    ""hideInUI"" : true,
    ""controls"" : [
        { ""name"" : ""dpad"", ""offset"" : 0, ""bit"" : 19, ""sizeInBits"" : 4 },
        { ""name"" : ""dpad/left"", ""bit"" : 21 },
        { ""name"" : ""dpad/right"", ""bit"" : 22 },
        { ""name"" : ""dpad/up"", ""bit"" : 19 },
        { ""name"" : ""dpad/down"", ""bit"" : 20 }
    ]
}
            ");

            InputSystem.RegisterProcessor<AndroidCompensateDirectionProcessor>();
            InputSystem.RegisterProcessor<AndroidCompensateRotationProcessor>();

            // Add sensors
            InputSystem.RegisterLayout<AndroidAccelerometer>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Accelerometer));
            InputSystem.RegisterLayout<AndroidMagneticFieldSensor>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.MagneticField));
            InputSystem.RegisterLayout<AndroidGyroscope>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Gyroscope));
            InputSystem.RegisterLayout<AndroidLightSensor>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Light));
            InputSystem.RegisterLayout<AndroidPressureSensor>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Pressure));
            InputSystem.RegisterLayout<AndroidProximity>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Proximity));
            InputSystem.RegisterLayout<AndroidGravitySensor>(
                matches: new InputDeviceMatcher()
                    .WithInterface(kAndroidInterface)
                    .WithDeviceClass("AndroidSensor")
                    .WithCapability("sensorType", AndroidSensorType.Gravity));
            InputSystem.RegisterLayout<AndroidLinearAccelerationSensor>(
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

        internal static string OnFindLayoutForDevice(ref InputDeviceDescription description,
            string matchedLayout, InputDeviceExecuteCommandDelegate executeCommandDelegate)
        {
            // If we already have a matching layout, someone registered a better match.
            // We only want to act as a fallback.
            if (!string.IsNullOrEmpty(matchedLayout) && matchedLayout != "AndroidGamepad" && matchedLayout != "AndroidJoystick")
                return null;

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

                    if (caps.motionAxes == null)
                        return "AndroidGamepadWithDpadButtons";

                    // Vendor Ids, Product Ids can be found here http://www.linux-usb.org/usb.ids
                    const int kVendorMicrosoft = 0x045e;
                    const int kVendorSony = 0x054c;

                    // Tested with controllers: PS4 DualShock; XboxOne; Nvidia Shield
                    // Tested on devices: Shield console Android 9; Galaxy s9+ Android 10
                    if (caps.motionAxes.Contains(AndroidAxis.Z) &&
                        caps.motionAxes.Contains(AndroidAxis.Rz) &&
                        caps.motionAxes.Contains(AndroidAxis.HatX) &&
                        caps.motionAxes.Contains(AndroidAxis.HatY))
                    {
                        if (caps.vendorId == kVendorMicrosoft)
                            return "XboxOneGamepadAndroid";
                        if (caps.vendorId == kVendorSony)
                            return "DualShock4GamepadAndroid";
                    }


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
