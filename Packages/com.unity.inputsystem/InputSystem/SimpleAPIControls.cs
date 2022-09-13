namespace UnityEngine.InputSystem.HighLevelAPI
{
    public struct ControlReference
    {
        public int Index;

        public ControlReference(int index)
        {
            Index = index;
        }
    }

    internal static class ButtonControlImpl
    {
        public static bool IsPressed(ControlReference reference, DeviceSlot slot) => false;
        
        public static bool WasDown(ControlReference reference, DeviceSlot slot) => false;

        public static bool WasUp(ControlReference reference, DeviceSlot slot) => false;
    }
    
    internal static class OneWayAxisControlImpl
    {
        public static float GetAxis(ControlReference reference, DeviceSlot slot) => 0.0f;
    }

    internal static class TwoWayAxisControlImpl
    {
        public static float GetAxis(ControlReference reference, DeviceSlot slot = DeviceSlot.Any) => 0.0f;
    }

    internal static class StickControlImpl
    {
        public static Vector2 GetAxis(ControlReference reference, DeviceSlot slot = DeviceSlot.Any) => Vector2.zero;
    }

    public static partial class Input
    {
    }
}