using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Linq;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.PlayerLoop;

// TODO rename HighLevel to something that makes sense
namespace UnityEngine.InputSystem.HighLevel
{
    internal enum InputDeviceType
    {
        Invalid,
        Keyboard,
        Mouse,
        Gamepad,
        Joystick
    }

    /// <summary>
    /// An enum for all controls that have button-like behaviour on keyboard, gamepad, mouse, and joystick devices.
    /// </summary>
    /// <seealso cref="Input.IsControlDown(Inputs)"/>
    /// <seealso cref="Input.IsControlUp(Inputs)"/>
    /// <seealso cref="Input.IsControlPressed(Inputs)"/>
    /// <seealso cref="Input.GetAxis(Inputs)"/>
    /// <seealso cref="Input.GetAxis(Inputs, Inputs)"/>
    /// <seealso cref="Input.GetAxis(Inputs, Inputs, Inputs, Inputs)"/>
    public enum Inputs
    {
        Key_Space = 10001,
        Key_Enter = 10002,
        Key_Tab = 10003,
        Key_Backquote = 10004,
        Key_Quote = 10005,
        Key_Semicolon = 10006,
        Key_Comma = 10007,
        Key_Period = 10008,
        Key_Slash = 10009,
        Key_Backslash = 10010,
        Key_LeftBracket = 10011,
        Key_RightBracket = 10012,
        Key_Minus = 10013,
        Key_Equals = 10014,
        Key_A = 10015,
        Key_B = 10016,
        Key_C = 10017,
        Key_D = 10018,
        Key_E = 10019,
        Key_F = 10020,
        Key_G = 10021,
        Key_H = 10022,
        Key_I = 10023,
        Key_J = 10024,
        Key_K = 10025,
        Key_L = 10026,
        Key_M = 10027,
        Key_N = 10028,
        Key_O = 10029,
        Key_P = 10030,
        Key_Q = 10031,
        Key_R = 10032,
        Key_S = 10033,
        Key_T = 10034,
        Key_U = 10035,
        Key_V = 10036,
        Key_W = 10037,
        Key_X = 10038,
        Key_Y = 10039,
        Key_Z = 10040,
        Key_Digit1 = 10041,
        Key_Digit2 = 10042,
        Key_Digit3 = 10043,
        Key_Digit4 = 10044,
        Key_Digit5 = 10045,
        Key_Digit6 = 10046,
        Key_Digit7 = 10047,
        Key_Digit8 = 10048,
        Key_Digit9 = 10049,
        Key_Digit0 = 10050,
        Key_LeftShift = 10051,
        Key_RightShift = 10052,
        Key_LeftAlt = 10053,
        Key_RightAlt = 10054,
        Key_LeftCtrl = 10055,
        Key_RightCtrl = 10056,
        Key_LeftMeta = 10057,
        Key_RightMeta = 10058,
        Key_ContextMenu = 10059,
        Key_Escape = 10060,
        Key_LeftArrow = 10061,
        Key_RightArrow = 10062,
        Key_UpArrow = 10063,
        Key_DownArrow = 10064,
        Key_Backspace = 10065,
        Key_PageDown = 10066,
        Key_PageUp = 10067,
        Key_Home = 10068,
        Key_End = 10069,
        Key_Insert = 10070,
        Key_Delete = 10071,
        Key_CapsLock = 10072,
        Key_NumLock = 10073,
        Key_PrintScreen = 10074,
        Key_ScrollLock = 10075,
        Key_Pause = 10076,
        Key_NumpadEnter = 10077,
        Key_NumpadDivide = 10078,
        Key_NumpadMultiply = 10079,
        Key_NumpadPlus = 10080,
        Key_NumpadMinus = 10081,
        Key_NumpadPeriod = 10082,
        Key_NumpadEquals = 10083,
        Key_Numpad0 = 10084,
        Key_Numpad1 = 10085,
        Key_Numpad2 = 10086,
        Key_Numpad3 = 10087,
        Key_Numpad4 = 10088,
        Key_Numpad5 = 10089,
        Key_Numpad6 = 10090,
        Key_Numpad7 = 10091,
        Key_Numpad8 = 10092,
        Key_Numpad9 = 10093,
        Key_F1 = 10094,
        Key_F2 = 10095,
        Key_F3 = 10096,
        Key_F4 = 10097,
        Key_F5 = 10098,
        Key_F6 = 10099,
        Key_F7 = 10100,
        Key_F8 = 10101,
        Key_F9 = 10102,
        Key_F10 = 10103,
        Key_F11 = 10104,
        Key_F12 = 10105,
        Key_OEM1 = 10106,
        Key_OEM2 = 10107,
        Key_OEM3 = 10108,
        Key_OEM4 = 10109,
        Key_OEM5 = 10110,

        Mouse_Left = 20001,
        Mouse_Right = 20002,
        Mouse_Middle = 20003,
        Mouse_Forward = 20004,
        Mouse_Back = 20005,

        Gamepad_DpadUp = 30001,
        Gamepad_DpadDown = 30002,
        Gamepad_DpadLeft = 30003,
        Gamepad_DpadRight = 30004,
        Gamepad_North = 30005,
        Gamepad_East = 30006,
        Gamepad_South = 30007,
        Gamepad_West = 30008,

        Gamepad_LeftStickButton = 30009,
        Gamepad_RightStickButton = 30010,
        Gamepad_LeftShoulder = 30011,
        Gamepad_RightShoulder = 30012,
        Gamepad_LeftStickUp = 30013,
        Gamepad_LeftStickDown = 30014,
        Gamepad_LeftStickLeft = 30015,
        Gamepad_LeftStickRight = 30016,
        Gamepad_RightStickUp = 30017,
        Gamepad_RightStickDown = 30018,
        Gamepad_RightStickLeft = 30019,
        Gamepad_RightStickRight = 30020,
        Gamepad_LeftTrigger = 30021,
        Gamepad_RightTrigger = 30022,
        Gamepad_Start = 30023,
        Gamepad_Select = 30024,
        Gamepad_X = Gamepad_West,
        Gamepad_Y = Gamepad_North,
        Gamepad_A = Gamepad_South,
        Gamepad_B = Gamepad_East,
        Gamepad_Cross = Gamepad_South,
        Gamepad_Square = Gamepad_West,
        Gamepad_Triangle = Gamepad_North,
        Gamepad_Circle = Gamepad_East,

        Joystick_Trigger = 40001,
        Joystick_Button2 = 40002,
        Joystick_Button3 = 40003,
        Joystick_Button4 = 40004,
        Joystick_Button5 = 40005,
        Joystick_Button6 = 40006,
        Joystick_Button7 = 40007,
        Joystick_Button8 = 40008,
    }

    /// <summary>
    /// An enum for querying the state of joystick buttons.
    /// </summary>
    /// <seealso cref="Input.IsControlDown(JoystickButton, JoystickSlot)"/>
    /// <seealso cref="Input.IsControlUp(JoystickButton, JoystickSlot)"/>
    /// <seealso cref="Input.IsControlPressed(JoystickButton, JoystickSlot)"/>
    public enum JoystickButton
    {
        Trigger, // HID path maps the element named "Button1" as the trigger
        Button2,
        Button3,
        Button4,
        Button5,
        Button6,
        Button7,
        Button8
    }

    /// <summary>
    /// An enum for querying the state of gamepad sticks.
    /// </summary>
    /// <seealso cref="Input.GetAxis(GamepadAxis,GamepadSlot)"/>
    public enum GamepadAxis
    {
        LeftStick,
        RightStick
    }

    /// <summary>
    /// An enum for all buttons on a generic gamepad.
    /// </summary>
    /// <seealso cref="Input.IsControlDown(GamepadButton,GamepadSlot)"/>
    public enum GamepadButton
    {
        DpadUp = Inputs.Gamepad_DpadUp,
        DpadDown = Inputs.Gamepad_DpadDown,
        DpadLeft = Inputs.Gamepad_DpadLeft,
        DpadRight = Inputs.Gamepad_DpadRight,
        North = Inputs.Gamepad_North,
        East = Inputs.Gamepad_East,
        South = Inputs.Gamepad_South,
        West = Inputs.Gamepad_West,
        LeftStickButton = Inputs.Gamepad_LeftStickButton,  // left stick pressed
        RightStickButton = Inputs.Gamepad_RightStickButton, // right stick pressed
        LeftShoulder = Inputs.Gamepad_LeftShoulder,
        RightShoulder = Inputs.Gamepad_RightShoulder,
        LeftTrigger = Inputs.Gamepad_LeftTrigger,
        RightTrigger = Inputs.Gamepad_RightTrigger,
        Start = Inputs.Gamepad_Start,
        Select = Inputs.Gamepad_Select,
        X = West,
        Y = North,
        A = South,
        B = East,
        Cross = South,
        Square = West,
        Triangle = North,
        Circle = East
    }

