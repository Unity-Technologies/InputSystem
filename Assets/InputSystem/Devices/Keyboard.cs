using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

    ////FIXME: state layout somehow comes up with a size of 8 bits for this
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KeyboardState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('K', 'E', 'Y', 'S');

        public const int kSizeInBytes = ((int)Key.Count) / 8 + (((int)Key.Count) % 8 > 0 ? 1 : 0);

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

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    [InputState(typeof(KeyboardState))]
    public class Keyboard : InputDevice
    {
        ////TODO: add anyKeyPressed which simply does a MemCmp of th state against default(KeyboardState)

        public event Action<char> onTextInput
        {
            add
            {
                if (m_TextInputListeners == null)
                    m_TextInputListeners = new List<Action<char>>();
                lock (m_TextInputListeners)
                    m_TextInputListeners.Add(value);
            }
            remove
            {
                if (m_TextInputListeners != null)
                    lock (m_TextInputListeners)
                        m_TextInputListeners.Remove(value);
            }
        }

        // Some common keys.
        public ButtonControl escape { get; private set; }
        public ButtonControl space { get; private set; }
        public ButtonControl enter { get; private set; }
        public ButtonControl up { get; private set; }
        public ButtonControl down { get; private set; }
        public ButtonControl left { get; private set; }
        public ButtonControl right { get; private set; }

        public static Keyboard current { get; protected set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            escape = setup.GetControl<ButtonControl>("Escape");
            space = setup.GetControl<ButtonControl>("Space");
            enter = setup.GetControl<ButtonControl>("Enter");
            up = setup.GetControl<ButtonControl>("UpArrow");
            down = setup.GetControl<ButtonControl>("DownArrow");
            left = setup.GetControl<ButtonControl>("LeftArrow");
            right = setup.GetControl<ButtonControl>("RightArrow");

            base.FinishSetup(setup);
        }

        internal List<Action<char>> m_TextInputListeners;
    }
}
