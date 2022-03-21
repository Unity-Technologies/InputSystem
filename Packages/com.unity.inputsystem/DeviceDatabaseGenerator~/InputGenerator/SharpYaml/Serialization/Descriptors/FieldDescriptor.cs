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
using System.Reflection;

namespace SharpYaml.Serialization.Descriptors
{
    /// <summary>
    /// A <see cref="IMemberDescriptor"/> for a <see cref="FieldInfo"/>
    /// </summary>
    public class FieldDescriptor : MemberDescriptorBase
    {
        private readonly FieldInfo fieldInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldDescriptor" /> class.
        /// </summary>
        /// <param name="fieldInfo">The property information.</param>
        /// <param name="defaultNameComparer">The default name comparer.</param>
        /// <exception cref="System.ArgumentNullException">fieldInfo</exception>
        public FieldDescriptor(FieldInfo fieldInfo, StringComparer defaultNameComparer)
            : base(fieldInfo, defaultNameComparer)
        {
            if (fieldInfo == null)
                throw new ArgumentNullException("fieldInfo");

            this.fieldInfo = fieldInfo;
        }

        /// <summary>
        /// Gets the property information attached to this instance.
        /// </summary>
        /// <value>The property information.</value>
        public FieldInfo FieldInfo { get { return fieldInfo; } }

        public override Type Type { get { return fieldInfo.FieldType; } }

        public override object Get(object thisObject)
        {
            return fieldInfo.GetValue(thisObject);
        }

        public override void Set(object thisObject, object value)
        {
            fieldInfo.SetValue(thisObject, value);
        }

        public override bool HasSet { get { return true; } }

        public override bool IsPublic { get { return fieldInfo.IsPublic; } }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Format("Field [{0}] from Type [{1}]", OriginalName, FieldInfo.DeclaringType != null ? FieldInfo.DeclaringType.FullName : string.Empty);
        }
    }
}