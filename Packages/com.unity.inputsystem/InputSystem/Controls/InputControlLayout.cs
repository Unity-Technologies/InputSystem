using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

#if !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0 || UNITY_WSA)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

////TODO: allow layouts to set default device names

////TODO: allow creating generic controls as parents just to group child controls

////TODO: allow things like "-something" and "+something" for usages, processors, etc

////TODO: change interactions and processors to use kSeparator

////TODO: allow setting whether the device should automatically become current and whether it wants noise filtering

////TODO: turn 'overrides' into feature where layouts can be registered as overrides and they get merged *into* the layout
////      they are overriding

////TODO: ensure that if a layout sets a device description, it is indeed a device layout

////TODO: make offset on InputControlAttribute relative to field instead of relative to entire state struct

////REVIEW: common usages are on all layouts but only make sense for devices

////REVIEW: kill layout namespacing for remotes and have remote players instantiate layouts from editor instead?
////        loses the ability for layouts to be different in the player than in the editor but if we take it as granted that
////           a) a given layout X always is the same regardless to which player it is deployed, and that
////           b) the editor always has all layouts
////        then we can just kill off the entire namespacing. This also makes it much easier to tweak layouts in the
////        editor.

namespace UnityEngine.Experimental.Input.Layouts
{
    /// <summary>
    /// A control layout specifies the composition of an input control.
    /// </summary>
    /// <remarks>
    /// Control layouts can be created in three ways:
    ///
    /// <list type="number">
    /// <item><description>Loaded from JSON.</description></item>
    /// <item><description>Constructed through reflection from <see cref="InputControl">InputControls</see> classes.</description></item>
    /// <item><description>Through layout factories using <see cref="InputControlLayout.Builder"/>.</description></item>
    /// </list>
    ///
    /// Once constructed, control layouts are immutable (but you can always
    /// replace a registered layout in the system and it will affect
    /// everything constructed from the layout).
    ///
    /// Control layouts can be for arbitrary control rigs or for entire
    /// devices. Device layouts can be matched to <see cref="InputDeviceDescription">
    /// device description</see> using associated <see cref="InputDeviceMatcher">
    /// device matchers</see>.
    ///
    /// InputControlLayout objects are considered temporaries. Except in the
    /// editor, they are not kept around beyond device creation.
    /// </remarks>
    public class InputControlLayout
    {
        // String that is used to separate names from namespaces in layout names.
        public const string kNamespaceQualifier = "::";

        /// <summary>
        /// The "None" layout is a reserved layout name which signals to the system
        /// that no layout should be used (and thus no device should be created).
        /// </summary>
        public const string kNone = "None";

        public const char kListSeparator = ';';
        public const string kListSeparatorString = ";";

        private static InternedString s_DefaultVariant = new InternedString("Default");
        public static InternedString DefaultVariant
        {
            get { return s_DefaultVariant; }
        }

        ////TODO: replace ParameterValue with PrimitiveValueOrArray

        public enum ParameterType
        {
            Boolean,
            Integer,
            Float
        }

        // Both controls and processors can have public fields that can be set
        // directly from layouts. The values are usually specified in strings
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

            public void SetValue(string value)
            {
                fixed(byte* ptr = this.value)
                {
                    switch (type)
                    {
                        case ParameterType.Boolean:
                            bool result;
                            if (bool.TryParse(value, out result))
                            {
                                (*(bool*)ptr) = result;
                            }
                            break;
                        case ParameterType.Integer:
                            int intResult;
                            if (int.TryParse(value, out intResult))
                            {
                                (*(int*)ptr) = intResult;
                            }
                            break;
                        case ParameterType.Float:
                            float floatResult;
                            if (float.TryParse(value, out floatResult))
                            {
                                (*(float*)ptr) = floatResult;
                            }
                            break;
                    }
                }
            }

            public string GetValueAsString()
            {
                fixed(byte* ptr = value)
                {
                    switch (type)
                    {
                        case ParameterType.Boolean:
                            if (*((bool*)ptr))
                                return "true";
                            return "false";
                        case ParameterType.Integer:
                            var intValue = *((int*)ptr);
                            return "" + intValue;
                        case ParameterType.Float:
                            var floatValue = *((float*)ptr);
                            return "" + floatValue;
                    }
                }

                return string.Empty;
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
                            ////FIXME: this needs to be invariant culture
                            var floatValue = *((float*)ptr);
                            return string.Format("{0}={1}", name, floatValue);
                    }
                }

