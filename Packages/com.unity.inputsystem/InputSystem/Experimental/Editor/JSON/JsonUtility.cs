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
        public struct JsonNode
        {
            /// <summary>
            /// Returns the character sequence corresponding to the name of the current node.
            /// </summary>
            public ReadOnlySpan<char> name => Data.AsSpan(NameStartIndex, NameEndIndex - NameStartIndex);
            
            /// <summary>
            /// Returns the character sequence corresponding to the value of the current node.
            /// </summary>
            public ReadOnlySpan<char> value => Data.AsSpan(ValueStartIndex, ValueEndIndex - ValueStartIndex);
            
            public int arrayElementIndex => NameEndIndex;
            
            public string Data;
            public int ValueStartIndex;     // Value start index.
            public int ValueEndIndex;       // Value length.
            public int NameStartIndex;      // Start index of object name (excluding quotes)
            public int NameEndIndex;        // length of object name (excluding quotes)
            public JsonType Type;
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
                private JsonType m_Type;
                private readonly string m_Buffer;
                private bool m_HasKey;
                private bool m_HasValue;
                
                private struct Range
                {
                    public int StartIndex;  // inclusive
                    public int EndIndex;    // exclusive
                }
                
                [StructLayout(LayoutKind.Sequential)]
                private struct StackElement
                {
                    public Range name;
                    public int BeginObjectIndex;
                    public int EndObjectIndex;
                    public Range value;
                }
                
                public JsonEnumerator(string buffer)
                {
                    m_Stack = new StackElement[kMaxDepth];
                    m_Level = -1;
                    m_Buffer = buffer;
                    m_Index = 0;
                    m_Type = JsonType.Invalid;
                    m_Current = default;
                    m_HasKey = false;
                    m_HasValue = false;
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
                    for (var i = index; i != buffer.Length; ++i)
                    {
                        if (buffer[i] == kQuotationMark)
                            return new Range() { StartIndex = index, EndIndex = i };
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
                                    return new Range() { StartIndex = index, EndIndex = i };
                                default:
                                    throw new Exception(
                                        $"Unexpected character '{c}'. Expected digit or decimal-point.");
                            }    
                        }
                    }

                    throw new Exception("Unexpected end of JSON content");
                }
                
                public bool MoveNext()
                {
                    ref var e = ref m_Stack[++m_Level];

                    //m_Type = JsonType.Invalid;
                    
                    for (; m_Index != m_Buffer.Length; ++m_Index) // TODO begin and end should be established and stored on stack
                    {
                        var c = m_Buffer[m_Index];
                        switch (c)
                        {
                            case kBeginObject:
                                m_Type = JsonType.Object; // TODO This should be on approach object after name separator?!
                                e.EndObjectIndex = m_Buffer.LastIndexOf(kEndObject);
                                break;
                            case kEndObject:
                            case kBeginArray:
                            case kEndArray:
                                break;
                            case kQuotationMark: // TODO This would benefit form just indexing through array
                                if (!m_HasKey)
                                {
                                    var range = ReadString(m_Buffer, m_Index + 1);
                                    e.name = range;
                                    m_Index = range.EndIndex;
                                    m_HasKey = true;
                                }
                                else
                                {
                                    var range = ReadString(m_Buffer, m_Index + 1);
                                    e.value = range;
                                    m_Index = range.EndIndex + 1;
                                    SetCurrent(JsonType.String, e);
                                    return true;
                                }
                                break;
                            
                            case kNameSeparator:
                                if (!m_HasKey)
                                    throw new Exception($"Unexpected {kNameSeparator} found on line {Line(m_Index)}.");
                                break;
                            
                            case kValueSeparator:
                                if (m_HasKey && !m_HasValue)
                                    throw new Exception($"Unexpected {kValueSeparator} found on line {Line(m_Index)}.");
                                m_HasKey = false;
                                m_HasValue = false;
                                break;
                            
                            // Continue scanning if white-space
                            case kWhiteSpaceSpace:
                            case kWhiteSpaceTab:
                            case kWhiteSpaceLineFeed:
                            case kWhiteSpaceCarriageReturn:
                                break;
                            
                            default:
                                // We have encountered a non-white-space or syntax encoding token.
                                if (char.IsDigit(c))
                                {
                                    var range = ReadNumber(m_Buffer, m_Index);
                                    e.value = range;
                                    m_Index = range.EndIndex;
                                    SetCurrent(JsonType.Number, e);
                                    return true;
                                }
                                    
                                throw new Exception("");
                        }    
                    }
                    
                    m_Type = JsonType.Invalid;
                    return false;
                }

                private void SetCurrent(JsonType type, in StackElement e)
                {
                    m_Type = type;
                    
                    m_Current.Data = m_Buffer;
                    m_Current.NameStartIndex = e.name.StartIndex;
                    m_Current.NameEndIndex = e.name.EndIndex;
                    m_Current.ValueStartIndex = e.value.StartIndex;
                    m_Current.ValueEndIndex = e.value.EndIndex;
                    m_Current.Type = m_Type;

                    m_HasValue = true;
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
                
//                 public bool MoveNext()
//                 {
//                     // Object variants:
//                     // "name" : string | number | object | array
//
//                     // Start from current element in stack
//                     ref var current = ref m_Stack[m_Level];
//                     ++m_Level;
//                     
//                     switch (current.Type)
//                     {
//                         case JsonType.Object:
//                         {
//                             // Object must define a string name. White space is ignored and any non quote character
//                             // should be considered an error.
//                             var nameStartIndex = Scan(current.Data, current.StartIndex, current.EndIndex);
//                             if (current.Data[nameStartIndex] != kQuotationMark)
//                                 throw new Exception("Missing object name");
//
//                             var nameEndIndex = ScanFor(current.Data, nameStartIndex+1, current.EndIndex, kQuotationMark);
//                             var kvpIndex = ScanFor(current.Data, nameEndIndex+1, current.EndIndex, kNameSeparator);
//                             var valueIndex = Scan(current.Data, kvpIndex+1, current.EndIndex);
//                             if (valueIndex == current.EndIndex)
//                                 throw new Exception("Missing value");
//                             
//                             // Classify value type which must be either of:
//                             // object, array, number, string, literal (true, false, null)
//                             var c = current.Data[valueIndex];
//                             switch (c)
//                             {
//                                 case kBeginObject:
//                                     break;
//                                 case kBeginArray:
//                                     break;
//                                 case kQuotationMark:
//                                     break;
//                                 case kNameSeparator:
//                                     break;
//                                 case kWhiteSpaceSpace:
//                                 case kWhiteSpaceTab:
//                                 case kWhiteSpaceLineFeed:
//                                 case kWhiteSpaceCarriageReturn:
//                                     break;
//                                 default:
//                                     throw new Exception("");
//                             }
//                         }
//                             
//                             break;
//                         case JsonType.Array:
//                             break;
//                         case JsonType.Value:
//                             break;
//                         case JsonType.String:
//                             break;
//                         case JsonType.Number:
//                             break;
//                     }
//                     
//                     /*var n = m_Current.Data.Length;
//                     for (var i = 0; i < n; ++i)
//                     {
//                         // Skip whitespace
//                         var c = m_Current.Data[i];
//                         if (Char.IsWhiteSpace(c))
//                             continue;
//                         
//                         if (c == '{')
//                         {
//                             
//                         }
//                         if (c == '}')
//                         {
//                             
//                         }
//
//                         if (c == '"')
//                         {
//                             
//                         }
//
//                         if (c == '"')
//                         {
//                             
//                         }
//                     }*/
//                     return false;
//                 }

                private static int IndexOf(string s, char c, int startIndex, int endIndex)
                {
                    return s.IndexOf( c, startIndex, endIndex - startIndex);
                }
                
                private int Scan(string data, int startIndex, int endIndex)
                {
                    for (; startIndex < endIndex && char.IsWhiteSpace(data[startIndex]); ++startIndex) { }
                    if (startIndex == endIndex)
                        throw new Exception();
                    return startIndex;
                }
                
                private int ScanFor(string data, int startIndex, int endIndex, char terminate)
                {
                    for (; startIndex < endIndex && data[startIndex] != terminate; ++startIndex) { }
                    if (startIndex == endIndex)
                        throw new Exception();
                    return startIndex;
                }

                private int NextEnd(string data, int startIndex, int endIndex)
                {
                    return -1;
                }

                public void Reset()
                {
                    m_Level = 0;
                }

                public JsonNode Current => m_Current;

                object IEnumerator.Current => m_Current;

                public void Dispose() { }
            }

            private const char kOpen = '{';
            private const char kClose = '}';
            
            public JsonContext(string json)
            {
                m_Data = json;

                //var start = json.IndexOf(kOpen) + 1;
                //var end = json.LastIndexOf(kClose) - start;
                
                m_Root = new JsonNode
                {
                    Data = json,
                    Type = JsonType.Object,
                    ValueStartIndex = 0,
                    ValueEndIndex = json.Length,
                    NameStartIndex = -1,
                    NameEndIndex = 0
                };
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