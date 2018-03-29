using System;
using System.Text.RegularExpressions;

////REVIEW: should there be a way to make fields mandatory matches?

////REVIEW: add a 'devicePath' field for a platform-dependent device path?

namespace UnityEngine.Experimental.Input
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

        /// <summary>
        /// What the interface thinks the device classifies as.
        /// </summary>
        /// <remarks>
        /// If there is no template specifically matching a device description,
        /// the device class is used as as fallback. If, for example, this field
        /// is set to "Gamepad", the "Gamepad" template is used as a fallback.
        /// </remarks>
        public string deviceClass;

        /// <summary>
        /// Name of the vendor that produced the device.
        /// </summary>
        public string manufacturer;

        /// <summary>
        /// Name of the product assigned by the vendor to the device.
        /// </summary>
        public string product;

        /// <summary>
        /// If available, serial number for the device.
        /// </summary>
        public string serial;

        public string version;

        /// <summary>
        /// An optional JSON string listing device-specific capabilities.
        /// </summary>
        /// <remarks>
        /// The primary use of this field is to allow custom template factories
        /// to create templates on the fly from in-depth device descriptions delivered
        /// by external APIs.
        ///
        /// In the case of HID, for example, this field contains a JSON representation
        /// of the HID descriptor as reported by the device driver. This descriptor
        /// contains information about all I/O elements on the device which can be used
        /// to determine the control setup and data format used by the device.
        /// </remarks>
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
            var haveInterface = !string.IsNullOrEmpty(interfaceName);

            if (haveProduct && haveManufacturer)
            {
                if (haveInterface)
                    return string.Format("{0} {1} ({2})", manufacturer, product, interfaceName);

                return string.Format("{0} {1}", manufacturer, product);
            }

            if (haveProduct)
            {
                if (haveInterface)
                    return string.Format("{0} ({1})", product, interfaceName);

                return product;
            }

            if (!string.IsNullOrEmpty(deviceClass))
            {
                if (haveInterface)
                    return string.Format("{0} ({1})", deviceClass, interfaceName);

                return deviceClass;
            }

            // For some HIDs on Windows, we don't get a product and manufacturer string even though
            // the HID is guaranteed to have a product and vendor ID. Resort to printing capabilities
            // which for HIDs at least include the product and vendor ID.
            if (!string.IsNullOrEmpty(capabilities))
            {
                const int kMaxCapabilitiesLength = 40;

                var caps = capabilities;
                if (capabilities.Length > kMaxCapabilitiesLength)
                    caps = caps.Substring(0, kMaxCapabilitiesLength) + "...";

                if (haveInterface)
                    return string.Format("{0} ({1})", caps, interfaceName);

                return caps;
            }

            if (haveInterface)
                return interfaceName;

            return "<Empty Device Description>";
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
