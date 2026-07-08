---
uid: input-system-supported-devices
---
# Supported Input Devices

This page lists Input Device types and products that the Input System package supports, and the platforms they're supported on. The devices are divided into the following categories:

* [Generic devices](#generic-devices): pointers, keyboards, and joysticks which don't require specialized support of particular products.
* [Gamepads](#gamepads): devices which might require specialized platform support.

## Generic devices

Support for the following devices doesn't require specialized support of particular products. The following table outlines the generic devices that each platform supports.

| | Mouse | Keyboard | Pen | Touchscreen | Sensors | Joystick (as a generic HID device) |
| :---- | :---- | :---- | :---- | :---- | :---- | :---- |
| **Windows** | Supported | Supported | Supported | Supported | Not supported | Supported |
| **macOS** | Supported | Supported | Not supported | Not supported | Not supported | Supported |
| **Linux** | Supported | Supported | Not supported | Supported | Not supported | Supported |
| **UWP** | Supported | Supported | Supported | Supported | Not supported | Supported |
| **Android** | Supported | Supported | Supported | Supported | Supported | Supported |
| **iOS** | Not supported | Not supported | Supported | Supported | Supported | Not supported |
| **tvOS** | Not supported | Not supported | Not supported | Supported (indirect touches received from Siri Remote) | Not supported | Not supported |
| **Xbox** | Supported | Supported | Not supported | Not supported | Not supported | Not supported |
| **PlayStation** | Supported | Supported | Not supported | Not supported | Not supported | Not supported |
| **Switch** | Not supported | Not supported | Not supported | Not supported | Not supported | Not supported |
| **WebGL** | Supported | Supported | Not supported | Supported | Supported (Android and iOS from Unity 2021.2) | Supported |

> [!NOTE]
> * Joysticks are supported as generic HIDs; refer to [Other gamepads, joysticks, and racing wheels](#other-gamepads-joysticks-and-racing-wheels) to learn more.
> * Consoles are supported using separate packages. You need to install these packages in your Project to enable console support.

### Other gamepads, joysticks, and racing wheels

The Input System supports any Device which implements the USB HID specification. However, for Devices which don't have specific [layouts](xref:input-system-layouts) implemented in the Input System, the system can only surface the information available from the HID descriptor of the Device, which limits how precisely it can describe a control. These Devices often work best when allowing the user to [manually remap the controls](xref:UnityEngine.InputSystem.InputActionRebindingExtensions). If you need to support a specific Device, you can also [add your own mapping for it](xref:input-system-hid#creating-a-custom-device-layout). See documentation on [HID](xref:input-system-hid) for more information.

## Gamepads

The reference tables are organized by platform type:

* [Desktop](#desktop-device-support): Windows, macOS, Linux, Universal Windows Platform
* [Mobile](#mobile-device-support): Android, iOS, tvOS
* [Consoles](#console-device-support): Xbox, PlayStation, Switch
* [WebGL](#webgl-device-support)

The reference tables use the following support levels:

* **Supported**: Works with Unity projects.
* **Partially**: Only some functionality of the gamepad works with Unity projects.
* **Not supported**: Device not supported by Unity.
* **Not compatible**: Device not supported by the platform.

### Desktop device support

| Gamepad | Windows | macOS | Linux | Universal Windows Platform |
| :---- | :---- | :---- | :---- | :---- |
| **Xbox 360** | Supported | Supported ([driver required](https://github.com/360Controller/360Controller)) | Supported | Supported |
| **Xbox One** | Supported (trigger motors only on UWP and Xbox) | Supported ([driver required](https://github.com/360Controller/360Controller)) | Supported (trigger motors only on UWP and Xbox) | Supported |
| **PS3/PS4** | Supported (no motor rumble or light bar over Bluetooth) | Supported (no motor rumble or light bar over Bluetooth) | Supported (no motor rumble or light bar over Bluetooth) | Supported (USB only) |
| **PS5** | Supported | Supported | Supported (Unity 6000.4+, [additional setup on some distributions](#ps5-controller-support-on-linux)) | Supported |
| **Switch** | Supported (official accessories only) | Supported (official accessories only) | Supported | Supported |
| **MFi (such as SteelSeries)** | Not supported | Partially (SteelSeries Nimbus+ via HID) | Not supported | Not supported |

> [!NOTE]
> * XInput controllers on macOS require the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller). This driver only supports USB connections, and doesn't support wireless dongles. However, the latest generation of Xbox One controllers natively support Bluetooth, and are natively supported on Macs as HIDs without any additional drivers when connected via Bluetooth.
> * Unity doesn't support motor rumble and light bar color over Bluetooth. Unity doesn't support the gyro or accelerometer on PS4/PS5 controllers on platforms other than the PlayStation consoles. Unity also doesn't support the DualShock 4 USB Wireless Adapter. On UWP, only USB connection is supported, motor rumble and light bar are not working correctly.
> * PS5 DualSense is supported on Windows, macOS, and Linux via USB HID. Linux support begins with Unity Editor 6000.4 (refer to [PS5 controller support on Linux](#ps5-controller-support-on-linux)). On all platforms, setting motor rumble and light bar color when connected over Bluetooth is currently not supported.
> * SteelSeries Nimbus+ is supported via HID on macOS. On UWP only USB connection is supported, motor rumble and light bar are not working correctly. On Android it's expected to be working from Android 12. On iOS/tvOS it's currently recognized as a generic gamepad and most controls do work.

#### Windows and macOS

Switch Joy-Cons aren't supported on Windows and macOS. The following official accessories are supported:

* Hori Co HORIPAD for Nintendo Switch
* HORI Pokken Tournament DX Pro Pad
* HORI Wireless Switch Pad
* HORI Real Arcade Pro V Hayabusa in Switch Mode
* PowerA NSW Fusion Wired FightPad
* PowerA NSW Fusion Pro Controller (USB only)
* PDP Wired Fight Pad Pro: Mario
* PDP Faceoff Wired Pro Controller for Nintendo Switch
* PDP Faceoff Deluxe Wired Pro Controller for Nintendo Switch
* PDP Afterglow Wireless Switch Controller
* PDP Rockcandy Wired Controller

#### Universal Windows Platform

To ensure all controller types are detected on UWP, enable the HumanInterfaceDevice setting in [UWP Player Settings](https://docs.unity3d.com/Manual/class-PlayerSettingsWSA.html#Capabilities).

### Mobile device support

| Gamepad | Android | iOS | tvOS |
| :---- | :---- | :---- | :---- |
| **Xbox 360** | Not supported | Not supported | Not supported |
| **Xbox One** | Supported | Supported (iOS 13+) | Supported (iOS 13+) |
| **PS3/PS4** | Supported (Android 10+) | Supported (iOS 13+) | Supported |
| **PS5** | Supported (Android 12+) | Not supported | Not supported |
| **Switch** | Not supported | Not supported | Not supported |
| **MFi (such as SteelSeries)** | Not supported (expected from Android 12) | Supported | Supported |

> [!NOTE]
> * Unity supports Made for iOS (MFi) certified controllers on iOS. Xbox One and PS4 controllers are only supported on iOS 13 or higher.
> * Unity supports PS4 controllers on Android devices running [Android 10 or higher](https://playstation.com/en-us/support/hardware/ps4-pair-dualshock-4-wireless-with-sony-xperia-and-android), and PS5 controllers on Android devices running [Android 12 or higher](https://playstation.com/en-gb/support/hardware/pair-dualsense-controller-bluetooth/).

### Console device support

For information on developing for PlayStation®4, PlayStation®5, Xbox One, Xbox Series S|X, and Nintendo Switch™, refer to the [Game Development For Console Platforms](https://unity.com/solutions/console) page. Consoles are supported using separate packages. You need to install these packages in your Project to enable console support.

| Gamepad | Xbox | PlayStation | Switch |
| :---- | :---- | :---- | :---- |
| **Xbox 360** | Supported | Not supported | Not supported |
| **Xbox One** | Supported | Not supported | Not supported |
| **PS3/PS4** | Not supported | Supported | Not supported |
| **PS5** | Not supported | Supported | Not supported |
| **Switch** | Not supported | Not supported | Supported |

### WebGL device support

The Input System supports the *Standard Gamepad* mapping as specified in the [W3C Gamepad Specification](https://www.w3.org/TR/gamepad/#remapping). It also supports gamepads and joysticks that the browser surfaces without a mapping, but this support is generally limited to detecting the axes and buttons which are present, without any context as to what they mean. This means gamepads and joysticks are generally only useful when [the user manually remaps them](xref:UnityEngine.InputSystem.InputActionRebindingExtensions). The Input System reports these Devices as generic [`Joysticks`](xref:UnityEngine.InputSystem.Joystick).

Support varies between browsers, Devices, and operating systems, and further differs for different browser versions, so it's not feasible to provide an up-to-date compatibility list. At the time of this publication (September 2019), Safari, Chrome, Edge, and Firefox all support the gamepad API, but only Chrome reliably maps common gamepads (Xbox and PlayStation controllers) to the W3C Standard Gamepad mapping, which allows the Input System to correctly identify and map controls.

> [!NOTE]
> WebGL currently doesn't support rumble.

## PS5 controller support on Linux

Some Linux distributions restrict access to HIDRAW devices by default. If your user account doesn't have permission to access the PS5 controller through HIDRAW, Unity will fall back to treating the controller as a standard gamepad.

> [!NOTE]
> When Unity falls back to treating the controller as a standard gamepad, advanced PS5 features, such as light bar, will not be available.

If your PS5 controller isn't detected through the HID subsystem, follow these steps to grant the necessary permissions:

1. Open the terminal and create the following `udev` rule file:
    ```
    sudo nano /etc/udev/rules.d/70-sony-controllers.rules
    ```

2. Add the following lines to the `.rules` file. Save the file, then exit the nano editor.
    ```
    # PS5 DualSense Edge
    KERNEL=="hidraw*", SUBSYSTEM=="hidraw", ATTRS{idVendor}=="054c", ATTRS{idProduct}=="0df2", GROUP="gamepad", MODE="0660"

    # PS5 DualSense
    KERNEL=="hidraw*", SUBSYSTEM=="hidraw", ATTRS{idVendor}=="054c", ATTRS{idProduct}=="0ce6", GROUP="gamepad", MODE="0660"
    ```

3. Grant the current user access to PS5 gamepads:
    ```
    sudo groupadd gamepad
    sudo usermod -aG gamepad $USER
    ```

4. Apply the new `udev` rules:
    ```
    sudo udevadm control --reload-rules
    sudo udevadm trigger
    ```

5. Log out from the current session and log back in. This step is required for the session to recognize the updated group membership.
