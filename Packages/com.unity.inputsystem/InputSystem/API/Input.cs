using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.HighLevel
{
    public enum Inputs
    {
        Key_Space,
        Key_Enter,
        Key_Tab,
        Key_Backquote,
        Key_Quote,
        Key_Semicolon,
        Key_Comma,
        Key_Period,
        Key_Slash,
        Key_Backslash,
        Key_LeftBracket,
        Key_RightBracket,
        Key_Minus,
        Key_Equals,
        Key_A,
        Key_B,
        Key_C,
        Key_D,
        Key_E,
        Key_F,
        Key_G,
        Key_H,
        Key_I,
        Key_J,
        Key_K,
        Key_L,
        Key_M,
        Key_N,
        Key_O,
        Key_P,
        Key_Q,
        Key_R,
        Key_S,
        Key_T,
        Key_U,
        Key_V,
        Key_W,
        Key_X,
        Key_Y,
        Key_Z,
        Key_Digit1,
        Key_Digit2,
        Key_Digit3,
        Key_Digit4,
        Key_Digit5,
        Key_Digit6,
        Key_Digit7,
        Key_Digit8,
        Key_Digit9,
        Key_Digit0,
        Key_LeftShift,
        Key_RightShift,
        Key_LeftAlt,
        Key_RightAlt,
        Key_AltGr = Key_RightAlt,
        Key_LeftCtrl,
        Key_RightCtrl,
        Key_LeftMeta,
        Key_RightMeta,
        Key_LeftWindows = Key_LeftMeta,
        Key_RightWindows = Key_RightMeta,
        Key_LeftApple = Key_LeftMeta,
        Key_RightApple = Key_RightMeta,
        Key_LeftCommand = Key_LeftMeta,
        Key_RightCommand = Key_RightMeta,
        Key_ContextMenu,
        Key_Escape,
        Key_LeftArrow,
        Key_RightArrow,
        Key_UpArrow,
        Key_DownArrow,
        Key_Backspace,
        Key_PageDown,
        Key_PageUp,
        Key_Home,
        Key_End,
        Key_Insert,
        Key_Delete,
        Key_CapsLock,
        Key_NumLock,
        Key_PrintScreen,
        Key_ScrollLock,
        Key_Pause,
        Key_NumpadEnter,
        Key_NumpadDivide,
        Key_NumpadMultiply,
        Key_NumpadPlus,
        Key_NumpadMinus,
        Key_NumpadPeriod,
        Key_NumpadEquals,
        Key_Numpad0,
        Key_Numpad1,
        Key_Numpad2,
        Key_Numpad3,
        Key_Numpad4,
        Key_Numpad5,
        Key_Numpad6,
        Key_Numpad7,
        Key_Numpad8,
        Key_Numpad9,
        Key_F1,
        Key_F2,
        Key_F3,
        Key_F4,
        Key_F5,
        Key_F6,
        Key_F7,
        Key_F8,
        Key_F9,
        Key_F10,
        Key_F11,
        Key_F12,
        Key_OEM1,
        Key_OEM2,
        Key_OEM3,
        Key_OEM4,
        Key_OEM5,


        Mouse_Left,
        Mouse_Right,
        Mouse_Middle,
        Mouse_Forward,
        Mouse_Back,


        Gamepad_DpadUp,
        Gamepad_DpadDown,
        Gamepad_DpadLeft,
        Gamepad_DpadRight,
        Gamepad_North,
        Gamepad_East,
        Gamepad_South,
        Gamepad_West,
        Gamepad_LeftStick,  // left stick pressed
        Gamepad_RightStick, // right stick pressed
        Gamepad_LeftStickX,
        Gamepad_LeftStickY,
        Gamepad_RightStickX,
        Gamepad_RightStickY,
        Gamepad_LeftShoulder,
        Gamepad_RightShoulder,
        Gamepad_LeftTrigger,
        Gamepad_RightTrigger,
        Gamepad_Start,
        Gamepad_Select,
        Gamepad_X = Gamepad_West,
        Gamepad_Y = Gamepad_North,
        Gamepad_A = Gamepad_South,
        Gamepad_B = Gamepad_East,
        Gamepad_Cross = Gamepad_South,
        Gamepad_Square = Gamepad_West,
        Gamepad_Triangle = Gamepad_North,
        Gamepad_Circle = Gamepad_East,

        Joystick_Trigger
    }

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

    public static partial class Input
    {
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
        /// <summary>
        /// Is the indicated control currently pressed.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <remarks>
        /// This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        /// and return true if the control is currently pressed on any of them.
        /// </remarks>
        public static bool IsControlPressed(Inputs input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True in the frame that the input was pressed.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <remarks>
        /// This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        /// and return true if the control was actuated in the current frame on any of them.
        /// </remarks>
        public static bool IsControlDown(Inputs input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True in the frame that the input was released.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <remarks>
        /// This will look at all devices of the appropriate type (which will depend on the specified Inputs)
        /// and return true if the control was released in the current frame on any of them.
        /// </remarks>
        public static bool IsControlUp(Inputs input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Turns any two inputs into an axis value between -1 and 1.
        /// </summary>
        /// <param name="minAxis"></param>
        /// <param name="maxAxis"></param>
        /// <returns></returns>
        public static float GetAxis(Inputs minAxis, Inputs maxAxis)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Turns any four inputs into a non-normalized vector.
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
        /// Turns any four inputs into a normalized vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="up"></param>
        /// <param name="down"></param>
        /// <returns></returns>
        public static Vector2 GetAxisNormalized(Inputs left, Inputs right, Inputs up, Inputs down)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the value of either stick on a specific gamepad, or any gamepad if gamepadSlot is Any.
        /// </summary>
        /// <param name="stick"></param>
        /// <param name="gamepadSlot">If -1, uses the current gamepad, otherwise the gamepad at this index.</param>
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
        public static Vector2 GetJoystickAxis(int joystickIndex)
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
    }
}
