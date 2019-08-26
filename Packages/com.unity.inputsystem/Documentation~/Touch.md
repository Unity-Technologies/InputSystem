# Touch Support

* [`Touchscreen` Device](#touchscreen-device)
* [`Touch` Class](#touch-class)
    * [Using Touch with Actions](#using-touch-with-actionsactionsmd)

Touch support is divided into low-level support in the form of [`Touchscreen`](#touchscreen-device) and into high-level support in the form of the [`Touch`](#touch-class) class.

Touch input is supported on Android, iOS, UWP and Windows.

## `Touchscreen` Device

At the lowest level, a touch screen is represented by a [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) device which captures the raw state of the touchscreen. Touch screens are based on the [`Pointer`](Pointers.md) layout.

The last used or last added touch screen can be queried with [`Touchscreen.current`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_current).

### Controls

Additional to the [controls inherited from `Pointer`](Pointers.md#controls), touch screen devices implement the following controls:

|Control|Type|Description|
|-------|----|-----------|
|[`primaryTouch`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_primaryTouch)|[`TouchControl`](../api/UnityEngine.InputSystem.Controls.TouchControl.html)|A touch control representing the primary touch of the screen (the primary touch is the touch driving the [`Pointer`](Pointers.md) representation of the device).|
|[`touches`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches)|[`ReadOnlyArray<TouchControl>`](../api/UnityEngine.InputSystem.Controls.TouchControl.html)|An array of touch controls, representing all the touches on the device.|

As you can see, a touch screen device consists of multiple [`TouchControls`](../api/UnityEngine.InputSystem.Controls.TouchControl.html). Each of these represents a potential finger touching the device. The [`primaryTouch`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_primaryTouch) control will represent the touch which is currently driving the [`Pointer`](Pointers.md) representation, and which should be used to interacting with the UI. This is usually the first finger to have touched the screen. [`primaryTouch`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_primaryTouch) will always be identical to one of the entries in the [`touches`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches) array. The [`touches`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches) array contains all touches the system can track. Note that this array has a fixed size of [`TouchscreenState.MaxTouches`](../api/UnityEngine.InputSystem.LowLevel.TouchscreenState.html#UnityEngine_InputSystem_LowLevel_TouchscreenState_MaxTouches) - regardless of how many fingers are currently active. If you need an API which only represents active touches, you can look at the higher-level [`Touch` class](#touch-class).

Each [`TouchControl`](../api/UnityEngine.InputSystem.Controls.TouchControl.html) on the device (including [`primaryTouch`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_primaryTouch) is made up of the following child controls.

|Control|Type|Description|
|-------|----|-----------|
|[`position`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_position)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|Absolute position on the touch surface.|
|[`delta`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_delta)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|The difference in `position` since the last frame.|
|[`startPosition`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_startPosition)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|The `position` where the finger first touched the surface.|
|[`startTime`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_startTime)|[`DoubleControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|The time when the finger first touched the surface.|
|[`press`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_press)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Whether the finger is pressed down.|
|[`pressure`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_pressure)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|Normalized pressure with which the finger is currently pressed while in contact with the pointer surface.|
|[`radius`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_radius)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|The size of the area where the finger touches the surface.|
|[`touchId`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_touchId)|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|The id of the touch, used to distinguish individual touches.|
|[`phase`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_phase)|[`TouchPhaseControl`](../api/UnityEngine.InputSystem.Controls.TouchPhaseControl.html)|A control reporting the current  [`TouchPhase`](../api/UnityEngine.InputSystem.TouchPhase.html) of the touch.|
|[`tap`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_tap)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|A button control which reports whether the OS recognizes a "tap" gesture from this touch.|
|[`tapCount`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_tapCount)|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|If [`tap`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_tap) is reported as pressed, `tapCount` will report the number of consecutive taps as recognized by the OS. Can be used to detect double- and multi-tap gestures.|

### Using Touch with [Actions](Actions.md)

Touch input can be used with actions like any other [`Pointer`](Pointers.md) device, by [binding](ActionBindings.md) to `<Pointer>/press`, `<Pointer>/delta`, etc. This will get you input from the primary touch (as well as from any other non-touch pointer devices). However, if you care about getting input from multiple touches in your action, you can bind to individual touches by using bindings like `<Touchscreen>/touch3/press`, or use a wildcard binding to bind one action to all touches like this: `<Touchscreen>/touch*/press`. If you bind a single action to input from multiple touches like that, you will likely want to set the action type to [Pass-Through](Actions.md#pass-through), so the action get callbacks for each touch, instead of just from one.

## `Touch` Class

Enhanced touch support is provided by the [`EnhancedTouch.Touch`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) class. To enable it, call [`EnhancedTouchSupport.Enable()`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable).

```
    using UnityEngine.InputSystem.EnhancedTouch;
    // ...
    // Can be called from MonoBehaviour.Awake(), for example. Also from any
    // RuntimeInitializeOnLoadMethod code.
    EnhancedTouchSupport.Enable();
```

>NOTE: [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) does __NOT__ require [`EnhancedTouchSupport`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html) to be enabled. This also means that touch in combination with [actions](Actions.md) works fine without [`EnhancedTouchSupport`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html). Calling [`EnhancedTouchSupport.Enable()`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable) is only required if you want to use the [`EnhancedTouch.Touch`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) API.

The touch API is designed to provide access to touch information along two dimensions:

1. By finger.

   Each finger is defined as the Nth contact source on a [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html). You can use  [Touch.activeFingers](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeFingers) to get an array of all currently active fingers.

2. By touch.

   Each touch is a single finger contact with at least a beginning point ([`PointerPhase.Began`](../api/UnityEngine.InputSystem.TouchPhase.html)) and an endpoint ([`PointerPhase.Ended`](../api/UnityEngine.InputSystem.TouchPhase.html) or [`PointerPhase.Cancelled`](../api/UnityEngine.InputSystem.TouchPhase.html)). In-between those two points may be arbitrary many [`PointerPhase.Moved`](../api/UnityEngine.InputSystem.TouchPhase.html) and/or [`PointerPhase.Stationary`](../api/UnityEngine.InputSystem.TouchPhase.html) records. All records in a touch will have the same [`touchId`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_touchId). You can use  [Touch.activeTouches](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeTouches) to get an array of all currently active touches. This lets you track how a specific touch has moved over the screen, which is useful if you want to implement e.g. recognition of specific gestures.

See the [Scripting API Reference for the `EnhancedTouch.Touch`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) API for more info.

>NOTE: The [`Touch`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html) and [`Finger`](../api/UnityEngine.InputSystem.EnhancedTouch.Finger.html) API is written in a way that does not generate GC garbage and does not require object pooling either. The bulk of the data is stored in unmanaged memory that is indexed by wrapper structs. All arrays are pre-allocated.
