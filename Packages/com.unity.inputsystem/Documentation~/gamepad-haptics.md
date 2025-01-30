# Gamepad haptics

The [Gamepad](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Gamepad.html) class implements the [IDualMotorRumble](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Haptics.IDualMotorRumble.html) interface that allows you to control the left and right motor speeds. In most common gamepads, the left motor emits a low-frequency rumble, and the right motor emits a high-frequency rumble.

```c

// Rumble the  low-frequency (left) motor at 1/4 speed and the high-frequency
// (right) motor at 3/4 speed.
Gamepad.current.SetMotorSpeeds(0.25f, 0.75f);

```

Important: Only the following platforms support rumble:

* PlayStation, Xbox, and Switch controllers on their respective consoles.   
* PlayStation controllers on macOS, Windows or Universal Windows Platform.  
* Xbox controllers on Windows.

## Pausing, resuming, and stopping haptics

[IDualMotorRumble](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Haptics.IDualMotorRumble.html) is based on [IHaptics](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Haptics.IHaptics.html), which is the base interface for any haptics support on any device. You can pause, resume, and reset haptic feedback using the [PauseHaptics](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Haptics.IHaptics.html#UnityEngine_InputSystem_Haptics_IHaptics_PauseHaptics), [ResumeHaptics](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Haptics.IHaptics.html#UnityEngine_InputSystem_Haptics_IHaptics_ResumeHaptics), and [ResetHaptics](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Haptics.IHaptics.html#UnityEngine_InputSystem_Haptics_IHaptics_ResetHaptics) methods respectively.

In certain situations, you might want to globally pause or stop haptics for all devices. For example, if the player enters an in-game menu, you can pause haptics while the player is in the menu, and then resume haptics once the player resumes the game. 

You can use the corresponding methods on [InputSystem](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputSystem.html) to achieve this result. These methods work the same way as device-specific methods, but affect all devices:

```c

// Pause haptics globally.
InputSystem.PauseHaptics();

// Resume haptics globally.
InputSystem.ResumeHaptics();

// Stop haptics globally.
InputSystem.ResetHaptics();

```

The difference between [PauseHaptics](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_PauseHaptics) and [ResetHaptics](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_ResetHaptics) is that the latter resets haptics playback state on each device to its initial state, whereas [PauseHaptics](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_PauseHaptics) preserves playback state in memory and only stops playback on the hardware.