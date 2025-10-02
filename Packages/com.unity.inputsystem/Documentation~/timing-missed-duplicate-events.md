---
uid: input-system-timing-missed
---
# Avoid missed or duplicate discrete events

Discrete events are simple on/off events that occur when a user presses or releases a control such as a gamepad button, key, mouse, or touch press. This is in contrast to continuously changing values like those from gamepad stick movement. You can poll for these types of discrete event by using [`WasPressedThisFrame`](xref:UnityEngine.InputSystem.InputAction.WasPressedThisFrame*) or [`WasReleasedThisFrame`](xref:UnityEngine.InputSystem.InputAction.WasReleasedThisFrame*). However, you can get incorrect results such as missing an event or appearing to receive multiple, if you check for them at the wrong time.

If your Update Mode is set to **Process in FixedUpdate**, you must ensure that you only use [`WasPressedThisFrame`](xref:UnityEngine.InputSystem.InputAction.WasPressedThisFrame*) or [`WasReleasedThisFrame`](xref:UnityEngine.InputSystem.InputAction.WasReleasedThisFrame*) in **FixedUpdate** calls. Using them in Update might either miss events, or returns true across multiple consecutive frames depending on whether the frame rate is running slower or faster than the fixed time step.

Conversely, if your Update Mode is set to **process in Dynamic Update**, you must ensure that you only use [`WasPressedThisFrame`](xref:UnityEngine.InputSystem.InputAction.WasPressedThisFrame*) or [`WasReleasedThisFrame`](xref:UnityEngine.InputSystem.InputAction.WasReleasedThisFrame*) in `Update` calls. Using them in `FixedUpdate` might either miss events, or return true across multiple consecutive frames depending on whether the fixed time step is running slower or faster than your game’s frame rate.

If you find that you're missing events that should have been detected, or are receiving multiple events for what should have been a single press or release of a control, the reason is probably that you either have your Input Update Mode set to the wrong setting, or that you're reading the state of these events in the wrong `Update` or `FixedUpdate` call.
