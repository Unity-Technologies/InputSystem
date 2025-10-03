---
uid: input-system-layouts
---
# Layouts

Layouts are the central mechanism by which the Input System learns about types of Input Devices and Input Controls. Each layout represents a specific composition of Input Controls. By matching the description of a Device to a layout, the Input System is able to create the correct type of Device and interpret the incoming input data correctly.

> [!NOTE]
> Layouts are an advanced, mostly internal feature of the Input System. Knowledge of the layout system is mostly useful if you want to support custom Devices or change the behavior of existing Devices.

A layout describes a memory format for input, and the Input Controls to build in order to read and write data to or from that memory.

The Input System ships with a large set of layouts for common Control types and common Devices. For other Device types, the system automatically generates layouts based on the Device description that the Device's interface reports.

Open the Input Debugger to browse the set of currently available layouts. A layout has two primary functions:

* Describe a certain memory layout containing input data.
* Assign names, structure, and meaning to the Controls operating on the data.

A layout can either be for a Control on a Device (for example, `Stick`), or for a Device itself (that is, anything based on [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice)).

The Input System only loads layouts when they are needed (usually, when creating a new Device). To manually load a layout, you can use [`InputSystem.LoadLayout`](xref:UnityEngine.InputSystem.InputSystem.LoadLayout(System.String)). This returns an [`InputControlLayout`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout) instance, which contains the final, fully merged (that is, containing any information inherited from base layouts and/or affected by layout overrides) structure of the layout.

You can register new layouts through [`InputSystem.RegisterLayout`](xref:UnityEngine.InputSystem.InputSystem.RegisterLayout(System.String,System.String,System.Nullable{UnityEngine.InputSystem.Layouts.InputDeviceMatcher})).

## Layout formats

You can add new layouts layouts in one of three ways.

1. Represented by C# structs and classes.
2. In JSON format.
3. Built on the fly at runtime using layout builders.

### Layout from type

In its most basic form, a layout can be expressed by a C# class derived from:

* [`InputControl`](xref:UnityEngine.InputSystem.InputControl) for a Control layout.
* [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) for a Device layout.

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

You can then register the layout with [`InputSystem.RegisterLayout`](xref:UnityEngine.InputSystem.InputSystem.RegisterLayout(System.String,System.String,System.Nullable{UnityEngine.InputSystem.Layouts.InputDeviceMatcher})). This works the same for Control and for Device layouts.

```CSharp
// Note: This should generally be done from InitializeOnLoad/
// RuntimeInitializeOnLoad code.
InputSystem.RegisterLayout<MyDevice>();
```

