#if UNITY_EDITOR || UNITY_ANDROID
using System;
using System.Linq;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.Android.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.Android
{
    public static class AndroidSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterControlLayout<AndroidGamepad>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidGameController"
            });

            InputSystem.RegisterControlLayout<AndroidJoystick>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidGameController"
            });

            InputSystem.RegisterControlLayout(@"
{
    ""name"" : ""AndroidGamepadWithDpadAxes"",
    ""extend"" : ""AndroidGamepad"",
    ""controls"" : [
        { ""name"" : ""dpad"", ""offset"" : 88, ""format"" : ""VEC2"", ""sizeInBits"" : 64 },
        { ""name"" : ""dpad/left"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampMin=0,clampMax=0.5,normalize,normalizeMin=0,normalizeMax=0.5"" },
        { ""name"" : ""dpad/right"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0.5,clampMin=0.5,clampMax=1,normalize,normalizeMin=0.5,normalizeMax=1"" },
        { ""name"" : ""dpad/up"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0.5,clampMin=0.5,clampMax=1,normalize,normalizeMin=0.5,normalizeMax=1"" },
        { ""name"" : ""dpad/down"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampMin=0,clampMax=0.5,normalize,normalizeMin=0,normalizeMax=0.5"" }
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

            InputSystem.RegisterProcessor<AndroidAccelerationProcessor>();

            // Add sensors
            InputSystem.RegisterControlLayout<AndroidAccelerometer>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidMagneticField>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidOrientation>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidGyroscope>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidLight>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidPressure>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidProximity>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidTemperature>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidGravity>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidLinearAcceleration>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidRotationVector>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidRelativeHumidity>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidAmbientTemperature>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidMagneticFieldUncalibrated>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidGameRotationVector>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidGyroscopeUncalibrated>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidSignificantMotion>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidStepDetector>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidStepCounter>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidGeomagneticRotationVector>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });
            InputSystem.RegisterControlLayout<AndroidHeartRate>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidSensor"
            });

            InputSystem.onFindControlLayoutForDevice += OnFindControlLayoutForDevice;
        }

        internal static string OnFindControlLayoutForDevice(int deviceId, ref InputDeviceDescription description,
            string matchedTemplate, IInputRuntime runtime)
        {
            if (description.interfaceName != "Android" || string.IsNullOrEmpty(description.capabilities))
                return null;

            ////TODO: these should just be Controller and Sensor; the interface is already Android
            ////TODO: we want the ability to just match on capabilities in a good way without having to do it manually in code
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
                case "AndroidSensor":
                {
                    var caps = AndroidSensorCapabilities.FromJson(description.capabilities);
                    if (Enum.IsDefined(typeof(AndroidSenorType), caps.sensorType))
                        return "Android" + caps.sensorType.ToString();

                    ////FIXME: Don't return null here, because then Input system tries to create AndroidAccelerometer for unknown sensor type
                    //
                    return "AndroidUnknownSensor";
                }
                default:
                    return null;
            }
        }
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
