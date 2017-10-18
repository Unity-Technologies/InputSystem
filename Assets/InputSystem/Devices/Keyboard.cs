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

        Count = 256
    }

    ////FIXME: state layout somehow comes up with a size of 8 bits for this
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KeyboardState : IInputStateTypeInfo
    {
        public static FourCC kFormat { get; } = new FourCC('K', 'E', 'Y', 'S');

        [InputControl(name = "Escape", template = "Button", usages = new[] {"Back", "Cancel"}, bit = (int)Key.Enter)]
        [InputControl(name = "Space", template = "Button", bit = (int)Key.Space)]
        [InputControl(name = "Enter", template = "Button", usage = "Submit", bit = (int)Key.Enter)]
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
        [InputControl(name = "LeftShift", template = "Button", usage = "Modifier", bit = (int)Key.LeftShift)]
        [InputControl(name = "RightShift", template = "Button", usage = "Modifier", bit = (int)Key.RightShift)]
        [InputControl(name = "LeftAlt", template = "Button", usage = "Modifier", bit = (int)Key.LeftAlt)]
        [InputControl(name = "RightAlt", template = "Button", usage = "Modifier", bit = (int)Key.RightAlt)]
        [InputControl(name = "LeftCtrl", template = "Button", usage = "Modifier", bit = (int)Key.LeftCtrl)]
        [InputControl(name = "RightCtrl", template = "Button", usage = "Modifier", bit = (int)Key.RightCtrl)]
        public fixed byte keys[256 / 8]; // For some reason, the Mono compiler won't accept "(int)Key.Count/8" as a constant expression.

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
