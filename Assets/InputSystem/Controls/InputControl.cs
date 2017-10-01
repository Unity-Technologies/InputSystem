using System;
using System.Collections.ObjectModel;

namespace InputSystem
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
	    
	    ////REVIEW: store the template the control is based on?
	    
	    // Current value as boxed object.
	    // NOTE: Calling this will cause garbage.
		public abstract object valueAsObject { get; }
	    ////TODO: setting value (will it also go through the processor stack?)
	    
	    ////REVIEW: cache root and expose as property?
	    
	    // Immediate parent.
		public InputControl parent
		{
			get { return m_Parent; }
		}

	    // Immediate children.
	    public ReadOnlyCollection<InputControl> children
	    {
		    get
		    {
				if (m_ChildrenReadOnly == null)
				{
					var children = m_Children;
					if (children == null)
						children = Array.Empty<InputControl>();
					m_ChildrenReadOnly = Array.AsReadOnly(children);
				}
				return m_ChildrenReadOnly;
		    }
	    }
	    
		// List of uses for this control. Gives meaning to the control such that you can, for example,
		// find a button on a device to use as the "back" button regardless of what it is named. The "back"
		// button is also an example of why there are multiple possible usages of a button as a use may
		// be context-dependent; if "back" does not make sense in a context, another use may make sense for
		// the very same button.
		public ReadOnlyCollection<InputUsage> usages
		{
			get
			{
				if (m_UsagesReadOnly == null)
				{
                    var usages = m_Usages;
                    if (usages == null)
                        usages = Array.Empty<InputUsage>();
                    m_UsagesReadOnly = Array.AsReadOnly(usages);
				}
				return m_UsagesReadOnly;
			}
		}

	    protected InputControl(string name)
	    {
		    m_Name = name;
		    
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
	    
	    ////TODO: generalize so that the optimized array storage can also be used for usages
        // Helper to implement processing.
	    protected struct ProcessorStack<TValue>
	    {
		    // We inline the first processor so if there's only one, there's
		    // no additional allocation. If more are added, we allocate an array.
	        private IInputProcessor<TValue> m_FirstProcessor;
		    private IInputProcessor<TValue>[] m_AdditionalProcessors;

	        public void AddProcessor(IInputProcessor<TValue> processor)
	        {
		        if (m_FirstProcessor == null)
		        {
			        m_FirstProcessor = processor;
		        }
		        else if (m_AdditionalProcessors == null)
		        {
			        m_AdditionalProcessors = new IInputProcessor<TValue>[1];
			        m_AdditionalProcessors[0] = processor;
		        }
		        else
		        {
			        var numAdditionalProcessors = m_AdditionalProcessors.Length;
			        Array.Resize(ref m_AdditionalProcessors, numAdditionalProcessors + 1);
			        m_AdditionalProcessors[numAdditionalProcessors] = processor;
		        }
	        }

	        public void RemoveProcessor(IInputProcessor<TValue> processor)
	        {
		        if (m_FirstProcessor == processor)
		        {
			        if (m_AdditionalProcessors != null)
			        {
				        m_FirstProcessor = m_AdditionalProcessors[0];
				        if (m_AdditionalProcessors.Length == 1)
					        m_AdditionalProcessors = null;
				        else
					        Array.Resize(ref m_AdditionalProcessors, m_AdditionalProcessors.Length - 1);
			        }
			        else
			        {
				        m_FirstProcessor = null;
			        }
		        }
		        else if (m_AdditionalProcessors != null)
		        {
			        var numAdditionalProcessors = m_AdditionalProcessors.Length;
			        for (var i = 0; i < numAdditionalProcessors; ++i)
			        {
				        if (m_AdditionalProcessors[i] == processor)
				        {
					        if (i == numAdditionalProcessors - 1)
					        {
						        Array.Resize(ref m_AdditionalProcessors, numAdditionalProcessors - 1);
					        }
					        else
					        {
						        var newAdditionalProcessors = new IInputProcessor<TValue>[numAdditionalProcessors - 1];
						        if (i > 0)
							        Array.Copy(m_AdditionalProcessors, 0, newAdditionalProcessors, 0, i);
						        Array.Copy(m_AdditionalProcessors, i + 1, newAdditionalProcessors, i, numAdditionalProcessors - i);
					        }
					        break;
				        }
			        }
		        }
	        }

	        public TValue Process(TValue value)
	        {
		        if (m_FirstProcessor != null)
			        value = m_FirstProcessor.Process(value);
		        if (m_AdditionalProcessors != null)
                    for (var i = 0; i < m_AdditionalProcessors.Length; ++i)
                        value = m_AdditionalProcessors[i].Process(value);
	            return value;
	        }
	    }
	    
        internal string m_Name;
        internal string m_Path;
	    internal InputControl m_Parent;
	    
		internal InputUsage[] m_Usages;
	    private ReadOnlyCollection<InputUsage> m_UsagesReadOnly;
	    
		internal InputControl[] m_Children;
		private ReadOnlyCollection<InputControl> m_ChildrenReadOnly;

		internal void AddChild(InputControl control)
		{
			if (m_Children == null)
			{
				m_Children = new InputControl[1];
				m_Children[0] = control;
			}
			else
			{
				var numChildren = m_Usages.Length;
				Array.Resize(ref m_Children, numChildren + 1);
				m_Children[numChildren] = control;
			}
		}

		internal void RemoveChild(InputControl control)
		{
			if (m_Children == null)
				return;
			
			throw new NotImplementedException();
		}
	    
		// Add a new usage. Not publicly exposed as we want this to be done in a controlled
		// manner through InputControlSetup. Otherwise we can't guarantee that usages are unique.
		// The mantra is: every control setup change *has* to be done through InputControlSetup.
		internal void AddUsage(InputUsage usage)
		{
			if (m_Usages == null)
			{
				m_Usages = new InputUsage[1];
				m_Usages[0] = usage;
			}
			else
			{
				var numUsages = m_Usages.Length;
				Array.Resize(ref m_Usages, numUsages + 1);
				m_Usages[numUsages] = usage;
			}
			m_UsagesReadOnly = null;
		}

	    internal void RemoveUsage(InputUsage usage)
	    {
		    throw new NotImplementedException();
	    }

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
	        m_ProcessorStack.AddProcessor(processor);
	    }

	    public void RemoveProcessor(IInputProcessor<TValue> processor)
	    {
	        m_ProcessorStack.RemoveProcessor(processor);
	    }

        protected TValue Process(TValue value)
        {
            return m_ProcessorStack.Process(value);
        }

        protected InputControl(string name)
            : base(name)
        {
        }
		
        private ProcessorStack<TValue> m_ProcessorStack;
	}
}