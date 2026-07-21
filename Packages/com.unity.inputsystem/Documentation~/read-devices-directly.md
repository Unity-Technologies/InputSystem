---
uid: input-system-read-devices-directly
---

# Read devices directly

The input system allows you to directly read the state of a device's controls, which can be useful in some situations. This isn't generally the recommended workflow because it bypasses many of the Input Systems useful features, such as [actions](actions.md) and [bindings](bindings.md).

To read a device's controls directly you must:

1. Get a reference to a device
2. Identify the control on the device you want to read
3. Read the value from the control

## Get a reference to a device

To get a reference to a device, you can either:

- Get the currently connected device of a given type, or
- Check through all currently connected devices.

### Get the current device of a specific type

You can get references to any supported device currently connected by using one of the [InputDevice classes](xref:UnityEngine.InputSystem.InputDevice) and using the `.current` property to get the currently active device of that type. For example, [`Gamepad.current`](xref:UnityEngine.InputSystem.Gamepad) returns the most recently active connected gamepad.

You can browse the available device types from the [InputDevice classes API documentation](xref:UnityEngine.InputSystem.InputDevice).

- Some types listed are usable directly, such as `Gamepad` or `Joystick`.
- Some are abstract parent classes that have usable child classes. For example, `Pointer` is not directly usable, but has usable child classes of `Mouse`, `Pen`, and `Touch`.
- Some usable types also have more specialized child classes. For example `Gamepad` also has child classes such as `AndroidGamepad` as well as other Gampad types.

### Check through all connected devices

The `InputSystem.devices` property provides an array of all currently connected devices. You can [iterate through this array to get the reference to each connected device](query-gamepads.md#discover-all-connected-devices).


## Identify the control property on the device

Each type of device has its own configuration of [controls](controls.md), defined by its [layout](layouts.md). Each control has an API property that allows you to access the value of that control.

For example, all Gamepads have a `leftStick` and `rightStick` property, as well as a number of other properties which correspond to each of its controls.

You can browse the API documentation for any given device type to discover the control properties to use. For example, the [Gamepad class properties API documentation](xref:UnityEngine.InputSystem.Gamepad).

## Read the value from the control

Once you have the reference to the device, and you know the control property to read, you can read the value from your code using the API which matches the [control's type](control-types-reference.md).

- For value type controls, use `ReadValue`, which returns a value of the control's type.
- For button type controls, use `isPressed`, `wasPressedThisFrame` or `wasReleasedThisFrame`.

For example:

```CSharp
using UnityEngine;
using UnityEngine.InputSystem;

public class MyPlayerScript : MonoBehaviour
{
    void Update()
    {
        var gamepad = Gamepad.current;

        // check there is a Gamepad connected
        if (gamepad != null)
        {
            if (gamepad.rightTrigger.wasPressedThisFrame)
            {
                // 'Use' code here
            }

            Vector2 move = gamepad.leftStick.ReadValue();
            {
                //  'Move' code here
            }
        }
    }
}
```
