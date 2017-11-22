using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    // Named according to the US keyboard layout which is our reference layout.
    public enum Key
    {
        // Printable keys.
        Space,
        Enter,
        Tab,
        Backtick,
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
        LeftCtrl,
        RightCtrl,
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
        Erase,

        // Numpad.
        NumpadEnter,
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

        Count
    }

    // NOTE: This layout has to match the KeyboardInputState layout used in native!
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KeyboardState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('K', 'E', 'Y', 'S');
        public const int kSizeInBytes = ((int)Key.Count) / 8 + (((int)Key.Count) % 8 > 0 ? 1 : 0);
        public const int kSizeInBits = kSizeInBytes * 8;

        [InputControl(name = "AnyKey", template = "AnyKey", sizeInBits = kSizeInBits)]
        [InputControl(name = "Escape", template = "Key", usages = new[] {"Back", "Cancel"}, bit = (int)Key.Escape)]
        [InputControl(name = "Space", template = "Key", bit = (int)Key.Space)]
        [InputControl(name = "Enter", template = "Key", usage = "Accept", bit = (int)Key.Enter)]
        [InputControl(name = "Tab", template = "Key", bit = (int)Key.Tab)]
        [InputControl(name = "Backtick", template = "Key", bit = (int)Key.Backtick)]
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
        [InputControl(name = "RightAlt", template = "Key", usage = "Modifier", bit = (int)Key.RightAlt)]
        [InputControl(name = "LeftCtrl", template = "Key", usage = "Modifier", bit = (int)Key.LeftCtrl)]
        [InputControl(name = "RightCtrl", template = "Key", usage = "Modifier", bit = (int)Key.RightCtrl)]
        [InputControl(name = "Backspace", template = "Key", bit = (int)Key.Backspace)]
        [InputControl(name = "PageDown", template = "Key", bit = (int)Key.PageDown)]
        [InputControl(name = "PageUp", template = "Key", bit = (int)Key.PageUp)]
        [InputControl(name = "Home", template = "Key", bit = (int)Key.Home)]
        [InputControl(name = "End", template = "Key", bit = (int)Key.End)]
        [InputControl(name = "Insert", template = "Key", bit = (int)Key.Insert)]
        [InputControl(name = "Erase", template = "Key", bit = (int)Key.Erase)]
        [InputControl(name = "NumpadEnter", template = "Key", bit = (int)Key.NumpadEnter)]
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
        public fixed byte keys[kSizeInBytes];

        public KeyboardState(params Key[] pressedKeys)
        {
            fixed(byte* keysPtr = keys)
            {
                UnsafeUtility.MemClear(new IntPtr(keysPtr), kSizeInBytes);
                for (var i = 0; i < pressedKeys.Length; ++i)
                {
                    BitfieldHelpers.WriteSingleBit(new IntPtr(keysPtr), (uint)pressedKeys[i], true);
                }
            }
        }

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    [InputState(typeof(KeyboardState))]
    public class Keyboard : InputDevice
    {
        public event Action<char> onTextInput
        {
            add { m_TextInputListeners.Append(value); }
            remove { m_TextInputListeners.Remove(value); }
        }

        public AnyKeyControl any { get; private set; }
        public KeyControl space { get; private set; }
        public KeyControl enter { get; private set; }
        public KeyControl tab { get; private set; }
        public KeyControl backtick { get; private set; }
        public KeyControl semicolon { get; private set; }
        public KeyControl comma { get; private set; }
        public KeyControl period { get; private set; }
        public KeyControl slash { get; private set; }
        public KeyControl backslash { get; private set; }
        public KeyControl leftBracket { get; private set; }
        public KeyControl rightBracket { get; private set; }
        public KeyControl minus { get; private set; }
        public KeyControl equals { get; private set; }
        public KeyControl a { get; private set; }
        public KeyControl b { get; private set; }
        public KeyControl c { get; private set; }
        public KeyControl d { get; private set; }
        public KeyControl e { get; private set; }
        public KeyControl f { get; private set; }
        public KeyControl g { get; private set; }
        public KeyControl h { get; private set; }
        public KeyControl i { get; private set; }
        public KeyControl j { get; private set; }
        public KeyControl k { get; private set; }
        public KeyControl l { get; private set; }
        public KeyControl m { get; private set; }
        public KeyControl n { get; private set; }
        public KeyControl o { get; private set; }
        public KeyControl p { get; private set; }
        public KeyControl q { get; private set; }
        public KeyControl r { get; private set; }
        public KeyControl s { get; private set; }
        public KeyControl t { get; private set; }
        public KeyControl u { get; private set; }
        public KeyControl v { get; private set; }
        public KeyControl w { get; private set; }
        public KeyControl x { get; private set; }
        public KeyControl y { get; private set; }
        public KeyControl z { get; private set; }
        public KeyControl digit1 { get; private set; }
        public KeyControl digit2 { get; private set; }
        public KeyControl digit3 { get; private set; }
        public KeyControl digit4 { get; private set; }
        public KeyControl digit5 { get; private set; }
        public KeyControl digit6 { get; private set; }
        public KeyControl digit7 { get; private set; }
        public KeyControl digit8 { get; private set; }
        public KeyControl digit9 { get; private set; }
        public KeyControl digit0 { get; private set; }
        public KeyControl leftShift { get; private set; }
        public KeyControl rightShift { get; private set; }
        public KeyControl leftAlt { get; private set; }
        public KeyControl rightAlt { get; private set; }
        public KeyControl leftCtrl { get; private set; }
        public KeyControl rightCtrl { get; private set; }
        public KeyControl escape { get; private set; }
        public KeyControl leftArrow { get; private set; }
        public KeyControl rightArrow { get; private set; }
        public KeyControl upArrow { get; private set; }
        public KeyControl downArrow { get; private set; }
        public KeyControl backspace { get; private set; }
        public KeyControl pageDown { get; private set; }
        public KeyControl pageUp { get; private set; }
        public KeyControl home { get; private set; }
        public KeyControl end { get; private set; }
        public KeyControl insert { get; private set; }
        public KeyControl erase { get; private set; }
        public KeyControl numpadEnter { get; private set; }
        public KeyControl numpad0 { get; private set; }
        public KeyControl numpad1 { get; private set; }
        public KeyControl numpad2 { get; private set; }
        public KeyControl numpad3 { get; private set; }
        public KeyControl numpad4 { get; private set; }
        public KeyControl numpad5 { get; private set; }
        public KeyControl numpad6 { get; private set; }
        public KeyControl numpad7 { get; private set; }
        public KeyControl numpad8 { get; private set; }
        public KeyControl numpad9 { get; private set; }

        public static Keyboard current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            any = setup.GetControl<AnyKeyControl>("AnyKey");
            space = setup.GetControl<KeyControl>("Space");
            enter = setup.GetControl<KeyControl>("Enter");
            tab = setup.GetControl<KeyControl>("Tab");
            backtick = setup.GetControl<KeyControl>("Backtick");
            semicolon = setup.GetControl<KeyControl>("Semicolon");
            comma = setup.GetControl<KeyControl>("Comma");
            period = setup.GetControl<KeyControl>("Period");
            slash = setup.GetControl<KeyControl>("Slash");
            backslash = setup.GetControl<KeyControl>("Backslash");
            leftBracket = setup.GetControl<KeyControl>("LeftBracket");
            rightBracket = setup.GetControl<KeyControl>("RightBracket");
            minus = setup.GetControl<KeyControl>("Minus");
            equals = setup.GetControl<KeyControl>("Equals");
            a = setup.GetControl<KeyControl>("A");
            b = setup.GetControl<KeyControl>("B");
            c = setup.GetControl<KeyControl>("C");
            d = setup.GetControl<KeyControl>("D");
            e = setup.GetControl<KeyControl>("E");
            f = setup.GetControl<KeyControl>("F");
            g = setup.GetControl<KeyControl>("G");
            h = setup.GetControl<KeyControl>("H");
            i = setup.GetControl<KeyControl>("I");
            j = setup.GetControl<KeyControl>("J");
            k = setup.GetControl<KeyControl>("K");
            l = setup.GetControl<KeyControl>("L");
            m = setup.GetControl<KeyControl>("M");
            n = setup.GetControl<KeyControl>("N");
            o = setup.GetControl<KeyControl>("O");
            p = setup.GetControl<KeyControl>("P");
            q = setup.GetControl<KeyControl>("Q");
            r = setup.GetControl<KeyControl>("R");
            s = setup.GetControl<KeyControl>("S");
            t = setup.GetControl<KeyControl>("T");
            u = setup.GetControl<KeyControl>("U");
            v = setup.GetControl<KeyControl>("V");
            w = setup.GetControl<KeyControl>("W");
            x = setup.GetControl<KeyControl>("X");
            y = setup.GetControl<KeyControl>("Y");
            z = setup.GetControl<KeyControl>("Z");
            digit1 = setup.GetControl<KeyControl>("1");
            digit2 = setup.GetControl<KeyControl>("2");
            digit3 = setup.GetControl<KeyControl>("3");
            digit4 = setup.GetControl<KeyControl>("4");
            digit5 = setup.GetControl<KeyControl>("5");
            digit6 = setup.GetControl<KeyControl>("6");
            digit7 = setup.GetControl<KeyControl>("7");
            digit8 = setup.GetControl<KeyControl>("8");
            digit9 = setup.GetControl<KeyControl>("9");
            digit0 = setup.GetControl<KeyControl>("0");
            leftShift = setup.GetControl<KeyControl>("LeftShift");
            rightShift = setup.GetControl<KeyControl>("RightShift");
            leftAlt = setup.GetControl<KeyControl>("LeftAlt");
            rightAlt = setup.GetControl<KeyControl>("RightAlt");
            leftCtrl = setup.GetControl<KeyControl>("LeftCtrl");
            rightCtrl = setup.GetControl<KeyControl>("RightCtrl");
            escape = setup.GetControl<KeyControl>("Escape");
            leftArrow = setup.GetControl<KeyControl>("LeftArrow");
            rightArrow = setup.GetControl<KeyControl>("RightArrow");
            upArrow = setup.GetControl<KeyControl>("UpArrow");
            downArrow = setup.GetControl<KeyControl>("DownArrow");
            backspace = setup.GetControl<KeyControl>("Backspace");
            pageDown = setup.GetControl<KeyControl>("PageDown");
            pageUp = setup.GetControl<KeyControl>("PageUp");
            home = setup.GetControl<KeyControl>("Home");
            end = setup.GetControl<KeyControl>("End");
            insert = setup.GetControl<KeyControl>("Insert");
            erase = setup.GetControl<KeyControl>("Erase");
            numpadEnter = setup.GetControl<KeyControl>("NumpadEnter");
            numpad0 = setup.GetControl<KeyControl>("Numpad0");
            numpad1 = setup.GetControl<KeyControl>("Numpad1");
            numpad2 = setup.GetControl<KeyControl>("Numpad2");
            numpad3 = setup.GetControl<KeyControl>("Numpad3");
            numpad4 = setup.GetControl<KeyControl>("Numpad4");
            numpad5 = setup.GetControl<KeyControl>("Numpad5");
            numpad6 = setup.GetControl<KeyControl>("Numpad6");
            numpad7 = setup.GetControl<KeyControl>("Numpad7");
            numpad8 = setup.GetControl<KeyControl>("Numpad8");
            numpad9 = setup.GetControl<KeyControl>("Numpad9");

            base.FinishSetup(setup);
        }

        internal InlinedArray<Action<char>> m_TextInputListeners;
    }
}
