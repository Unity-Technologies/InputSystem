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

    /// <summary>
    /// Low-level input state for <see cref="LocationSensor"/>.
    /// </summary>
    internal struct LocationState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('L', 'O', 'C', ' ');

        // Order matches native LocationInfo. Do not reorder.
        [InputControl(displayName = "Timestamp", layout = "Double")]
        public double timestamp;
        [InputControl(displayName = "Latitude", layout = "Axis", noisy = true)]
        public float latitude;
        [InputControl(displayName = "Longitude", layout = "Axis", noisy = true)]
        public float longitude;
        [InputControl(displayName = "Altitude", layout = "Axis", noisy = true)]
        public float altitude;
        [InputControl(displayName = "Horizontal Accuracy", layout = "Axis", noisy = true)]
        public float horizontalAccuracy;
        [InputControl(displayName = "Vertical Accuracy", layout = "Axis", noisy = true)]
        public float verticalAccuracy;

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
        /// To support setting frequencies, it must implement <see cref="SetSamplingFrequencyCommand"/>.
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
    public class Accelerometer : Sensor
    {
        public Vector3Control acceleration { get; protected set; }

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
    /// A gyroscope lets you measure the angular velocity of a device, and can be useful to control content by rotating a device.
    /// </remarks>
    [InputControlLayout(stateType = typeof(GyroscopeState))]
    public class Gyroscope : Sensor
    {
        public Vector3Control angularVelocity { get; protected set; }

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
    public class GravitySensor : Sensor
    {
        public Vector3Control gravity { get; protected set; }

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
    public class AttitudeSensor : Sensor
    {
        public QuaternionControl attitude { get; protected set; }

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
    public class LinearAccelerationSensor : Sensor
    {
        public Vector3Control acceleration { get; protected set; }

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
        public Vector3Control magneticField { get; protected set; }

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
    public class LightSensor : Sensor
    {
        /// <summary>
        /// Light level in SI lux units.
        /// </summary>
        [InputControl(displayName = "Light Level", noisy = true)]
        public AxisControl lightLevel { get; protected set; }

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
    public class PressureSensor : Sensor
    {
        /// <summary>
        /// Atmospheric pressure in hPa (millibar).
        /// </summary>
        [InputControl(displayName = "Atmospheric Pressure", noisy = true)]
        public AxisControl atmosphericPressure { get; protected set; }

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
    public class ProximitySensor : Sensor
    {
        /// <summary>
        /// Proximity sensor distance measured in centimeters.
        /// </summary>
        [InputControl(displayName = "Distance", noisy = true)]
        public AxisControl distance { get; protected set; }

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
    public class HumiditySensor : Sensor
    {
        /// <summary>
        /// Relative ambient air humidity in percent.
        /// </summary>
        [InputControl(displayName = "Relative Humidity", noisy = true)]
        public AxisControl relativeHumidity { get; protected set; }

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
    public class AmbientTemperatureSensor : Sensor
    {
        /// <summary>
        /// Temperature in degree Celsius.
        /// </summary>
        [InputControl(displayName = "Ambient Temperature", noisy = true)]
        public AxisControl ambientTemperature { get; protected set; }

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
    public class StepCounter : Sensor
    {
        /// <summary>
        /// The number of steps taken by the user since the last reboot while activated.
        /// </summary>
        [InputControl(displayName = "Step Counter", noisy = true)]
        public IntegerControl stepCounter { get; protected set; }

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

    /// <summary>
    /// Hinge angle sensor.
    /// This sensor is usually available on foldable devices.
    /// > [!NOTE]
    /// > The step resolution for angle is device dependentent, on Android you can query the sensor resolution by querying device capabilities.
    /// </summary>
    [InputControlLayout(displayName = "Hinge Angle")]
    public class HingeAngle : Sensor
    {
        /// <summary>
        /// The angle in degrees on how much the device is unfolded.
        /// </summary>
        /// <value>0 means fully folded, 180 means fully unfolded.</value>
        public AxisControl angle { get; protected set; }

        /// <summary>
        /// The hinge angle sensor that was last added or had activity last.
        /// </summary>
        /// <value>Current hinge angle sensor or <c>null</c>.</value>
        public static HingeAngle current { get; private set; }

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
            angle = GetChildControl<AxisControl>("angle");
            base.FinishSetup();
        }
    }

    /// <summary>
    /// Input device representing a GPS location sensor.
    /// </summary>
    /// <remarks>
    /// A location sensor reports the device's geographic position (<see cref="latitude"/>,
    /// <see cref="longitude"/>, <see cref="altitude"/>).
    /// After enabling it with <see cref="InputSystem.EnableDevice"/>, the location service may take several
    /// seconds to acquire valid data, so readings are only valid once <see cref="status"/> reaches
    /// <see cref="LocationServiceStatus.Running"/>. Accessing location requires the user to have
    /// granted permission (<see cref="isEnabledByUser"/>).
    /// </remarks>
    [InputControlLayout(stateType = typeof(LocationState), displayName = "Location")]
    public class LocationSensor : Sensor
    {
        /// <summary>
        /// Latitude in degrees.
        /// </summary>
        public AxisControl latitude { get; protected set; }

        /// <summary>
        /// Longitude in degrees.
        /// </summary>
        public AxisControl longitude { get; protected set; }

        /// <summary>
        /// Altitude in meters.
        /// </summary>
        public AxisControl altitude { get; protected set; }

        /// <summary>
        /// Horizontal accuracy of the reading in meters.
        /// </summary>
        public AxisControl horizontalAccuracy { get; protected set; }

        /// <summary>
        /// Vertical accuracy of the reading in meters.
        /// </summary>
        public AxisControl verticalAccuracy { get; protected set; }

        /// <summary>
        /// Time the reading was taken, in seconds since the epoch used by the platform location service.
        /// </summary>
        public DoubleControl timestamp { get; protected set; }

        /// <summary>
        /// The location sensor that was last added or had activity last.
        /// </summary>
        /// <value>Current location sensor or <c>null</c>.</value>
        public static LocationSensor current { get; private set; }

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
            latitude = GetChildControl<AxisControl>("latitude");
            longitude = GetChildControl<AxisControl>("longitude");
            altitude = GetChildControl<AxisControl>("altitude");
            horizontalAccuracy = GetChildControl<AxisControl>("horizontalAccuracy");
            verticalAccuracy = GetChildControl<AxisControl>("verticalAccuracy");
            timestamp = GetChildControl<DoubleControl>("timestamp");
            base.FinishSetup();
        }

        /// <summary>
        /// Current status of the location service.
        /// </summary>
        /// <remarks>
        /// After the sensor is enabled the service starts asynchronously, passing through
        /// <see cref="LocationServiceStatus.Initializing"/> before it reaches
        /// <see cref="LocationServiceStatus.Running"/>. Readings are only valid while running.
        /// </remarks>
        public LocationServiceStatus status
        {
            get
            {
                var command = QueryLocationStatusCommand.Create();
                if (ExecuteCommand(ref command) >= 0)
                    return (LocationServiceStatus)command.status;
                return LocationServiceStatus.Stopped; // no native impl (editor/desktop) -> degrades
            }
        }

        /// <summary>
        /// Whether the user has granted the app permission to access the device location.
        /// </summary>
        /// <remarks>
        /// A user can grant permission while the service is not running. If permission is denied,
        /// the service won't reach <see cref="LocationServiceStatus.Running"/>.
        /// </remarks>
        public bool isEnabledByUser
        {
            get
            {
                var command = QueryLocationEnabledByUserCommand.Create();
                if (ExecuteCommand(ref command) >= 0)
                    return command.enabledByUser;
                return false;
            }
        }

        /// <summary>
        /// Sets the desired accuracy and update-distance threshold for location readings.
        /// </summary>
        /// <param name="desiredAccuracyInMeters">Desired horizontal accuracy, in meters.</param>
        /// <param name="updateDistanceInMeters">Minimum distance the device must move before a new reading is reported, in meters.</param>
        /// <remarks>
        /// By default, the sensor is configured with the values from <see cref="InputSettings.locationAccuracy"/>
        /// and <see cref="InputSettings.locationDistanceThreshold"/>. <see cref="ResetConfiguration"/> return to those defaults.
        ///
        /// If the sensor is already enabled, readings may briefly pause while they are applied.
        /// If the sensor is disabled, values apply when device is enabled.
        /// </remarks>
        public void Configure(float desiredAccuracyInMeters, float updateDistanceInMeters)
        {
            var command = ConfigureLocationCommand.Create(desiredAccuracyInMeters, updateDistanceInMeters);
            ExecuteCommand(ref command);
        }

        /// <summary>
        /// Reverts the accuracy and update-distance threshold to the <see cref="InputSettings"/> defaults.
        /// </summary>
        /// <remarks>
        /// Subject to the same application timing as <see cref="Configure"/>.
        /// </remarks>
        public void ResetConfiguration()
        {
            var settings = InputSystem.settings;
            Configure(settings.locationAccuracy, settings.locationDistanceThreshold);
        }

        /// <inheritdoc />
        protected override void OnAdded()
        {
            base.OnAdded();

            // Seed InputSettings defaults into native once on add.
            ResetConfiguration();
        }
    }
}
