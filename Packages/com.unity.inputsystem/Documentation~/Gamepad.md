# Gamepad Support

A [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) is narrowly defined as a Device with two thumbsticks, a D-pad, and four face buttons. Additionally, gamepads usually have two shoulder and two trigger buttons. Most gamepads also have two buttons in the middle.

A gamepad may have additional Controls, such as a gyro, which are exposed from the Device. However, all gamepads are guaranteed to have at least the minimum set of Controls described above.

Gamepad support guarantees the correct location and functioning of Controls across platforms and hardware. A PS4 DualShock controller, for example, is meant to look identical regardless of which platform it is supported on. A gamepad's south face button is meant to be expected to always be the lowermost face button.

## Controls

Every gamepad has the following Controls:

|Control|Type|Description|
|-------|----|-----------|
|[`leftStick`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_leftStick)|[`StickControl`](../api/UnityEngine.InputSystem.Controls.StickControl.html)|Thumbstick on the left side of the gamepad. Deadzoned. Provides a normalized 2D motion vector. X is [-1..1] from left to right, Y is [-1..1] from bottom to top. Has up/down/left/right buttons for use in D-pad-like fashion.|
|[`rightStick`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_rightStick)|[`StickControl`](../api/UnityEngine.InputSystem.Controls.StickControl.html)|Thumbstick on the right side of the gamepad. Deadzoned. Provides a normalized 2D motion vector. X is [-1..1] from left to right, Y is [-1..1] from bottom to top. Has up/down/left/right buttons for use in D-pad-like fashion.|
|[`dpad`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_dpad)|[`DpadControl`](../api/UnityEngine.InputSystem.Controls.DpadControl.html)|The D-pad on the gamepad.|
|[`buttonNorth`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_buttonNorth)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The upper one of the four actions buttons, which are usually located on the right side of the gamepad. Labelled "Y" on Xbox controllers and "Triangle" on PlayStation controllers.|
|[`buttonSouth`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_buttonSouth)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The lower one of the four actions buttons, which are usually located on the right side of the gamepad. Labelled "A" on Xbox controllers and "Cross" on PlayStation controllers.|
|[`buttonWest`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_buttonWest)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The left one of the four actions buttons, which are usually located on the right side of the gamepad. Labelled "X" on Xbox controllers and "Square" on PlayStation controllers.|
|[`buttonEast`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_buttonEast)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The right one of the four actions buttons, which are usually located on the right side of the gamepad. Labelled "B" on Xbox controllers and "Circle" on PlayStation controllers.|
|[`leftShoulder`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_leftShoulder)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The left shoulder button.|
|[`rightShoulder`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_rightShoulder)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The right shoulder button.|
|[`leftTrigger`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_leftTrigger)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The left trigger button.|
|[`rightTrigger`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_rightTrigger)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The right trigger button.|
|[`startButton`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_startButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The start button.|
|[`selectButton`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_selectButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The select button.|
|[`leftStickButton`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_leftStickButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The button pressed by pressing down the left stick.|
|[`rightStickButton`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_rightStickButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The button pressed by pressing down the right stick.|

>__Note__: Buttons are also full floating-point axes. For example, the left and right triggers can function as buttons as well as full floating-point axes.

You can also access gamepad buttons using the indexer property on [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_Item_UnityEngine_InputSystem_LowLevel_GamepadButton_) and the [`GamepadButton`](../api/UnityEngine.InputSystem.LowLevel.GamepadButton.html) enumeration:

```CSharp
Gamepad.current[GamepadButton.LeftShoulder];
```

Gamepads have both both Xbox-style and PS4-style aliases on buttons. For example, the following four accessors all retrieve the same "north" face button:

```CSharp
Gamepad.current[GamepadButton.Y]
Gamepad.current["Y"]
Gamepad.current[GamepadButton.Triangle]
Gamepad.current["Triangle"]
```

## Polling

On Windows (XInput controllers only), UWP and Switch, Unity polls gamepads explicitly rather than deliver updates as events.

You can control polling frequency manually. The default polling frequency is 60 Hz. Use [`InputSystem.pollingFrequency`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_pollingFrequency) to get or set the frequency.

```CSharp
// Poll gamepads at 120 Hz.
InputSystem.pollingFrequency = 120;
```

Increased frequency should lead to an increased number of events on the respective Devices. The timestamps provided on the events should roughly following the spacing dicated by the polling frequency. Note, however, that the asynchronous background polling depends on OS thread scheduling and can vary.

## Rumble

The [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) class implements the [`IDualMotorRumble`](../api/UnityEngine.InputSystem.Haptics.IDualMotorRumble.html) interface to allow you to control the left and right motor speeds. In most common gamepads, the left motor emits a low-frequency rumble, and the right motor emits a high frequency rumble.

```CSharp
// Rumble the  low-frequency (left) motor at 1/4 speed and the high-frequency
// (right) motor at 3/4 speed.
Gamepad.current.SetMotorSpeeds(0.25f, 0.75f);
```

>__Note__: Only the following combinations of Devices/OSes currently support rumble:
>* PS4, Xbox and Switch controllers when connected to their respective consoles. Only supported if you install console-specific input packages in your project.
>* PS4 controllers when connected to Mac or Windows/UWP computers.
>* Xbox controllers on windows.
[//]: # (TODO: are we missing any supported configs?)

### Pausing, resuming, and stopping haptics

[`IDualMotorRumble`](../api/UnityEngine.InputSystem.Haptics.IDualMotorRumble.html) is based on [`IHaptics`](../api/UnityEngine.InputSystem.Haptics.IHaptics.html), which is the base interface for any haptics support on any Device. This allows you to pause, resume and reset haptic feedback, using the [`PauseHaptics`](../api/UnityEngine.InputSystem.Haptics.IHaptics.html#UnityEngine_InputSystem_Haptics_IHaptics_PauseHaptics), [`ResumeHaptics`](../api/UnityEngine.InputSystem.Haptics.IHaptics.html#UnityEngine_InputSystem_Haptics_IHaptics_ResumeHaptics) and [`ResetHaptics`](../api/UnityEngine.InputSystem.Haptics.IHaptics.html#UnityEngine_InputSystem_Haptics_IHaptics_ResetHaptics) methods respectively.

In certain situations, you might want to globally pause or stop haptics for all Devices. For example, if the player enters the in-game menu, you can pause haptics while the player is in the menu, and then resume haptics once the player resumes the game. You can use the corresponding methods on [`InputSystem`](../api/UnityEngine.InputSystem.InputSystem.html) to achieve this result. These methods work the same way as Device-specific methods, but affect all Devices:

```CSharp
// Pause haptics globally.
InputSystem.PauseHaptics();

// Resume haptics globally.
InputSystem.ResumeHaptics();

// Stop haptics globally.
InputSystem.ResetHaptics();
```

The difference between [`PauseHaptics`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_PauseHaptics) and [`ResetHaptics`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_ResetHaptics) is that the latter will reset haptics playback state on each Device to its initial state whereas [`PauseHaptics`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_PauseHaptics) will preserve playback state in memory and only stop playback on the hardware.

## PlayStation controllers

PlayStation controllers are well supported on different Devices. The Input System implements these as different derived types of the [`DualShockGamepad`](../api/UnityEngine.InputSystem.DualShock.DualShockGamepad.html) base class, which derives from [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html)):

* [`DualShock3GamepadHID`](../api/UnityEngine.InputSystem.DualShock.DualShock3GamepadHID.html): A DualShock 3 controller connected to a desktop computer using the HID interface. Currently only supported on macOS. Doesn't support [rumble](#rumble).

* [`DualShock4GamepadHID`](../api/UnityEngine.InputSystem.DualShock.DualShock4GamepadHID.html): A DualShock 4 controller connected to a desktop computer using the HID interface. Supported on macOS, Windows, UWP, and Linux.

*
[`DualShock4GampadiOS`](../api/UnityEngine.InputSystem.iOS.DualShock4GampadiOS.html): A DualShock 4 controller connected to an iOS Device via Bluetooth. Requires iOS 13 or higher.

[`DualShock4GamepadHID`](../api/UnityEngine.InputSystem.DualShock.DualShock4GamepadHID.html) implements additional, DualShock-specific functionality on top the general support in the [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) class:

* [`SetLightBarColor(Color)`](../api/UnityEngine.InputSystem.DualShock.DualShockGamepad.html#UnityEngine_InputSystem_DualShock_DualShockGamepad_SetLightBarColor_Color_): Lets you set the color of the light bar on the controller.

>__Note__:
>* Unity supports PlayStation controllers on WebGL in some browser and OS configurations, but treats them as basic [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) or [`Joystick`](../api/UnityEngine.InputSystem.Joystick.html) Devices, and doesn't support Rumble or any other DualShock-specific functionality.
>* Unity doesn't support connecting a PlayStation controller to a desktop machine using the DualShock 4 USB Wireless Adaptor. Use USB or Bluetooth to connect it.

## Xbox

Xbox controllers are well supported on different Devices. The Input System implements these using the [`XInputController`](../api/UnityEngine.InputSystem.XInput.XInputController.html) class, which derives from [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html). On Windows and UWP, Unity connects to any type of supported XInput controller, including all Xbox One or Xbox 360-compatible controllers, using the XInput API. These controllers are represented as an [`XInputController`](../api/UnityEngine.InputSystem.XInput.XInputController.html) instance. You can query the [`XInputController.subType`](../api/UnityEngine.InputSystem.XInput.XInputController.html#UnityEngine_InputSystem_XInput_XInputController_subType) property to get information about the type of controller (for example, a wheel or a gamepad).

On other platforms Unity uses derived classes to represent Xbox controllers:

* [`XboxGamepadMacOS`](../api/UnityEngine.InputSystem.XInput.XboxGamepadMacOS.html): Any Xbox or compatible gamepad connected to a Mac via USB using the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller).

* [`XboxOneGampadMacOSWireless`](../api/UnityEngine.InputSystem.XInput.XboxOneGampadMacOSWireless.html): An Xbox One controller connected to a Mac via Bluetooth. Only the latest generation of Xbox One controllers supports Bluetooth. These controllers don't require any additional drivers in this scenario.

*
[`XboxOneGampadiOS`](../api/UnityEngine.InputSystem.iOS.XboxOneGampadiOS.html): An Xbox One controller connected to an iOS Device via Bluetooth. Requires iOS 13 or higher.

>__Note__:
>* XInput controllers on Mac currently require the installation of the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller). Only USB connections are supported by this driver, no wireless dongles. However, the latest generation of Xbox One controllers natively supported Bluetooth. Macs natively support these controllers as HIDs without any additional drivers when connected via Bluetooth.
>* Unity supports Xbox controllers on WebGL in some browser and OS configurations, but treats them as basic [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) or [`Joystick`](../api/UnityEngine.InputSystem.Joystick.html) Devices, and doesn't support Rumble or any other Xbox-specific functionality.

## Switch

The Input System support Switch Pro controllers on desktop computers via the [`SwitchProControllerHID`](../api/UnityEngine.InputSystem.Switch.SwitchProControllerHID.html) class, which implements basic gamepad functionality.
