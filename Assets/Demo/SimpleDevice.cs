using System.Linq;
using ISX;
using ISX.Utilities;
using UnityEngine;

public struct MyDeviceState : IInputStateTypeInfo
{
    [InputControl(name = "button1", template = "Button", bit = 0)]
    public int buttons;

    [InputControl(template = "Axis")]
    public float axis1;

    public FourCC GetFormat()
    {
        return new FourCC('M', 'Y', 'D', 'V');
    }
}

[InputState(typeof(MyDeviceState))]
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    public ButtonControl button1 { get; private set; }
    public AxisControl axis1 { get; private set; }

    public static MyDevice current { get; private set; }

    protected override void FinishSetup(InputControlSetup setup)
    {
        button1 = setup.GetControl<ButtonControl>(this, "button1");
        axis1 = setup.GetControl<AxisControl>(this, "axis1");
        base.FinishSetup(setup);
    }

    public override void MakeCurrent()
    {
        base.MakeCurrent();
        current = this;
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
}

[ExecuteInEditMode]
public class SimpleDevice : MonoBehaviour
{
    public void OnEnable()
    {
        InputSystem.RegisterTemplate<MyDevice>();

        var device = InputSystem.devices.FirstOrDefault(x => x is MyDevice);
        if (device == null)
            InputSystem.AddDevice("MyDevice");
    }
}
