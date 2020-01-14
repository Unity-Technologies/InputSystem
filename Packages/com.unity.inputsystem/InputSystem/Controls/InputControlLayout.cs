using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////TODO: we really need proper verification to be in place to ensure that the resulting layout isn't coming out with a bad memory layout

////TODO: add code-generation that takes a layout and spits out C# code that translates it to a common value format
////      (this can be used, for example, to translate all the various gamepad formats into one single common gamepad format)

////TODO: allow layouts to set default device names

////TODO: allow creating generic controls as parents just to group child controls

////TODO: allow things like "-something" and "+something" for usages, processors, etc

////TODO: allow setting whether the device should automatically become current and whether it wants noise filtering

////TODO: ensure that if a layout sets a device description, it is indeed a device layout

////TODO: make offset on InputControlAttribute relative to field instead of relative to entire state struct

////REVIEW: common usages are on all layouts but only make sense for devices

////REVIEW: useStateFrom seems like a half-measure; it solves the problem of setting up state blocks but they often also
////        require a specific set of processors

namespace UnityEngine.InputSystem.Layouts
{
    /// <summary>
    /// Delegate used by <see cref="InputSystem.onFindLayoutForDevice"/>.
    /// </summary>
    /// <param name="description">The device description supplied by the runtime or through <see
    /// cref="InputSystem.AddDevice(InputDeviceDescription)"/>. This is passed by reference instead of
    /// by value to allow the callback to fill out fields such as <see cref="InputDeviceDescription.capabilities"/>
    /// on the fly based on information queried from external APIs or from the runtime.</param>
    /// <param name="matchedLayout">Name of the layout that has been selected for the device or <c>null</c> if
    /// no matching layout could be found. Matching is determined from the <see cref="InputDeviceMatcher"/>s for
    /// layouts registered in the system.</param>
    /// <param name="executeDeviceCommand">A delegate which can be invoked to execute <see cref="InputDeviceCommand"/>s
    /// on the device.</param>
    /// <returns>  Return <c>null</c> or an empty string to indicate that </returns>
    /// <remarks>
    /// </remarks>
    /// <seealso cref="InputSystem.onFindLayoutForDevice"/>
    /// <seealso cref="InputSystem.RegisterLayoutBuilder"/>
    /// <seealso cref="InputControlLayout"/>
    public delegate string InputDeviceFindControlLayoutDelegate(ref InputDeviceDescription description,
        string matchedLayout, InputDeviceExecuteCommandDelegate executeDeviceCommand);

    /// <summary>
    /// A control layout specifies the composition of an <see cref="InputControl"/> or
    /// <see cref="InputDevice"/>.
    /// </summary>
    /// <remarks>
    /// Control layouts can be created in three possible ways:
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
    ///
    /// See the <a href="../manual/Layouts.html">manual</a> for more details on control layouts.
    /// </remarks>
    public class InputControlLayout
    {
        private static InternedString s_DefaultVariant = new InternedString("Default");
        public static InternedString DefaultVariant => s_DefaultVariant;

        public const string VariantSeparator = ";";

        /// <summary>
        /// Specification for the composition of a direct or indirect child control.
        /// </summary>
        public struct ControlItem
        {
            /// <summary>
            /// Name of the control. Cannot be empty or <c>null</c>.
            /// </summary>
            /// <value>Name of the control.</value>
            /// <remarks>
            /// This may also be a path of the form <c>"parentName/childName..."</c>.
            /// This can be used to reach inside another layout and modify properties of
            /// a control inside of it. An example for this is adding a "leftStick" control
            /// using the Stick layout and then adding two control layouts that refer to
            /// "leftStick/x" and "leftStick/y" respectively to modify the state format used
            /// by the stick.
            ///
            /// This field is required.
            /// </remarks>
            /// <seealso cref="isModifyingExistingControl"/>
            /// <seealso cref="InputControlAttribute.name"/>
            public InternedString name { get; internal set; }

            /// <summary>
            /// Name of the layout to use for the control.
            /// </summary>
            /// <value>Name of layout to use.</value>
            /// <remarks>
            /// Must be the name of a control layout, not device layout.
            ///
            /// An example would be "Stick".
            /// </remarks>
            /// <seealso cref="InputSystem.RegisterLayout(Type,string,Nullable{InputDeviceMatcher}"/>
            public InternedString layout { get; internal set; }

            public InternedString variants { get; internal set; }
            public string useStateFrom { get; internal set; }

            /// <summary>
            /// Optional display name of the control.
            /// </summary>
            /// <seealso cref="InputControl.displayName"/>
            public string displayName { get; internal set; }

            /// <summary>
            /// Optional abbreviated display name of the control.
            /// </summary>
            /// <seealso cref="InputControl.shortDisplayName"/>
            public string shortDisplayName { get; internal set; }

            public ReadOnlyArray<InternedString> usages { get; internal set; }
            public ReadOnlyArray<InternedString> aliases { get; internal set; }
            public ReadOnlyArray<NamedValue> parameters { get; internal set; }
            public ReadOnlyArray<NameAndParameters> processors { get; internal set; }
            public uint offset { get; internal set; }
            public uint bit { get; internal set; }
            public uint sizeInBits { get; internal set; }
            public FourCC format { get; internal set; }
            private Flags flags { get; set; }
            public int arraySize { get; internal set; }

            /// <summary>
            /// Optional default value for the state memory associated with the control.
            /// </summary>
            public PrimitiveValue defaultState { get; internal set; }

            public PrimitiveValue minValue { get; internal set; }
            public PrimitiveValue maxValue { get; internal set; }

            // If true, the layout will not add a control but rather a modify a control
            // inside the hierarchy added by 'layout'. This allows, for example, to modify
            // just the X axis control of the left stick directly from within a gamepad
            // layout instead of having to have a custom stick layout for the left stick
            // than in turn would have to make use of a custom axis layout for the X axis.
            // Instead, you can just have a control layout with the name "leftStick/x".
            public bool isModifyingExistingControl
            {
                get => (flags & Flags.isModifyingExistingControl) == Flags.isModifyingExistingControl;
                internal set
                {
                    if (value)
                        flags |= Flags.isModifyingExistingControl;
                    else
                        flags &= ~Flags.isModifyingExistingControl;
                }
            }

