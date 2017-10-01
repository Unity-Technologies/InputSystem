using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace InputSystem
{
    // Captures a specific setup of input controls.
	// Can be used to create setups as well as to adjust them later.
	// InputControlSetup is the *only* way to create control or alter setups in the system.
	// Once a setup has been established, it yields a list of controls and the setup itself is abandoned.
	// NOTE: InputControlSetups generate garbage. They are meant to be used for initialization only. Don't
	//       use them during normal gameplay.
    public class InputControlSetup
    {
        // Flat list of controls including all component controls.
	    private List<InputControl> m_Controls = new List<InputControl>();
	    
	    // Paths and usages have to be unique so we keep dictionaries
	    // that quickly map them to controls.
	    private Dictionary<string, InputControl> m_PathToControl = new Dictionary<string, InputControl>();
	    private Dictionary<string, InputControl> m_UsageToControl = new Dictionary<string, InputControl>();
	    
	    ////TODO: need to prevent someone doing "mySetup.AddControl(new InputControl(foo, 0, myControlAreadyInSetup));"

	    // Add a control
	    public void AddControl(string template, string parent = null, bool ignoreConflictingUsages = false)
	    {
	    }
	    
	    ////TODO: do not expose this
	    // Add a new control from an already constructed control.
	    public void AddControl(InputControl control, string parent = null, bool ignoreDuplicates = false)
	    {
		    var fullPath = control.path;
		    
		    // If we've been given a parent, find it. Bomb if it's not part
		    // of our setup.
		    InputControl parentControl = null;
		    if (parent != null)
		    {
			    parentControl = GetControl(parent);
			    fullPath = $"{parentControl.path}/{control.path}";
		    }
		    
		    // Only allow the parent of the control to be set if it corresponds to the parent
		    // we've been given.
		    if (control.parent != null)
		    {
			    var currentControlParent = TryGetControl(control.parent.path);
			    if (currentControlParent == null || currentControlParent != control.parent ||
			        currentControlParent.path.ToLower() != parent.ToLower())
				    throw new InvalidOperationException("Cannot add a subcontrol to the toplevel of an InputControlSetup");
		    }

		    // Make sure the name is unique.
		    if (TryGetControl(fullPath) != null)
			    throw new InvalidOperationException($"A control with path '{fullPath}' is already present in the setup.");
		    
		    // Make sure the usage is unique.
		    var usages = control.m_Usages;// Don't use .usages to not create objects unnecessarily.
		    if (usages != null)
		    {
			    for (var i = 0; i < usages.Length; ++i)
			    {
				    var usageName = usages[i].name;
				    if (TryGetControl(usageName) != null)
					    throw new InvalidOperationException($"A control with usage '{usageName}' is already present in the setup.");
			    }
		    }
		    
		    // Add to parent, if present.
		    if (parentControl != null && control.parent == null)
			    parentControl.AddChild(control);

		    // Allocate data structures, if necessary.
			if (m_Controls == null)
				m_Controls = new List<InputControl>();
			if (m_PathToControl == null)
				m_PathToControl = new Dictionary<string, InputControl>();
		    if (m_UsageToControl == null)
			    m_UsageToControl = new Dictionary<string, InputControl>();

		    m_Controls.Add(control);
		    m_PathToControl[fullPath.ToLower()] = control;
		    
		    if (usages != null)
			    for (var i = 0; i < usages.Length; ++i)
				    m_UsageToControl[usages[i].name.ToLower()] = control;

		    ////TODO: Optimize to not require the child control to do needless parent checks
            // Recursively add children.
		    var children = control.m_Children;
		    if (children != null)
		    {
			    for (var i = 0; i < children.Length; ++i)
				    AddControl(children[i], parentControl.path);
		    }
	    }

	    public void AddUsage(string path, string usage)
	    {
		    var control = GetControl(path);
		    var usageData = InputUsage.GetUsage(usage);
		    control.AddUsage(usageData);
	    }

	    public void RemoveUsage(string path, string usage)
	    {
		    var control = GetControl(path);
		    var usageData = InputUsage.GetUsage(usage);
		    control.RemoveUsage(usageData);
	    }

	    public void RenameControl(string from, string to)
	    {
	    }

	    // Remove the control at the given path including all its children.
	    public void RemoveControl(string path)
	    {
	    }

	    public void SwapUsages(string firstPath, string secondPath)
	    {
	    }

	    public void SwapValues(string fromPath, string toPath)
	    {
		    //this may only be possible after layouting
	    }

	    public InputControl GetControl(InputControl parent, string path)
	    {
		    //if there's no slash in path, just look in the parent's list of children
		    throw new NotImplementedException();
	    }

	    public TControl GetControl<TControl>(InputControl parent, string path)
	    	where TControl : InputControl
	    {
		    throw new NotImplementedException();
	    }

        public InputControl GetControl(string path)
        {
	        var control = TryGetControl(path);
            if (control == null)
                throw new Exception("Invalid control setup: missing controls for '" + path + "'");
            return control;
        }

        public InputControl TryGetControl(string path)
        {
	        if (m_PathToControl == null)
		        return null;

	        var nameLowerCase = path.ToLower();

	        InputControl control;
	        m_PathToControl.TryGetValue(nameLowerCase, out control);
	        return control;
        }

        public TControl GetControl<TControl>(string path)
            where TControl : InputControl
        {
            var control = TryGetControl<TControl>(path);
            if (control == null)
                throw new Exception("Invalid control setup: missing controls for '" + path + "'");
	        return control;
        }

	    public TControl TryGetControl<TControl>(string path)
            where TControl : InputControl
	    {
            var control = TryGetControl(path);
		    if (control == null)
			    return null;

            var controlOfType = control as TControl;
            if (controlOfType == null)
                throw new Exception($"Expected control '{path}' to be of type '{typeof(TControl).Name}' but is of type '{control.GetType().Name}' instead!");

            return controlOfType;
	    }
    }
}