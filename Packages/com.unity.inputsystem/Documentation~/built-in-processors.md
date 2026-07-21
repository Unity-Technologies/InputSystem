---
uid: input-system-built-in-processors
---

# Built-in processors

The Input System package comes with a set of built-in Processors, which you can use with [bindings](bindings.md), [actions](actions.md) and [controls](controls.md).


|**Processor name**|**Description**|**Operand type**|**Parameters**|
|---|---|---|---|
|[`Clamp`](xref:UnityEngine.InputSystem.Processors.ClampProcessor)|Clamps input values to the [`min`..`max`] range.|`float`|<ul><li>`float min`</li><li>`float max`</li></ul>|
|[`Invert`](xref:UnityEngine.InputSystem.Processors.InvertProcessor)|Inverts the values from a Control (that is, multiplies the values by &minus;1).|`float`|None|
|[`InvertVector2`](xref:UnityEngine.InputSystem.Processors.InvertVector2Processor)|Inverts the values from a Control (that is, multiplies the values by &minus;1). Inverts the x-axis of the vector if `invertX` is true, and the y-axis if `invertY` is true.|`Vector2`|<ul><li>`bool invertX`</li><li>`bool invertY`</li></ul>|
|[`Invert Vector 3`](xref:UnityEngine.InputSystem.Processors.InvertVector3Processor)|Inverts the values from a Control (that is, multiplies the values by &minus;1). Inverts the x-axis of the vector if `invertX` is true, the y-axis if `invertY` is true, and the z-axis if `invertZ` is true.|`Vector3`|<ul><li>`bool invertX`</li><li>`bool invertY`</li><li>`bool invertZ`</li></ul>|
|[`Normalize`](xref:UnityEngine.InputSystem.Processors.NormalizeProcessor)|Normalizes input values in the range [`min`..`max`] to unsigned normalized form [0..1] if `min` is >= `zero`, and to signed normalized form [-1..1] if `min` < `zero`.|`float`|<ul><li>`float min`</li><li>`float max`</li><li>`float zero`</li></ul>|
|[`NormalizeVector2`](xref:UnityEngine.InputSystem.Processors.NormalizeVector2Processor)|Normalizes input vectors to be of unit length (1). This is the same as calling `Vector2.normalized`.|`Vector2`|None|
|[`NormalizeVector3`](xref:UnityEngine.InputSystem.Processors.NormalizeVector3Processor)|Normalizes input vectors to be of unit length (1). This is the same as calling `Vector3.normalized`.|`Vector3`|None|
|[`Scale`](xref:UnityEngine.InputSystem.Processors.ScaleProcessor)|Multiplies all input values by `factor`.|`float`|`float factor`|
|[`ScaleVector2`](xref:UnityEngine.InputSystem.Processors.ScaleVector2Processor)|Multiplies all input values by `x` along the x-axis and by `y` along the y-axis.|`Vector2`|<ul><li>`float x`</li><li>`float y`</li></ul>|
|[`ScaleVector3`](xref:UnityEngine.InputSystem.Processors.ScaleVector3Processor)|Multiplies all input values by `x` along the x-axis, by `y` along the y-axis, and by `z` along the z-axis.|`Vector3`|<ul><li>`float x`</li><li>`float y`</li><li>`float z`</li></ul>|
|[`AxisDeadzone`](xref:UnityEngine.InputSystem.Processors.AxisDeadzoneProcessor)|Scales the values of a Control so that any value with an absolute value smaller than `min` is 0, and any value with an absolute value larger than `max` is 1 or &minus;1.<br/><br/>Many Controls don't have a precise resting point (that is, they don't always report exactly 0 when the Control is in the center). Using the `min` value on a deadzone Processor avoids unintentional input from such Controls. Also, some Controls don't consistently report their maximum values when moving the axis all the way. Using the `max` value on a deadzone Processor ensures that you always get the maximum value in such cases.|`float`|<ul><li>`float min`</li><li>`float max`</li></ul>|
|[`StickDeadzone`](xref:UnityEngine.InputSystem.Processors.StickDeadzoneProcessor)|Scales the values of a Vector2 Control, such as a stick, so that any input vector with a magnitude smaller than `min` results in (0,0), and any input vector with a magnitude greater than `max` is normalized to length 1.<br/><br/>Many Controls don't have a precise resting point (that is, they don't always report exactly 0,0 when the Control is in the center). Using the `min` value on a deadzone Processor avoids unintentional input from such Controls. Also, some Controls don't consistently report their maximum values when moving the axis all the way. Using the `max` value on a deadzone Processor ensures that you always get the maximum value in such cases.|`Vector2`|<ul><li>`float min`</li><li>`float max`</li></ul>
