using System;
using UnityEngine;

namespace ISX
{
    // A typed and named value.
    // Actual value is stored in central state storage managed by InputSystem.
    // Controls form hierarchies and can be looked with paths.
    // Can have usages that give meaning to the control.
    public abstract class InputControl
    {
        // Final name part of the path.
        public string name => m_Name;

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
                        m_Path = $"{m_Parent.path}/{m_Name}";
                    else
                        m_Path = $"/{m_Name}";
                }
                return m_Path;
            }
        }

        // Template the control is based on.
        // We store the name rather than reference the InputTemplate as we want
        // to avoid allocating those objects except where necessary.
        public string template => m_Template;

        // Variant of the template or "default".
        // Example: "Lefty" when using the "Lefty" gamepad layout.
        public string variant => m_Variant;

        ////TODO: setting value (will it also go through the processor stack?)

        // Current value as boxed object.
        // NOTE: Calling this will cause garbage.
        public virtual object valueAsObject => null;

        // Root of the control hierarchy.
        public InputDevice device => m_Device;

        // Immediate parent.
        public InputControl parent => m_Parent;

        // Immediate children.
        // NOTE: This will only be populated when setup is finished.
        public ReadOnlyArray<InputControl> children => m_ChildrenReadOnly;

        // List of uses for this control. Gives meaning to the control such that you can, for example,
        // find a button on a device to use as the "back" button regardless of what it is named. The "back"
        // button is also an example of why there are multiple possible usages of a button as a use may
        // be context-dependent; if "back" does not make sense in a context, another use may make sense for
        // the very same button.
        // NOTE: This will only be populated when setup is finished.
        public ReadOnlyArray<InternedString> usages => m_UsagesReadOnly;

        // List of alternate names for the control.
        public ReadOnlyArray<string> aliases => m_AliasesReadOnly;

        // Information about where the control stores its state.
        public InputStateBlock stateBlock => m_StateBlock;

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

        protected internal IntPtr currentValuePtr =>
        InputStateBuffers.GetFrontBuffer(ResolveDeviceIndex()) + (int)m_StateBlock.byteOffset;
        protected internal IntPtr previousValuePtr =>
        InputStateBuffers.GetBackBuffer(ResolveDeviceIndex()) + (int)m_StateBlock.byteOffset;


        // This data is initialized by InputControlSetup.
        internal InternedString m_Name;
        internal string m_Path;
        internal InternedString m_Template;
        internal InternedString m_Variant;
        internal InputDevice m_Device;
        internal InputControl m_Parent;
        internal ReadOnlyArray<InternedString> m_UsagesReadOnly;
        internal ReadOnlyArray<string> m_AliasesReadOnly;
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
            return $"{this.path}/{path}";
        }

        internal void BakeOffsetIntoStateBlockRecursive(uint offset)
        {
            m_StateBlock.byteOffset += offset;

            for (var i = 0; i < m_ChildrenReadOnly.Count; ++i)
                m_ChildrenReadOnly[i].BakeOffsetIntoStateBlockRecursive(offset);
        }

        // We don't allow custom default values for state so all zeros indicates
        // default states for us.
        // NOTE: The given argument should point directly to the value *not* to the
        //       base state to which the state block offset has to be added.
        internal unsafe bool CheckStateIsAllZeros(IntPtr valuePtr = new IntPtr())
        {
            if (valuePtr == IntPtr.Zero)
                valuePtr = currentValuePtr;

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
                throw new InvalidOperationException($"Cannot query value of control '{path}' before '{device.name}' has been added to system!");
            return deviceIndex;
        }

        internal virtual void AddProcessor(object first)
        {
        }
    }

    // Helper to more quickly implement new control types.
    public abstract class InputControl<TValue> : InputControl
    {
        public abstract TValue value { get; }
        public abstract TValue previous { get; }

        public override object valueAsObject => value;

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
                throw new Exception($"Cannot add processor of type '{processor.GetType().Name}' to control of type '{GetType().Name}'");
            m_ProcessorStack.Append(processorOfType);
        }

        internal IInputProcessor<TValue>[] processors => m_ProcessorStack.ToArray();
    }
}
