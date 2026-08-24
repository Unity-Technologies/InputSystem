using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

#region mydevice
/// <summary>
/// Example state struct for a custom device with a single deadzoned axis control.
/// </summary>
public struct MyDeviceState : IInputStateTypeInfo
{
    /// <summary>
    /// The memory format identifier for this state struct.
    /// </summary>
    public FourCC format => new FourCC('M', 'Y', 'D', 'V');

    // Add an axis deadzone to the Control to ignore values
    // smaller then 0.2, as our Control does not have a stable
    // resting position.
    /// <summary>
    /// The axis control's raw value.
    /// </summary>
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
