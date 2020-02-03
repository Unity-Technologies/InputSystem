# Layouts

Layouts are the central mechanism by which the Input System learns about types of Input Devices and Input Controls. Each layout represents a specific composition of Input Controls. By matching the description of a Device to a layout, the Input System is able to create the correct type of Device and interpret the incoming input data correctly.

>__Note__: Layouts are an advanced, mostly internal feature of the Input System. Knowledge of the layout system is mostly useful if you want to support custom Devices or change the behavior of existing Devices.

A layout describes a memory format for input, and the Input Controls to build in order to read and write data to or from that memory.

The Input System ships with a large set of layouts for common Control types and common Devices. For other Device types, the system automatically generates layouts based on the Device description that the Device's interface reports.

You can browse the set of currently understood layouts from the Input Debugger.

![Layouts in Debugger](Images/LayoutsInDebugger.png)

A layout has two primary functions:

* Describe a certain memory layout containing input data.
* Assign names, structure, and meaning to the Controls operating on the data.

A layout can either be for a Control on a Device (for example, `Stick`), or for a Device itself (that is, anything based on [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html)).

The Input System only loads layouts when they are needed (usually, when creating a new Device). To manually load a layout, you can use [`InputSystem.LoadLayout`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_LoadLayout_System_String_). This returns an [`InputControlLayout`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.html) instance, which contains the final, fully merged (that is, containing any information inherited from base layouts and/or affected by layout overrides) structure of the layout.

You can register new layouts through [`InputSystem.RegisterLayout`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterLayout_System_String_System_String_System_Nullable_UnityEngine_InputSystem_Layouts_InputDeviceMatcher__).

## Layout formats

You can add new layouts layouts in one of three ways.

1. Represented by C# structs and classes.
2. In JSON format.
3. Built on the fly at runtime using layout builders.

### Layout from type

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

#### Using a state structure

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

### Layout from JSON

You can also create a layout from a JSON string that contains the same information. This is mostly useful if you want to be able to store and transfer layout information separate from your code - for instance, if you want to be able to add support for new Devices dynamically without making a new build of your application. You can use [`InputControlLayout.ToJson()`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.html#UnityEngine_InputSystem_Layouts_InputControlLayout_ToJson) and [`InputControlLayout.FromJson()`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.html#UnityEngine_InputSystem_Layouts_InputControlLayout_FromJson_System_String_) to convert layouts to and from the format.

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

### Layout builders

Finally, the Input System can also build layouts on the fly in code. This is useful for Device interfaces such as [HID](HID.md) that supply descriptive information for each Device.

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

## Layout inheritance

You can derive a layout from an existing layout. This process is based on merging the information from the derived layout on top of the information that the base layout contains.

* For layouts defined as types, the base layout is the layout of the base type (if any).
* For layouts defined in JSON, you can specify the base layout in the `extends` property of the root node.
* For layouts created in code using [`InputControlLayout.Builder`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.html), you can specify a base layout using [`InputControlLayout.Builder.Extend()`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.html#UnityEngine_InputSystem_Layouts_InputControlLayout_Builder_Extend_System_String_).

## Control items

Each layout is comprised of zero or more Control items. Each item either describes a new Control, or modifies the properties of an existing Control. The latter can also reach down into the hierarchy and modify properties of a Control added implicitly as a child by another item.

```CSharp
    // Add a dpad Control.
    [InputControl(layout = "Dpad")]
    // And now modify the properties of the "up" Control that was added by the
    // "Dpad" layout above.
    [InputControl(name = "dpad/up", displayName = "DPADUP")]
    public int buttons;
```

The following table details the properties that a Control item can have. These can be set as properties on [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html), as properties on the Control in JSON, or through methods on [`InputControlLayout.Builder.ControlBuilder`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.ControlBuilder.html).

|Property|Description|
|--------|-----------|
|[`name`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_name)|Name of the Control.<br>By default, this is the name of the field/property that [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html) is applied to.|
|[`displayName`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_displayName)|Display name of the Control (for use in UI strings).|
|[`shortDisplayName`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_shortDisplayName)|Short display name of the Control (for use in UI strings).|
|[`layout`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_layout)|Layout to use for the Control.|
|[`variants`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_variants)|Variants of the Control.|
|[`aliases`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_aliases)|Aliases for the Control. These are alternative names the Control can be referred by.|
|[`usages`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_usages)|[Usages](Controls.md#control-usages) of the Control.|
|[`offset`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_offset)|The byte offset at which the state for the Control is found.|
|[`bit`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_bit)|The bit offset at which the state of the Control is found within its byte.|
|[`sizeInBits`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_sizeInBits)|The total size of the Control's state, in bits.|
|[`arraySize`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_arraySize)|If this is set to a non-zero value, the system will create an array of Controls of this size.|
|[`parameters`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_parameters)|Any parameters to be passed to the Control. The system will apply these to any fields the Control type might have, such as [`AxisControl.scaleFactor`](../api/UnityEngine.InputSystem.Controls.AxisControl.html#UnityEngine_InputSystem_Controls_AxisControl_scaleFactor).|
|[`processors`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_processors)|[Processors](Processors.md) to apply to the Control.|
|[`noisy`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_noisy)|Whether the Control is to be considered [noisy](Controls.md#noisy-controls).|
|[`synthetic`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_synthetic)|Whether the Control is to be considered [synthetic](Controls.md#synthetic-controls).|
|[`defaultState`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_defaultState)|Default initial value of the state __memory__ Control.|
|[`useStateFrom`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_useStateFrom)|For [synthetic](Controls.md#synthetic-controls) Controls, used to synthesize Control state.|
|[`minValue`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_minValue)|The minimum value the Control can report. Used for evaluating [Control magnitude](Controls.md#control-actuation).|
|[`maxValue`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_maxValue)|The maximum value the Control can report. Used for evaluating [Control magnitude](Controls.md#control-actuation).|

## Layout overrides

You can non-destructively change aspects of an existing layout using layout overrides. You can call [`InputSystem.RegisterLayoutOverride`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterLayoutOverride_System_String_System_String_) to register a layout as an override of its [base layout](#layout-inheritance). The system then adds any property present in the override to the base layout or to existing properties.

```CSharp
// Add an extra Control to the "Mouse" layout
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
