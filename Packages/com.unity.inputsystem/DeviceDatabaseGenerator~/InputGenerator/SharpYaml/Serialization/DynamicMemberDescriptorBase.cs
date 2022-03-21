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

using System;
using System.Collections.Generic;
using SharpYaml.Serialization.Descriptors;

namespace SharpYaml.Serialization
{
    /// <summary>
    /// A dynamic member to allow to add dynamic members to objects (that could store additional properties outside of the instance).
    /// </summary>
    public abstract class DynamicMemberDescriptorBase : IMemberDescriptor
    {
        protected DynamicMemberDescriptorBase(string name, Type type)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (type == null)
                throw new ArgumentNullException("type");
            Name = name;
            Type = type;
            OriginalName = Name;
            Mask = 1;
            ShouldSerialize = ObjectDescriptor.ShouldSerializeDefault;
            DefaultNameComparer = StringComparer.OrdinalIgnoreCase;
        }

        public string Name { get; set; }

        public string OriginalName { get; set; }

        public StringComparer DefaultNameComparer { get; set; }

        public Type Type { get; set; }

        public int? Order { get; set; }

        public SerializeMemberMode SerializeMemberMode { get; set; }

        public abstract object Get(object thisObject);

        public abstract void Set(object thisObject, object value);

        public abstract bool HasSet { get; }

        public bool IsPublic { get { return true; } }

        public uint Mask { get; set; }

        public YamlStyle Style { get; set; }

        public Func<object, bool> ShouldSerialize { get; set; }

        public List<string> AlternativeNames { get; set; }

        public object Tag { get; set; }
    }
}