using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Analytics;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;

namespace UnityEngine.InputSystem.Experimental.JSON
{
    /// <summary>
    /// A minimalistic streaming JSON parser supporting RFC-8259 https://datatracker.ietf.org/doc/html/rfc8259#page-5.
    /// See https://www.json.org/json-en.html for additional details and syntax.
    /// </summary>
    /// <remarks>
    /// Mainly exists to mitigate lack of System.Text.Json of .NET Core 3.0 and beyond capabilities.
    /// </remarks>
    public static class JsonUtility
    {
        /// <summary>
        /// Represents a JSON type according to RFC-8259.
        /// </summary>
        public enum JsonType
        {
            /// <summary>
            /// Indicates that the associated JSON object or key-value pair is not valid.
            /// </summary>
            Invalid,
            
            /// <summary>
            /// Represents a JSON object as defined by RFC-8259.
            /// </summary>
            Object,
            
            /// <summary>
            /// Represents a JSON array as defined by RFC-8259.
            /// </summary>
            Array,
            
            /// <summary>
            /// Represents a JSON value as defined by RFC-8259.
            /// </summary>
            Value,
            
            /// <summary>
            /// Represents a JSON string as defined by RFC-8259.
            /// </summary>
            String,
            
            /// <summary>
            /// Represents a JSON number as defined by RFC-8259.
            /// </summary>
            Number
        }

        /// <summary>
        /// A JSON key-value pair where name represents the identifier and value represents the value.
        /// Note that this is a basic representation of the JSON context and only classifies elements based on
        /// the JSON grammar as defined in RFC-8259.
        /// </summary>
        /// <remarks>
        /// The following may be expected:
        /// - String: name holds the identifier and value contains the string (excluding quotation mark encoding).
        /// - Object: name holds the identifier and value contains the surrounding curly-braces of the object including
        ///           any sub-hierarchy. Note that for root object, name is null.
        /// - Array:  name holds the identifier and value contains the array content surrounded by brackets.
        /// - Number: name holds the identifier and value contains the value literal.
        /// - Literal: name holds the identifier and value contains the literal.
        ///
        /// Note that only when parsing array elements index is defined.
        /// </remarks>
        public readonly struct JsonNode
        {
            public JsonNode(JsonType type, string data, int nameStartIndex, int nameEndIndex, 
                int valueStartIndex, int valueEndIndex, int arrayIndex)
            {
                m_Data = data;
                m_NameStartIndex = nameStartIndex;
                m_NameEndIndex = nameEndIndex;
                m_ValueStartIndex = valueStartIndex;
                m_ValueEndIndex = valueEndIndex;
                arrayElementIndex = arrayIndex;
                this.type = type;
            }
            
            /// <summary>
            /// The associated JSON type.
            /// </summary>
            public JsonType type { get; }

            /// <summary>
            /// Returns the character sequence corresponding to the name of the current node.
            /// </summary>
            public ReadOnlySpan<char> name => m_Data.AsSpan(m_NameStartIndex, m_NameEndIndex - m_NameStartIndex);
            
            /// <summary>
            /// Returns the character sequence corresponding to the value of the current node.
            /// </summary>
            public ReadOnlySpan<char> value => m_Data.AsSpan(m_ValueStartIndex, m_ValueEndIndex - m_ValueStartIndex);
            
            public int arrayElementIndex { get; }

            private readonly string m_Data;
            private readonly int m_ValueStartIndex;     // Value start index.
            private readonly int m_ValueEndIndex;       // Value length.
            private readonly int m_NameStartIndex;      // Start index of object name (excluding quotes)
            private readonly int m_NameEndIndex;        // length of object name (excluding quotes)
        }

        public class JsonParseException : Exception
        {
            public JsonParseException(string message)
                : base(message)
            { }
        }

