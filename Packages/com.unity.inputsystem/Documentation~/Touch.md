---
uid: input-system-touch
---
# Touch support

Touch support is divided into:
* low-level support implemented in the [`Touchscreen`](#touchscreen-device) class.
* high-level support implemented in the [`EnhancedTouch.Touch`](#enhancedtouchtouch-class) class.

> [!NOTE]
> You should not use [`Touchscreen`](#touchscreen-device) for polling. If you want to read out touches similar to [`UnityEngine.Input.touches`](https://docs.unity3d.com/ScriptReference/Input-touches.html), see [`EnhancedTouch`](#enhancedtouchtouch-class). If you read out touch state from [`Touchscreen`](#touchscreen-device) directly inside of the `Update` or `FixedUpdate` methods, your app will miss changes in touch state.

Touch input is supported on Android, iOS, Windows, and the Universal Windows Platform (UWP).

> [!NOTE]
> To test your app on iOS or Android in the editor with touch input from your mobile device, you can use the Unity Remote as described [here](xref:input-system-debugging#unity-remote).

## `Touchscreen` Device

At the lowest level, a touch screen is represented by an [`InputSystem.Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) Device which captures the touch screen's raw state. Touch screens are based on the [`Pointer`](xref:input-system-pointers) layout.

To query the touch screen that was last used or last added, use [`Touchscreen.current`](xref:UnityEngine.InputSystem.Touchscreen.current).

### Controls

Additional to the [Controls inherited from `Pointer`](xref:input-system-pointers#controls), touch screen Devices implement the following Controls:

|Control|Type|Description|
|-------|----|-----------|
|[`primaryTouch`](xref:UnityEngine.InputSystem.Touchscreen.primaryTouch)|[`TouchControl`](xref:UnityEngine.InputSystem.Controls.TouchControl)|A touch Control that represents the primary touch of the screen. The primary touch drives the [`Pointer`](xref:input-system-pointers) representation on the Device.|
|[`touches`](xref:UnityEngine.InputSystem.Touchscreen.touches)|[`ReadOnlyArray<TouchControl>`](xref:UnityEngine.InputSystem.Controls.TouchControl)|An array of touch Controls that represents all the touches on the Device.|

A touch screen Device consists of multiple [`TouchControls`](xref:UnityEngine.InputSystem.Controls.TouchControl). Each of these represents a potential finger touching the Device. The [`primaryTouch`](xref:UnityEngine.InputSystem.Touchscreen.primaryTouch) Control represents the touch which is currently driving the [`Pointer`](xref:input-system-pointers) representation, and which should be used to interact with the UI. This is usually the first finger that touches the screen.

 [`primaryTouch`](xref:UnityEngine.InputSystem.Touchscreen.primaryTouch) is always identical to one of the entries in the [`touches`](xref:UnityEngine.InputSystem.Touchscreen.touches) array. The [`touches`](xref:UnityEngine.InputSystem.Touchscreen.touches) array contains all the touches that the system can track. This array has a fixed size, regardless of how many touches are currently active. If you need an API that only represents active touches, see the higher-level [`EnhancedTouch.Touch` class](#enhancedtouchtouch-class).

Each [`TouchControl`](xref:UnityEngine.InputSystem.Controls.TouchControl) on the Device, including [`primaryTouch`](xref:UnityEngine.InputSystem.Touchscreen.primaryTouch), is made up of the following child Controls:

|Control|Type|Description|
|-------|----|-----------|
|[`position`](xref:UnityEngine.InputSystem.Controls.TouchControl.position)|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|Absolute position on the touch surface.|
|[`delta`](xref:UnityEngine.InputSystem.Controls.TouchControl.delta)|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|The difference in `position` since the last frame.|
|[`startPosition`](xref:UnityEngine.InputSystem.Controls.TouchControl.startPosition)|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|The `position` where the finger first touched the surface.|
|[`startTime`](xref:UnityEngine.InputSystem.Controls.TouchControl.startTime)|[`DoubleControl`](xref:UnityEngine.InputSystem.Controls.IntegerControl)|The time when the finger first touched the surface.|
|[`press`](xref:UnityEngine.InputSystem.Controls.TouchControl.press)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Whether the finger is pressed down.|
|[`pressure`](xref:UnityEngine.InputSystem.Controls.TouchControl.pressure)|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)|Normalized pressure with which the finger is currently pressed while in contact with the pointer surface.|
|[`radius`](xref:UnityEngine.InputSystem.Controls.TouchControl.radius)|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|The size of the area where the finger touches the surface.|
|[`touchId`](xref:UnityEngine.InputSystem.Controls.TouchControl.touchId)|[`IntegerControl`](xref:UnityEngine.InputSystem.Controls.IntegerControl)|The ID of the touch. This allows you to distinguish individual touches.|
|[`phase`](xref:UnityEngine.InputSystem.Controls.TouchControl.phase)|[`TouchPhaseControl`](xref:UnityEngine.InputSystem.Controls.TouchPhaseControl)|A Control that reports the current  [`TouchPhase`](xref:UnityEngine.InputSystem.TouchPhase) of the touch.|
|[`tap`](xref:UnityEngine.InputSystem.Controls.TouchControl.tap)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|A button Control that reports whether the OS recognizes a tap gesture from this touch.|
|[`tapCount`](xref:UnityEngine.InputSystem.Controls.TouchControl.tapCount)|[`IntegerControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Reports the number of consecutive [`tap`](xref:UnityEngine.InputSystem.Controls.TouchControl.tap) reports from the OS. You can use this to detect double- and multi-tap gestures.|

### Using touch with Actions

You can use touch input with Actions, like any other [`Pointer`](xref:input-system-pointers) Device. To do this, [bind](xref:input-system-action-bindings) to the [pointer Controls](xref:input-system-pointers#controls), like `<Pointer>/press` or `<Pointer>/delta`. This gets input from the primary touch, and any other non-touch pointer Devices.

However, if you want to get input from multiple touches in your Action, you can bind to individual touches by using Bindings like `<Touchscreen>/touch3/press`. Alternatively, use a wildcard Binding to bind one Action to all touches: `<Touchscreen>/touch*/press`.

If you bind a single Action to input from multiple touches, you should set the Action type to [pass-through](xref:input-system-responding#pass-through) so the Action gets callbacks for each touch, instead of just one.

## `EnhancedTouch.Touch` Class

The [`EnhancedTouch.Touch`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch) class provides a polling API for touches similar to [`UnityEngine.Input.touches`](https://docs.unity3d.com/ScriptReference/Input-touches.html). You can use it to query touches on a frame-by-frame basis.

Because the API comes with a certain overhead due to having to record touches as they happen, you must explicitly enable it. To do this, call [`EnhancedTouchSupport.Enable()`](xref:UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable):

```
    using UnityEngine.InputSystem.EnhancedTouch;
    // ...
    // Can be called from MonoBehaviour.Awake(), for example. Also from any
    // RuntimeInitializeOnLoadMethod code.
    EnhancedTouchSupport.Enable();
```

> [!NOTE]
> [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) does not require [`EnhancedTouchSupport`](xref:UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport) to be enabled. You only need to call [`EnhancedTouchSupport.Enable()`](xref:UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable) if you want to use the [`EnhancedTouch.Touch`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch) API.

The [`EnhancedTouch.Touch`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch) API is designed to provide access to touch information along two dimensions:

1. By finger: Each finger is defined as the Nth contact source on a [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen). You can use  [Touch.activeFingers](xref:UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers) to get an array of all currently active fingers.

2. By touch: Each touch is a single finger contact with at least a beginning point ([`PointerPhase.Began`](xref:UnityEngine.InputSystem.TouchPhase)) and an endpoint ([`PointerPhase.Ended`](xref:UnityEngine.InputSystem.TouchPhase) or [`PointerPhase.Cancelled`](xref:UnityEngine.InputSystem.TouchPhase)). Between those two points, an arbitrary number of [`PointerPhase.Moved`](xref:UnityEngine.InputSystem.TouchPhase) and/or [`PointerPhase.Stationary`](xref:UnityEngine.InputSystem.TouchPhase) records exist. All records in a touch have the same [`touchId`](xref:UnityEngine.InputSystem.Controls.TouchControl.touchId). You can use  [Touch.activeTouches](xref:UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches) to get an array of all currently active touches. This lets you track how a specific touch moves over the screen, which is useful if you want to implement recognition of specific gestures.

See [`EnhancedTouch.Touch` API documentation](xref:UnityEngine.InputSystem.EnhancedTouch.Touch) for more details.

> [!NOTE]
> The [`Touch`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch) and [`Finger`](xref:UnityEngine.InputSystem.EnhancedTouch.Finger) APIs don't generate GC garbage. The bulk of the data is stored in unmanaged memory that is indexed by wrapper structs. All arrays are pre-allocated.

## Touch Simulation

Touch input can be simulated from input on other kinds of [Pointer](xref:input-system-pointers) devices such as [Mouse](xref:input-system-mouse) and [Pen](xref:input-system-pen) devices. To enable this, you can either add the [`TouchSimulation`](xref:UnityEngine.InputSystem.EnhancedTouch.TouchSimulation) `MonoBehaviour` to a `GameObject` in your scene or simply call [`TouchSimulation.Enable`](xref:UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable) somewhere in your startup code.

```CSharp
    void OnEnable()
    {
        TouchSimulation.Enable();
    }
```

In the editor, you can also enable touch simulation by toggling "Simulate Touch Input From Mouse or Pen" on in the "Options" dropdown of the [Input Debugger](xref:input-system-debugging).

[`TouchSimulation`](xref:UnityEngine.InputSystem.EnhancedTouch.TouchSimulation) will add a [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) device and automatically mirror input on any [`Pointer`](xref:UnityEngine.InputSystem.Pointer) device to the virtual touchscreen device.


## Reading all touches

To get all current touches from the touchscreen, use [`EnhancedTouch.Touch.activeTouches`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches), as in this example:

```C#
    using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

    public void Update()
    {
        foreach (var touch in Touch.activeTouches)
            Debug.Log($"{touch.touchId}: {touch.screenPosition},{touch.phase}");
    }
```

> [!NOTE]
> You must first enable enhanced touch support by calling  [`InputSystem.EnhancedTouchSupport.Enable()`](xref:UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable).

You can also use the lower-level [`Touchscreen.current.touches`](xref:UnityEngine.InputSystem.Touchscreen.touches) API.
