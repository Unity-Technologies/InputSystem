using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISX.LowLevel;
using ISX.Utilities;

////REVIEW: it probably makes sense to have an initial phase where we process the initial set of
////        device discoveries from native and keep the template cache around instead of throwing
////        it away after the creation of every single device; best approach may be to just
////        reuse the same InputControlSetup instance over and over

////TODO: ensure that things are aligned properly for ARM; should that be done on the reading side or in the state layouts?
////       (make sure that alignment works the same on *all* platforms; otherwise editor will not be able to process events from players properly)

namespace ISX
{
    /// <summary>
    /// Turns a template into a control hierarchy.
    /// </summary>
    /// <remarks>
    /// Ultimately produces a device but can also be used to query the control setup described
    /// by a template.
    ///
    /// Can be used to create setups as well as to adjust them later.
    ///
    /// InputControlSetup is the only way to create control hierarchies.
    ///
    /// Also computes a final state layout when setup is finished.
    ///
    /// Once a setup has been established, it yields an independent control hierarchy and the setup itself
    /// is abandoned.
    ///
    /// Note InputControlSetups generate garbage. They are meant to be used for initialization only. Don't
    /// use them during normal gameplay.
    ///
    /// Running an *existing* device through another control setup is a *destructive* operation.
    /// Existing controls may be reused while at the same time the hierarchy and even the device instance
    /// itself may change.
    /// </remarks>
    public class InputControlSetup
    {
        // We use this constructor when we create devices in batches.
        internal InputControlSetup(InputTemplate.Collection templates)
        {
            m_TemplateCache.templates = templates;
        }

        public InputControlSetup(string template, InputDevice existingDevice = null, string variant = null)
        {
            m_TemplateCache.templates = InputTemplate.s_Templates;
            Setup(new InternedString(template), existingDevice, new InternedString(variant));
        }

        internal void Setup(InternedString template, InputDevice existingDevice, InternedString variant)
        {
            if (existingDevice != null && existingDevice.m_DeviceIndex != InputDevice.kInvalidDeviceIndex)
                throw new InvalidOperationException(
                    string.Format("Cannot modify control setup of existing device {0} while added to system.",
                        existingDevice));

            if (variant.IsEmpty())
                variant = new InternedString("Default");

            InstantiateTemplate(template, variant, new InternedString(), null, existingDevice);
            FinalizeControlHierarchy();
            m_Device.CallFinishSetupRecursive(this);
        }

        ////TODO: do away with this
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
                throw new ArgumentException("path");

            if (m_Device == null)
                return null;

            if (parent == null)
                parent = m_Device;

            var match = InputControlPath.TryFindChild(parent, path);
            if (match != null)
                return match;

            if (ReferenceEquals(parent, m_Device))
                return InputControlPath.TryFindControl(m_Device, string.Format("{0}/{1}", m_Device.name, path));

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
                throw new Exception(string.Format(
                        "Expected control '{0}' to be of type '{1}' but is of type '{2}' instead!", path,
                        typeof(TControl).Name, control.GetType().Name));

            return controlOfType;
        }

        // Look up a direct or indirect child control.
        // Throws if control does not exist.
        public InputControl GetControl(InputControl parent, string path)
        {
            var control = TryGetControl(parent, path);
            if (control == null)
                throw new Exception(string.Format("Cannot find input control '{0}'", parent.MakeChildPath(path)));
            return control;
        }

        public TControl GetControl<TControl>(InputControl parent, string path)
            where TControl : InputControl
        {
            var control = GetControl(parent, path);

            var controlOfType = control as TControl;
            if (controlOfType == null)
                throw new Exception(string.Format(
                        "Expected control '{0}' to be of type '{1}' but is of type '{2}' instead!", path,
                        typeof(TControl).Name, control.GetType().Name));

            return controlOfType;
        }

        public InputControl GetControl(string path)
        {
            var control = TryGetControl(path);
            if (control == null)
                throw new Exception(string.Format("Cannot find input control '{0}'", path));
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
                throw new Exception(string.Format("Cannot find input control '{0}'", path));
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
                throw new Exception(string.Format(
                        "Expected control '{0}' to be of type '{1}' but is of type '{2}' instead!", path,
                        typeof(TControl).Name, control.GetType().Name));

            return controlOfType;
        }

