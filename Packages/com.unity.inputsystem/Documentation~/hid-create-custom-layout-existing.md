---
uid: input-system-use-existing-layout
---

# Use an existing input device to create a layout

To use one of the existing C# [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) classes in code to interface with a device, you can build on an existing layout using JSON:

```json

   {
        "name" : "MyDevice",
        "extend" : "Gamepad", // Or some other thing
        "controls" : [
            {
                "name" : "firstButton",
                "layout" : "Button",
                "offset" : 0,
                "bit": 0,
                "format" : "BIT",
            },
            {
                "name" : "secondButton",
                "layout" : "Button",
                "offset" : 0,
                "bit": 1,
                "format" : "BIT",
            },
            {
                "name" : "axis",
                "layout" : "Axis",
                "offset" : 4,
                "format" : "FLT",
                "parameters" : "clamp=true,clampMin=0,clampMax=1"
            }
        ]
    }

```

You then register your layout with the system and then instantiate it:

```c#

   InputSystem.RegisterControlLayout(myDeviceJson);
    var device = InputSystem.AddDevice("MyDevice");

```
