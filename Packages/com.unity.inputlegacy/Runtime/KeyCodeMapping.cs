using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputLegacy
{
    internal static class KeyCodeMapping
    {
        static KeyCodeMapping()
        {
            for (var i = 0; i < s_KeyCodeToKeyName.Length; i++)
                s_KeyNameToKeyCode[s_KeyCodeToKeyName[i]] = (KeyCode) i;
        }
        public static KeyCode KeyNameToKeyCode(string name)
        {
            return s_KeyNameToKeyCode.TryGetValue(name, out var keyCode) ? keyCode : KeyCode.None;
        }

        public static string KeyCodeToKeyName(KeyCode keyCode)
        {
            return s_KeyCodeToKeyName[(int) keyCode];
        }

        private const int k_MaxButtonsPerJoystick = 20;

        public static (int joyNum, KeyCode joystick0KeyCode) KeyCodeToJoystickNumberAndJoystick0KeyCode(KeyCode keyCode)
        {
            if (keyCode < KeyCode.JoystickButton0 || keyCode > KeyCode.Joystick8Button19)
                return (-1, KeyCode.None);

            var joyNum = (keyCode - KeyCode.JoystickButton0) / k_MaxButtonsPerJoystick;
            var mappedKeyCode = keyCode - joyNum * k_MaxButtonsPerJoystick;

            return (joyNum, mappedKeyCode);
        }

        public static MouseButton? KeyCodeToMouseButton(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.Mouse0: return MouseButton.Left;
                case KeyCode.Mouse1: return MouseButton.Right;
                case KeyCode.Mouse2: return MouseButton.Middle;
                ////REVIEW: With these two, is it this way around or the other?
                case KeyCode.Mouse3: return MouseButton.Forward;
                case KeyCode.Mouse4: return MouseButton.Back;
                // TODO KeyCode.Mouse5 / KeyCode.Mouse6
                default: return null;
            }
        }

        public static GamepadButton? Joystick0KeyCodeToGamepadButton(KeyCode keyCode)
        {
            switch (keyCode)
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                // https://answers.unity.com/questions/1350081/xbox-one-controller-mapping-solved.html
                case KeyCode.JoystickButton0: return GamepadButton.South;
                case KeyCode.JoystickButton1: return GamepadButton.East;
                case KeyCode.JoystickButton2: return GamepadButton.West;
                case KeyCode.JoystickButton3: return GamepadButton.North;
                case KeyCode.JoystickButton4: return GamepadButton.LeftShoulder;
                case KeyCode.JoystickButton5: return GamepadButton.RightShoulder;
                case KeyCode.JoystickButton6: return GamepadButton.Select;
                case KeyCode.JoystickButton7: return GamepadButton.Start;
                case KeyCode.JoystickButton8: return GamepadButton.LeftStick;
                case KeyCode.JoystickButton9: return GamepadButton.RightStick;
#else
                // TODO macOS bindings
#endif
                default: return null;
            }
        }

        private static Dictionary<string, KeyCode> s_KeyNameToKeyCode = new Dictionary<string, KeyCode>();

        private static string[] s_KeyCodeToKeyName =
        {
            "", // SDLK_FIRST
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "backspace", // SDLK_BACKSPACE
            "tab", // SDLK_TAB
            "",
            "",
            "clear", // SDLK_CLEAR
            "return", // SDLK_RETURN
            "",
            "",
            "",
            "",
            "",
            "pause", // SDLK_PAUSE
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "escape", // SDLK_ESCAPE
            "",
            "",
            "",
            "",
            "space", // SDLK_SPACE
            "!", // SDLK_EXCLAIM
            "\"", // SDLK_QUOTEDBL
            "#", // SDLK_HASH
            "$", // SDLK_DOLLAR
            "%", // SDLK_PERCENT
            "&", // SDLK_AMPERSAND
            "'", // SDLK_QUOTE
            "(", // SDLK_LEFTPAREN
            ")", // SDLK_RIGHTPAREN
            "*", // SDLK_ASTERISK
            "+", // SDLK_PLUS
            ",", // SDLK_COMMA
            "-", // SDLK_MINUS
            ".", // SDLK_PERIOD
            "/", // SDLK_SLASH
            "0", // SDLK_0
            "1", // SDLK_1
            "2", // SDLK_2
            "3", // SDLK_3
            "4", // SDLK_4
            "5", // SDLK_5
            "6", // SDLK_6
            "7", // SDLK_7
            "8", // SDLK_8
            "9", // SDLK_9
            ":", // SDLK_COLON
            ";", // SDLK_SEMICOLON
            "<", // SDLK_LESS
            "=", // SDLK_EQUALS
            ">", // SDLK_GREATER
            "?", // SDLK_QUESTION
            "@", // SDLK_AT
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "[", // SDLK_LEFTBRACKET
            "\\", // SDLK_BACKSLASH
            "]", // SDLK_RIGHTBRACKET
            "^", // SDLK_CARET
            "_", // SDLK_UNDERSCORE
            "`", // SDLK_BACKQUOTE
            "a", // SDLK_a
            "b", // SDLK_b
            "c", // SDLK_c
            "d", // S
            "e", // SDLK_e
            "f", // SDLK_f
            "g", // SDLK_g
            "h", // SDLK_h
            "i", // SDLK_i
            "j", // SDLK_j
            "k", // SDLK_k
            "l", // SDLK_l
            "m", // SDLK_m
            "n", // SDLK_n
            "o", // SDLK_o
            "p", // SDLK_p
            "q", // SDLK_q
            "r", // SDLK_r
            "s", // SDLK_s
            "t", // SDLK_t
            "u", // SDLK_u
            "v", // SDLK_v
            "w", // SDLK_w
            "x", // SDLK_x
            "y", // SDLK_y
            "z", // SDLK_z
            "{", // SDLK_LEFTCURLYBRACKET
            "|", // SDLK_PIPE
            "}", // SDLK_RIGHTCURLYBRACKET
            "~", // SDLK_TILDE
            "delete", // SDLK_DELETE
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "world 0", // SDLK_WORLD_0
            "world 1", // SDLK_WORLD_1
            "world 2", // SDLK_WORLD_2
            "world 3", // SDLK_WORLD_3
            "world 4", // SDLK_WORLD_4
            "world 5", // SDLK_WORLD_5
            "world 6", // SDLK_WORLD_6
            "world 7", // SDLK_WORLD_7
            "world 8", // SDLK_WORLD_8
            "world 9", // SDLK_WORLD_9
            "world 10", // SDLK_WORLD_10
            "world 11", // SDLK_WORLD_11
            "world 12", // SDLK_WORLD_12
            "world 13", // SDLK_WORLD_13
            "world 14", // SDLK_WORLD_14
            "world 15", // SDLK_WORLD_15
            "world 16", // SDLK_WORLD_16
            "world 17", // SDLK_WORLD_17
            "world 18", // SDLK_WORLD_18
            "world 19", // SDLK_WORLD_19
            "world 20", // SDLK_WORLD_20
            "world 21", // SDLK_WORLD_21
            "world 22", // SDLK_WORLD_22
            "world 23", // SDLK_WORLD_23
            "world 24", // SDLK_WORLD_24
            "world 25", // SDLK_WORLD_25
            "world 26", // SDLK_WORLD_26
            "world 27", // SDLK_WORLD_27
            "world 28", // SDLK_WORLD_28
            "world 29", // SDLK_WORLD_29
            "world 30", // SDLK_WORLD_30
            "world 31", // SDLK_WORLD_31
            "world 32", // SDLK_WORLD_32
            "world 33", // SDLK_WORLD_33
            "world 34", // SDLK_WORLD_34
            "world 35", // SDLK_WORLD_35
            "world 36", // SDLK_WORLD_36
            "world 37", // SDLK_WORLD_37
            "world 38", // SDLK_WORLD_38
            "world 39", // SDLK_WORLD_39
            "world 40", // SDLK_WORLD_40
            "world 41", // SDLK_WORLD_41
            "world 42", // SDLK_WORLD_42
            "world 43", // SDLK_WORLD_43
            "world 44", // SDLK_WORLD_44
            "world 45", // SDLK_WORLD_45
            "world 46", // SDLK_WORLD_46
            "world 47", // SDLK_WORLD_47
            "world 48", // SDLK_WORLD_48
            "world 49", // SDLK_WORLD_49
            "world 50", // SDLK_WORLD_50
            "world 51", // SDLK_WORLD_51
            "world 52", // SDLK_WORLD_52
            "world 53", // SDLK_WORLD_53
            "world 54", // SDLK_WORLD_54
            "world 55", // SDLK_WORLD_55
            "world 56", // SDLK_WORLD_56
            "world 57", // SDLK_WORLD_57
            "world 58", // SDLK_WORLD_58
            "world 59", // SDLK_WORLD_59
            "world 60", // SDLK_WORLD_60
            "world 61", // SDLK_WORLD_61
            "world 62", // SDLK_WORLD_62
            "world 63", // SDLK_WORLD_63
            "world 64", // SDLK_WORLD_64
            "world 65", // SDLK_WORLD_65
            "world 66", // SDLK_WORLD_66
            "world 67", // SDLK_WORLD_67
            "world 68", // SDLK_WORLD_68
            "world 69", // SDLK_WORLD_69
            "world 70", // SDLK_WORLD_70
            "world 71", // SDLK_WORLD_71
            "world 72", // SDLK_WORLD_72
            "world 73", // SDLK_WORLD_73
            "world 74", // SDLK_WORLD_74
            "world 75", // SDLK_WORLD_75
            "world 76", // SDLK_WORLD_76
            "world 77", // SDLK_WORLD_77
            "world 78", // SDLK_WORLD_78
            "world 79", // SDLK_WORLD_79
            "world 80", // SDLK_WORLD_80
            "world 81", // SDLK_WORLD_81
            "world 82", // SDLK_WORLD_82
            "world 83", // SDLK_WORLD_83
            "world 84", // SDLK_WORLD_84
            "world 85", // SDLK_WORLD_85
            "world 86", // SDLK_WORLD_86
            "world 87", // SDLK_WORLD_87
            "world 88", // SDLK_WORLD_88
            "world 89", // SDLK_WORLD_89
            "world 90", // SDLK_WORLD_90
            "world 91", // SDLK_WORLD_91
            "world 92", // SDLK_WORLD_92
            "world 93", // SDLK_WORLD_93
            "world 94", // SDLK_WORLD_94
            "world 95", // SDLK_WORLD_95
            "[0]", // SDLK_KP0
            "[1]", // SDLK_KP1
            "[2]", // SDLK_KP2
            "[3]", // SDLK_KP3
            "[4]", // SDLK_KP4
            "[5]", // SDLK_KP5
            "[6]", // SDLK_KP6
            "[7]", // SDLK_KP7
            "[8]", // SDLK_KP8
            "[9]", // SDLK_KP9
            "[.]", // SDLK_KP_PERIOD
            "[/]", // SDLK_KP_DIVIDE
            "[*]", // SDLK_KP_MULTIPLY
            "[-]", // SDLK_KP_MINUS
            "[+]", // SDLK_KP_PLUS
            "enter", // SDLK_KP_ENTER
            "equals", // SDLK_KP_EQUALS
            "up", // SDLK_UP
            "down", // SDLK_DOWN
            "right", // SDLK_RIGHT
            "left", // SDLK_LEFT
            "insert", // SDLK_INSERT
            "home", // SDLK_HOME
            "end", // SDLK_END
            "page up", // SDLKP_PAGEUP
            "page down", // SDLK_PAGEDOWN
            "f1", // SDLK_F1
            "f2", // SDLK_F2
            "f3", // SDLK_F3
            "f4", // SDLK_F4
            "f5", // SDLK_F5
            "f6", // SDLK_F6
            "f7", // SDLK_F7
            "f8", // SDLK_F8
            "f9", // SDLK_F9
            "f10", // SDLK_F10
            "f11", // SDLK_F11
            "f12", // SDLK_F12
            "f13", // SDLK_F13
            "f14", // SDLK_F14
            "f15", // SDLK_F15
            "",
            "",
            "",
            "numlock", // SDLK_NUMLOCK
            "caps lock", // SDLK_CAPSLOCK
            "scroll lock", // SDLK_SCROLLOCK
            "right shift", // SDLK_RSHIFT
            "left shift", // SDLK_LSHIFT
            "right ctrl", // SDLK_RCTRL
            "left ctrl", // SDLK_LCTRL
            "right alt", // SDLK_RALT
            "left alt", // SDLK_LALT
            "right cmd", // SDLK_RMETA
            "left cmd", // SDLK_LMETA
            "left super", // SDLK_LSUPER
            "right super", // SDLK_RSUPER
            "alt gr", // SDLK_MODE
            "compose", // SDLK_COMPOSE
            "help", // SDLK_HELP
            "print screen", // SDLK_PRINT
            "sys req", // SDLK_SYSREQ
            "break", // SDLK_BREAK
            "menu", // SDLK_MENU
            "power", // SDLK_POWER
            "euro", // SDLK_EURO
            "undo", // SDLK_UNDO
            "mouse 0",
            "mouse 1",
            "mouse 2",
            "mouse 3",
            "mouse 4",
            "mouse 5",
            "mouse 6",
            "joystick button 0",
            "joystick button 1",
            "joystick button 2",
            "joystick button 3",
            "joystick button 4",
            "joystick button 5",
            "joystick button 6",
            "joystick button 7",
            "joystick button 8",
            "joystick button 9",
            "joystick button 10",
            "joystick button 11",
            "joystick button 12",
            "joystick button 13",
            "joystick button 14",
            "joystick button 15",
            "joystick button 16",
            "joystick button 17",
            "joystick button 18",
            "joystick button 19",
            "joystick 1 button 0",
            "joystick 1 button 1",
            "joystick 1 button 2",
            "joystick 1 button 3",
            "joystick 1 button 4",
            "joystick 1 button 5",
            "joystick 1 button 6",
            "joystick 1 button 7",
            "joystick 1 button 8",
            "joystick 1 button 9",
            "joystick 1 button 10",
            "joystick 1 button 11",
            "joystick 1 button 12",
            "joystick 1 button 13",
            "joystick 1 button 14",
            "joystick 1 button 15",
            "joystick 1 button 16",
            "joystick 1 button 17",
            "joystick 1 button 18",
            "joystick 1 button 19",
            "joystick 2 button 0",
            "joystick 2 button 1",
            "joystick 2 button 2",
            "joystick 2 button 3",
            "joystick 2 button 4",
            "joystick 2 button 5",
            "joystick 2 button 6",
            "joystick 2 button 7",
            "joystick 2 button 8",
            "joystick 2 button 9",
            "joystick 2 button 10",
            "joystick 2 button 11",
            "joystick 2 button 12",
            "joystick 2 button 13",
            "joystick 2 button 14",
            "joystick 2 button 15",
            "joystick 2 button 16",
            "joystick 2 button 17",
            "joystick 2 button 18",
            "joystick 2 button 19",
            "joystick 3 button 0",
            "joystick 3 button 1",
            "joystick 3 button 2",
            "joystick 3 button 3",
            "joystick 3 button 4",
            "joystick 3 button 5",
            "joystick 3 button 6",
            "joystick 3 button 7",
            "joystick 3 button 8",
            "joystick 3 button 9",
            "joystick 3 button 10",
            "joystick 3 button 11",
            "joystick 3 button 12",
            "joystick 3 button 13",
            "joystick 3 button 14",
            "joystick 3 button 15",
            "joystick 3 button 16",
            "joystick 3 button 17",
            "joystick 3 button 18",
            "joystick 3 button 19",
            "joystick 4 button 0",
            "joystick 4 button 1",
            "joystick 4 button 2",
            "joystick 4 button 3",
            "joystick 4 button 4",
            "joystick 4 button 5",
            "joystick 4 button 6",
            "joystick 4 button 7",
            "joystick 4 button 8",
            "joystick 4 button 9",
            "joystick 4 button 10",
            "joystick 4 button 11",
            "joystick 4 button 12",
            "joystick 4 button 13",
            "joystick 4 button 14",
            "joystick 4 button 15",
            "joystick 4 button 16",
            "joystick 4 button 17",
            "joystick 4 button 18",
            "joystick 4 button 19",
            "joystick 5 button 0",
            "joystick 5 button 1",
            "joystick 5 button 2",
            "joystick 5 button 3",
            "joystick 5 button 4",
            "joystick 5 button 5",
            "joystick 5 button 6",
            "joystick 5 button 7",
            "joystick 5 button 8",
            "joystick 5 button 9",
            "joystick 5 button 10",
            "joystick 5 button 11",
            "joystick 5 button 12",
            "joystick 5 button 13",
            "joystick 5 button 14",
            "joystick 5 button 15",
            "joystick 5 button 16",
            "joystick 5 button 17",
            "joystick 5 button 18",
            "joystick 5 button 19",
            "joystick 6 button 0",
            "joystick 6 button 1",
            "joystick 6 button 2",
            "joystick 6 button 3",
            "joystick 6 button 4",
            "joystick 6 button 5",
            "joystick 6 button 6",
            "joystick 6 button 7",
            "joystick 6 button 8",
            "joystick 6 button 9",
            "joystick 6 button 10",
            "joystick 6 button 11",
            "joystick 6 button 12",
            "joystick 6 button 13",
            "joystick 6 button 14",
            "joystick 6 button 15",
            "joystick 6 button 16",
            "joystick 6 button 17",
            "joystick 6 button 18",
            "joystick 6 button 19",
            "joystick 7 button 0",
            "joystick 7 button 1",
            "joystick 7 button 2",
            "joystick 7 button 3",
            "joystick 7 button 4",
            "joystick 7 button 5",
            "joystick 7 button 6",
            "joystick 7 button 7",
            "joystick 7 button 8",
            "joystick 7 button 9",
            "joystick 7 button 10",
            "joystick 7 button 11",
            "joystick 7 button 12",
            "joystick 7 button 13",
            "joystick 7 button 14",
            "joystick 7 button 15",
            "joystick 7 button 16",
            "joystick 7 button 17",
            "joystick 7 button 18",
            "joystick 7 button 19",
            "joystick 8 button 0",
            "joystick 8 button 1",
            "joystick 8 button 2",
            "joystick 8 button 3",
            "joystick 8 button 4",
            "joystick 8 button 5",
            "joystick 8 button 6",
            "joystick 8 button 7",
            "joystick 8 button 8",
            "joystick 8 button 9",
            "joystick 8 button 10",
            "joystick 8 button 11",
            "joystick 8 button 12",
            "joystick 8 button 13",
            "joystick 8 button 14",
            "joystick 8 button 15",
            "joystick 8 button 16",
            "joystick 8 button 17",
            "joystick 8 button 18",
            "joystick 8 button 19",
            "joystick 9 button 0",
            "joystick 9 button 1",
            "joystick 9 button 2",
            "joystick 9 button 3",
            "joystick 9 button 4",
            "joystick 9 button 5",
            "joystick 9 button 6",
            "joystick 9 button 7",
            "joystick 9 button 8",
            "joystick 9 button 9",
            "joystick 9 button 10",
            "joystick 9 button 11",
            "joystick 9 button 12",
            "joystick 9 button 13",
            "joystick 9 button 14",
            "joystick 9 button 15",
            "joystick 9 button 16",
            "joystick 9 button 17",
            "joystick 9 button 18",
            "joystick 9 button 19",
            "joystick 10 button 0",
            "joystick 10 button 1",
            "joystick 10 button 2",
            "joystick 10 button 3",
            "joystick 10 button 4",
            "joystick 10 button 5",
            "joystick 10 button 6",
            "joystick 10 button 7",
            "joystick 10 button 8",
            "joystick 10 button 9",
            "joystick 10 button 10",
            "joystick 10 button 11",
            "joystick 10 button 12",
            "joystick 10 button 13",
            "joystick 10 button 14",
            "joystick 10 button 15",
            "joystick 10 button 16",
            "joystick 10 button 17",
            "joystick 10 button 18",
            "joystick 10 button 19",
            "joystick 11 button 0",
            "joystick 11 button 1",
            "joystick 11 button 2",
            "joystick 11 button 3",
            "joystick 11 button 4",
            "joystick 11 button 5",
            "joystick 11 button 6",
            "joystick 11 button 7",
            "joystick 11 button 8",
            "joystick 11 button 9",
            "joystick 11 button 10",
            "joystick 11 button 11",
            "joystick 11 button 12",
            "joystick 11 button 13",
            "joystick 11 button 14",
            "joystick 11 button 15",
            "joystick 11 button 16",
            "joystick 11 button 17",
            "joystick 11 button 18",
            "joystick 11 button 19",
            "joystick 12 button 0",
            "joystick 12 button 1",
            "joystick 12 button 2",
            "joystick 12 button 3",
            "joystick 12 button 4",
            "joystick 12 button 5",
            "joystick 12 button 6",
            "joystick 12 button 7",
            "joystick 12 button 8",
            "joystick 12 button 9",
            "joystick 12 button 10",
            "joystick 12 button 11",
            "joystick 12 button 12",
            "joystick 12 button 13",
            "joystick 12 button 14",
            "joystick 12 button 15",
            "joystick 12 button 16",
            "joystick 12 button 17",
            "joystick 12 button 18",
            "joystick 12 button 19",
            "joystick 13 button 0",
            "joystick 13 button 1",
            "joystick 13 button 2",
            "joystick 13 button 3",
            "joystick 13 button 4",
            "joystick 13 button 5",
            "joystick 13 button 6",
            "joystick 13 button 7",
            "joystick 13 button 8",
            "joystick 13 button 9",
            "joystick 13 button 10",
            "joystick 13 button 11",
            "joystick 13 button 12",
            "joystick 13 button 13",
            "joystick 13 button 14",
            "joystick 13 button 15",
            "joystick 13 button 16",
            "joystick 13 button 17",
            "joystick 13 button 18",
            "joystick 13 button 19",
            "joystick 14 button 0",
            "joystick 14 button 1",
            "joystick 14 button 2",
            "joystick 14 button 3",
            "joystick 14 button 4",
            "joystick 14 button 5",
            "joystick 14 button 6",
            "joystick 14 button 7",
            "joystick 14 button 8",
            "joystick 14 button 9",
            "joystick 14 button 10",
            "joystick 14 button 11",
            "joystick 14 button 12",
            "joystick 14 button 13",
            "joystick 14 button 14",
            "joystick 14 button 15",
            "joystick 14 button 16",
            "joystick 14 button 17",
            "joystick 14 button 18",
            "joystick 14 button 19",
            "joystick 15 button 0",
            "joystick 15 button 1",
            "joystick 15 button 2",
            "joystick 15 button 3",
            "joystick 15 button 4",
            "joystick 15 button 5",
            "joystick 15 button 6",
            "joystick 15 button 7",
            "joystick 15 button 8",
            "joystick 15 button 9",
            "joystick 15 button 10",
            "joystick 15 button 11",
            "joystick 15 button 12",
            "joystick 15 button 13",
            "joystick 15 button 14",
            "joystick 15 button 15",
            "joystick 15 button 16",
            "joystick 15 button 17",
            "joystick 15 button 18",
            "joystick 15 button 19",
            "joystick 16 button 0",
            "joystick 16 button 1",
            "joystick 16 button 2",
            "joystick 16 button 3",
            "joystick 16 button 4",
            "joystick 16 button 5",
            "joystick 16 button 6",
            "joystick 16 button 7",
            "joystick 16 button 8",
            "joystick 16 button 9",
            "joystick 16 button 10",
            "joystick 16 button 11",
            "joystick 16 button 12",
            "joystick 16 button 13",
            "joystick 16 button 14",
            "joystick 16 button 15",
            "joystick 16 button 16",
            "joystick 16 button 17",
            "joystick 16 button 18",
            "joystick 16 button 19"
        };
    }
}