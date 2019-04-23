# Devices

* [Device Descriptions](#device-descriptions)
    * [Hijacking the Matching Process](#hijacking-the-matching-process)
* [Native Devices](#native-devices)
    * [Disconnected Devices](#disconnected-devices)
* [Device Creation](#device-creation)
* [Device IDs](#device-ids)
* [Device Usages](#device-usages)
* [Device Commands](#device-commands)
* [Working with Devices](#working-with-devices)
    * [Monitoring Devices](#monitoring-devices)
    * [Adding and Removing Devices](#adding-and-removing-devices)
    * [Modifying Existing Devices](#modifying-existing-devices)
    * [Creating Custom Devices](#creating-custom-devices)

Devices are the toplevel container for [controls](Controls.md). A device is itself a control.

The set of all currently present devices can be queried through `InputSystem.devices`.

## Device Descriptions

An `InputDescription` describes a device. This is primarily used during the device discovery process. When a new device is reported present (by the runtime or by the user), it is reported along with a device description. Based on the description, the system will then attempt to find a device layout that matches the description. This process is based on ["device matchers"](#matching).

After a device has been created, the description it was created from can be retrieved through the `InputDevice.description` property.

Every description has a set of standard fields:

|Field|Description|
|-----|-----------|
|`interfaceName`|Identifier for the interface/API that is making the device available. In many cases, this corresponds to the name of the platform but there are several more specific interfaces that are commonly used:<br><dl><dt><a href="https://www.usb.org/hid">HID</a></dt><dd>bar</dd><dt><a href="https://docs.microsoft.com/en-us/windows/desktop/inputdev/raw-input">RawInput</a></dt><dd></dd><dt><a href="https://docs.microsoft.com/en-us/windows/desktop/xinput/xinput-game-controller-apis-portal">XInput</a></dt><dd></dd><dl>This field is required.|
|`deviceClass`|A broad categorization of the device. For example, "Gamepad" or "Keyboard".|
|`product`|Name of the product as reported by the device/driver itself.|
|`manufacturer`|Name of the manufacturer as reported by the device/driver itself.|
|`version`|If available, provides the version of the driver or hardware for the device.|
|`serial`|If available, provides the serial number for the device.|
|`capabilities`|A string in JSON format describing device/interface-specific capabilities. See the [section on capabililities](#capabilities).|

### Capabilities

Aside from a number of standardized fields, such as `product` and `manufacturer`, a device description may contain a `capabilities` string in JSON format. This string is used to describe characteristics ...

### Matching

Matching an `InputDeviceDescription` to a registered layout is facilitated by `InputDeviceMatcher`. Each matcher loosely functions as a kind of regular expression. Each field can be independently matched with either a plain string or regular expression. Matching is case-insensitive. For a matcher to apply, all its individual expressions have to match.

Matchers can be added to any layout by calling `InputSystem.RegisterLayoutMatcher` or supplied when registering a layout.

```CSharp
// Register a new layout and supply a matcher for it.
InputSystem.RegisterLayoutMatcher<MyDevice>(
    matches: new InputDeviceMatcher()
        .WithInterface("HID")
        .WithProduct("MyDevice.*")
        .WithManufacturer("MyBrand");

// Register an alternate matcher for an already registered layout.
InputSystem.RegisterLayoutMatcher<MyDevice>(
    new InputDeviceMatcher()
        .WithInterface("HID")

```

If multiple matchers are matching the same `InputDeviceDescription`, the matcher that has the most

### Hijacking the Matching Process

It is possible to overrule the internal matching process from outside and thus select a different layout for a device than the system would normally choose. This also makes it possible to build new layouts on the fly.

TODO

## Native Devices

Devices that are reported by the runtime are considered "native". These devices come in through the `IInputRuntime.onDeviceDiscovered` callback which is handled internally by the input system. Externally, devices created this way can be told apart from others by them having their `InputDevice.native` property be true.

There are some special rules that apply to native devices:

* A native device will be remembered by the input system. This means that, for example, if at the time the device was reported the system has no matching layout but TODO

### Disconnected Devices

## Device IDs

Each device that is created will receive a unique, numeric ID. The ID can be accessed through `InputDevice.id`.

The IDs are managed by the runtime and allocated through `IInputRuntime.AllocateDeviceId`. The runtime itself does not keep a record about which ID corresponds to which device.

During a session of Unity, no ID will get used a second time. This means that even after removal from the system, a device will not ...

## Device Creation

Input devices are exclusively created through `InputDeviceBuilder`. This process is mostly internal.

TODO

## Device Usages

Like any `InputControl`, a device may have one or more usages associated with it. TODO

## Device Configuration

## Device Commands

While input events deliver data coming __from__ a device, commands are used to communicate in the opposite direction, i.e. to talk back at the device. This can be used for retrieving specific information from the device, for triggering functions on the device (such as rumble effects), or for a variety of other needs.

A command is send to a device through `InputDevice.ExecuteCommand<TCommand>` which, for native devices, will relay the command to `IInputRuntime.DeviceCommand`. It is possible to intercept/monitor device commands through `InputSystem.onDeviceCommand`.

Each device command TODO

### Sending Commands to Devices

### Adding Custom Device Comands

## Working With Devices

### Monitoring Devices

In many situations, it is useful to know when new devices are added or when existing devices are removed. To be notified of such changes, use `InputSystem.onDeviceChange`.

```CSharp
InputSystem.onDeviceChange +=
    (device, change) =>
    {
        switch (change)
        {
            case InputDeviceChange.Added:
                Debug.Log("New device added: " + device);
                break;

            case InputDeviceChange.Removed:
                Debug.Log("Device removed: " + device);
                break;
        }
    };
```

Notifications through `InputSystem.onDeviceChange` are always delivered for a host of other device-related changes. See the API documentation for `InputDeviceChange` for more details (`////TODO: insert link here`).

### Adding and Removing Devices

Devices can be manually added and removed through the API.

### Modifying Existing Devices

-setting usage
-setting variant

### Creating Custom Devices
