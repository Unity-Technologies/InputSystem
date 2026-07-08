---
uid: input-system-simulate-touch
---

# Simulate touches

You can simulate touch input from [pointer devices](pointers-introduction.md) such as [mouse](devices-mouse.md) and [pen](devices-pen.md) devices in the following ways:

* [Enable touch simulation in the Unity Editor](#add-touch-simulation-to-a-gameobject).
* [Add touch simulation to a GameObject](#add-touch-simulation-to-a-gameobject).
* [Enable touch simulation in startup code](#enable-touch-simulation-in-the-unity-editor).

## Enable touch simulation in the Unity Editor

To enable touch simulation in the Unity Editor, perform the following steps:

1. Open the [Input Debugger](the-input-debugger-window.md) (**Window** > **Analysis** > **Input Debugger**)
1. Select the **Options** dropdown, and enable **Simulate Touch Input From Mouse or Pen**.

## Add touch simulation to a GameObject

Add the [`TouchSimulation`](xref:UnityEngine.InputSystem.EnhancedTouch.TouchSimulation) MonoBehaviour to a GameObject in your scene. `TouchSimulation` adds a [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) device and automatically mirrors input on any [Pointer](xref:UnityEngine.InputSystem.Pointer) device to the virtual touchscreen device.

## Enable touch simulation in startup code

Call [`TouchSimulation.Enable`](xref:UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable) somewhere in your startup code:

```c#

   void OnEnable()
    {
        TouchSimulation.Enable();
    }

```
