---
uid: input-system-listen-to-events
---

# Listen to events

If you want to do any monitoring or processing on incoming events yourself, subscribe to the [`InputSystem.onEvent`](xref:UnityEngine.InputSystem.InputSystem) callback.

```CSharp
InputSystem.onEvent +=
   (eventPtr, device) =>
   {
       Debug.Log($"Received event for {device}");
   };
```

An [`IObservable`](https://docs.microsoft.com/en-us/dotnet/api/system.iobservable-1) interface is provided to more conveniently process events.

```CSharp
// Wait for first button press on a gamepad.
InputSystem.onEvent
    .ForDevice<Gamepad>()
    .Where(e => e.HasButtonPress())
    .CallOnce(ctrl => Debug.Log($"Button {ctrl} pressed"));
```

To enumerate the controls that have value changes in an event, you can use [`InputControlExtensions.EnumerateChangedControls`](xref:UnityEngine.InputSystem.InputControlExtensions).

```CSharp
InputSystem.onEvent
    .Call(eventPtr =>
    {
        foreach (var control in eventPtr.EnumerateChangedControls())
            Debug.Log($"Control {control} changed value to {control.ReadValueFromEventAsObject(eventPtr)}");
    });
```

This is significantly more efficient than manually iterating over [`InputDevice.allControls`](xref:UnityEngine.InputSystem.InputDevice) and reading out the value of each control from the event.
