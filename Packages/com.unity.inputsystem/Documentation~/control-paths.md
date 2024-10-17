# Control paths

>Example: `<Gamepad>/leftStick/x` means "X Control on left stick of gamepad".

The Input System can look up Controls using textual paths. [Bindings](ActionBindings.md) on Input Actions rely on this feature to identify the Control(s) they read input from. However, you can also use them for lookup directly on Controls and Devices, or to let the Input System search for Controls among all devices using [`InputSystem.FindControls`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_FindControls_System_String_).

```CSharp
var gamepad = Gamepad.all[0];
var leftStickX = gamepad["leftStick/x"];
var submitButton = gamepad["{Submit}"];
var allSubmitButtons = InputSystem.FindControls("*/{Submit}");
```

Control paths resemble file system paths. Each path consists of one or more components separated by a forward slash:

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
