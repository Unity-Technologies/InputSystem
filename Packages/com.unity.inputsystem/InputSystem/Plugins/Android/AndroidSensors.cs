#if UNITY_EDITOR || UNITY_ANDROID
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.Android.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.Android.LowLevel
{
    public enum AndroidSenorType
    {
        Accelerometer = 1,
        MagneticField = 2,
        Orientation = 3,
        Gyroscope = 4,
        Light = 5,
        Pressure = 6,
        Proximity = 8,
        Temperature = 7,
        Gravity = 9,
        LinearAcceleration = 10,
        RotationVector = 11,
        RelativeHumidity = 12,
        AmbientTemperature = 13,
        MagneticFieldUncalibrated = 14,
        GameRotationVector = 15,
        GyroscopeUncalibrated = 16,
        SignificantMotion = 17,
        StepDetector = 18,
        StepCounter = 19,
        GeomagneticRotationVector = 20,
        HeartRate = 21,
    }

    [Serializable]
    public struct AndroidSensorCapabilities
    {
        public AndroidSenorType sensorType;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static AndroidSensorCapabilities FromJson(string json)
        {
            if (json == null)
                throw new ArgumentNullException("json");
            return JsonUtility.FromJson<AndroidSensorCapabilities>(json);
        }

        public override string ToString()
        {
            return string.Format("type = {0}", sensorType.ToString());
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AndroidSensorState : IInputStateTypeInfo
    {
        public static FourCC kFormat = new FourCC('A', 'S', 'S', ' ');

        [InputControl(name = "acceleration", template = "Vector3", format = "VEC3", offset = 0, processors = "androidacceleration")]
        public fixed float data[16];

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public class AndroidAccelerationProcessor : IInputProcessor<Vector3>
    {
        // Taken fron platforms\android-<API>\arch-arm\usr\include\android\sensor.h
        private const float kSensorStandardGravity = 9.80665f;

        private const float kAccelerationMultiplier = -1.0f / kSensorStandardGravity;

        public Vector3 Process(Vector3 vector, InputControl control)
        {
            ////FIXME: Old Input system rotate this value depending on the orientation, do the same here
            return vector * kAccelerationMultiplier;
        }
    }
}


namespace UnityEngine.Experimental.Input.Plugins.Android
{
    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidAccelerometer : Accelerometer
    {
    }
}

#endif