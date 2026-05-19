---
uid: input-system-create-device
---

# Create a device 

Once the system has chosen a [layout](layouts.md) for a device, it instantiates an [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) and populates it with [`InputControls`](../api/UnityEngine.InputSystem.InputControl.html) as the layout dictates. This process is internal and happens automatically.

>__Note__: You can't create valid [`InputDevices`](../api/UnityEngine.InputSystem.InputDevice.html) and [`InputControls`](../api/UnityEngine.InputSystem.InputControl.html) by manually instantiating them with `new`. To guide the creation process, you must use [layouts](layouts.md).

After the Input System assembles the [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html), it calls [`FinishSetup`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_FinishSetup_) on each control of the device and on the device itself. Use this to finalize the setup of the Controls.

After an [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) is fully assembled, the Input System adds it to the system. As part of this process, the Input System calls [`MakeCurrent`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_MakeCurrent_) on the Device, and signals  [`InputDeviceChange.Added`](../api/UnityEngine.InputSystem.InputDeviceChange.html#UnityEngine_InputSystem_InputDeviceChange_Added) on [`InputSystem.onDeviceChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange). The Input System also calls [`InputDevice.OnAdded`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_OnAdded_).

Once added, the [`InputDevice.added`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_added) flag is set to true.

## Add devices manually

To add devices manually, you can call one of the `InputSystem.AddDevice` methods such as [`InputSystem.AddDevice(layout)`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_AddDevice_System_String_System_String_System_String_).

```CSharp
// Add a gamepad. This bypasses the matching process and creates 
// a device directly
// with the Gamepad layout.
InputSystem.AddDevice<Gamepad>();

// Add a device such that the matching process is employed:
InputSystem.AddDevice(new InputDeviceDescription
{
    interfaceName = "XInput",
    product = "Xbox Controller",
});
```

When a device is added, the Input System automatically issues a [sync request](../api/UnityEngine.InputSystem.LowLevel.RequestSyncCommand.html) on the device. This instructs the device to send an event representing its current state. Whether this request succeeds depends on the whether the given device supports the sync command.
