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
        public static Vector2 mousePosition => Pointer.current?.position.ReadValue() ?? default;

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
        private static int s_JoystickCount;
        private static Joystick[] s_Joysticks;
        private static Action<InputDevice, InputDeviceChange> s_OnDeviceChangeDelegate;

        private struct Joystick
        {
            public int index;
            public string name;
            public InputDevice device;
            public InputActionMap map;

            public bool connected => name != string.Empty;

            public void Connect(int index, InputDevice device)
            {
                name = device.displayName;
                this.device = device;
                this.index = index;

                // Tag the device such that bindings can bind to it by index.
                // NOTE: Plus 1 here because joystick #0 is not an actual device but an aggregated
                //       view of all joysticks.
                InputSystem.InputSystem.AddDeviceUsage(device, "Joystick" + (index + 1));

                if (map == null)
                    map = CreateJoystickButtonMap();

                map.devices = new[] { device };
                map.Enable();
            }

            public void Disconnect()
            {
                name = string.Empty;
                map.Disable();

                // Remove tag.
                InputSystem.InputSystem.RemoveDeviceUsage(device, "Joystick" + (index + 1));

                // We keep the device around in case we reconnect.
            }

            public InputAction Button(int index)
            {
                return map.actions[index];
            }

            private static InputActionMap CreateJoystickButtonMap()
            {
                var map = new InputActionMap();
                for (var i = 0; i < kMaxButtonsPerJoystickAsPerKeyCodeEnum; ++i)
                {
                    var code = KeyCode.JoystickButton0 + i;
                    var action = map.AddAction("JoystickButton" + i, type: InputActionType.Button);
                    foreach (var path in code.JoystickButtonToBindingPath())
                        action.AddBinding(path);
                }
                return map;
            }
        }

        // We actually only support 5 ATM but keeping this at 7 for compatibility with legacy implementation.
        internal const int kMouseButtonCount = 7;

        // We support arbitrary many joysticks but the KeyCode enum has a fixed upper limit on the number of joysticks.
        internal const int kMaxJoysticksAsPerKeyCodeEnum = 17;

        // Same for buttons. We don't limit them but the KeyCode enum has a fixed set per joystick.
        internal const int kMaxButtonsPerJoystickAsPerKeyCodeEnum = 20;

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

            InputNative.redirect = new Impl();

            if (s_OnDeviceChangeDelegate == null)
                s_OnDeviceChangeDelegate = OnDeviceChange;
            InputSystem.InputSystem.onDeviceChange += s_OnDeviceChangeDelegate;
            foreach (var device in InputSystem.InputSystem.devices)
                OnDeviceChange(device, InputDeviceChange.Added);
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                case InputDeviceChange.Reconnected:
                {
                    var isJoystick = device is Joystick;
                    var isGamepad = !isJoystick && device is Gamepad;

                    if (isJoystick || isGamepad)
                    {
                        // This branch demonstrates why GetJoystickNames() and the approach of talking
                        // to joysticks by their index is broken. If the joystick at index #1 is unplugged
                        // and we just blindly remove it, joystick #2 suddenly becomes joystick #1. So if
                        // we were trying to pick up the input from two local players concurrently, player #1
                        // now suddenly gets the device from player #2 when in fact player #1 simply lost their
                        // device.
                        //
                        // So, a better approach is to not remove entries but rather null them out. And when
                        // a device is plugged in, to see if it came back

                        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
                        {
                            // Try to find the device in our s_Joysticks list.
                            var index = GetJoystickIndex(device);
                            if (index == -1)
                            {
                                // Not found. See if there is an empty slot in the joystick list.
                                for (index = 0; index < s_JoystickCount; ++index)
                                {
                                    if (!s_Joysticks[index].connected)
                                        break;
                                }
                            }

                            if (index < 0 || index >= s_JoystickCount)
                            {
                                // No available entry. Append new joystick to end of array.
                                index = ArrayHelpers.AppendWithCapacity(ref s_Joysticks, ref s_JoystickCount, default);
                            }

                            // Switch slot to joystick. Maybe a new connect, a reconnect, or a switch
                            // to a different joystick/gamepad.
                            s_Joysticks[index].Connect(index, device);
                        }
                        else
                        {
                            var index = GetJoystickIndex(device);
                            if (index != -1)
                            {
                                // Mark joystick as disconnected.
                                s_Joysticks[index].Disconnect();
                            }
                        }
                    }
                    break;
                }
            }
        }

        private static int GetJoystickIndex(InputDevice device)
        {
            for (var i = 0; i < s_JoystickCount; ++i)
                if (s_Joysticks[i].device == device)
                    return i;
            return -1;
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
            s_Joysticks = default;
            s_JoystickCount = default;

            // Not all of these hold state but for more predictable behavior,
            // wipe them all.
            s_Gyro = new Gyroscope();
            s_Location = new LocationService();
            s_Compass = new Compass();

            InputSystem.InputSystem.onDeviceChange -= s_OnDeviceChangeDelegate;
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
                var joystickIndex = key.GetJoystickIndex();
                var buttonIndex = key.GetJoystickButtonIndex();

                if (joystickIndex == 0)
                {
                    for (var i = 0; i < s_JoystickCount; ++i)
                        if (s_Joysticks[i].Button(buttonIndex).IsPressed())
                            return true;
                    return false;
                }

                if (joystickIndex - 1 >= s_JoystickCount)
                    return false;

                return s_Joysticks[joystickIndex - 1].Button(buttonIndex).IsPressed();
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
                var joystickIndex = key.GetJoystickIndex();
                var buttonIndex = key.GetJoystickButtonIndex();

                if (joystickIndex == 0)
                {
                    var anyWasPressed = false;
                    var noneWasAlreadyPressed = true;

                    for (var i = 0; i < s_JoystickCount; ++i)
                    {
                        var button = s_Joysticks[i].Button(buttonIndex);
                        var wasPressedThisFrame = button.WasPressedThisFrame();
                        anyWasPressed |= wasPressedThisFrame;
                        if (!wasPressedThisFrame)
                            noneWasAlreadyPressed &= !button.IsPressed();
                    }

                    return anyWasPressed && noneWasAlreadyPressed;
                }

                if (joystickIndex - 1 >= s_JoystickCount)
                    return false;

                return s_Joysticks[joystickIndex - 1].Button(buttonIndex).WasPressedThisFrame();
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
                var joystickIndex = key.GetJoystickIndex();
                var buttonIndex = key.GetJoystickButtonIndex();

                if (joystickIndex == 0)
                {
                    var anyWasReleased = false;
                    var noneIsPressed = true;

                    for (var i = 0; i < s_JoystickCount; ++i)
                    {
                        var button = s_Joysticks[i].Button(buttonIndex);
                        var wasReleasedThisFrame = button.WasReleasedThisFrame();
                        anyWasReleased |= wasReleasedThisFrame;
                        if (!wasReleasedThisFrame)
                            noneIsPressed &= !button.IsPressed();
                    }

                    return anyWasReleased && noneIsPressed;
                }

                if (joystickIndex - 1 >= s_JoystickCount)
                    return false;

                return s_Joysticks[joystickIndex - 1].Button(buttonIndex).WasReleasedThisFrame();
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

        #region Unimplemented

        public static float GetAxisRaw(string axisName)
        {
            ////TODO: implement properly
            return GetAxis(axisName);
        }

        private static void ResetInputAxes()
        {
        }

        public static Touch GetTouch(int index)
        {
            return default;
        }

        private static void SimulateTouch(Touch touch)
        {
        }

        public static string[] GetJoystickNames()
        {
            var result = new string[s_JoystickCount];
            for (var i = 0; i < s_JoystickCount; ++i)
                result[i] = s_Joysticks[i].name;
            return result;
        }

        /*
        public static int GetJoystickCount()
        {
            return m_JoystickNameCount;
        }

        public static string GetJoystickName(int index)
        {
            //...
        }

        public static InputDevice GetJoystick(int index)
        {
            //...
        }
        */

        public static bool IsJoystickPreconfigured(string joystickName)
        {
            return false;
        }

        public static PenData GetPenEvent(int index)
        {
            return default;
        }

        public static void ResetPenEvents()
        {
        }

        private static PenData GetLastPenContactEvent()
        {
            return default;
        }

        public static void ClearLastPenContactEvent()
        {
        }

        public static Vector2 mouseScrollDelta => default;
        public static int touchCount => default;
        public static Touch[] touches => new Touch[0];
        public static bool touchSupported => false;
        public static bool multiTouchEnabled { get; set; }
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
        private static bool gyroSupported { get; }
        private static int mainGyro { get; }
        public static DeviceOrientation deviceOrientation { get; }
        public static bool compensateSensors { get; set; }
        public static bool stylusTouchSupported { get; }
        public static bool touchPressureSupported { get; }
        public static int penEventCount { get; }
        public static bool anyKey { get; }
        public static bool anyKeyDown { get; }
        public static bool eatKeyPressOnTextFieldFocus { get; set; }
        public static bool backButtonLeavesApp { get; set; }

        #endregion

        // ReSharper disable MemberHidesStaticFromOuterClass
        // This is the redirect that we install in Modules/Input in order for us to get
        // all calls that the Unity runtime itself makes into UnityEngine.Input.
        // Note that right now, many of the methods are not actually called from native.
        private class Impl : IInputNative
        {
            public float GetAxis(string axisName)
            {
                return Input.GetAxis(axisName);
            }

            public float GetAxisRaw(string axisName)
            {
                return Input.GetAxisRaw(axisName);
            }

            public bool GetButton(string buttonName)
            {
                return Input.GetButton(buttonName);
            }

            public bool GetButtonDown(string buttonName)
            {
                return Input.GetButtonDown(buttonName);
            }

            public bool GetButtonUp(string buttonName)
            {
                return Input.GetButtonUp(buttonName);
            }

            public void ResetInputAxes()
            {
                Input.ResetInputAxes();
            }

            public bool GetKey(KeyCode key)
            {
                return Input.GetKey(key);
            }

            public bool GetKey(string name)
            {
                return Input.GetKey(name);
            }

            public bool GetKeyUp(KeyCode key)
            {
                return Input.GetKeyUp(key);
            }

            public bool GetKeyUp(string name)
            {
                return Input.GetKeyUp(name);
            }

            public bool GetKeyDown(KeyCode key)
            {
                return Input.GetKeyDown(key);
            }

            public bool GetKeyDown(string name)
            {
                return Input.GetKeyDown(name);
            }

            public bool GetMouseButton(int button)
            {
                return Input.GetMouseButton(button);
            }

            public bool GetMouseButtonDown(int button)
            {
                return Input.GetMouseButtonDown(button);
            }

            public bool GetMouseButtonUp(int button)
            {
                return Input.GetMouseButtonUp(button);
            }

            public string[] GetJoystickNames()
            {
                return Input.GetJoystickNames();
            }

            public bool IsJoystickPreconfigured(string joystickName)
            {
                return Input.IsJoystickPreconfigured(joystickName);
            }

            public PenData GetPenEvent(int index)
            {
                return Input.GetPenEvent(index);
            }

            public void ResetPenEvents()
            {
                Input.ResetPenEvents();
            }

            public PenData GetLastPenContactEvent()
            {
                return Input.GetLastPenContactEvent();
            }

            public void ClearLastPenContactEvent()
            {
                Input.ClearLastPenContactEvent();
            }

            public Touch GetTouch(int index)
            {
                return Input.GetTouch(index);
            }

            public void SimulateTouch(Touch touch)
            {
                Input.SimulateTouch(touch);
            }

            public AccelerationEventNative GetAccelerationEvent(int index)
            {
                var e = Input.GetAccelerationEvent(index);
                return new AccelerationEventNative
                {
                    x = e.x, y = e.y, z = e.z,
                    m_TimeDelta = e.deltaTime
                };
            }

            public Vector3 GetGyroRotationRate(int idx)
            {
                ////TODO: index
                return gyro.rotationRate;
            }

            public Vector3 GetGyroRotationRateUnbiased(int idx)
            {
                ////TODO: index
                return gyro.rotationRateUnbiased;
            }

            public Vector3 GetGravity(int idx)
            {
                ////TODO: index
                return gyro.gravity;
            }

            public Vector3 GetUserAcceleration(int idx)
            {
                ////TODO: index
                return gyro.userAcceleration;
            }

            public Quaternion GetAttitude(int idx)
            {
                ////TODO: index
                return gyro.attitude;
            }

            public bool IsGyroEnabled(int idx)
            {
                ////TODO: index
                return gyro.enabled;
            }

            public void SetGyroEnabled(int idx, bool enabled)
            {
                ////TODO: index
                gyro.enabled = enabled;
            }

            public float GetGyroUpdateInterval(int idx)
            {
                ////TODO: index
                return gyro.updateInterval;
            }

            public void SetGyroUpdateInterval(int idx, float interval)
            {
                ////TODO: index
                gyro.updateInterval = interval;
            }

            public void StartUpdatingLocation()
            {
                location.Start();
            }

            public void StopUpdatingLocation()
            {
                location.Stop();
            }

            public bool backButtonLeavesApp
            {
                get => Input.backButtonLeavesApp;
                set => Input.backButtonLeavesApp = value;
            }

            public string inputString => Input.inputString;

            public IMECompositionMode imeCompositionMode
            {
                get => Input.imeCompositionMode;
                set => Input.imeCompositionMode = value;
            }

            public string compositionString => Input.compositionString;
            public bool imeIsSelected => Input.imeIsSelected;

            public Vector2 compositionCursorPos
            {
                get => Input.compositionCursorPos;
                set => Input.compositionCursorPos = value;
            }

            public bool eatKeyPressOnTextFieldFocus
            {
                get => Input.eatKeyPressOnTextFieldFocus;
                set => Input.eatKeyPressOnTextFieldFocus = value;
            }

            public bool anyKey => Input.anyKey;
            public bool anyKeyDown => Input.anyKeyDown;
            public bool mousePresent => Input.mousePresent;
            public Vector3 mousePosition => Input.mousePosition;
            public Vector2 mouseScrollDelta => Input.mouseScrollDelta;

            public bool simulateMouseWithTouches
            {
                get => Input.simulateMouseWithTouches;
                set => Input.simulateMouseWithTouches = value;
            }

            public int penEventCount => Input.penEventCount;
            public bool touchSupported => Input.touchSupported;
            public bool touchPressureSupported => Input.touchPressureSupported;
            public bool stylusTouchSupported => Input.stylusTouchSupported;

            public bool multiTouchEnabled
            {
                get => Input.multiTouchEnabled;
                set => Input.multiTouchEnabled = value;
            }

            public int touchCount => Input.touchCount;
            public int accelerationEventCount => Input.accelerationEventCount;
            public Vector3 acceleration => Input.acceleration;

            public bool compensateSensors
            {
                get => Input.compensateSensors;
                set => Input.compensateSensors = value;
            }
            public DeviceOrientationNative deviceOrientation => (DeviceOrientationNative)(int)Input.deviceOrientation;
            public int mainGyro => Input.mainGyro;
            public bool gyroSupported => Input.gyroSupported;
            public bool isLocationServiceEnabledByUser => location.isEnabledByUser;
            public LocationServiceStatusNative locationServiceStatus => (LocationServiceStatusNative)(int)location.status;

            public LocationInfoNative lastLocation
            {
                get
                {
                    var l = location.lastData;
                    return new LocationInfoNative
                    {
                        m_Latitude = l.latitude,
                        m_Longitude = l.longitude,
                        m_Altitude = l.altitude,
                        m_HorizontalAccuracy = l.horizontalAccuracy,
                        m_VerticalAccuracy = l.verticalAccuracy,
                        m_Timestamp = l.timestamp,
                    };
                }
            }

            public HeadingInfoNative lastHeading
            {
                get
                {
                    var heading = InputRuntime.s_Instance.lastHeading;
                    return new HeadingInfoNative
                    {
                        magneticHeading = heading.magneticHeading,
                        trueHeading = heading.trueHeading,
                        headingAccuracy = heading.headingAccuracy,
                        raw = heading.raw,
                        timestamp = heading.timestamp
                    };
                }
            }

            public bool headingUpdatesEnabled
            {
                get => compass.enabled;
                set => compass.enabled = value;
            }

            // In the API, these are parameters to the start method. So we don't have these as separate
            // settings. For now, just go directly to LegacyInputNative here. Nothing in native is actually
            // calling this but even if, until we change where the location service sits, this will still
            // do the right thing.
            public float desiredLocationAccuracy
            {
                set => LegacyInputNative.SetDesiredLocationAccuracy(value);
            }
            public float locationDistanceFilter
            {
                set => LegacyInputNative.SetLocationDistanceFilter(value);
            }
        }
        // ReSharper restore MemberHidesStaticFromOuterClass
    }
}
