namespace DocCodeSamples.Tests
{
    using UnityEngine.InputSystem;

    internal class HidCreateCustomLayoutExisting
    {
        const string myDeviceJson = @"
#region myDeviceJson
    {
        ""name"" : ""MyDevice"",
        ""extend"" : ""Gamepad"", // Or some other thing
        ""controls"" : [
            {
                ""name"" : ""firstButton"",
                ""layout"" : ""Button"",
                ""offset"" : 0,
                ""bit"": 0,
                ""format"" : ""BIT""
            },
            {
                ""name"" : ""secondButton"",
                ""layout"" : ""Button"",
                ""offset"" : 0,
                ""bit"": 1,
                ""format"" : ""BIT""
            },
            {
                ""name"" : ""axis"",
                ""layout"" : ""Axis"",
                ""offset"" : 4,
                ""format"" : ""FLT"",
                ""parameters"" : ""clamp=true,clampMin=0,clampMax=1""
            }
        ]
    }
#endregion
";

        void Example()
        {
            #region registerAndCreate
            InputSystem.RegisterLayout(myDeviceJson);
            var device = InputSystem.AddDevice("MyDevice");
            #endregion
        }
    }
}
