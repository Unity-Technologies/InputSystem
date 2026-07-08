---
uid: input-system-step-3-update-method
---

# Step 3 The Update method

You now have a Device in place along with its associated state format. You can call the following method to create a fully set-up Device with your two Controls on it:

```CSharp
InputSystem.AddDevice<MyDevice>();
```

However, this Device doesn't receive input yet, because you haven't added any code that generates input. To do that, you can use [`InputSystem.QueueStateEvent`](xref:UnityEngine.InputSystem.InputSystem) or [`InputSystem.QueueDeltaStateEvent`](xref:UnityEngine.InputSystem.InputSystem) from anywhere, including from a thread. The following example uses [`IInputUpdateCallbackReceiver`](xref:UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver), which, when implemented by any [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice), adds an [`OnUpdate()`](xref:UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver) method that automatically gets called during [`InputSystem.onBeforeUpdate`](xref:UnityEngine.InputSystem.InputSystem) and provides input events to the current input update.

> [!NOTE]
> If you already have a place where input for your device becomes available, you can skip this step and queue input events from there instead of using [`IInputUpdateCallbackReceiver`](xref:UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver).

```CSharp
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    //...

    public void OnUpdate()
    {
        // In practice, this would read out data from an external
        // API. This example uses some empty input.
        var state = new MyDeviceState();
        InputSystem.QueueStateEvent(this, state);
    }
}
```
