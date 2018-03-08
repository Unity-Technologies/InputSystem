using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ISX.LowLevel;
using ISX.Utilities;
using UnityEngine;

#if !(NET_4_0 || NET_4_6)
using ISX.Net35Compatibility;
#endif

////TODO: rename 'overrides' to 'replaces'

////TODO: make it so that a control with no variant set can act as the base template for controls with the same name that have a variant set

////TODO: ensure that if a template sets a device description, it is indeed a device template

////TODO: array support

////REVIEW: common usages are on all templates but only make sense for devices

namespace ISX
{
    /// <summary>
    /// A template lays out the composition of an input control.
    /// </summary>
    /// <remarks>
    /// Templates can be created in three ways:
    ///
    /// <list type="number">
    /// <item><description>Loaded from JSON.</description></item>
    /// <item><description>Constructed through reflection from InputControls classes.</description></item>
    /// <item><description>Through template constructors using InputTemplate.Builder.</description></item>
    /// </list>
    ///
    /// Once constructed, templates are immutable (but you can always
    /// replace a registered template in the system and it will affect
    /// everything constructed from the template).
    ///
    /// Templates can be for arbitrary control rigs or for entire
    /// devices. Device templates can use the 'deviceDescriptor' field
    /// to specify regexs that are to match against compatible devices.
    ///
    /// InputTemplate objects are considered temporaries. Except in the
    /// editor, we don't keep them around beyond device creation.
    /// </remarks>
    public class InputTemplate
    {
        // String that is used to separate names from namespaces in template names.
        public const string kNamespaceQualifier = "::";

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

            public override string ToString()
            {
                fixed(byte* ptr = value)
                {
                    switch (type)
                    {
                        case ParameterType.Boolean:
                            if (*((bool*)ptr))
                                return name;
                            return string.Format("{0}=false", name);
                        case ParameterType.Integer:
                            var intValue = *((int*)ptr);
                            return string.Format("{0}={1}", name, intValue);
                        case ParameterType.Float:
                            var floatValue = *((float*)ptr);
                            return string.Format("{0}={1}", name, floatValue);
                    }
                }

                return string.Empty;
            }
        }

        public struct NameAndParameters
        {
            public string name;
            public ReadOnlyArray<ParameterValue> parameters;

            public override string ToString()
            {
                if (parameters.Count == 0)
                    return name;
                var parameterString = string.Join(",", parameters.Select(x => x.ToString()).ToArray());
                return string.Format("name({0})", parameterString);
            }
        }

        /// <summary>
        /// Specification for the composition of a direct or indirect child control.
        /// </summary>
        public struct ControlTemplate
        {
            [Flags]
            public enum Flags
            {
                IsModifyingChildControlByPath = 1 << 0,
                IsNoisy = 1 << 1,
            }

            /// <summary>
            /// Name of the control.
            /// </summary>
            /// <remarks>
            /// This may also be a path. This can be used to reach
            /// inside another template and modify properties of a control inside
            /// of it. An example for this is adding a "leftStick" control using the
            /// Stick template and then adding two control templates that refer to
            /// "leftStick/x" and "leftStick/y" respectively to modify the state
            /// format used by the stick.
            ///
            /// This field is required.
            /// </remarks>
            /// <seealso cref="isModifyingChildControlByPath"/>
            public InternedString name;

            public InternedString template;
            public InternedString variant;
            public string useStateFrom;

            /// <summary>
            /// Optional display name of the control.
            /// </summary>
            /// <seealso cref="InputControl.displayName"/>
            public string displayName;

            public string resourceName;
            public ReadOnlyArray<InternedString> usages;
            public ReadOnlyArray<InternedString> aliases;
            public ReadOnlyArray<ParameterValue> parameters;
            public ReadOnlyArray<NameAndParameters> processors;
            public uint offset;
            public uint bit;
            public uint sizeInBits;
            public FourCC format;
            public Flags flags;

            // If true, the template will not add a control but rather a modify a control
            // inside the hierarchy added by 'template'. This allows, for example, to modify
            // just the X axis control of the left stick directly from within a gamepad
            // template instead of having to have a custom stick template for the left stick
            // than in turn would have to make use of a custom axis template for the X axis.
            // Insted, you can just have a control template with the name "leftStick/x".
            public bool isModifyingChildControlByPath
            {
                get { return (flags & Flags.IsModifyingChildControlByPath) == Flags.IsModifyingChildControlByPath; }
                set
                {
                    if (value)
                        flags |= Flags.IsModifyingChildControlByPath;
                    else
                        flags &= ~Flags.IsModifyingChildControlByPath;
                }
            }

