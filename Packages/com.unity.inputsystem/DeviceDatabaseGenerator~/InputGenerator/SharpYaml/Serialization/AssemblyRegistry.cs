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
using SharpYaml.Schemas;

namespace SharpYaml.Serialization
{
    /// <summary>
    /// Default implementation of ITagTypeRegistry.
    /// </summary>
    internal class AssemblyRegistry : ITagTypeRegistry
    {
        private readonly IYamlSchema schema;
        private readonly Dictionary<string, MappedType> tagToType;
        private readonly Dictionary<Type, string> typeToTag;
        private readonly List<Assembly> lookupAssemblies;
        private readonly object lockCache = new object();

        private static readonly List<Assembly> DefaultLookupAssemblies = new List<Assembly>()
        {
            typeof(int).GetTypeInfo().Assembly,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyRegistry"/> class.
        /// </summary>
        public AssemblyRegistry(IYamlSchema schema)
        {
            if (schema == null)
                throw new ArgumentNullException("schema");
            this.schema = schema;
            tagToType = new Dictionary<string, MappedType>();
            typeToTag = new Dictionary<Type, string>();
            lookupAssemblies = new List<Assembly>();
            SerializableFactories = new List<IYamlSerializableFactory>();
        }

        /// <summary>
        /// Gets the serializable factories.
        /// </summary>
        /// <value>The serializable factories.</value>
        public List<IYamlSerializableFactory> SerializableFactories { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use short type name].
        /// </summary>
        /// <value><c>true</c> if [use short type name]; otherwise, <c>false</c>.</value>
        public bool UseShortTypeName { get; set; }

        public void RegisterAssembly(Assembly assembly, IAttributeRegistry attributeRegistry)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");
            if (attributeRegistry == null)
                throw new ArgumentNullException("attributeRegistry");

            // Add automatically the assembly for lookup
            if (!DefaultLookupAssemblies.Contains(assembly) && !lookupAssemblies.Contains(assembly))
            {
                lookupAssemblies.Add(assembly);

                var types = new Type[0];

                // Register all tags automatically.
                foreach (var type in assembly.GetTypes())
                {
                    var attributes = attributeRegistry.GetAttributes(type.GetTypeInfo());
                    foreach (var attribute in attributes)
                    {
                        string name = null;
                        bool isAlias = false;
                        var tagAttribute = attribute as YamlTagAttribute;
                        if (tagAttribute != null)
                        {
                            name = tagAttribute.Tag;
                        }
                        else
                        {
                            var yamlRemap = attribute as YamlRemapAttribute;
                            if (yamlRemap != null)
                            {
                                name = yamlRemap.Name;
                                isAlias = true;
                            }
                        }
                        if (!string.IsNullOrEmpty(name))
                        {
                            RegisterTagMapping(name, type, isAlias);
                        }
                    }

                    // Automatically register YamlSerializableFactory
                    if (typeof(IYamlSerializableFactory).IsAssignableFrom(type) && type.GetConstructor(types) != null)
                    {
                        try
                        {
                            SerializableFactories.Add((IYamlSerializableFactory) Activator.CreateInstance(type));
                        }
                        catch
                        {
                            // Registrying an assembly should not fail, so we are silently discarding a factory if 
                            // we are not able to load it.
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Register a mapping between a tag and a type.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="type">The type.</param>
        /// <param name="alias"></param>
        public virtual void RegisterTagMapping(string tag, Type type, bool alias)
        {
            if (tag == null)
                throw new ArgumentNullException("tag");
            if (type == null)
                throw new ArgumentNullException("type");

            // Prefix all tags by !
            tag = Uri.EscapeUriString(tag);
            if (tag.StartsWith("tag:"))
            {
                // shorten tag
                // TODO this is not really failsafe
                var shortTag = "!!" + tag.Substring(tag.LastIndexOf(':') + 1);

                // Auto register tag to schema
                schema.RegisterTag(shortTag, tag);
                tag = shortTag;
            }

            tag = tag.StartsWith("!") ? tag : "!" + tag;

            lock (lockCache)
            {
                tagToType[tag] = new MappedType(type, alias);

                // Only register types that are not aliases
                if (!alias)
                    typeToTag[type] = tag;
            }
        }

        public virtual Type TypeFromTag(string tag, out bool isAlias)
        {
            isAlias = false;

            if (tag == null)
            {
                return null;
            }

            // Get the default schema type if there is any
            var shortTag = schema.ShortenTag(tag);
            Type type;
            if (shortTag != tag || shortTag.StartsWith("!!"))
            {
                type = schema.GetTypeForDefaultTag(shortTag);
                if (type != null)
                {
                    return type;
                }
            }

            // un-escape tag
            shortTag = Uri.UnescapeDataString(shortTag);

            lock (lockCache)
            {
                MappedType mappedType;
                // Else try to find a registered alias
                if (tagToType.TryGetValue(shortTag, out mappedType))
                {
                    isAlias = mappedType.Remapped;
                    return mappedType.Type;
                }

                // Else resolve type from assembly
                var tagAsType = shortTag.StartsWith("!") ? shortTag.Substring(1) : shortTag;

                // Try to resolve the type from registered assemblies
                type = ResolveType(tagAsType);

                // Register a type that was found
                tagToType.Add(shortTag, new MappedType(type, false));
                if (type != null && !typeToTag.ContainsKey(type))
                {
                    typeToTag.Add(type, shortTag);
                }
            }

            return type;
        }

        public virtual string TagFromType(Type type)
        {
            if (type == null)
            {
                return "!!null";
            }

            string tagName;

            lock (lockCache)
            {
                // First try to resolve a tag from registered tag
                if (!typeToTag.TryGetValue(type, out tagName))
                {
                    // Else try to use schema tag for scalars
                    // Else use full name of the type
                    var typeName = UseShortTypeName ? type.GetShortAssemblyQualifiedName() : type.AssemblyQualifiedName;
                    tagName = schema.GetDefaultTag(type) ?? Uri.EscapeUriString(string.Format("!{0}", typeName));
                    typeToTag.Add(type, tagName);
                }
            }

            return tagName;
        }

        public virtual Type ResolveType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null)
            {
                string assemblyName = null;

                // Find assembly name start (skip up to one space if needed)
                // We ignore everything else (version, publickeytoken, etc...)
                if (UseShortTypeName)
                {
                    var typeNameEnd = typeName.IndexOf(',');
                    var assemblyNameStart = typeNameEnd;
                    if (assemblyNameStart != -1 && typeName[++assemblyNameStart] == ' ') // Skip first comma and check if we have a space
                        assemblyNameStart++; // Skip first space

                    // Extract assemblyName and readjust typeName to not include assemblyName anymore
                    if (assemblyNameStart != -1)
                    {
                        var assemblyNameEnd = typeName.IndexOf(',', assemblyNameStart);
                        assemblyName = assemblyNameEnd != -1
                            ? typeName.Substring(assemblyNameStart, assemblyNameEnd - assemblyNameStart)
                            : typeName.Substring(assemblyNameStart);

                        typeName = typeName.Substring(0, typeNameEnd);
                    }
                }

                // Look for type in loaded assemblies
                foreach (var assembly in lookupAssemblies)
                {
                    if (assemblyName != null)
                    {
                        // Check that assembly name match, by comparing up to the first comma
                        var assemblyFullName = assembly.FullName;
                        if (string.Compare(assemblyFullName, 0, assemblyName, 0, assemblyName.Length) != 0
                            || !(assemblyFullName.Length == assemblyName.Length || assemblyFullName[assemblyName.Length] == ','))
                        {
                            continue;
                        }
                    }

                    type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        break;
                    }
                }

                // No type found, let's try again ignoring assembly name (in case a type moved)
                if (type == null && assemblyName != null)
                {
                    foreach (var assembly in lookupAssemblies)
                    {
                        type = assembly.GetType(typeName);
                        if (type != null)
                        {
                            break;
                        }
                    }
                }
            }
            return type;
        }

        struct MappedType
        {
            public MappedType(Type type, bool remapped)
            {
                Type = type;
                Remapped = remapped;
            }

            public readonly Type Type;

            public readonly bool Remapped;
        }
    }
}