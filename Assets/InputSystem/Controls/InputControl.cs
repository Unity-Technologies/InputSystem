using System;
using System.Collections.ObjectModel;
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
	    public string path
	    {
		    get
		    {
			    if (m_Path == null && m_Parent != null)
				    m_Path = $"{m_Parent.path}/{m_Name}";
			    return m_Path;
		    }
	    }

	    // Template the control is based on.
	    public InputTemplate template
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
			    var statePtr = currentStatePtr;
			    if (statePtr == IntPtr.Zero)
				    return Array.Empty<byte>();
			    else
			    {
				    var buffer = new byte[stateBlock.sizeInBits / 8];
				    Marshal.Copy(currentStatePtr, buffer, 0, buffer.Length);
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
		public ReadOnlyArray<InputUsage> usages
		{
			get { return m_UsagesReadOnly; }
		}

	    // Constructor for devices which are assigned names once plugged
	    // into the system.
	    protected InputControl()
	    {
		    // Set defaults for state block setup. Subclasses may override.
		    stateBlock.usage = InputStateBlock.Usage.Input;
	    }

	    // Set up of the control has been finalized. This can be used, for example, to look up
	    // child controls for fast access.
	    // NOTE: This function will be called repeatedly in case the setup is changed repeatedly.
	    protected virtual void FinishSetup(InputControlSetup setup)
	    {
	    }
	    
		protected InputStateBlock stateBlock;
	    
		protected IntPtr currentStatePtr
		{
			get { return stateBlock.currentStatePtr; }
		}
	    
		protected IntPtr previousStatePtr
		{
			get { return stateBlock.previousStatePtr; }
		}
	    
	    // This data is initialized by InputControlSetup.
        internal string m_Name;
        internal string m_Path;
	    internal InputDevice m_Device;
	    internal InputTemplate m_Template;
	    internal InputControl m_Parent;
	    internal ReadOnlyArray<InputUsage> m_UsagesReadOnly;
		internal ReadOnlyArray<InputControl> m_ChildrenReadOnly;

	    // This method exists only to not slap the internal modifier on all overrides of
	    // FinishSetup().
	    internal void CallFinishSetup(InputControlSetup setup)
	    {
		    FinishSetup(setup);
	    }
    }
	
    // Helper to more quickly implement new control types.
	public abstract class InputControl<TValue> : InputControl
	{
		public abstract TValue value { get; }

	    public override object valueAsObject
		{
			get { return value; }
		}
		
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

        protected InputControl()
        {
        }
		
        private OptimizedArray<IInputProcessor<TValue>> m_ProcessorStack;
	}
}