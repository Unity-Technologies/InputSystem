using System;
using System.Collections.Generic;

////FIXME: doxygen can't handle two classes 'Foo' and 'Foo<T>'; Foo won't show any of its members and Foo<T> won't get any docs at all

namespace ISX
{
    using Configuration = KeyValuePair<InternedString, PrimitiveValue>;

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
    /// Controls can be looked up by their path (see <see cref="InputControlSetup.GetControl"/> and
    /// <see cref="InputControlPath.FindControl"/>).
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
    public abstract class InputControl
    {
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

        ////TODO: include icon-related info from control template

        // Full semantic path all the way from the root.
        // NOTE: Allocates on first hit. We don't create paths until someone asks for them.
        public string path
        {
            get
            {
                if (m_Path == null)
                {
                    if (m_Parent != null)
                        m_Path = string.Format("{0}/{1}", m_Parent.path, m_Name);
                    else
                        m_Path = string.Format("/{0}", m_Name);
                }
                return m_Path;
            }
        }

        // Template the control is based on.
        // We store the name rather than reference the InputTemplate as we want
        // to avoid allocating those objects except where necessary.
        public string template
        {
            get { return m_Template; }
        }

        // Variant of the template or "default".
        // Example: "Lefty" when using the "Lefty" gamepad layout.
        public string variant
        {
            get { return m_Variant; }
        }

        ////TODO: setting value (will it also go through the processor stack?)

        // Current value as boxed object.
        // NOTE: Calling this will cause garbage.
        public virtual object valueAsObject
        {
            get { return null; }
        }

        // Root of the control hierarchy.
        public InputDevice device
        {
            get { return m_Device; }
        }

        // Immediate parent.
        public InputControl parent
        {
            get { return m_Parent; }
        }

        // Immediate children.
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

        public override string ToString()
        {
            return string.Format("{0}:{1}", template, path);
        }

        public TValue GetValue<TValue>()
        {
            var controlOfType = this as InputControl<TValue>;
            if (controlOfType == null)
                throw new InvalidCastException(
                    string.Format("Cannot query value of type '{0}' from control of type '{1}", typeof(TValue).Name,
                        this.GetType().Name));
            return controlOfType.value;
        }

        ////REVIEW: where would this go? what about configuration events?
        public virtual ReadOnlyArray<Configuration> GetConfiguration()
        {
            //For device:
            //  If native device (have flag on control), call through to NativeInputSystem.GetDeviceConfiguration
            //For controls:
            //  Same but need to find way to identify the control on the device
            throw new NotImplementedException();
        }

        public virtual void SetConfiguration(ReadOnlyArray<Configuration> configuration)
        {
            //For native API, instead of sending actual string names for keys, CRC the keys and use ints for communication
            throw new NotImplementedException();
        }

        // Constructor for devices which are assigned names once plugged
        // into the system.
        protected InputControl()
        {
            // Set defaults for state block setup. Subclasses may override.
            m_StateBlock.semantics = InputStateBlock.Semantics.Input;
            m_StateBlock.byteOffset = InputStateBlock.kInvalidOffset; // Request automatic layout by default.
            m_StateBlock.bitOffset = 0;
        }

        // Set up of the control has been finalized. This can be used, for example, to look up
        // child controls for fast access.
        // NOTE: This function will be called repeatedly in case the setup is changed repeatedly.
        protected virtual void FinishSetup(InputControlSetup setup)
        {
        }

        protected internal InputStateBlock m_StateBlock;

        protected internal IntPtr currentStatePtr
        {
            get { return InputStateBuffers.GetFrontBuffer(ResolveDeviceIndex()); }
        }

        protected internal IntPtr previousStatePtr
        {
            get { return InputStateBuffers.GetBackBuffer(ResolveDeviceIndex()); }
        }

        // This data is initialized by InputControlSetup.
        internal InternedString m_Name;
        internal string m_Path;
        internal InternedString m_Template;
        internal InternedString m_Variant;
        internal InputDevice m_Device;
        internal InputControl m_Parent;
        internal ReadOnlyArray<InternedString> m_UsagesReadOnly;
        internal ReadOnlyArray<InternedString> m_AliasesReadOnly;
        internal ReadOnlyArray<InputControl> m_ChildrenReadOnly;

