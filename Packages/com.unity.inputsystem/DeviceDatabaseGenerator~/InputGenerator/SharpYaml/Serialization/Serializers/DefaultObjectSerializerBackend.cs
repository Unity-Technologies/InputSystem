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

namespace SharpYaml.Serialization.Serializers
{
    /// <summary>
    /// Default implementation for <see cref="IObjectSerializerBackend"/>
    /// </summary>
    public class DefaultObjectSerializerBackend : IObjectSerializerBackend
    {
        public virtual YamlStyle GetStyle(ref ObjectContext objectContext)
        {
            var context = objectContext.SerializerContext;

            // Resolve the style, use default style if not defined.
            // First pop style of current member being serialized.
            var style = objectContext.Style;

            // If no style yet defined
            if (style != YamlStyle.Any)
            {
                return style;
            }

            // Try to get the style from this serializer
            style = objectContext.Descriptor.Style;

            // In case of any style, allow to emit a flow sequence depending on Settings LimitPrimitiveFlowSequence.
            // Apply this only for primitives
            if (style == YamlStyle.Any)
            {
                bool isPrimitiveElementType = false;
                var collectionDescriptor = objectContext.Descriptor as CollectionDescriptor;
                int count = 0;
                if (collectionDescriptor != null)
                {
                    isPrimitiveElementType = PrimitiveDescriptor.IsPrimitive(collectionDescriptor.ElementType);
                    count = collectionDescriptor.GetCollectionCount(objectContext.Instance);
                }
                else
                {
                    var arrayDescriptor = objectContext.Descriptor as ArrayDescriptor;
                    if (arrayDescriptor != null)
                    {
                        isPrimitiveElementType = PrimitiveDescriptor.IsPrimitive(arrayDescriptor.ElementType);
                        count = objectContext.Instance != null ? ((Array) objectContext.Instance).Length : -1;
                    }
                }

                if (isPrimitiveElementType)
                {
                    style = objectContext.Instance == null || count >= objectContext.SerializerContext.Settings.LimitPrimitiveFlowSequence
                        ? YamlStyle.Block
                        : YamlStyle.Flow;
                }
            }

            // If not defined, get the default style
            if (style == YamlStyle.Any)
            {
                style = context.Settings.DefaultStyle;

                // If default style is set to Any, set it to Block by default.
                if (style == YamlStyle.Any)
                {
                    style = YamlStyle.Block;
                }
            }

            return style;
        }

        public virtual string ReadMemberName(ref ObjectContext objectContext, string memberName, out bool skipMember)
        {
            skipMember = false;
            return memberName;
        }

        public virtual object ReadMemberValue(ref ObjectContext objectContext, IMemberDescriptor memberDescriptor, object memberValue,
            Type memberType)
        {
            return objectContext.SerializerContext.ReadYaml(memberValue, memberType);
        }

        public virtual object ReadCollectionItem(ref ObjectContext objectContext, object value, Type itemType, int index)
        {
            return objectContext.SerializerContext.ReadYaml(value, itemType);
        }

        public virtual KeyValuePair<object, object> ReadDictionaryItem(ref ObjectContext objectContext, KeyValuePair<Type, Type> keyValueType)
        {
            var keyResult = objectContext.SerializerContext.ReadYaml(null, keyValueType.Key);
            var valueResult = objectContext.SerializerContext.ReadYaml(null, keyValueType.Value);

            return new KeyValuePair<object, object>(keyResult, valueResult);
        }

        public virtual void WriteMemberName(ref ObjectContext objectContext, IMemberDescriptor member, string name)
        {
            // Emit the key name
            objectContext.Writer.Emit(new ScalarEventInfo(name, typeof(string))
            {
                RenderedValue = name,
                IsPlainImplicit = true,
                Style = ScalarStyle.Plain
            });
        }

        public virtual void WriteMemberValue(ref ObjectContext objectContext, IMemberDescriptor member, object memberValue,
            Type memberType)
        {
            // Push the style of the current member
            objectContext.SerializerContext.WriteYaml(memberValue, memberType, member.Style);
        }

        public virtual void WriteCollectionItem(ref ObjectContext objectContext, object item, Type itemType, int index)
        {
            objectContext.SerializerContext.WriteYaml(item, itemType);
        }

        public virtual void WriteDictionaryItem(ref ObjectContext objectContext, KeyValuePair<object, object> keyValue, KeyValuePair<Type, Type> types)
        {
            objectContext.SerializerContext.WriteYaml(keyValue.Key, types.Key);
            objectContext.SerializerContext.WriteYaml(keyValue.Value, types.Value);
        }
    }
}