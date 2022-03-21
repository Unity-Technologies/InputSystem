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
using System.Reflection;

namespace SharpYaml.Serialization.Descriptors
{
    /// <summary>
    /// A descriptor for an array.
    /// </summary>
    public class ArrayDescriptor : ObjectDescriptor
    {
        private readonly Type elementType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectDescriptor" /> class.
        /// </summary>
        /// <param name="attributeRegistry">The attribute registry.</param>
        /// <param name="type">The type.</param>
        /// <param name="namingConvention">The naming convention.</param>
        /// <exception cref="System.ArgumentException">Expecting arrat type;type</exception>
        public ArrayDescriptor(IAttributeRegistry attributeRegistry, Type type, IMemberNamingConvention namingConvention)
            : base(attributeRegistry, type, false, false, namingConvention)
        {
            if (!type.IsArray)
                throw new ArgumentException("Expecting array type", "type");

            if (type.GetArrayRank() != 1)
            {
                throw new ArgumentException("Cannot support dimension [{0}] for type [{1}]. Only supporting dimension of 1".DoFormat(type.GetArrayRank(), type.FullName));
            }

            elementType = type.GetElementType();
        }

        public override DescriptorCategory Category { get { return DescriptorCategory.Array; } }

        /// <summary>
        /// Gets the type of the array element.
        /// </summary>
        /// <value>The type of the element.</value>
        public Type ElementType { get { return elementType; } }

        /// <summary>
        /// Creates the equivalent of list type for this array.
        /// </summary>
        /// <returns>A list type with same element type than this array.</returns>
        public Array CreateArray(int dimension)
        {
            return Array.CreateInstance(ElementType, dimension);
        }
    }
}