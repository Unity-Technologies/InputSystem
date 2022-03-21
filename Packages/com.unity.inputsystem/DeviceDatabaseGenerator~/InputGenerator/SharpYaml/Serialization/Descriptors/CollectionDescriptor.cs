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
using System.Collections.ObjectModel;
using System.Reflection;

namespace SharpYaml.Serialization.Descriptors
{
    /// <summary>
    /// Provides a descriptor for a <see cref="System.Collections.ICollection"/>.
    /// </summary>
    public class CollectionDescriptor : ObjectDescriptor
    {
        private static readonly List<string> ListOfMembersToRemove = new List<string> {"Capacity", "Count", "IsReadOnly", "IsFixedSize", "IsSynchronized", "SyncRoot", "Comparer"};

        private readonly Func<object, bool> IsReadOnlyFunction;
        private readonly Func<object, int> GetCollectionCountFunction;
        private readonly Action<object, object> CollectionAddFunction;
        private readonly bool isKeyedCollection = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionDescriptor" /> class.
        /// </summary>
        /// <param name="attributeRegistry">The attribute registry.</param>
        /// <param name="type">The type.</param>
        /// <param name="emitDefaultValues">if set to <c>true</c> [emit default values].</param>
        /// <param name="namingConvention">The naming convention.</param>
        /// <exception cref="System.ArgumentException">Expecting a type inheriting from System.Collections.ICollection;type</exception>
        public CollectionDescriptor(IAttributeRegistry attributeRegistry, Type type, bool emitDefaultValues, bool respectPrivateSetters, IMemberNamingConvention namingConvention)
            : base(attributeRegistry, type, emitDefaultValues, respectPrivateSetters, namingConvention)
        {
            if (!IsCollection(type))
                throw new ArgumentException("Expecting a type inheriting from System.Collections.ICollection", "type");

            // Gets the element type
            var collectionType = type.GetInterface(typeof(IEnumerable<>));
            ElementType = (collectionType != null) ? collectionType.GetGenericArguments()[0] : typeof(object);

            // implements ICollection<T> 
            Type itype;
            if ((itype = type.GetInterface(typeof(ICollection<>))) != null)
            {
                var add = itype.GetMethod("Add", new[] {ElementType});
                CollectionAddFunction = (obj, value) => add.Invoke(obj, new[] {value});
                var countMethod = itype.GetProperty("Count").GetGetMethod();
                GetCollectionCountFunction = o => (int) countMethod.Invoke(o, null);
                var isReadOnly = itype.GetProperty("IsReadOnly").GetGetMethod();
                IsReadOnlyFunction = obj => (bool) isReadOnly.Invoke(obj, null);
                isKeyedCollection = type.ExtendsGeneric(typeof(KeyedCollection<,>));
            }
            // implements IList 
            else if (typeof(IList).IsAssignableFrom(type))
            {
                CollectionAddFunction = (obj, value) => ((IList) obj).Add(value);
                GetCollectionCountFunction = o => ((IList) o).Count;
                IsReadOnlyFunction = obj => ((IList) obj).IsReadOnly;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            IsPureCollection = Count == 0;
        }

        public override DescriptorCategory Category { get { return DescriptorCategory.Collection; } }

        /// <summary>
        /// Gets or sets the type of the element.
        /// </summary>
        /// <value>The type of the element.</value>
        public Type ElementType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is a pure collection (no public property/field)
        /// </summary>
        /// <value><c>true</c> if this instance is pure collection; otherwise, <c>false</c>.</value>
        public bool IsPureCollection { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this collection type has add method.
        /// </summary>
        /// <value><c>true</c> if this instance has add; otherwise, <c>false</c>.</value>
        public bool HasAdd { get { return CollectionAddFunction != null; } }

        /// <summary>
        /// Add to the collections of the same type than this descriptor.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="value">The value to add to this collection.</param>
        public void CollectionAdd(object collection, object value)
        {
            CollectionAddFunction(collection, value);
        }

        /// <summary>
        /// Determines whether the specified collection is read only.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns><c>true</c> if the specified collection is read only; otherwise, <c>false</c>.</returns>
        public bool IsReadOnly(object collection)
        {
            return collection == null || IsReadOnlyFunction == null || IsReadOnlyFunction(collection);
        }

        /// <summary>
        /// Determines the number of elements of a collection, -1 if it cannot determine the number of elements.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>The number of elements of a collection, -1 if it cannot determine the number of elements.</returns>
        public int GetCollectionCount(object collection)
        {
            return collection == null || GetCollectionCountFunction == null ? -1 : GetCollectionCountFunction(collection);
        }

        /// <summary>
        /// Determines whether the specified type is collection.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is collection; otherwise, <c>false</c>.</returns>
        public static bool IsCollection(Type type)
        {
            return !type.IsArray && (typeof(ICollection).IsAssignableFrom(type) || type.HasInterface(typeof(ICollection<>)));
        }

        /// <inheritdoc/>
        protected override bool PrepareMember(MemberDescriptorBase member)
        {
            // Filter members
            if (member is PropertyDescriptor && ListOfMembersToRemove.Contains(member.OriginalName))
                //if (member is PropertyDescriptor && (member.DeclaringType.Namespace ?? string.Empty).StartsWith(SystemCollectionsNamespace) && ListOfMembersToRemove.Contains(member.Name))
            {
                return false;
            }

            return !IsCompilerGenerated && base.PrepareMember(member);
        }
    }
}