When the layout is instantiated, the system looks at every field and property defined directly in the type to potentially turn it into one or more [Control items](#control-items).

1. If the field or property is annotated with [`InputControlAttribute`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute), the system applies the attribute's properties to the Control item. Some special defaults apply in this case:
    * If no [`offset`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.offset) is set, and the attribute is applied to a field, [`offset`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.offset) defaults to the offset of the field.
    * If no [`name`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.name) is set, it defaults to the name of the property/field.
    * If no [`layout`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.layout) is set, the system infers it from the type of the field/property.
2. If the field or property has a struct type which implements [`IInputStateTypeInfo`](xref:UnityEngine.InputSystem.LowLevel.IInputStateTypeInfo), the field is considered to be an embedded [state struct](#using-a-state-structure) and the system recurses into the field or property to gather Controls from it.
3. Otherwise, if the type of the field or property is based on [`InputControl`](xref:UnityEngine.InputSystem.InputControl), the system adds a [Control item](#control-items) similar to case 1, where the member is annotated with [`InputControlAttribute`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute).

#### Using a state structure

When you implement support for a new Input Device, there's usually an existing data format in which the Input System receives input for the Device. The easiest way to add support for the data format is to describe it with a C# struct annotated with [`InputControlAttribute`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute).

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

You can also create a layout from a JSON string that contains the same information. This is mostly useful if you want to be able to store and transfer layout information separate from your code - for instance, if you want to be able to add support for new Devices dynamically without making a new build of your application. You can use [`InputControlLayout.ToJson()`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout.ToJson) and [`InputControlLayout.FromJson()`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout.FromJson(System.String)) to convert layouts to and from the format.

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

### Generated layouts

Finally, the Input System can also build layouts on the fly in code. This is useful for Device interfaces such as [HID](xref:input-system-hid) that supply descriptive information for each Device.

To build layouts dynamically in code, you can use the [`InputControlLayout.Builder`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout.Builder) API.

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
* For layouts created in code using [`InputControlLayout.Builder`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout.Builder), you can specify a base layout using [`InputControlLayout.Builder.Extend()`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.Extend(System.String)).

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

The following table details the properties that a Control item can have. These can be set as properties on [`InputControlAttribute`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute), as properties on the Control in JSON, or through methods on [`InputControlLayout.Builder.ControlBuilder`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.ControlBuilder).

|Property|Description|
|--------|-----------|
|[`name`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.name)|Name of the Control.<br>By default, this is the name of the field/property that [`InputControlAttribute`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute) is applied to.|
|[`displayName`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.displayName)|Display name of the Control (for use in UI strings).|
|[`shortDisplayName`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.shortDisplayName)|Short display name of the Control (for use in UI strings).|
|[`layout`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.layout)|Layout to use for the Control.|
|[`variants`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.variants)|Variants of the Control.|
|[`aliases`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.aliases)|Aliases for the Control. These are alternative names the Control can be referred by.|
|[`usages`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.usages)|[Usages](xref:input-system-controls#control-usages) of the Control.|
|[`offset`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.offset)|The byte offset at which the state for the Control is found.|
|[`bit`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.bit)|The bit offset at which the state of the Control is found within its byte.|
|[`sizeInBits`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.sizeInBits)|The total size of the Control's state, in bits.|
|[`arraySize`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.arraySize)|If this is set to a non-zero value, the system will create an array of Controls of this size.|
|[`parameters`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.parameters)|Any parameters to be passed to the Control. The system will apply these to any fields the Control type might have, such as [`AxisControl.scaleFactor`](xref:UnityEngine.InputSystem.Controls.AxisControl.scaleFactor).|
|[`processors`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.processors)|[Processors](UsingProcessors.md) to apply to the Control.|
|[`noisy`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.noisy)|Whether the Control is to be considered [noisy](xref:input-system-controls#noisy-controls).|
|[`synthetic`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.synthetic)|Whether the Control is to be considered [synthetic](xref:input-system-controls#synthetic-controls).|
|[`defaultState`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.defaultState)|Default initial value of the state __memory__ Control.|
|[`useStateFrom`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.useStateFrom)|For [synthetic](xref:input-system-controls#synthetic-controls) Controls, used to synthesize Control state.|
|[`minValue`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.minValue)|The minimum value the Control can report. Used for evaluating [Control magnitude](xref:input-system-controls#control-actuation).|
|[`maxValue`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.maxValue)|The maximum value the Control can report. Used for evaluating [Control magnitude](xref:input-system-controls#control-actuation).|
|[`dontReset`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute.dontReset)|When a device ["soft" reset](xref:input-system-devices#device-resets) is performed, the state of this control will not be reset. This is useful for controls such as pointer positions which should not go to `(0,0)` on a reset. When a "hard" reset is performed, the control will still be reset to its default value.|

## Layout overrides

You can non-destructively change aspects of an existing layout using layout overrides. You can call [`InputSystem.RegisterLayoutOverride`](xref:UnityEngine.InputSystem.InputSystem.RegisterLayoutOverride(System.String,System.String)) to register a layout as an override of its [base layout](#layout-inheritance). The system then adds any property present in the override to the base layout or to existing properties.

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

## Precompiled layouts

Building a device at runtime from an [`InputControlLayout`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout) is a slow process. The layout instance itself has to be built (which might involve reflection) and then interpreted in order to put the final [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) instance together. This process usually involves the loading of multiple [`InputControlLayout`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout) instances, each of which might be the result of merging multiple layouts together (if the layout involves [inheritance](#layout-inheritance) or [overrides](#layout-overrides)).

You can speed up this process up by "baking" the final form of a layout into a "precompiled layout". A precompiled layout is generated C# code that, when run, will build the corresponding device without relying on loading and interpreting an [`InputControlLayout`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout). Aside from running faster, this will also create far less garbage and will not involve C# reflection (which generally causes runtime overhead by inflating the number of objects internally kept by the C# runtime).

> [!NOTE]
> Precompiled layouts must be device layouts. It is not possible to precompile the layout for an [`InputControl`](xref:UnityEngine.InputSystem.InputControl).

### Creating a precompiled layout

The first step in setting up a precompiled layout is to generate it.

To generate a layout:

1. Open the [Input Debugger](xref:input-system-debugging).
2. Navigate to the layout you want to precompile within the **Layouts** branch and right-click it.
3. Select **Generate Precompiled Layout**.

    Unity will ask you where to store the generated code. Pick a directory in your project, enter a file name, and click **Save**.

Once generated, you can register the precompiled layout with the Input System using [`InputSystem.RegisterPrecompiledLayout`](xref:UnityEngine.InputSystem.InputSystem.RegisterPrecompiledLayout``1(System.String)). The method expects a string argument containing metadata for the precompiled layout. This string is automatically emitted as a `const` inside the generated class.

 ```CSharp
 InputSystem.RegisterPrecompiledLayout<MyPrecompiledDevice>(MyPrecompiledDevice.metadata);
 ```

> [!IMPORTANT]
> It is very important that this method is called with all relevant layout registrations being in the same state as at the time the layout was precompiled. There is no internal check whether the precompiled layout will still generate an identical result to the non-precompiled version.

Once registered, a precompiled layout is automatically used whenever the layout that the precompiled layout is based on is instantiated.

```CSharp
// Let's assume you have a custom device class.
public class MyDevice : InputDevice
{
    // Setters for your control getters need to have at least `protected`
    // or `internal` access so the precompiled version can use them.
    [InputControl]
    public ButtonControl button { get; protected set; }

    // This method will *NOT* be invoked by the precompiled version. Instead, all the lookups
    // performed here will get hardcoded into the generated C# code.
    protected override void FinishSetup()
    {
        base.FinishSetup();

        button = GetChildControl<ButtonControl>("button1");
    }
}

// You register the device as a layout somewhere during startup.
InputSystem.RegisterLayout<MyDevice>();

// And you register a precompiled version of it then as well.
InputSystem.RegisterPrecompiledLayout<PrecompiledMyDevice>(PrecompiledMyDevice.metadata);

// Then the following will implicitly use the precompiled version.
InputSystem.AddDevice<MyDevice>();
```

A precompiled layout will automatically be unregistered in the following cases:

* A [layout override](#layout-overrides) is applied to one of the layouts used by the precompiled Device. This also extends to [controls](xref:input-system-controls) used by the Device.
* A layout with the same name as one of the layouts used by the precompiled Device is registered (which replaces the layout already registered under the name).
* A [processor](UsingProcessors.md) is registered that replaces a processor used by the precompiled Device.

This causes the Input System to fall back to the non-precompiled version of the layout. Note also that a precompiled layout will not be used for layouts [derived](#layout-inheritance) from the layout the precompiled version is based on. In the example above, if someone derives a new layout from `MyDevice`, the precompiled version is unaffected (it will not be unregistered) but is also not used for the newly created type of device.

```CSharp
// Let's constinue from the example above and assume that sometime
// later, someone replaces the built-in button with an extended version.
InputSystem.RegisterLayout<ExtendedButtonControl>("Button");

// PrecompiledMyDevice has implicitly been removed now, because the ButtonControl it uses
// has now been replaced with ExtendedButtonControl.
```

If needed, you can add `#if` checks to the generated code, if needed. The code generator will scan the start of an existing file for a line starting with `#if` and, if found, preserve it in newly generated code and generate a corresponding `#endif` at the end of the file. Similarly, you can change the generated class from `public` to `internal` and the modifier will be preserved when regenerating the class. Finally, you can also modify the namespace in the generated file with the change being preserved.

The generated class is marked as `partial`, which means you can add additional overloads and other code by  having a parallel, `partial` class definition.

```CSharp
// The next line will be preserved when regenerating the precompiled layout. A
// corresponding #endif will be emitted at the end of the file.
#if UNITY_EDITOR || UNITY_STANDALONE

// If you change the namespace to a different one, the name of the namespace will be
// preserved when you regenerate the precompiled layout.
namepace MyNamespace
{
    // If you change `public` to `internal`, the change will be preserved
    // when regenerating the precompiled layout.
    public partial class PrecompiledMyDevice : MyDevice
    {
        //...
```

The namespace of the generated layout will correspond to the
