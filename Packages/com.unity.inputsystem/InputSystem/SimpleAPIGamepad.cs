namespace UnityEngine.InputSystem.HighLevelAPI
{
    // buttons
    public enum GamepadButton
    {
        LeftTrigger = 0x0200000,
        RightTrigger,

        DpadLeft,
        DpadUp,
        DpadRight,
        DpadDown,

        LeftStickLeft,
        LeftStickUp,
        LeftStickRight,
        LeftStickDown,

        RightStickLeft,
        RightStickUp,
        RightStickRight,
        RightStickDown,

        West,
        North,
        East,
        South,

        LeftStickPress,
        RightStickPress,

        LeftShoulder,
        RightShoulder,

        Start,
        Select,
    }
    
    internal static class GamepadButtonExtensions
    {
        internal static ControlReference ToControlId(this GamepadButton button) => new((int)button);
    }

    // 1D axis with values in [-1, 1] range
    public enum GamepadTwoWayAxis
    {
        DpadHorizontal = 0x0220000,
        DpadVertical,

        LeftStickHorizontal,
        LeftStickVertical,

        RightStickHorizontal,
        RightStickVertical,
    }
    
    internal static class GamepadTwoWayAxisExtensions
    {
        internal static ControlReference ToControlId(this GamepadTwoWayAxis button) => new((int)button);
    }

    // 2D normalized vector
    public enum GamepadStick
    {
        Dpad = 0x0230000,
        Left,
        Right,
    }
    
    internal static class GamepadStickExtensions
    {
        internal static ControlReference ToControlId(this GamepadStick button) => new((int)button);
    }


    public static partial class Input
    {
        public static bool IsPressed(GamepadButton button, DeviceSlot slot = DeviceSlot.Any) => ButtonControlImpl.IsPressed(button.ToControlId(), slot);

        public static bool WasDown(GamepadButton button, DeviceSlot slot = DeviceSlot.Any) => ButtonControlImpl.WasDown(button.ToControlId(), slot);

        public static bool WasUp(GamepadButton button, DeviceSlot slot = DeviceSlot.Any) => ButtonControlImpl.WasUp(button.ToControlId(), slot);

        public static float GetAxis(GamepadButton button, DeviceSlot slot = DeviceSlot.Any) => OneWayAxisControlImpl.GetAxis(button.ToControlId(), slot);

        public static float GetAxis(GamepadTwoWayAxis axis, DeviceSlot slot = DeviceSlot.Any) => TwoWayAxisControlImpl.GetAxis(axis.ToControlId(), slot);

        public static Vector2 GetAxis(GamepadStick stick, DeviceSlot slot = DeviceSlot.Any) => StickControlImpl.GetAxis(stick.ToControlId(), slot);
    }
}