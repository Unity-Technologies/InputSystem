    ////WIP

>NOTE: Layouts are an advanced, mostly internal feature of the input system. It is not necessary to understand this feature to use the input system. Knowledge of the layout system is mostly useful when wanting to support custom devices or when wanting to modify the behavior of existing devices.

# Layouts

A layout describes a memory format and the controls to build in order to read and write data to/from memory in the given format.

Layouts are the central mechanism by which the input system learns about types of input devices and input controls. Each layout represents a specific composition of input controls. By matching the description of a device to a layout, the input system is able to create the correct type of device and interpret the incoming input data correctly.

The set of currently understood layouts can be browsed from the input debugger.

![Layouts in Debugger](Images/LayoutsInDebugger.png)

A layout has two primary functions:

* Describe a certain memory layout containing input data.
* Assign names, structure, and meaning to the controls operating on the data.

A layout can either be for a control on a device (e.g. `Stick`) or for a device itself (i.e. something based on `InputDevice`).

Layouts are only loaded when needed -- usually when creating a new device. A layout can be manually loaded through `InputSystem.LoadLayout`. This returns an `InputControlLayout` instance containing the fully merged (i.e. containing any information inherited from based layouts and/or affected by layout overrides), final structure of the layout.

New layouts can be registered through `InputSystem.RegisterLayout`.

## Layout Formats

New layouts can be added in one of three ways.

1. Represented by C# structs and classes.
2. In JSON format.
3. Built-on the fly at runtime using what's called "layout builders".

### Layout from Type

In its most basic form, a layout can simply be a C# class (derived from `InputControl` for a control layout or derived from `InputDevice` for a device layout).

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

The layout can be registered with `InputSystem.RegisterLayout`. This works the same for control and for device layouts.

```CSharp
// NOTE: This should generally be done from InitializeOnLoad/
// RuntimeInitializeOnLoad code.
InputSystem.RegisterLayout<MyDevice>();
```

When the layout is instantiated, the system will look at every field and property defined directly in the type to potentially turn it into one or more [control items](#control-items).

1. If the field or property is annotated with `InputControlAttribute`, the attribute's properties will be applied to the control item. Some special defaults apply in this case:
    * If no `offset` is set and the attribute is applied to a field, `offset` defaults to the offset of the field.
    * If no `name` is set, it defaults to the name of the property/field.
    * If no `layout` is set, it is inferred from the type of the field/property.
2. If the field or property has a struct type which implements `IInputStateTypeInfo`, the field is considered to be an embedded [state struct](#using-a-state-structure) and the field/property will be recursed into to gather controls from it.
3. Otherwise, if the type of the field or property is based on `InputControl`, a  [control item](#control-items) will be added similar to case 1) when the member is annotated with `InputControlAttribute`.

#### Using a State Structure

When implementing support for a new input device, there usually is an existing data format in which input for the device is received. The easiest way to add support for the given data format is to describe it with a C# struct annotated with `InputControlAttribute`.

```CSharp
// It can be useful to use LayoutKind.Explicit to force explicit field offsets
// so as to not get caught by alignment/packing difference introduced by the VM.
public struct MyDeviceState : IInputStateTypeInfo
{
    public static FourCC Format => new FourCC('M', 'D', 'E', 'V');

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

    public FourCC GetFormat()
    {
        return Format;
    }
}

// The device must be directed to the state struct we have created.
[InputControlLayout(stateType = typeof(MyDeviceState)]
public class MyDevice : InputDevice
{
}
```

### Layout from JSON

TODO

### Layout Builders

Finally, layouts can also be built on the fly in code. This is useful for device interfaces such as [HID](https://www.usb.org/hid) that supply descriptive information for each device.

TODO

## Layout Inheritance

A layout can be derived from an existing layout. This process is based on a simple process of *merging* the information from a derived layout on top of the information present in the derived layout.

* For layouts defined as types, the base layout TODO
* For layouts defined in JSON, TODO
* For layouts ...

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

The following table details the properties that a control item can have. These can be set either as properties on `InputControlAttribute`, as properties on the control in JSON, or through methods on `InputControlLayout.Builder.ControlBuilder`.

|Property|Description|
|--------|-----------|
|`name`|<br><br>Defaults to the name of the field/property that `InputControlAttribute` is applied to.|
|`displayName`||
|`shortDisplayName`||
|`layout`||
|`variants`||
|`aliases`||
|`usages`||
|`offset`|The byte offset at which the state for the control is found.|
|`bit`||
|`sizeInBits`|The total size of the control's state, in bits.|
|`arraySize`||
|`parameters`||
|`processors`||
|`noisy`|Whether the control is to be considered [noisy](Controls.md#noisy-controls).|
|`synthetic`|Whether the control is to be considered [synthetic](Controls.md#synthetic-controls).|
|`defaultState`|What value to initialize the state __memory__ for the control to by default. This is not the same as a default __value__ but instead TODO.|
|`useStateFrom`||
|`minValue`||
|`maxValue`||

## Layout Overrides

It is possible to non-destructively alter aspects of an existing layout using what's called "layout overrides".

## Built-In Layouts

### Controls

|Layout|Description|
|------|-----------|
|`Stick`|A thumbstick-like controls. Based on `Vector2`. Has an `X` and a `Y` axis as well as `up`, `down`, `left`, and `right` buttons corresponding to the cardinal directions.|

### Devices

|Layout|Description|
|------|-----------|
