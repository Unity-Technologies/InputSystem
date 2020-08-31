/*
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputLegacy.Controls;

namespace UnityEngine.InputLegacy
{
    internal class ControlPathMapper
    {
        public static string GetKeyboardControlPathForKeyCode(KeyCode keyCode, string usage)
        {
            switch (keyCode)
            {
                case KeyCode.Escape: return $"<Keyboard>{usage}/escape";
                case KeyCode.Space: return $"<Keyboard>{usage}/space";
                case KeyCode.Return: return $"<Keyboard>{usage}/Enter";
                case KeyCode.Tab: return $"<Keyboard>{usage}/tab";
                case KeyCode.BackQuote: return $"<Keyboard>{usage}/backquote";
                case KeyCode.Quote: return $"<Keyboard>{usage}/quote";
                case KeyCode.Semicolon: return $"<Keyboard>{usage}/semicolon";
                case KeyCode.Comma: return $"<Keyboard>{usage}/comma";
                case KeyCode.Period: return $"<Keyboard>{usage}/period";
                case KeyCode.Slash: return $"<Keyboard>{usage}/slash";
                case KeyCode.Backslash: return $"<Keyboard>{usage}/backslash";
                case KeyCode.LeftBracket: return $"<Keyboard>{usage}/leftBracket";
                case KeyCode.RightBracket: return $"<Keyboard>{usage}/rightBracket";
                case KeyCode.Minus: return $"<Keyboard>{usage}/minus";
                case KeyCode.Equals: return $"<Keyboard>{usage}/equals";
                case KeyCode.UpArrow: return $"<Keyboard>{usage}/upArrow";
                case KeyCode.DownArrow: return $"<Keyboard>{usage}/downArrow";
                case KeyCode.LeftArrow: return $"<Keyboard>{usage}/leftArrow";
                case KeyCode.RightArrow: return $"<Keyboard>{usage}/rightArrow";
                // TODO reevaluate this, how do we get cyrillic for example?
                // Bind by display name rather than key code. Means we respect keyboard layouts
                // like the old input system does.
                case KeyCode.A: return $"<Keyboard>{usage}/#(A)";
                case KeyCode.B: return $"<Keyboard>{usage}/#(B)";
                case KeyCode.C: return $"<Keyboard>{usage}/#(C)";
                case KeyCode.D: return $"<Keyboard>{usage}/#(D)";
                case KeyCode.E: return $"<Keyboard>{usage}/#(E)";
                case KeyCode.F: return $"<Keyboard>{usage}/#(F)";
                case KeyCode.G: return $"<Keyboard>{usage}/#(G)";
                case KeyCode.H: return $"<Keyboard>{usage}/#(H)";
                case KeyCode.I: return $"<Keyboard>{usage}/#(I)";
                case KeyCode.J: return $"<Keyboard>{usage}/#(J)";
                case KeyCode.K: return $"<Keyboard>{usage}/#(K)";
                case KeyCode.L: return $"<Keyboard>{usage}/#(L)";
                case KeyCode.M: return $"<Keyboard>{usage}/#(M)";
                case KeyCode.N: return $"<Keyboard>{usage}/#(N)";
                case KeyCode.O: return $"<Keyboard>{usage}/#(O)";
                case KeyCode.P: return $"<Keyboard>{usage}/#(P)";
                case KeyCode.Q: return $"<Keyboard>{usage}/#(Q)";
                case KeyCode.R: return $"<Keyboard>{usage}/#(R)";
                case KeyCode.S: return $"<Keyboard>{usage}/#(S)";
                case KeyCode.T: return $"<Keyboard>{usage}/#(T)";
                case KeyCode.U: return $"<Keyboard>{usage}/#(U)";
                case KeyCode.V: return $"<Keyboard>{usage}/#(V)";
                case KeyCode.W: return $"<Keyboard>{usage}/#(W)";
                case KeyCode.X: return $"<Keyboard>{usage}/#(X)";
                case KeyCode.Y: return $"<Keyboard>{usage}/#(Y)";
                case KeyCode.Z: return $"<Keyboard>{usage}/#(Z)";
                case KeyCode.Alpha1: return $"<Keyboard>{usage}/1";
                case KeyCode.Alpha2: return $"<Keyboard>{usage}/2";
                case KeyCode.Alpha3: return $"<Keyboard>{usage}/3";
                case KeyCode.Alpha4: return $"<Keyboard>{usage}/4";
                case KeyCode.Alpha5: return $"<Keyboard>{usage}/5";
                case KeyCode.Alpha6: return $"<Keyboard>{usage}/6";
                case KeyCode.Alpha7: return $"<Keyboard>{usage}/7";
                case KeyCode.Alpha8: return $"<Keyboard>{usage}/8";
                case KeyCode.Alpha9: return $"<Keyboard>{usage}/9";
                case KeyCode.Alpha0: return $"<Keyboard>{usage}/0";
                case KeyCode.LeftShift: return $"<Keyboard>{usage}/leftShift";
                case KeyCode.RightShift: return $"<Keyboard>{usage}/rightShift";
                case KeyCode.LeftAlt: return $"<Keyboard>{usage}/leftAlt";
                case KeyCode.RightAlt: return $"<Keyboard>{usage}/rightAlt";
                case KeyCode.LeftControl: return $"<Keyboard>{usage}/leftCtrl";
                case KeyCode.RightControl: return $"<Keyboard>{usage}/rightCtrl";
                case KeyCode.LeftWindows: return $"<Keyboard>{usage}/leftMeta";
                case KeyCode.LeftApple: return $"<Keyboard>{usage}/leftMeta";
                case KeyCode.RightWindows: return $"<Keyboard>{usage}/rightMeta";
                case KeyCode.RightApple: return $"<Keyboard>{usage}/rightMeta";
                case KeyCode.Menu: return $"<Keyboard>{usage}/contextMenu";
                case KeyCode.Backspace: return $"<Keyboard>{usage}/backspace";
                case KeyCode.PageDown: return $"<Keyboard>{usage}/pageDown";
                case KeyCode.PageUp: return $"<Keyboard>{usage}/pageUp";
                case KeyCode.Home: return $"<Keyboard>{usage}/home";
                case KeyCode.End: return $"<Keyboard>{usage}/end";
                case KeyCode.Insert: return $"<Keyboard>{usage}/insert";
                case KeyCode.Delete: return $"<Keyboard>{usage}/delete";
                case KeyCode.CapsLock: return $"<Keyboard>{usage}/capsLock";
                case KeyCode.Numlock: return $"<Keyboard>{usage}/numLock";
                case KeyCode.Print: return $"<Keyboard>{usage}/printScreen";
                case KeyCode.ScrollLock: return $"<Keyboard>{usage}/scrollLock";
                case KeyCode.Pause: return $"<Keyboard>{usage}/pause"; // TODO is this correct?
                case KeyCode.SysReq: return $"<Keyboard>{usage}/pause"; // TODO is this correct?
                case KeyCode.KeypadEnter: return $"<Keyboard>{usage}/numpadEnter";
                case KeyCode.KeypadDivide: return $"<Keyboard>{usage}/numpadDivide";
                case KeyCode.KeypadMultiply: return $"<Keyboard>{usage}/numpadMultiply";
                case KeyCode.KeypadPlus: return $"<Keyboard>{usage}/numpadPlus";
                case KeyCode.KeypadMinus: return $"<Keyboard>{usage}/numpadMinus";
                case KeyCode.KeypadPeriod: return $"<Keyboard>{usage}/numpadPeriod";
                case KeyCode.KeypadEquals: return $"<Keyboard>{usage}/numpadEquals";
                case KeyCode.Keypad1: return $"<Keyboard>{usage}/numpad1";
                case KeyCode.Keypad2: return $"<Keyboard>{usage}/numpad2";
                case KeyCode.Keypad3: return $"<Keyboard>{usage}/numpad3";
                case KeyCode.Keypad4: return $"<Keyboard>{usage}/numpad4";
                case KeyCode.Keypad5: return $"<Keyboard>{usage}/numpad5";
                case KeyCode.Keypad6: return $"<Keyboard>{usage}/numpad6";
                case KeyCode.Keypad7: return $"<Keyboard>{usage}/numpad7";
                case KeyCode.Keypad8: return $"<Keyboard>{usage}/numpad8";
                case KeyCode.Keypad9: return $"<Keyboard>{usage}/numpad9";
                case KeyCode.Keypad0: return $"<Keyboard>{usage}/numpad0";
                case KeyCode.F1: return $"<Keyboard>{usage}/f1";
                case KeyCode.F2: return $"<Keyboard>{usage}/f2";
                case KeyCode.F3: return $"<Keyboard>{usage}/f3";
                case KeyCode.F4: return $"<Keyboard>{usage}/f4";
                case KeyCode.F5: return $"<Keyboard>{usage}/f5";
                case KeyCode.F6: return $"<Keyboard>{usage}/f6";
                case KeyCode.F7: return $"<Keyboard>{usage}/f7";
                case KeyCode.F8: return $"<Keyboard>{usage}/f8";
                case KeyCode.F9: return $"<Keyboard>{usage}/f9";
                case KeyCode.F10: return $"<Keyboard>{usage}/f10";
                case KeyCode.F11: return $"<Keyboard>{usage}/f11";
                case KeyCode.F12: return $"<Keyboard>{usage}/f12";
                // case KeyCode.OEM1: return $"<Keyboard>{usage}/#(OEM1)";
                // case KeyCode.OEM2: return $"<Keyboard>{usage}/#(OEM2)";
                // case KeyCode.OEM3: return $"<Keyboard>{usage}/#(OEM3)";
                // case KeyCode.OEM4: return $"<Keyboard>{usage}/#(OEM4)";
                // case KeyCode.OEM5: return $"<Keyboard>{usage}/#(OEM5)";
                // case KeyCode.IMESelected: return $"<Keyboard>{usage}/#(IMESelected)";
            }

            return null;
        }

        // public static string GetMouseControlPathForKeyCode(KeyCode keyCode, string usage)
        // {
        //     switch (keyCode)
        //     {
        //         case KeyCode.Mouse0: return $"<Mouse>{usage}/leftButton";
        //         case KeyCode.Mouse1: return $"<Mouse>{usage}/rightButton";
        //         case KeyCode.Mouse2: return $"<Mouse>{usage}/middleButton";
        //         ////REVIEW: With these two, is it this way around or the other?
        //         case KeyCode.Mouse3: return $"<Mouse>{usage}/forwardButton";
        //         case KeyCode.Mouse4: return $"<Mouse>{usage}/backButton";
        //     }
        //
        //     return null;
        // }
        //
        // public static KeyCode? GetMouseKeyCodeForButtonNumber(int buttonNumber)
        // {
        //     switch (buttonNumber)
        //     {
        //         case 0: return KeyCode.Mouse0;
        //         case 1: return KeyCode.Mouse1;
        //         case 2: return KeyCode.Mouse2;
        //         case 3: return KeyCode.Mouse3;
        //         case 4: return KeyCode.Mouse4;
        //     }
        //
        //     return null;
        // }

        public static string GetJoystickControlPathForKeyCode(KeyCode button, string usage)
        {
            switch (button)
            {
                case KeyCode.JoystickButton0: return $"<Joystick>{usage}/trigger";
                case KeyCode.JoystickButton1: return $"<Joystick>{usage}/button2";
                case KeyCode.JoystickButton2: return $"<Joystick>{usage}/button3";
                //etc.
            }

            return null;
        }

        public static string GetGamepadControlPathForKeyCode(KeyCode button, string usage)
        {
            ////REVIEW: If we do it platform-dependent like this and move things into the importer, we'll
            ////        have to make the imported asset dependent on the build target.

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Follow Xbox layout for gamepads.
            switch (button)
            {
                // https://answers.unity.com/questions/1350081/xbox-one-controller-mapping-solved.html
                case KeyCode.JoystickButton0: return $"<Gamepad>{usage}/buttonSouth";
                case KeyCode.JoystickButton1: return $"<Gamepad>{usage}/buttonEast";
                case KeyCode.JoystickButton2: return $"<Gamepad>{usage}/buttonWest";
                //etc.
            }
#endif

            ////TODO: other platforms
            ////TODO: fallback (maybe just use Windows path?)

            return null;
        }

        private const int kMaxJoysticks = 16;
        private const int kMaxButtonsPerJoystick = 19;

        public static KeyCode MapJoystickButtonToJoystick0(KeyCode code)
        {
            for (var i = 0; i < kMaxJoysticks; ++i)
            {
                var min = KeyCode.Joystick1Button0 + i * kMaxButtonsPerJoystick;
                var max = min + kMaxButtonsPerJoystick;
                if (code >= min && code <= max)
                    return KeyCode.Joystick1Button0 + ((int) code - (int) min);
            }

            return code;
        }

        public static int GetJoystickNumber(KeyCode keyCode)
        {
            for (var i = 0; i < kMaxJoysticks; ++i)
            {
                var min = KeyCode.Joystick1Button0 + i * kMaxButtonsPerJoystick;
                var max = min + kMaxButtonsPerJoystick;
                if (keyCode >= min && keyCode <= max)
                    return i;
            }

            return -1;
        }


        // For joystick buttons and axes, we have the situation that we don't just want to blindly
        // map to joystick and gamepad buttons of the input system. Instead, for the controllers that
        // the input system explicitly supports, we want to retain the mapping that buttons and axes
        // have (on the specific platform) in the old system. This means we can end up with several
        // bindings for each such joystick button/axis in the old system.


        // public static IEnumerable<string> GetControlPathsForJoystickAxis(string button)
        // {
        //     throw new NotImplementedException();
        // }

        public static string[] GetAllControlPathsForKeyCode(KeyCode keyCode, string usage)
        {
            // If the binding is associated with a particular joystick, reflect that
            // through a usage tag on the binding.
            var joyNum = GetJoystickNumber(keyCode);
            if (joyNum >= 1)
                // TODO some more obvious way to indicate that usage is overriden? an exception if external usage was provided?
                usage = DeviceMonitor.JoyNumToUsage(joyNum);

            keyCode = MapJoystickButtonToJoystick0(keyCode);

            var result = new List<string>();
            result.Add(GetGamepadControlPathForKeyCode(keyCode, usage));
            result.Add(GetJoystickControlPathForKeyCode(keyCode, usage));
            result.Add(GetKeyboardControlPathForKeyCode(keyCode, usage));
            //result.Add(GetMouseControlPathForKeyCode(keyCode, usage));
            result.RemoveAll(string.IsNullOrEmpty);
            return result.ToArray();
        }

        public static string[] GetAllControlPathsForKeyName(string keyName, string usage)
        {
            return GetAllControlPathsForKeyCode(KeyNames.NameToKey(keyName), usage);
        }
    }
}
*/