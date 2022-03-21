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
    /// Base class for <see cref="IMemberDescriptor"/> for a <see cref="MemberInfo"/>
    /// </summary>
    public abstract class MemberDescriptorBase : IMemberDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberDescriptorBase"/> class.
        /// </summary>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="defaultNameComparer">The default name comparer.</param>
        /// <exception cref="System.ArgumentNullException">memberInfo</exception>
        protected MemberDescriptorBase(MemberInfo memberInfo, StringComparer defaultNameComparer)
        {
            if (memberInfo == null)
                throw new ArgumentNullException("memberInfo");
            if (defaultNameComparer == null)
                throw new ArgumentNullException("defaultNameComparer");

            MemberInfo = memberInfo;
            Name = MemberInfo.Name;
            OriginalName = Name;
            DeclaringType = memberInfo.DeclaringType;
            DefaultNameComparer = defaultNameComparer;
        }

        public string Name { get; internal set; }
        public string OriginalName { get; private set; }
        public StringComparer DefaultNameComparer { get; private set; }
        public abstract Type Type { get; }
        public int? Order { get; internal set; }

        /// <summary>
        /// Gets the type of the declaring this member.
        /// </summary>
        /// <value>The type of the declaring.</value>
        public Type DeclaringType { get; private set; }

        public SerializeMemberMode SerializeMemberMode { get; internal set; }
        public abstract object Get(object thisObject);
        public abstract void Set(object thisObject, object value);
        public abstract bool HasSet { get; }
        public abstract bool IsPublic { get; }
        public uint Mask { get; internal set; }
        public YamlStyle Style { get; internal set; }
        public Func<object, bool> ShouldSerialize { get; internal set; }

        public List<string> AlternativeNames { get; set; }

        public object Tag { get; set; }

        /// <summary>
        /// Gets the member information.
        /// </summary>
        /// <value>The member information.</value>
        public MemberInfo MemberInfo { get; private set; }
    }
}