using UnityEngine;
using UnityEngine.InputSystem;

class RecordControlStateHistoryExample 
{
    void Example()
    {
    #region history
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
    #endregion
    }

    void Example100Samples()
    {
        #region 100samples
        var history = new InputStateHistory<Vector2>(Gamepad.current.leftStick);
        history.historyDepth = 100;
        history.StartRecording();
        #endregion
    }
}