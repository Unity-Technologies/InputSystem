---
uid: input-system-add-custom-device-command
---

# Add a custom device command 

To create custom Device commands (for example, to support some functionality for a specific HID), create a `struct` that contains all the data to be sent to the Device, and add a [`typeStatic`](../api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html#UnityEngine_InputSystem_LowLevel_IInputDeviceCommandInfo_typeStatic) property to make that struct implement the [`IInputDeviceCommandInfo`](../api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html) interface. To send data to a HID, this property should return `"HIDO"`.

You can then create an instance of this struct and populate all its fields, then use [`InputDevice.ExecuteCommand<TCommand>`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_ExecuteCommand__1___0__) to send it to the Device. The data layout of the struct must match the native representation of the data as the device interprets it.
