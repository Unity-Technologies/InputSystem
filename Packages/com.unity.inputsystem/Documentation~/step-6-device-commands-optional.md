---
uid: input-system-step-6-optional
---

# Step 6 Device Commands (Optional)

A final, but optional, step is to add support for Device commands. A "device command" is that opposite of input. In other words, it consists of data traveling __to__ the input device, which might also return data as part of the operation (much like a function call). You can use this to communicate with the backend of the device in order to query configuration, or to initiate effects such as haptics. At the moment there isn't a proper interface available for this, however there are still some scenarios that can be solved with the current interfaces.

The following examples shows, when implementing a non-hardware-backed device (simulated device), how to simulate hardware reporting that the device can be run in the background and supports sync commands. This is useful to prevent the device from cancelling Actions when application focus is lost and restored. For more information, refer to [Device syncs](sync-device.md)

```CSharp
public class MyDevice : InputDevice, IInputCallbackReceiver
{
    //...

    protected override unsafe long ExecuteCommand(InputDeviceCommand* commandPtr)
    {
        var type = commandPtr->type;
        if (type == RequestSyncCommand.Type)
        {
            // Report that the device supports the sync command and has handled it.
            // This will prevent device reset during focus changes.
            result = InputDeviceCommand.GenericSuccess;
            return true;
        }

        if (type == QueryCanRunInBackground.Type)
        {
            // Notify that the device supports running in the background.
            ((QueryCanRunInBackground*)commandPtr)->canRunInBackground = true;
            result = InputDeviceCommand.GenericSuccess;
            return true;
        }

        result = default;
        return false;
    }
}
```
