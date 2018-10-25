using System;
using System.Diagnostics;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Layouts;

////REVIEW: as soon as we gain the ability to have blittable type constraints, InputControl<TValue> should be constrained such

////REVIEW: Reading and writing is asymmetric. Writing does not involve processors, reading does.

////REVIEW: While the arrays used by controls are already nicely centralized on InputDevice, InputControls still
////        hold a bunch of reference data that requires separate scanning. Can we move *all* reference data to arrays
////        on InputDevice and make InputControls reference-free? Most challenging thing probably is getting rid of
////        the InputDevice reference itself.

////FIXME: Doxygen can't handle two classes 'Foo' and 'Foo<T>'; Foo won't show any of its members and Foo<T> won't get any docs at all
////       (also Doxygen doesn't understand usings and thus only finds types if they are qualified properly)

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A typed and named value in a hierarchy of controls.
    /// </summary>
    /// <remarks>
    /// Controls do not actually store values. Instead, every control receives an InputStateBlock
    /// which, after the control's device has been added to the system, is used to read out values
    /// from the device's backing store.
    ///
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
    /// </remarks>
    /// <seealso cref="InputDevice"/>
    /// \todo Add ability to get and to set configuration on a control (probably just key/value pairs)
    /// \todo Remove the distinction between input and output controls; allow every InputControl to write values
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class InputControl
    {
        ////REVIEW: we could allow the parenthetical characters if we require escaping them in paths
        /// <summary>
        /// Characters that may not appear in control names.
        /// </summary>
        public static string ReservedCharacters = "/;{}[]<>";

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
        public string name
        {
            get { return m_Name; }
        }

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
        /// </remarks>
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
            protected set { m_DisplayName = value; }
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
        public string layout
        {
            get { return m_Layout; }
        }


        /// <summary>
        /// Semicolon-separated list of variants of the control layout or "default".
        /// </summary>
        /// <example>
        /// "Lefty" when using the "Lefty" gamepad layout.
        /// </example>
        public string variants
        {
            get { return m_Variants; }
        }

        /// <summary>
        /// The device that this control is a part of.
        /// </summary>
        /// <remarks>
        /// This is the root of the control hiearchy. For the device at the root, this
        /// will point to itself.
        /// </remarks>
        public InputDevice device
        {
            get { return m_Device; }
        }

        /// <summary>
        /// The immediate parent of the control or null if the control has no parent
        /// (which, once fully constructed) will only be the case for InputDevices).
        /// </summary>
        public InputControl parent
        {
            get { return m_Parent; }
        }

        /// <summary>
        /// List of immediate children.
        /// </summary>
        /// <remarks>
        /// Does not allocate.
        /// </remarks>
        public ReadOnlyArray<InputControl> children
        {
            get { return m_ChildrenReadOnly; }
        }

        // List of uses for this control. Gives meaning to the control such that you can, for example,
        // find a button on a device to use as the "back" button regardless of what it is named. The "back"
        // button is also an example of why there are multiple possible usages of a button as a use may
        // be context-dependent; if "back" does not make sense in a context, another use may make sense for
        // the very same button.
        public ReadOnlyArray<InternedString> usages
        {
            get { return m_UsagesReadOnly; }
        }

        // List of alternate names for the control.
        public ReadOnlyArray<InternedString> aliases
        {
            get { return m_AliasesReadOnly; }
        }

        // Information about where the control stores its state.
        public InputStateBlock stateBlock
        {
            get { return m_StateBlock; }
        }

        public bool noisy
        {
            get { return (m_ControlFlags & ControlFlags.IsNoisy) == ControlFlags.IsNoisy; }
            internal set
            {
                if (value)
                    m_ControlFlags |= ControlFlags.IsNoisy;
                else
                    m_ControlFlags &= ~ControlFlags.IsNoisy;
            }
        }

        public InputControl this[string path]
        {
            get { return InputControlPath.TryFindChild(this, path); }
        }

        /// <summary>
        /// Returns the underlying value type of this control.
        /// </summary>
        /// <remarks>
        /// This is the type of values that are returned when reading the current value of a control
        /// or when reading a value of a control from an event.
        /// </remarks>
        /// <seealso cref="valueSizeInBytes"/>
        /// <seealso cref="WriteValueInto"/>
        public abstract Type valueType { get; }

        /// <summary>
        /// Size in bytes of values that the control returns.
        /// </summary>
        /// <seealso cref="valueType"/>
        public abstract int valueSizeInBytes { get; }

        public override string ToString()
        {
            return string.Format("{0}:{1}", layout, path);
        }

        private string DebuggerDisplay()
        {
            return string.Format("{0}:{1}={2}", layout, path, ReadValueAsObject());
        }

        ////TODO: setting value

        // Current value as boxed object.
        // NOTE: Calling this will allocate.
        public abstract object ReadValueAsObject();

        public abstract object ReadDefaultValueAsObject();

        public abstract void WriteValueFromObjectInto(IntPtr buffer, long bufferSize, object value);

        public abstract unsafe void WriteValueInto(void* buffer, int bufferSize);

        public void WriteValueFromObjectInto(InputEventPtr eventPtr, object value)
        {
            var statePtr = GetStatePtrFromStateEvent(eventPtr);
            if (statePtr == IntPtr.Zero)
                return;

            var bufferSize = m_StateBlock.byteOffset + eventPtr.sizeInBytes;
            WriteValueFromObjectInto(statePtr, bufferSize, value);
        }

        public virtual bool HasSignificantChange(InputEventPtr eventPtr)
        {
            return GetStatePtrFromStateEvent(eventPtr) != IntPtr.Zero;
        }

        // Constructor for devices which are assigned names once plugged
        // into the system.
        protected InputControl()
        {
            // Set defaults for state block setup. Subclasses may override.
            m_StateBlock.byteOffset = InputStateBlock.kInvalidOffset; // Request automatic layout by default.
            m_StateBlock.bitOffset = 0;
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
        protected internal IntPtr currentStatePtr
        {
            get { return InputStateBuffers.GetFrontBufferForDevice(ResolveDeviceIndex()); }
        }
        protected internal IntPtr previousStatePtr
        {
            get { return InputStateBuffers.GetBackBufferForDevice(ResolveDeviceIndex()); }
        }
        protected internal IntPtr defaultStatePtr
        {
            get { return InputStateBuffers.s_DefaultStateBuffer; }
        }

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
        internal PrimitiveValueOrArray m_DefaultValue;

        [Flags]
        internal enum ControlFlags
        {
            ConfigUpToDate = 1 << 0,
            IsNoisy = 1 << 1,
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

        internal bool hasDefaultValue
        {
            get { return !m_DefaultValue.isEmpty; }
        }

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
            return string.Format("{0}/{1}", this.path, path);
        }

        internal void BakeOffsetIntoStateBlockRecursive(uint offset)
        {
            m_StateBlock.byteOffset += offset;

            for (var i = 0; i < m_ChildrenReadOnly.Count; ++i)
                m_ChildrenReadOnly[i].BakeOffsetIntoStateBlockRecursive(offset);
        }

        ////TODO: pass state ptr *NOT* value ptr (it's confusing)
        // NOTE: The given argument should point directly to the value *not* to the
        //       base state to which the state block offset has to be added.
        internal unsafe bool CheckStateIsAtDefault(IntPtr valuePtr = new IntPtr())
        {
            ////REVIEW: for compound controls, do we want to go check leaves so as to not pick up on non-control noise in the state?
            ////        e.g. from HID input reports

            var defaultPtr = new IntPtr((byte*)defaultStatePtr.ToPointer() + (int)m_StateBlock.byteOffset);
            if (valuePtr == IntPtr.Zero)
                valuePtr = new IntPtr(currentStatePtr.ToInt64() + (int)m_StateBlock.byteOffset);

            if (m_StateBlock.sizeInBits == 1)
            {
                return MemoryHelpers.ReadSingleBit(valuePtr, m_StateBlock.bitOffset) ==
                    MemoryHelpers.ReadSingleBit(defaultPtr, m_StateBlock.bitOffset);
            }

            return MemoryHelpers.MemCmpBitRegion(defaultPtr.ToPointer(), valuePtr.ToPointer(),
                m_StateBlock.bitOffset, m_StateBlock.sizeInBits);
        }

        internal unsafe IntPtr GetStatePtrFromStateEvent(InputEventPtr eventPtr)
        {
            if (!eventPtr.valid)
                throw new ArgumentNullException("eventPtr");

            uint stateOffset;
            FourCC stateFormat;
            uint stateSizeInBytes;
            IntPtr statePtr;
            if (eventPtr.IsA<DeltaStateEvent>())
            {
                var deltaEvent = DeltaStateEvent.From(eventPtr);

                // If it's a delta event, we need to subtract the delta state offset if it's not set to the root of the device
                stateOffset = deltaEvent->stateOffset;
                stateFormat = deltaEvent->stateFormat;
                stateSizeInBytes = deltaEvent->deltaStateSizeInBytes;
                statePtr = deltaEvent->deltaState;
            }
            else if (eventPtr.IsA<StateEvent>())
            {
                var stateEvent = StateEvent.From(eventPtr);

                stateOffset = 0;
                stateFormat = stateEvent->stateFormat;
                stateSizeInBytes = stateEvent->stateSizeInBytes;
                statePtr = stateEvent->state;
            }
            else
            {
                throw new ArgumentException("Event must be a state or delta state event", "eventPtr");
            }

            // Make sure we have a state event compatible with our device. The event doesn't
            // have to be specifically for our device (we don't require device IDs to match) but
            // the formats have to match and the size must be within range of what we're trying
            // to read.
            if (stateFormat != device.m_StateBlock.format)
                throw new InvalidOperationException(
                    string.Format(
                        "Cannot read control '{0}' from {1} with format {2}; device '{3}' expects format {4}",
                        path, eventPtr.type, stateFormat, device, device.m_StateBlock.format));

            // Once a device has been added, global state buffer offsets are baked into control hierarchies.
            // We need to unsubtract those offsets here.
            stateOffset += device.m_StateBlock.byteOffset;

            if (m_StateBlock.byteOffset - stateOffset + m_StateBlock.alignedSizeInBytes > stateSizeInBytes)
                return IntPtr.Zero;

            return new IntPtr(statePtr.ToInt64() - (int)stateOffset);
        }

        internal int ResolveDeviceIndex()
        {
            var deviceIndex = m_Device.m_DeviceIndex;
            if (deviceIndex == InputDevice.kInvalidDeviceIndex)
                throw new InvalidOperationException(string.Format(
                    "Cannot query value of control '{0}' before '{1}' has been added to system!", path, device.name));
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
        public override Type valueType
        {
            get { return typeof(TValue); }
        }

        public override int valueSizeInBytes
        {
            get { return UnsafeUtility.SizeOf<TValue>(); }
        }

        public TValue ReadValue()
        {
            return ReadValueFrom(currentStatePtr);
        }

        ////REVIEW: rename this to something like ReadValueFromPreviousFrame()?
        public TValue ReadPreviousValue()
        {
            return ReadValueFrom(previousStatePtr);
        }

        public TValue ReadDefaultValue()
        {
            return ReadValueFrom(defaultStatePtr);
        }

        public override object ReadValueAsObject()
        {
            return ReadValue();
        }

        public override object ReadDefaultValueAsObject()
        {
            return ReadDefaultValue();
        }

        public override unsafe void WriteValueInto(void* buffer, int bufferSize)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (bufferSize < UnsafeUtility.AlignOf<TValue>())
                throw new ArgumentException(
                    string.Format("bufferSize={0} < sizeof(TValue)={1}", bufferSize, valueSizeInBytes), "bufferSize");

            var adjustedBufferPtr = (byte*)buffer - m_StateBlock.byteOffset;
            WriteUnprocessedValueInto(new IntPtr(adjustedBufferPtr), ReadValue());
        }

        public override void WriteValueFromObjectInto(IntPtr buffer, long bufferSize, object value)
        {
            if (buffer == IntPtr.Zero)
                throw new ArgumentNullException("buffer");
            if (value == null)
                throw new ArgumentNullException("value");
            if (bufferSize < (m_StateBlock.byteOffset + m_StateBlock.alignedSizeInBytes))
                throw new ArgumentException(
                    string.Format("Buffer size {0} is too small for control at offset {1} with length {2}", bufferSize,
                        m_StateBlock.byteOffset, m_StateBlock.alignedSizeInBytes), "bufferSize");

            // If value is not of expected type, try to convert.
            if (!(value is TValue))
                value = Convert.ChangeType(value, typeof(TValue));

            WriteUnprocessedValueInto(buffer, (TValue)value);
        }

        // NOTE: Using this method not only ensures that format conversion is automatically taken care of
        //       but also profits from the fact that remapping is already established in a control hierarchy
        //       and reading from the right offsets is taken care of.
        public bool ReadValueFrom(InputEventPtr inputEvent, out TValue value)
        {
            var statePtr = GetStatePtrFromStateEvent(inputEvent);
            if (statePtr == IntPtr.Zero)
            {
                value = ReadDefaultValue();
                return false;
            }

            value = ReadValueFrom(statePtr);
            return true;
        }

        public TValue ReadUnprocessedValueFrom(InputEventPtr eventPtr)
        {
            var result = default(TValue);
            ReadUnprocessedValueFrom(eventPtr, out result);
            return result;
        }

        public bool ReadUnprocessedValueFrom(InputEventPtr inputEvent, out TValue value)
        {
            var statePtr = GetStatePtrFromStateEvent(inputEvent);
            if (statePtr == IntPtr.Zero)
            {
                value = ReadDefaultValue();
                return false;
            }

            value = ReadUnprocessedValueFrom(statePtr);
            return true;
        }

        public TValue ReadValueFrom(IntPtr statePtr)
        {
            return Process(ReadUnprocessedValueFrom(statePtr));
        }

        public TValue ReadUnprocessedValue()
        {
            return ReadUnprocessedValueFrom(currentStatePtr);
        }

        public abstract TValue ReadUnprocessedValueFrom(IntPtr statePtr);

        protected virtual void WriteUnprocessedValueInto(IntPtr statePtr, TValue value)
        {
            ////TODO: indicate properly that this control does not support writing
            throw new NotSupportedException();
        }

        public void WriteValueInto(InputEventPtr eventPtr)
        {
            ////REVIEW: have an option to write unprocessed values?
            WriteValueInto(eventPtr, ReadValue());
        }

        public void WriteValueInto(InputEventPtr eventPtr, TValue value)
        {
            var statePtr = GetStatePtrFromStateEvent(eventPtr);
            if (statePtr == IntPtr.Zero)
                return;

            WriteValueInto(statePtr, value);
        }

        public void WriteValueInto(IntPtr statePtr)
        {
            WriteValueInto(statePtr, ReadValue());
        }

        public void WriteValueInto(IntPtr statePtr, TValue value)
        {
            WriteUnprocessedValueInto(statePtr, value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
        /// <param name="value"></param>
        /// <typeparam name="TState"></typeparam>
        /// <exception cref="ArgumentException">Control's value does not fit within the memory of <paramref name="state"/>.</exception>
        public unsafe void WriteValueInto<TState>(ref TState state, TValue value)
            where TState : struct, IInputStateTypeInfo
        {
            // Make sure the control's state actually fits within the given state.
            var sizeOfState = UnsafeUtility.SizeOf<TState>();
            if (stateOffsetRelativeToDeviceRoot + m_StateBlock.alignedSizeInBytes >= sizeOfState)
                throw new ArgumentException(
                    string.Format("Control {0} with offset {1} and size of {2} bits is out of bounds for state of type {3} with size {4}",
                        path, stateOffsetRelativeToDeviceRoot, m_StateBlock.sizeInBits, typeof(TState).Name, sizeOfState), "state");

            // Write value.
            var addressOfState = (byte*)UnsafeUtility.AddressOf(ref state);
            var adjustedStatePtr = addressOfState - device.m_StateBlock.byteOffset;
            WriteValueInto(new IntPtr(adjustedStatePtr), value);
        }

        public TValue Process(TValue value)
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

        internal InlinedArray<IInputControlProcessor<TValue>> m_ProcessorStack;

        // Only layouts are allowed to modify the processor stack.
        internal void AddProcessor(IInputControlProcessor<TValue> processor)
        {
            m_ProcessorStack.Append(processor);
        }

        internal void RemoveProcessor(IInputControlProcessor<TValue> processor)
        {
            m_ProcessorStack.Remove(processor);
        }

        internal TProcessor TryGetProcessor<TProcessor>()
            where TProcessor : IInputControlProcessor<TValue>
        {
            if (m_ProcessorStack.length > 0)
            {
                if (m_ProcessorStack.firstValue is TProcessor)
                    return (TProcessor)m_ProcessorStack.firstValue;
                if (m_ProcessorStack.additionalValues != null)
                    for (var i = 0; i < m_ProcessorStack.length - 1; ++i)
                        if (m_ProcessorStack.additionalValues[i] is TProcessor)
                            return (TProcessor)m_ProcessorStack.additionalValues[i];
            }
            return default(TProcessor);
        }

        internal override void AddProcessor(object processor)
        {
            var processorOfType = processor as IInputControlProcessor<TValue>;
            if (processorOfType == null)
                throw new Exception(string.Format("Cannot add processor of type '{0}' to control of type '{1}'",
                    processor.GetType().Name, GetType().Name));
            m_ProcessorStack.Append(processorOfType);
        }

        internal override void ClearProcessors()
        {
            m_ProcessorStack = new InlinedArray<IInputControlProcessor<TValue>>();
        }

        internal IInputControlProcessor<TValue>[] processors
        {
            get { return m_ProcessorStack.ToArray(); }
        }
    }
}
