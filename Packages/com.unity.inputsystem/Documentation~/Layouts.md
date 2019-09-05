>NOTE: Layouts are an advanced, mostly internal feature of the input system. It is not necessary to understand this feature to use the input system. Knowledge of the layout system is mostly useful when wanting to support custom devices or when wanting to modify the behavior of existing devices.

# Layouts

A layout describes a memory format and the controls to build in order to read and write data to/from memory in the given format.

Layouts are the central mechanism by which the input system learns about types of input devices and input controls. Each layout represents a specific composition of input controls. By matching the description of a device to a layout, the input system is able to create the correct type of device and interpret the incoming input data correctly.

The input system ships with a big set of layouts for common control types, as well as for common devices. For other device types, layouts are automatically generated on the fly based on the device description reported by the device's interface.

The set of currently understood layouts can be browsed from the input debugger.

![Layouts in Debugger](Images/LayoutsInDebugger.png)

A layout has two primary functions:

* Describe a certain memory layout containing input data.
* Assign names, structure, and meaning to the controls operating on the data.

A layout can either be for a control on a device (e.g. `Stick`) or for a device itself (i.e. something based on [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html)).

Layouts are only loaded when needed -- usually when creating a new device. A layout can be manually loaded through [`InputSystem.LoadLayout`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_LoadLayout_System_String_). This returns an [`InputControlLayout`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.html) instance containing the fully merged (i.e. containing any information inherited from based layouts and/or affected by layout overrides), final structure of the layout.

New layouts can be registered through [`InputSystem.RegisterLayout`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterLayout_System_String_System_String_System_Nullable_UnityEngine_InputSystem_Layouts_InputDeviceMatcher__).

## Layout Formats

New layouts can be added in one of three ways.

1. Represented by C# structs and classes.
2. In JSON format.
3. Built-on the fly at runtime using what's called "layout builders".

### Layout from Type

In its most basic form, a layout can simply be a C# class (derived from [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html) for a control layout or derived from [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) for a device layout).

```CSharp
// The InputControlLayout attribute is not strictly necessary here.
// However, it can be used to set additional properties (such as
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

The layout can be registered with [`InputSystem.RegisterLayout`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterLayout_System_String_System_String_System_Nullable_UnityEngine_InputSystem_Layouts_InputDeviceMatcher__). This works the same for control and for device layouts.

```CSharp
// NOTE: This should generally be done from InitializeOnLoad/
// RuntimeInitializeOnLoad code.
InputSystem.RegisterLayout<MyDevice>();
```

When the layout is instantiated, the system will look at every field and property defined directly in the type to potentially turn it into one or more [control items](#control-items).

1. If the field or property is annotated with [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html), the attribute's properties will be applied to the control item. Some special defaults apply in this case:
    * If no [`offset`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_offset) is set and the attribute is applied to a field, [`offset`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_offset) defaults to the offset of the field.
    * If no [`name`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_name) is set, it defaults to the name of the property/field.
    * If no [`layout`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_layout) is set, it is inferred from the type of the field/property.
2. If the field or property has a struct type which implements [`IInputStateTypeInfo`](../api/UnityEngine.InputSystem.LowLevel.IInputStateTypeInfo.html), the field is considered to be an embedded [state struct](#using-a-state-structure) and the field/property will be recursed into to gather controls from it.
3. Otherwise, if the type of the field or property is based on [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html), a  [control item](#control-items) will be added similar to case 1) when the member is annotated with [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html).

#### Using a State Structure

When implementing support for a new input device, there usually is an existing data format in which input for the device is received. The easiest way to add support for the given data format is to describe it with a C# struct annotated with [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html).

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

// The device must be directed to the state struct we have created.
[InputControlLayout(stateType = typeof(MyDeviceState)]
public class MyDevice : InputDevice
{
}
```

### Layout from JSON