            /// <summary>
            /// Get or set whether to mark the control as noisy.
            /// </summary>
            /// <value>Whether to mark the control as noisy.</value>
            /// <remarks>
            /// Noisy controls may generate varying input even without "proper" user interaction. For example,
            /// a sensor may generate slightly different input values over time even if in fact the very thing
            /// (such as the device orientation) that is being measured is not changing.
            /// </remarks>
            /// <seealso cref="InputControl.noisy"/>
            public bool isNoisy
            {
                get => (flags & Flags.IsNoisy) == Flags.IsNoisy;
                internal set
                {
                    if (value)
                        flags |= Flags.IsNoisy;
                    else
                        flags &= ~Flags.IsNoisy;
                }
            }

            /// <summary>
            /// Get or set whether to mark the control as "synthetic".
            /// </summary>
            /// <value>Whether to mark the control as synthetic.</value>
            /// <remarks>
            /// Synthetic controls are artificial controls that provide input but do not correspond to actual controls
            /// on the hardware. An example is <see cref="Keyboard.anyKey"/> which is an artificial button that triggers
            /// if any key on the keyboard is pressed.
            /// </remarks>
            /// <seealso cref="InputControl.synthetic"/>
            public bool isSynthetic
            {
                get => (flags & Flags.IsSynthetic) == Flags.IsSynthetic;
                internal set
                {
                    if (value)
                        flags |= Flags.IsSynthetic;
                    else
                        flags &= ~Flags.IsSynthetic;
                }
            }

            /// <summary>
            /// Whether the control is introduced by the layout.
            /// </summary>
            /// <value>If true, the control is first introduced by this layout.</value>
            /// <remarks>
            /// The value of this property is automatically determined by the input system.
            /// </remarks>
            public bool isFirstDefinedInThisLayout
            {
                get => (flags & Flags.IsFirstDefinedInThisLayout) != 0;
                internal set
                {
                    if (value)
                        flags |= Flags.IsFirstDefinedInThisLayout;
                    else
                        flags &= ~Flags.IsFirstDefinedInThisLayout;
                }
            }

            public bool isArray => (arraySize != 0);

            /// <summary>
            /// For any property not set on this control layout, take the setting from <paramref name="other"/>.
            /// </summary>
            /// <param name="other">Control layout providing settings.</param>
            /// <remarks>
            /// <see cref="name"/> will not be touched.
            /// </remarks>
            /// <seealso cref="InputControlLayout.MergeLayout"/>
            public ControlItem Merge(ControlItem other)
            {
                var result = new ControlItem();

                result.name = name;
                Debug.Assert(!name.IsEmpty(), "Name must not be empty");
                result.isModifyingExistingControl = isModifyingExistingControl;

                result.displayName = string.IsNullOrEmpty(displayName) ? other.displayName : displayName;
                result.shortDisplayName = string.IsNullOrEmpty(shortDisplayName) ? other.shortDisplayName : shortDisplayName;
                result.layout = layout.IsEmpty() ? other.layout : layout;
                result.variants = variants.IsEmpty() ? other.variants : variants;
                result.useStateFrom = useStateFrom ?? other.useStateFrom;
                result.arraySize = !isArray ? other.arraySize : arraySize;
                ////FIXME: allow overrides to unset this
                result.isNoisy = isNoisy || other.isNoisy;
                result.isSynthetic = isSynthetic || other.isSynthetic;
                result.isFirstDefinedInThisLayout = false;

                if (offset != InputStateBlock.InvalidOffset)
                    result.offset = offset;
                else
                    result.offset = other.offset;

                if (bit != InputStateBlock.InvalidOffset)
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

                ////FIXME: this should properly merge the parameters, not just pick one or the other
                ////       easiest thing may be to just concatenate the two strings

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

                if (!defaultState.isEmpty)
                    result.defaultState = defaultState;
                else
                    result.defaultState = other.defaultState;

                if (!minValue.isEmpty)
                    result.minValue = minValue;
                else
                    result.minValue = other.minValue;

                if (!maxValue.isEmpty)
                    result.maxValue = maxValue;
                else
                    result.maxValue = other.maxValue;

                return result;
            }

            [Flags]
            private enum Flags
            {
                isModifyingExistingControl = 1 << 0,
                IsNoisy = 1 << 1,
                IsSynthetic = 1 << 2,
                IsFirstDefinedInThisLayout = 1 << 3,
            }
        }

        // Unique name of the layout.
        // NOTE: Case-insensitive.
        public InternedString name => m_Name;

        public string displayName => m_DisplayName ?? m_Name;

        public Type type => m_Type;

        public InternedString variants => m_Variants;

        public FourCC stateFormat => m_StateFormat;

        public int stateSizeInBytes => m_StateSizeInBytes;

        public IEnumerable<InternedString> baseLayouts => m_BaseLayouts;

        public IEnumerable<InternedString> appliedOverrides => m_AppliedOverrides;

        public ReadOnlyArray<InternedString> commonUsages => new ReadOnlyArray<InternedString>(m_CommonUsages);

        /// <summary>
        /// List of child controls defined for the layout.
        /// </summary>
        /// <value>Child controls defined for the layout.</value>
        /// <remarks>
        /// Note that this list TODO
        /// </remarks>
        public ReadOnlyArray<ControlItem> controls => new ReadOnlyArray<ControlItem>(m_Controls);

        public bool updateBeforeRender => m_UpdateBeforeRender ?? false;

        public bool isDeviceLayout => typeof(InputDevice).IsAssignableFrom(m_Type);

        public bool isControlLayout => !isDeviceLayout;

        /// <summary>
        /// Whether the layout is applies overrides to other layouts instead of
        /// defining a layout by itself.
        /// </summary>
        /// <value>True if the layout acts as an override.</value>
        /// <seealso cref="InputSystem.RegisterLayoutOverride"/>
        public bool isOverride
        {
            get => (m_Flags & Flags.IsOverride) != 0;
            internal set
            {
                if (value)
                    m_Flags |= Flags.IsOverride;
                else
                    m_Flags &= ~Flags.IsOverride;
            }
        }

