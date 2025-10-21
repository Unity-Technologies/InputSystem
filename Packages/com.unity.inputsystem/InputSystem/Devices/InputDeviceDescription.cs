using System;
using UnityEngine.InputSystem.Utilities;

////TODO: add a 'devicePath' property that platforms can use to relay their internal device locators
////      (but do *not* take it into account when comparing descriptions for disconnected devices)

namespace UnityEngine.InputSystem.Layouts
{
    /// <summary>
    /// Metadata for an input device.
    /// </summary>
    /// <remarks>
    /// Device descriptions are mainly used to determine which <see cref="InputControlLayout"/>
    /// to create an actual <see cref="InputDevice"/> instance from. Each description is comprised
    /// of a set of properties that each are individually optional. However, for a description
    /// to be usable, at least some need to be set. Generally, the minimum viable description
    /// for a device is one with <see cref="deviceClass"/> filled out.
    ///
    /// <example>
    /// <code>
    /// // Device description equivalent to a generic gamepad with no
    /// // further information about the device.
    /// new InputDeviceDescription
    /// {
    ///     deviceClass = "Gamepad"
    /// };
    /// </code>
    /// </example>
    ///
    /// Device descriptions will usually be supplied by the Unity runtime but can also be manually
    /// fed into the system using <see cref="InputSystem.AddDevice(InputDeviceDescription)"/>. The
    /// system will remember each device description it has seen regardless of whether it was
    /// able to successfully create a device from the description. To query the list of descriptions
    /// that for whatever reason did not result in a device being created, call <see
    /// cref="InputSystem.GetUnsupportedDevices()"/>.
    ///
    /// Whenever layout registrations in the system are changed (e.g. by calling <see
    /// cref="InputSystem.RegisterLayout{T}"/> or whenever <see cref="InputSettings.supportedDevices"/>
    /// is changed, the system will go through the list of unsupported devices itself and figure out
    /// if there are device descriptions that now it can turn into devices. The same also applies
    /// in reverse; if, for example, a layout is removed that is currently used a device, the
    /// device will be removed and its description (if any) will be placed on the list of
    /// unsupported devices.
    /// </remarks>
    /// <seealso cref="InputDevice.description"/>
    /// <seealso cref="InputDeviceMatcher"/>
    [Serializable]
    public struct InputDeviceDescription : IEquatable<InputDeviceDescription>
    {
        /// <summary>
        /// How we talk to the device; usually name of the underlying backend that feeds
        /// state for the device (e.g. "HID" or "XInput").
        /// </summary>
        /// <value>Name of interface through which the device is reported.</value>
        /// <see cref="InputDeviceMatcher.WithInterface"/>
        public string interfaceName
        {
            get => m_InterfaceName;
            set => m_InterfaceName = value;
        }

        /// <summary>
        /// What the interface thinks the device classifies as.
        /// </summary>
        /// <value>Broad classification of device.</value>
        /// <remarks>
        /// If there is no layout specifically matching a device description,
        /// the device class is used as as fallback. If, for example, this field
        /// is set to "Gamepad", the "Gamepad" layout is used as a fallback.
        /// </remarks>
        /// <seealso cref="InputDeviceMatcher.WithDeviceClass"/>
        public string deviceClass
        {
            get => m_DeviceClass;
            set => m_DeviceClass = value;
        }

        /// <summary>
        /// Name of the vendor that produced the device.
        /// </summary>
        /// <value>Name of manufacturer.</value>
        /// <seealso cref="InputDeviceMatcher.WithManufacturer"/>
        public string manufacturer
        {
            get => m_Manufacturer;
            set => m_Manufacturer = value;
        }

        /// <summary>
        /// Name of the product assigned by the vendor to the device.
        /// </summary>
        /// <value>Name of product.</value>
        /// <seealso cref="InputDeviceMatcher.WithProduct"/>
        public string product
        {
            get => m_Product;
            set => m_Product = value;
        }

