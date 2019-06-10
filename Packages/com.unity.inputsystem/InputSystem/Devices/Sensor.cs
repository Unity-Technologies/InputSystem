using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////TODO: gyro and accelerometer (and potentially other sensors) need adjusting for screen orientation

////TODO: hook up all sensor controls to noise suppression (actually... for sensors we probably do NOT want that)

////REVIEW: Is there a better way than having all the sensor classes?

namespace UnityEngine.InputSystem.LowLevel
{
    public struct AccelerometerState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('A', 'C', 'C', 'L');

        [InputControl(processors = "CompensateDirection", noisy = true)]
        public Vector3 acceleration;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public struct GyroscopeState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('G', 'Y', 'R', 'O');

        [InputControl(processors = "CompensateDirection", noisy = true)]
        public Vector3 angularVelocity;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public struct GravityState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('G', 'R', 'V', ' ');

        [InputControl(processors = "CompensateDirection", noisy = true)]
        public Vector3 gravity;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public struct AttitudeState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('A', 'T', 'T', 'D');

        [InputControl(processors = "CompensateRotation", noisy = true)]
        public Quaternion attitude;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public struct LinearAccelerationState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('L', 'A', 'A', 'C');

        [InputControl(processors = "CompensateDirection", noisy = true)]
        public Vector3 acceleration;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace UnityEngine.InputSystem
{
    [InputControlLayout(isGenericTypeOfDevice = true)]
    public abstract class Sensor : InputDevice
    {
        public float samplingFrequency
        {
            get
            {
                var command = QuerySamplingFrequencyCommand.Create();
                if (ExecuteCommand(ref command) >= 0)
                    return command.frequency;
                throw new NotSupportedException($"Device '{this}' does not support querying sampling frequency");
            }
            set
            {
                var command = SetSamplingFrequencyCommand.Create(value);
                ExecuteCommand(ref command);
            }
        }
    }

    [InputControlLayout(stateType = typeof(AccelerometerState))]
    public class Accelerometer : Sensor
    {
        public Vector3Control acceleration { get; private set; }

        public static Accelerometer current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            acceleration = builder.GetControl<Vector3Control>("acceleration");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(stateType = typeof(GyroscopeState))]
    public class Gyroscope : Sensor
    {
        public Vector3Control angularVelocity { get; private set; }

        public static Gyroscope current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            angularVelocity = builder.GetControl<Vector3Control>("angularVelocity");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(stateType = typeof(GravityState), displayName = "Gravity")]
    public class GravitySensor : Sensor
    {
        public Vector3Control gravity { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            gravity = builder.GetControl<Vector3Control>("gravity");
            base.FinishSetup(builder);
        }

        public static GravitySensor current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }
    }

    //// REVIEW: Is this name good enough, possible other name RotationVector, here's how Android docs describe it. "A rotation vector sensor reports the orientation of the device relative to the East-North-Up coordinates frame."
    ////         This is the same as https://docs.unity3d.com/ScriptReference/Gyroscope-attitude.html
    [InputControlLayout(stateType = typeof(AttitudeState), displayName = "Attitude")]
    public class AttitudeSensor : Sensor
    {
        public QuaternionControl attitude { get; private set; }

        public static AttitudeSensor current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            attitude = builder.GetControl<QuaternionControl>("attitude");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(stateType = typeof(LinearAccelerationState), displayName = "Linear Acceleration")]
    public class LinearAccelerationSensor : Sensor
    {
        public Vector3Control acceleration { get; private set; }

        public static LinearAccelerationSensor current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            acceleration = builder.GetControl<Vector3Control>("acceleration");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(displayName = "Magnetic Field")]
    public class MagneticFieldSensor : Sensor
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <remarks>
        /// Values are in micro-Tesla (uT) and measure the ambient magnetic field in the X, Y and Z axis.
        /// </remarks>
        public Vector3Control magneticField { get; private set; }

        public static MagneticFieldSensor current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            magneticField = builder.GetControl<Vector3Control>("magneticField");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(displayName = "Light")]
    public class LightSensor : Sensor
    {
        /// <summary>
        /// Light level in SI lux units.
        /// </summary>
        public AxisControl lightLevel { get; private set; }

        public static LightSensor current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            lightLevel = builder.GetControl<AxisControl>("lightLevel");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(displayName = "Pressure")]
    public class PressureSensor : Sensor
    {
        /// <summary>
        /// Atmospheric pressure in hPa (millibar).
        /// </summary>
        public AxisControl atmosphericPressure { get; private set; }

        public static PressureSensor current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            atmosphericPressure = builder.GetControl<AxisControl>("atmosphericPressure");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(displayName = "Proximity")]
    public class ProximitySensor : Sensor
    {
        /// <summary>
        /// Proximity sensor distance measured in centimeters.
        /// </summary>
        public AxisControl distance { get; private set; }

        public static ProximitySensor current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            distance = builder.GetControl<AxisControl>("distance");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(displayName = "Humidity")]
    public class HumiditySensor : Sensor
    {
        /// <summary>
        /// Relative ambient air humidity in percent.
        /// </summary>
        public AxisControl relativeHumidity { get; private set; }

        public static HumiditySensor current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            relativeHumidity = builder.GetControl<AxisControl>("relativeHumidity");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(displayName = "Ambient Temperature")]
    public class AmbientTemperatureSensor : Sensor
    {
        /// <summary>
        /// Temperature in degree Celsius.
        /// </summary>
        public AxisControl ambientTemperature { get; private set; }

        public static AmbientTemperatureSensor current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            ambientTemperature = builder.GetControl<AxisControl>("ambientTemperature");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(displayName = "StepCounter")]
    public class StepCounter : Sensor
    {
        /// <summary>
        /// The number of steps taken by the user since the last reboot while activated.
        /// </summary>
        public IntegerControl stepCounter { get; private set; }

        public static StepCounter current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            stepCounter = builder.GetControl<IntegerControl>("stepCounter");
            base.FinishSetup(builder);
        }
    }
}
