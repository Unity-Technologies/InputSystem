    ////WIP

# Gamepad Support

A gamepad is narrowly defined as a device with two thumbsticks, a dpad, and four face buttons. Additionally, they will usually have two shoulder and two trigger buttons. Most gamepads also have two buttons in the middle section of the gamepad.

A gamepad may have additional controls (such as a gyro) which are exposed from the device but each gamepad is guaranteed to at least have the above-mentioned minimum set of controls.

The goal of gamepad support is to guarantee correct location and functioning of controls across platforms and hardware. A PS4 DualShock controller, for example, is meant to look identical regardless of which platform it is supported on. And a gamepad's south face button, for example, is meant to be expected to always indeed be the bottom-most face button.

## Controls

The following controls are present on every gamepad:

|Control|Type|Description|
|-------|----|-----------|
|`leftStick`|Stick|Thumbstick on the left side of the gamepad. Deadzoned. Provides a normalized 2D motion vector. X is [-1..1] from left to right, Y is [-1..1] from bottom to top. Has up/down/left/right buttons for use in dpad-like fashion.|
|`rightStick`|Stick|Thumbstick on the right side of the gamepad. Deadzoned. Provides a normalized 2D motion vector. X is [-1..1] from left to right, Y is [-1..1] from bottom to top. Has up/down/left/right buttons for use in dpad-like fashion.|
|`dpad`|Dpad||
|`buttonNorth`|Button|The topmost of the four face buttons (usually located on the right side of the gamepad).|
|`buttonSouth`|Button||
|`buttonWest`|Button||
|`buttonEast`|Button||
|`leftShoulder`|Button||
|`rightShoulder`|Button||
|`leftTrigger`|Button||
|`rightTrigger`|Button||
|`startButton`|Button||
|`selectButton`|Button||
|`leftStickPress`|Button||
|`rightStickPress`|Button||

>NOTE: Be aware that buttons are also full floating-point axes. This means that, for example, the left and right triggers can function both as buttons as well as full floating-point axes.

Gamepad buttons can also be accessed using the indexer property on `Gamepad` and the `GamepadButton` enumeration:

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

    ////TODO: provide list of the platforms/devices where we poll

On many platforms, gamepads are polled explicitly by Unity, rather than delivered as events. For example, in the Windows editor and standalone player, XInput gamepads are polled asynchronously in the background.

The frequency of the polling can be controlled manually. The default polling frequency is 60 Hz. Use `InputSystem.pollingFrequency` to get or set the frequency.

```CSharp
// Poll gamepads at 120 Hz.
InputSystem.pollingFrequency = 120;
```

Increased frequency should lead to an increased number of events on the respective devices. The timestamps provided on the events should roughly following the spacing dicated by the polling frequency. Note, however, that the asynchronous background polling depends on OS thread scheduling and is thus susceptible to variance.

## Rumble

    ////TODO: provide list of what platforms/devices we support rumble on

Gamepads implement the `IDualMotorRumble` interface to control left and right motor speed. Note, however, that not all gamepads support rumble at this point (it is not supported on WebGL, for example).

```CSharp
// Rumble the  low-frequency (left) motor at 1/4 speed and the high-frequency
// (right) motor at 3/4 speed.
Gamepad.current.SetMotorSpeeds(0.25f, 0.75f);
```

## PS4

## Xbox

    ////REVIEW: do we support trigger motors on UWP ATM?

    ////TODO: document gamepadId and xboxUserId

Xbox gamepads have extended rumble functionality in the form of two additional motors that are located in the triggers. `XboxOneGamepad` implements `IXboxOneRumble` that features an extended `SetMotorSpeeds` method giving access to all four motors.

```CSharp
// Rumble the low-frequency (left) motor at 1/4 speed, the high-frequency (right)
// motor at 3/4 speed, turn off the left trigger motor, and rumble the right
// trigger motor at full speed.
XboxOneGamepad.current.SetMotorSpeeds(0.25f, 0.75, 0f, 1f);
```

## Switch
