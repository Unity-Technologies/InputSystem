using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.EnhancedTouch;
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

    /// <summary>
    /// Interface for accessing and managing input.
    /// </summary>
    /// <remarks>
    /// This is the central API for accessing input in Unity.
    /// </remarks>
    public static partial class Input // Partial to allow us to inject project-specific generated code into the API.
    {
        private static InputActionMap s_ButtonActions;
        private static InputAction s_AnyKeyAction;
        private static KeySet s_PressedKeys;
        private static KeySet s_PressedKeysBefore;
        private static KeySet s_ThisFramePressedKeys;
        private static KeySet s_ThisFrameReleasedKeys;
        private static Dictionary<string, KeyCode> s_KeyCodeMapping;
        private static LocationService s_Location = new LocationService();
        private static Gyroscope s_Gyro = new Gyroscope();
        private static Compass s_Compass = new Compass();
        private static Action<InputDevice, InputDeviceChange> s_OnDeviceChangeDelegate;

        // We actually only support 5 ATM but keeping this at 7 for compatibility with legacy implementation.
        internal const int kMouseButtonCount = 7;

        // We support arbitrary many joysticks but the KeyCode enum has a fixed upper limit on the number of joysticks.
        internal const int kMaxJoysticksAsPerKeyCodeEnum = 17;

        // Same for buttons. We don't limit them but the KeyCode enum has a fixed set per joystick.
        internal const int kMaxButtonsPerJoystickAsPerKeyCodeEnum = 20;

        internal static void OnEnteringPlayMode()
        {
            // EnhancedTouch is required for the Touch API
            EnhancedTouchSupport.Enable();

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
                    if (device is Joystick || device is Gamepad)
                    {
                        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
                        {
                            AddJoystick(device);
                        }
                        else
                        {
                            RemoveJoystick(device);
                        }
                    }
                    else if (device is Accelerometer accelerometer)
                    {
                        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
                            AddAccelerometer(accelerometer);
                    }
                    else if (device is Pen pen)
                    {
                        AddPen(pen);
                    }
                    break;
                }
            }
        }

        internal static void OnBeforeUpdate()
        {
            // Ignore updates that don't advance frames. Particularly
            // BeforeRender and Editor updates.
            if (InputState.currentUpdateType != InputUpdateType.Dynamic &&
                InputState.currentUpdateType != InputUpdateType.Fixed &&
                InputState.currentUpdateType != InputUpdateType.Manual)
                return;

            // Advance frame.
            s_ThisFramePressedKeys.Clear();
            s_ThisFrameReleasedKeys.Clear();
            s_Accelerometer.NextFrame();
            s_Pen.NextFrame();
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

            s_Accelerometer.Cleanup();
            s_Pen.Cleanup();

            InputSystem.InputSystem.onDeviceChange -= s_OnDeviceChangeDelegate;

            EnhancedTouchSupport.Disable();
            EnhancedTouchSupport.Reset();
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

        private static void SimulateTouch(Touch touch)
        {
        }

        public static Vector2 mouseScrollDelta => default;
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