        private InputDevice m_Device;

        // We construct templates lazily as we go but keep them cached while we
        // set up hierarchies so that we don't re-construt the same Button template
        // 256 times for a keyboard.
        private InputTemplate.Cache m_TemplateCache;

        // Table mapping (lower-cased) control paths to control templates that contain
        // overrides for the control at the given path.
        private Dictionary<string, InputTemplate.ControlTemplate> m_ChildControlOverrides;

        // Reset the setup in a way where it can be reused for another setup.
        // Should retain allocations that can be reused.
        private void Reset()
        {
            m_Device = null;
            m_ChildControlOverrides = null;
            // Leave the cache in place so we can reuse them in another setup path.
        }

        private InputControl InstantiateTemplate(InternedString template, InternedString variant, InternedString name, InputControl parent, InputControl existingControl)
        {
            // Look up template by name.
            var templateInstance = FindOrLoadTemplate(template);

            // Create control hierarchy.
            return InstantiateTemplate(templateInstance, variant, name, parent, existingControl);
        }

        private InputControl InstantiateTemplate(InputTemplate template, InternedString variant, InternedString name, InputControl parent, InputControl existingControl)
        {
            InputControl control;

            // If we have an existing control, see whether it's usable.
            if (existingControl != null && existingControl.template == template.name && existingControl.GetType() == template.type)
            {
                control = existingControl;
            }
            else
            {
                Debug.Assert(template.type != null);

                // No, so create a new control.
                var controlObject = Activator.CreateInstance(template.type);
                control = controlObject as InputControl;
                if (control == null)
                {
                    throw new Exception(string.Format("Type '{0}' referenced by template '{1}' is not an InputControl",
                            template.type.Name, template.name));
                }
            }

            // If it's a device, perform some extra work specific to the control
            // hierarchy root.
            var controlAsDevice = control as InputDevice;
            if (controlAsDevice != null)
            {
                if (parent != null)
                    throw new Exception(string.Format(
                            "Cannot instantiate device template '{0}' as child of '{1}'; devices must be added at root",
                            template.name, parent.path));

                m_Device = controlAsDevice;
                m_Device.m_StateBlock.byteOffset = 0;
                m_Device.m_StateBlock.format = template.stateFormat;

                // If we have an existing device, we'll start the various control arrays
                // from scratch. Note that all the controls still refer to the existing
                // arrays and so we can iterate children, for example, just fine while
                // we are rebuilding the control hierarchy.
                m_Device.m_AliasesForEachControl = null;
                m_Device.m_ChildrenForEachControl = null;
                m_Device.m_UsagesForEachControl = null;
                m_Device.m_UsageToControl = null;

                // But we preserve IDs and descriptions of existing devices.
                if (existingControl != null)
                {
                    var existingDevice = (InputDevice)existingControl;
                    m_Device.m_Id = existingDevice.m_Id;
                    m_Device.m_Description = existingDevice.m_Description;
                }

                if (template.m_UpdateBeforeRender == true)
                    m_Device.m_Flags |= InputDevice.Flags.UpdateBeforeRender;

                // Devices get their names from the topmost base templates.
                if (name.IsEmpty())
                {
                    name = InputTemplate.s_Templates.GetRootTemplateName(template.name);

                    // If there's a namespace in the template name, snip it out.
                    var indexOfLastColon = name.ToString().LastIndexOf(':');
                    if (indexOfLastColon != -1)
                        name = new InternedString(name.ToString().Substring(indexOfLastColon + 1));
                }
            }
            else if (parent == null)
            {
                // Someone did "new InputControlSetup(...)" with a control template.
                // We don't support creating control hierarchies without a device at the root.
                throw new InvalidOperationException(
                    string.Format(
                        "Toplevel template used with InputControlSetup must be a device template; '{0}' is a control template",
                        template.name));
            }

            // Set common properties.
            if (name.IsEmpty())
                name = template.name;

            control.m_Name = name;
            control.m_DisplayNameFromTemplate = template.m_DisplayName;
            control.m_Template = template.name;
            control.m_Variant = variant;
            control.m_Parent = parent;
            control.m_Device = m_Device;

            // Create children and configure their settings from our
            // template values.
            var haveChildrenUsingStateFromOtherControl = false;
            try
            {
                // Pass list of existing control on to function as we may have decided to not
                // actually reuse the existing control (and thus control.m_ChildrenReadOnly will
                // now be blank) but still want crawling down the hierarchy to preserve existing
                // controls where possible.
                AddChildControls(template, variant, control,
                    existingControl != null ? existingControl.m_ChildrenReadOnly : (ReadOnlyArray<InputControl>?)null,
                    ref haveChildrenUsingStateFromOtherControl);
            }
            catch
            {
                ////TODO: remove control from collection and rethrow
                throw;
            }

            // Come up with a layout for our state.
            ComputeStateLayout(control);

            // Finally, if we have child controls that take their state blocks from other
            // controls, assign them their blocks now.
            if (haveChildrenUsingStateFromOtherControl)
            {
                foreach (var controlTemplate in template.controls)
                {
                    if (string.IsNullOrEmpty(controlTemplate.useStateFrom))
                        continue;

                    var child = TryGetControl(control, controlTemplate.name);
                    Debug.Assert(child != null);

                    // Find the referenced control.
                    var referencedControl = TryGetControl(control, controlTemplate.useStateFrom);
                    if (referencedControl == null)
                        throw new Exception(
                            string.Format(
                                "Cannot find control '{0}' referenced in 'useStateFrom' of control '{1}' in template '{2}'",
                                controlTemplate.useStateFrom, controlTemplate.name, template.name));

                    // Copy its state settings.
                    child.m_StateBlock = referencedControl.m_StateBlock;

                    // At this point, all byteOffsets are relative to parents so we need to
                    // walk up the referenced control's parent chain and add offsets until
                    // we are at the same level that we are at.
                    for (var parentInChain = referencedControl.parent; parentInChain != control; parentInChain = parentInChain.parent)
                        child.m_StateBlock.byteOffset = parentInChain.m_StateBlock.byteOffset;
                }
            }

            return control;
        }

