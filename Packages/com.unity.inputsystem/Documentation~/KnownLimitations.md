# Known Limitations

The following is a list of known limitations that the Input System currently has.

## Actions

* Actions cannot currently "pre-empt" each other's input. Meaning that it is currently not possible to "consume" input from one action to prevent it from triggering input on another action.
  - A common scenario is having, for example, a binding for "A" on one action and a binding for "SHIFT+A" on another action. Currently, pressing "SHIFT+A" will trigger both actions.

## Compatibility with other Unity features

* Input processing in the background is tied to `Application.runInBackground` (i.e. the "Run In Background" setting in "Player Preferences") which, however, Unity always forces to `true` in __development__ players. This means that in development players, input will always be processed, even if the app is in the background. Of course, this only pertains to platforms where the player can actually run in the background (iOS and Android are thus unaffected).
* `PlayerInput` split-screen support does not work with Cinemachine virtual cameras.
* The Input System cannot generate input for IMGUI or UIElements. Support for the latter is being worked on.
* The Input System does not yet support the new 2019.3 mode where domain reloads are disabled when entering play mode.

### uGUI

* After enabling, the UI will not react to a pointer's position until the position is changed.
* The new input system cannot yet feed text input into uGUI and TextMesh Pro input field components. This means that text input ATM is still picked up directly and internally from the Unity native runtime.

## Device support

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