            public bool isNoisy
            {
                get { return (flags & Flags.IsNoisy) == Flags.IsNoisy; }
                set
                {
                    if (value)
                        flags |= Flags.IsNoisy;
                    else
                        flags &= ~Flags.IsNoisy;
                }
            }

            /// <summary>
            /// For any property not set on this control template, take the setting from <paramref name="other"/>.
            /// </summary>
            /// <param name="other">Control template providing settings.</param>
            /// <remarks>
            /// <see cref="name"/> will not be touched.
            /// </remarks>
            public ControlTemplate Merge(ControlTemplate other)
            {
                var result = new ControlTemplate();

                result.name = name;
                Debug.Assert(!name.IsEmpty());

                result.template = template.IsEmpty() ? other.template : template;
                result.variant = variant.IsEmpty() ? other.variant : variant;
                result.useStateFrom = useStateFrom ?? other.useStateFrom;

                if (offset != InputStateBlock.kInvalidOffset)
                    result.offset = offset;
                else
                    result.offset = other.offset;

                if (bit != InputStateBlock.kInvalidOffset)
                    result.bit = bit;
                else
                    result.bit = other.bit;

                if (format != 0)
                    result.format = format;
                else
                    result.format = other.format;

                if (sizeInBits != 0)
                    result.sizeInBits = sizeInBits;
                else
                    result.sizeInBits = other.sizeInBits;

                result.aliases = new ReadOnlyArray<InternedString>(
                        ArrayHelpers.Merge(aliases.m_Array,
                            other.aliases.m_Array));

                result.usages = new ReadOnlyArray<InternedString>(
                        ArrayHelpers.Merge(usages.m_Array,
                            other.usages.m_Array));

                if (parameters.Count == 0)
                    result.parameters = other.parameters;
                else if (other.parameters.Count == 0)
                    result.parameters = parameters;
                else
                    throw new NotImplementedException("merging parameters");////REVIEW: probably best to not merge them actually

                if (processors.Count == 0)
                    result.processors = other.processors;
                else if (other.parameters.Count == 0)
                    result.processors = processors;
                else
                    throw new NotImplementedException("merging processors");

                if (!string.IsNullOrEmpty(displayName))
                    result.displayName = displayName;
                else
                    result.displayName = other.displayName;

                if (!string.IsNullOrEmpty(resourceName))
                    result.resourceName = resourceName;
                else
                    result.resourceName = other.resourceName;

                return result;
            }
        }

        // Unique name of the template.
        // NOTE: Case-insensitive.
        public InternedString name
        {
            get { return m_Name; }
        }

        public Type type
        {
            get { return m_Type; }
        }

        public FourCC stateFormat
        {
            get { return m_StateFormat; }
        }

        public string extendsTemplate
        {
            get { return m_ExtendsTemplate; }
        }

        public ReadOnlyArray<InternedString> commonUsages
        {
            get { return new ReadOnlyArray<InternedString>(m_CommonUsages); }
        }

        // Unlike in a normal device descriptor, the strings in this descriptor are
        // regular expressions which can be used to match against the strings of an
        // actual device descriptor.
        public InputDeviceDescription deviceDescription
        {
            get { return m_DeviceDescription; }
        }

        public ReadOnlyArray<ControlTemplate> controls
        {
            get { return new ReadOnlyArray<ControlTemplate>(m_Controls); }
        }

        public bool isDeviceTemplate
        {
            get { return typeof(InputDevice).IsAssignableFrom(m_Type); }
        }

        public bool isControlTemplate
        {
            get { return !isDeviceTemplate; }
        }

        /// <summary>
        /// Build a template programmatically. Primarily for use by template constructors
        /// registered with the system.
        /// </summary>
        /// <seealso cref="InputSystem.RegisterTemplateConstructor"/>
        public struct Builder
        {
            public string name;
            public Type type;
            public FourCC stateFormat;
            public string extendsTemplate;
            public InputDeviceDescription deviceDescription;

            private int m_ControlCount;
            private ControlTemplate[] m_Controls;

            public struct ControlBuilder
            {
                internal Builder builder;
                internal ControlTemplate[] controls;
                internal int index;

                public ControlBuilder WithTemplate(string template)
                {
                    if (string.IsNullOrEmpty(template))
                        throw new ArgumentException("Template name cannot be null or empty", "template");

                    controls[index].template = new InternedString(template);
                    return this;
                }

                public ControlBuilder WithFormat(FourCC format)
                {
                    controls[index].format = format;
                    return this;
                }

