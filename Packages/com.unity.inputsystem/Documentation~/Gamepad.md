# Gamepad Support

A [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) is narrowly defined as a device with two thumbsticks, a dpad, and four face buttons. Additionally, they will usually have two shoulder and two trigger buttons. Most gamepads also have two buttons in the middle section of the gamepad.

A gamepad may have additional controls (such as a gyro) which are exposed from the device but each gamepad is guaranteed to at least have the above-mentioned minimum set of controls.

The goal of gamepad support is to guarantee correct location and functioning of controls across platforms and hardware. A PS4 DualShock controller, for example, is meant to look identical regardless of which platform it is supported on. And a gamepad's south face button, for example, is meant to be expected to always indeed be the bottom-most face button.

## Controls

The following controls are present on every gamepad:

|Control|Type|Description|
|-------|----|-----------|
|[`leftStick`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_leftStick)|[`StickControl`](../api/UnityEngine.InputSystem.Controls.StickControl.html)|Thumbstick on the left side of the gamepad. Deadzoned. Provides a normalized 2D motion vector. X is [-1..1] from left to right, Y is [-1..1] from bottom to top. Has up/down/left/right buttons for use in dpad-like fashion.|
|[`rightStick`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_rightStick)|[`StickControl`](../api/UnityEngine.InputSystem.Controls.StickControl.html)|Thumbstick on the right side of the gamepad. Deadzoned. Provides a normalized 2D motion vector. X is [-1..1] from left to right, Y is [-1..1] from bottom to top. Has up/down/left/right buttons for use in dpad-like fashion.|
|[`dpad`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_dpad)|Dpad||
|[`buttonNorth`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_buttonNorth)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The upper one of the four actions buttons (usually located on the right side of the gamepad). Labelled "Y" on Xbox controllers and "Triangle" on PlayStation controllers.|
|[`buttonSouth`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_buttonSouth)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The lower one of the four actions buttons (usually located on the right side of the gamepad). Labelled "A" on Xbox controllers and "Cross" on PlayStation controllers.|
|[`buttonWest`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_buttonWest)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The left one of the four actions buttons (usually located on the right side of the gamepad). Labelled "X" on Xbox controllers and "Square" on PlayStation controllers.|
|[`buttonEast`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_buttonEast)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The right one of the four actions buttons (usually located on the right side of the gamepad). Labelled "B" on Xbox controllers and "Circle" on PlayStation controllers.|
|[`leftShoulder`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_leftShoulder)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The left shoulder button.|
|[`rightShoulder`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_rightShoulder)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The right shoulder button.|
|[`leftTrigger`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_leftTrigger)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The left trigger button.|
|[`rightTrigger`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_rightTrigger)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The right trigger button.|
|[`startButton`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_startButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The start button.|
|[`selectButton`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_selectButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The select button.|
|[`leftStickButton`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_leftStickButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The button pressed by pressing down the left stick.|
|[`rightStickButton`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_rightStickButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The button pressed by pressing down the right stick.|

>NOTE: Be aware that buttons are also full floating-point axes. This means that, for example, the left and right triggers can function both as buttons as well as full floating-point axes.

Gamepad buttons can also be accessed using the indexer property on [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_Item_UnityEngine_InputSystem_LowLevel_GamepadButton_) and the [`GamepadButton`](../api/UnityEngine.InputSystem.LowLevel.GamepadButton.html) enumeration:

```CSharp
Gamepad.current[GamepadButton.LeftShoulder];
```

Gamepads have both both Xbox-style and PS4-style aliases on buttons. The following four accessors all retrieve the same "north" face button, for example:

```CSharp
Gamepad.current[GamepadButton.Y]
Gamepad.current["Y"]
Gamepad.current[GamepadButton.Triangle]
Gamepad.current["Triangle"]
```

## Polling

On Windows (XInput controllers only), UWP and Switch, gamepads are polled explicitly by Unity, rather than delivered as events.

The frequency of the polling can be controlled manually. The default polling frequency is 60 Hz. Use [`InputSystem.pollingFrequency`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_pollingFrequency) to get or set the frequency.

```CSharp
// Poll gamepads at 120 Hz.
InputSystem.pollingFrequency = 120;
```

Increased frequency should lead to an increased number of events on the respective devices. The timestamps provided on the events should roughly following the spacing dicated by the polling frequency. Note, however, that the asynchronous background polling depends on OS thread scheduling and is thus susceptible to variance.

## Rumble

The [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) class implements the [`IDualMotorRumble`](../api/UnityEngine.InputSystem.Haptics.IDualMotorRumble.html) interface to allow you to control the left and right motor speeds. In most common gamepads, the left motor emits a low-frequency rumble, and the right motor emits a high frequency rumble.

```CSharp
// Rumble the  low-frequency (left) motor at 1/4 speed and the high-frequency
// (right) motor at 3/4 speed.
Gamepad.current.SetMotorSpeeds(0.25f, 0.75f);
```

>NOTE: Only the following combinations of devices/OSes currently support rumble:
>* PS4, Xbox and Switch controllers when connected to their respective consoles.
>* PS4 controllers when connected to Mac or Windows/UWP computers.
>* Xbox controllers on windows.
[//]: # (TODO: are we missing any supported configs?)

### Pausing, Resuming, and Stopping Haptics

[`IDualMotorRumble`](../api/UnityEngine.InputSystem.Haptics.IDualMotorRumble.html) is based on [`IHaptics`](../api/UnityEngine.InputSystem.Haptics.IHaptics.html), which is the base interface for any haptics support on any device, and allows you to pause, resume and reset haptic feedback, using the [`PauseHaptics`](../api/UnityEngine.InputSystem.Haptics.IHaptics.html#UnityEngine_InputSystem_Haptics_IHaptics_PauseHaptics), [`ResumeHaptics`](../api/UnityEngine.InputSystem.Haptics.IHaptics.html#UnityEngine_InputSystem_Haptics_IHaptics_ResumeHaptics) and [`ResetHaptics`](../api/UnityEngine.InputSystem.Haptics.IHaptics.html#UnityEngine_InputSystem_Haptics_IHaptics_ResetHaptics) methods respectively.

In can be desirable to globally pause or stop haptics for all devices in certain situation. For example, if the player enters the in-game menu, it can make sense to pauses haptics while the player is in the menu and then resume haptics effects once the player resumes the game. You can use the corresponding methods on [`InputSystem`](../api/UnityEngine.InputSystem.InputSystem.html) for that (which work the same way as the per-device methods, but affect all devices):

```CSharp
// Pause haptics globally.
InputSystem.PauseHaptics();

// Resume haptics globally.
InputSystem.ResumeHaptics();

// Stop haptics globally.
InputSystem.ResetHaptics();
```

The difference between [`PauseHaptics`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_PauseHaptics) and [`ResetHaptics`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_ResetHaptics) is that the latter will reset haptics playback state on each device to its initial state whereas [`PauseHaptics`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_PauseHaptics) will preserve playback state in memory and only stop playback on the hardware.

## PlayStation controllers

PlayStation controllers are well supported on different devices. They are implemented as different derived types of the [`DualShockGamepad`](../api/UnityEngine.InputSystem.DualShock.DualShockGamepad.html) base class (which itself derives from [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html)):

* [`DualShock3GamepadHID`](../api/UnityEngine.InputSystem.DualShock.DualShock3GamepadHID.html): A DualShock 3 controller connected to a desktop computer using the HID interface. Currently only supported on macOS, and does not support [rumble](#rumble).

* [`DualShock4GamepadHID`](../api/UnityEngine.InputSystem.DualShock.DualShock4GamepadHID.html): A DualShock 4 controller connected to a desktop computer using the HID interface (supported on macOS, Windows, UWP, and Linux).

* [`DualShockGamepadPS4`](../api/UnityEngine.InputSystem.PS4.DualShockGamepadPS4.html) A DualShock controller connected to a PlayStation 4 console. Only available when building for PS4, will not compile on other platforms.

Some of these implement additional, DualShock-specific functionality on top the general support in the [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) class:

* [`SetLightBarColor(Color)`](../api/UnityEngine.InputSystem.DualShock.DualShockGamepad.html#UnityEngine_InputSystem_DualShock_DualShockGamepad_SetLightBarColor_Color_): Lets you set the color of the light bar on the controller. Supported on [`DualShock4GamepadHID`](../api/UnityEngine.InputSystem.DualShock.DualShock4GamepadHID.html) and [`DualShockGamepadPS4`](../api/UnityEngine.InputSystem.PS4.DualShockGamepadPS4.html).

* [`acceleration`](../api/UnityEngine.InputSystem.PS4.DualShockGamepadPS4.html#UnityEngine_InputSystem_PS4_DualShockGamepadPS4_acceleration), [`orientation`](../api/UnityEngine.InputSystem.PS4.DualShockGamepadPS4.html#UnityEngine_InputSystem_PS4_DualShockGamepadPS4_orientation) and [`angularVelocity`](../api/UnityEngine.InputSystem.PS4.DualShockGamepadPS4.html#UnityEngine_InputSystem_PS4_DualShockGamepadPS4_angularVelocity): Controls which let you access the sensor data on the gamepad ([`DualShockGamepadPS4`](../api/UnityEngine.InputSystem.PS4.DualShockGamepadPS4.html) only)

* [`touches`](../api/UnityEngine.InputSystem.PS4.DualShockGamepadPS4.html#UnityEngine_InputSystem_PS4_DualShockGamepadPS4_touches): Lets you get input from the touch screen on the gamepad. ([`DualShockGamepadPS4`](../api/UnityEngine.InputSystem.PS4.DualShockGamepadPS4.html) only)


>NOTES:
>* We support PlayStation controllers on WebGL in some browser/OS configs, but they will always be represented as basic [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) or [`Joystick`](../api/UnityEngine.InputSystem.Joystick.html) devices, and we do not support Rumble or any other DualShock specific functionality.
>* We do not support the "DualShock 4 USB Wireless Adaptor" to connect a PlayStation controller to a desktop machine. Use USB or Bluetooth to connect  it.


## Xbox

Xbox controllers are well supported on different devices. They are implemented using the [`XInputController`](../api/UnityEngine.InputSystem.XInput.XInputController.html) class, (which  derives from [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html)). On Windows/UWP, any type of supported XInput controller (which includes all Xbox One or Xbox 360 compatible controllers) will be connected to using the XInput API and is represented directly as an [`XInputController`](../api/UnityEngine.InputSystem.XInput.XInputController.html) instance. You can query the [`XInputController.subType`](../api/UnityEngine.InputSystem.XInput.XInputController.html#UnityEngine_InputSystem_XInput_XInputController_subType) property to get information about the type of controller (wheel, gamepad, etc).

On other platforms we use specific derived classes to represent Xbox controllers:

* [`XboxGamepadMacOS`](../api/UnityEngine.InputSystem.XInput.XboxGamepadMacOS.html): Any Xbox or compatible gamepad connected to a Mac via USB using the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller).

* [`XboxOneGampadMacOSWireless`](../api/UnityEngine.InputSystem.XInput.XboxOneGampadMacOSWireless.html): An Xbox One controller connected to a Mac via Bluetooth (only the latest generation of Xbox One controllers supports Bluetooth). No additional driver is needed for this case.

* [`XboxOneGamepad`](../api/UnityEngine.InputSystem.XInput.XboxOneGamepad.html): A gamepad on an Xbox one console. We support additional, Xbox specific functionality for this case:
>*  The `gamepadId` and `xboxUserId` properties can be used to identify the gamepad and user.
>* Xbox gamepads have extended rumble functionality in the form of two additional motors that are located in the triggers. [`XboxOneGamepad`](../api/UnityEngine.InputSystem.XInput.XboxOneGamepad.html) implements [`IXboxOneRumble`](../api/UnityEngine.InputSystem.XInput.IXboxOneRumble.html) that features an extended `SetMotorSpeeds` method giving access to all four motors:

```CSharp
// Rumble the low-frequency (left) motor at 1/4 speed, the high-frequency (right)
// motor at 3/4 speed, turn off the left trigger motor, and rumble the right
// trigger motor at full speed.
XboxOneGamepad.current.SetMotorSpeeds(0.25f, 0.75, 0f, 1f);
```

>NOTES:
>* XInput controllers on Mac currently require the installation of the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller). Only USB connections are supported, no wireless dongles. However, the latest generation of Xbox One controllers natively supported Bluetooth, and are natively supported on Macs as HID devices without any additional driver when connected via Bluetooth.
>* We support Xbox controllers on WebGL in some browser/OS configs, but they will always be represented as basic [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) or [`Joystick`](../api/UnityEngine.InputSystem.Joystick.html) devices, and we do not support Rumble or any other Xbox specific functionality.


## Switch

We support Switch Pro controllers on desktop computers via the [`SwitchProControllerHID`](../api/UnityEngine.InputSystem.Switch.SwitchProControllerHID.html) class, which implements the basic gamepad functionality.

On the Switch console itself, we have extended support for Switch Pro as well as Joy-Con controllers using the [`NPad`](../api/UnityEngine.InputSystem.Switch.NPad.html) class. Refer to the [`NPad` scripting API documentation](../api/UnityEngine.InputSystem.Switch.NPad.html)  for more information.
