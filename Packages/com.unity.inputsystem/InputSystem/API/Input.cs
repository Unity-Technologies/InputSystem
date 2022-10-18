using System;
using UnityEngine.InputSystem.Controls;

// TODO rename HighLevel to something that makes sense
namespace UnityEngine.InputSystem.HighLevel
{
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

        Gamepad_LeftStickButton =
            30009, // TODO figure out a better name, because just LeftStick is poor, e.g. Input.IsControlDown(Input.Gamepad_LeftStick) in plain english implies left stick is moved down
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

        Joystick_Trigger = 40001
    }

// RE-ENABLE ME WHEN YOU GONNA IMPLEMENT ME
#if false
    public enum GamepadAxis
    {
        LeftStick,
        RightStick
    }

    public enum GamepadButton
    {
        DpadUp,
        DpadDown,
        DpadLeft,
        DpadRight,
        North,
        East,
        South,
        West,
        LeftStick,  // left stick pressed
        RightStick, // right stick pressed
        LeftShoulder,
        RightShoulder,
        LeftTrigger,
        RightTrigger,
        Start,
        Select,
        X = West,
        Y = North,
        A = South,
        B = East,
        Cross = South,
        Square = West,
        Triangle = North,
        Circle = East
    }

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
        All = Int32.MaxValue,
        Any = Int32.MaxValue
    }

    public enum JoystickSlot
    {
        Slot1 = 0,
        Slot2,
        Slot3,
        Slot4,
        All = Int32.MaxValue,
        Any = Int32.MaxValue
    }

