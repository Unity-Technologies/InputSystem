using System;
using System.Linq;
using UnityEngine.InputSystem.Controls;

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
                case Inputs.Key_RightBracket:
                    return k[Key.RightBracket];
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
                case Inputs.Key_NumpadDivide:
                    return k[Key.NumpadDivide];
                case Inputs.Key_NumpadMultiply:
                    return k[Key.NumpadMultiply];
                case Inputs.Key_NumpadPlus: return k[Key.NumpadPlus];
                case Inputs.Key_NumpadMinus: return k[Key.NumpadMinus];
                case Inputs.Key_NumpadPeriod:
                    return k[Key.NumpadPeriod];
                case Inputs.Key_NumpadEquals:
                    return k[Key.NumpadEquals];
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
        
        private static ButtonControl GetGamepadButtonControl(Gamepad g, Inputs input)
        {
            if (g == null)
                return null;
            switch (input)
            {
                case Inputs.Gamepad_DpadUp: return g.dpad.up;
                case Inputs.Gamepad_DpadDown: return g.dpad.down;
                case Inputs.Gamepad_DpadLeft: return g.dpad.left;
                case Inputs.Gamepad_DpadRight: return g.dpad.right;
                case Inputs.Gamepad_North: return g.buttonNorth;
                case Inputs.Gamepad_East: return g.buttonEast;
                case Inputs.Gamepad_South: return g.buttonSouth;
                case Inputs.Gamepad_West: return g.buttonWest;
                case Inputs.Gamepad_LeftStickButton:
                    return g.leftStickButton;
                case Inputs.Gamepad_RightStickButton:
                    return g.rightStickButton;
                case Inputs.Gamepad_LeftShoulder: return g.leftShoulder;
                case Inputs.Gamepad_RightShoulder:
                    return g.rightShoulder;
                case Inputs.Gamepad_LeftStickUp: return g.leftStick.up;
                case Inputs.Gamepad_LeftStickDown:
                    return g.leftStick.down;
                case Inputs.Gamepad_LeftStickLeft:
                    return g.leftStick.left;
                case Inputs.Gamepad_LeftStickRight:
                    return g.leftStick.right;
                case Inputs.Gamepad_RightStickUp: return g.rightStick.up;
                case Inputs.Gamepad_RightStickDown:
                    return g.rightStick.down;
                case Inputs.Gamepad_RightStickLeft:
                    return g.rightStick.left;
                case Inputs.Gamepad_RightStickRight:
                    return g.rightStick.right;
                case Inputs.Gamepad_LeftTrigger: return g.leftTrigger;
                case Inputs.Gamepad_RightTrigger: return g.rightTrigger;
                case Inputs.Gamepad_Start: return g.startButton;
                case Inputs.Gamepad_Select: return g.selectButton;
                default: return null;
            }
        }

        private static ButtonControl GetJoystickButtonControl(Joystick j, Inputs input)
        {
            if (j == null)
                return null;
            switch (input)
            {
                case Inputs.Joystick_Trigger: return j.trigger;
                default: return null;
            }
        }

        /// <summary>
        ///     Is the indicated control currently pressed.
        /// </summary>
        /// <param name="input">Control from Inputs enum.</param>
        /// <returns>True if control is currently pressed, false if control is not pressed or not available (device disconnected, etc).</returns>
        /// <remarks>
        ///     This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        ///     and return true if the control is currently pressed on any of them.
        /// </remarks>
        public static bool IsControlPressed(Inputs input)
        {
            var deviceType = GetDeviceTypeForInput(input);

            switch (deviceType)
            {
                case InputDeviceType.Keyboard:
                    return InputSystem.devices
                        .OfType<Keyboard>()
                        .Any(x => GetKeyboardButtonControl(x, input).isPressed);
                case InputDeviceType.Mouse:
                    return InputSystem.devices
                        .OfType<Mouse>()
                        .Any(x => GetMouseButtonControl(x, input).isPressed);
                case InputDeviceType.Gamepad:
                    return InputSystem.devices
                        .OfType<Gamepad>()
                        .Any(x => GetGamepadButtonControl(x, input).isPressed);
                case InputDeviceType.Joystick:
                    return InputSystem.devices
                        .OfType<Joystick>()
                        .Any(x => GetJoystickButtonControl(x, input).isPressed);
                case InputDeviceType.Invalid:
                default:
                    return false;
            }
        }

        /// <summary>
        ///     True in the frame that the input was pressed.
        /// </summary>
        /// <param name="input">Control from Inputs enum.</param>
        /// <returns>True if control was actuated in the current frame, false if control was not actuated or not available (device disconnected, etc).</returns>
        /// <remarks>
        ///     This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        ///     and return true if the control was actuated in the current frame on any of them.
        /// </remarks>
        public static bool IsControlDown(Inputs input)
        {
            var deviceType = GetDeviceTypeForInput(input);

            switch (deviceType)
            {
                case InputDeviceType.Keyboard:
                    return InputSystem.devices
                        .OfType<Keyboard>()
                        .Any(x => GetKeyboardButtonControl(x, input).wasPressedThisFrame);
                case InputDeviceType.Mouse:
                    return InputSystem.devices
                        .OfType<Mouse>()
                        .Any(x => GetMouseButtonControl(x, input).wasPressedThisFrame);
                case InputDeviceType.Gamepad:
                    return InputSystem.devices
                        .OfType<Gamepad>()
                        .Any(x => GetGamepadButtonControl(x, input).wasPressedThisFrame);
                case InputDeviceType.Joystick:
                    return InputSystem.devices
                        .OfType<Joystick>()
                        .Any(x => GetJoystickButtonControl(x, input).wasPressedThisFrame);
                case InputDeviceType.Invalid:
                default:
                    return false;
            }
        }

        /// <summary>
        ///     True in the frame that the input was released.
        /// </summary>
        /// <param name="input">Control from Inputs enum.</param>
        /// <returns>True if control was released in the current frame, false if control was not released or not available (device disconnected, etc).</returns>
        /// <remarks>
        ///     This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        ///     and return true if the control was released in the current frame on any of them.
        /// </remarks>
        public static bool IsControlUp(Inputs input)
        {            var deviceType = GetDeviceTypeForInput(input);

            switch (deviceType)
            {
                case InputDeviceType.Keyboard:
                    return InputSystem.devices
                        .OfType<Keyboard>()
                        .Any(x => GetKeyboardButtonControl(x, input).wasReleasedThisFrame);
                case InputDeviceType.Mouse:
                    return InputSystem.devices
                        .OfType<Mouse>()
                        .Any(x => GetMouseButtonControl(x, input).wasReleasedThisFrame);
                case InputDeviceType.Gamepad:
                    return InputSystem.devices
                        .OfType<Gamepad>()
                        .Any(x => GetGamepadButtonControl(x, input).wasReleasedThisFrame);
                case InputDeviceType.Joystick:
                    return InputSystem.devices
                        .OfType<Joystick>()
                        .Any(x => GetJoystickButtonControl(x, input).wasReleasedThisFrame);
                case InputDeviceType.Invalid:
                default:
                    return false;
            }
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