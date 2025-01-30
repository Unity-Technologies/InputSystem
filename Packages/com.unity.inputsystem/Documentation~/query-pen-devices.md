# Query pen devices in code

To query the last used or last added pen, use [Pen.current](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_current).

Note: Some devices support tracking multiple pens independently. Unity's Input System doesn't support this currently.

## Access the pressure of the pen

To access the pen's current pressure, use [Pen.pressure](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_pressure), where 0 means no pressure, and 1 means maximum pressure. 

Pressure can go over 1 if the system applies a custom pressure curve where a pressure value of 1 doesn't require pressing the pen down all the way to the maximum force the hardware supports. If a pen doesn't support different pressure levels, the Pen.pressure always returns 1\.

## Access the tilt of the pen

If supported, the [Pen.tilt](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_tilt) control represents the angle at which the pen tilts towards the tablet or screen surface. The X and Y axes correspond to the respective screen axes. A value of 1 on either axis means that the pen is fully parallel to the tablet or screen surface on that axis. A value of 0 means that the pen is perpendicular to the tablet or screen surface on that axis. If a pen doesn't support tilt angles, Pen.tilt is always (0,0).

## Access the rotation of the pen

Some pens support twist detection (the pen rotating around its own axis). If supported, [Pen.twist](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_twist) represents the current rotation, where 0 means that the pen is facing up towards the Y axis, and values close to 1 mean that the pen is fully rotated clockwise around its own axis.

## In-range detection

A pen might not need to touch the tablet or screen surface to be able to control the cursor. You can use the [inRange](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_inRange) button Control to determine whether the pen is currently in detection range. If [inRange](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_inRange) reports as pressed, the pen registers with the tablet or screen. For devices that don't support this feature, [inRange](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_inRange) always reports as pressed.

## Barrel buttons

Pen devices often have one or multiple buttons on the side of the pen. These are represented by the [firstBarrelButton](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_firstBarrelButton), [secondBarrelButton](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_secondBarrelButton), [thirdBarrelButton](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_thirdBarrelButton), and [fourthBarrelButton](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pen.html#UnityEngine_InputSystem_Pen_fourthBarrelButton) where applicable.