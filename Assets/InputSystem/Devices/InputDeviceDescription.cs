using System;
using System.Text.RegularExpressions;

namespace ISX
{
    // Metadata for a device. Primarily used to find a matching template
    // which we can then use to create a control setup for the device.
    [Serializable]
    public struct InputDeviceDescription
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

        public bool empty => (string.IsNullOrEmpty(interfaceName) &&
                              string.IsNullOrEmpty(deviceClass) &&
                              string.IsNullOrEmpty(manufacturer) &&
                              string.IsNullOrEmpty(product) &&
                              string.IsNullOrEmpty(serial) &&
                              string.IsNullOrEmpty(version));

        public bool Matches(InputDeviceDescription other)
        {
            return MatchPair(interfaceName, other.interfaceName)
                && MatchPair(deviceClass, other.deviceClass)
                && MatchPair(manufacturer, other.manufacturer)
                && MatchPair(product, other.product)
                // We don't match serials; seems nonsense to do that.
                && MatchPair(version, other.version);
        }

        private static bool MatchPair(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
                return true;
            if (string.IsNullOrEmpty(right))
                return false;
            if (!Regex.IsMatch(right, left))
                return false;
            return true;
        }
    }
}
