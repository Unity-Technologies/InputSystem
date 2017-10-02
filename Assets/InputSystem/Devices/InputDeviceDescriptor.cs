using System;

namespace ISX
{
    // Metadata for a device. Primarily used to find a matching template
    // which we can then use to create a control setup for the device.
    public struct InputDeviceDescriptor
    {
        // How we talk to the device; usually name of the underlying backend that feeds
        // state for the device.
        public string interfaceName;
        // What the interface thinks the device classifies as.
        public string deviceClass;
        // Who made the thing.
        public string manufacturer;
        // What they call it.
        public string product;
        public string serial;
        public string version;
        
        ////REVIEW: this doesn't seem very useful anymore in the new system; maybe have a predefined format for these?
        // A potentially large JSON string fully that may contain a full info dump including
        // detailed capababilities of every control as well as of the device itself.
        public string fullDescriptor;

        public static InputDeviceDescriptor Parse(string json)
        {
            throw new NotImplementedException();
        }
    }
}