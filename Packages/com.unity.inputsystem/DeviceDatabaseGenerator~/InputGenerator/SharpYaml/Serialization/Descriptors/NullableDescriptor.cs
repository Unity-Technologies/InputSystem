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
    /// Describes a descriptor for a nullable type <see cref="Nullable{T}"/>.
    /// </summary>
    internal class NullableDescriptor : ObjectDescriptor
    {
        private static readonly List<IMemberDescriptor> EmptyMembers = new List<IMemberDescriptor>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectDescriptor" /> class.
        /// </summary>
        /// <param name="attributeRegistry">The attribute registry.</param>
        /// <param name="type">The type.</param>
        /// <param name="namingConvention">The naming convention.</param>
        /// <exception cref="System.ArgumentException">Type [{0}] is not a primitive</exception>
        public NullableDescriptor(IAttributeRegistry attributeRegistry, Type type, IMemberNamingConvention namingConvention)
            : base(attributeRegistry, type, false, false, namingConvention)
        {
            if (!IsNullable(type))
                throw new ArgumentException("Type [{0}] is not a primitive");

            UnderlyingType = Nullable.GetUnderlyingType(type);
        }

        public override DescriptorCategory Category { get { return DescriptorCategory.Nullable; } }

        /// <summary>
        /// Gets the type underlying type T of the nullable <see cref="Nullable{T}"/>
        /// </summary>
        /// <value>The type of the element.</value>
        public Type UnderlyingType { get; private set; }

        /// <summary>
        /// Determines whether the specified type is nullable.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is nullable; otherwise, <c>false</c>.</returns>
        public static bool IsNullable(Type type)
        {
            return type.IsNullable();
        }

        protected override System.Collections.Generic.List<IMemberDescriptor> PrepareMembers()
        {
            return EmptyMembers;
        }
    }
}