using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;

////TODO: usages on modifiers so they can be identified regardless of platform conventions

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Default state layout for keyboards.
    /// </summary>
    // NOTE: This layout has to match the KeyboardInputState layout used in native!
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KeyboardState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('K', 'E', 'Y', 'S');

        // Number of keys rounded up to nearest size of 4.
        private const int kSizeInBytesUnrounded = Keyboard.KeyCount / 8 + (Keyboard.KeyCount % 8 > 0 ? 1 : 0);
        internal const int kSizeInBytes = kSizeInBytesUnrounded + (4 - kSizeInBytesUnrounded % 4);
        private const int kSizeInBits = kSizeInBytes * 8;

        [InputControl(name = "anyKey", layout = "AnyKey", sizeInBits = kSizeInBits, synthetic = true)]
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
        [InputControl(name = "IMESelected", layout = "Button", bit = (int)Key.IMESelected, synthetic = true)]
        public fixed byte keys[kSizeInBytes];

        public KeyboardState(params Key[] pressedKeys)
        {
            fixed(byte* keysPtr = keys)
            {
                UnsafeUtility.MemClear(keysPtr, kSizeInBytes);
                for (var i = 0; i < pressedKeys.Length; ++i)
                {
                    MemoryHelpers.WriteSingleBit(keysPtr, (uint)pressedKeys[i], true);
                }
            }
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace UnityEngine.InputSystem
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
        IMESelected
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
    [InputControlLayout(stateType = typeof(KeyboardState), isGenericTypeOfDevice = true)]
    public class Keyboard : InputDevice, ITextInputReceiver
    {
        public const int KeyCount = (int)Key.OEM5;

        /// <summary>
        /// Event that is fired for every single character entered on the keyboard.
        /// </summary>
        public event Action<char> onTextInput
        {
            add => m_TextInputListeners.Append(value);
            remove => m_TextInputListeners.Remove(value);
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
            add => m_ImeCompositionListeners.Append(value);
            remove => m_ImeCompositionListeners.Remove(value);
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
            protected set => m_KeyboardLayoutName = value;
        }


        /// <summary>
        /// A synthetic button control that is considered pressed if any key on the keyboard is pressed.
        /// </summary>
        public AnyKeyControl anyKey { get; private set; }

        /// <summary>
        /// The space bar key.
        /// </summary>
        public KeyControl spaceKey => this[Key.Space];

        /// <summary>
        /// The enter/return key in the main key block.
        /// </summary>
        /// <remarks>
        /// This key is distinct from the enter key on the numpad which is <see cref="numpadEnterKey"/>.
        /// </remarks>
        public KeyControl enterKey => this[Key.Enter];

        /// <summary>
        /// The tab key.
        /// </summary>
        public KeyControl tabKey => this[Key.Tab];

        /// <summary>
        /// The ` key. The leftmost key in the row of digits. Directly above tab.
        /// </summary>
        public KeyControl backquoteKey => this[Key.Backquote];

        /// <summary>
        /// The ' key. The key immediately to the left of the enter/return key.
        /// </summary>
        public KeyControl quoteKey => this[Key.Quote];

        /// <summary>
        /// The ';' key. The key immediately to the left of the quote key.
        /// </summary>
        public KeyControl semicolonKey => this[Key.Semicolon];

        /// <summary>
        /// The ',' key. Third key to the left of the right shift key.
        /// </summary>
        public KeyControl commaKey => this[Key.Comma];
        public KeyControl periodKey => this[Key.Period];
        public KeyControl slashKey => this[Key.Slash];
        public KeyControl backslashKey => this[Key.Backslash];
        public KeyControl leftBracketKey => this[Key.LeftBracket];
        public KeyControl rightBracketKey => this[Key.RightBracket];
        public KeyControl minusKey => this[Key.Minus];

        /// <summary>
        /// The '=' key in the main key block. Key immediately to the left of the backspace key.
        /// </summary>
        public KeyControl equalsKey => this[Key.Equals];

        /// <summary>
        /// The 'a' key. Key immediately to the right of the caps lock key.
        /// </summary>
        public KeyControl aKey => this[Key.A];
        public KeyControl bKey => this[Key.B];
        public KeyControl cKey => this[Key.C];
        public KeyControl dKey => this[Key.D];
        public KeyControl eKey => this[Key.E];
        public KeyControl fKey => this[Key.F];
        public KeyControl gKey => this[Key.G];
        public KeyControl hKey => this[Key.H];
        public KeyControl iKey => this[Key.I];
        public KeyControl jKey => this[Key.J];
        public KeyControl kKey => this[Key.K];
        public KeyControl lKey => this[Key.L];
        public KeyControl mKey => this[Key.M];
        public KeyControl nKey => this[Key.N];
        public KeyControl oKey => this[Key.O];
        public KeyControl pKey => this[Key.P];
        public KeyControl qKey => this[Key.Q];
        public KeyControl rKey => this[Key.R];
        public KeyControl sKey => this[Key.S];
        public KeyControl tKey => this[Key.T];
        public KeyControl uKey => this[Key.U];
        public KeyControl vKey => this[Key.V];
        public KeyControl wKey => this[Key.W];
        public KeyControl xKey => this[Key.X];
        public KeyControl yKey => this[Key.Y];
        public KeyControl zKey => this[Key.Z];
        public KeyControl digit1Key => this[Key.Digit1];
        public KeyControl digit2Key => this[Key.Digit2];
        public KeyControl digit3Key => this[Key.Digit3];
        public KeyControl digit4Key => this[Key.Digit4];
        public KeyControl digit5Key => this[Key.Digit5];
        public KeyControl digit6Key => this[Key.Digit6];
        public KeyControl digit7Key => this[Key.Digit7];
        public KeyControl digit8Key => this[Key.Digit8];
        public KeyControl digit9Key => this[Key.Digit9];
        public KeyControl digit0Key => this[Key.Digit0];
        public KeyControl leftShiftKey => this[Key.LeftShift];
        public KeyControl rightShiftKey => this[Key.RightShift];
        public KeyControl leftAltKey => this[Key.LeftAlt];
        public KeyControl rightAltKey => this[Key.RightAlt];
        public KeyControl leftCtrlKey => this[Key.LeftCtrl];
        public KeyControl rightCtrlKey => this[Key.RightCtrl];
        public KeyControl leftMetaKey => this[Key.LeftMeta];
        public KeyControl rightMetaKey => this[Key.RightMeta];
        public KeyControl leftWindowsKey => this[Key.LeftWindows];
        public KeyControl rightWindowsKey => this[Key.RightWindows];
        public KeyControl leftAppleKey => this[Key.LeftApple];
        public KeyControl rightAppleKey => this[Key.RightApple];
        public KeyControl leftCommandKey => this[Key.LeftCommand];
        public KeyControl rightCommandKey => this[Key.RightCommand];
        public KeyControl contextMenuKey => this[Key.ContextMenu];
        public KeyControl escapeKey => this[Key.Escape];
        public KeyControl leftArrowKey => this[Key.LeftArrow];
        public KeyControl rightArrowKey => this[Key.RightArrow];
        public KeyControl upArrowKey => this[Key.UpArrow];
        public KeyControl downArrowKey => this[Key.DownArrow];
        public KeyControl backspaceKey => this[Key.Backspace];
        public KeyControl pageDownKey => this[Key.PageDown];
        public KeyControl pageUpKey => this[Key.PageUp];
        public KeyControl homeKey => this[Key.Home];
        public KeyControl endKey => this[Key.End];
        public KeyControl insertKey => this[Key.Insert];
        public KeyControl deleteKey => this[Key.Delete];
        public KeyControl capsLockKey => this[Key.CapsLock];
        public KeyControl scrollLockKey => this[Key.ScrollLock];
        public KeyControl numLockKey => this[Key.NumLock];
        public KeyControl printScreenKey => this[Key.PrintScreen];
        public KeyControl pauseKey => this[Key.Pause];
        public KeyControl numpadEnterKey => this[Key.NumpadEnter];
        public KeyControl numpadDivideKey => this[Key.NumpadDivide];
        public KeyControl numpadMultiplyKey => this[Key.NumpadMultiply];
        public KeyControl numpadMinusKey => this[Key.NumpadMinus];
        public KeyControl numpadPlusKey => this[Key.NumpadPlus];
        public KeyControl numpadPeriodKey => this[Key.NumpadPeriod];
        public KeyControl numpadEqualsKey => this[Key.NumpadEquals];
        public KeyControl numpad0Key => this[Key.Numpad0];
        public KeyControl numpad1Key => this[Key.Numpad1];
        public KeyControl numpad2Key => this[Key.Numpad2];
        public KeyControl numpad3Key => this[Key.Numpad3];
        public KeyControl numpad4Key => this[Key.Numpad4];
        public KeyControl numpad5Key => this[Key.Numpad5];
        public KeyControl numpad6Key => this[Key.Numpad6];
        public KeyControl numpad7Key => this[Key.Numpad7];
        public KeyControl numpad8Key => this[Key.Numpad8];
        public KeyControl numpad9Key => this[Key.Numpad9];
        public KeyControl f1Key => this[Key.F1];
        public KeyControl f2Key => this[Key.F2];
        public KeyControl f3Key => this[Key.F3];
        public KeyControl f4Key => this[Key.F4];
        public KeyControl f5Key => this[Key.F5];
        public KeyControl f6Key => this[Key.F6];
        public KeyControl f7Key => this[Key.F7];
        public KeyControl f8Key => this[Key.F8];
        public KeyControl f9Key => this[Key.F9];
        public KeyControl f10Key => this[Key.F10];
        public KeyControl f11Key => this[Key.F11];
        public KeyControl f12Key => this[Key.F12];
        public KeyControl oem1Key => this[Key.OEM1];
        public KeyControl oem2Key => this[Key.OEM2];
        public KeyControl oem3Key => this[Key.OEM3];
        public KeyControl oem4Key => this[Key.OEM4];
        public KeyControl oem5Key => this[Key.OEM5];

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

        /// <summary>
        /// Look up a key control by its key code.
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentException">The given <see cref="key"/> is not valid.</exception>
        public KeyControl this[Key key]
        {
            get
            {
                var index = (int)key;
                if (index <= 0 || index >= m_Keys.Length)
                    throw new ArgumentOutOfRangeException(nameof(key));
                return m_Keys[(int)key - 1];
            }
        }

        public static Keyboard current { get; private set; }

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

        private KeyControl[] m_Keys;

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            var keyStrings = new[]
            {
                "space",
                "enter",
                "tab",
                "backquote",
                "quote",
                "semicolon",
                "comma",
                "period",
                "slash",
                "backslash",
                "leftbracket",
                "rightbracket",
                "minus",
                "equals",
                "a",
                "b",
                "c",
                "d",
                "e",
                "f",
                "g",
                "h",
                "i",
                "j",
                "k",
                "l",
                "m",
                "n",
                "o",
                "p",
                "q",
                "r",
                "s",
                "t",
                "u",
                "v",
                "w",
                "x",
                "y",
                "z",
                "1",
                "2",
                "3",
                "4",
                "5",
                "6",
                "7",
                "8",
                "9",
                "0",
                "leftshift",
                "rightshift",
                "leftalt",
                "rightalt",
                "leftctrl",
                "rightctrl",
                "leftmeta",
                "rightmeta",
                "contextmenu",
                "escape",
                "leftarrow",
                "rightarrow",
                "uparrow",
                "downarrow",
                "backspace",
                "pagedown",
                "pageup",
                "home",
                "end",
                "insert",
                "delete",
                "capslock",
                "numlock",
                "printscreen",
                "scrolllock",
                "pause",
                "numpadenter",
                "numpaddivide",
                "numpadmultiply",
                "numpadplus",
                "numpadminus",
                "numpadperiod",
                "numpadequals",
                "numpad0",
                "numpad1",
                "numpad2",
                "numpad3",
                "numpad4",
                "numpad5",
                "numpad6",
                "numpad7",
                "numpad8",
                "numpad9",
                "f1",
                "f2",
                "f3",
                "f4",
                "f5",
                "f6",
                "f7",
                "f8",
                "f9",
                "f10",
                "f11",
                "f12",
                "oem1",
                "oem2",
                "oem3",
                "oem4",
                "oem5",
            };
            m_Keys = new KeyControl[keyStrings.Length];
            for (var i = 0; i < keyStrings.Length; ++i)
            {
                m_Keys[i] = builder.GetControl<KeyControl>(keyStrings[i]);

                ////REVIEW: Ideally, we'd have a way to do this through layouts; this way nested key controls could work, too,
                ////        and it just seems somewhat dirty to jam the data into the control here
                m_Keys[i].keyCode = (Key)(i + 1);
            }
            Debug.Assert(keyStrings[(int)Key.OEM5 - 1] == "oem5",
                "keyString array layout doe not match Key enum layout");
            anyKey = builder.GetControl<AnyKeyControl>("anyKey");
            imeSelected = builder.GetControl<ButtonControl>("IMESelected");

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

    public static class KeyboardExtensions
    {
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
    }
}
