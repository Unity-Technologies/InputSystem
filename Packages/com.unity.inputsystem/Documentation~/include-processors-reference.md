## Processors Editor reference

The Processors foldout contains properties for [processors](processors.md) that you can assign to actions and bindings.

All properties in the Processors foldout correspond to the [`InputSystem.Processors`](../api/UnityEngine.InputSystem.Processors.html) API.

|Processor|Description|
|-|-|
|**Axis Deadzone**| Scale the values of a control so that any absolute value smaller than **Min** is `0`, and any absolute value larger than **Max** is `1` or `-1`.|
|**Clamp**| Restrict the input values to the [`min..max`] range.|
|**Invert**| Invert the values from a Control (that is, multiply the values by -1). <br/><br/>For Value-type and PassThrough-type actions, you can also use **Invert Vector2** and **Invert Vector3** processors. |
|**Normalize**| Normalize input values in the range [`min..max`] to unsigned normalized form [`0..1`]. <br/><br/>For Value-type and PassThrough-type actions, you can also use **Normalize Vector2** and **Normalize Vector3** processors. |
|**Scale**| Multiply all input values by **Factor**. <br/><br/>For Value-type and PassThrough-type actions, you can also use **Scale Vector2** and **Scale Vector3** processors.|
|**Stick Deadzone**| Scale the values of a Vector2 control so that any input vector with a magnitude smaller than **Min** results in (`0,0`), and any input vector with a magnitude greater than **Max** is normalized to length `1`.|
