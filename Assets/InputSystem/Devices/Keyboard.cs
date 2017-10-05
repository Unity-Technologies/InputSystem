using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ISX
{
    // Named according to the US keyboard layout which is our reference layout.
    public enum Key
    {
        None,

        // Printable keys.
        Space = ' ',
        Enter = '\r',
        Tab = '\t',
        Backtick = '`',
        Semicolon = ';',
        Comma = ',',
        Period = '.',
        Slash = '/',
        Backslash = '\\',
        LeftBracket = '[',
        RightBracket = ']',
        Minus = '-',
        Equals = '=',
        A = 'a',
        B = 'b',
        C = 'c',
        D = 'd',
        E = 'e',
        F = 'f',
        G = 'g',
        H = 'h',
        I = 'i',
        J = 'j',
        K = 'k',
        L = 'l',
        M = 'm',
        N = 'n',
        O = 'o',
        P = 'p',
        Q = 'q',
        R = 'r',
        S = 's',
        T = 't',
        U = 'u',
        V = 'v',
        W = 'w',
        X = 'x',
        Y = 'y',
        Z = 'z',
        Digit1 = '1',
        Digit2 = '2',
        Digit3 = '3',
        Digit4 = '4',
        Digit5 = '5',
        Digit6 = '6',
        Digit7 = '7',
        Digit8 = '8',
        Digit9 = '9',
        Digit0 = '0',

        // Non-printable keys.
        LeftShift = 128, // Make sure we don't conflict with any of the printable keys.
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

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KeyboardState : IInputStateTypeInfo
    {
        public static FourCC kStateTypeCode
        {
            get { return new FourCC('K', 'E', 'Y', 'S'); }
        }

        [InputControl(name = "Escape", template = "Button", usages = new [] {"Back", "Cancel"}, bit = (int)Key.Enter)]
        [InputControl(name = "Space", template = "Button", bit = (int)Key.Space)]
        [InputControl(name = "Enter", template = "Button", usage = "Submit", bit = (int)Key.Enter)]
        [InputControl(name = "UpArrow", template = "Button", bit = (int)Key.UpArrow)]
        [InputControl(name = "DownArrow", template = "Button", bit = (int)Key.DownArrow)]
        [InputControl(name = "LeftArrow", template = "Button", bit = (int)Key.LeftArrow)]
        [InputControl(name = "RightArrow", template = "Button", bit = (int)Key.RightArrow)]
        public fixed byte keys[256 / 8]; // For some reason, the Mono compiler won't accept "(int)Key.Count/8" as a constant expression.

        public FourCC GetTypeStatic()
        {
            return kStateTypeCode;
        }
    }

    [InputState(typeof(KeyboardState))]
    public class Keyboard : InputDevice
    {
        public KeyboardState state
        {
            get
            {
                unsafe
                {
                    return *((KeyboardState*)currentValuePtr);
                }
            }
        }

        public override object valueAsObject
        {
            get { return state; }
        }

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

        public Keyboard()
        {
        }

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
