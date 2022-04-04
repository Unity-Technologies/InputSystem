using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngineInternal;

// GOAL: Show how you can load an existing Unity project using legacy input, install com.unity.inputsystem, and play it as is while actually not using the old input system at all
// More specifically: Can play *unmodified* 2D Rogue project through input system with zero popups by simply adding the package. Likewise, removing the package simply reverts the project to the old system.

// Notable legacy InputManager behaviors:
//  - Querying an axis that does not exist throws ArgumentException
//  - Horizontal and Vertical are *movement*

// Issues to figure out:
//  - How to implement touch polling given that EnhancedTouch has overhead and is disabled by default
//  - How to do per-control wasPressed/wasReleased and what to do about the underlying broken mechanism

// As a 2022.2 user, I want to be able to
// - run my existing project using com.unity.inputsystem@1.4 without any noticeable behavioral regressions
// - run my existing project using legacy input without any noticeable behavioral regressions
// - create a new project (using default legacy input) and have it work as expected out of the box
// - migrate an existing project using legacy input to input v2 with reasonable close behavior
// - switch a new project from default legacy input to input v2

namespace UnityEngine
{
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

        private bool IsEnabled(InputDevice device)
        {
            return device != null && device.enabled;
        }

        private void SetEnabled(InputDevice device, bool enabled)
        {
            if (device == null)
                return;

            if (enabled)
                InputSystem.InputSystem.EnableDevice(device);
            else
                InputSystem.InputSystem.DisableDevice(device);
        }

        private float GetFrequency(Sensor device)
        {
            if (device == null)
                return default;

            return device.samplingFrequency;
        }

        private void SetFrequency(Sensor device, float frequency)
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

    /// <summary>
    /// Interface for accessing and managing input.
    /// </summary>
    /// <remarks>
    /// This is the central API for accessing input in Unity.
    /// </remarks>
    public static partial class Input // Partial to allow us to inject project-specific generated code into the API.
    {
        public static bool mousePresent => Mouse.current != null;

        public static InputActionAsset actions => InputSystem.InputSystem.settings.actions;
        private static InputActionMap s_ButtonActions;
        private static InputAction s_LeftMouseButtonAction;
        private static InputAction s_RightMouseButtonAction;
        private static InputAction s_MiddleMouseButtonAction;
        private static InputAction s_ForwardMouseButtonAction;
        private static InputAction s_BackMouseButtonAction;
        private static InputAction s_AnyKeyAction;
        private static KeySet s_PressedKeys;
        private static KeySet s_PressedKeysBefore;
        private static KeySet s_ThisFramePressedKeys;
        private static KeySet s_ThisFrameReleasedKeys;
        private static Dictionary<string, KeyCode> s_KeyCodeMapping;
        private static LocationService s_Location = new LocationService();
        private static Gyroscope s_Gyro = new Gyroscope();
        private static Compass s_Compass = new Compass();

        private struct KeySet
        {
            public int Count => m_Count;
            public Key this[int index] => (Key)m_Keys[index];

            private int m_Count;
            private byte[] m_Keys;

            public void Add(Key key)
            {
                Debug.Assert(key != Key.None, "Must not try to enter Key.None into KeySet");
                ArrayHelpers.AppendWithCapacity(ref m_Keys, ref m_Count, (byte)key);
            }

            public void Remove(Key key)
            {
                for (var i = 0; i < m_Count; ++i)
                {
                    if (m_Keys[i] == (byte)key)
                    {
                        ArrayHelpers.EraseAtByMovingTail(m_Keys, ref m_Count, i);
                        return;
                    }
                }
            }

            public bool Contains(Key key)
            {
                for (var i = 0; i < m_Count; ++i)
                    if (m_Keys[i] == (byte)key)
                        return true;
                return false;
            }

            public void Clear()
            {
                m_Count = 0;
            }
        }

        private static unsafe void SyncKeys(Keyboard keyboard)
        {
            var statePtr = (KeyboardState*)keyboard.currentStatePtr;
            MemoryHelpers.Swap(ref s_PressedKeys, ref s_PressedKeysBefore);
            s_PressedKeys.Clear();

            // Scan int by int.
            for (var i = 0; i < KeyboardState.kSizeInInts; ++i)
            {
                var n = ((int*)statePtr->keys)[i];
                if (n == 0)
                    continue;

                // Scan byte by byte.
                for (var k = 0; k < 4; ++k)
                {
                    var b = (byte)(n & 0xff);
                    n >>= 8;

                    if (b == 0)
                        continue;

                    // Scan bit by bit.
                    for (var j = 0; j < 8; ++j)
                    {
                        if ((b & (1 << j)) != 0)
                        {
                            var key = (Key)(i * 32 + k * 8 + j);
                            s_PressedKeys.Add(key);
                            if (!s_ThisFramePressedKeys.Contains(key))
                                s_ThisFramePressedKeys.Add(key);
                        }
                    }
                }
            }

            // Release keys that are no longer pressed.
            for (var i = 0; i < s_PressedKeysBefore.Count; ++i)
            {
                var key = s_PressedKeysBefore[i];
                if (!s_PressedKeys.Contains(key))
                    s_ThisFrameReleasedKeys.Add(key);
            }
        }

