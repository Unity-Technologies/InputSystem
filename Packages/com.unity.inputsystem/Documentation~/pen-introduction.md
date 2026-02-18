---
uid: input-system-pen-devices-intro
---

# Pen devices introduction

The Input System supports pen devices on tablets and desktops, such as the various Wacom tablets. It also supports styluses on mobile devices, such as the stylus on the Samsung Note, the Apple Pencil on iOS, or the Surface Pen on the Microsoft Surface line of notebooks.

Pens offer input features such as pressure sensitivity, in-range detection (being able to control the cursor while not yet touching the tablet or screen surface), and the ability to flip the pen for eraser-like behavior.

For a list of platforms that support pen devices, refer to [Supported devices reference](supported-devices-reference.md).

Pens are represented by the [`PenState`](xref:UnityEngine.InputSystem.LowLevel.PenState) [device layout](layouts.md) implemented by the `Pen` class. Pens are based on the [pointer layout](pointers-introduction.md). The `Pen` class inherits the controls from the `Pointer` layout, and also implements some additional controls. For more information, refer to the [Pen class API documentation](xref:UnityEngine.InputSystem.Pen).
