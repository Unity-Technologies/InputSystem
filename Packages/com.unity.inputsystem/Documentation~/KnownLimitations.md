# Known Limitations

The following is a little of known shortcomings that the Input System currently has.

## Compatibility with other Unity features

* `PlayerInput` split-screen support does not work with Cinemachine virtual cameras.
* The Input System cannot generate input for IMGUI or UIElements.

## Device support

* (Windows) Mouse input from remote desktop connections does not come through properly.
* (Windows) Pen input will not work with Wacom devices if "Windows Ink" support is turned off.
* Joy-Cons are only supported on Switch.
* Sensors in the PS4 controller are currently only supported on PS4.

## Features Supported by Old Input Manager

* `MonoBehaviour` mouse methods (`OnMouseEnter`, `OnMouseDrag`, etc) will not be called by the Input System.
