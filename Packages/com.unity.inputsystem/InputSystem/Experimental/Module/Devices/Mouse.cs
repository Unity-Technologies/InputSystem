using System;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    public static partial class Usages
    {
        public static partial class MouseUsages
        {
            public static readonly Usage PrimaryMouseButton = new(87247892);
            public static readonly Usage SecondaryMouseButton = new(89234982);
            public static readonly Usage Delta = new(9518473);
        }
    }

    [Serializable]
    public struct MouseState
    {
        
    }
    
    [Serializable]
    public struct Mouse
    {
        public static ObservableInput<bool> primaryMouseButton = new (Usages.MouseUsages.PrimaryMouseButton);
        public static ObservableInput<bool> secondaryMouseButton = new (Usages.MouseUsages.SecondaryMouseButton);
        public static ObservableInput<Vector2> delta = new(Usages.MouseUsages.Delta);
    }
}