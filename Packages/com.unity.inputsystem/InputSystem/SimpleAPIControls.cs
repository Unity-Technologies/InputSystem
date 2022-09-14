namespace UnityEngine.InputSystem.HighLevelAPI
{
    // It is in principle beneficial to have a list of all controls,
    // as this is where stability of enum API values could be enforced.
    // Also we could separate controls by type based on this enum,
    // e.g. have bool IsButton(InputControl control) and similar.
    // It's also useful for rebinding activities.
    // But for sake of type safety, let's not expose it directly to the user,
    // and provide necessary overrides instead.
    internal enum InputControl
    {
        KeyboardButtonSpace = 0x0100000,
        KeyboardButtonDigit0,
        KeyboardButtonDigit1,
        KeyboardButtonDigit2,
        KeyboardButtonDigit3,
        // ... 100 more

        MouseButtonLeft = 0x0200000,
        MouseButtonRight,
        MouseButtonMiddle,
        MouseButtonForward,
        MouseButtonBack,
        MouseButtonScrollLeft,
        MouseButtonScrollUp,
        MouseButtonScrollRight,
        MouseButtonScrollDown,

        // two way axis goes [-1, 1]
        MouseTwoWayAxisScrollHorizontal = 0x0210000,
        MouseTwoWayAxisScrollVertical,

        // Cursor position is an absolute value control, unlike two way axis
        MouseCursorPosition = 0x0230000,

        GamepadButtonLeftTrigger = 0x0300000,
        GamepadButtonRightTrigger,
        GamepadButtonDpadLeft,
        GamepadButtonDpadUp,
        GamepadButtonDpadRight,
        GamepadButtonDpadDown,
        GamepadButtonLeftStickLeft,
        GamepadButtonLeftStickUp,
        GamepadButtonLeftStickRight,
        GamepadButtonLeftStickDown,
        GamepadButtonRightStickLeft,
        GamepadButtonRightStickUp,
        GamepadButtonRightStickRight,
        GamepadButtonRightStickDown,
        GamepadButtonWest,
        GamepadButtonNorth,
        GamepadButtonEast,
        GamepadButtonSouth,
        GamepadButtonLeftStickPress,
        GamepadButtonRightStickPress,
        GamepadButtonLeftShoulder,
        GamepadButtonRightShoulder,
        GamepadButtonStart,
        GamepadButtonSelect,

        GamepadTwoWayAxisDpadHorizontal = 0x0310000,
        GamepadTwoWayAxisDpadVertical,
        GamepadTwoWayAxisLeftStickHorizontal,
        GamepadTwoWayAxisLeftStickVertical,
        GamepadTwoWayAxisRightStickHorizontal,
        GamepadTwoWayAxisRightStickVertical,

        // stick is normalized vector2
        GamepadStickDpad = 0x0320000,
        GamepadStickLeft,
        GamepadStickRight
    }

    public enum KeyboardButton
    {
        Space = InputControl.KeyboardButtonSpace,
        Digit0 = InputControl.KeyboardButtonDigit0,
        Digit1 = InputControl.KeyboardButtonDigit1,
        Digit2 = InputControl.KeyboardButtonDigit2,
        Digit3 = InputControl.KeyboardButtonDigit3
    }

    public enum MouseButton
    {
        Left = InputControl.MouseButtonLeft,
        Right = InputControl.MouseButtonRight,
        Middle = InputControl.MouseButtonMiddle,
        Forward = InputControl.MouseButtonForward,
        Back = InputControl.MouseButtonBack,
        ScrollLeft = InputControl.MouseButtonScrollLeft,
        ScrollUp = InputControl.MouseButtonScrollUp,
        ScrollRight = InputControl.MouseButtonScrollRight,
        ScrollDown = InputControl.MouseButtonScrollDown,
    }
    
    public enum MouseTwoWayAxis
    {
        ScrollHorizontal = InputControl.MouseTwoWayAxisScrollHorizontal,
        ScrollVertical = InputControl.MouseTwoWayAxisScrollVertical
    }

    // buttons
    public enum GamepadButton
    {
        LeftTrigger = InputControl.GamepadButtonLeftTrigger,
        RightTrigger = InputControl.GamepadButtonRightTrigger,
        DpadLeft = InputControl.GamepadButtonDpadLeft,
        DpadUp = InputControl.GamepadButtonDpadUp,
        DpadRight = InputControl.GamepadButtonDpadRight,
        DpadDown = InputControl.GamepadButtonDpadDown,
        LeftStickLeft = InputControl.GamepadButtonLeftStickLeft,
        LeftStickUp = InputControl.GamepadButtonLeftStickUp,
        LeftStickRight = InputControl.GamepadButtonLeftStickRight,
        LeftStickDown = InputControl.GamepadButtonLeftStickDown,
        RightStickLeft = InputControl.GamepadButtonRightStickLeft,
        RightStickUp = InputControl.GamepadButtonRightStickUp,
        RightStickRight = InputControl.GamepadButtonRightStickRight,
        RightStickDown = InputControl.GamepadButtonRightStickDown,
        West = InputControl.GamepadButtonWest,
        North = InputControl.GamepadButtonNorth,
        East = InputControl.GamepadButtonEast,
        South = InputControl.GamepadButtonSouth,
        LeftStickPress = InputControl.GamepadButtonLeftStickPress,
        RightStickPress = InputControl.GamepadButtonRightStickPress,
        LeftShoulder = InputControl.GamepadButtonLeftShoulder,
        RightShoulder = InputControl.GamepadButtonRightShoulder,
        Start = InputControl.GamepadButtonStart,
        Select = InputControl.GamepadButtonSelect
    }

    // 1D axis with values in [-1, 1] range
    public enum GamepadTwoWayAxis
    {
        DpadHorizontal = InputControl.GamepadTwoWayAxisDpadHorizontal,
        DpadVertical = InputControl.GamepadTwoWayAxisDpadVertical,
        LeftStickHorizontal = InputControl.GamepadTwoWayAxisLeftStickHorizontal,
        LeftStickVertical = InputControl.GamepadTwoWayAxisLeftStickVertical,
        RightStickHorizontal = InputControl.GamepadTwoWayAxisRightStickHorizontal,
        RightStickVertical = InputControl.GamepadTwoWayAxisRightStickVertical
    }

    // 2D normalized vector
    public enum GamepadStick
    {
        Dpad = InputControl.GamepadStickDpad,
        Left = InputControl.GamepadStickLeft,
        Right = InputControl.GamepadStickRight
    }

    // Device slots are similar concept to player slots in split screen games.
    // But a specific device slot can contain multiple devices, like a keyboard and a mouse.
    // Device slots could be used for player assignment management.
    // By default all new devices go to Unassigned slot.
    public enum DeviceSlot
    {
        Any,
        Unassigned,
        Slot1,
        Slot2,
        Slot3,
        Slot4,
        Slot5,
        Slot6,
        Slot7,
        Slot8
    }


    public static class Input
    {
        public static void AssignDeviceToSlot(InputDevice device, DeviceSlot slot)
        {
        }

        public static void RemoveDeviceFromSlot(InputDevice device, DeviceSlot slot)
        {
        }

        public static InputDevice[] GetDevices(DeviceSlot slot)
        {
            return null;
        }
        
        public static InputDevice GetLatestUsedDevice(DeviceSlot slot)
        {
            return null;
        }

        public static bool IsConnected(DeviceSlot slot)
        {
            return GetDevices(slot).Length != 0;
        }

        private static bool IsPressed(InputControl control, DeviceSlot slot) => false;

        private static bool WasDown(InputControl control, DeviceSlot slot) => false;

        private static bool WasUp(InputControl control, DeviceSlot slot) => false;

        // any button is also a one way axis
        private static float GetOneWayAxis(InputControl control, DeviceSlot slot) => 0.0f;

        private static float GetTwoWayAxis(InputControl control, DeviceSlot slot) => 0.0f;

        private static Vector2 GetStick(InputControl control, DeviceSlot slot) => Vector2.zero;

        public static bool IsPressed(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return IsPressed((InputControl) button, slot);
        }

        public static bool WasDown(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return WasDown((InputControl) button, slot);
        }

        public static bool WasUp(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return WasUp((InputControl) button, slot);
        }

        public static float GetAxis(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return GetOneWayAxis((InputControl) button, slot);
        }

        // helper for WASD like controls
        public static Vector2 GetAxis(KeyboardButton left, KeyboardButton up, KeyboardButton right, KeyboardButton down,
            DeviceSlot slot = DeviceSlot.Any)
        {
            return Vector2.zero;
        }
        
        public static bool IsPressed(MouseButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return IsPressed((InputControl) button, slot);
        }

        public static bool WasDown(MouseButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return WasDown((InputControl) button, slot);
        }

        public static bool WasUp(MouseButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return WasUp((InputControl) button, slot);
        }

        public static float GetAxis(MouseButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return GetOneWayAxis((InputControl) button, slot);
        }
        
        public static float GetAxis(MouseTwoWayAxis axis, DeviceSlot slot = DeviceSlot.Any)
        {
            return GetTwoWayAxis((InputControl) axis, slot);
        }

        public static Vector2 GetMousePosition(DeviceSlot slot = DeviceSlot.Any) => Vector2.zero;

        public static Vector2 GetMouseScroll(DeviceSlot slot = DeviceSlot.Any) => Vector2.zero;

        public static bool IsPressed(GamepadButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return IsPressed((InputControl) button, slot);
        }

        public static bool WasDown(GamepadButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return WasDown((InputControl) button, slot);
        }

        public static bool WasUp(GamepadButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return WasUp((InputControl) button, slot);
        }

        // returns value in [0, 1] range
        public static float GetAxis(GamepadButton button, DeviceSlot slot = DeviceSlot.Any)
        {
            return GetOneWayAxis((InputControl) button, slot);
        }

        // returns value in [-1, 1] range
        public static float GetAxis(GamepadTwoWayAxis axis, DeviceSlot slot = DeviceSlot.Any)
        {
            return GetTwoWayAxis((InputControl) axis, slot);
        }

        public static Vector2 GetAxis(GamepadStick stick, DeviceSlot slot = DeviceSlot.Any)
        {
            return GetStick((InputControl) stick, slot);
        }
    }
}