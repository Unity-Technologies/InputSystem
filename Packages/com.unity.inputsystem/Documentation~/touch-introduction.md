---
uid: input-system-touch-devices-intro
---

# Touch devices introduction

You can receive and process touch input from touch devices with the following APIs:

* [High-level support](#high-level-touch-support) implemented in the [`EnhancedTouch.Touch`](xref:UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport) class. This class provides in-built functionality for finger and touch information, and you can also use it for [polling touch input](touch-polling.md).
* [Low-level support](#low-level-touch-support) implemented in the [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) class. Use this class to implement custom functionality that `EnhancedTouch.Touch` doesn't have.

For a list of platforms that support touch devices, refer to [Supported devices reference](supported-devices-reference.md).

## High-level touch support

The [`EnhancedTouch.Touch`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch) API provides access to touch information along two dimensions:

* **By finger**: Each finger is defined as the Nth contact source on a [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen). Use [`Touch.activeFingers`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers) to get an array of all currently active fingers.
* **By touch**: Each touch is a single finger contact with at least a beginning point ([`PointerPhase.Began`](xref:UnityEngine.InputSystem.TouchPhase.Began)) and an endpoint ([`PointerPhase.Ended`](xref:UnityEngine.InputSystem.TouchPhase.Ended) or [`PointerPhase.Cancelled`](xref:UnityEngine.InputSystem.TouchPhase.Canceled)). Between those two points, an arbitrary number of [`PointerPhase.Moved`](xref:UnityEngine.InputSystem.TouchPhase.Moved) or [`PointerPhase.Stationary`](xref:UnityEngine.InputSystem.TouchPhase.Stationary) records exist. All records in a touch have the same [`touchId`](xref:UnityEngine.InputSystem.Controls.TouchControl.touchId). You can use [`Touch.activeTouches`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches) to get an array of all currently active touches. This lets you track how a specific touch moves over the screen, which is useful if you want to implement recognition of specific gestures.

Refer to the [`EnhancedTouch.Touch` API documentation](xref:UnityEngine.InputSystem.EnhancedTouch.Touch) for more information.

> [!NOTE]
> The [`Touch`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch) and [`Finger`](xref:UnityEngine.InputSystem.EnhancedTouch.Finger) APIs don't allocate managed memory, and therefore don't cause [garbage collection](xref:um-performance-garbage-collector). The bulk of the data is stored in unmanaged memory that's indexed by wrapper structs. All arrays are pre-allocated.

## Low-level touch support

At the lowest level, a touch screen is represented by an [`InputSystem.Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) device which captures the touchscreen's raw state. Touch screens are based on the [pointer](pointers-introduction.md) layout, and they implement some additional controls outlined in the [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) API documentation.

To query the touch screen that was last used or last added, use [`Touchscreen.current`](xref:UnityEngine.InputSystem.Touchscreen.current).

### API controls

A touch screen device consists of multiple [`TouchControl`](xref:UnityEngine.InputSystem.Controls.TouchControl) instances. Each of these represents a finger which is touching the device. The [`primaryTouch`](xref:UnityEngine.InputSystem.Touchscreen.primaryTouch) control represents the touch which is currently driving the [pointer](pointers-introduction.md) representation, and which should be used to interact with the UI.

The `primaryTouch` control usually represents the first finger that touches the screen and is always identical to one of the entries in the [`touches`](xref:UnityEngine.InputSystem.Touchscreen.touches) array. The `touches` array contains all the touches that the system can track. This array has a fixed size, regardless of how many touches are currently active. If you need an API that only represents active touches, refer to the higher-level [`EnhancedTouch.Touch` class](#high-level-touch-support).

## Testing touch devices

To test your app on iOS or Android in the unity Editor with touch input from your mobile device, use the [Unity Remote](use-mobile-device-input-editor-unity-remote.md).