        private const uint kSizeForControlUsingStateFromOtherControl = InputStateBlock.kInvalidOffset;

        private void AddChildControls(InputTemplate template, InternedString variant, InputControl parent, ReadOnlyArray<InputControl>? existingChildren, ref bool haveChildrenUsingStateFromOtherControls)
        {
            var controlTemplates = template.m_Controls;
            if (controlTemplates == null)
                return;

            // Find out how many direct children we will add.
            var childCount = 0;
            var haveControlTemplateWithPath = false;
            for (var i = 0; i < controlTemplates.Length; ++i)
            {
                // Not a new child if it's a template reaching in to the hierarchy to modify
                // an existing child.
                if (controlTemplates[i].isModifyingChildControlByPath)
                {
                    haveControlTemplateWithPath = true;
                    InsertChildControlOverrides(parent, ref controlTemplates[i]);
                    continue;
                }

                // Skip if variant doesn't match.
                if (!controlTemplates[i].variant.IsEmpty() &&
                    controlTemplates[i].variant != variant)
                    continue;

                ++childCount;
            }

            // Add room for us in the device's child array.
            var firstChildIndex = ArrayHelpers.GrowBy(ref m_Device.m_ChildrenForEachControl, childCount);

            // Add controls from all control templates except the ones that have
            // paths in them.
            var childIndex = firstChildIndex;
            for (var i = 0; i < controlTemplates.Length; ++i)
            {
                var controlTemplate = controlTemplates[i];

                // Skip control templates that don't add controls but rather modify child
                // controls of other controls added by the template. We do a second pass
                // to apply their settings.
                if (controlTemplate.isModifyingChildControlByPath)
                    continue;

                // If the control is part of a variant, skip it if it isn't the variant we're
                // looking for.
                if (!controlTemplate.variant.IsEmpty() && controlTemplate.variant != variant)
                    continue;

                AddChildControl(template, variant, parent, existingChildren, ref haveChildrenUsingStateFromOtherControls,
                    ref controlTemplate, ref childIndex);
            }

            // Install child array on parent. We will later patch up the array
            // reference again as we finalize the hierarchy. However, the reference
            // will point to a valid child array all the same even while we are
            // constructing the hierarchy.
            //
            // NOTE: It's important to do this *after* the loop above where we call InstantiateTemplate for each child
            //       as each child may end up moving the m_ChildrenForEachControl array around.
            parent.m_ChildrenReadOnly = new ReadOnlyArray<InputControl>(m_Device.m_ChildrenForEachControl, firstChildIndex, childCount);

            ////TODO: replace the entire post-creation modification logic here with using m_ChildControlOverrides
            ////      (note that we have to *merge* into the table; if there's already overrides, only replace properties that haven't been set)
            ////      (however, this will also require moving the child insertion logic somewhere else)

            // Apply control modifications from control templates with paths.
            if (haveControlTemplateWithPath)
            {
                for (var i = 0; i < controlTemplates.Length; ++i)
                {
                    var controlTemplate = controlTemplates[i];
                    if (!controlTemplate.isModifyingChildControlByPath)
                        continue;

                    // If the control is part of a variant, skip it if it isn't the variant we're
                    // looking for.
                    if (!controlTemplate.variant.IsEmpty() && controlTemplate.variant != variant)
                        continue;

                    ModifyChildControl(template, variant, parent, ref haveChildrenUsingStateFromOtherControls,
                        ref controlTemplate);
                }
            }
        }

