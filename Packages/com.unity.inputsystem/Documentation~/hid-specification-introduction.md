---
uid: input-system-hid-intro
---

# Human Interface Device specification introduction

Human Interface Device (HID) is a [specification](https://www.usb.org/hid) to describe peripheral user input devices connected to computers with USB or Bluetooth. HID is commonly used to implement devices such as gamepads, joysticks, or racing wheels.

The Input System directly supports HID (connected with both USB and Bluetooth) on Windows, macOS, and the Universal Windows Platform (UWP). The system might support HID on other platforms, but not deliver input through HID-specific APIs. For example, on Linux, the system supports gamepad and joystick HIDs through SDL, but doesn't support other HIDs.

The Input System handles HIDs in one of the following ways:

* Uses a set layout if the system has a known layout for the specific HID.
* If the system doesn't have a known layout, Unity auto-generates one for the HID.

## Auto-generated layouts

By default, the Input System creates layouts and device representations for any HID which reports its usage as GenericDesktop/Joystick, GenericDesktop/Gamepad, or GenericDesktop/MultiAxisController (see the [HID usage table specifications](https://www.usb.org/document-library/hid-usage-tables-112) for more information). To change the list of supported usages, set [HIDSupport.supportedHIDUsages](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.HID.HIDSupport.html#UnityEngine_InputSystem_HID_HIDSupport_supportedHIDUsages).

When the Input System automatically creates a layout for an HID, it always reports these Devices as [Joystick](http://localhost:57437/com.unity.inputsystem@1.12/manual/Joystick.html) instances, represented by the [Joystick device class](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Joystick.html). The first elements with a reported HID usage of GenericDesktop/X and GenericDesktop/Y together form the joystick's [stick](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Joystick.html#UnityEngine_InputSystem_Joystick_stick) control. The system then adds Controls for all further HID axis or button elements, using the Control names reported by the HID specification. The Input System assigns the first control with an HID usage of Button/Button 1 to the joystick's [trigger](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Joystick.html#UnityEngine_InputSystem_Joystick_trigger) Control.

The auto-generated layouts represent a best effort on the part of the Input System. The way HIDs describe themselves in accordance with the HID standard varies, so generated layouts might lead to controls that don't work as expected. For example, while the layout builder can identify hat switches and D-pads, it can often only make guesses as to which direction represents which. The same goes for individual buttons, which generally aren't assigned any meaning in HID.

To resolve the situation of HIDs not working as expected, you can add a custom layout, which bypasses auto-generation altogether. Refer to [Creating a custom device layout](http://localhost:57437/com.unity.inputsystem@1.12/manual/HID.html#creating-a-custom-device-layout) for details.

## HID in the Unity Editor

Every HID comes with a device descriptor. To browse through the descriptor of an HID from the Input Debugger, select the HID Descriptor button in the device debugger window.

To specify the type of the device, the HID descriptor reports entry numbers in the [HID usage tables](https://www.usb.org/document-library/hid-usage-tables-112), and a list of all controls on the device, along with their data ranges and usages.

## HID output

HIDs can support output (for example, to toggle lights or force feedback motors on a gamepad). Unity controls output by sending HID Output Report commands to a device. Output reports use Device-specific data formats.

To use HID Output Reports, call [InputDevice.ExecuteCommand](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_ExecuteCommand__1___0__) to send a command struct with the [typeStatic](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html#properties) property set as "HIDO" to a device. The command struct contains the device-specific data sent out to the HID.
