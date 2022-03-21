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

namespace SharpYaml.Serialization.Descriptors
{
    /// <summary>
    /// Default comparer used to sort keys for object members and dictionary keys. See remarks.
    /// </summary>
    /// <remarks><ul>
    ///   <li>For members of an object, this comparer will first try to use an explicit order defined by
    /// using <see cref="YamlMemberAttribute.Order" /> otherwise It will use the name of the field/property.
    ///   </li>
    ///   <li>If both objects are string, use <see cref="string.CompareOrdinal(string,int,string,int,int)"/></li>
    ///   <li>
    /// For keys of a dictionary, this comparer will try to use <see cref="IComparable" /> interface
    /// when comparing keys.
    ///   </li>
    ///   </ul></remarks>
    public class DefaultKeyComparer : IComparer<object>
    {
        public virtual int Compare(object x, object y)
        {
            var left = x as IMemberDescriptor;
            var right = y as IMemberDescriptor;
            if (left != null && right != null)
            {
                // If order is defined, first order by order
                if (left.Order.HasValue | right.Order.HasValue)
                {
                    var leftOrder = left.Order.HasValue ? left.Order.Value : int.MaxValue;
                    var rightOrder = right.Order.HasValue ? right.Order.Value : int.MaxValue;
                    return leftOrder.CompareTo(rightOrder);
                }

                // else order by name
                return left.DefaultNameComparer.Compare(left.Name, right.Name);
            }

            if (x is string && y is string)
            {
                return string.CompareOrdinal((string) x, (string) y);
            }

            var leftComparable = x as IComparable;
            if (leftComparable != null)
            {
                return leftComparable.CompareTo(y);
            }

            var rightComparable = y as IComparable;
            return rightComparable != null ? rightComparable.CompareTo(y) : 0;
        }
    }
}