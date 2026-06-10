---
uid: input-system-optimize-controls
---

# Optimize controls

The [recommended workflow](workflows.md) is sufficiently optimized for most scenarios. However in some specialized situations when reading values directly from controls, you can make small performance gains by implementing some of the following techniques.

## Avoiding defensive copies

Use [`InputControl<T>.value`](xref:UnityEngine.InputSystem.InputControl-1) instead of [`InputControl<T>.ReadValue`](xref:UnityEngine.InputSystem.InputControl-1) to avoid creating a copy of the control state on every call, as the former returns the value as `ref readonly` while the latter always makes a copy. Note that this optimization only applies if the call site assigns the return value to a variable that has been declared 'ref readonly'. Otherwise a copy will be made as before. Additionally, be aware of defensive copies that can be allocated by the compiler when it is unable to determine that it can safely use the readonly reference i.e. if it can't determine that the reference won't be changed, it will create a defensive copy for you. For more details, see https://learn.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code#use-ref-readonly-return-statements.

## Control Value Caching

When the `'USE_READ_VALUE_CACHING'` internal feature flag is set, the Input System will switch to an optimized path for reading control values. This path efficiently marks controls as 'stale' when they have been actuated. Subsequent calls to [`InputControl<T>.ReadValue`](xref:UnityEngine.InputSystem.InputControl-1) will only apply control processing when there have been changes to that control or in case of control processing. Control processing in this case can mean any hard-coded processing that might exist on the control, such as with [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) which has built-in inversion, normalisation, scaling etc, or any processors that have been applied to the controls' [processor stack](add-processors-controls.md).

> [!NOTE] 
> Performance improvements **are currently not guaranteed** for all use cases. Even though this performance path marks controls as "stale" in an efficient way, it still has an overhead which can degrade performance in some cases.

A positive performance impact has been seen when:

- Reading from controls that do not change frequently.
- In case the controls change every frame, are being read and have actions bound to them as well, e.g. on a Gamepad, reading `leftStick`, `leftStick.x` and `leftStick.left` for example when there's a action with composite bindings setup.

On the other hand, it is likely to have a negative performance impact when:

- No control reads are performed for a control, and there are a lot of changes for that particular control.
- Reading from controls that change frequently that have no actions bound to those controls.

Moreover, this feature is not enabled by default as it can result in the following minor behavioural changes:

* Some control processors use global state. Without cached value optimizations, it is possible to read the control value, change the global state, read the control value again, and get a new value due to the fact that the control processor runs on every call. With cached value optimizations, reading the control value will only ever return a new value if the physical control has been actuated. Changing the global state of a control processor will have no effect otherwise.
* Writing to device state using low-level APIs like [`InputControl<T>.WriteValueIntoState`](xref:UnityEngine.InputSystem.InputControl-1) does not set the stale flag and subsequent calls to [`InputControl<T>.value`](xref:UnityEngine.InputSystem.InputControl-1) will not reflect those changes.
* After changing properties on [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) the [`ApplyParameterChanges`](xref:UnityEngine.InputSystem.InputControl) has to be called to invalidate cached value.

Processors that need to run on every read can set their respective caching policy to EvaluateOnEveryRead. That will disable caching on controls that are using such processor.

If there are any non-obvious inconsistencies, 'PARANOID_READ_VALUE_CACHING_CHECKS' internal feature flag can be enabled to compare cached and uncached value on every read and log an error if they don't match.

## Optimized control read value

When the `'USE_OPTIMIZED_CONTROLS'` internal feature flag is set, the Input System will use faster way to use state memory for some controls instances. This is very specific optimization and should be used with caution.

Most controls are flexible with regards to memory representation, like [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) can be one bit, multiple bits, a float, etc, or in [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) where x and y can have different memory representation.

Yet for most controls there are common memory representation patterns, for example [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) are floats or single bytes. Or some [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) are two consequitive floats in memory.

If a control matches a common representation we can bypass reading its children control and cast the memory directly to the common representation. For example if [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) is two consecutive floats in memory we can bypass reading `x` and `y` separately and just cast the state memory to `Vector2`.

This optimization has a performance impact on `PlayMode` as we do extra checks to ensure that the controls have the correct memory representation during development. Don't be alarmed if you see a performance drop in `PlayMode` when using this optimization as it's expected at this stage.

This optimization only works if the controls don't need any processing applied to them, such as `invert`, `clamp`, `normalize`, `scale` or any other processor. If any of these are applied to the control, **there won't be any optimization applied** and the control will be read as usual.

Also, [`InputControl.ApplyParameterChanges()`](xref:UnityEngine.InputSystem.InputControl) **must be explicitly called** in specific changes to ensure [`InputControl.optimizedControlDataType`](xref:UnityEngine.InputSystem.InputControl) is updated to the correct memory representation. Make sure to call it when:

* Configuration changes after [`InputControl.FinishSetup()`](xref:UnityEngine.InputSystem.InputControl) is called.
* Changing parameters such [`AxisControl.invert`](xref:UnityEngine.InputSystem.Controls.AxisControl), [`AxisControl.clamp`](xref:UnityEngine.InputSystem.Controls.AxisControl), [`AxisControl.normalize`](xref:UnityEngine.InputSystem.Controls.AxisControl), [`AxisControl.scale`](xref:UnityEngine.InputSystem.Controls.AxisControl) or changing processors. The memory representation needs to be recalculated after these changes so that we know that the control is not optimized anymore. Otherwise, the control will be read with wrong values.

The optimized controls work as follows:

* A potential memory representation is set using [`InputControl.CalculateOptimizedControlDataType()`](xref:UnityEngine.InputSystem.InputControl)
* Its memory representation is stored in [`InputControl.optimizedControlDataType`](xref:UnityEngine.InputSystem.InputControl)
* Finally, [`ReadUnprocessedValueFromState`](xref:UnityEngine.InputSystem.InputControl-1) uses the optimized memory representation to decide if it should cast to memory directly instead of reading every children control on it's own to reconstruct the controls state.
