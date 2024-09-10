using System;
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
        W = UnityEngine.InputSystem.Key.W,
        A = UnityEngine.InputSystem.Key.A,
        S = UnityEngine.InputSystem.Key.S,
        D = UnityEngine.InputSystem.Key.D,
        C = UnityEngine.InputSystem.Key.C,
        LeftCtrl = UnityEngine.InputSystem.Key.LeftCtrl,
        Space = UnityEngine.InputSystem.Key.Space,
        Escape = UnityEngine.InputSystem.Key.Escape,
        LeftShift = UnityEngine.InputSystem.Key.LeftShift,
        RightShift = UnityEngine.InputSystem.Key.RightShift
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
                return UnsafeUtils.GetBit(ptr, (uint)key);
            }
        }
        
        public fixed byte keys[16]; // TODO Review HID + extensions
    }
    
    // CONSIDER
    // If we instead would consider a sparse set, would it help?
    // - HID usages contain, technically in HUT 1.5 up to 0xE7 usages with possible future extension up to 0xFFFF.
    // - If doing sparse we would represents keys being in ON state in the set. If the set is empty, no keys are
    //   pressed. The set could have variable size or be fixed depending on use-case dependent setting. This way
    //   one could pick a size, e.g. 16 bytes of memory and support 8 simultaneously pressed keys.
    // - Processing potentially becomes simpler, needs to be investigated and measured. It could be its own interface.

    public struct ObservableCollection<TIn, TOut>
    {
        
    }

    public struct ObservableKey 
    {
        
    }

    public struct Keyboard
    {
        // NOTE: This is just a limited hand-crafted set for proof-of-concept for something that should
        //       be auto-generated from native definitions.

        /// <summary>
        /// An observable aggregate device of all device instances present on the system.
        /// </summary>
        public static ObservableInput<KeyboardState> any = new(Experimental.Usages.Devices.Keyboard,
            "Keyboard");
        
        // TODO public static ReadOnlySpan<Keyboard> devices => GetDevices(Context.instance);


        // TODO Maybe its just a bad idea supporting monitoring of individual keys. Instead we could support monitoring button groups and only filter when required. For this form one would hardcode the offset/checking. But that is quite similar to what we do here.
        
        // TODO Another option is a KeyboardDecoder node which does all of this for us, e.g. map each bit to an output, providing separate ObservableInputNode instances for each. The problem is that we must store the Key to filter on somewhere. 
        
        // Cached decoders
        private static readonly ConvertFunc<KeyboardState, bool> GetKeyW = (state) => state.GetKey(Key.W);
        private static readonly ConvertFunc<KeyboardState, bool> GetKeyA = (state) => state.GetKey(Key.A);
        private static readonly ConvertFunc<KeyboardState, bool> GetKeyS = (state) => state.GetKey(Key.S);
        private static readonly ConvertFunc<KeyboardState, bool> GetKeyD = (state) => state.GetKey(Key.D);
        private static readonly ConvertFunc<KeyboardState, bool> GetKeyC = (state) => state.GetKey(Key.C);
        private static readonly ConvertFunc<KeyboardState, bool> GetKeyLeftCtrl = (state) => state.GetKey(Key.LeftCtrl);
        private static readonly ConvertFunc<KeyboardState, bool> GetKeySpace = (state) => state.GetKey(Key.Space);
        private static readonly ConvertFunc<KeyboardState, bool> GetKeyEscape = (state) => state.GetKey(Key.Escape);
        private static readonly ConvertFunc<KeyboardState, bool> GetLeftShift = (state) => state.GetKey(Key.LeftShift);
        private static readonly ConvertFunc<KeyboardState, bool> GetRightShift = (state) => state.GetKey(Key.RightShift);
        
        // Individual key proxies
        public static Convert<ObservableInput<KeyboardState>, KeyboardState, bool> W = any.Convert(GetKeyW);
        public static Convert<ObservableInput<KeyboardState>, KeyboardState, bool> A = any.Convert(GetKeyA);
        public static Convert<ObservableInput<KeyboardState>, KeyboardState, bool> S = any.Convert(GetKeyS);
        public static Convert<ObservableInput<KeyboardState>, KeyboardState, bool> D = any.Convert(GetKeyD);
        public static Convert<ObservableInput<KeyboardState>, KeyboardState, bool> C = any.Convert(GetKeyC);
        public static Convert<ObservableInput<KeyboardState>, KeyboardState, bool> LeftCtrl = any.Convert(GetKeyLeftCtrl);
        public static Convert<ObservableInput<KeyboardState>, KeyboardState, bool> Space = any.Convert(GetKeySpace);
        public static Convert<ObservableInput<KeyboardState>, KeyboardState, bool> Escape = any.Convert(GetKeyEscape);
        public static Convert<ObservableInput<KeyboardState>, KeyboardState, bool> LeftShift = any.Convert(GetLeftShift);
        public static Convert<ObservableInput<KeyboardState>, KeyboardState, bool> RightShift = any.Convert(GetRightShift);
        
        // TODO Should we build the decoding into the type, e.g. ObservableInputCollection<Keyboard, Key, bool>(Usages.Keyboard) keys;
        // Keyboard.keys[Key.A].Subscribe(); // TODO This can provide a decoder  
        
        // TODO This is not great, capture used. ObservableInputNode basically mediates subscribtion to a device stream context. That context is usage aware.
        //      We probably need a special transform for sub streams  
        //public static IObservableInput<bool> W = any.Convert((KeyboardState ks) => ks.GetKey(Key.W));
        //public static IObservableInput<bool> A = any.Convert((KeyboardState ks) => ks.GetKey(Key.A));
        //public static IObservableInput<bool> S = any.Convert((KeyboardState ks) => ks.GetKey(Key.S));
        //public static IObservableInput<bool> D = any.Convert((KeyboardState ks) => ks.GetKey(Key.D));
        
        // TODO Wowuld it be beneficial to map usages in 2 tiers. First lookup page (interface), then lookup id (control).
        // If we do this. 
        
        //public static ObservableInputNode<bool> W = new(Usages.Keyboard.w, "W", Field.Bit((uint)Key.W), Experimental.Usages.Devices.Keyboard);
        //public static ObservableInputNode<bool> W = new(Usages.Keyboard.w, "W");
        //public static ObservableInputNode<bool> A = new(Usages.Keyboard.a, "A");
        //public static ObservableInputNode<bool> S = new(Usages.Keyboard.s, "S");
        //public static ObservableInputNode<bool> D = new(Usages.Keyboard.d, "D");
        public static ObservableInput<bool> Q = new(Usages.Keyboard.q, "Q");
        public static ObservableInput<bool> E = new(Usages.Keyboard.e, "E");
        public static ObservableInput<bool> UpArrow = new(Usages.Keyboard.upArrow, "Up Arrow");
        public static ObservableInput<bool> DownArrow = new(Usages.Keyboard.downArrow, "Down Arrow");
        public static ObservableInput<bool> LeftArrow = new(Usages.Keyboard.leftArrow, "Left Arrow");
        public static ObservableInput<bool> RightArrow = new(Usages.Keyboard.rightArrow, "Right Arrow");
        //public static ObservableInput<bool> LeftShift = new(Usages.Keyboard.leftShift, "Left Shift");
        //public static ObservableInput<bool> RightShift = new(Usages.Keyboard.rightShift, "Right Shift");
        //public static ObservableInputNode<bool> Shift = new(Usages.Keyboard.shift, "Shift");
        //public static ObservableInputNode<bool> Control = new(Usages.Keyboard.control, "Control");
        //public static ObservableInputNode<bool> Alt = new(Usages.Keyboard.alt, "Alt");
        //public static ObservableInputNode<bool> Space = new(Usages.Keyboard.space, "Space");
    }
}
