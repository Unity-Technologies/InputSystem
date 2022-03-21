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
using SharpYaml.Events;
using SharpYaml.Schemas;
using SharpYaml.Serialization.Descriptors;

namespace SharpYaml.Serialization
{
    /// <summary>
    /// A context used while deserializing.
    /// </summary>
    public class SerializerContext : ITagTypeResolver
    {
        private readonly SerializerSettings settings;
        private readonly ITagTypeRegistry tagTypeRegistry;
        private readonly ITypeDescriptorFactory typeDescriptorFactory;
        private IEmitter emitter;
        private readonly SerializerContextSettings contextSettings;
        internal int AnchorCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerContext"/> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializerContextSettings">The serializer context settings.</param>
        internal SerializerContext(Serializer serializer, SerializerContextSettings serializerContextSettings)
        {
            Serializer = serializer;
            settings = serializer.Settings;
            tagTypeRegistry = settings.AssemblyRegistry;
            ObjectFactory = settings.ObjectFactory;
            ObjectSerializerBackend = settings.ObjectSerializerBackend;
            Schema = Settings.Schema;
            ObjectSerializer = serializer.ObjectSerializer;
            typeDescriptorFactory = serializer.TypeDescriptorFactory;
            contextSettings = serializerContextSettings ?? SerializerContextSettings.Default;
        }

        /// <summary>
        /// Gets a value indicating whether we are in the context of serializing.
        /// </summary>
        /// <value><c>true</c> if we are in the context of serializing; otherwise, <c>false</c>.</value>
        public bool IsSerializing { get { return Writer != null; } }

        /// <summary>
        /// Gets the context settings.
        /// </summary>
        /// <value>
        /// The context settings.
        /// </value>
        public SerializerContextSettings ContextSettings { get { return contextSettings; } }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>The settings.</value>
        public SerializerSettings Settings { get { return settings; } }

        /// <summary>
        /// Gets the schema.
        /// </summary>
        /// <value>The schema.</value>
        public IYamlSchema Schema { get; private set; }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        /// <value>The serializer.</value>
        public Serializer Serializer { get; private set; }

        /// <summary>
        /// Gets or sets the reader used while deserializing.
        /// </summary>
        /// <value>The reader.</value>
        public EventReader Reader { get; set; }

        /// <summary>
        /// Gets the object serializer backend.
        /// </summary>
        /// <value>The object serializer backend.</value>
        public IObjectSerializerBackend ObjectSerializerBackend { get; private set; }

        private IYamlSerializable ObjectSerializer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether errors are allowed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if errors are allowed; otherwise, <c>false</c>.
        /// </value>
        public bool AllowErrors { get; set; }

        /// <summary>
        /// Gets a value indicating whether the deserialization has generated some remap.
        /// </summary>
        /// <value><c>true</c> if the deserialization has generated some remap; otherwise, <c>false</c>.</value>
        public bool HasRemapOccurred { get; internal set; }

        /// <summary>
        /// Gets or sets the member mask that will be used to filter <see cref="YamlMemberAttribute.Mask"/>.
        /// </summary>
        /// <value>
        /// The member mask.
        /// </value>
        public uint MemberMask { get { return contextSettings.MemberMask; } }

        /// <summary>
        /// The default function to read an object from the current Yaml stream.
        /// </summary>
        /// <param name="value">The value of the receiving object, may be null.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <returns>System.Object.</returns>
        public object ReadYaml(object value, Type expectedType)
        {
            var node = Reader.Parser.Current;
            try
            {
                var objectContext = new ObjectContext(this, value, FindTypeDescriptor(expectedType));
                return ObjectSerializer.ReadYaml(ref objectContext);
            }
            catch (YamlException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new YamlException(node.Start, node.End, "Error while deserializing node [{0}]".DoFormat(node), ex);
            }
        }

        /// <summary>
        /// Gets or sets the type of the create.
        /// </summary>
        /// <value>The type of the create.</value>
        public IObjectFactory ObjectFactory { get; set; }

        /// <summary>
        /// Gets or sets the writer used while deserializing.
        /// </summary>
        /// <value>The writer.</value>
        public IEventEmitter Writer { get; set; }

        /// <summary>
        /// Gets the emitter.
        /// </summary>
        /// <value>The emitter.</value>
        public IEmitter Emitter { get { return emitter; } internal set { emitter = value; } }

        /// <summary>
        /// The default function to write an object to Yaml
        /// </summary>
        public void WriteYaml(object value, Type expectedType, YamlStyle style = YamlStyle.Any)
        {
            var objectContext = new ObjectContext(this, value, FindTypeDescriptor(expectedType)) {Style = style};
            ObjectSerializer.WriteYaml(ref objectContext);
        }

        /// <summary>
        /// Finds the type descriptor for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>An instance of <see cref="ITypeDescriptor"/>.</returns>
        public ITypeDescriptor FindTypeDescriptor(Type type)
        {
            return typeDescriptorFactory.Find(type, Settings.ComparerForKeySorting);
        }

        /// <summary>
        /// Resolves a type from the specified tag.
        /// </summary>
        /// <param name="tagName">Name of the tag.</param>
        /// <param name="isAlias"></param>
        /// <returns>Type.</returns>
        public Type TypeFromTag(string tagName, out bool isAlias)
        {
            return tagTypeRegistry.TypeFromTag(tagName, out isAlias);
        }

        /// <summary>
        /// Resolves a tag from a type
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The associated tag</returns>
        public string TagFromType(Type type)
        {
            return tagTypeRegistry.TagFromType(type);
        }

        /// <summary>
        /// Resolves a type from the specified typename using registered assemblies.
        /// </summary>
        /// <param name="typeFullName">Full name of the type.</param>
        /// <returns>The type of null if not found</returns>
        public Type ResolveType(string typeFullName)
        {
            return tagTypeRegistry.ResolveType(typeFullName);
        }

        /// <summary>
        /// Gets the default tag and value for the specified <see cref="Scalar" />. The default tag can be different from a actual tag of this <see cref="NodeEvent" />.
        /// </summary>
        /// <param name="scalar">The scalar event.</param>
        /// <param name="defaultTag">The default tag decoded from the scalar.</param>
        /// <param name="value">The value extracted from a scalar.</param>
        /// <returns>System.String.</returns>
        public bool TryParseScalar(Scalar scalar, out string defaultTag, out object value)
        {
            return Settings.Schema.TryParse(scalar, true, out defaultTag, out value);
        }
    }
}