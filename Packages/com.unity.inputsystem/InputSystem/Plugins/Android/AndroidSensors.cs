#if UNITY_EDITOR || UNITY_ANDROID
using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Plugins.Android.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Processors;

namespace UnityEngine.InputSystem.Plugins.Android.LowLevel
{
    public enum AndroidSensorType
    {
        Accelerometer = 1,
        MagneticField = 2,
        Orientation = 3,            // Was deprecated in API 8 https://developer.android.com/reference/android/hardware/Sensor#TYPE_ORIENTATION
        Gyroscope = 4,
        Light = 5,
        Pressure = 6,
        Temperature = 7,            // Was deprecated in API 14 https://developer.android.com/reference/android/hardware/Sensor#TYPE_TEMPERATURE
        Proximity = 8,
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
        Pose6DOF = 28,
        StationaryDetect = 29,
        MotionDetect = 30,
        HeartBeat = 31,
        LowLatencyOffBodyDetect = 34,
        AccelerometerUncalibrated = 35,
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
                throw new ArgumentNullException(nameof(json));
            return JsonUtility.FromJson<AndroidSensorCapabilities>(json);
        }

        public override string ToString()
        {
            return $"type = {sensorType.ToString()}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AndroidSensorState : IInputStateTypeInfo
    {
        public static FourCC kFormat = new FourCC('A', 'S', 'S', ' ');

        ////FIXME: Sensors to check if values matches old system
        // Accelerometer - OK
        // MagneticField - no alternative in old system
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

        [InputControl(name = "acceleration", layout = "Vector3", processors = "AndroidCompensateDirection", variants = "Accelerometer")]
        [InputControl(name = "magneticField", layout = "Vector3", variants = "MagneticField")]
        // Note: Using CompensateDirection instead of AndroidCompensateDirection, because we don't need to normalize velocity
        [InputControl(name = "angularVelocity", layout = "Vector3", processors = "CompensateDirection", variants = "Gyroscope")]
        [InputControl(name = "lightLevel", layout = "Axis", variants = "Light")]
        [InputControl(name = "atmosphericPressure", layout = "Axis", variants = "Pressure")]
        [InputControl(name = "distance", layout = "Axis", variants = "Proximity")]
        [InputControl(name = "gravity", layout = "Vector3", processors = "AndroidCompensateDirection", variants = "Gravity")]
        [InputControl(name = "acceleration", layout = "Vector3", processors = "AndroidCompensateDirection", variants = "LinearAcceleration")]
        [InputControl(name = "attitude", layout = "Quaternion", processors = "AndroidCompensateRotation", variants = "RotationVector")]
        [InputControl(name = "relativeHumidity", layout = "Axis", variants = "RelativeHumidity")]
        [InputControl(name = "ambientTemperature", layout = "Axis", variants = "AmbientTemperature")]
        [InputControl(name = "stepCounter", layout = "Integer", variants = "StepCounter")]
        [InputControl(name = "rotation", layout = "Quaternion", processors = "AndroidCompensateRotation", variants = "GeomagneticRotationVector")]
        [InputControl(name = "rate", layout = "Axis", variants = "HeartRate")]
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

    internal class AndroidCompensateDirectionProcessor : CompensateDirectionProcessor
    {
        // Taken from platforms\android-<API>\arch-arm\usr\include\android\sensor.h
        private const float kSensorStandardGravity = 9.80665f;

        private const float kAccelerationMultiplier = -1.0f / kSensorStandardGravity;

        public override Vector3 Process(Vector3 vector, InputControl<Vector3> control)
        {
            return base.Process(vector * kAccelerationMultiplier, control);
        }
    }

    internal class AndroidCompensateRotationProcessor : CompensateRotationProcessor
    {
        public override Quaternion Process(Quaternion value, InputControl<Quaternion> control)
        {
            // https://developer.android.com/reference/android/hardware/SensorEvent#values
            // "...The rotation vector represents the orientation of the device as a combination of an angle and an axis, in which the device has rotated through an angle theta around an axis <x, y, z>."
            // "...The three elements of the rotation vector are < x * sin(theta / 2), y* sin(theta / 2), z* sin(theta / 2)>, such that the magnitude of the rotation vector is equal to sin(theta / 2), and the direction of the rotation vector is equal to the direction of the axis of rotation."
            // "...The three elements of the rotation vector are equal to the last three components of a unit quaternion < cos(theta / 2), x* sin(theta/ 2), y* sin(theta / 2), z* sin(theta/ 2)>."
            //
            // In other words, axis + rotation is combined into Vector3, to recover the quaternion from it, we must compute 4th component as 1 - sqrt(x*x + y*y + z*z)
            var sinRho2 = value.x * value.x + value.y * value.y + value.z * value.z;
            value.w = (sinRho2 < 1.0f) ? Mathf.Sqrt(1.0f - sinRho2) : 0.0f;

            return base.Process(value, control);
        }
    }
}

namespace UnityEngine.InputSystem.Plugins.Android
{
    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Accelerometer", hideInUI = true)]
    public class AndroidAccelerometer : Accelerometer
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "MagneticField", hideInUI = true)]
    public class AndroidMagneticFieldSensor : MagneticFieldSensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Gyroscope", hideInUI = true)]
    public class AndroidGyroscope : Gyroscope
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Light", hideInUI = true)]
    public class AndroidLightSensor : LightSensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Pressure", hideInUI = true)]
    public class AndroidPressureSensor : PressureSensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Proximity", hideInUI = true)]
    public class AndroidProximity : ProximitySensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Gravity", hideInUI = true)]
    public class AndroidGravitySensor : GravitySensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "LinearAcceleration", hideInUI = true)]
    public class AndroidLinearAccelerationSensor : LinearAccelerationSensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "RotationVector", hideInUI = true)]
    public class AndroidRotationVector : AttitudeSensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "RelativeHumidity", hideInUI = true)]
    public class AndroidRelativeHumidity : HumiditySensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "AmbientTemperature", hideInUI = true)]
    public class AndroidAmbientTemperature : AmbientTemperatureSensor
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "StepCounter", hideInUI = true)]
    public class AndroidStepCounter : StepCounter
    {
    }
}

#endif