                return string.Empty;
            }

            public bool IsDefaultValue()
            {
                fixed(byte* ptr = value)
                {
                    switch (type)
                    {
                        case ParameterType.Boolean:
                            return *((bool*)ptr) == default(bool);
                        case ParameterType.Integer:
                            var intValue = *((int*)ptr);
                            return intValue == default(int);
                        case ParameterType.Float:
                            var floatValue = *((float*)ptr);
                            return floatValue == default(float);
                    }
                }
                return false;
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
                return string.Format("{0}({1})", name, parameterString);
            }
        }

        /// <summary>
        /// Specification for the composition of a direct or indirect child control.
        /// </summary>
        public struct ControlItem
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
            /// inside another layout and modify properties of a control inside
            /// of it. An example for this is adding a "leftStick" control using the
            /// Stick layout and then adding two control layouts that refer to
            /// "leftStick/x" and "leftStick/y" respectively to modify the state
            /// format used by the stick.
            ///
            /// This field is required.
            /// </remarks>
            /// <seealso cref="isModifyingChildControlByPath"/>
            public InternedString name;

            public InternedString layout;
            public InternedString variants;
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
            public int arraySize;

            /// <summary>
            /// Optional default value for the state memory associated with the control.
            /// </summary>
            public PrimitiveValueOrArray defaultState;

            // If true, the layout will not add a control but rather a modify a control
            // inside the hierarchy added by 'layout'. This allows, for example, to modify
            // just the X axis control of the left stick directly from within a gamepad
            // layout instead of having to have a custom stick layout for the left stick
            // than in turn would have to make use of a custom axis layout for the X axis.
            // Insted, you can just have a control layout with the name "leftStick/x".
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

            public bool isArray
            {
                get { return (arraySize != 0); }
            }

            /// <summary>
            /// For any property not set on this control layout, take the setting from <paramref name="other"/>.
            /// </summary>
            /// <param name="other">Control layout providing settings.</param>
            /// <remarks>
            /// <see cref="name"/> will not be touched.
            /// </remarks>
            public ControlItem Merge(ControlItem other)
            {
                var result = new ControlItem();

                result.name = name;
                Debug.Assert(!name.IsEmpty());
                result.isModifyingChildControlByPath = isModifyingChildControlByPath;

                result.layout = layout.IsEmpty() ? other.layout : layout;
                result.variants = variants.IsEmpty() ? other.variants : variants;
                result.useStateFrom = useStateFrom ?? other.useStateFrom;
                result.arraySize = !isArray ? other.arraySize : arraySize;

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

                if (aliases.Count > 0)
                    result.aliases = aliases;
                else
                    result.aliases = other.aliases;

                if (usages.Count > 0)
                    result.usages = usages;
                else
                    result.usages = other.usages;

                if (parameters.Count == 0)
                    result.parameters = other.parameters;
                else
                    result.parameters = parameters;

                if (processors.Count == 0)
                    result.processors = other.processors;
                else
                    result.processors = processors;

                if (!string.IsNullOrEmpty(displayName))
                    result.displayName = displayName;
                else
                    result.displayName = other.displayName;

                if (!string.IsNullOrEmpty(resourceName))
                    result.resourceName = resourceName;
                else
                    result.resourceName = other.resourceName;

                if (!defaultState.isEmpty)
                    result.defaultState = defaultState;
                else
                    result.defaultState = other.defaultState;

                return result;
            }
        }

        // Unique name of the layout.
        // NOTE: Case-insensitive.
        public InternedString name
        {
            get { return m_Name; }
        }

        public Type type
        {
            get { return m_Type; }
        }

        public InternedString variants
        {
            get { return m_Variants; }
        }

        public FourCC stateFormat
        {
            get { return m_StateFormat; }
        }

        public IEnumerable<InternedString> baseLayouts
        {
            get { return m_BaseLayouts; }
        }

        public IEnumerable<InternedString> appliedOverrides
        {
            get { return m_AppliedOverrides; }
        }

        public ReadOnlyArray<InternedString> commonUsages
        {
            get { return new ReadOnlyArray<InternedString>(m_CommonUsages); }
        }

        public ReadOnlyArray<ControlItem> controls
        {
            get { return new ReadOnlyArray<ControlItem>(m_Controls); }
        }

        public bool updateBeforeRender
        {
            get { return m_UpdateBeforeRender.HasValue ? m_UpdateBeforeRender.Value : false; }
        }

        public bool isDeviceLayout
        {
            get { return typeof(InputDevice).IsAssignableFrom(m_Type); }
        }

        public bool isControlLayout
        {
            get { return !isDeviceLayout; }
        }

        public ControlItem this[string path]
        {
            get
            {
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentNullException("path");

                if (m_Controls != null)
                {
                    for (var i = 0; i < m_Controls.Length; ++i)
                    {
                        if (m_Controls[i].name == path)
                            return m_Controls[i];
                    }
                }

                throw new KeyNotFoundException(string.Format("Cannot find control '{0}' in layout '{1}'", path, name));
            }
        }

        /// <summary>
        /// Build a layout programmatically. Primarily for use by layout builders
        /// registered with the system.
        /// </summary>
        /// <seealso cref="InputSystem.RegisterLayoutBuilder"/>
        public struct Builder
        {
            public string name;
            public Type type;
            public FourCC stateFormat;
            public string extendsLayout;
            public bool? updateBeforeRender;

            private int m_ControlCount;
            private ControlItem[] m_Controls;

            public ReadOnlyArray<ControlItem> controls
            {
                get { return new ReadOnlyArray<ControlItem>(m_Controls, 0, m_ControlCount);}
            }

            public struct ControlBuilder
            {
                internal Builder builder;
                internal ControlItem[] controls;
                internal int index;

                public ControlBuilder WithLayout(string layout)
                {
                    if (string.IsNullOrEmpty(layout))
                        throw new ArgumentException("Layout name cannot be null or empty", "layout");

                    controls[index].layout = new InternedString(layout);
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

                public ControlBuilder WithByteOffset(uint offset)
                {
                    controls[index].offset = offset;
                    return this;
                }

                public ControlBuilder WithBitOffset(uint bit)
                {
                    controls[index].bit = bit;
                    return this;
                }

                public ControlBuilder WithSizeInBits(uint sizeInBits)
                {
                    controls[index].sizeInBits = sizeInBits;
                    return this;
                }

                public ControlBuilder WithUsages(params InternedString[] usages)
                {
                    if (usages == null || usages.Length == 0)
                        return this;

                    for (var i = 0; i < usages.Length; ++i)
                        if (usages[i].IsEmpty())
                            throw new ArgumentException(
                                string.Format("Empty usage entry at index {0} for control '{1}' in layout '{2}'", i,
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

                public ControlBuilder WithParameters(string parameters)
                {
                    var parsed = ParseParameters(parameters);
                    controls[index].parameters = new ReadOnlyArray<ParameterValue>(parsed);
                    return this;
                }

                public ControlBuilder WithDefaultState(PrimitiveValue value)
                {
                    controls[index].defaultState = new PrimitiveValueOrArray(value);
                    return this;
                }

                public ControlBuilder WithDefaultState(PrimitiveValueOrArray value)
                {
                    controls[index].defaultState = value;
                    return this;
                }

                public ControlBuilder AsArrayOfControlsWithSize(int arraySize)
                {
                    controls[index].arraySize = arraySize;
                    return this;
                }
            }

            // This invalidates the ControlBuilders from previous calls! (our array may move)
            /// <summary>
            /// Add a new control to the layout.
            /// </summary>
            /// <param name="name">Name or path of the control. If it is a path (e.g. <c>"leftStick/x"</c>,
            /// then the control either modifies the setup of a child control of another control in the layout
            /// or adds a new child control to another control in the layout. Modifying child control is useful,
            /// for example, to alter the state format of controls coming from the base layout. Likewise,
            /// adding child controls to another control is useful to modify the setup of of the control layout
            /// being used without having to create and register a custom control layout.</param>
            /// <returns>A control builder that permits setting various parameters on the control.</returns>
            /// <exception cref="ArgumentException"><paramref name="name"/> is null or empty.</exception>
            public ControlBuilder AddControl(string name)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException(name);

                var index = ArrayHelpers.AppendWithCapacity(ref m_Controls, ref m_ControlCount,
                    new ControlItem
                    {
                        name = new InternedString(name),
                        isModifyingChildControlByPath = name.IndexOf('/') != -1,
                    });

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

            public Builder Extend(string baseLayoutName)
            {
                extendsLayout = baseLayoutName;
                return this;
            }

            public InputControlLayout Build()
            {
                ControlItem[] controls = null;
                if (m_ControlCount > 0)
                {
                    controls = new ControlItem[m_ControlCount];
                    Array.Copy(m_Controls, controls, m_ControlCount);
                }

                // Allow layout to be unnamed. The system will automatically set the
                // name that the layout has been registered under.
                var layout =
                    new InputControlLayout(new InternedString(name),
                        type == null && string.IsNullOrEmpty(extendsLayout) ? typeof(InputDevice) : type)
                {
                    m_StateFormat = stateFormat,
                    m_BaseLayouts = new InlinedArray<InternedString>(new InternedString(extendsLayout)),
                    m_Controls = controls,
                    m_UpdateBeforeRender = updateBeforeRender
                };

                return layout;
            }
        }

        // Uses reflection to construct a layout from the given type.
        // Can be used with both control classes and state structs.
        public static InputControlLayout FromType(string name, Type type)
        {
            var controlLayouts = new List<ControlItem>();
            var layoutAttribute = type.GetCustomAttribute<InputControlLayoutAttribute>(true);

            // If there's an InputControlLayoutAttribute on the type that has 'stateType' set,
            // add control layouts from its state (if present) instead of from the type.
            var stateFormat = new FourCC();
            if (layoutAttribute != null && layoutAttribute.stateType != null)
            {
                AddControlItems(layoutAttribute.stateType, controlLayouts, name);

                // Get state type code from state struct.
                if (typeof(IInputStateTypeInfo).IsAssignableFrom(layoutAttribute.stateType))
                {
                    stateFormat = ((IInputStateTypeInfo)Activator.CreateInstance(layoutAttribute.stateType))
                        .GetFormat();
                }
            }
            else
            {
                // Add control layouts from type contents.
                AddControlItems(type, controlLayouts, name);
            }

            if (layoutAttribute != null && !string.IsNullOrEmpty(layoutAttribute.stateFormat))
                stateFormat = new FourCC(layoutAttribute.stateFormat);

            // Determine variants (if any).
            var variants = new InternedString();
            if (layoutAttribute != null)
                variants = new InternedString(layoutAttribute.variants);

            ////TODO: make sure all usages are unique (probably want to have a check method that we can run on json layouts as well)
            ////TODO: make sure all paths are unique (only relevant for JSON layouts?)

            // Create layout object.
            var layout = new InputControlLayout(name, type);
            layout.m_Controls = controlLayouts.ToArray();
            layout.m_StateFormat = stateFormat;
            layout.m_Variants = variants;

            if (layoutAttribute != null && layoutAttribute.commonUsages != null)
                layout.m_CommonUsages =
                    ArrayHelpers.Select(layoutAttribute.commonUsages, x => new InternedString(x));

            return layout;
        }

        public string ToJson()
        {
            var layout = LayoutJson.FromLayout(this);
            return JsonUtility.ToJson(layout, true);
        }

        // Constructs a layout from the given JSON source.
        public static InputControlLayout FromJson(string json)
        {
            var layoutJson = JsonUtility.FromJson<LayoutJson>(json);
            return layoutJson.ToLayout();
        }

        ////REVIEW: shouldn't state be split between input and output? how does output fit into the layout picture in general?
        ////        should the control layout alone determine the direction things are going in?

        private InternedString m_Name;
        internal Type m_Type; // For extension chains, we can only discover types after loading multiple layouts, so we make this accessible to InputDeviceBuilder.
        internal InternedString m_Variants;
        internal FourCC m_StateFormat;
        internal int m_StateSizeInBytes; // Note that this is the combined state size for input and output.
        internal bool? m_UpdateBeforeRender;
        internal InlinedArray<InternedString> m_BaseLayouts;
        private InlinedArray<InternedString> m_AppliedOverrides;
        private InternedString[] m_CommonUsages;
        internal ControlItem[] m_Controls;
        internal string m_DisplayName;
        internal string m_ResourceName;

        private InputControlLayout(string name, Type type)
        {
            m_Name = new InternedString(name);
            m_Type = type;
        }

        private static void AddControlItems(Type type, List<ControlItem> controlLayouts, string layoutName)
        {
            AddControlItemsFromFields(type, controlLayouts, layoutName);
            AddControlItemsFromProperties(type, controlLayouts, layoutName);
        }

        // Add ControlLayouts for every public property in the given type thas has
        // InputControlAttribute applied to it or has an InputControl-derived value type.
        private static void AddControlItemsFromFields(Type type, List<ControlItem> controlLayouts, string layoutName)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            AddControlItemsFromMembers(fields, controlLayouts, layoutName);
        }

        // Add ControlLayouts for every public property in the given type thas has
        // InputControlAttribute applied to it or has an InputControl-derived value type.
        private static void AddControlItemsFromProperties(Type type, List<ControlItem> controlLayouts, string layoutName)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            AddControlItemsFromMembers(properties, controlLayouts, layoutName);
        }

        // Add ControlLayouts for every member in the list thas has InputControlAttribute applied to it
        // or has an InputControl-derived value type.
        private static void AddControlItemsFromMembers(MemberInfo[] members, List<ControlItem> controlItems, string layoutName)
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
                    var controlCountBefore = controlItems.Count;

                    AddControlItems(valueType, controlItems, layoutName);

                    // If the current member is a field that is embedding the state structure, add
                    // the field offset to all control layouts that were added from the struct.
                    var memberAsField = member as FieldInfo;
                    if (memberAsField != null)
                    {
                        var fieldOffset = Marshal.OffsetOf(member.DeclaringType, member.Name).ToInt32();
                        var countrolCountAfter = controlItems.Count;
                        for (var i = controlCountBefore; i < countrolCountAfter; ++i)
                        {
                            var controlLayout = controlItems[i];
                            if (controlItems[i].offset != InputStateBlock.kInvalidOffset)
                            {
                                controlLayout.offset += (uint)fieldOffset;
                                controlItems[i] = controlLayout;
                            }
                        }
                    }

                    ////TODO: allow attributes on the member to modify control layouts inside the struct
                }

                // Look for InputControlAttributes. If they aren't there, the member has to be
                // of an InputControl-derived value type.
                var attributes = member.GetCustomAttributes<InputControlAttribute>(false).ToArray();
                if (attributes.Length == 0)
                {
                    if (valueType == null || !typeof(InputControl).IsAssignableFrom(valueType))
                        continue;
                }

                AddControlItemsFromMember(member, attributes, controlItems, layoutName);
            }
        }

        private static void AddControlItemsFromMember(MemberInfo member,
            InputControlAttribute[] attributes, List<ControlItem> controlItems, string layoutName)
        {
            // InputControlAttribute can be applied multiple times to the same member,
            // generating a separate control for each ocurrence. However, it can also
            // not be applied at all in which case we still add a control layout (the
            // logic that called us already made sure the member is eligible for this kind
            // of operation).

            if (attributes.Length == 0)
            {
                var controlLayout = CreateControlItemFromMember(member, null, layoutName);
                ThrowIfControlItemIsDuplicate(ref controlLayout, controlItems, layoutName);
                controlItems.Add(controlLayout);
            }
            else
            {
                foreach (var attribute in attributes)
                {
                    var controlLayout = CreateControlItemFromMember(member, attribute, layoutName);
                    ThrowIfControlItemIsDuplicate(ref controlLayout, controlItems, layoutName);
                    controlItems.Add(controlLayout);
                }
            }
        }

        private static ControlItem CreateControlItemFromMember(MemberInfo member, InputControlAttribute attribute, string layoutName)
        {
            ////REVIEW: make sure that the value type of the field and the value type of the control match?

            // Determine name.
            var name = attribute != null ? attribute.name : null;
            if (string.IsNullOrEmpty(name))
                name = member.Name;

            var isModifyingChildControlByPath = name.IndexOf('/') != -1;

            // Determine layout.
            var layout = attribute != null ? attribute.layout : null;
            if (string.IsNullOrEmpty(layout) && !isModifyingChildControlByPath &&
                (!(member is FieldInfo) || member.GetCustomAttribute<FixedBufferAttribute>(false) == null)) // Ignore fixed buffer fields.
            {
                var valueType = TypeHelpers.GetValueType(member);
                layout = InferLayoutFromValueType(valueType);
            }

            // Determine variants.
            string variants = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.variants))
                variants = attribute.variants;

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
                ////REVIEW: this logic makes it hard to inherit settings from the base layout; if we do this stuff,
                ////        we should probably do it in InputDeviceBuilder and not directly on the layout
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

            // Determine array size.
            var arraySize = 0;
            if (attribute != null)
                arraySize = attribute.arraySize;

            // Determine default state.
            var defaultState = new PrimitiveValueOrArray();
            if (attribute != null)
                defaultState = PrimitiveValueOrArray.FromObject(attribute.defaultState);

            return new ControlItem
            {
                name = new InternedString(name),
                layout = new InternedString(layout),
                variants = new InternedString(variants),
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
                arraySize = arraySize,
                defaultState = defaultState,
            };
        }

        internal static NameAndParameters[] ParseNameAndParameterList(string text)
        {
            List<NameAndParameters> list = null;
            if (!ParseNameAndParameterList(text, ref list))
                return null;
            return list.ToArray();
        }

        internal static bool ParseNameAndParameterList(string text, ref List<NameAndParameters> list)
        {
            text = text.Trim();
            if (string.IsNullOrEmpty(text))
                return false;

            if (list == null)
                list = new List<NameAndParameters>();
            else
                list.Clear();

            var index = 0;
            var textLength = text.Length;

            while (index < textLength)
                list.Add(ParseNameAndParameters(text, ref index));

            return true;
        }

        internal static NameAndParameters ParseNameAndParameters(string text)
        {
            var index = 0;
            return ParseNameAndParameters(text, ref index);
        }

        private static NameAndParameters ParseNameAndParameters(string text, ref int index)
        {
            var textLength = text.Length;

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

            return new NameAndParameters {name = name, parameters = new ReadOnlyArray<ParameterValue>(parameters)};
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
                if (string.Compare(value, "true", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    parameter.type = ParameterType.Boolean;
                    *((bool*)parameter.value) = true;
                }
                else if (string.Compare(value, "false", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    parameter.type = ParameterType.Boolean;
                    *((bool*)parameter.value) = false;
                }
                else if (value.IndexOf('.') != -1)
                {
                    parameter.type = ParameterType.Float;
                    *((float*)parameter.value) = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                }
                else
                {
                    parameter.type = ParameterType.Integer;
                    *((int*)parameter.value) = int.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                }
            }

            if (index < parameterStringLength && parameterString[index] == ',')
                ++index;

            return parameter;
        }

        ////REVIEW: this tends to cause surprises; is it worth its cost?
        private static string InferLayoutFromValueType(Type type)
        {
            var typeName = type.Name;
            if (typeName.EndsWith("Control"))
                return typeName.Substring(0, typeName.Length - "Control".Length);
            if (!type.IsPrimitive)
                return typeName;
            return null;
        }

        /// <summary>
        /// Merge the settings from <paramref name="other"/> into the layout such that they become
        /// the base settings.
        /// </summary>
        /// <param name="other"></param>
        /// <remarks>
        /// This is the central method for allowing layouts to 'inherit' settings from their
        /// base layout. It will merge the information in <paramref name="other"/> into the current
        /// layout such that the existing settings in the current layout acts as if applied on top
        /// of the settings in the base layout.
        /// </remarks>
        public void MergeLayout(InputControlLayout other)
        {
            m_UpdateBeforeRender = m_UpdateBeforeRender ?? other.m_UpdateBeforeRender;

            if (m_Variants.IsEmpty())
                m_Variants = other.m_Variants;

            // Determine type. Basically, if the other layout's type is more specific
            // than our own, we switch to that one. Otherwise we stay on our own type.
            if (m_Type == null)
                m_Type = other.m_Type;
            else if (m_Type.IsAssignableFrom(other.m_Type))
                m_Type = other.m_Type;

            // If the layout has variants set on it, we want to merge away information coming
            // from 'other' than isn't relevant to those variants.
            var layoutIsTargetingSpecificVariants = !m_Variants.IsEmpty();

            if (m_StateFormat == new FourCC())
                m_StateFormat = other.m_StateFormat;

            if (string.IsNullOrEmpty(m_DisplayName))
                m_DisplayName = other.m_DisplayName;
            if (string.IsNullOrEmpty(m_ResourceName))
                m_ResourceName = other.m_ResourceName;

            // Combine common usages.
            m_CommonUsages = ArrayHelpers.Merge(other.m_CommonUsages, m_CommonUsages);

            // Retain list of overrides.
            m_AppliedOverrides.Merge(other.m_AppliedOverrides);

            // Merge controls.
            if (m_Controls == null)
            {
                m_Controls = other.m_Controls;
            }
            else if (other.m_Controls != null)
            {
                var baseControls = other.m_Controls;

                // Even if the counts match we don't know how many controls are in the
                // set until we actually gone through both control lists and looked at
                // the names.

                var controls = new List<ControlItem>();
                var baseControlVariants = new List<string>();

                ////REVIEW: should setting variants directly on a layout force that variant to automatically
                ////        be set on every control item directly defined in that layout?

                var baseControlTable = CreateLookupTableForControls(baseControls, baseControlVariants);
                var thisControlTable = CreateLookupTableForControls(m_Controls);

                // First go through every control we have in this layout. Add every control from
                // `thisControlTable` while removing corresponding control items from `baseControlTable`.
                foreach (var pair in thisControlTable)
                {
                    ControlItem baseControlItem;
                    if (baseControlTable.TryGetValue(pair.Key, out baseControlItem))
                    {
                        var mergedLayout = pair.Value.Merge(baseControlItem);
                        controls.Add(mergedLayout);

                        // Remove the entry so we don't hit it again in the pass through
                        // baseControlTable below.
                        baseControlTable.Remove(pair.Key);
                    }
                    ////REVIEW: is this really the most useful behavior?
                    // We may be looking at a control that is using variants on the base layout but
                    // isn't targeting specific variants on the derived layout. In that case, we
                    // want to take each of the variants from the base layout and merge them with
                    // the control layout in the derived layout.
                    else if (pair.Value.variants.IsEmpty() || pair.Value.variants == DefaultVariant)
                    {
                        var isTargetingVariants = false;
                        if (layoutIsTargetingSpecificVariants)
                        {
                            // We're only looking for specific variants so try only that those.
                            for (var i = 0; i < baseControlVariants.Count; ++i)
                            {
                                if (VariantsMatch(m_Variants.ToLower(), baseControlVariants[i]))
                                {
                                    var key = string.Format("{0}@{1}", pair.Key, baseControlVariants[i]);
                                    if (baseControlTable.TryGetValue(key, out baseControlItem))
                                    {
                                        var mergedLayout = pair.Value.Merge(baseControlItem);
                                        controls.Add(mergedLayout);
                                        baseControlTable.Remove(key);
                                        isTargetingVariants = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Try each variants present in the base layout.
                            foreach (var variant in baseControlVariants)
                            {
                                var key = string.Format("{0}@{1}", pair.Key, variant);
                                if (baseControlTable.TryGetValue(key, out baseControlItem))
                                {
                                    var mergedLayout = pair.Value.Merge(baseControlItem);
                                    controls.Add(mergedLayout);
                                    baseControlTable.Remove(key);
                                    isTargetingVariants = true;
                                }
                            }
                        }

                        // Okay, this control item isn't corresponding to anything in the base layout
                        // so just add it as is.
                        if (!isTargetingVariants)
                            controls.Add(pair.Value);
                    }
                    // We may be looking at a control that is targeting a specific variant
                    // in this layout but not targeting a variant in the base layout. We still want to
                    // merge information from that non-targeted base control.
                    else if (baseControlTable.TryGetValue(pair.Value.name.ToLower(), out baseControlItem))
                    {
                        var mergedLayout = pair.Value.Merge(baseControlItem);
                        controls.Add(mergedLayout);
                        baseControlTable.Remove(pair.Value.name.ToLower());
                    }
                    // Seems like we can't match it to a control in the base layout. We already know it
                    // must have a variants setting (because we checked above) so if the variants setting
                    // doesn't prevent us, just include the control. It's most likely a path-modifying
                    // control (e.g. "rightStick/x").
                    else if (VariantsMatch(m_Variants, pair.Value.variants))
                    {
                        controls.Add(pair.Value);
                    }
                }

                // And then go through all the controls in the base and take the
                // ones we're missing. We've already removed all the ones that intersect
                // and had to be merged so the rest we can just slurp into the list as is.
                if (!layoutIsTargetingSpecificVariants)
                {
                    controls.AddRange(baseControlTable.Values);
                }
                else
                {
                    // Filter out controls coming from the base layout which are targeting variants
                    // that we're not interested in.
                    controls.AddRange(
                        baseControlTable.Values.Where(x => VariantsMatch(m_Variants, x.variants)));
                }

                m_Controls = controls.ToArray();
            }
        }

        private static Dictionary<string, ControlItem> CreateLookupTableForControls(
            ControlItem[] controlItems, List<string> variants = null)
        {
            var table = new Dictionary<string, ControlItem>();
            for (var i = 0; i < controlItems.Length; ++i)
            {
                var key = controlItems[i].name.ToLower();
                // Need to take variants into account as well. Otherwise two variants for
                // "leftStick", for example, will overwrite each other.
                var itemVariants = controlItems[i].variants;
                if (!itemVariants.IsEmpty() && itemVariants != DefaultVariant)
                {
                    // If there's multiple variants on the control, we add it to the table multiple times.
                    if (itemVariants.ToString().IndexOf(kListSeparator) != -1)
                    {
                        var itemVariantArray = itemVariants.ToLower().Split(kListSeparator);
                        foreach (var name in itemVariantArray)
                        {
                            if (variants != null)
                                variants.Add(name);
                            key = string.Format("{0}@{1}", key, name);
                            table[key] = controlItems[i];
                        }

                        continue;
                    }

                    key = string.Format("{0}@{1}", key, itemVariants.ToLower());
                    if (variants != null)
                        variants.Add(itemVariants.ToLower());
                }
                table[key] = controlItems[i];
            }
            return table;
        }

        internal static bool VariantsMatch(InternedString expected, InternedString actual)
        {
            return VariantsMatch(expected.ToLower(), actual.ToLower());
        }

        internal static bool VariantsMatch(string expected, string actual)
        {
            ////REVIEW: does this make sense?
            // Default variant works with any other expected variant.
            if (actual != null &&
                StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(DefaultVariant, actual, kListSeparator))
                return true;

            // If we don't expect a specific variant, we accept any variant.
            if (expected == null)
                return true;

            // If we there's no variant set on what we actual got, then it matches even if we
            // expect specific variants.
            if (actual == null)
                return true;

            // Match if the two variant sets intersect on at least one element.
            return StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(expected, actual, kListSeparator);
        }

        private static void ThrowIfControlItemIsDuplicate(ref ControlItem controlItem,
            IEnumerable<ControlItem> controlLayouts, string layoutName)
        {
            var name = controlItem.name;
            foreach (var existing in controlLayouts)
                if (string.Compare(name, existing.name, StringComparison.OrdinalIgnoreCase) == 0 &&
                    existing.variants == controlItem.variants)
                    throw new Exception(string.Format("Duplicate control '{0}' in layout '{1}'", name, layoutName));
        }

        internal static void ParseHeaderFieldsFromJson(string json, out InternedString name,
            out InlinedArray<InternedString> baseLayouts, out InputDeviceMatcher deviceMatcher)
        {
            var header = JsonUtility.FromJson<LayoutJsonNameAndDescriptorOnly>(json);
            name = new InternedString(header.name);

            baseLayouts = new InlinedArray<InternedString>();
            if (!string.IsNullOrEmpty(header.extend))
                baseLayouts.Append(new InternedString(header.extend));
            if (header.extendMultiple != null)
                foreach (var item in header.extendMultiple)
                    baseLayouts.Append(new InternedString(item));

            deviceMatcher = header.device.ToMatcher();
        }

        [Serializable]
        internal struct LayoutJsonNameAndDescriptorOnly
        {
            public string name;
            public string extend;
            public string[] extendMultiple;
            public InputDeviceMatcher.MatcherJson device;
        }

        [Serializable]
        private struct LayoutJson
        {
            // Disable warnings that these fields are never assigned to. They are set
            // by JsonUtility.
            #pragma warning disable 0649
            // ReSharper disable MemberCanBePrivate.Local

            public string name;
            public string extend;
            public string[] extendMultiple;
            public string format;
            public string beforeRender; // Can't be simple bool as otherwise we can't tell whether it was set or not.
            public string[] commonUsages;
            public string displayName;
            public string resourceName;
            public string type; // This is mostly for when we turn arbitrary InputControlLayouts into JSON; less for layouts *coming* from JSON.
            public string variant;
            public InputDeviceMatcher.MatcherJson device;
            public ControlItemJson[] controls;

            // ReSharper restore MemberCanBePrivate.Local
            #pragma warning restore 0649

            public InputControlLayout ToLayout()
            {
                // By default, the type of the layout is determined from the first layout
                // in its 'extend' property chain that has a type set. However, if the layout
                // extends nothing, we can't know what type to use for it so we default to
                // InputDevice.
                Type type = null;
                if (!string.IsNullOrEmpty(this.type))
                {
                    type = Type.GetType(this.type, false);
                    if (type == null)
                    {
                        Debug.Log(string.Format(
                            "Cannot find type '{0}' used by layout '{1}'; falling back to using InputDevice",
                            this.type, name));
                        type = typeof(InputDevice);
                    }
                    else if (!typeof(InputControl).IsAssignableFrom(type))
                    {
                        throw new Exception(string.Format("'{0}' used by layout '{1}' is not an InputControl",
                            this.type, name));
                    }
                }
                else if (string.IsNullOrEmpty(extend))
                    type = typeof(InputDevice);

                // Create layout.
                var layout = new InputControlLayout(name, type);
                layout.m_DisplayName = displayName;
                layout.m_ResourceName = resourceName;
                layout.m_Variants = new InternedString(variant);
                if (!string.IsNullOrEmpty(format))
                    layout.m_StateFormat = new FourCC(format);

                // Base layout.
                if (!string.IsNullOrEmpty(extend))
                    layout.m_BaseLayouts.Append(new InternedString(extend));
                if (extendMultiple != null)
                    foreach (var element in extendMultiple)
                        layout.m_BaseLayouts.Append(new InternedString(element));

                // Before render behavior.
                if (!string.IsNullOrEmpty(beforeRender))
                {
                    var beforeRenderLowerCase = beforeRender.ToLower();
                    if (beforeRenderLowerCase == "ignore")
                        layout.m_UpdateBeforeRender = false;
                    else if (beforeRenderLowerCase == "update")
                        layout.m_UpdateBeforeRender = true;
                    else
                        throw new Exception(string.Format("Invalid beforeRender setting '{0}'", beforeRender));
                }

                // Add common usages.
                if (commonUsages != null)
                    layout.m_CommonUsages = ArrayHelpers.Select(commonUsages, x => new InternedString(x));

                // Add controls.
                if (controls != null)
                {
                    var controlLayouts = new List<ControlItem>();
                    foreach (var control in controls)
                    {
                        if (string.IsNullOrEmpty(control.name))
                            throw new Exception(string.Format("Control with no name in layout '{0}", name));
                        var controlLayout = control.ToLayout();
                        ThrowIfControlItemIsDuplicate(ref controlLayout, controlLayouts, layout.name);
                        controlLayouts.Add(controlLayout);
                    }
                    layout.m_Controls = controlLayouts.ToArray();
                }

                return layout;
            }

            public static LayoutJson FromLayout(InputControlLayout layout)
            {
                return new LayoutJson
                {
                    name = layout.m_Name,
                    type = layout.type.AssemblyQualifiedName,
                    variant = layout.m_Variants,
                    displayName = layout.m_DisplayName,
                    resourceName = layout.m_ResourceName,
                    extend = layout.m_BaseLayouts.length == 1 ? layout.m_BaseLayouts[0].ToString() : null,
                    extendMultiple = layout.m_BaseLayouts.length > 1 ? layout.m_BaseLayouts.ToArray(x => x.ToString()) : null,
                    format = layout.stateFormat.ToString(),
                    controls = ControlItemJson.FromControlItems(layout.m_Controls),
                };
            }
        }

        // This is a class instead of a struct so that we can assign 'offset' a custom
        // default value. Otherwise we can't tell whether the user has actually set it
        // or not (0 is a valid offset). Sucks, though, as we now get lots of allocations
        // from the control array.
        [Serializable]
        private class ControlItemJson
        {
            // Disable warnings that these fields are never assigned to. They are set
            // by JsonUtility.
            #pragma warning disable 0649
            // ReSharper disable MemberCanBePrivate.Local

            public string name;
            public string layout;
            public string variants;
            public string usage; // Convenince to not have to create array for single usage.
            public string alias; // Same.
            public string useStateFrom;
            public uint offset;
            public uint bit;
            public uint sizeInBits;
            public string format;
            public int arraySize;
            public string[] usages;
            public string[] aliases;
            public string parameters;
            public string processors;
            public string displayName;
            public string resourceName;
            public bool noisy;

            // This should be an object type field and allow any JSON primitive value type as well
            // as arrays of those. Unfortunately, the Unity JSON serializer, given it uses Unity serialization
            // and thus doesn't support polymorphism, can do no such thing. Hopefully we do get support
            // for this later but for now, we use a string-based value fallback instead.
            public string defaultState;

            // ReSharper restore MemberCanBePrivate.Local
            #pragma warning restore 0649

            public ControlItemJson()
            {
                offset = InputStateBlock.kInvalidOffset;
                bit = InputStateBlock.kInvalidOffset;
            }

            public ControlItem ToLayout()
            {
                var layout = new ControlItem
                {
                    name = new InternedString(name),
                    layout = new InternedString(this.layout),
                    variants = new InternedString(variants),
                    displayName = displayName,
                    resourceName = resourceName,
                    offset = offset,
                    useStateFrom = useStateFrom,
                    bit = bit,
                    sizeInBits = sizeInBits,
                    isModifyingChildControlByPath = name.IndexOf('/') != -1,
                    isNoisy = noisy,
                    arraySize = arraySize,
                };

                if (!string.IsNullOrEmpty(format))
                    layout.format = new FourCC(format);

                if (!string.IsNullOrEmpty(usage) || usages != null)
                {
                    var usagesList = new List<string>();
                    if (!string.IsNullOrEmpty(usage))
                        usagesList.Add(usage);
                    if (usages != null)
                        usagesList.AddRange(usages);
                    layout.usages = new ReadOnlyArray<InternedString>(usagesList.Select(x => new InternedString(x)).ToArray());
                }

                if (!string.IsNullOrEmpty(alias) || aliases != null)
                {
                    var aliasesList = new List<string>();
                    if (!string.IsNullOrEmpty(alias))
                        aliasesList.Add(alias);
                    if (aliases != null)
                        aliasesList.AddRange(aliases);
                    layout.aliases = new ReadOnlyArray<InternedString>(aliasesList.Select(x => new InternedString(x)).ToArray());
                }

                if (!string.IsNullOrEmpty(parameters))
                    layout.parameters = new ReadOnlyArray<ParameterValue>(ParseParameters(parameters));

                if (!string.IsNullOrEmpty(processors))
                    layout.processors = new ReadOnlyArray<NameAndParameters>(ParseNameAndParameterList(processors));

                if (defaultState != null)
                    layout.defaultState = PrimitiveValueOrArray.FromObject(defaultState);

                return layout;
            }

            public static ControlItemJson[] FromControlItems(ControlItem[] items)
            {
                if (items == null)
                    return null;

                var count = items.Length;
                var result = new ControlItemJson[count];

                for (var i = 0; i < count; ++i)
                {
                    var item = items[i];
                    result[i] = new ControlItemJson
                    {
                        name = item.name,
                        layout = item.layout,
                        variants = item.variants,
                        displayName = item.displayName,
                        resourceName = item.resourceName,
                        bit = item.bit,
                        offset = item.offset,
                        sizeInBits = item.sizeInBits,
                        format = item.format.ToString(),
                        parameters = string.Join(",", item.parameters.Select(x => x.ToString()).ToArray()),
                        processors = string.Join(",", item.processors.Select(x => x.ToString()).ToArray()),
                        usages = item.usages.Select(x => x.ToString()).ToArray(),
                        aliases = item.aliases.Select(x => x.ToString()).ToArray(),
                        noisy = item.isNoisy,
                        arraySize = item.arraySize,
                    };
                }

                return result;
            }
        }


        internal struct Collection
        {
            public const float kBaseScoreForNonGeneratedLayouts = 1.0f;

            public Dictionary<InternedString, Type> layoutTypes;
            public Dictionary<InternedString, string> layoutStrings;
            public Dictionary<InternedString, BuilderInfo> layoutBuilders;
            public Dictionary<InternedString, InternedString> baseLayoutTable;
            public Dictionary<InternedString, InternedString[]> layoutOverrides;

            public struct LayoutMatcher
            {
                public InternedString layoutName;
                public InputDeviceMatcher deviceMatcher;

                // In the editor, when we perform a domain reload, we only want to preserve device matchers
                // coming from
                #if UNITY_EDITOR
                //public bool;
                #endif
            }

            ////TODO: find a smarter approach that doesn't require linearly scanning through all matchers
            public int layoutMatcherCount;
            public KeyValuePair<InputDeviceMatcher, InternedString>[] layoutMatchers;

            public void Allocate()
            {
                layoutTypes = new Dictionary<InternedString, Type>();
                layoutStrings = new Dictionary<InternedString, string>();
                layoutBuilders = new Dictionary<InternedString, BuilderInfo>();
                baseLayoutTable = new Dictionary<InternedString, InternedString>();
                layoutOverrides = new Dictionary<InternedString, InternedString[]>();
            }

            public InternedString TryFindLayoutForType(Type layoutType)
            {
                foreach (var entry in layoutTypes)
                    if (entry.Value == layoutType)
                        return entry.Key;
                return new InternedString();
            }

            public InternedString TryFindMatchingLayout(InputDeviceDescription deviceDescription)
            {
                var highestScore = 0f;
                var highestScoringLayout = new InternedString();

                for (var i = 0; i < layoutMatcherCount; ++i)
                {
                    var matcher = layoutMatchers[i].Key;
                    var score = matcher.MatchPercentage(deviceDescription);

                    // We want auto-generated layouts to take a backseat compared to manually created
                    // layouts. We do this by boosting the score of every layout that isn't coming from
                    // a layout builder.
                    if (score > 0 && !layoutBuilders.ContainsKey(layoutMatchers[i].Value))
                        score += kBaseScoreForNonGeneratedLayouts;

                    if (score > highestScore)
                    {
                        highestScore = score;
                        highestScoringLayout = layoutMatchers[i].Value;
                    }
                }

                return highestScoringLayout;
            }

            public bool HasLayout(InternedString name)
            {
                return layoutTypes.ContainsKey(name) || layoutStrings.ContainsKey(name) ||
                    layoutBuilders.ContainsKey(name);
            }

            private InputControlLayout TryLoadLayoutInternal(InternedString name)
            {
                // See if we have a string layout for it. These
                // always take precedence over ones from type so that we can
                // override what's in the code using data.
                string json;
                if (layoutStrings.TryGetValue(name, out json))
                    return FromJson(json);

                // No, but maybe we have a type layout for it.
                Type type;
                if (layoutTypes.TryGetValue(name, out type))
                    return FromType(name, type);

                // Finally, check builders. Always the last ones to get a shot at
                // providing layouts.
                BuilderInfo builder;
                if (layoutBuilders.TryGetValue(name, out builder))
                {
                    var layoutObject = builder.method.Invoke(builder.instance, null);
                    if (layoutObject == null)
                        throw new Exception(string.Format("Layout builder '{0}' returned null when invoked", name));
                    var layout = layoutObject as InputControlLayout;
                    if (layout == null)
                        throw new Exception(string.Format(
                            "Layout builder '{0}' returned '{1}' which is not an InputControlLayout", name,
                            layoutObject));
                    return layout;
                }

                return null;
            }

            public InputControlLayout TryLoadLayout(InternedString name, Dictionary<InternedString, InputControlLayout> table = null)
            {
                var layout = TryLoadLayoutInternal(name);
                if (layout != null)
                {
                    layout.m_Name = name;
                    if (table != null)
                        table[name] = layout;

                    // If the layout extends another layout, we need to merge the
                    // base layout into the final layout.
                    // NOTE: We go through the baseLayoutTable here instead of looking at
                    //       the baseLayouts property so as to make this work for all types
                    //       of layouts (FromType() does not set the property, for example).
                    var baseLayoutName = new InternedString();
                    if (baseLayoutTable.TryGetValue(name, out baseLayoutName))
                    {
                        Debug.Assert(!baseLayoutName.IsEmpty());

                        ////TODO: catch cycles
                        var baseLayout = TryLoadLayout(baseLayoutName, table);
                        if (baseLayout == null)
                            throw new LayoutNotFoundException(string.Format(
                                "Cannot find base layout '{0}' of layout '{1}'", baseLayoutName, name));
                        layout.MergeLayout(baseLayout);

                        if (layout.m_BaseLayouts.length == 0)
                            layout.m_BaseLayouts.Append(baseLayoutName);
                    }

                    // If there's overrides for the layout, apply them now.
                    InternedString[] overrides;
                    if (layoutOverrides.TryGetValue(name, out overrides))
                    {
                        for (var i = 0; i < overrides.Length; ++i)
                        {
                            var overrideName = overrides[i];
                            var overrideLayout = TryLoadLayout(overrideName, table);
                            overrideLayout.MergeLayout(layout);
                            layout = overrideLayout;
                            layout.m_AppliedOverrides.Append(overrideName);
                        }
                    }
                }

                return layout;
            }

            // Return name of layout at root of "extend" chain of given layout.
            public InternedString GetRootLayoutName(InternedString layoutName)
            {
                InternedString baseLayout;
                while (baseLayoutTable.TryGetValue(layoutName, out baseLayout))
                    layoutName = baseLayout;
                return layoutName;
            }

            // Get the type which will be instantiated for the given layout.
            // Returns null if no layout with the given name exists.
            public Type GetControlTypeForLayout(InternedString layoutName)
            {
                // Try layout strings.
                while (layoutStrings.ContainsKey(layoutName))
                {
                    InternedString baseLayout;
                    if (baseLayoutTable.TryGetValue(layoutName, out baseLayout))
                    {
                        // Work our way up the inheritance chain.
                        layoutName = baseLayout;
                    }
                    else
                    {
                        // Layout doesn't extend anything and ATM we don't support setting
                        // types explicitly from JSON layouts. So has to be InputDevice.
                        return typeof(InputDevice);
                    }
                }

                // Try layout types.
                Type result;
                layoutTypes.TryGetValue(layoutName, out result);
                return result;
            }

            public bool IsBasedOn(InternedString parentLayout, InternedString childLayout)
            {
                var layout = childLayout;
                while (baseLayoutTable.TryGetValue(layout, out layout))
                {
                    if (layout == parentLayout)
                        return true;
                }
                return false;
            }

            public void AddMatcher(InternedString layout, InputDeviceMatcher matcher)
            {
                // Ignore if already added.
                for (var i = 0; i < layoutMatcherCount; ++i)
                    if (layoutMatchers[i].Key == matcher)
                        return;

                // Append.
                ArrayHelpers.AppendWithCapacity(ref layoutMatchers, ref layoutMatcherCount,
                    new KeyValuePair<InputDeviceMatcher, InternedString>(matcher, layout));
            }
        }

        // This collection is owned and managed by InputManager.
        internal static Collection s_Layouts;

        internal struct BuilderInfo
        {
            public MethodInfo method;
            public object instance;
        }

        internal class LayoutNotFoundException : Exception
        {
            public string layout { get; private set; }
            public LayoutNotFoundException(string name, string message = null)
                : base(message ?? string.Format("Cannot find control layout '{0}'", name))
            {
                layout = name;
            }
        }

        // Constructs InputControlLayout instances and caches them.
        internal struct Cache
        {
            public Collection layouts;
            public Dictionary<InternedString, InputControlLayout> table;

            public InputControlLayout FindOrLoadLayout(string name)
            {
                var internedName = new InternedString(name);

                // See if we have it cached.
                InputControlLayout layout;
                if (table != null && table.TryGetValue(internedName, out layout))
                    return layout;

                if (table == null)
                    table = new Dictionary<InternedString, InputControlLayout>();

                layout = layouts.TryLoadLayout(internedName, table);
                if (layout != null)
                    return layout;

                // Nothing.
                throw new LayoutNotFoundException(name);
            }
        }
    }
}
