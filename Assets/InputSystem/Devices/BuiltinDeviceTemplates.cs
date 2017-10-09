namespace ISX
{
    internal static class BuiltinDeviceTemplates
    {
        public static void RegisterTemplates(InputManager manager)
        {
            ////TODO: it would be totally rad if instead of going to JSON in code here,
            ////      you could just create a new state struct representing the changed
            ////      state layout and then feed that into the template system; essentially,
            ////      InputReport below would become a real C# struct
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            // Xbox one controller on OSX. State layout can be found here:
            // https://github.com/360Controller/360Controller/blob/master/360Controller/ControlStruct.h
            // struct InputReport
            // {
            //     byte command;
            //     byte size;
            //     short buttons;
            //     byte triggerLeft;
            //     byte triggerRight;
            //     short leftX;
            //     short leftY;
            //     short rightX;
            //     short rightY;
            // }
            // Report size is 14 bytes. First two bytes are header information for the report.
            ////TODO: come up with a way that allows us to snip that data out of the state we store and the
            ////      state we compare
            ////TODO: rumble and LED output
            manager.RegisterTemplate(@"{
""name"" : ""XboxGamepadOSX"",
""extend"" : ""Gamepad"",
""format"" : ""HID"",
""device"" : { ""interface"" : ""HID"", ""product"" : ""Xbox One.*Controller"" },
""controls"" : [
    { ""name"" : ""leftShoulder"", ""offset"" : 2, ""bit"" : 0 },
    { ""name"" : ""rightShoulder"", ""offset"" : 2, ""bit"" : 1 },
    { ""name"" : ""buttonSouth"", ""offset"" : 2, ""bit"" : 4 },
    { ""name"" : ""buttonEast"", ""offset"" : 2, ""bit"" : 5 },
    { ""name"" : ""buttonWest"", ""offset"" : 2, ""bit"" : 6 },
    { ""name"" : ""buttonNorth"", ""offset"" : 2, ""bit"" : 7 },
    { ""name"" : ""dpad"", ""offset"" : 2 },
    { ""name"" : ""dpad/up"", ""offset"" : 2, ""bit"" : 8 },
    { ""name"" : ""dpad/down"", ""offset"" : 2, ""bit"" : 9 },
    { ""name"" : ""dpad/left"", ""offset"" : 2, ""bit"" : 10 },
    { ""name"" : ""dpad/right"", ""offset"" : 2, ""bit"" : 11 },
    { ""name"" : ""start"", ""offset"" : 2, ""bit"" : 12 },
    { ""name"" : ""select"", ""offset"" : 2, ""bit"" : 13 },
    { ""name"" : ""xbox"", ""offset"" : 2, ""bit"" : 2, ""template"" : ""Button"" },
    { ""name"" : ""leftHat"", ""offset"" : 2, ""bit"" : 14, ""template"" : ""Button"" },
    { ""name"" : ""rightHat"", ""offset"" : 2, ""bit"" : 15, ""template"" : ""Button"" },
    { ""name"" : ""leftTrigger"", ""offset"" : 4, ""format"" : ""BYTE"" },
    { ""name"" : ""rightTrigger"", ""offset"" : 5, ""format"" : ""BYTE"" },
    { ""name"" : ""leftStick"", ""offset"" : 6, ""format"" : ""VC2S"" },
    { ""name"" : ""leftStick/x"", ""offset"" : 0, ""format"" : ""SHRT"", ""parameters"" : ""normalize,normalizeMin=-0.5,normalizeMax=0.5"" },
    { ""name"" : ""leftStick/y"", ""offset"" : 2, ""format"" : ""SHRT"", ""parameters"" : ""invert,normalize,normalizeMin=-0.5,normalizeMax=0.5"" },
    { ""name"" : ""rightStick"", ""offset"" : 10, ""format"" : ""VC2S"" },
    { ""name"" : ""rightStick/x"", ""offset"" : 0, ""format"" : ""SHRT"", ""parameters"" : ""normalize,normalizeMin=-0.5,normalizeMax=0.5"" },
    { ""name"" : ""rightStick/y"", ""offset"" : 2, ""format"" : ""SHRT"", ""parameters"" : ""invert,normalize,normalizeMin=-0.5,normalizeMax=0.5"" }
] }");
#endif
        }
    }
}
