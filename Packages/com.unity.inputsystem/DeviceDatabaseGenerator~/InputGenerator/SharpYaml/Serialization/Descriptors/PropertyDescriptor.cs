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
    /// A <see cref="IMemberDescriptor"/> for a <see cref="PropertyInfo"/>
    /// </summary>
    public class PropertyDescriptor : MemberDescriptorBase
    {
        private readonly PropertyInfo propertyInfo;
        private readonly MethodInfo getMethod;
        private readonly MethodInfo setMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDescriptor" /> class.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="defaultNameComparer">The default name comparer.</param>
        /// <param name="respectPrivateSetters"></param>
        /// <exception cref="System.ArgumentNullException">propertyInfo</exception>
        public PropertyDescriptor(PropertyInfo propertyInfo, StringComparer defaultNameComparer, bool respectPrivateSetters)
            : base(propertyInfo, defaultNameComparer)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");

            this.propertyInfo = propertyInfo;

            getMethod = propertyInfo.GetGetMethod(true);
            if (propertyInfo.CanWrite && propertyInfo.GetSetMethod(respectPrivateSetters || !IsPublic) != null)
            {
                setMethod = propertyInfo.GetSetMethod(respectPrivateSetters || !IsPublic);
            }
        }

        /// <summary>
        /// Gets the property information attached to this instance.
        /// </summary>
        /// <value>The property information.</value>
        public PropertyInfo PropertyInfo { get { return propertyInfo; } }

        public override Type Type { get { return propertyInfo.PropertyType; } }

        public override object Get(object thisObject)
        {
            return getMethod.Invoke(thisObject, null);
        }

        public override void Set(object thisObject, object value)
        {
            if (HasSet)
                setMethod.Invoke(thisObject, new[] {value});
        }

        public override bool HasSet { get { return setMethod != null; } }

        public override bool IsPublic { get { return getMethod.IsPublic; } }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Format("Property [{0}] from Type [{1}]", OriginalName, PropertyInfo.DeclaringType != null ? PropertyInfo.DeclaringType.FullName : string.Empty);
        }
    }
}