        private static void ReleaseKeys()
        {
            for (var i = 0; i < s_PressedKeys.Count; ++i)
            {
                var key = s_PressedKeys[i];
                if (!s_ThisFrameReleasedKeys.Contains(key))
                    s_ThisFrameReleasedKeys.Add(key);
            }

            s_PressedKeys.Clear();
        }

        private static InputAction GetActionForMouseButton(int button)
        {
            // Legacy input throws ArgumentException if button >= 7.
            if (button < 0 || button >= kMouseButtonCount)
                throw new ArgumentException("Invalid mouse button index.", nameof(button));

            // Only support 3 buttons for now.
            switch (button)
            {
                case 0:
                    return s_LeftMouseButtonAction;
                case 1:
                    return s_RightMouseButtonAction;
                case 2:
                    return s_MiddleMouseButtonAction;
                ////TODO: Old input Win32 backend maps 3=back(XBUTTON1) and 4=forward(XBUTTON2); find out what other platforms do
                case 3:
                    return s_BackMouseButtonAction;
                case 4:
                    return s_ForwardMouseButtonAction;
            }

            return null;
        }

        internal static void OnEnteringPlayMode()
        {
            // We use actions to monitor for button presses and releases.
            s_ButtonActions = new InputActionMap("ButtonActions");

            // LMB.
            s_LeftMouseButtonAction = s_ButtonActions.AddAction("Mouse0", InputActionType.Button);
            s_LeftMouseButtonAction.AddBinding("<Mouse>/leftButton");
            s_LeftMouseButtonAction.AddBinding("<Pen>/tip");

            // RMB.
            s_RightMouseButtonAction = s_ButtonActions.AddAction("Mouse1", InputActionType.Button);
            s_RightMouseButtonAction.AddBinding("<Mouse>/rightButton");
            s_RightMouseButtonAction.AddBinding("<Pen>/firstBarrelButton");

            // MMB.
            s_MiddleMouseButtonAction = s_ButtonActions.AddAction("Mouse2", InputActionType.Button);
            s_MiddleMouseButtonAction.AddBinding("<Mouse>/middleButton");
            s_MiddleMouseButtonAction.AddBinding("<Pen>/secondBarrelButton");

            // Back.
            s_BackMouseButtonAction = s_ButtonActions.AddAction("Mouse3", InputActionType.Button);
            s_BackMouseButtonAction.AddBinding("<Mouse>/backButton");

            // Forward.
            s_ForwardMouseButtonAction = s_ButtonActions.AddAction("Mouse4", InputActionType.Button);
            s_ForwardMouseButtonAction.AddBinding("<Mouse>/forwardButton");

            // Key.
            s_AnyKeyAction = s_ButtonActions.AddAction("Key", InputActionType.PassThrough);
            s_AnyKeyAction.AddBinding("<Keyboard>/anyKey");
            s_AnyKeyAction.performed += ctx =>
            {
                if (ctx.control.device is Keyboard keyboard)
                    SyncKeys(keyboard);
            };
            s_AnyKeyAction.canceled += ctx =>
            {
                ReleaseKeys();
            };

            s_ButtonActions.Enable();
            actions?.Enable();
        }

        internal static void NextFrame()
        {
            s_ThisFramePressedKeys.Clear();
            s_ThisFrameReleasedKeys.Clear();
        }

        internal static void Reset()
        {
            s_ButtonActions?.Disable();
            s_ButtonActions = default;
            s_LeftMouseButtonAction = default;
            s_RightMouseButtonAction = default;
            s_MiddleMouseButtonAction = default;
            s_ForwardMouseButtonAction = default;
            s_BackMouseButtonAction = default;
            s_AnyKeyAction = default;
            s_PressedKeys = default;
            s_PressedKeysBefore = default;
            s_ThisFramePressedKeys = default;
            s_ThisFrameReleasedKeys = default;

            // Not all of these hold state but for more predictable behavior,
            // wipe them all.
            s_Gyro = new Gyroscope();
            s_Location = new LocationService();
            s_Compass = new Compass();
        }

        // We throw the same exceptions as the current native code.
        [DoesNotReturn]
        private static void AxisNotFound(string axisName)
        {
            throw new ArgumentException($"No input axis called {axisName} is defined; to set up input actions, go to: Edit -> Project Settings... -> Input", nameof(axisName));
        }

        private static KeyCode StringToKeyCode(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            var keyCode = key.ToKeyCode();
            if (keyCode == null)
                throw new ArgumentException($"Input Key named: {key} is unknown");
            return keyCode.Value;
        }