        private InputControl AddChildControl(InputTemplate template, InternedString variant, InputControl parent,
            ReadOnlyArray<InputControl>? existingChildren, ref bool haveChildrenUsingStateFromOtherControls,
            ref InputTemplate.ControlTemplate controlTemplate, ref int childIndex, string nameOverride = null)
        {
            var name = nameOverride ?? controlTemplate.name;
            var nameLowerCase = name.ToLower();
            var nameInterned = new InternedString(name);
            string path = null;

            ////REVIEW: can we check this in InputTemplate instead?
            if (string.IsNullOrEmpty(controlTemplate.template))
                throw new Exception(string.Format("Template has not been set on control '{0}' in '{1}'",
                        controlTemplate.name, template.name));

            // See if there is an override for the control.
            InputTemplate.ControlTemplate? controlOverride = null;
            if (m_ChildControlOverrides != null)
            {
                path = string.Format("{0}/{1}", parent.path, name);
                var pathLowerCase = path.ToLower();

                InputTemplate.ControlTemplate match;
                if (m_ChildControlOverrides.TryGetValue(pathLowerCase, out match))
                    controlOverride = match;
            }

            // Get name of template to use for control.
            var templateName = controlTemplate.template;
            if (controlOverride != null && !controlOverride.Value.template.IsEmpty())
                templateName = controlOverride.Value.template;

            // See if we have an existing control that we might be able to re-use.
            InputControl existingControl = null;
            if (existingChildren != null)
            {
                var existingChildCount = existingChildren.Value.Count;
                for (var n = 0; n < existingChildCount; ++n)
                {
                    var existingChild = existingChildren.Value[n];
                    if (existingChild.template == templateName
                        && existingChild.name.ToLower() == nameLowerCase)
                    {
                        existingControl = existingChild;
                        break;
                    }
                }
            }

            // Create control.
            InputControl control;
            try
            {
                control = InstantiateTemplate(templateName, variant, nameInterned, parent, existingControl);
            }
            catch (InputTemplate.TemplateNotFoundException exception)
            {
                // Throw better exception that gives more info.
                throw new Exception(
                    string.Format("Cannot find template '{0}' used in control '{1}' of template '{2}'",
                        exception.template, templateName, template.name),
                    exception);
            }

            // Add to array.
            m_Device.m_ChildrenForEachControl[childIndex] = control;
            ++childIndex;

            // Set display name.
            control.m_DisplayNameFromTemplate = controlTemplate.displayName;

            // Pass state block config on to control.
            var usesStateFromOtherControl = !string.IsNullOrEmpty(controlTemplate.useStateFrom);
            if (!usesStateFromOtherControl)
            {
                control.m_StateBlock.byteOffset = controlTemplate.offset;
                if (controlTemplate.bit != InputStateBlock.kInvalidOffset)
                    control.m_StateBlock.bitOffset = controlTemplate.bit;
                if (controlTemplate.sizeInBits != 0)
                    control.m_StateBlock.sizeInBits = controlTemplate.sizeInBits;
                if (controlTemplate.format != 0)
                    SetFormat(control, controlTemplate);
            }
            else
            {
                // Mark controls that don't have state blocks of their own but rather get their
                // blocks from other controls by setting their state size to kInvalidOffset.
                control.m_StateBlock.sizeInBits = kSizeForControlUsingStateFromOtherControl;
                haveChildrenUsingStateFromOtherControls = true;
            }

            ////REVIEW: the constant appending to m_UsagesForEachControl and m_AliasesForEachControl may lead to a lot
            ////        of successive re-allocations

            // Add usages.
            if (controlTemplate.usages.Count > 0)
            {
                var usageCount = controlTemplate.usages.Count;
                var usageIndex =
                    ArrayHelpers.AppendToImmutable(ref m_Device.m_UsagesForEachControl, controlTemplate.usages.m_Array);
                control.m_UsagesReadOnly =
                    new ReadOnlyArray<InternedString>(m_Device.m_UsagesForEachControl, usageIndex, usageCount);

                ArrayHelpers.GrowBy(ref m_Device.m_UsageToControl, usageCount);
                for (var n = 0; n < usageCount; ++n)
                    m_Device.m_UsageToControl[usageIndex + n] = control;
            }

            // Add aliases.
            if (controlTemplate.aliases.Count > 0)
            {
                var aliasCount = controlTemplate.aliases.Count;
                var aliasIndex =
                    ArrayHelpers.AppendToImmutable(ref m_Device.m_AliasesForEachControl, controlTemplate.aliases.m_Array);
                control.m_AliasesReadOnly =
                    new ReadOnlyArray<InternedString>(m_Device.m_AliasesForEachControl, aliasIndex, aliasCount);
            }

            // Set parameters.
            if (controlTemplate.parameters.Count > 0)
                SetParameters(control, controlTemplate.parameters);

            // Add processors.
            if (controlTemplate.processors.Count > 0)
                AddProcessors(control, ref controlTemplate, template.name);

            return control;
        }

