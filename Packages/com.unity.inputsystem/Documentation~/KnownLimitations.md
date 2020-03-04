# Known Limitations

The following is a list of known limitations that the Input System currently has.

## Compatibility with other Unity features

* `PlayerInput` split-screen support does not work with Cinemachine virtual cameras.
* The Input System cannot generate input for IMGUI or UIElements.
* The Input System does not yet support the new 2019.3 mode where domain reloads are disabled when entering play mode.

### uGUI

* After enabling, the UI will not react to a pointer's position until the position is changed.

## Device support

* (Windows) Mouse input from remote desktop connections does not come through properly.
* (Windows) Pen input will not work with Wacom devices if "Windows Ink" support is turned off.
* Joy-Cons are only supported on Switch.
* Sensors in the PS4 controller are currently only supported on PS4.
  >NOTE: Support for NDA platforms is distributed as separate packages due to licensing restrictions. The packages, at this point, are made available separately to licensees for download and installation.

## Features Supported by Old Input Manager

* `MonoBehaviour` mouse methods (`OnMouseEnter`, `OnMouseDrag`, etc) will not be called by the Input System.
