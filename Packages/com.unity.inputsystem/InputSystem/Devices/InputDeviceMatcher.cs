using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Experimental.Input.Utilities;

//for serialization during domain reload, turn into JSON
//(or even always store them as such?)

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Specification that can be matched against an <see cref="InputDeviceDescription"/>.
    /// </summary>
    /// <remarks>
    /// InputDeviceMatchers are used to match descriptions of input devices to
    /// specific control layouts.
    ///
    /// Each matcher is basically a set of key/value pairs.
    /// </remarks>
    public struct InputDeviceMatcher
    {
        private KeyValuePair<InternedString, object>[] m_Patterns;

        public bool empty
        {
            get { return m_Patterns == null; }
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
                    value = new Regex(str);
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

            var propertyCountInDescription = GetNumPropertiesIn(deviceDescription);
            var scorePerProperty = 1.0f / propertyCountInDescription;
            var score = 0f;

            var numPatterns = m_Patterns.Length;
            for (var i = 0; i < numPatterns; ++i)
            {
                var key = m_Patterns[i].Key;
                var pattern = m_Patterns[i].Value;

                var isMatch = false;
                if (key == kInterfaceKey)
                {
                    if (string.IsNullOrEmpty(deviceDescription.interfaceName))
                        return 0;

                    isMatch = MatchSingleProperty(pattern, deviceDescription.interfaceName);
                }
                else if (key == kDeviceClassKey)
                {
                    if (string.IsNullOrEmpty(deviceDescription.deviceClass))
                        return 0;

                    isMatch = MatchSingleProperty(pattern, deviceDescription.deviceClass);
                }
                else if (key == kManufacturerKey)
                {
                    if (string.IsNullOrEmpty(deviceDescription.manufacturer))
                        return 0;

                    isMatch = MatchSingleProperty(pattern, deviceDescription.manufacturer);
                }
                else if (key == kProductKey)
                {
                    if (string.IsNullOrEmpty(deviceDescription.product))
                        return 0;

                    isMatch = MatchSingleProperty(pattern, deviceDescription.product);
                }
                else if (key == kVersionKey)
                {
                    if (string.IsNullOrEmpty(deviceDescription.version))
                        return 0;

                    isMatch = MatchSingleProperty(pattern, deviceDescription.version);
                }
                else
                {
                    // Capabilities match. Take the key as a path into the JSON
                    // object and match the value found at the given path.

                    if (string.IsNullOrEmpty(deviceDescription.capabilities))
                        return 0;

                    var graph = new JsonGraph(deviceDescription.capabilities);
                    isMatch = graph.NavigateToProperty(key.ToString()) &&
                        graph.CurrentPropertyHasValueEqualTo(pattern);
                }

                if (isMatch)
                    score += scorePerProperty;
            }

            return score;
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
            throw new NotImplementedException();
        }

        public static InputDeviceMatcher FromJson(string json)
        {
            throw new NotImplementedException();
        }

        ////TODO: ToString

        public static InternedString InterfaceKey
        {
            get { return kInterfaceKey; }
        }

        public static InternedString DeviceClassKey
        {
            get { return kDeviceClassKey; }
        }

        private static InternedString kInterfaceKey = new InternedString("interface");
        private static InternedString kDeviceClassKey = new InternedString("deviceClass");
        private static InternedString kManufacturerKey = new InternedString("manufacturer");
        private static InternedString kProductKey = new InternedString("product");
        private static InternedString kVersionKey = new InternedString("version");
    }
}
