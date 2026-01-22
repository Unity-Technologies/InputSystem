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

The following table details the properties that a Control item can have. These can be set as properties on [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html), as properties on the Control in JSON, or through methods on [`InputControlLayout.Builder.ControlBuilder`](../api/UnityEngine.InputSystem.Layouts.InputControlLayout.Builder.ControlBuilder.html).

|Property|Description|
|--------|-----------|
|[`name`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_name)|Name of the Control.<br>By default, this is the name of the field/property that [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html) is applied to.|
|[`displayName`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_displayName)|Display name of the Control (for use in UI strings).|
|[`shortDisplayName`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_shortDisplayName)|Short display name of the Control (for use in UI strings).|
|[`layout`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_layout)|Layout to use for the Control.|
|[`variants`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_variants)|Variants of the Control.|
|[`aliases`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_aliases)|Aliases for the Control. These are alternative names the Control can be referred by.|
|[`usages`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_usages)|[Usages](controls.md#control-usages) of the Control.|
|[`offset`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_offset)|The byte offset at which the state for the Control is found.|
|[`bit`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_bit)|The bit offset at which the state of the Control is found within its byte.|
|[`sizeInBits`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_sizeInBits)|The total size of the Control's state, in bits.|
|[`arraySize`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_arraySize)|If this is set to a non-zero value, the system will create an array of Controls of this size.|
|[`parameters`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_parameters)|Any parameters to be passed to the Control. The system will apply these to any fields the Control type might have, such as [`AxisControl.scaleFactor`](../api/UnityEngine.InputSystem.Controls.AxisControl.html#UnityEngine_InputSystem_Controls_AxisControl_scaleFactor).|
|[`processors`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_processors)|[Processors](Processors.md) to apply to the Control.|
|[`noisy`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_noisy)|Whether the Control is to be considered [noisy](controls.md#noisy-controls).|
|[`synthetic`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_synthetic)|Whether the Control is to be considered [synthetic](controls.md#synthetic-controls).|
|[`defaultState`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_defaultState)|Default initial value of the state __memory__ Control.|
|[`useStateFrom`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_useStateFrom)|For [synthetic](controls.md#synthetic-controls) Controls, used to synthesize Control state.|
|[`minValue`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_minValue)|The minimum value the Control can report. Used for evaluating [Control magnitude](controls.md#control-actuation).|
|[`maxValue`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_maxValue)|The maximum value the Control can report. Used for evaluating [Control magnitude](controls.md#control-actuation).|
|[`dontReset`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html#UnityEngine_InputSystem_Layouts_InputControlAttribute_dontReset)|When a device ["soft" reset](Devices.md#device-resets) is performed, the state of this control will not be reset. This is useful for controls such as pointer positions which should not go to `(0,0)` on a reset. When a "hard" reset is performed, the control will still be reset to its default value.|
