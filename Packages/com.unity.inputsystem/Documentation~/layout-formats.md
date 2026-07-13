---
uid: input-system-layout-formats
---

# Layout formats

Add new layouts using the approach that fits how you author and register devices in your project.

You can define layouts in three ways: as C# types, as JSON, or with the layout builder API at runtime. Use C# or JSON when your layout is fixed at build time. Use the layout builder when device structure is discovered dynamically, such as for [HID](hid-specification.md) devices.

| **Topic** | **Description** |
| :--- | :--- |
| **[Add a layout from C#](add-layout-from-cs.md)** | Register a layout using C# classes derived from InputControl or InputDevice. |
| **[Add a layout from JSON](add-layout-from-json.md)** | Register a layout from JSON to store or load definitions separately from your code. |
| **[Add a layout using Layout Builder](add-layout-using-layout-builder.md)** | Build a layout at runtime with the layout builder API. |

## Additional resources

- [Human Interface Device specification](hid-specification.md)
- [Use an existing input device to create a layout](hid-create-custom-layout-existing.md)
- [Create a custom device](create-custom-device.md)
