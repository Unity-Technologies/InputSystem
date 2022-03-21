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
using System.Reflection;

namespace SharpYaml.Serialization.Descriptors
{
    /// <summary>
    /// Describes a descriptor for a primitive (bool, char, sbyte, byte, int, uint, long, ulong, float, double, decimal, string, DateTime).
    /// </summary>
    public class PrimitiveDescriptor : ObjectDescriptor
    {
        private static readonly List<IMemberDescriptor> EmptyMembers = new List<IMemberDescriptor>();

        private readonly Dictionary<string, object> enumRemap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectDescriptor" /> class.
        /// </summary>
        /// <param name="attributeRegistry">The attribute registry.</param>
        /// <param name="type">The type.</param>
        /// <param name="namingConvention">The naming convention.</param>
        /// <exception cref="System.ArgumentException">Type [{0}] is not a primitive</exception>
        public PrimitiveDescriptor(IAttributeRegistry attributeRegistry, Type type, IMemberNamingConvention namingConvention)
            : base(attributeRegistry, type, false, false, namingConvention)
        {
            if (!IsPrimitive(type))
                throw new ArgumentException("Type [{0}] is not a primitive");

            // Handle remap for enum items
            if (type.GetTypeInfo().IsEnum)
            {
                foreach (var member in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    var attributes = attributeRegistry.GetAttributes(member);
                    foreach (var attribute in attributes)
                    {
                        var yamlRemap = attribute as YamlRemapAttribute;
                        if (yamlRemap != null)
                        {
                            if (enumRemap == null)
                            {
                                enumRemap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            }
                            enumRemap[yamlRemap.Name] = member.GetValue(null);
                        }
                    }
                }
            }
        }

        public override DescriptorCategory Category { get { return DescriptorCategory.Primitive; } }

        /// <summary>
        /// Determines whether the specified type is a primitive.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is primitive; otherwise, <c>false</c>.</returns>
        public static bool IsPrimitive(Type type)
        {
            switch (type.GetTypeCode())
            {
                case TypeCode.Object:
                case TypeCode.Empty:
                    return type == typeof(object) || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(DateTimeOffset);
            }
            return true;
        }

        /// <summary>
        /// Parses the enum and trying to use remap if any declared.
        /// </summary>
        /// <param name="enumAsText">The enum as text.</param>
        /// <param name="remapped">if set to <c>true</c> the enum was remapped.</param>
        /// <returns>System.Object.</returns>
        public object ParseEnum(string enumAsText, out bool remapped)
        {
            object value;
            remapped = false;
            if (enumRemap != null && enumRemap.TryGetValue(enumAsText, out value))
            {
                remapped = true;
                return value;
            }

            return Enum.Parse(Type, enumAsText, true);
        }

        protected override System.Collections.Generic.List<IMemberDescriptor> PrepareMembers()
        {
            return EmptyMembers;
        }
    }
}