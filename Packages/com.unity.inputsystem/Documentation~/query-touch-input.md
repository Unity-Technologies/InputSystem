# Query touch input in code

The [EnhancedTouch.Touch](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) class provides a \[polling API\]() for touch input similar to [UnityEngine.Input.touches](https://docs.unity3d.com/ScriptReference/Input-touches.html). You can use it to query touches on a frame-by-frame basis.

## Enable enhanced touch

Because the API comes with a certain overhead due to having to record touches as they happen, you must explicitly enable it. To do this, call [EnhancedTouchSupport.Enable()](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable):

```c

   using UnityEngine.InputSystem.EnhancedTouch;
    // ...
    // Can be called from MonoBehaviour.Awake(), for example. Also from any
    // RuntimeInitializeOnLoadMethod code.
    EnhancedTouchSupport.Enable();

```

[Touchscreen](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Touchscreen.html) does not require [EnhancedTouchSupport](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html) to be enabled. You only need to call [EnhancedTouchSupport.Enable()](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable) if you want to use the [EnhancedTouch.Touch](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) API.

## Read all touches

To get all current touches from the touchscreen, use [EnhancedTouch.Touch.activeTouches](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeTouches), as in this example:

```c

   using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

    public void Update()
    {
        foreach (var touch in Touch.activeTouches)
            Debug.Log($"{touch.touchId}: {touch.screenPosition},{touch.phase}");
    }

```

You must first enable enhanced touch support by calling [InputSystem.EnhancedTouchSupport.Enable()](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable). You can also use the lower-level [Touchscreen.current.touches](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches) API.