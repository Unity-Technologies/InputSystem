using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

public struct MyDeviceState : IInputStateTypeInfo
{
    [InputControl(name = "button1", layout = "Button", bit = 0)]
    public int buttons;

    [InputControl(layout = "Axis")]
    public float axis1;

    public FourCC format => new FourCC('M', 'Y', 'D', 'V');
}

[InputControlLayout(stateType = typeof(MyDeviceState))]
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    public ButtonControl button1 { get; private set; }
    public AxisControl axis1 { get; private set; }

    protected override void FinishSetup()
    {
        button1 = GetChildControl<ButtonControl>("button1");
        axis1 = GetChildControl<AxisControl>("axis1");
        base.FinishSetup();
    }

    public void OnUpdate()
    {
        var isButton1Pressed = Time.frameCount % 100 == 0;
        if (isButton1Pressed)
            InputSystem.QueueStateEvent(this, new MyDeviceState {buttons = 1});
        else
            InputSystem.QueueStateEvent(this, new MyDeviceState());
    }

    public static void Initialize()
    {
        InputSystem.RegisterLayout<MyDevice>();

        if (!InputSystem.devices.Any(x => x is MyDevice))
            InputSystem.AddDevice<MyDevice>();
    }
}
