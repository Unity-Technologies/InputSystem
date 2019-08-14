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
|Joystick(3)|Yes|Yes|Yes|Yes|Yes|No|No|No|No|Yes|

    (1) Tablet support for Mac is planned for 2020.1.
    (2) Tablet support for UWP is planned for 2019.3.
    (3) Joysticks are supported as generic HIDs ...
    (4) Touchscreen support for Switch is planned for 2019.3.
    (5) The gyro/accelerometer in the PS4 controller is supported but is built
        directly into the gamepad and not represented as a separate Gyroscope
        device.

## Gamepads

|Device|Windows|Mac|Linux|UWP|Android|iOS|Xbox|PS4|Switch|WebGL|
|------|-------|---|-----|---|-------|---|----|---|------|-----|
|Xbox 360(4)|Yes|Yes(3)|Yes|Yes|No|No|Yes|No|No|Sometimes(2)|
|Xbox One|Yes(1)|Yes(3)|Yes(1)|Yes|Yes(1)|No|Yes|No|No|Sometimes(2)|
|PS4|Yes(5)|Yes(5)|Yes(5)|Yes(5)|Yes(5)|No|No|Yes|No|Sometimes(2)|
|Switch|Yes|Yes|Yes|Yes|No|No|No|No|Yes|Sometimes(2)|

    (1) The trigger motors on the Xbox One controller are only supported on
        UWP and Xbox at the moment.
    (2) WebGL support varies wildly between browsers, devices, and OSes.
    (3) XInput controllers on Mac currently require the installation of the
        TattieBogle Xbox 360 controller driver which can be found at
        https://github.com/360Controller/360Controller. However, the latest
        generation of Xbox One controllers are natively supported on Macs as HID devices
        when connected via Bluetooth.
    (4) This includes any XInput-compatible device.
    (5) We do not support the gyro/accelerometer on PS4 controllers on platforms
        other than the PS4 at the moment. Also, on such platforms, we only support
        PS4 controllers when connected via Bluetooth or USB; we do NOT support the
        "DualShock 4 USB Wireless Adaptor".

### WebGL

The input system supports the "Standard Gamepad" mapping as specified in the [W3C Gamepad Specification](https://www.w3.org/TR/gamepad/#remapping). We also support gamepads and joysticks that are surfaced by the browser without a mapping, but that support is generally limited to detecting axes and buttons which are present, without any context as to what they mean, which means that this is generally only useful when [manually remapped by the user](HowDoI.md#-create-a-ui-to-rebind-input-in-my-game). Such devices will be reported as generic [`Joysticks`](../api/UnityEngine.InputSystem.Joystick.html) by the input system.

Support varies wildly between browsers, devices, and OSes, and is changing between browser releases, so it is not feasible to provide an up-to-date compatibility list. As of this writing (August 2019), Safari, Chrome, Edge and Firefox all support the gamepad API, but only Chrome reliably maps common gamepads (Xbox and PlayStation controllers) to the W3C Standard Gamepad mapping, allowing us to correctly identify and map controls.

>NOTE: There is no rumble support currently on WebGL.

## Other Gamepads, Joysticks and Racing Wheels

The input system supports any device which implements the USB HID specification. However, for devices which don't have specific [layouts](Layouts.md) implemented in the input system, we can only surface the information available from the HID descriptor of the device, which limits how precisely we can describe a control. These devices often work best when allowing the user to manually [manually remap the controls](HowDoI.md#-create-a-ui-to-rebind-input-in-my-game). If you need to support a specific device, you can also [add your own mapping for it](HowDoI.md#-create-my-own-custom-devices). See the [manual page on HID](HID.md) for more information.
