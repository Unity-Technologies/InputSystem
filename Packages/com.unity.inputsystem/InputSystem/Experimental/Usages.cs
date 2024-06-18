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
            public static readonly Usage Mouse = new(0x00010000);
            public static readonly Usage Keyboard = new(0x00020000);
            public static readonly Usage Gamepad = new(0x00040000);
        }
        
        public static partial class Keyboard
        {
            public static readonly Usage w = new(54654654);
            public static readonly Usage a = new(231321);
            public static readonly Usage s = new(564654);
            public static readonly Usage d = new(987897122);
            public static readonly Usage space = new(12313747);
        }
        
        public static partial class GamepadUsages
        {
            public static readonly Usage LeftStick = new(436321321);
            public static readonly Usage RightStick = new(3213574);
            public static readonly Usage ButtonEast = new(64155486);
            public static readonly Usage ButtonSouth = new(2313185468);
            public static readonly Usage ButtonWest = new(12312312);
            public static readonly Usage ButtonNorth = new(2123123468);
            public static readonly Usage RumbleHaptic = new(2521315);
        }

        public static partial class Haptic
        {
            public static readonly Usage Uniform = new(0x234234);
        }
    }
}