        // This method exists only to not slap the internal modifier on all overrides of
        // FinishSetup().
        internal void CallFinishSetupRecursive(InputControlSetup setup)
        {
            for (var i = 0; i < m_ChildrenReadOnly.Count; ++i)
                m_ChildrenReadOnly[i].CallFinishSetupRecursive(setup);

            FinishSetup(setup);
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
        // We don't allow custom default values for state so all zeros indicates
        // default states for us.
        // NOTE: The given argument should point directly to the value *not* to the
        //       base state to which the state block offset has to be added.
        internal unsafe bool CheckStateIsAllZeros(IntPtr valuePtr = new IntPtr())
        {
            if (valuePtr == IntPtr.Zero)
                valuePtr = new IntPtr(currentStatePtr.ToInt64() + (int)m_StateBlock.byteOffset);

            // Bitfield value.
            if (m_StateBlock.sizeInBits % 8 != 0 || m_StateBlock.bitOffset != 0)
            {
                if (m_StateBlock.sizeInBits > 1)
                    throw new NotImplementedException("multi-bit zero check");

                return BitfieldHelpers.ReadSingleBit(valuePtr, m_StateBlock.bitOffset) == false;
            }

            // Multi-byte value.
            var ptr = (byte*)valuePtr;
            var numBytes = m_StateBlock.alignedSizeInBytes;
            for (var i = 0; i < numBytes; ++i, ++ptr)
                if (*ptr != 0)
                    return false;

            return true;
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
    }

    // Helper to more quickly implement new control types.
    // Adds processing stack.
    public abstract class InputControl<TValue> : InputControl
    {
        public TValue value
        {
            get { return ReadValueFrom(currentStatePtr); }
        }

        public TValue previous
        {
            get { return ReadValueFrom(previousStatePtr); }
        }

        public override object valueAsObject
        {
            get { return value; }
        }

        // Read a control value directly from a state event.
        //
        // NOTE: Using this method not only ensures that format conversion is automatically taken care of
        //       but also profits from the fact that remapping is already established in a control hierarchy
        //       and reading from the right offsets is taken care of.
        public unsafe TValue ReadValueFrom(InputEventPtr inputEvent, bool process = true)
        {
            if (!inputEvent.valid)
                throw new ArgumentNullException("inputEvent");
            if (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>())
                throw new ArgumentException("Event must be a state or delta state event", "inputEvent");

            ////TODO: support delta events
            if (inputEvent.IsA<DeltaStateEvent>())
                throw new NotImplementedException("Read control value from delta state events");

            var stateEvent = StateEvent.From(inputEvent);

            // Make sure we have a state event compatible with our device. The event doesn't
            // have to be specifically for our device (we don't require device IDs to match) but
            // the formats have to match and the size must be within range of what we're trying
            // to read.
            var stateFormat = stateEvent->stateFormat;
            if (stateEvent->stateFormat != device.m_StateBlock.format)
                throw new InvalidOperationException(
                    string.Format(
                        "Cannot read control '{0}' from StateEvent with format {1}; device '{2}' expects format {3}",
                        path, stateFormat, device, device.m_StateBlock.format));

            // Once a device has been added, global state buffer offsets are baked into control hierarchies.
            // We need to unsubtract those offsets here.
            var deviceStateOffset = device.m_StateBlock.byteOffset;

            var stateSizeInBytes = stateEvent->stateSizeInBytes;
            if (m_StateBlock.byteOffset - deviceStateOffset + m_StateBlock.alignedSizeInBytes > stateSizeInBytes)
                throw new Exception(
                    string.Format(
                        "StateEvent with format {0} and size {1} bytes provides less data than expected by control {2}",
                        stateFormat, stateSizeInBytes, path));

            var statePtr = new IntPtr(stateEvent->state.ToInt64() - (int)deviceStateOffset);
            var value = ReadRawValueFrom(statePtr);

            if (process)
                value = Process(value);

            return value;
        }

        public TValue ReadValueFrom(IntPtr statePtr)
        {
            return Process(ReadRawValueFrom(statePtr));
        }

        protected abstract TValue ReadRawValueFrom(IntPtr statePtr);

        protected TValue Process(TValue value)
        {
            if (m_ProcessorStack.firstValue != null)
                value = m_ProcessorStack.firstValue.Process(value);
            if (m_ProcessorStack.additionalValues != null)
                for (var i = 0; i < m_ProcessorStack.additionalValues.Length; ++i)
                    value = m_ProcessorStack.additionalValues[i].Process(value);
            return value;
        }

        internal InlinedArray<IInputProcessor<TValue>> m_ProcessorStack;

        // Only templates are allowed to modify the processor stack.
        internal void AddProcessor(IInputProcessor<TValue> processor)
        {
            m_ProcessorStack.Append(processor);
        }

        internal void RemoveProcessor(IInputProcessor<TValue> processor)
        {
            m_ProcessorStack.Remove(processor);
        }

        internal TProcessor TryGetProcessor<TProcessor>()
            where TProcessor : IInputProcessor<TValue>
        {
            if (m_ProcessorStack.firstValue is TProcessor)
                return (TProcessor)m_ProcessorStack.firstValue;
            if (m_ProcessorStack.additionalValues != null)
                for (var i = 0; i < m_ProcessorStack.additionalValues.Length; ++i)
                    if (m_ProcessorStack.additionalValues[i] is TProcessor)
                        return (TProcessor)m_ProcessorStack.additionalValues[i];
            return default(TProcessor);
        }

        internal override void AddProcessor(object processor)
        {
            var processorOfType = processor as IInputProcessor<TValue>;
            if (processorOfType == null)
                throw new Exception(string.Format("Cannot add processor of type '{0}' to control of type '{1}'",
                        processor.GetType().Name, GetType().Name));
            m_ProcessorStack.Append(processorOfType);
        }

        internal IInputProcessor<TValue>[] processors
        {
            get { return m_ProcessorStack.ToArray(); }
        }
    }
}