    /// <summary>
    /// An enum for addressing gamepad slots.
    /// </summary>
    /// <remarks>
    /// The Input class maintains a collection of Gamepad instances that use slot indexing. That is, if a gamepad disconnects,
    /// the next gamepad to connect goes in the vacant slot. This makes it easier to abstract gameplay or application logic
    /// from gamepad instances.
    /// </remarks>
    /// <seealso cref="Input.IsControlDown(GamepadButton,GamepadSlot)"/>
    /// <seealso cref="Input.IsControlUp(GamepadButton,GamepadSlot)"/>
    /// <seealso cref="Input.IsControlPressed(GamepadButton,GamepadSlot)"/>
    /// <seealso cref="Input.IsGamepadConnected(GamepadSlot)"/>
    /// <seealso cref="Input.DidGamepadConnectThisFrame(GamepadSlot)"/>
    /// <seealso cref="Input.DidGamepadDisconnectThisFrame(GamepadSlot)"/>
    /// <seealso cref="Input.GetAxis(GamepadAxis,GamepadSlot)"/>
    /// <seealso cref="Input.GetGamepadDeadZone(GamepadSlot)"/>
    public enum GamepadSlot
    {
        Slot1 = 0,
        Slot2,
        Slot3,
        Slot4,
        Slot5,
        Slot6,
        Slot7,
        Slot8,
        Slot9,
        Slot10,
        Slot11,
        Slot12,
        All = Int32.MaxValue
    }

    /// <summary>
    /// An enum for addressing joystick slots.
    /// </summary>
    /// <remarks>
    /// The Input class maintains a collection of joystick instances that use slot indexing. That is, if a joystick disconnects,
    /// the next joystick to connect goes in the vacant slot. This makes it easier to abstract joystick or application logic
    /// from joystick instances.
    /// </remarks>
    /// <seealso cref="Input.IsControlDown(JoystickButton, JoystickSlot)"/>
    /// <seealso cref="Input.IsControlUp(JoystickButton, JoystickSlot)"/>
    /// <seealso cref="Input.IsControlPressed(JoystickButton, JoystickSlot)"/>
    public enum JoystickSlot
    {
        Slot1 = 0,
        Slot2,
        Slot3,
        Slot4,
        Max = Slot4 + 1,
        All = Int32.MaxValue
    }

    public static class Input
    {
        internal const float kDefaultJoystickDeadzone = 0.125f;

        static Input()
        {
            s_GamepadSlotEnums = new ReadOnlyArray<GamepadSlot>(new[]
            {
                GamepadSlot.Slot1,
                GamepadSlot.Slot2,
                GamepadSlot.Slot3,
                GamepadSlot.Slot4,
                GamepadSlot.Slot5,
                GamepadSlot.Slot6,
                GamepadSlot.Slot7,
                GamepadSlot.Slot8,
                GamepadSlot.Slot9,
                GamepadSlot.Slot10,
                GamepadSlot.Slot11,
                GamepadSlot.Slot12
            });
            s_Gamepads = new Gamepad[maxGamepadSlots];
            s_GamepadsConnectedFrames = new int[maxGamepadSlots];
            s_GamepadsDisconnectedFrames = new int[maxGamepadSlots];

            s_Joysticks = new Joystick[(int)JoystickSlot.Max];
        }

        /// <summary>
        /// A collection of all gamepads currently connected to the system.
        /// </summary>
        /// <remarks>
        /// This collection uses "stable" indexing. If a gamepad that was connected disconnects, the
        /// array index that it occupied remains empty until another gamepad connects. It can be the
        /// same physical gamepad that just disconnected, or one the system has not seen before. Either
        /// way, it will occupy the previously vacated index. In this way, applications that need to
        /// work with multiple gamepads directly, as opposed to through the high-level APIs, can simply
        /// keep the reference to the index and use it to query state from that gamepad.
        /// </remarks>
        public static IReadOnlyList<Gamepad> gamepads => s_Gamepads;

        /// <summary>
        /// A collection of all joysticks currently connected to the system.
        /// </summary>
        public static IReadOnlyList<Joystick> joysticks => s_Joysticks;

        /// <summary>
        /// A collection for conveniently looping over all gamepad slot enum values.
        /// </summary>
        /// <remarks>
        /// It is a very common pattern to loop over all possibly connected gamepads and perform some action.
        /// Without this convenience collection, the code might look like:
        /// <example>
        /// <code>
        /// for(var i = 0; i &lt; Input.maxGamepadSlots; i++)
        /// {
        ///     if(Input.IsGamepadConnected((GamepadSlot)i))
        ///     {
        ///         ...
        ///     }
        /// }
        /// </code>
        /// </example>
        /// but with the collection can look like this:
        /// <example>
        /// <code>
        /// foreach(var slot in Input.gamepadSlotEnums)
        /// {
        ///     if(Input.IsGamepadConnected(slot))
        ///     {
        ///         ...
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        public static ReadOnlyArray<GamepadSlot> gamepadSlotEnums => s_GamepadSlotEnums;

        /// <summary>
        /// The maximum number of supported gamepad slots.
        /// </summary>
        public static int maxGamepadSlots => s_GamepadSlotEnums.Count;

        /// <summary>
        /// The pixel position of the pointer in window space.
        /// </summary>
        /// <returns>The position of the most recently actuated pointer device.</returns>
        /// <remarks>
        /// If there are multiple pointer devices attached to the system, this will return the pointer
        /// position of the one that was most recently actuated. In window space coordinates, the bottom
        /// left corner of the window is 0, 0.
        /// pointerPosition will work for mouse, pen, and touchscreen devices.
        /// Note that for Touchscreen devices, this will return the position of the primary touch.
        /// </remarks>
        public static Vector2 pointerPosition => s_PointerPosition;

