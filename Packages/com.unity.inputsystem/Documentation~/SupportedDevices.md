# Supported Input Devices

This page lists the types/products of input devices supported by the input system and the platforms they are supported on.

## Generic

The following devices are supported in a way that does not require recognition of particular products.

|Device|Windows|Mac|Linux|UWP|Android|iOS|Xbox|PS4|Switch|WebGL|
|------|-------|---|-----|---|-------|---|----|---|------|-----|
|Mouse|Yes|Yes|Yes|Yes|Yes|No|Yes|Yes|No|Yes|
|Keyboard|Yes|Yes|Yes|Yes|Yes|No|Yes|Yes|No|Yes|
|Pen|Yes|No(1)|No|No(2)|Yes|Yes|No|No|No|No|
|Touchscreen|Yes|No|No|Yes|Yes|Yes|No|No|No(4)|No|
|Gyroscope|No|No|No|No|Yes|Yes|No|No(5)|No|No|
|Accelerometer|No|No|No|No|Yes|Yes|No|No(5)|No|No|
|Compass|No|No|No|No|Yes|Yes|No|No|No|No|
|Joystick(3)|Yes|Yes|Yes|Yes|Yes|No|No|No|No|No|

    (1) Tablet support for Mac is planned for 2019.3.
    (2) Tablet support for UWP is planned for 2019.3.
    (3) Joysticks are supported as generic HIDs ...
    (4) Touchscreen support for Switch is planned for 2019.3.
    (5) The gyro/accelerometer in the PS4 controller is supported but is built
        directly into the gamepad and not represented as a separate Gyroscope
        device.

## Gamepads

|Device|Windows|Mac|Linux|UWP|Android|iOS|Xbox|PS4|Switch|WebGL|
|------|-------|---|-----|---|-------|---|----|---|------|-----|
|Xbox 360(4)|Yes|Yes(3)|Yes|Yes|No|No|Yes|No|No|No(2)|
|Xbox One|Yes(1)|Yes(1)|Yes(1)|Yes|Yes(1)|No|Yes|No|No|No(2)|
|PS4|Yes(5)|Yes(5)|Yes(5)|Yes(5)|Yes(5)|No|No|Yes|No|No|
|Switch|No(6)|No(6)|No|No(6)|No|No|No|No|Yes|No|

    (1) The trigger motors on the Xbox One controller are only supported on
        UWP and Xbox at the moment.
    (2) Implementation of the Gamepad W3C spec varies wildly between browsers.
        We are hoping to improve our support for the mostly wildly used
        controllers across all major browsers.
    (3) XInput controllers on Mac currently require the installation of the
        TattieBogle Xbox 360 controller driver which can be found at
        https://github.com/360Controller/360Controller. We not yet support
        consuming raw USB input.
    (4) This includes any XInput-compatible device.
    (5) We do not support the gyro/accelerometer on PS4 controllers on platforms
        other than the PS4 at the moment.
    (6) Support for the Switch controller on desktops is being worked on.

### WebGL

>NOTE: There is no rumble support currently on WebGL.

>NOTE: WebGL does __NOT__ support running both the old and the new input system in parallel.

The new input system supports the "Standard Gamepad" mapping as specified in the [W3C Gamepad Specification](https://www.w3.org/TR/gamepad/#remapping). We do not yet support gamepads that are surfaced by the browser without a mapping.

The following browser&OS combinations are known to support the "Standard Gamepad" mapping.

|Chrome(Win)|Chrome(Mac)|Chrome(Linux)|Firefox(Win)|Firefox(Mac)|....|
|-----------|-----------|-------------|------------|------------|----|

### Steam

    NOTE: Steam controller API support is still work-in-progress.

For details about Steam controller API support, please consult the [Steam Support Guide](Steam.md).

### MFi

    NOTE: The native changes for this have not yet landed in all releases.

The new input system supports MFi ("Made for iPod/iPhone/iPad") controllers (see [MFi Program](https://developer.apple.com/programs/mfi/)) on MacOS and iOS.

## Joysticks

## Racing Wheels

    Racing Wheel support is planned for later in 2019.

## XR

|Device|Windows|Mac|Linux|UWP|Android|iOS|Xbox|PS4|Switch|WebGL|
|------|-------|---|-----|---|-------|---|----|---|------|-----|
|Vive|
|Rift|
|Daydream|
|WMR|
