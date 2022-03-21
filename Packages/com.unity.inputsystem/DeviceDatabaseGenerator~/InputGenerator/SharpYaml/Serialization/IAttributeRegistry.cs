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
using System.Linq;
using System.Reflection;
using SharpYaml.Serialization.Descriptors;

namespace SharpYaml.Serialization
{
    /// <summary>
    /// A registry for all attributes.
    /// </summary>
    public interface IAttributeRegistry
    {
        /// <summary>
        /// Gets or sets the attribute remapper. May be null
        /// </summary>
        /// <value>The remap attribute.</value>
        Func<Attribute, Attribute> AttributeRemap { get; set; }

        /// <summary>
        /// Gets the attributes associated with the specified member.
        /// </summary>
        /// <param name="memberInfo">The reflection member.</param>
        /// <param name="inherit">if set to <c>true</c> includes inherited attributes.</param>
        /// <returns>An enumeration of <see cref="Attribute"/>.</returns>
        List<Attribute> GetAttributes(MemberInfo memberInfo, bool inherit = true);

        /// <summary>
        /// Gets or sets the prepare member callback.
        /// </summary>
        Action<ObjectDescriptor, List<IMemberDescriptor>> PrepareMembersCallback { get; set; }

        /// <summary>
        /// Registers an attribute for the specified member. Restriction: Attributes registered this way cannot be listed in inherited attributes.
        /// </summary>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="attribute">The attribute.</param>
        void Register(MemberInfo memberInfo, Attribute attribute);
    }

    /// <summary>
    /// Extension methods for attribute registry.
    /// </summary>
    public static class AttributeRegistryExtensions
    {
        /// <summary>
        /// Gets the attributes associated with the specified member.
        /// </summary>
        /// <typeparam name="T">Type of the attribute</typeparam>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="inherit">if set to <c>true</c> [inherit].</param>
        /// <returns>An enumeration of <see cref="Attribute"/>.</returns>
        public static IEnumerable<T> GetAttributes<T>(this IAttributeRegistry attributeRegistry, MemberInfo memberInfo, bool inherit = true) where T : Attribute
        {
            return attributeRegistry.GetAttributes(memberInfo, inherit).OfType<T>();
        }

        /// <summary>
        /// Gets the first attribute of type T associated with the specified member.
        /// </summary>
        /// <typeparam name="T">Type of the attribute</typeparam>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="inherit">if set to <c>true</c> [inherit].</param>
        /// <returns>An attribute of type {T} if it was found; otherwise <c>null</c> </returns>
        public static T GetAttribute<T>(this IAttributeRegistry attributeRegistry, MemberInfo memberInfo, bool inherit = true) where T : Attribute
        {
            return attributeRegistry.GetAttributes(memberInfo, inherit).OfType<T>().FirstOrDefault();
        }
    }
}