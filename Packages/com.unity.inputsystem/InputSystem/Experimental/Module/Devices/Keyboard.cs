using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    public static partial class Usages
    {
        public static partial class Keyboard
        {
            public static readonly Usage w = new(54654654);
            public static readonly Usage a = new(231321);
            public static readonly Usage s = new(564654);
            public static readonly Usage d = new(987897122);
            public static readonly Usage q = new(78979825);
            public static readonly Usage e = new(754545411);
            public static readonly Usage upArrow = new(754445411);
            public static readonly Usage downArrow = new(343545411);
            public static readonly Usage leftArrow = new(754543411);
            public static readonly Usage rightArrow = new(754122411);
            public static readonly Usage escape = new(412333331);
            public static readonly Usage space = new(1237747);
            public static readonly Usage leftShift = new(456756747);
            public static readonly Usage rightShift = new(1235467447);
            public static readonly Usage shift = new(1231371257);
            public static readonly Usage control = new(1233919257);
            public static readonly Usage alt = new(1237123);
        }
    }
    
    // Auto-generated from extended HID standard specification of Keys. 
    public enum Key
    {
        
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KeyboardState
    {
        public fixed uint keys[16];
    }
    
    public struct Keyboard
    {
        // NOTE: This is just a limited hand-crafted set for proof-of-concept for something that should
        //       be auto-generated from native definitions.
        
        public static ObservableInputNode<bool> W = new(Usages.Keyboard.w, "W");
        public static ObservableInputNode<bool> A = new(Usages.Keyboard.a, "A");
        public static ObservableInputNode<bool> S = new(Usages.Keyboard.s, "S");
        public static ObservableInputNode<bool> D = new(Usages.Keyboard.d, "D");
        public static ObservableInputNode<bool> Q = new(Usages.Keyboard.q, "Q");
        public static ObservableInputNode<bool> E = new(Usages.Keyboard.e, "E");
        public static ObservableInputNode<bool> UpArrow = new(Usages.Keyboard.upArrow, "Up Arrow");
        public static ObservableInputNode<bool> DownArrow = new(Usages.Keyboard.downArrow, "Down Arrow");
        public static ObservableInputNode<bool> LeftArrow = new(Usages.Keyboard.leftArrow, "Left Arrow");
        public static ObservableInputNode<bool> RightArrow = new(Usages.Keyboard.rightArrow, "Right Arrow");
        public static ObservableInputNode<bool> Escape = new(Usages.Keyboard.escape, "Escape");
        public static ObservableInputNode<bool> LeftShift = new(Usages.Keyboard.leftShift, "Left Shift");
        public static ObservableInputNode<bool> RightShift = new(Usages.Keyboard.rightShift, "Right Shift");
        public static ObservableInputNode<bool> Shift = new(Usages.Keyboard.shift, "Shift");
        public static ObservableInputNode<bool> Control = new(Usages.Keyboard.control, "Control");
        public static ObservableInputNode<bool> Alt = new(Usages.Keyboard.alt, "Alt");
        public static ObservableInputNode<bool> Space = new(Usages.Keyboard.space, "Space");
    }
}
