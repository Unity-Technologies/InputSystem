#if UNITY_EDITOR || UNITY_ANDROID
namespace UnityEngine.Experimental.Input.Plugins.Android
{
    public static class AndroidSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterControlLayout<AndroidGameController>(
                deviceDescription: new InputDeviceDescription
            {
                interfaceName = "Android",
                deviceClass = "AndroidGameController"
            });

            InputSystem.RegisterControlLayout(@"
{
    ""name"" : ""AndroidGamepadWithDpadAxes"",
    ""extend"" : ""AndroidGameController"",
    ""controls"" : [
        { ""name"" : ""dpad"", ""offset"" : 88, ""format"" : ""VEC2"", ""sizeInBits"" : 64 },
        { ""name"" : ""dpad/left"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampMin=0,clampMax=0.5,normalize,normalizeMin=0,normalizeMax=0.5"" },
        { ""name"" : ""dpad/right"", ""offset"" : 0, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0.5,clampMin=0.5,clampMax=1,normalize,normalizeMin=0.5,normalizeMax=1"" },
        { ""name"" : ""dpad/up"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampConstant=0.5,clampMin=0.5,clampMax=1,normalize,normalizeMin=0.5,normalizeMax=1"" },
        { ""name"" : ""dpad/down"", ""offset"" : 4, ""bit"" : 0, ""format"" : ""FLT"", ""parameters"" : ""clampToConstant,clampMin=0,clampMax=0.5,normalize,normalizeMin=0,normalizeMax=0.5"" }
    ]
}
            ");
            InputSystem.RegisterControlLayout(@"
{
    ""name"" : ""AndroidGamepadWithDpadButtons"",
    ""extend"" : ""AndroidGameController"",
    ""controls"" : [
        { ""name"" : ""dpad"", ""offset"" : 0, ""bit"" : 19, ""sizeInBits"" : 4 },
        { ""name"" : ""dpad/left"", ""bit"" : 21 },
        { ""name"" : ""dpad/right"", ""bit"" : 22 },
        { ""name"" : ""dpad/up"", ""bit"" : 19 },
        { ""name"" : ""dpad/down"", ""bit"" : 20 }
    ]
}
            ");

            InputSystem.onFindControlLayoutForDevice += AndroidGameController.OnFindControlLayoutForDevice;
        }
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
