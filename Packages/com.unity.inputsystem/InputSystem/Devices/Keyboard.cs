using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections.LowLevel.Unsafe;

////TODO: IME support

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Default state layout for keyboards.
    /// </summary>
    // NOTE: This layout has to match the KeyboardInputState layout used in native!
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KeyboardState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('K', 'E', 'Y', 'S'); }
        }

        // Number of keys rounded up to nearest size of 4.
        private const int kSizeInBytesUnrounded = ((int)Key.Count) / 8 + (((int)Key.Count) % 8 > 0 ? 1 : 0);
        public const int kSizeInBytes = kSizeInBytesUnrounded + (4 - kSizeInBytesUnrounded % 4);
        public const int kSizeInBits = kSizeInBytes * 8;

        [InputControl(name = "AnyKey", template = "AnyKey", sizeInBits = kSizeInBits)]
        [InputControl(name = "Escape", template = "Key", usages = new[] {"Back", "Cancel"}, bit = (int)Key.Escape)]
        [InputControl(name = "Space", template = "Key", bit = (int)Key.Space)]
        [InputControl(name = "Enter", template = "Key", usage = "Accept", bit = (int)Key.Enter)]
        [InputControl(name = "Tab", template = "Key", bit = (int)Key.Tab)]
        [InputControl(name = "Backquote", template = "Key", bit = (int)Key.Backquote)]
        [InputControl(name = "Quote", template = "Key", bit = (int)Key.Quote)]
        [InputControl(name = "Semicolon", template = "Key", bit = (int)Key.Semicolon)]
        [InputControl(name = "Comma", template = "Key", bit = (int)Key.Comma)]
        [InputControl(name = "Period", template = "Key", bit = (int)Key.Period)]
        [InputControl(name = "Slash", template = "Key", bit = (int)Key.Slash)]
        [InputControl(name = "Backslash", template = "Key", bit = (int)Key.Backslash)]
        [InputControl(name = "LeftBracket", template = "Key", bit = (int)Key.LeftBracket)]
        [InputControl(name = "RightBracket", template = "Key", bit = (int)Key.RightBracket)]
        [InputControl(name = "Minus", template = "Key", bit = (int)Key.Minus)]
        [InputControl(name = "Equals", template = "Key", bit = (int)Key.Equals)]
        [InputControl(name = "UpArrow", template = "Key", bit = (int)Key.UpArrow)]
        [InputControl(name = "DownArrow", template = "Key", bit = (int)Key.DownArrow)]
        [InputControl(name = "LeftArrow", template = "Key", bit = (int)Key.LeftArrow)]
        [InputControl(name = "RightArrow", template = "Key", bit = (int)Key.RightArrow)]
        [InputControl(name = "A", template = "Key", bit = (int)Key.A)]
        [InputControl(name = "B", template = "Key", bit = (int)Key.B)]
        [InputControl(name = "C", template = "Key", bit = (int)Key.C)]
        [InputControl(name = "D", template = "Key", bit = (int)Key.D)]
        [InputControl(name = "E", template = "Key", bit = (int)Key.E)]
        [InputControl(name = "F", template = "Key", bit = (int)Key.F)]
        [InputControl(name = "G", template = "Key", bit = (int)Key.G)]
        [InputControl(name = "H", template = "Key", bit = (int)Key.H)]
        [InputControl(name = "I", template = "Key", bit = (int)Key.I)]
        [InputControl(name = "J", template = "Key", bit = (int)Key.J)]
        [InputControl(name = "K", template = "Key", bit = (int)Key.K)]
        [InputControl(name = "L", template = "Key", bit = (int)Key.L)]
        [InputControl(name = "M", template = "Key", bit = (int)Key.M)]
        [InputControl(name = "N", template = "Key", bit = (int)Key.N)]
        [InputControl(name = "O", template = "Key", bit = (int)Key.O)]
        [InputControl(name = "P", template = "Key", bit = (int)Key.P)]
        [InputControl(name = "Q", template = "Key", bit = (int)Key.Q)]
        [InputControl(name = "R", template = "Key", bit = (int)Key.R)]
        [InputControl(name = "S", template = "Key", bit = (int)Key.S)]
        [InputControl(name = "T", template = "Key", bit = (int)Key.T)]
        [InputControl(name = "U", template = "Key", bit = (int)Key.U)]
        [InputControl(name = "V", template = "Key", bit = (int)Key.V)]
        [InputControl(name = "W", template = "Key", bit = (int)Key.W)]
        [InputControl(name = "X", template = "Key", bit = (int)Key.X)]
        [InputControl(name = "Y", template = "Key", bit = (int)Key.Y)]
        [InputControl(name = "Z", template = "Key", bit = (int)Key.Z)]
        [InputControl(name = "1", template = "Key", bit = (int)Key.Digit1)]
        [InputControl(name = "2", template = "Key", bit = (int)Key.Digit2)]
        [InputControl(name = "3", template = "Key", bit = (int)Key.Digit3)]
        [InputControl(name = "4", template = "Key", bit = (int)Key.Digit4)]
        [InputControl(name = "5", template = "Key", bit = (int)Key.Digit5)]
        [InputControl(name = "6", template = "Key", bit = (int)Key.Digit6)]
        [InputControl(name = "7", template = "Key", bit = (int)Key.Digit7)]
        [InputControl(name = "8", template = "Key", bit = (int)Key.Digit8)]
        [InputControl(name = "9", template = "Key", bit = (int)Key.Digit9)]
        [InputControl(name = "0", template = "Key", bit = (int)Key.Digit0)]
        [InputControl(name = "LeftShift", template = "Key", usage = "Modifier", bit = (int)Key.LeftShift)]
        [InputControl(name = "RightShift", template = "Key", usage = "Modifier", bit = (int)Key.RightShift)]
        [InputControl(name = "LeftAlt", template = "Key", usage = "Modifier", bit = (int)Key.LeftAlt)]
        [InputControl(name = "RightAlt", template = "Key", usage = "Modifier", bit = (int)Key.RightAlt, alias = "AltGr")]
        [InputControl(name = "LeftCtrl", template = "Key", usage = "Modifier", bit = (int)Key.LeftCtrl)]
        [InputControl(name = "RightCtrl", template = "Key", usage = "Modifier", bit = (int)Key.RightCtrl)]
        [InputControl(name = "LeftMeta", template = "Key", usage = "Modifier", bit = (int)Key.LeftMeta, aliases = new[] { "LeftWindows", "LeftApple", "LeftCommand" })]
        [InputControl(name = "RightMeta", template = "Key", usage = "Modifier", bit = (int)Key.RightMeta, aliases = new[] { "RightWindows", "RightApple", "RightCommand" })]
        [InputControl(name = "ContextMenu", template = "Key", usage = "Modifier", bit = (int)Key.ContextMenu)]
        [InputControl(name = "Backspace", template = "Key", bit = (int)Key.Backspace)]
        [InputControl(name = "PageDown", template = "Key", bit = (int)Key.PageDown)]
        [InputControl(name = "PageUp", template = "Key", bit = (int)Key.PageUp)]
        [InputControl(name = "Home", template = "Key", bit = (int)Key.Home)]
        [InputControl(name = "End", template = "Key", bit = (int)Key.End)]
        [InputControl(name = "Insert", template = "Key", bit = (int)Key.Insert)]
        [InputControl(name = "Delete", template = "Key", bit = (int)Key.Delete)]
        [InputControl(name = "CapsLock", template = "Key", bit = (int)Key.CapsLock)]
        [InputControl(name = "NumLock", template = "Key", bit = (int)Key.NumLock)]
        [InputControl(name = "PrintScreen", template = "Key", bit = (int)Key.PrintScreen)]
        [InputControl(name = "ScrollLock", template = "Key", bit = (int)Key.ScrollLock)]
        [InputControl(name = "Pause", template = "Key", bit = (int)Key.Pause)]
        [InputControl(name = "NumpadEnter", template = "Key", bit = (int)Key.NumpadEnter)]
        [InputControl(name = "NumpadDivide", template = "Key", bit = (int)Key.NumpadDivide)]
        [InputControl(name = "NumpadMultiply", template = "Key", bit = (int)Key.NumpadMultiply)]
        [InputControl(name = "NumpadPlus", template = "Key", bit = (int)Key.NumpadPlus)]
        [InputControl(name = "NumpadMinus", template = "Key", bit = (int)Key.NumpadMinus)]
        [InputControl(name = "NumpadPeriod", template = "Key", bit = (int)Key.NumpadPeriod)]
        [InputControl(name = "NumpadEquals", template = "Key", bit = (int)Key.NumpadEquals)]
        [InputControl(name = "Numpad1", template = "Key", bit = (int)Key.Numpad1)]
        [InputControl(name = "Numpad2", template = "Key", bit = (int)Key.Numpad2)]
        [InputControl(name = "Numpad3", template = "Key", bit = (int)Key.Numpad3)]
        [InputControl(name = "Numpad4", template = "Key", bit = (int)Key.Numpad4)]
        [InputControl(name = "Numpad5", template = "Key", bit = (int)Key.Numpad5)]
        [InputControl(name = "Numpad6", template = "Key", bit = (int)Key.Numpad6)]
        [InputControl(name = "Numpad7", template = "Key", bit = (int)Key.Numpad7)]
        [InputControl(name = "Numpad8", template = "Key", bit = (int)Key.Numpad8)]
        [InputControl(name = "Numpad9", template = "Key", bit = (int)Key.Numpad9)]
        [InputControl(name = "Numpad0", template = "Key", bit = (int)Key.Numpad0)]
        [InputControl(name = "F1", template = "Key", bit = (int)Key.F1)]
        [InputControl(name = "F2", template = "Key", bit = (int)Key.F2)]
        [InputControl(name = "F3", template = "Key", bit = (int)Key.F3)]
        [InputControl(name = "F4", template = "Key", bit = (int)Key.F4)]
        [InputControl(name = "F5", template = "Key", bit = (int)Key.F5)]
        [InputControl(name = "F6", template = "Key", bit = (int)Key.F6)]
        [InputControl(name = "F7", template = "Key", bit = (int)Key.F7)]
        [InputControl(name = "F8", template = "Key", bit = (int)Key.F8)]
        [InputControl(name = "F9", template = "Key", bit = (int)Key.F9)]
        [InputControl(name = "F10", template = "Key", bit = (int)Key.F10)]
        [InputControl(name = "F11", template = "Key", bit = (int)Key.F11)]
        [InputControl(name = "F12", template = "Key", bit = (int)Key.F12)]
        [InputControl(name = "OEM1", template = "Key", bit = (int)Key.OEM1)]
        [InputControl(name = "OEM2", template = "Key", bit = (int)Key.OEM2)]
        [InputControl(name = "OEM3", template = "Key", bit = (int)Key.OEM3)]
        [InputControl(name = "OEM4", template = "Key", bit = (int)Key.OEM4)]
        [InputControl(name = "OEM5", template = "Key", bit = (int)Key.OEM5)]
        public fixed byte keys[kSizeInBytes];

        public KeyboardState(params Key[] pressedKeys)
        {
            fixed(byte* keysPtr = keys)
            {
                UnsafeUtility.MemClear(keysPtr, kSizeInBytes);
                for (var i = 0; i < pressedKeys.Length; ++i)
                {
                    MemoryHelpers.WriteSingleBit(new IntPtr(keysPtr), (uint)pressedKeys[i], true);
                }
            }
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Enumeration of key codes.
    /// </summary>
    /// <remarks>
    /// Named according to the US keyboard layout which is our reference layout.
    /// </remarks>
    // NOTE: Has to match up with 'KeyboardInputState::KeyCode' in native.
    // NOTE: In the keyboard code, we depend on the order of the keys in the various keyboard blocks.
    public enum Key
    {
        None,

        // Printable keys.
        Space,
        Enter,
        Tab,
        Backquote,
        Quote,
        Semicolon,
        Comma,
        Period,
        Slash,
        Backslash,
        LeftBracket,
        RightBracket,
        Minus,
        Equals,
        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z,
        Digit1,
        Digit2,
        Digit3,
        Digit4,
        Digit5,
        Digit6,
        Digit7,
        Digit8,
        Digit9,
        Digit0,

        // Non-printable keys.
        LeftShift,
        RightShift,
        LeftAlt,
        RightAlt,
        AltGr = RightAlt,
        LeftCtrl,
        RightCtrl,
        LeftMeta,
        RightMeta,
        LeftWindows = LeftMeta,
        RightWindows = RightMeta,
        LeftApple = LeftMeta,
        RightApple = RightMeta,
        LeftCommand = LeftMeta,
        RightCommand = RightMeta,
        ContextMenu,
        Escape,
        LeftArrow,
        RightArrow,
        UpArrow,
        DownArrow,
        Backspace,
        PageDown,
        PageUp,
        Home,
        End,
        Insert,
        Delete,
        CapsLock,
        NumLock,
        PrintScreen,
        ScrollLock,
        Pause,

        // Numpad.
        // NOTE: Numpad layout follows the 18-key numpad layout. Some PC keyboards
        //       have a 17-key numpad layout where the plus key is an elongated key
        //       like the numpad enter key. Be aware that in those layouts the positions
        //       of some of the operator keys are also different. However, we stay
        //       layout neutral here, too, and always use the 18-key blueprint.
        NumpadEnter,
        NumpadDivide,
        NumpadMultiply,
        NumpadPlus,
        NumpadMinus,
        NumpadPeriod,
        NumpadEquals,
        Numpad0,
        Numpad1,
        Numpad2,
        Numpad3,
        Numpad4,
        Numpad5,
        Numpad6,
        Numpad7,
        Numpad8,
        Numpad9,

        F1,
        F2,
        F3,
        F4,
        F5,
        F6,
        F7,
        F8,
        F9,
        F10,
        F11,
        F12,

        // Extra keys that a keyboard may have. We make no guarantees about where
        // they end up on the keyboard (if they are present).
        OEM1,
        OEM2,
        OEM3,
        OEM4,
        OEM5,

        Count
    }

    /// <summary>
    /// A keyboard input device.
    /// </summary>
    /// <remarks>
    /// Keyboards allow for both individual button input as well as text input.
    ///
    /// Be aware that identification of keys in the system is layout-agnostic. For example, the
    /// key referred to as the "a" key is always the key to the right of Caps Lock regardless of
    /// where the current keyboard layout actually puts the "a" character (if it even has any).
    /// To find what is actually behind a key according to the current keyboard layout, use
    /// <see cref="InputControl.displayName"/>, <see cref="KeyControl.shiftDisplayName"/>, and
    /// <see cref="KeyControl.altDisplayName"/>.
    /// </remarks>
    [InputTemplate(stateType = typeof(KeyboardState))]
    public class Keyboard : InputDevice
    {
        /// <summary>
        /// Event that is fired for every single character entered on the keyboard.
        /// </summary>
        public event Action<char> onTextInput
        {
            add { m_TextInputListeners.Append(value); }
            remove { m_TextInputListeners.Remove(value); }
        }

        /// <summary>
        /// The name of the layout currently used by the keyboard.
        /// </summary>
        /// <remarks>
        /// Note that keyboard layout names are platform-specific.
        ///
        /// The value of this property reflects the currently used layout and thus changes
        /// whenever the layout of the system or the one for the application is changed.
        ///
        /// To determine what a key represents in the current layout, use <see cref="InputControl.displayName"/>,
        /// <see cref="KeyControl.shiftDisplayName"/>, and <see cref="KeyControl.altDisplayName"/>.
        /// </remarks>
        public string layout
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_LayoutName;
            }
            protected set { m_LayoutName = value; }
        }

        /// <summary>
        /// A synthetic button control that is considered pressed if any key on the keyboard is pressed.
        /// </summary>
        public AnyKeyControl anyKey { get; private set; }

        /// <summary>
        /// The space bar key.
        /// </summary>
        public KeyControl spaceKey { get; private set; }

        /// <summary>
        /// The enter/return key in the main key block.
        /// </summary>
        /// <remarks>
        /// This key is distinct from the enter key on the numpad which is <see cref="numpadEnterKey"/>.
        /// </remarks>
        public KeyControl enterKey { get; private set; }

        /// <summary>
        /// The tab key.
        /// </summary>
        public KeyControl tabKey { get; private set; }

        /// <summary>
        /// The ` key. The leftmost key in the row of digits. Directly above tab.
        /// </summary>
        public KeyControl backquoteKey { get; private set; }

        /// <summary>
        /// The ' key. The key immediately to the left of the enter/return key.
        /// </summary>
        public KeyControl quoteKey { get; private set; }

        /// <summary>
        /// The ';' key. The key immediately to the left of the quote key.
        /// </summary>
        public KeyControl semicolonKey { get; private set; }

        /// <summary>
        /// The ',' key. Third key to the left of the right shift key.
        /// </summary>
        public KeyControl commaKey { get; private set; }
        public KeyControl periodKey { get; private set; }
        public KeyControl slashKey { get; private set; }
        public KeyControl backslashKey { get; private set; }
        public KeyControl leftBracketKey { get; private set; }
        public KeyControl rightBracketKey { get; private set; }
        public KeyControl minusKey { get; private set; }

        /// <summary>
        /// The '=' key in the main key block. Key immediately to the left of the backspace key.
        /// </summary>
        public KeyControl equalsKey { get; private set; }

        /// <summary>
        /// The 'a' key. Key immediately to the right of the caps lock key.
        /// </summary>
        public KeyControl aKey { get; private set; }
        public KeyControl bKey { get; private set; }
        public KeyControl cKey { get; private set; }
        public KeyControl dKey { get; private set; }
        public KeyControl eKey { get; private set; }
        public KeyControl fKey { get; private set; }
        public KeyControl gKey { get; private set; }
        public KeyControl hKey { get; private set; }
        public KeyControl iKey { get; private set; }
        public KeyControl jKey { get; private set; }
        public KeyControl kKey { get; private set; }
        public KeyControl lKey { get; private set; }
        public KeyControl mKey { get; private set; }
        public KeyControl nKey { get; private set; }
        public KeyControl oKey { get; private set; }
        public KeyControl pKey { get; private set; }
        public KeyControl qKey { get; private set; }
        public KeyControl rKey { get; private set; }
        public KeyControl sKey { get; private set; }
        public KeyControl tKey { get; private set; }
        public KeyControl uKey { get; private set; }
        public KeyControl vKey { get; private set; }
        public KeyControl wKey { get; private set; }
        public KeyControl xKey { get; private set; }
        public KeyControl yKey { get; private set; }
        public KeyControl zKey { get; private set; }
        public KeyControl digit1Key { get; private set; }
        public KeyControl digit2Key { get; private set; }
        public KeyControl digit3Key { get; private set; }
        public KeyControl digit4Key { get; private set; }
        public KeyControl digit5Key { get; private set; }
        public KeyControl digit6Key { get; private set; }
        public KeyControl digit7Key { get; private set; }
        public KeyControl digit8Key { get; private set; }
        public KeyControl digit9Key { get; private set; }
        public KeyControl digit0Key { get; private set; }
        public KeyControl leftShiftKey { get; private set; }
        public KeyControl rightShiftKey { get; private set; }
        public KeyControl leftAltKey { get; private set; }
        public KeyControl rightAltKey { get; private set; }
        public KeyControl leftCtrlKey { get; private set; }
        public KeyControl rightCtrlKey { get; private set; }
        public KeyControl leftMetaKey { get; private set; }
        public KeyControl rightMetaKey { get; private set; }
        public KeyControl leftWindowsKey { get; private set; }
        public KeyControl rightWindowsKey { get; private set; }
        public KeyControl leftAppleKey { get; private set; }
        public KeyControl rightAppleKey { get; private set; }
        public KeyControl leftCommandKey { get; private set; }
        public KeyControl rightCommandKey { get; private set; }
        public KeyControl contextMenuKey { get; private set; }
        public KeyControl escapeKey { get; private set; }
        public KeyControl leftArrowKey { get; private set; }
        public KeyControl rightArrowKey { get; private set; }
        public KeyControl upArrowKey { get; private set; }
        public KeyControl downArrowKey { get; private set; }
        public KeyControl backspaceKey { get; private set; }
        public KeyControl pageDownKey { get; private set; }
        public KeyControl pageUpKey { get; private set; }
        public KeyControl homeKey { get; private set; }
        public KeyControl endKey { get; private set; }
        public KeyControl insertKey { get; private set; }
        public KeyControl deleteKey { get; private set; }
        public KeyControl capsLockKey { get; private set; }
        public KeyControl scrollLockKey { get; private set; }
        public KeyControl numLockKey { get; private set; }
        public KeyControl printScreenKey { get; private set; }
        public KeyControl pauseKey { get; private set; }
        public KeyControl numpadEnterKey { get; private set; }
        public KeyControl numpadDivideKey { get; private set; }
        public KeyControl numpadMultiplyKey { get; private set; }
        public KeyControl numpadMinusKey { get; private set; }
        public KeyControl numpadPlusKey { get; private set; }
        public KeyControl numpadPeriodKey { get; private set; }
        public KeyControl numpadEqualsKey { get; private set; }
        public KeyControl numpad0Key { get; private set; }
        public KeyControl numpad1Key { get; private set; }
        public KeyControl numpad2Key { get; private set; }
        public KeyControl numpad3Key { get; private set; }
        public KeyControl numpad4Key { get; private set; }
        public KeyControl numpad5Key { get; private set; }
        public KeyControl numpad6Key { get; private set; }
        public KeyControl numpad7Key { get; private set; }
        public KeyControl numpad8Key { get; private set; }
        public KeyControl numpad9Key { get; private set; }
        public KeyControl f1Key { get; private set; }
        public KeyControl f2Key { get; private set; }
        public KeyControl f3Key { get; private set; }
        public KeyControl f4Key { get; private set; }
        public KeyControl f5Key { get; private set; }
        public KeyControl f6Key { get; private set; }
        public KeyControl f7Key { get; private set; }
        public KeyControl f8Key { get; private set; }
        public KeyControl f9Key { get; private set; }
        public KeyControl f10Key { get; private set; }
        public KeyControl f11Key { get; private set; }
        public KeyControl f12Key { get; private set; }
        public KeyControl oem1Key { get; private set; }
        public KeyControl oem2Key { get; private set; }
        public KeyControl oem3Key { get; private set; }
        public KeyControl oem4Key { get; private set; }
        public KeyControl oem5Key { get; private set; }

        public static Keyboard current { get; internal set; }

        /// <summary>
        /// Look up a key control by its key code.
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentException">The given <see cref="key"/> is not valid.</exception>
        public KeyControl this[Key key]
        {
            get
            {
                if (key >= Key.A && key <= Key.Z)
                {
                    switch (key)
                    {
                        case Key.A: return aKey;
                        case Key.B: return bKey;
                        case Key.C: return cKey;
                        case Key.D: return dKey;
                        case Key.E: return eKey;
                        case Key.F: return fKey;
                        case Key.G: return gKey;
                        case Key.H: return hKey;
                        case Key.I: return iKey;
                        case Key.J: return jKey;
                        case Key.K: return kKey;
                        case Key.L: return lKey;
                        case Key.M: return mKey;
                        case Key.N: return nKey;
                        case Key.O: return oKey;
                        case Key.P: return pKey;
                        case Key.Q: return qKey;
                        case Key.R: return rKey;
                        case Key.S: return sKey;
                        case Key.T: return tKey;
                        case Key.U: return uKey;
                        case Key.V: return vKey;
                        case Key.W: return wKey;
                        case Key.X: return xKey;
                        case Key.Y: return yKey;
                        case Key.Z: return zKey;
                    }
                }

                if (key >= Key.Digit1 && key <= Key.Digit0)
                {
                    switch (key)
                    {
                        case Key.Digit1: return digit1Key;
                        case Key.Digit2: return digit2Key;
                        case Key.Digit3: return digit3Key;
                        case Key.Digit4: return digit4Key;
                        case Key.Digit5: return digit5Key;
                        case Key.Digit6: return digit6Key;
                        case Key.Digit7: return digit7Key;
                        case Key.Digit8: return digit8Key;
                        case Key.Digit9: return digit9Key;
                        case Key.Digit0: return digit0Key;
                    }
                }

                if (key >= Key.F1 && key <= Key.F12)
                {
                    switch (key)
                    {
                        case Key.F1: return f1Key;
                        case Key.F2: return f2Key;
                        case Key.F3: return f3Key;
                        case Key.F4: return f4Key;
                        case Key.F5: return f5Key;
                        case Key.F6: return f6Key;
                        case Key.F7: return f7Key;
                        case Key.F8: return f8Key;
                        case Key.F9: return f9Key;
                        case Key.F10: return f10Key;
                        case Key.F11: return f11Key;
                        case Key.F12: return f12Key;
                    }
                }

                if (key >= Key.NumpadEnter && key <= Key.Numpad9)
                {
                    switch (key)
                    {
                        case Key.NumpadEnter: return numpadEnterKey;
                        case Key.NumpadDivide: return numpadDivideKey;
                        case Key.NumpadMultiply: return numpadMultiplyKey;
                        case Key.NumpadPlus: return numpadPlusKey;
                        case Key.NumpadMinus: return numpadMinusKey;
                        case Key.NumpadPeriod: return numpadPeriodKey;
                        case Key.NumpadEquals: return numpadEqualsKey;
                        case Key.Numpad0: return numpad0Key;
                        case Key.Numpad1: return numpad1Key;
                        case Key.Numpad2: return numpad2Key;
                        case Key.Numpad3: return numpad3Key;
                        case Key.Numpad4: return numpad4Key;
                        case Key.Numpad5: return numpad5Key;
                        case Key.Numpad6: return numpad6Key;
                        case Key.Numpad7: return numpad7Key;
                        case Key.Numpad8: return numpad8Key;
                        case Key.Numpad9: return numpad9Key;
                    }
                }

                switch (key)
                {
                    case Key.Space: return spaceKey;
                    case Key.Enter: return enterKey;
                    case Key.Tab: return tabKey;
                    case Key.Backquote: return backquoteKey;
                    case Key.Quote: return quoteKey;
                    case Key.Semicolon: return semicolonKey;
                    case Key.Comma: return commaKey;
                    case Key.Period: return periodKey;
                    case Key.Slash: return slashKey;
                    case Key.Backslash: return backslashKey;
                    case Key.LeftBracket: return leftBracketKey;
                    case Key.RightBracket: return rightBracketKey;
                    case Key.Minus: return minusKey;
                    case Key.Equals: return equalsKey;
                    case Key.LeftShift: return leftShiftKey;
                    case Key.RightShift: return rightShiftKey;
                    case Key.LeftAlt: return leftAltKey;
                    case Key.RightAlt: return rightAltKey;
                    case Key.LeftCtrl: return leftCtrlKey;
                    case Key.RightCtrl: return rightCtrlKey;
                    case Key.LeftMeta: return leftMetaKey;
                    case Key.RightMeta: return rightMetaKey;
                    case Key.ContextMenu: return contextMenuKey;
                    case Key.Escape: return escapeKey;
                    case Key.LeftArrow: return leftArrowKey;
                    case Key.RightArrow: return rightArrowKey;
                    case Key.UpArrow: return upArrowKey;
                    case Key.DownArrow: return downArrowKey;
                    case Key.Backspace: return backspaceKey;
                    case Key.PageDown: return pageDownKey;
                    case Key.PageUp: return pageUpKey;
                    case Key.Home: return homeKey;
                    case Key.End: return endKey;
                    case Key.Insert: return insertKey;
                    case Key.Delete: return deleteKey;
                    case Key.CapsLock: return capsLockKey;
                    case Key.NumLock: return numLockKey;
                    case Key.PrintScreen: return printScreenKey;
                    case Key.ScrollLock: return scrollLockKey;
                    case Key.Pause: return pauseKey;
                    case Key.OEM1: return oem1Key;
                    case Key.OEM2: return oem2Key;
                    case Key.OEM3: return oem3Key;
                    case Key.OEM4: return oem4Key;
                    case Key.OEM5: return oem5Key;
                }

                throw new ArgumentException("key");
            }
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            anyKey = setup.GetControl<AnyKeyControl>("AnyKey");
            spaceKey = setup.GetControl<KeyControl>("Space");
            enterKey = setup.GetControl<KeyControl>("Enter");
            tabKey = setup.GetControl<KeyControl>("Tab");
            backquoteKey = setup.GetControl<KeyControl>("Backquote");
            quoteKey = setup.GetControl<KeyControl>("Quote");
            semicolonKey = setup.GetControl<KeyControl>("Semicolon");
            commaKey = setup.GetControl<KeyControl>("Comma");
            periodKey = setup.GetControl<KeyControl>("Period");
            slashKey = setup.GetControl<KeyControl>("Slash");
            backslashKey = setup.GetControl<KeyControl>("Backslash");
            leftBracketKey = setup.GetControl<KeyControl>("LeftBracket");
            rightBracketKey = setup.GetControl<KeyControl>("RightBracket");
            minusKey = setup.GetControl<KeyControl>("Minus");
            equalsKey = setup.GetControl<KeyControl>("Equals");
            aKey = setup.GetControl<KeyControl>("A");
            bKey = setup.GetControl<KeyControl>("B");
            cKey = setup.GetControl<KeyControl>("C");
            dKey = setup.GetControl<KeyControl>("D");
            eKey = setup.GetControl<KeyControl>("E");
            fKey = setup.GetControl<KeyControl>("F");
            gKey = setup.GetControl<KeyControl>("G");
            hKey = setup.GetControl<KeyControl>("H");
            iKey = setup.GetControl<KeyControl>("I");
            jKey = setup.GetControl<KeyControl>("J");
            kKey = setup.GetControl<KeyControl>("K");
            lKey = setup.GetControl<KeyControl>("L");
            mKey = setup.GetControl<KeyControl>("M");
            nKey = setup.GetControl<KeyControl>("N");
            oKey = setup.GetControl<KeyControl>("O");
            pKey = setup.GetControl<KeyControl>("P");
            qKey = setup.GetControl<KeyControl>("Q");
            rKey = setup.GetControl<KeyControl>("R");
            sKey = setup.GetControl<KeyControl>("S");
            tKey = setup.GetControl<KeyControl>("T");
            uKey = setup.GetControl<KeyControl>("U");
            vKey = setup.GetControl<KeyControl>("V");
            wKey = setup.GetControl<KeyControl>("W");
            xKey = setup.GetControl<KeyControl>("X");
            yKey = setup.GetControl<KeyControl>("Y");
            zKey = setup.GetControl<KeyControl>("Z");
            digit1Key = setup.GetControl<KeyControl>("1");
            digit2Key = setup.GetControl<KeyControl>("2");
            digit3Key = setup.GetControl<KeyControl>("3");
            digit4Key = setup.GetControl<KeyControl>("4");
            digit5Key = setup.GetControl<KeyControl>("5");
            digit6Key = setup.GetControl<KeyControl>("6");
            digit7Key = setup.GetControl<KeyControl>("7");
            digit8Key = setup.GetControl<KeyControl>("8");
            digit9Key = setup.GetControl<KeyControl>("9");
            digit0Key = setup.GetControl<KeyControl>("0");
            leftShiftKey = setup.GetControl<KeyControl>("LeftShift");
            rightShiftKey = setup.GetControl<KeyControl>("RightShift");
            leftAltKey = setup.GetControl<KeyControl>("LeftAlt");
            rightAltKey = setup.GetControl<KeyControl>("RightAlt");
            leftCtrlKey = setup.GetControl<KeyControl>("LeftCtrl");
            rightCtrlKey = setup.GetControl<KeyControl>("RightCtrl");
            leftMetaKey = setup.GetControl<KeyControl>("LeftMeta");
            rightMetaKey = setup.GetControl<KeyControl>("RightMeta");
            leftWindowsKey = setup.GetControl<KeyControl>("LeftWindows");
            rightWindowsKey = setup.GetControl<KeyControl>("RightWindows");
            leftAppleKey = setup.GetControl<KeyControl>("LeftApple");
            rightAppleKey = setup.GetControl<KeyControl>("RightApple");
            leftCommandKey = setup.GetControl<KeyControl>("LeftCommand");
            rightCommandKey = setup.GetControl<KeyControl>("RightCommand");
            contextMenuKey = setup.GetControl<KeyControl>("ContextMenu");
            escapeKey = setup.GetControl<KeyControl>("Escape");
            leftArrowKey = setup.GetControl<KeyControl>("LeftArrow");
            rightArrowKey = setup.GetControl<KeyControl>("RightArrow");
            upArrowKey = setup.GetControl<KeyControl>("UpArrow");
            downArrowKey = setup.GetControl<KeyControl>("DownArrow");
            backspaceKey = setup.GetControl<KeyControl>("Backspace");
            pageDownKey = setup.GetControl<KeyControl>("PageDown");
            pageUpKey = setup.GetControl<KeyControl>("PageUp");
            homeKey = setup.GetControl<KeyControl>("Home");
            endKey = setup.GetControl<KeyControl>("End");
            insertKey = setup.GetControl<KeyControl>("Insert");
            deleteKey = setup.GetControl<KeyControl>("Delete");
            numpadEnterKey = setup.GetControl<KeyControl>("NumpadEnter");
            numpadDivideKey = setup.GetControl<KeyControl>("NumpadDivide");
            numpadMultiplyKey = setup.GetControl<KeyControl>("NumpadMultiply");
            numpadPlusKey = setup.GetControl<KeyControl>("NumpadPlus");
            numpadMinusKey = setup.GetControl<KeyControl>("NumpadMinus");
            numpadPeriodKey = setup.GetControl<KeyControl>("NumpadPeriod");
            numpadEqualsKey = setup.GetControl<KeyControl>("NumpadEquals");
            numpad0Key = setup.GetControl<KeyControl>("Numpad0");
            numpad1Key = setup.GetControl<KeyControl>("Numpad1");
            numpad2Key = setup.GetControl<KeyControl>("Numpad2");
            numpad3Key = setup.GetControl<KeyControl>("Numpad3");
            numpad4Key = setup.GetControl<KeyControl>("Numpad4");
            numpad5Key = setup.GetControl<KeyControl>("Numpad5");
            numpad6Key = setup.GetControl<KeyControl>("Numpad6");
            numpad7Key = setup.GetControl<KeyControl>("Numpad7");
            numpad8Key = setup.GetControl<KeyControl>("Numpad8");
            numpad9Key = setup.GetControl<KeyControl>("Numpad9");
            f1Key = setup.GetControl<KeyControl>("F1");
            f2Key = setup.GetControl<KeyControl>("F2");
            f3Key = setup.GetControl<KeyControl>("F3");
            f4Key = setup.GetControl<KeyControl>("F4");
            f5Key = setup.GetControl<KeyControl>("F5");
            f6Key = setup.GetControl<KeyControl>("F6");
            f7Key = setup.GetControl<KeyControl>("F7");
            f8Key = setup.GetControl<KeyControl>("F8");
            f9Key = setup.GetControl<KeyControl>("F9");
            f10Key = setup.GetControl<KeyControl>("F10");
            f11Key = setup.GetControl<KeyControl>("F11");
            f12Key = setup.GetControl<KeyControl>("F12");
            capsLockKey = setup.GetControl<KeyControl>("CapsLock");
            numLockKey = setup.GetControl<KeyControl>("NumLock");
            scrollLockKey = setup.GetControl<KeyControl>("ScrollLock");
            printScreenKey = setup.GetControl<KeyControl>("PrintScreen");
            pauseKey = setup.GetControl<KeyControl>("Pause");
            oem1Key = setup.GetControl<KeyControl>("OEM1");
            oem2Key = setup.GetControl<KeyControl>("OEM2");
            oem3Key = setup.GetControl<KeyControl>("OEM3");
            oem4Key = setup.GetControl<KeyControl>("OEM4");
            oem5Key = setup.GetControl<KeyControl>("OEM5");

            ////REVIEW: Ideally, we'd have a way to do this through templates; this way nested key controls could work, too,
            ////        and it just seems somewhat dirty to jam the data into the control here

            // Assign key code to all keys.
            for (var key = 1; key < (int)Key.Count; ++key)
                this[(Key)key].keyCode = (Key)key;

            base.FinishSetup(setup);
        }

        protected override unsafe void RefreshConfiguration()
        {
            layout = null;
            var command = QueryKeyboardLayoutCommand.Create();
            if (OnDeviceCommand(ref command) >= 0)
                layout = command.ReadLayoutName();
        }

        public override void OnTextInput(char character)
        {
            for (var i = 0; i < m_TextInputListeners.Count; ++i)
                m_TextInputListeners[i](character);
        }

        internal InlinedArray<Action<char>> m_TextInputListeners;
        private string m_LayoutName;
    }
}
