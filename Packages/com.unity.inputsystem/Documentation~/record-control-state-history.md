---
uid: input-system-control-state-history
---

# Record control state history

If you want to access the history of value changes on a control (for example, in order to compute exit velocity on a touch release), you can record state changes over time with [`InputStateHistory`](xref:UnityEngine.InputSystem.LowLevel.InputStateHistory) or [`InputStateHistory<TValue>`](xref:UnityEngine.InputSystem.LowLevel.InputStateHistory`1). The latter restricts controls to those of a specific value type, which in turn simplifies some of the API.


```CSharp
// Create history that records Vector2 control value changes.
// NOTE: You can also pass controls directly or use paths that match multiple
//       controls (For example, "<Gamepad>/<Button>").
// NOTE: The unconstrained InputStateHistory class can record changes on controls
//        of different value types.
var history = new InputStateHistory<Vector2>("<Touchscreen>/primaryTouch/position");

// To start recording state changes of the controls to which the history
// is attached, call StartRecording.
history.StartRecording();

// To stop recording state changes, call StopRecording.
history.StopRecording();

// Recorded history can be accessed like an array.
for (var i = 0; i < history.Count; ++i)
{
    // Each recorded value provides information about which control changed
    // value (in cases state from multiple controls is recorded concurrently
    // by the same InputStateHistory) and when it did so.

    var time = history[i].time;
    var control = history[i].control;
    var value = history[i].ReadValue();
}

// Recorded history can also be iterated over.
foreach (var record in history)
    Debug.Log(record.ReadValue());
Debug.Log(string.Join(",\n", history));

// You can also record state changes manually, which allows
// storing arbitrary histories in InputStateHistory.
// NOTE: This records a value change that didn't actually happen on the control.
history.RecordStateChange(Touchscreen.current.primaryTouch.position,
    new Vector2(0.123f, 0.234f));

// State histories allocate unmanaged memory and need to be disposed.
history.Dispose();
```

For example, if you want to have the last 100 samples of the left stick on the gamepad available, you can use this code:

```CSharp
var history = new InputStateHistory<Vector2>(Gamepad.current.leftStick);
history.historyDepth = 100;
history.StartRecording();
```
