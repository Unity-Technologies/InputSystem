# Supported Input Devices

This page lists Input Device types and products that the Input System package supports, and the platforms they're supported on.

## Generic

Support for the following Devices doesn't require specialized support of particular products.

|Device|Windows|Mac|Linux|UWP|Android|iOS|tvOS|Xbox(3)|PS4(3)|Switch(3)|WebGL|
|------|-------|---|-----|---|-------|---|----|----|---|------|-----|
|[Mouse](Mouse.md)|Yes|Yes|Yes|Yes|Yes|No|No|Yes|Yes|No|Yes|
|[Keyboard](Keyboard.md)|Yes|Yes|Yes|Yes|Yes|No|No|Yes|Yes|No|Yes|
|[Pen](Pen.md)|Yes|No (1)|No|Yes|Yes|Yes|No|No|No|No|No|
|[Touchscreen](Touch.md)|Yes|No|No|Yes|Yes|Yes|Yes(4)|No|No|No|Yes|
|[Sensors](Sensors.md)|No|No|No|No|Yes|Yes|No|No|No|No|Yes(5)|
|[Joystick](#other-gamepads-joysticks-and-racing-wheels) (2)|Yes|Yes|Yes|Yes|Yes|No|No|No|No|No|Yes|

>__Notes__:
>1. Tablet support for macOS is coming in Unity 2020.1.
>2. Joysticks are supported as generic HIDs (See [Other gamepads, joysticks, and racing wheels](#other-gamepads-joysticks-and-racing-wheels) below).
>3. Consoles are supported using separate packages. You need to install these packages in your Project to enable console support.
>4. Indirect touches are received from Siri Remote.
>5. Sensor support for WebGL on Android and iOS devices is available in Unity 2021.2

## Gamepads

|Device|Windows|Mac|Linux|UWP|Android|iOS(6)|tvOS(6)|Xbox(7)|PS4(7)|Switch(7)|WebGL|
|------|-------|---|-----|---|-------|---|----|----|---|------|-----|
|Xbox 360 (4)|Yes|Yes (3)|Yes|Yes|No|No|No|Yes|No|No|Sometimes (2)|
|Xbox One|Yes (1)|Yes (3)|Yes (1)|Yes|Yes (1)|Yes (6)|Yes (6)|Yes|No|No|Sometimes (2)|
|PS4|Yes (5)|Yes (5)|Yes (5)|Yes (5)|Yes (5)|Yes (5, 6)|Yes (5, 6)|No|Yes|No|Sometimes (2)|
|Switch|Yes (8)|Yes (8)|Yes|Yes|No|No|No|No|No|Yes|Sometimes (2)|
|MFi (such as SteelSeries)|No|No|No|No|No|Yes|Yes|No|No|No|No|

>__Notes__:
>1. The trigger motors on the Xbox One controller are only supported on UWP and Xbox.
>2. WebGL support varies between browsers, Devices, and operating systems.
>3. XInput controllers on Mac currently require the installation of the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller). This driver only supports only USB connections, and doesn't support wireless dongles. However, the latest generation of Xbox One controllers natively support Bluetooth, and are natively supported on Macs as HIDs without any additional drivers when connected via Bluetooth.
>4. This includes any XInput-compatible Device.
>5. Unity doesn't support the gyro or accelerometer on PS4 controllers on platforms other than the PlayStation 4 console. Unity also doesn't support the DualShock 4 USB Wireless Adaptor.
>6. Unity supports Made for iOS (Mfi) certified controllers on iOS. Xbox One and PS4 controllers are only supported on iOS 13 or higher.
>7. Consoles are supported using separate packages. You need to install these packages in your Project to enable console support.
>8. Unity officially supports PS4 controllers only on [Android 10 or higher](https://playstation.com/en-us/support/hardware/ps4-pair-dualshock-4-wireless-with-sony-xperia-and-android).
>9. Switch Joy-Cons are not currently supported on Windows and Mac. Also, Switch Pro controllers are supported only when connected via Bluetooth but not when connected via wired USB.

### WebGL

The Input System supports the *Standard Gamepad* mapping as specified in the [W3C Gamepad Specification](https://www.w3.org/TR/gamepad/#remapping). It also supports gamepads and joysticks that the browser surfaces without a mapping, but this support is generally limited to detecting the axes and buttons which are present, without any context as to what they mean. This means gamepads and joysticks are generally only useful when [the user manually remaps them](HowDoI.md#create-a-ui-to-rebind-input-in-my-game). The Input System reports these Devices as generic [`Joysticks`](../api/UnityEngine.InputSystem.Joystick.html).

Support varies between browsers, Devices, and operating systems, and further differs for different browser versions, so it's not feasible to provide an up-to-date compatibility list. At the time of this publication (September 2019), Safari, Chrome, Edge, and Firefox all support the gamepad API, but only Chrome reliably maps common gamepads (Xbox and PlayStation controllers) to the W3C Standard Gamepad mapping, which allows the Input System to correctly identify and map controls.

>__Note__: WebGL currently doesn't support rumble.

## Other gamepads, joysticks, and racing wheels

The Input System supports any Device which implements the USB HID specification. However, for Devices which don't have specific [layouts](Layouts.md) implemented in the Input System, the system can only surface the information available from the HID descriptor of the Device, which limits how precisely it can describe a control. These Devices often work best when allowing the user to [manually remap the controls](HowDoI.md#create-a-ui-to-rebind-input-in-my-game). If you need to support a specific Device, you can also [add your own mapping for it](HowDoI.md#create-my-own-custom-devices). See documentation on [HID](HID.md) for more information.
