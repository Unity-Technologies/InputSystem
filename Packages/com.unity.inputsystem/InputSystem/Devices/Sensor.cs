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
    internal struct AccelerometerState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('A', 'C', 'C', 'L');

        [InputControl(processors = "CompensateDirection", noisy = true)]
        public Vector3 acceleration;

        public FourCC format
        {
            get { return kFormat; }
        }
    }

    internal struct GyroscopeState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('G', 'Y', 'R', 'O');

        [InputControl(processors = "CompensateDirection", noisy = true)]
        public Vector3 angularVelocity;

        public FourCC format
        {
            get { return kFormat; }
        }
    }

    internal struct GravityState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('G', 'R', 'V', ' ');

        [InputControl(processors = "CompensateDirection", noisy = true)]
        public Vector3 gravity;

        public FourCC format
        {
            get { return kFormat; }
        }
    }

    internal struct AttitudeState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('A', 'T', 'T', 'D');

        [InputControl(processors = "CompensateRotation", noisy = true)]
        public Quaternion attitude;

        public FourCC format
        {
            get { return kFormat; }
        }
    }

    internal struct LinearAccelerationState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('L', 'A', 'A', 'C');

        [InputControl(processors = "CompensateDirection", noisy = true)]
        public Vector3 acceleration;

        public FourCC format
        {
            get { return kFormat; }
        }
    }
}

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Base class representing any sensor kind of input device.
    /// </summary>
    /// <remarks>
    /// Sensors represent device environmental sensors, such as <see cref="Accelerometer"/>s, <see cref="Gyroscope"/>s, <see cref="GravitySensor"/>s and others.
    /// </remarks>
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

    /// <summary>
    /// Input device representing an accelerometer sensor.
    /// </summary>
    /// <remarks>
    /// An accelerometer let's you measure the acceleration of a device, and can be useful to control content by moving a device around.
    /// Note that the accelerometer will report the acceleration measured on a device both due to moving the device around, and due gravity
    /// pulling the device down. You can use <see cref="GravitySensor"/> and <see cref="LinearAccelerationSensor"/> to get decouped values
    /// for these.
    /// </remarks>
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
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            acceleration = builder.GetControl<Vector3Control>("acceleration");
            base.FinishSetup(builder);
        }
    }

    /// <summary>
    /// Input device representing a gyroscope sensor.
    /// </summary>
    /// <remarks>
    /// A gyroscope let's you measure the angular velocity of a device, and can be useful to control content by rotating a device.
    /// </remarks>
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
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            angularVelocity = builder.GetControl<Vector3Control>("angularVelocity");
            base.FinishSetup(builder);
        }
    }

    /// <summary>
    /// Input device representing a gravity sensor.
    /// </summary>
    /// <remarks>
    /// A gravity sensor let's you determine the direction of the gravity vector relative to a device, and can be useful to control content by device orientation.
    /// This is usually derived from a hardware <see cref="Accelerometer"/>, by subtracting the effect of linear acceleration (see <see cref="LinearAccelerationSensor"/>).
    /// </remarks>
    [InputControlLayout(stateType = typeof(GravityState), displayName = "Gravity")]
    public class GravitySensor : Sensor
    {
        public Vector3Control gravity { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

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
    /// <summary>
    /// Input device representing an attitude sensor.
    /// </summary>
    /// <remarks>
    /// An attitude sensor let's you determine the orientation of a device, and can be useful to control content by rotating a device.
    /// </remarks>
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
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            attitude = builder.GetControl<QuaternionControl>("attitude");
            base.FinishSetup(builder);
        }
    }

    /// <summary>
    /// Input device representing linear acceleration affecting the device playing the content.
    /// </summary>
    /// <remarks>
    /// An accelerometer let's you measure the acceleration of a device, and can be useful to control content by moving a device around.
    /// Linear acceleration is the acceleration of a device unaffected by gravity forces.
    /// This is usually derived from a hardware <see cref="Accelerometer"/>, by subtracting the effect of gravity (see <see cref="GravitySensor"/>).
    /// </remarks>
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
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            acceleration = builder.GetControl<Vector3Control>("acceleration");
            base.FinishSetup(builder);
        }
    }

    /// <summary>
    /// Input device representing the magnetic field affecting the device playing the content.
    /// </summary>
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
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            magneticField = builder.GetControl<Vector3Control>("magneticField");
            base.FinishSetup(builder);
        }
    }

    /// <summary>
    /// Input device representing the ambient light measured by the device playing the content.
    /// </summary>
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
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            lightLevel = builder.GetControl<AxisControl>("lightLevel");
            base.FinishSetup(builder);
        }
    }

    /// <summary>
    /// Input device representing the atmospheric pressure measured by the device playing the content.
    /// </summary>
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
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            atmosphericPressure = builder.GetControl<AxisControl>("atmosphericPressure");
            base.FinishSetup(builder);
        }
    }

    /// <summary>
    /// Input device representing the proximity of the device playing the content to the user.
    /// </summary>
    /// <remarks>
    /// The proximity sensor is usually used by phones to determine if the user is holding the phone to their ear or not.
    /// </remarks>
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
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            distance = builder.GetControl<AxisControl>("distance");
            base.FinishSetup(builder);
        }
    }

    /// <summary>
    /// Input device representing the ambient air humidity measured by the device playing the content.
    /// </summary>
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
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            relativeHumidity = builder.GetControl<AxisControl>("relativeHumidity");
            base.FinishSetup(builder);
        }
    }

    /// <summary>
    /// Input device representing the ambient air temperature measured by the device playing the content.
    /// </summary>
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
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            ambientTemperature = builder.GetControl<AxisControl>("ambientTemperature");
            base.FinishSetup(builder);
        }
    }

    /// <summary>
    /// Input device representing the foot steps taken by the user as measured by the device playing the content.
    /// </summary>
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
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            stepCounter = builder.GetControl<IntegerControl>("stepCounter");
            base.FinishSetup(builder);
        }
    }
}
