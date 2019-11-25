using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// A JSON parser that instead of turning a string in JSON format into a
    /// C# object graph, allows navigating the source text directly.
    /// </summary>
    /// <remarks>
    /// This helper is most useful for avoiding a great many string and general object allocations
    /// that would happen when turning a JSON object into a C# object graph.
    /// </remarks>
    internal struct JsonParser
    {
        public JsonParser(string json)
            : this()
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            m_Text = json;
            m_Length = json.Length;
        }

        public void Reset()
        {
            m_Position = 0;
            m_MatchAnyElementInArray = false;
            m_DryRun = false;
        }

        public override string ToString()
        {
            if (m_Text != null)
                return $"{m_Position}: {m_Text.Substring(m_Position)}";
            return base.ToString();
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
                throw new ArgumentNullException(nameof(path));

            var pathLength = path.Length;
            var pathPosition = 0;

            m_DryRun = true;
            if (!ParseToken('{'))
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
                        if (!ParseToken('{'))
                            return false;
                    }
                    else if (path[pathPosition] == '[')
                    {
                        ++pathPosition;
                        if (pathPosition == pathLength)
                            throw new ArgumentException("Malformed JSON property path: " + path, nameof(path));
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
        public bool CurrentPropertyHasValueEqualTo(JsonValue expectedValue)
        {
            // Grab property value.
            var savedPosition = m_Position;
            m_DryRun = false;
            if (!ParseValue(out var propertyValue))
            {
                m_Position = savedPosition;
                return false;
            }
            m_Position = savedPosition;

            // Match given value.
            var isMatch = false;
            if (propertyValue.type == JsonValueType.Array && m_MatchAnyElementInArray)
            {
                var array = propertyValue.arrayValue;
                for (var i = 0; !isMatch && i < array.Count; ++i)
                    isMatch = array[i] == expectedValue;
            }
            else
            {
                isMatch = propertyValue == expectedValue;
            }

            return isMatch;
        }

        public bool ParseToken(char token)
        {
            SkipWhitespace();
            if (m_Position == m_Length)
                return false;

            if (m_Text[m_Position] != token)
                return false;

            ++m_Position;
            SkipWhitespace();

            return m_Position < m_Length;
        }

        public bool ParseValue()
        {
            return ParseValue(out var result);
        }

        public bool ParseValue(out JsonValue result)
        {
            result = default;

            SkipWhitespace();
            if (m_Position == m_Length)
                return false;

            var ch = m_Text[m_Position];
            switch (ch)
            {
                case '"':
                    if (ParseStringValue(out result))
                        return true;
                    break;
                case '[':
                    if (ParseArrayValue(out result))
                        return true;
                    break;
                case '{':
                    if (ParseObjectValue(out result))
                        return true;
                    break;
                case 't':
                case 'f':
                    if (ParseBooleanValue(out result))
                        return true;
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

        public bool ParseStringValue(out JsonValue result)
        {
            result = default;

            SkipWhitespace();
            if (m_Position == m_Length || m_Text[m_Position] != '"')
                return false;
            ++m_Position;

            var startIndex = m_Position;
            var hasEscapes = false;

            while (m_Position < m_Length)
            {
                var ch = m_Text[m_Position];
                if (ch == '\\')
                {
                    ++m_Position;
                    if (m_Position == m_Length)
                        break;
                    hasEscapes = true;
                }
                else if (ch == '"')
                {
                    ++m_Position;
                    result = new JsonString
                    {
                        text = new Substring(m_Text, startIndex, m_Position - startIndex - 1),
                        hasEscapes = hasEscapes
                    };
                    return true;
                }
                ++m_Position;
            }

            return false;
        }

        public bool ParseArrayValue(out JsonValue result)
        {
            result = default;

            SkipWhitespace();
            if (m_Position == m_Length || m_Text[m_Position] != '[')
                return false;
            ++m_Position;

            if (m_Position == m_Length)
                return false;
            if (m_Text[m_Position] == ']')
            {
                // Empty array.
                result = new JsonValue { type = JsonValueType.Array };
                ++m_Position;
                return true;
            }

            List<JsonValue> values = null;
            if (!m_DryRun)
                values = new List<JsonValue>();

            while (m_Position < m_Length)
            {
                if (!ParseValue(out var value))
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

        public bool ParseObjectValue(out JsonValue result)
        {
            result = default;

            if (!ParseToken('{'))
                return false;
            if (m_Position < m_Length && m_Text[m_Position] == '}')
            {
                result = new JsonValue { type = JsonValueType.Object };
                ++m_Position;
                return true;
            }

            while (m_Position < m_Length)
            {
                if (!ParseStringValue(out var propertyName))
                    return false;

                if (!SkipToValue())
                    return false;

                if (!ParseValue(out var propertyValue))
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

        public bool ParseNumber(out JsonValue result)
        {
            result = default;

            SkipWhitespace();
            if (m_Position == m_Length)
                return false;

            var negative = false;
            var haveFractionalPart = false;
            var integralPart = 0L;
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

        public bool ParseBooleanValue(out JsonValue result)
        {
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

            result = default;
            return false;
        }

        public bool ParseNullValue(out JsonValue result)
        {
            result = default;
            return SkipString("null");
        }

        public bool SkipToValue()
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

        public bool isAtEnd => m_Position >= m_Length;

        private readonly string m_Text;
        private readonly int m_Length;
        private int m_Position;
        private bool m_MatchAnyElementInArray;
        private bool m_DryRun;

        public enum JsonValueType
        {
            None,
            Bool,
            Real,
            Integer,
            String,
            Array,
            Object,
            Any,
        }

        public struct JsonString : IEquatable<JsonString>
        {
            public Substring text;
            public bool hasEscapes;

            public override string ToString()
            {
                if (!hasEscapes)
                    return text.ToString();

                var builder = new StringBuilder();
                var length = text.length;
                for (var i = 0; i < length; ++i)
                {
                    var ch = text[i];
                    if (ch == '\\')
                    {
                        ++i;
                        if (i == length)
                            break;
                        ch = text[i];
                    }
                    builder.Append(ch);
                }
                return builder.ToString();
            }

            public bool Equals(JsonString other)
            {
                if (hasEscapes == other.hasEscapes)
                    return Substring.Compare(text, other.text, StringComparison.InvariantCultureIgnoreCase) == 0;

                var thisLength = text.length;
                var otherLength = other.text.length;

                int thisIndex = 0, otherIndex = 0;
                for (; thisIndex < thisLength && otherIndex < otherLength; ++thisIndex, ++otherIndex)
                {
                    var thisChar = text[thisIndex];
                    var otherChar = other.text[otherIndex];

                    if (thisChar == '\\')
                    {
                        ++thisIndex;
                        if (thisIndex == thisLength)
                            return false;
                        thisChar = text[thisIndex];
                    }

                    if (otherChar == '\\')
                    {
                        ++otherIndex;
                        if (otherIndex == otherLength)
                            return false;
                        otherChar = other.text[otherIndex];
                    }

                    if (char.ToUpperInvariant(thisChar) != char.ToUpperInvariant(otherChar))
                        return false;
                }

                return thisIndex == thisLength && otherIndex == otherLength;
            }

            public override bool Equals(object obj)
            {
                return obj is JsonString other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (text.GetHashCode() * 397) ^ hasEscapes.GetHashCode();
                }
            }

            public static bool operator==(JsonString left, JsonString right)
            {
                return left.Equals(right);
            }

            public static bool operator!=(JsonString left, JsonString right)
            {
                return !left.Equals(right);
            }

            public static implicit operator JsonString(string str)
            {
                return new JsonString { text = str };
            }
        }

        public struct JsonValue : IEquatable<JsonValue>
        {
            public JsonValueType type;
            public bool boolValue;
            public double realValue;
            public long integerValue;
            public JsonString stringValue;
            public List<JsonValue> arrayValue; // Allocates.
            public Dictionary<string, JsonValue> objectValue; // Allocates.
            public object anyValue;

            public bool ToBoolean()
            {
                switch (type)
                {
                    case JsonValueType.Bool: return boolValue;
                    case JsonValueType.Integer: return integerValue != 0;
                    case JsonValueType.Real: return NumberHelpers.Approximately(0, realValue);
                    case JsonValueType.String: return Convert.ToBoolean(ToString());
                }
                return default;
            }

            public long ToInteger()
            {
                switch (type)
                {
                    case JsonValueType.Bool: return boolValue ? 1 : 0;
                    case JsonValueType.Integer: return integerValue;
                    case JsonValueType.Real: return (long)realValue;
                    case JsonValueType.String: return Convert.ToInt64(ToString());
                }
                return default;
            }

            public double ToDouble()
            {
                switch (type)
                {
                    case JsonValueType.Bool: return boolValue ? 1 : 0;
                    case JsonValueType.Integer: return integerValue;
                    case JsonValueType.Real: return realValue;
                    case JsonValueType.String: return Convert.ToSingle(ToString());
                }
                return default;
            }

            public override string ToString()
            {
                switch (type)
                {
                    case JsonValueType.None: return "null";
                    case JsonValueType.Bool: return boolValue.ToString();
                    case JsonValueType.Integer: return integerValue.ToString(CultureInfo.InvariantCulture);
                    case JsonValueType.Real: return realValue.ToString(CultureInfo.InvariantCulture);
                    case JsonValueType.String: return stringValue.ToString();
                    case JsonValueType.Array:
                        if (arrayValue == null)
                            return "[]";
                        return $"[{string.Join(",", arrayValue.Select(x => x.ToString()))}]";
                    case JsonValueType.Object:
                        if (objectValue == null)
                            return "{}";
                        var elements = objectValue.Select(pair => $"\"{pair.Key}\" : \"{pair.Value}\"");
                        return $"{{{string.Join(",", elements)}}}";
                    case JsonValueType.Any: return anyValue.ToString();
                }
                return base.ToString();
            }

            public static implicit operator JsonValue(bool val)
            {
                return new JsonValue
                {
                    type = JsonValueType.Bool,
                    boolValue = val
                };
            }

            public static implicit operator JsonValue(long val)
            {
                return new JsonValue
                {
                    type = JsonValueType.Integer,
                    integerValue = val
                };
            }

            public static implicit operator JsonValue(double val)
            {
                return new JsonValue
                {
                    type = JsonValueType.Real,
                    realValue = val
                };
            }

            public static implicit operator JsonValue(string str)
            {
                return new JsonValue
                {
                    type = JsonValueType.String,
                    stringValue = new JsonString { text = str }
                };
            }

            public static implicit operator JsonValue(JsonString str)
            {
                return new JsonValue
                {
                    type = JsonValueType.String,
                    stringValue = str
                };
            }

            public static implicit operator JsonValue(List<JsonValue> array)
            {
                return new JsonValue
                {
                    type = JsonValueType.Array,
                    arrayValue = array
                };
            }

            public static implicit operator JsonValue(Dictionary<string, JsonValue> obj)
            {
                return new JsonValue
                {
                    type = JsonValueType.Object,
                    objectValue = obj
                };
            }

            public static implicit operator JsonValue(Enum val)
            {
                return new JsonValue
                {
                    type = JsonValueType.Any,
                    anyValue = val
                };
            }

            public bool Equals(JsonValue other)
            {
                // Default comparisons.
                if (type == other.type)
                {
                    switch (type)
                    {
                        case JsonValueType.None: return true;
                        case JsonValueType.Bool: return boolValue == other.boolValue;
                        case JsonValueType.Integer: return integerValue == other.integerValue;
                        case JsonValueType.Real: return NumberHelpers.Approximately(realValue, other.realValue);
                        case JsonValueType.String: return stringValue == other.stringValue;
                        case JsonValueType.Object: throw new NotImplementedException();
                        case JsonValueType.Array: throw new NotImplementedException();
                        case JsonValueType.Any: return anyValue.Equals(other.anyValue);
                    }
                    return false;
                }

                // anyValue-based comparisons.
                if (anyValue != null)
                    return Equals(anyValue, other);
                if (other.anyValue != null)
                    return Equals(other.anyValue, this);

                return false;
            }

            private static bool Equals(object obj, JsonValue value)
            {
                if (obj == null)
                    return false;

                if (obj is Regex regex)
                    return regex.IsMatch(value.ToString());
                if (obj is string str)
                {
                    switch (value.type)
                    {
                        case JsonValueType.String: return value.stringValue == str;
                        case JsonValueType.Integer: return long.TryParse(str, out var si) && si == value.integerValue;
                        case JsonValueType.Real:
                            return double.TryParse(str, out var sf) && NumberHelpers.Approximately(sf, value.realValue);
                        case JsonValueType.Bool:
                            if (value.boolValue)
                                return str == "True" || str == "true" || str == "1";
                            return str == "False" || str == "false" || str == "0";
                    }
                }
                if (obj is float f)
                {
                    if (value.type == JsonValueType.Real)
                        return NumberHelpers.Approximately(f, value.realValue);
                    if (value.type == JsonValueType.String)
                        return float.TryParse(value.ToString(), out var otherF) && Mathf.Approximately(f, otherF);
                }
                if (obj is double d)
                {
                    if (value.type == JsonValueType.Real)
                        return NumberHelpers.Approximately(d, value.realValue);
                    if (value.type == JsonValueType.String)
                        return double.TryParse(value.ToString(), out var otherD) &&
                            NumberHelpers.Approximately(d, otherD);
                }
                if (obj is int i)
                {
                    if (value.type == JsonValueType.Integer)
                        return i == value.integerValue;
                    if (value.type == JsonValueType.String)
                        return int.TryParse(value.ToString(), out var otherI) && i == otherI;
                }
                if (obj is long l)
                {
                    if (value.type == JsonValueType.Integer)
                        return l == value.integerValue;
                    if (value.type == JsonValueType.String)
                        return long.TryParse(value.ToString(), out var otherL) && l == otherL;
                }
                if (obj is bool b)
                {
                    if (value.type == JsonValueType.Bool)
                        return b == value.boolValue;
                    if (value.type == JsonValueType.String)
                    {
                        if (b)
                            return value.stringValue == "true" || value.stringValue == "True" ||
                                value.stringValue == "1";
                        return value.stringValue == "false" || value.stringValue == "False" ||
                            value.stringValue == "0";
                    }
                }
                // NOTE: The enum-based comparisons allocate both on the Convert.ToInt64() and Enum.GetName() path. I've found
                //       no way to do either comparison in a way that does not allocate.
                if (obj is Enum)
                {
                    if (value.type == JsonValueType.Integer)
                        return Convert.ToInt64(obj) == value.integerValue;
                    if (value.type == JsonValueType.String)
                        return value.stringValue == Enum.GetName(obj.GetType(), obj);
                }

                return false;
            }

            public override bool Equals(object obj)
            {
                return obj is JsonValue other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (int)type;
                    hashCode = (hashCode * 397) ^ boolValue.GetHashCode();
                    hashCode = (hashCode * 397) ^ realValue.GetHashCode();
                    hashCode = (hashCode * 397) ^ integerValue.GetHashCode();
                    hashCode = (hashCode * 397) ^ stringValue.GetHashCode();
                    hashCode = (hashCode * 397) ^ (arrayValue != null ? arrayValue.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (objectValue != null ? objectValue.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (anyValue != null ? anyValue.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator==(JsonValue left, JsonValue right)
            {
                return left.Equals(right);
            }

            public static bool operator!=(JsonValue left, JsonValue right)
            {
                return !left.Equals(right);
            }
        }
    }
}
