# Pen/Tablet/Stylus Support

Pen support comprises both tablets on desktops (such as the various tablets produced by Wacom) as well as styluses support on mobile devices such as the stylus on the Samsung Note, the Apple Pencil on iOS, or the Surface Pen on the Micrsoft Surface line of notebooks.

Pens generally offer pressure-sensitivity, in-range detection (i.e. being able to control the cursor while not yet touching the tablet/screen surface), and often the ability to flip the pen for eraser-like behavior.

Pens are represented by the [`Pen`](../api/UnityEngine.InputSystem.Pen.html) device layout which is implemented by the [`Pen`](../api/UnityEngine.InputSystem.Pen.html) class. Pens are based on the [`Pointer`](Pointers.md) layout.

The last used or last added pen can be queried with [`Pen.current`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_current).

>NOTES:
>* Pen/tablet support is currently implemented on Windows, UWP, iOS, and Android. Support on macOS is coming in Unity 2020.1.
>* Some devices support tracking multiple pens independently. This is not currently supported by the input system in Unity.


## Controls

Additional to the [controls inherited from `Pointer`](Pointers.md#controls), Pen devices implement the following controls:

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
|[`twist`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_twist)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|Rotation of the pen around its own axis. Only supported on few devices.|

## Pressure, Tilt, Twist

The current pressure is available through the [`Pen.pressure`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_pressure) control. Pen pressure is normalized (0=no pressure, 1=maximum pressure). However, note that pressure may go __beyond__ 1 in case the system is applying a custom pressure curve where reaching a pressure value of 1 does not require pressing the pen down all the way to the maximum supported by hardware.

If a pen does not support pressure, the [`pressure`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_pressure) control will always return 1.

If supported, the [`Pen.tilt`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_tilt) control represents the tilt angle of the pen, i.e. the angle with which the pen is titled towards the tablet/screen surface. The X and Y axis correspond to respective screen axes. A value of 1 on either axis means that the pen is fully parallel to the tablet/screen surface on the respective axis whereas a value of 0 means that the pen is perpendicular to the tablet/screen surface on the respective axis. If not support, `Pen.tilt` will always be `(0,0)`.

A small number of pens (such as the Wacom Art Pen) also support twist, i.e. detecting the pen rotating about its own axis. If support, [`Pen.twist`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_twist) represents the current rotation with 0 meaning the pen is facing up on Y and values close to 1 meaning the pen is fully rotated clock-wise about its own axis.

## In-Range Detection

A pen may not need to touch the tablet/screen surface in order to be able to control the cursor. The [`inRange`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_inRange) control can be used to determine whether the pen is currently in detection range. If the button is on, the pen registers with the tablet/screen.

For devices that do not support this feature, [`inRange`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_inRange) is always on.

## Barrel Buttons

Pen devices often have one or multiple buttons on the side of the pen. These are represented by the [`firstBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_firstBarrelButton), [`secondBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_secondBarrelButton), [`thirdBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_thirdBarrelButton) and [`fourthBarrelButton`](../api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_fourthBarrelButton) where applicable.
