#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace UnityEngine.InputLegacy
{
    public static class WindowsKeyboardMapping
    {
        static WindowsKeyboardMapping()
        {
            foreach (var pair in s_VirtualKeyToSdlKey)
                if (s_SdlKeyToVirtualKey.TryGetValue(pair.Value, out var list))
                    list.Add(pair.Key);
                else
                    s_SdlKeyToVirtualKey[pair.Value] = new List<VK> {pair.Key};

            foreach (var pair in s_KeyToScanCode)
            {
                var key = (pair.scancode, pair.extended);
                if (s_ScanCodeToKey.ContainsKey(key))
                    Debug.Assert(false);
                s_ScanCodeToKey[key] = pair.key;
            }
        }

        // Some virtual keys should map to extended scan codes based on state of shift/numlock,
        // but MapVirtualKeyEx doesn't take into the account the state of shift/numlock.
        private static VK[] VirtualKeysBasedOnShiftNumlockStatus(VK key, bool shiftStatus, bool numlockStatus)
        {
            if ((numlockStatus && !shiftStatus) || (!numlockStatus && shiftStatus))
                return new[] {key};

            switch (key)
            {
                // In numlock state extended keys should work if any of two are pressed.
                // So VK.INSERT should trigger when either VK.INSERT or VK.NUMPAD0 are pressed.
                case VK.INSERT: return new[] {key, VK.NUMPAD0};
                case VK.DELETE: return new[] {key, VK.DECIMAL};
                case VK.HOME: return new[] {key, VK.NUMPAD7};
                case VK.END: return new[] {key, VK.NUMPAD1};
                case VK.PRIOR: return new[] {key, VK.NUMPAD9};
                case VK.NEXT: return new[] {key, VK.NUMPAD3};
                case VK.LEFT: return new[] {key, VK.NUMPAD4};
                case VK.RIGHT: return new[] {key, VK.NUMPAD6};
                case VK.UP: return new[] {key, VK.NUMPAD8};
                case VK.DOWN: return new[] {key, VK.NUMPAD2};
                case VK.CLEAR: return new[] {key, VK.NUMPAD5};

                // If we're in numlock state, old system ignores numpad keys.
                case VK.NUMPAD0:
                case VK.NUMPAD1:
                case VK.NUMPAD2:
                case VK.NUMPAD3:
                case VK.NUMPAD4:
                case VK.NUMPAD5:
                case VK.NUMPAD6:
                case VK.NUMPAD7:
                case VK.NUMPAD8:
                case VK.NUMPAD9:
                case VK.DECIMAL:
                    return new VK[] { };
            }

            return new[] {key};
        }

        // Some virtual keys should always map to extended scancode for our mapping,
        // but MapVirtualKeyEx returns non-extended scancodes for them.
        // This function returns true if we should override MapVirtualKeyEx result and use extended scancode instead.
        private static bool ShouldUseExtendedScanCode(VK key)
        {
            // Extended keys should always use extended scancode.
            switch (key)
            {
                case VK.INSERT:
                case VK.DELETE:
                case VK.HOME:
                case VK.END:
                case VK.PRIOR:
                case VK.NEXT:
                case VK.LEFT:
                case VK.RIGHT:
                case VK.UP:
                case VK.DOWN:
                    return true;
            }

            return false;
        }

        // For some keys we do custom processing in the native backend,
        // so we should use custom mapping between virtual key and scancode for them.
        private static uint? DirectResolveScanCode(VK key)
        {
            uint extended = 0xE000;
            switch (key)
            {
                case VK.PAUSE:
                    return 0x46 + extended;
                case VK.KEYPAD_ENTER:
                    return 0x1C + extended;
            }

            return null;
        }

        public static bool GetNumlockState()
        {
            // Virtual shift status is NOT accurate, it can change when using numpad
            // see https://stackoverflow.com/questions/24822505/how-to-tell-if-shift-is-pressed-on-numpad-input-with-numlock-on-or-at-least-get
            // So we're using raw input to get shift status instead.
            return (GetKeyState((int) VK.NUMLOCK) & 0xFF) != 0;
        }

        private static List<ButtonControl> MapVirtualKeyCodesToKeys(List<VK> virtualKeyCodes, bool shiftStatus,
            bool numlockStatus, IntPtr layout)
        {
            var mappedKeys = new List<ButtonControl>();
            foreach (var virtualKeyCodeAsHardMapped in virtualKeyCodes)
            {
                foreach (var virtualKeyCode in VirtualKeysBasedOnShiftNumlockStatus(
                    virtualKeyCodeAsHardMapped,
                    shiftStatus, numlockStatus))
                {
                    var scanCode = DirectResolveScanCode(virtualKeyCode) ?? MapVirtualKeyEx(
                        (uint) virtualKeyCode,
                        MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC_EX,
                        layout);

                    var scanCodeLowPart = scanCode & 0xFF;

                    var isExtended = ((scanCode & (0xE000) + scanCode & (0xE100)) != 0) ||
                                     ShouldUseExtendedScanCode(virtualKeyCode);

                    if (!s_ScanCodeToKey.TryGetValue((scanCodeLowPart, isExtended), out var key) ||
                        Keyboard.current == null) continue;

                    if (virtualKeyCode == VK.LWIN || virtualKeyCode == VK.RWIN)
                    {
                        Debug.Log($"VK {virtualKeyCode} is mapped to scancode {scanCode:X}");
                    }

                    var control = Keyboard.current[key];
                    if (control != null)
                        mappedKeys.Add(control);
                }
            }

            return mappedKeys;
        }

        public static Dictionary<(KeyCode keyCode, bool shiftStatus, bool numlockStatus), ButtonControl[]>
            GetMappingForCurrentLayout()
        {
            var layout = GetKeyboardLayout(0);

            var mapping = new Dictionary<(KeyCode keyCode, bool shiftStatus, bool numlockStatus), ButtonControl[]>();

            foreach (var keyCode in (KeyCode[]) Enum.GetValues(typeof(KeyCode)))
            {
                if (!s_SdlKeyToVirtualKey.TryGetValue((SDLK) keyCode, out var virtualKeyCodes))
                    continue;

                foreach (var shiftStatus in new[] {false, true})
                foreach (var numlockStatus in new[] {false, true})
                    mapping[(keyCode, shiftStatus, numlockStatus)] =
                        MapVirtualKeyCodesToKeys(virtualKeyCodes, shiftStatus, numlockStatus, layout).ToArray();
            }

            return mapping;
        }

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKeyEx(uint uCode, MapVirtualKeyMapTypes uMapType, IntPtr dwhkl);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private enum MapVirtualKeyMapTypes : uint
        {
            MAPVK_VK_TO_VSC = 0x00,
            MAPVK_VSC_TO_VK = 0x01,
            MAPVK_VK_TO_CHAR = 0x02,
            MAPVK_VSC_TO_VK_EX = 0x03,
            MAPVK_VK_TO_VSC_EX = 0x04
        }

        private enum SDLK
        {
            UNKNOWN = 0,
            FIRST = 0,
            BACKSPACE = 8,
            TAB = 9,
            CLEAR = 12,
            RETURN = 13,
            PAUSE = 19,
            ESCAPE = 27,
            SPACE = 32,
            EXCLAIM = 33,
            QUOTEDBL = 34,
            HASH = 35,
            DOLLAR = 36,
            PERCENT = 37,
            AMPERSAND = 38,
            QUOTE = 39,
            LEFTPAREN = 40,
            RIGHTPAREN = 41,
            ASTERISK = 42,
            PLUS = 43,
            COMMA = 44,
            MINUS = 45,
            PERIOD = 46,
            SLASH = 47,
            DIGIT_0 = 48,
            DIGIT_1 = 49,
            DIGIT_2 = 50,
            DIGIT_3 = 51,
            DIGIT_4 = 52,
            DIGIT_5 = 53,
            DIGIT_6 = 54,
            DIGIT_7 = 55,
            DIGIT_8 = 56,
            DIGIT_9 = 57,
            COLON = 58,
            SEMICOLON = 59,
            LESS = 60,
            EQUALS = 61,
            GREATER = 62,
            QUESTION = 63,
            AT = 64,
            LEFTBRACKET = 91,
            BACKSLASH = 92,
            RIGHTBRACKET = 93,
            CARET = 94,
            UNDERSCORE = 95,
            BACKQUOTE = 96,
            LOWER_A = 97,
            LOWER_B = 98,
            LOWER_C = 99,
            LOWER_D = 100,
            LOWER_E = 101,
            LOWER_F = 102,
            LOWER_G = 103,
            LOWER_H = 104,
            LOWER_I = 105,
            LOWER_J = 106,
            LOWER_K = 107,
            LOWER_L = 108,
            LOWER_M = 109,
            LOWER_N = 110,
            LOWER_O = 111,
            LOWER_P = 112,
            LOWER_Q = 113,
            LOWER_R = 114,
            LOWER_S = 115,
            LOWER_T = 116,
            LOWER_U = 117,
            LOWER_V = 118,
            LOWER_W = 119,
            LOWER_X = 120,
            LOWER_Y = 121,
            LOWER_Z = 122,
            LEFTCURLYBRACKET = 123,
            PIPE = 124,
            RIGHTCURLYBRACKET = 125,
            TILDE = 126,
            DELETE = 127,
            WORLD_0 = 160,
            WORLD_1 = 161,
            WORLD_2 = 162,
            WORLD_3 = 163,
            WORLD_4 = 164,
            WORLD_5 = 165,
            WORLD_6 = 166,
            WORLD_7 = 167,
            WORLD_8 = 168,
            WORLD_9 = 169,
            WORLD_10 = 170,
            WORLD_11 = 171,
            WORLD_12 = 172,
            WORLD_13 = 173,
            WORLD_14 = 174,
            WORLD_15 = 175,
            WORLD_16 = 176,
            WORLD_17 = 177,
            WORLD_18 = 178,
            WORLD_19 = 179,
            WORLD_20 = 180,
            WORLD_21 = 181,
            WORLD_22 = 182,
            WORLD_23 = 183,
            WORLD_24 = 184,
            WORLD_25 = 185,
            WORLD_26 = 186,
            WORLD_27 = 187,
            WORLD_28 = 188,
            WORLD_29 = 189,
            WORLD_30 = 190,
            WORLD_31 = 191,
            WORLD_32 = 192,
            WORLD_33 = 193,
            WORLD_34 = 194,
            WORLD_35 = 195,
            WORLD_36 = 196,
            WORLD_37 = 197,
            WORLD_38 = 198,
            WORLD_39 = 199,
            WORLD_40 = 200,
            WORLD_41 = 201,
            WORLD_42 = 202,
            WORLD_43 = 203,
            WORLD_44 = 204,
            WORLD_45 = 205,
            WORLD_46 = 206,
            WORLD_47 = 207,
            WORLD_48 = 208,
            WORLD_49 = 209,
            WORLD_50 = 210,
            WORLD_51 = 211,
            WORLD_52 = 212,
            WORLD_53 = 213,
            WORLD_54 = 214,
            WORLD_55 = 215,
            WORLD_56 = 216,
            WORLD_57 = 217,
            WORLD_58 = 218,
            WORLD_59 = 219,
            WORLD_60 = 220,
            WORLD_61 = 221,
            WORLD_62 = 222,
            WORLD_63 = 223,
            WORLD_64 = 224,
            WORLD_65 = 225,
            WORLD_66 = 226,
            WORLD_67 = 227,
            WORLD_68 = 228,
            WORLD_69 = 229,
            WORLD_70 = 230,
            WORLD_71 = 231,
            WORLD_72 = 232,
            WORLD_73 = 233,
            WORLD_74 = 234,
            WORLD_75 = 235,
            WORLD_76 = 236,
            WORLD_77 = 237,
            WORLD_78 = 238,
            WORLD_79 = 239,
            WORLD_80 = 240,
            WORLD_81 = 241,
            WORLD_82 = 242,
            WORLD_83 = 243,
            WORLD_84 = 244,
            WORLD_85 = 245,
            WORLD_86 = 246,
            WORLD_87 = 247,
            WORLD_88 = 248,
            WORLD_89 = 249,
            WORLD_90 = 250,
            WORLD_91 = 251,
            WORLD_92 = 252,
            WORLD_93 = 253,
            WORLD_94 = 254,
            WORLD_95 = 255,
            KP0 = 256,
            KP1 = 257,
            KP2 = 258,
            KP3 = 259,
            KP4 = 260,
            KP5 = 261,
            KP6 = 262,
            KP7 = 263,
            KP8 = 264,
            KP9 = 265,
            KP_PERIOD = 266,
            KP_DIVIDE = 267,
            KP_MULTIPLY = 268,
            KP_MINUS = 269,
            KP_PLUS = 270,
            KP_ENTER = 271,
            KP_EQUALS = 272,
            UP = 273,
            DOWN = 274,
            RIGHT = 275,
            LEFT = 276,
            INSERT = 277,
            HOME = 278,
            END = 279,
            PAGEUP = 280,
            PAGEDOWN = 281,
            F1 = 282,
            F2 = 283,
            F3 = 284,
            F4 = 285,
            F5 = 286,
            F6 = 287,
            F7 = 288,
            F8 = 289,
            F9 = 290,
            F10 = 291,
            F11 = 292,
            F12 = 293,
            F13 = 294,
            F14 = 295,
            F15 = 296,
            NUMLOCK = 300,
            CAPSLOCK = 301,
            SCROLLOCK = 302,
            RSHIFT = 303,
            LSHIFT = 304,
            RCTRL = 305,
            LCTRL = 306,
            RALT = 307,
            LALT = 308,
            RMETA = 309,
            LMETA = 310,
            RGUI = 309,
            LGUI = 310,
            LSUPER = 311,
            RSUPER = 312,
            MODE = 313,
            COMPOSE = 314,
            HELP = 315,
            PRINT = 316,
            SYSREQ = 317,
            BREAK = 318,
            MENU = 319,
            POWER = 320,
            EURO = 321,
            UNDO = 322,
            SDLK_LAST
        };

        private enum VK
        {
            LBUTTON = 0x01,
            RBUTTON = 0x02,
            CANCEL = 0x03,
            MBUTTON = 0x04,
            XBUTTON1 = 0x05,
            XBUTTON2 = 0x06,
            BACK = 0x08,
            TAB = 0x09,
            CLEAR = 0x0C,
            RETURN = 0x0D,
            SHIFT = 0x10,
            CONTROL = 0x11,
            MENU = 0x12,
            PAUSE = 0x13,
            CAPITAL = 0x14,
            KANA = 0x15,
            HANGEUL = 0x15,
            HANGUL = 0x15,
            JUNJA = 0x17,
            FINAL = 0x18,
            HANJA = 0x19,
            KANJI = 0x19,
            ESCAPE = 0x1B,
            CONVERT = 0x1C,
            NONCONVERT = 0x1D,
            ACCEPT = 0x1E,
            MODECHANGE = 0x1F,
            SPACE = 0x20,
            PRIOR = 0x21,
            NEXT = 0x22,
            END = 0x23,
            HOME = 0x24,
            LEFT = 0x25,
            UP = 0x26,
            RIGHT = 0x27,
            DOWN = 0x28,
            SELECT = 0x29,
            PRINT = 0x2A,
            EXECUTE = 0x2B,
            SNAPSHOT = 0x2C,
            INSERT = 0x2D,
            DELETE = 0x2E,
            HELP = 0x2F,
            DIGIT_0 = 48,
            DIGIT_1 = 49,
            DIGIT_2 = 50,
            DIGIT_3 = 51,
            DIGIT_4 = 52,
            DIGIT_5 = 53,
            DIGIT_6 = 54,
            DIGIT_7 = 55,
            DIGIT_8 = 56,
            DIGIT_9 = 57,
            UPPER_A = 65,
            UPPER_B = 66,
            UPPER_C = 67,
            UPPER_D = 68,
            UPPER_E = 69,
            UPPER_F = 70,
            UPPER_G = 71,
            UPPER_H = 72,
            UPPER_I = 73,
            UPPER_J = 74,
            UPPER_K = 75,
            UPPER_L = 76,
            UPPER_M = 77,
            UPPER_N = 78,
            UPPER_O = 79,
            UPPER_P = 80,
            UPPER_Q = 81,
            UPPER_R = 82,
            UPPER_S = 83,
            UPPER_T = 84,
            UPPER_U = 85,
            UPPER_V = 86,
            UPPER_W = 87,
            UPPER_X = 88,
            UPPER_Y = 89,
            UPPER_Z = 90,
            LWIN = 0x5B,
            RWIN = 0x5C,
            APPS = 0x5D,
            SLEEP = 0x5F,
            NUMPAD0 = 0x60,
            NUMPAD1 = 0x61,
            NUMPAD2 = 0x62,
            NUMPAD3 = 0x63,
            NUMPAD4 = 0x64,
            NUMPAD5 = 0x65,
            NUMPAD6 = 0x66,
            NUMPAD7 = 0x67,
            NUMPAD8 = 0x68,
            NUMPAD9 = 0x69,
            MULTIPLY = 0x6A,
            ADD = 0x6B,
            SEPARATOR = 0x6C,
            SUBTRACT = 0x6D,
            DECIMAL = 0x6E,
            DIVIDE = 0x6F,
            F1 = 0x70,
            F2 = 0x71,
            F3 = 0x72,
            F4 = 0x73,
            F5 = 0x74,
            F6 = 0x75,
            F7 = 0x76,
            F8 = 0x77,
            F9 = 0x78,
            F10 = 0x79,
            F11 = 0x7A,
            F12 = 0x7B,
            F13 = 0x7C,
            F14 = 0x7D,
            F15 = 0x7E,
            F16 = 0x7F,
            F17 = 0x80,
            F18 = 0x81,
            F19 = 0x82,
            F20 = 0x83,
            F21 = 0x84,
            F22 = 0x85,
            F23 = 0x86,
            F24 = 0x87,
            NAVIGATION_VIEW = 0x88,
            NAVIGATION_MENU = 0x89,
            NAVIGATION_UP = 0x8A,
            NAVIGATION_DOWN = 0x8B,
            NAVIGATION_LEFT = 0x8C,
            NAVIGATION_RIGHT = 0x8D,
            NAVIGATION_ACCEPT = 0x8E,
            NAVIGATION_CANCEL = 0x8F,
            NUMLOCK = 0x90,
            SCROLL = 0x91,
            OEM_NEC_EQUAL = 0x92,
            OEM_FJ_JISHO = 0x92,
            OEM_FJ_MASSHOU = 0x93,
            OEM_FJ_TOUROKU = 0x94,
            OEM_FJ_LOYA = 0x95,
            OEM_FJ_ROYA = 0x96,
            LSHIFT = 0xA0,
            RSHIFT = 0xA1,
            LCONTROL = 0xA2,
            RCONTROL = 0xA3,
            LMENU = 0xA4,
            RMENU = 0xA5,
            BROWSER_BACK = 0xA6,
            BROWSER_FORWARD = 0xA7,
            BROWSER_REFRESH = 0xA8,
            BROWSER_STOP = 0xA9,
            BROWSER_SEARCH = 0xAA,
            BROWSER_FAVORITES = 0xAB,
            BROWSER_HOME = 0xAC,
            VOLUME_MUTE = 0xAD,
            VOLUME_DOWN = 0xAE,
            VOLUME_UP = 0xAF,
            MEDIA_NEXT_TRACK = 0xB0,
            MEDIA_PREV_TRACK = 0xB1,
            MEDIA_STOP = 0xB2,
            MEDIA_PLAY_PAUSE = 0xB3,
            LAUNCH_MAIL = 0xB4,
            LAUNCH_MEDIA_SELECT = 0xB5,
            LAUNCH_APP1 = 0xB6,
            LAUNCH_APP2 = 0xB7,
            OEM_1 = 0xBA,
            OEM_PLUS = 0xBB,
            OEM_COMMA = 0xBC,
            OEM_MINUS = 0xBD,
            OEM_PERIOD = 0xBE,
            OEM_2 = 0xBF,
            OEM_3 = 0xC0,
            GAMEPAD_A = 0xC3,
            GAMEPAD_B = 0xC4,
            GAMEPAD_X = 0xC5,
            GAMEPAD_Y = 0xC6,
            GAMEPAD_RIGHT_SHOULDER = 0xC7,
            GAMEPAD_LEFT_SHOULDER = 0xC8,
            GAMEPAD_LEFT_TRIGGER = 0xC9,
            GAMEPAD_RIGHT_TRIGGER = 0xCA,
            GAMEPAD_DPAD_UP = 0xCB,
            GAMEPAD_DPAD_DOWN = 0xCC,
            GAMEPAD_DPAD_LEFT = 0xCD,
            GAMEPAD_DPAD_RIGHT = 0xCE,
            GAMEPAD_MENU = 0xCF,
            GAMEPAD_VIEW = 0xD0,
            GAMEPAD_LEFT_THUMBSTICK_BUTTON = 0xD1,
            GAMEPAD_RIGHT_THUMBSTICK_BUTTON = 0xD2,
            GAMEPAD_LEFT_THUMBSTICK_UP = 0xD3,
            GAMEPAD_LEFT_THUMBSTICK_DOWN = 0xD4,
            GAMEPAD_LEFT_THUMBSTICK_RIGHT = 0xD5,
            GAMEPAD_LEFT_THUMBSTICK_LEFT = 0xD6,
            GAMEPAD_RIGHT_THUMBSTICK_UP = 0xD7,
            GAMEPAD_RIGHT_THUMBSTICK_DOWN = 0xD8,
            GAMEPAD_RIGHT_THUMBSTICK_RIGHT = 0xD9,
            GAMEPAD_RIGHT_THUMBSTICK_LEFT = 0xDA,
            OEM_4 = 0xDB,
            OEM_5 = 0xDC,
            OEM_6 = 0xDD,
            OEM_7 = 0xDE,
            OEM_8 = 0xDF,
            OEM_AX = 0xE1,
            OEM_102 = 0xE2,
            ICO_HELP = 0xE3,
            ICO_00 = 0xE4,
            PROCESSKEY = 0xE5,
            ICO_CLEAR = 0xE6,
            PACKET = 0xE7,
            OEM_RESET = 0xE9,
            OEM_JUMP = 0xEA,
            OEM_PA1 = 0xEB,
            OEM_PA2 = 0xEC,
            OEM_PA3 = 0xED,
            OEM_WSCTRL = 0xEE,
            OEM_CUSEL = 0xEF,
            OEM_ATTN = 0xF0,
            OEM_FINISH = 0xF1,
            OEM_COPY = 0xF2,
            OEM_AUTO = 0xF3,
            OEM_ENLW = 0xF4,
            OEM_BACKTAB = 0xF5,
            ATTN = 0xF6,
            CRSEL = 0xF7,
            EXSEL = 0xF8,
            EREOF = 0xF9,
            PLAY = 0xFA,
            ZOOM = 0xFB,
            NONAME = 0xFC,
            PA1 = 0xFD,
            OEM_CLEAR = 0xFE,

            // Not a real VK, but just a wrapper to pipe keypad enter through to scancodes.
            // Native backend does custom handling of VK_RETURN to get the extended flag.
            KEYPAD_ENTER = 0xFF00,
        }

        private static IDictionary<SDLK, List<VK>> s_SdlKeyToVirtualKey = new Dictionary<SDLK, List<VK>>();

        private static IDictionary<VK, SDLK> s_VirtualKeyToSdlKey = new Dictionary<VK, SDLK>()
        {
            {VK.ESCAPE, SDLK.ESCAPE},
            {VK.DIGIT_1, SDLK.DIGIT_1},
            {VK.DIGIT_2, SDLK.DIGIT_2},
            {VK.DIGIT_3, SDLK.DIGIT_3},
            {VK.DIGIT_4, SDLK.DIGIT_4},
            {VK.DIGIT_5, SDLK.DIGIT_5},
            {VK.DIGIT_6, SDLK.DIGIT_6},
            {VK.DIGIT_7, SDLK.DIGIT_7},
            {VK.DIGIT_8, SDLK.DIGIT_8},
            {VK.DIGIT_9, SDLK.DIGIT_9},
            {VK.DIGIT_0, SDLK.DIGIT_0},
            {VK.OEM_MINUS, SDLK.MINUS},
            {VK.OEM_PLUS, SDLK.EQUALS},
            {VK.BACK, SDLK.BACKSPACE},
            {VK.TAB, SDLK.TAB},
            {VK.UPPER_Q, SDLK.LOWER_Q},
            {VK.UPPER_W, SDLK.LOWER_W},
            {VK.UPPER_E, SDLK.LOWER_E},
            {VK.UPPER_R, SDLK.LOWER_R},
            {VK.UPPER_T, SDLK.LOWER_T},
            {VK.UPPER_Y, SDLK.LOWER_Y},
            {VK.UPPER_U, SDLK.LOWER_U},
            {VK.UPPER_I, SDLK.LOWER_I},
            {VK.UPPER_O, SDLK.LOWER_O},
            {VK.UPPER_P, SDLK.LOWER_P},
            {VK.OEM_4, SDLK.LEFTBRACKET},
            {VK.OEM_6, SDLK.RIGHTBRACKET},
            {VK.RETURN, SDLK.RETURN},
            {VK.LCONTROL, SDLK.LCTRL},
            {VK.CONTROL, SDLK.LCTRL},
            {VK.UPPER_A, SDLK.LOWER_A},
            {VK.UPPER_S, SDLK.LOWER_S},
            {VK.UPPER_D, SDLK.LOWER_D},
            {VK.UPPER_F, SDLK.LOWER_F},
            {VK.UPPER_G, SDLK.LOWER_G},
            {VK.UPPER_H, SDLK.LOWER_H},
            {VK.UPPER_J, SDLK.LOWER_J},
            {VK.UPPER_K, SDLK.LOWER_K},
            {VK.UPPER_L, SDLK.LOWER_L},
            {VK.OEM_1, SDLK.SEMICOLON},
            {VK.OEM_7, SDLK.QUOTE},
            {VK.OEM_3, SDLK.BACKQUOTE},
            {VK.OEM_8, SDLK.BACKQUOTE},
            {VK.LSHIFT, SDLK.LSHIFT},
            {VK.OEM_5, SDLK.BACKSLASH},
            {VK.OEM_102, SDLK.BACKSLASH},
            {VK.UPPER_Z, SDLK.LOWER_Z},
            {VK.UPPER_X, SDLK.LOWER_X},
            {VK.UPPER_C, SDLK.LOWER_C},
            {VK.UPPER_V, SDLK.LOWER_V},
            {VK.UPPER_B, SDLK.LOWER_B},
            {VK.UPPER_N, SDLK.LOWER_N},
            {VK.UPPER_M, SDLK.LOWER_M},
            {VK.OEM_COMMA, SDLK.COMMA},
            {VK.OEM_PERIOD, SDLK.PERIOD},
            {VK.OEM_2, SDLK.SLASH},
            {VK.RSHIFT, SDLK.RSHIFT},
            {VK.MULTIPLY, SDLK.KP_MULTIPLY},
            {VK.LMENU, SDLK.LALT},
            {VK.SPACE, SDLK.SPACE},
            {VK.CAPITAL, SDLK.CAPSLOCK},
            {VK.F1, SDLK.F1},
            {VK.F2, SDLK.F2},
            {VK.F3, SDLK.F3},
            {VK.F4, SDLK.F4},
            {VK.F5, SDLK.F5},
            {VK.F6, SDLK.F6},
            {VK.F7, SDLK.F7},
            {VK.F8, SDLK.F8},
            {VK.F9, SDLK.F9},
            {VK.F10, SDLK.F10},
            {VK.NUMLOCK, SDLK.NUMLOCK},
            {VK.SCROLL, SDLK.SCROLLOCK},
            {VK.NUMPAD7, SDLK.KP7},
            {VK.NUMPAD8, SDLK.KP8},
            {VK.NUMPAD9, SDLK.KP9},
            {VK.SUBTRACT, SDLK.KP_MINUS},
            {VK.NUMPAD4, SDLK.KP4},
            {VK.NUMPAD5, SDLK.KP5},
            {VK.NUMPAD6, SDLK.KP6},
            {VK.ADD, SDLK.KP_PLUS},
            {VK.NUMPAD1, SDLK.KP1},
            {VK.NUMPAD2, SDLK.KP2},
            {VK.NUMPAD3, SDLK.KP3},
            {VK.NUMPAD0, SDLK.KP0},
            {VK.DECIMAL, SDLK.KP_PERIOD},
            {VK.F11, SDLK.F11},
            {VK.F12, SDLK.F12},
            {VK.F13, SDLK.F13},
            {VK.F14, SDLK.F14},
            {VK.F15, SDLK.F15},
            {VK.RCONTROL, SDLK.RCTRL},
            {VK.DIVIDE, SDLK.KP_DIVIDE},
            {VK.SNAPSHOT, SDLK.SYSREQ},
            {VK.RMENU, SDLK.RALT},
            {VK.PAUSE, SDLK.PAUSE},
            {VK.HOME, SDLK.HOME},
            {VK.UP, SDLK.UP},
            {VK.PRIOR, SDLK.PAGEUP},
            {VK.LEFT, SDLK.LEFT},
            {VK.RIGHT, SDLK.RIGHT},
            {VK.END, SDLK.END},
            {VK.DOWN, SDLK.DOWN},
            {VK.NEXT, SDLK.PAGEDOWN},
            {VK.INSERT, SDLK.INSERT},
            {VK.DELETE, SDLK.DELETE},
            {VK.LWIN, SDLK.LMETA},
            {VK.RWIN, SDLK.RMETA},
            {VK.APPS, SDLK.MENU},

            // Not part of original mapping.
            {VK.KEYPAD_ENTER, SDLK.KP_ENTER},
        };

        private static IDictionary<(uint scancode, bool extended), Key> s_ScanCodeToKey =
            new Dictionary<(uint scancode, bool extended), Key>();

        private static List<(Key key, uint scancode, bool extended)> s_KeyToScanCode =
            new List<(Key key, uint scancode, bool extended)>()
            {
                (Key.Escape, 0x01, false),
                (Key.F1, 0x3B, false),
                (Key.F2, 0x3C, false),
                (Key.F3, 0x3D, false),
                (Key.F4, 0x3E, false),
                (Key.F5, 0x3F, false),
                (Key.F6, 0x40, false),
                (Key.F7, 0x41, false),
                (Key.F8, 0x42, false),
                (Key.F9, 0x43, false),
                (Key.F10, 0x44, false),
                (Key.F11, 0x57, false),
                (Key.F12, 0x58, false),
                (Key.PrintScreen, 0x2A, true),
                (Key.ScrollLock, 0x46, false),
                (Key.Pause, 0x46, true),
                (Key.Backquote, 0x29, false),
                (Key.Digit1, 0x02, false),
                (Key.Digit2, 0x03, false),
                (Key.Digit3, 0x04, false),
                (Key.Digit4, 0x05, false),
                (Key.Digit5, 0x06, false),
                (Key.Digit6, 0x07, false),
                (Key.Digit7, 0x08, false),
                (Key.Digit8, 0x09, false),
                (Key.Digit9, 0x0A, false),
                (Key.Digit0, 0x0B, false),
                (Key.Minus, 0x0C, false),
                (Key.Equals, 0x0D, false),
                (Key.Backspace, 0x0E, false),
                (Key.Tab, 0x0F, false),
                (Key.Q, 0x10, false),
                (Key.W, 0x11, false),
                (Key.E, 0x12, false),
                (Key.R, 0x13, false),
                (Key.T, 0x14, false),
                (Key.Y, 0x15, false),
                (Key.U, 0x16, false),
                (Key.I, 0x17, false),
                (Key.O, 0x18, false),
                (Key.P, 0x19, false),
                (Key.LeftBracket, 0x1A, false),
                (Key.RightBracket, 0x1B, false),
                (Key.Backslash, 0x2B, false),
                (Key.CapsLock, 0x3A, false),
                (Key.A, 0x1E, false),
                (Key.S, 0x1F, false),
                (Key.D, 0x20, false),
                (Key.F, 0x21, false),
                (Key.G, 0x22, false),
                (Key.H, 0x23, false),
                (Key.J, 0x24, false),
                (Key.K, 0x25, false),
                (Key.L, 0x26, false),
                (Key.Semicolon, 0x27, false),
                (Key.Quote, 0x28, false),
                (Key.Enter, 0x1C, false),
                (Key.LeftShift, 0x2A, false),
                (Key.OEM1, 0x56, false),
                (Key.Z, 0x2C, false),
                (Key.X, 0x2D, false),
                (Key.C, 0x2E, false),
                (Key.V, 0x2F, false),
                (Key.B, 0x30, false),
                (Key.N, 0x31, false),
                (Key.M, 0x32, false),
                (Key.Comma, 0x33, false),
                (Key.Period, 0x34, false),
                (Key.Slash, 0x35, false),
                (Key.OEM2, 0x73, false),
                (Key.RightShift, 0x36, false),
                (Key.LeftCtrl, 0x1D, false),
                (Key.LeftMeta, 0x5B, true),
                (Key.LeftAlt, 0x38, false),
                (Key.Space, 0x39, false),
                (Key.RightAlt, 0x38, true),
                (Key.RightMeta, 0x5C, true),
                (Key.ContextMenu, 0x5D, true),
                (Key.RightCtrl, 0x1D, true),
                (Key.NumLock, 0x45, false),
                (Key.NumpadDivide, 0x35, true),
                (Key.NumpadMultiply, 0x37, false),
                (Key.Numpad7, 0x47, false),
                (Key.Numpad8, 0x48, false),
                (Key.Numpad9, 0x49, false),
                (Key.NumpadMinus, 0x4A, false),
                (Key.Numpad4, 0x4B, false),
                (Key.Numpad5, 0x4C, false),
                (Key.Numpad6, 0x4D, false),
                (Key.NumpadPlus, 0x4E, false),
                (Key.Numpad2, 0x50, false),
                (Key.Numpad3, 0x51, false),
                (Key.Numpad1, 0x4F, false),
                (Key.Numpad0, 0x52, false),
                (Key.NumpadPeriod, 0x53, false),
                (Key.NumpadEnter, 0x1C, true),
                (Key.Insert, 0x52, true),
                (Key.Delete, 0x53, true),
                (Key.Home, 0x47, true),
                (Key.End, 0x4F, true),
                (Key.PageUp, 0x49, true),
                (Key.PageDown, 0x51, true),
                (Key.UpArrow, 0x48, true),
                (Key.LeftArrow, 0x4B, true),
                (Key.DownArrow, 0x50, true),
                (Key.RightArrow, 0x4D, true),
            };
    }
}

#endif