        private void InsertChildControlOverrides(InputControl parent, ref InputTemplate.ControlTemplate controlTemplate)
        {
            if (m_ChildControlOverrides == null)
                m_ChildControlOverrides = new Dictionary<string, InputTemplate.ControlTemplate>();

            var path = InputControlPath.Combine(parent, controlTemplate.name);
            var pathLowerCase = path.ToLower();

            // See if there are existing overrides for the control.
            InputTemplate.ControlTemplate existingOverrides;
            if (!m_ChildControlOverrides.TryGetValue(pathLowerCase, out existingOverrides))
            {
                // So, so just insert our overrides and we're done.
                m_ChildControlOverrides[pathLowerCase] = controlTemplate;
                return;
            }

            // Yes, there's existing overrides so we have to merge.
            existingOverrides = existingOverrides.Merge(controlTemplate);
            m_ChildControlOverrides[pathLowerCase] = existingOverrides;
        }

        private void ModifyChildControl(InputTemplate template, InternedString variant, InputControl parent,
            ref bool haveChildrenUsingStateFromOtherControls,
            ref InputTemplate.ControlTemplate controlTemplate)
        {
            // Controls layout themselves as we come back up the hierarchy. However, when we
            // apply layout modifications reaching *into* the hierarchy, we need to retrigger
            // layouting on their parents.
            var haveChangedLayoutOfParent = false;

            // Find the child control.
            var child = TryGetControl(parent, controlTemplate.name);
            if (child == null)
            {
                // We're adding a child somewhere in the existing hierarchy. This is a tricky
                // case as we have to potentially shift indices around in the hierarchy to make
                // room for the new control.

                ////TODO: this path does not support recovering existing controls? does it matter?

                child = InsertChildControl(template, variant, parent,
                        ref haveChildrenUsingStateFromOtherControls, ref controlTemplate);
                haveChangedLayoutOfParent = true;
            }
            else
            {
                // Apply modifications.
                if (controlTemplate.sizeInBits != 0 &&
                    child.m_StateBlock.sizeInBits != controlTemplate.sizeInBits)
                {
                    child.m_StateBlock.sizeInBits = controlTemplate.sizeInBits;
                }
                if (controlTemplate.format != 0 && child.m_StateBlock.format != controlTemplate.format)
                {
                    SetFormat(child, controlTemplate);
                    haveChangedLayoutOfParent = true;
                }
                ////REVIEW: ATM, when you move a child with a fixed offset, we only move the child
                ////        and don't move the parent or siblings. What this means is that if you move
                ////        leftStick/x, for example, leftStick stays put. ATM you have to move *all*
                ////        controls that are part of a chain manually. Not sure what the best behavior
                ////        is. If we opt to move parents along with children, we have to make sure we
                ////        are not colliding with any other relocations of children (e.g. if you move
                ////        both leftStick/x and leftStick/y, leftStick itself should move only once and
                ////        not at all if there indeed is a leftStick control template with an offset;
                ////        so, it'd get quite complicated)
                if (controlTemplate.offset != InputStateBlock.kInvalidOffset)
                    child.m_StateBlock.byteOffset = controlTemplate.offset;
                if (controlTemplate.bit != InputStateBlock.kInvalidOffset)
                    child.m_StateBlock.bitOffset = controlTemplate.bit;
                if (controlTemplate.processors.Count > 0)
                    AddProcessors(child, ref controlTemplate, template.name);
                if (controlTemplate.parameters.Count > 0)
                    SetParameters(child, controlTemplate.parameters);
                if (!string.IsNullOrEmpty(controlTemplate.displayName))
                    child.m_DisplayNameFromTemplate = controlTemplate.displayName;

                ////TODO: other modifications
            }

            // Apply layout change.
            ////REVIEW: not sure what's better here; trigger this immediately means we may trigger
            ////        it a number of times on the same parent but doing it as a final pass would
            ////        require either collecting the necessary parents or doing another pass through
            ////        the list of control templates
            if (haveChangedLayoutOfParent && !ReferenceEquals(child.parent, parent))
                ComputeStateLayout(child.parent);
        }

