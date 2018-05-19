#if UNITY_EDITOR || UNITY_ANDROID
using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Plugins.Android.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;

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

        ////FIXME: Sensors to check if values matches old system
        // Accelerometer - OK
        // MagneticField - no alternative in old system
        // Orientation - seems this constant was deprecated in API 8 https://developer.android.com/reference/android/hardware/Sensor#TYPE_ORIENTATION . Remove it?
        // Gyroscope - OK
        // Light - no alternative in old system
        // Pressure - no alternative in old system
        // Proximity - no alternative in old system
        // Gravity - OK
        // LinearAcceleration - need to check
        // RotationVector - OK
        // RelativeHumidity - no alternative in old system
        // AmbientTemperature - no alternative in old system
        // StepCounter - no alternative in old system
        // GeomagneticRotationVector - no alternative in old system
        // HeartRate - no alternative in old system

        [InputControl(name = "acceleration", layout = "Vector3", processors = "AndroidCompensateDirection", variant = "Accelerometer")]
        [InputControl(name = "magneticField", layout = "Vector3", variant = "MagneticField")]
        [InputControl(name = "orientation", layout = "Vector3", variant = "Orientation")]
        [InputControl(name = "angularVelocity", layout = "Vector3", processors = "AndroidCompensateDirection", variant = "Gyroscope")]
        [InputControl(name = "lightLevel", layout = "Axis", variant = "Light")]
        [InputControl(name = "atmosphericPressure", layout = "Axis", variant = "Pressure")]
        [InputControl(name = "distance", layout = "Axis", variant = "Proximity")]
        [InputControl(name = "gravity", layout = "Vector3", variant = "Gravity")]
        [InputControl(name = "acceleration", layout = "Vector3", processors = "AndroidCompensateDirection", variant = "LinearAcceleration")]
        [InputControl(name = "attitude", layout = "Quaternion", processors = "CompensateRotation", variant = "RotationVector")]
        [InputControl(name = "relativeHumidity", layout = "Axis", variant = "RelativeHumidity")]
        [InputControl(name = "ambientTemperature", layout = "Axis", variant = "AmbientTemperature")]
        [InputControl(name = "stepCounter", layout = "Integer", variant = "StepCounter")]
        [InputControl(name = "rotation", layout = "Quaternion", variant = "GeomagneticRotationVector")]
        [InputControl(name = "rate", layout = "Integer", variant = "HeartRate")]
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

    public class AndroidCompensateDirectionProcessor : CompensateDirectionProcessor
    {
        // Taken fron platforms\android-<API>\arch-arm\usr\include\android\sensor.h
        private const float kSensorStandardGravity = 9.80665f;

        private const float kAccelerationMultiplier = -1.0f / kSensorStandardGravity;

        public new Vector3 Process(Vector3 vector, InputControl control)
        {
            return base.Process(vector * kAccelerationMultiplier, control);
        }
    }
}

namespace UnityEngine.Experimental.Input.Plugins.Android
{
    ////TODO: Setup InputControls for sensors below

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "Accelerometer")]
    public class AndroidAccelerometer : Accelerometer
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "MagneticField")]
    public class AndroidMagneticField : Sensor
    {
        public Vector3Control mangeticField { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            mangeticField = builder.GetControl<Vector3Control>("magneticField");
            base.FinishSetup(builder);
        }
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
    public class AndroidGravity : Gravity
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "LinearAcceleration")]
    public class AndroidLinearAcceleration : LinearAcceleration
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variant = "RotationVector")]
    public class AndroidRotationVector : Attitude
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
        public IntegerControl stepCounter { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            stepCounter = builder.GetControl<IntegerControl>("stepCounter");
            base.FinishSetup(builder);
        }
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
