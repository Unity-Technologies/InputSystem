using System;

////TODO: add a 'devicePath' property that platforms can use to relay their internal device locators
////      (but do *not* take it into account when comparing descriptions for disconnected devices)

namespace UnityEngine.Experimental.Input.Layouts
{
    /// <summary>
    /// Metadata for an input device.
    /// </summary>
    /// <remarks>
    /// Device descriptions are used to determine which layout to create an actual <see cref="InputDevice"/>
    /// instance from that matches the device.
    /// </remarks>
    [Serializable]
    public struct InputDeviceDescription : IEquatable<InputDeviceDescription>
    {
        /// <summary>
        /// How we talk to the device; usually name of the underlying backend that feeds
        /// state for the device.
        /// </summary>
        /// <example>Examples: "HID", "XInput"</example>
        public string interfaceName
        {
            get { return m_InterfaceName; }
            set { m_InterfaceName = value; }
        }

        /// <summary>
        /// What the interface thinks the device classifies as.
        /// </summary>
        /// <remarks>
        /// If there is no layout specifically matching a device description,
        /// the device class is used as as fallback. If, for example, this field
        /// is set to "Gamepad", the "Gamepad" layout is used as a fallback.
        /// </remarks>
        public string deviceClass
        {
            get { return m_DeviceClass; }
            set { m_DeviceClass = value; }
        }

        /// <summary>
        /// Name of the vendor that produced the device.
        /// </summary>
        public string manufacturer
        {
            get { return m_Manufacturer; }
            set { m_Manufacturer = value; }
        }

        /// <summary>
        /// Name of the product assigned by the vendor to the device.
        /// </summary>
        public string product
        {
            get { return m_Product; }
            set { m_Product = value; }
        }

        /// <summary>
        /// If available, serial number for the device.
        /// </summary>
        public string serial
        {
            get { return m_Serial; }
            set { m_Serial = value; }
        }

        public string version
        {
            get { return m_Version; }
            set { m_Version = value; }
        }

        /// <summary>
        /// An optional JSON string listing device-specific capabilities.
        /// </summary>
        /// <remarks>
        /// The primary use of this field is to allow custom layout factories
        /// to create layouts on the fly from in-depth device descriptions delivered
        /// by external APIs.
        ///
        /// In the case of HID, for example, this field contains a JSON representation
        /// of the HID descriptor as reported by the device driver. This descriptor
        /// contains information about all I/O elements on the device which can be used
        /// to determine the control setup and data format used by the device.
        /// </remarks>
        public string capabilities
        {
            get { return m_Capabilities; }
            set { m_Capabilities = value; }
        }

        public bool empty
        {
            get
            {
                return string.IsNullOrEmpty(m_InterfaceName) &&
                    string.IsNullOrEmpty(m_DeviceClass) &&
                    string.IsNullOrEmpty(m_Manufacturer) &&
                    string.IsNullOrEmpty(m_Product) &&
                    string.IsNullOrEmpty(m_Serial) &&
                    string.IsNullOrEmpty(m_Version) &&
                    string.IsNullOrEmpty(m_Capabilities);
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

        public bool Equals(InputDeviceDescription other)
        {
            return string.Equals(m_InterfaceName, other.m_InterfaceName) &&
                string.Equals(m_DeviceClass, other.m_DeviceClass) &&
                string.Equals(m_Manufacturer, other.m_Manufacturer) &&
                string.Equals(m_Product, other.m_Product) &&
                string.Equals(m_Serial, other.m_Serial) &&
                string.Equals(m_Version, other.m_Version) &&
                string.Equals(m_Capabilities, other.m_Capabilities);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is InputDeviceDescription && Equals((InputDeviceDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (m_InterfaceName != null ? m_InterfaceName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_DeviceClass != null ? m_DeviceClass.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Manufacturer != null ? m_Manufacturer.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Product != null ? m_Product.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Serial != null ? m_Serial.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Version != null ? m_Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Capabilities != null ? m_Capabilities.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator==(InputDeviceDescription left, InputDeviceDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(InputDeviceDescription left, InputDeviceDescription right)
        {
            return !left.Equals(right);
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
            return JsonUtility.ToJson(data, true);
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

        [SerializeField] private string m_InterfaceName;
        [SerializeField] private string m_DeviceClass;
        [SerializeField] private string m_Manufacturer;
        [SerializeField] private string m_Product;
        [SerializeField] private string m_Serial;
        [SerializeField] private string m_Version;
        [SerializeField] private string m_Capabilities;

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
