using System;
using System.Runtime.InteropServices;

namespace ISX
{
    // A typed and named value.
    // Actual value is stored in central state storage managed by InputSystem.
    // Controls form hierarchies and can be looked with paths.
    // Can have usages that give meaning to the control.
    public abstract class InputControl
    {
        // Final name part of the path.
        public string name
        {
            get { return m_Name; }
        }

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
        public string template
        {
            get { return m_Template; }
        }

        ////TODO: setting value (will it also go through the processor stack?)
        // Current value as boxed object.
        // NOTE: Calling this will cause garbage.
        public virtual object valueAsObject
        {
            get
            {
                ////REVIEW: Not sure yet which is better; return null or raw byte data?
                ////        Actually, this should probably always return a value of the type given in the template
                var statePtr = currentValuePtr;
                if (statePtr == IntPtr.Zero)
                    return Array.Empty<byte>();
                else
                {
                    var buffer = new byte[m_StateBlock.sizeInBits / 8];
                    Marshal.Copy(currentValuePtr, buffer, 0, buffer.Length);
                    return buffer;
                }
            }
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
        // NOTE: This will only be populated when setup is finished.
        public ReadOnlyArray<InputControl> children
        {
            get { return m_ChildrenReadOnly; }
        }

        // List of uses for this control. Gives meaning to the control such that you can, for example,
        // find a button on a device to use as the "back" button regardless of what it is named. The "back"
        // button is also an example of why there are multiple possible usages of a button as a use may
        // be context-dependent; if "back" does not make sense in a context, another use may make sense for
        // the very same button.
        // NOTE: This will only be populated when setup is finished.
        public ReadOnlyArray<string> usages
        {
            get { return m_UsagesReadOnly; }
        }

        // List of alternate names for the control.
        public ReadOnlyArray<string> aliases
        {
            get { return m_AliasesReadOnly; }
        }

        // Information about where the control stores its state.
        public InputStateBlock stateBlock
        {
            get { return m_StateBlock; }
        }

        // Constructor for devices which are assigned names once plugged
        // into the system.
        protected InputControl()
        {
            // Set defaults for state block setup. Subclasses may override.
            m_StateBlock.semantics = InputStateBlock.Semantics.Input;
            m_StateBlock.byteOffset = InputStateBlock.kInvalidOffset;
            m_StateBlock.bitOffset = 0;
        }

        // Set up of the control has been finalized. This can be used, for example, to look up
        // child controls for fast access.
        // NOTE: This function will be called repeatedly in case the setup is changed repeatedly.
        protected virtual void FinishSetup(InputControlSetup setup)
        {
        }

        protected internal InputStateBlock m_StateBlock;

        protected IntPtr currentValuePtr =>
        InputStateBuffers.GetFrontBuffer(ResolveDeviceIndex()) + (int)m_StateBlock.byteOffset;
        protected IntPtr previousValuePtr =>
        InputStateBuffers.GetBackBuffer(ResolveDeviceIndex()) + (int)m_StateBlock.byteOffset;

        // This data is initialized by InputControlSetup.
        internal string m_Name;
        internal string m_Path;
        internal string m_Template;
        internal InputDevice m_Device;
        internal InputControl m_Parent;
        internal ReadOnlyArray<string> m_UsagesReadOnly;
        internal ReadOnlyArray<string> m_AliasesReadOnly;
        internal ReadOnlyArray<InputControl> m_ChildrenReadOnly;

        // This method exists only to not slap the internal modifier on all overrides of
        // FinishSetup().
        internal void CallFinishSetup(InputControlSetup setup)
        {
            for (var i = 0; i < m_ChildrenReadOnly.Count; ++i)
                m_ChildrenReadOnly[i].CallFinishSetup(setup);

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

        internal unsafe bool CheckStateIsAllZeroes()
        {
            var numBytes = m_StateBlock.alignedSizeInBytes;
            var ptr = (byte*)currentValuePtr;

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
    }

    // Helper to more quickly implement new control types.
    public abstract class InputControl<TValue> : InputControl
    {
        public abstract TValue value { get; }
        public abstract TValue previous { get; }

        public override object valueAsObject
        {
            get { return value; }
        }

        ////TODO: make AddProcessor and RemoveProcessor internal and allow usage through templates only
        public void AddProcessor(IInputProcessor<TValue> processor)
        {
            m_ProcessorStack.Append(processor);
        }

        public void RemoveProcessor(IInputProcessor<TValue> processor)
        {
            m_ProcessorStack.Remove(processor);
        }

        protected TValue Process(TValue value)
        {
            if (m_ProcessorStack.firstValue != null)
                value = m_ProcessorStack.firstValue.Process(value);
            if (m_ProcessorStack.additionalValues != null)
                for (var i = 0; i < m_ProcessorStack.additionalValues.Length; ++i)
                    value = m_ProcessorStack.additionalValues[i].Process(value);
            return value;
        }

        private OptimizedArray<IInputProcessor<TValue>> m_ProcessorStack;
    }
}
