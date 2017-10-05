using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Security;
using UnityEditor;
using UnityEngine;

namespace ISX
{
    // A template lays out the composition of an input control.
    //
    // Can be created in one of three ways:
    //  1) Manually in code through InputTemplateBuilder.
    //  2) Loaded from JSON.
    //  3) Constructed through reflection from InputControls classes.
    //
    // Once constructed, templates are immutable (but you can always
    // replace a registered template in the system and it will affect
    // everything constructed from the template).
    //
    // Templates can be for arbitrary control rigs or for entire
    // devices. Device templates can use the 'deviceDescriptor' field
    // to specify regexs that are to match against compatible devices.
    //
    // NOTE: The class is internal as we consider its objects temporaries
    //       that we keep around only during control hierarchy construction
    //       and let be reclaimed by the garbage collector. This way we're
    //       not paying the cost for these objects while the game is running.
    //       Especially for templates that are constructed through reflecton,
    //       we can always get them back easily and since templates are
    //       immutable, there's no modifications we have to preserve.
    internal class InputTemplate
    {
        // Both controls and processors can have public fields that can be set
        // directly from templates. The values are usually specified in strings
        // (like "clampMin=-1") but we parse them ahead of time into instances
        // of this structure that tell us where to store the value in the control.
        public unsafe struct ParameterValue
        {
            public const int kMaxValueSize = 8;

            public uint offset;
            public uint sizeInBytes;
            public fixed byte value[kMaxValueSize];
        }

        // Specifies the composition of an input control.
        public struct ControlTemplate
        {
            public string name; // Can be null/empty for "root" control but only one such control may exist.
            public string template;
            public ParameterValue[] parameters;
            public string[] usages;
            public string[] aliases;
            public KeyValuePair<string, ParameterValue[]>[] processors;
            public uint offset;
            public uint bit;
        }

        // Unique name of the template.
        // NOTE: Case-insensitive.
        public string name
        {
            get { return m_Name; }
        }

        public Type type
        {
            get { return m_Type; }
        }

        public string extendsTemplate
        {
            get { return m_ExtendsTemplate; }
        }

        // Unlike in a normal device descriptor, the strings in this descriptor are
        // regular expressions which can be used to match against the strings of an
        // actual device descriptor.
        public InputDeviceDescriptor deviceDescriptor
        {
            get { return m_DeviceDescriptor; }
        }

        public ReadOnlyCollection<ControlTemplate> controls
        {
            get
            {
                if (m_ControlsReadOnly == null)
                {
                    var controls = m_Controls;
                    if (controls == null)
                        controls = Array.Empty<ControlTemplate>();
                    m_ControlsReadOnly = Array.AsReadOnly(controls);
                }
                return m_ControlsReadOnly;
            }
        }

        public string ToJson()
        {
            throw new NotImplementedException();
        }

        // Uses reflection to construct a template from the given type.
        // Can be used with both control classes and state structs.
        public static InputTemplate FromType(string name, Type type)
        {
            var controlTemplates = new List<ControlTemplate>();

            // If it's a device with an InputStructAttribute, add control templates
            // from its state (if present) instead of from the device.
            var isDeviceWithStateAttribute = false;
            var stateTypeCode = new FourCC();
            if (typeof(InputDevice).IsAssignableFrom(type))
            {
                var stateAttribute = type.GetCustomAttribute<InputStateAttribute>();
                if (stateAttribute != null)
                {
                    isDeviceWithStateAttribute = true;
                    AddControlTemplates(stateAttribute.type, controlTemplates);

                    // Get state type code from state struct.
                    if (typeof(IInputStateTypeInfo).IsAssignableFrom(stateAttribute.type))
                    {
                        stateTypeCode = ((IInputStateTypeInfo) Activator.CreateInstance(stateAttribute.type))
                            .GetTypeStatic();
                    }
                }
            }
            if (!isDeviceWithStateAttribute)
            {
                // Add control templates from type contents.
                AddControlTemplates(type, controlTemplates);
            }

            ////TODO: make sure all usages are unique (probably want to have a check method that we can run on json templates as well)
            ////TODO: make sure all paths are unique (only relevant for JSON templates?)

            // Create template object.
            var template = new InputTemplate(name, type);
            template.m_Controls = controlTemplates.ToArray();
            template.m_StateTypeCode = stateTypeCode;

            return template;
        }

