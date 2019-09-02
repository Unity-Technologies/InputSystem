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

        [InputControl(name = "anyKey", displayName = "Any Key", layout = "AnyKey", sizeInBits = kSizeInBits, synthetic = true)]
        [InputControl(name = "escape", displayName = "Escape", layout = "Key", usages = new[] {"Back", "Cancel"}, bit = (int)Key.Escape)]
        [InputControl(name = "space", displayName = "Space", layout = "Key", bit = (int)Key.Space)]
        [InputControl(name = "enter", displayName = "Enter", layout = "Key", usage = "Accept", bit = (int)Key.Enter)]
        [InputControl(name = "tab", displayName = "Tab", layout = "Key", bit = (int)Key.Tab)]
        [InputControl(name = "backquote", displayName = "`", layout = "Key", bit = (int)Key.Backquote)]
        [InputControl(name = "quote", displayName = "'", layout = "Key", bit = (int)Key.Quote)]
        [InputControl(name = "semicolon", displayName = ";", layout = "Key", bit = (int)Key.Semicolon)]
        [InputControl(name = "comma", displayName = ",", layout = "Key", bit = (int)Key.Comma)]
        [InputControl(name = "period", displayName = ".", layout = "Key", bit = (int)Key.Period)]
        [InputControl(name = "slash", displayName = "/", layout = "Key", bit = (int)Key.Slash)]
        [InputControl(name = "backslash", displayName = "\\", layout = "Key", bit = (int)Key.Backslash)]
        [InputControl(name = "leftBracket", displayName = "[", layout = "Key", bit = (int)Key.LeftBracket)]
        [InputControl(name = "rightBracket", displayName = "]", layout = "Key", bit = (int)Key.RightBracket)]
        [InputControl(name = "minus", displayName = "-", layout = "Key", bit = (int)Key.Minus)]
        [InputControl(name = "equals", displayName = "=", layout = "Key", bit = (int)Key.Equals)]
        [InputControl(name = "upArrow", displayName = "Up Arrow", layout = "Key", bit = (int)Key.UpArrow)]
        [InputControl(name = "downArrow", displayName = "Down Arrow", layout = "Key", bit = (int)Key.DownArrow)]
        [InputControl(name = "leftArrow", displayName = "Left Arrow", layout = "Key", bit = (int)Key.LeftArrow)]
        [InputControl(name = "rightArrow", displayName = "Right Arrow", layout = "Key", bit = (int)Key.RightArrow)]
        [InputControl(name = "a", displayName = "A", layout = "Key", bit = (int)Key.A)]
        [InputControl(name = "b", displayName = "B", layout = "Key", bit = (int)Key.B)]
        [InputControl(name = "c", displayName = "C", layout = "Key", bit = (int)Key.C)]
        [InputControl(name = "d", displayName = "D", layout = "Key", bit = (int)Key.D)]
        [InputControl(name = "e", displayName = "E", layout = "Key", bit = (int)Key.E)]
        [InputControl(name = "f", displayName = "F", layout = "Key", bit = (int)Key.F)]
        [InputControl(name = "g", displayName = "G", layout = "Key", bit = (int)Key.G)]
        [InputControl(name = "h", displayName = "H", layout = "Key", bit = (int)Key.H)]
        [InputControl(name = "i", displayName = "I", layout = "Key", bit = (int)Key.I)]
        [InputControl(name = "j", displayName = "J", layout = "Key", bit = (int)Key.J)]
        [InputControl(name = "k", displayName = "K", layout = "Key", bit = (int)Key.K)]
        [InputControl(name = "l", displayName = "L", layout = "Key", bit = (int)Key.L)]
        [InputControl(name = "m", displayName = "M", layout = "Key", bit = (int)Key.M)]
        [InputControl(name = "n", displayName = "N", layout = "Key", bit = (int)Key.N)]
        [InputControl(name = "o", displayName = "O", layout = "Key", bit = (int)Key.O)]
        [InputControl(name = "p", displayName = "P", layout = "Key", bit = (int)Key.P)]
        [InputControl(name = "q", displayName = "Q", layout = "Key", bit = (int)Key.Q)]
        [InputControl(name = "r", displayName = "R", layout = "Key", bit = (int)Key.R)]
        [InputControl(name = "s", displayName = "S", layout = "Key", bit = (int)Key.S)]
        [InputControl(name = "t", displayName = "T", layout = "Key", bit = (int)Key.T)]
        [InputControl(name = "u", displayName = "U", layout = "Key", bit = (int)Key.U)]
        [InputControl(name = "v", displayName = "V", layout = "Key", bit = (int)Key.V)]
        [InputControl(name = "w", displayName = "W", layout = "Key", bit = (int)Key.W)]
        [InputControl(name = "x", displayName = "X", layout = "Key", bit = (int)Key.X)]
        [InputControl(name = "y", displayName = "Y", layout = "Key", bit = (int)Key.Y)]
        [InputControl(name = "z", displayName = "Z", layout = "Key", bit = (int)Key.Z)]
        [InputControl(name = "1", displayName = "1", layout = "Key", bit = (int)Key.Digit1)]
        [InputControl(name = "2", displayName = "2", layout = "Key", bit = (int)Key.Digit2)]
        [InputControl(name = "3", displayName = "3", layout = "Key", bit = (int)Key.Digit3)]
        [InputControl(name = "4", displayName = "4", layout = "Key", bit = (int)Key.Digit4)]
        [InputControl(name = "5", displayName = "5", layout = "Key", bit = (int)Key.Digit5)]
        [InputControl(name = "6", displayName = "6", layout = "Key", bit = (int)Key.Digit6)]
        [InputControl(name = "7", displayName = "7", layout = "Key", bit = (int)Key.Digit7)]
        [InputControl(name = "8", displayName = "8", layout = "Key", bit = (int)Key.Digit8)]
        [InputControl(name = "9", displayName = "9", layout = "Key", bit = (int)Key.Digit9)]
        [InputControl(name = "0", displayName = "0", layout = "Key", bit = (int)Key.Digit0)]
        [InputControl(name = "leftShift", displayName = "Left Shift", layout = "Key", usage = "Modifier", bit = (int)Key.LeftShift)]
        [InputControl(name = "rightShift", displayName = "Right Shift", layout = "Key", usage = "Modifier", bit = (int)Key.RightShift)]
        [InputControl(name = "leftAlt", displayName = "Left Alt", layout = "Key", usage = "Modifier", bit = (int)Key.LeftAlt)]
        [InputControl(name = "rightAlt", displayName = "Right Alt", layout = "Key", usage = "Modifier", bit = (int)Key.RightAlt, alias = "AltGr")]
        [InputControl(name = "leftCtrl", displayName = "Left Control", layout = "Key", usage = "Modifier", bit = (int)Key.LeftCtrl)]
        [InputControl(name = "rightCtrl", displayName = "Rigth Control", layout = "Key", usage = "Modifier", bit = (int)Key.RightCtrl)]
        [InputControl(name = "leftMeta", displayName = "Left System", layout = "Key", usage = "Modifier", bit = (int)Key.LeftMeta, aliases = new[] { "LeftWindows", "LeftApple", "LeftCommand" })]
        [InputControl(name = "rightMeta", displayName = "Right System", layout = "Key", usage = "Modifier", bit = (int)Key.RightMeta, aliases = new[] { "RightWindows", "RightApple", "RightCommand" })]
        [InputControl(name = "contextMenu", displayName = "Context Menu", layout = "Key", usage = "Modifier", bit = (int)Key.ContextMenu)]
        [InputControl(name = "backspace", displayName = "Backspace", layout = "Key", bit = (int)Key.Backspace)]
        [InputControl(name = "pageDown", displayName = "Page Down", layout = "Key", bit = (int)Key.PageDown)]
        [InputControl(name = "pageUp", displayName = "Page Up", layout = "Key", bit = (int)Key.PageUp)]
        [InputControl(name = "home", displayName = "Home", layout = "Key", bit = (int)Key.Home)]
        [InputControl(name = "end", displayName = "End", layout = "Key", bit = (int)Key.End)]
        [InputControl(name = "insert", displayName = "Insert", layout = "Key", bit = (int)Key.Insert)]
        [InputControl(name = "delete", displayName = "Delete", layout = "Key", bit = (int)Key.Delete)]
        [InputControl(name = "capsLock", displayName = "Caps Lock", layout = "Key", bit = (int)Key.CapsLock)]
        [InputControl(name = "numLock", displayName = "Num Lock", layout = "Key", bit = (int)Key.NumLock)]
        [InputControl(name = "printScreen", displayName = "Print Screen", layout = "Key", bit = (int)Key.PrintScreen)]
        [InputControl(name = "scrollLock", displayName = "Scroll Lock", layout = "Key", bit = (int)Key.ScrollLock)]
        [InputControl(name = "pause", displayName = "Pause/Break", layout = "Key", bit = (int)Key.Pause)]
        [InputControl(name = "numpadEnter", displayName = "Numpad Enter", layout = "Key", bit = (int)Key.NumpadEnter)]
        [InputControl(name = "numpadDivide", displayName = "Numpad /", layout = "Key", bit = (int)Key.NumpadDivide)]
        [InputControl(name = "numpadMultiply", displayName = "Numpad *", layout = "Key", bit = (int)Key.NumpadMultiply)]
        [InputControl(name = "numpadPlus", displayName = "Numpad +", layout = "Key", bit = (int)Key.NumpadPlus)]
        [InputControl(name = "numpadMinus", displayName = "Numpad -", layout = "Key", bit = (int)Key.NumpadMinus)]
        [InputControl(name = "numpadPeriod", displayName = "Numpad .", layout = "Key", bit = (int)Key.NumpadPeriod)]
        [InputControl(name = "numpadEquals", displayName = "Numpad =", layout = "Key", bit = (int)Key.NumpadEquals)]
        [InputControl(name = "numpad1", displayName = "Numpad 1", layout = "Key", bit = (int)Key.Numpad1)]
        [InputControl(name = "numpad2", displayName = "Numpad 2", layout = "Key", bit = (int)Key.Numpad2)]
        [InputControl(name = "numpad3", displayName = "Numpad 3", layout = "Key", bit = (int)Key.Numpad3)]
        [InputControl(name = "numpad4", displayName = "Numpad 4", layout = "Key", bit = (int)Key.Numpad4)]
        [InputControl(name = "numpad5", displayName = "Numpad 5", layout = "Key", bit = (int)Key.Numpad5)]
        [InputControl(name = "numpad6", displayName = "Numpad 6", layout = "Key", bit = (int)Key.Numpad6)]
        [InputControl(name = "numpad7", displayName = "Numpad 7", layout = "Key", bit = (int)Key.Numpad7)]
        [InputControl(name = "numpad8", displayName = "Numpad 8", layout = "Key", bit = (int)Key.Numpad8)]
        [InputControl(name = "numpad9", displayName = "Numpad 9", layout = "Key", bit = (int)Key.Numpad9)]
        [InputControl(name = "numpad0", displayName = "Numpad 0", layout = "Key", bit = (int)Key.Numpad0)]
        [InputControl(name = "f1", displayName = "F1", layout = "Key", bit = (int)Key.F1)]
        [InputControl(name = "f2", displayName = "F2", layout = "Key", bit = (int)Key.F2)]
        [InputControl(name = "f3", displayName = "F3", layout = "Key", bit = (int)Key.F3)]
        [InputControl(name = "f4", displayName = "F4", layout = "Key", bit = (int)Key.F4)]
        [InputControl(name = "f5", displayName = "F5", layout = "Key", bit = (int)Key.F5)]
        [InputControl(name = "f6", displayName = "F6", layout = "Key", bit = (int)Key.F6)]
        [InputControl(name = "f7", displayName = "F7", layout = "Key", bit = (int)Key.F7)]
        [InputControl(name = "f8", displayName = "F8", layout = "Key", bit = (int)Key.F8)]
        [InputControl(name = "f9", displayName = "F9", layout = "Key", bit = (int)Key.F9)]
        [InputControl(name = "f10", displayName = "F10", layout = "Key", bit = (int)Key.F10)]
        [InputControl(name = "f11", displayName = "F11", layout = "Key", bit = (int)Key.F11)]
        [InputControl(name = "f12", displayName = "F12", layout = "Key", bit = (int)Key.F12)]
        [InputControl(name = "OEM1", layout = "Key", bit = (int)Key.OEM1)]
        [InputControl(name = "OEM2", layout = "Key", bit = (int)Key.OEM2)]
        [InputControl(name = "OEM3", layout = "Key", bit = (int)Key.OEM3)]
        [InputControl(name = "OEM4", layout = "Key", bit = (int)Key.OEM4)]
        [InputControl(name = "OEM5", layout = "Key", bit = (int)Key.OEM5)]
        [InputControl(name = "IMESelected", layout = "Button", bit = (int)Key.IMESelected, synthetic = true)]
        public fixed byte keys[kSizeInBytes];

        public KeyboardState(params Key[] pressedKeys)
        {
            if (pressedKeys == null)
                throw new ArgumentNullException(nameof(pressedKeys));

            fixed(byte* keysPtr = keys)
            {
                UnsafeUtility.MemClear(keysPtr, kSizeInBytes);
                for (var i = 0; i < pressedKeys.Length; ++i)
                {
                    MemoryHelpers.WriteSingleBit(keysPtr, (uint)pressedKeys[i], true);
                }
            }
        }

        public FourCC format => kFormat;
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
        /// <summary>
        /// Invalid key. Does not represent a key on the keyboard and is only used to have a
        /// default for the Key enumeration not represent any specific key.
        /// </summary>
        None,

        // Printable keys.
        /// <summary>
        /// The <see cref="Keyboard.spaceKey"/>.
        /// </summary>
        Space,

        /// <summary>
        /// The <see cref="Keyboard.enterKey"/>.
        /// </summary>
        Enter,

        /// <summary>
        /// The <see cref="Keyboard.tabKey"/>.
        /// </summary>
        Tab,

        /// <summary>
        /// The <see cref="Keyboard.backquoteKey"/>.
        /// </summary>
        Backquote,

        /// <summary>
        /// The <see cref="Keyboard.quoteKey"/>.
        /// </summary>
        Quote,

        /// <summary>
        /// The <see cref="Keyboard.semicolonKey"/>.
        /// </summary>
        Semicolon,

        /// <summary>
        /// The <see cref="Keyboard.commaKey"/>.
        /// </summary>
        Comma,

        /// <summary>
        /// The <see cref="Keyboard.periodKey"/>.
        /// </summary>
        Period,

        /// <summary>
        /// The <see cref="Keyboard.slashKey"/>.
        /// </summary>
        Slash,

        /// <summary>
        /// The <see cref="Keyboard.backslashKey"/>.
        /// </summary>
        Backslash,

        /// <summary>
        /// The <see cref="Keyboard.leftBracketKey"/>.
        /// </summary>
        LeftBracket,

        /// <summary>
        /// The <see cref="Keyboard.rightBracketKey"/>.
        /// </summary>
        RightBracket,

        /// <summary>
        /// The <see cref="Keyboard.minusKey"/>.
        /// </summary>
        Minus,

        /// <summary>
        /// The <see cref="Keyboard.equalsKey"/>.
        /// </summary>
        Equals,

        /// <summary>
        /// The <see cref="Keyboard.aKey"/>.
        /// </summary>
        A,

        /// <summary>
        /// The <see cref="Keyboard.bKey"/>.
        /// </summary>
        B,

        /// <summary>
        /// The <see cref="Keyboard.cKey"/>.
        /// </summary>
        C,

        /// <summary>
        /// The <see cref="Keyboard.dKey"/>.
        /// </summary>
        D,

        /// <summary>
        /// The <see cref="Keyboard.eKey"/>.
        /// </summary>
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
    /// Represents a standard, physical PC-type keyboard.
    /// </summary>
    /// <remarks>
    /// Keyboards allow for both individual button input as well as text input. To receive button
    /// input, use the individual <see cref="KeyControl"/>-type controls present on the keyboard.
    /// For example, <see cref="aKey"/>. To receive text input, use the <see cref="onTextInput"/>
    /// callback.
    ///
    /// The naming/identification of keys is agnostic to keyboard layouts. This means that <see cref="aKey"/>,
    /// for example, will always be the key to the right of <see cref="capsLockKey"/> regardless of where
    /// the current keyboard language layout puts the "a" character. This also means that having a
    /// binding to <c>"&lt;Keyboard&gt;/a"</c> on an <see cref="InputAction"/>, for example, will
    /// bind to the same key regardless of locale -- an important feature, for example, for getting
    /// stable WASD bindings.
    ///
    /// To find what text character (if any) is produced by a key, you can use the key's <see
    /// cref="InputControl.displayName"/> property. This can also be used in bindings.
    /// <c>"&lt;Keyboard&gt;/#(a)"</c>, for example, will bind to the key that produces the "a"
    /// character according to the currently active keyboard layout.
    ///
    /// To find out which keyboard layout is currently active, you can use the <see cref="keyboardLayout"/>
    /// property. Note that keyboard layout names are platform-dependent.
    /// </remarks>
    [InputControlLayout(stateType = typeof(KeyboardState), isGenericTypeOfDevice = true)]
    [UnityEngine.Scripting.Preserve]
    public class Keyboard : InputDevice, ITextInputReceiver
    {
        /// <summary>
        /// Total number of key controls on a keyboard, i.e. the number of controls
        /// in <see cref="allKeys"/>.
        /// </summary>
        /// <value>Total number of key controls.</value>
        public const int KeyCount = (int)Key.OEM5;

        /// <summary>
        /// Event that is fired for every single character entered on the keyboard.
        /// </summary>
        /// <remarks>
        /// <example>
        /// <code>
        /// Keyboard.current.onTextInput +=
        ///     ch =>
        ///     {
        ///         TODO
        ///     }
        /// </code>
        /// </example>
        /// </remarks>
        public event Action<char> onTextInput
        {
            add => m_TextInputListeners.Append(value);
            remove => m_TextInputListeners.Remove(value);
        }

        /// <summary>
        /// An event that is fired to get IME composition strings.  Fired once for every change,
        /// sends the entire string to date, and sends a blank string whenever a composition is submitted or reset.
        /// </summary>
        /// <remarks>
        /// Some languages use complex input methods which involve opening windows to insert characters.
        /// Typically, this is not desirable while playing a game, as games may just interpret key strokes as game input, not as text.
        ///
        /// See <see cref="Keyboard.SetIMEEnabled"/> for turning IME on/off
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
        /// See <see cref="Keyboard.SetIMECursorPosition"/>, <see cref="Keyboard.onIMECompositionChange"/>,
        /// <see cref="Keyboard.imeSelected"/> for more IME settings and data.
        /// </remarks>
        public void SetIMEEnabled(bool enabled)
        {
            EnableIMECompositionCommand command = EnableIMECompositionCommand.Create(enabled);
            ExecuteCommand(ref command);
        }

        /// <summary>
        /// Sets the cursor position for IME composition dialogs.  Units are from the upper left, in pixels, moving down and to the right.
        /// </summary>
        /// <remarks>
        /// Some languages use complex input methods which involve opening windows to insert characters.
        /// Typically, this is not desirable while playing a game, as games may just interpret key strokes as game input, not as text.
        ///
        /// See <see cref="Keyboard.SetIMEEnabled"/> for turning IME on/off
        /// </remarks>
        public void SetIMECursorPosition(Vector2 position)
        {
            SetIMECursorPositionCommand command = SetIMECursorPositionCommand.Create(position);
            ExecuteCommand(ref command);
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
        /// <value>Control representing the synthetic "anyKey".</value>
        public AnyKeyControl anyKey { get; private set; }

        /// <summary>
        /// The space bar key.
        /// </summary>
        /// <value>Control representing the space bar key.</value>
        public KeyControl spaceKey => this[Key.Space];

        /// <summary>
        /// The enter/return key in the main key block.
        /// </summary>
        /// <value>Control representing the enter key.</value>
        /// <remarks>
        /// This key is distinct from the enter key on the numpad which is <see cref="numpadEnterKey"/>.
        /// </remarks>
        public KeyControl enterKey => this[Key.Enter];

        /// <summary>
        /// The tab key.
        /// </summary>
        /// <value>Control representing the tab key.</value>
        public KeyControl tabKey => this[Key.Tab];

        /// <summary>
        /// The ` key. The leftmost key in the row of digits. Directly above <see cref="tabKey"/>.
        /// </summary>
        /// <value>Control representing the backtick/quote key.</value>
        public KeyControl backquoteKey => this[Key.Backquote];

        /// <summary>
        /// The ' key. The key immediately to the left of <see cref="enterKey"/>.
        /// </summary>
        /// <value>Control representing the quote key.</value>
        public KeyControl quoteKey => this[Key.Quote];

        /// <summary>
        /// The ';' key. The key immediately to the left of <see cref="quoteKey"/>.
        /// </summary>
        /// <value>Control representing the semicolon key.</value>
        public KeyControl semicolonKey => this[Key.Semicolon];

        /// <summary>
        /// The ',' key. Third key to the left of <see cref="rightShiftKey"/>.
        /// </summary>
        /// <value>Control representing the comma key.</value>
        public KeyControl commaKey => this[Key.Comma];

        /// <summary>
        /// The '.' key. Second key to the left of <see cref="rightShiftKey"/>.
        /// </summary>
        /// <value>Control representing the period key.</value>
        public KeyControl periodKey => this[Key.Period];

        /// <summary>
        /// The '/' key. The key immediately to the left of <see cref="rightShiftKey"/>.
        /// </summary>
        /// <value>Control representing the forward slash key.</value>
        public KeyControl slashKey => this[Key.Slash];

        /// <summary>
        /// The '\' key. The key immediately to the right of <see cref="rightBracketKey"/> and
        /// next to or above <see cref="enterKey"/>.
        /// </summary>
        /// <value>Control representing the backslash key.</value>
        public KeyControl backslashKey => this[Key.Backslash];

        /// <summary>
        /// The '[' key. The key immediately to the left of <see cref="rightBracketKey"/>.
        /// </summary>
        /// <value>Control representing the left bracket key.</value>
        public KeyControl leftBracketKey => this[Key.LeftBracket];

        /// <summary>
        /// The ']' key. The key in-between <see cref="leftBracketKey"/> to the left and
        /// <see cref="backslashKey"/> to the right.
        /// </summary>
        /// <value>Control representing the right bracket key.</value>
        public KeyControl rightBracketKey => this[Key.RightBracket];

        /// <summary>
        /// The '-' key. The second key to the left of <see cref="backspaceKey"/>.
        /// </summary>
        /// <value>Control representing the minus key.</value>
        public KeyControl minusKey => this[Key.Minus];

        /// <summary>
        /// The '=' key in the main key block. The key in-between <see cref="minusKey"/> to the left
        /// and <see cref="backspaceKey"/> to the right.
        /// </summary>
        /// <value>Control representing the equals key.</value>
        public KeyControl equalsKey => this[Key.Equals];

        /// <summary>
        /// The 'a' key. The key immediately to the right of <see cref="capsLockKey"/>.
        /// </summary>
        /// <value>Control representing the a key.</value>
        public KeyControl aKey => this[Key.A];

        /// <summary>
        /// The 'b' key. The key in-between the <see cref="vKey"/> to the left and the <see cref="nKey"/>
        /// to the right in the bottom-most row of alphabetic characters.
        /// </summary>
        /// <value>Control representing the b key.</value>
        public KeyControl bKey => this[Key.B];

        /// <summary>
        /// The 'c' key. The key in-between the <see cref="xKey"/> to the left and the <see cref="vKey"/>
        /// to the right in the bottom-most row of alphabetic characters.
        /// </summary>
        /// <value>Control representing the c key.</value>
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
        /// True when IME composition is enabled.  Requires <see cref="Keyboard.SetIMEEnabled"/> to be called to enable IME, and the user to enable it at the OS level.
        /// </summary>
        /// <remarks>
        ///
        /// Some languages use complex input methods which involve opening windows to insert characters.
        /// Typically, this is not desirable while playing a game, as games may just interpret key strokes as game input, not as text.
        ///
        /// See <see cref="Keyboard.SetIMEEnabled"/> for turning IME on/off
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

        /// <summary>
        /// List of all key controls on the keyboard.
        /// </summary>
        public ReadOnlyArray<KeyControl> allKeys => new ReadOnlyArray<KeyControl>(m_Keys);

        public static Keyboard current { get; private set; }

        /// <summary>
        /// Make the keyboard the current keyboard (i.e. <see cref="current"/>).
        /// </summary>
        /// <remarks>
        /// A keyboard will automatically be made current when receiving input events.
        /// </remarks>
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

        /// <inheritdoc/>
        protected override void FinishSetup()
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
                m_Keys[i] = GetChildControl<KeyControl>(keyStrings[i]);

                ////REVIEW: Ideally, we'd have a way to do this through layouts; this way nested key controls could work, too,
                ////        and it just seems somewhat dirty to jam the data into the control here
                m_Keys[i].keyCode = (Key)(i + 1);
            }
            Debug.Assert(keyStrings[(int)Key.OEM5 - 1] == "oem5",
                "keyString array layout doe not match Key enum layout");
            anyKey = GetChildControl<AnyKeyControl>("anyKey");
            imeSelected = GetChildControl<ButtonControl>("IMESelected");

            base.FinishSetup();
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

        private InlinedArray<Action<char>> m_TextInputListeners;
        private string m_KeyboardLayoutName;

        private InlinedArray<Action<IMECompositionString>> m_ImeCompositionListeners;
    }
}
