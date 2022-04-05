using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

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
        public static Vector3 acceleration => InputRuntime.s_Instance.acceleration;

        public static AccelerationEvent GetAccelerationEvent(int index)
        {
            return InputRuntime.s_Instance.GetAccelerationEvent(index);
        }

        public static int accelerationEventCount => InputRuntime.s_Instance.accelerationEventCount;

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
    }
}