        // Constructs a template from the given JSON source.
        public static InputTemplate FromJson(string name, string json)
        {
            var templateJson = JsonUtility.FromJson<TemplateJson>(json);
            return templateJson.ToTemplate();
        }

        ////REVIEW: for device templates, should we always add one ControlTemplate that represents the
        ////        device control itself? this would get rid of a number of special cases in InputControlSetup

        private string m_Name;
        internal Type m_Type; // For extension chains, we can only discover types after loading multiple templates, so we make this accessible to InputControlSetup.
        internal FourCC m_StateTypeCode;
        private string m_ExtendsTemplate;
        private string[] m_OverridesTemplates;
        internal ControlTemplate[] m_Controls;
        private ReadOnlyCollection<ControlTemplate> m_ControlsReadOnly;
        private InputDeviceDescriptor m_DeviceDescriptor;

        private InputTemplate(string name, Type type)
        {
            m_Name = name;
            m_Type = type;
        }

        private static void AddControlTemplates(Type type, List<ControlTemplate> controlTemplates)
        {
            AddControlTemplatesFromFields(type, controlTemplates);
            AddControlTemplatesFromProperties(type, controlTemplates);
        }

        // Add ControlTemplates for every public property in the given type thas has
        // InputControlAttribute applied to it or has an InputControl-derived value type.
        private static void AddControlTemplatesFromFields(Type type, List<ControlTemplate> controlTemplates)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            AddControlTemplatesFromMembers(fields, controlTemplates);
        }

