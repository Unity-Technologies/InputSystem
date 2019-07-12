# Touch Support

* [`Touchscreen` Device](#touchscreen-device)
* [`Touch` Class](#touch-class)
    * [Using Touch with Actions](#using-touch-with-actions)
* [Gesture Support](#gesture-support)

Touch support is divided into low-level support in the form of [`Touchscreen`](#touchscreen-device) and into high-level support in the form of the [`Touch`](#touch-class) class.

|Platform|Touch Supported|Remarks|
|--------|---------------|-------|
|Android||
|iOS||
|Windows Classic||
|Windows UWP||
|MacOS||
|Linux||
|Switch||

## `Touchscreen` Device

At the lowest level, a touchscreen is represented by a `Touchscreen` device which captures the raw state of the touchscreen.

|Control|Type|Description|
|-------|----|-----------|

Each `TouchControl` on the device (including `primaryTouch`) is made up of the following child controls.

|Control|Type|Description|
|-------|----|-----------|

### Using Touch with [Actions](Actions.md)

## `Touch` Class

Enhanced touch support is provided by the `EnhancedTouch` plugin. To enable it, call `EnhancedTouchSupport.Enable()`.

```
    // Can be called from MonoBehaviour.Awake(), for example. Also from any
    // RuntimeInitializeOnLoadMethod code.
    EnhancedTouchSupport.Enable();
```

>NOTE: `Touchscreen` does __NOT__ require `EnhancedTouchSupport` to be enabled. This also means that touch in combination with [actions](Actions.md) works fine without `EnhancedTouchSupport`. Calling `EnhancedTouchSupport.Enable()` is only required if you want to use the `Touch` API.

The touch API is designed to provide access to touch information along two dimensions:

1. By finger.

   Each finger is defined as the Nth contact source on a `Touchscreen`.
2. By touch.

   Each touch is a single finger contact with at least a beginning point (`PointerPhase.Began`) and an endpoint (`PointerPhase.Ended` or `PointerPhase.Cancelled`). In-between those two points may be arbitrary many `PointerPhase.Moved` and/or `PointerPhase.Stationary` records. All records in a touch will have the same `touchId`.

>NOTE: The `Touch` and `Touch.Finger` API is written in a way that does not generate GC garbage and does not require object pooling either. The bulk of the data is stored in unmanaged memory that is indexed by wrapper structs. All arrays are pre-allocated.

### `Touch.Finger` Class

### A Note About Fixed/Dynamic/Editor Updates

The information captured by the `Touch` class has to track input seen from a specific perspective.

## Gesture Support
