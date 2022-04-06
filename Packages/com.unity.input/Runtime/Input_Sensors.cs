using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine
{
    public struct AccelerationEvent : IEquatable<AccelerationEvent>
    {
        internal float x, y, z;
        internal float m_TimeDelta;

        public Vector3 acceleration => new Vector3(x, y, z);
        public float deltaTime => m_TimeDelta;

        public bool Equals(AccelerationEvent other)
        {
            return Mathf.Approximately(x, other.x) && Mathf.Approximately(y, other.y) && Mathf.Approximately(z, other.z) &&
                Mathf.Approximately(deltaTime, other.deltaTime);
        }

        public override bool Equals(object obj)
        {
            return obj is AccelerationEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, z, m_TimeDelta);
        }

        public static bool operator==(AccelerationEvent left, AccelerationEvent right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(AccelerationEvent left, AccelerationEvent right)
        {
            return !left.Equals(right);
        }
    }

    public struct LocationInfo
    {
        internal double m_Timestamp;
        internal float m_Latitude;
        internal float m_Longitude;
        internal float m_Altitude;
        internal float m_HorizontalAccuracy;
        internal float m_VerticalAccuracy;

        public float latitude => m_Latitude;
        public float longitude => m_Longitude;
        public float altitude => m_Altitude;
        public float horizontalAccuracy => m_HorizontalAccuracy;
        public float verticalAccuracy => m_VerticalAccuracy;
        public double timestamp => m_Timestamp;
    }

    public enum LocationServiceStatus
    {
        Stopped = 0,
        Initializing = 1,
        Running = 2,
        Failed = 3
    }

    public class Compass
    {
        public float magneticHeading => InputRuntime.s_Instance.lastHeading.magneticHeading;
        public float trueHeading => InputRuntime.s_Instance.lastHeading.trueHeading;
        public float headingAccuracy => InputRuntime.s_Instance.lastHeading.headingAccuracy;
        public Vector3 rawVector => InputRuntime.s_Instance.lastHeading.raw;
        public double timestamp => InputRuntime.s_Instance.lastHeading.timestamp;
        public bool enabled
        {
            get => InputRuntime.s_Instance.headingUpdatesEnabled;
            set => InputRuntime.s_Instance.headingUpdatesEnabled = value;
        }

        internal struct Heading
        {
            public float magneticHeading;
            public float trueHeading;
            public float headingAccuracy;
            public Vector3 raw;
            public double timestamp;
        }
    }

    public class Gyroscope
    {
        public Vector3 rotationRate => gyroscopeSensor?.angularVelocity.ReadValue() ?? default;
        // So apparently, iOS is the only platform where rotationRateUnbiased is a thing. Other platforms seem to just
        // return rotationRate here. In the current Sensor API of InputSystem, we are not surfacing this information.
        // For now, go and just return rotationRate on all platforms.
        public Vector3 rotationRateUnbiased => rotationRate;
        public Vector3 gravity => gravitySensor?.gravity.ReadValue() ?? default;
        public Vector3 userAcceleration => accelerationSensor?.acceleration.ReadValue() ?? default;
        public Quaternion attitude => attitudeSensor?.attitude.ReadValue() ?? default;
        public bool enabled
        {
            ////REVIEW: Should this return true if *any* is enabled? Right now, failing to turn on any one single sensor seems makes it seem like the entire gyro failed to turn on.
            get => IsEnabled(gyroscopeSensor) && IsEnabled(accelerationSensor) && IsEnabled(gravitySensor) && IsEnabled(attitudeSensor);
            set
            {
                SetEnabled(gyroscopeSensor, value);
                SetEnabled(accelerationSensor, value);
                SetEnabled(gravitySensor, value);
                SetEnabled(attitudeSensor, value);
            }
        }
        public float updateInterval
        {
            ////REVIEW: Not clear which to pick here; current code goes for min frequency of any of the actual sensors.
            get => Mathf.Min(GetFrequency(gyroscopeSensor),
                Mathf.Min(GetFrequency(accelerationSensor), Mathf.Min(GetFrequency(gravitySensor), GetFrequency(attitudeSensor))));
            set
            {
                SetFrequency(gyroscopeSensor, value);
                SetFrequency(accelerationSensor, value);
                SetFrequency(gravitySensor, value);
                SetFrequency(attitudeSensor, value);
            }
        }

        // The UnityEngine.Input gyroscope is actually an aggregation
        // of multiple sensor types. So, we hold on to multiple devices
        // here.
        private InputSystem.Gyroscope m_Gyro;
        private LinearAccelerationSensor m_Accel;
        private GravitySensor m_Gravity;
        private AttitudeSensor m_Attitude;

        private InputSystem.Gyroscope gyroscopeSensor
        {
            get
            {
                if (m_Gyro == null || !m_Gyro.added)
                    m_Gyro = InputSystem.Gyroscope.current;
                return m_Gyro;
            }
        }

        private LinearAccelerationSensor accelerationSensor
        {
            get
            {
                if (m_Accel == null || !m_Accel.added)
                    m_Accel = LinearAccelerationSensor.current;
                return m_Accel;
            }
        }

        private GravitySensor gravitySensor
        {
            get
            {
                if (m_Gravity == null || !m_Gravity.added)
                    m_Gravity = GravitySensor.current;
                return m_Gravity;
            }
        }

        private AttitudeSensor attitudeSensor
        {
            get
            {
                if (m_Attitude == null || !m_Attitude.added)
                    m_Attitude = AttitudeSensor.current;
                return m_Attitude;
            }
        }

        private static bool IsEnabled(InputDevice device)
        {
            return device != null && device.enabled;
        }

        private static void SetEnabled(InputDevice device, bool enabled)
        {
            if (device == null)
                return;

            if (enabled)
                InputSystem.InputSystem.EnableDevice(device);
            else
                InputSystem.InputSystem.DisableDevice(device);
        }

        private static float GetFrequency(Sensor device)
        {
            if (device == null)
                return default;

            return device.samplingFrequency;
        }

        private static void SetFrequency(Sensor device, float frequency)
        {
            if (device == null)
                return;

            device.samplingFrequency = frequency;
        }
    }

    public class LocationService
    {
        public bool isEnabledByUser => InputRuntime.s_Instance.isLocationServiceEnabledByUser;
        public LocationServiceStatus status => InputRuntime.s_Instance.locationServiceStatus;
        public LocationInfo lastData => InputRuntime.s_Instance.lastLocation;

        public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters)
        {
            InputRuntime.s_Instance.StartUpdatingLocation(desiredAccuracyInMeters, updateDistanceInMeters);
        }

        public void Start(float desiredAccuracyInMeters)
        {
            Start(desiredAccuracyInMeters, 10f);
        }

        public void Start()
        {
            Start(10f, 10f);
        }

        public void Stop()
        {
            InputRuntime.s_Instance.StopUpdatingLocation();
        }
    }

    public static partial class Input
    {
        public static Gyroscope gyro => s_Gyro;
        public static bool isGyroAvailable => InputSystem.Gyroscope.current != null;
        public static LocationService location => s_Location;
        public static Compass compass => s_Compass;
        public static Vector3 acceleration => s_Accelerometer.acceleration;

        public static AccelerationEvent GetAccelerationEvent(int index)
        {
            if (index < 0 || index >= s_Accelerometer.eventCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return s_Accelerometer.events[index];
        }

        public static int accelerationEventCount => s_Accelerometer.eventCount;

        public static AccelerationEvent[] accelerationEvents
        {
            get
            {
                var count = accelerationEventCount;
                var result = new AccelerationEvent[count];
                for (var i = 0; i < count; ++i)
                    result[i] = GetAccelerationEvent(i);
                return result;
            }
        }

        private static AccelerometerData s_Accelerometer;

        private struct AccelerometerData
        {
            public InputAction action;
            public Vector3 acceleration;
            public double timestamp;
            public int eventCount;
            public AccelerationEvent[] events;

            public void NextFrame()
            {
                eventCount = 0;
            }

            public void Cleanup()
            {
                action?.Disable();
                action?.Dispose();

                action = default;
                acceleration = default;
                timestamp = default;
                eventCount = default;
                events = default;
            }
        }

        private static void AddAccelerometer(Accelerometer device)
        {
            ////REVIEW: How should this behave if multiple Accelerometers are present. At the moment, treats them
            ////        all the same and sources input from all of them concurrently.

            // We use an action to gather data from accelerometers.
            if (s_Accelerometer.action == null)
            {
                s_Accelerometer.action = new InputAction(
                    name: "<UnityEngine.Input.acceleration>",
                    type: InputActionType.PassThrough,
                    binding: "<Accelerometer>/acceleration");
                s_Accelerometer.action.Enable();

                s_Accelerometer.action.performed +=
                    ctx =>
                {
                    var value = ctx.ReadValue<Vector3>();
                    var time = ctx.time;
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    var delta = s_Accelerometer.timestamp == default ? 0 : time - s_Accelerometer.timestamp;

                    // Store event.
                    ArrayHelpers.AppendWithCapacity(ref s_Accelerometer.events, ref s_Accelerometer.eventCount, new AccelerationEvent
                    {
                        x = value.x,
                        y = value.y,
                        z = value.z,
                        m_TimeDelta = (float)delta
                    });

                    // Store latest acceleration.
                    s_Accelerometer.acceleration = value;
                    s_Accelerometer.timestamp = time;
                };

                ////REVIEW: Or is it better to just leave the recorded data untouched?
                s_Accelerometer.action.canceled +=
                    ctx =>
                {
                    s_Accelerometer.acceleration = default;
                    s_Accelerometer.timestamp = ctx.time;
                    ////REVIEW: record event?
                };
            }

            // If the accelerometer is not enabled, enable it now.
            if (!device.enabled)
                InputSystem.InputSystem.EnableDevice(device);
        }
    }
}
