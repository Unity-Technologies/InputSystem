---
uid: input-system-step-3-update-method
---

# Step 3 The Update method 

You now have a Device in place along with its associated state format. You can call the following method to create a fully set-up Device with your two Controls on it:

```CSharp
InputSystem.AddDevice<MyDevice>();
```

However, this Device doesn't receive input yet, because you haven't added any code that generates input. To do that, you can use [`InputSystem.QueueStateEvent`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_QueueStateEvent__1_UnityEngine_InputSystem_InputDevice___0_System_Double_) or [`InputSystem.QueueDeltaStateEvent`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_QueueDeltaStateEvent__1_UnityEngine_InputSystem_InputControl___0_System_Double_) from anywhere, including from a thread. The following example uses [`IInputUpdateCallbackReceiver`](../api/UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver.html), which, when implemented by any [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html), adds an [`OnUpdate()`](../api/UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver.html#UnityEngine_InputSystem_LowLevel_IInputUpdateCallbackReceiver_OnUpdate) method that automatically gets called during [`InputSystem.onBeforeUpdate`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onBeforeUpdate) and provides input events to the current input update.

>__Note__: If you already have a place where input for your device becomes available, you can skip this step and queue input events from there instead of using [`IInputUpdateCallbackReceiver`](../api/UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver.html).

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
