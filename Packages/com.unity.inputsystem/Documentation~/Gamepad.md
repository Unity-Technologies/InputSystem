---
uid: input-system-gamepad
---
# Gamepad Support

A [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) is narrowly defined as a device with two thumbsticks, a D-pad, and four face buttons. Additionally, gamepads usually have two shoulder and two trigger buttons. Most gamepads also have two buttons in the middle.

A gamepad can have additional controls, such as a gyro, which the device can expose. However, all gamepads are guaranteed to have at least the minimum set of controls described above.

Gamepad support guarantees the correct location and functioning of controls across platforms and hardware. For example, a PS4 DualShock controller layout should look identical regardless of which platform it is supported on. A gamepad's south face button should always be the lowermost face button.

> [!NOTE]
> Generic [HID](xref:input-system-hid) gamepads will __not__ be surfaced as [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) devices but rather be created as generic [joysticks](xref:input-system-joystick). This is because the Input System cannot guarantee correct mapping of buttons and axes on the controller (the information is simply not available at the HID level). Only HID gamepads that are explicitly supported by the Input System (like the PS4 controller) will come out as gamepads. Note that you can set up the same kind of support for specific HID gamepads yourself (see ["Overriding the HID Fallback"](xref:input-system-hid#creating-a-custom-device-layout)).
>
> In case you want to use the gamepad for driving mouse input, there is a sample called `Gamepad Mouse Cursor` you can install from the package manager UI when selecting the Input System package. The sample demonstrates how to set up gamepad input to drive a virtual mouse cursor.

## Controls

Every gamepad has the following controls:

|Control|Type|Description|
|-------|----|-----------|
|[`leftStick`](xref:UnityEngine.InputSystem.Gamepad.leftStick)|[`StickControl`](xref:UnityEngine.InputSystem.Controls.StickControl)|Thumbstick on the left side of the gamepad. Deadzoned. Provides a normalized 2D motion vector. X is [-1..1] from left to right, Y is [-1..1] from bottom to top. Has up/down/left/right buttons for use like a D-pad.|
|[`rightStick`](xref:UnityEngine.InputSystem.Gamepad.rightStick)|[`StickControl`](xref:UnityEngine.InputSystem.Controls.StickControl)|Thumbstick on the right side of the gamepad. Deadzoned. Provides a normalized 2D motion vector. X is [-1..1] from left to right, Y is [-1..1] from bottom to top. Has up/down/left/right buttons for use like a D-pad.|
|[`dpad`](xref:UnityEngine.InputSystem.Gamepad.dpad)|[`DpadControl`](xref:UnityEngine.InputSystem.Controls.DpadControl)|The D-pad on the gamepad.|
|[`buttonNorth`](xref:UnityEngine.InputSystem.Gamepad.buttonNorth)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The upper button of the four action buttons, which are usually located on the right side of the gamepad. Labelled "Y" on Xbox controllers and "Triangle" on PlayStation controllers.|
|[`buttonSouth`](xref:UnityEngine.InputSystem.Gamepad.buttonSouth)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The lower button of the four action buttons, which are usually located on the right side of the gamepad. Labelled "A" on Xbox controllers and "Cross" on PlayStation controllers.|
|[`buttonWest`](xref:UnityEngine.InputSystem.Gamepad.buttonWest)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The left button of the four action buttons, which are usually located on the right side of the gamepad. Labelled "X" on Xbox controllers and "Square" on PlayStation controllers.|
|[`buttonEast`](xref:UnityEngine.InputSystem.Gamepad.buttonEast)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The right button of the four action buttons, which are usually located on the right side of the gamepad. Labelled "B" on Xbox controllers and "Circle" on PlayStation controllers.|
|[`leftShoulder`](xref:UnityEngine.InputSystem.Gamepad.leftShoulder)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The left shoulder button.|
|[`rightShoulder`](xref:UnityEngine.InputSystem.Gamepad.rightShoulder)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The right shoulder button.|
|[`leftTrigger`](xref:UnityEngine.InputSystem.Gamepad.leftTrigger)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The left trigger button.|
|[`rightTrigger`](xref:UnityEngine.InputSystem.Gamepad.rightTrigger)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The right trigger button.|
|[`startButton`](xref:UnityEngine.InputSystem.Gamepad.startButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The start button.|
|[`selectButton`](xref:UnityEngine.InputSystem.Gamepad.selectButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The select button.|
|[`leftStickButton`](xref:UnityEngine.InputSystem.Gamepad.leftStickButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The button pressed when the user presses down the left stick.|
|[`rightStickButton`](xref:UnityEngine.InputSystem.Gamepad.rightStickButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The button pressed when the user presses down the right stick.|

> [!NOTE]
> Buttons are also full floating-point axes. For example, the left and right triggers can function as buttons as well as full floating-point axes.

You can also access gamepad buttons using the indexer property on [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad.Item(UnityEngine.InputSystem.LowLevel.GamepadButton)) and the [`GamepadButton`](xref:UnityEngine.InputSystem.LowLevel.GamepadButton) enumeration:

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

### Deadzones

Deadzones prevent accidental input due to slight variations in where gamepad sticks come to rest at their centre point. They allow a certain small inner area where the input is considered to be zero even if it is slightly off from the zero position.

To add a deadzone to gamepad stick, put a [stick deadzone Processor](ProcessorTypes.md#stick-deadzone) on the sticks, like this:

```JSON
     {
        "name" : "MyGamepad",
        "extend" : "Gamepad",
        "controls" : [
            {
                "name" : "leftStick",
                "processors" : "stickDeadzone(min=0.125,max=0.925)"
            },
            {
                "name" : "rightStick",
                "processors" : "stickDeadzone(min=0.125,max=0.925)"
            }
        ]
    }
```

You can do the same in your C# state structs.

```C#
    public struct MyDeviceState
    {
        [InputControl(processors = "stickDeadzone(min=0.125,max=0.925)"]
        public StickControl leftStick;
        [InputControl(processors = "stickDeadzone(min=0.125,max=0.925)"]
        public StickControl rightStick;
    }
```

The gamepad layout already adds stick deadzone processors which take their min and max values from [`InputSettings.defaultDeadzoneMin`](xref:UnityEngine.InputSystem.InputSettings.defaultDeadzoneMin) and [`InputSettings.defaultDeadzoneMax`](xref:UnityEngine.InputSystem.InputSettings.defaultDeadzoneMax).



## Polling

On Windows (XInput controllers only), Universal Windows Platform (UWP), and Switch, Unity polls gamepads explicitly rather than deliver updates as events.

The platform sets the default polling frequency to provide a good user experience for the devices supported on the platform. This frequency is guaranteed to be at least 60 Hz. You can override the polling frequency suggested by the target platform by explicitly setting [`InputSystem.pollingFrequency`](xref:UnityEngine.InputSystem.InputSystem.pollingFrequency) at runtime.

```CSharp
// Poll gamepads at 120 Hz.
InputSystem.pollingFrequency = 120;
```

Increased frequency should lead to an increased number of events on the respective devices. The timestamps provided on the events should roughly follow the spacing dictated by the polling frequency. Note, however, that the asynchronous background polling depends on OS thread scheduling and can vary.

## Rumble

The [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) class implements the [`IDualMotorRumble`](xref:UnityEngine.InputSystem.Haptics.IDualMotorRumble) interface that allows you to control the left and right motor speeds. In most common gamepads, the left motor emits a low-frequency rumble, and the right motor emits a high-frequency rumble.

```CSharp
// Rumble the  low-frequency (left) motor at 1/4 speed and the high-frequency
// (right) motor at 3/4 speed.
Gamepad.current.SetMotorSpeeds(0.25f, 0.75f);
```

> [!NOTE]
> Only the following combinations of devices/OSes currently support rumble:
>* PS4, Xbox, and Switch controllers, when connected to their respective consoles. Only supported if you install console-specific input packages in your Project.
>* PS4 controllers, when connected to Mac or Windows/UWP computers.
>* Xbox controllers on Windows.

[//]: # (TODO: are we missing any supported configs?)

### Pausing, resuming, and stopping haptics

[`IDualMotorRumble`](xref:UnityEngine.InputSystem.Haptics.IDualMotorRumble) is based on [`IHaptics`](xref:UnityEngine.InputSystem.Haptics.IHaptics), which is the base interface for any haptics support on any device. You can pause, resume, and reset haptic feedback using the [`PauseHaptics`](xref:UnityEngine.InputSystem.Haptics.IHaptics.PauseHaptics), [`ResumeHaptics`](xref:UnityEngine.InputSystem.Haptics.IHaptics.ResumeHaptics), and [`ResetHaptics`](xref:UnityEngine.InputSystem.Haptics.IHaptics.ResetHaptics) methods respectively.

In certain situations, you might want to globally pause or stop haptics for all devices. For example, if the player enters an in-game menu, you can pause haptics while the player is in the menu, and then resume haptics once the player resumes the game. You can use the corresponding methods on [`InputSystem`](xref:UnityEngine.InputSystem.InputSystem) to achieve this result. These methods work the same way as device-specific methods, but affect all devices:

```CSharp
// Pause haptics globally.
InputSystem.PauseHaptics();

// Resume haptics globally.
InputSystem.ResumeHaptics();

// Stop haptics globally.
InputSystem.ResetHaptics();
```

The difference between [`PauseHaptics`](xref:UnityEngine.InputSystem.InputSystem.PauseHaptics) and [`ResetHaptics`](xref:UnityEngine.InputSystem.InputSystem.ResetHaptics) is that the latter resets haptics playback state on each device to its initial state, whereas [`PauseHaptics`](xref:UnityEngine.InputSystem.InputSystem.PauseHaptics) preserves playback state in memory and only stops playback on the hardware.

## PlayStation controllers

PlayStation controllers are well supported on different devices. The Input System implements these as different derived types of the [`DualShockGamepad`](xref:UnityEngine.InputSystem.DualShock.DualShockGamepad) base class, which derives from [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad)):

* [`DualShock3GamepadHID`](xref:UnityEngine.InputSystem.DualShock.DualShock3GamepadHID): A DualShock 3 controller connected to a desktop computer using the HID interface. Currently only supported on macOS. Doesn't support [rumble](#rumble).

* [`DualShock4GamepadHID`](xref:UnityEngine.InputSystem.DualShock.DualShock4GamepadHID): A DualShock 4 controller connected to a desktop computer using the HID interface. Supported on macOS, Windows, UWP, and Linux.
*
* [`DualSenseGamepadHID`](xref:UnityEngine.InputSystem.DualShock.DualSenseGamepadHID): A DualSense controller connected to a desktop computer using the HID interface. Supported on macOS, Windows.

* [`DualShock4GampadiOS`](xref:UnityEngine.InputSystem.iOS.DualShock4GampadiOS): A DualShock 4 controller connected to an iOS device via Bluetooth. Requires iOS 13 or higher.

* [`SetLightBarColor(Color)`](xref:UnityEngine.InputSystem.DualShock.DualShockGamepad.SetLightBarColor(UnityEngine.Color)): Used to set the color of the light bar on the controller.

Note that, due to limitations in the USB driver and/or the hardware, only one IOCTL (input/output control) command can be serviced at a time. [`SetLightBarColor(Color)`](xref:UnityEngine.InputSystem.DualShock.DualShockGamepad.SetLightBarColor(UnityEngine.Color)) and [`SetMotorSpeeds(Single, Single)`](xref:UnityEngine.InputSystem.Gamepad.SetMotorSpeeds(System.Single,System.Single)) functionality on Dualshock 4 is implemented using IOCTL commands, and so if either method is called in quick succession, it is likely that only the first command will successfully complete. The other commands will be dropped. If there is a need to set both lightbar color and rumble motor speeds at the same time, use the [`SetMotorSpeedsAndLightBarColor(Single, Single, Color)`](xref:UnityEngine.InputSystem.DualShock.DualShock4GamepadHID.SetMotorSpeedsAndLightBarColor(System.Single,System.Single,UnityEngine.Color)) method.

> [!NOTE]
>* Unity supports PlayStation controllers on WebGL in some browser and OS configurations, but treats them as basic [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) or [`Joystick`](xref:UnityEngine.InputSystem.Joystick) devices, and doesn't support rumble or any other DualShock-specific functionality.
>* Unity doesn't support connecting a PlayStation controller to a desktop machine using the DualShock 4 USB Wireless Adaptor. Use USB or Bluetooth to connect it.

## Xbox controllers

Xbox controllers are well supported on different devices. The Input System implements these using the [`XInputController`](xref:UnityEngine.InputSystem.XInput.XInputController) class, which derives from [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad). On Windows and UWP, Unity uses the XInput API to connect to any type of supported XInput controller, including all Xbox One or Xbox 360-compatible controllers. These controllers are represented as an [`XInputController`](xref:UnityEngine.InputSystem.XInput.XInputController) instance. You can query the [`XInputController.subType`](xref:UnityEngine.InputSystem.XInput.XInputController.subType) property to get information about the type of controller (for example, a wheel or a gamepad).

On other platforms Unity, uses derived classes to represent Xbox controllers:

* [`XboxGamepadMacOS`](xref:UnityEngine.InputSystem.XInput.XboxGamepadMacOS): Any Xbox or compatible gamepad connected to a Mac via USB using the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller). This class is only used when the `360Controller` driver is in use, and as such you shouldn't see it in use on modern versions of macOS - it is provided primarily for legacy reasons, and for scenarios where macOS 10.15 may still be used.

* [`XboxGamepadMacOSNative`](xref:UnityEngine.InputSystem.XInput.XboxGamepadMacOSNative): Any Xbox gamepad connected to a Mac (macOS 11.0 or higher) via USB. On modern macOS versions, you will get this class instead of `XboxGamepadMacOS`

* [`XboxOneGampadMacOSWireless`](xref:UnityEngine.InputSystem.XInput.XboxOneGampadMacOSWireless): An Xbox One controller connected to a Mac via Bluetooth. Only the latest generation of Xbox One controllers supports Bluetooth. These controllers don't require any additional drivers in this scenario.

* [`XboxOneGampadiOS`](xref:UnityEngine.InputSystem.iOS.XboxOneGampadiOS): An Xbox One controller connected to an iOS device via Bluetooth. Requires iOS 13 or higher.

> [!NOTE]
> * XInput controllers on Mac currently require the installation of the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller). This driver only supports USB connections, and doesn't support wireless dongles. However, the latest generation of Xbox One controllers natively support Bluetooth. Macs natively support these controllers as HIDs without any additional drivers when connected via Bluetooth.
> * Unity supports Xbox controllers on WebGL in some browser and OS configurations, but treats them as basic [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) or [`Joystick`](xref:UnityEngine.InputSystem.Joystick) devices, and doesn't support rumble or any other Xbox-specific functionality.

## Switch controllers

The Input System support Switch Pro controllers on desktop computers via the [`SwitchProControllerHID`](../api/UnityEngine.InputSystem.Switch.SwitchProControllerHID.html) class, which implements basic gamepad functionality.

> [!NOTE]
> This support does not currently work for Switch Pro controllers connected via wired USB. Instead, the Switch Pro controller *must* be connected via Bluetooth. This is due to the controller using a prioprietary communication protocol on top of HID which does not allow treating the controller like any other HID.

> [!NOTE]
> Switch Joy-Cons are not currently supported on desktop.

## Cursor Control

To give gamepads and joysticks control over a hardware or software cursor, you can use the [`VirtualMouseInput`](xref:UnityEngine.InputSystem.UI.VirtualMouseInput) component. See [`VirtualMouseInput` component](xref:input-system-ui-support#virtual-mouse-cursor-control) in the UI section of the manual.

## Discover all connected devices

There are various ways to discover the currently connected devices, as shown in the code samples below.

To query a list of all connected devices (does not allocate; read-only access):
```
InputSystem.devices
```

To get notified when a device is added or removed:
```
InputSystem.onDeviceChange +=
    (device, change) =>
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed)
        {
            Debug.Log($"Device '{device}' was {change}");
        }
    }
```

To find all gamepads and joysticks:
```
var devices = InputSystem.devices;
for (var i = 0; i < devices.Count; ++i)
{
    var device = devices[i];
    if (device is Joystick || device is Gamepad)
    {
        Debug.Log("Found " + device);
    }
}
```
