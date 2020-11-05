#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace UnityEngine.InputLegacy
{
    public static class MacOSKeyboardMapping
    {
        static MacOSKeyboardMapping()
        {
            foreach (var pair in s_MainScanCodeToSDLK)
            {
                Debug.Assert(!s_SDLKToMainScanCode.ContainsKey(pair.Value), "main shouldn't contain multiple mappings");
                s_SDLKToMainScanCode[pair.Value] = pair.Key;
            }

            foreach (var pair in s_KeypadScanCodeToSDLK)
            {
                Debug.Assert(!s_SDLKToKeypadScanCode.ContainsKey(pair.Value),
                    "keypad shouldn't contain multiple mappings");
                s_SDLKToKeypadScanCode[pair.Value] = pair.Key;
            }
            
            // ------------------------------
            // Original code contains this exact sequence for bootstrap.

            for (int i = 0; i < s_KeyMap.Length; ++i)
                s_KeyMap[i] = (int)SDLK.UNKNOWN;

            foreach (var pair in s_MainScanCodeToSDLK)
                s_KeyMap[(int) pair.Key] = (int) pair.Value;
            
            AdjustKeyMapForCurrentKeyboardLayout(s_KeyMap, false);

            foreach (var pair in s_KeypadScanCodeToSDLK)
                s_KeyMap[(int) pair.Key] = (int) pair.Value;
            
            for (int i = 0; i < s_KeyMap.Length; ++i)
                s_KeymapCommandKeyModifier[i] = s_KeyMap[i];

            AdjustKeyMapForCurrentKeyboardLayout(s_KeymapCommandKeyModifier, true);

            // ------------------------------
            // It seems layout change notification arrives immediately during registration.
            // Which results in keypad mapping being ruined.
            // Preserve this behavior for accurate emulation.

            AdjustKeyMapForCurrentKeyboardLayout(s_KeyMap, false);
            AdjustKeyMapForCurrentKeyboardLayout(s_KeymapCommandKeyModifier, true);

            // ------------------------------

            ConstructKeyboardMapping();
        }

        private static readonly int[] s_KeyMap = new int[256];
        private static readonly int[] s_KeymapCommandKeyModifier = new int[256];
        
        private static Dictionary<(KeyCode keyCode, bool commandKeyStatus), List<ButtonControl>>
            s_KeyboardMapping = new Dictionary<(KeyCode keyCode, bool commandKeyStatus), List<ButtonControl>>();

        private static void AdjustKeyMapForCurrentKeyboardLayout(int[] keyMap, bool commandKeyModifierDown)
        {
            var carbon = dlopen("/System/Library/Frameworks/Carbon.framework/Versions/A/Carbon", RTLD_NOW);
            if (carbon == IntPtr.Zero)
                return;

            try
            {
                var kTisPropertyUnicodeKeyLayoutData = dlsym(carbon, "kTISPropertyUnicodeKeyLayoutData");
                if (kTisPropertyUnicodeKeyLayoutData == IntPtr.Zero)
                    throw new ArgumentNullException();

                kTisPropertyUnicodeKeyLayoutData = Marshal.ReadIntPtr(kTisPropertyUnicodeKeyLayoutData);
                if (kTisPropertyUnicodeKeyLayoutData == IntPtr.Zero)
                    throw new ArgumentNullException();

                var kbdType = LMGetKbdType();

                var currentKeyboard = TISCopyCurrentASCIICapableKeyboardLayoutInputSource();
                if (currentKeyboard == IntPtr.Zero)
                    throw new ArgumentNullException();

                var keyLayoutData = TISGetInputSourceProperty(currentKeyboard, kTisPropertyUnicodeKeyLayoutData);
                if (keyLayoutData == IntPtr.Zero)
                    throw new ArgumentNullException();

                var layout = CFDataGetBytePtr(keyLayoutData);
                if (layout == IntPtr.Zero)
                    throw new ArgumentNullException();

                const ulong maxStringLength = 255;
                var unicodeString = new ushort[maxStringLength];

                const ushort kUCKeyActionDown = 0;
                const ushort kVK_Space = 0x31;
                
                var world = (int)SDLK.WORLD_0;

                // Original code only iterates up to 0x7f, even if the array contains 256 elements.
                for (var i = 0; i < 0x7F; ++i)
                {
                    var modifierKeyState =
                        commandKeyModifierDown ? (uint) 1 : 0; // 1 equals to ((cmdKey >> 8) & 0xFF)

                    uint state = 0;
                    ulong actualStringLength = 0;

                    var status = UCKeyTranslate(
                        layout,
                        (ushort) i,
                        kUCKeyActionDown,
                        modifierKeyState,
                        kbdType,
                        0,
                        ref state,
                        maxStringLength,
                        ref actualStringLength,
                        unicodeString);

                    if (status == 0 && state != 0)
                        status = UCKeyTranslate(
                            layout,
                            kVK_Space,
                            kUCKeyActionDown,
                            modifierKeyState,
                            kbdType,
                            0,
                            ref state,
                            maxStringLength,
                            ref actualStringLength,
                            unicodeString);

                    if (status != 0 || actualStringLength == 0)
                        continue;

                    var value = unicodeString[0];
                    
                    if (value >= 128)
                        keyMap[i] = world++;
                    else if (value >= 32)
                        keyMap[i] = value;
                }
            }
            finally
            {
                if (carbon != null)
                    dlclose(carbon);
            }
        }

        private static void ResolveKeyMapControls(int[] keyMap, bool commandKeyStatus)
        {
            for (var scanCode = 0; scanCode < keyMap.Length; ++scanCode)
            {
                var keyCode = (KeyCode)keyMap[scanCode];
                if (keyCode == KeyCode.None)
                    continue;

                var control = s_ScanCodeToKey.TryGetValue((QZ)scanCode, out var keyValue) ? Keyboard.current[keyValue] : null;
                if (control == null)
                    continue;

                var key = (keyCode, commandKeyStatus);
                if (s_KeyboardMapping.TryGetValue(key, out var controls))
                    controls.Add(control);
                else
                    s_KeyboardMapping[key] = new List<ButtonControl> {control};
            }

            foreach (var pair in s_DirectKeyCodeToKeyMapping)
            {
                var key = ((KeyCode) pair.Key, commandKeyStatus);
                var control = Keyboard.current[pair.Value];
                if (control != null)
                    s_KeyboardMapping[key] = new List<ButtonControl> {control};
                else if (s_KeyboardMapping.ContainsKey(key))
                    s_KeyboardMapping.Remove(key);
            }
        }

        private static void ConstructKeyboardMapping()
        {
            ResolveKeyMapControls(s_KeyMap, false);
            ResolveKeyMapControls(s_KeymapCommandKeyModifier, true);
            
            foreach (var pair in s_KeyboardMapping)
                Debug.Log($"{pair.Key.keyCode}/{pair.Key.commandKeyStatus} -> {string.Join("/", pair.Value)}");
        }

        public static void ResolveMappingForCurrentLayout()
        {
            s_KeyboardMapping.Clear();

            if (Keyboard.current == null)
                return;

            // Original code doesn't refill keymap arrays, preserve it to preserve bugs related to that.
            AdjustKeyMapForCurrentKeyboardLayout(s_KeyMap, false);
            AdjustKeyMapForCurrentKeyboardLayout(s_KeymapCommandKeyModifier, true);

            ConstructKeyboardMapping();
        }

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/A/Carbon")]
        private static extern IntPtr /*TISInputSourceRef*/ TISCopyCurrentASCIICapableKeyboardLayoutInputSource();

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/A/Carbon")]
        private static extern IntPtr /*void* */ TISGetInputSourceProperty(IntPtr /*TISInputSourceRef*/ inputSource,
            IntPtr /*CFStringRef*/ propertyKey);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation")]
        private static extern IntPtr /*const UInt8* */ CFDataGetBytePtr(IntPtr /*CFDataRef*/ theData);

        [DllImport("/System/Library/Frameworks/CoreServices.framework/Versions/A/CoreServices")]
        private static extern int UCKeyTranslate(
            IntPtr /*const UCKeyboardLayout* */ keyLayoutPtr,
            ushort virtualKeyCode,
            ushort keyAction,
            uint modifierKeyState,
            uint keyboardType,
            uint /*OptionBits*/ keyTranslateOptions,
            ref uint /*UInt32* */ deadKeyState,
            ulong /*UniCharCount*/ maxStringLength, // UniCharCount is unsigned long, which is 4 or 8 bytes
            ref ulong /*UniCharCount* */ actualStringLength,
            ushort[] /*UniChar[]*/ unicodeString);

        [DllImport("/System/Library/Frameworks/Carbon.framework/Versions/A/Carbon")]
        private static extern byte LMGetKbdType();

        [DllImport("libdl.dylib", CharSet = CharSet.Ansi)]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport("libdl.dylib")]
        private static extern int dlclose(IntPtr handle);

        [DllImport("libdl.dylib", CharSet = CharSet.Ansi)]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        private const int RTLD_NOW = 0x2;

        public static ButtonControl[] GetControlsForKeyCode(KeyCode keyCode)
        {
            if (Keyboard.current == null)
                return null;

            var commandKeyStatus =
                Keyboard.current.leftCommandKey.isPressed || Keyboard.current.rightCommandKey.isPressed;

            return s_KeyboardMapping.TryGetValue((keyCode, commandKeyStatus),
                out var buttonControls)
                ? buttonControls.ToArray()
                : null;
        }
        
        // Some of key mappings are implemented in code logic,
        // so they override any scancode resolution maps.
        private static readonly IDictionary<SDLK, Key> s_DirectKeyCodeToKeyMapping = new Dictionary<SDLK, Key>
        {
            {SDLK.LALT, Key.LeftAlt},
            {SDLK.RALT, Key.RightAlt},
            {SDLK.LSHIFT, Key.LeftShift},
            {SDLK.RSHIFT, Key.RightShift},
            {SDLK.LCTRL, Key.LeftCtrl},
            {SDLK.RCTRL, Key.RightCtrl},
            {SDLK.LMETA, Key.LeftMeta},
            {SDLK.RMETA, Key.RightMeta},
        };

        // These are the Macintosh key scancode constants - from Inside Macintosh.
        private enum QZ
        {
            ESCAPE = 0x35,
            F1 = 0x7A,
            F2 = 0x78,
            F3 = 0x63,
            F4 = 0x76,
            F5 = 0x60,
            F6 = 0x61,
            F7 = 0x62,
            F8 = 0x64,
            F9 = 0x65,
            F10 = 0x6D,
            F11 = 0x67,
            F12 = 0x6F,
            F13 = 0x69,
            F14 = 0x6B,
            F15 = 0x71,
            PRINT = 0x69, // Same as F13.
            SCROLLLOCK = 0x6B, // Same as F14.
            PAUSE = 0x71, // Same as F15.
            POWER = 0x7F,
            BACKQUOTE = 0x32,
            DIGIT1 = 0x12,
            DIGIT2 = 0x13,
            DIGIT3 = 0x14,
            DIGIT4 = 0x15,
            DIGIT5 = 0x17,
            DIGIT6 = 0x16,
            DIGIT7 = 0x1A,
            DIGIT8 = 0x1C,
            DIGIT9 = 0x19,
            DIGIT0 = 0x1D,
            MINUS = 0x1B,
            EQUALS = 0x18,
            BACKSPACE = 0x33,
            INSERT = 0x72,
            HOME = 0x73,
            PAGEUP = 0x74,
            NUMLOCK = 0x47,
            KP_EQUALS = 0x51,
            KP_DIVIDE = 0x4B,
            KP_MULTIPLY = 0x43,
            TAB = 0x30,
            q = 0x0C,
            w = 0x0D,
            e = 0x0E,
            r = 0x0F,
            t = 0x11,
            y = 0x10,
            u = 0x20,
            i = 0x22,
            o = 0x1F,
            p = 0x23,
            LEFTBRACKET = 0x21,
            RIGHTBRACKET = 0x1E,
            BACKSLASH = 0x2A,
            DELETE = 0x75,
            END = 0x77,
            PAGEDOWN = 0x79,
            KP7 = 0x59,
            KP8 = 0x5B,
            KP9 = 0x5C,
            KP_MINUS = 0x4E,
            CAPSLOCK = 0x39,
            a = 0x00,
            s = 0x01,
            d = 0x02,
            f = 0x03,
            g = 0x05,
            h = 0x04,
            j = 0x26,
            k = 0x28,
            l = 0x25,
            SEMICOLON = 0x29,
            QUOTE = 0x27,
            RETURN = 0x24,
            KP4 = 0x56,
            KP5 = 0x57,
            KP6 = 0x58,
            KP_PLUS = 0x45,
            LSHIFT = 0x38,
            z = 0x06,
            x = 0x07,
            c = 0x08,
            v = 0x09,
            b = 0x0B,
            n = 0x2D,
            m = 0x2E,
            COMMA = 0x2B,
            PERIOD = 0x2F,
            SLASH = 0x2C,
            RSHIFT = 0x3C,
            UP = 0x7E,
            KP1 = 0x53,
            KP2 = 0x54,
            KP3 = 0x55,
            KP_ENTER = 0x4C,
            FN = 0x3F,
            LCTRL = 0x3B,
            LALT = 0x3A,
            LMETA = 0x37,
            SPACE = 0x31,
            RMETA = 0x36,
            RALT = 0x3D,
            RCTRL = 0x3E,
            LEFT = 0x7B,
            DOWN = 0x7D,
            RIGHT = 0x7C,
            KP0 = 0x52,
            KP_PERIOD = 0x41,
        }

        private static Dictionary<SDLK, QZ> s_SDLKToMainScanCode = new Dictionary<SDLK, QZ>();

        private static IDictionary<QZ, SDLK> s_MainScanCodeToSDLK = new Dictionary<QZ, SDLK>()
        {
            {QZ.ESCAPE, SDLK.ESCAPE},
            {QZ.F1, SDLK.F1},
            {QZ.F2, SDLK.F2},
            {QZ.F3, SDLK.F3},
            {QZ.F4, SDLK.F4},
            {QZ.F5, SDLK.F5},
            {QZ.F6, SDLK.F6},
            {QZ.F7, SDLK.F7},
            {QZ.F8, SDLK.F8},
            {QZ.F9, SDLK.F9},
            {QZ.F10, SDLK.F10},
            {QZ.F11, SDLK.F11},
            {QZ.F12, SDLK.F12},
            {QZ.F13, SDLK.F13},
            {QZ.F14, SDLK.F14},
            {QZ.F15, SDLK.F15},
            {QZ.POWER, SDLK.POWER},
            {QZ.BACKQUOTE, SDLK.BACKQUOTE},
            {QZ.DIGIT1, SDLK.DIGIT_1},
            {QZ.DIGIT2, SDLK.DIGIT_2},
            {QZ.DIGIT3, SDLK.DIGIT_3},
            {QZ.DIGIT4, SDLK.DIGIT_4},
            {QZ.DIGIT5, SDLK.DIGIT_5},
            {QZ.DIGIT6, SDLK.DIGIT_6},
            {QZ.DIGIT7, SDLK.DIGIT_7},
            {QZ.DIGIT8, SDLK.DIGIT_8},
            {QZ.DIGIT9, SDLK.DIGIT_9},
            {QZ.DIGIT0, SDLK.DIGIT_0},
            {QZ.MINUS, SDLK.MINUS},
            {QZ.EQUALS, SDLK.EQUALS},
            {QZ.BACKSPACE, SDLK.BACKSPACE},
            {QZ.INSERT, SDLK.INSERT},
            {QZ.HOME, SDLK.HOME},
            {QZ.PAGEUP, SDLK.PAGEUP},
            {QZ.NUMLOCK, SDLK.NUMLOCK},
            {QZ.KP_EQUALS, SDLK.KP_EQUALS},
            {QZ.KP_DIVIDE, SDLK.KP_DIVIDE},
            {QZ.KP_MULTIPLY, SDLK.KP_MULTIPLY},
            {QZ.TAB, SDLK.TAB},
            {QZ.q, SDLK.LOWER_Q},
            {QZ.w, SDLK.LOWER_W},
            {QZ.e, SDLK.LOWER_E},
            {QZ.r, SDLK.LOWER_R},
            {QZ.t, SDLK.LOWER_T},
            {QZ.y, SDLK.LOWER_Y},
            {QZ.u, SDLK.LOWER_U},
            {QZ.i, SDLK.LOWER_I},
            {QZ.o, SDLK.LOWER_O},
            {QZ.p, SDLK.LOWER_P},
            {QZ.LEFTBRACKET, SDLK.LEFTBRACKET},
            {QZ.RIGHTBRACKET, SDLK.RIGHTBRACKET},
            {QZ.BACKSLASH, SDLK.BACKSLASH},
            {QZ.DELETE, SDLK.DELETE},
            {QZ.END, SDLK.END},
            {QZ.PAGEDOWN, SDLK.PAGEDOWN},
            {QZ.KP7, SDLK.KP7},
            {QZ.KP8, SDLK.KP8},
            {QZ.KP9, SDLK.KP9},
            {QZ.KP_MINUS, SDLK.KP_MINUS},
            // While original listing contains capslock,
            // it was never reported to the backend, so the key was not working.
            // {QZ.CAPSLOCK, SDLK.CAPSLOCK},
            {QZ.a, SDLK.LOWER_A},
            {QZ.s, SDLK.LOWER_S},
            {QZ.d, SDLK.LOWER_D},
            {QZ.f, SDLK.LOWER_F},
            {QZ.g, SDLK.LOWER_G},
            {QZ.h, SDLK.LOWER_H},
            {QZ.j, SDLK.LOWER_J},
            {QZ.k, SDLK.LOWER_K},
            {QZ.l, SDLK.LOWER_L},
            {QZ.SEMICOLON, SDLK.SEMICOLON},
            {QZ.QUOTE, SDLK.QUOTE},
            {QZ.RETURN, SDLK.RETURN},
            {QZ.KP4, SDLK.KP4},
            {QZ.KP5, SDLK.KP5},
            {QZ.KP6, SDLK.KP6},
            {QZ.KP_PLUS, SDLK.KP_PLUS},
            {QZ.LSHIFT, SDLK.LSHIFT},
            {QZ.z, SDLK.LOWER_Z},
            {QZ.x, SDLK.LOWER_X},
            {QZ.c, SDLK.LOWER_C},
            {QZ.v, SDLK.LOWER_V},
            {QZ.b, SDLK.LOWER_B},
            {QZ.n, SDLK.LOWER_N},
            {QZ.m, SDLK.LOWER_M},
            {QZ.COMMA, SDLK.COMMA},
            {QZ.PERIOD, SDLK.PERIOD},
            {QZ.SLASH, SDLK.SLASH},
            {QZ.UP, SDLK.UP},
            {QZ.KP1, SDLK.KP1},
            {QZ.KP2, SDLK.KP2},
            {QZ.KP3, SDLK.KP3},
            {QZ.KP_ENTER, SDLK.KP_ENTER},
            {QZ.LCTRL, SDLK.LCTRL},
            {QZ.LALT, SDLK.LALT},
            {QZ.LMETA, SDLK.LMETA},
            {QZ.SPACE, SDLK.SPACE},
            {QZ.LEFT, SDLK.LEFT},
            {QZ.DOWN, SDLK.DOWN},
            {QZ.RIGHT, SDLK.RIGHT},
            {QZ.KP0, SDLK.KP0},
            {QZ.KP_PERIOD, SDLK.KP_PERIOD},
        };

        private static readonly Dictionary<SDLK, QZ> s_SDLKToKeypadScanCode = new Dictionary<SDLK, QZ>();

        private static readonly IDictionary<QZ, SDLK> s_KeypadScanCodeToSDLK = new Dictionary<QZ, SDLK>()
        {
            {QZ.KP0, SDLK.KP0},
            {QZ.KP1, SDLK.KP1},
            {QZ.KP2, SDLK.KP2},
            {QZ.KP3, SDLK.KP3},
            {QZ.KP4, SDLK.KP4},
            {QZ.KP5, SDLK.KP5},
            {QZ.KP6, SDLK.KP6},
            {QZ.KP7, SDLK.KP7},
            {QZ.KP8, SDLK.KP8},
            {QZ.KP9, SDLK.KP9},
            {QZ.KP_MINUS, SDLK.KP_MINUS},
            {QZ.KP_PLUS, SDLK.KP_PLUS},
            {QZ.KP_PERIOD, SDLK.KP_PERIOD},
            {QZ.KP_EQUALS, SDLK.KP_EQUALS},
            {QZ.KP_DIVIDE, SDLK.KP_DIVIDE},
            {QZ.KP_MULTIPLY, SDLK.KP_MULTIPLY},
            {QZ.KP_ENTER, SDLK.KP_ENTER},
        };

        private static readonly IDictionary<QZ, Key> s_ScanCodeToKey = new Dictionary<QZ, Key>
        {
            {QZ.ESCAPE, Key.Escape},
            {QZ.F1, Key.F1},
            {QZ.F2, Key.F2},
            {QZ.F3, Key.F3},
            {QZ.F4, Key.F4},
            {QZ.F5, Key.F5},
            {QZ.F6, Key.F6},
            {QZ.F7, Key.F7},
            {QZ.F8, Key.F8},
            {QZ.F9, Key.F9},
            {QZ.F10, Key.F10},
            {QZ.F11, Key.F11},
            {QZ.F12, Key.F12},
            {QZ.PRINT, Key.PrintScreen},
            {QZ.SCROLLLOCK, Key.ScrollLock},
            {QZ.PAUSE, Key.Pause},
            {QZ.BACKQUOTE, Key.Backquote},
            {QZ.DIGIT1, Key.Digit1},
            {QZ.DIGIT2, Key.Digit2},
            {QZ.DIGIT3, Key.Digit3},
            {QZ.DIGIT4, Key.Digit4},
            {QZ.DIGIT5, Key.Digit5},
            {QZ.DIGIT6, Key.Digit6},
            {QZ.DIGIT7, Key.Digit7},
            {QZ.DIGIT8, Key.Digit8},
            {QZ.DIGIT9, Key.Digit9},
            {QZ.DIGIT0, Key.Digit0},
            {QZ.MINUS, Key.Minus},
            {QZ.EQUALS, Key.Equals},
            {QZ.BACKSPACE, Key.Backspace},
            {QZ.TAB, Key.Tab},
            {QZ.q, Key.Q},
            {QZ.w, Key.W},
            {QZ.e, Key.E},
            {QZ.r, Key.R},
            {QZ.t, Key.T},
            {QZ.y, Key.Y},
            {QZ.u, Key.U},
            {QZ.i, Key.I},
            {QZ.o, Key.O},
            {QZ.p, Key.P},
            {QZ.LEFTBRACKET, Key.LeftBracket},
            {QZ.RIGHTBRACKET, Key.RightBracket},
            {QZ.BACKSLASH, Key.Backslash},
            {QZ.CAPSLOCK, Key.CapsLock},
            {QZ.a, Key.A},
            {QZ.s, Key.S},
            {QZ.d, Key.D},
            {QZ.f, Key.F},
            {QZ.g, Key.G},
            {QZ.h, Key.H},
            {QZ.j, Key.J},
            {QZ.k, Key.K},
            {QZ.l, Key.L},
            {QZ.SEMICOLON, Key.Semicolon},
            {QZ.QUOTE, Key.Quote},
            {QZ.RETURN, Key.Enter},
            {QZ.LSHIFT, Key.LeftShift},
            {QZ.z, Key.Z},
            {QZ.x, Key.X},
            {QZ.c, Key.C},
            {QZ.v, Key.V},
            {QZ.b, Key.B},
            {QZ.n, Key.N},
            {QZ.m, Key.M},
            {QZ.COMMA, Key.Comma},
            {QZ.PERIOD, Key.Period},
            {QZ.SLASH, Key.Slash},
            {QZ.RSHIFT, Key.RightShift},
            {QZ.LCTRL, Key.LeftCtrl},
            {QZ.LALT, Key.LeftAlt},
            {QZ.LMETA, Key.LeftMeta},
            {QZ.SPACE, Key.Space},
            {QZ.RMETA, Key.RightMeta},
            {QZ.RALT, Key.RightAlt},
            {QZ.RCTRL, Key.RightCtrl},
            {QZ.INSERT, Key.Insert},
            {QZ.DELETE, Key.Delete},
            {QZ.HOME, Key.Home},
            {QZ.END, Key.End},
            {QZ.PAGEUP, Key.PageUp},
            {QZ.PAGEDOWN, Key.PageDown},
            {QZ.UP, Key.UpArrow},
            {QZ.LEFT, Key.LeftArrow},
            {QZ.DOWN, Key.DownArrow},
            {QZ.RIGHT, Key.RightArrow},
            {QZ.NUMLOCK, Key.NumLock},
            {QZ.KP_EQUALS, Key.NumpadEquals},
            {QZ.KP_DIVIDE, Key.NumpadDivide},
            {QZ.KP_MULTIPLY, Key.NumpadMultiply},
            {QZ.KP7, Key.Numpad7},
            {QZ.KP8, Key.Numpad8},
            {QZ.KP9, Key.Numpad9},
            {QZ.KP_MINUS, Key.NumpadMinus},
            {QZ.KP4, Key.Numpad4},
            {QZ.KP5, Key.Numpad5},
            {QZ.KP6, Key.Numpad6},
            {QZ.KP_PLUS, Key.NumpadPlus},
            {QZ.KP1, Key.Numpad1},
            {QZ.KP2, Key.Numpad2},
            {QZ.KP3, Key.Numpad3},
            {QZ.KP0, Key.Numpad0},
            {QZ.KP_PERIOD, Key.NumpadPeriod},
            {QZ.KP_ENTER, Key.NumpadEnter},
        };
    }
}

#endif