        private InputControl InsertChildControl(InputTemplate template, InternedString variant, InputControl parent,
            ref bool haveChildrenUsingStateFromOtherControls,
            ref InputTemplate.ControlTemplate controlTemplate)
        {
            var path = controlTemplate.name.ToString();

            // First we need to find the immediate parent from the given path.
            var indexOfSlash = path.LastIndexOf('/');
            if (indexOfSlash == -1)
                throw new ArgumentException("InsertChildControl has to be called with a slash-separated path", "path");
            Debug.Assert(indexOfSlash != 0);
            var immediateParentPath = path.Substring(0, indexOfSlash);
            var immediateParent = InputControlPath.TryFindChild(parent, immediateParentPath);
            if (immediateParent == null)
                throw new Exception(
                    string.Format("Cannot find parent '{0}' of control '{1}' in template '{2}'", immediateParentPath,
                        controlTemplate.name, template.name));

            var controlName = path.Substring(indexOfSlash + 1);
            if (controlName.Length == 0)
                throw new Exception(
                    string.Format("Path cannot end in '/' (control '{0}' in template '{1}')", controlTemplate.name,
                        template.name));

            // Make room in the device's child array.
            var childStartIndex = immediateParent.m_ChildrenReadOnly.m_StartIndex;
            var childIndex = childStartIndex + immediateParent.m_ChildrenReadOnly.m_Length;
            ArrayHelpers.InsertAt(ref m_Device.m_ChildrenForEachControl, childIndex, null);
            ++immediateParent.m_ChildrenReadOnly.m_Length;

            // Insert the child.
            var control = AddChildControl(template, variant, immediateParent, null,
                    ref haveChildrenUsingStateFromOtherControls, ref controlTemplate, ref childIndex, controlName);

            // Adjust indices of control's that have been shifted around by our insertion.
            ShiftChildIndicesInHierarchyOneUp(parent, childIndex);

            return control;
        }

