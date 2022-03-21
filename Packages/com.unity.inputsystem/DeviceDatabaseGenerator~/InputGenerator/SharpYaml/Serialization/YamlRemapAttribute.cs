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

namespace SharpYaml.Serialization
{
    /// <summary>
    /// Allows to re-map a previous name to the specified property/field/enum/class/struct.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class YamlRemapAttribute : Attribute
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlRemapAttribute"/> class.
        /// </summary>
        /// <param name="name">The previous name.</param>
        public YamlRemapAttribute(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Gets the previous name.
        /// </summary>
        /// <value>The previous name.</value>
        public string Name { get { return name; } }
    }
}