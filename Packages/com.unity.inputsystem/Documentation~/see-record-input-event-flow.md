---
uid: input-system-see-record-flow
---

# See and record input event flow

To record events flowing through the system, use this code:

```C#

    // You can also provide a device ID to only
    // trace events for a specific device.
    var trace = new InputEventTrace();

    trace.Enable();

    var current = new InputEventPtr();
    while (trace.GetNextEvent(ref current))
    {
        Debug.Log("Got some event: " + current);
    }

    // Also supports IEnumerable.
    foreach (var eventPtr in trace)
        Debug.Log("Got some event: " + eventPtr);

    // Trace consumes unmanaged resources. Make sure you dispose it correctly to avoid memory leaks.
    trace.Dispose();

```

To see events as they're processed, use this code:

```C#

    InputSystem.onEvent +=
        (eventPtr, device) =>
        {
            // Can handle events yourself, for example, and then stop them
            // from further processing by marking them as handled.
            eventPtr.handled = true;
        };

```
