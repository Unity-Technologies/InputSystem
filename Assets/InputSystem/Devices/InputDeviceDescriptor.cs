using System;

namespace ISX
{
    // Metadata for a device. Primarily used to find a matching template
    // which we can then use to create a control setup for the device.
    [Serializable]
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
    }
}
