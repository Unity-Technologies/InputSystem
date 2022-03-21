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
using System.Collections.Generic;

namespace SharpYaml.Serialization.Descriptors
{
    /// <summary>
    /// A default implementation for the <see cref="ITypeDescriptorFactory"/>.
    /// </summary>
    internal class TypeDescriptorFactory : ITypeDescriptorFactory
    {
        private readonly IAttributeRegistry attributeRegistry;
        private readonly Dictionary<Type, ITypeDescriptor> registeredDescriptors = new Dictionary<Type, ITypeDescriptor>();
        private readonly bool emitDefaultValues;
        private readonly bool respectPrivateSetters;
        private readonly IMemberNamingConvention namingConvention;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDescriptorFactory" /> class.
        /// </summary>
        /// <param name="attributeRegistry">The attribute registry.</param>
        /// <param name="emitDefaultValues">if set to <c>true</c> [emit default values].</param>
        /// <param name="respectPrivateSetters">If set to <c>true</c> will de/serialize properties with private setters.</param>
        /// <param name="namingConvention">The naming convention.</param>
        /// <exception cref="System.ArgumentNullException">attributeRegistry</exception>
        public TypeDescriptorFactory(IAttributeRegistry attributeRegistry, bool emitDefaultValues, bool respectPrivateSetters, IMemberNamingConvention namingConvention)
        {
            if (attributeRegistry == null)
                throw new ArgumentNullException("attributeRegistry");
            if (namingConvention == null)
                throw new ArgumentNullException("namingConvention");
            this.namingConvention = namingConvention;
            this.respectPrivateSetters = respectPrivateSetters;
            this.emitDefaultValues = emitDefaultValues;
            this.attributeRegistry = attributeRegistry;
        }

        public ITypeDescriptor Find(Type type, IComparer<object> memberComparer)
        {
            if (type == null)
                return null;

            lock (registeredDescriptors)
            {
                // Caching is integrated in this class, avoiding a ChainedTypeDescriptorFactory
                ITypeDescriptor descriptor;
                if (registeredDescriptors.TryGetValue(type, out descriptor))
                {
                    return descriptor;
                }

                descriptor = Create(type);

                var objectDescriptor = descriptor as ObjectDescriptor;
                if (objectDescriptor != null)
                {
                    objectDescriptor.SortMembers(memberComparer);
                }

                // Register this descriptor
                registeredDescriptors.Add(type, descriptor);

                return descriptor;
            }
        }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>The settings.</value>
        protected IAttributeRegistry AttributeRegistry { get { return attributeRegistry; } }

        /// <summary>
        /// Creates a type descriptor for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>An instance of type descriptor.</returns>
        protected virtual ITypeDescriptor Create(Type type)
        {
            ObjectDescriptor descriptor;
            // The order of the descriptors here is important

            if (PrimitiveDescriptor.IsPrimitive(type))
            {
                descriptor = new PrimitiveDescriptor(attributeRegistry, type, namingConvention);
            }
            else if (DictionaryDescriptor.IsDictionary(type)) // resolve dictionary before collections, as they are also collections
            {
                // IDictionary
                descriptor = new DictionaryDescriptor(attributeRegistry, type, emitDefaultValues, respectPrivateSetters, namingConvention);
            }
            else if (CollectionDescriptor.IsCollection(type))
            {
                // ICollection
                descriptor = new CollectionDescriptor(attributeRegistry, type, emitDefaultValues, respectPrivateSetters, namingConvention);
            }
            else if (type.IsArray)
            {
                // array[]
                descriptor = new ArrayDescriptor(attributeRegistry, type, namingConvention);
            }
            else if (NullableDescriptor.IsNullable(type))
            {
                descriptor = new NullableDescriptor(attributeRegistry, type, namingConvention);
            }
            else
            {
                // standard object (class or value type)
                descriptor = new ObjectDescriptor(attributeRegistry, type, emitDefaultValues, respectPrivateSetters, namingConvention);
            }

            // Initialize the descriptor
            descriptor.Initialize();
            return descriptor;
        }
    }
}