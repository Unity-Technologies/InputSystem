using System;
using System.Collections.Generic;
using System.Linq;

namespace ISX
{
    // Turns a template into a control hiearchy.
	// Ultimately produces a devices but can also be used to query the control setup described
	// by a template.
	// Can be used to create setups as well as to adjust them later.
	// InputControlSetup is the *only* way to create control hierarchies.
	// Once a setup has been established, it yields a list of controls and the setup itself is abandoned.
	// NOTE: InputControlSetups generate garbage. They are meant to be used for initialization only. Don't
	//       use them during normal gameplay.
    public class InputControlSetup
    {
	    public InputControlSetup(string template)
	    {
		    Initialize();
		    
		    // Populate.
		    AddControlInternal(template, null, null);
	    }
	    
	    // Complete the setup and return the full control hierarchy setup
	    // with its device root.
	    public InputDevice Finish()
	    {
		    // Create device.
		    var device = (InputDevice) Activator.CreateInstance(m_DeviceType);
		    device.m_Template = m_DeviceTemplate;
		    device.m_Name = m_DeviceTemplate.name;
		    
		    // Install the control hierarchy.
		    SetUpControlHierarchy(device);
		    device.CallFinishSetup(this);
		    
		    // Kill off our state.
		    Reset();

		    return device;
	    }

	    // Look up a direct or indirect child control.
	    public InputControl TryGetControl(InputControl parent, string path)
	    {
		    // If there's no slash in the path, just do a linear scan through
		    // the children rather than constructing a full path and doing
		    // a dictionary lookup.
		    if (path.IndexOf('/') == -1)
		    {
			    var pathLowerCase = path.ToLower();
			    foreach (var child in parent.children)
				    if (child.name.ToLower() == pathLowerCase)
					    return child;
			    
			    // Fall back to full lookup. Important as the code above will
			    // only work when called from within FinishSetup(). Otherwise
			    // the hierarchy won't be in place yet. However, during FinishSetup()
			    // we want things to be fast so that's what the shortcut is for.
		    }

		    var fullPath = parent.MakeChildPath(path);
		    return TryGetControl(fullPath);
	    }

	    // Look up a direct or indirect chid control expected to be of a specific type.
	    // Throws if actual type is not compatible.
	    public TControl TryGetControl<TControl>(InputControl parent, string path)
	    	where TControl : InputControl
	    {
		    var control = TryGetControl(parent, path);
		    if (control == null)
			    return null;

            var controlOfType = control as TControl;
            if (controlOfType == null)
                throw new Exception($"Expected control '{path}' to be of type '{typeof(TControl).Name}' but is of type '{control.GetType().Name}' instead!");

            return controlOfType;
	    }
	    
	    // Look up a direct or indirect child control.
	    // Throws if control does not exist.
	    public InputControl GetControl(InputControl parent, string path)
	    {
		    var control = TryGetControl(parent, path);
		    if (control == null)
			    throw new Exception($"Cannot find input control '{parent.MakeChildPath(path)}'");
            return control;
	    }

	    public TControl GetControl<TControl>(InputControl parent, string path)
	    	where TControl : InputControl
	    {
		    var control = GetControl(parent, path);

            var controlOfType = control as TControl;
            if (controlOfType == null)
                throw new Exception($"Expected control '{path}' to be of type '{typeof(TControl).Name}' but is of type '{control.GetType().Name}' instead!");

            return controlOfType;
	    }

