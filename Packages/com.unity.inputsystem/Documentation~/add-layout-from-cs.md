---
uid: input-system-add-layout-from-cs
---

# Add a layout from C# 

In its most basic form, a layout can be expressed by a C# class derived from:

* [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html) for a Control layout.
* [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) for a Device layout.

```CSharp
// The InputControlLayout attribute is not strictly necessary here.
// However, you can use it to set additional properties (such as
// a custom display name for the layout).
[InputControlLayout]
public class MyDevice : InputDevice
{
    public AxisControl axis { get; private set; }
    public ButtonControl button { get; private set; }

    protected override void FinishSetup(InputDeviceBuilder builder)
    {
        base.FinishSetup(builder);

        axis = builder.GetControl<AxisControl>("axis");
        button = builder.GetControl<ButtonControl>("button");
    }
}
```

You can then register the layout with [`InputSystem.RegisterLayout`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterLayout_System_String_System_String_System_Nullable_UnityEngine_InputSystem_Layouts_InputDeviceMatcher__). This works the same for Control and for Device layouts.

```CSharp
// Note: This should generally be done from InitializeOnLoad/
// RuntimeInitializeOnLoad code.
InputSystem.RegisterLayout<MyDevice>();
```

When the layout is instantiated, the system looks at every field and property defined directly in the type to potentially turn it into one or more [Control items](#control-items).

1. If the field or property is annotated with [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html), the system applies the attribute's properties to the Control item. Some special defaults apply in this case:
    * If no [`offset`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_offset) is set, and the attribute is applied to a field, [`offset`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_offset) defaults to the offset of the field.
    * If no [`name`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_name) is set, it defaults to the name of the property/field.
    * If no [`layout`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_layout) is set, the system infers it from the type of the field/property.
2. If the field or property has a struct type which implements [`IInputStateTypeInfo`](../api/UnityEngine.InputSystem.LowLevel.IInputStateTypeInfo.html), the field is considered to be an embedded [state struct](#using-a-state-structure) and the system recurses into the field or property to gather Controls from it.
3. Otherwise, if the type of the field or property is based on [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html), the system adds a [Control item](#control-items) similar to case 1, where the member is annotated with [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html).

## Using a state structure

When you implement support for a new Input Device, there's usually an existing data format in which the Input System receives input for the Device. The easiest way to add support for the data format is to describe it with a C# struct annotated with [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html).

```CSharp
public struct MyDeviceState : IInputStateTypeInfo
{
    public FourCC format => new FourCC('M', 'D', 'E', 'V');

    [InputControl(name = "button1", layout = "Button", bit = 0)]
    [InputControl(name = "button2", layout = "Button", bit = 1)]
    [InputControl(name = "dpad", layout = "Dpad", bit = 2, sizeInBits = 4)]
    [InputControl(name = "dpad/up", bit = 2)]
    [InputControl(name = "dpad/down", bit = 3)]
    [InputControl(name = "dpad/left", bit = 4)]
    [InputControl(name = "dpad/right", bit = 5)]
    public int buttons;

    [InputControl(layout = "Stick")]
    public Vector2 stick;

    [InputControl(layout = "Axis")] // Automatically converts from byte to float.
    public byte trigger;
}

// The Device must be directed to the state struct we have created.
[InputControlLayout(stateType = typeof(MyDeviceState)]
public class MyDevice : InputDevice
{
}
```