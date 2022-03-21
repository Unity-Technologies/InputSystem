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

namespace SharpYaml.Serialization
{
    /// <summary>
    /// Specify the way to store a property or field of some class or structure.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class YamlMemberAttribute : Attribute
    {
        public const uint DefaultMask = 1;

        private readonly SerializeMemberMode serializeMethod;
        private readonly string name;
        private uint mask = DefaultMask;

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlMemberAttribute"/> class.
        /// </summary>
        public YamlMemberAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlMemberAttribute"/> class.
        /// </summary>
        /// <param name="order">The order.</param>
        public YamlMemberAttribute(int order)
        {
            Order = order;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlMemberAttribute"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public YamlMemberAttribute(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Specify the way to store a property or field of some class or structure.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="serializeMethod">The serialize method.</param>
        public YamlMemberAttribute(string name, SerializeMemberMode serializeMethod)
        {
            this.name = name;
            this.serializeMethod = serializeMethod;
        }

        /// <summary>
        /// Specify the way to store a property or field of some class or structure.
        /// </summary>
        /// <param name="serializeMethod">The serialize method.</param>
        public YamlMemberAttribute(SerializeMemberMode serializeMethod)
        {
            this.serializeMethod = serializeMethod;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get { return name; } }

        /// <summary>
        /// Gets the serialize method1.
        /// </summary>
        /// <value>The serialize method1.</value>
        public SerializeMemberMode SerializeMethod { get { return serializeMethod; } }

        /// <summary>
        /// Gets or sets the order. Default is -1 (default to alphabetical)
        /// </summary>
        /// <value>The order.</value>
        public int? Order { get; set; }

        /// <summary>
        /// Gets the mask.
        /// </summary>
        /// <value>The mask.</value>
        public uint Mask { get { return mask; } set { mask = value; } }
    }
}