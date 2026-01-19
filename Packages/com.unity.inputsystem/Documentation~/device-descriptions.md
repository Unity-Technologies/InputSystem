---
uid: input-system-device-descriptions
---

# Device descriptions 

An [`InputDeviceDescription`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html) describes a Device. The Input System uses this primarily during the Device discovery process. When a new Device is reported (by the runtime or by the user), the report contains a Device description. Based on the description, the system then attempts to find a Device [layout](layouts.md) that matches the description. This process is based on [Device matchers](#matching).

After a Device has been created, you can retrieve the description it was created from through the [`InputDevice.description`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_description) property.

Every description has a set of standard fields:

|Field|Description|
|-----|-----------|
|[`interfaceName`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_interfaceName)|Identifier for the interface/API that is making the Device available. In many cases, this corresponds to the name of the platform, but there are several more specific interfaces that are commonly used: [HID](https://www.usb.org/hid), [RawInput](https://docs.microsoft.com/en-us/windows/desktop/inputdev/raw-input), [XInput](https://docs.microsoft.com/en-us/windows/desktop/xinput/xinput-game-controller-apis-portal).<br>This field is required.|
|[`deviceClass`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_deviceClass)|A broad categorization of the Device. For example, "Gamepad" or "Keyboard".|
|[`product`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_product)|Name of the product as reported by the Device/driver itself.|
|[`manufacturer`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_manufacturer)|Name of the manufacturer as reported by the Device/driver itself.|
|[`version`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_version)|If available, provides the version of the driver or hardware for the Device.|
|[`serial`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_serial)|If available, provides the serial number for the Device.|
|[`capabilities`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_capabilities)|A string in JSON format that describes Device/interface-specific capabilities. See the [section on capabilities](#capabilities).|
