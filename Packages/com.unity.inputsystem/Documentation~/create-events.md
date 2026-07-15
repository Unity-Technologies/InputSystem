---
uid: input-system-create-events
---

# Create events

Anyone can create and queue new input events against any existing Device. Queueing an input event is thread-safe, which means that event generation can happen in background threads.

> [!NOTE]
> Unity allocates limited memory to events that come from background threads. If background threads produce too many events, queueing an event from a thread blocks the thread until the main thread flushes out the background event queue.

Note that queuing an event doesn't immediately consume the event. Event processing happens on the next update (depending on [`InputSettings.updateMode`](update-mode.md), it is triggered either manually with [`InputSystem.Update`](xref:UnityEngine.InputSystem.InputSystem), or automatically as part of the Player loop).

## Sending state events

For Devices that have a corresponding "state struct" describing the state of the device, the easiest way of sending input to the Device is to simply queue instances of those structs:

```CSharp
// Mouse.
InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = new Vector2(123, 234) });

// Keyboard.
InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.LeftCtrl, Key.A));
```

`Touchscreen` is somewhat special in that it expects its input to be in [`TouchState`](xref:UnityEngine.InputSystem.LowLevel.TouchState) format.

```CSharp
// Start touch.
InputSystem.QueueStateEvent(Touchscreen.current,
    new TouchState { touchId = 1, phase = TouchPhase.Began, position = new Vector2(123, 234) });

// Move touch.
InputSystem.QueueStateEvent(Touchscreen.current,
    new TouchState { touchId = 1, phase = TouchPhase.Moved, position = new Vector2(234, 345) });

// End touch.
InputSystem.QueueStateEvent(Touchscreen.current,
    new TouchState { touchId = 1, phase = TouchPhase.Ended, position = new Vector2(123, 234) });
```

> [!IMPORTANT]
> [Touch IDs](xref:UnityEngine.InputSystem.Controls.TouchControl) cannot be 0! A valid touch must have a non-zero touch ID. Concurrent touches must each have a unique ID. After a touch has ended, its ID can be reused &ndash; although it is recommended to not do so.

If the exact format of the state used by a given Device is not known, the easiest way to send input to it is to simply create a [`StateEvent`](xref:UnityEngine.InputSystem.LowLevel.StateEvent) from the Device itself:

```CSharp
// `StateEvent.From` creates a temporary buffer in unmanaged memory that holds
// a state event large enough for the given device and contains a memory
// copy of the device's current state.
InputEventPtr eventPtr;
using (StateEvent.From(myDevice, out eventPtr))
{
    ((AxisControl) myDevice["myControl"]).WriteValueIntoEvent(0.5f, eventPtr);
    InputSystem.QueueEvent(eventPtr);
}
```

Alternatively, you can send events for individual Controls.

```CSharp
// Send event to update leftStick on the gamepad.
InputSystem.QueueDeltaStateEvent(Gamepad.current.leftStick,
    new Vector2(0.123f, 0.234f);
```

Note that delta state events only work for Controls that are both byte-aligned and a multiple of 8 bits in size in memory. You can't send a delta state event for a button Control that is stored as a single bit, for example.
