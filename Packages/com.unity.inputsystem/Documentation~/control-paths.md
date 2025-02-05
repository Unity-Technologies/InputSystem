# Control paths

Control paths represent the way to describe a control or group of controls on a device, when [binding](bindings.md) them to an [action](actions.md).

An example of a control path is `<Gamepad>/leftStick/x`, which refers to the X-axis of the left stick of any gamepad.

At runtime, the Input System performs a look-up of all control paths against the currently connected devices to discover which controls match the ones specified in the paths. This process is called [binding resolution](binding-resolution.md). 


## Specificity

When [selecting a control path for binding](select-control-binding.md), there are various levels of specificity you can use. These are available in the **Control paths** menu, in the [Binding Properties panel](binding-properties-panel.md).

These levels of specificity depend on whether you want to refer to a certain type of device, specific models of device, or certain usages of a control regardless of device type.

### By generic device

You can refer to a generic type of device, for example referring to a common control across all types of gamepad (such as the left stick on a gamepad). In this case, this matches the left stick on all types of gamepad.

### By specific device

You can refer to a specific control on a specific model of gamepad (such as the `A` button on an Xbox controller). In this case, the path does not match any controls on any other type of gamepad even if they're similar in position or usage.

### By usage

You can also refer to controls by usage, which allows you to use a variety of common names that controls are associated with by convention, such as a `Back` button, or a `Submit` button. See [control usages](control-usages.md) for more information about these.

## The device and control tree

The device and control tree is organized hierarchically from generic to specific. For example, the __Gamepad__ Control path `<Gamepad>/buttonSouth` matches the lower action button on any gamepad. Alternatively, if you navigate to __Gamepad__ > __More Specific Gamepads__ and select __PS4 Controller__, and then choose the Control path `<DualShockGamepad>/buttonSouth`, this only matches the "Cross" button on PlayStation gamepads, and doesn't match any other gamepads.


## Format

Control paths are strings, which resemble file system paths. Each path consists of one or more components separated by a forward slash:

    component/component...

Each component uses a similar syntax made up of multiple fields. Each field is optional, but at least one field must be present. All fields are case-insensitive.

    <layoutName>{usageName}controlName#(displayName)

The following table explains the use of each field:

|Field|Description|Example|
|-----|-----------|-------|
|`<layoutName>`|Requires the Control at the current level to be based on the given layout. The actual layout of the Control may be the same or a layout *based* on the given layout.|`<Gamepad>/buttonSouth`|
|`{usageName}`|Works differently for Controls and Devices.<br><br>When used on a Device (the first component of a path), it requires the device to have the given usage. See [Device usages](Devices.md#device-usages) for more details.<br><br>For looking up a Control, the usage field is currently restricted to the path component immediately following the Device (the second component in the path). It finds the Control on the Device that has the given usage. The Control can be anywhere in the Control hierarchy of the Device.|Device:<br><br>`<XRController>{LeftHand}/trigger`<br><br>Control:<br><br>`<Gamepad>/{Submit}`|
|`controlName`|Requires the Control at the current level to have the given name. Takes both "proper" names ([`InputControl.name`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_name)) and aliases ([`InputControl.aliases`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_aliases)) into account.<br><br>This field can also be a wildcard (`*`) to match any name.|`MyGamepad/buttonSouth`<br><br>`*/{PrimaryAction}` (match `PrimaryAction` usage on Devices with any name)|
|`#(displayName)`|Requires the Control at the current level to have the given display name (i.e. [`InputControl.displayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_displayName)). The display name may contain whitespace and symbols.|`<Keyboard>/#(a)` (matches the key that generates the "a" character, if any, according to the current keyboard layout).<br><br>`<Gamepad>/#(Cross)`|

### Wildcard characters

If you entering a control path as text, you can use the wildcard asterisk character (`*`) to match multiple controls in the hierarchy specified. For example, you can use `<Touchscreen>/touch*/press` to bind to any finger pressed on the touchscreen, instead of individually binding to `<Touchscreen>/touch0/press`, `<Touchscreen>/touch1/press` and each other numbered touch separately.


## Access from code

You can access the literal path of a given control via its [`InputControl.path`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_path) property.

If needed, you can manually parse a control path into its components using the [`InputControlPath.Parse(path)`](../api/UnityEngine.InputSystem.InputControlPath.html#UnityEngine_InputSystem_InputControlPath_Parse_System_String_) API.

```CSharp
var parsed = InputControlPath.Parse("<XRController>{LeftHand}/trigger").ToArray();

Debug.Log(parsed.Length); // Prints 2.
Debug.Log(parsed[0].layout); // Prints "XRController".
Debug.Log(parsed[0].name); // Prints an empty string.
Debug.Log(parsed[0].usages.First()); // Prints "LeftHand".
Debug.Log(parsed[1].layout); // Prints null.
Debug.Log(parsed[1].name); // Prints "trigger".
```

You can use control paths to directly reference controls, or to let the Input System search for Controls among all devices using [`InputSystem.FindControls`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_FindControls_System_String_).

```CSharp
var gamepad = Gamepad.all[0];
var leftStickX = gamepad["leftStick/x"];
var submitButton = gamepad["{Submit}"];
var allSubmitButtons = InputSystem.FindControls("*/{Submit}");
```
