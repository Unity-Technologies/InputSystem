using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ISX
{
    // A template lays out the composition of an input control.
    //
    // Can be created in two ways:
    //
    //  1) Loaded from JSON.
    //  2) Constructed through reflection from InputControls classes.
    //
    // Once constructed, templates are immutable (but you can always
    // replace a registered template in the system and it will affect
    // everything constructed from the template).
    //
    // Templates can be for arbitrary control rigs or for entire
    // devices. Device templates can use the 'deviceDescriptor' field
    // to specify regexs that are to match against compatible devices.
    //
    // InputTemplate objects are considered temporaries. Except in the
    // editor, we don't keep them around beyond device creation.
    public class InputTemplate
    {
        public enum ParameterType
        {
            Boolean,
            Integer,
            Float
        }

        // Both controls and processors can have public fields that can be set
        // directly from templates. The values are usually specified in strings
        // (like "clampMin=-1") but we parse them ahead of time into instances
        // of this structure that tell us where to store the value in the control.
        public unsafe struct ParameterValue
        {
            public const int kMaxValueSize = 4;

            public string name;
            public ParameterType type;
            public fixed byte value[kMaxValueSize];

            public int sizeInBytes
            {
                get
                {
                    switch (type)
                    {
                        case ParameterType.Boolean: return sizeof(bool);
                        case ParameterType.Float: return sizeof(float);
                        case ParameterType.Integer: return sizeof(int);
                    }
                    return 0;
                }
            }
        }

        public struct Processor
        {
            public string name;
            public ReadOnlyArray<ParameterValue> parameters;
        }

        ////TODO: need to figure out how to handle the root control; ATM the ControlTemplates always represent children
        ////      and you can't really set any properties on the root control
        ////      (the existing way works fine normal control templates but doesn't allow devices to make use of
        ////      what's offered to any other type control)

        // Specifies the composition of an input control.
        public struct ControlTemplate
        {
            ////REVIEW: if we expose InputTemplate, the arrays in here should turn into ReadOnlyArrays
            public string name; // Can be null/empty for "root" control but only one such control may exist.
            public string template;
            ////REVIEW: maybe make this more flexible to include name + image; maybe combined string
            public string icon; ////TODO: fill this (also have InputControlSetup put it on the control)
            public ReadOnlyArray<ParameterValue> parameters;
            public ReadOnlyArray<string> usages;
            public ReadOnlyArray<string> aliases;
            public ReadOnlyArray<Processor> processors;
            public uint offset;
            public uint bit;
            public uint sizeInBits;
            public FourCC format;

            // If true, the template will not add a control but rather a modify a control
            // inside the hierarchy added by 'template'. This allows, for example, to modify
            // just the X axis control of the left stick directly from within a gamepad
            // template instead of having to have a custom stick template for the left stick
            // than in turn would have to make use of a custom axis template for the X axis.
            // Insted, you can just have a control template with the name "leftStick/x".
            public bool isModifyingChildControlByPath;
        }

        // Unique name of the template.
        // NOTE: Case-insensitive.
        public string name => m_Name;

        public Type type => m_Type;

        public FourCC format => m_Format;

        public string extendsTemplate => m_ExtendsTemplate;

        // Unlike in a normal device descriptor, the strings in this descriptor are
        // regular expressions which can be used to match against the strings of an
        // actual device descriptor.
        public InputDeviceDescription deviceDescription => m_DeviceDescription;

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

            ////TODO: allow InputControl-derived classes to communicate their state type code
            // If it's a device with an InputStructAttribute, add control templates
            // from its state (if present) instead of from the device.
            var isDeviceWithStateAttribute = false;
            var format = new FourCC();
            if (typeof(InputDevice).IsAssignableFrom(type))
            {
                var stateAttribute = type.GetCustomAttribute<InputStateAttribute>();
                if (stateAttribute != null)
                {
                    isDeviceWithStateAttribute = true;
                    AddControlTemplates(stateAttribute.type, controlTemplates, name);

                    // Get state type code from state struct.
                    if (typeof(IInputStateTypeInfo).IsAssignableFrom(stateAttribute.type))
                    {
                        format = ((IInputStateTypeInfo)Activator.CreateInstance(stateAttribute.type))
                            .GetFormat();
                    }
                }
            }
            if (!isDeviceWithStateAttribute)
            {
                // Add control templates from type contents.
                AddControlTemplates(type, controlTemplates, name);
            }

            ////TODO: make sure all usages are unique (probably want to have a check method that we can run on json templates as well)
            ////TODO: make sure all paths are unique (only relevant for JSON templates?)

            // Create template object.
            var template = new InputTemplate(name, type);
            template.m_Controls = controlTemplates.ToArray();
            template.m_Format = format;

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
        internal FourCC m_Format;
        internal bool? m_UpdateBeforeRender;
        private string m_ExtendsTemplate;
        private string[] m_OverridesTemplates;
        internal ControlTemplate[] m_Controls;
        private ReadOnlyCollection<ControlTemplate> m_ControlsReadOnly;
        private InputDeviceDescription m_DeviceDescription;

        private InputTemplate(string name, Type type)
        {
            m_Name = name;
            m_Type = type;
        }

        private static void AddControlTemplates(Type type, List<ControlTemplate> controlTemplates, string templateName)
        {
            AddControlTemplatesFromFields(type, controlTemplates, templateName);
            AddControlTemplatesFromProperties(type, controlTemplates, templateName);
        }

        // Add ControlTemplates for every public property in the given type thas has
        // InputControlAttribute applied to it or has an InputControl-derived value type.
        private static void AddControlTemplatesFromFields(Type type, List<ControlTemplate> controlTemplates, string templateName)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            AddControlTemplatesFromMembers(fields, controlTemplates, templateName);
        }

        // Add ControlTemplates for every public property in the given type thas has
        // InputControlAttribute applied to it or has an InputControl-derived value type.
        private static void AddControlTemplatesFromProperties(Type type, List<ControlTemplate> controlTemplates, string templateName)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            AddControlTemplatesFromMembers(properties, controlTemplates, templateName);
        }

        // Add ControlTemplates for every member in the list thas has InputControlAttribute applied to it
        // or has an InputControl-derived value type.
        private static void AddControlTemplatesFromMembers(MemberInfo[] members, List<ControlTemplate> controlTemplates, string templateName)
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

                    AddControlTemplates(valueType, controlTemplates, templateName);

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

                AddControlTemplatesFromMember(member, attributes, controlTemplates, templateName);
            }
        }

        private static void AddControlTemplatesFromMember(MemberInfo member,
            InputControlAttribute[] attributes, List<ControlTemplate> controlTemplates, string templateName)
        {
            // InputControlAttribute can be applied multiple times to the same member,
            // generating a separate control for each ocurrence. However, it can also
            // not be applied at all in which case we still add a control template (the
            // logic that called us already made sure the member is eligible for this kind
            // of operation).

            if (attributes.Length == 0)
            {
                var controlTemplate = CreateControlTemplateFromMember(member, null, templateName);
                ThrowIfControlTemplateIsDuplicate(ref controlTemplate, controlTemplates, templateName);
                controlTemplates.Add(controlTemplate);
            }
            else
            {
                foreach (var attribute in attributes)
                {
                    var controlTemplate = CreateControlTemplateFromMember(member, attribute, templateName);
                    ThrowIfControlTemplateIsDuplicate(ref controlTemplate, controlTemplates, templateName);
                    controlTemplates.Add(controlTemplate);
                }
            }
        }

        private static ControlTemplate CreateControlTemplateFromMember(MemberInfo member, InputControlAttribute attribute, string templateName)
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

            // Determine format.
            var format = new FourCC();
            if (attribute != null && !string.IsNullOrEmpty(attribute.format))
                format = new FourCC(attribute.format);

            // Determine offset.
            var offset = InputStateBlock.kInvalidOffset;
            if (attribute != null && attribute.offset != InputStateBlock.kInvalidOffset)
                offset = attribute.offset;
            else if (member is FieldInfo)
                offset = (uint)Marshal.OffsetOf(member.DeclaringType, member.Name).ToInt32();

            // Determine bit offset.
            var bit = InputStateBlock.kInvalidOffset;
            if (attribute != null)
                bit = attribute.bit;

            // Determine size.
            var sizeInBits = InputStateBlock.kInvalidOffset;
            if (attribute != null)
                sizeInBits = attribute.sizeInBits;

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
                format = format,
                offset = offset,
                bit = bit,
                sizeInBits = sizeInBits,
                parameters = new ReadOnlyArray<ParameterValue>(parameters),
                usages = new ReadOnlyArray<string>(usages),
                aliases = new ReadOnlyArray<string>(aliases)
            };
        }

        internal static Processor[] ParseProcessors(string processorString)
        {
            processorString = processorString.Trim();
            if (string.IsNullOrEmpty(processorString))
                return null;

            var list = new List<Processor>();

            var index = 0;
            var processorStringLength = processorString.Length;

            while (index < processorStringLength)
            {
                // Skip whitespace.
                while (index < processorStringLength && char.IsWhiteSpace(processorString[index]))
                    ++index;

                // Parse name.
                var nameStart = index;
                while (index < processorStringLength)
                {
                    var nextChar = processorString[index];
                    if (nextChar == '(' || nextChar == ',' || char.IsWhiteSpace(nextChar))
                        break;
                    ++index;
                }
                if (index - nameStart == 0)
                    throw new Exception($"Expecting name at position {nameStart} in '{processorString}'");
                var name = processorString.Substring(nameStart, index - nameStart);

                // Skip whitespace.
                while (index < processorStringLength && char.IsWhiteSpace(processorString[index]))
                    ++index;

                // Parse parameters.
                ParameterValue[] parameters = null;
                if (index < processorStringLength && processorString[index] == '(')
                {
                    ++index;
                    var closeParenIndex = processorString.IndexOf(')', index);
                    if (closeParenIndex == -1)
                        throw new Exception($"Expecting ')' after '(' at position {index} in '{processorString}'");

                    var parameterString = processorString.Substring(index, closeParenIndex - index);
                    parameters = ParseParameters(parameterString);
                    index = closeParenIndex + 1;
                }

                if (index < processorStringLength && processorString[index] == ',')
                    ++index;

                list.Add(new Processor { name = name, parameters = new ReadOnlyArray<ParameterValue>(parameters) });
            }

            return list.ToArray();
        }

        private static ParameterValue[] ParseParameters(string parameterString)
        {
            parameterString = parameterString.Trim();
            if (string.IsNullOrEmpty(parameterString))
                return null;

            var parameterCount = parameterString.CountOccurrences(',') + 1;
            var parameters = new ParameterValue[parameterCount];

            var index = 0;
            for (var i = 0; i < parameterCount; ++i)
            {
                var parameter = ParseParameter(parameterString, ref index);
                parameters[i] = parameter;
            }

            return parameters;
        }

        private static unsafe ParameterValue ParseParameter(string parameterString, ref int index)
        {
            var parameter = new ParameterValue();
            var parameterStringLength = parameterString.Length;

            // Skip whitespace.
            while (index < parameterStringLength && char.IsWhiteSpace(parameterString[index]))
                ++index;

            // Parse name.
            var nameStart = index;
            while (index < parameterStringLength)
            {
                var nextChar = parameterString[index];
                if (nextChar == '=' || nextChar == ',' || char.IsWhiteSpace(nextChar))
                    break;
                ++index;
            }
            parameter.name = parameterString.Substring(nameStart, index - nameStart);

            // Skip whitespace.
            while (index < parameterStringLength && char.IsWhiteSpace(parameterString[index]))
                ++index;

            if (index == parameterStringLength || parameterString[index] != '=')
            {
                // No value given so take "=true" as implied.
                parameter.type = ParameterType.Boolean;
                *((bool*)parameter.value) = true;
            }
            else
            {
                ++index; // Skip over '='.

                // Skip whitespace.
                while (index < parameterStringLength && char.IsWhiteSpace(parameterString[index]))
                    ++index;

                // Parse value.
                var valueStart = index;
                while (index < parameterStringLength &&
                       !(parameterString[index] == ',' || char.IsWhiteSpace(parameterString[index])))
                    ++index;

                var value = parameterString.Substring(valueStart, index - valueStart);
                if (string.Compare(value, "true", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    parameter.type = ParameterType.Boolean;
                    *((bool*)parameter.value) = true;
                }
                else if (string.Compare(value, "false", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    parameter.type = ParameterType.Boolean;
                    *((bool*)parameter.value) = false;
                }
                else if (value.IndexOf('.') != -1)
                {
                    parameter.type = ParameterType.Float;
                    *((float*)parameter.value) = float.Parse(value);
                }
                else
                {
                    parameter.type = ParameterType.Integer;
                    *((int*)parameter.value) = int.Parse(value);
                }
            }

            if (index < parameterStringLength && parameterString[index] == ',')
                ++index;

            return parameter;
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
            m_Type = m_Type ?? other.m_Type;
            m_UpdateBeforeRender = m_UpdateBeforeRender ?? other.m_UpdateBeforeRender;

            if (m_Format == new FourCC())
                m_Format = other.m_Format;

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

            if (derivedTemplate.bit != InputStateBlock.kInvalidOffset)
                result.bit = derivedTemplate.bit;
            else
                result.bit = baseTemplate.bit;

            if (derivedTemplate.format != 0)
                result.format = derivedTemplate.format;
            else
                result.format = baseTemplate.format;

            if (derivedTemplate.sizeInBits != 0)
                result.sizeInBits = derivedTemplate.sizeInBits;
            else
                result.sizeInBits = baseTemplate.sizeInBits;

            result.aliases = new ReadOnlyArray<string>(
                    ArrayHelpers.Merge(derivedTemplate.aliases.m_Array,
                        baseTemplate.aliases.m_Array,
                        StringComparer.OrdinalIgnoreCase));

            result.usages = new ReadOnlyArray<string>(
                    ArrayHelpers.Merge(derivedTemplate.usages.m_Array,
                        baseTemplate.usages.m_Array,
                        StringComparer.OrdinalIgnoreCase));

            if (derivedTemplate.parameters.Count == 0)
                result.parameters = baseTemplate.parameters;
            else if (baseTemplate.parameters.Count == 0)
                result.parameters = derivedTemplate.parameters;
            else
                throw new NotImplementedException("merging parameters");

            if (derivedTemplate.processors.Count == 0)
                result.processors = baseTemplate.processors;
            else if (baseTemplate.parameters.Count == 0)
                result.processors = derivedTemplate.processors;
            else
                throw new NotImplementedException("merging processors");

            return result;
        }

        private static void ThrowIfControlTemplateIsDuplicate(ref ControlTemplate controlTemplate,
            IEnumerable<ControlTemplate> controlTemplates, string templateName)
        {
            var name = controlTemplate.name;
            foreach (var existing in controlTemplates)
                if (string.Compare(name, existing.name, StringComparison.OrdinalIgnoreCase) == 0)
                    throw new Exception($"Duplicate control '{name}' in template '{templateName}'");
        }

        internal static string ParseHeaderFromJson(string json, out InputDeviceDescription deviceDescription, out string baseTemplate)
        {
            var templateJson = JsonUtility.FromJson<TemplateJsonNameAndDescriptorOnly>(json);
            deviceDescription = templateJson.device.ToDescriptor();
            baseTemplate = templateJson.extend;
            return templateJson.name;
        }

        [Serializable]
        private struct TemplateJsonNameAndDescriptorOnly
        {
            public string name;
            public string extend;
            public DeviceDescriptorJson device;
        }

        [Serializable]
        private struct TemplateJson
        {
            // Disable warnings that these fields are never assigned to. They are set
            // by JsonUtility.
            #pragma warning disable CS0649
            // ReSharper disable MemberCanBePrivate.Local

            public string name;
            public string extend;
            public string @override; // Convenience to not have to create array for single override.
            public string[] overrides;
            public string format;
            public string beforeRender; // Can't be simple bool as otherwise we can't tell whether it was set or not.
            public DeviceDescriptorJson device;
            public ControlTemplateJson[] controls;

            // ReSharper restore MemberCanBePrivate.Local
            #pragma warning restore CS0649

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
                template.m_DeviceDescription = device.ToDescriptor();
                if (!string.IsNullOrEmpty(format))
                    template.m_Format = new FourCC(format);

                if (!string.IsNullOrEmpty(beforeRender))
                {
                    var beforeRenderLowerCase = beforeRender.ToLower();
                    if (beforeRenderLowerCase == "ignore")
                        template.m_UpdateBeforeRender = false;
                    else if (beforeRenderLowerCase == "update")
                        template.m_UpdateBeforeRender = true;
                    else
                        throw new Exception($"Invalid beforeRender setting '{beforeRender}'");
                }

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
                        var controlTemplate = control.ToTemplate();
                        ThrowIfControlTemplateIsDuplicate(ref controlTemplate, controlTemplates, template.name);
                        controlTemplates.Add(controlTemplate);
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
            // Disable warnings that these fields are never assigned to. They are set
            // by JsonUtility.
            #pragma warning disable CS0649
            // ReSharper disable MemberCanBePrivate.Local

            public string name;
            public string template;
            public string usage; // Convenince to not have to create array for single usage.
            public uint offset;
            public uint bit;
            public uint sizeInBits;
            public string format;
            public string[] usages;
            public string parameters;
            public string processors;

            // ReSharper restore MemberCanBePrivate.Local
            #pragma warning restore CS0649

            public ControlTemplateJson()
            {
                offset = InputStateBlock.kInvalidOffset;
                bit = InputStateBlock.kInvalidOffset;
            }

            public ControlTemplate ToTemplate()
            {
                var template = new ControlTemplate
                {
                    name = name,
                    template = this.template,
                    offset = offset,
                    bit = bit,
                    sizeInBits = sizeInBits,
                    isModifyingChildControlByPath = name.IndexOf('/') != -1
                };

                if (!string.IsNullOrEmpty(format))
                    template.format = new FourCC(format);

                if (!string.IsNullOrEmpty(usage) || usages != null)
                {
                    var usagesList = new List<string>();
                    if (!string.IsNullOrEmpty(usage))
                        usagesList.Add(usage);
                    if (usages != null)
                        usagesList.AddRange(usages);
                    template.usages = new ReadOnlyArray<string>(usagesList.ToArray());
                }

                if (!string.IsNullOrEmpty(parameters))
                    template.parameters = new ReadOnlyArray<ParameterValue>(ParseParameters(parameters));

                if (!string.IsNullOrEmpty(processors))
                    template.processors = new ReadOnlyArray<Processor>(ParseProcessors(processors));

                return template;
            }
        }

        [Serializable]
        private struct DeviceDescriptorJson
        {
            // Disable warnings that these fields are never assigned to. They are set
            // by JsonUtility.
            #pragma warning disable CS0649
            // ReSharper disable MemberCanBePrivate.Local

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

            // ReSharper restore MemberCanBePrivate.Local
            #pragma warning restore CS0649

            public InputDeviceDescription ToDescriptor()
            {
                return new InputDeviceDescription
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
        internal static Dictionary<string, string> s_BaseTemplateTable;

        // Constructs InputTemplate instances and caches them.
        internal struct Cache
        {
            public Dictionary<string, InputTemplate> table;

            public InputTemplate FindOrLoadTemplate(string name)
            {
                Debug.Assert(s_TemplateTypes != null);
                Debug.Assert(s_TemplateStrings != null);

                var nameLowerCase = name.ToLower();

                // See if we have it cached.
                InputTemplate template;
                if (table != null && table.TryGetValue(nameLowerCase, out template))
                    return template;

                if (table == null)
                    table = new Dictionary<string, InputTemplate>();

                // No, so see if we have a string template for it. These
                // always take precedence over ones from type so that we can
                // override what's in the code using data.
                string json;
                if (InputTemplate.s_TemplateStrings.TryGetValue(nameLowerCase, out json))
                {
                    template = FromJson(name, json);
                    table[nameLowerCase] = template;

                    // If the template extends another template, we need to merge the
                    // base template into the final template.
                    if (!String.IsNullOrEmpty(template.extendsTemplate))
                    {
                        ////TODO: catch cycles
                        var superTemplate = FindOrLoadTemplate(template.extendsTemplate);
                        template.MergeTemplate(superTemplate);
                    }

                    return template;
                }

                // No, but maybe we have a type template for it.
                Type type;
                if (InputTemplate.s_TemplateTypes.TryGetValue(nameLowerCase, out type))
                {
                    template = FromType(name, type);
                    table[nameLowerCase] = template;
                    return template;
                }

                // Nothing.
                throw new Exception($"Cannot find input template called '{name}'");
            }
        }
    }
}
