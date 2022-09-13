namespace UnityEngine.InputSystem.HighLevelAPI
{
    public enum KeyboardButton
    {
        Space = 0x0100000,
        Digit0,
        Digit1,
        Digit2,
        Digit3,
    };

    internal static class KeyboardButtonExtensions
    {
        internal static ControlReference ToControlId(this KeyboardButton button) => new((int)button);
    }


    public static partial class Input
    {
        public static bool IsPressed(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any) => ButtonControlImpl.IsPressed(button.ToControlId(), slot);
        
        public static bool WasDown(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any) => ButtonControlImpl.WasDown(button.ToControlId(), slot);

        public static bool WasUp(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any) => ButtonControlImpl.WasUp(button.ToControlId(), slot);

        public static float GetAxis(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any) => OneWayAxisControlImpl.GetAxis(button.ToControlId(), slot);

        // helper for WASD like controls
        public static Vector2 GetAxis(KeyboardButton left, KeyboardButton up, KeyboardButton right, KeyboardButton down, DeviceSlot slot = DeviceSlot.Any) => Vector2.zero;


    }

    // public interface IControl
    // {
    //     DeviceSlot deviceSlot { get; }
    // }
    //
    // public interface IButtonControl : IControl
    // {
    //     bool isPressed { get; }
    //     bool wasPressedThisFrame { get; }
    //     bool wasReleasedThisFrame { get; }
    // }
    //
    // public interface IOneWayAxisControl : IControl
    // {
    // }
    //
    // public struct KeyboardButtonControl : IButtonControl
    // {
    //     public readonly KeyboardButton Button;
    //
    //     public readonly DeviceSlot deviceSlot => DeviceSlot.Any;
    //
    //     public bool isPressed => Input.IsPressed(Button);
    //     public bool wasPressedThisFrame => false;
    //     public bool wasReleasedThisFrame => false;
    //
    //     public KeyboardButtonControl(KeyboardButton button)
    //     {
    //         Button = button;
    //     }
    // }
    //
    // public struct GamepadButtonControl : IButtonControl
    // {
    //     public readonly GamepadButton Button;
    //
    //     public readonly DeviceSlot deviceSlot => DeviceSlot.Any;
    //
    //     public bool isPressed => Input.IsPressed(Button);
    //     public bool wasPressedThisFrame => false;
    //     public bool wasReleasedThisFrame => false;
    //
    //     public GamepadButtonControl(GamepadButton button)
    //     {
    //         Button = button;
    //     }
    // }
    //
    // public static class Keyboard
    // {
    //     public static KeyboardButtonControl Space = new KeyboardButtonControl(KeyboardButton.Space);
    //     public static KeyboardButtonControl Digit0 = new KeyboardButtonControl(KeyboardButton.Digit0);
    //     public static KeyboardButtonControl Digit1 = new KeyboardButtonControl(KeyboardButton.Digit1);
    //     public static KeyboardButtonControl Digit2 = new KeyboardButtonControl(KeyboardButton.Digit2);
    //     public static KeyboardButtonControl Digit3 = new KeyboardButtonControl(KeyboardButton.Digit3);
    // }
    //
    // public static class Gamepad
    // {
    //     public static GamepadButtonControl NorthButton = new GamepadButtonControl(GamepadButton.North);
    // }
}