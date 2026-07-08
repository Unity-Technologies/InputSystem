---
uid: input-system-add-layout-from-json
---

# Add a layout from JSON

You can also create a layout from a JSON string that contains the same information. This is mostly useful if you want to be able to store and transfer layout information separate from your code - for instance, if you want to be able to add support for new Devices dynamically without making a new build of your application. You can use [`InputControlLayout.ToJson()`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout) and [`InputControlLayout.FromJson()`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout) to convert layouts to and from the format.

The same layout as above looks like this in JSON format:

```
{
    "name": "MyDevice",
    "format": "MDEV",
    "controls": [
        {
            "name": "button1",
            "layout": "Button",
            "offset": 0,
            "bit": 0,
        },
        {
            "name": "button2",
            "layout": "Button",
            "offset": 0,
            "bit": 1,
        },
        {
            "name": "dpad",
            "layout": "Dpad",
            "offset": 0,
            "bit": 2,
            "sizeInBits": 4,
        },
        {
            "name": "dpad/up",
            "offset": -1,
            "bit": 2,
        },
        {
            "name": "dpad/down",
            "offset": -1,
            "bit": 3,
        },
        {
            "name": "dpad/left",
            "offset": -1,
            "bit": 4,
        },
        {
            "name": "dpad/right",
            "offset": -1,
            "bit": 5,
        },
        {
            "name": "stick",
            "layout": "Stick",
            "offset": 4,
            "format": "VEC2",
        },
        {
            "name": "trigger",
            "layout": "Axis",
            "offset": 12,
            "format": "BYTE",

        }
    ]
}
```
