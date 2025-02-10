# Noisy controls

Noisy controls are those that can change value without any actual or intentional user interaction required. For example, they gyroscope sensor in a cellphone provides noisy input data because even if the cellphone is at rest, there are usually fluctuations in the control's value readings. Another example are orientation readings from a head-mounted display.

Some built-in control types are marked as **noisy**. You can query this using the  [`InputControl.noisy`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_noisy) property, or by inspecting the control types in the [Input Debugger window](the-input-debugger-window.md).

If a control is marked as noisy:

- The control is not considered for [interactive rebinding](interactive-rebinding.md). [`InputActionRebindingExceptions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) ignores the control by default (you can bypass this using [`WithoutIgnoringNoisyControls`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithoutIgnoringNoisyControls)).

- The Input System performs additional event filtering to filter out noise, then calls [`InputDevice.MakeCurrent`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_MakeCurrent) if any non-noise values cause the control to change state. If an input event for a Device contains no state change on a control that is not marked noisy, then the Device will not be made current based on the event. This avoids, for example, a plugged in PS4 controller constantly making itself the current gamepad ([`Gamepad.current`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_current)) due to its sensors constantly feeding data into the system.

- When the application loses focus and Devices are [reset](Devices.md#device-resets) as a result, the state of noisy control will be preserved as is. This ensures that sensor readings will remain at their last value rather than being reset to default values.

>**Note**: If any control on a device is noisy, the device itself is flagged as noisy.

In addition to the [`input state`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_currentStatePtr) and the [`default state`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_defaultStatePtr) that the Input System keeps for all Devices currently present, it also maintains a [`noise mask`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_noiseMaskPtr) in which only bits for state that is __not__ noise are set. You can use this to efficiently mask out noise in input.
