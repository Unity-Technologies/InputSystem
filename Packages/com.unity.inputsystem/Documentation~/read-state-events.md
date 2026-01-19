---
uid: input-system-read-state-events
---

# Read state events 

State events contain raw memory snapshots for Devices. As such, interpreting the data in the event requires knowledge about where and how individual state is stored for a given Device.

The easiest way to access state contained in a state event is to rely on the Device that the state is meant for. You can ask any Control to read its value from a given event rather than from its own internally stored state.

For example, the following code demonstrates how to read a value for [`Gamepad.leftStick`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_leftStick) from a state event targeted at a [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html).

```CSharp
InputSystem.onEvent +=
    (eventPtr, device) =>
    {
        // Ignore anything that isn't a state event.
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        var gamepad = device as Gamepad;
        if (gamepad == null)
        {
            // Event isn't for a gamepad or device ID is no longer valid.
            return;
        }

        var leftStickValue = gamepad.leftStick.ReadValueFromEvent(eventPtr);
    };
```
