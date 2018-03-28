namespace UnityEngine.Experimental.Input.Plugins.Switch
{
    /// <summary>
    /// Adds support for Switch NPad controllers.
    /// </summary>
    [InputPlugin]
    public static class NPadSupport
    {
        public static void Initialize()
        {
			InputSystem.RegisterTemplate<NPad>(deviceDescription: new InputDeviceDescription
            {
				interfaceName = "NPad",
				manufacturer = "Nintendo",
                product = "Wireless Controller",
			});
#if UNITY_SWITCH
			InputSystem.RegisterTemplate(@"{
				""name"" : ""NintendoSwitchController"",
				""extend"" : ""NPad"",
				""format"" : ""NPAD"",
				""device"" : { ""interface"" : ""NPad"" },
				""controls"" : [
				{ ""name"" : ""dpad"", ""offset"" : 0, ""bit"" : 0 },
				{ ""name"" : ""dpad/up"", ""offset"" : 0, ""bit"" : 0 },
				{ ""name"" : ""dpad/down"", ""offset"" : 0, ""bit"" : 1 },
				{ ""name"" : ""dpad/left"", ""offset"" : 0, ""bit"" : 2 },
				{ ""name"" : ""dpad/right"", ""offset"" : 0, ""bit"" : 3 },
				{ ""name"" : ""X"", ""offset"" : 0, ""bit"" : 4 },
				{ ""name"" : ""B"", ""offset"" : 0, ""bit"" : 5 },
				{ ""name"" : ""Y"", ""offset"" : 0, ""bit"" : 6 },
				{ ""name"" : ""A"", ""offset"" : 0, ""bit"" : 7 },
				{ ""name"" : ""StickL"", ""offset"" : 0, ""bit"" : 8 },
				{ ""name"" : ""StickR"", ""offset"" : 0, ""bit"" : 9 },
				{ ""name"" : ""L"", ""offset"" : 0, ""bit"" : 10 },
				{ ""name"" : ""R"", ""offset"" : 0, ""bit"" : 11 },
				{ ""name"" : ""ZL"", ""offset"" : 0, ""bit"" : 12 },
				{ ""name"" : ""ZR"", ""offset"" : 0, ""bit"" : 13 },
				{ ""name"" : ""Plus"", ""offset"" : 0, ""bit"" : 14 },
				{ ""name"" : ""Minus"", ""offset"" : 0, ""bit"" : 15 },
				{ ""name"" : ""LSL"", ""offset"" : 0, ""bit"" : 16 },
				{ ""name"" : ""LSR"", ""offset"" : 0, ""bit"" : 17 },
				{ ""name"" : ""RSL"", ""offset"" : 0, ""bit"" : 18 },
				{ ""name"" : ""RSR"", ""offset"" : 0, ""bit"" : 19 },
				{ ""name"" : ""VK_LUp"", ""offset"" : 0, ""bit"" : 20 },
				{ ""name"" : ""VK_LDown"", ""offset"" : 0, ""bit"" : 21 },
				{ ""name"" : ""VK_LLeft"", ""offset"" : 0, ""bit"" : 22 },
				{ ""name"" : ""VK_LRight"", ""offset"" : 0, ""bit"" : 23 },
				{ ""name"" : ""VK_RUp"", ""offset"" : 0, ""bit"" : 24 },
				{ ""name"" : ""VK_RDown"", ""offset"" : 0, ""bit"" : 25 },
				{ ""name"" : ""VK_RLeft"", ""offset"" : 0, ""bit"" : 26 },
				{ ""name"" : ""VK_RRight"", ""offset"" : 0, ""bit"" : 27 },
				{ ""name"" : ""leftStick"", ""offset"" : 4, ""format"" : ""VC2S"" },
				{ ""name"" : ""leftStick/x"", ""offset"" : 0, ""format"" : ""SHRT"", ""parameters"" : ""clamp=false,invert=false,normalize=false"" },
				{ ""name"" : ""leftStick/left"", ""offset"" : 0, ""format"" : ""SHRT"", ""parameters"" : ""invert=false,normalize=false"" },
				{ ""name"" : ""leftStick/right"", ""offset"" : 0, ""format"" : ""SHRT"", ""parameters"" : ""invert=false,normalize=false"" },
				{ ""name"" : ""leftStick/y"", ""offset"" : 2, ""format"" : ""SHRT"", ""parameters"" : ""clamp=false,invert=false,normalize=false"" },
				{ ""name"" : ""leftStick/up"", ""offset"" : 2, ""format"" : ""SHRT"", ""parameters"" : ""invert=false,normalize=false"" },
				{ ""name"" : ""leftStick/down"", ""offset"" : 2, ""format"" : ""SHRT"", ""parameters"" : ""invert=false,normalize=false"" },
				{ ""name"" : ""rightStick"", ""offset"" : 8, ""format"" : ""VC2S"" },
				{ ""name"" : ""rightStick/x"", ""offset"" : 0, ""format"" : ""SHRT"", ""parameters"" : ""clamp=false,invert=false,normalize=false"" },
				{ ""name"" : ""rightStick/left"", ""offset"" : 0, ""format"" : ""SHRT"", ""parameters"" : ""invert=false,normalize=false"" },
				{ ""name"" : ""rightStick/right"", ""offset"" : 0, ""format"" : ""SHRT"", ""parameters"" : ""invert=false,normalize=false"" },
				{ ""name"" : ""rightStick/y"", ""offset"" : 2, ""format"" : ""SHRT"", ""parameters"" : ""clamp=false,invert=false,normalize=false"" },
				{ ""name"" : ""rightStick/up"", ""offset"" : 2, ""format"" : ""SHRT"", ""parameters"" : ""invert=false,normalize=false"" },
				{ ""name"" : ""rightStick/down"", ""offset"" : 2, ""format"" : ""SHRT"", ""parameters"" : ""invert=false,normalize=false"" }
				] }");
#endif
		}
	}
}
