# Noisy Controls

The Input System can label a Control as "noisy". You can query this using the  [`InputControl.noisy`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_noisy) property.

Noisy Controls are those that can change value without any actual or intentional user interaction required. A good example of this is a gravity sensor in a cellphone. Even if the cellphone is perfectly still, there are usually fluctuations in gravity readings. Another example are orientation readings from an HMD.

If a Control is marked as noisy, it means that:

1. The Control is not considered for [interactive rebinding](ActionBindings.md#interactive-rebinding). [`InputActionRebindingExceptions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) ignores the Control by default (you can bypass this using [`WithoutIgnoringNoisyControls`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithoutIgnoringNoisyControls)).
2. If enabled in the Project Settings, the system performs additional event filtering, then calls [`InputDevice.MakeCurrent`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_MakeCurrent). If an input event for a Device contains no state change on a Control that is not marked noisy, then the Device will not be made current based on the event. This avoids, for example, a plugged in PS4 controller constantly making itself the current gamepad ([`Gamepad.current`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_current)) due to its sensors constantly feeding data into the system.
3. When the application loses focus and Devices are [reset](Devices.md#device-resets) as a result, the state of noisy Controls will be preserved as is. This ensures that sensor readinds will remain at their last value rather than being reset to default values.

>**Note**: If any Control on a Device is noisy, the Device itself is flagged as noisy.

Parallel to the [`input state`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_currentStatePtr) and the [`default state`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_defaultStatePtr) that the Input System keeps for all Devices currently present, it also maintains a [`noise mask`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_noiseMaskPtr) in which only bits for state that is __not__ noise are set. This can be used to very efficiently mask out noise in input.
