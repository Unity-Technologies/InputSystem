using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISX
{
    // Turns a template into a control hiearchy.
	// Ultimately produces a devices but can also be used to query the control setup described
	// by a template.
	// Can be used to create setups as well as to adjust them later.
	// InputControlSetup is the *only* way to create control hierarchies.
	// Also computes a final state layout when setup is finished.
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
		    ////TODO: allow reusing a previously created device; this way an InputControlSetup
		    ////      can be used to adjust a device's control setup without also causing it to
		    ////      become an entirely new device
		    ////      (probably also want to retain InputControls that are the same in that case
		    ////      so that if anyone holds on to them, they still work)

		    // Create device.
		    var device = (InputDevice) Activator.CreateInstance(m_DeviceType);
		    device.m_Template = m_DeviceTemplate.name;
		    device.m_Name = m_DeviceTemplate.name;
		    
		    // Install the control hierarchy.
		    SetUpControlHierarchy(device);
		    MakeChildOffsetsRelativeToRootRecursive(device);
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

	    // We construct templates lazily as we go but keep them cached while we
	    // set up hierarchies so that we don't re-construt the same Button template
	    // 256 times for a keyboard.
	    private Dictionary<string, InputTemplate> m_Templates;

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
		    m_Templates = null;
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
		    m_Templates = new Dictionary<string, InputTemplate>();

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
		    
		    // Hook up parent to device for all toplevel controls. Device doesn't
		    // exist yet when we do AddControl for all control templates so toplevel
		    // controls come out with their parent being null.
		    for (var i = 0; i < device.children.Count; ++i)
			    device.children[i].m_Parent = device;
	    }

        // Also makes sure offsets and sizes are set. When coming back up out of
        // SetUpControlHierarchyRecursive, the control will have its state size set correctly
        // but the offsets between all the children need to be arranged in proper order.
        // Note that we still end up with offsets that are always relative to the parent. To get
        // to the final absolute offsets, we need a final traversal down the hierarchy.
	    private void SetUpControlHierarchyRecursive(InputDevice device, InputControl control, ref int childArrayIndex,
		    ref int usageArrayIndex)
	    {
		    control.m_Device = device;
		    
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
		    
		    // Lay out our state.
		    ComputeStateLayout(control, children);
			    
		    // Nuke path on control as the final path is only known once we hook
		    // a device into the system. Do this last so we can still spill good
		    // diagnostics for as long as possible.
		    control.m_Path = null;
	    }

	    private InputControl AddControlInternal(string template, string name, InputControl parent)
	    {
		    var templateInstance = FindOrLoadTemplate(template);

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
		    if (controlTemplates == null)
			    return;
		    
		    foreach (var controlTemplate in controlTemplates)
		    {
			    var control = AddControlInternal(controlTemplate.template, controlTemplate.name, parent);
			    
			    // Pass remaining settings of control template on to newly created control.
			    control.m_StateBlock.byteOffset = controlTemplate.offset;
			    control.m_StateBlock.bitOffset = controlTemplate.bit;
			    
			    ////TODO: process parameters, processors, and usages
		    }
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
		    control.m_Template = template.name;
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

	    private InputTemplate FindOrLoadTemplate(string name)
	    {
		    var nameLowerCase = name.ToLower();
		    
		    // See if we have it cached.
		    InputTemplate template;
		    if (m_Templates.TryGetValue(nameLowerCase, out template))
			    return template;
		    
		    // No, so see if we have a string template for it. These
		    // always take precedence over ones from type so that we can
		    // override what's in the code using data.
		    string json;
		    if (InputTemplate.s_TemplateStrings.TryGetValue(nameLowerCase, out json))
		    {
			    template = InputTemplate.FromJson(name, json);
			    m_Templates[nameLowerCase] = template;

			    // If the template extends another template, we need to merge the
			    // base template into the final template.
			    if (!string.IsNullOrEmpty(template.extendsTemplate))
			    {
				    var superTemplate = FindOrLoadTemplate(template.extendsTemplate);
				    template.MergeTemplate(superTemplate);
			    }
			    
			    return template;
		    }
		    
		    // No, but maybe we have a type template for it.
		    Type type;
		    if (InputTemplate.s_TemplateTypes.TryGetValue(nameLowerCase, out type))
		    {
			    template = InputTemplate.FromType(name, type);
			    m_Templates[nameLowerCase] = template;
			    return template;
		    }
		    
		    // Nothing.
		    throw new Exception($"Cannot find input template called '{name}'");
	    }

	    private void ComputeStateLayout(InputControl control, List<InputControl> children)
	    {
		    // If state size is not set, it means it's computed from the size of the
		    // children so make sure we actually have children.
		    if (control.m_StateBlock.sizeInBits == 0 && children == null)
			    throw new Exception(
				    $"Control '{control.path}' with template '{control.template}' has no size set but has no children to compute size from");
		    
		    // If there's no children, our job is done.
		    if (children == null)
			    return;
		    
		    // First deal with children that want fixed offsets. All the other ones
		    // will get appended to the end.
		    var firstUnfixedByteOffset = 0u;
		    foreach (var child in children)
		    {
			    // Skip children that don't have fixed offsets.
			    if (child.m_StateBlock.byteOffset == InputStateBlock.kInvalidOffset)
				    continue;

			    if (child.m_StateBlock.byteOffset >= firstUnfixedByteOffset)
				    firstUnfixedByteOffset =
					    BitfieldHelpers.ComputeFollowingByteOffset(child.m_StateBlock.byteOffset, child.m_StateBlock.sizeInBits);
		    }
		    
		    // Now assign an offset to every control that wants an
		    // automatic offset. For bitfields, we need to delay advancing byte
		    // offsets until we've seen all bits in the fields.
		    // NOTE: Bit addressing controls using automatic offsets *must* be consecutive.
		    var runningByteOffset = firstUnfixedByteOffset;
			InputControl firstBitAddressingChild = null;
		    var bitfieldSizeInBits = 0u;
		    foreach (var child in children)
		    {
			    // Skip children with fixed offsets.
			    if (child.m_StateBlock.byteOffset != InputStateBlock.kInvalidOffset)
				    continue;
			    
                // See if it's a bit addressing control.
			    var isBitAddressingChild = (child.m_StateBlock.bitOffset != 0);
                if (isBitAddressingChild)
                {
	                // Remember start of bitfield group.
                    if (firstBitAddressingChild == null)
                        firstBitAddressingChild = child;

	                // Keep a running count of the size of the bitfield.
	                var lastBit = child.m_StateBlock.bitOffset + child.m_StateBlock.sizeInBits;
	                if (lastBit > bitfieldSizeInBits)
		                bitfieldSizeInBits = lastBit;
                }
                else
                {
	                // Terminate bitfield group (if there was one).
	                if (firstBitAddressingChild != null)
	                {
		                runningByteOffset = BitfieldHelpers.ComputeFollowingByteOffset(runningByteOffset, bitfieldSizeInBits);
	                	firstBitAddressingChild = null;
	                }
                }
			    
				child.m_StateBlock.byteOffset = runningByteOffset;

			    if (!isBitAddressingChild)
				    runningByteOffset =
					    BitfieldHelpers.ComputeFollowingByteOffset(runningByteOffset, child.m_StateBlock.sizeInBits);
		    }
		    
		    // Compute total size.
            // If we ended on a bitfield, account for its size.
            if (firstBitAddressingChild != null)
                runningByteOffset = BitfieldHelpers.ComputeFollowingByteOffset(runningByteOffset, bitfieldSizeInBits);
		    var totalSizeInBytes = runningByteOffset;
		    
		    // If our size isn't set, set it now from the total size we
		    // have accumulated.
		    if (control.m_StateBlock.sizeInBits == 0)
			    control.m_StateBlock.sizeInBits = totalSizeInBytes * 8;
	    }

	    // Walk down the hierarchy and add the offset of each control to the offsets of all its
	    // direct and indirect children.
	    private void MakeChildOffsetsRelativeToRootRecursive(InputControl control)
	    {
		    var ourOffset = control.m_StateBlock.byteOffset;
		    foreach (var child in control.children)
		    {
		    	child.m_StateBlock.byteOffset += ourOffset;
			    MakeChildOffsetsRelativeToRootRecursive(child);
		    }
	    }
    }
}