        // Add ControlTemplates for every public property in the given type thas has
        // InputControlAttribute applied to it or has an InputControl-derived value type.
        private static void AddControlTemplatesFromProperties(Type type, List<ControlTemplate> controlTemplates)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            AddControlTemplatesFromMembers(properties, controlTemplates);
        }

        // Add ControlTemplates for every member in the list thas has InputControlAttribute applied to it
        // or has an InputControl-derived value type.
        private static void AddControlTemplatesFromMembers(MemberInfo[] members, List<ControlTemplate> controlTemplates)
        {
            foreach (var member in members)
            {
                // Skip anything declared inside InputControl itself.
                // Filters out m_Device etc.
                if (member.DeclaringType == typeof(InputControl))
                    continue;

                var valueType = TypeHelpers.GetValueType(member);

                // If the value type of the member is a struct type and implements the IInputStateTypeInfo
                // interface, dive inside and look. This is useful for composing states of one another.
                if (valueType != null && valueType.IsValueType && typeof(IInputStateTypeInfo).IsAssignableFrom(valueType))
                {
                    var controlCountBefore = controlTemplates.Count;

                    AddControlTemplates(valueType, controlTemplates);

                    // If the current member is a field that is embedding the state structure, add
                    // the field offset to all control templates that were added from the struct.
                    var memberAsField = member as FieldInfo;
                    if (memberAsField != null)
                    {
                        var fieldOffset = Marshal.OffsetOf(member.DeclaringType, member.Name).ToInt32();
                        var countrolCountAfter = controlTemplates.Count;
                        for (var i = controlCountBefore; i < countrolCountAfter; ++i)
                        {
                            var controlTemplate = controlTemplates[i];
                            if (controlTemplates[i].offset != InputStateBlock.kInvalidOffset)
                            {
                                controlTemplate.offset += (uint)fieldOffset;
                                controlTemplates[i] = controlTemplate;
                            }
                        }
                    }

                    ////TODO: allow attributes on the member to modify control templates inside the struct
                }

                // Look for InputControlAttributes. If they aren't there, the member has to be
                // of an InputControl-derived value type.
                var attributes = member.GetCustomAttributes<InputControlAttribute>().ToArray();
                if (attributes.Length == 0)
                {
                    if (valueType == null || !typeof(InputControl).IsAssignableFrom(valueType))
                        continue;
                }

                AddControlTemplatesFromMember(member, attributes, controlTemplates);
            }
        }

        private static void AddControlTemplatesFromMember(MemberInfo member,
            InputControlAttribute[] attributes, List<ControlTemplate> controlTemplates)
        {
            // InputControlAttribute can be applied multiple times to the same member,
            // generating a separate control for each ocurrence. However, it can also
            // not be applied at all in which case we still add a control template (the
            // logic that called us already made sure the member is eligible for this kind
            // of operation).

            if (attributes.Length == 0)
            {
                var controlTemplate = CreateControlTemplateFromMember(member, null);
                controlTemplates.Add(controlTemplate);
            }
            else
            {
                foreach (var attribute in attributes)
                {
                    var controlTemplate = CreateControlTemplateFromMember(member, attribute);
                    controlTemplates.Add(controlTemplate);
                }
            }
        }

        private static ControlTemplate CreateControlTemplateFromMember(MemberInfo member, InputControlAttribute attribute)
        {
            ////REVIEW: make sure that the value type of the field and the value type of the control match?

            // Determine name.
            var name = attribute?.name;
            if (string.IsNullOrEmpty(name))
            {
                name = member.Name;
                if (name.IndexOf('/') != -1)
                    throw new Exception($"InputControlAttribute annotations cannot have paths as names: " + name);
            }

            // Determine template.
            var template = attribute?.template;
            if (string.IsNullOrEmpty(template))
            {
                var valueType = TypeHelpers.GetValueType(member);
                template = InferTemplateFromValueType(valueType);
            }

            // Determine offset.
            var offset = InputStateBlock.kInvalidOffset;
            if (attribute != null && attribute.offset != InputStateBlock.kInvalidOffset)
                offset = attribute.offset;
            else if (member is FieldInfo)
                offset = (uint)Marshal.OffsetOf(member.DeclaringType, member.Name).ToInt32();

            // Determine bit offset.
            var bit = 0u;
            if (attribute != null)
                bit = (uint)attribute.bit;

            // Determine aliases.
            string[] aliases = null;
            if (attribute != null)
                aliases = ArrayHelpers.Join(attribute.alias, attribute.aliases);

            // Determine usages.
            string[] usages = null;
            if (attribute != null)
                usages = ArrayHelpers.Join(attribute.usage, attribute.usages);

            // Determine parameters.
            ParameterValue[] parameters = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.parameters))
                parameters = ParseParameters(attribute.parameters);

            ////TODO: remaining template stuff

            return new ControlTemplate
            {
                name = name,
                template = template,
                offset = offset,
                bit = bit,
                parameters = parameters,
                usages = usages,
                aliases = aliases
            };
        }

        private static ParameterValue[] ParseParameters(string parameterString)
        {
            parameterString = parameterString.Trim();
            if (string.IsNullOrEmpty(parameterString))
                return null;

            var parameterCount = parameterString.CountOccurrences(',') + 1;
            var parameters = new ParameterValue[parameterCount];
            var parameterStringLength = parameterString.Length;

            var index = 0;
            for (var i = 0; i < parameterCount; ++i)
            {
                var parameter = ParseParameter(parameterString, ref index);
                parameters[i] = parameter;
            }

            return parameters;
        }

        private static ParameterValue ParseParameter(string parameterString, ref int index)
        {
            //can't look up name in type as all we have is a template name
            //for the from-type path it probably works but not for json templates

            // Parse name.

            // Parse value.

            return new ParameterValue();
            //throw new NotImplementedException();
        }

        private static string InferTemplateFromValueType(Type type)
        {
            var typeName = type.Name;
            if (typeName.EndsWith("Control"))
                return typeName.Substring(0, typeName.Length - "Control".Length);
            return typeName;
        }

        internal void MergeTemplate(InputTemplate other)
        {
            if (m_Type == null)
                m_Type = other.m_Type;

            if (m_Controls == null)
                m_Controls = other.m_Controls;
            else
            {
                var baseControls = other.m_Controls;
                var baseControlCount = baseControls.Length;

                // Even if the counts match we don't know how many controls are in the
                // set until we actually gone through both control lists and looked at
                // the names.

                var controls = new List<ControlTemplate>();

                var baseControlTable = CreateLookupTableForControls(baseControls);
                var thisControlTable = CreateLookupTableForControls(m_Controls);

                // First go through every control we have in this template.
                foreach (var pair in thisControlTable)
                {
                    ControlTemplate baseControlTemplate;
                    if (baseControlTable.TryGetValue(pair.Key, out baseControlTemplate))
                    {
                        ControlTemplate mergedTemplate = MergeControlTemplate(pair.Value, baseControlTemplate);
                        controls.Add(mergedTemplate);
                        
                        // Remove the entry so we don't hit it again in the pass through
                        // baseControlTable below.
                        baseControlTable.Remove(pair.Key);
                    }
                    else
                    {
                        controls.Add(pair.Value);
                    }
                }
                
                // And then go through all the controls in the base and take the
                // ones we're missing. We've already removed all the ones that intersect
                // and had to be merged so the rest we can just slurp into the list as is.
                controls.AddRange(baseControlTable.Values);

                m_Controls = controls.ToArray();
            }
        }

        private static Dictionary<string, ControlTemplate> CreateLookupTableForControls(
            ControlTemplate[] controlTemplates)
        {
            var table = new Dictionary<string, ControlTemplate>();
            for (var i = 0; i < controlTemplates.Length; ++i)
                table[controlTemplates[i].name.ToLower()] = controlTemplates[i];
            return table;
        }

        private static ControlTemplate MergeControlTemplate(ControlTemplate derivedTemplate, ControlTemplate baseTemplate)
        {
            var result = new ControlTemplate();

            result.name = derivedTemplate.name;
            Debug.Assert(derivedTemplate.name != null);
            
            result.template = derivedTemplate.template ?? baseTemplate.template;
            if (derivedTemplate.offset != InputStateBlock.kInvalidOffset)
                result.offset = derivedTemplate.offset;
            else
                result.offset = baseTemplate.offset;

            result.aliases = ArrayHelpers.Merge(derivedTemplate.aliases, baseTemplate.aliases,
                StringComparer.OrdinalIgnoreCase);
            result.usages = ArrayHelpers.Merge(derivedTemplate.usages, baseTemplate.usages,
                StringComparer.OrdinalIgnoreCase);
            
            ////TODO: merge rest

            return result;
        }
        
        internal static string ParseNameFromJson(string json)
        {
            var templateJson = JsonUtility.FromJson<TemplateJsonNameAndDescriptorOnly>(json);
            return templateJson.name;
        }

        [Serializable]
        private struct TemplateJsonNameAndDescriptorOnly
        {
            public string name;
            public DeviceDescriptorJson device;
        }

        [Serializable]
        private struct TemplateJson
        {
            public string name;
            public string extend;
            public string @override; // Convenience to not have to create array for single override.
            public string[] overrides;
            public string stateTypeCode;
            public DeviceDescriptorJson device;
            public ControlTemplateJson[] controls;

            public InputTemplate ToTemplate()
            {
                // By default, the type of the template is determine from the first template
                // in its 'extend' property chain that has a type set. However, if the template
                // extends nothing, we can't know what type to use for it so we default to
                // InputDevice.
                Type type = null;
                if (string.IsNullOrEmpty(extend))
                    type = typeof(InputDevice);

                // Create template.
                var template = new InputTemplate(name, type);
                template.m_ExtendsTemplate = extend;
                template.m_DeviceDescriptor = device.ToDescriptor();
                if (!string.IsNullOrEmpty(stateTypeCode))
                    template.m_StateTypeCode = new FourCC(stateTypeCode);

                // Add overrides.
                if (!string.IsNullOrEmpty(@override) || overrides != null)
                {
                    var names = new List<string>();
                    if (!string.IsNullOrEmpty(@override))
                        names.Add(@override);
                    if (overrides != null)
                        names.AddRange(overrides);
                    template.m_OverridesTemplates = names.ToArray();
                }

                // Add controls.
                if (controls != null)
                {
                    var controlTemplates = new List<ControlTemplate>();
                    foreach (var control in controls)
                    {
                        if (string.IsNullOrEmpty(control.name))
                            throw new Exception($"Control with no name in template '{name}");
                        controlTemplates.Add(control.ToTemplate());
                    }
                    template.m_Controls = controlTemplates.ToArray();
                }

                return template;
            }
        }

        // This is a class instead of a struct so that we can assign 'offset' a custom
        // default value. Otherwise we can't tell whether the user has actually set it
        // or not (0 is a valid offset). Sucks, though, as we now get lots of allocations
        // from the control array.
        [Serializable]
        private class ControlTemplateJson
        {
            public string name;
            public string template;
            public string usage; // Convenince to not have to create array for single usage.
            public uint offset;
            public string[] usages;
            public ParameterValueJson[] parameters;

            public ControlTemplateJson()
            {
                offset = InputStateBlock.kInvalidOffset;
            }

            public ControlTemplate ToTemplate()
            {
                var template = new ControlTemplate
                {
                    name = name,
                    template = this.template,
                    offset = offset
                };

                if (!string.IsNullOrEmpty(usage) || usages != null)
                {
                    var usagesList = new List<string>();
                    if (!string.IsNullOrEmpty(usage))
                        usagesList.Add(usage);
                    if (usages != null)
                        usagesList.AddRange(usages);
                    template.usages = usagesList.ToArray();
                }

                ////TODO: parameters

                return template;
            }
        }

        [Serializable]
        private struct ParameterValueJson
        {
            public string name;
        }

        [Serializable]
        private struct DeviceDescriptorJson
        {
            public string @interface;
            public string[] interfaces;
            public string deviceClass;
            public string[] deviceClasses;
            public string manufacturer;
            public string[] manufacturers;
            public string product;
            public string[] products;
            public string version;
            public string[] versions;

            public InputDeviceDescriptor ToDescriptor()
            {
                return new InputDeviceDescriptor
                {
                    interfaceName = JoinRegexStrings(@interface, interfaces),
                    deviceClass = JoinRegexStrings(deviceClass, deviceClasses),
                    manufacturer = JoinRegexStrings(manufacturer, manufacturers),
                    product = JoinRegexStrings(product, products),
                    version = JoinRegexStrings(version, versions)
                };
            }

            private static string JoinRegexStrings(string first, string[] subsequent)
            {
                var result = AppendRegexString(null, first);
                if (subsequent != null)
                    foreach (var str in subsequent)
                        result = AppendRegexString(result, str);
                return result;
            }

            private static string AppendRegexString(string regex, string part)
            {
                if (string.IsNullOrEmpty(regex))
                {
                    if (string.IsNullOrEmpty(part))
                        return null;

                    return $"({part})";
                }

                return "$regex|({part})";
            }
        }


        // These dictionaries are owned and managed by InputManager.
        internal static Dictionary<string, Type> s_TemplateTypes;
        internal static Dictionary<string, string> s_TemplateStrings;

        // Constructs InputTemlate instances and caches them.
        internal struct Cache
        {
            private Dictionary<string, InputTemplate> m_CachedTemplates;

            public InputTemplate FindOrLoadTemplate(string name)
            {
                Debug.Assert(s_TemplateTypes != null);
                Debug.Assert(s_TemplateStrings != null);

                var nameLowerCase = name.ToLower();

                // See if we have it cached.
                InputTemplate template;
                if (m_CachedTemplates != null && m_CachedTemplates.TryGetValue(nameLowerCase, out template))
                    return template;

                if (m_CachedTemplates == null)
                    m_CachedTemplates = new Dictionary<string, InputTemplate>();

                // No, so see if we have a string template for it. These
                // always take precedence over ones from type so that we can
                // override what's in the code using data.
                string json;
                if (s_TemplateStrings.TryGetValue(nameLowerCase, out json))
                {
                    template = InputTemplate.FromJson(name, json);
                    m_CachedTemplates[nameLowerCase] = template;

                    // If the template extends another template, we need to merge the
                    // base template into the final template.
                    if (!string.IsNullOrEmpty(template.extendsTemplate))
                    {
                        ////TODO: catch cycles
                        var superTemplate = FindOrLoadTemplate(template.extendsTemplate);
                        template.MergeTemplate(superTemplate);
                    }

                    return template;
                }

                // No, but maybe we have a type template for it.
                Type type;
                if (s_TemplateTypes.TryGetValue(nameLowerCase, out type))
                {
                    template = InputTemplate.FromType(name, type);
                    m_CachedTemplates[nameLowerCase] = template;
                    return template;
                }

                // Nothing.
                throw new Exception($"Cannot find input template called '{name}'");
            }
        }
    }
}