                public ControlBuilder WithFormat(string format)
                {
                    return WithFormat(new FourCC(format));
                }

                public ControlBuilder WithOffset(uint offset)
                {
                    controls[index].offset = offset;
                    return this;
                }

                public ControlBuilder WithBit(uint bit)
                {
                    controls[index].bit = bit;
                    return this;
                }

                public ControlBuilder WithUsages(params InternedString[] usages)
                {
                    if (usages == null || usages.Length == 0)
                        return this;

                    for (var i = 0; i < usages.Length; ++i)
                        if (usages[i].IsEmpty())
                            throw new ArgumentException(
                                string.Format("Empty usage entry at index {0} for control '{1}' in template '{2}'", i,
                                    controls[index].name, builder.name), "usages");

                    controls[index].usages = new ReadOnlyArray<InternedString>(usages);
                    return this;
                }

                public ControlBuilder WithUsages(IEnumerable<string> usages)
                {
                    var usagesArray = usages.Select(x => new InternedString(x)).ToArray();
                    return WithUsages(usagesArray);
                }

                public ControlBuilder WithUsages(params string[] usages)
                {
                    return WithUsages((IEnumerable<string>)usages);
                }
            }

            // This invalidates the ControlBuilders from previous calls! (our array may move)
            /// <summary>
            /// Add a new control to the template.
            /// </summary>
            /// <param name="name">Name or path of the control. If it is a path (e.g. <c>"leftStick/x"</c>,
            /// then the control either modifies the setup of a child control of another control in the template
            /// or adds a new child control to another control in the template. Modifying child control is useful,
            /// for example, to alter the state format of controls coming from the base template. Likewise,
            /// adding child controls to another control is useful to modify the setup of of the control template
            /// being used without having to create and register a custom control template.</param>
            /// <returns>A control builder that permits setting various parameters on the control.</returns>
            /// <exception cref="ArgumentException"><paramref name="name"/> is null or empty.</exception>
            public ControlBuilder AddControl(string name)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException(name);

                var index = ArrayHelpers.AppendWithCapacity(ref m_Controls, ref m_ControlCount,
                        new ControlTemplate {name = new InternedString(name)});

                return new ControlBuilder
                {
                    builder = this,
                    controls = m_Controls,
                    index = index
                };
            }

            public Builder WithName(string name)
            {
                this.name = name;
                return this;
            }

            public Builder WithType<T>()
                where T : InputControl
            {
                type = typeof(T);
                return this;
            }

            public Builder WithFormat(FourCC format)
            {
                stateFormat = format;
                return this;
            }

            public Builder WithFormat(string format)
            {
                return WithFormat(new FourCC(format));
            }

            public Builder ForDevice(InputDeviceDescription deviceDescription)
            {
                this.deviceDescription = deviceDescription;
                return this;
            }

            public Builder Extend(string baseTemplateName)
            {
                extendsTemplate = baseTemplateName;
                return this;
            }

