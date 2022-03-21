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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpYaml.Serialization.Descriptors
{
    /// <summary>
    /// Default implementation of a <see cref="ITypeDescriptor"/>.
    /// </summary>
    public class ObjectDescriptor : ITypeDescriptor
    {
        public static readonly Func<object, bool> ShouldSerializeDefault = o => true;

        protected static readonly string SystemCollectionsNamespace = typeof(int).Namespace;

        private readonly static object[] EmptyObjectArray = new object[0];
        private readonly Type type;
        private List<IMemberDescriptor> members;
        private Dictionary<string, IMemberDescriptor> mapMembers;
        private readonly bool emitDefaultValues;
        private readonly bool respectPrivateSetters;
        private YamlStyle style;
        private bool isSorted;
        private readonly IMemberNamingConvention memberNamingConvention;
        private HashSet<string> remapMembers;
        private List<Attribute> attributes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectDescriptor" /> class.
        /// </summary>
        /// <param name="attributeRegistry">The attribute registry.</param>
        /// <param name="type">The type.</param>
        /// <param name="emitDefaultValues">if set to <c>true</c> [emit default values].</param>
        /// <param name="respectPrivateSetters">If set to <c>true</c> will de/serialize properties with private setters.</param>
        /// <param name="namingConvention">The naming convention.</param>
        /// <exception cref="System.ArgumentNullException">type</exception>
        /// <exception cref="YamlException">type</exception>
        public ObjectDescriptor(IAttributeRegistry attributeRegistry, Type type, bool emitDefaultValues, bool respectPrivateSetters, IMemberNamingConvention namingConvention)
        {
            if (attributeRegistry == null)
                throw new ArgumentNullException("attributeRegistry");
            if (type == null)
                throw new ArgumentNullException("type");
            if (namingConvention == null)
                throw new ArgumentNullException("namingConvention");

            this.memberNamingConvention = namingConvention;
            this.emitDefaultValues = emitDefaultValues;
            this.respectPrivateSetters = respectPrivateSetters;
            this.AttributeRegistry = attributeRegistry;
            this.type = type;

            attributes = AttributeRegistry.GetAttributes(type.GetTypeInfo());

            this.style = YamlStyle.Any;
            foreach (var attribute in attributes)
            {
                var styleAttribute = attribute as YamlStyleAttribute;
                if (styleAttribute != null)
                {
                    style = styleAttribute.Style;
                    continue;
                }
                if (attribute is CompilerGeneratedAttribute)
                {
                    this.IsCompilerGenerated = true;
                }
            }
        }

        /// <summary>
        /// Gets attributes attached to this type.
        /// </summary>
        public List<Attribute> Attributes { get { return attributes; } }

        /// <summary>
        /// Gets the naming convention.
        /// </summary>
        /// <value>The naming convention.</value>
        public IMemberNamingConvention NamingConvention { get { return memberNamingConvention; } }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <exception cref="YamlException">Failed to get ObjectDescriptor for type [{0}]. The member [{1}] cannot be registered as a member with the same name is already registered [{2}].DoFormat(type.FullName, member, existingMember)</exception>
        public virtual void Initialize()
        {
            if (members != null)
            {
                return;
            }

            members = PrepareMembers();

            // If no members found, we don't need to build a dictionary map
            if (members.Count <= 0)
                return;

            mapMembers = new Dictionary<string, IMemberDescriptor>((int) (members.Count*1.2));

            foreach (var member in members)
            {
                IMemberDescriptor existingMember;
                if (mapMembers.TryGetValue(member.Name, out existingMember))
                {
                    throw new YamlException("Failed to get ObjectDescriptor for type [{0}]. The member [{1}] cannot be registered as a member with the same name is already registered [{2}]".DoFormat(type.FullName, member, existingMember));
                }

                mapMembers.Add(member.Name, member);

                // If there is any alternative names, register them
                if (member.AlternativeNames != null)
                {
                    foreach (var alternateName in member.AlternativeNames)
                    {
                        if (mapMembers.TryGetValue(alternateName, out existingMember))
                        {
                            throw new YamlException("Failed to get ObjectDescriptor for type [{0}]. The member [{1}] cannot be registered as a member with the same name [{2}] is already registered [{3}]".DoFormat(type.FullName, member, alternateName, existingMember));
                        }
                        else
                        {
                            if (remapMembers == null)
                            {
                                remapMembers = new HashSet<string>();
                            }

                            mapMembers[alternateName] = member;
                            remapMembers.Add(alternateName);
                        }
                    }
                }
            }
        }

        protected IAttributeRegistry AttributeRegistry { get; private set; }

        public Type Type { get { return type; } }

        public IEnumerable<IMemberDescriptor> Members { get { return members; } }

        public int Count { get { return members == null ? 0 : members.Count; } }

        public virtual DescriptorCategory Category { get { return DescriptorCategory.Object; } }

        public bool HasMembers { get { return members.Count > 0; } }

        public YamlStyle Style { get { return style; } }

        /// <summary>
        /// Sorts the members of this instance with the specified instance.
        /// </summary>
        /// <param name="keyComparer">The key comparer.</param>
        public void SortMembers(IComparer<object> keyComparer)
        {
            if (keyComparer != null && !isSorted)
            {
                members.Sort(keyComparer.Compare);
                isSorted = true;
            }
        }

        public IMemberDescriptor this[string name]
        {
            get
            {
                if (mapMembers == null)
                    throw new KeyNotFoundException(name);
                IMemberDescriptor member;
                mapMembers.TryGetValue(name, out member);
                return member;
            }
        }

        public bool IsMemberRemapped(string name)
        {
            return remapMembers != null && remapMembers.Contains(name);
        }

        public bool IsCompilerGenerated { get; private set; }

        public bool Contains(string memberName)
        {
            return mapMembers != null && mapMembers.ContainsKey(memberName);
        }

        protected virtual List<IMemberDescriptor> PrepareMembers()
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (Category == DescriptorCategory.Object)
                bindingFlags |= BindingFlags.NonPublic;

            // Add all public properties with a readable get method
            var memberList = (from propertyInfo in type.GetProperties(bindingFlags)
                where
                    propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0
                select new PropertyDescriptor(propertyInfo, NamingConvention.Comparer, respectPrivateSetters)
                into member
                where PrepareMember(member)
                select member).Cast<IMemberDescriptor>().ToList();

            // Add all public fields
            foreach (var item in (from fieldInfo in type.GetFields(bindingFlags)
                select new FieldDescriptor(fieldInfo, NamingConvention.Comparer)
                into member
                where PrepareMember(member)
                select member))
            {
                memberList.Add(item);
            }

            // Allow to add dynamic members per type
            if (AttributeRegistry.PrepareMembersCallback != null)
            {
                AttributeRegistry.PrepareMembersCallback(this, memberList);
            }

            return memberList;
        }

        protected virtual bool PrepareMember(MemberDescriptorBase member)
        {
            var memberType = member.Type;

            // Remove all SyncRoot from members
            if (member is PropertyDescriptor && member.OriginalName == "SyncRoot" &&
                (member.DeclaringType.Namespace ?? string.Empty).StartsWith(SystemCollectionsNamespace))
            {
                return false;
            }

            // Process all attributes just once instead of getting them one by one
            var attributes = AttributeRegistry.GetAttributes(member.MemberInfo);
            YamlStyleAttribute styleAttribute = null;
            YamlMemberAttribute memberAttribute = null;
            DefaultValueAttribute defaultValueAttribute = null;
            foreach (var attribute in attributes)
            {
                // Member is not displayed if there is a YamlIgnore attribute on it
                if (attribute is YamlIgnoreAttribute)
                {
                    return false;
                }

                if (attribute is YamlMemberAttribute)
                {
                    memberAttribute = (YamlMemberAttribute) attribute;
                    continue;
                }

                if (attribute is DefaultValueAttribute)
                {
                    defaultValueAttribute = (DefaultValueAttribute) attribute;
                    continue;
                }

                if (attribute is YamlStyleAttribute)
                {
                    styleAttribute = (YamlStyleAttribute) attribute;
                    continue;
                }

                var yamlRemap = attribute as YamlRemapAttribute;
                if (yamlRemap != null)
                {
                    if (member.AlternativeNames == null)
                    {
                        member.AlternativeNames = new List<string>();
                    }
                    if (!string.IsNullOrEmpty(yamlRemap.Name))
                    {
                        member.AlternativeNames.Add(yamlRemap.Name);
                    }
                }
            }

            // If the member has a set, this is a conventional assign method
            if (member.HasSet)
            {
                member.SerializeMemberMode = SerializeMemberMode.Content;
            }
            else
            {
                // Else we cannot only assign its content if it is a class
                member.SerializeMemberMode = (memberType != typeof(string) && memberType.GetTypeInfo().IsClass) || memberType.GetTypeInfo().IsInterface || type.IsAnonymous() ? SerializeMemberMode.Content : SerializeMemberMode.Never;
            }

            // If it's a private member, check it has a YamlMemberAttribute on it
            if (!member.IsPublic)
            {
                if (memberAttribute == null)
                    return false;
            }

            // Gets the style
            member.Style = styleAttribute != null ? styleAttribute.Style : YamlStyle.Any;
            member.Mask = 1;

            // Handle member attribute
            if (memberAttribute != null)
            {
                member.Mask = memberAttribute.Mask;
                if (!member.HasSet)
                {
                    if (memberAttribute.SerializeMethod == SerializeMemberMode.Assign ||
                        (memberType.GetTypeInfo().IsValueType && member.SerializeMemberMode == SerializeMemberMode.Content))
                        throw new ArgumentException("{0} {1} is not writeable by {2}.".DoFormat(memberType.FullName, member.OriginalName, memberAttribute.SerializeMethod.ToString()));
                }

                if (memberAttribute.SerializeMethod != SerializeMemberMode.Default)
                {
                    member.SerializeMemberMode = memberAttribute.SerializeMethod;
                }
                member.Order = memberAttribute.Order;
            }

            if (member.SerializeMemberMode == SerializeMemberMode.Binary)
            {
                if (!memberType.IsArray)
                    throw new InvalidOperationException("{0} {1} of {2} is not an array. Can not be serialized as binary."
                        .DoFormat(memberType.FullName, member.OriginalName, type.FullName));
                if (!memberType.GetElementType().IsPureValueType())
                    throw new InvalidOperationException("{0} is not a pure ValueType. {1} {2} of {3} can not serialize as binary.".DoFormat(memberType.GetElementType(), memberType.FullName, member.OriginalName, type.FullName));
            }

            // If this member cannot be serialized, remove it from the list
            if (member.SerializeMemberMode == SerializeMemberMode.Never)
            {
                return false;
            }

            // ShouldSerialize
            //	  YamlSerializeAttribute(Never) => false
            //	  ShouldSerializeSomeProperty => call it
            //	  DefaultValueAttribute(default) => compare to it
            //	  otherwise => true
            var shouldSerialize = type.GetMethod("ShouldSerialize" + member.OriginalName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (shouldSerialize != null && shouldSerialize.ReturnType == typeof(bool) && member.ShouldSerialize == null)
                member.ShouldSerialize = obj => (bool) shouldSerialize.Invoke(obj, EmptyObjectArray);

            if (defaultValueAttribute != null && member.ShouldSerialize == null && !emitDefaultValues)
            {
                object defaultValue = defaultValueAttribute.Value;
                Type defaultType = defaultValue == null ? null : defaultValue.GetType();
                if (defaultType.IsNumeric() && defaultType != memberType)
                    defaultValue = memberType.CastToNumericType(defaultValue);
                member.ShouldSerialize = obj => !TypeExtensions.AreEqual(defaultValue, member.Get(obj));
            }

            if (member.ShouldSerialize == null)
                member.ShouldSerialize = ShouldSerializeDefault;

            if (memberAttribute != null && !string.IsNullOrEmpty(memberAttribute.Name))
            {
                member.Name = memberAttribute.Name;
            }
            else
            {
                member.Name = NamingConvention.Convert(member.OriginalName);
            }

            return true;
        }

        public override string ToString()
        {
            return type.ToString();
        }
    }
}