## Control state

Each Control is connected to a block of memory that is considered the Control's "state". You can query the size, format, and location of this block of memory from a Control through the [`InputControl.stateBlock`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_stateBlock) property.

The state of Controls is stored in unmanaged memory that the Input System handles internally. All Devices added to the system share one block of unmanaged memory that contains the state of all the Controls on the Devices.

A Control's state might not be stored in the natural format for that Control. For example, the system often represents buttons as bitfields, and axis controls as 8-bit or 16-bit integer values. This format is determined by the combination of platform, hardware, and drivers. Each Control knows the format of its storage and how to translate the values as needed. The Input System uses [layouts](Layouts.md) to understand this representation.

You can access the current state of a Control through its [`ReadValue`](../api/UnityEngine.InputSystem.InputControl-1.html#UnityEngine_InputSystem_InputControl_1_ReadValue) method.

```CSharp
Gamepad.current.leftStick.x.ReadValue();
```

Each type of Control has a specific type of values that it returns, regardless of how many different types of formats it supports for its state. You can access this value type through the [`InputControl.valueType`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_valueType) property.

Reading a value from a Control might apply one or more value Processors. See documentation on [Processors](Processors.md) for more information.

