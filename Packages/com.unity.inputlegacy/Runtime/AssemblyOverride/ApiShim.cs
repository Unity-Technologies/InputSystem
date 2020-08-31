using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public enum TouchPhase
    {
        Began = 0,
        Moved = 1,
        Stationary = 2,
        Ended = 3,
        Canceled = 4
    }

    public enum IMECompositionMode
    {
        Auto = 0,
        On = 1,
        Off = 2
    }

    public enum TouchType
    {
        Direct,
        Indirect,
        Stylus
    }

    public struct Touch
    {
        private int m_FingerId;
        private Vector2 m_Position;
        private Vector2 m_RawPosition;
        private Vector2 m_PositionDelta;
        private float m_TimeDelta;
        private int m_TapCount;
        private TouchPhase m_Phase;
        private TouchType m_Type;
        private float m_Pressure;
        private float m_maximumPossiblePressure;
        private float m_Radius;
        private float m_RadiusVariance;
        private float m_AltitudeAngle;
        private float m_AzimuthAngle;

        public int fingerId
        {
            get { return m_FingerId; }
            set { m_FingerId = value; }
        }

        public Vector2 position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        public Vector2 rawPosition
        {
            get { return m_RawPosition; }
            set { m_RawPosition = value; }
        }

        public Vector2 deltaPosition
        {
            get { return m_PositionDelta; }
            set { m_PositionDelta = value; }
        }

        public float deltaTime
        {
            get { return m_TimeDelta; }
            set { m_TimeDelta = value; }
        }

        public int tapCount
        {
            get { return m_TapCount; }
            set { m_TapCount = value; }
        }

        public TouchPhase phase
        {
            get { return m_Phase; }
            set { m_Phase = value; }
        }

        public float pressure
        {
            get { return m_Pressure; }
            set { m_Pressure = value; }
        }

        public float maximumPossiblePressure
        {
            get { return m_maximumPossiblePressure; }
            set { m_maximumPossiblePressure = value; }
        }

        public TouchType type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        public float altitudeAngle
        {
            get { return m_AltitudeAngle; }
            set { m_AltitudeAngle = value; }
        }

        public float azimuthAngle
        {
            get { return m_AzimuthAngle; }
            set { m_AzimuthAngle = value; }
        }

        public float radius
        {
            get { return m_Radius; }
            set { m_Radius = value; }
        }

        public float radiusVariance
        {
            get { return m_RadiusVariance; }
            set { m_RadiusVariance = value; }
        }
    }

    public enum DeviceOrientation
    {
        Unknown = 0,
        Portrait = 1,
        PortraitUpsideDown = 2,
        LandscapeLeft = 3,
        LandscapeRight = 4,
        FaceUp = 5,
        FaceDown = 6
    }

    public struct AccelerationEvent
    {
        internal float x, y, z;
        internal float m_TimeDelta;

        public Vector3 acceleration
        {
            get { return new Vector3(x, y, z); }
        }

        public float deltaTime
        {
            get { return m_TimeDelta; }
        }
    }

    public class Gyroscope
    {
        internal Gyroscope(int index)
        {
            m_GyroIndex = index;
        }

        private int m_GyroIndex;

        private static Vector3 rotationRate_Internal(int idx)
        {
            return Vector3.zero;
        }

        private static Vector3 rotationRateUnbiased_Internal(int idx)
        {
            return Vector3.zero;
        }

        private static Vector3 gravity_Internal(int idx)
        {
            return Vector3.zero;
        }

        private static Vector3 userAcceleration_Internal(int idx)
        {
            return Vector3.zero;
        }

        private static Quaternion attitude_Internal(int idx)
        {
            return Quaternion.identity;
        }

        private static bool getEnabled_Internal(int idx)
        {
            return false;
        }

        private static void setEnabled_Internal(int idx, bool enabled)
        {
        }

        private static float getUpdateInterval_Internal(int idx)
        {
            return 0.0f;
        }

        private static void setUpdateInterval_Internal(int idx, float interval)
        {
        }

        public Vector3 rotationRate
        {
            get { return rotationRate_Internal(m_GyroIndex); }
        }

        public Vector3 rotationRateUnbiased
        {
            get { return rotationRateUnbiased_Internal(m_GyroIndex); }
        }

        public Vector3 gravity
        {
            get { return gravity_Internal(m_GyroIndex); }
        }

        public Vector3 userAcceleration
        {
            get { return userAcceleration_Internal(m_GyroIndex); }
        }

        public Quaternion attitude
        {
            get { return attitude_Internal(m_GyroIndex); }
        }

        public bool enabled
        {
            get { return getEnabled_Internal(m_GyroIndex); }
            set { setEnabled_Internal(m_GyroIndex, value); }
        }

        public float updateInterval
        {
            get { return getUpdateInterval_Internal(m_GyroIndex); }
            set { setUpdateInterval_Internal(m_GyroIndex, value); }
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

        public float latitude
        {
            get { return m_Latitude; }
        }

        public float longitude
        {
            get { return m_Longitude; }
        }

        public float altitude
        {
            get { return m_Altitude; }
        }

        public float horizontalAccuracy
        {
            get { return m_HorizontalAccuracy; }
        }

        public float verticalAccuracy
        {
            get { return m_VerticalAccuracy; }
        }

        public double timestamp
        {
            get { return m_Timestamp; }
        }
    }

    public enum LocationServiceStatus
    {
        Stopped = 0,
        Initializing = 1,
        Running = 2,
        Failed = 3
    }

    public class LocationService
    {
        internal struct HeadingInfo
        {
            public float magneticHeading;
            public float trueHeading;
            public float headingAccuracy;
            public Vector3 raw;
            public double timestamp;
        }

        internal static bool IsServiceEnabledByUser()
        {
            return false;
        }

        internal static LocationServiceStatus GetLocationStatus()
        {
            return LocationServiceStatus.Stopped;
        }

        internal static LocationInfo GetLastLocation()
        {
            return new LocationInfo();
        }

        internal static void SetDesiredAccuracy(float value)
        {
        }

        internal static void SetDistanceFilter(float value)
        {
        }

        internal static void StartUpdatingLocation()
        {
        }

        internal static void StopUpdatingLocation()
        {
        }

        internal static HeadingInfo GetLastHeading()
        {
            return new HeadingInfo();
        }

        internal static bool IsHeadingUpdatesEnabled()
        {
            return false;
        }

        internal static void SetHeadingUpdatesEnabled(bool value)
        {
        }

        public bool isEnabledByUser
        {
            get { return IsServiceEnabledByUser(); }
        }

        public LocationServiceStatus status
        {
            get { return GetLocationStatus(); }
        }

        public LocationInfo lastData
        {
            get
            {
                if (status != LocationServiceStatus.Running)
                    Debug.Log(
                        "Location service updates are not enabled. Check LocationService.status before querying last location.");

                return GetLastLocation();
            }
        }

        public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters)
        {
            SetDesiredAccuracy(desiredAccuracyInMeters);
            SetDistanceFilter(updateDistanceInMeters);
            StartUpdatingLocation();
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
            StopUpdatingLocation();
        }
    }

    public class Compass
    {
        public float magneticHeading
        {
            get { return LocationService.GetLastHeading().magneticHeading; }
        }

        public float trueHeading
        {
            get { return LocationService.GetLastHeading().trueHeading; }
        }

        public float headingAccuracy
        {
            get { return LocationService.GetLastHeading().headingAccuracy; }
        }

        public Vector3 rawVector
        {
            get { return LocationService.GetLastHeading().raw; }
        }

        public double timestamp
        {
            get { return LocationService.GetLastHeading().timestamp; }
        }

        public bool enabled
        {
            get { return LocationService.IsHeadingUpdatesEnabled(); }
            set { LocationService.SetHeadingUpdatesEnabled(value); }
        }
    }

    // This is needed to internally call Physics/Physics2D using an interface if available, so we don't have a hard dependency on Physics.
    // internal class CameraRaycastHelper
    // {
    //     [FreeFunction("CameraScripting::RaycastTry")]   extern internal static GameObject RaycastTry(Camera cam, Ray ray, float distance, int layerMask);
    //     [FreeFunction("CameraScripting::RaycastTry2D")] extern internal static GameObject RaycastTry2D(Camera cam, Ray ray, float distance, int layerMask);
    // }

    public class Input
    {
        public abstract class DataProvider
        {
            public enum Request
            {
                Pressed,
                PressedThisFrame,
                ReleasedThisFrame
            };

            public abstract float GetAxis(string axisName);
            public abstract bool GetButton(string axisName, Request request);

            public abstract bool GetKey(KeyCode key, Request request);
            public abstract bool GetKey(string keyCodeName, Request request);

            public abstract bool IsAnyKey(Request request);

            public abstract string GetInputString();

            public abstract bool IsMousePresent();
            public abstract Vector3 GetMousePosition();
            public abstract Vector2 GetMouseScrollDelta();

            public abstract Touch GetTouch(int index);
            public abstract int GetTouchCount();
            public abstract bool GetTouchPressureSupported();
            public abstract bool GetStylusTouchSupported();
            public abstract bool GetTouchSupported();
            public abstract void SetMultiTouchEnabled(bool enable);
            public abstract bool GetMultiTouchEnabled();
        };

        public static DataProvider provider;

        public static float GetAxis(string axisName)
        {
            return provider?.GetAxis(axisName) ?? 0.0f;
        }

        public static float GetAxisRaw(string axisName)
        {
            return GetAxis(axisName); // TODO check if this is valid
        }

        public static bool GetButton(string buttonName)
        {
            return provider?.GetButton(buttonName, DataProvider.Request.Pressed) ?? false;
        }

        public static bool GetButtonDown(string buttonName)
        {
            return provider?.GetButton(buttonName, DataProvider.Request.PressedThisFrame) ?? false;
        }

        public static bool GetButtonUp(string buttonName)
        {
            return provider?.GetButton(buttonName, DataProvider.Request.ReleasedThisFrame) ?? false;
        }

        private static KeyCode MouseButtonToKeyCode(int buttonNumber)
        {
            switch (buttonNumber)
            {
                case 0: return KeyCode.Mouse0;
                case 1: return KeyCode.Mouse1;
                case 2: return KeyCode.Mouse2;
                case 3: return KeyCode.Mouse3;
                case 4: return KeyCode.Mouse4;
                case 5: return KeyCode.Mouse5;
                case 6: return KeyCode.Mouse6;
                default: return KeyCode.None;
            }
        }

        public static bool GetMouseButton(int button)
        {
            return provider?.GetKey(MouseButtonToKeyCode(button), DataProvider.Request.Pressed) ?? false;
        }

        public static bool GetMouseButtonDown(int button)
        {
            return provider?.GetKey(MouseButtonToKeyCode(button), DataProvider.Request.PressedThisFrame) ?? false;
        }

        public static bool GetMouseButtonUp(int button)
        {
            return provider?.GetKey(MouseButtonToKeyCode(button), DataProvider.Request.ReleasedThisFrame) ?? false;
        }

        public static void ResetInputAxes()
        {
        }
#if UNITY_STANDALONE_LINUX_API
        public static bool IsJoystickPreconfigured(string joystickName) {return false;}
#endif
        public static string[] GetJoystickNames()
        {
            return null;
        }

        public static Touch GetTouch(int index)
        {
            return provider?.GetTouch(index) ?? new Touch();
        }

        public static AccelerationEvent GetAccelerationEvent(int index)
        {
            return new AccelerationEvent();
        }

        public static bool GetKey(KeyCode key)
        {
            return provider?.GetKey(key, DataProvider.Request.Pressed) ?? false;
        }

        public static bool GetKey(string name)
        {
            return provider?.GetKey(name, DataProvider.Request.Pressed) ?? false;
        }

        public static bool GetKeyUp(KeyCode key)
        {
            return provider?.GetKey(key, DataProvider.Request.ReleasedThisFrame) ?? false;
        }

        public static bool GetKeyUp(string name)
        {
            return provider?.GetKey(name, DataProvider.Request.ReleasedThisFrame) ?? false;
        }

        public static bool GetKeyDown(KeyCode key)
        {
            return provider?.GetKey(key, DataProvider.Request.PressedThisFrame) ?? false;
        }

        public static bool GetKeyDown(string name)
        {
            return provider?.GetKey(name, DataProvider.Request.PressedThisFrame) ?? false;
        }

        [Conditional("UNITY_EDITOR")]
        internal static void SimulateTouch(int id, Vector2 position, TouchPhase action)
        {
            //     SimulateTouchInternal(id, position, action, DateTime.Now.Ticks);
        }

        // [Conditional("UNITY_EDITOR")]
        // [NativeConditional("UNITY_EDITOR")]
        // [FreeFunction("SimulateTouch")]
        // private extern static void SimulateTouchInternal(int id, Vector2 position, TouchPhase action, long timestamp);

        public static bool simulateMouseWithTouches { get; set; }
        public static bool anyKey => provider?.IsAnyKey(DataProvider.Request.Pressed) ?? false;

        public static bool anyKeyDown => provider?.IsAnyKey(DataProvider.Request.PressedThisFrame) ?? false;

        public static string inputString => provider?.GetInputString() ?? "";

        public static Vector3 mousePosition => provider?.GetMousePosition() ?? Vector3.zero;

        public static Vector2 mouseScrollDelta => provider?.GetMouseScrollDelta() ?? Vector2.zero;
        public static IMECompositionMode imeCompositionMode { get; set; }
        public static string compositionString
        {
            get { return ""; }
        }
        public static bool imeIsSelected { get; }
        public static Vector2 compositionCursorPos { get; set; }

        [Obsolete("eatKeyPressOnTextFieldFocus property is deprecated, and only provided to support legacy behavior.")]
        public static bool eatKeyPressOnTextFieldFocus { get; set; }

        public static bool mousePresent
        {
            get { return provider?.IsMousePresent() ?? false; }
        }
        public static int touchCount => provider?.GetTouchCount() ?? 0;
        public static bool touchPressureSupported => provider?.GetTouchPressureSupported() ?? false;
        public static bool stylusTouchSupported => provider?.GetStylusTouchSupported() ?? false;
        public static bool touchSupported => provider?.GetTouchSupported() ?? false;
        public static bool multiTouchEnabled
        {
            get => provider?.GetMultiTouchEnabled() ?? false;
            set => provider?.SetMultiTouchEnabled(value);
        }

        [Obsolete("isGyroAvailable property is deprecated. Please use SystemInfo.supportsGyroscope instead.")]
        public static bool isGyroAvailable { get; }

        public static DeviceOrientation deviceOrientation { get; }
        public static Vector3 acceleration { get; }
        public static bool compensateSensors { get; set; }
        public static int accelerationEventCount { get; }
        public static bool backButtonLeavesApp { get; set; }

        private static LocationService locationServiceInstance;

        public static LocationService location
        {
            get
            {
                if (locationServiceInstance == null)
                    locationServiceInstance = new LocationService();
                return locationServiceInstance;
            }
        }

        private static Compass compassInstance;

        public static Compass compass
        {
            get
            {
                if (compassInstance == null)
                    compassInstance = new Compass();
                return compassInstance;
            }
        }

        private static int GetGyroInternal()
        {
            return 0;
        }

        private static Gyroscope s_MainGyro;

        public static Gyroscope gyro
        {
            get
            {
                if (s_MainGyro == null)
                    s_MainGyro = new Gyroscope(GetGyroInternal());
                return s_MainGyro;
            }
        }

        public static Touch[] touches
        {
            get
            {
                int count = touchCount;
                Touch[] touches = new Touch[count];
                for (int q = 0; q < count; ++q)
                    touches[q] = GetTouch(q);
                return touches;
            }
        }

        public static AccelerationEvent[] accelerationEvents
        {
            get
            {
                int count = accelerationEventCount;
                AccelerationEvent[] events = new AccelerationEvent[count];
                for (int q = 0; q < count; ++q)
                    events[q] = GetAccelerationEvent(q);
                return events;
            }
        }
    }

    // required by native side when using old input manager
    internal class SendMouseEvents
    {
        [RequiredByNativeCode]
        static void SetMouseMoved()
        {
        }

        [RequiredByNativeCode]
        static void DoSendMouseEvents(int skipRTCameras)
        {
        }
    }
}