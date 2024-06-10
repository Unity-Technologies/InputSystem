namespace UnityEngine.InputSystem.Experimental
{
    // NOTE: This is auto-generated from native code usage definitions.

    /// <summary>
    /// Input System usages.
    /// </summary>
    public static partial class Usages
    {
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
            public static readonly Usage leftStick = new(436321321);
            public static readonly Usage rightStick = new(3213574);
            public static readonly Usage buttonEast = new(64155486);
            public static readonly Usage buttonSouth = new(2313185468);
            public static readonly Usage buttonWest = new(12312312);
            public static readonly Usage buttonNorth = new(2123123468);
            public static readonly Usage rumbleHaptic = new(2521315);
        }

        public static partial class Haptic
        {
            public static readonly Usage uniform = new(0x234234);
        }
    }
}
