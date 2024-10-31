---
uid: input-system-touch
---
# Touch support

- [Touch support](#touch-support)
  - [`Touchscreen` Device](#touchscreen-device)
    - [Controls](#controls)
    - [Using touch with Actions](#using-touch-with-actions)
  - [`EnhancedTouch.Touch` Class](#enhancedtouchtouch-class)
  - [Touch Simulation](#touch-simulation)
  - [Reading all touches](#reading-all-touches)

Touch support is divided into:
* low-level support implemented in the [`Touchscreen`](#touchscreen-device) class.
* high-level support implemented in the [`EnhancedTouch.Touch`](#enhancedtouchtouch-class) class.

>__Note__: You should not use [`Touchscreen`](#touchscreen-device) for polling. If you want to read out touches similar to [`UnityEngine.Input.touches`](https://docs.unity3d.com/ScriptReference/Input-touches.html), see [`EnhancedTouch`](#enhancedtouchtouch-class). If you read out touch state from [`Touchscreen`](#touchscreen-device) directly inside of the `Update` or `FixedUpdate` methods, your app will miss changes in touch state.

Touch input is supported on Android, iOS, Windows, and the Universal Windows Platform (UWP).

>__Note__: To test your app on iOS or Android in the editor with touch input from your mobile device, you can use the Unity Remote as described [here](Debugging.md#unity-remote).

## `Touchscreen` Device

At the lowest level, a touch screen is represented by an [`InputSystem.Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) Device which captures the touch screen's raw state. Touch screens are based on the [`Pointer`](Pointers.md) layout.

To query the touch screen that was last used or last added, use [`Touchscreen.current`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_current).

### Controls

Additional to the [Controls inherited from `Pointer`](Pointers.md#controls), touch screen Devices implement the following Controls:

|Control|Type|Description|
|-------|----|-----------|
|[`primaryTouch`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_primaryTouch)|[`TouchControl`](../api/UnityEngine.InputSystem.Controls.TouchControl.html)|A touch Control that represents the primary touch of the screen. The primary touch drives the [`Pointer`](Pointers.md) representation on the Device.|
|[`touches`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches)|[`ReadOnlyArray<TouchControl>`](../api/UnityEngine.InputSystem.Controls.TouchControl.html)|An array of touch Controls that represents all the touches on the Device.|

A touch screen Device consists of multiple [`TouchControls`](../api/UnityEngine.InputSystem.Controls.TouchControl.html). Each of these represents a potential finger touching the Device. The [`primaryTouch`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_primaryTouch) Control represents the touch which is currently driving the [`Pointer`](Pointers.md) representation, and which should be used to interact with the UI. This is usually the first finger that touches the screen.

 [`primaryTouch`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_primaryTouch) is always identical to one of the entries in the [`touches`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches) array. The [`touches`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches) array contains all the touches that the system can track. This array has a fixed size, regardless of how many touches are currently active. If you need an API that only represents active touches, see the higher-level [`EnhancedTouch.Touch` class](#enhancedtouchtouch-class).

Each [`TouchControl`](../api/UnityEngine.InputSystem.Controls.TouchControl.html) on the Device, including [`primaryTouch`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_primaryTouch), is made up of the following child Controls:

|Control|Type|Description|
|-------|----|-----------|
|[`position`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_position)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|Absolute position on the touch surface.|
|[`delta`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_delta)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|The difference in `position` since the last frame.|
|[`startPosition`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_startPosition)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|The `position` where the finger first touched the surface.|
|[`startTime`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_startTime)|[`DoubleControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|The time when the finger first touched the surface.|
|[`press`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_press)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Whether the finger is pressed down.|
|[`pressure`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_pressure)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|Normalized pressure with which the finger is currently pressed while in contact with the pointer surface.|
|[`radius`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_radius)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|The size of the area where the finger touches the surface.|
|[`touchId`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_touchId)|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|The ID of the touch. This allows you to distinguish individual touches.|
|[`phase`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_phase)|[`TouchPhaseControl`](../api/UnityEngine.InputSystem.Controls.TouchPhaseControl.html)|A Control that reports the current  [`TouchPhase`](../api/UnityEngine.InputSystem.TouchPhase.html) of the touch.|
|[`tap`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_tap)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|A button Control that reports whether the OS recognizes a tap gesture from this touch.|
|[`tapCount`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_tapCount)|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Reports the number of consecutive [`tap`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_tap) reports from the OS. You can use this to detect double- and multi-tap gestures.|

### Using touch with Actions

You can use touch input with Actions, like any other [`Pointer`](Pointers.md) Device. To do this, [bind](ActionBindings.md) to the [pointer Controls](Pointers.md#controls), like `<Pointer>/press` or `<Pointer>/delta`. This gets input from the primary touch, and any other non-touch pointer Devices.

However, if you want to get input from multiple touches in your Action, you can bind to individual touches by using Bindings like `<Touchscreen>/touch3/press`. Alternatively, use a wildcard Binding to bind one Action to all touches: `<Touchscreen>/touch*/press`.

If you bind a single Action to input from multiple touches, you should set the Action type to [pass-through](RespondingToActions.md#pass-through) so the Action gets callbacks for each touch, instead of just one.

## `EnhancedTouch.Touch` Class

The [`EnhancedTouch.Touch`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) class provides a polling API for touches similar to [`UnityEngine.Input.touches`](https://docs.unity3d.com/ScriptReference/Input-touches.html). You can use it to query touches on a frame-by-frame basis.

Because the API comes with a certain overhead due to having to record touches as they happen, you must explicitly enable it. To do this, call [`EnhancedTouchSupport.Enable()`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable):

```
    using UnityEngine.InputSystem.EnhancedTouch;
    // ...
    // Can be called from MonoBehaviour.Awake(), for example. Also from any
    // RuntimeInitializeOnLoadMethod code.
    EnhancedTouchSupport.Enable();
```

>__Note__: [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) does not require [`EnhancedTouchSupport`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html) to be enabled. You only need to call [`EnhancedTouchSupport.Enable()`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable) if you want to use the [`EnhancedTouch.Touch`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) API.

The [`EnhancedTouch.Touch`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) API is designed to provide access to touch information along two dimensions:

1. By finger: Each finger is defined as the Nth contact source on a [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html). You can use  [Touch.activeFingers](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeFingers) to get an array of all currently active fingers.

2. By touch: Each touch is a single finger contact with at least a beginning point ([`PointerPhase.Began`](../api/UnityEngine.InputSystem.TouchPhase.html)) and an endpoint ([`PointerPhase.Ended`](../api/UnityEngine.InputSystem.TouchPhase.html) or [`PointerPhase.Cancelled`](../api/UnityEngine.InputSystem.TouchPhase.html)). Between those two points, an arbitrary number of [`PointerPhase.Moved`](../api/UnityEngine.InputSystem.TouchPhase.html) and/or [`PointerPhase.Stationary`](../api/UnityEngine.InputSystem.TouchPhase.html) records exist. All records in a touch have the same [`touchId`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_touchId). You can use  [Touch.activeTouches](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeTouches) to get an array of all currently active touches. This lets you track how a specific touch moves over the screen, which is useful if you want to implement recognition of specific gestures.

See [`EnhancedTouch.Touch` API documentation](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) for more details.

>__Note__: The [`Touch`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) and [`Finger`](../api/UnityEngine.InputSystem.EnhancedTouch.Finger.html) APIs don't generate GC garbage. The bulk of the data is stored in unmanaged memory that is indexed by wrapper structs. All arrays are pre-allocated.

## Touch Simulation

Touch input can be simulated from input on other kinds of [Pointer](./Pointers.md) devices such as [Mouse](./Mouse.md) and [Pen](./Pen.md) devices. To enable this, you can either add the [`TouchSimulation`](../api/UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.html) `MonoBehaviour` to a `GameObject` in your scene or simply call [`TouchSimulation.Enable`](../api/UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.html#UnityEngine_InputSystem_EnhancedTouch_TouchSimulation_Enable) somewhere in your startup code.

```CSharp
    void OnEnable()
    {
        TouchSimulation.Enable();
    }
```

In the editor, you can also enable touch simulation by toggling "Simulate Touch Input From Mouse or Pen" on in the "Options" dropdown of the [Input Debugger](./Debugging.md).

[`TouchSimulation`](../api/UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.html) will add a [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) device and automatically mirror input on any [`Pointer`](../api/UnityEngine.InputSystem.Pointer.html) device to the virtual touchscreen device.


## Reading all touches

To get all current touches from the touchscreen, use [`EnhancedTouch.Touch.activeTouches`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeTouches), as in this example:

```C#
    using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

    public void Update()
    {
        foreach (var touch in Touch.activeTouches)
            Debug.Log($"{touch.touchId}: {touch.screenPosition},{touch.phase}");
    }
```

>__Note__: You must first enable enhanced touch support by calling  [`InputSystem.EnhancedTouchSupport.Enable()`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable).

You can also use the lower-level [`Touchscreen.current.touches`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches) API.