        // TODO Might be that root should be value
        public readonly struct JsonContext : IEnumerable<JsonNode>
        {
            private readonly string m_Data;

            // TODO It depends a lot on what we want to achieve....
            // Currently works mainly as a lexer.
            public struct JsonEnumerator : IEnumerator<JsonNode>
            {
                private const int kMaxDepth = 10;
                private readonly StackElement[] m_Stack;
                private JsonNode m_Current;
                private int m_Level;
                private int m_Index;
                private readonly string m_Buffer;
                
                private struct StackElement
                {
                    public Range Name;
                    public Range Value;
                    public int Index;
                    public bool IsArray;
                }
                
                public JsonEnumerator(string buffer, int maxDepth = kMaxDepth)
                {
                    m_Stack = new StackElement[maxDepth];
                    m_Level = 0;
                    m_Buffer = buffer;
                    m_Index = 0;
                    m_Current = default;

                    // Treat root as:
                    //      "": { ... } 
                    // where only part after name separator correspond to passed buffer
                    m_Stack[0].Name = new Range(0, 0);
                }

                private const char kBeginObject = '{';
                private const char kEndObject = '}';
                private const char kBeginArray = '[';
                private const char kEndArray = ']';
                private const char kNameSeparator = ':';
                private const char kValueSeparator = ',';
                private const char kQuotationMark = '"';
                private const char kEscapeSequence = '\\';
                
                private const char kDecimalPoint = '.';
                private const char kExponential1 = 'e';
                private const char kExponential2 = 'E';
                private const char kNegative = '-';
                private const char kPositive = '+';
                
                private const char kWhiteSpaceSpace = ' ';
                private const char kWhiteSpaceTab = '\t';  
                private const char kWhiteSpaceLineFeed = '\n';
                private const char kWhiteSpaceCarriageReturn = '\r';

                private const string kNull = "null";
                private const string kFalse = "false";
                private const string kTrue = "true";
                
                private static Range ReadString(string buffer, int index)
                {
                    // TODO Needs to handle escape sequences to comply to RFC
                    for (var i = index; i != buffer.Length; ++i)
                    {
                        if (buffer[i] == kQuotationMark)
                            return new Range(index, i);
                    }
                    
                    throw new JsonParseException($"Missing '{kQuotationMark}' terminating string.");
                }
                
                // TODO This incorrect verifies numbers, should extract range first
                private static Range ReadNumber(string buffer, int index)
                {
                    var hasDecimalPoint = false;
                    var hasExponent = false;
                    var i = index + 1;
                    for (; i != buffer.Length; ++i)
                    {
                        var c = buffer[i];
                        if (char.IsDigit(c))
                            continue;
                        
                        switch (c)
                        {
                            case kDecimalPoint:
                                if (hasDecimalPoint)
                                    throw new JsonParseException($"Unexpected '{c}'. Expected digit, white-space or value separator.");
                                hasDecimalPoint = true;
                                break;
                            case kExponential1:
                            case kExponential2:
                                if (!char.IsDigit(buffer[i-1]))
                                    throw new JsonParseException($"Exponent '{kExponential1}'/'{kExponential2}' must be preceded by a digit.");
                                if (hasExponent)
                                    throw new JsonParseException($"Unexpected '{c}'. Expected digit, white-space or value separator.");
                                hasExponent = true;
                                var j = i + 1;
                                if (j == buffer.Length)
                                    throw new JsonParseException($"Unexpected '{c}'. Expected digit, white-space or value separator.");
                                var k = buffer[j];
                                if (!char.IsDigit(k) && k != kNegative && k != kPositive)
                                    throw new JsonParseException($"Unexpected '{k}'. Expected digit, '{kNegative}' or '{kPositive}'.");
                                ++i;
                                break;
                            case kValueSeparator:
                            case kWhiteSpaceTab:
                            case kWhiteSpaceSpace:
                            case kWhiteSpaceLineFeed:
                            case kWhiteSpaceCarriageReturn:
                                // TODO Verify last is number
                                return new Range(index, i);
                            default:
                                throw new JsonParseException($"Unexpected character '{c}'. Expected digit or decimal-point.");
                        }   
                    }
                    
                    // TODO Move SetCurrent here and return bool
                    // TODO Verify last is number
                    return new Range(index, i);
                }

                private static Range ReadScope(string buffer, int index, char begin, char end)
                {
                    var i = index + 1;
                    var x = 1;
                    for (; i < buffer.Length && x != 0; ++i)
                    {
                        var c = buffer[i];
                        if (c == begin) 
                            ++x;
                        else if (c == end) 
                            --x;
                    }

                    if (x != 0)
                        throw new JsonParseException($"Failed to find matching end tag '{end}' for opening tag '{begin}' on line {Line(buffer, index)}");
                    return new Range(index, i);
                }
                
                public bool MoveNext()
                {
                    var hasKey = m_Index == 0 || m_Stack[m_Level].IsArray; // Note: Root has empty key, also treat key as being set if inside array
                    
                    for (; m_Index != m_Buffer.Length; ++m_Index) // TODO begin and end should be established and stored on stack
                    {
                        var c = m_Buffer[m_Index];
                        switch (c)
                        {
                            case kBeginObject:
                                SetCurrent(JsonType.Object, ReadScope(m_Buffer, m_Index, kBeginObject, kEndObject));
                                ++m_Index;
                                ++m_Level;
                                return true;
                            
                            case kEndObject:
                                --m_Level;
                                break;
                            
                            case kBeginArray:
                                SetCurrent(JsonType.Array, ReadScope(m_Buffer, m_Index, kBeginArray, kEndArray));
                                ++m_Index;
                                m_Stack[m_Level].Name = new Range(0, 0);
                                m_Stack[m_Level].Index = 0;
                                m_Stack[m_Level].IsArray = true;
                                return true;
                            
                            case kEndArray:
                                break;
                            
                            case kQuotationMark:
                                if (!hasKey)
                                {
                                    var range = ReadString(m_Buffer, m_Index + 1);
                                    m_Stack[m_Level].Name = range;
                                    m_Index = range.End.Value;
                                    hasKey = true;
                                }
                                else
                                {
                                    var range = ReadString(m_Buffer, m_Index + 1);
                                    m_Index = range.End.Value + 1;
                                    SetCurrent(JsonType.String, range);
                                    return true;
                                }
                                break;
                            
                            case kNameSeparator:
                                if (!hasKey)
                                    FailUnexpected(kNameSeparator);
                                break;
                            
                            case kValueSeparator:
                                var isArray = m_Stack[m_Level].IsArray;
                                if (hasKey && !isArray) // TODO Should not throw if within array and index > 0
                                    FailUnexpected(kValueSeparator);
                                ++m_Stack[m_Level].Index;
                                hasKey = isArray; // Arrays do not use keys so pretend we already have one
                                break;
                            
                            case kWhiteSpaceSpace:
                            case kWhiteSpaceTab:
                            case kWhiteSpaceLineFeed:
                            case kWhiteSpaceCarriageReturn:
                                break;
                                
                            default:
                                if (char.IsDigit(c) || c == kNegative)
                                {
                                    var range = ReadNumber(m_Buffer, m_Index);
                                    m_Index = range.End.Value;
                                    SetCurrent(JsonType.Number, range);
                                    return true;
                                }
                                if (IsValue(kFalse)) 
                                    return true;
                                if (IsValue(kTrue)) 
                                    return true;
                                if (IsValue(kNull)) 
                                    return true;
                                    
                                throw new JsonParseException("");
                        }    
                    }
                    
                    // No more key-value pairs to iterate
                    return false;
                }

                private bool IsValue(string expected)
                {
                    if (m_Index + expected.Length >= m_Buffer.Length)
                        return false;
                    if (!m_Buffer.AsSpan(m_Index, expected.Length).StartsWith(expected)) 
                        return false;
                    var range = new Range(m_Index, m_Index + expected.Length);
                    m_Index = range.End.Value;
                    SetCurrent(JsonType.Value, range);
                    return true;
                }

                private void FailMissingTermination(char begin, char end)
                {
                    throw new JsonParseException($"Failed to find matching end tag '{end}' for opening tag '{begin}' on line {Line(m_Buffer, m_Index)}");
                }

                private void FailUnexpected(char c, string expected = null)
                {
                    var message = $"Unexpected '{c}' found on line {Line(m_Buffer, m_Index)}.";
                    if (expected != null)
                        throw new JsonParseException($"{message} {expected}");
                    throw new JsonParseException(message);
                }

                private void SetCurrent(JsonType type, Range value)
                {
                    ref StackElement e = ref m_Stack[m_Level];
                    e.Value = value;
                    m_Current = new JsonNode(type, m_Buffer, 
                        e.Name.Start.Value, e.Name.End.Value, 
                        e.Value.Start.Value, e.Value.End.Value, e.Index);
                }

                private static int Line(string buffer, int index)
                {
                    int line = 0;
                    var n = Math.Min(index, buffer.Length);
                    for (var i = 0; i < n; ++i)
                    {
                        if (buffer[i] == kWhiteSpaceLineFeed)
                            ++line;
                    }
                    return line;
                }

                public void Reset()
                {
                    m_Level = 0;
                    m_Index = 0;
                }

                public JsonNode Current => m_Current;

                object IEnumerator.Current => m_Current;

                public void Dispose() { }
            }

            public JsonContext(string json)
            {
                m_Data = json;
                root = new JsonNode(JsonType.Object, json, -1, 0, 0, json.Length, -1);
            }

            public JsonNode root { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public JsonEnumerator GetEnumerator() => new JsonEnumerator(m_Data);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerator<JsonNode> IEnumerable<JsonNode>.GetEnumerator() => GetEnumerator();
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}