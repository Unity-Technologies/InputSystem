    ////WIP

# Devices

## Controls

### Noisy Controls

## Device Descriptions

An `InputDescription` describes a device. This is primarily used during the device discovery process. When a new device is reported present (by the runtime or by the user), it is reported along with a device description. Based on the description, the system will then attempt to find a device layout that matches the description.

After a device has been created, the description it was created from can be retrieved through the `InputDevice.description` property.

### Capabilities

Aside from a number of standardized fields, such as `product` and `manufacturer`, a device description may contain a `capabilities` string in JSON format. This string is used to describe characteristics ...

## 'Native' vs Non-Native Devices

## Device Matchers

## Device Configuration

## Device Commands

While input events deliver data coming __from__ a device, commands are used to communicate in the opposite direction, i.e. to talk back at the device. This can be used for retrieving information from the device, for triggering functions on the device (such as rumble effects), or for a variety of other needs.

## Working With Devices

### Monitoring Devices

In many situations, it is useful to know when new devices are added or when existing devices are removed. To be notified of such changes, use `InputSystem.onDeviceChange`.

```
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

### Modifying Existing Devices

### Creating Custom Devices

### Sending Commands to Devices
