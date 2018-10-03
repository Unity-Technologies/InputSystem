using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Layouts;

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
        private const int kSizeInBytesUnrounded = ((int)Keyboard.KeyCount) / 8 + (((int)Keyboard.KeyCount) % 8 > 0 ? 1 : 0);
        public const int kSizeInBytes = kSizeInBytesUnrounded + (4 - kSizeInBytesUnrounded % 4);
        public const int kSizeInBits = kSizeInBytes * 8;

        [InputControl(name = "anyKey", layout = "AnyKey", sizeInBits = kSizeInBits)]
        [InputControl(name = "escape", layout = "Key", usages = new[] {"Back", "Cancel"}, bit = (int)Key.Escape)]
        [InputControl(name = "space", layout = "Key", bit = (int)Key.Space)]
        [InputControl(name = "enter", layout = "Key", usage = "Accept", bit = (int)Key.Enter)]
        [InputControl(name = "tab", layout = "Key", bit = (int)Key.Tab)]
        [InputControl(name = "backquote", layout = "Key", bit = (int)Key.Backquote)]
        [InputControl(name = "quote", layout = "Key", bit = (int)Key.Quote)]
        [InputControl(name = "semicolon", layout = "Key", bit = (int)Key.Semicolon)]
        [InputControl(name = "comma", layout = "Key", bit = (int)Key.Comma)]
        [InputControl(name = "period", layout = "Key", bit = (int)Key.Period)]
        [InputControl(name = "slash", layout = "Key", bit = (int)Key.Slash)]
        [InputControl(name = "backslash", layout = "Key", bit = (int)Key.Backslash)]
        [InputControl(name = "leftBracket", layout = "Key", bit = (int)Key.LeftBracket)]
        [InputControl(name = "rightBracket", layout = "Key", bit = (int)Key.RightBracket)]
        [InputControl(name = "minus", layout = "Key", bit = (int)Key.Minus)]
        [InputControl(name = "equals", layout = "Key", bit = (int)Key.Equals)]
        [InputControl(name = "upArrow", layout = "Key", bit = (int)Key.UpArrow)]
        [InputControl(name = "downArrow", layout = "Key", bit = (int)Key.DownArrow)]
        [InputControl(name = "leftArrow", layout = "Key", bit = (int)Key.LeftArrow)]
        [InputControl(name = "rightArrow", layout = "Key", bit = (int)Key.RightArrow)]
        [InputControl(name = "a", layout = "Key", bit = (int)Key.A)]
        [InputControl(name = "b", layout = "Key", bit = (int)Key.B)]
        [InputControl(name = "c", layout = "Key", bit = (int)Key.C)]
        [InputControl(name = "d", layout = "Key", bit = (int)Key.D)]
        [InputControl(name = "e", layout = "Key", bit = (int)Key.E)]
        [InputControl(name = "f", layout = "Key", bit = (int)Key.F)]
        [InputControl(name = "g", layout = "Key", bit = (int)Key.G)]
        [InputControl(name = "h", layout = "Key", bit = (int)Key.H)]
        [InputControl(name = "i", layout = "Key", bit = (int)Key.I)]
        [InputControl(name = "j", layout = "Key", bit = (int)Key.J)]
        [InputControl(name = "k", layout = "Key", bit = (int)Key.K)]
        [InputControl(name = "l", layout = "Key", bit = (int)Key.L)]
        [InputControl(name = "m", layout = "Key", bit = (int)Key.M)]
        [InputControl(name = "n", layout = "Key", bit = (int)Key.N)]
        [InputControl(name = "o", layout = "Key", bit = (int)Key.O)]
        [InputControl(name = "p", layout = "Key", bit = (int)Key.P)]
        [InputControl(name = "q", layout = "Key", bit = (int)Key.Q)]
        [InputControl(name = "r", layout = "Key", bit = (int)Key.R)]
        [InputControl(name = "s", layout = "Key", bit = (int)Key.S)]
        [InputControl(name = "t", layout = "Key", bit = (int)Key.T)]
        [InputControl(name = "u", layout = "Key", bit = (int)Key.U)]
        [InputControl(name = "v", layout = "Key", bit = (int)Key.V)]
        [InputControl(name = "w", layout = "Key", bit = (int)Key.W)]
        [InputControl(name = "x", layout = "Key", bit = (int)Key.X)]
        [InputControl(name = "y", layout = "Key", bit = (int)Key.Y)]
        [InputControl(name = "z", layout = "Key", bit = (int)Key.Z)]
        [InputControl(name = "1", layout = "Key", bit = (int)Key.Digit1)]
        [InputControl(name = "2", layout = "Key", bit = (int)Key.Digit2)]
        [InputControl(name = "3", layout = "Key", bit = (int)Key.Digit3)]
        [InputControl(name = "4", layout = "Key", bit = (int)Key.Digit4)]
        [InputControl(name = "5", layout = "Key", bit = (int)Key.Digit5)]
        [InputControl(name = "6", layout = "Key", bit = (int)Key.Digit6)]
        [InputControl(name = "7", layout = "Key", bit = (int)Key.Digit7)]
        [InputControl(name = "8", layout = "Key", bit = (int)Key.Digit8)]
        [InputControl(name = "9", layout = "Key", bit = (int)Key.Digit9)]
        [InputControl(name = "0", layout = "Key", bit = (int)Key.Digit0)]
        [InputControl(name = "leftShift", layout = "Key", usage = "Modifier", bit = (int)Key.LeftShift)]
        [InputControl(name = "rightShift", layout = "Key", usage = "Modifier", bit = (int)Key.RightShift)]
        [InputControl(name = "leftAlt", layout = "Key", usage = "Modifier", bit = (int)Key.LeftAlt)]
        [InputControl(name = "rightAlt", layout = "Key", usage = "Modifier", bit = (int)Key.RightAlt, alias = "AltGr")]
        [InputControl(name = "leftCtrl", layout = "Key", usage = "Modifier", bit = (int)Key.LeftCtrl)]
        [InputControl(name = "rightCtrl", layout = "Key", usage = "Modifier", bit = (int)Key.RightCtrl)]
        [InputControl(name = "leftMeta", layout = "Key", usage = "Modifier", bit = (int)Key.LeftMeta, aliases = new[] { "LeftWindows", "LeftApple", "LeftCommand" })]
        [InputControl(name = "rightMeta", layout = "Key", usage = "Modifier", bit = (int)Key.RightMeta, aliases = new[] { "RightWindows", "RightApple", "RightCommand" })]
        [InputControl(name = "contextMenu", layout = "Key", usage = "Modifier", bit = (int)Key.ContextMenu)]
        [InputControl(name = "backspace", layout = "Key", bit = (int)Key.Backspace)]
        [InputControl(name = "pageDown", layout = "Key", bit = (int)Key.PageDown)]
        [InputControl(name = "pageUp", layout = "Key", bit = (int)Key.PageUp)]
        [InputControl(name = "home", layout = "Key", bit = (int)Key.Home)]
        [InputControl(name = "end", layout = "Key", bit = (int)Key.End)]
        [InputControl(name = "insert", layout = "Key", bit = (int)Key.Insert)]
        [InputControl(name = "delete", layout = "Key", bit = (int)Key.Delete)]
        [InputControl(name = "capsLock", layout = "Key", bit = (int)Key.CapsLock)]
        [InputControl(name = "numLock", layout = "Key", bit = (int)Key.NumLock)]
        [InputControl(name = "printScreen", layout = "Key", bit = (int)Key.PrintScreen)]
        [InputControl(name = "scrollLock", layout = "Key", bit = (int)Key.ScrollLock)]
        [InputControl(name = "pause", layout = "Key", bit = (int)Key.Pause)]
        [InputControl(name = "numpadEnter", layout = "Key", bit = (int)Key.NumpadEnter)]
        [InputControl(name = "numpadDivide", layout = "Key", bit = (int)Key.NumpadDivide)]
        [InputControl(name = "numpadMultiply", layout = "Key", bit = (int)Key.NumpadMultiply)]
        [InputControl(name = "numpadPlus", layout = "Key", bit = (int)Key.NumpadPlus)]
        [InputControl(name = "numpadMinus", layout = "Key", bit = (int)Key.NumpadMinus)]
        [InputControl(name = "numpadPeriod", layout = "Key", bit = (int)Key.NumpadPeriod)]
        [InputControl(name = "numpadEquals", layout = "Key", bit = (int)Key.NumpadEquals)]
        [InputControl(name = "numpad1", layout = "Key", bit = (int)Key.Numpad1)]
        [InputControl(name = "numpad2", layout = "Key", bit = (int)Key.Numpad2)]
        [InputControl(name = "numpad3", layout = "Key", bit = (int)Key.Numpad3)]
        [InputControl(name = "numpad4", layout = "Key", bit = (int)Key.Numpad4)]
        [InputControl(name = "numpad5", layout = "Key", bit = (int)Key.Numpad5)]
        [InputControl(name = "numpad6", layout = "Key", bit = (int)Key.Numpad6)]
        [InputControl(name = "numpad7", layout = "Key", bit = (int)Key.Numpad7)]
        [InputControl(name = "numpad8", layout = "Key", bit = (int)Key.Numpad8)]
        [InputControl(name = "numpad9", layout = "Key", bit = (int)Key.Numpad9)]
        [InputControl(name = "numpad0", layout = "Key", bit = (int)Key.Numpad0)]
        [InputControl(name = "f1", layout = "Key", bit = (int)Key.F1)]
        [InputControl(name = "f2", layout = "Key", bit = (int)Key.F2)]
        [InputControl(name = "f3", layout = "Key", bit = (int)Key.F3)]
        [InputControl(name = "f4", layout = "Key", bit = (int)Key.F4)]
        [InputControl(name = "f5", layout = "Key", bit = (int)Key.F5)]
        [InputControl(name = "f6", layout = "Key", bit = (int)Key.F6)]
        [InputControl(name = "f7", layout = "Key", bit = (int)Key.F7)]
        [InputControl(name = "f8", layout = "Key", bit = (int)Key.F8)]
        [InputControl(name = "f9", layout = "Key", bit = (int)Key.F9)]
        [InputControl(name = "f10", layout = "Key", bit = (int)Key.F10)]
        [InputControl(name = "f11", layout = "Key", bit = (int)Key.F11)]
        [InputControl(name = "f12", layout = "Key", bit = (int)Key.F12)]
        [InputControl(name = "OEM1", layout = "Key", bit = (int)Key.OEM1)]
        [InputControl(name = "OEM2", layout = "Key", bit = (int)Key.OEM2)]
        [InputControl(name = "OEM3", layout = "Key", bit = (int)Key.OEM3)]
        [InputControl(name = "OEM4", layout = "Key", bit = (int)Key.OEM4)]
        [InputControl(name = "OEM5", layout = "Key", bit = (int)Key.OEM5)]
        [InputControl(name = "imeSelected", layout = "Button", bit = (int)Key.imeSelected)]
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

        // Not exactly a key, but binary data sent by the Keyboard to say if IME is being used.
        imeSelected
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
    [InputControlLayout(stateType = typeof(KeyboardState))]
    public class Keyboard : InputDevice, ITextInputReceiver
    {
        public const int KeyCount = (int)Key.OEM5;

        /// <summary>
        /// Event that is fired for every single character entered on the keyboard.
        /// </summary>
        public event Action<char> onTextInput
        {
            add { m_TextInputListeners.Append(value); }
            remove { m_TextInputListeners.Remove(value); }
        }

        /// <summary>
        /// An event that is fired to get IME composition strings.  Fired once for every change, sends the entire string to date, and sends a blank string whenever a composition is submitted or reset.
        /// </summary>
        /// <remarks>
        ///
        /// Some languages use complex input methods which involve opening windows to insert characters.
        /// Typically, this is not desirable while playing a game, as games may just interpret key strokes as game input, not as text.
        ///
        /// See <see cref="Keyboard.imeEnabled"/> for turning IME on/off
        /// </remarks>
        public event Action<IMECompositionString> onIMECompositionChange
        {
            add { m_ImeCompositionListeners.Append(value); }
            remove { m_ImeCompositionListeners.Remove(value); }
        }

        /// <summary>
        /// Activates/deactivates IME composition while typing.  This decides whether or not to use the OS supplied IME system.
        /// </summary>
        /// <remarks>
        ///
        /// Some languages use complex input methods which involve opening windows to insert characters.
        /// Typically, this is not desirable while playing a game, as games may just interpret key strokes as game input, not as text.
        /// Setting this to On, will enable the OS-level IME system when the user presses keystrokes.
        ///
        /// See <see cref="Keyboard.imeCursorPosition"/>, <see cref="Keyboard.onIMECompositionChange"/>, <see cref="Keyboard.imeSelected"/> for more IME settings and data.
        /// </remarks>
        public bool imeEnabled
        {
            set
            {
                EnableIMECompositionCommand command = EnableIMECompositionCommand.Create(value);
                ExecuteCommand(ref command);
            }
        }

        /// <summary>
        /// Sets the cursor position for IME composition dialogs.  Units are from the upper left, in pixels, moving down and to the right.
        /// </summary>
        /// <remarks>
        ///
        /// Some languages use complex input methods which involve opening windows to insert characters.
        /// Typically, this is not desirable while playing a game, as games may just interpret key strokes as game input, not as text.
        ///
        /// See <see cref="Keyboard.imeEnabled"/> for turning IME on/off
        /// </remarks>
        public Vector2 imeCursorPosition
        {
            set
            {
                SetIMECursorPositionCommand command = SetIMECursorPositionCommand.Create(value);
                ExecuteCommand(ref command);
            }
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
        public string keyboardLayout
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_KeyboardLayoutName;
            }
            protected set { m_KeyboardLayoutName = value; }
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

        /// <summary>
        /// True when IME composition is enabled.  Requires <see cref="Keyboard.imeEnabled"/> to be set to true, and the user to enable it at the OS level.
        /// </summary>
        /// <remarks>
        ///
        /// Some languages use complex input methods which involve opening windows to insert characters.
        /// Typically, this is not desirable while playing a game, as games may just interpret key strokes as game input, not as text.
        ///
        /// See <see cref="Keyboard.imeEnabled"/> for turning IME on/off
        /// </remarks>
        public ButtonControl imeSelected { get; private set; }

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

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            anyKey = builder.GetControl<AnyKeyControl>("anyKey");
            spaceKey = builder.GetControl<KeyControl>("space");
            enterKey = builder.GetControl<KeyControl>("enter");
            tabKey = builder.GetControl<KeyControl>("tab");
            backquoteKey = builder.GetControl<KeyControl>("backquote");
            quoteKey = builder.GetControl<KeyControl>("quote");
            semicolonKey = builder.GetControl<KeyControl>("semicolon");
            commaKey = builder.GetControl<KeyControl>("comma");
            periodKey = builder.GetControl<KeyControl>("period");
            slashKey = builder.GetControl<KeyControl>("slash");
            backslashKey = builder.GetControl<KeyControl>("backslash");
            leftBracketKey = builder.GetControl<KeyControl>("leftBracket");
            rightBracketKey = builder.GetControl<KeyControl>("rightBracket");
            minusKey = builder.GetControl<KeyControl>("minus");
            equalsKey = builder.GetControl<KeyControl>("equals");
            aKey = builder.GetControl<KeyControl>("a");
            bKey = builder.GetControl<KeyControl>("b");
            cKey = builder.GetControl<KeyControl>("c");
            dKey = builder.GetControl<KeyControl>("d");
            eKey = builder.GetControl<KeyControl>("e");
            fKey = builder.GetControl<KeyControl>("f");
            gKey = builder.GetControl<KeyControl>("g");
            hKey = builder.GetControl<KeyControl>("h");
            iKey = builder.GetControl<KeyControl>("i");
            jKey = builder.GetControl<KeyControl>("j");
            kKey = builder.GetControl<KeyControl>("k");
            lKey = builder.GetControl<KeyControl>("l");
            mKey = builder.GetControl<KeyControl>("m");
            nKey = builder.GetControl<KeyControl>("n");
            oKey = builder.GetControl<KeyControl>("o");
            pKey = builder.GetControl<KeyControl>("p");
            qKey = builder.GetControl<KeyControl>("q");
            rKey = builder.GetControl<KeyControl>("r");
            sKey = builder.GetControl<KeyControl>("s");
            tKey = builder.GetControl<KeyControl>("t");
            uKey = builder.GetControl<KeyControl>("u");
            vKey = builder.GetControl<KeyControl>("v");
            wKey = builder.GetControl<KeyControl>("w");
            xKey = builder.GetControl<KeyControl>("x");
            yKey = builder.GetControl<KeyControl>("y");
            zKey = builder.GetControl<KeyControl>("z");
            digit1Key = builder.GetControl<KeyControl>("1");
            digit2Key = builder.GetControl<KeyControl>("2");
            digit3Key = builder.GetControl<KeyControl>("3");
            digit4Key = builder.GetControl<KeyControl>("4");
            digit5Key = builder.GetControl<KeyControl>("5");
            digit6Key = builder.GetControl<KeyControl>("6");
            digit7Key = builder.GetControl<KeyControl>("7");
            digit8Key = builder.GetControl<KeyControl>("8");
            digit9Key = builder.GetControl<KeyControl>("9");
            digit0Key = builder.GetControl<KeyControl>("0");
            leftShiftKey = builder.GetControl<KeyControl>("leftShift");
            rightShiftKey = builder.GetControl<KeyControl>("rightShift");
            leftAltKey = builder.GetControl<KeyControl>("leftAlt");
            rightAltKey = builder.GetControl<KeyControl>("rightAlt");
            leftCtrlKey = builder.GetControl<KeyControl>("leftCtrl");
            rightCtrlKey = builder.GetControl<KeyControl>("rightCtrl");
            leftMetaKey = builder.GetControl<KeyControl>("leftMeta");
            rightMetaKey = builder.GetControl<KeyControl>("rightMeta");
            leftWindowsKey = builder.GetControl<KeyControl>("leftWindows");
            rightWindowsKey = builder.GetControl<KeyControl>("rightWindows");
            leftAppleKey = builder.GetControl<KeyControl>("leftApple");
            rightAppleKey = builder.GetControl<KeyControl>("rightApple");
            leftCommandKey = builder.GetControl<KeyControl>("leftCommand");
            rightCommandKey = builder.GetControl<KeyControl>("rightCommand");
            contextMenuKey = builder.GetControl<KeyControl>("contextMenu");
            escapeKey = builder.GetControl<KeyControl>("escape");
            leftArrowKey = builder.GetControl<KeyControl>("leftArrow");
            rightArrowKey = builder.GetControl<KeyControl>("rightArrow");
            upArrowKey = builder.GetControl<KeyControl>("upArrow");
            downArrowKey = builder.GetControl<KeyControl>("downArrow");
            backspaceKey = builder.GetControl<KeyControl>("backspace");
            pageDownKey = builder.GetControl<KeyControl>("pageDown");
            pageUpKey = builder.GetControl<KeyControl>("pageUp");
            homeKey = builder.GetControl<KeyControl>("home");
            endKey = builder.GetControl<KeyControl>("end");
            insertKey = builder.GetControl<KeyControl>("insert");
            deleteKey = builder.GetControl<KeyControl>("delete");
            numpadEnterKey = builder.GetControl<KeyControl>("numpadEnter");
            numpadDivideKey = builder.GetControl<KeyControl>("numpadDivide");
            numpadMultiplyKey = builder.GetControl<KeyControl>("numpadMultiply");
            numpadPlusKey = builder.GetControl<KeyControl>("numpadPlus");
            numpadMinusKey = builder.GetControl<KeyControl>("numpadMinus");
            numpadPeriodKey = builder.GetControl<KeyControl>("numpadPeriod");
            numpadEqualsKey = builder.GetControl<KeyControl>("numpadEquals");
            numpad0Key = builder.GetControl<KeyControl>("numpad0");
            numpad1Key = builder.GetControl<KeyControl>("numpad1");
            numpad2Key = builder.GetControl<KeyControl>("numpad2");
            numpad3Key = builder.GetControl<KeyControl>("numpad3");
            numpad4Key = builder.GetControl<KeyControl>("numpad4");
            numpad5Key = builder.GetControl<KeyControl>("numpad5");
            numpad6Key = builder.GetControl<KeyControl>("numpad6");
            numpad7Key = builder.GetControl<KeyControl>("numpad7");
            numpad8Key = builder.GetControl<KeyControl>("numpad8");
            numpad9Key = builder.GetControl<KeyControl>("numpad9");
            f1Key = builder.GetControl<KeyControl>("f1");
            f2Key = builder.GetControl<KeyControl>("f2");
            f3Key = builder.GetControl<KeyControl>("f3");
            f4Key = builder.GetControl<KeyControl>("f4");
            f5Key = builder.GetControl<KeyControl>("f5");
            f6Key = builder.GetControl<KeyControl>("f6");
            f7Key = builder.GetControl<KeyControl>("f7");
            f8Key = builder.GetControl<KeyControl>("f8");
            f9Key = builder.GetControl<KeyControl>("f9");
            f10Key = builder.GetControl<KeyControl>("f10");
            f11Key = builder.GetControl<KeyControl>("f11");
            f12Key = builder.GetControl<KeyControl>("f12");
            capsLockKey = builder.GetControl<KeyControl>("capsLock");
            numLockKey = builder.GetControl<KeyControl>("numLock");
            scrollLockKey = builder.GetControl<KeyControl>("scrollLock");
            printScreenKey = builder.GetControl<KeyControl>("printScreen");
            pauseKey = builder.GetControl<KeyControl>("pause");
            oem1Key = builder.GetControl<KeyControl>("OEM1");
            oem2Key = builder.GetControl<KeyControl>("OEM2");
            oem3Key = builder.GetControl<KeyControl>("OEM3");
            oem4Key = builder.GetControl<KeyControl>("OEM4");
            oem5Key = builder.GetControl<KeyControl>("OEM5");

            imeSelected = builder.GetControl<ButtonControl>("imeSelected");

            ////REVIEW: Ideally, we'd have a way to do this through layouts; this way nested key controls could work, too,
            ////        and it just seems somewhat dirty to jam the data into the control here

            // Assign key code to all keys.
            for (var key = 1; key < (int)Keyboard.KeyCount; ++key)
                this[(Key)key].keyCode = (Key)key;

            base.FinishSetup(builder);
        }

        protected override void RefreshConfiguration()
        {
            keyboardLayout = null;
            var command = QueryKeyboardLayoutCommand.Create();
            if (ExecuteCommand(ref command) >= 0)
                keyboardLayout = command.ReadLayoutName();
        }

        public void OnTextInput(char character)
        {
            for (var i = 0; i < m_TextInputListeners.length; ++i)
                m_TextInputListeners[i](character);
        }

        public void OnIMECompositionChanged(IMECompositionString compositionString)
        {
            if (m_ImeCompositionListeners.length > 0)
            {
                for (var i = 0; i < m_ImeCompositionListeners.length; ++i)
                    m_ImeCompositionListeners[i](compositionString);
            }
        }

        internal InlinedArray<Action<char>> m_TextInputListeners;
        private string m_KeyboardLayoutName;

        internal InlinedArray<Action<IMECompositionString>> m_ImeCompositionListeners;
    }
}
