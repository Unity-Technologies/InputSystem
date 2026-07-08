---
uid: input-system-query-gamepads
---

# Query and control gamepads in code

You can use the methods in the Gamepad class to access information about the gamepads connected to your application.

## Discover all connected devices

There are various ways to discover the currently connected devices. To query a list of all connected devices (doesn't allocate; read-only access) use the following:

```c#

InputSystem.devices

```

To get notified when a device is added or removed:

```c#

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

```c#

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

## Access gamepad buttons

To access gamepad buttons, you can use the indexer property on [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad.Item(UnityEngine.InputSystem.LowLevel.GamepadButton)) and the [`GamepadButton`](xref:UnityEngine.InputSystem.LowLevel.GamepadButton) enumeration:

```c#

Gamepad.current[GamepadButton.LeftShoulder];

```

Gamepads have both Xbox-style and PlayStation-style aliases on buttons. For example, the following four accessors all retrieve the same north face button:

```c

Gamepad.current[GamepadButton.Y]
Gamepad.current["Y"]
Gamepad.current[GamepadButton.Triangle]
Gamepad.current["Triangle"]

```

## Add a deadzone to a gamepad

Deadzones prevent accidental input due to slight variations in where gamepad sticks come to rest at their center point. They allow a certain small inner area where the input is considered to be zero even if the input is slightly off from the zero position.

To add a deadzone to gamepad stick, put a [stick deadzone Processor](built-in-processors.md) on the sticks, like this:

```json

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

You can do the same in your C# state structs:

```c#

   public struct MyDeviceState
    {
        [InputControl(processors = "stickDeadzone(min=0.125,max=0.925)"]
        public StickControl leftStick;
        [InputControl(processors = "stickDeadzone(min=0.125,max=0.925)"]
        public StickControl rightStick;
    }

```

The gamepad layout already adds stick deadzone processors which take their minimum and maximum values from [`InputSettings.defaultDeadzoneMin`](xref:UnityEngine.InputSystem.InputSettings.defaultDeadzoneMin) and [`InputSettings.defaultDeadzoneMax`](xref:UnityEngine.InputSystem.InputSettings.defaultDeadzoneMax).

## Using gamepads for mouse input

To use a gamepad for driving mouse input, refer to the sample called Gamepad Mouse Cursor. To access the sample, open the Package Manager window, and select the Input System package. Then select the Samples tab. The sample demonstrates how to set up gamepad input to drive a virtual mouse cursor.

You can also use the [`VirtualMouseInput`](xref:UnityEngine.InputSystem.UI.VirtualMouseInput) component to control the hardware or software cursor. For more information, refer to [`VirtualMouseInput` component](virtual-mouse-ui-cursor-control.md).
