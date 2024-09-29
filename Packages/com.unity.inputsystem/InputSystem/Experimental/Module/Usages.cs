namespace UnityEngine.InputSystem.Experimental
{
    // NOTE: This is auto-generated from native code usage definitions.

    /// <summary>
    /// Input System usages.
    /// </summary>
    public static partial class Usages
    {
        // TODO Consider porting this into an enum type for built-in usages and rely to on external string generators for raw Usage type? (Developer/debugging convenience only)

        internal static partial class Internal
        {
            public static readonly Usage Events = new(0x01);
        }
        
        public static partial class Devices
        {
            /// <summary>
            /// Represents the usage of a device interface for a standard-model mouse device.
            /// </summary>
            public static readonly Usage Mouse = new(0x00010000);
            
            /// <summary>
            /// Represents the usage of a device interface for a standard-model keyboard device.
            /// </summary>
            public static readonly Usage Keyboard = new(0x00020000);

            /// <summary>
            /// Represents the usage of a device interface for a standard-model pointer device.
            /// </summary>
            public static readonly Usage Pointer = new(0x00030000);
            
            /// <summary>
            /// Represents the usage of a device interface for a standard-model gamepad device.
            /// </summary>
            public static readonly Usage Gamepad = new(0x00040000);
        }

        public static partial class Haptic
        {
            public static readonly Usage Uniform = new(0x234234);
        }
    }
}
