using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Analytics;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental.JSON
{
    // TODO We need System.Text.Json .NET Core 3.0 and beyond
    
    // RFC-8259 https://datatracker.ietf.org/doc/html/rfc8259#page-5
    // Depth first
    // https://www.json.org/json-en.html
    // TODO Consider converting to ReadOnlySpan
    public static class JsonUtility
    {
        public enum JsonType
        {
            Invalid,
            /// <summary>
            /// Represents a JSON object as defined by RFC-8259.
            /// </summary>
            Object,
            Array,
            Value,
            String,
            Number
        }

        //struct KeyValuePair
        
        public struct JsonNode
        {
            public JsonNode FirstChild()
            {
                return new JsonNode();
            }

            public string name => Data.Substring(NameStartIndex, NameEndIndex - NameStartIndex);
            public string value => Data.Substring(ValueStartIndex, ValueEndIndex - ValueStartIndex);

            public string Data;
            public int ValueStartIndex;     // Value start index.
            public int ValueEndIndex;       // Value length.
            public int NameStartIndex; // Start index of object name (excluding quotes)
            public int NameEndIndex;   // length of object name (excluding quotes)
            public JsonType Type;
        }

        // TODO Might be that root should be value
        public readonly struct JsonContext : IEnumerable<JsonNode>
        {
            private readonly string m_Data;
            private readonly JsonNode m_Root;

            // TODO It depends a lot on what we want to achieve....
            public struct JsonEnumerator : IEnumerator<JsonNode>
            {
                private const int kMaxDepth = 10;
                private readonly StackElement[] m_Stack;
                private JsonNode m_Current;
                private int m_Level;
                private int m_Index;
                private JsonType m_Type;
                private readonly string m_Buffer;

                private struct StackElement
                {
                    public int NameStartIndex;
                    public int NameEndIndex;
                    public int BeginObjectIndex;
                    public int EndObjectIndex;
                    public int ValueStartIndex;
                    public int ValueEndIndex;
                }
                
                public JsonEnumerator(string buffer)
                {
                    m_Stack = new StackElement[kMaxDepth];
                    m_Level = -1;
                    m_Buffer = buffer;
                    m_Index = 0;
                    m_Type = JsonType.Invalid;
                    m_Current = default;
                }

                private const char kBeginObject = '{';
                private const char kEndObject = '}';
                private const char kBeginArray = '[';
                private const char kEndArray = ']';
                private const char kNameSeparator = ':';
                private const char kValueSeparator = ',';
                private const char kQuotationMark = '"';
                private const char kEscapeSequence = '\\';
                
                private const char kWhiteSpaceSpace = ' ';
                private const char kWhiteSpaceTab = '\t';  
                private const char kWhiteSpaceLineFeed = '\n';
                private const char kWhiteSpaceCarriageReturn = '\r';
                
                public bool MoveNext()
                {
                    ref var e = ref m_Stack[++m_Level];
                    
                    for (; m_Index != m_Buffer.Length; ++m_Index) // TODO begin and end should be established and stored on stack
                    {
                        var c = m_Buffer[m_Index];
                        switch (c)
                        {
                            case kBeginObject:
                                m_Type = JsonType.Object;
                                e.EndObjectIndex = m_Buffer.LastIndexOf(kEndObject);
                                e.NameStartIndex = -1;
                                e.NameEndIndex = -1;
                                // TODO Current: Object 
                                break;
                            case kEndObject:
                                break;
                            case kBeginArray:
                                break;
                            case kEndArray:
                                break;
                            case kQuotationMark:
                                if (e.NameStartIndex == -1)
                                    e.NameStartIndex = m_Index + 1;
                                else if (e.NameEndIndex == -1)
                                    e.NameEndIndex = m_Index; 
                                    // TODO Current: String
                                //else if ()
                                else
                                    throw new Exception($"Unexpected {kQuotationMark} found on line {Line(m_Index)}.");
                                break;
                            
                            case kNameSeparator:
                                //m_Type = JsonType.Value;
                                // TODO This is incorrect, we should just mark we are looking for value and continue loop
                                /*var valueEndIndex = m_Buffer.LastIndexOf(kEndObject, e.EndObjectIndex - 1, e.EndObjectIndex - m_Index - 1);
                                m_Current = new JsonNode()
                                {
                                    Data = m_Buffer,
                                    NameStartIndex = e.NameStartIndex,
                                    NameEndIndex = e.NameEndIndex,
                                    ValueStartIndex = m_Index,
                                    ValueEndIndex = valueEndIndex,
                                    Type = m_Type
                                };
                                return true;*/
                                e.ValueStartIndex = -1;
                                e.ValueEndIndex = -1;
                                break;
                                
                            case kWhiteSpaceSpace:
                            case kWhiteSpaceTab:
                            case kWhiteSpaceLineFeed:
                            case kWhiteSpaceCarriageReturn:
                                break;
                            default:
                                if (e.NameStartIndex >= 0 && e.NameEndIndex == -1)
                                    continue;
                                throw new Exception("");
                        }    
                    }
                    
                    return false;
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