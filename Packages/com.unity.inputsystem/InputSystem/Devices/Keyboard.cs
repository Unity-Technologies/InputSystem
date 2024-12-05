using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;

////FIXME: display names for keys should be localized key names, not just printable characters (e.g. "Space" should be called "Leertaste")

////TODO: usages on modifiers so they can be identified regardless of platform conventions

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Default state layout for keyboards.
    /// </summary>
    /// <remarks>
    /// Can be used to update the state of <seealso cref="Keyboard"/> devices.
    ///
    /// <example>
    /// <code>
    /// using UnityEngine;
    /// using UnityEngine.InputSystem;
    /// using UnityEngine.InputSystem.LowLevel;
    ///
    /// public class Example : MonoBehaviour
    /// {
    ///     void Start()
    ///     {
    ///         // Send input event with A key pressed on keyboard.
    ///         InputSystem.QueueStateEvent(Keyboard.current,
    ///             new KeyboardState(Key.A));
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="Keyboard"/>
    // NOTE: This layout has to match the KeyboardInputState layout used in native!
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KeyboardState : IInputStateTypeInfo
    {
        /// <summary>
        /// Memory format tag for KeyboardState. Returns "KEYS".
        /// </summary>
        /// <seealso cref="InputStateBlock.format"/>
        public static FourCC Format => new FourCC('K', 'E', 'Y', 'S');

        private const int kSizeInBits = Keyboard.KeyCount;
        internal const int kSizeInBytes = (kSizeInBits + 7) / 8;

        [InputControl(name = "anyKey", displayName = "Any Key", layout = "AnyKey", sizeInBits = kSizeInBits - 1, synthetic = true)] // Exclude IMESelected.
        [InputControl(name = "escape", displayName = "Escape", layout = "Key", usages = new[] {"Back", "Cancel"}, bit = (int)Key.Escape)]
        [InputControl(name = "space", displayName = "Space", layout = "Key", bit = (int)Key.Space)]
        [InputControl(name = "enter", displayName = "Enter", layout = "Key", usage = "Submit", bit = (int)Key.Enter)]
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
        [InputControl(name = "shift", displayName = "Shift", layout = "DiscreteButton", usage = "Modifier", bit = (int)Key.LeftShift, sizeInBits = 2, synthetic = true, parameters = "minValue=1,maxValue=3,writeMode=1")]
        [InputControl(name = "leftAlt", displayName = "Left Alt", layout = "Key", usage = "Modifier", bit = (int)Key.LeftAlt)]
        [InputControl(name = "rightAlt", displayName = "Right Alt", layout = "Key", usage = "Modifier", bit = (int)Key.RightAlt, alias = "AltGr")]
        [InputControl(name = "alt", displayName = "Alt", layout = "DiscreteButton", usage = "Modifier", bit = (int)Key.LeftAlt, sizeInBits = 2, synthetic = true, parameters = "minValue=1,maxValue=3,writeMode=1")]
        [InputControl(name = "leftCtrl", displayName = "Left Control", layout = "Key", usage = "Modifier", bit = (int)Key.LeftCtrl)]
        [InputControl(name = "rightCtrl", displayName = "Right Control", layout = "Key", usage = "Modifier", bit = (int)Key.RightCtrl)]
        [InputControl(name = "ctrl", displayName = "Control", layout = "DiscreteButton", usage = "Modifier", bit = (int)Key.LeftCtrl, sizeInBits = 2, synthetic = true, parameters = "minValue=1,maxValue=3,writeMode=1")]
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
                    MemoryHelpers.WriteSingleBit(keysPtr, (uint)pressedKeys[i], true);
            }
        }

        public void Set(Key key, bool state)
        {
            fixed(byte* keysPtr = keys)
            MemoryHelpers.WriteSingleBit(keysPtr, (uint)key, state);
        }

        public void Press(Key key)
        {
            Set(key, true);
        }

        public void Release(Key key)
        {
            Set(key, false);
        }

        public FourCC format => Format;
    }
}

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Enumeration of key codes.
    /// </summary>
    /// <remarks>
    /// Named according to the US keyboard layout which is used as a reference layout.
    ///
    /// Note:
    /// Unity input system key codes and input manager key codes are designed with game controls in mind.
    ///
    /// This means the way they are assigned is intended to preserve the location of keys on keyboards,
    /// so that pressing a key in the same location on different keyboards should result in the same action
    /// regardless of what is printed on a key or what current system language is set.
    ///
    /// This means, for example, that <seealso cref="A"/> is always the key to the right of <seealso cref="CapsLock"/>,
    /// regardless of which key (if any) produces the "a" character on the current keyboard layout.
    ///
    /// Unity relies on physical hardware in the keyboards to report same USB HID "usage" for the keys in
    /// the same location.This puts a practical limit on what can be achieved, because different keyboards
    /// might report different data, and this is outside of Unity's control.
    ///
    /// For this reason, you should not use key codes to read text input.
    /// Instead, you should use the <seealso cref="Keyboard.onTextInput"/> callback.
    /// The `onTextInput` callback provides you with the actual text characters which correspond
    /// to the symbols printed on a keyboard, based on the end user's current system language layout.
    ///
    /// To find the text character (if any) generated by a key according to the currently active keyboard
    /// layout, use the <seealso cref="InputControl.displayName"/> property of <seealso cref="KeyControl"/>.
    ///
    /// <example>
    /// <code>
    /// using UnityEngine;
    /// using UnityEngine.InputSystem;
    ///
    /// public class Example : MonoBehaviour
    /// {
    ///     void LookUpTextInputByKey()
    ///     {
    ///         // Look up key by key code.
    ///         var aKey = Keyboard.current[Key.A];
    ///
    ///         // Find out which text is produced by the key.
    ///         Debug.Log($"The '{aKey.keyCode}' key produces '{aKey.displayName}' as text input");
    ///     }
    /// }
    /// </code>
    /// </example>
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

        // ---- Printable keys ----

        /// <summary>
        /// The <seealso cref="Keyboard.spaceKey"/>.
        /// </summary>
        Space,

        /// <summary>
        /// The <seealso cref="Keyboard.enterKey"/>.
        /// </summary>
        Enter,

        /// <summary>
        /// The <seealso cref="Keyboard.tabKey"/>.
        /// </summary>
        Tab,

        /// <summary>
        /// The <seealso cref="Keyboard.backquoteKey"/>.
        /// </summary>
        Backquote,

        /// <summary>
        /// The <seealso cref="Keyboard.quoteKey"/>.
        /// </summary>
        Quote,

        /// <summary>
        /// The <seealso cref="Keyboard.semicolonKey"/>.
        /// </summary>
        Semicolon,

        /// <summary>
        /// The <seealso cref="Keyboard.commaKey"/>.
        /// </summary>
        Comma,

        /// <summary>
        /// The <seealso cref="Keyboard.periodKey"/>.
        /// </summary>
        Period,

        /// <summary>
        /// The <seealso cref="Keyboard.slashKey"/>.
        /// </summary>
        Slash,

        /// <summary>
        /// The <seealso cref="Keyboard.backslashKey"/>.
        /// </summary>
        Backslash,

        /// <summary>
        /// The <seealso cref="Keyboard.leftBracketKey"/>.
        /// </summary>
        LeftBracket,

        /// <summary>
        /// The <seealso cref="Keyboard.rightBracketKey"/>.
        /// </summary>
        RightBracket,

        /// <summary>
        /// The <seealso cref="Keyboard.minusKey"/>.
        /// </summary>
        Minus,

        /// <summary>
        /// The <seealso cref="Keyboard.equalsKey"/>.
        /// </summary>
        Equals,

        /// <summary>
        /// The <seealso cref="Keyboard.aKey"/>.
        /// </summary>
        A,

        /// <summary>
        /// The <seealso cref="Keyboard.bKey"/>.
        /// </summary>
        B,

        /// <summary>
        /// The <seealso cref="Keyboard.cKey"/>.
        /// </summary>
        C,

        /// <summary>
        /// The <seealso cref="Keyboard.dKey"/>.
        /// </summary>
        D,

        /// <summary>
        /// The <seealso cref="Keyboard.eKey"/>.
        /// </summary>
        E,

        /// <summary>
        /// The <seealso cref="Keyboard.fKey"/>.
        /// </summary>
        F,

        /// <summary>
        /// The <seealso cref="Keyboard.gKey"/>.
        /// </summary>
        G,

        /// <summary>
        /// The <seealso cref="Keyboard.hKey"/>.
        /// </summary>
        H,

        /// <summary>
        /// The <seealso cref="Keyboard.iKey"/>.
        /// </summary>
        I,

        /// <summary>
        /// The <seealso cref="Keyboard.jKey"/>.
        /// </summary>
        J,

        /// <summary>
        /// The <seealso cref="Keyboard.kKey"/>.
        /// </summary>
        K,

        /// <summary>
        /// The <seealso cref="Keyboard.lKey"/>.
        /// </summary>
        L,

        /// <summary>
        /// The <seealso cref="Keyboard.mKey"/>.
        /// </summary>
        M,

        /// <summary>
        /// The <seealso cref="Keyboard.nKey"/>.
        /// </summary>
        N,

        /// <summary>
        /// The <seealso cref="Keyboard.oKey"/>.
        /// </summary>
        O,

        /// <summary>
        /// The <seealso cref="Keyboard.pKey"/>.
        /// </summary>
        P,

        /// <summary>
        /// The <seealso cref="Keyboard.qKey"/>.
        /// </summary>
        Q,

        /// <summary>
        /// The <seealso cref="Keyboard.rKey"/>.
        /// </summary>
        R,

        /// <summary>
        /// The <seealso cref="Keyboard.sKey"/>.
        /// </summary>
        S,

        /// <summary>
        /// The <seealso cref="Keyboard.tKey"/>.
        /// </summary>
        T,

        /// <summary>
        /// The <seealso cref="Keyboard.uKey"/>.
        /// </summary>
        U,

        /// <summary>
        /// The <seealso cref="Keyboard.vKey"/>.
        /// </summary>
        V,

        /// <summary>
        /// The <seealso cref="Keyboard.wKey"/>.
        /// </summary>
        W,

        /// <summary>
        /// The <seealso cref="Keyboard.xKey"/>.
        /// </summary>
        X,

        /// <summary>
        /// The <seealso cref="Keyboard.yKey"/>.
        /// </summary>
        Y,

        /// <summary>
        /// The <seealso cref="Keyboard.zKey"/>.
        /// </summary>
        Z,

        /// <summary>
        /// The <seealso cref="Keyboard.digit1Key"/>.
        /// </summary>
        Digit1,

        /// <summary>
        /// The <seealso cref="Keyboard.digit2Key"/>.
        /// </summary>
        Digit2,

        /// <summary>
        /// The <seealso cref="Keyboard.digit3Key"/>.
        /// </summary>
        Digit3,

        /// <summary>
        /// The <seealso cref="Keyboard.digit4Key"/>.
        /// </summary>
        Digit4,

        /// <summary>
        /// The <seealso cref="Keyboard.digit5Key"/>.
        /// </summary>
        Digit5,

        /// <summary>
        /// The <seealso cref="Keyboard.digit6Key"/>.
        /// </summary>
        Digit6,

        /// <summary>
        /// The <seealso cref="Keyboard.digit7Key"/>.
        /// </summary>
        Digit7,

        /// <summary>
        /// The <seealso cref="Keyboard.digit8Key"/>.
        /// </summary>
        Digit8,

        /// <summary>
        /// The <seealso cref="Keyboard.digit9Key"/>.
        /// </summary>
        Digit9,

        /// <summary>
        /// The <seealso cref="Keyboard.digit0Key"/>.
        /// </summary>
        Digit0,

        // ---- Non-printable keys ----

        // NOTE: The left&right variants for shift, ctrl, and alt must be next to each other.

        /// <summary>
        /// The <seealso cref="Keyboard.leftShiftKey"/>.
        /// </summary>
        LeftShift,

        /// <summary>
        /// The <seealso cref="Keyboard.rightShiftKey"/>.
        /// </summary>
        RightShift,

        /// <summary>
        /// The <seealso cref="Keyboard.leftAltKey"/>.
        /// </summary>
        LeftAlt,

        /// <summary>
        /// The <seealso cref="Keyboard.rightAltKey"/>.
        /// </summary>
        RightAlt,

        /// <summary>
        /// Same as <seealso cref="RightAlt"/>.
        /// </summary>
        AltGr = RightAlt,

        /// <summary>
        /// The <seealso cref="Keyboard.leftCtrlKey"/>.
        /// </summary>
        LeftCtrl,

        /// <summary>
        /// The <seealso cref="Keyboard.rightCtrlKey"/>.
        /// </summary>
        RightCtrl,

        /// <summary>
        /// The <seealso cref="Keyboard.leftMetaKey"/>.
        /// </summary>
        LeftMeta,

        /// <summary>
        /// The <seealso cref="Keyboard.rightMetaKey"/>.
        /// </summary>
        RightMeta,

        /// <summary>
        /// Same as <seealso cref="LeftMeta"/>.
        /// </summary>
        LeftWindows = LeftMeta,

        /// <summary>
        /// Same as <seealso cref="RightMeta"/>.
        /// </summary>
        RightWindows = RightMeta,

        /// <summary>
        /// Same as <seealso cref="LeftMeta"/>.
        /// </summary>
        LeftApple = LeftMeta,

        /// <summary>
        /// Same as <seealso cref="RightMeta"/>.
        /// </summary>
        RightApple = RightMeta,

        /// <summary>
        /// Same as <seealso cref="LeftMeta"/>.
        /// </summary>
        LeftCommand = LeftMeta,

        /// <summary>
        /// Same as <seealso cref="RightMeta"/>.
        /// </summary>
        RightCommand = RightMeta,

        /// <summary>
        /// The <seealso cref="Keyboard.contextMenuKey"/>.
        /// </summary>
        ContextMenu,

        /// <summary>
        /// The <seealso cref="Keyboard.escapeKey"/>.
        /// </summary>
        Escape,

        /// <summary>
        /// The <seealso cref="Keyboard.leftArrowKey"/>.
        /// </summary>
        LeftArrow,

        /// <summary>
        /// The <seealso cref="Keyboard.rightArrowKey"/>.
        /// </summary>
        RightArrow,

        /// <summary>
        /// The <seealso cref="Keyboard.upArrowKey"/>.
        /// </summary>
        UpArrow,

        /// <summary>
        /// The <seealso cref="Keyboard.downArrowKey"/>.
        /// </summary>
        DownArrow,

        /// <summary>
        /// The <seealso cref="Keyboard.backspaceKey"/>.
        /// </summary>
        Backspace,

        /// <summary>
        /// The <seealso cref="Keyboard.pageDownKey"/>.
        /// </summary>
        PageDown,

        /// <summary>
        /// The <seealso cref="Keyboard.pageUpKey"/>.
        /// </summary>
        PageUp,

        /// <summary>
        /// The <seealso cref="Keyboard.homeKey"/>.
        /// </summary>
        Home,

        /// <summary>
        /// The <seealso cref="Keyboard.endKey"/>.
        /// </summary>
        End,

        /// <summary>
        /// The <seealso cref="Keyboard.insertKey"/>.
        /// </summary>
        Insert,

        /// <summary>
        /// The <seealso cref="Keyboard.deleteKey"/>.
        /// </summary>
        Delete,

        /// <summary>
        /// The <seealso cref="Keyboard.capsLockKey"/>.
        /// </summary>
        CapsLock,

        /// <summary>
        /// The <seealso cref="Keyboard.numLockKey"/>.
        /// </summary>
        NumLock,

        /// <summary>
        /// The <seealso cref="Keyboard.printScreenKey"/>.
        /// </summary>
        PrintScreen,

        /// <summary>
        /// The <seealso cref="Keyboard.scrollLockKey"/>.
        /// </summary>
        ScrollLock,

        /// <summary>
        /// The <seealso cref="Keyboard.pauseKey"/>.
        /// </summary>
        Pause,

        // ---- Numpad ----
        // NOTE: Numpad layout follows the 18-key numpad layout. Some PC keyboards
        //       have a 17-key numpad layout where the plus key is an elongated key
        //       like the numpad enter key. Be aware that in those layouts the positions
        //       of some of the operator keys are also different. However, we stay
        //       layout neutral here, too, and always use the 18-key blueprint.

        /// <summary>
        /// The <seealso cref="Keyboard.numpadEnterKey"/>.
        /// </summary>
        NumpadEnter,

        /// <summary>
        /// The <seealso cref="Keyboard.numpadDivideKey"/>.
        /// </summary>
        NumpadDivide,

        /// <summary>
        /// The <seealso cref="Keyboard.numpadMultiplyKey"/>.
        /// </summary>
        NumpadMultiply,

        /// <summary>
        /// The <seealso cref="Keyboard.numpadPlusKey"/>.
        /// </summary>
        NumpadPlus,

        /// <summary>
        /// The <seealso cref="Keyboard.numpadMinusKey"/>.
        /// </summary>
        NumpadMinus,

        /// <summary>
        /// The <seealso cref="Keyboard.numpadPeriodKey"/>.
        /// </summary>
        NumpadPeriod,

        /// <summary>
        /// The <seealso cref="Keyboard.numpadEqualsKey"/>.
        /// </summary>
        NumpadEquals,

        /// <summary>
        /// The <seealso cref="Keyboard.numpad0Key"/>.
        /// </summary>
        Numpad0,

        /// <summary>
        /// The <seealso cref="Keyboard.numpad1Key"/>.
        /// </summary>
        Numpad1,

        /// <summary>
        /// The <seealso cref="Keyboard.numpad2Key"/>.
        /// </summary>
        Numpad2,

        /// <summary>
        /// The <seealso cref="Keyboard.numpad3Key"/>.
        /// </summary>
        Numpad3,

        /// <summary>
        /// The <seealso cref="Keyboard.numpad4Key"/>.
        /// </summary>
        Numpad4,

        /// <summary>
        /// The <seealso cref="Keyboard.numpad5Key"/>.
        /// </summary>
        Numpad5,

        /// <summary>
        /// The <seealso cref="Keyboard.numpad6Key"/>.
        /// </summary>
        Numpad6,

        /// <summary>
        /// The <seealso cref="Keyboard.numpad7Key"/>.
        /// </summary>
        Numpad7,

        /// <summary>
        /// The <seealso cref="Keyboard.numpad8Key"/>.
        /// </summary>
        Numpad8,

        /// <summary>
        /// The <seealso cref="Keyboard.numpad9Key"/>.
        /// </summary>
        Numpad9,

        /// <summary>
        /// The <seealso cref="Keyboard.f1Key"/>.
        /// </summary>
        F1,

        /// <summary>
        /// The <seealso cref="Keyboard.f2Key"/>.
        /// </summary>
        F2,

        /// <summary>
        /// The <seealso cref="Keyboard.f3Key"/>.
        /// </summary>
        F3,

        /// <summary>
        /// The <seealso cref="Keyboard.f4Key"/>.
        /// </summary>
        F4,

        /// <summary>
        /// The <seealso cref="Keyboard.f5Key"/>.
        /// </summary>
        F5,

        /// <summary>
        /// The <seealso cref="Keyboard.f6Key"/>.
        /// </summary>
        F6,

        /// <summary>
        /// The <seealso cref="Keyboard.f7Key"/>.
        /// </summary>
        F7,

        /// <summary>
        /// The <seealso cref="Keyboard.f8Key"/>.
        /// </summary>
        F8,

        /// <summary>
        /// The <seealso cref="Keyboard.f9Key"/>.
        /// </summary>
        F9,

        /// <summary>
        /// The <seealso cref="Keyboard.f10Key"/>.
        /// </summary>
        F10,

        /// <summary>
        /// The <seealso cref="Keyboard.f11Key"/>.
        /// </summary>
        F11,

        /// <summary>
        /// The <seealso cref="Keyboard.f12Key"/>.
        /// </summary>
        F12,

        // Extra keys that a keyboard may have. We make no guarantees about where
        // they end up on the keyboard (if they are present).

        /// <summary>
        /// The <seealso cref="Keyboard.oem1Key"/>.
        /// </summary>
        OEM1,

        /// <summary>
        /// The <seealso cref="Keyboard.oem2Key"/>.
        /// </summary>
        OEM2,

        /// <summary>
        /// The <seealso cref="Keyboard.oem3Key"/>.
        /// </summary>
        OEM3,

        /// <summary>
        /// The <seealso cref="Keyboard.oem4Key"/>.
        /// </summary>
        OEM4,

        /// <summary>
        /// The <seealso cref="Keyboard.oem5Key"/>.
        /// </summary>
        OEM5,

        ////FIXME: This should never have been a Key but rather just an extra button or state on keyboard
        // Not exactly a key, but binary data sent by the Keyboard to say if IME is being used.
        IMESelected
    }

    /// <summary>
    /// Represents a standard, physical PC-type keyboard.
    /// </summary>
    /// <remarks>
    /// Keyboards allow for both individual button input as well as text input. To receive button
    /// input, use the individual <seealso cref="KeyControl"/>-type controls present on the keyboard.
    /// For example, <seealso cref="aKey"/>. To receive text input, use the <seealso cref="onTextInput"/>
    /// callback.
    ///
    /// The naming/identification of keys is agnostic to keyboard layouts. This means that <seealso cref="aKey"/>,
    /// for example, will always be the key to the right of <seealso cref="capsLockKey"/> regardless of where
    /// the current keyboard language layout puts the "a" character. This also means that having a
    /// binding to <c>"&lt;Keyboard&gt;/a"</c> on an <seealso cref="InputAction"/>, for example, will
    /// bind to the same key regardless of locale -- an important feature, for example, for getting
    /// stable WASD bindings.
    ///
    /// To find what text character (if any) is produced by a key, you can use the key's <see
    /// cref="InputControl.displayName"/> property. This can also be used in bindings.
    /// <c>"&lt;Keyboard&gt;/#(a)"</c>, for example, will bind to the key that produces the "a"
    /// character according to the currently active keyboard layout.
    ///
    /// To find out which keyboard layout is currently active, you can use the <seealso cref="keyboardLayout"/>
    /// property. Note that keyboard layout names are platform-dependent.
    ///
    /// Note that keyboard devices will always have key controls added for all keys in the
    /// <seealso cref="Key"/> enumeration -- whether they are actually present on the physical
    /// keyboard or not. It is thus not possible to find out this way whether the underlying
    /// keyboard has certain keys or not.
    /// </remarks>
    /// <example>
    /// <code>
    /// using UnityEngine;
    /// using UnityEngine.InputSystem;
    ///
    /// public class InputExample : MonoBehaviour
    /// {
    ///     private string inputText = "";
    ///     void Start()
    ///     {
    ///         Keyboard.current.onTextInput += OnTextInput;
    ///     }
    ///     void Update()
    ///     {
    ///         // Check whether the A key on the current keyboard is pressed.
    ///         if (Keyboard.current.aKey.wasPressedThisFrame)
    ///             Debug.Log("A Key pressed");
    ///     }
    ///     private void OnTextInput(char ch)
    ///     {
    ///         inputText += ch;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="InputDevice"/>
    [InputControlLayout(stateType = typeof(KeyboardState), isGenericTypeOfDevice = true)]
    public class Keyboard : InputDevice, ITextInputReceiver
    {
        /// <summary>
        /// Total number of key controls on a keyboard, i.e. the number of controls
        /// in <seealso cref="allKeys"/>.
        /// </summary>
        /// <remarks>The integer value represents the total number of key controls.</remarks>
        public const int KeyCount = (int)Key.OEM5;

        /// <summary>
        /// Event that is fired for every single character entered on the keyboard.
        /// See <seealso cref="Keyboard.OnTextInput"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
        ///
        /// // Let's say we want to do a typing game. We could define a component
        /// // something along those lines to match the typed input.
        /// public class MatchTextByTyping : MonoBehaviour
        /// {
        ///     public string text
        ///     {
        ///         get => m_Text;
        ///         set
        ///         {
        ///             m_Text = value;
        ///             m_Position = 0;
        ///         }
        ///     }
        ///
        ///     public Action onTextTypedCorrectly { get; set; }
        ///     public Action onTextTypedIncorrectly { get; set; }
        ///
        ///     private int m_Position;
        ///     private string m_Text;
        ///
        ///     protected void OnEnable()
        ///     {
        ///         Keyboard.current.onTextInput += OnTextInput;
        ///     }
        ///
        ///     protected void OnDisable()
        ///     {
        ///         Keyboard.current.onTextInput -= OnTextInput;
        ///     }
        ///
        ///     private void OnTextInput(char ch)
        ///     {
        ///         if (m_Text == null || m_Position >= m_Text.Length)
        ///             return;
        ///
        ///         if (m_Text[m_Position] == ch)
        ///         {
        ///             ++m_Position;
        ///             if (m_Position == m_Text.Length)
        ///                 onTextTypedCorrectly?.Invoke();
        ///         }
        ///         else
        ///         {
        ///             m_Text = null;
        ///             m_Position = 0;
        ///
        ///             onTextTypedIncorrectly?.Invoke();
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public event Action<char> onTextInput
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (!m_TextInputListeners.Contains(value))
                    m_TextInputListeners.Append(value);
            }
            remove => m_TextInputListeners.Remove(value);
        }

        /// <summary>
        /// An event that is fired to get IME composition strings. Fired once for every change containing the entire string to date.
        /// When using an IME, this event can be used to display the composition string while it is being edited.
        /// </summary>
        /// <remarks>
        /// The composition string is held by the <seealso cref="UnityEngine.InputSystem.LowLevel.IMECompositionString"/> struct.
        /// When a composition string is submitted, one or many <seealso cref="Keyboard.OnTextInput"/> events will fire with the submitted characters.
        ///
        /// Some languages use complex input methods which involve opening windows to insert characters.
        /// Typically, this is not desirable while playing a game, as games may just interpret key strokes as game input, not as text.
        ///
        /// Many IMEs cause this event to fire with a blank string when the composition is submitted or reset, however it is best
        /// not to rely on this behaviour since it is IME dependent.
        ///
        /// See <seealso cref="Keyboard.SetIMEEnabled"/> for turning IME on/off
        /// </remarks>
        /// <example>
        /// <para>To subscribe to the onIMECompositionChange event, use the following sample code:</para>
        ///
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
        ///
        /// public class KeyboardUtils : MonoBehaviour
        /// {
        ///     private string compositionString = "";
        ///
        ///     void Start(){
        ///         Keyboard.current.onIMECompositionChange += composition =>
        ///         {
        ///             compositionString = composition.ToString();
        ///         };
        ///     }
        /// }
        /// </code>
        /// </example>
        public event Action<IMECompositionString> onIMECompositionChange
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (!m_ImeCompositionListeners.Contains(value))
                    m_ImeCompositionListeners.Append(value);
            }
            remove => m_ImeCompositionListeners.Remove(value);
        }

        /// <summary>
        /// Activates/deactivates IME composition while typing.  This decides whether to use the OS supplied IME system or not.
        /// </summary>
        /// <remarks>
        ///
        /// Some languages use complex input methods which involve opening windows to insert characters.
        /// Typically, this is not desirable while playing a game, as games may just interpret key strokes as game input, not as text.
        /// Setting this to On, will enable the OS-level IME system when the user presses keystrokes.
        ///
        /// See <seealso cref="Keyboard.SetIMECursorPosition"/>, <seealso cref="Keyboard.onIMECompositionChange"/>,
        /// <seealso cref="Keyboard.imeSelected"/> for more IME settings and data.
        /// </remarks>
        /// <param name="enabled">
        ///     The new IME composition enabled state. True to enable the IME, false to disable it.
        /// </param>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
        ///
        /// public class KeyboardUtils : MonoBehaviour
        /// {
        ///     private string compositionString = "";
        ///
        ///     void Start(){
        ///         Keyboard.current.SetIMEEnabled(true);
        ///         Keyboard.current.onIMECompositionChange += composition =>
        ///         {
        ///             compositionString = composition.ToString();
        ///         };
        ///     }
        /// }
        /// </code>
        /// </example>
        public void SetIMEEnabled(bool enabled)
        {
            var command = EnableIMECompositionCommand.Create(enabled);
            ExecuteCommand(ref command);
        }

        /// <summary>
        /// Sets the cursor position for IME composition dialogs.  Units are from the upper left, in pixels, moving down and to the right.
        /// </summary>
        /// <remarks>
        /// Some languages use complex input methods which involve opening windows to insert characters.
        /// Typically, this is not desirable while playing a game, as games may just interpret key strokes as game input, not as text.
        ///
        /// See <seealso cref="Keyboard.SetIMEEnabled"/> for turning IME on/off
        /// </remarks>
        /// <param name="position">
        ///     A Vector2 of the IME cursor position to set.
        /// </param>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
        ///
        /// public class KeyboardUtils : MonoBehaviour
        /// {
        ///     private Vector2 cursorPosition;
        ///
        ///     void Update ()
        ///     {
        ///         // Set the IME cursor position to the mouse position
        ///         var x = Mouse.current.position.x.ReadValue();
        ///         var y = Mouse.current.position.y.ReadValue();
        ///         cursorPosition = new Vector2(x, y);
        ///         Keyboard.current.SetIMECursorPosition(cursorPosition);
        ///     }
        /// }
        /// </code>
        /// </example>
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
        /// To determine what a key represents in the current layout, use <seealso cref="InputControl.displayName"/>.
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
        /// <remarks><seealso cref="AnyKeyControl"/> representing the synthetic "anyKey".</remarks>
        public AnyKeyControl anyKey { get; protected set; }

        /// <summary>
        /// The space bar key at the bottom of the keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the space bar key.</remarks>
        public KeyControl spaceKey => this[Key.Space];

        /// <summary>
        /// The enter/return key in the main key block.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing the enter key.
        /// This key is distinct from the enter key on the numpad which is <seealso cref="numpadEnterKey"/>.
        /// </remarks>
        public KeyControl enterKey => this[Key.Enter];

        /// <summary>
        /// The tab key, located on the left side above the <seealso cref="capsLockKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the tab key.</remarks>
        public KeyControl tabKey => this[Key.Tab];

        /// <summary>
        /// The ` key. The leftmost key in the row of digits. Directly above <seealso cref="tabKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the backtick/quote key.</remarks>
        public KeyControl backquoteKey => this[Key.Backquote];

        /// <summary>
        /// The ' key. The key immediately to the left of <seealso cref="enterKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the quote key.</remarks>
        public KeyControl quoteKey => this[Key.Quote];

        /// <summary>
        /// The ';' key. The key immediately to the left of <seealso cref="quoteKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the semicolon key.</remarks>
        public KeyControl semicolonKey => this[Key.Semicolon];

        /// <summary>
        /// The ',' key. Third key to the left of <seealso cref="rightShiftKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the comma key.</remarks>
        public KeyControl commaKey => this[Key.Comma];

        /// <summary>
        /// The '.' key. Second key to the left of <seealso cref="rightShiftKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the period key.</remarks>
        public KeyControl periodKey => this[Key.Period];

        /// <summary>
        /// The '/' key. The key immediately to the left of <seealso cref="rightShiftKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the forward slash key.</remarks>
        public KeyControl slashKey => this[Key.Slash];

        /// <summary>
        /// The '\' key. The key immediately to the right of <seealso cref="rightBracketKey"/> and
        /// next to or above <seealso cref="enterKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the backslash key.</remarks>
        public KeyControl backslashKey => this[Key.Backslash];

        /// <summary>
        /// The '[' key. The key immediately to the left of <seealso cref="rightBracketKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the left bracket key.</remarks>
        public KeyControl leftBracketKey => this[Key.LeftBracket];

        /// <summary>
        /// The ']' key. The key in-between <seealso cref="leftBracketKey"/> to the left and
        /// <seealso cref="backslashKey"/> to the right.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the right bracket key.</remarks>
        public KeyControl rightBracketKey => this[Key.RightBracket];

        /// <summary>
        /// The '-' key. The second key to the left of <seealso cref="backspaceKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the minus key.</remarks>
        public KeyControl minusKey => this[Key.Minus];

        /// <summary>
        /// The '=' key in the main key block. The key in-between <seealso cref="minusKey"/> to the left
        /// and <seealso cref="backspaceKey"/> to the right.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the equals key.</remarks>
        public KeyControl equalsKey => this[Key.Equals];

        /// <summary>
        /// The 'a' key. The key immediately to the right of <seealso cref="capsLockKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the a key.</remarks>
        public KeyControl aKey => this[Key.A];

        /// <summary>
        /// The 'b' key. The key in-between the <seealso cref="vKey"/> to the left and the <seealso cref="nKey"/>
        /// to the right in the bottom-most row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the b key.</remarks>
        public KeyControl bKey => this[Key.B];

        /// <summary>
        /// The 'c' key. The key in-between the <seealso cref="xKey"/> to the left and the <seealso cref="vKey"/>
        /// to the right in the bottom-most row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the c key.</remarks>
        public KeyControl cKey => this[Key.C];

        /// <summary>
        /// The 'd' key. The key in-between the <seealso cref="sKey"/> to the left and the <seealso cref="fKey"/>
        /// to the right in the middle row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the d key.</remarks>
        public KeyControl dKey => this[Key.D];

        /// <summary>
        /// The 'e' key. The key in-between the <seealso cref="wKey"/> to the left and the <seealso cref="rKey"/>
        /// to the right in the topmost row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the e key.</remarks>
        public KeyControl eKey => this[Key.E];

        /// <summary>
        /// The 'f' key. The key in-between the <seealso cref="dKey"/> to the left and the <seealso cref="gKey"/>
        /// to the right in the middle row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the f key.</remarks>
        public KeyControl fKey => this[Key.F];

        /// <summary>
        /// The 'g' key. The key in-between the <seealso cref="fKey"/> to the left and the <seealso cref="hKey"/>
        /// to the right in the middle row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the g key.</remarks>
        public KeyControl gKey => this[Key.G];

        /// <summary>
        /// The 'h' key. The key in-between the <seealso cref="gKey"/> to the left and the <seealso cref="jKey"/>
        /// to the right in the middle row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the h key.</remarks>
        public KeyControl hKey => this[Key.H];

        /// <summary>
        /// The 'i' key. The key in-between the <seealso cref="uKey"/> to the left and the <seealso cref="oKey"/>
        /// to the right in the top row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the i key.</remarks>
        public KeyControl iKey => this[Key.I];

        /// <summary>
        /// The 'j' key. The key in-between the <seealso cref="hKey"/> to the left and the <seealso cref="kKey"/>
        /// to the right in the middle row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the j key.</remarks>
        public KeyControl jKey => this[Key.J];

        /// <summary>
        /// The 'k' key. The key in-between the <seealso cref="jKey"/> to the left and the <seealso cref="lKey"/>
        /// to the right in the middle row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the k key.</remarks>
        public KeyControl kKey => this[Key.K];

        /// <summary>
        /// The 'l' key. The key in-between the <seealso cref="kKey"/> to the left and the <seealso cref="semicolonKey"/>
        /// to the right in the middle row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the l key.</remarks>
        public KeyControl lKey => this[Key.L];

        /// <summary>
        /// The 'm' key. The key in-between the <seealso cref="nKey"/> to the left and the <seealso cref="commaKey"/>
        /// to the right in the bottom row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the m key.</remarks>
        public KeyControl mKey => this[Key.M];

        /// <summary>
        /// The 'n' key. The key in-between the <seealso cref="bKey"/> to the left and the <seealso cref="mKey"/> to
        /// the right in the bottom row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the n key.</remarks>
        public KeyControl nKey => this[Key.N];

        /// <summary>
        /// The 'o' key. The key in-between the <seealso cref="iKey"/> to the left and the <seealso cref="pKey"/> to
        /// the right in the top row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the o key.</remarks>
        public KeyControl oKey => this[Key.O];

        /// <summary>
        /// The 'p' key. The key in-between the <seealso cref="oKey"/> to the left and the <seealso cref="leftBracketKey"/>
        /// to the right in the top row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the p key.</remarks>
        public KeyControl pKey => this[Key.P];

        /// <summary>
        /// The 'q' key. The key in-between the <seealso cref="tabKey"/> to the left and the <seealso cref="wKey"/>
        /// to the right in the top row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the q key.</remarks>
        public KeyControl qKey => this[Key.Q];

        /// <summary>
        /// The 'r' key. The key in-between the <seealso cref="eKey"/> to the left and the <seealso cref="tKey"/>
        /// to the right in the top row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the r key.</remarks>
        public KeyControl rKey => this[Key.R];

        /// <summary>
        /// The 's' key. The key in-between the <seealso cref="aKey"/> to the left and the <seealso cref="dKey"/>
        /// to the right in the middle row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the s key.</remarks>
        public KeyControl sKey => this[Key.S];

        /// <summary>
        /// The 't' key. The key in-between the <seealso cref="rKey"/> to the left and the <seealso cref="yKey"/>
        /// to the right in the top row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the t key.</remarks>
        public KeyControl tKey => this[Key.T];

        /// <summary>
        /// The 'u' key. The key in-between the <seealso cref="yKey"/> to the left and the <seealso cref="iKey"/>
        /// to the right in the top row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the u key.</remarks>
        public KeyControl uKey => this[Key.U];

        /// <summary>
        /// The 'v' key. The key in-between the <seealso cref="cKey"/> to the left and the <seealso cref="bKey"/>
        /// to the right in the bottom row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the v key.</remarks>
        public KeyControl vKey => this[Key.V];

        /// <summary>
        /// The 'w' key. The key in-between the <seealso cref="qKey"/> to the left and the <seealso cref="eKey"/>
        /// to the right in the top row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the w key.</remarks>
        public KeyControl wKey => this[Key.W];

        /// <summary>
        /// The 'x' key. The key in-between the <seealso cref="zKey"/> to the left and the <seealso cref="cKey"/>
        /// to the right in the bottom row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the x key.</remarks>
        public KeyControl xKey => this[Key.X];

        /// <summary>
        /// The 'y' key. The key in-between the <seealso cref="tKey"/> to the left and the <seealso cref="uKey"/>
        /// to the right in the top row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the y key.</remarks>
        public KeyControl yKey => this[Key.Y];

        /// <summary>
        /// The 'z' key. The key in-between the <seealso cref="leftShiftKey"/> to the left and the <seealso cref="xKey"/>
        /// to the right in the bottom row of alphabetic characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the z key.</remarks>
        public KeyControl zKey => this[Key.Z];

        /// <summary>
        /// The '1' key. The key in-between the <seealso cref="backquoteKey"/> to the left and the <seealso cref="digit2Key"/>
        /// to the right in the row of digit characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the 1 key.</remarks>
        public KeyControl digit1Key => this[Key.Digit1];

        /// <summary>
        /// The '2' key. The key in-between the <seealso cref="digit1Key"/> to the left and the <seealso cref="digit3Key"/>
        /// to the right in the row of digit characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the 2 key.</remarks>
        public KeyControl digit2Key => this[Key.Digit2];

        /// <summary>
        /// The '3' key. The key in-between the <seealso cref="digit2Key"/> to the left and the <seealso cref="digit4Key"/>
        /// to the right in the row of digit characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the 3 key.</remarks>
        public KeyControl digit3Key => this[Key.Digit3];

        /// <summary>
        /// The '4' key. The key in-between the <seealso cref="digit3Key"/> to the left and the <seealso cref="digit5Key"/>
        /// to the right in the row of digit characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the 4 key.</remarks>
        public KeyControl digit4Key => this[Key.Digit4];

        /// <summary>
        /// The '5' key. The key in-between the <seealso cref="digit4Key"/> to the left and the <seealso cref="digit6Key"/>
        /// to the right in the row of digit characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the 5 key.</remarks>
        public KeyControl digit5Key => this[Key.Digit5];

        /// <summary>
        /// The '6' key. The key in-between the <seealso cref="digit5Key"/> to the left and the <seealso cref="digit7Key"/>
        /// to the right in the row of digit characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the 6 key.</remarks>
        public KeyControl digit6Key => this[Key.Digit6];

        /// <summary>
        /// The '7' key. The key in-between the <seealso cref="digit6Key"/> to the left and the <seealso cref="digit8Key"/>
        /// to the right in the row of digit characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the 7 key.</remarks>
        public KeyControl digit7Key => this[Key.Digit7];

        /// <summary>
        /// The '8' key. The key in-between the <seealso cref="digit7Key"/> to the left and the <seealso cref="digit9Key"/>
        /// to the right in the row of digit characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the 8 key.</remarks>
        public KeyControl digit8Key => this[Key.Digit8];

        /// <summary>
        /// The '9' key. The key in-between the <seealso cref="digit8Key"/> to the left and the <seealso cref="digit0Key"/>
        /// to the right in the row of digit characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the 9 key.</remarks>
        public KeyControl digit9Key => this[Key.Digit9];

        /// <summary>
        /// The '0' key. The key in-between the <seealso cref="digit9Key"/> to the left and the <seealso cref="minusKey"/>
        /// to the right in the row of digit characters.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the 0 key.</remarks>
        public KeyControl digit0Key => this[Key.Digit0];

        /// <summary>
        /// The shift key on the left side of the keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the left shift key.</remarks>
        public KeyControl leftShiftKey => this[Key.LeftShift];

        /// <summary>
        /// The shift key on the right side of the keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the right shift key.</remarks>
        public KeyControl rightShiftKey => this[Key.RightShift];

        /// <summary>
        /// The alt/option key on the left side of the keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the left alt/option key.</remarks>
        public KeyControl leftAltKey => this[Key.LeftAlt];

        /// <summary>
        /// The alt/option key on the right side of the keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the right alt/option key.</remarks>
        public KeyControl rightAltKey => this[Key.RightAlt];

        /// <summary>
        /// The control/ctrl key on the left side of the keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the left control key.</remarks>
        public KeyControl leftCtrlKey => this[Key.LeftCtrl];

        /// <summary>
        /// The control/ctrl key on the right side of the keyboard.
        /// </summary>
        /// <remarks>
        /// This key is usually not present on Mac laptops.
        /// <seealso cref="KeyControl"/> representing the right control key.
        /// </remarks>
        public KeyControl rightCtrlKey => this[Key.RightCtrl];

        /// <summary>
        /// The system "meta" key (Windows key on PC, Apple/command key on Mac) on the left
        /// side of the keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the left system meta key.</remarks>
        public KeyControl leftMetaKey => this[Key.LeftMeta];

        /// <summary>
        /// The system "meta" key (Windows key on PC, Apple/command key on Mac) on the right
        /// side of the keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the right system meta key.</remarks>
        public KeyControl rightMetaKey => this[Key.RightMeta];

        /// <summary>
        /// Same as <seealso cref="leftMetaKey"/>. Windows system key on left side of keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the left Windows system key.</remarks>
        public KeyControl leftWindowsKey => this[Key.LeftWindows];

        /// <summary>
        /// Same as <seealso cref="rightMetaKey"/>. Windows system key on right side of keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the right Windows system key.</remarks>
        public KeyControl rightWindowsKey => this[Key.RightWindows];

        /// <summary>
        /// Same as <seealso cref="leftMetaKey"/>. Apple/command system key on left side of keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the left Apple/command system key.</remarks>
        public KeyControl leftAppleKey => this[Key.LeftApple];

        /// <summary>
        /// Same as <seealso cref="rightMetaKey"/>. Apple/command system key on right side of keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the right Apple/command system key.</remarks>
        public KeyControl rightAppleKey => this[Key.RightApple];

        /// <summary>
        /// Same as <seealso cref="leftMetaKey"/>. Apple/command system key on left side of keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the left Apple/command system key.</remarks>
        public KeyControl leftCommandKey => this[Key.LeftCommand];

        /// <summary>
        /// Same as <seealso cref="rightMetaKey"/>. Apple/command system key on right side of keyboard.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the right Apple/command system key.</remarks>
        public KeyControl rightCommandKey => this[Key.RightCommand];

        /// <summary>
        /// The context menu key. This key is generally only found on PC keyboards.
        /// If present, it is located in-between the <seealso cref="rightWindowsKey"/> and the
        /// <seealso cref="rightCtrlKey"/>. It brings up the context menu according to the current selection.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the context menu key.</remarks>
        public KeyControl contextMenuKey => this[Key.ContextMenu];

        /// <summary>
        /// The escape key, i.e. the key generally in the top left corner of the keyboard.
        /// Usually to the left of <seealso cref="f1Key"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the escape key.</remarks>
        public KeyControl escapeKey => this[Key.Escape];

        /// <summary>
        /// The left arrow key. Usually in a block by itself and generally to the left
        /// of <seealso cref="downArrowKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the left arrow key.</remarks>
        public KeyControl leftArrowKey => this[Key.LeftArrow];

        /// <summary>
        /// The right arrow key. Usually in a block by itself and generally to the right
        /// of <seealso cref="downArrowKey"/>
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the right arrow key.</remarks>
        public KeyControl rightArrowKey => this[Key.RightArrow];

        /// <summary>
        /// The up arrow key. Usually in a block by itself and generally on top of the
        /// <seealso cref="downArrowKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the up arrow key.</remarks>
        public KeyControl upArrowKey => this[Key.UpArrow];

        /// <summary>
        /// The down arrow key. Usually in a block by itself and generally below the
        /// <seealso cref="upArrowKey"/> and in-between <seealso cref="leftArrowKey"/> to the
        /// left and <seealso cref="rightArrowKey"/> to the right.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the down arrow key.</remarks>
        public KeyControl downArrowKey => this[Key.DownArrow];

        /// <summary>
        /// The backspace key (usually labeled "delete" on Mac). The rightmost key
        /// in the top digit row with <seealso cref="equalsKey"/> to the left.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing the backspace key.
        /// On the Mac, this key may be labeled "delete" which however is a
        /// key different from <seealso cref="deleteKey"/>.
        /// </remarks>
        public KeyControl backspaceKey => this[Key.Backspace];

        /// <summary>
        /// The page down key. Usually in a separate block with <seealso cref="endKey"/>
        /// to the left and <seealso cref="pageUpKey"/> above it.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the page down key.</remarks>
        public KeyControl pageDownKey => this[Key.PageDown];

        /// <summary>
        /// The page up key. Usually in a separate block with <seealso cref="homeKey"/>
        /// to the left and <seealso cref="pageDownKey"/> below it.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the page up key.</remarks>
        public KeyControl pageUpKey => this[Key.PageUp];

        /// <summary>
        /// The 'home' key. Usually in a separate block with <seealso cref="pageUpKey"/>
        /// to the right and <seealso cref="insertKey"/> to the left.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the insert key.</remarks>
        public KeyControl homeKey => this[Key.Home];

        /// <summary>
        /// The 'end' key. Usually in a separate block with <seealso cref="deleteKey"/>
        /// to the left and <seealso cref="pageDownKey"/> to the right.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the end key.</remarks>
        public KeyControl endKey => this[Key.End];

        /// <summary>
        /// The 'insert' key. Usually in a separate block with <seealso cref="homeKey"/>
        /// to its right and <seealso cref="deleteKey"/> sitting below it.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the insert key.</remarks>
        public KeyControl insertKey => this[Key.Insert];

        /// <summary>
        /// The 'delete' key. Usually in a separate block with <seealso cref="endKey"/>
        /// to its right and <seealso cref="insertKey"/> sitting above it.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing the delete key.
        /// On the Mac, the <seealso cref="backspaceKey"/> is also labeled "delete".
        /// However, this is not this key.
        /// </remarks>
        public KeyControl deleteKey => this[Key.Delete];

        /// <summary>
        /// The Caps Lock key. The key below <seealso cref="tabKey"/> and above
        /// <seealso cref="leftShiftKey"/>.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the caps lock key.</remarks>
        public KeyControl capsLockKey => this[Key.CapsLock];

        /// <summary>
        /// The Scroll Lock key. The key in-between the <seealso cref="printScreenKey"/>
        /// to the left and the <seealso cref="pauseKey"/> to the right. May also
        /// be labeled "F14".
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the scroll lock key.</remarks>
        public KeyControl scrollLockKey => this[Key.ScrollLock];

        /// <summary>
        /// The Num Lock key. The key sitting in the top left corner of the
        /// numpad and which usually toggles the numpad between generating
        /// digits and triggering functions like "insert" etc. instead.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the num lock key.</remarks>
        public KeyControl numLockKey => this[Key.NumLock];

        /// <summary>
        /// The Print Screen key. The key sitting in-between <seealso cref="f12Key"/>
        /// to the left and <seealso cref="scrollLockKey"/> to the right. May also
        /// be labeled "F13".
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the print screen key.</remarks>
        public KeyControl printScreenKey => this[Key.PrintScreen];

        /// <summary>
        /// The pause/break key. The key sitting to the left of <seealso cref="scrollLockKey"/>.
        /// May also be labeled "F15".
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the pause/break key.</remarks>
        public KeyControl pauseKey => this[Key.Pause];

        /// <summary>
        /// The enter key on the numpad. The key sitting in the bottom right corner
        /// of the numpad.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the numpad enter key.</remarks>
        public KeyControl numpadEnterKey => this[Key.NumpadEnter];

        /// <summary>
        /// The divide ('/') key on the numpad. The key in-between <seealso cref="numpadEqualsKey"/>
        /// to the left and <seealso cref="numpadMultiplyKey"/> to the right.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing the numpad divide key.
        /// PC keyboards usually have a 17-key numpad layout that differs from the 18-key layout
        /// we use for reference. The 18-key layout is usually found on Mac keyboards. The numpad
        /// divide key usually is the <seealso cref="numpadEqualsKey"/> on PC keyboards.
        /// </remarks>
        public KeyControl numpadDivideKey => this[Key.NumpadDivide];

        /// <summary>
        /// The multiply ('*') key on the numpad. The key in the upper right corner of the numpad
        /// with <seealso cref="numpadDivideKey"/> to the left and <seealso cref="numpadMultiplyKey"/>
        /// below it.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing the numpad multiply key.
        /// PC keyboards usually have a 17-key numpad layout that differs from the 18-key layout
        /// we use for reference. The 18-key layout is usually found on Mac keyboards. The numpad
        /// multiply key usually is the <seealso cref="numpadMinusKey"/> on PC keyboards.
        /// </remarks>
        public KeyControl numpadMultiplyKey => this[Key.NumpadMultiply];

        /// <summary>
        /// The minus ('-') key on the numpad. The key on the right side of the numpad with
        /// <seealso cref="numpadMultiplyKey"/> above it and <seealso cref="numpadPlusKey"/> below it.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing the numpad minus key.
        /// PC keyboards usually have a 17-key numpad layout that differs from the 18-key layout
        /// we use for reference. The 18-key layout is usually found on Mac keyboards. The numpad
        /// minus key is usually <em>not</em> present on PC keyboards. Instead, the 17-key layout
        /// has an elongated <seealso cref="numpadPlusKey"/> that covers the space of two keys.
        /// </remarks>
        public KeyControl numpadMinusKey => this[Key.NumpadMinus];

        /// <summary>
        /// The plus ('+') key on the numpad. The key on the right side of the numpad with
        /// <seealso cref="numpadMinusKey"/> above it and <seealso cref="numpadEnterKey"/> below it.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing the numpad plus key.
        /// PC keyboards usually have a 17-key numpad layout that differs from the 18-key layout
        /// we use for reference. The 18-key layout is usually found on Mac keyboards.
        ///
        /// In particular, the plus key on the numpad is usually an elongated key that covers
        /// the space of two keys. These 17-key numpads do not usually have a <seealso cref="numpadEqualsKey"/>
        /// and the key above the plus key will usually be the numpad minus key.
        ///
        /// However, both on a 17-key and 18-key numpad, the plus key references the same physical key.
        /// </remarks>
        public KeyControl numpadPlusKey => this[Key.NumpadPlus];

        /// <summary>
        /// The period ('.') key on the numpad. The key in-between the <seealso cref="numpadEnterKey"/>
        /// to the right and the <seealso cref="numpad0Key"/> to the left.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing the numpad period key.
        /// This key is the same in 17-key and 18-key numpad layouts.
        /// </remarks>
        public KeyControl numpadPeriodKey => this[Key.NumpadPeriod];

        /// <summary>
        /// The equals ('=') key on the numpad. The key in-between <seealso cref="numLockKey"/> to the left
        /// and <seealso cref="numpadDivideKey"/> to the right in the top row of the numpad.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing the numpad equals key.
        /// PC keyboards usually have a 17-key numpad layout that differs from the 18-key layout
        /// we use for reference. The 18-key layout is usually found on Mac keyboards.
        ///
        /// 17-key numpad layouts do not usually have an equals key. On these PC keyboards, the
        /// equals key is usually the divide key.
        /// </remarks>
        public KeyControl numpadEqualsKey => this[Key.NumpadEquals];

        /// <summary>
        /// The 0 key on the numpad. The key in the bottom left corner of the numpad. Usually
        /// and elongated key.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the numpad 0 key.</remarks>
        public KeyControl numpad0Key => this[Key.Numpad0];

        /// <summary>
        /// The 1 key on the numpad. The key on the left side of the numpad with <seealso cref="numpad0Key"/>
        /// below it and <seealso cref="numpad4Key"/> above it.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the numpad 1 key.</remarks>
        public KeyControl numpad1Key => this[Key.Numpad1];

        /// <summary>
        /// The 2 key on the numpad. The key with the <seealso cref="numpad1Key"/> to its left and
        /// the <seealso cref="numpad3Key"/> to its right.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the numpad 2 key.</remarks>
        public KeyControl numpad2Key => this[Key.Numpad2];

        /// <summary>
        /// The 3 key on the numpad. The key with the <seealso cref="numpad2Key"/> to its left and
        /// the <seealso cref="numpadEnterKey"/> to its right.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the numpad 3 key.</remarks>
        public KeyControl numpad3Key => this[Key.Numpad3];

        /// <summary>
        /// The 4 key on the numpad. The key on the left side of the numpad with the <seealso cref="numpad1Key"/>
        /// below it and the <seealso cref="numpad7Key"/> above it.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the numpad 4 key.</remarks>
        public KeyControl numpad4Key => this[Key.Numpad4];

        /// <summary>
        /// The 5 key on the numpad. The key in-between the <seealso cref="numpad4Key"/> to the left and the
        /// <seealso cref="numpad6Key"/> to the right.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the numpad 5 key.</remarks>
        public KeyControl numpad5Key => this[Key.Numpad5];

        /// <summary>
        /// The 6 key on the numpad. The key in-between the <seealso cref="numpad5Key"/> to the let and
        /// the <seealso cref="numpadPlusKey"/> to the right.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the numpad 6 key.</remarks>
        public KeyControl numpad6Key => this[Key.Numpad6];

        /// <summary>
        /// The 7 key on the numpad. The key on the left side of the numpad with <seealso cref="numpad4Key"/>
        /// below it and <seealso cref="numLockKey"/> above it.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the numpad 7 key.</remarks>
        public KeyControl numpad7Key => this[Key.Numpad7];

        /// <summary>
        /// The 8 key on the numpad. The key in-between the <seealso cref="numpad7Key"/> to the left and the
        /// <seealso cref="numpad9Key"/> to the right.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the numpad 8 key.</remarks>
        public KeyControl numpad8Key => this[Key.Numpad8];

        /// <summary>
        /// The 9 key on the numpad. The key in-between the <seealso cref="numpad8Key"/> to the left and
        /// the <seealso cref="numpadMinusKey"/> to the right (or, on 17-key PC keyboard numpads, the elongated
        /// plus key).
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the numpad 9 key.</remarks>
        public KeyControl numpad9Key => this[Key.Numpad9];

        /// <summary>
        /// The F1 key. The key in-between <seealso cref="escapeKey"/> to the left and <seealso cref="f1Key"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F1 key.</remarks>
        public KeyControl f1Key => this[Key.F1];

        /// <summary>
        /// The F2 key. The key in-between <seealso cref="f1Key"/> to the left and <seealso cref="f3Key"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F2 key.</remarks>
        public KeyControl f2Key => this[Key.F2];

        /// <summary>
        /// The F3 key. The key in-between <seealso cref="f2Key"/> to the left and <seealso cref="f4Key"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F3 key.</remarks>
        public KeyControl f3Key => this[Key.F3];

        /// <summary>
        /// The F4 key. The key in-between <seealso cref="f3Key"/> to the left and <seealso cref="f5Key"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F4 key.</remarks>
        public KeyControl f4Key => this[Key.F4];

        /// <summary>
        /// The F5 key. The key in-between <seealso cref="f4Key"/> to the left and <seealso cref="f6Key"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F5 key.</remarks>
        public KeyControl f5Key => this[Key.F5];

        /// <summary>
        /// The F6 key. The key in-between <seealso cref="f5Key"/> to the left and <seealso cref="f7Key"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F6 key.</remarks>
        public KeyControl f6Key => this[Key.F6];

        /// <summary>
        /// The F7 key. The key in-between <seealso cref="f6Key"/> to the left and <seealso cref="f8Key"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F7 key.</remarks>
        public KeyControl f7Key => this[Key.F7];

        /// <summary>
        /// The F8 key. The key in-between <seealso cref="f7Key"/> to the left and <seealso cref="f9Key"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F8 key.</remarks>
        public KeyControl f8Key => this[Key.F8];

        /// <summary>
        /// The F9 key. The key in-between <seealso cref="f8Key"/> to the left and <seealso cref="f10Key"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F9 key.</remarks>
        public KeyControl f9Key => this[Key.F9];

        /// <summary>
        /// The F10 key. The key in-between <seealso cref="f9Key"/> to the left and <seealso cref="f11Key"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F10 key.</remarks>
        public KeyControl f10Key => this[Key.F10];

        /// <summary>
        /// The F11 key. The key in-between <seealso cref="f10Key"/> to the left and <seealso cref="f12Key"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F11 key.</remarks>
        public KeyControl f11Key => this[Key.F11];

        /// <summary>
        /// The F12 key. The key in-between <seealso cref="f11Key"/> to the left and <seealso cref="printScreenKey"/>
        /// to the right in the topmost row of keys.
        /// </summary>
        /// <remarks><seealso cref="KeyControl"/> representing the F12 key.</remarks>
        public KeyControl f12Key => this[Key.F12];

        /// <summary>
        /// First additional key on the keyboard.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing <seealso cref="Key.OEM1"/>.
        /// Keyboards may have additional keys that are not part of the standardized 104-key keyboard layout
        /// (105 in the case of an 18-key numpad). For example, many non-English keyboard layouts have an additional
        /// key in-between <seealso cref="leftShiftKey"/> and <seealso cref="zKey"/>.
        ///
        /// Additional keys may be surfaced by the platform as "OEM" keys. There is no guarantee about where the
        /// keys are located and what symbols they produce. The OEM key controls are mainly there to surface the
        /// inputs but not with the intention of being used in standard bindings.
        /// </remarks>
        public KeyControl oem1Key => this[Key.OEM1];

        /// <summary>
        /// Second additional key on the keyboard.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing <seealso cref="Key.OEM2"/>.
        /// Keyboards may have additional keys that are not part of the standardized 104-key keyboard layout
        /// (105 in the case of an 18-key numpad). For example, many non-English keyboard layouts have an additional
        /// key in-between <seealso cref="leftShiftKey"/> and <seealso cref="zKey"/>.
        ///
        /// Additional keys may be surfaced by the platform as "OEM" keys. There is no guarantee about where the
        /// keys are located and what symbols they produce. The OEM key controls are mainly there to surface the
        /// inputs but not with the intention of being used in standard bindings.
        /// </remarks>
        public KeyControl oem2Key => this[Key.OEM2];

        /// <summary>
        /// Third additional key on the keyboard.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing <seealso cref="Key.OEM3"/>.
        /// Keyboards may have additional keys that are not part of the standardized 104-key keyboard layout
        /// (105 in the case of an 18-key numpad). For example, many non-English keyboard layouts have an additional
        /// key in-between <seealso cref="leftShiftKey"/> and <seealso cref="zKey"/>.
        ///
        /// Additional keys may be surfaced by the platform as "OEM" keys. There is no guarantee about where the
        /// keys are located and what symbols they produce. The OEM key controls are mainly there to surface the
        /// inputs but not with the intention of being used in standard bindings.
        /// </remarks>
        public KeyControl oem3Key => this[Key.OEM3];

        /// <summary>
        /// Fourth additional key on the keyboard.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing <seealso cref="Key.OEM4"/>.
        /// Keyboards may have additional keys that are not part of the standardized 104-key keyboard layout
        /// (105 in the case of an 18-key numpad). For example, many non-English keyboard layouts have an additional
        /// key in-between <seealso cref="leftShiftKey"/> and <seealso cref="zKey"/>.
        ///
        /// Additional keys may be surfaced by the platform as "OEM" keys. There is no guarantee about where the
        /// keys are located and what symbols they produce. The OEM key controls are mainly there to surface the
        /// inputs but not with the intention of being used in standard bindings.
        /// </remarks>
        public KeyControl oem4Key => this[Key.OEM4];

        /// <summary>
        /// Fifth additional key on the keyboard.
        /// </summary>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing <seealso cref="Key.OEM5"/>.
        /// Keyboards may have additional keys that are not part of the standardized 104-key keyboard layout
        /// (105 in the case of an 18-key numpad). For example, many non-English keyboard layouts have an additional
        /// key in-between <seealso cref="leftShiftKey"/> and <seealso cref="zKey"/>.
        ///
        /// Additional keys may be surfaced by the platform as "OEM" keys. There is no guarantee about where the
        /// keys are located and what symbols they produce. The OEM key controls are mainly there to surface the
        /// inputs but not with the intention of being used in standard bindings.
        /// </remarks>
        public KeyControl oem5Key => this[Key.OEM5];

        /// <summary>
        /// An artificial combination of <seealso cref="leftShiftKey"/> and <seealso cref="rightShiftKey"/> into one control.
        /// </summary>
        /// <remarks>
        /// <seealso cref="ButtonControl"/> representing a combined left and right shift key.
        /// This is a <seealso cref="InputControl.synthetic"/> button which is considered pressed whenever the left and/or
        /// right shift key is pressed.
        /// </remarks>
        public ButtonControl shiftKey { get; protected set; }

        /// <summary>
        /// An artificial combination of <seealso cref="leftCtrlKey"/> and <seealso cref="rightCtrlKey"/> into one control.
        /// </summary>
        /// <remarks>
        /// <seealso cref="ButtonControl"/> representing a combined left and right ctrl key.
        /// This is a <seealso cref="InputControl.synthetic"/> button which is considered pressed whenever the left and/or
        /// right ctrl key is pressed.
        /// </remarks>
        public ButtonControl ctrlKey { get; protected set; }

        /// <summary>
        /// An artificial combination of <seealso cref="leftAltKey"/> and <seealso cref="rightAltKey"/> into one control.
        /// </summary>
        /// <remarks>
        /// <seealso cref="ButtonControl"/> representing a combined left and right alt key.
        /// This is a <seealso cref="InputControl.synthetic"/> button which is considered pressed whenever the left and/or
        /// right alt key is pressed.
        /// </remarks>
        public ButtonControl altKey { get; protected set; }

        /// <summary>
        /// True when IME composition is enabled.  Requires <seealso cref="Keyboard.SetIMEEnabled"/> to be called to enable IME, and the user to enable it at the OS level.
        /// </summary>
        /// <remarks>
        /// <seealso cref="ButtonControl"/> representing a combined left and right alt key.
        /// Some languages use complex input methods which involve opening windows to insert characters.
        /// Typically, this is not desirable while playing a game, as games may just interpret key strokes as game input, not as text.
        ///
        /// See <seealso cref="Keyboard.SetIMEEnabled"/> for turning IME on/off
        /// </remarks>
        public ButtonControl imeSelected { get; protected set; }

        /// <summary>
        /// Look up a key control by its key code.
        /// </summary>
        /// <param name="key">Key code of key control to return.</param>
        /// <exception cref="ArgumentOutOfRangeException">The given <paramref name="key"/> is not valid.</exception>
        /// <remarks>
        /// <seealso cref="KeyControl"/> representing a combined left and right alt key.
        /// This is equivalent to <c>allKeys[(int)key - 1]</c>.
        /// </remarks>
        public KeyControl this[Key key]
        {
            get
            {
                var index = (int)key - 1;
                if (index < 0 || index >= m_Keys.Length)
                    throw new ArgumentOutOfRangeException(nameof(key));
                return m_Keys[index];
            }
        }

        /// <summary>
        /// List of all key controls on the keyboard.
        /// </summary>
        public ReadOnlyArray<KeyControl> allKeys => new ReadOnlyArray<KeyControl>(m_Keys);

        /// <summary>
        /// The keyboard that was last used or added. Null if there is no keyboard.
        /// </summary>
        public static Keyboard current { get; private set; }

        /// <summary>
        /// Make a keyboard the current active keyboard.
        /// </summary>
        /// <remarks>
        /// A keyboard will automatically be made current when receiving input or when
        /// added to the input system.
        /// The currently active keyboard is tracked in <seealso cref="Keyboard.current"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
        ///
        /// public class InputExample : MonoBehaviour
        /// {
        ///     void Start()
        ///     {
        ///         // Add a keyboard and make it the current keyboard.
        ///         var keyboard = InputSystem.AddDevice("Keyboard");
        ///         keyboard.MakeCurrent();
        ///     }
        /// }
        /// </code>
        /// </example>
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <inheritdoc/>
        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        /// <summary>
        /// Called after the keyboard has been constructed but before it is added to
        /// the system.
        /// </summary>
        /// <remarks>
        /// This method can be overridden to perform control- or device-specific setup work. The most
        /// common use case is for looking up child controls and storing them in local getters.
        ///</remarks>
        /// <example>
        /// <code>
        /// using UnityEngine.InputSystem;
        /// using UnityEngine.InputSystem.Controls;
        ///
        /// public class MyKeyboard : Keyboard
        /// {
        ///     public ButtonControl button { get; private set; }
        ///
        ///     protected override void FinishSetup()
        ///     {
        ///         // Cache controls in getters.
        ///         button = (ButtonControl)GetChildControl("button");
        ///     }
        /// }
        /// </code>
        /// </example>
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
            shiftKey = GetChildControl<ButtonControl>("shift");
            ctrlKey = GetChildControl<ButtonControl>("ctrl");
            altKey = GetChildControl<ButtonControl>("alt");
            imeSelected = GetChildControl<ButtonControl>("IMESelected");

            base.FinishSetup();
        }

        /// <inheritdoc/>
        protected override void RefreshConfiguration()
        {
            keyboardLayout = null;
            var command = QueryKeyboardLayoutCommand.Create();
            if (ExecuteCommand(ref command) >= 0)
                keyboardLayout = command.ReadLayoutName();
        }

        /// <summary>
        /// Called when text input on the keyboard is received.
        /// </summary>
        /// <param name="character">Char value that represents the character that has been entered.</param>
        /// <remarks>
        /// The system will call this method automatically whenever a <seealso cref="TextEvent"/> is
        /// received that targets the keyboard device. Subscribe to this event by using <seealso cref="onTextInput"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
        ///
        /// public class UserTest : MonoBehaviour
        /// {
        ///     // Simulate text input event on the current keyboard.
        ///     private void FakeInput()
        ///     {
        ///         Keyboard.current.OnTextInput('a');
        ///     }
        /// }
        /// </code>
        /// </example>
        public void OnTextInput(char character)
        {
            for (var i = 0; i < m_TextInputListeners.length; ++i)
                m_TextInputListeners[i](character);
        }

        /// <summary>
        /// Return the key control that, according to the currently active keyboard layout (see <seealso cref="keyboardLayout"/>),
        /// is associated with the given text.
        /// </summary>
        /// <param name="displayName">Display name reported for the key according to the currently active keyboard layout.</param>
        /// <returns>The key control corresponding to the given text or <c>null</c> if no such key was found on the current
        /// keyboard layout.</returns>
        /// <remarks>
        /// In most cases, this means that the key inputs the given text when pressed. However, this does not have to be the
        /// case. Keys do not necessarily lead to character input.
        ///
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
        ///
        /// public class KeyboardUtils : MonoBehaviour
        /// {
        ///     private void FindKey()
        ///     {
        ///         // Retrieve the q key on the current keyboard layout.
        ///         Keyboard.current.FindKeyOnCurrentKeyboardLayout("q");
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="keyboardLayout"/>
        public KeyControl FindKeyOnCurrentKeyboardLayout(string displayName)
        {
            var keys = allKeys;
            for (var i = 0; i < keys.Count; ++i)
                if (string.Equals(keys[i].displayName, displayName, StringComparison.CurrentCultureIgnoreCase))
                    return keys[i];
            return null;
        }

        /// <summary>
        /// This is called to set the IME composition strings. Fired once for every change containing the entire string to date.
        /// </summary>
        /// <param name="compositionString">The <seealso cref="IMECompositionString"/> for the IME composition.</param>
        /// <remarks>
        /// To call back to the changed composition string, subscribe to the <seealso cref="onIMECompositionChange"/> event.
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
        /// using UnityEngine.InputSystem.LowLevel;
        ///
        /// public class KeyboardUtils : MonoBehaviour
        /// {
        ///     private string compositionString = "";
        ///     void ChangeIMEComposition()
        ///     {
        ///         // Manually creating an input event to change the IME composition
        ///         var inputEvent = IMECompositionEvent.Create(Keyboard.current.deviceId, "CompositionTestCharacters! ɝ", Time.time);
        ///         Keyboard.current.OnIMECompositionChanged(inputEvent.compositionString);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="IMECompositionString"/>
        /// <seealso cref="onIMECompositionChange"/>
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
        private KeyControl[] m_Keys;
        private InlinedArray<Action<IMECompositionString>> m_ImeCompositionListeners;

        /// <summary>
        /// Raw array of key controls on the keyboard.
        /// </summary>
        protected KeyControl[] keys
        {
            get => m_Keys;
            set => m_Keys = value;
        }
    }
}
