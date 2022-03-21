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

using System.Collections;
using System.Collections.Generic;
using SharpYaml.Events;
using SharpYaml.Serialization.Descriptors;

namespace SharpYaml.Serialization.Serializers
{
    internal class ArraySerializer : IYamlSerializable, IYamlSerializableFactory
    {
        public IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor is ArrayDescriptor ? this : null;
        }

        public virtual object ReadYaml(ref ObjectContext objectContext)
        {
            var reader = objectContext.Reader;
            var arrayDescriptor = (ArrayDescriptor) objectContext.Descriptor;

            bool isArray = objectContext.Instance != null && objectContext.Instance.GetType().IsArray;
            var arrayList = (IList) objectContext.Instance;

            reader.Expect<SequenceStart>();
            int index = 0;
            if (isArray)
            {
                while (!reader.Accept<SequenceEnd>())
                {
                    var node = reader.Peek<ParsingEvent>();
                    if (index >= arrayList.Count)
                    {
                        throw new YamlException(node.Start, node.End, "Unable to deserialize array. Current number of elements [{0}] exceeding array size [{1}]".DoFormat(index, arrayList.Count));
                    }

                    // Handle aliasing
                    arrayList[index++] = objectContext.SerializerContext.ReadYaml(null, arrayDescriptor.ElementType);
                }
            }
            else
            {
                var results = new List<object>();
                while (!reader.Accept<SequenceEnd>())
                {
                    results.Add(objectContext.SerializerContext.ReadYaml(null, arrayDescriptor.ElementType));
                }

                // Handle aliasing
                arrayList = arrayDescriptor.CreateArray(results.Count);
                foreach (var arrayItem in results)
                {
                    arrayList[index++] = arrayItem;
                }
            }
            reader.Expect<SequenceEnd>();

            return arrayList;
        }

        public void WriteYaml(ref ObjectContext objectContext)
        {
            var value = objectContext.Instance;
            var arrayDescriptor = (ArrayDescriptor) objectContext.Descriptor;

            var valueType = value.GetType();
            var arrayList = (IList) value;

            // Emit a Flow sequence or block sequence depending on settings 
            objectContext.Writer.Emit(new SequenceStartEventInfo(value, valueType)
            {
                Tag = objectContext.Tag,
                Style = objectContext.Style != YamlStyle.Any ? objectContext.Style : (arrayList.Count < objectContext.Settings.LimitPrimitiveFlowSequence ? YamlStyle.Flow : YamlStyle.Block)
            });

            foreach (var element in arrayList)
            {
                objectContext.SerializerContext.WriteYaml(element, arrayDescriptor.ElementType);
            }
            objectContext.Writer.Emit(new SequenceEndEventInfo(value, valueType));
        }
    }
}