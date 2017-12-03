using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ISX
{
    /// <summary>
    /// Metadata for a device. Primarily used to find a matching template
    /// which we can then use to create a control setup for the device.
    /// </summary>
    [Serializable]
    public struct InputDeviceDescription
    {
        /// <summary>
        /// How we talk to the device; usually name of the underlying backend that feeds
        /// state for the device.
        /// </summary>
        /// <example>Examples: "HID", "XInput"</example>
        public string interfaceName;

        // What the interface thinks the device classifies as.
        public string deviceClass;
        // Who made the thing.
        public string manufacturer;
        // What they call it.
        public string product;
        public string serial;
        public string version;
        // An optional JSON string listing device-specific capabilities.
        // Example: For HIDs, this will be the HID descriptor.
        public string capabilities;

        public bool empty
        {
            get
            {
                return string.IsNullOrEmpty(interfaceName) &&
                    string.IsNullOrEmpty(deviceClass) &&
                    string.IsNullOrEmpty(manufacturer) &&
                    string.IsNullOrEmpty(product) &&
                    string.IsNullOrEmpty(serial) &&
                    string.IsNullOrEmpty(version) &&
                    string.IsNullOrEmpty(capabilities);
            }
        }

        public override string ToString()
        {
            var haveProduct = !string.IsNullOrEmpty(product);
            var haveManufacturer = !string.IsNullOrEmpty(manufacturer);

            if (haveProduct && haveManufacturer)
                return string.Format("{0} {1}", manufacturer, product);
            if (haveProduct)
                return product;

            if (!string.IsNullOrEmpty(deviceClass))
                return deviceClass;

            return string.Empty;
        }

        public bool Matches(InputDeviceDescription other)
        {
            return MatchPair(interfaceName, other.interfaceName)
                && MatchPair(deviceClass, other.deviceClass)
                && MatchPair(manufacturer, other.manufacturer)
                && MatchPair(product, other.product)
                // We don't match serials; seems nonsense to do that.
                && MatchPair(version, other.version)
                && MatchPair(capabilities, other.capabilities);
        }

        private static bool MatchPair(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
                return true;
            if (string.IsNullOrEmpty(right))
                return false;
            if (!Regex.IsMatch(right, left, RegexOptions.IgnoreCase))
                return false;
            return true;
        }

        public string ToJson()
        {
            var data = new DeviceDescriptionJson
            {
                @interface = interfaceName,
                type = deviceClass,
                product = product,
                manufacturer = manufacturer,
                serial = serial,
                version = version,
                capabilities = capabilities
            };
            return JsonUtility.ToJson(data);
        }

        public static InputDeviceDescription FromJson(string json)
        {
            var data = JsonUtility.FromJson<DeviceDescriptionJson>(json);

            return new InputDeviceDescription
            {
                interfaceName = data.@interface,
                deviceClass = data.type,
                product = data.product,
                manufacturer = data.manufacturer,
                serial = data.serial,
                version = data.version,
                capabilities = data.capabilities
            };
        }

        ////FIXME: this format differs from the one used in templates; we should only have one format
        private struct DeviceDescriptionJson
        {
            public string @interface;
            public string type;
            public string product;
            public string serial;
            public string version;
            public string manufacturer;
            public string capabilities;
        }
    }
}
