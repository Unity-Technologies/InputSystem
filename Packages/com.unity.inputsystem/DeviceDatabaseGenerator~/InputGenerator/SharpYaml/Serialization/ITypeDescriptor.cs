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

namespace SharpYaml.Serialization
{
    /// <summary>
    /// Provides access members of a type.
    /// </summary>
    public interface ITypeDescriptor
    {
        /// <summary>
        /// Gets the type described by this instance.
        /// </summary>
        /// <value>The type.</value>
        Type Type { get; }

        /// <summary>
        /// Gets the members of this type.
        /// </summary>
        /// <value>The members.</value>
        IEnumerable<IMemberDescriptor> Members { get; }

        /// <summary>
        /// Gets the member count.
        /// </summary>
        /// <value>The member count.</value>
        int Count { get; }

        /// <summary>
        /// Gets the category of this descriptor.
        /// </summary>
        /// <value>The category.</value>
        DescriptorCategory Category { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has members.
        /// </summary>
        /// <value><c>true</c> if this instance has members; otherwise, <c>false</c>.</value>
        bool HasMembers { get; }

        /// <summary>
        /// Gets the <see cref="IMemberDescriptor"/> with the specified name. Return null if not found
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The member.</returns>
        IMemberDescriptor this[string name] { get; }

        /// <summary>
        /// Determines whether the named member is remmaped.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the named member is remmaped; otherwise, <c>false</c>.</returns>
        bool IsMemberRemapped(string name);

        /// <summary>
        /// Gets a value indicating whether this instance is a compiler generated type.
        /// </summary>
        /// <value><c>true</c> if this instance is a compiler generated type; otherwise, <c>false</c>.</value>
        bool IsCompilerGenerated { get; }

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <value>The style.</value>
        YamlStyle Style { get; }

        /// <summary>
        /// Determines whether this instance contains a member with the specified member name.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <returns><c>true</c> if this instance contains a member with the specified member name; otherwise, <c>false</c>.</returns>
        bool Contains(string memberName);
    }
}