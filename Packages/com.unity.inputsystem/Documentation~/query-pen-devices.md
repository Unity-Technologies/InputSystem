---
uid: input-system-query-pen-devices
---

# Query pen devices in code

To query the last used or last added pen, use [`Pen.current`](xref:UnityEngine.InputSystem.Pen.current).

> [!NOTE]
> Some devices support tracking multiple pens independently, but the Input System doesn't support this currently.

## Access the pressure of the pen

To access the pen's current pressure, use [`Pen.pressure`](xref:UnityEngine.InputSystem.Pointer.pressure), where 0 means no pressure, and 1 means maximum pressure.

Pressure can go over 1 if the system applies a custom pressure curve where a pressure value of 1 doesn't require pressing the pen down all the way to the maximum force the hardware supports. If a pen doesn't support different pressure levels, `Pen.pressure` always returns 1.

## Access the tilt of the pen

If supported, the [`Pen.tilt`](xref:UnityEngine.InputSystem.Pen.tilt) control represents the angle at which the pen tilts towards the tablet or screen surface. The x and y axes correspond to the respective screen axes. A value of 1 on either axis means that the pen is fully parallel to the tablet or screen surface on that axis. A value of 0 means that the pen is perpendicular to the tablet or screen surface on that axis. If a pen doesn't support tilt angles, `Pen.tilt` is always (0,0).

## Access the rotation of the pen

Some pens support twist detection (the pen rotating around its own axis). If supported, [`Pen.twist`](xref:UnityEngine.InputSystem.Pen.twist) represents the current rotation, where 0 means that the pen is facing up towards the y-axis, and values close to 1 mean that the pen is fully rotated clockwise around its own axis.

## In-range detection

A pen might not need to touch the tablet or screen surface to be able to control the cursor. You can use the [`inRange`](xref:UnityEngine.InputSystem.Pen.inRange) button Control to determine whether the pen is currently in detection range. If `inRange` reports as pressed, the pen registers with the tablet or screen. For devices that don't support this feature, `inRange` always reports as pressed.

## Barrel buttons

Pen devices often have one or multiple buttons on the side of the pen. These are represented by the [`firstBarrelButton`](xref:UnityEngine.InputSystem.Pen.firstBarrelButton), [`secondBarrelButton`](xref:UnityEngine.InputSystem.Pen.secondBarrelButton), [`thirdBarrelButton`](xref:UnityEngine.InputSystem.Pen.thirdBarrelButton), and [`fourthBarrelButton`](xref:UnityEngine.InputSystem.Pen.fourthBarrelButton) where applicable.
