using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tests.InputLegacy
{
    public class KeyboardTests : InputTestFixture
    {
        [Test]
        public void GetKeyByCode()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            foreach (var (key, keyCode) in s_KeysToKeyCodes)
            {
                Assert.False(Input.GetKey(keyCode),
                    $"Before pressing keyboard[{key}], Input.GetKey({keyCode}) should return false");

                Press(keyboard[key]);
                Assert.True(Input.GetKey(keyCode),
                    $"After pressing keyboard[{key}], Input.GetKey({keyCode}) should return true");

                Release(keyboard[key]);
                Assert.False(Input.GetKey(keyCode),
                    $"After release keyboard[{key}], Input.GetKey({keyCode}) should return false");
            }
        }

        [Test]
        public void GetKeyDownByCode()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            foreach (var (key, keyCode) in s_KeysToKeyCodes)
            {
                Assert.False(Input.GetKeyDown(keyCode),
                    $"Before pressing keyboard[{key}], Input.GetKeyDown({keyCode}) should return false");

                Press(keyboard[key]);
                Assert.True(Input.GetKeyDown(keyCode),
                    $"After pressing keyboard[{key}], Input.GetKeyDown({keyCode}) should return true");

                InputSystem.Update();
                Assert.False(Input.GetKeyDown(keyCode),
                    $"After one frame, Input.GetKeyDown({keyCode}) should return false");
            }
        }

        [Test]
        public void GetKeyUpByCode()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            foreach (var (key, keyCode) in s_KeysToKeyCodes)
            {
                Assert.False(Input.GetKeyUp(keyCode),
                    $"Before pressing keyboard[{key}], Input.GetKeyUp({keyCode}) should return false");

                Press(keyboard[key]);
                Assert.False(Input.GetKeyUp(keyCode),
                    $"After pressing keyboard[{key}], Input.GetKeyUp({keyCode}) should return false");

                Release(keyboard[key]);
                Assert.True(Input.GetKeyUp(keyCode),
                    $"After releasing keyboard[{key}], Input.GetKeyUp({keyCode}) should return true");

                InputSystem.Update();
                Assert.False(Input.GetKeyDown(keyCode),
                    $"After one frame, Input.GetKeyUp({keyCode}) should return false");
            }
        }

        [Test]
        public void GetKeyByName()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            foreach (var (key, keyName) in s_KeysToKeyCodes)
            {
                Assert.False(Input.GetKey(keyName),
                    $"Before pressing keyboard[{key}], Input.GetKey({keyName}) should return false");

                Press(keyboard[key]);
                Assert.True(Input.GetKey(keyName),
                    $"After pressing keyboard[{key}], Input.GetKey({keyName}) should return true");

                Release(keyboard[key]);
                Assert.False(Input.GetKey(keyName),
                    $"After release keyboard[{key}], Input.GetKey({keyName}) should return false");
            }
        }

        static List<(Key key, KeyCode keyCode)> s_KeysToKeyCodes = new List<(Key key, KeyCode keyCode)>
        {
            (Key.Space, KeyCode.Space),
            (Key.Enter, KeyCode.Return),
            (Key.Tab, KeyCode.Tab),
            (Key.Backquote, KeyCode.BackQuote),
            (Key.Quote, KeyCode.Quote),
            (Key.Semicolon, KeyCode.Semicolon),
            (Key.Comma, KeyCode.Comma),
            (Key.Period, KeyCode.Period),
            (Key.Slash, KeyCode.Slash),
            (Key.Backslash, KeyCode.Backslash),
            (Key.LeftBracket, KeyCode.LeftBracket),
            (Key.RightBracket, KeyCode.RightBracket),
            (Key.Minus, KeyCode.Minus),
            (Key.Equals, KeyCode.Equals),
            (Key.A, KeyCode.A),
            (Key.B, KeyCode.B),
            (Key.C, KeyCode.C),
            (Key.D, KeyCode.D),
            (Key.E, KeyCode.E),
            (Key.F, KeyCode.F),
            (Key.G, KeyCode.G),
            (Key.H, KeyCode.H),
            (Key.I, KeyCode.I),
            (Key.J, KeyCode.J),
            (Key.K, KeyCode.K),
            (Key.L, KeyCode.L),
            (Key.M, KeyCode.M),
            (Key.N, KeyCode.N),
            (Key.O, KeyCode.O),
            (Key.P, KeyCode.P),
            (Key.Q, KeyCode.Q),
            (Key.R, KeyCode.R),
            (Key.S, KeyCode.S),
            (Key.T, KeyCode.T),
            (Key.U, KeyCode.U),
            (Key.V, KeyCode.V),
            (Key.W, KeyCode.W),
            (Key.X, KeyCode.X),
            (Key.Y, KeyCode.Y),
            (Key.Z, KeyCode.Z),
            (Key.Digit1, KeyCode.Alpha1),
            (Key.Digit2, KeyCode.Alpha2),
            (Key.Digit3, KeyCode.Alpha3),
            (Key.Digit4, KeyCode.Alpha4),
            (Key.Digit5, KeyCode.Alpha5),
            (Key.Digit6, KeyCode.Alpha6),
            (Key.Digit7, KeyCode.Alpha7),
            (Key.Digit8, KeyCode.Alpha8),
            (Key.Digit9, KeyCode.Alpha9),
            (Key.Digit0, KeyCode.Alpha0),
            (Key.LeftShift, KeyCode.LeftShift),
            (Key.RightShift, KeyCode.RightShift),
            (Key.LeftAlt, KeyCode.LeftAlt),
            (Key.RightAlt, KeyCode.RightAlt),
            (Key.LeftCtrl, KeyCode.LeftControl),
            (Key.RightCtrl, KeyCode.RightControl),

            (Key.LeftMeta, KeyCode.LeftWindows), // TODO Check if this is correct
            (Key.RightMeta, KeyCode.RightWindows), // TODO Check if this is correct

            (Key.ContextMenu, KeyCode.Menu),
            (Key.Escape, KeyCode.Escape),
            (Key.LeftArrow, KeyCode.LeftArrow),
            (Key.RightArrow, KeyCode.RightArrow),
            (Key.UpArrow, KeyCode.UpArrow),
            (Key.DownArrow, KeyCode.DownArrow),
            (Key.Backspace, KeyCode.Backspace),
            (Key.PageDown, KeyCode.PageDown),
            (Key.PageUp, KeyCode.PageUp),
            (Key.Home, KeyCode.Home),
            (Key.End, KeyCode.End),
            (Key.Insert, KeyCode.Insert),
            (Key.Delete, KeyCode.Delete),
            (Key.CapsLock, KeyCode.CapsLock),
            (Key.NumLock, KeyCode.Numlock),
            (Key.PrintScreen, KeyCode.Print),
            (Key.ScrollLock, KeyCode.ScrollLock),
            (Key.Pause, KeyCode.Pause),
            (Key.NumpadEnter, KeyCode.KeypadEnter),
            (Key.NumpadDivide, KeyCode.KeypadDivide),
            (Key.NumpadMultiply, KeyCode.KeypadMultiply),
            (Key.NumpadPlus, KeyCode.KeypadPlus),
            (Key.NumpadMinus, KeyCode.KeypadMinus),
            (Key.NumpadPeriod, KeyCode.KeypadPeriod),
            (Key.NumpadEquals, KeyCode.KeypadEquals),
            (Key.Numpad0, KeyCode.Keypad0),
            (Key.Numpad1, KeyCode.Keypad1),
            (Key.Numpad2, KeyCode.Keypad2),
            (Key.Numpad3, KeyCode.Keypad3),
            (Key.Numpad4, KeyCode.Keypad4),
            (Key.Numpad5, KeyCode.Keypad5),
            (Key.Numpad6, KeyCode.Keypad6),
            (Key.Numpad7, KeyCode.Keypad7),
            (Key.Numpad8, KeyCode.Keypad8),
            (Key.Numpad9, KeyCode.Keypad9),
            (Key.F1, KeyCode.F1),
            (Key.F2, KeyCode.F2),
            (Key.F3, KeyCode.F3),
            (Key.F4, KeyCode.F4),
            (Key.F5, KeyCode.F5),
            (Key.F6, KeyCode.F6),
            (Key.F7, KeyCode.F7),
            (Key.F8, KeyCode.F8),
            (Key.F9, KeyCode.F9),
            (Key.F10, KeyCode.F10),
            (Key.F11, KeyCode.F11),
            (Key.F12, KeyCode.F12),
        };

        private static List<(Key, string)> s_KeysToNames = new List<(Key, string)>()
        {
            (Key.Backspace, "backspace"), // SDLK_BACKSPACE
            (Key.Tab, "tab"), // SDLK_TAB
            //(Key., "clear"), // SDLK_CLEAR
            (Key.Enter, "return"), // SDLK_RETURN
            (Key.Pause, "pause"), // SDLK_PAUSE
            (Key.Escape, "escape"), // SDLK_ESCAPE
            (Key.Space, "space"), // SDLK_SPACE
            //(Key., "!"), // SDLK_EXCLAIM
            //(Key., "\""), // SDLK_QUOTEDBL
            //(Key., "#"), // SDLK_HASH
            //(Key., "$"), // SDLK_DOLLAR
            //(Key., "%"), // SDLK_PERCENT
            //(Key., "&"), // SDLK_AMPERSAND
            (Key.Quote, "'"), // SDLK_QUOTE
            //(Key., "("), // SDLK_LEFTPAREN
            //(Key.None, ")"), // SDLK_RIGHTPAREN
            //(Key., "*"}), // SDLK_ASTERISK
            //(Key., "+"}), // SDLK_PLUS
            (Key.Comma, ","), // SDLK_COMMA
            (Key.Minus, "-"), // SDLK_MINUS
            (Key.Period, "."), // SDLK_PERIOD
            (Key.Slash, "/"), // SDLK_SLASH
            (Key.Digit0, "0"), // SDLK_0
            (Key.Digit1, "1"), // SDLK_1
            (Key.Digit2, "2"), // SDLK_2
            (Key.Digit3, "3"), // SDLK_3
            (Key.Digit4, "4"), // SDLK_4
            (Key.Digit5, "5"), // SDLK_5
            (Key.Digit6, "6"), // SDLK_6
            (Key.Digit7, "7"), // SDLK_7
            (Key.Digit8, "8"), // SDLK_8
            (Key.Digit9, "9"), // SDLK_9
            //(Key., ":"), // SDLK_COLON
            (Key.Semicolon, ";"), // SDLK_SEMICOLON
            //(Key., "<"), // SDLK_LESS
            (Key.Equals, "="), // SDLK_EQUALS
            //(Key., ">"), // SDLK_GREATER
            //(Key., "?"), // SDLK_QUESTION
            //(Key., "@"), // SDLK_AT
            (Key.LeftBracket, "["), // SDLK_LEFTBRACKET
            (Key.Backslash, "\\"), // SDLK_BACKSLASH
            (Key.RightBracket, "]"), // SDLK_RIGHTBRACKET
            //(Key., "^"), // SDLK_CARET
            //(Key., "_"), // SDLK_UNDERSCORE
            (Key.Backquote, "`"), // SDLK_BACKQUOTE
            (Key.A, "a"), // SDLK_a
            (Key.B, "b"), // SDLK_b
            (Key.C, "c"), // SDLK_c
            (Key.D, "d"), // S
            (Key.E, "e"), // SDLK_e
            (Key.F, "f"), // SDLK_f
            (Key.G, "g"), // SDLK_g
            (Key.H, "h"), // SDLK_h
            (Key.I, "i"), // SDLK_i
            (Key.J, "j"), // SDLK_j
            (Key.K, "k"), // SDLK_k
            (Key.L, "l"), // SDLK_l
            (Key.M, "m"), // SDLK_m
            (Key.N, "n"), // SDLK_n
            (Key.O, "o"), // SDLK_o
            (Key.P, "p"), // SDLK_p
            (Key.Q, "q"), // SDLK_q
            (Key.R, "r"), // SDLK_r
            (Key.S, "s"), // SDLK_s
            (Key.T, "t"), // SDLK_t
            (Key.U, "u"), // SDLK_u
            (Key.V, "v"), // SDLK_v
            (Key.W, "w"), // SDLK_w
            (Key.X, "x"), // SDLK_x
            (Key.Y, "y"), // SDLK_y
            (Key.Z, "z"), // SDLK_z
            //(Key., "{"), // SDLK_LEFTCURLYBRACKET
            //(Key., "|"), // SDLK_PIPE
            //(Key., "}"), // SDLK_RIGHTCURLYBRACKET
            //(Key., "~"), // SDLK_TILDE
            (Key.Delete, "delete"), // SDLK_DELETE
            (Key.Numpad0, "[0]"), // SDLK_KP0
            (Key.Numpad1, "[1]"), // SDLK_KP1
            (Key.Numpad2, "[2]"), // SDLK_KP2
            (Key.Numpad3, "[3]"), // SDLK_KP3
            (Key.Numpad4, "[4]"), // SDLK_KP4
            (Key.Numpad5, "[5]"), // SDLK_KP5
            (Key.Numpad6, "[6]"), // SDLK_KP6
            (Key.Numpad7, "[7]"), // SDLK_KP7
            (Key.Numpad8, "[8]"), // SDLK_KP8
            (Key.Numpad9, "[9]"), // SDLK_KP9
            (Key.NumpadPeriod, "[.]"), // SDLK_KP_PERIOD
            (Key.NumpadDivide, "[/]"), // SDLK_KP_DIVIDE
            (Key.NumpadMultiply, "[*]"), // SDLK_KP_MULTIPLY
            (Key.NumpadMinus, "[-]"), // SDLK_KP_MINUS
            (Key.NumpadPlus, "[+]"), // SDLK_KP_PLUS
            (Key.NumpadEnter, "enter"), // SDLK_KP_ENTER
            (Key.NumpadEquals, "equals"), // SDLK_KP_EQUALS
            (Key.UpArrow, "up"), // SDLK_UP
            (Key.DownArrow, "down"), // SDLK_DOWN
            (Key.RightArrow, "right"), // SDLK_RIGHT
            (Key.LeftArrow, "left"), // SDLK_LEFT
            (Key.Insert, "insert"), // SDLK_INSERT
            (Key.Home, "home"), // SDLK_HOME
            (Key.End, "end"), // SDLK_END
            (Key.PageUp, "page up"), // SDLKP_PAGEUP
            (Key.PageDown, "page down"), // SDLK_PAGEDOWN
            (Key.F1, "f1"), // SDLK_F1
            (Key.F2, "f2"), // SDLK_F2
            (Key.F3, "f3"), // SDLK_F3
            (Key.F4, "f4"), // SDLK_F4
            (Key.F5, "f5"), // SDLK_F5
            (Key.F6, "f6"), // SDLK_F6
            (Key.F7, "f7"), // SDLK_F7
            (Key.F8, "f8"), // SDLK_F8
            (Key.F9, "f9"), // SDLK_F9
            (Key.F10, "f10"), // SDLK_F10
            (Key.F11, "f11"), // SDLK_F11
            (Key.F12, "f12"), // SDLK_F12
            //(Key.F13, "f13"), // SDLK_F13
            //(Key.F14, "f14"), // SDLK_F14
            //(Key.F15, "f15"), // SDLK_F15
            (Key.NumLock, "numlock"), // SDLK_NUMLOCK
            (Key.CapsLock, "caps lock"), // SDLK_CAPSLOCK
            (Key.ScrollLock, "scroll lock"), // SDLK_SCROLLOCK
            (Key.RightShift, "right shift"), // SDLK_RSHIFT
            (Key.LeftShift, "left shift"), // SDLK_LSHIFT
            (Key.RightCtrl, "right ctrl"), // SDLK_RCTRL
            (Key.LeftCtrl, "left ctrl"), // SDLK_LCTRL
            (Key.RightAlt, "right alt"), // SDLK_RALT
            (Key.LeftAlt, "left alt"), // SDLK_LALT
            (Key.RightCommand, "right cmd"), // SDLK_RMETA
            (Key.LeftCommand, "left cmd"), // SDLK_LMETA
            (Key.LeftMeta, "left super"), // SDLK_LSUPER
            (Key.RightMeta, "right super"), // SDLK_RSUPER
            (Key.AltGr, "alt gr"), // SDLK_MODE
            //(Key., "compose"), // SDLK_COMPOSE
            //(Key., "help"), // SDLK_HELP
            (Key.PrintScreen, "print screen"), // SDLK_PRINT
            //(Key., "sys req"), // SDLK_SYSREQ
            //(Key., "break"), // SDLK_BREAK
            (Key.ContextMenu, "menu"), // SDLK_MENU
            //(Key., "power"), // SDLK_POWER
            //(Key., "euro"), // SDLK_EURO
            //(Key., "undo") // SDLK_UNDO
        };
    }
}