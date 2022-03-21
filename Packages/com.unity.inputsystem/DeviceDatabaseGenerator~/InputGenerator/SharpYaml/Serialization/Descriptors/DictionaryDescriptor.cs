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
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpYaml.Serialization.Descriptors
{
    /// <summary>
    /// Provides a descriptor for a <see cref="System.Collections.IDictionary"/>.
    /// </summary>
    public class DictionaryDescriptor : ObjectDescriptor
    {
        private static readonly List<string> ListOfMembersToRemove = new List<string> {"Comparer", "Keys", "Values", "Capacity"};

        private readonly Type keyType;
        private readonly Type valueType;
        private readonly MethodInfo getEnumeratorGeneric;
        private readonly MethodInfo addMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryDescriptor" /> class.
        /// </summary>
        /// <param name="attributeRegistry">The attribute registry.</param>
        /// <param name="type">The type.</param>
        /// <param name="emitDefaultValues">if set to <c>true</c> [emit default values].</param>
        /// <param name="respectPrivateSetters">If set to <c>true</c> will de/serialize properties with private setters.</param>
        /// <param name="namingConvention">The naming convention.</param>
        /// <exception cref="System.ArgumentException">Expecting a type inheriting from System.Collections.IDictionary;type</exception>
        public DictionaryDescriptor(IAttributeRegistry attributeRegistry, Type type, bool emitDefaultValues, bool respectPrivateSetters, IMemberNamingConvention namingConvention)
            : base(attributeRegistry, type, emitDefaultValues, respectPrivateSetters, namingConvention)
        {
            if (!IsDictionary(type))
                throw new ArgumentException("Expecting a type inheriting from System.Collections.IDictionary", "type");

            // extract Key, Value types from IDictionary<??, ??>
            var interfaceType = type.GetInterface(typeof(IDictionary<,>));
            if (interfaceType != null)
            {
                keyType = interfaceType.GetGenericArguments()[0];
                valueType = interfaceType.GetGenericArguments()[1];
                IsGenericDictionary = true;
                getEnumeratorGeneric = typeof(DictionaryDescriptor).GetMethod("GetGenericEnumerable").MakeGenericMethod(keyType, valueType);
                addMethod = interfaceType.GetMethod("Add", new[] {keyType, valueType});
            }
            else
            {
                keyType = typeof(object);
                valueType = typeof(object);
                addMethod = type.GetMethod("Add", new[] {keyType, valueType});
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            // Only Keys and Values
            IsPureDictionary = Count == 0;
        }

        public override DescriptorCategory Category { get { return DescriptorCategory.Dictionary; } }

        /// <summary>
        /// Gets a value indicating whether this instance is generic dictionary.
        /// </summary>
        /// <value><c>true</c> if this instance is generic dictionary; otherwise, <c>false</c>.</value>
        public bool IsGenericDictionary { get; private set; }

        /// <summary>
        /// Gets the type of the key.
        /// </summary>
        /// <value>The type of the key.</value>
        public Type KeyType { get { return keyType; } }

        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        /// <value>The type of the value.</value>
        public Type ValueType { get { return valueType; } }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is pure dictionary.
        /// </summary>
        /// <value><c>true</c> if this instance is pure dictionary; otherwise, <c>false</c>.</value>
        public bool IsPureDictionary { get; private set; }

        /// <summary>
        /// Determines whether the value passed is readonly.
        /// </summary>
        /// <param name="thisObject">The this object.</param>
        /// <returns><c>true</c> if [is read only] [the specified this object]; otherwise, <c>false</c>.</returns>
        public bool IsReadOnly(object thisObject)
        {
            return ((IDictionary) thisObject).IsReadOnly;
        }

        /// <summary>
        /// Gets a generic enumerator for a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>A generic enumerator.</returns>
        /// <exception cref="System.ArgumentNullException">dictionary</exception>
        /// <exception cref="System.NotSupportedException">Key value-pair [{0}] is not supported for IDictionary. Only DictionaryEntry.DoFormat(keyValueObject)</exception>
        public IEnumerable<KeyValuePair<object, object>> GetEnumerator(object dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");
            if (IsGenericDictionary)
            {
                foreach (var item in (IEnumerable<KeyValuePair<object, object>>) getEnumeratorGeneric.Invoke(null, new[] {dictionary}))
                {
                    yield return item;
                }
            }
            else
            {
                var simpleDictionary = (IDictionary) dictionary;
                foreach (var keyValueObject in simpleDictionary)
                {
                    if (!(keyValueObject is DictionaryEntry))
                    {
                        throw new NotSupportedException("Key value-pair type [{0}] is not supported for IDictionary. Only DictionaryEntry".DoFormat(keyValueObject));
                    }
                    var entry = (DictionaryEntry) keyValueObject;
                    yield return new KeyValuePair<object, object>(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// Adds a a key-value to a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.InvalidOperationException">No Add() method found on dictionary [{0}].DoFormat(Type)</exception>
        public void AddToDictionary(object dictionary, object key, object value)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");
            var simpleDictionary = dictionary as IDictionary;
            if (simpleDictionary != null)
            {
                simpleDictionary.Add(key, value);
            }
            else
            {
                // Only throw an exception if the addMethod is not accessible when adding to a dictionary
                if (addMethod == null)
                {
                    throw new InvalidOperationException("No Add() method found on dictionary [{0}]".DoFormat(Type));
                }
                addMethod.Invoke(dictionary, new object[] {key, value});
            }
        }

        /// <summary>
        /// Determines whether the specified type is a .NET dictionary.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is dictionary; otherwise, <c>false</c>.</returns>
        public static bool IsDictionary(Type type)
        {
            return typeof(IDictionary).IsAssignableFrom(type) || type.HasInterface(typeof(IDictionary<,>));
        }

        public static IEnumerable<KeyValuePair<object, object>> GetGenericEnumerable<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            return dictionary.Select(keyValue => new KeyValuePair<object, object>(keyValue.Key, keyValue.Value));
        }

        protected override bool PrepareMember(MemberDescriptorBase member)
        {
            // Filter members
            if (member is PropertyDescriptor && ListOfMembersToRemove.Contains(member.OriginalName))
                //if (member is PropertyDescriptor && (member.DeclaringType.Namespace ?? string.Empty).StartsWith(SystemCollectionsNamespace) && ListOfMembersToRemove.Contains(member.Name))
            {
                return false;
            }

            return base.PrepareMember(member);
        }
    }
}