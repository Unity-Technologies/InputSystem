---
uid: input-system-record-events
---

# Record events 

>NOTE: To download a sample project which contains a reusable MonoBehaviour called `InputRecorder`, which can capture and replay input from arbitrary devices, open the Package Manager, select the Input System Package, and choose the sample project "Input Recorder" to download.

You can use the [`InputEventTrace`](../api/UnityEngine.InputSystem.LowLevel.InputEventTrace.html) class to record input events for later processing:

```CSharp
var trace = new InputEventTrace(); // Can also give device ID to only
                                   // trace events for a specific device.

trace.Enable();

//... run stuff

var current = new InputEventPtr();
while (trace.GetNextEvent(ref current))
{
    Debug.Log("Got some event: " + current);
}

// Also supports IEnumerable.
foreach (var eventPtr in trace)
    Debug.Log("Got some event: " + eventPtr);

// Trace consumes unmanaged resources. Make sure to dispose.
trace.Dispose();
```

Dispose event traces after use, so that they do not leak memory on the unmanaged (C++) memory heap.

You can also write event traces out to files/streams, load them back in, and replay recorded streams.

```CSharp
// Set up a trace with such that it automatically grows in size as needed.
var trace = new InputEventTrace(growBuffer: true);
trace.Enable();

// ... capture some input ...

// Write trace to file.
trace.WriteTo("mytrace.inputtrace.");

// Load trace from same file.
var loadedTrace = InputEventTrace.LoadFrom("mytrace.inputtrace");
```

You can replay captured traces directly from [`InputEventTrace`](../api/UnityEngine.InputSystem.LowLevel.InputEventTrace.html) instances using the [`Replay`](../api/UnityEngine.InputSystem.LowLevel.InputEventTrace.html#UnityEngine_InputSystem_LowLevel_InputEventTrace_Replay_) method.

```CSharp
// The Replay method returns a ReplayController that can be used to
// configure and control playback.
var controller = trace.Replay();

// For example, to not replay the events as is but rather create new devices and send
// the events to them, call WithAllDevicesMappedToNewInstances.
controller.WithAllDevicessMappedToNewInstances();

// Replay all frames one by one.
controller.PlayAllFramesOnyByOne();

// Replay events in a way that tries to simulate original event timing.
controller.PlayAllEventsAccordingToTimestamps();
```