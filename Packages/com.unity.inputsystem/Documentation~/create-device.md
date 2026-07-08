---
uid: input-system-create-device
---

# Create a device

Once the system has chosen a [layout](layouts.md) for a device, it instantiates an [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) and populates it with [`InputControls`](xref:UnityEngine.InputSystem.InputControl) as the layout dictates. This process is internal and happens automatically.

> [!NOTE]
> You can't create valid [`InputDevices`](xref:UnityEngine.InputSystem.InputDevice) and [`InputControls`](xref:UnityEngine.InputSystem.InputControl) by manually instantiating them with `new`. To guide the creation process, you must use [layouts](layouts.md).

After the Input System assembles the [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice), it calls [`FinishSetup`](xref:UnityEngine.InputSystem.InputControl) on each control of the device and on the device itself. Use this to finalize the setup of the Controls.

After an [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) is fully assembled, the Input System adds it to the system. As part of this process, the Input System calls [`MakeCurrent`](xref:UnityEngine.InputSystem.InputDevice) on the Device, and signals  [`InputDeviceChange.Added`](xref:UnityEngine.InputSystem.InputDeviceChange) on [`InputSystem.onDeviceChange`](xref:UnityEngine.InputSystem.InputSystem). The Input System also calls [`InputDevice.OnAdded`](xref:UnityEngine.InputSystem.InputDevice).

Once added, the [`InputDevice.added`](xref:UnityEngine.InputSystem.InputDevice) flag is set to true.

## Add devices manually

To add devices manually, you can call one of the `InputSystem.AddDevice` methods such as [`InputSystem.AddDevice(layout)`](xref:UnityEngine.InputSystem.InputSystem).

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

When a device is added, the Input System automatically issues a [sync request](xref:UnityEngine.InputSystem.LowLevel.RequestSyncCommand) on the device. This instructs the device to send an event representing its current state. Whether this request succeeds depends on the whether the given device supports the sync command.
