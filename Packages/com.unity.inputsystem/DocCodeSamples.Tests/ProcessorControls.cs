using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

#region mydevice
public struct MyDeviceState : IInputStateTypeInfo
{
    public FourCC format => new FourCC('M', 'Y', 'D', 'V');

    // Add an axis deadzone to the Control to ignore values
    // smaller then 0.2, as our Control does not have a stable
    // resting position.
    [InputControl(layout = "Axis", processors = "AxisDeadzone(min=0.2)")]
    public short axis;
}
#endregion

class MyDeviceLayoutJson
{
    const string json = @"
    #region mydevicejson
    {
        ""name"" : ""MyDevice"",
        ""extend"" : ""Gamepad"", // Or some other thing
        ""controls"" : [
            {
                ""name"" : ""axis"",
                ""layout"" : ""Axis"",
                ""offset"" : 4,
                ""format"" : ""FLT"",
                ""processors"" : ""AxisDeadzone(min=0.2)""
            }
        ]
    }
    #endregion
";
}