        /// <summary>
        /// Indicates if a pointer device is attached to the system.
        /// </summary>
        /// <returns>True if any pointer device is detected.</returns>
        /// <remarks>
        /// This will return true for mouse, pen, and touchscreen devices.
        /// </remarks>
        public static bool pointerPresent
        {
            get
            {
                foreach (var inputDevice in InputSystem.devices)
                {
                    if (inputDevice is Pointer)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// The current mouse scroll delta.
        /// </summary>
        /// <returns>A vector2 representing horizontal and vertical scroll values, or Vector2.zero if no mouse is detected.</returns>
        /// <remarks>
        /// For a common desktop mouse, this property will store the mouse wheel scroll delta in the Vector2.y property. In
        /// this case, Input.scrollDelta can be positive (up) or negative (down). For trackpad devices, scrolling is
        /// emulated using double finger movement, and can represent horizontal scrolling (Vector2.x can be negative (left) or positive
        /// (right)) or vertical scrolling (Vector2.y can be positive (up) or negative (down)).
        /// The value returned by scrollDelta will need to be adjusted for sensitivity based on your applications needs and the
        /// platform. For example, on Windows, mouse wheel scroll deltas are reported in increments of 120 units.
        /// If there are multiple devices attached to a system that identify as a mouse, the values from the most recently
        /// actuated device are used.
        /// Note that scrollDelta is read-only.
        /// </remarks>
        public static Vector2 scrollDelta => s_ScrollDelta;


        private static Gamepad[] s_Gamepads;
        private static int[] s_GamepadsConnectedFrames;
        private static int[] s_GamepadsDisconnectedFrames;
        private static ReadOnlyArray<GamepadSlot> s_GamepadSlotEnums;
        private static GamepadConfig[] s_GamepadConfigs = new GamepadConfig[(int)GamepadSlot.Slot12 + 1];

        private static Joystick[] s_Joysticks;

        private static Vector2 s_PointerPosition;
        private static Vector2 s_ScrollDelta;
        private static InputAction s_PointerAction;
        private static InputAction s_ScrollAction;

#if UNITY_EDITOR
        internal static bool s_TimeHasUpdatedThisFrame;
#endif

        private struct GamepadConfig
        {
            public float TriggerPressPoint;
            public float DeadZone;

            public static GamepadConfig CreateConfigFromSettings()
            {
                return new GamepadConfig
                {
                    TriggerPressPoint = InputSystem.settings.defaultButtonPressPoint,
                    DeadZone = InputSystem.settings.defaultDeadzoneMin
                };
            }
        }

        private static void InitializeGamepadConfigs()
        {
            var defaultConfig = GamepadConfig.CreateConfigFromSettings();
            for (var i = 0; i < s_GamepadConfigs.Length; ++i)
                s_GamepadConfigs[i] = defaultConfig;
        }

        private static InputDeviceType GetDeviceTypeForInput(Inputs input)
        {
            switch (input)
            {
                case Inputs.Key_Space:
                case Inputs.Key_Enter:
                case Inputs.Key_Tab:
                case Inputs.Key_Backquote:
                case Inputs.Key_Quote:
                case Inputs.Key_Semicolon:
                case Inputs.Key_Comma:
                case Inputs.Key_Period:
                case Inputs.Key_Slash:
                case Inputs.Key_Backslash:
                case Inputs.Key_LeftBracket:
                case Inputs.Key_RightBracket:
                case Inputs.Key_Minus:
                case Inputs.Key_Equals:
                case Inputs.Key_A:
                case Inputs.Key_B:
                case Inputs.Key_C:
                case Inputs.Key_D:
                case Inputs.Key_E:
                case Inputs.Key_F:
                case Inputs.Key_G:
                case Inputs.Key_H:
                case Inputs.Key_I:
                case Inputs.Key_J:
                case Inputs.Key_K:
                case Inputs.Key_L:
                case Inputs.Key_M:
                case Inputs.Key_N:
                case Inputs.Key_O:
                case Inputs.Key_P:
                case Inputs.Key_Q:
                case Inputs.Key_R:
                case Inputs.Key_S:
                case Inputs.Key_T:
                case Inputs.Key_U:
                case Inputs.Key_V:
                case Inputs.Key_W:
                case Inputs.Key_X:
                case Inputs.Key_Y:
                case Inputs.Key_Z:
                case Inputs.Key_Digit1:
                case Inputs.Key_Digit2:
                case Inputs.Key_Digit3:
                case Inputs.Key_Digit4:
                case Inputs.Key_Digit5:
                case Inputs.Key_Digit6:
                case Inputs.Key_Digit7:
                case Inputs.Key_Digit8:
                case Inputs.Key_Digit9:
                case Inputs.Key_Digit0:
                case Inputs.Key_LeftShift:
                case Inputs.Key_RightShift:
                case Inputs.Key_LeftAlt:
                case Inputs.Key_RightAlt:
                case Inputs.Key_LeftCtrl:
                case Inputs.Key_RightCtrl:
                case Inputs.Key_LeftMeta:
                case Inputs.Key_RightMeta:
                case Inputs.Key_ContextMenu:
                case Inputs.Key_Escape:
                case Inputs.Key_LeftArrow:
                case Inputs.Key_RightArrow:
                case Inputs.Key_UpArrow:
                case Inputs.Key_DownArrow:
                case Inputs.Key_Backspace:
                case Inputs.Key_PageDown:
                case Inputs.Key_PageUp:
                case Inputs.Key_Home:
                case Inputs.Key_End:
                case Inputs.Key_Insert:
                case Inputs.Key_Delete:
                case Inputs.Key_CapsLock:
                case Inputs.Key_NumLock:
                case Inputs.Key_PrintScreen:
                case Inputs.Key_ScrollLock:
                case Inputs.Key_Pause:
                case Inputs.Key_NumpadEnter:
                case Inputs.Key_NumpadDivide:
                case Inputs.Key_NumpadMultiply:
                case Inputs.Key_NumpadPlus:
                case Inputs.Key_NumpadMinus:
                case Inputs.Key_NumpadPeriod:
                case Inputs.Key_NumpadEquals:
                case Inputs.Key_Numpad0:
                case Inputs.Key_Numpad1:
                case Inputs.Key_Numpad2:
                case Inputs.Key_Numpad3:
                case Inputs.Key_Numpad4:
                case Inputs.Key_Numpad5:
                case Inputs.Key_Numpad6:
                case Inputs.Key_Numpad7:
                case Inputs.Key_Numpad8:
                case Inputs.Key_Numpad9:
                case Inputs.Key_F1:
                case Inputs.Key_F2:
                case Inputs.Key_F3:
                case Inputs.Key_F4:
                case Inputs.Key_F5:
                case Inputs.Key_F6:
                case Inputs.Key_F7:
                case Inputs.Key_F8:
                case Inputs.Key_F9:
                case Inputs.Key_F10:
                case Inputs.Key_F11:
                case Inputs.Key_F12:
                case Inputs.Key_OEM1:
                case Inputs.Key_OEM2:
                case Inputs.Key_OEM3:
                case Inputs.Key_OEM4:
                case Inputs.Key_OEM5:
                    return InputDeviceType.Keyboard;

                case Inputs.Mouse_Left:
                case Inputs.Mouse_Right:
                case Inputs.Mouse_Middle:
                case Inputs.Mouse_Forward:
                case Inputs.Mouse_Back:
                    return InputDeviceType.Mouse;

                case Inputs.Gamepad_DpadUp:
                case Inputs.Gamepad_DpadDown:
                case Inputs.Gamepad_DpadLeft:
                case Inputs.Gamepad_DpadRight:
                case Inputs.Gamepad_North:
                case Inputs.Gamepad_East:
                case Inputs.Gamepad_South:
                case Inputs.Gamepad_West:
                case Inputs.Gamepad_LeftStickButton:
                case Inputs.Gamepad_RightStickButton:
                case Inputs.Gamepad_LeftShoulder:
                case Inputs.Gamepad_RightShoulder:
                case Inputs.Gamepad_LeftStickUp:
                case Inputs.Gamepad_LeftStickDown:
                case Inputs.Gamepad_LeftStickLeft:
                case Inputs.Gamepad_LeftStickRight:
                case Inputs.Gamepad_RightStickUp:
                case Inputs.Gamepad_RightStickDown:
                case Inputs.Gamepad_RightStickLeft:
                case Inputs.Gamepad_RightStickRight:
                case Inputs.Gamepad_LeftTrigger:
                case Inputs.Gamepad_RightTrigger:
                case Inputs.Gamepad_Start:
                case Inputs.Gamepad_Select:
                    return InputDeviceType.Gamepad;

                case Inputs.Joystick_Trigger:
                case Inputs.Joystick_Button2:
                case Inputs.Joystick_Button3:
                case Inputs.Joystick_Button4:
                case Inputs.Joystick_Button5:
                case Inputs.Joystick_Button6:
                case Inputs.Joystick_Button7:
                case Inputs.Joystick_Button8:
                    return InputDeviceType.Joystick;
            }

            throw new ArgumentException($"Unexpected Inputs enum value '{input}'");
        }

        private static ButtonControl GetKeyboardButtonControl(Keyboard k, Inputs input)
        {
            if (k == null)
                return null;

            switch (input)
            {
                case Inputs.Key_Space: return k[Key.Space];
                case Inputs.Key_Enter: return k[Key.Enter];
                case Inputs.Key_Tab: return k[Key.Tab];
                case Inputs.Key_Backquote: return k[Key.Backquote];
                case Inputs.Key_Quote: return k[Key.Quote];
                case Inputs.Key_Semicolon: return k[Key.Semicolon];
                case Inputs.Key_Comma: return k[Key.Comma];
                case Inputs.Key_Period: return k[Key.Period];
                case Inputs.Key_Slash: return k[Key.Slash];
                case Inputs.Key_Backslash: return k[Key.Backslash];
                case Inputs.Key_LeftBracket: return k[Key.LeftBracket];
                case Inputs.Key_RightBracket: return k[Key.RightBracket];
                case Inputs.Key_Minus: return k[Key.Minus];
                case Inputs.Key_Equals: return k[Key.Equals];
                case Inputs.Key_A: return k[Key.A];
                case Inputs.Key_B: return k[Key.B];
                case Inputs.Key_C: return k[Key.C];
                case Inputs.Key_D: return k[Key.D];
                case Inputs.Key_E: return k[Key.E];
                case Inputs.Key_F: return k[Key.F];
                case Inputs.Key_G: return k[Key.G];
                case Inputs.Key_H: return k[Key.H];
                case Inputs.Key_I: return k[Key.I];
                case Inputs.Key_J: return k[Key.J];
                case Inputs.Key_K: return k[Key.K];
                case Inputs.Key_L: return k[Key.L];
                case Inputs.Key_M: return k[Key.M];
                case Inputs.Key_N: return k[Key.N];
                case Inputs.Key_O: return k[Key.O];
                case Inputs.Key_P: return k[Key.P];
                case Inputs.Key_Q: return k[Key.Q];
                case Inputs.Key_R: return k[Key.R];
                case Inputs.Key_S: return k[Key.S];
                case Inputs.Key_T: return k[Key.T];
                case Inputs.Key_U: return k[Key.U];
                case Inputs.Key_V: return k[Key.V];
                case Inputs.Key_W: return k[Key.W];
                case Inputs.Key_X: return k[Key.X];
                case Inputs.Key_Y: return k[Key.Y];
                case Inputs.Key_Z: return k[Key.Z];
                case Inputs.Key_Digit1: return k[Key.Digit1];
                case Inputs.Key_Digit2: return k[Key.Digit2];
                case Inputs.Key_Digit3: return k[Key.Digit3];
                case Inputs.Key_Digit4: return k[Key.Digit4];
                case Inputs.Key_Digit5: return k[Key.Digit5];
                case Inputs.Key_Digit6: return k[Key.Digit6];
                case Inputs.Key_Digit7: return k[Key.Digit7];
                case Inputs.Key_Digit8: return k[Key.Digit8];
                case Inputs.Key_Digit9: return k[Key.Digit9];
                case Inputs.Key_Digit0: return k[Key.Digit0];
                case Inputs.Key_LeftShift: return k[Key.LeftShift];
                case Inputs.Key_RightShift: return k[Key.RightShift];
                case Inputs.Key_LeftAlt: return k[Key.LeftAlt];
                case Inputs.Key_RightAlt: return k[Key.RightAlt];
                case Inputs.Key_LeftCtrl: return k[Key.LeftCtrl];
                case Inputs.Key_RightCtrl: return k[Key.RightCtrl];
                case Inputs.Key_LeftMeta: return k[Key.LeftMeta];
                case Inputs.Key_RightMeta: return k[Key.RightMeta];
                case Inputs.Key_ContextMenu: return k[Key.ContextMenu];
                case Inputs.Key_Escape: return k[Key.Escape];
                case Inputs.Key_LeftArrow: return k[Key.LeftArrow];
                case Inputs.Key_RightArrow: return k[Key.RightArrow];
                case Inputs.Key_UpArrow: return k[Key.UpArrow];
                case Inputs.Key_DownArrow: return k[Key.DownArrow];
                case Inputs.Key_Backspace: return k[Key.Backspace];
                case Inputs.Key_PageDown: return k[Key.PageDown];
                case Inputs.Key_PageUp: return k[Key.PageUp];
                case Inputs.Key_Home: return k[Key.Home];
                case Inputs.Key_End: return k[Key.End];
                case Inputs.Key_Insert: return k[Key.Insert];
                case Inputs.Key_Delete: return k[Key.Delete];
                case Inputs.Key_CapsLock: return k[Key.CapsLock];
                case Inputs.Key_NumLock: return k[Key.NumLock];
                case Inputs.Key_PrintScreen: return k[Key.PrintScreen];
                case Inputs.Key_ScrollLock: return k[Key.ScrollLock];
                case Inputs.Key_Pause: return k[Key.Pause];
                case Inputs.Key_NumpadEnter: return k[Key.NumpadEnter];
                case Inputs.Key_NumpadDivide: return k[Key.NumpadDivide];
                case Inputs.Key_NumpadMultiply: return k[Key.NumpadMultiply];
                case Inputs.Key_NumpadPlus: return k[Key.NumpadPlus];
                case Inputs.Key_NumpadMinus: return k[Key.NumpadMinus];
                case Inputs.Key_NumpadPeriod: return k[Key.NumpadPeriod];
                case Inputs.Key_NumpadEquals: return k[Key.NumpadEquals];
                case Inputs.Key_Numpad0: return k[Key.Numpad0];
                case Inputs.Key_Numpad1: return k[Key.Numpad1];
                case Inputs.Key_Numpad2: return k[Key.Numpad2];
                case Inputs.Key_Numpad3: return k[Key.Numpad3];
                case Inputs.Key_Numpad4: return k[Key.Numpad4];
                case Inputs.Key_Numpad5: return k[Key.Numpad5];
                case Inputs.Key_Numpad6: return k[Key.Numpad6];
                case Inputs.Key_Numpad7: return k[Key.Numpad7];
                case Inputs.Key_Numpad8: return k[Key.Numpad8];
                case Inputs.Key_Numpad9: return k[Key.Numpad9];
                case Inputs.Key_F1: return k[Key.F1];
                case Inputs.Key_F2: return k[Key.F2];
                case Inputs.Key_F3: return k[Key.F3];
                case Inputs.Key_F4: return k[Key.F4];
                case Inputs.Key_F5: return k[Key.F5];
                case Inputs.Key_F6: return k[Key.F6];
                case Inputs.Key_F7: return k[Key.F7];
                case Inputs.Key_F8: return k[Key.F8];
                case Inputs.Key_F9: return k[Key.F9];
                case Inputs.Key_F10: return k[Key.F10];
                case Inputs.Key_F11: return k[Key.F11];
                case Inputs.Key_F12: return k[Key.F12];
                case Inputs.Key_OEM1: return k[Key.OEM1];
                case Inputs.Key_OEM2: return k[Key.OEM2];
                case Inputs.Key_OEM3: return k[Key.OEM3];
                case Inputs.Key_OEM4: return k[Key.OEM4];
                case Inputs.Key_OEM5: return k[Key.OEM5];
                default: return null;
            }
        }

        private static ButtonControl GetMouseButtonControl(Mouse m, Inputs input)
        {
            if (m == null)
                return null;
            switch (input)
            {
                case Inputs.Mouse_Left: return m.leftButton;
                case Inputs.Mouse_Right: return m.rightButton;
                case Inputs.Mouse_Middle: return m.middleButton;
                case Inputs.Mouse_Forward: return m.forwardButton;
                case Inputs.Mouse_Back: return m.backButton;
                default: return null;
            }
        }

        private static ButtonControl GetGamepadButtonControl(Gamepad gamepad, Inputs input)
        {
            if (gamepad == null)
                throw new ArgumentNullException(nameof(gamepad));

            switch (input)
            {
                case Inputs.Gamepad_DpadUp: return gamepad.dpad.up;
                case Inputs.Gamepad_DpadDown: return gamepad.dpad.down;
                case Inputs.Gamepad_DpadLeft: return gamepad.dpad.left;
                case Inputs.Gamepad_DpadRight: return gamepad.dpad.right;
                case Inputs.Gamepad_North: return gamepad.buttonNorth;
                case Inputs.Gamepad_East: return gamepad.buttonEast;
                case Inputs.Gamepad_South: return gamepad.buttonSouth;
                case Inputs.Gamepad_West: return gamepad.buttonWest;
                case Inputs.Gamepad_LeftStickButton:
                    return gamepad.leftStickButton;
                case Inputs.Gamepad_RightStickButton:
                    return gamepad.rightStickButton;
                case Inputs.Gamepad_LeftShoulder: return gamepad.leftShoulder;
                case Inputs.Gamepad_RightShoulder:
                    return gamepad.rightShoulder;
                case Inputs.Gamepad_LeftStickUp: return gamepad.leftStick.up;
                case Inputs.Gamepad_LeftStickDown:
                    return gamepad.leftStick.down;
                case Inputs.Gamepad_LeftStickLeft:
                    return gamepad.leftStick.left;
                case Inputs.Gamepad_LeftStickRight:
                    return gamepad.leftStick.right;
                case Inputs.Gamepad_RightStickUp: return gamepad.rightStick.up;
                case Inputs.Gamepad_RightStickDown:
                    return gamepad.rightStick.down;
                case Inputs.Gamepad_RightStickLeft:
                    return gamepad.rightStick.left;
                case Inputs.Gamepad_RightStickRight:
                    return gamepad.rightStick.right;
                case Inputs.Gamepad_LeftTrigger: return gamepad.leftTrigger;
                case Inputs.Gamepad_RightTrigger: return gamepad.rightTrigger;
                case Inputs.Gamepad_Start: return gamepad.startButton;
                case Inputs.Gamepad_Select: return gamepad.selectButton;
                default:
                    throw new InvalidEnumArgumentException(nameof(input), (int)input, typeof(Inputs));
            }
        }

        private static ButtonControl GetJoystickButtonControl(Joystick joystick, Inputs input)
        {
            if (joystick == null)
                return null;

            string buttonName;
            switch (input)
            {
                case Inputs.Joystick_Trigger:
                    return joystick.trigger;
                case Inputs.Joystick_Button2:
                    buttonName = "button2";
                    break;
                case Inputs.Joystick_Button3:
                    buttonName = "button3";
                    break;
                case Inputs.Joystick_Button4:
                    buttonName = "button4";
                    break;
                case Inputs.Joystick_Button5:
                    buttonName = "button5";
                    break;
                case Inputs.Joystick_Button6:
                    buttonName = "button6";
                    break;
                case Inputs.Joystick_Button7:
                    buttonName = "button7";
                    break;
                case Inputs.Joystick_Button8:
                    buttonName = "button8";
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(input), (int)input, typeof(JoystickButton));
            }

            // do a non-recursive search for the button based on name
            for (var i = 0; i < joystick.allControls.Count; i++)
            {
                var control = joystick.allControls[i];
                if (string.Equals(control.name, buttonName, StringComparison.OrdinalIgnoreCase))
                    return control as ButtonControl;
            }

            return null;
        }

        private static ButtonControl GetJoystickButtonControl(Joystick joystick, JoystickButton joystickButton)
        {
            if (joystick == null)
                return null;

            string buttonName;
            switch (joystickButton)
            {
                case JoystickButton.Trigger:
                    return joystick.trigger;
                case JoystickButton.Button2:
                    buttonName = "button2";
                    break;
                case JoystickButton.Button3:
                    buttonName = "button3";
                    break;
                case JoystickButton.Button4:
                    buttonName = "button4";
                    break;
                case JoystickButton.Button5:
                    buttonName = "button5";
                    break;
                case JoystickButton.Button6:
                    buttonName = "button6";
                    break;
                case JoystickButton.Button7:
                    buttonName = "button7";
                    break;
                case JoystickButton.Button8:
                    buttonName = "button8";
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(joystickButton), (int)joystickButton, typeof(JoystickButton));
            }

            // do a non-recursive search for the button based on name
            for (var i = 0; i < joystick.allControls.Count; i++)
            {
                var control = joystick.allControls[i];
                if (string.Equals(control.name, buttonName, StringComparison.OrdinalIgnoreCase))
                    return control as ButtonControl;
            }

            return null;
        }

        /// <summary>
        /// Is the indicated control currently pressed.
        /// </summary>
        /// <param name="input">Control from Inputs enum.</param>
        /// <returns>True if control is currently pressed, false if control is not pressed or not available (device disconnected, etc).</returns>
        /// <remarks>
        /// This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        /// and return true if the control is currently pressed on any of them.
        /// </remarks>
        public static bool IsControlPressed(Inputs input)
        {
            var deviceType = GetDeviceTypeForInput(input);

            switch (deviceType)
            {
                case InputDeviceType.Keyboard:
                    foreach (var inputDevice in InputSystem.devices)
                    {
                        if (inputDevice is Keyboard keyboard &&
                            GetKeyboardButtonControl(keyboard, input).isPressed)
                            return true;
                    }

                    return false;
                case InputDeviceType.Mouse:
                    foreach (var inputDevice in InputSystem.devices)
                    {
                        if (inputDevice is Mouse mouse &&
                            GetMouseButtonControl(mouse, input).isPressed)
                            return true;
                    }

                    return false;
                case InputDeviceType.Gamepad:
                    for (var i = 0; i < s_Gamepads.Length; i++)
                    {
                        var gamepad = s_Gamepads[i];
                        if (gamepad == null) continue;

                        if (GetGamepadButtonControl(gamepad, input).ReadValue() >= 
                            GetGamepadTriggerPressPoint((GamepadSlot)i))
                            return true;
                    }

                    return false;
                case InputDeviceType.Joystick:
                    for (var i = 0; i < (int)JoystickSlot.Max; i++)
                    {
                        var button = GetJoystickButtonControl(s_Joysticks[i], input);
                        if (button != null && button.ReadValue() >= InputSystem.settings.defaultButtonPressPoint)
                            return true;
                    }
                    return false;
                case InputDeviceType.Invalid:
                default:
                    return false;
            }
        }

        /// <summary>
        /// Is the indicated gamepad button currently pressed.
        /// </summary>
        /// <param name="button">A control from the GamepadButton enum.</param>
        /// <param name="slot">Which gamepad to check for input. Default is 'Any'.</param>
        /// <returns>True if the input is currently held down, false if the input is not pressed,
        /// or if 'slot' is specified and no gamepad exists in that slot.</returns>
        public static bool IsControlPressed(GamepadButton button, GamepadSlot slot = GamepadSlot.All)
        {
            if (slot != GamepadSlot.All)
                return s_Gamepads[(int)slot] != null &&
                    GetGamepadButtonControl(s_Gamepads[(int)slot], (Inputs)button).isPressed;

            for (var i = 0; i < maxGamepadSlots; i++)
            {
                if (s_Gamepads[i] == null) continue;

                if (GetGamepadButtonControl(s_Gamepads[i], (Inputs)button).isPressed)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Is the indicated joystick button pressed.
        /// </summary>
        /// <param name="button">A button from the JoystickButton enum.</param>
        /// <param name="slot">The joystick to read input from.</param>
        /// <returns>True if the button is currently pressed, and false if it is not, including when
        /// there is no joystick in the specified slot..</returns>
        /// <remarks>If JoystickSlot.All is specified for the 'slot' argument, the method will
        /// return true if the specified button is pressed on any joystick in the available slots.</remarks>
        public static bool IsControlPressed(JoystickButton button, JoystickSlot slot = JoystickSlot.All)
        {
            if (slot != JoystickSlot.All)
            {
                var joystick = s_Joysticks[(int)slot];
                if (joystick == null)
                    return false;

                var control = GetJoystickButtonControl(joystick, button);
                return control != null && control.isPressed;
            }

            for (var i = 0; i < (int)JoystickSlot.Max; i++)
            {
                if (s_Joysticks[i] == null) continue;

                var control = GetJoystickButtonControl(s_Joysticks[i], button);
                if (control != null && control.isPressed)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// True in the frame that the input was pressed.
        /// </summary>
        /// <param name="input">Control from Inputs enum.</param>
        /// <returns>True if control was actuated in the current frame, false if control was not actuated or not available (device disconnected, etc).</returns>
        /// <remarks>
        /// This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        /// and return true if the control was actuated in the current frame on any of them.
        /// </remarks>
        public static bool IsControlDown(Inputs input)
        {
            var deviceType = GetDeviceTypeForInput(input);

            switch (deviceType)
            {
                case InputDeviceType.Keyboard:
                    foreach (var inputDevice in InputSystem.devices)
                    {
                        if (inputDevice is Keyboard keyboard &&
                            GetKeyboardButtonControl(keyboard, input).wasPressedThisFrame)
                            return true;
                    }

                    return false;
                case InputDeviceType.Mouse:
                    foreach (var inputDevice in InputSystem.devices)
                    {
                        if (inputDevice is Mouse mouse &&
                            GetMouseButtonControl(mouse, input).wasPressedThisFrame)
                            return true;
                    }

                    return false;
                case InputDeviceType.Gamepad:
                    for (var i = 0; i < s_Gamepads.Length; i++)
                    {
                        var gamepad = s_Gamepads[i];
                        if (gamepad == null) continue;

                        var control = GetGamepadButtonControl(gamepad, input);
                        var pressPoint = GetGamepadTriggerPressPoint((GamepadSlot)i);
                        if (gamepad.wasUpdatedThisFrame &&
                            control.ReadValue() >= pressPoint &&
                            control.ReadValueFromPreviousFrame() < pressPoint)
                            return true;
                    }

                    return false;
                case InputDeviceType.Joystick:
                    for (var i = 0; i < (int)JoystickSlot.Max; i++)
                    {
                        var button = GetJoystickButtonControl(s_Joysticks[i], input);
                        if (button != null && button.wasPressedThisFrame)
                            return true;
                    }
                    return false;
                case InputDeviceType.Invalid:
                default:
                    return false;
            }
        }

        /// <summary>
        /// Was the specified control pressed in the current frame.
        /// </summary>
        /// <param name="button">A control from the GamepadButton enum.</param>
        /// <param name="slot">Which gamepad to check for input. Default is 'All'.</param>
        /// <returns>True if the input was pressed in the current frame, false if the input is not pressed, was
        /// pressed in a frame previous to the current one, or if 'slot' is specified and no gamepad is connected to that slot.</returns>
        public static bool IsControlDown(GamepadButton button, GamepadSlot slot = GamepadSlot.All)
        {
            if (slot != GamepadSlot.All)
                return s_Gamepads[(int)slot] != null &&
                    GetGamepadButtonControl(s_Gamepads[(int)slot], (Inputs)button).wasPressedThisFrame;

            for (var i = 0; i < maxGamepadSlots; i++)
            {
                if (s_Gamepads[i] == null) continue;

                if (GetGamepadButtonControl(s_Gamepads[i], (Inputs)button).wasPressedThisFrame)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Was the specified control pressed in the current frame.
        /// </summary>
        /// <param name="button">A control from the JoystickButton enum.</param>
        /// <param name="slot">Which joystick to check for input. Default is 'All'.</param>
        /// <returns>True if the input was pressed in the current frame, otherwise false, including
        /// when 'slot' is specified and no joystick is connected to that slot.</returns>
        public static bool IsControlDown(JoystickButton button, JoystickSlot slot = JoystickSlot.All)
        {
            if (slot != JoystickSlot.All)
            {
                var joystick = s_Joysticks[(int)slot];
                if (joystick == null)
                    return false;

                var control = GetJoystickButtonControl(joystick, button);
                return control != null && control.wasPressedThisFrame;
            }

            for (var i = 0; i < (int)JoystickSlot.Max; i++)
            {
                if (s_Joysticks[i] == null) continue;

                var control = GetJoystickButtonControl(s_Joysticks[i], button);
                if (control != null && control.wasPressedThisFrame)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// True in the frame that the input was released.
        /// </summary>
        /// <param name="input">Control from Inputs enum.</param>
        /// <returns>True if control was released in the current frame, false if control was not released or not available (device disconnected, etc).</returns>
        /// <remarks>
        /// This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        /// and return true if the control was released in the current frame on any of them.
        /// </remarks>
        public static bool IsControlUp(Inputs input)
        {
            var deviceType = GetDeviceTypeForInput(input);

            switch (deviceType)
            {
                case InputDeviceType.Keyboard:
                    foreach (var inputDevice in InputSystem.devices)
                    {
                        if (inputDevice is Keyboard keyboard &&
                            GetKeyboardButtonControl(keyboard, input).wasReleasedThisFrame)
                            return true;
                    }

                    return false;
                case InputDeviceType.Mouse:
                    foreach (var inputDevice in InputSystem.devices)
                    {
                        if (inputDevice is Mouse mouse &&
                            GetMouseButtonControl(mouse, input).wasReleasedThisFrame)
                            return true;
                    }

                    return false;
                case InputDeviceType.Gamepad:
                    for (var i = 0; i < s_Gamepads.Length; i++)
                    {
                        var gamepad = s_Gamepads[i];
                        if (gamepad == null) continue;

                        var control = GetGamepadButtonControl(gamepad, input);
                        var pressPoint = GetGamepadTriggerPressPoint((GamepadSlot)i);
                        if (gamepad.wasUpdatedThisFrame &&
                            control.ReadValue() < pressPoint &&
                            control.ReadValueFromPreviousFrame() >= pressPoint)
                            return true;
                    }

                    return false;
                case InputDeviceType.Joystick:
                    for (var i = 0; i < (int)JoystickSlot.Max; i++)
                    {
                        var button = GetJoystickButtonControl(s_Joysticks[i], input);
                        if (button != null && button.wasReleasedThisFrame)
                            return true;
                    }
                    return false;
                case InputDeviceType.Invalid:
                default:
                    return false;
            }
        }

        /// <summary>
        /// True in the frame that the button was released.
        /// </summary>
        /// <param name="button">A control from the GamepadButton enum.</param>
        /// <param name="slot">Which gamepad to check for input. Default is 'Any'.</param>
        /// <returns>True if the input was released in the current frame, otherwise false.
        /// Also returns false if 'slot' is specified and no gamepad is connected to that slot.</returns>
        public static bool IsControlUp(GamepadButton button, GamepadSlot slot = GamepadSlot.All)
        {
            if (slot != GamepadSlot.All)
                return s_Gamepads[(int)slot] != null &&
                    GetGamepadButtonControl(s_Gamepads[(int)slot], (Inputs)button).wasReleasedThisFrame;

            for (var i = 0; i < maxGamepadSlots; i++)
            {
                if (s_Gamepads[i] == null) continue;

                if (GetGamepadButtonControl(s_Gamepads[i], (Inputs)button).wasReleasedThisFrame)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Was the specified control released in the current frame.
        /// </summary>
        /// <param name="button">A control from the JoystickButton enum.</param>
        /// <param name="slot">Which joystick to check for input. Default is 'All'.</param>
        /// <returns>True if the input was released in the current frame, otherwise false, including
        /// when 'slot' is specified and no joystick is connected to that slot.</returns>
        public static bool IsControlUp(JoystickButton button, JoystickSlot slot = JoystickSlot.All)
        {
            if (slot != JoystickSlot.All)
            {
                var joystick = s_Joysticks[(int)slot];
                if (joystick == null)
                    return false;

                var control = GetJoystickButtonControl(joystick, button);
                return control != null && control.wasReleasedThisFrame;
            }

            for (var i = 0; i < (int)JoystickSlot.Max; i++)
            {
                if (s_Joysticks[i] == null) continue;

                var control = GetJoystickButtonControl(s_Joysticks[i], button);
                if (control != null && control.wasReleasedThisFrame)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns actuation value for single analogue controls.
        /// </summary>
        /// <param name="input">Control from Inputs enum.</param>
        /// <returns>A value between 0 at un-actuated to 1 at fully actuated.</returns>
        public static float GetAxis(Inputs input)
        {
            var deviceType = GetDeviceTypeForInput(input);
            var maxValue = 0.0f;

            switch (deviceType)
            {
                case InputDeviceType.Keyboard:
                    foreach (var device in InputSystem.devices)
                    {
                        if (!(device is Keyboard keyboard)) continue;

                        maxValue = Mathf.Max(maxValue,
                            Mathf.Clamp01(GetKeyboardButtonControl(keyboard, input).ReadValue()));
                    }
                    break;
                case InputDeviceType.Mouse:
                    foreach (var device in InputSystem.devices)
                    {
                        if (!(device is Mouse mouse)) continue;

                        maxValue = Mathf.Max(maxValue,
                            Mathf.Clamp01(GetMouseButtonControl(mouse, input).ReadValue()));
                    }
                    break;
                case InputDeviceType.Gamepad:
                    foreach (var gamepad in s_Gamepads)
                    {
	                    if (gamepad == null) continue;

	                    maxValue = Mathf.Max(maxValue,
                            Mathf.Clamp01(GetGamepadButtonControl(gamepad, input).ReadValue()));
                    }
                    break;
                case InputDeviceType.Joystick:
                    foreach (var joystick in s_Joysticks)
                    {
	                    if (joystick == null) continue;

                        var control = GetJoystickButtonControl(joystick, input);
                        if (control == null) continue;

                        maxValue = Mathf.Max(maxValue, Mathf.Clamp01(control.ReadValue()));
                    }
                    break;
                case InputDeviceType.Invalid:
                default:
                    break;
            }

            return maxValue;
        }

        /// <summary>
        /// Turns any two inputs into an axis value between -1 and 1.
        /// </summary>
        /// <param name="negativeAxis">Control from Inputs enum.</param>
        /// <param name="positiveAxis">Control from Inputs enum.</param>
        /// <returns>Value from [-1, 1] inclusive. If both controls are fully actuated, 0 will be returned.</returns>
        public static float GetAxis(Inputs negativeAxis, Inputs positiveAxis)
        {
            return GetAxis(positiveAxis) - GetAxis(negativeAxis);
        }

        internal static Vector2 NormalizeAxis(Vector2 axis, float deadzone)
        {
            var currentMag = axis.magnitude;
            if (currentMag < deadzone)
                return Vector2.zero;
            var newMag = Mathf.InverseLerp(deadzone, 1.0f, currentMag);
            return axis * (newMag / currentMag);
        }

        /// <summary>
        /// Turns any four inputs into a normalized vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="up"></param>
        /// <param name="down"></param>
        /// <returns></returns>
        public static Vector2 GetAxis(Inputs left, Inputs right, Inputs up, Inputs down)
        {
            var axis = GetAxisRaw(left, right, up, down);
            return NormalizeAxis(axis, InputSystem.settings.defaultDeadzoneMin);
        }

        /// <summary>
        /// Turns any four inputs into an un-normalized vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="up"></param>
        /// <param name="down"></param>
        /// <returns></returns>
        public static Vector2 GetAxisRaw(Inputs left, Inputs right, Inputs up, Inputs down)
        {
            return new Vector2(GetAxis(left, right), GetAxis(down, up));
        }

        private static StickControl GetGamepadStickControl(Gamepad gamepad, GamepadAxis stick)
        {
            if (gamepad == null)
                return null;

            switch (stick)
            {
                case GamepadAxis.LeftStick: return gamepad.leftStick;
                case GamepadAxis.RightStick: return gamepad.rightStick;
            }

            throw new ArgumentException($"Unexpected GamepadAxis enum value '{stick}'");
        }

        /// <summary>
        /// Get the value of either stick on a specific gamepad, or maximum magnitude value from all gamepads if gamepadSlot is GamepadSlot.All.
        /// </summary>
        /// <param name="stick"></param>
        /// <param name="gamepadSlot">Read values from the gamepad in this slot, or maximum magnitude value from all gamepads if gamepadSlot is GamepadSlot.All.</param>
        /// <returns>A normalized Vector2 containing the actuation of the specified stick control.</returns>
        public static Vector2 GetAxis(GamepadAxis stick, GamepadSlot gamepadSlot = GamepadSlot.All)
        {
            var maxAxis = Vector2.zero;
            var maxAxisMagSquared = 0.0f;

            for (var i = 0; i < s_Gamepads.Length; i++)
            {
                var gamepad = s_Gamepads[i];
                if (gamepadSlot != GamepadSlot.All && i != (int)gamepadSlot)
                    continue;

                if (gamepad == null)
                    continue;

                var rawAxis = GetGamepadStickControl(gamepad, stick).ReadUnprocessedValue();
                var deadZone = GetGamepadStickDeadZone((GamepadSlot)i);
                var axis = NormalizeAxis(rawAxis, deadZone);
                var axisMaxSquared = axis.sqrMagnitude;
                if (axisMaxSquared >= maxAxisMagSquared)
                {
                    maxAxisMagSquared = axisMaxSquared;
                    maxAxis = axis;
                }
            }

            return maxAxis;
        }

        /// <summary>
        /// Get the value of the main axis from the joystick in the specified slot, or the
        /// maximum magnitude value from all joysticks if joystickSlot is JoystickSlot.All
        /// </summary>
        /// <param name="joystickSlot">Read the axis value from the joystick in this slot, or specify JoystickSlot.All
        /// to return the maximum magnitude from all joysticks main axes.</param>
        /// <param name="deadzone">A deadzone to apply to the joystick axis. Default is 0.125.</param>
        /// <returns>A normalized Vector2 with the value of the joystick X axis in the Vector2.x component and
        /// the Y axis in Vector2.y components. Both values are in the range [-1, 1].</returns>
        public static Vector2 GetAxis(JoystickSlot joystickSlot, float deadzone = kDefaultJoystickDeadzone)
        {
            var maxAxis = Vector2.zero;
            var maxAxisMagSquared = 0.0f;
            for (var i = 0; i < s_Joysticks.Length; i++)
            {
                var joystick = s_Joysticks[i];
                if (joystickSlot != JoystickSlot.All && i != (int)joystickSlot)
                    continue;

                var control = joystick?.stick;
                if (control == null)
                    continue;

                var rawAxis = control.ReadUnprocessedValue();
                var axis = NormalizeAxis(rawAxis, deadzone);
                var axisMaxSquared = axis.sqrMagnitude;
                if (axisMaxSquared >= maxAxisMagSquared)
                {
                    maxAxisMagSquared = axisMaxSquared;
                    maxAxis = axis;
                }
            }

            return maxAxis;
        }

        /// <summary>
        /// True if there is a connected gamepad in the indicated slot.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static bool IsGamepadConnected(GamepadSlot slot)
        {
	        if (slot != GamepadSlot.All) 
		        return s_Gamepads[(int)slot] != null;

	        foreach (var g in s_Gamepads)
	        {
		        if (g == null) return false;
	        }

	        return true;
        }

        /// <summary>
        /// Did the gamepad in the specified slot connect in this frame.
        /// </summary>
        /// <param name="slot">The gamepad slot to check.</param>
        /// <returns>True in the frame that the gamepad in the specified slot connected in.</returns>
        /// <remarks>
        /// Use this method when you have logic that you want to run once when a gamepad connects.
        /// </remarks>
        public static bool DidGamepadConnectThisFrame(GamepadSlot slot)
        {
	        if (slot != GamepadSlot.All) 
		        return s_GamepadsConnectedFrames[(int)slot] == Time.frameCount;

	        foreach (var frame in s_GamepadsConnectedFrames)
	        {
		        if (frame != Time.frameCount)
			        return false;
	        }

	        return true;
        }

        /// <summary>
        /// Did the gamepad in the specified slot disconnect in this frame.
        /// </summary>
        /// <param name="slot">The gamepad slot to check.</param>
        /// <returns>True in the frame that the gamepad in the specified slot disconnected in.</returns>
        /// <remarks>
        /// Use this method when you have logic that you want to run once when a gamepad disconnects, such as
        /// showing a reconnect message.
        /// </remarks>
        public static bool DidGamepadDisconnectThisFrame(GamepadSlot slot)
        {
	        if (slot != GamepadSlot.All) 
		        return s_GamepadsDisconnectedFrames[(int)slot] == Time.frameCount;

	        foreach (var frame in s_GamepadsDisconnectedFrames)
	        {
		        if (frame != Time.frameCount)
			        return false;
	        }

	        return true;
        }

        private static float GetGamepadTriggerPressPoint(GamepadSlot gamepadSlot)
        {
            switch (gamepadSlot)
            {
                case GamepadSlot.Slot1:
                case GamepadSlot.Slot2:
                case GamepadSlot.Slot3:
                case GamepadSlot.Slot4:
                case GamepadSlot.Slot5:
                case GamepadSlot.Slot6:
                case GamepadSlot.Slot7:
                case GamepadSlot.Slot8:
                case GamepadSlot.Slot9:
                case GamepadSlot.Slot10:
                case GamepadSlot.Slot11:
                case GamepadSlot.Slot12:
                    return s_GamepadConfigs[(int)gamepadSlot].TriggerPressPoint;
                case GamepadSlot.All:
                    throw new ArgumentException("Passing GamepadSlot.All is not valid for this operation");
                default:
                    throw new ArgumentOutOfRangeException(nameof(gamepadSlot), gamepadSlot, null);
            }
        }

        /// <summary>
        /// Set the value at which gamepad triggers need to be actuated before they will be considered pressed.
        /// </summary>
        /// <param name="pressPoint"></param>
        /// <param name="gamepadSlot"></param>
        /// <remarks>
        /// If this is set and a gamepad subsequently connects, the set values should also apply to that gamepad.
        /// </remarks>
        public static void SetGamepadTriggerPressPoint(float pressPoint, GamepadSlot gamepadSlot = GamepadSlot.All)
        {
            switch (gamepadSlot)
            {
                case GamepadSlot.Slot1:
                case GamepadSlot.Slot2:
                case GamepadSlot.Slot3:
                case GamepadSlot.Slot4:
                case GamepadSlot.Slot5:
                case GamepadSlot.Slot6:
                case GamepadSlot.Slot7:
                case GamepadSlot.Slot8:
                case GamepadSlot.Slot9:
                case GamepadSlot.Slot10:
                case GamepadSlot.Slot11:
                case GamepadSlot.Slot12:
                    s_GamepadConfigs[(int)gamepadSlot].TriggerPressPoint = pressPoint;
                    break;
                case GamepadSlot.All:
                    for (var i = 0; i < s_GamepadConfigs.Length; ++i)
                        s_GamepadConfigs[i].TriggerPressPoint = pressPoint;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gamepadSlot), gamepadSlot, null);
            }
        }

        private static float GetGamepadStickDeadZone(GamepadSlot gamepadSlot)
        {
            switch (gamepadSlot)
            {
                case GamepadSlot.Slot1:
                case GamepadSlot.Slot2:
                case GamepadSlot.Slot3:
                case GamepadSlot.Slot4:
                case GamepadSlot.Slot5:
                case GamepadSlot.Slot6:
                case GamepadSlot.Slot7:
                case GamepadSlot.Slot8:
                case GamepadSlot.Slot9:
                case GamepadSlot.Slot10:
                case GamepadSlot.Slot11:
                case GamepadSlot.Slot12:
                    return s_GamepadConfigs[(int)gamepadSlot].DeadZone;
                case GamepadSlot.All:
                    throw new ArgumentException("Passing GamepadSlot.All is not valid for this operation");
                default:
                    throw new ArgumentOutOfRangeException(nameof(gamepadSlot), gamepadSlot, null);
            }
        }

        /// <summary>
        /// Set the stick deadzone for both gamepad sticks.
        /// </summary>
        /// <param name="deadzone"></param>
        /// <param name="gamepadSlot"></param>
        public static void SetGamepadStickDeadzone(float deadzone, GamepadSlot gamepadSlot = GamepadSlot.All)
        {
            switch (gamepadSlot)
            {
                case GamepadSlot.Slot1:
                case GamepadSlot.Slot2:
                case GamepadSlot.Slot3:
                case GamepadSlot.Slot4:
                case GamepadSlot.Slot5:
                case GamepadSlot.Slot6:
                case GamepadSlot.Slot7:
                case GamepadSlot.Slot8:
                case GamepadSlot.Slot9:
                case GamepadSlot.Slot10:
                case GamepadSlot.Slot11:
                case GamepadSlot.Slot12:
                    s_GamepadConfigs[(int)gamepadSlot].DeadZone = deadzone;
                    break;
                case GamepadSlot.All:
                    for (var i = 0; i < s_GamepadConfigs.Length; ++i)
                        s_GamepadConfigs[i].DeadZone = deadzone;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gamepadSlot), gamepadSlot, null);
            }
        }

#if UNITY_EDITOR && UNITY_2020_2_OR_NEWER
        internal struct InputSystemPlayerLoopHighLevelTimeUpdate{}
        internal struct InputSystemPlayerLoopHighLevelEndFrame{}
#endif

        internal static void Initialize()
        {
            for (var i = 0; i < maxGamepadSlots; i++)
            {
                s_Gamepads[i] = null;
                s_GamepadsConnectedFrames[i] = -1;
                s_GamepadsDisconnectedFrames[i] = -1;
            }

            for (var i = 0; i < (int)JoystickSlot.Max; i++)
            {
                s_Joysticks[i] = null;
            }

            InputSystem.onDeviceChange -= OnDeviceChange;
            InputSystem.onDeviceChange += OnDeviceChange;

            InitializeGamepadConfigs();

            // add any existing gamepads
            foreach (var gamepad in Gamepad.all)
            {
                AddGamepadToFirstFreeSlot(gamepad);
            }

            foreach (var joystick in Joystick.all)
            {
                AddJoystickToFirstFreeSlot(joystick);
            }

#if UNITY_EDITOR && UNITY_2020_2_OR_NEWER
            // To support DidGamepadConnectThisFrame and DidGamepadDisconnectThisFrame, we make use of Time.frameCount
            // to record what frame a device was connected or disconnected on. A problem with this is that in the editor,
            // device connected events can come from an editor update that happens before Time.frameCount has been
            // updated for the frame, and from the managed side, we have no way to tell that we're inside that update.
            // An additional complication is that device connected events happen asynchronously on the native side, so
            // sometimes the event will come through the editor update and sometimes through the normal player loop.
            // So what we do is install two additional PlayerLoop systems, one to track when the Time.frameCount value has
            // been updated and one to reset at the end of the frame. If we end up adding a device before the frame
            // count has been updated, we manually add 1 to the frame count that we record.
            var playerLoop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            playerLoop.InsertSystemAsSubSystemOf<InputSystemPlayerLoopHighLevelTimeUpdate, TimeUpdate>(
                () => s_TimeHasUpdatedThisFrame = true);
            playerLoop.InsertSystemAsSubSystemOf<InputSystemPlayerLoopHighLevelEndFrame, PostLateUpdate>(
                () => s_TimeHasUpdatedThisFrame = false);
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(playerLoop);
#endif
            s_PointerPosition = Vector2.zero;
            s_ScrollDelta = Vector2.zero;

            s_PointerAction = new InputAction("Pointer", InputActionType.PassThrough, expectedControlType: "Vector2");
            s_PointerAction.AddBinding("<Pointer>/position");
            s_PointerAction.performed += OnPointerMoved;
            s_PointerAction.Enable();

            s_ScrollAction = new InputAction("Scroll", InputActionType.PassThrough, expectedControlType: "Vector2");
            s_ScrollAction.AddBinding("<Mouse>/scroll");
            s_ScrollAction.performed += OnMouseScrolled;
            s_ScrollAction.Enable();
        }

        internal static void Shutdown()
        {
            if (s_PointerAction != null)
            {
                s_PointerAction.performed -= OnPointerMoved;
                s_PointerAction.Disable();
                s_PointerAction = null;
            }

            if (s_ScrollAction != null)
            {
                s_ScrollAction.performed -= OnMouseScrolled;
                s_ScrollAction.Disable();
                s_ScrollAction = null;
            }

            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private static void OnPointerMoved(InputAction.CallbackContext context)
        {
            s_PointerPosition = context.ReadValue<Vector2>();
        }

        private static void OnMouseScrolled(InputAction.CallbackContext context)
        {
            s_ScrollDelta = context.ReadValue<Vector2>();
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)
            {
                AddGamepadToFirstFreeSlot(device);
                AddJoystickToFirstFreeSlot(device);
            }
            else if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            {
                RemoveGamepad(device);
                RemoveJoystick(device);
            }
        }

        private static void AddGamepadToFirstFreeSlot(InputDevice device)
        {
            if (!(device is Gamepad gamepad)) return;

            for (var i = 0; i < maxGamepadSlots; i++)
            {
                if (s_Gamepads[i] != null) continue;

                var frameCount = Time.frameCount;
#if UNITY_EDITOR && UNITY_2020_2_OR_NEWER
                if (!s_TimeHasUpdatedThisFrame)
                    frameCount += 1;
#endif
                s_Gamepads[i] = gamepad;
                s_GamepadsConnectedFrames[i] = frameCount;
                break;
            }
        }

        private static void RemoveGamepad(InputDevice device)
        {
            for (var i = 0; i < maxGamepadSlots; i++)
            {
                if (s_Gamepads[i] != device) continue;

                s_Gamepads[i] = null;

                var frameCount = Time.frameCount;
#if UNITY_EDITOR && UNITY_2020_2_OR_NEWER
                if (!s_TimeHasUpdatedThisFrame)
                    frameCount += 1;
#endif
                s_GamepadsDisconnectedFrames[i] = frameCount;
                break;
            }
        }

        private static void AddJoystickToFirstFreeSlot(InputDevice device)
        {
            if (!(device is Joystick joystick)) return;

            for (var i = 0; i < (int)JoystickSlot.Max; i++)
            {
                if (s_Joysticks[i] != null) continue;

                s_Joysticks[i] = joystick;
                break;
            }
        }

        private static void RemoveJoystick(InputDevice device)
        {
            for (var i = 0; i < (int)JoystickSlot.Max; i++)
            {
                if (s_Joysticks[i] != device) continue;

                s_Joysticks[i] = null;
            }
        }
    }
}
