---
uid: input-system-noisy-controls
---

# Noisy controls

Noisy controls are those that can change value without any actual or intentional user interaction required. For example, they gyroscope sensor in a cellphone provides noisy input data because even if the cellphone is at rest, there are usually fluctuations in the control's value readings. Another example are orientation readings from a head-mounted display.

Some built-in control types are marked as **noisy**. You can query this using the  [`InputControl.noisy`](xref:UnityEngine.InputSystem.InputControl) property, or by inspecting the control types in the [Input Debugger window](the-input-debugger-window.md).

If a control is marked as noisy:

- The control is not considered for [interactive rebinding](interactive-rebinding.md). [`InputActionRebindingExceptions.RebindingOperation`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation) ignores the control by default (you can bypass this using [`WithoutIgnoringNoisyControls`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation)).

- The Input System performs additional event filtering to filter out noise, then calls [`InputDevice.MakeCurrent`](xref:UnityEngine.InputSystem.InputDevice) if any non-noise values cause the control to change state. If an input event for a Device contains no state change on a control that is not marked noisy, then the Device will not be made current based on the event. This avoids, for example, a plugged in PS4 controller constantly making itself the current gamepad ([`Gamepad.current`](xref:UnityEngine.InputSystem.Gamepad)) due to its sensors constantly feeding data into the system.

- When the application loses focus and Devices are [reset](reset-device.md) as a result, the state of noisy control will be preserved as is. This ensures that sensor readings will remain at their last value rather than being reset to default values.

> [!NOTE]
> To query whether a control is noisy, use the [`InputControl.noisy`](xref:UnityEngine.InputSystem.InputControl.noisy) property.
>
> If any control on a device is noisy, the device itself is flagged as noisy.

In addition to the [`input state`](xref:UnityEngine.InputSystem.InputControl) and the [`default state`](xref:UnityEngine.InputSystem.InputControl) that the Input System keeps for all Devices currently present, it also maintains a [`noise mask`](xref:UnityEngine.InputSystem.InputControl) in which only bits for state that is __not__ noise are set. You can use this to efficiently mask out noise in input.

### Noise masks

Parallel to the [`input state`](xref:UnityEngine.InputSystem.InputControl.currentStatePtr) and the [`default state`](xref:UnityEngine.InputSystem.InputControl.defaultStatePtr) that the Input System keeps for all devices currently present, it also maintains a [`noise mask`](xref:UnityEngine.InputSystem.InputControl.noiseMaskPtr) in which only bits for state that is __not__ noise are set. This can be used to very efficiently mask out noise in input.