        /// <summary>
        /// If available, serial number for the device.
        /// </summary>
        /// <value>Serial number of device.</value>
        public string serial
        {
            get => m_Serial;
            set => m_Serial = value;
        }

        /// <summary>
        /// Version string of the device and/or driver.
        /// </summary>
        /// <value>Version of device and/or driver.</value>
        /// <seealso cref="InputDeviceMatcher.WithVersion"/>
        public string version
        {
            get => m_Version;
            set => m_Version = value;
        }

        /// <summary>
        /// An optional JSON string listing device-specific capabilities.
        /// </summary>
        /// <value>Interface-specific listing of device capabilities.</value>
        /// <remarks>
        /// The primary use of this field is to allow custom layout factories
        /// to create layouts on the fly from in-depth device descriptions delivered
        /// by external APIs.
        ///
        /// In the case of HID, for example, this field contains a JSON representation
        /// of the HID descriptor (see <see cref="HID.HID.HIDDeviceDescriptor"/>) as
        /// reported by the device driver. This descriptor contains information about
        /// all I/O elements on the device which can be used to determine the control
        /// setup and data format used by the device.
        /// </remarks>
        /// <seealso cref="InputDeviceMatcher.WithCapability{T}"/>
        public string capabilities
        {
            get => m_Capabilities;
            set => m_Capabilities = value;
        }

        /// <summary>
        /// Whether any of the properties in the description are set.
        /// </summary>
        /// <value>True if any of <see cref="interfaceName"/>, <see cref="deviceClass"/>,
        /// <see cref="manufacturer"/>, <see cref="product"/>, <see cref="serial"/>,
        /// <see cref="version"/>, or <see cref="capabilities"/> is not <c>null</c> and
        /// not empty.</value>
        public bool empty =>
            string.IsNullOrEmpty(m_InterfaceName) &&
            string.IsNullOrEmpty(m_DeviceClass) &&
            string.IsNullOrEmpty(m_Manufacturer) &&
            string.IsNullOrEmpty(m_Product) &&
            string.IsNullOrEmpty(m_Serial) &&
            string.IsNullOrEmpty(m_Version) &&
            string.IsNullOrEmpty(m_Capabilities);

        /// <summary>
        /// Return a string representation of the description useful for
        /// debugging.
        /// </summary>
        /// <returns>A script representation of the description.</returns>
        public override string ToString()
        {
            var haveProduct = !string.IsNullOrEmpty(product);
            var haveManufacturer = !string.IsNullOrEmpty(manufacturer);
            var haveInterface = !string.IsNullOrEmpty(interfaceName);

            if (haveProduct && haveManufacturer)
            {
                if (haveInterface)
                    return $"{manufacturer} {product} ({interfaceName})";

                return $"{manufacturer} {product}";
            }

            if (haveProduct)
            {
                if (haveInterface)
                    return $"{product} ({interfaceName})";

                return product;
            }

            if (!string.IsNullOrEmpty(deviceClass))
            {
                if (haveInterface)
                    return $"{deviceClass} ({interfaceName})";

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
                    return $"{caps} ({interfaceName})";

                return caps;
            }

            if (haveInterface)
                return interfaceName;

            return "<Empty Device Description>";
        }

        /// <summary>
        /// Compare the description to the given <paramref name="other"/> description.
        /// </summary>
        /// <param name="other">Another device description.</param>
        /// <returns>True if the two descriptions are equivalent.</returns>
        /// <remarks>
        /// Two descriptions are equivalent if all their properties are equal
        /// (ignore case).
        /// </remarks>
        public bool Equals(InputDeviceDescription other)
        {
            return m_InterfaceName.InvariantEqualsIgnoreCase(other.m_InterfaceName) &&
                m_DeviceClass.InvariantEqualsIgnoreCase(other.m_DeviceClass) &&
                m_Manufacturer.InvariantEqualsIgnoreCase(other.m_Manufacturer) &&
                m_Product.InvariantEqualsIgnoreCase(other.m_Product) &&
                m_Serial.InvariantEqualsIgnoreCase(other.m_Serial) &&
                m_Version.InvariantEqualsIgnoreCase(other.m_Version) &&
                ////REVIEW: this would ideally compare JSON contents not just the raw string
                m_Capabilities.InvariantEqualsIgnoreCase(other.m_Capabilities);
        }

