---
uid: input-system-known-limitations
---
# Known Limitations

The following is a list of known limitations that the Input System currently has.

## Compatibility with other Unity features

* Input processing in the background is tied to `Application.runInBackground` (i.e. the "Run In Background" setting in "Player Preferences") which, however, Unity always forces to `true` in __development__ players. This means that in development players, input will always be processed, even if the app is in the background. Of course, this only pertains to platforms where the player can actually run in the background (iOS and Android are thus unaffected).
* `PlayerInput` split-screen support does not work with Cinemachine virtual cameras.
* The Input System cannot generate input for IMGUI.
* UI Toolkit can be used with `InputSystemUIInputModule` but only pointer (mouse, pen, touch) and gamepad input is supported at the moment. XR support is coming.
  * Also, UI Toolkit support currently requires use of an `EventSystem` setup in order to interface the Input System with UITK.

### uGUI

* After enabling, the UI will not react to a pointer's position until the position is changed.
* The new input system cannot yet feed text input into uGUI and TextMesh Pro input field components. This means that text input ATM is still picked up directly and internally from the Unity native runtime.
* The UI will not consume input such that it will not also trigger in-game actions.

## Device support

* Currently, devices whose input sources depend on application focus (generally, keyboards and pointers but can be any device depending on platform) will not automatically sync their current state when the app loses and subsequently regains focus. This means that, for example, if the W key is held when application comes back into the foreground, the key needs to be depressed and pressed again for the input to come through.
  * This is being worked on.
* (Desktop) We do not yet support distinguishing input from multiple pointers (mouse, pen, touch) or keyboards. There will be a single Mouse, Pen, Touch, and Keyboard device.
* (Windows) Pen input will not work with Wacom devices if "Windows Ink" support is turned off.
* (Windows) HID input is not currently supported in 32-bit players. This means that devices such as the PS4 controller will not work in 32-bit standalone players. Use the 64-bit standalone player instead.
* (Android) We only support a single Touchscreen at the moment.
* (Stadia) The Stadia controller is only supported __in__ the Stadia player at the moment. In the editor, use the generic `Gamepad` for bindings and use any Xbox or PS4 controller for testing.
* Joy-Cons are only supported on Switch.
* Sensors in the PS4 controller are currently only supported on PS4.
  >NOTE: Support for NDA platforms is distributed as separate packages due to licensing restrictions. The packages, at this point, are made available separately to licensees for download and installation.

## Features Supported by Old Input Manager

* `MonoBehaviour` mouse methods (`OnMouseEnter`, `OnMouseDrag`, etc) will not be called by the Input System.
* Unity Remote doesn't currently support the Input System. This is being worked on.
