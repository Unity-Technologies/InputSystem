namespace UnityEngine.InputSystem.HighLevelAPI
{
    // Device slots are similar concept to player slots in split screen games.
    // But a specific device slot can contain multiple devices, like a keyboard and a mouse.
    // Device slots could be used for player assignment management.
    public enum DeviceSlot
    {
        Any,
        Slot1,
        Slot2,
        Slot3,
        Slot4,
        Slot5,
        Slot6,
        Slot7,
        Slot8
    }

    public static partial class Input
    {
        public static void AssignToSlot(InputDevice device, DeviceSlot slot)
        {
        }

        public static void RemoveFromSlot(InputDevice device, DeviceSlot slot)
        {
        }
    }
}