using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    public static partial class Usages
    {
        // TODO We current use the exist Keys enum for simplicity with adapter
        // We use HID compliant usages for keyboard to not diverge from standardized values.
        public static partial class Keyboard
        {
            public static readonly Usage w = new(0x07, (ushort)UnityEngine.InputSystem.Key.W);
            public static readonly Usage a = new(0x07, (ushort)UnityEngine.InputSystem.Key.A);
            public static readonly Usage s = new(0x07, (ushort)UnityEngine.InputSystem.Key.S);
            public static readonly Usage d = new(0x07, (ushort)UnityEngine.InputSystem.Key.D);
            public static readonly Usage q = new(0x07, (ushort)UnityEngine.InputSystem.Key.Q);
            public static readonly Usage e = new(0x07, (ushort)UnityEngine.InputSystem.Key.E);
            public static readonly Usage f = new(0x07, (ushort)UnityEngine.InputSystem.Key.F);
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

    public interface IBitField // Consider this instead?
    {
        int Count { get; }
        bool GetValue(int bitIndex);
    }
    
    // TODO Currently using fixed since that was present from previous code, but if we store these unsafe structs in native memory we shouldn't need to fix address.
    /// <summary>
    /// Represents a mutable keyboard state.
    /// </summary>
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
        public static ObservableInput<KeyboardState> Any = new(Endpoint.FromUsage(Experimental.Usages.Devices.Keyboard),
            "Keyboard");
        
        // Keyboard.Any.leftStick; AND Keyboard.devices[0].leftStick;
        //
        // vs
        //
        // Keyboard.leftStick AND Keyboard.devices[0].leftStick AND Keyboard.devices["platform-dependent-identifier"].leftStick
        
        // TODO public static ReadOnlySpan<Keyboard> devices => GetDevices(Context.instance);

        // TODO Maybe its just a bad idea supporting monitoring of individual keys. Instead we could support monitoring button groups and only filter when required. For this form one would hardcode the offset/checking. But that is quite similar to what we do here.
        
        // TODO Another option is a KeyboardDecoder node which does all of this for us, e.g. map each bit to an output, providing separate ObservableInputNode instances for each. The problem is that we must store the Key to filter on somewhere. 
        
        // TODO Need to implement ButtonControl or BitControl or BinaryControl, HID calls it button, lets make a dedicated KeyControl to support this device
        // TODO Consider specializing KeyControl further to instead deriving usage from changed bits
        
        // Individual key proxies
        public static KeyControl B = Any.Key(Key.B);
        public static KeyControl W = Any.Key(Key.W);
        public static KeyControl A = Any.Key(Key.A);
        public static KeyControl S = Any.Key(Key.S);
        public static KeyControl D = Any.Key(Key.D);
        public static KeyControl C = Any.Key(Key.C);
        public static KeyControl Q = Any.Key(Key.Q); 
        public static KeyControl E = Any.Key(Key.E);
        public static KeyControl F = Any.Key(Key.F);
        public static KeyControl G = Any.Key(Key.G); 
        public static KeyControl Digit1 = Any.Key(Key.Digit1);
        public static KeyControl Digit2 = Any.Key(Key.Digit2);
        public static KeyControl Digit3 = Any.Key(Key.Digit3);
        public static KeyControl Digit4 = Any.Key(Key.Digit4);
        public static KeyControl Digit5 = Any.Key(Key.Digit5);
        public static KeyControl Digit6 = Any.Key(Key.Digit6);
        public static KeyControl Digit7 = Any.Key(Key.Digit7);
        public static KeyControl Digit8 = Any.Key(Key.Digit8);
        public static KeyControl Digit9 = Any.Key(Key.Digit9);
        public static KeyControl Tab = Any.Key(Key.Tab);
        public static KeyControl LeftCtrl = Any.Key(Key.LeftCtrl);
        public static KeyControl Space = Any.Key(Key.Space);
        public static KeyControl Escape = Any.Key(Key.Escape);
        public static KeyControl LeftShift = Any.Key(Key.LeftShift);
        public static KeyControl RightShift = Any.Key(Key.RightShift);
        public static KeyControl LeftAlt = Any.Key(Key.LeftAlt);
        public static KeyControl RightAlt = Any.Key(Key.RightAlt);
        public static KeyControl UpArrow = Any.Key(Key.UpArrow);
        public static KeyControl DownArrow = Any.Key(Key.DownArrow);
        public static KeyControl LeftArrow = Any.Key(Key.LeftArrow);
        public static KeyControl RightArrow = Any.Key(Key.RightArrow);
    }
}
