namespace UnityEngine.InputSystem.Experimental.Devices
{
    public static partial class Usages
    {
        public static partial class PointerUsages
        {
            public static readonly Usage Trigger = new(43291321);
            public static readonly Usage Position = new(9876342);
        }
    }

    public enum Button
    {
        Off,
        On
    }
    
    [InputSource]
    public readonly struct Pointer
    {
        public static readonly ObservableInput<Vector2> position = new(Usages.PointerUsages.Position, nameof(Pointer) + "." + nameof(position));
        public static readonly ObservableInput<Button> trigger = new(Usages.PointerUsages.Trigger, nameof(Pointer) + "." + nameof(trigger));
    }
}