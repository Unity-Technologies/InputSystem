---
uid: input-system-step-2-device-class
---

# Step 2 The Device class

Next, you need a class derived from one of the [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) base classes. You can either base your Device directly on [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice), or you can pick a more specific Device type, like [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad).

This example assumes that your Device doesn't fit into any of the existing Device classes, so it derives directly from [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice).

```CSharp
// InputControlLayoutAttribute attribute is only necessary if you want
// to override the default behavior that occurs when you register your Device
// as a layout.
// The most common use of InputControlLayoutAttribute is to direct the system
// to a custom "state struct" through the `stateType` property. See below for details.
[InputControlLayout(displayName = "My Device", stateType = typeof(MyDeviceState))]
public class MyDevice : InputDevice
{
    // In the state struct, you added two Controls that you now want to
    // surface on the Device, for convenience. The Controls
    // get added to the Device either way. When you expose them as properties,
    // it is easier to get to the Controls in code.

    public ButtonControl button { get; private set; }
    public AxisControl axis { get; private set; }

    // The Input System calls this method after it constructs the Device,
    // but before it adds the device to the system. Do any last-minute setup
    // here.
    protected override void FinishSetup()
    {
        base.FinishSetup();

        // NOTE: The Input System creates the Controls automatically.
        //       This is why don't do `new` here but rather just look
        //       the Controls up.
        button = GetChildControl<ButtonControl>("button");
        axis = GetChildControl<AxisControl>("axis");
    }
}
```
