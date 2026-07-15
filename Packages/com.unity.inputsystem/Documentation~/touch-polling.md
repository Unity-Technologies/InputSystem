---
uid: input-system-polling-touch-input
---

# Touch polling

The [`EnhancedTouch.Touch`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch) class provides a [polling API](polling-actions.md) for touch input similar to [`UnityEngine.Input.touches`](https://docs.unity3d.com/ScriptReference/Input-touches.html). You can use it to query touches on a frame-by-frame basis.

> [!IMPORTANT]
> Don't use [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) for polling. If you read out touch state from `Touchscreen` directly inside of the `Update` or `FixedUpdate` methods, your application misses changes in touch state.

## Enable enhanced touch

Because the API comes with a certain overhead due to having to record touches as they happen, you must explicitly enable it. To do this, call [EnhancedTouchSupport.Enable](xref:UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable):

```c#

   using UnityEngine.InputSystem.EnhancedTouch;
    // ...
    // Can be called from MonoBehaviour.Awake(), for example. Also from any
    // RuntimeInitializeOnLoadMethod code.
    EnhancedTouchSupport.Enable();

```

> [!NOTE]
> You don't need to enable `EnhancedTouchSupport` if you're using the [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) class. You only need to call `EnhancedTouchSupport.Enable` if you want to use the `EnhancedTouch.Touch`] API.

## Read all touches

To get all current touches from the touchscreen, use [`EnhancedTouch.Touch.activeTouches`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches). You must first [enable enhanced touch support](#enable-enhanced-touch).

For example:

```c#

   using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

    public void Update()
    {
        foreach (var touch in Touch.activeTouches)
            Debug.Log($"{touch.touchId}: {touch.screenPosition},{touch.phase}");
    }

```

You can also use the lower-level [`Touchscreen.current.touches`](xref:UnityEngine.InputSystem.Touchscreen.touches) API to read all touches.
