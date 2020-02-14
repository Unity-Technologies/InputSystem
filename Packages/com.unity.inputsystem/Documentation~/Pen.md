# Pen, tablet, and stylus support

Pen support comprises both tablets on desktops (such as the various tablets produced by Wacom), and styluses on mobile devices (such as the stylus on the Samsung Note, the Apple Pencil on iOS, or the Surface Pen on the Microsoft Surface line of notebooks).

Pens generally offer pressure sensitivity, in-range detection (being able to control the cursor while not yet touching the tablet/screen surface), and often the ability to flip the pen for eraser-like behavior.

Pens are represented by the [`Pen`](../api/UnityEngine.InputSystem.Pen.html) Device layout implemented by the [`Pen`](../api/UnityEngine.InputSystem.Pen.html) class. Pens are based on the [`Pointer`](Pointers.md) layout.

You can query the last used or last added pen with [`Pen.current`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_current).

>__Note__:
>* Pen/tablet support is currently implemented on Windows, UWP, iOS, and Android. Support on macOS is coming in Unity 2020.1.
>* Some devices support tracking multiple pens independently. Unity's Input System doesn't support this currently.

## Controls

In addition to the [Controls inherited from `Pointer`](Pointers.md#controls), pen Devices implement the following Controls:

|Control|Type|Description|
|-------|----|-----------|
|[`tip`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_tip)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Whether the tip of the pen touches the surface. Same as the inherited [`Pointer.press`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_press).|
|[`eraser`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_eraser)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Whether the eraser/back end of the pen touches the surface.|
|[`firstBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_firstBarrelButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Whether the first button on the barrel of the pen is pressed.|
|[`secondBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_secondBarrelButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Whether the second button on the barrel of the pen is pressed.|
|[`thirdBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_thirdBarrelButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Whether the third button on the barrel of the pen is pressed.|
|[`fourthBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_fourthBarrelButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Whether the forth button on the barrel of the pen is pressed.|
|[`inRange`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_inRange)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Whether the pen is currently in detection range of the tablet.|
|[`tilt`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_tilt)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|Tilt of the pen relative to the surface.|
|[`twist`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_twist)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|Rotation of the pen around its own axis. Only supported on a limited number of pens, such as the Wacom Art Pen.|

## Pressure, tilt, and twist

**Pressure:** You can access the pen's current pressure via  [`Pen.pressure`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_pressure), where 0 means no pressure, and 1 means maximum pressure. However, pressure can go over 1 if the system applies a custom pressure curve where a pressure value of 1 doesn't require pressing the pen down all the way to the maximum force the hardware supports. If a pen doesn't support  different pressure levels, the  [`pressure`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_pressure) Control always returns 1.

**Tilt:** If supported, the [`Pen.tilt`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_tilt) Control represents the angle at which the pen tilts towards the tablet or screen surface. The X and Y axes correspond to the respective screen axes. A value of 1 on either axis means that the pen is fully parallel to the tablet or screen surface on that axis. A value of 0 means that the pen is perpendicular to the tablet or screen surface on that axis. If a pen doesn't support tilt angles, `Pen.tilt` is always `(0,0)`.

**Twist:** Some pens also support twist detection (the pen rotating around its own axis). If supported, [`Pen.twist`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_twist) represents the current rotation, where 0 means that the pen is facing up towards the Y axis, and values close to 1 mean that the pen is fully rotated clockwise around its own axis.

## In-range detection

A pen might not need to touch the tablet or screen surface in order to be able to control the cursor. You can use the [`inRange`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_inRange) button Control to determine whether the pen is currently in detection range. If [`inRange`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_inRange) reports as pressed, the pen registers with the tablet or screen. For Devices that don't support this feature, [`inRange`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_inRange) always reports as pressed.

## Barrel buttons

Pen Devices often have one or multiple buttons on the side of the pen. These are represented by the [`firstBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_firstBarrelButton), [`secondBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_secondBarrelButton), [`thirdBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_thirdBarrelButton), and [`fourthBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_fourthBarrelButton) where applicable.
