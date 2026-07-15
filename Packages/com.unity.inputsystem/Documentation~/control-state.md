---
uid: input-system-control-state
---

# Control state

A control's **state** is the current value stored by the Input System based on the control's [actuation](control-actuation.md).

The recommended workflow is to [bind controls to actions](add-duplicate-delete-binding.md), and then [respond to input at runtime](./respond-to-input.md) by polling or recieving callbacks from those actions. For this reason, it is not typically necessary to directly read control states.

However, the documentation on this page gives information about the details of how controls states are stored, and how to directly access the state, which may be useful if you are using a different workflow for a specialized situation.

## Details

Each control is connected to a block of memory that is considered the control's "state". You can query the size, format, and location of this block of memory from a control through the [`InputControl.stateBlock`](xref:UnityEngine.InputSystem.InputControl) property.

The Input System stores the state of controls in unmanaged memory that it handles internally. All Devices added to the system share one block of unmanaged memory that contains the state of all the controls on the Devices.

A control's state might not be stored in the natural format for that control. For example, the system often represents buttons as bitfields, and axis controls as 8-bit or 16-bit integer values. This format is determined by the combination of platform, hardware, and drivers. Each control knows the format of its storage and how to translate the values as needed. The Input System uses [layouts](layouts.md) to understand this representation.

You can access the current state of a control through its [`ReadValue`](xref:UnityEngine.InputSystem.InputControl`1) method.

```CSharp
Gamepad.current.leftStick.x.ReadValue();
```

Each type of control has a specific type of values that it returns, regardless of how many different types of formats it supports for its state. You can access this value type through the [`InputControl.valueType`](xref:UnityEngine.InputSystem.InputControl) property.

Reading a value from a control might apply one or more value Processors. Refer to [Processors](processors.md) for more information.
