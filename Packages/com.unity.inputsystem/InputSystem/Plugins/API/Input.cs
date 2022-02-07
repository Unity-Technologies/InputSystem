using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Bindings;
using UnityEngine.InputSystem;

// GOAL: Show how you can load an existing Unity project using legacy input, install com.unity.inputsystem, and play it as is while actually not using the old input system at all
// More specifically: Can play *unmodified* 2D Rogue project through input system with zero popups by simply adding the package. Likewise, removing the package simply reverts the project to the old system.

////TODO: rename to just Input as soon as we have Modules/InputLegacy under control

// Notable legacy InputManager behaviors:
//  - Querying an axis that does not exist throws ArgumentException
//  - Horizontal and Vertical are *movement*

// Issues to figure out:
//  - How to implement touch polling given that EnhancedTouch has overhead and is disabled by default
//  - How to do per-control wasPressed/wasReleased and what to do about the underlying broken mechanism

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

    // Controls IME input
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

        public int fingerId { get { return m_FingerId; } set { m_FingerId = value; } }
        public Vector2 position { get { return m_Position; } set { m_Position = value; }  }
        public Vector2 rawPosition { get { return m_RawPosition; } set { m_RawPosition = value; }  }
        public Vector2 deltaPosition { get { return m_PositionDelta; } set { m_PositionDelta = value; }  }
        public float deltaTime { get { return m_TimeDelta; } set { m_TimeDelta = value; }  }
        public int tapCount { get { return m_TapCount; } set { m_TapCount = value; }  }
        public TouchPhase phase { get { return m_Phase; } set { m_Phase = value; }  }
        public float pressure { get { return m_Pressure; } set { m_Pressure = value; }  }
        public float maximumPossiblePressure { get { return m_maximumPossiblePressure; } set { m_maximumPossiblePressure = value; }  }

        public TouchType type { get { return m_Type; } set { m_Type = value; }  }
        public float altitudeAngle { get { return m_AltitudeAngle; } set { m_AltitudeAngle = value; }  }
        public float azimuthAngle { get { return m_AzimuthAngle; } set { m_AzimuthAngle = value; }  }
        public float radius { get { return m_Radius; } set { m_Radius = value; }  }
        public float radiusVariance { get { return m_RadiusVariance; } set { m_RadiusVariance = value; }  }
    }

    [Flags]
    public enum PenStatus
    {
        None = 0x0,
        Contact = 0x1,
        Barrel = 0x2,
        Inverted = 0x4,
        Eraser = 0x8,
    }

    public enum PenEventType
    {
        NoContact,
        PenDown,
        PenUp
    }

    public struct PenData
    {
        public Vector2 position;
        public Vector2 tilt;
        public PenStatus penStatus;
        public float twist;
        public float pressure;
        public PenEventType contactType;
        public Vector2 deltaPos;
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

        public Vector3 acceleration => new Vector3(x, y, z);
        public float deltaTime => m_TimeDelta;
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

    #region Unimplemented

    public class Compass
    {
        public float magneticHeading => throw new NotImplementedException();
        public float trueHeading => throw new NotImplementedException();
        public float headingAccuracy => throw new NotImplementedException();
        public Vector3 rawVector => throw new NotImplementedException();
        public double timestamp => throw new NotImplementedException();
        public bool enabled
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }

    public class Gyroscope
    {
        public Vector3 rotationRate => throw new NotImplementedException();
        public Vector3 rotationRateUnbiased => throw new NotImplementedException();
        public Vector3 gravity => throw new NotImplementedException();
        public Vector3 userAcceleration => throw new NotImplementedException();
        public Quaternion attitude => throw new NotImplementedException();
        public bool enabled
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public float updateInterval
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }

    public class LocationService
    {
        public bool isEnabledByUser => throw new NotImplementedException();
        public LocationServiceStatus status => throw new NotImplementedException();
        public LocationInfo lastData => throw new NotImplementedException();

        public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }

    #endregion

    /// <summary>
    /// Interface for accessing and managing input.
    /// </summary>
    public static partial class Input
    {
        private static InputActionAsset actions => InputSystem.InputSystem.settings.actions;

        [DoesNotReturn]
        private static void AxisNotFound(string axisName)
        {
            throw new ArgumentException($"No input axis called {axisName} is defined; to set up input actions, go to: Edit -> Project Settings... -> Input", nameof(axisName));
        }

        public static float GetAxis(string axisName)
        {
            var action = axisName != null ? actions.FindAction(axisName) : null;
            if (action == null)
            {
                // We didn't find the action. See if the user is looking for "Vertical" and "Horizontal".
                // If so, we alternatively look for a "Move" actions and take its X or Y value if found.

                var isVertical = string.Equals(axisName, "Vertical", StringComparison.InvariantCultureIgnoreCase);
                var isHorizontal = !isVertical && string.Equals(axisName, "Horizontal", StringComparison.InvariantCultureIgnoreCase);

                if (isVertical || isHorizontal)
                {
                    var moveAction = actions.FindAction("Move");
                    if (moveAction != null)
                    {
                        if (isVertical)
                            return moveAction.ReadValue<Vector2>().y;
                        return moveAction.ReadValue<Vector2>().x;
                    }
                }

                AxisNotFound(axisName);
            }

            return action.ReadValue<float>();
        }

        public static bool GetButton(string buttonName)
        {
            var action = buttonName != null ? actions.FindAction(buttonName) : null;
            if (action == null)
                AxisNotFound(buttonName);

            return action.IsPressed();
        }

        public static bool GetButtonUp(string buttonName)
        {
            var action = buttonName != null ? actions.FindAction(buttonName) : null;
            if (action == null)
                AxisNotFound(buttonName);

            return action.WasReleasedThisFrame();
        }

        public static bool GetButtonDown(string buttonName)
        {
            var action = buttonName != null ? actions.FindAction(buttonName) : null;
            if (action == null)
                AxisNotFound(buttonName);

            return action.WasPressedThisFrame();
        }

        #region Unimplemented

        public static bool GetKey(string key)
        {
            throw new NotImplementedException();
        }

        public static bool GetKey(KeyCode key)
        {
            throw new NotImplementedException();
        }

        public static bool GetKeyDown(string key)
        {
            throw new NotImplementedException();
        }

        public static bool GetKeyDown(KeyCode key)
        {
            throw new NotImplementedException();
        }

        public static bool GetKeyUp(string key)
        {
            throw new NotImplementedException();
        }

        public static bool GetKeyUp(KeyCode key)
        {
            throw new NotImplementedException();
        }

        public static Compass compass => throw new NotImplementedException();
        public static Gyroscope gyro => throw new NotImplementedException();
        public static Vector3 acceleration => throw new NotImplementedException();
        public static Vector2 mousePosition => throw new NotImplementedException();
        public static int touchCount => throw new NotImplementedException();
        public static bool touchSupported => throw new NotImplementedException();
        public static bool multiTouchEnabled => throw new NotImplementedException();
        public static string compositionString => throw new NotImplementedException();
        public static string inputString => throw new NotImplementedException();
        public static bool imeIsSelected => throw new NotImplementedException();
        public static Vector2 compositionCursorPos
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public static IMECompositionMode imeCompositionMode
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        #endregion
    }
}
