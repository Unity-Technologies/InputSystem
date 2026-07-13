---
uid: input-system-device-descriptions
---

# Device descriptions

The Input System uses the device description defined as a [`InputDeviceDescription`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription) primarily during the Device discovery process. When a new Device is reported (by the runtime or by the user), the system then attempts to find a Device [layout](layouts.md) that matches the Device description contained in the report. This process is based on [Device matching](device-matching.md).

After a Device has been created, you can retrieve the description it was created from through the [`InputDevice.description`](xref:UnityEngine.InputSystem.InputDevice.description) property.

Every description has a set of standard fields:

|Field|Description|
|-----|-----------|
|[`interfaceName`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription)|Identifier for the interface/API that is making the Device available. In many cases, this corresponds to the name of the platform, but there are several more specific interfaces that are commonly used: [HID](https://www.usb.org/hid), [RawInput](https://docs.microsoft.com/en-us/windows/desktop/inputdev/raw-input), [XInput](https://docs.microsoft.com/en-us/windows/desktop/xinput/xinput-game-controller-apis-portal).<br>This field is required.|
|[`deviceClass`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription)|A broad categorization of the Device. For example, "Gamepad" or "Keyboard".|
|[`product`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription)|Name of the product as reported by the Device/driver itself.|
|[`manufacturer`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription)|Name of the manufacturer as reported by the Device/driver itself.|
|[`version`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription)|If available, provides the version of the driver or hardware for the Device.|
|[`serial`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription)|If available, provides the serial number for the Device.|
|[`capabilities`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription)|A string in JSON format that describes Device/interface-specific capabilities. See the [section on capabilities](device-capabilities.md).|
