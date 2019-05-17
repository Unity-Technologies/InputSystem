using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;

////REVIEW: should EvaluateMagnitude() be called EvaluateActuation() or something similar?

////REVIEW: as soon as we gain the ability to have blittable type constraints, InputControl<TValue> should be constrained such

////REVIEW: Reading and writing is asymmetric. Writing does not involve processors, reading does.

////REVIEW: While the arrays used by controls are already nicely centralized on InputDevice, InputControls still
////        hold a bunch of reference data that requires separate scanning. Can we move *all* reference data to arrays
////        on InputDevice and make InputControls reference-free? Most challenging thing probably is getting rid of
////        the InputDevice reference itself.

////REVIEW: how do we do stuff like smoothing over time?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A typed and named value in a hierarchy of controls.
    /// </summary>
    /// <remarks>
    /// Controls can have children which in turn may have children. At the root of the child
    /// hierarchy is always an InputDevice (which themselves are InputControls).
    ///
    /// Controls can be looked up by their path (see <see cref="InputDeviceBuilder.GetControl"/> and
    /// <see cref="InputControlPath.TryFindControl"/>).
    ///
    /// Each control must have a unique name within its parent (see <see cref="name"/>). Multiple
    /// names can be assigned to controls using aliases (see <see cref="aliases"/>). Name lookup
    /// is case-insensitive.
    ///
    /// In addition to names, a control may have usages associated with it (see <see cref="usages"/>).
    /// A usage indicates how a control is meant to be used. For example, a button can be assigned
    /// the "PrimaryAction" usage to indicate it is the primary action button the device. Within a
    /// device, usages have to be unique. See CommonUsages for a list of standardized usages.
    ///
    /// Controls do not actually store values. Instead, every control receives an InputStateBlock
    /// which, after the control's device has been added to the system, is used to read out values
    /// from the device's backing store. This backing store is referred to as "state" in the API
    /// as opposed to "values" which represent the data resulting from reading state. The format that
    /// each control stores state in is specific to the control. It can vary not only between controls
    /// of different types but also between controls of the same type. An <see cref="AxisControl"/>,
    /// for example, can be stored as a float or as a byte or in a number of other formats. <see cref="stateBlock"/>
    /// identifies both where the control stores its state as well as the format it stores it in.
    /// </remarks>
    /// <seealso cref="InputDevice"/>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class InputControl
    {
        ////REVIEW: we could allow the parenthetical characters if we require escaping them in paths
        /// <summary>
        /// Characters that may not appear in control names.
        /// </summary>
        /// TODO: these are currently not used. Check against these if we think this is useful.
        // internal const string ReservedCharacters = "/;{}[]<>";

        /// <summary>
        /// The name of the control, i.e. the final name part in its path.
        /// </summary>
        /// <remarks>
        /// Names of controls must be unique within the context of their parent.
        ///
        /// Lookup of names is case-insensitive.
        /// </remarks>
        /// <seealso cref="path"/>
        /// <seealso cref="aliases"/>
        public string name => m_Name;

        ////TODO: protect against empty strings
        /// <summary>
        /// The text to display as the name of the control.
        /// </summary>
        /// <remarks>
        /// Note that the display name of a control may change over time. For example, when changing
        /// from a QWERTY keyboard layout to an AZERTY keyboard layout, the "q" key (which will keep
        /// that <see cref="name"/>) will change its display name from "q" to "a".
        ///
        /// By default, a control's display name will come from its layout. If it is not assigned
        /// a display name there, the display name will default to <see cref="name"/>. However, specific
        /// controls may override this behavior. <see cref="KeyControl"/>, for example, will set the
        /// display name to the actual key name corresponding to the current keyboard layout.
        ///
        /// For nested controls, the display name will include the display names of all parent controls,
        /// i.e. the display name will fully identify the control on the device. For example, the display
        /// name for the left D-Pad button on a gamepad is "D-Pad Left" and not just "Left".
        /// </remarks>
        /// <seealso cref="shortDisplayName"/>
        public string displayName
        {
            get
            {
                RefreshConfigurationIfNeeded();
                if (m_DisplayName != null)
                    return m_DisplayName;
                if (m_DisplayNameFromLayout != null)
                    return m_DisplayNameFromLayout;
                return m_Name;
            }
            // This is not public as a domain reload will wipe the change. This should really
            // come from the control itself *if* the control wants to have a custom display name
            // not driven by its layout.
            protected set => m_DisplayName = value;
        }

        /// <summary>
        /// An alternate, abbreviated <see cref="displayName"/> (for example "LMB" instead of "Left Button").
        /// </summary>
        /// <remarks>
        /// If the control has no abbreviated version, this will be null. Note that this behavior is different
        /// from <see cref="displayName"/> which will fall back to <see cref="name"/> if not display name has
        /// been assigned to the control.
        ///
        /// For nested controls, the short display name will include the short display names of all parent controls,
        /// i.e. the display name will fully identify the control on the device. For example, the display
        /// name for the left D-Pad button on a gamepad is "D-Pad \u2190" and not just "\u2190". Note that if a parent
        /// control has no short name, its long name will be used instead.
        /// </remarks>
        /// <seealso cref="displayName"/>
        public string shortDisplayName
        {
            get
            {
                RefreshConfigurationIfNeeded();
                if (m_ShortDisplayName != null)
                    return m_ShortDisplayName;
                if (m_ShortDisplayNameFromLayout != null)
                    return m_ShortDisplayNameFromLayout;
                return null;
            }
            protected set => m_ShortDisplayName = value;
        }

        /// <summary>
        /// Full path all the way from the root.
        /// </summary>
        /// <remarks>
        /// Allocates on first hit. Paths are not created until someone asks for them.
        /// </remarks>
        /// <example>
        /// Example: "/gamepad/leftStick/x"
        /// </example>
        public string path
        {
            get
            {
                if (m_Path == null)
                    m_Path = InputControlPath.Combine(m_Parent, m_Name);
                return m_Path;
            }
        }

        /// <summary>
        /// Layout the control is based on.
        /// </summary>
        /// <remarks>
        /// This is the layout name rather than a reference to an <see cref="InputControlLayout"/> as
        /// we only create layout instances during device creation and treat them
        /// as temporaries in general so as to not waste heap space during normal operation.
        /// </remarks>
        public string layout => m_Layout;

        /// <summary>
        /// Semicolon-separated list of variants of the control layout or "default".
        /// </summary>
        /// <example>
        /// "Lefty" when using the "Lefty" gamepad layout.
        /// </example>
        public string variants => m_Variants;

        /// <summary>
        /// The device that this control is a part of.
        /// </summary>
        /// <remarks>
        /// This is the root of the control hiearchy. For the device at the root, this
        /// will point to itself.
        /// </remarks>
        public InputDevice device => m_Device;

        /// <summary>
        /// The immediate parent of the control or null if the control has no parent
        /// (which, once fully constructed) will only be the case for InputDevices).
        /// </summary>
        public InputControl parent => m_Parent;

        /// <summary>
        /// List of immediate children.
        /// </summary>
        /// <remarks>
        /// Does not allocate.
        /// </remarks>
        public ReadOnlyArray<InputControl> children => m_ChildrenReadOnly;

        // List of uses for this control. Gives meaning to the control such that you can, for example,
        // find a button on a device to use as the "back" button regardless of what it is named. The "back"
        // button is also an example of why there are multiple possible usages of a button as a use may
        // be context-dependent; if "back" does not make sense in a context, another use may make sense for
        // the very same button.
        public ReadOnlyArray<InternedString> usages => m_UsagesReadOnly;

        // List of alternate names for the control.
        public ReadOnlyArray<InternedString> aliases => m_AliasesReadOnly;

        // Information about where the control stores its state.
        public InputStateBlock stateBlock => m_StateBlock;

        /// <summary>
        /// Whether the control is considered noisy.
        /// </summary>
        /// <remarks>
        /// A control is considered "noisy" if it produces different values without necessarily requiring user
        /// interaction. Sensors are a good example.
        ///
        /// The value of this property is determined by the layout (<see cref="InputControlLayout"/>) that the
        /// control has been built from.
        ///
        /// Note that for devices (<see cref="InputDevice"/>) this property is true if any control on the device
        /// is marked as noisy.
        /// </remarks>
        /// <seealso cref="InputControlLayout.ControlItem.isNoisy"/>
        /// <seealso cref="InputControlAttribute.noisy"/>
        public bool noisy
        {
            get => (m_ControlFlags & ControlFlags.IsNoisy) != 0;
            internal set
            {
                if (value)
                {
                    m_ControlFlags |= ControlFlags.IsNoisy;
                    // Making a control noisy makes all its children noisy.
                    foreach (var child in children)
                        child.noisy = true;
                }
                else
                    m_ControlFlags &= ~ControlFlags.IsNoisy;
            }
        }

        /// <summary>
        /// Whether the control is considered synthetic.
        /// </summary>
        /// <remarks>
        /// A control is considered "synthetic" if it does not correspond to an actual, physical control on the
        /// device. An example for this is <see cref="Keyboard.anyKey"/> or the up/down/left/right buttons added
        /// by <see cref="StickControl"/>.
        ///
        /// The value of this property is determined by the layout (<see cref="InputControlLayout"/>) that the
        /// control has been built from.
        /// </remarks>
        /// <seealso cref="InputControlLayout.ControlItem.isSynthetic"/>
        /// <seealso cref="InputControlAttribute.synthetic"/>
        public bool synthetic
        {
            get => (m_ControlFlags & ControlFlags.IsSynthetic) != 0;
            internal set
            {
                if (value)
                    m_ControlFlags |= ControlFlags.IsSynthetic;
                else
                    m_ControlFlags &= ~ControlFlags.IsSynthetic;
            }
        }

        /// <summary>
        /// Fetch a control from the control's hierarchy by name.
        /// </summary>
        /// <remarks>
        /// Note that path matching is case-insensitive.
        /// </remarks>
        /// <example>
        /// <code>
        /// gamepad["leftStick"] // Returns Gamepad.leftStick
        /// gamepad["leftStick/x"] // Returns Gamepad.leftStick.x
        /// gamepad["{PrimaryAction}"] // Returns the control with PrimaryAction usage, i.e. Gamepad.aButton
        /// </code>
        /// </example>
        /// <exception cref="KeyNotFoundException"><paramref name="path"/> cannot be found.</exception>
        /// <seealso cref="InputControlPath"/>
        /// <seealso cref="path"/>
        /// <seealso cref="TryGetChildControl"/>
        public InputControl this[string path]
        {
            get
            {
                var control = InputControlPath.TryFindChild(this, path);
                if (control == null)
                    throw new KeyNotFoundException(
                        $"Cannot find control '{path}' as child of '{this}'");
                return control;
            }
        }

        /// <summary>
        /// Returns the underlying value type of this control.
        /// </summary>
        /// <remarks>
        /// This is the type of values that are returned when reading the current value of a control
        /// or when reading a value of a control from an event.
        /// </remarks>
        /// <seealso cref="valueSizeInBytes"/>
        /// <seealso cref="ReadValueIntoBuffer"/>
        public abstract Type valueType { get; }

        /// <summary>
        /// Size in bytes of values that the control returns.
        /// </summary>
        /// <seealso cref="valueType"/>
        public abstract int valueSizeInBytes { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{layout}:{path}";
        }

        private string DebuggerDisplay()
        {
            // If the device hasn't been added, don't try to read the control's value.
            if (!device.added)
                return ToString();

            // ReadValueAsObject might throw. Revert to just ToString() in that case.
            try
            {
                return $"{layout}:{path}={this.ReadValueAsObject()}";
            }
            catch (Exception)
            {
                return ToString();
            }
        }

        /// <summary>
        /// Compute an absolute, normalized magnitude value that indicates the extent to which the control
        /// is actuated.
        /// </summary>
        /// <returns>Amount of actuation of the control or -1 if it cannot be determined.</returns>
        /// <remarks>
        /// Magnitudes do not make sense for all types of controls. For example, for a control that represents
        /// an enumeration of values (such as <see cref="PointerPhaseControl"/>), there is no meaningful
        /// linear ordering of values (one could derive a linear ordering through the actual enum values but
        /// their assignment may be entirely arbitrary; it is unclear whether a state of <see cref="PointerPhase.Canceled"/>
        /// has a higher or lower "magnitude" as a state of <see cref="PointerPhase.Began"/>).
        ///
        /// Controls that have no meaningful magnitude will return -1 when calling this method. Any negative
        /// return value should be considered an invalid value.
        /// </remarks>
        /// <seealso cref="EvaluateMagnitude(void*)"/>
        public unsafe float EvaluateMagnitude()
        {
            return EvaluateMagnitude(currentStatePtr);
        }

        /// <summary>
        /// Compute an absolute, normalized magnitude value that indicates the extent to which the control
        /// is actuated in the given state.
        /// </summary>
        /// <param name="statePtr">State containing the control's <see cref="stateBlock"/>.</param>
        /// <returns>Amount of actuation of the control or -1 if it cannot be determined.</returns>
        /// <seealso cref="EvaluateMagnitude()"/>
        /// <seealso cref="stateBlock"/>
        public virtual unsafe float EvaluateMagnitude(void* statePtr)
        {
            return -1;
        }

        public abstract unsafe object ReadValueFromBufferAsObject(void* buffer, int bufferSize);

        /// <summary>
        /// Read the control's final, processed value from the given state and return the value as an object.
        /// </summary>
        /// <param name="statePtr"></param>
        /// <returns>The control's value as stored in <paramref name="statePtr"/>.</returns>
        /// <remarks>
        /// This method allocates GC memory and should not be used during normal gameplay operation.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="statePtr"/> is null.</exception>
        /// <seealso cref="ReadValueFromStateIntoBuffer"/>
        public abstract unsafe object ReadValueFromStateAsObject(void* statePtr);

        /// <summary>
        /// Read the control's final, processed value from the given state and store it in the given buffer.
        /// </summary>
        /// <param name="statePtr">State to read the value for the control from.</param>
        /// <param name="bufferPtr">Buffer to store the value in.</param>
        /// <param name="bufferSize">Size of <paramref name="bufferPtr"/> in bytes. Must be at least <see cref="valueSizeInBytes"/>.
        /// If it is smaller, <see cref="ArgumentException"/> will be thrown.</param>
        /// <exception cref="ArgumentNullException"><paramref name="statePtr"/> is null, or <paramref name="bufferPtr"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="bufferSize"/> is smaller than <see cref="valueSizeInBytes"/>.</exception>
        /// <seealso cref="ReadValueFromStateAsObject"/>
        /// <seealso cref="WriteValueFromBufferIntoState"/>
        public abstract unsafe void ReadValueFromStateIntoBuffer(void* statePtr, void* bufferPtr, int bufferSize);

        /// <summary>
        /// Read a value from the given memory and store it as state.
        /// </summary>
        /// <param name="bufferPtr">Memory containing value.</param>
        /// <param name="bufferSize">Size of <paramref name="bufferPtr"/> in bytes. Must be at least <see cref="valueSizeInBytes"/>.</param>
        /// <param name="statePtr">State containing the control's <see cref="stateBlock"/>. Will receive the state
        /// as converted from the given value.</param>
        /// <remarks>
        /// Writing values will NOT apply processors to the given value. This can mean that when reading a value
        /// from a control after it has been written to its state, the resulting value differs from what was
        /// written.
        /// </remarks>
        /// <exception cref="NotSupportedException">The control does not support writing. This is the case, for
        /// example, that compute values (such as the magnitude of a vector).</exception>
        /// <seealso cref="ReadValueFromStateIntoBuffer"/>
        /// <seealso cref="WriteValueFromObjectIntoState"/>
        public virtual unsafe void WriteValueFromBufferIntoState(void* bufferPtr, int bufferSize, void* statePtr)
        {
            throw new NotSupportedException(
                $"Control '{this}' does not support writing");
        }

        /// <summary>
        /// Read a value object and store it as state in the given memory.
        /// </summary>
        /// <param name="value">Value for the control.</param>
        /// <param name="statePtr">State containing the control's <see cref="stateBlock"/>. Will receive
        /// the state state as converted from the given value.</param>
        /// <remarks>
        /// Writing values will NOT apply processors to the given value. This can mean that when reading a value
        /// from a control after it has been written to its state, the resulting value differs from what was
        /// written.
        /// </remarks>
        /// <exception cref="NotSupportedException">The control does not support writing. This is the case, for
        /// example, that compute values (such as the magnitude of a vector).</exception>
        /// <seealso cref="WriteValueFromBufferIntoState"/>
        public virtual unsafe void WriteValueFromObjectIntoState(object value, void* statePtr)
        {
            throw new NotSupportedException(
                $"Control '{this}' does not support writing");
        }

        /// <summary>
        /// Compare the value of the control as read from <paramref name="firstStatePtr"/> to that read from
        /// <paramref name="secondStatePtr"/> and return true if they are equal.
        /// </summary>
        /// <param name="firstStatePtr">Memory containing the control's <see cref="stateBlock"/>.</param>
        /// <param name="secondStatePtr">Memory containing the control's <see cref="stateBlock"/></param>
        /// <returns>True if the value of the control is equal in both <paramref name="firstStatePtr"/> and
        /// <paramref name="secondStatePtr"/>.</returns>
        /// <remarks>
        /// Unlike <see cref="CompareState"/>, this method will have to do more than just compare the memory
        /// for the control in the two state buffers. It will have to read out state for the control and run
        /// the full processing machinery for the control to turn the state into a final, processed value.
        /// CompareValue is thus more costly than <see cref="CompareState"/>.
        ///
        /// This method will apply epsilons (<see cref="Mathf.Epsilon"/>) when comparing floats.
        /// </remarks>
        /// <seealso cref="CompareState"/>
        public abstract unsafe bool CompareValue(void* firstStatePtr, void* secondStatePtr);

        /// <summary>
        /// Compare the control's stored state in <paramref name="firstStatePtr"/> to <paramref name="secondStatePtr"/>.
        /// </summary>
        /// <param name="firstStatePtr">Memory containing the control's <see cref="stateBlock"/>.</param>
        /// <param name="secondStatePtr">Memory containing the control's <see cref="stateBlock"/></param>
        /// <param name="maskPtr">Optional mask. If supplied, it will be used to mask the comparison between
        /// <paramref name="firstStatePtr"/> and <paramref name="secondStatePtr"/> such that any bit not set in the
        /// mask will be ignored even if different between the two states. This can be used, for example, to ignore
        /// noise in the state (<see cref="noiseMaskPtr"/>).</param>
        /// <returns>True if the state is equivalent in both memory buffers.</returns>
        /// <remarks>
        /// Unlike <see cref="CompareValue"/>, this method only compares raw memory state. If used on a stick, for example,
        /// it may mean that this method returns false for two stick values that would compare equal using <see cref="CompareValue"/>
        /// (e.g. if both stick values fall below the deadzone).
        /// </remarks>
        /// <seealso cref="CompareValue"/>
        public unsafe bool CompareState(void* firstStatePtr, void* secondStatePtr, void* maskPtr = null)
        {
            ////REVIEW: for compound controls, do we want to go check leaves so as to not pick up on non-control noise in the state?
            ////        e.g. from HID input reports; or should we just leave that to maskPtr?

            var firstPtr = (byte*)firstStatePtr + (int)m_StateBlock.byteOffset;
            var secondPtr = (byte*)secondStatePtr + (int)m_StateBlock.byteOffset;
            var mask = maskPtr != null ? (byte*)maskPtr + (int)m_StateBlock.byteOffset : null;

            if (m_StateBlock.sizeInBits == 1)
            {
                // If we have a mask and the bit is set in the mask, the control is to be ignored
                // and thus we consider it at default value.
                if (mask != null && MemoryHelpers.ReadSingleBit(mask, m_StateBlock.bitOffset))
                    return true;

                return MemoryHelpers.ReadSingleBit(secondPtr, m_StateBlock.bitOffset) ==
                    MemoryHelpers.ReadSingleBit(firstPtr, m_StateBlock.bitOffset);
            }

            return MemoryHelpers.MemCmpBitRegion(firstPtr, secondPtr,
                m_StateBlock.bitOffset, m_StateBlock.sizeInBits, mask);
        }

        public InputControl TryGetChildControl(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            return InputControlPath.TryFindChild(this, path);
        }

        // Constructor for devices which are assigned names once plugged
        // into the system.
        protected InputControl()
        {
            // Set defaults for state block setup. Subclasses may override.
            m_StateBlock.byteOffset = InputStateBlock.InvalidOffset; // Request automatic layout by default.
        }

        ////REVIEW: replace InputDeviceBuilder here with an interface?
        // Set up of the control has been finalized. This can be used, for example, to look up
        // child controls for fast access.
        // NOTE: This function will be called repeatedly in case the setup is changed repeatedly.
        protected virtual void FinishSetup(InputDeviceBuilder builder)
        {
        }

        protected void RefreshConfigurationIfNeeded()
        {
            if (!isConfigUpToDate)
            {
                RefreshConfiguration();
                isConfigUpToDate = true;
            }
        }

        protected virtual void RefreshConfiguration()
        {
        }

        protected internal InputStateBlock m_StateBlock;

        ////REVIEW: shouldn't these sit on the device?
        protected internal unsafe void* currentStatePtr => InputStateBuffers.GetFrontBufferForDevice(ResolveDeviceIndex());

        protected internal unsafe void* previousFrameStatePtr => InputStateBuffers.GetBackBufferForDevice(ResolveDeviceIndex());

        protected internal unsafe void* defaultStatePtr => InputStateBuffers.s_DefaultStateBuffer;

        protected internal unsafe void* noiseMaskPtr => InputStateBuffers.s_NoiseMaskBuffer;

        /// <summary>
        /// The offset of this control's state relative to its device root.
        /// </summary>
        /// <remarks>
        /// Once a device has been added to the system, its state block will get allocated
        /// in the global state buffers and the offset of the device's state block will
        /// get baked into all of the controls on the device. This property always returns
        /// the "unbaked" offset.
        /// </remarks>
        protected internal uint stateOffsetRelativeToDeviceRoot
        {
            get
            {
                var deviceStateOffset = device.m_StateBlock.byteOffset;
                Debug.Assert(deviceStateOffset <= m_StateBlock.byteOffset);
                return m_StateBlock.byteOffset - deviceStateOffset;
            }
        }

        // This data is initialized by InputDeviceBuilder.
        internal InternedString m_Name;
        internal string m_Path;
        internal string m_DisplayName; // Display name set by the control itself (may be null).
        internal string m_DisplayNameFromLayout; // Display name coming from layout (may be null).
        internal string m_ShortDisplayName; // Short display name set by the control itself (may be null).
        internal string m_ShortDisplayNameFromLayout; // Short display name coming from layout (may be null).
        internal InternedString m_Layout;
        internal InternedString m_Variants;
        internal InputDevice m_Device;
        internal InputControl m_Parent;
        ////REVIEW: This is stupid. We're storing the array references on here when in fact they should
        ////        be fetched on demand from InputDevice. What we do here is needlessly add three extra
        ////        references to every single InputControl
        internal ReadOnlyArray<InternedString> m_UsagesReadOnly;
        internal ReadOnlyArray<InternedString> m_AliasesReadOnly;
        internal ReadOnlyArray<InputControl> m_ChildrenReadOnly;
        internal ControlFlags m_ControlFlags;

        ////REVIEW: store these in arrays in InputDevice instead?
        internal PrimitiveValueOrArray m_DefaultValue;
        internal PrimitiveValue m_MinValue;
        internal PrimitiveValue m_MaxValue;

        [Flags]
        internal enum ControlFlags
        {
            ConfigUpToDate = 1 << 0,
            IsNoisy = 1 << 1,
            IsSynthetic = 1 << 2,
        }

        internal bool isConfigUpToDate
        {
            get { return (m_ControlFlags & ControlFlags.ConfigUpToDate) == ControlFlags.ConfigUpToDate; }
            set
            {
                if (value)
                    m_ControlFlags |= ControlFlags.ConfigUpToDate;
                else
                    m_ControlFlags &= ~ControlFlags.ConfigUpToDate;
            }
        }

        internal bool hasDefaultValue => !m_DefaultValue.isEmpty;

        // This method exists only to not slap the internal interaction on all overrides of
        // FinishSetup().
        internal void CallFinishSetupRecursive(InputDeviceBuilder builder)
        {
            for (var i = 0; i < m_ChildrenReadOnly.Count; ++i)
                m_ChildrenReadOnly[i].CallFinishSetupRecursive(builder);

            FinishSetup(builder);
        }

        internal string MakeChildPath(string path)
        {
            if (this is InputDevice)
                return path;
            return $"{this.path}/{path}";
        }

        internal void BakeOffsetIntoStateBlockRecursive(uint offset)
        {
            m_StateBlock.byteOffset += offset;

            for (var i = 0; i < m_ChildrenReadOnly.Count; ++i)
                m_ChildrenReadOnly[i].BakeOffsetIntoStateBlockRecursive(offset);
        }

        internal int ResolveDeviceIndex()
        {
            var deviceIndex = m_Device.m_DeviceIndex;
            if (deviceIndex == InputDevice.kInvalidDeviceIndex)
                throw new InvalidOperationException(
                    $"Cannot query value of control '{path}' before '{device.name}' has been added to system!");
            return deviceIndex;
        }

        internal virtual void AddProcessor(object first)
        {
        }

        internal virtual void ClearProcessors()
        {
        }
    }

    /// <summary>
    /// Base class for input controls with a specific value type.
    /// </summary>
    /// <typeparam name="TValue">Type of value captured by the control. Note that this does not mean
    /// that the control has to store data in the given value format. A control that captures float
    /// values, for example, may be stored in state as byte values instead.</typeparam>
    public abstract class InputControl<TValue> : InputControl
        where TValue : struct
    {
        public override Type valueType => typeof(TValue);

        public override int valueSizeInBytes => UnsafeUtility.SizeOf<TValue>();

        /// <summary>
        /// Get the control's current value as read from <see cref="InputControl.currentStatePtr"/>
        /// </summary>
        /// <returns>The control's current value.</returns>
        /// <remarks>
        /// This can only be called on devices that have been added to the system (<see cref="InputDevice.added"/>).
        /// </remarks>
        public TValue ReadValue()
        {
            unsafe
            {
                return ReadValueFromState(currentStatePtr);
            }
        }

        ////REVIEW: is 'frame' really the best wording here?
        /// <summary>
        /// Get the control's value from the previous frame (<see cref="InputControl.previousFrameStatePtr"/>).
        /// </summary>
        /// <returns>The control's value in the previous frame.</returns>
        public TValue ReadValueFromPreviousFrame()
        {
            unsafe
            {
                return ReadValueFromState(previousFrameStatePtr);
            }
        }

        /// <summary>
        /// Get the control's default value.
        /// </summary>
        /// <returns>The control's default value.</returns>
        /// <remarks>
        /// This is not necessarily equivalent to <c>default(TValue)</c>. A control's default value is determined
        /// by reading its value from the default state (<see cref="InputControl.defaultStatePtr"/>) which in turn
        /// is determined from settings in the control's registered layout (<see cref="InputControlLayout.ControlItem.defaultState"/>).
        /// </remarks>
        public TValue ReadDefaultValue()
        {
            unsafe
            {
                return ReadValueFromState(defaultStatePtr);
            }
        }

        public unsafe TValue ReadValueFromState(void* statePtr)
        {
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));
            return ProcessValue(ReadUnprocessedValueFromState(statePtr));
        }

        public TValue ReadUnprocessedValue()
        {
            unsafe
            {
                return ReadUnprocessedValueFromState(currentStatePtr);
            }
        }

        public abstract unsafe TValue ReadUnprocessedValueFromState(void* statePtr);

        /// <inheritdoc />
        public override unsafe object ReadValueFromStateAsObject(void* statePtr)
        {
            return ReadValueFromState(statePtr);
        }

        /// <inheritdoc />
        public override unsafe void ReadValueFromStateIntoBuffer(void* statePtr, void* bufferPtr, int bufferSize)
        {
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));
            if (bufferPtr == null)
                throw new ArgumentNullException(nameof(bufferPtr));

            var numBytes = UnsafeUtility.SizeOf<TValue>();
            if (bufferSize < numBytes)
                throw new ArgumentException(
                    $"bufferSize={bufferSize} < sizeof(TValue)={numBytes}", nameof(bufferSize));

            var value = ReadValueFromState(statePtr);
            var valuePtr = UnsafeUtility.AddressOf(ref value);

            UnsafeUtility.MemCpy(bufferPtr, valuePtr, numBytes);
        }

        public override unsafe void WriteValueFromBufferIntoState(void* bufferPtr, int bufferSize, void* statePtr)
        {
            if (bufferPtr == null)
                throw new ArgumentNullException(nameof(bufferPtr));
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));

            var numBytes = UnsafeUtility.SizeOf<TValue>();
            if (bufferSize < numBytes)
                throw new ArgumentException(
                    $"bufferSize={bufferSize} < sizeof(TValue)={numBytes}", nameof(bufferSize));

            // C# won't let us use a pointer to a generically defined type. Work
            // around this by using UnsafeUtility.
            var value = default(TValue);
            var valuePtr = UnsafeUtility.AddressOf(ref value);
            UnsafeUtility.MemCpy(valuePtr, bufferPtr, numBytes);

            WriteValueIntoState(value, statePtr);
        }

        /// <inheritdoc />
        public override unsafe void WriteValueFromObjectIntoState(object value, void* statePtr)
        {
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // If value is not of expected type, try to convert.
            if (!(value is TValue))
                value = Convert.ChangeType(value, typeof(TValue));

            var valueOfType = (TValue)value;
            WriteValueIntoState(valueOfType, statePtr);
        }

        public virtual unsafe void WriteValueIntoState(TValue value, void* statePtr)
        {
            ////REVIEW: should we be able to even tell from layouts which controls support writing and which don't?

            throw new NotSupportedException(
                $"Control '{this}' does not support writing");
        }

        /// <inheritdoc />
        public override unsafe object ReadValueFromBufferAsObject(void* buffer, int bufferSize)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var valueSize = UnsafeUtility.SizeOf<TValue>();
            if (bufferSize < valueSize)
                throw new ArgumentException(
                    $"Expecting buffer of at least {valueSize} bytes for value of type {typeof(TValue).Name} but got buffer of only {bufferSize} bytes instead",
                    nameof(bufferSize));

            var value = default(TValue);
            var valuePtr = UnsafeUtility.AddressOf(ref value);
            UnsafeUtility.MemCpy(valuePtr, buffer, valueSize);

            return value;
        }

        public override unsafe bool CompareValue(void* firstStatePtr, void* secondStatePtr)
        {
            ////REVIEW: should we first compare state here? if there's no change in state, there can be no change in value and we can skip the rest

            var firstValue = ReadValueFromState(firstStatePtr);
            var secondValue = ReadValueFromState(secondStatePtr);

            var firstValuePtr = UnsafeUtility.AddressOf(ref firstValue);
            var secondValuePtr = UnsafeUtility.AddressOf(ref secondValue);

            // NOTE: We're comparing raw memory of processed values here (which are guaranteed to be structs or
            //       primitives), not state. Means we don't have to take bits into account here.

            return UnsafeUtility.MemCmp(firstValuePtr, secondValuePtr, UnsafeUtility.SizeOf<TValue>()) != 0;
        }

        public TValue ProcessValue(TValue value)
        {
            if (m_ProcessorStack.length > 0)
            {
                value = m_ProcessorStack.firstValue.Process(value, this);
                if (m_ProcessorStack.additionalValues != null)
                    for (var i = 0; i < m_ProcessorStack.length - 1; ++i)
                        value = m_ProcessorStack.additionalValues[i].Process(value, this);
            }
            return value;
        }

        internal InlinedArray<InputProcessor<TValue>> m_ProcessorStack;

        // Only layouts are allowed to modify the processor stack.
        internal void AddProcessor(InputProcessor<TValue> processor)
        {
            m_ProcessorStack.Append(processor);
        }

        internal void RemoveProcessor(InputProcessor<TValue> processor)
        {
            m_ProcessorStack.Remove(processor);
        }

        internal TProcessor TryGetProcessor<TProcessor>()
            where TProcessor : InputProcessor<TValue>
        {
            if (m_ProcessorStack.length > 0)
            {
                if (m_ProcessorStack.firstValue is TProcessor processor)
                    return processor;
                if (m_ProcessorStack.additionalValues != null)
                    for (var i = 0; i < m_ProcessorStack.length - 1; ++i)
                        if (m_ProcessorStack.additionalValues[i] is TProcessor result)
                            return result;
            }
            return default;
        }

        internal override void AddProcessor(object processor)
        {
            if (!(processor is InputProcessor<TValue> processorOfType))
                throw new Exception(
                    $"Cannot add processor of type '{processor.GetType().Name}' to control of type '{GetType().Name}'");
            m_ProcessorStack.Append(processorOfType);
        }

        internal override void ClearProcessors()
        {
            m_ProcessorStack = new InlinedArray<InputProcessor<TValue>>();
        }

        internal InputProcessor<TValue>[] processors => m_ProcessorStack.ToArray();
    }
}
