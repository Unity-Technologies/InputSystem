---
uid: input-system-control-paths
---

# Control paths

The Input System can look up controls using textual paths. [Bindings](bindings.md) on Input [Actions](actions.md) rely on this feature to identify the control(s) they read input from. For example, `<Gamepad>/leftStick/x` means "X control on left stick of gamepad". However, you can also use them for lookup directly on controls and devices, or to let the Input System search for controls among all devices using [`InputSystem.FindControls`](xref:UnityEngine.InputSystem.InputSystem.FindControls(System.String)):

At runtime, the Input System performs a look-up of all control paths against the currently connected devices to discover which controls match the ones specified in the paths. This process is called [binding resolution](binding-resolution.md).


## Specificity

When [selecting a control path for binding](select-control-binding.md), there are various levels of specificity you can use. These are available in the **Control paths** menu, in the [Binding Properties panel](binding-properties-panel-reference.md).

These levels of specificity depend on whether you want to refer to a certain type of device, specific models of device, or certain usages of a control regardless of device type.

### By generic device

You can refer to a generic type of device, for example referring to a common control across all types of gamepad (such as the left stick on a gamepad). In this case, this matches the left stick on all types of gamepad.

### By specific device

You can refer to a specific control on a specific model of gamepad (such as the `A` button on an Xbox controller). In this case, the path does not match any controls on any other type of gamepad even if they're similar in position or usage.

### By usage

You can also refer to controls by usage, which allows you to use a variety of common names that controls are associated with by convention, such as a `Back` button, or a `Submit` button. Refer to [control usages](control-usages.md) for more information about these.

## The device and control tree

The device and control tree is organized hierarchically from generic to specific. For example, the __Gamepad__ control path `<Gamepad>/buttonSouth` matches the lower action button on any gamepad. Alternatively, if you navigate to __Gamepad__ > __More Specific Gamepads__ and select __PS4 Controller__, and then choose the control path `<DualShockGamepad>/buttonSouth`, this only matches the "Cross" button on PlayStation gamepads, and doesn't match any other gamepads.


## Format

Control paths resemble file system paths: they contain components separated by a forward slash (`/`):

```
component/component...
```

Each component itself contains a set of fields with its own syntax. Each field is individually optional, provided that at least one of the fields is present as either a name or a wildcard:

```structured text
<layoutName>{usageName}#(displayName)controlName
```

The following table explains the use of each field:

|Field|Description|Example|
|-----|-----------|-------|
|`<layoutName>`|Requires the control at the current level to be based on the given layout. The actual layout of the control may be the same or a layout *based* on the given layout.|`<Gamepad>/buttonSouth`|
|`{usageName}`|Works differently for controls and Devices.<br><br>When used on a Device (the first component of a path), it requires the device to have the given usage. Refer to [Device usages](device-usages.md) for more details.<br><br>For looking up a control, the usage field is currently restricted to the path component immediately following the Device (the second component in the path). It finds the control on the Device that has the given usage. The control can be anywhere in the control hierarchy of the Device.|Device:<br><br>`<XRController>{LeftHand}/trigger`<br><br>Control:<br><br>`<Gamepad>/{Submit}`|
|`controlName`|Requires the control at the current level to have the given name. Takes both "proper" names ([`InputControl.name`](xref:UnityEngine.InputSystem.InputControl)) and aliases ([`InputControl.aliases`](xref:UnityEngine.InputSystem.InputControl)) into account.<br><br>This field can also be a wildcard (`*`) to match any name.|`MyGamepad/buttonSouth`<br><br>`*/{PrimaryAction}` (match `PrimaryAction` usage on Devices with any name)|
|`#(displayName)`|Requires the control at the current level to have the given display name (That is, [`InputControl.displayName`](xref:UnityEngine.InputSystem.InputControl)). The display name may contain whitespace and symbols.|`<Keyboard>/#(a)` (matches the key that generates the "a" character, if any, according to the current keyboard layout).<br><br>`<Gamepad>/#(Cross)`|

Here are examples of control paths:

```csharp
// Matches all gamepads (also gamepads *based* on the Gamepad layout):
"<Gamepad>"
// Matches the "Submit" control on all devices:
"*/"
// Matches the key that prints the "a" character on the current keyboard layout:
"<Keyboard>/#(a)"
// Matches the X axis of the left stick on a gamepad.
"<Gamepad>/leftStick/x"
// Matches the orientation control of the right-hand XR controller:
"<XRController>/orientation"
// Matches all buttons on a gamepad.
"<Gamepad>/<Button>"
```

### Wildcard characters

If you enter a control path as text, you can use the wildcard asterisk character (`*`) to match multiple controls in the hierarchy specified. For example, you can use `<Touchscreen>/touch*/press` to bind to any finger pressed on the touchscreen, instead of individually binding to `<Touchscreen>/touch0/press`, `<Touchscreen>/touch1/press` and each other numbered touch separately.


## Access from code

You can access the literal path of a given control with its [`InputControl.path`](xref:UnityEngine.InputSystem.InputControl.path) property. If you need to, you can manually parse a control path into its components using the [`InputControlPath.Parse(path)`](xref:UnityEngine.InputSystem.InputControlPath.Parse(System.String)) API:


```CSharp
var parsed = InputControlPath.Parse("<XRController>{LeftHand}/trigger").ToArray();

Debug.Log(parsed.Length); // Prints 2.
Debug.Log(parsed[0].layout); // Prints "XRController".
Debug.Log(parsed[0].name); // Prints an empty string.
Debug.Log(parsed[0].usages.First()); // Prints "LeftHand".
Debug.Log(parsed[1].layout); // Prints null.
Debug.Log(parsed[1].name); // Prints "trigger".
```

You can use control paths to directly reference controls, or to let the Input System search for Controls among all devices using [`InputSystem.FindControls`](xref:UnityEngine.InputSystem.InputSystem).

```CSharp
var gamepad = Gamepad.all[0];
var leftStickX = gamepad["leftStick/x"];
var submitButton = gamepad["{Submit}"];
var allSubmitButtons = InputSystem.FindControls("*/{Submit}");
```
