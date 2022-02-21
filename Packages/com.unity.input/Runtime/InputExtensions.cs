////REVIEW: move everything from InputControlExtensions here?

using System.Collections.Generic;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Various useful extension methods.
    /// </summary>
    public static class InputExtensions
    {
        /// <summary>
        /// Return true if the given phase is <see cref="InputActionPhase.Started"/> or <see cref="InputActionPhase.Performed"/>.
        /// </summary>
        /// <param name="phase">An action phase.</param>
        /// <returns>True if the phase is started or performed.</returns>
        /// <seealso cref="InputAction.phase"/>
        public static bool IsInProgress(this InputActionPhase phase)
        {
            return phase == InputActionPhase.Started || phase == InputActionPhase.Performed;
        }

        /// <summary>
        /// Return true if the given phase is <see cref="TouchPhase.Canceled"/> or <see cref="TouchPhase.Ended"/>, i.e.
        /// if a touch with that phase would no longer be ongoing.
        /// </summary>
        /// <param name="phase">A touch phase.</param>
        /// <returns>True if the phase indicates a touch that has ended.</returns>
        /// <seealso cref="Controls.TouchControl.phase"/>
        public static bool IsEndedOrCanceled(this TouchPhase phase)
        {
            return phase == TouchPhase.Canceled || phase == TouchPhase.Ended;
        }

        /// <summary>
        /// Return true if the given phase is <see cref="TouchPhase.Began"/>, <see cref="UnityEngine.TouchPhase.Moved"/>, or
        /// <see cref="TouchPhase.Stationary"/>, i.e. if a touch with that phase would indicate an ongoing touch.
        /// </summary>
        /// <param name="phase">A touch phase.</param>
        /// <returns>True if the phase indicates a touch that is ongoing.</returns>
        /// <seealso cref="Controls.TouchControl.phase"/>
        public static bool IsActive(this TouchPhase phase)
        {
            switch (phase)
            {
                case TouchPhase.Began:
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if a <see cref="Key"/> enum value represents a modifier key.
        /// </summary>
        /// <param name="key">The key enum value you want to check.</param>
        /// <remarks>
        /// Returns true if this key is a modifier key, false otherwise.
        /// Modifier keys are any keys you can hold down to modify the output of other keys pressed simultaneously,
        /// such as the "shift" or "control" keys.
        /// </remarks>
        public static bool IsModifierKey(this Key key)
        {
            switch (key)
            {
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LeftMeta:
                case Key.RightMeta:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    return true;
            }
            return false;
        }

        ////REVIEW: Is this a good idea? Ultimately it's up to any one keyboard layout to define this however it wants.
        /// <summary>
        /// Check if a <see cref="Key"/> enum value represents key generating text input.
        /// </summary>
        /// <param name="key">The key enum value you want to check.</param>
        /// <remarks>
        /// Returns true if this key is a key generating non-whitespace character input, false otherwise.
        /// </remarks>
        public static bool IsTextInputKey(this Key key)
        {
            switch (key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftMeta:
                case Key.RightMeta:
                case Key.ContextMenu:
                case Key.Escape:
                case Key.LeftArrow:
                case Key.RightArrow:
                case Key.UpArrow:
                case Key.DownArrow:
                case Key.Backspace:
                case Key.PageDown:
                case Key.PageUp:
                case Key.Home:
                case Key.End:
                case Key.Insert:
                case Key.Delete:
                case Key.CapsLock:
                case Key.NumLock:
                case Key.PrintScreen:
                case Key.ScrollLock:
                case Key.Pause:
                case Key.None:
                case Key.Space:
                case Key.Enter:
                case Key.Tab:
                case Key.NumpadEnter:
                case Key.F1:
                case Key.F2:
                case Key.F3:
                case Key.F4:
                case Key.F5:
                case Key.F6:
                case Key.F7:
                case Key.F8:
                case Key.F9:
                case Key.F10:
                case Key.F11:
                case Key.F12:
                case Key.OEM1:
                case Key.OEM2:
                case Key.OEM3:
                case Key.OEM4:
                case Key.OEM5:
                case Key.IMESelected:
                    return false;
            }
            return true;
        }

        internal static bool IsMouseButton(this KeyCode code)
        {
            return code >= KeyCode.Mouse0 && code <= KeyCode.Mouse6;
        }

        internal static bool IsJoystickButton(this KeyCode code)
        {
            return code >= KeyCode.JoystickButton0;
        }

        public static KeyCode? ToKeyCode(this Key key)
        {
            return null;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="code">TODO</param>
        /// <returns>TODO</returns>
        /// <remarks>
        /// TODO
        ///
        /// Technically, the legacy implementation of the <see cref="UnityEngine.Input"/> API does not map keys by their
        /// physical location on the keyboard like <see cref="Keyboard"/> does (although a switch was provided at sone point
        /// in the Input Manager settings to enable this behavior). This API ignores this implementation aspect and simply
        /// assumes a mapping based on a hardcoded keyboard layout instead of a shifting locale-specific layout.
        /// </remarks>
        public static Key? ToKey(this KeyCode code)
        {
            if ((int)code >= 320)
                return null;

            switch (code)
            {
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
            }

            return null;
        }

        private static Dictionary<string, KeyCode> s_KeyCodeMapping;
        public static KeyCode? ToKeyCode(this string name)
        {
            if (s_KeyCodeMapping == null)
            {
                s_KeyCodeMapping = new Dictionary<string, KeyCode>
                {
                    { "backspace", KeyCode.Backspace },
                    { "tab", KeyCode.Tab },
                    { "clear", KeyCode.Clear },
                    { "return", KeyCode.Return },
                    { "pause", KeyCode.Pause },
                    { "escape", KeyCode.Escape },
                    { "space", KeyCode.Space },
                    { "!", KeyCode.Exclaim },
                    { "\"", KeyCode.DoubleQuote },
                    { "#", KeyCode.Hash },
                    { "$", KeyCode.Dollar },
                    { "%", KeyCode.Percent },
                    { "&", KeyCode.Ampersand },
                    { "'", KeyCode.Quote },
                    { "(", KeyCode.LeftParen },
                    { ")", KeyCode.RightParen },
                    { "*", KeyCode.Asterisk },
                    { "+", KeyCode.Plus },
                    { ",", KeyCode.Comma },
                    { "-", KeyCode.Minus },
                    { ".", KeyCode.Period },
                    { "/", KeyCode.Slash },
                    { "0", KeyCode.Alpha0 },
                    { "1", KeyCode.Alpha1 },
                    { "2", KeyCode.Alpha2 },
                    { "3", KeyCode.Alpha3 },
                    { "4", KeyCode.Alpha4 },
                    { "5", KeyCode.Alpha5 },
                    { "6", KeyCode.Alpha6 },
                    { "7", KeyCode.Alpha7 },
                    { "8", KeyCode.Alpha8 },
                    { "9", KeyCode.Alpha9 },
                    { ":", KeyCode.Colon },
                    { ";", KeyCode.Semicolon },
                    { "<", KeyCode.Less },
                    { "=", KeyCode.Equals },
                    { ">", KeyCode.Greater },
                    { "?", KeyCode.Question },
                    { "@", KeyCode.At },
                    { "[", KeyCode.LeftBracket },
                    { "\\", KeyCode.Backslash },
                    { "]", KeyCode.RightBracket },
                    { "^", KeyCode.Caret },
                    { "_", KeyCode.Underscore },
                    { "`", KeyCode.BackQuote },
                    { "a", KeyCode.A },
                    { "b", KeyCode.B },
                    { "c", KeyCode.C },
                    { "d", KeyCode.D },
                    { "e", KeyCode.E },
                    { "f", KeyCode.F },
                    { "g", KeyCode.G },
                    { "h", KeyCode.H },
                    { "i", KeyCode.I },
                    { "j", KeyCode.J },
                    { "k", KeyCode.K },
                    { "l", KeyCode.L },
                    { "m", KeyCode.M },
                    { "n", KeyCode.N },
                    { "o", KeyCode.O },
                    { "p", KeyCode.P },
                    { "q", KeyCode.Q },
                    { "r", KeyCode.R },
                    { "s", KeyCode.S },
                    { "t", KeyCode.T },
                    { "u", KeyCode.U },
                    { "v", KeyCode.V },
                    { "w", KeyCode.W },
                    { "x", KeyCode.X },
                    { "y", KeyCode.Y },
                    { "z", KeyCode.Z },
                    { "{", KeyCode.LeftCurlyBracket },
                    { "|", KeyCode.Pipe },
                    { "}", KeyCode.RightCurlyBracket },
                    { "~", KeyCode.Tilde },
                    { "delete", KeyCode.Delete },
                    { "[0]", KeyCode.Keypad0 },
                    { "[1]", KeyCode.Keypad1 },
                    { "[2]", KeyCode.Keypad2 },
                    { "[3]", KeyCode.Keypad3 },
                    { "[4]", KeyCode.Keypad4 },
                    { "[5]", KeyCode.Keypad5 },
                    { "[6]", KeyCode.Keypad6 },
                    { "[7]", KeyCode.Keypad7 },
                    { "[8]", KeyCode.Keypad8 },
                    { "[9]", KeyCode.Keypad9 },
                    { "[.]", KeyCode.KeypadPeriod },
                    { "[/]", KeyCode.KeypadDivide },
                    { "[*]", KeyCode.KeypadMultiply },
                    { "[-]", KeyCode.KeypadMinus },
                    { "[+]", KeyCode.KeypadPlus },
                    { "enter", KeyCode.KeypadEnter },
                    { "equals", KeyCode.KeypadEquals },
                    { "up", KeyCode.UpArrow },
                    { "down", KeyCode.DownArrow },
                    { "right", KeyCode.RightArrow },
                    { "left", KeyCode.LeftArrow },
                    { "insert", KeyCode.Insert },
                    { "home", KeyCode.Home },
                    { "end", KeyCode.End },
                    { "page up", KeyCode.PageUp },
                    { "page down", KeyCode.PageDown },
                    { "f1", KeyCode.F1 },
                    { "f2", KeyCode.F1 },
                    { "f3", KeyCode.F3 },
                    { "f4", KeyCode.F4 },
                    { "f5", KeyCode.F5 },
                    { "f6", KeyCode.F6 },
                    { "f7", KeyCode.F7 },
                    { "f8", KeyCode.F8 },
                    { "f9", KeyCode.F9 },
                    { "f10", KeyCode.F10 },
                    { "f11", KeyCode.F11 },
                    { "f12", KeyCode.F12 },
                    { "f13", KeyCode.F13 },
                    { "f14", KeyCode.F14 },
                    { "f15", KeyCode.F15 },
                    { "numlock", KeyCode.Numlock },
                    { "caps lock", KeyCode.CapsLock },
                    { "scroll lock", KeyCode.ScrollLock },
                    { "right shift", KeyCode.RightShift },
                    { "left shift", KeyCode.LeftShift },
                    { "right ctrl", KeyCode.RightControl },
                    { "left ctrl", KeyCode.LeftControl },
                    { "right alt", KeyCode.RightAlt },
                    { "left alt", KeyCode.LeftAlt },
                    { "right cmd", KeyCode.RightCommand },
                    { "left cmd", KeyCode.LeftCommand },
                    { "left super", KeyCode.LeftWindows },
                    { "right super", KeyCode.RightWindows },
                    { "alt gr", KeyCode.AltGr },
                    { "help", KeyCode.Help },
                    { "print screen", KeyCode.Print },
                    { "sys req", KeyCode.SysReq },
                    { "break", KeyCode.Break },
                    { "menu", KeyCode.Menu },
                };

                for (var i = 0; i <= 6; ++i)
                    s_KeyCodeMapping["mouse " + i] = KeyCode.Mouse0 + i;

                const int kButtonsPerJoystick = 20;
                for (var n = 0; n < kButtonsPerJoystick; ++n)
                    s_KeyCodeMapping["joystick button " + n] = KeyCode.JoystickButton0 + n;

                for (var i = 1; i <= 16; ++i)
                    for (var n = 0; n < kButtonsPerJoystick; ++n)
                        s_KeyCodeMapping[$"joystick {i} button {n}"] = KeyCode.JoystickButton0 + (i * kButtonsPerJoystick) + n;

                ////REVIEW: there's some old stuff in the native g_KeyToName which maps to SDLK codes but not to public KeyCode values; how do we want to handle this here?
            }

            if (s_KeyCodeMapping.TryGetValue(name, out var value))
                return value;
            return null;
        }
    }
}
