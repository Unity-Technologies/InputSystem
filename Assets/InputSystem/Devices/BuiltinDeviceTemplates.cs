namespace ISX
{
    internal static class BuiltinDeviceTemplates
    {
        public static void RegisterTemplates(InputManager manager)
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            // Xbox one controller on OSX. State layout can be found here:
            // https://github.com/360Controller/360Controller/blob/master/360Controller/ControlStruct.h
            // struct State
            // {
            //     short buttons;
            //     byte triggerLeft;
            //     byte triggerRight;
            //     short leftX;
            //     short leftY;
            //     short rightX;
            //     short rightY;
            // }
            // Report size is 14 bytes with some stuff at the end we can ignore.
            manager.RegisterTemplate(@"
{
    ""name"" : ""XboxGamepadOSX"",
    ""extend"" : ""Gamepad"",
    ""stateTypeCode"" : ""HID"",
    ""device"" : {
        ""interface"" : ""HID"",
        ""product"" : ""Xbox One Wired Controller""
    }
}
            ");
#endif
        }
    }
}