        public bool isGenericTypeOfDevice
        {
            get => (m_Flags & Flags.IsGenericTypeOfDevice) != 0;
            internal set
            {
                if (value)
                    m_Flags |= Flags.IsGenericTypeOfDevice;
                else
                    m_Flags &= ~Flags.IsGenericTypeOfDevice;
            }
        }

        public bool hideInUI
        {
            get => (m_Flags & Flags.HideInUI) != 0;
            internal set
            {
                if (value)
                    m_Flags |= Flags.HideInUI;
                else
                    m_Flags &= ~Flags.HideInUI;
            }
        }

        public ControlItem this[string path]
        {
            get
            {
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentNullException(nameof(path));

                // Does not use FindControl so that we don't force-intern the given path string.
                if (m_Controls != null)
                {
                    for (var i = 0; i < m_Controls.Length; ++i)
                    {
                        if (m_Controls[i].name == path)
                            return m_Controls[i];
                    }
                }

                throw new KeyNotFoundException($"Cannot find control '{path}' in layout '{name}'");
            }
        }

        public ControlItem? FindControl(InternedString path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (m_Controls == null)
                return null;

            for (var i = 0; i < m_Controls.Length; ++i)
            {
                if (m_Controls[i].name == path)
                    return m_Controls[i];
            }

            return null;
        }

        public ControlItem? FindControlIncludingArrayElements(string path, out int arrayIndex)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            arrayIndex = -1;
            if (m_Controls == null)
                return null;

            var arrayIndexAccumulated = 0;
            var lastDigitIndex = path.Length;
            while (lastDigitIndex > 0 && char.IsDigit(path[lastDigitIndex - 1]))
            {
                --lastDigitIndex;
                arrayIndexAccumulated *= 10;
                arrayIndexAccumulated += path[lastDigitIndex] - '0';
            }

            var arrayNameLength = 0;
            if (lastDigitIndex < path.Length && lastDigitIndex > 0) // Protect against name being all digits.
                arrayNameLength = lastDigitIndex;

