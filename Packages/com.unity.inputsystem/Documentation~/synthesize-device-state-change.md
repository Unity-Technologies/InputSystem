---
uid: input-system-syntehsize-device-state
---

# Synthesize a device state change

The Input System can synthesize a new state from an existing state. An example of such a synthesized state is the [`press`](xref:UnityEngine.InputSystem.Pointer) button  Control that [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) inherits from [`Pointer`](xref:UnityEngine.InputSystem.Pointer). Unlike a mouse, which has a physical button, for [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) this is a [synthetic Control](synthetic-controls.md) that doesn't correspond to actual data coming in from the Device backend. Instead, the Input System considers the button to be pressed if any touch is currently ongoing, and released otherwise.

To do this, the Input System uses [`InputState.Change`](xref:UnityEngine.InputSystem.LowLevel.InputState), which allows feeding arbitrary state changes into the system without having to run them through the input event queue. The Input System incorporates state changes directly and synchronously. State change [monitors](monitor-device-state-changes.md) still trigger as expected.
