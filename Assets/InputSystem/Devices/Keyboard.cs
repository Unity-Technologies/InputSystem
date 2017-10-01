using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace InputSystem
{
    // Named according to the US keyboard layout which is our reference layout.
    public enum Key
    {
        None,
        
        // Non-printable keys.
        LeftShift,
        RightShift,
        LeftAlt,
        RightAlt,
        Escape,
        
        // Printable keys.
        Space = ' ',
        Enter = '\r',
        Backtick = '`',
        Semicolon = ';',
        Comma = ',',
        Slash = '/',
        Backslash = '\\',
        LeftBracket = '[',
        RightBracket = ']',
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
        One = '1',
        Two = '2',
        Three = '3',
        Four = '4',
        Five = '5',
        Six = '6',
        Seven = '7',
        Eight = '8',
        Nine = '9',
        Zero = '0',
        
        Count = 256
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KeyboardState : IInputStateTypeInfo
    {
        public static FourCC kStateTypeCode
        {
            get { return new FourCC('K', 'E', 'Y', 'S'); }
        }

        [InputControl(name = "Escape", type = "Button", usage = "Back", bit = (int) Key.Enter)]
        [InputControl(name = "Space", type = "Button", bit = (int) Key.Space)]
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
	                return *((KeyboardState*) currentStatePtr);
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

        public static Keyboard current { get; protected set; }

        public Keyboard(string name)
            : base(name)
        {
        }
        
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        internal List<Action<char>> m_TextInputListeners;
    }
}