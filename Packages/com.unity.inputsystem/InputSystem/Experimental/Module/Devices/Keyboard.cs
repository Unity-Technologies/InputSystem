using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    public static partial class Usages
    {
        // TODO We current use the exist Keys enum for simplicity
        // We use HID compliant usages for keyboard to not diverge from standardized values.
        public static partial class Keyboard
        {
            public static readonly Usage w = new(0x07, (ushort)UnityEngine.InputSystem.Key.W);
            public static readonly Usage a = new(0x07, (ushort)UnityEngine.InputSystem.Key.A);
            public static readonly Usage s = new(0x07, (ushort)UnityEngine.InputSystem.Key.S);
            public static readonly Usage d = new(0x07, (ushort)UnityEngine.InputSystem.Key.D);
            public static readonly Usage q = new(0x07, (ushort)UnityEngine.InputSystem.Key.Q);
            public static readonly Usage e = new(0x07, (ushort)UnityEngine.InputSystem.Key.E);
            public static readonly Usage upArrow = new(0x07, (ushort)UnityEngine.InputSystem.Key.UpArrow);
            public static readonly Usage downArrow = new(0x07, (ushort)UnityEngine.InputSystem.Key.DownArrow);
            public static readonly Usage leftArrow = new(0x07, (ushort)UnityEngine.InputSystem.Key.LeftArrow);
            public static readonly Usage rightArrow = new(0x07, (ushort)UnityEngine.InputSystem.Key.RightArrow);
            public static readonly Usage escape = new(0x07, (ushort)UnityEngine.InputSystem.Key.Escape);
            public static readonly Usage space = new(0x07, (ushort)UnityEngine.InputSystem.Key.Space);
            public static readonly Usage leftShift = new(0x07, (ushort)UnityEngine.InputSystem.Key.LeftShift);
            public static readonly Usage rightShift = new(0x07, (ushort)UnityEngine.InputSystem.Key.RightShift);
            public static readonly Usage leftControl = new(0x07, (ushort)UnityEngine.InputSystem.Key.LeftCtrl);
            public static readonly Usage rightControl = new(0x07, (ushort)UnityEngine.InputSystem.Key.RightCtrl);

            //public static readonly Usage shift = new(0x07, (ushort)UnityEngine.InputSystem.Key.Shift); // TODO Missing combined support
            //public static readonly Usage control = new(0x07, 0x11);
            //public static readonly Usage alt = new(0x07, 0x12);
        }
    }
    
    // Auto-generated from extended HID standard specification of Keys. 
    public enum Key
    {
        
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 4)]
    public struct Buttons
    {
        public void Clear(int bitIndex)
        {
            value &= (1U << (bitIndex & 31));
        }
        
        public void Set(int bitIndex)
        {
            value |= (1U << (bitIndex & 31));
        }

        [FieldOffset(0)] public uint value;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KeyboardState
    {
        public void ClearKey(Key key)
        {
            fixed (byte* ptr = keys)
            {
                UnsafeUtils.ClearBit((byte*)ptr, (uint)key);
            }
        }
        
        public void SetKey(Key key)
        {
            fixed(byte* ptr = keys)
            {
                UnsafeUtils.SetBit((byte*)ptr, (uint)key);
            }
        }
        
        public void SetKey(Key key, bool value)
        {
            fixed(byte* ptr = keys)
            {
                UnsafeUtils.SetBit((byte*)ptr, (uint)key, value);
            }
        }

        public bool GetKey(Key key)
        {
            fixed(byte* ptr = keys)
            {
                return UnsafeUtils.GetBit((byte*)ptr, (uint)key);
            }
        }
        
        public fixed byte keys[16]; // TODO Review HID + extensions
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
        //public static ObservableInputNode<bool> Shift = new(Usages.Keyboard.shift, "Shift");
        //public static ObservableInputNode<bool> Control = new(Usages.Keyboard.control, "Control");
        //public static ObservableInputNode<bool> Alt = new(Usages.Keyboard.alt, "Alt");
        public static ObservableInputNode<bool> Space = new(Usages.Keyboard.space, "Space");
    }
}
