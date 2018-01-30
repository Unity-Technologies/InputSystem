namespace ISX.DualShock
{
    /// <summary>
    /// Adds support for PS4 DualShock controllers.
    /// </summary>
    [InputPlugin]
    public static class DualShockSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<DualShockGamepad>();

            // HID version for platforms where we pick up the controller as a raw HID.
            // This works without any PS4-specific drivers.
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
            // struct PS4InputReport
            // {
            //     byte reportId;             // #0
            //     byte leftStickX;           // #1
            //     byte leftStickY;           // #2
            //     byte rightStickX;          // #3
            //     byte rightStickY;          // #4
            //     byte dpad : 4;             // #5 bit #0 (0=up, 2=right, 4=down, 6=right)
            //     byte squareButton : 1;     // #5 bit #4
            //     byte crossButton : 1;      // #5 bit #5
            //     byte circleButton : 1;     // #5 bit #6
            //     byte triangleButton : 1;   // #5 bit #7
            //     byte leftShoulder : 1;     // #6 bit #0
            //     byte rightShoulder : 1;    // #6 bit #1
            //     byte leftTriggerButton : 2;// #6 bit #2
            //     byte rightTriggerButton : 2;// #6 bit #3
            //     byte shareButton : 1;      // #6 bit #4
            //     byte optionsButton : 1;    // #6 bit #5
            //     byte leftStickPress : 1;   // #6 bit #6
            //     byte rightStickPress : 1;  // #6 bit #7
            //     byte psButton : 1;         // #7 bit #0
            //     byte touchpadPress : 1;    // #7 bit #1
            //     byte padding : 6;
            //     byte leftTrigger;          // #8
            //     byte rightTrigger;         // #9
            // }
            InputSystem.RegisterTemplate(@"{
""name"" : ""DualShockGamepadHID"",
""extend"" : ""DualShockGamepad"",
""format"" : ""HID"",
""device"" : { ""interface"" : ""HID"", ""product"" : ""Wireless Controller"", ""manufacturer"" : ""Sony Interactive Entertainment"" },
""controls"" : [
    { ""name"" : ""dpad"", ""offset"" : 5, ""bit"" : 0 },
    { ""name"" : ""dpad/up"", ""template"" : ""DiscreteButton"", ""parameters"" : ""minValue=7,maxValue=1,nullValue=8,wrapAtValue=7"", ""bit"" : 0, ""sizeInBits"" : 4 },
    { ""name"" : ""dpad/right"", ""template"" : ""DiscreteButton"", ""parameters"" : ""minValue=1,maxValue=3"", ""bit"" : 0, ""sizeInBits"" : 4 },
    { ""name"" : ""dpad/down"", ""template"" : ""DiscreteButton"", ""parameters"" : ""minValue=3,maxValue=5"", ""bit"" : 0, ""sizeInBits"" : 4 },
    { ""name"" : ""dpad/left"", ""template"" : ""DiscreteButton"", ""parameters"" : ""minValue=5, maxValue=7"", ""bit"" : 0, ""sizeInBits"" : 4 },
    { ""name"" : ""start"", ""offset"" : 6, ""bit"" : 5 },
    { ""name"" : ""select"", ""offset"" : 6, ""bit"" : 4 },
    { ""name"" : ""leftStickPress"", ""offset"" : 6, ""bit"" : 6 },
    { ""name"" : ""rightStickPress"", ""offset"" : 6, ""bit"" : 7 },
    { ""name"" : ""leftShoulder"", ""offset"" : 6, ""bit"" : 0 },
    { ""name"" : ""rightShoulder"", ""offset"" : 6, ""bit"" : 1 },
    { ""name"" : ""buttonSouth"", ""offset"" : 5, ""bit"" : 5 },
    { ""name"" : ""buttonEast"", ""offset"" : 5, ""bit"" : 6 },
    { ""name"" : ""buttonWest"", ""offset"" : 5, ""bit"" : 4 },
    { ""name"" : ""buttonNorth"", ""offset"" : 5, ""bit"" : 7 },
    { ""name"" : ""leftTrigger"", ""offset"" : 9, ""format"" : ""BYTE"" },
    { ""name"" : ""rightTrigger"", ""offset"" : 8, ""format"" : ""BYTE"" },
    { ""name"" : ""leftStick"", ""offset"" : 1, ""format"" : ""VC2B"" },
    { ""name"" : ""leftStick/x"", ""offset"" : 0, ""format"" : ""BYTE"", ""parameters"" : ""normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""leftStick/left"", ""offset"" : 0, ""format"" : ""BYTE"", ""parameters"" : ""normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""leftStick/right"", ""offset"" : 0, ""format"" : ""BYTE"", ""parameters"" : ""normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""leftStick/y"", ""offset"" : 1, ""format"" : ""BYTE"", ""parameters"" : ""invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""leftStick/up"", ""offset"" : 1, ""format"" : ""BYTE"", ""parameters"" : ""invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""leftStick/down"", ""offset"" : 1, ""format"" : ""BYTE"", ""parameters"" : ""invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""rightStick"", ""offset"" : 3, ""format"" : ""VC2B"" },
    { ""name"" : ""rightStick/x"", ""offset"" : 0, ""format"" : ""BYTE"", ""parameters"" : ""normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""rightStick/left"", ""offset"" : 0, ""format"" : ""BYTE"", ""parameters"" : ""normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""rightStick/right"", ""offset"" : 0, ""format"" : ""BYTE"", ""parameters"" : ""normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""rightStick/y"", ""offset"" : 1, ""format"" : ""BYTE"", ""parameters"" : ""invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""rightStick/up"", ""offset"" : 1, ""format"" : ""BYTE"", ""parameters"" : ""invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""rightStick/down"", ""offset"" : 1, ""format"" : ""BYTE"", ""parameters"" : ""invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5"" },
    { ""name"" : ""touchpadPress"", ""template"" : ""Button"", ""offset"" : 7, ""bit"" : 1 }
] }");
#endif
        }
    }
}
