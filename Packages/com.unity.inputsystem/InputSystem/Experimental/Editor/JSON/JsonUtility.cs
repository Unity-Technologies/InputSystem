using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Analytics;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental.JSON
{
    /// <summary>
    /// A minimalistic streaming JSON parser supporting RFC-8259 https://datatracker.ietf.org/doc/html/rfc8259#page-5.
    /// See https://www.json.org/json-en.html for additional details.
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
                m_ArrayIndex = arrayIndex;
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
            
            public int arrayElementIndex => m_ArrayIndex;
            
            private readonly string m_Data;
            private readonly int m_ValueStartIndex;     // Value start index.
            private readonly int m_ValueEndIndex;       // Value length.
            private readonly int m_NameStartIndex;      // Start index of object name (excluding quotes)
            private readonly int m_NameEndIndex;        // length of object name (excluding quotes)
            private readonly int m_ArrayIndex; // TODO Use name range for array index instead?!
        }

        // TODO Might be that root should be value
        public readonly struct JsonContext : IEnumerable<JsonNode>
        {
            private readonly string m_Data;
            private readonly JsonNode m_Root;

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
                
                [StructLayout(LayoutKind.Sequential)]
                private struct StackElement
                {
                    public Range name;
                    public Range value;
                    public int arrayIndex;
                    public bool isArray;
                }
                
                public JsonEnumerator(string buffer)
                {
                    m_Stack = new StackElement[kMaxDepth];
                    m_Level = 0;
                    m_Buffer = buffer;
                    m_Index = 0;
                    m_Current = default;

                    // Treat root as:
                    //      "": { ... } 
                    // where only part after name separator correspond to passed buffer
                    m_Stack[0].name = new Range(0, 0);
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
                
                private const char kWhiteSpaceSpace = ' ';
                private const char kWhiteSpaceTab = '\t';  
                private const char kWhiteSpaceLineFeed = '\n';
                private const char kWhiteSpaceCarriageReturn = '\r';

                private static Range ReadString(string buffer, int index)
                {
                    // TODO Needs to handle escape sequences to comply to RFC
                    for (var i = index; i != buffer.Length; ++i)
                    {
                        if (buffer[i] == kQuotationMark)
                            return new Range(index, i);
                    }
                    throw new Exception($"Missing '{kQuotationMark}' terminating string.");
                }

                private static Range ReadNumber(string buffer, int index)
                {
                    var hasFloatingPoint = false;
                    for (var i = index; i != buffer.Length; ++i)
                    {
                        var c = buffer[i];
                        if (!char.IsDigit(c))
                        {
                            switch (c)
                            {
                                case kDecimalPoint:
                                    if (hasFloatingPoint)
                                        throw new Exception($"Unexpected '{kDecimalPoint}'.");
                                    hasFloatingPoint = true;
                                    break;
                                case kValueSeparator:
                                case kWhiteSpaceTab:
                                case kWhiteSpaceSpace:
                                case kWhiteSpaceLineFeed:
                                case kWhiteSpaceCarriageReturn:
                                    return new Range(index, i);
                                default:
                                    throw new Exception(
                                        $"Unexpected character '{c}'. Expected digit or decimal-point.");
                            }    
                        }
                    }

                    throw new Exception("Unexpected end of JSON content");
                }

                private static Range ReadObject(string buffer, int index)
                {
                    var endIndex = buffer.LastIndexOf(kEndObject);
                    return new Range(index, endIndex + 1);
                }

                private static Range ReadArray(string buffer, int index)
                {
                    var endIndex = buffer.LastIndexOf(kEndArray);
                    return new Range(index, endIndex + 1);
                }
                
                public bool MoveNext()
                {
                    var hasKey = m_Index == 0 || m_Stack[m_Level].isArray; // Note: Root has empty key, also treat key as being set if inside array
                    
                    for (; m_Index != m_Buffer.Length; ++m_Index) // TODO begin and end should be established and stored on stack
                    {
                        var c = m_Buffer[m_Index];
                        switch (c)
                        {
                            case kBeginObject:
                                SetCurrent(JsonType.Object, ReadObject(m_Buffer, m_Index)); 
                                ++m_Index;
                                ++m_Level;
                                return true;
                            
                            case kEndObject:
                                --m_Level;
                                break;
                            
                            case kBeginArray:
                                SetCurrent(JsonType.Array, ReadArray(m_Buffer, m_Index));
                                ++m_Index;
                                m_Stack[m_Level].name = new Range(0, 0);
                                m_Stack[m_Level].arrayIndex = 0;
                                m_Stack[m_Level].isArray = true;
                                return true;
                            
                            case kEndArray:
                                break;
                            
                            case kQuotationMark:
                                if (!hasKey)
                                {
                                    var range = ReadString(m_Buffer, m_Index + 1);
                                    m_Stack[m_Level].name = range;
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
                                    throw new Exception($"Unexpected {kNameSeparator} found on line {Line(m_Index)}.");
                                break;
                            
                            case kValueSeparator:
                                var isArray = m_Stack[m_Level].isArray;
                                if (hasKey && !isArray) // TODO Should not throw if within array and index > 0
                                    throw new Exception($"Unexpected {kValueSeparator} found on line {Line(m_Index)}.");
                                ++m_Stack[m_Level].arrayIndex;
                                hasKey = isArray; // Arrays do not use keys so pretend we already have one
                                break;
                            
                            // Continue scanning if white-space
                            case kWhiteSpaceSpace:
                            case kWhiteSpaceTab:
                            case kWhiteSpaceLineFeed:
                            case kWhiteSpaceCarriageReturn:
                                break;
                            
                            default:
                                // We have encountered a non-white-space or syntax encoding token.
                                // TODO Handle literals here as well
                                if (char.IsDigit(c))
                                {
                                    var range = ReadNumber(m_Buffer, m_Index);
                                    m_Index = range.End.Value;
                                    SetCurrent(JsonType.Number, range);
                                    return true;
                                }
                                    
                                throw new Exception("");
                        }    
                    }
                    
                    // No more key-value pairs to iterate
                    return false;
                }

                private void FailUnexpected(char c) => throw new Exception($"Unexpected {c} found on line {Line(m_Index)}.");

                private void SetCurrent(JsonType type, Range value)
                {
                    ref StackElement e = ref m_Stack[m_Level];
                    e.value = value;
                    m_Current = new JsonNode(type, m_Buffer, 
                        e.name.Start.Value, e.name.End.Value, 
                        e.value.Start.Value, e.value.End.Value, e.arrayIndex);
                    //m_HasValue = true;
                }

                private int Line(int index)
                {
                    int line = 0;
                    var n = Math.Min(index, m_Buffer.Length);
                    for (var i = 0; i < n; ++i)
                    {
                        if (m_Buffer[i] == kWhiteSpaceLineFeed)
                            ++line;
                    }
                    return line;
                }

                public void Reset()
                {
                    m_Level = 0;
                }

                public JsonNode Current => m_Current;

                object IEnumerator.Current => m_Current;

                public void Dispose() { }
            }

            public JsonContext(string json)
            {
                m_Data = json;
                m_Root = new JsonNode(JsonType.Object, json, -1, 0, 0, json.Length, -1);
            }

            public JsonNode root => m_Root;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public JsonEnumerator GetEnumerator() => new JsonEnumerator(m_Data);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerator<JsonNode> IEnumerable<JsonNode>.GetEnumerator() => GetEnumerator();
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}