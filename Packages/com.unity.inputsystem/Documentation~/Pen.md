    ////WIP

>NOTE: Pen/tablet support is currently implemented on Windows, iOS, and Android. Support on UWP and Mac is planned for 2019.2.

# Pen/Tablet/Stylus Support

Pen support comprises both tablets on desktops (such as the various tablets produced by Wacom) as well as styluses support on mobile devices such as the stylus on the Samsung Note, the Apple Pencil on iOS, or the Surface Pen on the Micrsoft Surface line of notebooks.

Pens generally offer pressure-sensitivity, in-range detection (i.e. being able to control the cursor while not yet touching the tablet/screen surface), and often the ability to flip the pen for eraser-like behavior.

## Controls

|Control|Type|Description|
|-------|----|-----------|
|position|Vector2||
|delta|Vector2||
|pressure|Vector2||
|tilt|Vector2||
|twist|Axis||
|tip|Button||
|eraser|Button||
|firstBarrelButton|Button||
|secondBarrelButton||
|inRange|Button|Whether the pen is currently in detection range.|

## Pressure, Tilt, Twist

    ////TODO: explain 2D pressure (inherited from Pointer)

The current pressure is available through the `Pen.pressure` control. Pen pressure is normalized (0=no pressure, 1=maximum pressure). However, note that pressure may go __beyond__ 1 in case the system is applying a custom pressure curve where reaching a pressure value of 1 does not require pressing the pen down all the way to the maximum supported by hardware.

If a pen does not support pressure, the `pressure` control will always return 1.

If supported, the `Pen.tilt` control represents the tilt angle of the pen, i.e. the angle with which the pen is titled towards the tablet/screen surface. The X and Y axis correspond to respective screen axes. A value of 1 on either axis means that the pen is fully parallel to the tablet/screen surface on the respective axis whereas a value of 0 means that the pen is perpendicular to the tablet/screen surface on the respective axis. If not support, `Pen.tilt` will always be `(0,0)`.

A small number of pens (such as the Wacom Art Pen) also support twist, i.e. detecting the pen rotating about its own axis. If support, `Pen.twist` represents the current rotation with 0 meaning the pen is facing up on Y and values close to 1 meaning the pen is fully rotated clock-wise about its own axis.

## In-Range Detection

A pen may not need to touch the tablet/screen surface in order to be able to control the cursor. The `inRange` control can be used to determine whether the pen is currently in detection range. If the button is on, the pen registers with the tablet/screen.

For devices that do not support this feature, `inRange` is always on.

## Barrel Buttons

## Pen vs Touch

    ////WIP

An application may wish to surface pen input as touch input rather than as pen input.

## Multi-Pen Usage

    We do not currently support pen IDs. We are working on it.
