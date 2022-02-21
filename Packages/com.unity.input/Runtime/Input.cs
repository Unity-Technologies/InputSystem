using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

// GOAL: Show how you can load an existing Unity project using legacy input, install com.unity.inputsystem, and play it as is while actually not using the old input system at all
// More specifically: Can play *unmodified* 2D Rogue project through input system with zero popups by simply adding the package. Likewise, removing the package simply reverts the project to the old system.

// Notable legacy InputManager behaviors:
//  - Querying an axis that does not exist throws ArgumentException
//  - Horizontal and Vertical are *movement*

// Issues to figure out:
//  - How to implement touch polling given that EnhancedTouch has overhead and is disabled by default
//  - How to do per-control wasPressed/wasReleased and what to do about the underlying broken mechanism

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
        }
    }

    #endregion

    /// <summary>
    /// Interface for accessing and managing input.
    /// </summary>
    /// <remarks>
    /// This is the central API for accessing input in Unity.
    /// </remarks>
    public static partial class Input // Partial to allow us to inject project-specific generated code into the API.
    {
        public static bool mousePresent => Mouse.current != null;

        private static InputActionAsset actions => InputSystem.InputSystem.settings.actions;
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

        public static Compass compass => null;
        public static Gyroscope gyro => null;
        public static Vector3 acceleration => default;
        public static Vector2 mousePosition => default;
        public static Vector2 mouseScrollDelta => default;
        public static int touchCount => default;
        public static Touch[] touches => new Touch[0];
        public static bool touchSupported => false;
        public static bool multiTouchEnabled => false;
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
