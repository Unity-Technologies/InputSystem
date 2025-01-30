# Touch devices

Touch devices are divided into the following categories:

* Low-level support implemented in the [Touchscreen](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Touchscreen.html) class.  
* High-level support implemented in the [EnhancedTouch.Touch](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html) class.

For a list of platforms that support touch devices, refer to \[Platform support\].

## Low-level touch support

At the lowest level, a touch screen is represented by an [InputSystem.Touchscreen](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Touchscreen.html) device which captures the touch screen's raw state. Touch screens are based on the [Pointer](http://localhost:57437/com.unity.inputsystem@1.12/manual/pointers.html) layout, and they implement some additional controls outlined in the [Touchscreen](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Touchscreen.html) API documentation.

To query the touch screen that was last used or last added, use [Touchscreen.current](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_current).

Note: Don't use [Touchscreen](http://localhost:57437/com.unity.inputsystem@1.12/manual/Touch.html#touchscreen-device) for \[polling\](). If you want to read out touches similar to [UnityEngine.Input.touches](https://docs.unity3d.com/ScriptReference/Input-touches.html), use [EnhancedTouch](http://localhost:57437/com.unity.inputsystem@1.12/manual/Touch.html#enhancedtouchtouch-class). If you read out touch state from [Touchscreen](http://localhost:57437/com.unity.inputsystem@1.12/manual/Touch.html#touchscreen-device) directly inside of the Update or FixedUpdate methods, your app will miss changes in touch state.

### API controls

A touch screen device consists of multiple [TouchControl](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Controls.TouchControl.html) instances. Each of these represents a finger which is touching the device. The [primaryTouch](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_primaryTouch) control represents the touch which is currently driving the [Pointer](http://localhost:57437/com.unity.inputsystem@1.12/manual/pointers.html) representation, and which should be used to interact with the UI. 

The primaryTouch control usually represents the first finger that touches the screen and is always identical to one of the entries in the [touches](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches) array. The [touches](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches) array contains all the touches that the system can track. This array has a fixed size, regardless of how many touches are currently active. If you need an API that only represents active touches, refer to the higher-level [EnhancedTouch.Touch class](http://localhost:57437/com.unity.inputsystem@1.12/manual/Touch.html#enhancedtouchtouch-class).

## High-level touch support

The [EnhancedTouch.Touch](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) API provides access to touch information along two dimensions:

* By finger: Each finger is defined as the Nth contact source on a [Touchscreen](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Touchscreen.html). You can use [Touch.activeFingers](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeFingers) to get an array of all currently active fingers.  
* By touch: Each touch is a single finger contact with at least a beginning point ([PointerPhase.Began](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.TouchPhase.html)) and an endpoint ([PointerPhase.Ended](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.TouchPhase.html) or [PointerPhase.Cancelled](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.TouchPhase.html)). Between those two points, an arbitrary number of [PointerPhase.Moved](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.TouchPhase.html) and/or [PointerPhase.Stationary](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.TouchPhase.html) records exist. All records in a touch have the same [touchId](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_touchId). You can use [Touch.activeTouches](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeTouches) to get an array of all currently active touches. This lets you track how a specific touch moves over the screen, which is useful if you want to implement recognition of specific gestures.

See [EnhancedTouch.Touch API documentation](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) for more details.

Note: The [Touch](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) and [Finger](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.Finger.html) APIs don't generate GC garbage. The bulk of the data is stored in unmanaged memory that is indexed by wrapper structs. All arrays are pre-allocated.

## Testing touch devices

To test your app on iOS or Android in the editor with touch input from your mobile device, you can use the Unity Remote as described [here](http://localhost:57437/com.unity.inputsystem@1.12/manual/Debugging.html#unity-remote).