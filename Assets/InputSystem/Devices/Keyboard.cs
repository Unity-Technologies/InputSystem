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
        [InputControl(name = "Escape", template = "Button", usages = new[] {"Back", "Cancel"}, bit = (int)Key.Escape)]
        [InputControl(name = "Space", template = "Button", bit = (int)Key.Space)]
        [InputControl(name = "Enter", template = "Button", usage = "Submit", bit = (int)Key.Enter)]
        [InputControl(name = "Tab", template = "Button", bit = (int)Key.Tab)]
        [InputControl(name = "Backtick", template = "Button", bit = (int)Key.Backtick)]
        [InputControl(name = "Semicolon", template = "Button", bit = (int)Key.Semicolon)]
        [InputControl(name = "Comma", template = "Button", bit = (int)Key.Comma)]
        [InputControl(name = "Period", template = "Button", bit = (int)Key.Period)]
        [InputControl(name = "Slash", template = "Button", bit = (int)Key.Slash)]
        [InputControl(name = "Backslash", template = "Button", bit = (int)Key.Backslash)]
        [InputControl(name = "LeftBracket", template = "Button", bit = (int)Key.LeftBracket)]
        [InputControl(name = "RightBracket", template = "Button", bit = (int)Key.RightBracket)]
        [InputControl(name = "Minus", template = "Button", bit = (int)Key.Minus)]
        [InputControl(name = "Equals", template = "Button", bit = (int)Key.Equals)]
        [InputControl(name = "UpArrow", template = "Button", bit = (int)Key.UpArrow)]
        [InputControl(name = "DownArrow", template = "Button", bit = (int)Key.DownArrow)]
        [InputControl(name = "LeftArrow", template = "Button", bit = (int)Key.LeftArrow)]
        [InputControl(name = "RightArrow", template = "Button", bit = (int)Key.RightArrow)]
        [InputControl(name = "A", template = "Button", bit = (int)Key.A)]
        [InputControl(name = "B", template = "Button", bit = (int)Key.B)]
        [InputControl(name = "C", template = "Button", bit = (int)Key.C)]
        [InputControl(name = "D", template = "Button", bit = (int)Key.D)]
        [InputControl(name = "E", template = "Button", bit = (int)Key.E)]
        [InputControl(name = "F", template = "Button", bit = (int)Key.F)]
        [InputControl(name = "G", template = "Button", bit = (int)Key.G)]
        [InputControl(name = "H", template = "Button", bit = (int)Key.H)]
        [InputControl(name = "I", template = "Button", bit = (int)Key.I)]
        [InputControl(name = "J", template = "Button", bit = (int)Key.J)]
        [InputControl(name = "K", template = "Button", bit = (int)Key.K)]
        [InputControl(name = "L", template = "Button", bit = (int)Key.L)]
        [InputControl(name = "M", template = "Button", bit = (int)Key.M)]
        [InputControl(name = "N", template = "Button", bit = (int)Key.N)]
        [InputControl(name = "O", template = "Button", bit = (int)Key.O)]
        [InputControl(name = "P", template = "Button", bit = (int)Key.P)]
        [InputControl(name = "Q", template = "Button", bit = (int)Key.Q)]
        [InputControl(name = "R", template = "Button", bit = (int)Key.R)]
        [InputControl(name = "S", template = "Button", bit = (int)Key.S)]
        [InputControl(name = "T", template = "Button", bit = (int)Key.T)]
        [InputControl(name = "U", template = "Button", bit = (int)Key.U)]
        [InputControl(name = "V", template = "Button", bit = (int)Key.V)]
        [InputControl(name = "W", template = "Button", bit = (int)Key.W)]
        [InputControl(name = "X", template = "Button", bit = (int)Key.X)]
        [InputControl(name = "Y", template = "Button", bit = (int)Key.Y)]
        [InputControl(name = "Z", template = "Button", bit = (int)Key.Z)]
        [InputControl(name = "1", template = "Button", bit = (int)Key.Digit1)]
        [InputControl(name = "2", template = "Button", bit = (int)Key.Digit2)]
        [InputControl(name = "3", template = "Button", bit = (int)Key.Digit3)]
        [InputControl(name = "4", template = "Button", bit = (int)Key.Digit4)]
        [InputControl(name = "5", template = "Button", bit = (int)Key.Digit5)]
        [InputControl(name = "6", template = "Button", bit = (int)Key.Digit6)]
        [InputControl(name = "7", template = "Button", bit = (int)Key.Digit7)]
        [InputControl(name = "8", template = "Button", bit = (int)Key.Digit8)]
        [InputControl(name = "9", template = "Button", bit = (int)Key.Digit9)]
        [InputControl(name = "0", template = "Button", bit = (int)Key.Digit0)]
        [InputControl(name = "LeftShift", template = "Button", usage = "Modifier", bit = (int)Key.LeftShift)]
        [InputControl(name = "RightShift", template = "Button", usage = "Modifier", bit = (int)Key.RightShift)]
        [InputControl(name = "LeftAlt", template = "Button", usage = "Modifier", bit = (int)Key.LeftAlt)]
        [InputControl(name = "RightAlt", template = "Button", usage = "Modifier", bit = (int)Key.RightAlt)]
        [InputControl(name = "LeftCtrl", template = "Button", usage = "Modifier", bit = (int)Key.LeftCtrl)]
        [InputControl(name = "RightCtrl", template = "Button", usage = "Modifier", bit = (int)Key.RightCtrl)]
        [InputControl(name = "Backspace", template = "Button", bit = (int)Key.Backspace)]
        [InputControl(name = "PageDown", template = "Button", bit = (int)Key.PageDown)]
        [InputControl(name = "PageUp", template = "Button", bit = (int)Key.PageUp)]
        [InputControl(name = "Home", template = "Button", bit = (int)Key.Home)]
        [InputControl(name = "End", template = "Button", bit = (int)Key.End)]
        [InputControl(name = "Insert", template = "Button", bit = (int)Key.Insert)]
        [InputControl(name = "Erase", template = "Button", bit = (int)Key.Erase)]
        [InputControl(name = "NumpadEnter", template = "Button", bit = (int)Key.NumpadEnter)]
        [InputControl(name = "Numpad1", template = "Button", bit = (int)Key.Numpad1)]
        [InputControl(name = "Numpad2", template = "Button", bit = (int)Key.Numpad2)]
        [InputControl(name = "Numpad3", template = "Button", bit = (int)Key.Numpad3)]
        [InputControl(name = "Numpad4", template = "Button", bit = (int)Key.Numpad4)]
        [InputControl(name = "Numpad5", template = "Button", bit = (int)Key.Numpad5)]
        [InputControl(name = "Numpad6", template = "Button", bit = (int)Key.Numpad6)]
        [InputControl(name = "Numpad7", template = "Button", bit = (int)Key.Numpad7)]
        [InputControl(name = "Numpad8", template = "Button", bit = (int)Key.Numpad8)]
        [InputControl(name = "Numpad9", template = "Button", bit = (int)Key.Numpad9)]
        [InputControl(name = "Numpad0", template = "Button", bit = (int)Key.Numpad0)]
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
        public ButtonControl space { get; private set; }
        public ButtonControl enter { get; private set; }
        public ButtonControl tab { get; private set; }
        public ButtonControl backtick { get; private set; }
        public ButtonControl semicolon { get; private set; }
        public ButtonControl comma { get; private set; }
        public ButtonControl period { get; private set; }
        public ButtonControl slash { get; private set; }
        public ButtonControl backslash { get; private set; }
        public ButtonControl leftBracket { get; private set; }
        public ButtonControl rightBracket { get; private set; }
        public ButtonControl minus { get; private set; }
        public ButtonControl equals { get; private set; }
        public ButtonControl a { get; private set; }
        public ButtonControl b { get; private set; }
        public ButtonControl c { get; private set; }
        public ButtonControl d { get; private set; }
        public ButtonControl e { get; private set; }
        public ButtonControl f { get; private set; }
        public ButtonControl g { get; private set; }
        public ButtonControl h { get; private set; }
        public ButtonControl i { get; private set; }
        public ButtonControl j { get; private set; }
        public ButtonControl k { get; private set; }
        public ButtonControl l { get; private set; }
        public ButtonControl m { get; private set; }
        public ButtonControl n { get; private set; }
        public ButtonControl o { get; private set; }
        public ButtonControl p { get; private set; }
        public ButtonControl q { get; private set; }
        public ButtonControl r { get; private set; }
        public ButtonControl s { get; private set; }
        public ButtonControl t { get; private set; }
        public ButtonControl u { get; private set; }
        public ButtonControl v { get; private set; }
        public ButtonControl w { get; private set; }
        public ButtonControl x { get; private set; }
        public ButtonControl y { get; private set; }
        public ButtonControl z { get; private set; }
        public ButtonControl digit1 { get; private set; }
        public ButtonControl digit2 { get; private set; }
        public ButtonControl digit3 { get; private set; }
        public ButtonControl digit4 { get; private set; }
        public ButtonControl digit5 { get; private set; }
        public ButtonControl digit6 { get; private set; }
        public ButtonControl digit7 { get; private set; }
        public ButtonControl digit8 { get; private set; }
        public ButtonControl digit9 { get; private set; }
        public ButtonControl digit0 { get; private set; }
        public ButtonControl leftShift { get; private set; }
        public ButtonControl rightShift { get; private set; }
        public ButtonControl leftAlt { get; private set; }
        public ButtonControl rightAlt { get; private set; }
        public ButtonControl leftCtrl { get; private set; }
        public ButtonControl rightCtrl { get; private set; }
        public ButtonControl escape { get; private set; }
        public ButtonControl leftArrow { get; private set; }
        public ButtonControl rightArrow { get; private set; }
        public ButtonControl upArrow { get; private set; }
        public ButtonControl downArrow { get; private set; }
        public ButtonControl backspace { get; private set; }
        public ButtonControl pageDown { get; private set; }
        public ButtonControl pageUp { get; private set; }
        public ButtonControl home { get; private set; }
        public ButtonControl end { get; private set; }
        public ButtonControl insert { get; private set; }
        public ButtonControl erase { get; private set; }
        public ButtonControl numpadEnter { get; private set; }
        public ButtonControl numpad0 { get; private set; }
        public ButtonControl numpad1 { get; private set; }
        public ButtonControl numpad2 { get; private set; }
        public ButtonControl numpad3 { get; private set; }
        public ButtonControl numpad4 { get; private set; }
        public ButtonControl numpad5 { get; private set; }
        public ButtonControl numpad6 { get; private set; }
        public ButtonControl numpad7 { get; private set; }
        public ButtonControl numpad8 { get; private set; }
        public ButtonControl numpad9 { get; private set; }

        public static Keyboard current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            any = setup.GetControl<AnyKeyControl>("AnyKey");
            space = setup.GetControl<ButtonControl>("Space");
            enter = setup.GetControl<ButtonControl>("Enter");
            tab = setup.GetControl<ButtonControl>("Tab");
            backtick = setup.GetControl<ButtonControl>("Backtick");
            semicolon = setup.GetControl<ButtonControl>("Semicolon");
            comma = setup.GetControl<ButtonControl>("Comma");
            period = setup.GetControl<ButtonControl>("Period");
            slash = setup.GetControl<ButtonControl>("Slash");
            backslash = setup.GetControl<ButtonControl>("Backslash");
            leftBracket = setup.GetControl<ButtonControl>("LeftBracket");
            rightBracket = setup.GetControl<ButtonControl>("RightBracket");
            minus = setup.GetControl<ButtonControl>("Minus");
            equals = setup.GetControl<ButtonControl>("Equals");
            a = setup.GetControl<ButtonControl>("A");
            b = setup.GetControl<ButtonControl>("B");
            c = setup.GetControl<ButtonControl>("C");
            d = setup.GetControl<ButtonControl>("D");
            e = setup.GetControl<ButtonControl>("E");
            f = setup.GetControl<ButtonControl>("F");
            g = setup.GetControl<ButtonControl>("G");
            h = setup.GetControl<ButtonControl>("H");
            i = setup.GetControl<ButtonControl>("I");
            j = setup.GetControl<ButtonControl>("J");
            k = setup.GetControl<ButtonControl>("K");
            l = setup.GetControl<ButtonControl>("L");
            m = setup.GetControl<ButtonControl>("M");
            n = setup.GetControl<ButtonControl>("N");
            o = setup.GetControl<ButtonControl>("O");
            p = setup.GetControl<ButtonControl>("P");
            q = setup.GetControl<ButtonControl>("Q");
            r = setup.GetControl<ButtonControl>("R");
            s = setup.GetControl<ButtonControl>("S");
            t = setup.GetControl<ButtonControl>("T");
            u = setup.GetControl<ButtonControl>("U");
            v = setup.GetControl<ButtonControl>("V");
            w = setup.GetControl<ButtonControl>("W");
            x = setup.GetControl<ButtonControl>("X");
            y = setup.GetControl<ButtonControl>("Y");
            z = setup.GetControl<ButtonControl>("Z");
            digit1 = setup.GetControl<ButtonControl>("1");
            digit2 = setup.GetControl<ButtonControl>("2");
            digit3 = setup.GetControl<ButtonControl>("3");
            digit4 = setup.GetControl<ButtonControl>("4");
            digit5 = setup.GetControl<ButtonControl>("5");
            digit6 = setup.GetControl<ButtonControl>("6");
            digit7 = setup.GetControl<ButtonControl>("7");
            digit8 = setup.GetControl<ButtonControl>("8");
            digit9 = setup.GetControl<ButtonControl>("9");
            digit0 = setup.GetControl<ButtonControl>("0");
            leftShift = setup.GetControl<ButtonControl>("LeftShift");
            rightShift = setup.GetControl<ButtonControl>("RightShift");
            leftAlt = setup.GetControl<ButtonControl>("LeftAlt");
            rightAlt = setup.GetControl<ButtonControl>("RightAlt");
            leftCtrl = setup.GetControl<ButtonControl>("LeftCtrl");
            rightCtrl = setup.GetControl<ButtonControl>("RightCtrl");
            escape = setup.GetControl<ButtonControl>("Escape");
            leftArrow = setup.GetControl<ButtonControl>("LeftArrow");
            rightArrow = setup.GetControl<ButtonControl>("RightArrow");
            upArrow = setup.GetControl<ButtonControl>("UpArrow");
            downArrow = setup.GetControl<ButtonControl>("DownArrow");
            backspace = setup.GetControl<ButtonControl>("Backspace");
            pageDown = setup.GetControl<ButtonControl>("PageDown");
            pageUp = setup.GetControl<ButtonControl>("PageUp");
            home = setup.GetControl<ButtonControl>("Home");
            end = setup.GetControl<ButtonControl>("End");
            insert = setup.GetControl<ButtonControl>("Insert");
            erase = setup.GetControl<ButtonControl>("Erase");
            numpadEnter = setup.GetControl<ButtonControl>("NumpadEnter");
            numpad0 = setup.GetControl<ButtonControl>("Numpad0");
            numpad1 = setup.GetControl<ButtonControl>("Numpad1");
            numpad2 = setup.GetControl<ButtonControl>("Numpad2");
            numpad3 = setup.GetControl<ButtonControl>("Numpad3");
            numpad4 = setup.GetControl<ButtonControl>("Numpad4");
            numpad5 = setup.GetControl<ButtonControl>("Numpad5");
            numpad6 = setup.GetControl<ButtonControl>("Numpad6");
            numpad7 = setup.GetControl<ButtonControl>("Numpad7");
            numpad8 = setup.GetControl<ButtonControl>("Numpad8");
            numpad9 = setup.GetControl<ButtonControl>("Numpad9");

            base.FinishSetup(setup);
        }

        internal InlinedArray<Action<char>> m_TextInputListeners;
    }
}
