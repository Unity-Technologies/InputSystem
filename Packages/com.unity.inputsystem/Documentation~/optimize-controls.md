---
uid: input-system-optimize-controls
---

# Optimize controls

The [recommended workflow](workflows.md) is sufficiently optimized for most scenarios. However in some specialized situations when reading values directly from controls, you can make small performance gains by implementing some of the following techniques.

## Avoiding defensive copies

Use [`InputControl<T>.value`](xref:UnityEngine.InputSystem.InputControl`1.value) instead of [`InputControl<T>.ReadValue`](xref:UnityEngine.InputSystem.InputControl`1.ReadValue) to avoid creating a copy of the control state on every call. This is because `InputControl<T>.value` returns the value as `ref readonly` while `InputControl<T>.ReadValue` always makes a copy. Note that this optimization only applies if the call site assigns the return value to a variable that has been declared `ref readonly`. Otherwise, a copy is made as before.

Additionally, be aware of defensive copies that the compiler can allocate when it is unable to determine that it can safely use the read-only reference. This means that if the compiler can't determine that the reference won't be changed, it will create a defensive copy. For more details, refer to the [.NET guidance on reducing memory allocations](https://learn.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code#use-ref-readonly-return-statements).

## Control Value Caching

Enable the `USE_READ_VALUE_CACHING` internal feature flag to get the Input System to switch to an optimized path for reading control values. This path efficiently marks controls as "stale" when they have been actuated. Subsequent calls to [`InputControl<T>.ReadValue`](xref:UnityEngine.InputSystem.InputControl`1.ReadValue) will only apply control processing when there have been changes to that control or in the case of any [hard-coded processing](xref:input-system-add-processors-controls) that might exist on the control. For example, [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) has built-in inversion, normalization, scaling, and any other processors added to the controls' [processor stack](xref:input-system-add-processors-controls).

> [!NOTE]
> Performance improvements **are currently not guaranteed** for all use cases. Even though this performance path marks controls as "stale" in an efficient way, it still has an overhead which can degrade performance in some cases.

Positive performance impact can occur when:
- Reading from controls that don't change frequently.
- If the controls change every frame, are being read and have actions bound to them as well. For example, reading `leftStick`, `leftStick.x` and `leftStick.left` when there's an action with composite bindings on a Gamepad.

Negative performance impact can occur when:
- Reading from controls that change frequently but which have no bound actions.
- No readings from controls that change frequently.

`USE_READ_VALUE_CACHING` is not enabled by default because it can result in the following minor behavioral changes:
- For control processors that use a global state with cached value optimization, changing the global state of a control processor will have no effect. Reading the control value will only ever return a new value if the physical control has been actuated.

    This behavior differs from using global states without cached value optimizations, in which you can read the control value, change the global state, read the control value again, and get a new value due to the fact that the control processor runs on every call.
- Writing to device state using low-level APIs like [`InputControl<T>.WriteValueIntoState`](xref:UnityEngine.InputSystem.InputControl`1.WriteValueIntoState(`0,System.Void*)) doesn't set the stale flag and subsequent calls to [`InputControl<T>.value`](xref:UnityEngine.InputSystem.InputControl`1.value) won't reflect those changes.
- After changing properties on [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) the [`ApplyParameterChanges`](xref:UnityEngine.InputSystem.InputControl.ApplyParameterChanges) method has to be called to invalidate cached values.

Processors that must run on every read can set their caching policy to [EvaluateOnEveryRead](xref:UnityEngine.InputSystem.InputProcessor.CachingPolicy.EvaluateOnEveryRead), which disables caching on controls that are using such processors.

You can enable the `PARANOID_READ_VALUE_CACHING_CHECKS` internal feature flag to compare cached and uncached values on every read. If they don't match, the check logs an error.

## Optimized control read value

Enable the `USE_OPTIMIZED_CONTROLS` internal feature flag to get the Input System to access state memory faster for some control instances. This is very specific optimization and should be used with caution.

> [!NOTE]
> This optimization has a performance impact on `PlayMode` because the Input System performs extra checks in order to ensure that the controls have the correct memory representation during development. If you see a performance drop in `PlayMode` when using this optimization, that is expected at this stage.

Most controls are flexible with regards to memory representation. For example, [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) can be one bit, multiple bits, a float, or in [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) where `x` and `y` can have different memory representation. However, most controls use common memory representation patterns, such as [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl), which uses floats or single bytes. Another example is [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) which consists of two consecutive floats in memory.

If a control matches a common representation, you can bypass reading its children's controls and cast the memory directly to the common representation. For example if [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) has two consecutive floats in memory, you can bypass reading `x` and `y` separately and just cast the state memory to `Vector2`.

> [!NOTE]
> This optimization only works if the controls don't need any processing applied, such as `invert`, `clamp`, `normalize`, `scale` or any other processor. If any of these are applied to the control, the control will be read as usual without any optimization.

It is important to explicitly call [`InputControl.ApplyParameterChanges()`](xref:UnityEngine.InputSystem.InputControl.ApplyParameterChanges) to ensure [`InputControl.optimizedControlDataType`](xref:UnityEngine.InputSystem.InputControl.optimizedControlDataType) is updated to the correct memory representation for these specific changes:
- Configuration changes after calling [`InputControl.FinishSetup()`](xref:UnityEngine.InputSystem.InputControl.FinishSetup*).
- Changing parameters such as [`AxisControl.invert`](xref:UnityEngine.InputSystem.Controls.AxisControl.invert), [`AxisControl.clamp`](xref:UnityEngine.InputSystem.Controls.AxisControl.clamp), [`AxisControl.normalize`](xref:UnityEngine.InputSystem.Controls.AxisControl.normalize), [`AxisControl.scale`](xref:UnityEngine.InputSystem.Controls.AxisControl.scale) or changing processors. The memory representation needs to be recalculated after these changes to ensure that the control is no longer optimized. Otherwise, the control will be read with wrong values.

The optimized controls work as follows:
- A potential memory representation is set using [`InputControl.CalculateOptimizedControlDataType()`](xref:UnityEngine.InputSystem.InputControl.CalculateOptimizedControlDataType)
- Its memory representation is stored in [`InputControl.optimizedControlDataType`](xref:UnityEngine.InputSystem.InputControl.optimizedControlDataType)
- Finally,  [`ReadUnprocessedValueFromState`](xref:UnityEngine.InputSystem.InputControl`1.ReadUnprocessedValueFromState*) uses the optimized memory representation to decide if it should cast to memory directly instead of reading every children control on its own to reconstruct the controls state.
