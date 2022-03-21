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
    /// A default implementation for <see cref="IAttributeRegistry"/>. 
    /// This implementation allows to retrieve default attributes for a member or 
    /// to attach an attribute to a specific type/member.
    /// </summary>
    public class AttributeRegistry : IAttributeRegistry
    {
        private readonly object globalLock = new object();
        private readonly Dictionary<MemberInfoKey, List<Attribute>> cachedAttributes = new Dictionary<MemberInfoKey, List<Attribute>>();
        private readonly Dictionary<MemberInfo, List<Attribute>> registeredAttributes = new Dictionary<MemberInfo, List<Attribute>>();

        public Action<ObjectDescriptor, List<IMemberDescriptor>> PrepareMembersCallback { get; set; }

        /// <summary>
        /// Gets or sets the attribute remapper. May be null
        /// </summary>
        /// <value>The remap attribute.</value>
        public Func<Attribute, Attribute> AttributeRemap { get; set; }

        /// <summary>
        /// Gets the attributes associated with the specified member.
        /// </summary>
        /// <param name="memberInfo">The reflection member.</param>
        /// <param name="inherit">if set to <c>true</c> includes inherited attributes.</param>
        /// <returns>An enumeration of <see cref="Attribute"/>.</returns>
        public virtual List<Attribute> GetAttributes(MemberInfo memberInfo, bool inherit = true)
        {
            var key = new MemberInfoKey(memberInfo, inherit);

            lock (globalLock)
            {
                // Use a cache of attributes
                List<Attribute> attributes;
                if (cachedAttributes.TryGetValue(key, out attributes))
                {
                    return attributes;
                }

                // Else retrieve all default attributes
                var defaultAttributes = memberInfo.GetCustomAttributes(inherit).OfType<Attribute>();
                attributes = defaultAttributes.ToList();

                // And add registered attributes
                List<Attribute> registered;
                if (registeredAttributes.TryGetValue(memberInfo, out registered))
                {
                    attributes.AddRange(registered);
                }

                // Allow to remap attributes
                if (AttributeRemap != null)
                {
                    for (int i = 0; i < attributes.Count; i++)
                    {
                        attributes[i] = AttributeRemap(attributes[i]);
                    }
                }

                // Add to the cache
                cachedAttributes.Add(key, attributes);
                return attributes;
            }
        }


        /// <summary>
        /// Registers an attribute for the specified member. Restriction: Attributes registered this way cannot be listed in inherited attributes.
        /// </summary>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="attribute">The attribute.</param>
        public void Register(MemberInfo memberInfo, Attribute attribute)
        {
            lock (globalLock)
            {
                // Use a cache of attributes
                List<Attribute> attributes;

                if (!cachedAttributes.TryGetValue(new MemberInfoKey(memberInfo, false), out attributes))
                {
                    if (!registeredAttributes.TryGetValue(memberInfo, out attributes))
                    {
                        attributes = new List<Attribute>();
                        registeredAttributes.Add(memberInfo, attributes);
                    }
                }

                attributes.Add(attribute);
            }
        }

        private struct MemberInfoKey : IEquatable<MemberInfoKey>
        {
            private readonly MemberInfo memberInfo;

            private readonly bool inherit;

            public MemberInfoKey(MemberInfo memberInfo, bool inherit)
            {
                this.memberInfo = memberInfo;
                this.inherit = inherit;
            }

            public bool Equals(MemberInfoKey other)
            {
                return memberInfo.Equals(other.memberInfo) && inherit.Equals(other.inherit);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                return obj is MemberInfoKey && Equals((MemberInfoKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (memberInfo.GetHashCode()*397) ^ inherit.GetHashCode();
                }
            }
        }
    }
}