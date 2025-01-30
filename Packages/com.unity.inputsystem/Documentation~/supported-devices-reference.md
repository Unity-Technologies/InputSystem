---
uid: input-system-supported-devices
---
# Platform support reference

This section outlines the types of devices that each platform supports. The devices are divided into the following categories:

* [Generic devices](#generic-devices): pointers, keyboards, and joysticks which don't require specialized support of particular products.  
* [Gamepads](#gamepads): devices with two thumbsticks, a D-pad, four face buttons, two shoulder buttons, and two trigger buttons which might require specialized platform support.

## Generic devices

The following table outlines the generic devices that each platform supports.

|  | Mouse | Keyboard | Pen | Touchscreen | Sensors | Joystick (as a generic HID device) |
| :---- | :---- | :---- | :---- | :---- | :---- | :---- |
| **Windows** | Supported | Supported | Supported | Supported | Not supported | Supported |
| **macOS** | Supported | Supported | Supported | Not supported | Supported | Supported |
| **Linux** | Supported | Supported | Not supported | Not supported | Not supported | Supported |
| **UWP** | Supported | Supported | Supported | Supported | Not supported | Supported |
| **Android** | Supported | Supported | Supported | Supported | Supported | Supported |
| **iOS** | Not supported | Not supported | Supported | Supported | Supported | Not supported |
| **tvOS** | Not supported | Not supported | Not supported | Supported (indirect touches received from Siri touch) | Not supported | Not supported |
| **Xbox** | Supported | Supported | Not supported | Not supported | Not supported | Not supported |
| **PlayStation** | Supported | Supported | Not supported | Not supported | Not supported | Not supported |
| **Switch** | Not supported | Not supported | Not supported | Not supported | Not supported | Not supported |
| **WebGL** | Supported | Supported | Not supported | Supported | Supported | Supported |

### Other gamepads, joysticks, and racing wheels

The Input System supports any device which implements the [USB HID specification](hid-specification.md). However, for devices which don't have specific [layouts](Layouts.md) implemented in the Input System, the system can only surface the information available from the HID descriptor of the device, which limits how precisely it can describe a control. These devices often work best when you [manually remap the controls](xref:UnityEngine.InputSystem.InputActionRebindingExtensions). If you need to support a specific device, you can also [add your own mapping for it](hid-create-custom-layout.md). For more information, refer to the [Human Interface Device specification](hid-specification.md) documentation.

The double-tap interaction on the side of the Apple Pencil isn't surfaced as input at the moment. Also, no in-range detection is supported and [`inRange`](xref:UnityEngine.InputSystem.Pen.inRange) will remain at its default value.

## Gamepads 

The reference tables are organized by platform type:

* [Desktop](#desktop-device-support): Windows, macOS, Linux, Universal Windows Platform  
* [Mobile](#mobile-device-support): Android, iOS, tvOS  
* [Consoles](#console-device-support): Xbox, PlayStation, Switch  
* [WebGL](#webgl-device-support): Windows, macOS, Linux

### Desktop device support

The following table indicates whether Unity supports an input device:

* **Supported**: Works with Unity projects.  
* **Partially**: Only some functionality of the gamepad works with Unity projects.  
* **Not supported**: Device not supported by Unity.  
* **Not compatible**: Device not supported by the platform.

| Gamepad | Windows | macOS | Linux | Universal Windows Platform |
| :---- | :---- | :---- | :---- | :---- |
| **Xbox Controller (wired)** | Supported (no trigger motors) | Not supported | Supported (no trigger motors) | Supported |
| **Xbox Controller (wireless)** | Supported (no trigger motors) | Supported ([Driver required](https://github.com/360Controller/360Controller)) (no trigger motors) | Partially | Supported |
| **Xbox 360 Controller (wireless)** | Supported | Not compatible | Supported (no trigger motors) | Supported |
| **Xbox Controller Series 2 (wired)** | Supported (no trigger motors) | Not supported | Supported (no trigger motors) | Supported |
| **Xbox Controller Series 2 (wireless)** | Supported (no trigger motors) | Supported ([Driver required](https://github.com/360Controller/360Controller)) (no trigger motors) | Partially | Supported |
| **DualSense (wired)** | Supported | Supported | Supported | Not supported |
| **DualSense (wireless)** | Supported | Supported | Supported | Not supported |
| **DualShock 4 (wired)** | Supported | Supported | Supported | Not supported |
| **DualShock 4 (wireless)** | Supported (no motor rumble and lightbar color) | Supported (no motor rumble and lightbar color) | Not compatible | Not supported |
| **DualSense Edge (wired)** | Supported | Supported | Partially | Not supported |
| **DualSense Edge (wireless)** | Supported | Supported | Partially | Not supported |
| **Nintendo Switch Pro Controller (wired)** | Supported | Supported | Not supported | Not supported |
| **Nintendo Switch Pro Controller (wireless)** | Supported | Supported | Supported | Not supported |
| **Google Stadia Controller (wired)** | Supported | Supported | Supported | Not supported |
| **Google Stadia Controller (wireless)** | Supported | Supported | Supported | Not supported |
| **Nexus Gamepad (wireless)** | Partially | Partially | Partially | Not supported |
| **Nvidia Shield Controller (wired)** | Partially | Partially | Supported | Not supported |
| **Nvidia Shield Controller 2015 (wired)** | Not supported | Not supported | Not supported | Not supported |
| **Ouya Controller (wireless)** | Partially | Not supported | Partially | Not supported |
| **Steelseries Free (wireless)** | Partially | Partially  | Partially | Not supported |
| **Steelseries Straus (wireless)** | Not supported | Not supported | Not compatible | Not supported |
| **Trust Gaming Series GXT 555 Predator Joystick (wired)** | Supported | Supported | Partially | Not supported |
| **Apple Horipad (wireless)** | Partially | Partially  | Not supported | Not supported |

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

#### UWP

To ensure all controller types are detected on UWP, enable the HumanInterfaceDevice setting in [UWP Player Settings](https://docs.unity3d.com/Manual/class-PlayerSettingsWSA.html#Capabilities).

### Mobile device support

The following table indicates whether Unity supports an input device:

* **Supported**: Works with Unity projects.  
* **Partially**: Only some functionality of the gamepad works with Unity projects.  
* **Not supported**: Device not supported by Unity.  
* **Not compatible**: Device not supported by the platform.

| Gamepad | Android | iOS | tvOS |
| :---- | :---- | :---- | :---- |
| **Xbox Controller (wired)** | Supported (no trigger motors) | Supported (iOS 13+) (no trigger motors) | Not compatible |
| **Xbox Controller (wireless)** | Supported (no trigger motors) | Supported (iOS 13+) (no trigger motors) | Supported (no trigger motors) |
| **Xbox 360 Controller (wireless)** | Supported | Supported | Not compatible |
| **Xbox Controller Series 2 (wired)** | Supported (no trigger motors) | Supported (iOS 13+) (no trigger motors) | Not compatible |
| **Xbox Controller Series 2 (wireless)** | Supported (no trigger motors) | Supported (iOS 13+) (no trigger motors) | Supported (no trigger motors) |
| **DualSense (wired)** | Supported (Android 12+) | Supported | Not compatible |
| **DualSense (wireless)** | Supported (Android 12+) | Supported | Supported |
| **DualShock 4 (wired)** | Not supported | Supported (iOS 13+) | Not compatible |
| **DualShock 4 (wireless)** | Supported (Android 10+) (no motor rumble and lightbar color) | Supported (iOS 13+) (no motor rumble and lightbar color) | Supported (no motor rumble and lightbar color) |
| **DualSense Edge (wired)** | Supported (Android 12+) | Supported | Not compatible |
| **DualSense Edge (wireless)** | Supported (Android 12+) | Supported | Supported |
| **Nintendo Switch Pro Controller (wired)** | Supported | Supported | Not compatible |
| **Nintendo Switch Pro Controller (wireless)** | Supported | Supported | Supported |
| **Google Stadia Controller (wired)** | Supported | Supported | Not compatible |
| **Google Stadia Controller (wireless)** | Supported | Not supported | Not supported |
| **Nexus Gamepad (wireless)** | Not supported | Not compatible | Not compatible |
| **Nvidia Shield Controller (wired)** | Partially | Supported | Not compatible |
| **Nvidia Shield Controller 2015 (wired)** | Not supported | Not supported | Not compatible |
| **Ouya Controller (wireless)** | Partially | Not supported | Not supported |
| **Steelseries Free (wireless)** | Not supported | Not supported | Not compatible |
| **Steelseries Straus (wireless)** | Not compatible | Not supported | Not supported |
| **Trust Gaming Series GXT 555 Predator Joystick (wired)** | Not supported | Not supported | Not compatible |
| **Apple Horipad (wireless)** | Partially | Supported | Supported |

### Console device support

For information on developing for PlayStation®4, PlayStation®5, Xbox One, Xbox Series S|X, and Nintendo Switch™, refer to the [Game Development For Console Platforms](https://unity.com/solutions/console) page.

The following table indicates whether Unity supports an input device:

* **Supported**: Works with Unity projects.  
* **Partially**: Only some functionality of the gamepad works with Unity projects.  
* **Not supported**: Device not supported by Unity.  
* **Not compatible**: Device not supported by the platform.

| Gamepad | Xbox | PlayStation | Switch |
| :---- | :---- | :---- | :---- |
| **Xbox Controller (wired)** | Supported | Not compatible | Not compatible |
| **Xbox Controller (wireless)** | Supported | Not compatible | Not compatible |
| **Xbox 360 Controller (wireless)** | Not compatible | Not compatible | Not compatible |
| **Xbox Controller Series 2 (wired)** | Supported | Not compatible | Not compatible |
| **Xbox Controller Series 2 (wireless)** | Supported | Not compatible | Not compatible |
| **DualSense (wired)** | Not compatible | Supported | Not compatible |
| **DualSense (wireless)** | Not compatible | Supported | Not compatible |
| **DualShock 4 (wired)** | Not compatible | Not supported | Not compatible |
| **DualShock 4 (wireless)** | Not compatible | Not supported | Not compatible |
| **DualSense Edge (wired)** | Not compatible | Supported | Not compatible |
| **DualSense Edge (wireless)** | Not compatible | Supported | Not compatible |
| **Nintendo Switch Pro Controller (wired)** | Not compatible | Not compatible | Not supported |
| **Nintendo Switch Pro Controller (wireless)** | Not compatible | Not compatible | Not supported |

## WebGL device support

The Input System supports the Standard Gamepad mapping as specified in the [W3C Gamepad Specification](https://www.w3.org/TR/gamepad/#remapping). It also supports gamepads and joysticks that the browser surfaces without a mapping, but this support is limited to detecting the axes and buttons which are present, without any context as to what they mean. 

This means gamepads and joysticks are generally only useful if you [manually remap them](xref:UnityEngine.InputSystem.InputActionRebindingExtensions). The Input System reports these devices as generic [Joysticks](xref:UnityEngine.InputSystem.Joystick).

Support varies between browsers, devices, and operating systems, and further differs for different browser versions, so it's not feasible to provide an up-to-date compatibility list.The following table is a guideline for what gamepads work in WebGL builds.

* **Supported**: Works with Unity projects.  
* **Partially**: Only some functionality of the gamepad works with Unity projects.  
* **Not supported**: Device not supported by Unity.  
* **Not compatible**: Device not supported by the platform.

| Gamepad | Edge | Safari | Firefox (Linux) |
| :---- | :---- | :---- | :---- |
| **Xbox Controller (wired)** | Supported (no trigger motors) | Supported (no trigger motors) | Partially |
| **Xbox Controller (wireless)** | Supported (no trigger motors) | Supported (no trigger motors) | Partially |
| **Xbox 360 Controller (wireless)** | Supported | Not supported | Partially |
| **Xbox Controller Series 2 (wired)** | Supported (no trigger motors) | Supported (no trigger motors) | Partially |
| **Xbox Controller Series 2 (wireless)** | Supported (no trigger motors) | Supported (no trigger motors) | Partially |
| **DualSense (wired)** | Supported | Supported | Partially |
| **DualSense (wireless)** | Not supported | Supported (no motor rumble and lightbar color) | Partially |
| **DualShock 4 (wired)** | Supported | Supported | Partially |
| **DualShock 4 (wireless)** | Supported (no motor rumble and lightbar color) | Supported (no motor rumble and lightbar color) | Not compatible |
| **DualSense Edge (wired)** | Supported | Supported | Partially |
| **DualSense Edge (wireless)** | Supported | Supported | Partially |
| **Nintendo Switch Pro Controller (wired)** | Supported | Supported | Not supported |
| **Nintendo Switch Pro Controller (wireless)** | Supported | Supported | Partially |
| **Google Stadia Controller (wired)** | Supported | Supported | Partially |
| **Google Stadia Controller (wireless)** | Supported | Supported | Partially |
| **Nexus Gamepad (wireless)** | Supported | Not supported | Partially |
| **Nvidia Shield Controller (wired)** | Partially | Supported | Partially |
| **Nvidia Shield Controller 2015 (wired)** | Not supported | Not supported | Not supported |
| **Ouya Controller (wireless)** | Supported | Not supported | Partially |
| **Steelseries Free (wireless)** | Not supported | Not supported | Partially |
| **Steelseries Straus (wireless)** | Not supported | Not supported | Not compatible |
| **Trust Gaming Series GXT 555 Predator Joystick (wired)** | Supported | Not supported | Partially |
| **Apple Horipad (wireless)** | Not supported | Supported | Partially |