You can also create a layout from a JSON string containing the same information. This is mostly useful if you want to be able to store/transfer layout information separate from your code - for instance if you want to be able to add support for new devices dynamically without making a new build of your game. You can use [`InputControlLayout.ToJson()`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.html#UnityEngine_InputSystem_Layouts_InputControlLayout_ToJson) and [`InputControlLayout.FromJson()`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.html#UnityEngine_InputSystem_Layouts_InputControlLayout_FromJson_System_String_) to convert layouts to and from the format.

The same layout as above looks like this in JSON format:

```
{
    "name": "MyDevice",
    "format": "MDEV",
    "controls": [
        {
            "name": "button1",
            "layout": "Button",
            "offset": 0,
            "bit": 0,
        },
        {
            "name": "button2",
            "layout": "Button",
            "offset": 0,
            "bit": 1,
        },
        {
            "name": "dpad",
            "layout": "Dpad",
            "offset": 0,
            "bit": 2,
            "sizeInBits": 4,
        },
        {
            "name": "dpad/up",
            "offset": -1,
            "bit": 2,
        },
        {
            "name": "dpad/down",
            "offset": -1,
            "bit": 3,
        },
        {
            "name": "dpad/left",
            "offset": -1,
            "bit": 4,
        },
        {
            "name": "dpad/right",
            "offset": -1,
            "bit": 5,
        },
        {
            "name": "stick",
            "layout": "Stick",
            "offset": 4,
            "format": "VEC2",
        },
        {
            "name": "trigger",
            "layout": "Axis",
            "offset": 12,
            "format": "BYTE",

        }
    ]
}
```

### Layout Builders

Finally, layouts can also be built on the fly in code. This is useful for device interfaces such as [HID](https://www.usb.org/hid) that supply descriptive information for each device.

To build layouts dynamically in code, you can use the [`InputControlLayout.Builder`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.html) API.

Here's the same layout from the previous examples constructed programmatically:

```
var builder = new InputControlLayout.Builder()
    .WithName("MyDevice")
    .WithFormat("MDEV");

builder.AddControl("button1")
    .WithLayout("Button")
    .WithByteOffset(0)
    .WithBitOffset(0);

builder.AddControl("button2")
    .WithLayout("Button")
    .WithByteOffset(0)
    .WithBitOffset(1);

builder.AddControl("dpad")
    .WithLayout("Dpad")
    .WithByteOffset(0)
    .WithBitOffset(2)
    .WithSizeInBits(4);

builder.AddControl("dpad/up")
    .WithByteOffset(-1)
    .WithBitOffset(2);

builder.AddControl("dpad/down")
    .WithByteOffset(-1)
    .WithBitOffset(3);

builder.AddControl("dpad/left")
    .WithByteOffset(-1)
    .WithBitOffset(4);

builder.AddControl("dpad/right")
    .WithByteOffset(-1)
    .WithBitOffset(5);

builder.AddControl("stick")
    .WithLayout("Stick")
    .WithByteOffset(4)
    .WithFormat("VEC2");

builder.AddControl("trigger")
    .WithLayout("Axis")
    .WithByteOffset(12)
    .WithFormat("BYTE");

var layout = builder.Build();
```

## Layout Inheritance

A layout can be derived from an existing layout. This process is based on a simple process of *merging* the information from a derived layout on top of the information present in the derived layout.

* For layouts defined as types, the base layout is the layout of the base type (if any).
* For layouts defined in JSON, the base layout can be specified in the `extends` property of the root node.
* For layouts created in code using [`InputControlLayout.Builder`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.html), you can specify a base layout using [`InputControlLayout.Builder.Extend()`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.html#UnityEngine_InputSystem_Layouts_InputControlLayout_Builder_Extend_System_String_).

## Control Items

Each layout is comprised of zero or more control items. Each item either describes a new control or modifies the properties of an existing control. The latter can also reach down into the hierarchy and modify properties of a control added implicitly as a child by another item.

```CSharp
    // Add a dpad control.
    [InputControl(layout = "Dpad")]
    // And now modify the properties of the "up" control that was added by the
    // "Dpad" layout above.
    [InputControl(name = "dpad/up", displayName = "DPADUP")]
    public int buttons;
```

The following table details the properties that a control item can have. These can be set either as properties on [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html), as properties on the control in JSON, or through methods on [`InputControlLayout.Builder.ControlBuilder`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.ControlBuilder.html).

|Property|Description|
|--------|-----------|
|[`name`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_name)|Name of the control.<br>Defaults to the name of the field/property that [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html) is applied to.|
|[`displayName`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_displayName)|Display name of the control (for use in UI strings).|
|[`shortDisplayName`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_shortDisplayName)|Short display name of the control (for use in UI strings).|
|[`layout`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_layout)|Layout to use for the control.|
|[`variants`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_variants)|Variants of the control.|
|[`aliases`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_aliases)|Aliases for the control. These are alternative names the control can be referred to.|
|[`usages`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_usages)|[Usages](Controls.md#control-usages) of the control.|
|[`offset`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_offset)|The byte offset at which the state for the control is found.|
|[`bit`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_bit)|The bit offset at which the state for the control is found within it's byte.|
|[`sizeInBits`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_sizeInBits)|The total size of the control's state, in bits.|
|[`arraySize`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_arraySize)|If this is set to a non-zero value, will create an array of controls of this size.|
|[`parameters`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_parameters)|Any parameters to be passed to the control. These will be applied to any fields the control type may have, such as [`AxisControl.scaleFactor`](../api/UnityEngine.InputSystem.Controls.AxisControl.html#UnityEngine_InputSystem_Controls_AxisControl_scaleFactor).|
|[`processors`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_processors)|[Processors](Processors.md) to apply to the control.|
|[`noisy`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_noisy)|Whether the control is to be considered [noisy](Controls.md#noisy-controls).|
|[`synthetic`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_synthetic)|Whether the control is to be considered [synthetic](Controls.md#synthetic-controls).|
|[`defaultState`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_defaultState)|What value to initialize the state __memory__ for the control to by default.|
|[`useStateFrom`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_useStateFrom)|For [synthetic](Controls.md#synthetic-controls) controls, used to synthesize control state.|
|[`minValue`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_minValue)|The minimum value the control can report. Used for evaluating [control magnitude](Controls.md#control-actuation).|
|[`maxValue`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_maxValue)|The maximum value the control can report. Used for evaluating [control magnitude](Controls.md#control-actuation).|

## Layout Overrides

It is possible to non-destructively alter aspects of an existing layout using what's called "layout overrides". You can call [`InputSystem.RegisterLayoutOverride`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterLayoutOverride_System_String_System_String_) to register a layout as an override of it's [base layout](#layout-inheritance). Any property present in the override will then be added to the base layout (or override existing properties).

```CSharp
// Add an extra control to the "Mouse" layout
const string json = @"
    {
        ""name"" : ""Overrides"",
        ""extend"" : ""Mouse"",
        ""controls"" : [
            { ""name"" : ""extraControl"", ""layout"" : ""Button"" }
        ]
    }
";

InputSystem.RegisterLayoutOverride(json);
```
