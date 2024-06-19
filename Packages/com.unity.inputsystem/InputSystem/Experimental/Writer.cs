using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Experimental.Devices;

/*namespace UnityEngine.InputSystem.Experimental
{
    // Simulates a platform specific (native code, e.g. C) struct.
    [StructLayout(LayoutKind.Sequential)]
    struct PlatformSpecificGamepadState
    {
        public float x;
        public float y;
        public bool a;
        public bool b;
    }
    
    [StructLayout(LayoutKind.Explicit)]
    struct NativeGamepadState
    {
        [FieldOffset(0)] public float x;
        [FieldOffset(4)] public float y;
        [FieldOffset(0)] public Vector2 leftStick;
        [FieldOffset(8)] public GamepadButton buttons;
    }

    struct Conceptual
    {
        private NativeGamepadState m_Value;
        
        // This exemplifies a hard-coded remapping from a platform-dependent state struct to a
        // standard model state struct.
        public static void Map(ref NativeGamepadState dst, ref PlatformSpecificGamepadState src)
        {
            dst.x = src.x;
            dst.y = -src.y;

            var buttons = GamepadButton.None;
            if (src.a)
                buttons |= GamepadButton.ButtonSouth;
            if (src.b)
                buttons |= GamepadButton.ButtonWest;
            dst.buttons = buttons;
        }

        public static void Write(ref NativeGamepadState data)
        {
            
        }
        
        public static void Handle()
        {
            PlatformSpecificGamepadState src = default; // TODO Read
            NativeGamepadState dst = default;
            
            Map(ref dst, ref src);
            
            
            // No: 
            // TODO Preprocessing pipeline and database
            // TODO Write only subscribed output stream (based on registered receivers)
        }
    }
}*/