        public InputControl GetControl(string path)
        {
	        var control = TryGetControl(path);
            if (control == null)
			    throw new Exception($"Cannot find input control '{path}'");
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
			    throw new Exception($"Cannot find input control '{path}'");
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

	    public IEnumerable<InputControl> GetChildren(string path)
	    {
		    if (m_ControlToChildren != null)
		    {
                var control = GetControl(path);
                
                List<InputControl> children;
                if (m_ControlToChildren.TryGetValue(control, out children))
                    return children;
		    }
		    
			return Enumerable.Empty<InputControl>();
	    }

	    private bool m_Initialized;
	    private InputTemplate m_DeviceTemplate;
	    private Type m_DeviceType;
	    private List<InputControl> m_RootControls;
	    private List<InputUsage> m_RootUsages;
	    
	    // Child lists.
	    private int m_ChildRelationsCount;
	    private Dictionary<InputControl, List<InputControl>> m_ControlToChildren;
	    
	    // Usage lists.
	    private int m_UsageRelationsCount;
	    private Dictionary<InputControl, List<InputUsage>> m_ControlToUsages;
	    
	    // Paths and usages have to be unique so we keep dictionaries
	    // that quickly map them to controls.
	    private Dictionary<string, InputControl> m_PathToControl;
	    private Dictionary<string, InputControl> m_UsageToControl;

	    private void Reset()
	    {
		    m_DeviceTemplate = null;
		    m_DeviceType = null;
		    m_RootControls = null;
		    m_RootUsages = null;
		    m_ControlToChildren = null;
		    m_ControlToUsages = null;
		    m_PathToControl = null;
		    m_UsageToControl = null;
		    m_ChildRelationsCount = 0;
		    m_UsageRelationsCount = 0;
		    m_Initialized = false;
	    }

	    private void Initialize()
	    {
		    if (m_Initialized)
			    return;

		    m_DeviceTemplate = null;
		    m_DeviceType = typeof(InputDevice);
		    m_RootControls = new List<InputControl>();
		    m_RootUsages = new List<InputUsage>();
		    m_ControlToChildren = new Dictionary<InputControl, List<InputControl>>();
		    m_ControlToUsages = new Dictionary<InputControl, List<InputUsage>>();
		    m_PathToControl = new Dictionary<string, InputControl>();
		    m_UsageToControl = new Dictionary<string, InputControl>();

		    m_Initialized = true;
	    }

	    // Sets up the child and usage arrays on the given device and goes through the control
	    // hierarchy we have to assign their child and usage array slices.
	    private void SetUpControlHierarchy(InputDevice device)
	    {
		    device.m_ChildrenForEachControl = new InputControl[m_ChildRelationsCount];
		    device.m_UsagesForEachControl = new InputUsage[m_UsageRelationsCount];
		    
		    // Running indices.
		    var childArrayIndex = 0;
		    var usageArrayIndex = 0;

		    SetUpControlHierarchyRecursive(device, device, ref childArrayIndex, ref usageArrayIndex);
	    }

	    private void SetUpControlHierarchyRecursive(InputDevice device, InputControl control, ref int childArrayIndex,
		    ref int usageArrayIndex)
	    {
		    // Nuke path on control as the final path is only known once we hook
		    // a device into the system.
		    control.m_Path = null;
		    
		    // Get list of children.
		    List<InputControl> children;
		    if (object.ReferenceEquals(device, control))
			    children = m_RootControls;
		    else
			    m_ControlToChildren.TryGetValue(control, out children);

		    // Get list of usages.
		    List<InputUsage> usages;
		    if (object.ReferenceEquals(device, control))
			    usages = m_RootUsages;
		    else
			    m_ControlToUsages.TryGetValue(control, out usages);
		    
		    // Set up children on control.
		    if (children != null)
		    {
			    var childArray = device.m_ChildrenForEachControl;
			    var childCount = children.Count;
			    control.m_ChildrenReadOnly =
				    new ReadOnlyArray<InputControl>(childArray, childArrayIndex, childCount);
			    children.CopyTo(childArray, childArrayIndex);
			    childArrayIndex += childCount;
		    }
		    
		    // Set up usages on control.
		    if (usages != null)
		    {
			    var usageArray = device.m_UsagesForEachControl;
			    var usageCount = usages.Count;
			    control.m_UsagesReadOnly =
				    new ReadOnlyArray<InputUsage>(usageArray, usageArrayIndex, usageCount);
			    usages.CopyTo(usageArray, usageArrayIndex);
			    usageArrayIndex += usageCount;
		    }
		    
		    // Recurse into children.
		    if (children != null)
		    {
			    foreach (var child in children)
				    SetUpControlHierarchyRecursive(device, child, ref childArrayIndex, ref usageArrayIndex);
		    }
	    }

	    private InputControl AddControlInternal(string template, string name, InputControl parent)
	    {
		    var templateInstance = InputTemplate.GetTemplate(template);

		    // Device templates are somewhat special. We do not allow multiple to be added to the same control
		    // setup, they have to added the root and they will not result in the immediate creation of an
		    // InputControl. Instead, when the setup is finished, a single InputDevice control will be created
		    // and receives all InputControls we created as children.
		    if (typeof(InputDevice).IsAssignableFrom(templateInstance.type))
		    {
			    if (parent != null)
				    throw new Exception($"Cannot instantiate device template '{template}' as child of '{parent.path}'; devices must be added at root");

			    m_DeviceTemplate = templateInstance;
				m_DeviceType = templateInstance.type;

			    AddChildControls(templateInstance, null);
			    return null;
		    }
		    
		    return AddControlRecursive(templateInstance, name, parent);
	    }

	    private void AddChildControls(InputTemplate template, InputControl parent)
	    {
		    var controlTemplates = template.m_Controls;
		    foreach (var controlTemplate in controlTemplates)
			    AddControlInternal(controlTemplate.template, controlTemplate.name, parent);
	    }

	    private InputControl AddControlRecursive(InputTemplate template, string name, InputControl parent)
	    {
		    // Create control.
		    var control = (InputControl) Activator.CreateInstance(template.type);
		    if (name == null)
		    {
			    if (control is InputDevice)
				    name = "";
				else
			    	name = template.name;
		    }
		    
		    control.m_Name = name;
		    control.m_Template = template;
		    control.m_Parent = parent;
		    
		    // Assign path. The full path will change again later when the control is actually jacked into
		    // the system but we will need the path repeatedly during setup so we might just as well cache
		    // the path for now.
		    var path = name;
		    if (parent != null && parent.path != "")
			    path = $"{parent.path}/{name}";
		    
		    control.m_Path = path;
		    
		    // Make sure the resulting path is unique.
		    if (TryGetControl(path) != null)
			    throw new InvalidOperationException($"A control with path '{path}' is already present in the setup.");
		    
		    m_PathToControl[path.ToLower()] = control;
		    
		    // Insert into hierarchy.
		    if (parent == null)
			    m_RootControls.Add(control);
		    else
		    {
			    List<InputControl> children;
			    if (!m_ControlToChildren.TryGetValue(parent, out children))
			    {
				    children = new List<InputControl>();
				    m_ControlToChildren[parent] = children;
			    }
			    children.Add(control);
		    }
			++m_ChildRelationsCount;

		    // Create children and configure their settings from our
		    // template values.
		    try
		    {
			    AddChildControls(template, control);
		    }
		    catch
		    {
			    ////TODO: remove control from collection
			    throw;
		    }
		    
		    return control;
	    }
    }
}