#endif

    public static class Input
    {
// RE-ENABLE ME WHEN YOU GONNA IMPLEMENT ME
#if false
        // These device collections use "stable" indexing. If a device that was connected disconnects, the
        // array index that it occupied remains empty until the same device or another device of the same type
        // connects. This is to enable applications to abstract the concept of a device index and work at a
        // higher level of "slot" index. This is in contrast to some of the the lower level device collections,
        // Gamepads.all for example, where the collection is compacted when a device disconnects, and new devices,
        // and even reconnecting devices, are added to the end of the collection.

        /// <summary>
        /// A collection of all keyboards currently connected to the system.
        /// </summary>
        public static IReadOnlyList<Keyboard> keyboards { get; }

        /// <summary>
        /// A collection of all mice currently connected to the system.
        /// </summary>
        public static IReadOnlyList<Mouse> mice { get; }

        /// <summary>
        /// A collection of all joysticks currently connected to the system.
        /// </summary>
        public static IReadOnlyList<Joystick> joysticks { get; }

        /// <summary>
        /// A collection of all gamepads currently connected to the system.
        /// </summary>
        public static IReadOnlyList<Gamepad> gamepads { get; }

#endif

        private static ButtonControl GetButtonControl(Inputs input)
        {
            // WHAT THE FCK!? or .. can we do better?
            switch (input)
            {
                case Inputs.Key_Space: return Keyboard.current != null ? Keyboard.current[Key.Space] : null;
                case Inputs.Key_Enter: return Keyboard.current != null ? Keyboard.current[Key.Enter] : null;
                case Inputs.Key_Tab: return Keyboard.current != null ? Keyboard.current[Key.Tab] : null;
                case Inputs.Key_Backquote: return Keyboard.current != null ? Keyboard.current[Key.Backquote] : null;
                case Inputs.Key_Quote: return Keyboard.current != null ? Keyboard.current[Key.Quote] : null;
                case Inputs.Key_Semicolon: return Keyboard.current != null ? Keyboard.current[Key.Semicolon] : null;
                case Inputs.Key_Comma: return Keyboard.current != null ? Keyboard.current[Key.Comma] : null;
                case Inputs.Key_Period: return Keyboard.current != null ? Keyboard.current[Key.Period] : null;
                case Inputs.Key_Slash: return Keyboard.current != null ? Keyboard.current[Key.Slash] : null;
                case Inputs.Key_Backslash: return Keyboard.current != null ? Keyboard.current[Key.Backslash] : null;
                case Inputs.Key_LeftBracket: return Keyboard.current != null ? Keyboard.current[Key.LeftBracket] : null;
                case Inputs.Key_RightBracket:
                    return Keyboard.current != null ? Keyboard.current[Key.RightBracket] : null;
                case Inputs.Key_Minus: return Keyboard.current != null ? Keyboard.current[Key.Minus] : null;
                case Inputs.Key_Equals: return Keyboard.current != null ? Keyboard.current[Key.Equals] : null;
                case Inputs.Key_A: return Keyboard.current != null ? Keyboard.current[Key.A] : null;
                case Inputs.Key_B: return Keyboard.current != null ? Keyboard.current[Key.B] : null;
                case Inputs.Key_C: return Keyboard.current != null ? Keyboard.current[Key.C] : null;
                case Inputs.Key_D: return Keyboard.current != null ? Keyboard.current[Key.D] : null;
                case Inputs.Key_E: return Keyboard.current != null ? Keyboard.current[Key.E] : null;
                case Inputs.Key_F: return Keyboard.current != null ? Keyboard.current[Key.F] : null;
                case Inputs.Key_G: return Keyboard.current != null ? Keyboard.current[Key.G] : null;
                case Inputs.Key_H: return Keyboard.current != null ? Keyboard.current[Key.H] : null;
                case Inputs.Key_I: return Keyboard.current != null ? Keyboard.current[Key.I] : null;
                case Inputs.Key_J: return Keyboard.current != null ? Keyboard.current[Key.J] : null;
                case Inputs.Key_K: return Keyboard.current != null ? Keyboard.current[Key.K] : null;
                case Inputs.Key_L: return Keyboard.current != null ? Keyboard.current[Key.L] : null;
                case Inputs.Key_M: return Keyboard.current != null ? Keyboard.current[Key.M] : null;
                case Inputs.Key_N: return Keyboard.current != null ? Keyboard.current[Key.N] : null;
                case Inputs.Key_O: return Keyboard.current != null ? Keyboard.current[Key.O] : null;
                case Inputs.Key_P: return Keyboard.current != null ? Keyboard.current[Key.P] : null;
                case Inputs.Key_Q: return Keyboard.current != null ? Keyboard.current[Key.Q] : null;
                case Inputs.Key_R: return Keyboard.current != null ? Keyboard.current[Key.R] : null;
                case Inputs.Key_S: return Keyboard.current != null ? Keyboard.current[Key.S] : null;
                case Inputs.Key_T: return Keyboard.current != null ? Keyboard.current[Key.T] : null;
                case Inputs.Key_U: return Keyboard.current != null ? Keyboard.current[Key.U] : null;
                case Inputs.Key_V: return Keyboard.current != null ? Keyboard.current[Key.V] : null;
                case Inputs.Key_W: return Keyboard.current != null ? Keyboard.current[Key.W] : null;
                case Inputs.Key_X: return Keyboard.current != null ? Keyboard.current[Key.X] : null;
                case Inputs.Key_Y: return Keyboard.current != null ? Keyboard.current[Key.Y] : null;
                case Inputs.Key_Z: return Keyboard.current != null ? Keyboard.current[Key.Z] : null;
                case Inputs.Key_Digit1: return Keyboard.current != null ? Keyboard.current[Key.Digit1] : null;
                case Inputs.Key_Digit2: return Keyboard.current != null ? Keyboard.current[Key.Digit2] : null;
                case Inputs.Key_Digit3: return Keyboard.current != null ? Keyboard.current[Key.Digit3] : null;
                case Inputs.Key_Digit4: return Keyboard.current != null ? Keyboard.current[Key.Digit4] : null;
                case Inputs.Key_Digit5: return Keyboard.current != null ? Keyboard.current[Key.Digit5] : null;
                case Inputs.Key_Digit6: return Keyboard.current != null ? Keyboard.current[Key.Digit6] : null;
                case Inputs.Key_Digit7: return Keyboard.current != null ? Keyboard.current[Key.Digit7] : null;
                case Inputs.Key_Digit8: return Keyboard.current != null ? Keyboard.current[Key.Digit8] : null;
                case Inputs.Key_Digit9: return Keyboard.current != null ? Keyboard.current[Key.Digit9] : null;
                case Inputs.Key_Digit0: return Keyboard.current != null ? Keyboard.current[Key.Digit0] : null;
                case Inputs.Key_LeftShift: return Keyboard.current != null ? Keyboard.current[Key.LeftShift] : null;
                case Inputs.Key_RightShift: return Keyboard.current != null ? Keyboard.current[Key.RightShift] : null;
                case Inputs.Key_LeftAlt: return Keyboard.current != null ? Keyboard.current[Key.LeftAlt] : null;
                case Inputs.Key_RightAlt: return Keyboard.current != null ? Keyboard.current[Key.RightAlt] : null;
                case Inputs.Key_LeftCtrl: return Keyboard.current != null ? Keyboard.current[Key.LeftCtrl] : null;
                case Inputs.Key_RightCtrl: return Keyboard.current != null ? Keyboard.current[Key.RightCtrl] : null;
                case Inputs.Key_LeftMeta: return Keyboard.current != null ? Keyboard.current[Key.LeftMeta] : null;
                case Inputs.Key_RightMeta: return Keyboard.current != null ? Keyboard.current[Key.RightMeta] : null;
                case Inputs.Key_ContextMenu: return Keyboard.current != null ? Keyboard.current[Key.ContextMenu] : null;
                case Inputs.Key_Escape: return Keyboard.current != null ? Keyboard.current[Key.Escape] : null;
                case Inputs.Key_LeftArrow: return Keyboard.current != null ? Keyboard.current[Key.LeftArrow] : null;
                case Inputs.Key_RightArrow: return Keyboard.current != null ? Keyboard.current[Key.RightArrow] : null;
                case Inputs.Key_UpArrow: return Keyboard.current != null ? Keyboard.current[Key.UpArrow] : null;
                case Inputs.Key_DownArrow: return Keyboard.current != null ? Keyboard.current[Key.DownArrow] : null;
                case Inputs.Key_Backspace: return Keyboard.current != null ? Keyboard.current[Key.Backspace] : null;
                case Inputs.Key_PageDown: return Keyboard.current != null ? Keyboard.current[Key.PageDown] : null;
                case Inputs.Key_PageUp: return Keyboard.current != null ? Keyboard.current[Key.PageUp] : null;
                case Inputs.Key_Home: return Keyboard.current != null ? Keyboard.current[Key.Home] : null;
                case Inputs.Key_End: return Keyboard.current != null ? Keyboard.current[Key.End] : null;
                case Inputs.Key_Insert: return Keyboard.current != null ? Keyboard.current[Key.Insert] : null;
                case Inputs.Key_Delete: return Keyboard.current != null ? Keyboard.current[Key.Delete] : null;
                case Inputs.Key_CapsLock: return Keyboard.current != null ? Keyboard.current[Key.CapsLock] : null;
                case Inputs.Key_NumLock: return Keyboard.current != null ? Keyboard.current[Key.NumLock] : null;
                case Inputs.Key_PrintScreen: return Keyboard.current != null ? Keyboard.current[Key.PrintScreen] : null;
                case Inputs.Key_ScrollLock: return Keyboard.current != null ? Keyboard.current[Key.ScrollLock] : null;
                case Inputs.Key_Pause: return Keyboard.current != null ? Keyboard.current[Key.Pause] : null;
                case Inputs.Key_NumpadEnter: return Keyboard.current != null ? Keyboard.current[Key.NumpadEnter] : null;
                case Inputs.Key_NumpadDivide:
                    return Keyboard.current != null ? Keyboard.current[Key.NumpadDivide] : null;
                case Inputs.Key_NumpadMultiply:
                    return Keyboard.current != null ? Keyboard.current[Key.NumpadMultiply] : null;
                case Inputs.Key_NumpadPlus: return Keyboard.current != null ? Keyboard.current[Key.NumpadPlus] : null;
                case Inputs.Key_NumpadMinus: return Keyboard.current != null ? Keyboard.current[Key.NumpadMinus] : null;
                case Inputs.Key_NumpadPeriod:
                    return Keyboard.current != null ? Keyboard.current[Key.NumpadPeriod] : null;
                case Inputs.Key_NumpadEquals:
                    return Keyboard.current != null ? Keyboard.current[Key.NumpadEquals] : null;
                case Inputs.Key_Numpad0: return Keyboard.current != null ? Keyboard.current[Key.Numpad0] : null;
                case Inputs.Key_Numpad1: return Keyboard.current != null ? Keyboard.current[Key.Numpad1] : null;
                case Inputs.Key_Numpad2: return Keyboard.current != null ? Keyboard.current[Key.Numpad2] : null;
                case Inputs.Key_Numpad3: return Keyboard.current != null ? Keyboard.current[Key.Numpad3] : null;
                case Inputs.Key_Numpad4: return Keyboard.current != null ? Keyboard.current[Key.Numpad4] : null;
                case Inputs.Key_Numpad5: return Keyboard.current != null ? Keyboard.current[Key.Numpad5] : null;
                case Inputs.Key_Numpad6: return Keyboard.current != null ? Keyboard.current[Key.Numpad6] : null;
                case Inputs.Key_Numpad7: return Keyboard.current != null ? Keyboard.current[Key.Numpad7] : null;
                case Inputs.Key_Numpad8: return Keyboard.current != null ? Keyboard.current[Key.Numpad8] : null;
                case Inputs.Key_Numpad9: return Keyboard.current != null ? Keyboard.current[Key.Numpad9] : null;
                case Inputs.Key_F1: return Keyboard.current != null ? Keyboard.current[Key.F1] : null;
                case Inputs.Key_F2: return Keyboard.current != null ? Keyboard.current[Key.F2] : null;
                case Inputs.Key_F3: return Keyboard.current != null ? Keyboard.current[Key.F3] : null;
                case Inputs.Key_F4: return Keyboard.current != null ? Keyboard.current[Key.F4] : null;
                case Inputs.Key_F5: return Keyboard.current != null ? Keyboard.current[Key.F5] : null;
                case Inputs.Key_F6: return Keyboard.current != null ? Keyboard.current[Key.F6] : null;
                case Inputs.Key_F7: return Keyboard.current != null ? Keyboard.current[Key.F7] : null;
                case Inputs.Key_F8: return Keyboard.current != null ? Keyboard.current[Key.F8] : null;
                case Inputs.Key_F9: return Keyboard.current != null ? Keyboard.current[Key.F9] : null;
                case Inputs.Key_F10: return Keyboard.current != null ? Keyboard.current[Key.F10] : null;
                case Inputs.Key_F11: return Keyboard.current != null ? Keyboard.current[Key.F11] : null;
                case Inputs.Key_F12: return Keyboard.current != null ? Keyboard.current[Key.F12] : null;
                case Inputs.Key_OEM1: return Keyboard.current != null ? Keyboard.current[Key.OEM1] : null;
                case Inputs.Key_OEM2: return Keyboard.current != null ? Keyboard.current[Key.OEM2] : null;
                case Inputs.Key_OEM3: return Keyboard.current != null ? Keyboard.current[Key.OEM3] : null;
                case Inputs.Key_OEM4: return Keyboard.current != null ? Keyboard.current[Key.OEM4] : null;
                case Inputs.Key_OEM5: return Keyboard.current != null ? Keyboard.current[Key.OEM5] : null;

                case Inputs.Mouse_Left: return Mouse.current != null ? Mouse.current.leftButton : null;
                case Inputs.Mouse_Right: return Mouse.current != null ? Mouse.current.rightButton : null;
                case Inputs.Mouse_Middle: return Mouse.current != null ? Mouse.current.middleButton : null;
                case Inputs.Mouse_Forward: return Mouse.current != null ? Mouse.current.forwardButton : null;
                case Inputs.Mouse_Back: return Mouse.current != null ? Mouse.current.backButton : null;

                case Inputs.Gamepad_DpadUp: return Gamepad.current != null ? Gamepad.current.dpad.up : null;
                case Inputs.Gamepad_DpadDown: return Gamepad.current != null ? Gamepad.current.dpad.down : null;
                case Inputs.Gamepad_DpadLeft: return Gamepad.current != null ? Gamepad.current.dpad.left : null;
                case Inputs.Gamepad_DpadRight: return Gamepad.current != null ? Gamepad.current.dpad.right : null;
                case Inputs.Gamepad_North: return Gamepad.current != null ? Gamepad.current.buttonNorth : null;
                case Inputs.Gamepad_East: return Gamepad.current != null ? Gamepad.current.buttonEast : null;
                case Inputs.Gamepad_South: return Gamepad.current != null ? Gamepad.current.buttonSouth : null;
                case Inputs.Gamepad_West: return Gamepad.current != null ? Gamepad.current.buttonWest : null;
                case Inputs.Gamepad_LeftStickButton:
                    return Gamepad.current != null ? Gamepad.current.leftStickButton : null;
                case Inputs.Gamepad_RightStickButton:
                    return Gamepad.current != null ? Gamepad.current.rightStickButton : null;
                case Inputs.Gamepad_LeftShoulder: return Gamepad.current != null ? Gamepad.current.leftShoulder : null;
                case Inputs.Gamepad_RightShoulder:
                    return Gamepad.current != null ? Gamepad.current.rightShoulder : null;
                case Inputs.Gamepad_LeftStickUp: return Gamepad.current != null ? Gamepad.current.leftStick.up : null;
                case Inputs.Gamepad_LeftStickDown:
                    return Gamepad.current != null ? Gamepad.current.leftStick.down : null;
                case Inputs.Gamepad_LeftStickLeft:
                    return Gamepad.current != null ? Gamepad.current.leftStick.left : null;
                case Inputs.Gamepad_LeftStickRight:
                    return Gamepad.current != null ? Gamepad.current.leftStick.right : null;
                case Inputs.Gamepad_RightStickUp: return Gamepad.current != null ? Gamepad.current.rightStick.up : null;
                case Inputs.Gamepad_RightStickDown:
                    return Gamepad.current != null ? Gamepad.current.rightStick.down : null;
                case Inputs.Gamepad_RightStickLeft:
                    return Gamepad.current != null ? Gamepad.current.rightStick.left : null;
                case Inputs.Gamepad_RightStickRight:
                    return Gamepad.current != null ? Gamepad.current.rightStick.right : null;
                case Inputs.Gamepad_LeftTrigger: return Gamepad.current != null ? Gamepad.current.leftTrigger : null;
                case Inputs.Gamepad_RightTrigger: return Gamepad.current != null ? Gamepad.current.rightTrigger : null;
                case Inputs.Gamepad_Start: return Gamepad.current != null ? Gamepad.current.startButton : null;
                case Inputs.Gamepad_Select: return Gamepad.current != null ? Gamepad.current.selectButton : null;
                
                case Inputs.Joystick_Trigger: return Joystick.current != null ? Joystick.current.trigger : null;

                // TODO check do we need this for 2019.4?
                // default: return null;
            }

            throw new ArgumentException($"Unexpected Inputs enum value '{input}'");
        }
        
        // TODO consider overloads instead of one huge enum

        /// <summary>
        ///     Is the indicated control currently pressed.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <remarks>
        ///     This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        ///     and return true if the control is currently pressed on any of them.
        /// </remarks>
        public static bool IsControlPressed(Inputs input)
        {
            return GetButtonControl(input)?.isPressed ?? false;
        }

        /// <summary>
        ///     True in the frame that the input was pressed.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <remarks>
        ///     This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        ///     and return true if the control was actuated in the current frame on any of them.
        /// </remarks>
        public static bool IsControlDown(Inputs input)
        {
            return GetButtonControl(input)?.wasPressedThisFrame ?? false;
        }

        /// <summary>
        ///     True in the frame that the input was released.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <remarks>
        ///     This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        ///     and return true if the control was released in the current frame on any of them.
        /// </remarks>
        public static bool IsControlUp(Inputs input)
        {
            return GetButtonControl(input)?.wasReleasedThisFrame ?? false;
        }

        // RE-ENABLE ME WHEN YOU GONNA IMPLEMENT ME
#if false
        /// <summary>
        /// For single analogue inputs
        /// </summary>
        /// <param name="input"></param>
        /// <returns>A value between 0 at un-actuated to 1 at fully actuated.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static float GetAxis(Inputs input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Turns any two inputs into an axis value between -1 and 1.
        /// </summary>
        /// <param name="negativeAxis"></param>
        /// <param name="positiveAxis"></param>
        /// <returns></returns>
        public static float GetAxis(Inputs negativeAxis, Inputs positiveAxis)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the value of either stick on a specific gamepad, or any gamepad if gamepadSlot is Any.
        /// </summary>
        /// <param name="stick"></param>
        /// <param name="gamepadSlot">Read values from the gamepad in this slot, or any gamepad if GamepadSlot.Any is specified.</param>
        /// <returns></returns>
        public static Vector2 GetAxis(GamepadAxis stick, GamepadSlot gamepadSlot = GamepadSlot.Any)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the value of the main axis on the joystick at index joystickIndex.
        /// </summary>
        /// <param name="joystickIndex"></param>
        /// <returns></returns>
        /// <remarks>
        /// Joystick slot must always be specified
        /// </remarks>
        public static Vector2 GetJoystickAxis(int joystickAxis, JoystickSlot joystickSlot = JoystickSlot.Any)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True when the button is held.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="gamepadSlot"></param>
        /// <returns></returns>
        public static bool IsGamepadButtonPressed(GamepadButton button, GamepadSlot gamepadSlot = GamepadSlot.Any)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True in the frame the button was pressed.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="gamepadSlot"></param>
        /// <returns></returns>
        public static bool IsGamepadButtonDown(GamepadButton button, GamepadSlot gamepadSlot = GamepadSlot.Any)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True in the frame the button was released.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="gamepadSlot"></param>
        /// <returns></returns>
        public static bool IsGamepadButtonUp(GamepadButton input, GamepadSlot gamepadSlot = GamepadSlot.Any)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set the stick deadzone for both gamepad sticks.
        /// </summary>
        /// <param name="deadzone"></param>
        /// <param name="gamepadSlot"></param>
        public static void SetGamepadStickDeadzone(float deadzone, GamepadSlot gamepadSlot = GamepadSlot.All)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True if there is a connected gamepad in the indicated slot.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static bool IsGamepadConnected(GamepadSlot slot)
        {
            throw new NotImplementedException();
        }

        public static Vector2 mousePosition { get; }
        public static bool mousePresent { get; }
        public static float mouseScrollDelta { get; }

#endif
    }
}