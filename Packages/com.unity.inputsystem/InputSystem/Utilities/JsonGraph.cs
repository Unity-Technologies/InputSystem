using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityEngine.Experimental.Input.Utilities
{
    /// <summary>
    /// A JSON parser that instead of turning a string in JSON format into a
    /// C# object graph, allows navigating the source text directly.
    /// </summary>
    internal struct JsonGraph
    {
        public JsonGraph(string json)
            : this()
        {
            if (json == null)
                throw new ArgumentNullException("json");

            m_Text = json;
            m_Length = json.Length;
        }

        /// <summary>
        /// Navigate to the given property.
        /// </summary>
        /// <param name="path"></param>
        /// <remarks>
        /// This navigates from the current property.
        /// </remarks>
        public bool NavigateToProperty(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            var pathLength = path.Length;
            var pathPosition = 0;

            m_DryRun = true;
            if (!ParseObjectStart())
                return false;

            while (m_Position < m_Length && pathPosition < pathLength)
            {
                // Find start of property name.
                SkipWhitespace();
                if (m_Position == m_Length)
                    return false;
                if (m_Text[m_Position] != '"')
                    return false;
                ++m_Position;

                // Try to match single path component.
                var pathStartPosition = pathPosition;
                while (pathPosition < pathLength)
                {
                    var ch = path[pathPosition];
                    if (ch == '/' || ch == '[')
                        break;

                    if (m_Text[m_Position] != ch)
                        break;

                    ++m_Position;
                    ++pathPosition;
                }

                // See if we have a match.
                if (m_Position < m_Length && m_Text[m_Position] == '"')
                {
                    // Have matched a property name. Navigate to value.
                    ++m_Position;
                    if (!SkipToValue())
                        return false;

                    // Check if we have matched everything in the path.
                    if (pathPosition == pathLength)
                        return true;
                    if (path[pathPosition] == '/')
                    {
                        ++pathPosition;
                        if (!ParseObjectStart())
                            return false;
                    }
                    else if (path[pathPosition] == '[')
                    {
                        ++pathPosition;
                        if (pathPosition == pathLength)
                            throw new ArgumentException("Malformed JSON property path: " + path, "path");
                        if (path[pathPosition] == ']')
                        {
                            m_MatchAnyElementInArray = true;
                            ++pathPosition;
                            if (pathPosition == pathLength)
                                return true;
                        }
                        else
                            throw new NotImplementedException("Navigating to specific array element");
                    }
                }
                else
                {
                    // This property isn't it. Skip the property and its value and reset
                    // to where we started in this iteration in the property path.

                    pathPosition = pathStartPosition;
                    while (m_Position < m_Length && m_Text[m_Position] != '"')
                        ++m_Position;
                    if (m_Position == m_Length || m_Text[m_Position] != '"')
                        return false;
                    ++m_Position;
                    if (!SkipToValue() || !ParseValue())
                        return false;
                    SkipWhitespace();
                    if (m_Position == m_Length || m_Text[m_Position] == '}' || m_Text[m_Position] != ',')
                        return false;
                    ++m_Position;
                }
            }

            return false;
        }

        /// <summary>
        /// Return true if the current property has a value matching <paramref name="expectedValue"/>.
        /// </summary>
        /// <param name="expectedValue"></param>
        /// <returns></returns>
        public bool CurrentPropertyHasValueEqualTo(object expectedValue)
        {
            if (expectedValue == null)
                throw new ArgumentNullException("expectedValue");

            ////TODO: prevent boxing on numbers and bools

            // Grab property value.
            var savedPosition = m_Position;
            m_DryRun = false;
            object propertyValue;
            if (!ParseValue(out propertyValue))
            {
                m_Position = savedPosition;
                return false;
            }
            m_Position = savedPosition;

            // Match given value.
            var isMatch = false;
            var array = propertyValue as List<object>;
            if (array != null && m_MatchAnyElementInArray)
            {
                for (var i = 0; !isMatch && i < array.Count; ++i)
                    isMatch = MatchValue(array[i], expectedValue);
            }
            else
            {
                isMatch = MatchValue(propertyValue, expectedValue);
            }

            return isMatch;
        }

        private bool MatchValue(object propertyValue, object expectedValue)
        {
            var regex = expectedValue as Regex;
            if (regex != null)
            {
                return regex.IsMatch(propertyValue.ToString());
            }

            var str = expectedValue as string;
            if (str != null)
            {
                // Special case float comparison to take precision into account.
                if (propertyValue is float)
                {
                    float f;
                    if (float.TryParse(str, out f))
                        return Mathf.Approximately(f, (float)propertyValue);
                    return false;
                }

                // This path should work for ints and bools, too.
                return string.Compare(str, propertyValue.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0;
            }

            var enumValue = expectedValue as Enum;
            if (enumValue != null)
            {
                if (propertyValue is int)
                    return (int)propertyValue == Convert.ToInt32(enumValue);
                if (propertyValue is string)
                    return string.Compare(str, (string)propertyValue,
                        StringComparison.InvariantCultureIgnoreCase) == 0;
            }
            else
            {
                if (expectedValue is float && propertyValue is float)
                {
                    var expectedNum = Convert.ToSingle(expectedValue);
                    var actualNum = Convert.ToSingle(propertyValue);

                    return Mathf.Approximately(expectedNum, actualNum);
                }

                return propertyValue.Equals(expectedValue);
            }

            return false;
        }

        private bool ParseObjectStart()
        {
            SkipWhitespace();
            if (m_Position == m_Length)
                return false;

            if (m_Text[m_Position] != '{')
                return false;

            ++m_Position;
            SkipWhitespace();

            return (m_Position < m_Length);
        }

        private bool ParseValue()
        {
            object result;
            return ParseValue(out result);
        }

        private bool ParseValue(out object result)
        {
            result = null;

            SkipWhitespace();
            if (m_Position == m_Length)
                return false;

            var ch = m_Text[m_Position];
            switch (ch)
            {
                case '"':
                    string stringResult;
                    if (ParseStringValue(out stringResult))
                    {
                        result = stringResult;
                        return true;
                    }
                    break;
                case '[':
                    List<object> arrayResult;
                    if (ParseArrayValue(out arrayResult))
                    {
                        result = arrayResult;
                        return true;
                    }
                    break;
                case '{':
                    if (ParseObjectValue(out result))
                        return true;
                    break;
                case 't':
                case 'f':
                    bool boolValue;
                    if (ParseBooleanValue(out boolValue))
                    {
                        result = boolValue;
                        return true;
                    }
                    break;
                case 'n':
                    if (ParseNullValue(out result))
                        return true;
                    break;
                default:
                    if (ParseNumber(out result))
                        return true;
                    break;
            }

            return false;
        }

        private bool ParseStringValue(out string result)
        {
            result = null;
            if (!m_DryRun)
            {
                if (m_StringBuilder == null)
                    m_StringBuilder = new StringBuilder();
                m_StringBuilder.Length = 0;
            }

            SkipWhitespace();
            if (m_Position == m_Length || m_Text[m_Position] != '"')
                return false;
            ++m_Position;

            while (m_Position < m_Length)
            {
                var ch = m_Text[m_Position];
                if (ch == '\\')
                    throw new NotImplementedException("Escape sequences");
                if (ch == '"')
                {
                    ++m_Position;
                    if (!m_DryRun)
                        result = m_StringBuilder.ToString();
                    return true;
                }
                ++m_Position;

                if (!m_DryRun)
                    m_StringBuilder.Append(ch);
            }

            return false;
        }

        private bool ParseArrayValue(out List<object> result)
        {
            result = null;

            SkipWhitespace();
            if (m_Position == m_Length || m_Text[m_Position] != '[')
                return false;
            ++m_Position;

            if (m_Position == m_Length)
                return false;
            if (m_Text[m_Position] == ']')
            {
                // Empty array.
                ++m_Position;
                return true;
            }

            List<object> values = null;
            if (!m_DryRun)
                values = new List<object>();

            while (m_Position < m_Length)
            {
                object value;
                if (!ParseValue(out value))
                    return false;
                if (!m_DryRun)
                    values.Add(value);
                SkipWhitespace();
                if (m_Position == m_Length)
                    return false;
                var ch = m_Text[m_Position];
                if (ch == ']')
                {
                    ++m_Position;
                    if (!m_DryRun)
                        result = values;
                    return true;
                }
                if (ch == ',')
                    ++m_Position;
            }

            return false;
        }

        private bool ParseObjectValue(out object result)
        {
            result = null;

            if (!ParseObjectStart())
                return false;
            if (m_Position < m_Length && m_Text[m_Position] == '}')
            {
                ++m_Position;
                return true;
            }

            while (m_Position < m_Length)
            {
                string propertyName;
                if (!ParseStringValue(out propertyName))
                    return false;

                if (!SkipToValue())
                    return false;

                object propertyValue;
                if (!ParseValue(out propertyValue))
                    return false;

                if (!m_DryRun)
                    throw new NotImplementedException();

                SkipWhitespace();
                if (m_Position < m_Length && m_Text[m_Position] == '}')
                {
                    if (!m_DryRun)
                        throw new NotImplementedException();
                    ++m_Position;
                    return true;
                }
            }

            return false;
        }

        private bool ParseNumber(out object result)
        {
            result = null;

            SkipWhitespace();
            if (m_Position == m_Length)
                return false;

            var negative = false;
            var haveFractionalPart = false;
            var integralPart = 0;
            var fractionalPart = 0.0;
            var fractionalDivisor = 10.0;

            // Parse sign.
            if (m_Text[m_Position] == '-')
            {
                negative = true;
                ++m_Position;
            }

            if (m_Position == m_Length || !char.IsDigit(m_Text[m_Position]))
                return false;

            // Parse integral part.
            while (m_Position < m_Length)
            {
                var ch = m_Text[m_Position];
                if (ch == '.')
                    break;
                if (ch < '0' || ch > '9')
                    break;
                integralPart = integralPart * 10 + ch - '0';
                ++m_Position;
            }

            // Parse fractional part.
            if (m_Position < m_Length && m_Text[m_Position] == '.')
            {
                haveFractionalPart = true;
                ++m_Position;
                if (m_Position == m_Length || !char.IsDigit(m_Text[m_Position]))
                    return false;
                while (m_Position < m_Length)
                {
                    var ch = m_Text[m_Position];
                    if (ch < '0' || ch > '9')
                        break;
                    fractionalPart = (ch - '0') / fractionalDivisor + fractionalPart;
                    fractionalDivisor *= 10;
                    ++m_Position;
                }
            }

            if (m_Position < m_Length && (m_Text[m_Position] == 'e' || m_Text[m_Position] == 'E'))
                throw new NotImplementedException("exponents");

            if (!m_DryRun)
            {
                if (!haveFractionalPart)
                {
                    if (negative)
                        result = -integralPart;
                    else
                        result = integralPart;
                }
                else
                {
                    if (negative)
                        result = (float)-(integralPart + fractionalPart);
                    else
                        result = (float)(integralPart + fractionalPart);
                }
            }

            return true;
        }

        private bool ParseBooleanValue(out bool result)
        {
            result = false;

            SkipWhitespace();
            if (SkipString("true"))
            {
                result = true;
                return true;
            }

            if (SkipString("false"))
            {
                result = false;
                return true;
            }

            return false;
        }

        private bool ParseNullValue(out object result)
        {
            result = null;
            return SkipString("null");
        }

        private bool SkipToValue()
        {
            SkipWhitespace();
            if (m_Position == m_Length || m_Text[m_Position] != ':')
                return false;
            ++m_Position;
            SkipWhitespace();
            return true;
        }

        private bool SkipString(string text)
        {
            SkipWhitespace();
            var length = text.Length;
            if (m_Position + length >= m_Length)
                return false;
            for (var i = 0; i < length; ++i)
            {
                if (m_Text[m_Position + i] != text[i])
                    return false;
            }

            m_Position += length;
            return true;
        }

        private void SkipWhitespace()
        {
            while (m_Position < m_Length && char.IsWhiteSpace(m_Text[m_Position]))
                ++m_Position;
        }

        private string m_Text;
        private int m_Position;
        private int m_Length;
        private bool m_CurrentPropertyIsArray;
        private bool m_MatchAnyElementInArray;
        private bool m_DryRun;
        private StringBuilder m_StringBuilder;
    }
}