            for (var i = 0; i < m_Controls.Length; ++i)
            {
                ref var control = ref m_Controls[i];
                if (string.Compare(control.name, path, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return control;

                ////FIXME: what this can't handle is "outerArray4/innerArray5"; not sure we care, though
                // NOTE: This will *not* match something like "touch4/tap". Which is what we want.
                //       In case there is a ControlItem
                if (control.isArray && arrayNameLength > 0 && arrayNameLength == control.name.length &&
                    string.Compare(control.name.ToString(), 0, path, 0, arrayNameLength,
                        StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    arrayIndex = arrayIndexAccumulated;
                    return control;
                }
            }

            return null;
        }

        /// <summary>
        /// Return the type of values produced by controls created from the layout.
        /// </summary>
        /// <returns>The value type of the control or null if it cannot be determined.</returns>
        /// <remarks>
        /// This method only returns the statically inferred value type. This type corresponds
        /// to the type argument to <see cref="InputControl{TValue}"/> in the inheritance hierarchy
        /// of <see cref="type"/>. As the type used by the layout may not inherit from
        /// <see cref="InputControl{TValue}"/>, this may mean that the value type cannot be inferred
        /// and the method will return null.
        /// </remarks>
        /// <seealso cref="InputControl.valueType"/>
        public Type GetValueType()
        {
            return TypeHelpers.GetGenericTypeArgumentFromHierarchy(type, typeof(InputControl<>), 0);
        }

        /// <summary>
        /// Build a layout programmatically. Primarily for use by layout builders
        /// registered with the system.
        /// </summary>
        /// <seealso cref="InputSystem.RegisterLayoutBuilder"/>
        public class Builder
        {
            /// <summary>
            /// Name to assign to the layout.
            /// </summary>
            /// <value>Name to assign to the layout.</value>
            /// <seealso cref="InputControlLayout.name"/>
            public string name { get; set; }

            /// <summary>
            /// Display name to assign to the layout.
            /// </summary>
            /// <value>Display name to assign to the layout</value>
            /// <seealso cref="InputControlLayout.displayName"/>
            public string displayName { get; set; }

            /// <summary>
            /// <see cref="InputControl"/> type to instantiate for the layout.
            /// </summary>
            /// <value>Control type to instantiate for the layout.</value>
            /// <seealso cref="InputControlLayout.type"/>
            public Type type { get; set; }

            /// <summary>
            /// Memory format FourCC code to apply to state memory used by the
            /// layout.
            /// </summary>
            /// <value>FourCC memory format tag.</value>
            /// <seealso cref="InputControlLayout.stateFormat"/>
            /// <seealso cref="InputStateBlock.format"/>
            public FourCC stateFormat { get; set; }

            /// <summary>
            /// Total size of memory used by the layout.
            /// </summary>
            /// <value>Size of memory used by the layout.</value>
            /// <seealso cref="InputControlLayout.stateSizeInBytes"/>
            public int stateSizeInBytes { get; set; }

            /// <summary>
            /// Which layout to base this layout on.
            /// </summary>
            /// <value>Name of base layout.</value>
            /// <seealso cref="InputControlLayout.baseLayouts"/>
            public string extendsLayout { get; set; }

            /// <summary>
            /// For device layouts, whether the device wants an extra update
            /// before rendering.
            /// </summary>
            /// <value>True if before-render updates should be enabled for the device.</value>
            /// <seealso cref="InputDevice.updateBeforeRender"/>
            /// <seealso cref="InputControlLayout.updateBeforeRender"/>
            public bool? updateBeforeRender { get; set; }

            /// <summary>
            /// List of control items set up by the layout.
            /// </summary>
            /// <value>Controls set up by the layout.</value>
            /// <seealso cref="AddControl"/>
            public ReadOnlyArray<ControlItem> controls => new ReadOnlyArray<ControlItem>(m_Controls, 0, m_ControlCount);

            private int m_ControlCount;
            private ControlItem[] m_Controls;

            /// <summary>
            /// Syntax for configuring an individual <see cref="ControlItem"/>.
            /// </summary>
            public struct ControlBuilder
            {
                internal Builder builder;
                internal int index;

                public ControlBuilder WithDisplayName(string displayName)
                {
                    builder.m_Controls[index].displayName = displayName;
                    return this;
                }

                public ControlBuilder WithLayout(string layout)
                {
                    if (string.IsNullOrEmpty(layout))
                        throw new ArgumentException("Layout name cannot be null or empty", nameof(layout));

                    builder.m_Controls[index].layout = new InternedString(layout);
                    return this;
                }

                public ControlBuilder WithFormat(FourCC format)
                {
                    builder.m_Controls[index].format = format;
                    return this;
                }

                public ControlBuilder WithFormat(string format)
                {
                    return WithFormat(new FourCC(format));
                }

                public ControlBuilder WithByteOffset(uint offset)
                {
                    builder.m_Controls[index].offset = offset;
                    return this;
                }

                public ControlBuilder WithBitOffset(uint bit)
                {
                    builder.m_Controls[index].bit = bit;
                    return this;
                }

                public ControlBuilder IsSynthetic(bool value)
                {
                    builder.m_Controls[index].isSynthetic = value;
                    return this;
                }

                public ControlBuilder IsNoisy(bool value)
                {
                    builder.m_Controls[index].isNoisy = value;
                    return this;
                }

                public ControlBuilder WithSizeInBits(uint sizeInBits)
                {
                    builder.m_Controls[index].sizeInBits = sizeInBits;
                    return this;
                }

                public ControlBuilder WithUsages(params InternedString[] usages)
                {
                    if (usages == null || usages.Length == 0)
                        return this;

                    for (var i = 0; i < usages.Length; ++i)
                        if (usages[i].IsEmpty())
                            throw new ArgumentException(
                                $"Empty usage entry at index {i} for control '{builder.m_Controls[index].name}' in layout '{builder.name}'",
                                nameof(usages));

                    builder.m_Controls[index].usages = new ReadOnlyArray<InternedString>(usages);
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
                    if (string.IsNullOrEmpty(parameters))
                        return this;
                    var parsed = NamedValue.ParseMultiple(parameters);
                    builder.m_Controls[index].parameters = new ReadOnlyArray<NamedValue>(parsed);
                    return this;
                }

                public ControlBuilder WithProcessors(string processors)
                {
                    if (string.IsNullOrEmpty(processors))
                        return this;
                    var parsed = NameAndParameters.ParseMultiple(processors).ToArray();
                    builder.m_Controls[index].processors = new ReadOnlyArray<NameAndParameters>(parsed);
                    return this;
                }

                public ControlBuilder WithDefaultState(PrimitiveValue value)
                {
                    builder.m_Controls[index].defaultState = value;
                    return this;
                }

                public ControlBuilder UsingStateFrom(string path)
                {
                    if (string.IsNullOrEmpty(path))
                        return this;
                    builder.m_Controls[index].useStateFrom = path;
                    return this;
                }

                public ControlBuilder AsArrayOfControlsWithSize(int arraySize)
                {
                    builder.m_Controls[index].arraySize = arraySize;
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
                        isModifyingExistingControl = name.IndexOf('/') != -1,
                        offset = InputStateBlock.InvalidOffset,
                        bit = InputStateBlock.InvalidOffset
                    });

                return new ControlBuilder
                {
                    builder = this,
                    index = index
                };
            }

            public Builder WithName(string name)
            {
                this.name = name;
                return this;
            }

            public Builder WithDisplayName(string displayName)
            {
                this.displayName = displayName;
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

            public Builder WithSizeInBytes(int sizeInBytes)
            {
                stateSizeInBytes = sizeInBytes;
                return this;
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
                    m_DisplayName = displayName,
                    m_StateFormat = stateFormat,
                    m_StateSizeInBytes = stateSizeInBytes,
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
                    stateFormat = ((IInputStateTypeInfo)Activator.CreateInstance(layoutAttribute.stateType)).format;
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
            var layout = new InputControlLayout(name, type)
            {
                m_Controls = controlLayouts.ToArray(),
                m_StateFormat = stateFormat,
                m_Variants = variants,
                m_UpdateBeforeRender = layoutAttribute?.updateBeforeRenderInternal,
                isGenericTypeOfDevice = layoutAttribute?.isGenericTypeOfDevice ?? false,
                hideInUI = layoutAttribute?.hideInUI ?? false,
                m_Description = layoutAttribute?.description,
                m_DisplayName = layoutAttribute?.displayName,
            };

            if (layoutAttribute?.commonUsages != null)
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
        private Type m_Type; // For extension chains, we can only discover types after loading multiple layouts, so we make this accessible to InputDeviceBuilder.
        private InternedString m_Variants;
        private FourCC m_StateFormat;
        internal int m_StateSizeInBytes; // Note that this is the combined state size for input and output.
        internal bool? m_UpdateBeforeRender;
        internal InlinedArray<InternedString> m_BaseLayouts;
        private InlinedArray<InternedString> m_AppliedOverrides;
        private InternedString[] m_CommonUsages;
        internal ControlItem[] m_Controls;
        internal string m_DisplayName;
        private string m_Description;
        private Flags m_Flags;

        [Flags]
        private enum Flags
        {
            IsGenericTypeOfDevice = 1 << 0,
            HideInUI = 1 << 1,
            IsOverride = 1 << 2,
        }

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

        // Add ControlLayouts for every public property in the given type that has
        // InputControlAttribute applied to it or has an InputControl-derived value type.
        private static void AddControlItemsFromFields(Type type, List<ControlItem> controlLayouts, string layoutName)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            AddControlItemsFromMembers(fields, controlLayouts, layoutName);
        }

        // Add ControlLayouts for every public property in the given type that has
        // InputControlAttribute applied to it or has an InputControl-derived value type.
        private static void AddControlItemsFromProperties(Type type, List<ControlItem> controlLayouts, string layoutName)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            AddControlItemsFromMembers(properties, controlLayouts, layoutName);
        }

        // Add ControlLayouts for every member in the list that has InputControlAttribute applied to it
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
                        var controlCountAfter = controlItems.Count;
                        for (var i = controlCountBefore; i < controlCountAfter; ++i)
                        {
                            var controlLayout = controlItems[i];
                            if (controlItems[i].offset != InputStateBlock.InvalidOffset)
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

                    // On properties, we require explicit [InputControl] attributes to
                    // pick them up. Doing it otherwise has proven to lead too easily to
                    // situations where you inadvertently add new controls to a layout
                    // just because you added an InputControl-type property to a class.
                    if (member is PropertyInfo)
                        continue;
                }

                AddControlItemsFromMember(member, attributes, controlItems, layoutName);
            }
        }

