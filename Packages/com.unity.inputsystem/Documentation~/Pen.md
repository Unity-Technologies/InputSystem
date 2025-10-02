---
uid: input-system-pen
---
# Pen, tablet, and stylus support

Pen support comprises both tablets on desktops (such as the various tablets produced by Wacom), and styluses on mobile devices (such as the stylus on the Samsung Note, the Apple Pencil on iOS, or the Surface Pen on the Microsoft Surface line of notebooks).

Pens generally offer pressure sensitivity, in-range detection (being able to control the cursor while not yet touching the tablet/screen surface), and often the ability to flip the pen for eraser-like behavior.

Pens are represented by the [`Pen`](xref:UnityEngine.InputSystem.Pen) Device layout implemented by the [`Pen`](xref:UnityEngine.InputSystem.Pen) class. Pens are based on the [`Pointer`](xref:input-system-pointers) layout.

You can query the last used or last added pen with [`Pen.current`](xref:UnityEngine.InputSystem.Pen.current).

> [!NOTE]
> * Pen/tablet support is currently implemented on Windows, UWP, iOS, and Android. macOS is supported in Unity 2020.1+.
> * Some devices support tracking multiple pens independently. Unity's Input System doesn't support this currently.
> * iOS: The double-tap interaction on the side of the Apple Pencil is not surfaced as input at the moment. Also, no in-range detection is supported and [`inRange`](xref:UnityEngine.InputSystem.Pen.inRange) will remain at its default value.

## Controls

In addition to the [Controls inherited from `Pointer`](xref:input-system-pointers#controls), pen Devices implement the following Controls:

|Control|Type|Description|
|-------|----|-----------|
|[`tip`](xref:UnityEngine.InputSystem.Pen.tip)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Whether the tip of the pen touches the surface. Same as the inherited [`Pointer.press`](xref:UnityEngine.InputSystem.Pointer.press).|
|[`eraser`](xref:UnityEngine.InputSystem.Pen.eraser)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Whether the eraser/back end of the pen touches the surface.|
|[`firstBarrelButton`](xref:UnityEngine.InputSystem.Pen.firstBarrelButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Whether the first button on the barrel of the pen is pressed.|
|[`secondBarrelButton`](xref:UnityEngine.InputSystem.Pen.secondBarrelButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Whether the second button on the barrel of the pen is pressed.|
|[`thirdBarrelButton`](xref:UnityEngine.InputSystem.Pen.thirdBarrelButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Whether the third button on the barrel of the pen is pressed.|
|[`fourthBarrelButton`](xref:UnityEngine.InputSystem.Pen.fourthBarrelButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Whether the forth button on the barrel of the pen is pressed.|
|[`inRange`](xref:UnityEngine.InputSystem.Pen.inRange)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Whether the pen is currently in detection range of the tablet. If unsupported, this control will remain at a value of 1.|
|[`tilt`](xref:UnityEngine.InputSystem.Pen.tilt)|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|Tilt of the pen relative to the surface.|
|[`twist`](xref:UnityEngine.InputSystem.Pen.twist)|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)|Rotation of the pen around its own axis. Only supported on a limited number of pens, such as the Wacom Art Pen.|

## Pressure, tilt, and twist

**Pressure:** You can access the pen's current pressure via  [`Pen.pressure`](xref:UnityEngine.InputSystem.Pointer.pressure), where 0 means no pressure, and 1 means maximum pressure. However, pressure can go over 1 if the system applies a custom pressure curve where a pressure value of 1 doesn't require pressing the pen down all the way to the maximum force the hardware supports. If a pen doesn't support  different pressure levels, the  [`pressure`](xref:UnityEngine.InputSystem.Pointer.pressure) Control always returns 1.

**Tilt:** If supported, the [`Pen.tilt`](xref:UnityEngine.InputSystem.Pen.tilt) Control represents the angle at which the pen tilts towards the tablet or screen surface. The X and Y axes correspond to the respective screen axes. A value of 1 on either axis means that the pen is fully parallel to the tablet or screen surface on that axis. A value of 0 means that the pen is perpendicular to the tablet or screen surface on that axis. If a pen doesn't support tilt angles, `Pen.tilt` is always `(0,0)`.

**Twist:** Some pens also support twist detection (the pen rotating around its own axis). If supported, [`Pen.twist`](xref:UnityEngine.InputSystem.Pen.twist) represents the current rotation, where 0 means that the pen is facing up towards the Y axis, and values close to 1 mean that the pen is fully rotated clockwise around its own axis.

## In-range detection

A pen might not need to touch the tablet or screen surface in order to be able to control the cursor. You can use the [`inRange`](xref:UnityEngine.InputSystem.Pen.inRange) button Control to determine whether the pen is currently in detection range. If [`inRange`](xref:UnityEngine.InputSystem.Pen.inRange) reports as pressed, the pen registers with the tablet or screen. For Devices that don't support this feature, [`inRange`](xref:UnityEngine.InputSystem.Pen.inRange) always reports as pressed.

## Barrel buttons

Pen Devices often have one or multiple buttons on the side of the pen. These are represented by the [`firstBarrelButton`](xref:UnityEngine.InputSystem.Pen.firstBarrelButton), [`secondBarrelButton`](xref:UnityEngine.InputSystem.Pen.secondBarrelButton), [`thirdBarrelButton`](xref:UnityEngine.InputSystem.Pen.thirdBarrelButton), and [`fourthBarrelButton`](xref:UnityEngine.InputSystem.Pen.fourthBarrelButton) where applicable.
