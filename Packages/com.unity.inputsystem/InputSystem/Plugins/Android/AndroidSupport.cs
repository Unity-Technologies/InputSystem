#if UNITY_EDITOR || UNITY_ANDROID
using System.Linq;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.Android.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.Android
{
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
            InputSystem.RegisterControlLayout(@"
{
    ""name"" : ""AndroidGamepadWithDpadAxes"",
    ""extend"" : ""AndroidGamepad"",
    ""controls"" : [
        { ""name"" : ""dpad"", ""offset"" : 88, ""format"" : ""VEC2"", ""sizeInBits"" : 64 },
        { ""name"" : ""dpad/right"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=0,clampMax=1"" },
        { ""name"" : ""dpad/left"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=-1,clampMax=0,invert"" },
        { ""name"" : ""dpad/down"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=0,clampMax=1"" },
        { ""name"" : ""dpad/up"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0,clampMin=-1,clampMax=0,invert"" }
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
                        if (caps.motionAxes != null)
                        {
                            if (caps.motionAxes.Contains(AndroidAxis.HatX) &&
                                caps.motionAxes.Contains(AndroidAxis.HatY))
                                return "AndroidGamepadWithDpadAxes";
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
