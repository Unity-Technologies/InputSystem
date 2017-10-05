using System;
using System.Collections.Generic;

////REVIEW: it probably makes sense to have an initial phase where we process the initial set of
////        device discoveries from native and keep the template cache around instead of throwing
////        it away after the creation of every single device; best approach may be to just
////        reuse the same InputControlSetup instance over and over

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
            Setup(template);
        }

        internal void Setup(string template)
        {
            ////REVIEW: how do we get usages and aliases on the root?

            // Populate.
            AddControlInternal(template, null, null);

            ////TODO: allow reusing a previously created device; this way an InputControlSetup
            ////      can be used to adjust a device's control setup without also causing it to
            ////      become an entirely new device
            ////      (probably also want to retain InputControls that are the same in that case
            ////      so that if anyone holds on to them, they still work)

            // Create device.
            m_Device = (InputDevice)Activator.CreateInstance(m_DeviceType);
            m_Device.m_Template = template;
            m_Device.m_Name = template;

            // Install the control hierarchy.
            SetUpControlHierarchy(m_Device);
            MakeChildOffsetsRelativeToRootRecursive(m_Device);
            m_Device.CallFinishSetup(this);
        }

        // Complete the setup and return the full control hierarchy setup
        // with its device root.
        public InputDevice Finish()
        {
            var device = m_Device;

            // Kill off our state.
            Reset();

            return device;
        }

        // Look up a direct or indirect child control.
        public InputControl TryGetControl(InputControl parent, string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException(nameof(path));

            if (m_Device == null)
                return null;

            if (parent == null)
                parent = m_Device;

            var childCount = parent.m_ChildrenReadOnly.Count;
            for (var i = 0; i < childCount; ++i)
            {
                var child = parent.m_ChildrenReadOnly[i];
                var match = PathHelpers.FindControl(child, path);
                if (match != null)
                    return match;
            }

            return null;
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
            return TryGetControl(m_Device, path);
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

        private bool m_Initialized;
        private Type m_DeviceType;
        private InputDevice m_Device;

        private List<InputControl> m_RootControls;
        private List<string> m_Usages;
        private List<string> m_Aliases;

        // Child lists.
        private int m_ChildRelationsCount;
        private Dictionary<InputControl, List<InputControl>> m_ControlToChildren;

        // We construct templates lazily as we go but keep them cached while we
        // set up hierarchies so that we don't re-construt the same Button template
        // 256 times for a keyboard.
        private InputTemplate.Cache m_TemplateCache;

        // Reset the setup in a way where it can be reused for another setup.
        // Should retain allocations that can be reused.
        private void Reset()
        {
            m_DeviceType = null;
            m_Device = null;
            m_ChildRelationsCount = 0;

            m_RootControls?.Clear();
            m_Aliases?.Clear();
            m_Usages?.Clear();
            m_ControlToChildren?.Clear();

            m_Initialized = false;
        }

        // Prepare for a setup.
        private void Initialize()
        {
            if (m_Initialized)
                return;

            m_DeviceType = typeof(InputDevice);

            if (m_RootControls == null)
                m_RootControls = new List<InputControl>();
            if (m_ControlToChildren == null)
                m_ControlToChildren = new Dictionary<InputControl, List<InputControl>>();
            if (m_Usages == null)
                m_Usages = new List<string>();
            if (m_Aliases == null)
                m_Aliases = new List<string>();

            m_Initialized = true;
        }

        // Sets up the child and usage arrays on the given device and goes through the control
        // hierarchy we have to assign their child and usage array slices.
        private void SetUpControlHierarchy(InputDevice device)
        {
            device.m_ChildrenForEachControl = new InputControl[m_ChildRelationsCount];
            device.m_UsagesForEachControl = m_Usages.ToArray();
            device.m_UsageToControl = new InputControl[device.m_UsagesForEachControl.Length];
            device.m_AliasesForEachControl = m_Aliases.ToArray();

            // Running indices.
            var childArrayIndex = 0;
            var usageArrayIndex = 0;
            var aliasArrayIndex = 0;

            SetUpControlHierarchyRecursive(device, device, ref childArrayIndex, ref usageArrayIndex,
                ref aliasArrayIndex);

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
            ref int usageArrayIndex, ref int aliasArrayIndex)
        {
            control.m_Device = device;

            // Get list of children.
            List<InputControl> children;
            if (object.ReferenceEquals(device, control))
                children = m_RootControls;
            else
                m_ControlToChildren.TryGetValue(control, out children);

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
            var usageCount = control.m_UsagesReadOnly.Count;
            if (usageCount > 0)
            {
                var usageArray = device.m_UsagesForEachControl;
                control.m_UsagesReadOnly = new ReadOnlyArray<string>(usageArray, usageArrayIndex, usageCount);

                // Fill in our portion of m_UsageToControl.
                for (var i = 0; i < usageCount; ++i)
                    device.m_UsageToControl[usageArrayIndex + i] = control;

                usageArrayIndex += usageCount;
            }

            // Set up aliases on control.
            var aliasCount = control.m_AliasesReadOnly.Count;
            if (aliasCount > 0)
            {
                var aliasArray = device.m_AliasesForEachControl;
                control.m_AliasesReadOnly =
                    new ReadOnlyArray<string>(aliasArray, aliasArrayIndex, aliasCount);
                aliasArrayIndex += aliasCount;
            }

            // Recurse into children.
            if (children != null)
            {
                foreach (var child in children)
                    SetUpControlHierarchyRecursive(device, child, ref childArrayIndex, ref usageArrayIndex,
                        ref aliasArrayIndex);
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

                // Add usages.
                if (controlTemplate.usages != null)
                {
                    var usageIndex = m_Usages.Count;
                    var usageCount = controlTemplate.usages.Length;
                    m_Usages.AddRange(controlTemplate.usages);
                    control.m_UsagesReadOnly = new ReadOnlyArray<string>(null, usageIndex, usageCount);
                }

                // Add aliases.
                if (controlTemplate.aliases != null)
                {
                    var aliasIndex = m_Aliases.Count;
                    var aliasCount = controlTemplate.aliases.Length;
                    m_Aliases.AddRange(controlTemplate.aliases);
                    control.m_AliasesReadOnly = new ReadOnlyArray<string>(null, aliasIndex, aliasCount);
                }

                ////TODO: process parameters and processors
            }
        }

        private InputControl AddControlRecursive(InputTemplate template, string name, InputControl parent)
        {
            // Create control.
            var controlObject = Activator.CreateInstance(template.type);
            var control = controlObject as InputControl;
            if (control == null)
            {
                throw new Exception($"Type '{template.type.Name}' referenced by template '{template.name}' is not an InputControl");
            }

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
                ////TODO: remove control from collection and rethrow
                //throw;

                throw new NotImplementedException();
            }

            return control;
        }

        private InputTemplate FindOrLoadTemplate(string name)
        {
            return m_TemplateCache.FindOrLoadTemplate(name);
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
