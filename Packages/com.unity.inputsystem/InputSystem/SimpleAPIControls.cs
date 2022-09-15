namespace UnityEngine.InputSystem.HighLevelAPI
{
    // It is in principle beneficial to have a list of all controls,
    // as this is where stability of enum API values could be enforced.
    // Also we could separate controls by type based on this enum,
    // e.g. have bool IsButton(InputControl control) and similar.
    // It's also useful for rebinding activities.
    // Plus we can figure out if we need to split a control into multiple of different types,
    // e.g. mouse scroll is vector2 delta control, and also 2 single axis controls, plus 4 buttons.
    // But for the sake of type safety, let's not expose it directly to the user,
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

        // mouse scroll divided by delta time, and normalized to [-1, 1] range
        // useful for using scroll like gamepad stick
        MouseTwoWayAxisScrollHorizontalTimeNormalized = 0x0210000,
        MouseTwoWayAxisScrollVerticalTimeNormalized,

        // Cursor position is an absolute value control, unlike vector2 control which is normalized
        MouseCursorPosition = 0x0220000,

        // Scroll delta is a relative value control, unlike vector2 control which is normalized
        MouseScrollDelta = 0x0230000,

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
        ScrollHorizontalTimeNormalized = InputControl.MouseTwoWayAxisScrollHorizontalTimeNormalized,
        ScrollVerticalTimeNormalized = InputControl.MouseTwoWayAxisScrollVerticalTimeNormalized,
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
    
    // It is useful to allow the users to store controls in some generic data structure like a list,
    // for that we need to unify all enums back to generic InputControl, but keep the type safety.
    // This container struct tries to achieve exactly that.
    public struct InputControlReference
    {
        internal InputControl control;
        
        // TODO do we want to add some type checking here?
        // is it even possible?

        public static implicit operator InputControlReference(KeyboardButton btn)
        {
            return new InputControlReference() {control = (InputControl)btn};
        }

        public static implicit operator InputControlReference(MouseButton btn)
        {
            return new InputControlReference() {control = (InputControl)btn};
        }
        
        public static implicit operator InputControlReference(MouseTwoWayAxis btn)
        {
            return new InputControlReference() {control = (InputControl)btn};
        }

        public static implicit operator InputControlReference(GamepadButton btn)
        {
            return new InputControlReference() {control = (InputControl)btn};
        }

        public static implicit operator InputControlReference(GamepadTwoWayAxis btn)
        {
            return new InputControlReference() {control = (InputControl)btn};
        }

        public static implicit operator InputControlReference(GamepadStick btn)
        {
            return new InputControlReference() {control = (InputControl)btn};
        }
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

        // Less type-safe API, only to be used when required to store controls of different type
        // together in some data structure like list or array.
        // Notice that we can't do GetAxis here and have to revert to spell out method name fully,
        // this is to avoid clash of return type overloads.
        public static class ByReference
        {
            public static bool IsButtonPressed(InputControlReference control, DeviceSlot slot) => false;

            public static bool WasButtonDown(InputControlReference control, DeviceSlot slot) => false;
            
            public static bool WasButtonUp(InputControlReference control, DeviceSlot slot) => false;

            // any button is also a one way axis
            public static float GetOneWayAxis(InputControlReference control, DeviceSlot slot) => 0.0f;

            public static float GetTwoWayAxis(InputControlReference control, DeviceSlot slot) => 0.0f;

            public static Vector2 GetStick(InputControlReference control, DeviceSlot slot) => Vector2.zero;
        };

        public static bool IsPressed(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.IsButtonPressed(button, slot);

        public static bool WasDown(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.WasButtonDown(button, slot);

        public static bool WasUp(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.WasButtonUp(button, slot);

        public static float GetAxis(KeyboardButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.GetOneWayAxis(button, slot);

        // helper for WASD like controls
        public static Vector2 GetAxis(KeyboardButton left, KeyboardButton up, KeyboardButton right, KeyboardButton down,
            DeviceSlot slot = DeviceSlot.Any)
        {
            return new Vector2( GetAxis(right, slot) - GetAxis(left, slot), GetAxis(up, slot) - GetAxis(down, slot));
        }
        
        public static bool IsPressed(MouseButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.IsButtonPressed(button, slot);

        public static bool WasDown(MouseButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.WasButtonDown(button, slot);

        public static bool WasUp(MouseButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.WasButtonUp(button, slot);

        public static float GetAxis(MouseButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.GetOneWayAxis(button, slot);
        
        public static float GetAxis(MouseTwoWayAxis button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.GetTwoWayAxis(button, slot);

        public static Vector2 GetMousePosition(DeviceSlot slot = DeviceSlot.Any) => Vector2.zero;

        public static Vector2 GetMouseScroll(DeviceSlot slot = DeviceSlot.Any) => Vector2.zero;

        public static bool IsPressed(GamepadButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.IsButtonPressed(button, slot);

        public static bool WasDown(GamepadButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.WasButtonDown(button, slot);

        public static bool WasUp(GamepadButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.WasButtonUp(button, slot);

        // returns value in [0, 1] range
        public static float GetAxis(GamepadButton button, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.GetOneWayAxis(button, slot);

        // returns value in [-1, 1] range
        public static float GetAxis(GamepadTwoWayAxis axis, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.GetTwoWayAxis(axis, slot);
        public static Vector2 GetAxis(GamepadStick stick, DeviceSlot slot = DeviceSlot.Any) =>
            ByReference.GetStick(stick, slot);

        private static void SetRebinding(InputControl driveInputControl, InputControl[] withAnyOfFollowingControls, DeviceSlot inSlot)
        {
        }

        
        private static void StartRebinding(InputControl control, DeviceSlot slot)
        {
        }
    }
}