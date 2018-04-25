#if UNITY_EDITOR || UNITY_ANDROID
using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Plugins.Android.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.Android.LowLevel
{
    public enum AndroidSensorType
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
        public AndroidSensorType sensorType;

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

        [InputControl(name = "acceleration", layout = "Vector3", format = "VEC3", offset = 0, processors = "androidacceleration", variant = "Accelerometer")]
        public fixed float data[16];

        public AndroidSensorState WithData(params float[] data)
        {
            fixed(float* dataPtr = this.data)
            {
                for (var i = 0; i < data.Length && i < 16; i++)
                    dataPtr[i] = data[i];

                // Fill the rest with zeroes
                for (var i = data.Length; i < 16; i++)
                    dataPtr[i] = 0.0f;
            }

            return this;
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public class AndroidAccelerationProcessor : IInputControlProcessor<Vector3>
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
    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "Accelerometer")]
    public class AndroidAccelerometer : Accelerometer
    {
    }

    ////FIXME: Setup InputControls for sensors below

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "MagneticField")]
    public class AndroidMagneticField : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "Orientation")]
    public class AndroidOrientation : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "Gyroscope")]
    public class AndroidGyroscope : Gyroscope
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "Light")]
    public class AndroidLight : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "Pressure")]
    public class AndroidPressure : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "Proximity")]
    public class AndroidProximity : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "Temperature")]
    public class AndroidTemperature : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "Gravity")]
    public class AndroidGravity : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "LinearAcceleration")]
    public class AndroidLinearAcceleration : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "RotationVector")]
    public class AndroidRotationVector : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "RelativeHumidity")]
    public class AndroidRelativeHumidity : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "AmbientTemperature")]
    public class AndroidAmbientTemperature : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "MagneticFieldUncalibrated")]
    public class AndroidMagneticFieldUncalibrated : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "GameRotationVector")]
    public class AndroidGameRotationVector : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "GyroscopeUncalibrated")]
    public class AndroidGyroscopeUncalibrated : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "SignificantMotion")]
    public class AndroidSignificantMotion : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "StepDetector")]
    public class AndroidStepDetector : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "StepCounter")]
    public class AndroidStepCounter : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "GeomagneticRotationVector")]
    public class AndroidGeomagneticRotationVector : Sensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "HeartRate")]
    public class AndroidHeartRate : Sensor
    {
    }
}

#endif
