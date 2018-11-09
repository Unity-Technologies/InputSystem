using System.Linq;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine;
using UnityEngine.Experimental.Input.Layouts;

public struct MyDeviceState : IInputStateTypeInfo
{
    [InputControl(name = "button1", layout = "Button", bit = 0)]
    public int buttons;

    [InputControl(layout = "Axis")]
    public float axis1;

    public FourCC GetFormat()
    {
        return new FourCC('M', 'Y', 'D', 'V');
    }
}

[InputControlLayout(stateType = typeof(MyDeviceState))]
//[InputPlugin]
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    public ButtonControl button1 { get; private set; }
    public AxisControl axis1 { get; private set; }

    protected override void FinishSetup(InputDeviceBuilder builder)
    {
        button1 = builder.GetControl<ButtonControl>(this, "button1");
        axis1 = builder.GetControl<AxisControl>(this, "axis1");
        base.FinishSetup(builder);
    }

    public void OnUpdate(InputUpdateType updateType)
    {
        if (updateType == InputUpdateType.Dynamic)
        {
            var isButton1Pressed = Time.frameCount % 100 == 0;
            if (isButton1Pressed)
                InputSystem.QueueStateEvent(this, new MyDeviceState {buttons = 1});
            else
                InputSystem.QueueStateEvent(this, new MyDeviceState());
        }
    }

    public static void Initialize()
    {
        InputSystem.RegisterLayout<MyDevice>();

        if (!InputSystem.devices.Any(x => x is MyDevice))
            InputSystem.AddDevice<MyDevice>();
    }
}
