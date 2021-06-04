using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////TODO: make the sensors return values through their device
////      (e.g. GravitySensor should itself be an InputControl returning a Vector3 value which is the gravity value)

////REVIEW: Is there a better way than having all the sensor classes?

namespace UnityEngine.InputSystem.LowLevel
{
    internal struct AccelerometerState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('A', 'C', 'C', 'L');

        [InputControl(displayName = "Acceleration", processors = "CompensateDirection", noisy = true)]
        public Vector3 acceleration;

        public FourCC format => kFormat;
    }

    internal struct GyroscopeState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('G', 'Y', 'R', 'O');

        [InputControl(displayName = "Angular Velocity", processors = "CompensateDirection", noisy = true)]
        public Vector3 angularVelocity;

        public FourCC format => kFormat;
    }

    internal struct GravityState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('G', 'R', 'V', ' ');

        [InputControl(displayName = "Gravity", processors = "CompensateDirection", noisy = true)]
        public Vector3 gravity;

        public FourCC format => kFormat;
    }

    internal struct AttitudeState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('A', 'T', 'T', 'D');

        [InputControl(displayName = "Attitude", processors = "CompensateRotation", noisy = true)]
        public Quaternion attitude;

        public FourCC format => kFormat;
    }

    internal struct LinearAccelerationState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('L', 'A', 'A', 'C');

        [InputControl(displayName = "Acceleration", processors = "CompensateDirection", noisy = true)]
        public Vector3 acceleration;

        public FourCC format => kFormat;
    }
}

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Base class representing any sensor kind of input device.
    /// </summary>
    /// <remarks>
    /// Sensors represent device environmental sensors, such as <see cref="Accelerometer"/>s, <see cref="Gyroscope"/>s,
    /// <see cref="GravitySensor"/>s and others.
    ///
    /// Unlike other devices, sensor devices usually start out in a disabled state in order to reduce energy
    /// consumption (i.e. preserve battery life) when the sensors are not in fact used. To enable a specific sensor,
    /// call <see cref="InputSystem.EnableDevice"/> on the device instance.
    ///
    /// <example>
    /// <code>
    /// // Enable the gyroscope.
    /// InputSystem.EnableDevice(Gyroscope.current);
    /// </code>
    /// </example>
    ///
    /// Sensors are usually sampled automatically by the platform at regular intervals. For example, if a sensor
    /// is sampled at 50Hz, the platform will queue an event with an update at a rate of roughly 50 events per
    /// second. The default sampling rate for a sensor is usually platform-specific. A custom sampling frequency
    /// can be set through <see cref="samplingFrequency"/> but be aware that there may be limitations for how fast
    /// a given sensor can be sampled.
    /// </remarks>
    [InputControlLayout(isGenericTypeOfDevice = true)]
    [Scripting.Preserve]
    public class Sensor : InputDevice
    {
        /// <summary>
        /// The frequency (in Hertz) at which the underlying sensor will be refreshed and at which update
        /// events for it will be queued.
        /// </summary>
        /// <value>Times per second at which the sensor is refreshed.</value>
        /// <remarks>
        /// Note that when setting sampling frequencies, there may be limits on the range of frequencies
        /// supported by the underlying hardware/platform.
        ///
        /// To support querying sampling frequencies, a sensor device must implement <see cref="QuerySamplingFrequencyCommand"/>.
        /// To support setting frequencies, it must implemenet <see cref="SetSamplingFrequencyCommand"/>.
        /// </remarks>
        /// <exception cref="NotSupportedException">Thrown when reading the property and the underlying
        /// sensor does not support querying of sampling frequencies.</exception>
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
                ////REVIEW: should this throw NotSupportedException, too?
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
    /// pulling the device down. You can use <see cref="GravitySensor"/> and <see cref="LinearAccelerationSensor"/> to get decoupled values
    /// for these.
    ///
    /// <example>
    /// <code>
    /// class MyBehavior : MonoBehaviour
    /// {
    ///     protected void OnEnable()
    ///     {
    ///         // All sensors start out disabled so they have to manually be enabled first.
    ///         InputSystem.EnableDevice(Accelerometer.current);
    ///     }
    ///
    ///     protected void OnDisable()
    ///     {
    ///         InputSystem.DisableDevice(Accelerometer.current);
    ///     }
    ///
    ///     protected void Update()
    ///     {
    ///         var acceleration = Accelerometer.current.acceleration.ReadValue();
    ///         //...
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    [InputControlLayout(stateType = typeof(AccelerometerState))]
    [Scripting.Preserve]
    public class Accelerometer : Sensor
    {
        public Vector3Control acceleration { get; private set; }

        /// <summary>
        /// The accelerometer that was last added or had activity last.
        /// </summary>
        /// <value>Current accelerometer or <c>null</c>.</value>
        public static Accelerometer current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            acceleration = GetChildControl<Vector3Control>("acceleration");
            base.FinishSetup();
        }
    }

    /// <summary>
    /// Input device representing a gyroscope sensor.
    /// </summary>
    /// <remarks>
    /// A gyroscope let's you measure the angular velocity of a device, and can be useful to control content by rotating a device.
    /// </remarks>
    [InputControlLayout(stateType = typeof(GyroscopeState))]
    [Scripting.Preserve]
    public class Gyroscope : Sensor
    {
        public Vector3Control angularVelocity { get; private set; }

        /// <summary>
        /// The gyroscope that was last added or had activity last.
        /// </summary>
        /// <value>Current gyroscope or <c>null</c>.</value>
        public static Gyroscope current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            angularVelocity = GetChildControl<Vector3Control>("angularVelocity");
            base.FinishSetup();
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
    [Scripting.Preserve]
    public class GravitySensor : Sensor
    {
        public Vector3Control gravity { get; private set; }

        /// <summary>
        /// The gravity sensor that was last added or had activity last.
        /// </summary>
        /// <value>Current gravity sensor or <c>null</c>.</value>
        public static GravitySensor current { get; private set; }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            gravity = GetChildControl<Vector3Control>("gravity");
            base.FinishSetup();
        }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
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
    [Scripting.Preserve]
    public class AttitudeSensor : Sensor
    {
        public QuaternionControl attitude { get; private set; }

        /// <summary>
        /// The attitude sensor that was last added or had activity last.
        /// </summary>
        /// <value>Current attitude sensor or <c>null</c>.</value>
        public static AttitudeSensor current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            attitude = GetChildControl<QuaternionControl>("attitude");
            base.FinishSetup();
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
    [Scripting.Preserve]
    public class LinearAccelerationSensor : Sensor
    {
        public Vector3Control acceleration { get; private set; }

        /// <summary>
        /// The linear acceleration sensor that was last added or had activity last.
        /// </summary>
        /// <value>Current linear acceleration sensor or <c>null</c>.</value>
        public static LinearAccelerationSensor current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            acceleration = GetChildControl<Vector3Control>("acceleration");
            base.FinishSetup();
        }
    }

    /// <summary>
    /// Input device representing the magnetic field affecting the device playing the content.
    /// </summary>
    [InputControlLayout(displayName = "Magnetic Field")]
    [Scripting.Preserve]
    public class MagneticFieldSensor : Sensor
    {
        /// <summary>
        /// Strength of the magnetic field reported by the sensor.
        /// </summary>
        /// <value>Control representing the strength of the magnetic field.</value>
        /// <remarks>
        /// Values are in micro-Tesla (uT) and measure the ambient magnetic field in the X, Y and Z axis.
        /// </remarks>
        [InputControl(displayName = "Magnetic Field", noisy = true)]
        public Vector3Control magneticField { get; private set; }

        /// <summary>
        /// The linear acceleration sensor that was last added or had activity last.
        /// </summary>
        /// <value>Current linear acceleration sensor or <c>null</c>.</value>
        public static MagneticFieldSensor current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            magneticField = GetChildControl<Vector3Control>("magneticField");
            base.FinishSetup();
        }
    }

    /// <summary>
    /// Input device representing the ambient light measured by the device playing the content.
    /// </summary>
    [InputControlLayout(displayName = "Light")]
    [Scripting.Preserve]
    public class LightSensor : Sensor
    {
        /// <summary>
        /// Light level in SI lux units.
        /// </summary>
        [InputControl(displayName = "Light Level", noisy = true)]
        public AxisControl lightLevel { get; private set; }

        /// <summary>
        /// The light sensor that was last added or had activity last.
        /// </summary>
        /// <value>Current light sensor or <c>null</c>.</value>
        public static LightSensor current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            lightLevel = GetChildControl<AxisControl>("lightLevel");
            base.FinishSetup();
        }
    }

    /// <summary>
    /// Input device representing the atmospheric pressure measured by the device playing the content.
    /// </summary>
    [InputControlLayout(displayName = "Pressure")]
    [Scripting.Preserve]
    public class PressureSensor : Sensor
    {
        /// <summary>
        /// Atmospheric pressure in hPa (millibar).
        /// </summary>
        [InputControl(displayName = "Atmospheric Pressure", noisy = true)]
        public AxisControl atmosphericPressure { get; private set; }

        /// <summary>
        /// The pressure sensor that was last added or had activity last.
        /// </summary>
        /// <value>Current pressure sensor or <c>null</c>.</value>
        public static PressureSensor current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            atmosphericPressure = GetChildControl<AxisControl>("atmosphericPressure");
            base.FinishSetup();
        }
    }

    /// <summary>
    /// Input device representing the proximity of the device playing the content to the user.
    /// </summary>
    /// <remarks>
    /// The proximity sensor is usually used by phones to determine if the user is holding the phone to their ear or not.
    /// </remarks>
    [InputControlLayout(displayName = "Proximity")]
    [Scripting.Preserve]
    public class ProximitySensor : Sensor
    {
        /// <summary>
        /// Proximity sensor distance measured in centimeters.
        /// </summary>
        [InputControl(displayName = "Distance", noisy = true)]
        public AxisControl distance { get; private set; }

        /// <summary>
        /// The proximity sensor that was last added or had activity last.
        /// </summary>
        /// <value>Current proximity sensor or <c>null</c>.</value>
        public static ProximitySensor current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            distance = GetChildControl<AxisControl>("distance");
            base.FinishSetup();
        }
    }

    /// <summary>
    /// Input device representing the ambient air humidity measured by the device playing the content.
    /// </summary>
    [InputControlLayout(displayName = "Humidity")]
    [Scripting.Preserve]
    public class HumiditySensor : Sensor
    {
        /// <summary>
        /// Relative ambient air humidity in percent.
        /// </summary>
        [InputControl(displayName = "Relative Humidity", noisy = true)]
        public AxisControl relativeHumidity { get; private set; }

        /// <summary>
        /// The humidity sensor that was last added or had activity last.
        /// </summary>
        /// <value>Current humidity sensor or <c>null</c>.</value>
        public static HumiditySensor current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            relativeHumidity = GetChildControl<AxisControl>("relativeHumidity");
            base.FinishSetup();
        }
    }

    /// <summary>
    /// Input device representing the ambient air temperature measured by the device playing the content.
    /// </summary>
    [InputControlLayout(displayName = "Ambient Temperature")]
    [Scripting.Preserve]
    public class AmbientTemperatureSensor : Sensor
    {
        /// <summary>
        /// Temperature in degree Celsius.
        /// </summary>
        [InputControl(displayName = "Ambient Temperature", noisy = true)]
        public AxisControl ambientTemperature { get; private set; }

        /// <summary>
        /// The ambient temperature sensor that was last added or had activity last.
        /// </summary>
        /// <value>Current ambient temperature sensor or <c>null</c>.</value>
        public static AmbientTemperatureSensor current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            ambientTemperature = GetChildControl<AxisControl>("ambientTemperature");
            base.FinishSetup();
        }
    }

    /// <summary>
    /// Input device representing the foot steps taken by the user as measured by the device playing the content.
    /// </summary>
    /// <remarks>
    /// On iOS, access to the step counter must be enabled via <see cref="InputSettings.iOSSettings.motionUsage"/>.
    /// </remarks>
    [InputControlLayout(displayName = "Step Counter")]
    [Scripting.Preserve]
    public class StepCounter : Sensor
    {
        /// <summary>
        /// The number of steps taken by the user since the last reboot while activated.
        /// </summary>
        [InputControl(displayName = "Step Counter", noisy = true)]
        public IntegerControl stepCounter { get; private set; }

        /// <summary>
        /// The step counter that was last added or had activity last.
        /// </summary>
        /// <value>Current step counter or <c>null</c>.</value>
        public static StepCounter current { get; private set; }

        /// <inheritdoc />
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc />
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            stepCounter = GetChildControl<IntegerControl>("stepCounter");
            base.FinishSetup();
        }
    }
}
