---
uid: input-system-layouts
---
# Layouts

Define how the Input System represents input devices and controls in memory.

Layouts are an advanced feature you use when you support custom devices or change how existing devices behave. Each layout describes a memory format for input and the controls that read and write that data. The Input System uses layouts to create the correct device types and interpret incoming input.

For end-to-end custom device setup, refer to [Custom devices](custom-devices.md).

| **Topic** | **Description** |
| :--- | :--- |
| **[About layouts](about-layouts.md)** | Learn what layouts are, how the Input System loads them, and when you need them. |
| **[Layout formats](layout-formats.md)** | Add layouts from C# types, JSON, or the layout builder API. |
| **[Layout inheritance](layout-inheritance.md)** | Derive a layout from an existing layout to reuse and extend control definitions. |
| **[Control items](control-items.md)** | Configure the controls in a layout and set their properties. |
| **[Override layout definitions](override-layout-definitions.md)** | Change an existing layout without replacing the original definition. |
| **[Precompiled layouts](precompiled-layouts.md)** | Speed up device creation by baking layouts into precompiled C# code. |

## Additional resources

- [Custom devices](custom-devices.md)
- [Debug layouts](debug-layouts.md)
- [Human Interface Device specification](hid-specification.md)
