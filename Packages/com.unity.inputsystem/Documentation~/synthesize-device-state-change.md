---
uid: input-system-syntehsize-device-state
---

# Synthesize a device state change 

The Input System can synthesize a new state from an existing state. An example of such a synthesized state is the [`press`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_press) button  Control that [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) inherits from [`Pointer`](../api/UnityEngine.InputSystem.Pointer.html). Unlike a mouse, which has a physical button, for [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) this is a [synthetic Control](controls.md#synthetic-controls) that doesn't correspond to actual data coming in from the Device backend. Instead, the Input System considers the button to be pressed if any touch is currently ongoing, and released otherwise.

To do this, the Input System uses [`InputState.Change`](../api/UnityEngine.InputSystem.LowLevel.InputState.html#UnityEngine_InputSystem_LowLevel_InputState_Change__1_UnityEngine_InputSystem_InputControl___0_UnityEngine_InputSystem_LowLevel_InputUpdateType_UnityEngine_InputSystem_LowLevel_InputEventPtr_), which allows feeding arbitrary state changes into the system without having to run them through the input event queue. The Input System incorporates state changes directly and synchronously. State change [monitors](#monitoring-state-changes) still trigger as expected.