        private void ShiftChildIndicesInHierarchyOneUp(InputControl root, int startIndex)
        {
            if (root.m_ChildrenReadOnly.m_StartIndex >= startIndex)
                ++root.m_ChildrenReadOnly.m_StartIndex;
            root.m_ChildrenReadOnly.m_Array = m_Device.m_ChildrenForEachControl;

            foreach (var child in root.children)
                ShiftChildIndicesInHierarchyOneUp(child, startIndex);
        }

        private static void AddProcessors(InputControl control, ref InputTemplate.ControlTemplate controlTemplate, string templateName)
        {
            var processorCount = controlTemplate.processors.Count;
            for (var n = 0; n < processorCount; ++n)
            {
                var name = controlTemplate.processors[n].name;
                var type = InputProcessor.TryGet(name);
                if (type == null)
                    throw new Exception(
                        string.Format("Cannot find processor '{0}' referenced by control '{1}' in template '{2}'", name,
                            controlTemplate.name, templateName));

                var processor = Activator.CreateInstance(type);

                var parameters = controlTemplate.processors[n].parameters;
                if (parameters.Count > 0)
                    SetParameters(processor, parameters);

                control.AddProcessor(processor);
            }
        }

        internal static void SetParameters(object onObject, ReadOnlyArray<InputTemplate.ParameterValue> parameters)
        {
            var objectType = onObject.GetType();
            for (var i = 0; i < parameters.Count; ++i)
            {
                var parameter = parameters[i];

                var field = objectType.GetField(parameter.name);
                if (field == null)
                    throw new Exception(string.Format("Cannot find public field {0} in {1} (referenced by parameter)",
                            parameter.name, objectType.Name));

                ////REVIEW: can we do this without boxing?

                object value = null;
                unsafe
                {
                    switch (parameter.type)
                    {
                        case InputTemplate.ParameterType.Boolean:
                            value = *((bool*)parameter.value);
                            break;
                        case InputTemplate.ParameterType.Integer:
                            value = *((int*)parameter.value);
                            break;
                        case InputTemplate.ParameterType.Float:
                            value = *((float*)parameter.value);
                            break;
                    }
                }

                field.SetValue(onObject, value);
            }
        }

        private static void SetFormat(InputControl control, InputTemplate.ControlTemplate controlTemplate)
        {
            control.m_StateBlock.format = controlTemplate.format;
            if (controlTemplate.sizeInBits == 0)
            {
                var primitiveFormatSize = InputStateBlock.GetSizeOfPrimitiveFormatInBits(controlTemplate.format);
                if (primitiveFormatSize != -1)
                    control.m_StateBlock.sizeInBits = (uint)primitiveFormatSize;
            }
        }

        private InputTemplate FindOrLoadTemplate(string name)
        {
            return m_TemplateCache.FindOrLoadTemplate(name);
        }

