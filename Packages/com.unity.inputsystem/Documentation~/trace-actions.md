# Trace Actions


You can trace Actions to generate a log of all activity that happened on a particular set of Actions. To do so, use [`InputActionTrace`](../api/UnityEngine.InputSystem.Utilities.InputActionTrace.html). This behaves in a similar way to [`InputEventTrace`](../api/UnityEngine.InputSystem.LowLevel.InputEventTrace.html) for events.

>__Note__: `InputActionTrace` allocates unmanaged memory and needs to be disposed of so that it doesn't create memory leaks.

```CSharp
var trace = new InputActionTrace();

// Subscribe trace to single Action.
// (Use UnsubscribeFrom to unsubscribe)
trace.SubscribeTo(myAction);

// Subscribe trace to entire Action Map.
// (Use UnsubscribeFrom to unsubscribe)
trace.SubscribeTo(myActionMap);

// Subscribe trace to all Actions in the system.
trace.SubscribeToAll();

// Record a single triggering of an Action.
myAction.performed +=
    ctx =>
    {
        if (ctx.ReadValue<float>() > 0.5f)
            trace.RecordAction(ctx);
    };

// Output trace to console.
Debug.Log(string.Join(",\n", trace));

// Walk through all recorded Actions and then clear trace.
foreach (var record in trace)
{
    Debug.Log($"{record.action} was {record.phase} by control {record.control}");

    // To read out the value, you either have to know the value type or read the
    // value out as a generic byte buffer. Here, we assume that the value type is
    // float.

    Debug.Log("Value: " + record.ReadValue<float>());

    // If it's okay to accept a GC hit, you can also read out values as objects.
    // In this case, you don't have to know the value type.

    Debug.Log("Value: " + record.ReadValueAsObject());
}
trace.Clear();

// Unsubscribe trace from everything.
trace.UnsubscribeFromAll();

// Release memory held by trace.
trace.Dispose();
```

Once recorded, a trace can be safely read from multiple threads as long as it is not concurrently being written to and as long as the Action setup (that is, the configuration data accessed by the trace) is not concurrently being changed on the main thread.