        private static void AddControlItemsFromMember(MemberInfo member,
            InputControlAttribute[] attributes, List<ControlItem> controlItems, string layoutName)
        {
            // InputControlAttribute can be applied multiple times to the same member,
            // generating a separate control for each occurrence. However, it can also
            // generating a separate control for each occurrence. However, it can also
            // not be applied at all in which case we still add a control layout (the
            // logic that called us already made sure the member is eligible for this kind
            // of operation).

            if (attributes.Length == 0)
            {
                var controlLayout = CreateControlItemFromMember(member, null);
                ThrowIfControlItemIsDuplicate(ref controlLayout, controlItems, layoutName);
                controlItems.Add(controlLayout);
            }
            else
            {
                foreach (var attribute in attributes)
                {
                    var controlLayout = CreateControlItemFromMember(member, attribute);
                    ThrowIfControlItemIsDuplicate(ref controlLayout, controlItems, layoutName);
                    controlItems.Add(controlLayout);
                }
            }
        }

        private static ControlItem CreateControlItemFromMember(MemberInfo member, InputControlAttribute attribute)
        {
            ////REVIEW: make sure that the value type of the field and the value type of the control match?

            // Determine name.
            var name = attribute?.name;
            if (string.IsNullOrEmpty(name))
                name = member.Name;

            var isModifyingChildControlByPath = name.IndexOf('/') != -1;

            // Determine display name.
            var displayName = attribute?.displayName;
            var shortDisplayName = attribute?.shortDisplayName;

            // Determine layout.
            var layout = attribute?.layout;
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
            var offset = InputStateBlock.InvalidOffset;
            if (attribute != null && attribute.offset != InputStateBlock.InvalidOffset)
                offset = attribute.offset;
            else if (member is FieldInfo && !isModifyingChildControlByPath)
                offset = (uint)Marshal.OffsetOf(member.DeclaringType, member.Name).ToInt32();

            // Determine bit offset.
            var bit = InputStateBlock.InvalidOffset;
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
            else if (!isModifyingChildControlByPath && bit == InputStateBlock.InvalidOffset)
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
            NamedValue[] parameters = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.parameters))
                parameters = NamedValue.ParseMultiple(attribute.parameters);

