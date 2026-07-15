---
uid: input-system-gamepad-haptics
---

# Gamepad haptics

The [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) class implements the [`IDualMotorRumble`](xref:UnityEngine.InputSystem.Haptics.IDualMotorRumble) interface that allows you to control the left and right motor speeds. In most common gamepads, the left motor emits a low-frequency rumble, and the right motor emits a high-frequency rumble.

```c#

// Rumble the  low-frequency (left) motor at 1/4 speed and the high-frequency
// (right) motor at 3/4 speed.
Gamepad.current.SetMotorSpeeds(0.25f, 0.75f);

```

Only the following combinations of devices/OSes currently support rumble:

* PS4, Xbox, and Nintendo Switch controllers, when connected to their respective consoles. Only supported if you install console-specific input packages in your Project.
* PS4 controllers, when connected to Mac or Windows/UWP computers.
* Xbox controllers on Windows.
* Gamepads on Android. Rumble support varies with Android OS version and requires Unity 6000.6 or later.
    * Rumble automatically stops about 10 seconds after the last. [`SetMotorSpeeds`](xref:UnityEngine.InputSystem.Haptics.IDualMotorRumble) call unless you explicitly stop it.
    * Android 12 or later:
        * Supports most Bluetooth-connected gamepads with rumble support including PS4, PS5, and Xbox controllers.
        *  If the gamepad is connected with a USB cable, rumble support might vary.
        *  If the gamepad supports more than one motor, you can control the speed of each motor individually.
    * Android 11 or earlier:
        * Rumble support is limited.
        * Only one motor is supported. If the gamepad has more than one motor, the higher value of the left and right motor speed applies to both.
        * On some devices, motor speed control is limited. You can only set the left and right motor speed to either `1.0` (maximum speed) or `0.0` (turned off).
* Gamepads on iOS, tvOS, and visionOS. Requires Unity 6000.7 or later.
    * Supports rumble on gamepads for which Apple's GameController framework exposes haptics. Supported gamepads include PS4, PS5, Xbox, and Nintendo Switch Pro controllers.
    * If the gamepad supports left and right motors, you can control the speed of each motor individually. Otherwise, the higher value of the left and right motor speed applies to all motors on the gamepad.
    * The first [`SetMotorSpeeds`](xref:UnityEngine.InputSystem.Haptics.IDualMotorRumble) call allocates rumble resources, which adds latency. When you set all motor speeds to `0.0`, an inactivity timer starts. After 2 minutes, the system releases rumble resources to preserve the controller's battery. The next [`SetMotorSpeeds`](xref:UnityEngine.InputSystem.Haptics.IDualMotorRumble) call reallocates resources, adding further latency.
    * Not every controller model exposes haptics through Apple's GameController framework. To confirm rumble support for a given controller, it's recommended test using a native application.

## Pausing, resuming, and stopping haptics

[`IDualMotorRumble`](xref:UnityEngine.InputSystem.Haptics.IDualMotorRumble) is based on [`IHaptics`](xref:UnityEngine.InputSystem.Haptics.IHaptics), which is the base interface for any haptics support on any device. You can pause, resume, and reset haptic feedback using the [`PauseHaptics`](xref:UnityEngine.InputSystem.Haptics.IHaptics.PauseHaptics), [`ResumeHaptics`](xref:UnityEngine.InputSystem.Haptics.IHaptics.ResumeHaptics), and [`ResetHaptics`](xref:UnityEngine.InputSystem.Haptics.IHaptics.ResetHaptics) methods respectively.

In certain situations, you might want to globally pause or stop haptics for all devices. For example, if the player enters an in-game menu, you can pause haptics while the player is in the menu, and then resume haptics once the player resumes the game.

You can use the corresponding methods on [`InputSystem`](xref:UnityEngine.InputSystem.InputSystem) to achieve this result. These methods work the same way as device-specific methods, but affect all devices:

```c#

// Pause haptics globally.
InputSystem.PauseHaptics();

// Resume haptics globally.
InputSystem.ResumeHaptics();

// Stop haptics globally.
InputSystem.ResetHaptics();

```

The difference between `PauseHaptics` and `ResetHaptics` is that the latter resets haptics playback state on each device to its initial state, whereas `PauseHaptics` preserves playback state in memory and only stops playback on the hardware.