        private static void ComputeStateLayout(InputControl control)
        {
            var children = control.m_ChildrenReadOnly;

            // If the control has a format but no size specified and the format is a
            // primitive format, just set the size automatically.
            if (control.m_StateBlock.sizeInBits == 0 && control.m_StateBlock.format != 0)
            {
                var sizeInBits = InputStateBlock.GetSizeOfPrimitiveFormatInBits(control.m_StateBlock.format);
                if (sizeInBits != -1)
                    control.m_StateBlock.sizeInBits = (uint)sizeInBits;
            }

            // If state size is not set, it means it's computed from the size of the
            // children so make sure we actually have children.
            if (control.m_StateBlock.sizeInBits == 0 && children.Count == 0)
            {
                throw new Exception(
                    string.Format(
                        "Control '{0}' with template '{1}' has no size set but has no children to compute size from",
                        control.path, control.template));
            }

            // If there's no children, our job is done.
            if (children.Count == 0)
                return;

            // First deal with children that want fixed offsets. All the other ones
            // will get appended to the end.
            var firstUnfixedByteOffset = 0u;
            foreach (var child in children)
            {
                Debug.Assert(child.m_StateBlock.sizeInBits != 0);

                // Skip children using state from other controls.
                if (child.m_StateBlock.sizeInBits == kSizeForControlUsingStateFromOtherControl)
                    continue;

                // Make sure the child has a valid size set on it.
                var childSizeInBits = child.m_StateBlock.sizeInBits;
                if (childSizeInBits == 0)
                    throw new Exception(
                        string.Format("Child '{0}' of '{1}' has no size set!", child.name, control.name));

                // Skip children that don't have fixed offsets.
                if (child.m_StateBlock.byteOffset == InputStateBlock.kInvalidOffset)
                    continue;

                var endOffset =
                    BitfieldHelpers.ComputeFollowingByteOffset(child.m_StateBlock.byteOffset, child.m_StateBlock.bitOffset + childSizeInBits);
                if (endOffset > firstUnfixedByteOffset)
                    firstUnfixedByteOffset = endOffset;
            }

            ////TODO: this doesn't support mixed automatic and fixed layouting *within* bitfields;
            ////      I think it's okay not to support that but we should at least detect it

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

                // Skip children using state from other controls.
                if (child.m_StateBlock.sizeInBits == kSizeForControlUsingStateFromOtherControl)
                    continue;

                // See if it's a bit addressing control.
                var isBitAddressingChild = (child.m_StateBlock.sizeInBits % 8) != 0;
                if (isBitAddressingChild)
                {
                    // Remember start of bitfield group.
                    if (firstBitAddressingChild == null)
                        firstBitAddressingChild = child;

                    // Keep a running count of the size of the bitfield.
                    if (child.m_StateBlock.bitOffset == InputStateBlock.kInvalidOffset)
                        bitfieldSizeInBits += child.m_StateBlock.sizeInBits;
                    else
                    {
                        var lastBit = child.m_StateBlock.bitOffset + child.m_StateBlock.sizeInBits;
                        if (lastBit > bitfieldSizeInBits)
                            bitfieldSizeInBits = lastBit;
                    }
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

            // Set size. We force all parents to the combined size of their children.
            control.m_StateBlock.sizeInBits = totalSizeInBytes * 8;
        }

        // Finalize array references in the control hierarchy and make all state offets relative to the
        // device root.
        private void FinalizeControlHierarchy()
        {
            // Running indices.
            var childArrayIndex = 0;
            var usageArrayIndex = 0;
            var aliasArrayIndex = 0;

            FinalizeControlHierarchyRecursive(m_Device, ref childArrayIndex, ref usageArrayIndex,
                ref aliasArrayIndex);
        }

        private void FinalizeControlHierarchyRecursive(InputControl control, ref int childArrayIndex,
            ref int usageArrayIndex, ref int aliasArrayIndex)
        {
            // Finalize child, usage, and alias array references.
            // When we get here, all the array references are valid but we may have grown the arrays on
            // m_Device repeatedly so we want all controls to refer to those final arrays now so that the
            // garbage collector can reclaim the intermediate arrays.
            FinalizeReadonlyArray(ref control.m_ChildrenReadOnly, m_Device.m_ChildrenForEachControl, ref childArrayIndex);
            FinalizeReadonlyArray(ref control.m_UsagesReadOnly, m_Device.m_UsagesForEachControl, ref usageArrayIndex);
            FinalizeReadonlyArray(ref control.m_AliasesReadOnly, m_Device.m_AliasesForEachControl, ref aliasArrayIndex);

            // Recurse into children. Also bake our state offset into our children.
            var ourOffset = control.m_StateBlock.byteOffset;
            foreach (var child in control.m_ChildrenReadOnly)
            {
                child.m_StateBlock.byteOffset += ourOffset;
                FinalizeControlHierarchyRecursive(child, ref childArrayIndex, ref usageArrayIndex,
                    ref aliasArrayIndex);
            }
        }

        private static void FinalizeReadonlyArray<TValue>(ref ReadOnlyArray<TValue> array, TValue[] masterArray,
            ref int runningIndex)
        {
            var elementCount = array.Count;
            if (elementCount == 0)
                return;

            array = new ReadOnlyArray<TValue>(masterArray, runningIndex, elementCount);
            runningIndex += elementCount;
        }
    }
}