            // Determine processors.
            NameAndParameters[] processors = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.processors))
                processors = NameAndParameters.ParseMultiple(attribute.processors).ToArray();

            // Determine whether to use state from another control.
            string useStateFrom = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.useStateFrom))
                useStateFrom = attribute.useStateFrom;

            // Determine if it's a noisy control.
            var isNoisy = false;
            if (attribute != null)
                isNoisy = attribute.noisy;

            // Determine if it's a synthetic control.
            var isSynthetic = false;
            if (attribute != null)
                isSynthetic = attribute.synthetic;

            // Determine array size.
            var arraySize = 0;
            if (attribute != null)
                arraySize = attribute.arraySize;

            // Determine default state.
            var defaultState = new PrimitiveValue();
            if (attribute != null)
                defaultState = PrimitiveValue.FromObject(attribute.defaultState);

            // Determine min and max value.
            var minValue = new PrimitiveValue();
            var maxValue = new PrimitiveValue();
            if (attribute != null)
            {
                minValue = PrimitiveValue.FromObject(attribute.minValue);
                maxValue = PrimitiveValue.FromObject(attribute.maxValue);
            }

            return new ControlItem
            {
                name = new InternedString(name),
                displayName = displayName,
                shortDisplayName = shortDisplayName,
                layout = new InternedString(layout),
                variants = new InternedString(variants),
                useStateFrom = useStateFrom,
                format = format,
                offset = offset,
                bit = bit,
                sizeInBits = sizeInBits,
                parameters = new ReadOnlyArray<NamedValue>(parameters),
                processors = new ReadOnlyArray<NameAndParameters>(processors),
                usages = new ReadOnlyArray<InternedString>(usages),
                aliases = new ReadOnlyArray<InternedString>(aliases),
                isModifyingExistingControl = isModifyingChildControlByPath,
                isFirstDefinedInThisLayout = true,
                isNoisy = isNoisy,
                isSynthetic = isSynthetic,
                arraySize = arraySize,
                defaultState = defaultState,
                minValue = minValue,
                maxValue = maxValue,
            };
        }

        ////REVIEW: this tends to cause surprises; is it worth its cost?
        private static string InferLayoutFromValueType(Type type)
        {
            var layout = s_Layouts.TryFindLayoutForType(type);
            if (layout.IsEmpty())
            {
                var typeName = new InternedString(type.Name);
                if (s_Layouts.HasLayout(typeName))
                    layout = typeName;
                else if (type.Name.EndsWith("Control"))
                {
                    typeName = new InternedString(type.Name.Substring(0, type.Name.Length - "Control".Length));
                    if (s_Layouts.HasLayout(typeName))
                        layout = typeName;
                }
            }
            return layout;
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
            if (other == null)
                throw new ArgumentNullException(nameof(other));

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

            // Combine common usages.
            m_CommonUsages = ArrayHelpers.Merge(other.m_CommonUsages, m_CommonUsages);

            // Retain list of overrides.
            m_AppliedOverrides.Merge(other.m_AppliedOverrides);

            // Inherit display name.
            if (string.IsNullOrEmpty(m_DisplayName))
                m_DisplayName = other.m_DisplayName;

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
                    if (baseControlTable.TryGetValue(pair.Key, out var baseControlItem))
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
                                    var key = $"{pair.Key}@{baseControlVariants[i]}";
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
                                var key = $"{pair.Key}@{variant}";
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
                    var indexStart = controls.Count;
                    controls.AddRange(baseControlTable.Values);

                    // Mark the controls as being inherited.
                    for (var i = indexStart; i < controls.Count; ++i)
                    {
                        var control = controls[i];
                        control.isFirstDefinedInThisLayout = false;
                        controls[i] = control;
                    }
                }
                else
                {
                    // Filter out controls coming from the base layout which are targeting variants
                    // that we're not interested in.
                    var indexStart = controls.Count;
                    controls.AddRange(
                        baseControlTable.Values.Where(x => VariantsMatch(m_Variants, x.variants)));

                    // Mark the controls as being inherited.
                    for (var i = indexStart; i < controls.Count; ++i)
                    {
                        var control = controls[i];
                        control.isFirstDefinedInThisLayout = false;
                        controls[i] = control;
                    }
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
                    if (itemVariants.ToString().IndexOf(VariantSeparator[0]) != -1)
                    {
                        var itemVariantArray = itemVariants.ToLower().Split(VariantSeparator[0]);
                        foreach (var name in itemVariantArray)
                        {
                            variants?.Add(name);
                            key = $"{key}@{name}";
                            table[key] = controlItems[i];
                        }

                        continue;
                    }

                    key = $"{key}@{itemVariants.ToLower()}";
                    variants?.Add(itemVariants.ToLower());
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
                StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(DefaultVariant, actual, VariantSeparator[0]))
                return true;

            // If we don't expect a specific variant, we accept any variant.
            if (expected == null)
                return true;

            // If we there's no variant set on what we actual got, then it matches even if we
            // expect specific variants.
            if (actual == null)
                return true;

            // Match if the two variant sets intersect on at least one element.
            return StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(expected, actual, VariantSeparator[0]);
        }

        private static void ThrowIfControlItemIsDuplicate(ref ControlItem controlItem,
            IEnumerable<ControlItem> controlLayouts, string layoutName)
        {
            var name = controlItem.name;
            foreach (var existing in controlLayouts)
                if (string.Compare(name, existing.name, StringComparison.OrdinalIgnoreCase) == 0 &&
                    existing.variants == controlItem.variants)
                    throw new InvalidOperationException($"Duplicate control '{name}' in layout '{layoutName}'");
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
            public string description;
            public string type; // This is mostly for when we turn arbitrary InputControlLayouts into JSON; less for layouts *coming* from JSON.
            public string variant;
            public bool isGenericTypeOfDevice;
            public bool hideInUI;
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
                        Debug.Log(
                            $"Cannot find type '{this.type}' used by layout '{name}'; falling back to using InputDevice");
                        type = typeof(InputDevice);
                    }
                    else if (!typeof(InputControl).IsAssignableFrom(type))
                    {
                        throw new InvalidOperationException($"'{this.type}' used by layout '{name}' is not an InputControl");
                    }
                }
                else if (string.IsNullOrEmpty(extend))
                    type = typeof(InputDevice);

                // Create layout.
                var layout = new InputControlLayout(name, type)
                {
                    m_DisplayName = displayName,
                    m_Description = description,
                    isGenericTypeOfDevice = isGenericTypeOfDevice,
                    hideInUI = hideInUI,
                    m_Variants = new InternedString(variant)
                };
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
                        throw new InvalidOperationException($"Invalid beforeRender setting '{beforeRender}'");
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
                            throw new InvalidOperationException($"Control with no name in layout '{name}");
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
                    description = layout.m_Description,
                    isGenericTypeOfDevice = layout.isGenericTypeOfDevice,
                    hideInUI = layout.hideInUI,
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
            public string usage; // Convenience to not have to create array for single usage.
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
            public string shortDisplayName;
            public bool noisy;
            public bool synthetic;

            // This should be an object type field and allow any JSON primitive value type as well
            // as arrays of those. Unfortunately, the Unity JSON serializer, given it uses Unity serialization
            // and thus doesn't support polymorphism, can do no such thing. Hopefully we do get support
            // for this later but for now, we use a string-based value fallback instead.
            public string defaultState;
            public string minValue;
            public string maxValue;

            // ReSharper restore MemberCanBePrivate.Local
            #pragma warning restore 0649

            public ControlItemJson()
            {
                offset = InputStateBlock.InvalidOffset;
                bit = InputStateBlock.InvalidOffset;
            }

            public ControlItem ToLayout()
            {
                var layout = new ControlItem
                {
                    name = new InternedString(name),
                    layout = new InternedString(this.layout),
                    variants = new InternedString(variants),
                    displayName = displayName,
                    shortDisplayName = shortDisplayName,
                    offset = offset,
                    useStateFrom = useStateFrom,
                    bit = bit,
                    sizeInBits = sizeInBits,
                    isModifyingExistingControl = name.IndexOf('/') != -1,
                    isNoisy = noisy,
                    isSynthetic = synthetic,
                    isFirstDefinedInThisLayout = true,
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
                    layout.parameters = new ReadOnlyArray<NamedValue>(NamedValue.ParseMultiple(parameters));

                if (!string.IsNullOrEmpty(processors))
                    layout.processors = new ReadOnlyArray<NameAndParameters>(NameAndParameters.ParseMultiple(processors).ToArray());

                if (defaultState != null)
                    layout.defaultState = PrimitiveValue.FromObject(defaultState);
                if (minValue != null)
                    layout.minValue = PrimitiveValue.FromObject(minValue);
                if (maxValue != null)
                    layout.maxValue = PrimitiveValue.FromObject(maxValue);

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
                        shortDisplayName = item.shortDisplayName,
                        bit = item.bit,
                        offset = item.offset,
                        sizeInBits = item.sizeInBits,
                        format = item.format.ToString(),
                        parameters = string.Join(",", item.parameters.Select(x => x.ToString()).ToArray()),
                        processors = string.Join(",", item.processors.Select(x => x.ToString()).ToArray()),
                        usages = item.usages.Select(x => x.ToString()).ToArray(),
                        aliases = item.aliases.Select(x => x.ToString()).ToArray(),
                        noisy = item.isNoisy,
                        synthetic = item.isSynthetic,
                        arraySize = item.arraySize,
                        defaultState = item.defaultState.ToString(),
                        minValue = item.minValue.ToString(),
                        maxValue = item.maxValue.ToString(),
                    };
                }

                return result;
            }
        }


        internal struct Collection
        {
            public const float kBaseScoreForNonGeneratedLayouts = 1.0f;

            public struct LayoutMatcher
            {
                public InternedString layoutName;
                public InputDeviceMatcher deviceMatcher;
            }

            public Dictionary<InternedString, Type> layoutTypes;
            public Dictionary<InternedString, string> layoutStrings;
            public Dictionary<InternedString, Func<InputControlLayout>> layoutBuilders;
            public Dictionary<InternedString, InternedString> baseLayoutTable;
            public Dictionary<InternedString, InternedString[]> layoutOverrides;
            public HashSet<InternedString> layoutOverrideNames;
            ////TODO: find a smarter approach that doesn't require linearly scanning through all matchers
            ////  (also ideally shouldn't be a List but with Collection being a struct and given how it's
            ////  stored by InputManager.m_Layouts and in s_Layouts; we can't make it a plain array)
            public List<LayoutMatcher> layoutMatchers;

            public void Allocate()
            {
                layoutTypes = new Dictionary<InternedString, Type>();
                layoutStrings = new Dictionary<InternedString, string>();
                layoutBuilders = new Dictionary<InternedString, Func<InputControlLayout>>();
                baseLayoutTable = new Dictionary<InternedString, InternedString>();
                layoutOverrides = new Dictionary<InternedString, InternedString[]>();
                layoutOverrideNames = new HashSet<InternedString>();
                layoutMatchers = new List<LayoutMatcher>();
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

                var layoutMatcherCount = layoutMatchers.Count;
                for (var i = 0; i < layoutMatcherCount; ++i)
                {
                    var matcher = layoutMatchers[i].deviceMatcher;
                    var score = matcher.MatchPercentage(deviceDescription);

                    // We want auto-generated layouts to take a backseat compared to manually created
                    // layouts. We do this by boosting the score of every layout that isn't coming from
                    // a layout builder.
                    if (score > 0 && !layoutBuilders.ContainsKey(layoutMatchers[i].layoutName))
                        score += kBaseScoreForNonGeneratedLayouts;

                    if (score > highestScore)
                    {
                        highestScore = score;
                        highestScoringLayout = layoutMatchers[i].layoutName;
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
                if (layoutStrings.TryGetValue(name, out var json))
                    return FromJson(json);

                // No, but maybe we have a type layout for it.
                if (layoutTypes.TryGetValue(name, out var type))
                    return FromType(name, type);

                // Finally, check builders. Always the last ones to get a shot at
                // providing layouts.
                if (layoutBuilders.TryGetValue(name, out var builder))
                {
                    var layout = builder();
                    if (layout == null)
                        throw new InvalidOperationException($"Layout builder '{name}' returned null when invoked");
                    return layout;
                }

                return null;
            }

            public InputControlLayout TryLoadLayout(InternedString name, Dictionary<InternedString, InputControlLayout> table = null)
            {
                // See if we have it cached.
                if (table != null && table.TryGetValue(name, out var layout))
                    return layout;

                layout = TryLoadLayoutInternal(name);
                if (layout != null)
                {
                    layout.m_Name = name;

                    if (layoutOverrideNames.Contains(name))
                        layout.isOverride = true;

                    // If the layout extends another layout, we need to merge the
                    // base layout into the final layout.
                    // NOTE: We go through the baseLayoutTable here instead of looking at
                    //       the baseLayouts property so as to make this work for all types
                    //       of layouts (FromType() does not set the property, for example).
                    var baseLayoutName = new InternedString();
                    if (!layout.isOverride && baseLayoutTable.TryGetValue(name, out baseLayoutName))
                    {
                        Debug.Assert(!baseLayoutName.IsEmpty());

                        ////TODO: catch cycles
                        var baseLayout = TryLoadLayout(baseLayoutName, table);
                        if (baseLayout == null)
                            throw new LayoutNotFoundException(
                                $"Cannot find base layout '{baseLayoutName}' of layout '{name}'");
                        layout.MergeLayout(baseLayout);

                        if (layout.m_BaseLayouts.length == 0)
                            layout.m_BaseLayouts.Append(baseLayoutName);
                    }

                    // If there's overrides for the layout, apply them now.
                    if (layoutOverrides.TryGetValue(name, out var overrides))
                    {
                        for (var i = 0; i < overrides.Length; ++i)
                        {
                            var overrideName = overrides[i];
                            // NOTE: We do *NOT* pass `table` into TryLoadLayout here so that
                            //       the override we load will not get cached. The reason is that
                            //       we use MergeLayout which is destructive and thus should not
                            //       end up in the table.
                            var overrideLayout = TryLoadLayout(overrideName);
                            overrideLayout.MergeLayout(layout);

                            // We're switching the layout we initially to the layout with
                            // the overrides applied. Make sure we get rid of information here
                            // from the override that we don't want to come through once the
                            // override is applied.
                            overrideLayout.m_BaseLayouts.Clear();
                            overrideLayout.isOverride = false;
                            overrideLayout.isGenericTypeOfDevice = layout.isGenericTypeOfDevice;
                            overrideLayout.m_Name = layout.name;

                            layout = overrideLayout;
                            layout.m_AppliedOverrides.Append(overrideName);
                        }
                    }

                    if (table != null)
                        table[name] = layout;
                }

                return layout;
            }

            public InternedString GetBaseLayoutName(InternedString layoutName)
            {
                if (baseLayoutTable.TryGetValue(layoutName, out var baseLayoutName))
                    return baseLayoutName;
                return default;
            }

            // Return name of layout at root of "extend" chain of given layout.
            public InternedString GetRootLayoutName(InternedString layoutName)
            {
                while (baseLayoutTable.TryGetValue(layoutName, out var baseLayout))
                    layoutName = baseLayout;
                return layoutName;
            }

            public bool ComputeDistanceInInheritanceHierarchy(InternedString firstLayout, InternedString secondLayout, out int distance)
            {
                distance = 0;

                // First try, assume secondLayout is based on firstLayout.
                var secondDistanceToFirst = 0;
                var current = secondLayout;
                while (!current.IsEmpty() && current != firstLayout)
                {
                    current = GetBaseLayoutName(current);
                    ++secondDistanceToFirst;
                }
                if (current == firstLayout)
                {
                    distance = secondDistanceToFirst;
                    return true;
                }

                // Second try, assume firstLayout is based on secondLayout.
                var firstDistanceToSecond = 0;
                current = firstLayout;
                while (!current.IsEmpty() && current != secondLayout)
                {
                    current = GetBaseLayoutName(current);
                    ++firstDistanceToSecond;
                }
                if (current == secondLayout)
                {
                    distance = firstDistanceToSecond;
                    return true;
                }

                return false;
            }

            public InternedString FindLayoutThatIntroducesControl(InputControl control, Cache cache)
            {
                // Find the topmost child control on the device. A device layout can only
                // add children that sit directly underneath it (e.g. "leftStick"). Children of children
                // are indirectly added by other layouts (e.g. "leftStick/x" which is added by "Stick").
                // To determine which device contributes the control as a whole, we have to be looking
                // at the topmost child of the device.
                var topmostChild = control;
                while (topmostChild.parent != control.device)
                    topmostChild = topmostChild.parent;

                // Find the layout in the device's base layout chain that first mentions the given control.
                // If we don't find it, we know it's first defined directly in the layout of the given device,
                // i.e. it's not an inherited control.
                var deviceLayoutName = control.device.m_Layout;
                var baseLayoutName = deviceLayoutName;
                while (baseLayoutTable.TryGetValue(baseLayoutName, out baseLayoutName))
                {
                    var layout = cache.FindOrLoadLayout(baseLayoutName);

                    var controlItem = layout.FindControl(topmostChild.m_Name);
                    if (controlItem != null)
                        deviceLayoutName = baseLayoutName;
                }

                return deviceLayoutName;
            }

            // Get the type which will be instantiated for the given layout.
            // Returns null if no layout with the given name exists.
            public Type GetControlTypeForLayout(InternedString layoutName)
            {
                // Try layout strings.
                while (layoutStrings.ContainsKey(layoutName))
                {
                    if (baseLayoutTable.TryGetValue(layoutName, out var baseLayout))
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
                layoutTypes.TryGetValue(layoutName, out var result);
                return result;
            }

            // Return true if the given control layout has a value type whose values
            // can be assigned to variables of type valueType.
            public bool ValueTypeIsAssignableFrom(InternedString layoutName, Type valueType)
            {
                var controlType = GetControlTypeForLayout(layoutName);
                if (controlType == null)
                    return false;

                var valueTypOfControl =
                    TypeHelpers.GetGenericTypeArgumentFromHierarchy(controlType, typeof(InputControl<>), 0);
                if (valueTypOfControl == null)
                    return false;

                return valueType.IsAssignableFrom(valueTypOfControl);
            }

            public bool IsGeneratedLayout(InternedString layout)
            {
                return layoutBuilders.ContainsKey(layout);
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
                var layoutMatcherCount = layoutMatchers.Count;
                for (var i = 0; i < layoutMatcherCount; ++i)
                    if (layoutMatchers[i].deviceMatcher == matcher)
                        return;

                // Append.
                layoutMatchers.Add(new LayoutMatcher {layoutName = layout, deviceMatcher = matcher});
            }
        }

        // This collection is owned and managed by InputManager.
        internal static Collection s_Layouts;

        public class LayoutNotFoundException : Exception
        {
            public string layout { get; }

            public LayoutNotFoundException()
            {
            }

            public LayoutNotFoundException(string name, string message)
                : base(message)
            {
                layout = name;
            }

            public LayoutNotFoundException(string name)
                : base($"Cannot find control layout '{name}'")
            {
                layout = name;
            }

            public LayoutNotFoundException(string message, Exception innerException) :
                base(message, innerException)
            {
            }

            protected LayoutNotFoundException(SerializationInfo info,
                                              StreamingContext context) : base(info, context)
            {
            }
        }

        // Constructs InputControlLayout instances and caches them.
        internal struct Cache
        {
            public Dictionary<InternedString, InputControlLayout> table;

            public void Clear()
            {
                table = null;
            }

            public InputControlLayout FindOrLoadLayout(string name, bool throwIfNotFound = true)
            {
                var internedName = new InternedString(name);

                if (table == null)
                    table = new Dictionary<InternedString, InputControlLayout>();

                var layout = s_Layouts.TryLoadLayout(internedName, table);
                if (layout != null)
                    return layout;

                // Nothing.
                if (throwIfNotFound)
                    throw new LayoutNotFoundException(name);
                return null;
            }
        }

        internal static Cache s_CacheInstance;
        internal static int s_CacheInstanceRef;

        // Constructing InputControlLayouts is very costly as it tends to involve lots of reflection and
        // piecing data together. Thus, wherever possible, we want to keep layouts around for as long as
        // we need them yet at the same time not keep them needlessly around while we don't.
        //
        // This property makes a cache of layouts available globally yet implements a resource acquisition
        // based pattern to make sure we keep the cache alive only within specific execution scopes.
        internal static ref Cache cache
        {
            get
            {
                Debug.Assert(s_CacheInstanceRef > 0, "Must hold an instance reference");
                return ref s_CacheInstance;
            }
        }

        internal static CacheRefInstance CacheRef()
        {
            ++s_CacheInstanceRef;
            return new CacheRefInstance {valid = true};
        }

        internal struct CacheRefInstance : IDisposable
        {
            public bool valid; // Make sure we can distinguish default-initialized instances.
            public void Dispose()
            {
                if (!valid)
                    return;

                --s_CacheInstanceRef;
                if (s_CacheInstanceRef <= 0)
                {
                    s_CacheInstance = default;
                    s_CacheInstanceRef = 0;
                }

                valid = false;
            }
        }
    }
}
