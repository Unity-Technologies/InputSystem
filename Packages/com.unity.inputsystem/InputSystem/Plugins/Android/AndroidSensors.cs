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

    ////FIXME: Setup InputControls for sensors below

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidMagneticField : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidOrientation : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidGyroscope : Gyroscope
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidLight : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidPressure : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidProximity : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidTemperature : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidGravity : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidLinearAcceleration : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidRotationVector : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidRelativeHumidity : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidAmbientTemperature : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidMagneticFieldUncalibrated : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidGameRotationVector : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidGyroscopeUncalibrated : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidSignificantMotion : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidStepDetector : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidStepCounter : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidGeomagneticRotationVector : Sensor
    {
    }

    [InputTemplate(stateType = typeof(AndroidSensorState))]
    public class AndroidHeartRate : Sensor
    {
    }
}

#endif