        /// <summary>
        /// Compare the description to the given object.
        /// </summary>
        /// <param name="obj">An object.</param>
        /// <returns>True if <paramref name="obj"/> is an InputDeviceDescription
        /// equivalent to this one.</returns>
        /// <seealso cref="Equals(InputDeviceDescription)"/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is InputDeviceDescription description && Equals(description);
        }

        /// <summary>
        /// Compute a hash code for the device description.
        /// </summary>
        /// <returns>A hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_InterfaceName != null ? m_InterfaceName.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (m_DeviceClass != null ? m_DeviceClass.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Manufacturer != null ? m_Manufacturer.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Product != null ? m_Product.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Serial != null ? m_Serial.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Version != null ? m_Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Capabilities != null ? m_Capabilities.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Compare the two device descriptions.
        /// </summary>
        /// <param name="left">First device description.</param>
        /// <param name="right">Second device description.</param>
        /// <returns>True if the two descriptions are equivalent.</returns>
        /// <seealso cref="Equals(InputDeviceDescription)"/>
        public static bool operator==(InputDeviceDescription left, InputDeviceDescription right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare the two device descriptions for inequality.
        /// </summary>
        /// <param name="left">First device description.</param>
        /// <param name="right">Second device description.</param>
        /// <returns>True if the two descriptions are not equivalent.</returns>
        /// <seealso cref="Equals(InputDeviceDescription)"/>
        public static bool operator!=(InputDeviceDescription left, InputDeviceDescription right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Return a JSON representation of the device description.
        /// </summary>
        /// <returns>A JSON representation of the description.</returns>
        /// <remarks>
        /// <example>
        /// The result can be converted back into an InputDeviceDescription
        /// using <see cref="FromJson"/>.
        ///
        /// <code>
        /// var description = new InputDeviceDescription
        /// {
        ///     interfaceName = "HID",
        ///     product = "SomeDevice",
        ///     capabilities = @"
        ///         {
        ///             ""vendorId"" : 0xABA,
        ///             ""productId"" : 0xEFE
        ///         }
        ///     "
        /// };
        ///
        /// Debug.Log(description.ToJson());
        /// // Prints
        /// // {
        /// //     "interface" : "HID",
        /// //     "product" : "SomeDevice",
        /// //     "capabilities" : "{ \"vendorId\" : 0xABA, \"productId\" : 0xEFF }"
        /// // }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="FromJson"/>
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

        /// <summary>
        /// Read an InputDeviceDescription from its JSON representation.
        /// </summary>
        /// <param name="json">String in JSON format.</param>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
        /// <returns>The converted </returns>
        /// <exception cref="ArgumentException">There as a parse error in <paramref name="json"/>.
        /// </exception>
        /// <remarks>
        /// <example>
        /// <code>
        /// InputDeviceDescription.FromJson(@"
        ///     {
        ///         ""interface"" : ""HID"",
        ///         ""product"" : ""SomeDevice""
        ///     }
        /// ");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="ToJson"/>
        public static InputDeviceDescription FromJson(string json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

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

        internal static bool ComparePropertyToDeviceDescriptor(string propertyName, JsonParser.JsonString propertyValue, string deviceDescriptor)
        {
            // We use JsonParser instead of JsonUtility.Parse in order to not allocate GC memory here.

            var json = new JsonParser(deviceDescriptor);
            if (!json.NavigateToProperty(propertyName))
            {
                if (propertyValue.text.isEmpty)
                    return true;
                return false;
            }

            return json.CurrentPropertyHasValueEqualTo(propertyValue);
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
