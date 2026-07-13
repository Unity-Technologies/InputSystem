---
uid: input-system-reset-device
---

# Reset a device

Resetting a Device resets its Controls to their default state. You can do this manually using [`InputSystem.ResetDevice`](xref:UnityEngine.InputSystem.InputSystem):

```CSharp
    InputSystem.ResetDevice(Gamepad.current);
```

There are two types of resets as determined by the second parameter to [`InputSystem.ResetDevice`](xref:UnityEngine.InputSystem.InputSystem):

|Type|Description|
|----|-----------|
|"Soft" Resets|This is the default. With this type, only controls that are *not* marked as [`dontReset`](control-items.md) are reset to their default value. This excludes controls such as [`Pointer.position`](xref:UnityEngine.InputSystem.Pointer) from resets and thus prevents mouse positions resetting to `(0,0)`.|
|"Hard" Resets|In this type, *all* controls are reset to their default value regardless of whether they have [`dontReset`](control-items.md) set or not.|

Resetting Controls this way is visible on [Actions](actions.md). If you reset a Device that is currently driving one or more Action, the Actions are cancelled. This cancellation is different from sending an event with default state. Whereas the latter may inadvertently [perform](xref:UnityEngine.InputSystem.InputAction) Actions (For example, a button that was pressed would not appear to have been released), a reset will force clean cancellation.

Resets may be triggered automatically by the Input System depending on [application focus](device-background-focus-changes.md).
