#if UNITY_EDITOR || UNITY_ANDROID
using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Plugins.Android.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.Android.LowLevel
{
    public enum AndroidSensorType
    {
        Accelerometer = 1,
        MagneticField = 2,
        Orientation = 3,            // Was deprecated in API 8 https://developer.android.com/reference/android/hardware/Sensor#TYPE_ORIENTATION
        Gyroscope = 4,
        Light = 5,
        Pressure = 6,
        Proximity = 8,
        Temperature = 7,            // Was deprecated in API 14 https://developer.android.com/reference/android/hardware/Sensor#TYPE_TEMPERATURE
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
        [InputControl(name = "angularVelocity", layout = "Vector3", processors = "AndroidCompensateDirection", variants = "Gyroscope")]
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

    public class AndroidCompensateDirectionProcessor : CompensateDirectionProcessor
    {
        // Taken fron platforms\android-<API>\arch-arm\usr\include\android\sensor.h
        private const float kSensorStandardGravity = 9.80665f;

        private const float kAccelerationMultiplier = -1.0f / kSensorStandardGravity;

        public override Vector3 Process(Vector3 vector, InputControl control)
        {
            return base.Process(vector * kAccelerationMultiplier, control);
        }
    }

    public class AndroidCompensateRotationProcessor : CompensateRotationProcessor
    {
        public override Quaternion Process(Quaternion value, InputControl control)
        {
            // https://developer.android.com/reference/android/hardware/SensorEvent#values
            // "...The rotation vector represents the orientation of the device as a combination of an angle and an axis, in which the device has rotated through an angle theta around an axis <x, y, z>."
            // "...The three elements of the rotation vector are < x * sin(theta / 2), y* sin(theta / 2), z* sin(theta / 2)>, such that the magnitude of the rotation vector is equal to sin(theta / 2), and the direction of the rotation vector is equal to the direction of the axis of rotation."
            // "...The three elements of the rotation vector are equal to the last three components of a unit quaternion < cos(theta / 2), x* sin(theta/ 2), y* sin(theta / 2), z* sin(theta/ 2)>."
            //
            // In other words, axis + rotation is combined into Vector3, to recover the quaternion from it, we must compute 4th component as 1 - sqrt(x*x + y*y + z*z)
            float sinRho2 = value.x * value.x + value.y * value.y + value.z * value.z;
            value.w = (sinRho2 < 1.0f) ? Mathf.Sqrt(1.0f - sinRho2) : 0.0f;

            return base.Process(value, control);
        }
    }
}

namespace UnityEngine.Experimental.Input.Plugins.Android
{
    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Accelerometer")]
    public class AndroidAccelerometer : Accelerometer
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "MagneticField")]
    public class AndroidMagneticField : Sensor
    {
        /// <summary>
        /// All values are in micro-Tesla (uT) and measure the ambient magnetic field in the X, Y and Z axis.
        /// </summary>
        public Vector3Control magneticField { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            magneticField = builder.GetControl<Vector3Control>("magneticField");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Gyroscope")]
    public class AndroidGyroscope : Gyroscope
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Light")]
    public class AndroidLight : Sensor
    {
        /// <summary>
        /// Light level in SI lux units
        /// </summary>
        public AxisControl lightLevel { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            lightLevel = builder.GetControl<AxisControl>("lightLevel");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Pressure")]
    public class AndroidPressure : Sensor
    {
        /// <summary>
        /// Atmospheric pressure in hPa (millibar)
        /// </summary>
        public AxisControl atmosphericPressure { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            atmosphericPressure = builder.GetControl<AxisControl>("atmosphericPressure");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Proximity")]
    public class AndroidProximity : Sensor
    {
        /// <summary>
        /// Proximity sensor distance measured in centimeters
        /// </summary>
        public AxisControl distance { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            distance = builder.GetControl<AxisControl>("distance");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "Gravity")]
    public class AndroidGravity : Gravity
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "LinearAcceleration")]
    public class AndroidLinearAcceleration : LinearAcceleration
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "RotationVector")]
    public class AndroidRotationVector : Attitude
    {
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "RelativeHumidity")]
    public class AndroidRelativeHumidity : Sensor
    {
        /// <summary>
        /// Relative ambient air humidity in percent
        /// </summary>
        public AxisControl relativeHumidity { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            relativeHumidity = builder.GetControl<AxisControl>("relativeHumidity");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "AmbientTemperature")]
    public class AndroidAmbientTemperature : Sensor
    {
        /// <summary>
        /// Ambient (room) temperature in degree Celsius.
        /// </summary>
        public AxisControl ambientTemperature { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            ambientTemperature = builder.GetControl<AxisControl>("ambientTemperature");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(stateType = typeof(AndroidSensorState), variants = "StepCounter")]
    public class AndroidStepCounter : Sensor
    {
        /// <summary>
        /// The number of steps taken by the user since the last reboot while activated.
        /// </summary>
        public IntegerControl stepCounter { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            stepCounter = builder.GetControl<IntegerControl>("stepCounter");
            base.FinishSetup(builder);
        }
    }
}

#endif
