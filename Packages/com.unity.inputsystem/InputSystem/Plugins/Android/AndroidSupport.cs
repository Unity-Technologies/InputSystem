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
            InputSystem.RegisterTemplate<AndroidGamepad>(
                deviceDescription: new InputDeviceDescription
                {
                    interfaceName = "Android",
                    deviceClass = "AndroidGameController"
                });

            InputSystem.RegisterTemplate<AndroidJoystick>("AndroidJoystick");

            InputSystem.RegisterTemplate(@"
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
            InputSystem.RegisterTemplate(@"
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
            InputSystem.RegisterTemplate<AndroidAccelerometer>();
            InputSystem.RegisterTemplate<AndroidMagneticField>();
            InputSystem.RegisterTemplate<AndroidOrientation>();
            InputSystem.RegisterTemplate<AndroidGyroscope>();
            InputSystem.RegisterTemplate<AndroidLight>();
            InputSystem.RegisterTemplate<AndroidPressure>();
            InputSystem.RegisterTemplate<AndroidProximity>();
            InputSystem.RegisterTemplate<AndroidTemperature>();
            InputSystem.RegisterTemplate<AndroidGravity>();
            InputSystem.RegisterTemplate<AndroidLinearAcceleration>();
            InputSystem.RegisterTemplate<AndroidRotationVector>();
            InputSystem.RegisterTemplate<AndroidRelativeHumidity>();
            InputSystem.RegisterTemplate<AndroidAmbientTemperature>();
            InputSystem.RegisterTemplate<AndroidMagneticFieldUncalibrated>();
            InputSystem.RegisterTemplate<AndroidGameRotationVector>();
            InputSystem.RegisterTemplate<AndroidGyroscopeUncalibrated>();
            InputSystem.RegisterTemplate<AndroidSignificantMotion>();
            InputSystem.RegisterTemplate<AndroidStepDetector>();
            InputSystem.RegisterTemplate<AndroidStepCounter>();
            InputSystem.RegisterTemplate<AndroidGeomagneticRotationVector>();
            InputSystem.RegisterTemplate<AndroidHeartRate>();

            InputSystem.onFindTemplateForDevice += OnFindTemplateForDevice;
        }

        internal static string OnFindTemplateForDevice(int deviceId, ref InputDeviceDescription description,
            string matchedTemplate, IInputRuntime runtime)
        {
            if (description.interfaceName != "Android" || string.IsNullOrEmpty(description.capabilities))
                return null;

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
                    return null;
                }
                default:
                    return null;
            }
        }
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