            public InputTemplate Build()
            {
                ControlTemplate[] controls = null;
                if (m_ControlCount > 0)
                {
                    controls = new ControlTemplate[m_ControlCount];
                    Array.Copy(m_Controls, controls, m_ControlCount);
                }

                // Allow template to be unnamed. The system will automatically set the
                // name that the template has been registered under.
                var template =
                    new InputTemplate(new InternedString(name), type ?? typeof(InputDevice))
                {
                    m_StateFormat = stateFormat,
                    m_ExtendsTemplate = new InternedString(extendsTemplate),
                    m_DeviceDescription = deviceDescription,
                    m_Controls = controls
                };

                return template;
            }
        }

        // Uses reflection to construct a template from the given type.
        // Can be used with both control classes and state structs.
        public static InputTemplate FromType(string name, Type type)
        {
            var controlTemplates = new List<ControlTemplate>();
            var templateAttribute = type.GetCustomAttribute<InputTemplateAttribute>(true);

            // If there's an InputTemplateAttribute on the type that has 'stateType' set,
            // add control templates from its state (if present) instead of from the type.
            var stateFormat = new FourCC();
            if (templateAttribute != null && templateAttribute.stateType != null)
            {
                AddControlTemplates(templateAttribute.stateType, controlTemplates, name);

                // Get state type code from state struct.
                if (typeof(IInputStateTypeInfo).IsAssignableFrom(templateAttribute.stateType))
                {
                    stateFormat = ((IInputStateTypeInfo)Activator.CreateInstance(templateAttribute.stateType))
                        .GetFormat();
                }
            }
            else
            {
                // Add control templates from type contents.
                AddControlTemplates(type, controlTemplates, name);
            }

            if (templateAttribute != null && templateAttribute.stateFormat != new FourCC())
                stateFormat = templateAttribute.stateFormat;

            ////TODO: make sure all usages are unique (probably want to have a check method that we can run on json templates as well)
            ////TODO: make sure all paths are unique (only relevant for JSON templates?)

            // Create template object.
            var template = new InputTemplate(name, type);
            template.m_Controls = controlTemplates.ToArray();
            template.m_StateFormat = stateFormat;

            if (templateAttribute != null && templateAttribute.commonUsages != null)
                template.m_CommonUsages =
                    ArrayHelpers.Select(templateAttribute.commonUsages, x => new InternedString(x));

            return template;
        }

        public string ToJson()
        {
            var template = TemplateJson.FromTemplate(this);
            return JsonUtility.ToJson(template);
        }

        // Constructs a template from the given JSON source.
        public static InputTemplate FromJson(string json)
        {
            var templateJson = JsonUtility.FromJson<TemplateJson>(json);
            return templateJson.ToTemplate();
        }

        ////REVIEW: shouldn't state be split between input and output? how does output fit into the template picture in general?
        ////        should the control template alone determine the direction things are going in?

        private InternedString m_Name;
        internal Type m_Type; // For extension chains, we can only discover types after loading multiple templates, so we make this accessible to InputControlSetup.
        internal FourCC m_StateFormat;
        internal int m_StateSizeInBytes; // Note that this is the combined state size for input and output.
        internal bool? m_UpdateBeforeRender;
        private InternedString m_ExtendsTemplate;
        private InternedString[] m_OverridesTemplates;
        private InternedString[] m_CommonUsages;
        internal ControlTemplate[] m_Controls;
        private InputDeviceDescription m_DeviceDescription;
        internal string m_DisplayName;
        internal string m_ResourceName;

        private InputTemplate(string name, Type type)
        {
            m_Name = new InternedString(name);
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
                var attributes = member.GetCustomAttributes<InputControlAttribute>(false).ToArray();
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
            var name = attribute != null ? attribute.name : null;
            if (string.IsNullOrEmpty(name))
                name = member.Name;

            var isModifyingChildControlByPath = name.IndexOf('/') != -1;

            // Determine template.
            var template = attribute != null ? attribute.template : null;
            if (string.IsNullOrEmpty(template) && !isModifyingChildControlByPath)
            {
                var valueType = TypeHelpers.GetValueType(member);
                template = InferTemplateFromValueType(valueType);
            }

            // Determine variant.
            string variant = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.variant))
                variant = attribute.variant;

            // Determine offset.
            var offset = InputStateBlock.kInvalidOffset;
            if (attribute != null && attribute.offset != InputStateBlock.kInvalidOffset)
                offset = attribute.offset;
            else if (member is FieldInfo && !isModifyingChildControlByPath)
                offset = (uint)Marshal.OffsetOf(member.DeclaringType, member.Name).ToInt32();

            // Determine bit offset.
            var bit = InputStateBlock.kInvalidOffset;
            if (attribute != null)
                bit = attribute.bit;

            ////TODO: if size is not set, determine from type of field
            // Determine size.
            var sizeInBits = 0u;
            if (attribute != null)
                sizeInBits = attribute.sizeInBits;

            // Determine format.
            var format = new FourCC();
            if (attribute != null && !string.IsNullOrEmpty(attribute.format))
                format = new FourCC(attribute.format);
            else if (!isModifyingChildControlByPath && bit == InputStateBlock.kInvalidOffset)
            {
                var valueType = TypeHelpers.GetValueType(member);
                format = InputStateBlock.GetPrimitiveFormatFromType(valueType);
            }

            // Determine aliases.
            InternedString[] aliases = null;
            if (attribute != null)
            {
                var joined = ArrayHelpers.Join(attribute.alias, attribute.aliases);
                if (joined != null)
                    aliases = joined.Select(x => new InternedString(x)).ToArray();
            }

            // Determine usages.
            InternedString[] usages = null;
            if (attribute != null)
            {
                var joined = ArrayHelpers.Join(attribute.usage, attribute.usages);
                if (joined != null)
                    usages = joined.Select(x => new InternedString(x)).ToArray();
            }

            // Determine parameters.
            ParameterValue[] parameters = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.parameters))
                parameters = ParseParameters(attribute.parameters);

            // Determine processors.
            NameAndParameters[] processors = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.processors))
                processors = ParseNameAndParameterList(attribute.processors);

            // Determine whether to use state from another control.
            string useStateFrom = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.useStateFrom))
                useStateFrom = attribute.useStateFrom;

            // Determine if it's a noisy control.
            var isNoisy = false;
            if (attribute != null)
                isNoisy = attribute.noisy;

            return new ControlTemplate
            {
                name = new InternedString(name),
                template = new InternedString(template),
                variant = new InternedString(variant),
                useStateFrom = useStateFrom,
                format = format,
                offset = offset,
                bit = bit,
                sizeInBits = sizeInBits,
                parameters = new ReadOnlyArray<ParameterValue>(parameters),
                processors = new ReadOnlyArray<NameAndParameters>(processors),
                usages = new ReadOnlyArray<InternedString>(usages),
                aliases = new ReadOnlyArray<InternedString>(aliases),
                isModifyingChildControlByPath = isModifyingChildControlByPath,
                isNoisy = isNoisy,
            };
        }

        internal static NameAndParameters[] ParseNameAndParameterList(string text)
        {
            text = text.Trim();
            if (string.IsNullOrEmpty(text))
                return null;

            var list = new List<NameAndParameters>();

            var index = 0;
            var textLength = text.Length;

            while (index < textLength)
            {
                // Skip whitespace.
                while (index < textLength && char.IsWhiteSpace(text[index]))
                    ++index;

                // Parse name.
                var nameStart = index;
                while (index < textLength)
                {
                    var nextChar = text[index];
                    if (nextChar == '(' || nextChar == ',' || char.IsWhiteSpace(nextChar))
                        break;
                    ++index;
                }
                if (index - nameStart == 0)
                    throw new Exception(string.Format("Expecting name at position {0} in '{1}'", nameStart, text));
                var name = text.Substring(nameStart, index - nameStart);

                // Skip whitespace.
                while (index < textLength && char.IsWhiteSpace(text[index]))
                    ++index;

                // Parse parameters.
                ParameterValue[] parameters = null;
                if (index < textLength && text[index] == '(')
                {
                    ++index;
                    var closeParenIndex = text.IndexOf(')', index);
                    if (closeParenIndex == -1)
                        throw new Exception(string.Format("Expecting ')' after '(' at position {0} in '{1}'", index,
                                text));

                    var parameterString = text.Substring(index, closeParenIndex - index);
                    parameters = ParseParameters(parameterString);
                    index = closeParenIndex + 1;
                }

                if (index < textLength && text[index] == ',')
                    ++index;

                list.Add(new NameAndParameters { name = name, parameters = new ReadOnlyArray<ParameterValue>(parameters) });
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
            if (!type.IsPrimitive)
                return typeName;
            return null;
        }

        internal void MergeTemplate(InputTemplate other)
        {
            m_Type = m_Type ?? other.m_Type;
            m_UpdateBeforeRender = m_UpdateBeforeRender ?? other.m_UpdateBeforeRender;

            if (m_StateFormat == new FourCC())
                m_StateFormat = other.m_StateFormat;

            if (string.IsNullOrEmpty(m_DisplayName))
                m_DisplayName = other.m_DisplayName;
            if (string.IsNullOrEmpty(m_ResourceName))
                m_ResourceName = other.m_ResourceName;

            m_CommonUsages = ArrayHelpers.Merge(other.m_CommonUsages, m_CommonUsages);

            if (m_Controls == null)
                m_Controls = other.m_Controls;
            else
            {
                var baseControls = other.m_Controls;

                // Even if the counts match we don't know how many controls are in the
                // set until we actually gone through both control lists and looked at
                // the names.

                var controls = new List<ControlTemplate>();
                var baseControlVariants = new List<string>();

                var baseControlTable = CreateLookupTableForControls(baseControls, baseControlVariants);
                var thisControlTable = CreateLookupTableForControls(m_Controls);

                // First go through every control we have in this template.
                foreach (var pair in thisControlTable)
                {
                    ControlTemplate baseControlTemplate;
                    if (baseControlTable.TryGetValue(pair.Key, out baseControlTemplate))
                    {
                        var mergedTemplate = pair.Value.Merge(baseControlTemplate);
                        controls.Add(mergedTemplate);

                        // Remove the entry so we don't hit it again in the pass through
                        // baseControlTable below.
                        baseControlTable.Remove(pair.Key);
                    }
                    else
                    {
                        // We may be looking at a control that is using variants on the base template but
                        // isn't targeting a specific variant on the derived template. In that case, we
                        // want to take each of the variants from the base template and merge them with
                        // the control template in the derived template.
                        var isTargetingVariants = false;
                        foreach (var variant in baseControlVariants)
                        {
                            var key = string.Format("{0}@{1}", pair.Key, variant);
                            if (baseControlTable.TryGetValue(key, out baseControlTemplate))
                            {
                                var mergedTemplate = pair.Value.Merge(baseControlTemplate);
                                controls.Add(mergedTemplate);
                                baseControlTable.Remove(key);
                                isTargetingVariants = true;
                            }
                        }

                        // Okay, this template isn't corresponding to anything in the base template
                        // so just add it as is.
                        if (!isTargetingVariants)
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
            ControlTemplate[] controlTemplates, List<string> variants = null)
        {
            var table = new Dictionary<string, ControlTemplate>();
            for (var i = 0; i < controlTemplates.Length; ++i)
            {
                var key = controlTemplates[i].name.ToLower();
                // Need to take variant into account as well. Otherwise two variants for
                // "leftStick", for example, will overwrite each other.
                if (!controlTemplates[i].variant.IsEmpty())
                {
                    var variant = controlTemplates[i].variant.ToLower();
                    key = string.Format("{0}@{1}", key, variant);
                    if (variants != null)
                        variants.Add(variant);
                }
                table[key] = controlTemplates[i];
            }
            return table;
        }

        private static void ThrowIfControlTemplateIsDuplicate(ref ControlTemplate controlTemplate,
            IEnumerable<ControlTemplate> controlTemplates, string templateName)
        {
            var name = controlTemplate.name;
            foreach (var existing in controlTemplates)
                if (string.Compare(name, existing.name, StringComparison.OrdinalIgnoreCase) == 0 &&
                    existing.variant == controlTemplate.variant)
                    throw new Exception(string.Format("Duplicate control '{0}' in template '{1}'", name, templateName));
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
            public DeviceDescriptionJson device;
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
            public string[] commonUsages;
            public string displayName;
            public string resourceName;
            public string type; // This is mostly for when we turn arbitrary InputTemplates into JSON; less for templates *coming* from JSON.
            public DeviceDescriptionJson device;
            public ControlTemplateJson[] controls;

            // ReSharper restore MemberCanBePrivate.Local
            #pragma warning restore CS0649

            public InputTemplate ToTemplate()
            {
                // By default, the type of the template is determined from the first template
                // in its 'extend' property chain that has a type set. However, if the template
                // extends nothing, we can't know what type to use for it so we default to
                // InputDevice.
                Type type = null;
                if (!string.IsNullOrEmpty(this.type))
                {
                    type = Type.GetType(this.type, false);
                    if (type == null)
                    {
                        Debug.Log(string.Format(
                                "Cannot find type '{0}' used by template '{1}'; falling back to using InputDevice",
                                this.type, name));
                        type = typeof(InputDevice);
                    }
                    else if (!typeof(InputControl).IsAssignableFrom(type))
                    {
                        throw new Exception(string.Format("'{0}' used by template '{1}' is not an InputControl",
                                this.type, name));
                    }
                }
                else if (string.IsNullOrEmpty(extend))
                    type = typeof(InputDevice);

                // Create template.
                var template = new InputTemplate(name, type);
                template.m_ExtendsTemplate = new InternedString(extend);
                template.m_DeviceDescription = device.ToDescriptor();
                template.m_DisplayName = displayName;
                template.m_ResourceName = resourceName;
                if (!string.IsNullOrEmpty(format))
                    template.m_StateFormat = new FourCC(format);

                if (!string.IsNullOrEmpty(beforeRender))
                {
                    var beforeRenderLowerCase = beforeRender.ToLower();
                    if (beforeRenderLowerCase == "ignore")
                        template.m_UpdateBeforeRender = false;
                    else if (beforeRenderLowerCase == "update")
                        template.m_UpdateBeforeRender = true;
                    else
                        throw new Exception(string.Format("Invalid beforeRender setting '{0}'", beforeRender));
                }

                // Add common usages.
                if (commonUsages != null)
                {
                    template.m_CommonUsages = ArrayHelpers.Select(commonUsages, x => new InternedString(x));
                }

                // Add overrides.
                if (!string.IsNullOrEmpty(@override) || overrides != null)
                {
                    var names = new List<InternedString>();
                    if (!string.IsNullOrEmpty(@override))
                        names.Add(new InternedString(@override));
                    if (overrides != null)
                        names.AddRange(overrides.Select(x => new InternedString(x)));
                    template.m_OverridesTemplates = names.ToArray();
                }

                // Add controls.
                if (controls != null)
                {
                    var controlTemplates = new List<ControlTemplate>();
                    foreach (var control in controls)
                    {
                        if (string.IsNullOrEmpty(control.name))
                            throw new Exception(string.Format("Control with no name in template '{0}", name));
                        var controlTemplate = control.ToTemplate();
                        ThrowIfControlTemplateIsDuplicate(ref controlTemplate, controlTemplates, template.name);
                        controlTemplates.Add(controlTemplate);
                    }
                    template.m_Controls = controlTemplates.ToArray();
                }

                return template;
            }

            public static TemplateJson FromTemplate(InputTemplate template)
            {
                return new TemplateJson
                {
                    name = template.m_Name,
                    type = template.type.AssemblyQualifiedName,
                    displayName = template.m_DisplayName,
                    resourceName = template.m_ResourceName,
                    extend = template.m_ExtendsTemplate,
                    format = template.stateFormat.ToString(),
                    device = DeviceDescriptionJson.FromDescription(template.m_DeviceDescription),
                    controls = ControlTemplateJson.FromControlTemplates(template.m_Controls),
                };
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
            public string variant;
            public string usage; // Convenince to not have to create array for single usage.
            public string alias; // Same.
            public string useStateFrom;
            public uint offset;
            public uint bit;
            public uint sizeInBits;
            public string format;
            public string[] usages;
            public string[] aliases;
            public string parameters;
            public string processors;
            public string displayName;
            public string resourceName;
            public bool noisy;

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
                    name = new InternedString(name),
                    template = new InternedString(this.template),
                    variant = new InternedString(variant),
                    displayName = displayName,
                    resourceName = resourceName,
                    offset = offset,
                    useStateFrom = useStateFrom,
                    bit = bit,
                    sizeInBits = sizeInBits,
                    isModifyingChildControlByPath = name.IndexOf('/') != -1,
                    isNoisy = noisy,
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
                    template.usages = new ReadOnlyArray<InternedString>(usagesList.Select(x => new InternedString(x)).ToArray());
                }

                if (!string.IsNullOrEmpty(alias) || aliases != null)
                {
                    var aliasesList = new List<string>();
                    if (!string.IsNullOrEmpty(alias))
                        aliasesList.Add(alias);
                    if (aliases != null)
                        aliasesList.AddRange(aliases);
                    template.aliases = new ReadOnlyArray<InternedString>(aliasesList.Select(x => new InternedString(x)).ToArray());
                }

                if (!string.IsNullOrEmpty(parameters))
                    template.parameters = new ReadOnlyArray<ParameterValue>(ParseParameters(parameters));

                if (!string.IsNullOrEmpty(processors))
                    template.processors = new ReadOnlyArray<NameAndParameters>(ParseNameAndParameterList(processors));

                return template;
            }

            public static ControlTemplateJson[] FromControlTemplates(ControlTemplate[] templates)
            {
                if (templates == null)
                    return null;

                var count = templates.Length;
                var result = new ControlTemplateJson[count];

                for (var i = 0; i < count; ++i)
                {
                    var template = templates[i];
                    result[i] = new ControlTemplateJson
                    {
                        name = template.name,
                        template = template.template,
                        variant = template.variant,
                        displayName = template.displayName,
                        resourceName = template.resourceName,
                        bit = template.bit,
                        offset = template.offset,
                        sizeInBits = template.sizeInBits,
                        format = template.format.ToString(),
                        parameters = string.Join(",", template.parameters.Select(x => x.ToString()).ToArray()),
                        processors = string.Join(",", template.processors.Select(x => x.ToString()).ToArray()),
                        usages = template.usages.Select(x => x.ToString()).ToArray(),
                        aliases = template.aliases.Select(x => x.ToString()).ToArray(),
                        noisy = template.isNoisy
                    };
                }

                return result;
            }
        }

        [Serializable]
        private struct DeviceDescriptionJson
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

            public static DeviceDescriptionJson FromDescription(InputDeviceDescription description)
            {
                return new DeviceDescriptionJson
                {
                    @interface = description.interfaceName,
                    deviceClass = description.deviceClass,
                    manufacturer = description.manufacturer,
                    product = description.product,
                    version = description.version
                };
            }

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

                    return part;
                }

                if (regex[regex.Length - 1] != ')')
                    return string.Format("({0})|({1}", regex, part);

                return string.Format("{0}|({1})", regex, part);
            }
        }


        internal struct Collection
        {
            public Dictionary<InternedString, Type> templateTypes;
            public Dictionary<InternedString, string> templateStrings;
            public Dictionary<InternedString, Constructor> templateConstructors;
            public Dictionary<InternedString, InternedString> baseTemplateTable;

            public void Allocate()
            {
                templateTypes = new Dictionary<InternedString, Type>();
                templateStrings = new Dictionary<InternedString, string>();
                templateConstructors = new Dictionary<InternedString, Constructor>();
                baseTemplateTable = new Dictionary<InternedString, InternedString>();
            }

            public bool HasTemplate(InternedString name)
            {
                return templateTypes.ContainsKey(name) || templateStrings.ContainsKey(name) ||
                    templateConstructors.ContainsKey(name);
            }

            private InputTemplate TryLoadTemplateInternal(InternedString name)
            {
                // Check constructors.
                Constructor constructor;
                if (templateConstructors.TryGetValue(name, out constructor))
                    return (InputTemplate)constructor.method.Invoke(constructor.instance, null);

                // See if we have a string template for it. These
                // always take precedence over ones from type so that we can
                // override what's in the code using data.
                string json;
                if (templateStrings.TryGetValue(name, out json))
                    return FromJson(json);

                // No, but maybe we have a type template for it.
                Type type;
                if (templateTypes.TryGetValue(name, out type))
                    return FromType(name, type);

                return null;
            }

            public InputTemplate TryLoadTemplate(InternedString name, Dictionary<InternedString, InputTemplate> table = null)
            {
                var template = TryLoadTemplateInternal(name);
                if (template != null)
                {
                    template.m_Name = name;
                    if (table != null)
                        table[name] = template;

                    // If the template extends another template, we need to merge the
                    // base template into the final template.
                    // NOTE: We go through the baseTemplateTable here instead of looking at
                    //       the extendsTemplate property so as to make this work for all types
                    //       of templates (FromType() does not set the property, for example).
                    var baseTemplateName = new InternedString();
                    if (baseTemplateTable.TryGetValue(name, out baseTemplateName))
                    {
                        ////TODO: catch cycles
                        var baseTemplate = TryLoadTemplate(baseTemplateName, table);
                        if (baseTemplate == null)
                            throw new TemplateNotFoundException(string.Format(
                                    "Cannot find base template '{0}' of template '{1}'", baseTemplateName, name));
                        template.MergeTemplate(baseTemplate);
                        template.m_ExtendsTemplate = baseTemplateName;
                    }
                }

                return template;
            }

            // Return name of template at root of "extend" chain of given template.
            public InternedString GetRootTemplateName(InternedString templateName)
            {
                InternedString baseTemplate;
                while (baseTemplateTable.TryGetValue(templateName, out baseTemplate))
                    templateName = baseTemplate;
                return templateName;
            }

            // Get the type which will be instantiated for the given template.
            // Returns null if no template with the given name exists.
            public Type GetControlTypeForTemplate(InternedString templateName)
            {
                // Try template strings.
                while (templateStrings.ContainsKey(templateName))
                {
                    InternedString baseTemplate;
                    if (baseTemplateTable.TryGetValue(templateName, out baseTemplate))
                    {
                        // Work our way up the inheritance chain.
                        templateName = baseTemplate;
                    }
                    else
                    {
                        // Template doesn't extend anything and ATM we don't support setting
                        // types explicitly from JSON templates. So has to be InputDevice.
                        return typeof(InputDevice);
                    }
                }

                // Try template types.
                Type result;
                templateTypes.TryGetValue(templateName, out result);
                return result;
            }
        }

        // This collection is owned and managed by InputManager.
        internal static Collection s_Templates;

        internal struct Constructor
        {
            public MethodInfo method;
            public object instance;
        }

        internal class TemplateNotFoundException : Exception
        {
            public string template { get; private set; }
            public TemplateNotFoundException(string name, string message = null)
                : base(message ?? string.Format("Cannot find template '{0}'", name))
            {
                template = name;
            }
        }

        // Constructs InputTemplate instances and caches them.
        internal struct Cache
        {
            public Collection templates;
            public Dictionary<InternedString, InputTemplate> table;

            public InputTemplate FindOrLoadTemplate(string name)
            {
                var internedName = new InternedString(name);

                // See if we have it cached.
                InputTemplate template;
                if (table != null && table.TryGetValue(internedName, out template))
                    return template;

                if (table == null)
                    table = new Dictionary<InternedString, InputTemplate>();

                template = templates.TryLoadTemplate(internedName, table);
                if (template != null)
                    return template;

                // Nothing.
                throw new TemplateNotFoundException(name);
            }
        }
    }
}
