// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using SharpYaml.Serialization.Serializers;

namespace SharpYaml.Serialization
{
    /// <summary>
    /// This interface is used by <see cref="ObjectSerializer"/>, <see cref="DictionarySerializer"/> and <see cref="CollectionSerializer"/>
    /// as a backend interface for serializing/deserializing member name, member values, collection and dictionary items. The default
    /// implementation is <see cref="DefaultObjectSerializerBackend"/> that can be subclassed to provide aditionnal behavior.
    /// </summary>
    /// <remarks>
    /// TODO: Explain why this interface is used and how it can be extended for specific scenarios.
    /// </remarks>
    public interface IObjectSerializerBackend
    {
        /// <summary>
        /// Gets the style that will be used to serialize the object provided by <see cref="ObjectContext.Instance"/>.
        /// </summary>
        /// <param name="objectContext">The object context which contains the object instance to serialize.</param>
        /// <returns>The <see cref="YamlStyle"/> to use when serializing the object instance.</returns>
        YamlStyle GetStyle(ref ObjectContext objectContext);

        /// <summary>
        /// Allows to transform the name of the member while reading for the specified object context.
        /// </summary>
        /// <param name="objectContext">The object context to which the member name.</param>
        /// <param name="memberName">Name of the member read from the current yaml stream.</param>
        /// <param name="skipMember"></param>
        /// <returns>The name that will be used to get the <see cref="IMemberDescriptor"/> from the current object context.</returns>
        string ReadMemberName(ref ObjectContext objectContext, string memberName, out bool skipMember);

        /// <summary>
        /// Reads the value for the specified member from the current YAML stream.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="member">The member.</param>
        /// <param name="memberValue">The member value. See remarks</param>
        /// <param name="memberType">Type of the member.</param>
        /// <returns>The value read from YAML stream.</returns>
        /// <remarks>
        /// TODO: Explain memberValue when can be not null
        /// </remarks>
        object ReadMemberValue(ref ObjectContext objectContext, IMemberDescriptor member, object memberValue, Type memberType);

        /// <summary>
        /// Reads the collection item from the current YAML stream.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="value">The value.</param>
        /// <param name="itemType">Type of the item.</param>
        /// <param name="index"></param>
        /// <returns>The collection item read from YAML stream.</returns>
        object ReadCollectionItem(ref ObjectContext objectContext, object value, Type itemType, int index);

        /// <summary>
        /// Reads the dictionary item from the current YAML stream.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="keyValueType">Type of the key value.</param>
        /// <returns>The dictionary item (key,value) read from YAML stream.</returns>
        KeyValuePair<object, object> ReadDictionaryItem(ref ObjectContext objectContext, KeyValuePair<Type, Type> keyValueType);

        /// <summary>
        /// Writes the name of the member.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="member">The member.</param>
        /// <param name="memberName">Name of the member.</param>
        void WriteMemberName(ref ObjectContext objectContext, IMemberDescriptor member, string memberName);

        /// <summary>
        /// Writes the member value.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="member">The member.</param>
        /// <param name="memberValue">The member value.</param>
        /// <param name="memberType">Type of the member.</param>
        void WriteMemberValue(ref ObjectContext objectContext, IMemberDescriptor member, object memberValue, Type memberType);

        /// <summary>
        /// Writes the collection item.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="item">The item.</param>
        /// <param name="itemType">Type of the item.</param>
        /// <param name="index"></param>
        void WriteCollectionItem(ref ObjectContext objectContext, object item, Type itemType, int index);

        /// <summary>
        /// Writes the dictionary item.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="keyValue">The key value.</param>
        /// <param name="types">The types.</param>
        void WriteDictionaryItem(ref ObjectContext objectContext, KeyValuePair<object, object> keyValue, KeyValuePair<Type, Type> types);
    }
}