        public static float GetAxis(string axisName)
        {
            var action = axisName != null ? actions.FindAction(axisName) : null;
            if (action == null)
            {
                // We didn't find the action. See if the user is looking for "Vertical" and "Horizontal".
                // If so, we alternatively look for a "Move" actions and take its X or Y value if found.

                var isVertical = string.Equals(axisName, "Vertical", StringComparison.OrdinalIgnoreCase);
                var isHorizontal = !isVertical && string.Equals(axisName, "Horizontal", StringComparison.OrdinalIgnoreCase);

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
                else
                {
                    var isMouseX = string.Equals(axisName, "Mouse X", StringComparison.OrdinalIgnoreCase);
                    var isMouseY = !isMouseX && string.Equals(axisName, "Mouse Y", StringComparison.OrdinalIgnoreCase);

                    if (isMouseX || isMouseY)
                    {
                        var lookAction = actions.FindAction("Look");
                        if (lookAction != null)
                        {
                            if (isMouseY)
                                return lookAction.ReadValue<Vector2>().y;
                            return lookAction.ReadValue<Vector2>().x;
                        }
                    }
                }

                AxisNotFound(axisName);
            }

            return action.ReadValue<float>();
        }

        private static InputAction GetButtonAction(string buttonName)
        {
            var action = buttonName != null ? actions.FindAction(buttonName) : null;
            if (action == null)
            {
                if (buttonName == "Fire1")
                    action = actions.FindAction("Fire");

                if (action == null)
                    AxisNotFound(buttonName);
            }

            return action;
        }

        public static bool GetButton(string buttonName)
        {
            return GetButtonAction(buttonName).IsPressed();
        }

        public static bool GetButtonUp(string buttonName)
        {
            return GetButtonAction(buttonName).WasReleasedThisFrame();
        }

        public static bool GetButtonDown(string buttonName)
        {
            return GetButtonAction(buttonName).WasPressedThisFrame();
        }

        // We actually only support 5 ATM but keeping this at 7 for compatibility with legacy implementation.
        internal const int kMouseButtonCount = 7;

        public static bool GetMouseButton(int button)
        {
            var action = GetActionForMouseButton(button);
            if (action == null)
                return false;

            return action.IsPressed();
        }

        public static bool GetMouseButtonUp(int button)
        {
            var action = GetActionForMouseButton(button);
            if (action == null)
                return false;

            return action.WasReleasedThisFrame();
        }

        public static bool GetMouseButtonDown(int button)
        {
            var action = GetActionForMouseButton(button);
            if (action == null)
                return false;

            return action.WasPressedThisFrame();
        }

        public static bool GetKey(string key)
        {
            return GetKey(StringToKeyCode(key));
        }

        public static bool GetKey(KeyCode key)
        {
            if (key.IsMouseButton())
            {
                var button = key - KeyCode.Mouse0;
                return GetMouseButton(button);
            }

            if (key.IsJoystickButton())
            {
                throw new NotImplementedException();
            }

            var keyId = key.ToKey();
            if (keyId == null)
                return false;

            return s_PressedKeys.Contains(keyId.Value);
        }

        public static bool GetKeyDown(string key)
        {
            return GetKeyDown(StringToKeyCode(key));
        }

        public static bool GetKeyDown(KeyCode key)
        {
            if (key.IsMouseButton())
            {
                var button = key - KeyCode.Mouse0;
                return GetMouseButtonDown(button);
            }

            if (key.IsJoystickButton())
            {
                throw new NotImplementedException();
            }

            var keyId = key.ToKey();
            if (keyId == null)
                return false;

            return s_ThisFramePressedKeys.Contains(keyId.Value);
        }

        public static bool GetKeyUp(string key)
        {
            return GetKeyUp(StringToKeyCode(key));
        }

        public static bool GetKeyUp(KeyCode key)
        {
            if (key.IsMouseButton())
            {
                var button = key - KeyCode.Mouse0;
                return GetMouseButtonUp(button);
            }

            if (key.IsJoystickButton())
            {
                throw new NotImplementedException();
            }

            var keyId = key.ToKey();
            if (keyId == null)
                return false;

            return s_ThisFrameReleasedKeys.Contains(keyId.Value);
        }

        public static Gyroscope gyro => s_Gyro;
        public static bool isGyroAvailable => InputSystem.Gyroscope.current != null;
        public static LocationService location => s_Location;
        public static Compass compass => s_Compass;

        #region Unimplemented

        public static float GetAxisRaw(string axisName)
        {
            ////TODO: implement properly
            return GetAxis(axisName);
        }

        public static Touch GetTouch(int index)
        {
            return default;
        }

        public static Vector3 acceleration => default;
        public static Vector2 mousePosition => default;
        public static Vector2 mouseScrollDelta => default;
        public static int touchCount => default;
        public static Touch[] touches => new Touch[0];
        public static bool touchSupported => false;
        public static bool multiTouchEnabled => false;
        public static bool simulateMouseWithTouches
        {
            get => false;
            set {}
        }
        public static string compositionString => string.Empty;
        public static string inputString => string.Empty;
        public static bool imeIsSelected => false;
        public static Vector2 compositionCursorPos
        {
            get => default;
            set {}
        }
        public static IMECompositionMode imeCompositionMode
        {
            get => m_IMECompositionMode;
            set => m_IMECompositionMode = value;
        }
        private static IMECompositionMode m_IMECompositionMode = IMECompositionMode.Off;

        #endregion
    }
}
