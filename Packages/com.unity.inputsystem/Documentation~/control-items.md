---
uid: input-system-control-items
---

# Control items

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
|[`name`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|Name of the Control.<br>By default, this is the name of the field/property that [`InputControlAttribute`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute) is applied to.|
|[`displayName`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|Display name of the Control (for use in UI strings).|
|[`shortDisplayName`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|Short display name of the Control (for use in UI strings).|
|[`layout`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|Layout to use for the Control.|
|[`variants`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|Variants of the Control.|
|[`aliases`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|Aliases for the Control. These are alternative names the Control can be referred by.|
|[`usages`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|[Usages](control-usages.md) of the Control.|
|[`offset`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|The byte offset at which the state for the Control is found.|
|[`bit`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|The bit offset at which the state of the Control is found within its byte.|
|[`sizeInBits`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|The total size of the Control's state, in bits.|
|[`arraySize`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|If this is set to a non-zero value, the system will create an array of Controls of this size.|
|[`parameters`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|Any parameters to be passed to the Control. The system will apply these to any fields the Control type might have, such as [`AxisControl.scaleFactor`](xref:UnityEngine.InputSystem.Controls.AxisControl).|
|[`processors`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|[Processors](processors.md) to apply to the Control.|
|[`noisy`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|Whether the Control is to be considered [noisy](noisy-controls.md).|
|[`synthetic`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|Whether the Control is to be considered [synthetic](synthetic-controls.md).|
|[`defaultState`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|Default initial value of the state __memory__ Control.|
|[`useStateFrom`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|For [synthetic](synthetic-controls.md) Controls, used to synthesize Control state.|
|[`minValue`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|The minimum value the Control can report. Used for evaluating [Control magnitude](control-actuation.md).|
|[`maxValue`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|The maximum value the Control can report. Used for evaluating [Control magnitude](control-actuation.md).|
|[`dontReset`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute)|When a device ["soft" reset](reset-device.md) is performed, the state of this control will not be reset. This is useful for controls such as pointer positions which should not go to `(0,0)` on a reset. When a "hard" reset is performed, the control will still be reset to its default value.|
