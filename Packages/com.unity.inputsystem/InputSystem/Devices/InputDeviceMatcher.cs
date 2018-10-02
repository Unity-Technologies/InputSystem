using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Layouts
{
    /// <summary>
    /// Specification that can be matched against an <see cref="InputDeviceDescription"/>.
    /// </summary>
    /// <remarks>
    /// InputDeviceMatchers are used to match descriptions of input devices to
    /// specific control layouts.
    ///
    /// Each matcher is basically a set of key/value pairs where each value may either be
    /// a regular expression or a plain value object.
    /// </remarks>
    public struct InputDeviceMatcher : IEquatable<InputDeviceMatcher>
    {
        private KeyValuePair<InternedString, object>[] m_Patterns;

        public bool empty
        {
            get { return m_Patterns == null; }
        }

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

        public InputDeviceMatcher WithInterface(string pattern)
        {
            return With(kInterfaceKey, pattern);
        }

        public InputDeviceMatcher WithDeviceClass(string pattern)
        {
            return With(kDeviceClassKey, pattern);
        }

        public InputDeviceMatcher WithManufacturer(string pattern)
        {
            return With(kManufacturerKey, pattern);
        }

        public InputDeviceMatcher WithProduct(string pattern)
        {
            return With(kProductKey, pattern);
        }

        public InputDeviceMatcher WithVersion(string pattern)
        {
            return With(kVersionKey, pattern);
        }

        public InputDeviceMatcher WithCapability<TValue>(string path, TValue value)
        {
            return With(new InternedString(path), value);
        }

        public InputDeviceMatcher With(InternedString key, object value)
        {
            // If it's a string, check whether it's a regex.
            var str = value as string;
            if (str != null)
            {
                var mayBeRegex = !str.All(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch));
                if (mayBeRegex)
                    value = new Regex(str, RegexOptions.IgnoreCase);
            }

            // Add to list.
            var result = this;
            ArrayHelpers.Append(ref result.m_Patterns, new KeyValuePair<InternedString, object>(key, value));
            return result;
        }

        /// <summary>
        /// Return the level of matching to the given <paramref name="deviceDescription">device description</paramref>.
        /// </summary>
        /// <param name="deviceDescription"></param>
        /// <returns></returns>
        /// <remarks>
        /// The algorithm computes a score of how well the matcher matches the given description. For every property
        /// that is present on t
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

                    var graph = new JsonGraph(deviceDescription.capabilities);
                    if (!graph.NavigateToProperty(key.ToString()) ||
                        !graph.CurrentPropertyHasValueEqualTo(pattern))
                        return 0;
                }
            }

            // All patterns matched. Our score is determined by the number of properties
            // we matched against.
            var propertyCountInDescription = GetNumPropertiesIn(deviceDescription);
            var scorePerProperty = 1.0f / propertyCountInDescription;

            return numPatterns * scorePerProperty;
        }

        private bool MatchSingleProperty(object pattern, string value)
        {
            // String match.
            var str = pattern as string;
            if (str != null)
                return string.Compare(str, value, StringComparison.InvariantCultureIgnoreCase) == 0;

            // Regex match.
            var regex = pattern as Regex;
            if (regex != null)
                return regex.IsMatch(value);

            return false;
        }

        private int GetNumPropertiesIn(InputDeviceDescription description)
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

        public string ToJson()
        {
            var value = MatcherJson.FromMatcher(this);
            return JsonUtility.ToJson(value, true);
        }

        public static InputDeviceMatcher FromJson(string json)
        {
            var value = JsonUtility.FromJson<MatcherJson>(json);
            return value.ToMatcher();
        }

        public static InputDeviceMatcher FromDeviceDescription(InputDeviceDescription deviceDescription)
        {
            var matcher = new InputDeviceMatcher();
            if (!string.IsNullOrEmpty(deviceDescription.interfaceName))
                matcher = matcher.WithInterface(deviceDescription.interfaceName);
            if (!string.IsNullOrEmpty(deviceDescription.deviceClass))
                matcher = matcher.WithDeviceClass(deviceDescription.deviceClass);
            if (!string.IsNullOrEmpty(deviceDescription.manufacturer))
                matcher = matcher.WithManufacturer(deviceDescription.manufacturer);
            if (!string.IsNullOrEmpty(deviceDescription.product))
                matcher = matcher.WithProduct(deviceDescription.product);
            if (!string.IsNullOrEmpty(deviceDescription.version))
                matcher = matcher.WithVersion(deviceDescription.version);
            // We don't include capabilities in this conversion.
            return matcher;
        }

        public override string ToString()
        {
            if (empty)
                return "<empty>";

            var result = string.Empty;
            foreach (var pattern in m_Patterns)
            {
                if (result.Length > 0)
                    result += string.Format(",{0}={1}", pattern.Key, pattern.Value);
                else
                    result += string.Format("{0}={1}", pattern.Key, pattern.Value);
            }

            return result;
        }

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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is InputDeviceMatcher && Equals((InputDeviceMatcher)obj);
        }

        public static bool operator==(InputDeviceMatcher left, InputDeviceMatcher right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(InputDeviceMatcher left, InputDeviceMatcher right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return (m_Patterns != null ? m_Patterns.GetHashCode() : 0);
        }

        public static InternedString InterfaceKey
        {
            get { return kInterfaceKey; }
        }
        public static InternedString DeviceClassKey
        {
            get { return kDeviceClassKey; }
        }
        public static InternedString ManufacturerKey
        {
            get { return kManufacturerKey; }
        }
        public static InternedString ProductKey
        {
            get { return kProductKey; }
        }
        public static InternedString VersionKey
        {
            get { return kVersionKey; }
        }

        private static InternedString kInterfaceKey = new InternedString("interface");
        private static InternedString kDeviceClassKey = new InternedString("deviceClass");
        private static InternedString kManufacturerKey = new InternedString("manufacturer");
        private static InternedString kProductKey = new InternedString("product");
        private static InternedString kVersionKey = new InternedString("version");

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
