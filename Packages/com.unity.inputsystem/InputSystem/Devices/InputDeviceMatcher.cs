using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Layouts
{
    /// <summary>
    /// Specification that can be matched against an <see cref="InputDeviceDescription"/>. This is
    /// used to find which <see cref="InputControlLayout"/> to create for a device when it is discovered.
    /// </summary>
    /// <remarks>
    /// Each matcher is basically a set of key/value pairs where each value may either be
    /// a regular expression or a plain value object. The central method for testing a given matcher
    /// against an <see cref="InputDeviceDescription"/> is <see cref="MatchPercentage"/>.
    ///
    /// Various helper methods such as <see cref="WithInterface"/> or <see cref="WithCapability{TValue}"/>
    /// assist with creating matchers.
    ///
    /// <example>
    /// <code>
    /// // A matcher that matches a PS4 controller by name.
    /// new InputDeviceMatcher()
    ///     .WithInterface("HID")
    ///     .WithManufacturer("Sony.+Entertainment") // Regular expression
    ///     .WithProduct("Wireless Controller"));
    ///
    /// // A matcher that matches the same controller by PID and VID.
    /// new InputDeviceMatcher()
    ///     .WithInterface("HID")
    ///     .WithCapability("vendorId", 0x54C) // Sony Entertainment.
    ///     .WithCapability("productId", 0x9CC)); // Wireless controller.
    /// </code>
    /// </example>
    ///
    /// For each registered <see cref="InputControlLayout"/> in the system that represents
    /// a device, arbitrary many matchers can be added. A matcher can be supplied either
    /// at registration time or at any point after using <see cref="InputSystem.RegisterLayoutMatcher"/>.
    ///
    /// <example>
    /// <code>
    /// // Supply a matcher at registration time.
    /// InputSystem.RegisterLayout&lt;DualShock4GamepadHID&gt;(
    ///     matches: new InputDeviceMatcher()
    ///         .WithInterface("HID")
    ///         .WithCapability("vendorId", 0x54C) // Sony Entertainment.
    ///         .WithCapability("productId", 0x9CC)); // Wireless controller.
    ///
    /// // Supply a matcher for an already registered layout.
    /// // This can be called repeatedly and will add another matcher
    /// // each time.
    /// InputSystem.RegisterLayoutMatcher&lt;DualShock4GamepadHID&gt;(
    ///     matches: new InputDeviceMatcher()
    ///         .WithInterface("HID")
    ///         .WithManufacturer("Sony.+Entertainment")
    ///         .WithProduct("Wireless Controller"));
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="InputDeviceDescription"/>
    /// <seealso cref="InputDevice.description"/>
    /// <seealso cref="InputSystem.RegisterLayoutMatcher"/>
    public struct InputDeviceMatcher : IEquatable<InputDeviceMatcher>
    {
        private KeyValuePair<InternedString, object>[] m_Patterns;

        /// <summary>
        /// If true, the matcher has been default-initialized and contains no
        /// matching <see cref="patterns"/>.
        /// </summary>
        /// <value>Whether the matcher contains any matching patterns.</value>
        /// <seealso cref="patterns"/>
        public bool empty => m_Patterns == null;

        /// <summary>
        /// The list of patterns to match.
        /// </summary>
        /// <value>List of matching patterns.</value>
        /// <remarks>
        /// Each pattern is comprised of a key and a value. The key determines which part
        /// of an <see cref="InputDeviceDescription"/> to match.
        ///
        /// The value represents the expected value. This can be either a plain string
        /// (matched case-insensitive) or a regular expression.
        /// </remarks>
        /// <seealso cref="WithInterface"/>
        /// <seealso cref="WithCapability{TValue}"/>
        /// <seealso cref="WithProduct"/>
        /// <seealso cref="WithManufacturer"/>
        /// <seealso cref="WithVersion"/>
        /// <seealso cref="WithDeviceClass"/>
        public IEnumerable<KeyValuePair<string, object>> patterns
        {
            get
            {
                if (m_Patterns == null)
                    yield break;

                var count = m_Patterns.Length;
                for (var i = 0; i < count; ++i)
                    yield return new KeyValuePair<string, object>(m_Patterns[i].Key.ToString(), m_Patterns[i].Value);
            }
        }

        /// <summary>
        /// Add a pattern to <see cref="patterns"/> to match an <see cref="InputDeviceDescription.interfaceName"/>.
        /// </summary>
        /// <param name="pattern">String to match.</param>
        /// <param name="supportRegex">If true (default), <paramref name="pattern"/> can be
        /// a regular expression.</param>
        /// <returns>The modified device matcher with the added pattern.</returns>
        /// <seealso cref="InputDeviceDescription.interfaceName"/>
        public InputDeviceMatcher WithInterface(string pattern, bool supportRegex = true)
        {
            return With(kInterfaceKey, pattern, supportRegex);
        }

        /// <summary>
        /// Add a pattern to <see cref="patterns"/> to match a <see cref="InputDeviceDescription.deviceClass"/>.
        /// </summary>
        /// <param name="pattern">String to match.</param>
        /// <param name="supportRegex">If true (default), <paramref name="pattern"/> can be
        /// a regular expression.</param>
        /// <returns>The modified device matcher with the added pattern.</returns>
        /// <seealso cref="InputDeviceDescription.deviceClass"/>
        public InputDeviceMatcher WithDeviceClass(string pattern, bool supportRegex = true)
        {
            return With(kDeviceClassKey, pattern, supportRegex);
        }

        /// <summary>
        /// Add a pattern to <see cref="patterns"/> to match a <see cref="InputDeviceDescription.manufacturer"/>.
        /// </summary>
        /// <param name="pattern">String to match.</param>
        /// <param name="supportRegex">If true (default), <paramref name="pattern"/> can be
        /// a regular expression.</param>
        /// <returns>The modified device matcher with the added pattern.</returns>
        /// <seealso cref="InputDeviceDescription.manufacturer"/>
        public InputDeviceMatcher WithManufacturer(string pattern, bool supportRegex = true)
        {
            return With(kManufacturerKey, pattern, supportRegex);
        }

        /// <summary>
        /// Add a pattern to <see cref="patterns"/> to match a <see cref="InputDeviceDescription.product"/>.
        /// </summary>
        /// <param name="pattern">String to match.</param>
        /// <param name="supportRegex">If true (default), <paramref name="pattern"/> can be
        /// a regular expression.</param>
        /// <returns>The modified device matcher with the added pattern.</returns>
        /// <seealso cref="InputDeviceDescription.product"/>
        public InputDeviceMatcher WithProduct(string pattern, bool supportRegex = true)
        {
            return With(kProductKey, pattern, supportRegex);
        }

        /// <summary>
        /// Add a pattern to <see cref="patterns"/> to match a <see cref="InputDeviceDescription.version"/>.
        /// </summary>
        /// <param name="pattern">String to match.</param>
        /// <param name="supportRegex">If true (default), <paramref name="pattern"/> can be
        /// a regular expression.</param>
        /// <returns>The modified device matcher with the added pattern.</returns>
        /// <seealso cref="InputDeviceDescription.version"/>
        public InputDeviceMatcher WithVersion(string pattern, bool supportRegex = true)
        {
            return With(kVersionKey, pattern, supportRegex);
        }

        /// <summary>
        /// Add a pattern to <see cref="patterns"/> to match an individual capability in <see cref="InputDeviceDescription.capabilities"/>.
        /// </summary>
        /// <param name="path">Path to the JSON property using '/' as a separator,
        /// e.g. <c>"elements/count"</c>.</param>
        /// <param name="value">Value to match. This can be a string, a regular expression,
        /// a boolean, an integer, or a float. Floating-point numbers are matched with respect
        /// for <c>Mathf.Epsilon</c>. Values are converted between types automatically as
        /// needed (meaning that a bool can be compared to a string, for example).</param>
        /// <typeparam name="TValue">Type of value to match.</typeparam>
        /// <returns>The modified device matcher with the added pattern.</returns>
        /// <remarks>
        /// Capabilities are stored as JSON strings in <see cref="InputDeviceDescription.capabilities"/>.
        /// A matcher has the ability to match specific properties from the JSON object
        /// contained in the capabilities string.
        ///
        /// <example>
        /// <code>
        /// // The description for a HID will usually have a HIDDeviceDescriptor in
        /// // JSON format found on its InputDeviceDescription.capabilities. So, a
        /// // real-world device description could look the equivalent of this:
        /// var description = new InputDeviceDescription
        /// {
        ///     interfaceName = "HID",
        ///     capabilities = new HID.HIDDeviceDescriptor
        ///     {
        ///         vendorId = 0x54C,
        ///         productId = 0x9CC
        ///     }.ToJson()
        /// };
        ///
        /// // We can create a device matcher that looks for those to properties
        /// // directly in the JSON object.
        /// new InputDeviceMatcher()
        ///     .WithCapability("vendorId", 0x54C)
        ///     .WithCapability("productId", 0x9CC);
        /// </code>
        /// </example>
        ///
        /// Properties in nested objects can be referenced by separating properties
        /// with <c>/</c> and properties in arrays can be indexed with <c>[..]</c>.
        /// </remarks>
        /// <seealso cref="InputDeviceDescription.capabilities"/>
        public InputDeviceMatcher WithCapability<TValue>(string path, TValue value)
        {
            return With(new InternedString(path), value);
        }

        private InputDeviceMatcher With(InternedString key, object value, bool supportRegex = true)
        {
            // If it's a string, check whether it's a regex.
            if (supportRegex && value is string str)
            {
                var mayBeRegex = !str.All(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch)) &&
                    !double.TryParse(str, out var _);              // Avoid '.' in floats forcing the value to be a regex.
                if (mayBeRegex)
                    value = new Regex(str, RegexOptions.IgnoreCase);
            }

            // Add to list.
            var result = this;
            ArrayHelpers.Append(ref result.m_Patterns, new KeyValuePair<InternedString, object>(key, value));
            return result;
        }

        /// <summary>
        /// Return the level of matching to the given <paramref name="deviceDescription"/>.
        /// </summary>
        /// <param name="deviceDescription">A device description.</param>
        /// <returns>A score usually in the range between 0 and 1.</returns>
        /// <remarks>
        /// The algorithm computes a score of how well the matcher matches the given description.
        /// Essentially, a matcher that matches every single property that is present (as in
        /// not <c>null</c> and not an empty string) in <paramref name="deviceDescription"/> receives
        /// a score of 1, a matcher that matches none a score of 0. Matches that match only a subset
        /// receive a score in-between.
        ///
        /// An exception to this are capabilities. Every single match of a capability is counted
        /// as one property match and added to the score. This means that matchers that match
        /// on multiple capabilities may actually achieve a score &gt;1.
        ///
        /// <example>
        /// <code>
        /// var description = new InputDeviceDescription
        /// {
        ///     interfaceName = "HID",
        ///     product = "MadeUpDevice",
        ///     capabilities = new HID.HIDDeviceDescriptor
        ///     {
        ///         vendorId = 0xABC,
        ///         productId = 0xDEF
        ///     }.ToJson()
        /// };
        ///
        /// // This matcher will achieve a score of 0.666 (2/3) as it
        /// // matches two out of three available properties.
        /// new InputDeviceMatcher()
        ///     .WithInterface("HID")
        ///     .WithProduct("MadeUpDevice");
        ///
        /// // This matcher will achieve a score of 1 despite not matching
        /// // 'product'. The reason is that it matches two keys in
        /// // 'capabilities'.
        /// new InputDeviceMatcher()
        ///     .WithInterface("HID")
        ///     .WithCapability("vendorId", 0xABC)
        ///     .WithCapability("productId", 0xDEF);
        /// </code>
        /// </example>
        /// </remarks>
        public float MatchPercentage(InputDeviceDescription deviceDescription)
        {
            if (empty)
                return 0;

            // Go through all patterns. Score is 0 if any of the patterns
            // doesn't match.
            var numPatterns = m_Patterns.Length;
            for (var i = 0; i < numPatterns; ++i)
            {
                var key = m_Patterns[i].Key;
                var pattern = m_Patterns[i].Value;

                if (key == kInterfaceKey)
                {
                    if (string.IsNullOrEmpty(deviceDescription.interfaceName)
                        || !MatchSingleProperty(pattern, deviceDescription.interfaceName))
                        return 0;
                }
                else if (key == kDeviceClassKey)
                {
                    if (string.IsNullOrEmpty(deviceDescription.deviceClass)
                        || !MatchSingleProperty(pattern, deviceDescription.deviceClass))
                        return 0;
                }
                else if (key == kManufacturerKey)
                {
                    if (string.IsNullOrEmpty(deviceDescription.manufacturer)
                        || !MatchSingleProperty(pattern, deviceDescription.manufacturer))
                        return 0;
                }
                else if (key == kProductKey)
                {
                    if (string.IsNullOrEmpty(deviceDescription.product)
                        || !MatchSingleProperty(pattern, deviceDescription.product))
                        return 0;
                }
                else if (key == kVersionKey)
                {
                    if (string.IsNullOrEmpty(deviceDescription.version)
                        || !MatchSingleProperty(pattern, deviceDescription.version))
                        return 0;
                }
                else
                {
                    // Capabilities match. Take the key as a path into the JSON
                    // object and match the value found at the given path.

                    if (string.IsNullOrEmpty(deviceDescription.capabilities))
                        return 0;

                    var graph = new JsonParser(deviceDescription.capabilities);
                    if (!graph.NavigateToProperty(key.ToString()) ||
                        !graph.CurrentPropertyHasValueEqualTo(new JsonParser.JsonValue { type = JsonParser.JsonValueType.Any, anyValue = pattern}))
                        return 0;
                }
            }

            // All patterns matched. Our score is determined by the number of properties
            // we matched against.
            var propertyCountInDescription = GetNumPropertiesIn(deviceDescription);
            var scorePerProperty = 1.0f / propertyCountInDescription;

            return numPatterns * scorePerProperty;
        }

        private static bool MatchSingleProperty(object pattern, string value)
        {
            // String match.
            if (pattern is string str)
                return string.Compare(str, value, StringComparison.InvariantCultureIgnoreCase) == 0;

            // Regex match.
            if (pattern is Regex regex)
                return regex.IsMatch(value);

            return false;
        }

        private static int GetNumPropertiesIn(InputDeviceDescription description)
        {
            var count = 0;
            if (!string.IsNullOrEmpty(description.interfaceName))
                count += 1;
            if (!string.IsNullOrEmpty(description.deviceClass))
                count += 1;
            if (!string.IsNullOrEmpty(description.manufacturer))
                count += 1;
            if (!string.IsNullOrEmpty(description.product))
                count += 1;
            if (!string.IsNullOrEmpty(description.version))
                count += 1;
            if (!string.IsNullOrEmpty(description.capabilities))
                count += 1;
            return count;
        }

        /// <summary>
        /// Produce a matcher that matches the given device description verbatim.
        /// </summary>
        /// <param name="deviceDescription">A device description.</param>
        /// <returns>A matcher that matches <paramref name="deviceDescription"/> exactly.</returns>
        /// <remarks>
        /// This method can be used to produce a matcher for an existing device description,
        /// e.g. when writing a layout <see cref="InputControlLayout.Builder"/> that produces
        /// layouts for devices on the fly.
        /// </remarks>
        public static InputDeviceMatcher FromDeviceDescription(InputDeviceDescription deviceDescription)
        {
            var matcher = new InputDeviceMatcher();
            if (!string.IsNullOrEmpty(deviceDescription.interfaceName))
                matcher = matcher.WithInterface(deviceDescription.interfaceName, false);
            if (!string.IsNullOrEmpty(deviceDescription.deviceClass))
                matcher = matcher.WithDeviceClass(deviceDescription.deviceClass, false);
            if (!string.IsNullOrEmpty(deviceDescription.manufacturer))
                matcher = matcher.WithManufacturer(deviceDescription.manufacturer, false);
            if (!string.IsNullOrEmpty(deviceDescription.product))
                matcher = matcher.WithProduct(deviceDescription.product, false);
            if (!string.IsNullOrEmpty(deviceDescription.version))
                matcher = matcher.WithVersion(deviceDescription.version, false);
            // We don't include capabilities in this conversion.
            return matcher;
        }

        /// <summary>
        /// Return a string representation useful for debugging. Lists the
        /// <see cref="patterns"/> contained in the matcher.
        /// </summary>
        /// <returns>A string representation of the matcher.</returns>
        public override string ToString()
        {
            if (empty)
                return "<empty>";

            var result = string.Empty;
            foreach (var pattern in m_Patterns)
            {
                if (result.Length > 0)
                    result += $",{pattern.Key}={pattern.Value}";
                else
                    result += $"{pattern.Key}={pattern.Value}";
            }

            return result;
        }

        /// <summary>
        /// Test whether this matcher is equivalent to the <paramref name="other"/> matcher.
        /// </summary>
        /// <param name="other">Another device matcher.</param>
        /// <returns>True if the two matchers are equivalent.</returns>
        /// <remarks>
        /// Two matchers are equivalent if they contain the same number of patterns and the
        /// same pattern occurs in each of the matchers. Order of the patterns does not
        /// matter.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "False positive.")]
        public bool Equals(InputDeviceMatcher other)
        {
            if (m_Patterns == other.m_Patterns)
                return true;

            if (m_Patterns == null || other.m_Patterns == null)
                return false;

            if (m_Patterns.Length != other.m_Patterns.Length)
                return false;

            // Pattern count matches. Compare pattern by pattern. Order of patterns doesn't matter.
            for (var i = 0; i < m_Patterns.Length; ++i)
            {
                var thisPattern = m_Patterns[i];
                var foundPattern = false;
                for (var n = 0; n < m_Patterns.Length; ++n)
                {
                    var otherPattern = other.m_Patterns[n];
                    if (thisPattern.Key != otherPattern.Key)
                        continue;
                    if (!thisPattern.Value.Equals(otherPattern.Value))
                        return false;
                    foundPattern = true;
                    break;
                }

                if (!foundPattern)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compare this matcher to another.
        /// </summary>
        /// <param name="obj">A matcher object or <c>null</c>.</param>
        /// <returns>True if the matcher is equivalent.</returns>
        /// <seealso cref="Equals(InputDeviceMatcher)"/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is InputDeviceMatcher matcher && Equals(matcher);
        }

        /// <summary>
        /// Compare two matchers for equivalence.
        /// </summary>
        /// <param name="left">First device matcher.</param>
        /// <param name="right">Second device matcher.</param>
        /// <returns>True if the two matchers are equivalent.</returns>
        /// <seealso cref="Equals(InputDeviceMatcher)"/>
        public static bool operator==(InputDeviceMatcher left, InputDeviceMatcher right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare two matchers for non-equivalence.
        /// </summary>
        /// <param name="left">First device matcher.</param>
        /// <param name="right">Second device matcher.</param>
        /// <returns>True if the two matchers are not equivalent.</returns>
        /// <seealso cref="Equals(InputDeviceMatcher)"/>
        public static bool operator!=(InputDeviceMatcher left, InputDeviceMatcher right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Compute a hash code for the device matcher.
        /// </summary>
        /// <returns>A hash code for the matcher.</returns>
        public override int GetHashCode()
        {
            return m_Patterns != null ? m_Patterns.GetHashCode() : 0;
        }

        private static readonly InternedString kInterfaceKey = new InternedString("interface");
        private static readonly InternedString kDeviceClassKey = new InternedString("deviceClass");
        private static readonly InternedString kManufacturerKey = new InternedString("manufacturer");
        private static readonly InternedString kProductKey = new InternedString("product");
        private static readonly InternedString kVersionKey = new InternedString("version");

        [Serializable]
        internal struct MatcherJson
        {
            public string @interface;
            public string[] interfaces;
            public string deviceClass;
            public string[] deviceClasses;
            public string manufacturer;
            public string[] manufacturers;
            public string product;
            public string[] products;
            public string version;
            public string[] versions;
            public Capability[] capabilities;

            public struct Capability
            {
                public string path;
                public string value;
            }

            public static MatcherJson FromMatcher(InputDeviceMatcher matcher)
            {
                if (matcher.empty)
                    return new MatcherJson();

                var json = new MatcherJson();
                foreach (var pattern in matcher.m_Patterns)
                {
                    var key = pattern.Key;
                    var value = pattern.Value.ToString();

                    if (key == kInterfaceKey)
                    {
                        if (json.@interface == null)
                            json.@interface = value;
                        else
                            ArrayHelpers.Append(ref json.interfaces, value);
                    }
                    else if (key == kDeviceClassKey)
                    {
                        if (json.deviceClass == null)
                            json.deviceClass = value;
                        else
                            ArrayHelpers.Append(ref json.deviceClasses, value);
                    }
                    else if (key == kManufacturerKey)
                    {
                        if (json.manufacturer == null)
                            json.manufacturer = value;
                        else
                            ArrayHelpers.Append(ref json.manufacturers, value);
                    }
                    else if (key == kProductKey)
                    {
                        if (json.product == null)
                            json.product = value;
                        else
                            ArrayHelpers.Append(ref json.products, value);
                    }
                    else if (key == kVersionKey)
                    {
                        if (json.version == null)
                            json.version = value;
                        else
                            ArrayHelpers.Append(ref json.versions, value);
                    }
                    else
                    {
                        ArrayHelpers.Append(ref json.capabilities, new Capability {path = key, value = value});
                    }
                }

                return json;
            }

            public InputDeviceMatcher ToMatcher()
            {
                var matcher = new InputDeviceMatcher();

                ////TODO: get rid of the piecemeal array allocation and do it in one step

                // Interfaces.
                if (!string.IsNullOrEmpty(@interface))
                    matcher = matcher.WithInterface(@interface);
                if (interfaces != null)
                    foreach (var value in interfaces)
                        matcher = matcher.WithInterface(value);

                // Device classes.
                if (!string.IsNullOrEmpty(deviceClass))
                    matcher = matcher.WithDeviceClass(deviceClass);
                if (deviceClasses != null)
                    foreach (var value in deviceClasses)
                        matcher = matcher.WithDeviceClass(value);

                // Manufacturer.
                if (!string.IsNullOrEmpty(manufacturer))
                    matcher = matcher.WithManufacturer(manufacturer);
                if (manufacturers != null)
                    foreach (var value in manufacturers)
                        matcher = matcher.WithManufacturer(value);

                // Product.
                if (!string.IsNullOrEmpty(product))
                    matcher = matcher.WithProduct(product);
                if (products != null)
                    foreach (var value in products)
                        matcher = matcher.WithProduct(value);

                // Version.
                if (!string.IsNullOrEmpty(version))
                    matcher = matcher.WithVersion(version);
                if (versions != null)
                    foreach (var value in versions)
                        matcher = matcher.WithVersion(value);

                // Capabilities.
                if (capabilities != null)
                    foreach (var value in capabilities)
                        ////FIXME: we're turning all values into strings here
                        matcher = matcher.WithCapability(value.path, value.value);

                return matcher;
            }
        }
    }
}
