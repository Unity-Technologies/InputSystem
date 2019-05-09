using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////TODO: add ability to add to existing arrays rather than creating per-device arrays

////REVIEW: it probably makes sense to have an initial phase where we process the initial set of
////        device discoveries from native and keep the layout cache around instead of throwing
////        it away after the creation of every single device; best approach may be to just
////        reuse the same InputDeviceBuilder instance over and over

////TODO: ensure that things are aligned properly for ARM; should that be done on the reading side or in the state layouts?
////       (make sure that alignment works the same on *all* platforms; otherwise editor will not be able to process events from players properly)

////FIXME: looks like `useStateFrom` is not working properly in combination with isModifyingChildControlByPath

namespace UnityEngine.InputSystem.Layouts
{
    /// <summary>
    /// Turns a device layout into an actual <see cref="InputDevice"/> instance.
    /// </summary>
    /// <remarks>
    /// Ultimately produces a device but can also be used to query the control setup described
    /// by a layout.
    ///
    /// Can be used both to create control hierarchies from scratch as well as to re-create or
    /// change existing hierarchies.
    ///
    /// InputDeviceBuilder is the only way to create control hierarchies. InputControls cannot be
    /// <c>new</c>'d directly.
    ///
    /// Also computes a final state layout when setup is finished.
    ///
    /// Note that InputDeviceBuilders generate garbage. They are meant to be used for initialization only. Don't
    /// use them during normal gameplay.
    ///
    /// Running an *existing* device through another control build is a *destructive* operation.
    /// Existing controls may be reused while at the same time the hierarchy and even the device instance
    /// itself may change.
    /// </remarks>
    public class InputDeviceBuilder
    {
        // We use this constructor when we create devices in batches.
        internal InputDeviceBuilder()
        {
        }

        public InputDeviceBuilder(string layout, string variants = null,
                                  InputDeviceDescription deviceDescription = new InputDeviceDescription(),
                                  InputDevice existingDevice = null)
        {
            Setup(new InternedString(layout), new InternedString(variants), deviceDescription, existingDevice);
        }

        internal void Setup(InternedString layout, InternedString variants,
            InputDeviceDescription deviceDescription = new InputDeviceDescription(),
            InputDevice existingDevice = null)
        {
            if (existingDevice != null && existingDevice.m_DeviceIndex != InputDevice.kInvalidDeviceIndex)
                throw new InvalidOperationException(
                    $"Cannot modify control setup of existing device {existingDevice} while added to system.");

            InstantiateLayout(layout, variants, new InternedString(), null, existingDevice);
            FinalizeControlHierarchy();

            m_Device.m_Description = deviceDescription;
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
                return InputControlPath.TryFindControl(m_Device, $"{m_Device.name}/{path}");

            return null;
        }

        // Look up a direct or indirect child control expected to be of a specific type.
        // Throws if actual type is not compatible.
        public TControl TryGetControl<TControl>(InputControl parent, string path)
            where TControl : InputControl
        {
            var control = TryGetControl(parent, path);
            if (control == null)
                return null;

            var controlOfType = control as TControl;
            if (controlOfType == null)
                throw new Exception(
                    $"Expected control '{path}' to be of type '{typeof(TControl).Name}' but is of type '{control.GetType().Name}' instead!");

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

            if (!(control is TControl controlOfType))
                throw new Exception(
                    $"Expected control '{path}' to be of type '{typeof(TControl).Name}' but is of type '{control.GetType().Name}' instead!");

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
                throw new Exception(
                    $"Expected control '{path}' to be of type '{typeof(TControl).Name}' but is of type '{control.GetType().Name}' instead!");

            return controlOfType;
        }

        private InputDevice m_Device;

        // We construct layouts lazily as we go but keep them cached while we
        // set up hierarchies so that we don't re-construct the same Button layout
        // 256 times for a keyboard.
        private InputControlLayout.Cache m_LayoutCache;

        // Table mapping (lower-cased) control paths to control layouts that contain
        // overrides for the control at the given path.
        private Dictionary<string, InputControlLayout.ControlItem> m_ChildControlOverrides;

        private StringBuilder m_StringBuilder;

        // Reset the setup in a way where it can be reused for another setup.
        // Should retain allocations that can be reused.
        private void Reset()
        {
            m_Device = null;
            m_ChildControlOverrides = null;
            // Leave the cache in place so we can reuse them in another setup path.
        }

        private InputControl InstantiateLayout(InternedString layout, InternedString variants, InternedString name, InputControl parent, InputControl existingControl)
        {
            // Look up layout by name.
            var layoutInstance = FindOrLoadLayout(layout);

            // Create control hierarchy.
            return InstantiateLayout(layoutInstance, variants, name, parent, existingControl);
        }

        private InputControl InstantiateLayout(InputControlLayout layout, InternedString variants, InternedString name, InputControl parent, InputControl existingControl)
        {
            InputControl control;

            // If we have an existing control, see whether it's usable.
            // NOTE: We allow the layout to change to a different layout as long as the new layout uses
            //       the same type.
            if (existingControl != null && existingControl.GetType() == layout.type)
            {
                control = existingControl;

                ////FIXME: the re-use path probably has some data that could stick around when it shouldn't
                control.m_UsagesReadOnly = new ReadOnlyArray<InternedString>();
                control.ClearProcessors();
            }
            else
            {
                Debug.Assert(layout.type != null);

                // No, so create a new control.
                var controlObject = Activator.CreateInstance(layout.type);
                control = controlObject as InputControl;
                if (control == null)
                {
                    throw new Exception(
                        $"Type '{layout.type.Name}' referenced by layout '{layout.name}' is not an InputControl");
                }
            }

            // If it's a device, perform some extra work specific to the control
            // hierarchy root.
            if (control is InputDevice controlAsDevice)
            {
                if (parent != null)
                    throw new Exception(
                        $"Cannot instantiate device layout '{layout.name}' as child of '{parent.path}'; devices must be added at root");

                m_Device = controlAsDevice;
                m_Device.m_StateBlock.byteOffset = 0;
                m_Device.m_StateBlock.bitOffset = 0;
                m_Device.m_StateBlock.format = layout.stateFormat;

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

                if (layout.m_UpdateBeforeRender == true)
                    m_Device.m_DeviceFlags |= InputDevice.DeviceFlags.UpdateBeforeRender;
            }
            else if (parent == null)
            {
                // Someone did "new InputDeviceBuilder(...)" with a control layout.
                // We don't support creating control hierarchies without a device at the root.
                throw new InvalidOperationException(
                    $"Toplevel layout used with InputDeviceBuilder must be a device layout; '{layout.name}' is a control layout");
            }

            // Name defaults to name of layout.
            if (name.IsEmpty())
            {
                name = layout.name;

                // If there's a namespace in the layout name, snip it out.
                var indexOfLastColon = name.ToString().LastIndexOf(':');
                if (indexOfLastColon != -1)
                    name = new InternedString(name.ToString().Substring(indexOfLastColon + 1));
            }

            // Variant defaults to variants of layout.
            if (variants.IsEmpty())
            {
                variants = layout.variants;

                if (variants.IsEmpty())
                    variants = InputControlLayout.DefaultVariant;
            }

            control.m_Name = name;
            control.m_DisplayNameFromLayout = layout.m_DisplayName; // No short display names at layout roots.
            control.m_Layout = layout.name;
            control.m_Variants = variants;
            control.m_Parent = parent;
            control.m_Device = m_Device;

            // Create children and configure their settings from our
            // layout values.
            var haveChildrenUsingStateFromOtherControl = false;
            try
            {
                // Pass list of existing control on to function as we may have decided to not
                // actually reuse the existing control (and thus control.m_ChildrenReadOnly will
                // now be blank) but still want crawling down the hierarchy to preserve existing
                // controls where possible.
                AddChildControls(layout, variants, control,
                    existingControl?.m_ChildrenReadOnly,
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
                foreach (var controlLayout in layout.controls)
                {
                    if (string.IsNullOrEmpty(controlLayout.useStateFrom))
                        continue;

                    var child = TryGetControl(control, controlLayout.name);
                    Debug.Assert(child != null);

                    // Find the referenced control.
                    var referencedControl = TryGetControl(control, controlLayout.useStateFrom);
                    if (referencedControl == null)
                        throw new Exception(
                            $"Cannot find control '{controlLayout.useStateFrom}' referenced in 'useStateFrom' of control '{controlLayout.name}' in layout '{layout.name}'");

                    // Copy its state settings.
                    child.m_StateBlock = referencedControl.m_StateBlock;

                    // At this point, all byteOffsets are relative to parents so we need to
                    // walk up the referenced control's parent chain and add offsets until
                    // we are at the same level that we are at.
                    for (var parentInChain = referencedControl.parent; parentInChain != control; parentInChain = parentInChain.parent)
                        child.m_StateBlock.byteOffset += parentInChain.m_StateBlock.byteOffset;
                }
            }

            return control;
        }

        private const uint kSizeForControlUsingStateFromOtherControl = InputStateBlock.InvalidOffset;

        private void AddChildControls(InputControlLayout layout, InternedString variants, InputControl parent, ReadOnlyArray<InputControl>? existingChildren, ref bool haveChildrenUsingStateFromOtherControls)
        {
            var controlLayouts = layout.m_Controls;
            if (controlLayouts == null)
                return;

            // Find out how many direct children we will add.
            var childCount = 0;
            var haveControlLayoutWithPath = false;
            for (var i = 0; i < controlLayouts.Length; ++i)
            {
                ////REVIEW: I'm not sure this is good enough. ATM if you have a control layout with
                ////        name "foo" and one with name "foo/bar", then the latter is taken as an override
                ////        but the former isn't. However, whether it has a slash in the path or not shouldn't
                ////        matter. If a control layout of the same name already exists, it should be
                ////        considered an override, if not, it shouldn't.
                // Not a new child if it's a layout reaching in to the hierarchy to modify
                // an existing child.
                if (controlLayouts[i].isModifyingChildControlByPath)
                {
                    if (controlLayouts[i].isArray)
                        throw new NotSupportedException(
                            $"Control '{controlLayouts[i].name}' in layout '{layout.name}' is modifying the child of another control but is marked as an array");

                    haveControlLayoutWithPath = true;
                    InsertChildControlOverrides(parent, ref controlLayouts[i]);
                    continue;
                }

                // Skip if variants don't match.
                if (!controlLayouts[i].variants.IsEmpty() &&
                    !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(controlLayouts[i].variants,
                        variants, ','))
                    continue;

                if (controlLayouts[i].isArray)
                    childCount += controlLayouts[i].arraySize;
                else
                    ++childCount;
            }

            // Add room for us in the device's child array.
            var firstChildIndex = ArrayHelpers.GrowBy(ref m_Device.m_ChildrenForEachControl, childCount);

            // Add controls from all control layouts except the ones that have
            // paths in them.
            var childIndex = firstChildIndex;
            for (var i = 0; i < controlLayouts.Length; ++i)
            {
                var controlLayout = controlLayouts[i];

                // Skip control layouts that don't add controls but rather modify child
                // controls of other controls added by the layout. We do a second pass
                // to apply their settings.
                if (controlLayout.isModifyingChildControlByPath)
                    continue;

                // If the control is part of a variant, skip it if it isn't in the variants we're
                // looking for.
                if (!controlLayout.variants.IsEmpty() &&
                    !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(controlLayout.variants,
                        variants, ','))
                    continue;

                // If it's an array, add a control for each array element.
                if (controlLayout.isArray)
                {
                    for (var n = 0; n < controlLayout.arraySize; ++n)
                    {
                        var name = controlLayout.name + n;
                        var control = AddChildControl(layout, variants, parent, existingChildren, ref haveChildrenUsingStateFromOtherControls,
                            ref controlLayout, ref childIndex, nameOverride: name);

                        // Adjust offset, if the control uses explicit offsets.
                        if (control.m_StateBlock.byteOffset != InputStateBlock.InvalidOffset)
                            control.m_StateBlock.byteOffset = (uint)n * control.m_StateBlock.alignedSizeInBytes;
                    }
                }
                else
                {
                    AddChildControl(layout, variants, parent, existingChildren, ref haveChildrenUsingStateFromOtherControls,
                        ref controlLayout, ref childIndex);
                }
            }

            // Install child array on parent. We will later patch up the array
            // reference again as we finalize the hierarchy. However, the reference
            // will point to a valid child array all the same even while we are
            // constructing the hierarchy.
            //
            // NOTE: It's important to do this *after* the loop above where we call InstantiateLayout for each child
            //       as each child may end up moving the m_ChildrenForEachControl array around.
            parent.m_ChildrenReadOnly = new ReadOnlyArray<InputControl>(m_Device.m_ChildrenForEachControl, firstChildIndex, childCount);

            ////TODO: replace the entire post-creation modification logic here with using m_ChildControlOverrides
            ////      (note that we have to *merge* into the table; if there's already overrides, only replace properties that haven't been set)
            ////      (however, this will also require moving the child insertion logic somewhere else)

            // Apply control modifications from control layouts with paths.
            if (haveControlLayoutWithPath)
            {
                for (var i = 0; i < controlLayouts.Length; ++i)
                {
                    var controlLayout = controlLayouts[i];
                    if (!controlLayout.isModifyingChildControlByPath)
                        continue;

                    // If the control is part of a variants, skip it if it isn't the variants we're
                    // looking for.
                    if (!controlLayout.variants.IsEmpty() && controlLayout.variants != variants)
                        continue;

                    ModifyChildControl(layout, variants, parent, ref haveChildrenUsingStateFromOtherControls,
                        ref controlLayout);
                }
            }
        }

        private InputControl AddChildControl(InputControlLayout layout, InternedString variants, InputControl parent,
            ReadOnlyArray<InputControl>? existingChildren, ref bool haveChildrenUsingStateFromOtherControls,
            ref InputControlLayout.ControlItem controlItem, ref int childIndex, string nameOverride = null)
        {
            var name = nameOverride ?? controlItem.name;
            var nameLowerCase = name.ToLower();
            var nameInterned = new InternedString(name);

            ////REVIEW: can we check this in InputControlLayout instead?
            if (string.IsNullOrEmpty(controlItem.layout))
                throw new Exception($"Layout has not been set on control '{controlItem.name}' in '{layout.name}'");

            // See if there is an override for the control.
            InputControlLayout.ControlItem? controlOverride = null;
            if (m_ChildControlOverrides != null)
            {
                var path = $"{parent.path}/{name}";
                var pathLowerCase = path.ToLower();

                if (m_ChildControlOverrides.TryGetValue(pathLowerCase, out var match))
                    controlOverride = match;
            }

            // Get name of layout to use for control.
            var layoutName = controlItem.layout;
            if (controlOverride != null && !controlOverride.Value.layout.IsEmpty())
                layoutName = controlOverride.Value.layout;

            // See if we have an existing control that we might be able to re-use.
            InputControl existingControl = null;
            if (existingChildren != null)
            {
                var existingChildCount = existingChildren.Value.Count;
                for (var n = 0; n < existingChildCount; ++n)
                {
                    var existingChild = existingChildren.Value[n];
                    if (existingChild.layout == layoutName
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
                control = InstantiateLayout(layoutName, variants, nameInterned, parent, existingControl);
            }
            catch (InputControlLayout.LayoutNotFoundException exception)
            {
                // Throw better exception that gives more info.
                throw new Exception(
                    $"Cannot find layout '{exception.layout}' used in control '{name}' of layout '{layout.name}'",
                    exception);
            }

            // Add to array.
            // NOTE: AddChildControls and InstantiateLayout take care of growing the array and making
            //       room for the immediate children of each control.
            m_Device.m_ChildrenForEachControl[childIndex] = control;
            ++childIndex;

            // Set flags and misc things.
            control.noisy = controlItem.isNoisy;
            control.synthetic = controlItem.isSynthetic;
            if (control.noisy)
                m_Device.noisy = true;

            // Remember the display names from the layout. We later do a proper pass once we have
            // the full hierarchy to set final names.
            control.m_DisplayNameFromLayout = controlItem.displayName;
            control.m_ShortDisplayNameFromLayout = controlItem.shortDisplayName;

            // Set default value.
            control.m_DefaultValue = controlItem.defaultState;
            if (!control.m_DefaultValue.isEmpty)
                m_Device.hasControlsWithDefaultState = true;

            // Set min and max value. Don't just overwrite here as the control's constructor may
            // have set a default value.
            if (!controlItem.minValue.isEmpty)
                control.m_MinValue = controlItem.minValue;
            if (!controlItem.maxValue.isEmpty)
                control.m_MaxValue = controlItem.maxValue;

            // Pass state block config on to control.
            var usesStateFromOtherControl = !string.IsNullOrEmpty(controlItem.useStateFrom);
            if (!usesStateFromOtherControl)
            {
                control.m_StateBlock.byteOffset = controlItem.offset;
                control.m_StateBlock.bitOffset = controlItem.bit;
                if (controlItem.sizeInBits != 0)
                    control.m_StateBlock.sizeInBits = controlItem.sizeInBits;
                if (controlItem.format != 0)
                    SetFormat(control, controlItem);
            }
            else
            {
                // Mark controls that don't have state blocks of their own but rather get their
                // blocks from other controls by setting their state size to InvalidOffset.
                control.m_StateBlock.sizeInBits = kSizeForControlUsingStateFromOtherControl;
                haveChildrenUsingStateFromOtherControls = true;
            }

            ////REVIEW: the constant appending to m_UsagesForEachControl and m_AliasesForEachControl may lead to a lot
            ////        of successive re-allocations

            // Add usages.
            var usages = controlOverride?.usages ?? controlItem.usages;
            if (usages.Count > 0)
            {
                var usageCount = usages.Count;
                var usageIndex =
                    ArrayHelpers.AppendToImmutable(ref m_Device.m_UsagesForEachControl, usages.m_Array);
                control.m_UsagesReadOnly =
                    new ReadOnlyArray<InternedString>(m_Device.m_UsagesForEachControl, usageIndex, usageCount);

                ArrayHelpers.GrowBy(ref m_Device.m_UsageToControl, usageCount);
                for (var n = 0; n < usageCount; ++n)
                    m_Device.m_UsageToControl[usageIndex + n] = control;
            }

            // Add aliases.
            if (controlItem.aliases.Count > 0)
            {
                var aliasCount = controlItem.aliases.Count;
                var aliasIndex =
                    ArrayHelpers.AppendToImmutable(ref m_Device.m_AliasesForEachControl, controlItem.aliases.m_Array);
                control.m_AliasesReadOnly =
                    new ReadOnlyArray<InternedString>(m_Device.m_AliasesForEachControl, aliasIndex, aliasCount);
            }

            // Set parameters.
            if (controlItem.parameters.Count > 0)
                NamedValue.ApplyAllToObject(control, controlItem.parameters);

            // Add processors.
            if (controlItem.processors.Count > 0)
                AddProcessors(control, ref controlItem, layout.name);

            return control;
        }

        private void InsertChildControlOverrides(InputControl parent, ref InputControlLayout.ControlItem controlItem)
        {
            if (m_ChildControlOverrides == null)
                m_ChildControlOverrides = new Dictionary<string, InputControlLayout.ControlItem>();

            var path = InputControlPath.Combine(parent, controlItem.name);
            var pathLowerCase = path.ToLower();

            // See if there are existing overrides for the control.
            if (!m_ChildControlOverrides.TryGetValue(pathLowerCase, out var existingOverrides))
            {
                // So, so just insert our overrides and we're done.
                m_ChildControlOverrides[pathLowerCase] = controlItem;
                return;
            }

            // Yes, there's existing overrides so we have to merge.
            existingOverrides = existingOverrides.Merge(controlItem);
            m_ChildControlOverrides[pathLowerCase] = existingOverrides;
        }

        private void ModifyChildControl(InputControlLayout layout, InternedString variants, InputControl parent,
            ref bool haveChildrenUsingStateFromOtherControls,
            ref InputControlLayout.ControlItem controlItem)
        {
            ////TODO: support arrays (we may modify an entire array in bulk)

            // Controls layout themselves as we come back up the hierarchy. However, when we
            // apply layout modifications reaching *into* the hierarchy, we need to retrigger
            // layouting on their parents.
            var haveChangedLayoutOfParent = false;

            // Find the child control.
            var child = TryGetControl(parent, controlItem.name);
            if (child == null)
            {
                // We're adding a child somewhere in the existing hierarchy. This is a tricky
                // case as we have to potentially shift indices around in the hierarchy to make
                // room for the new control.

                ////TODO: this path does not support recovering existing controls? does it matter?

                child = InsertChildControl(layout, variants, parent,
                    ref haveChildrenUsingStateFromOtherControls, ref controlItem);
                haveChangedLayoutOfParent = true;
            }
            else
            {
                // Apply modifications.
                if (controlItem.sizeInBits != 0 &&
                    child.m_StateBlock.sizeInBits != controlItem.sizeInBits)
                {
                    child.m_StateBlock.sizeInBits = controlItem.sizeInBits;
                }
                if (controlItem.format != 0 && child.m_StateBlock.format != controlItem.format)
                {
                    SetFormat(child, controlItem);
                    haveChangedLayoutOfParent = true;
                }
                ////REVIEW: ATM, when you move a child with a fixed offset, we only move the child
                ////        and don't move the parent or siblings. What this means is that if you move
                ////        leftStick/x, for example, leftStick stays put. ATM you have to move *all*
                ////        controls that are part of a chain manually. Not sure what the best behavior
                ////        is. If we opt to move parents along with children, we have to make sure we
                ////        are not colliding with any other relocations of children (e.g. if you move
                ////        both leftStick/x and leftStick/y, leftStick itself should move only once and
                ////        not at all if there indeed is a leftStick control layout with an offset;
                ////        so, it'd get quite complicated)
                if (controlItem.offset != InputStateBlock.InvalidOffset)
                    child.m_StateBlock.byteOffset = controlItem.offset;
                if (controlItem.bit != InputStateBlock.InvalidOffset)
                    child.m_StateBlock.bitOffset = controlItem.bit;
                if (controlItem.processors.Count > 0)
                    AddProcessors(child, ref controlItem, layout.name);
                ////REVIEW: ATM parameters applied using this path add on top instead of just overriding existing parameters
                if (controlItem.parameters.Count > 0)
                    NamedValue.ApplyAllToObject(child, controlItem.parameters);
                if (!string.IsNullOrEmpty(controlItem.displayName))
                    child.m_DisplayNameFromLayout = controlItem.displayName;
                if (!controlItem.defaultState.isEmpty)
                {
                    child.m_DefaultValue = controlItem.defaultState;
                    m_Device.hasControlsWithDefaultState = true;
                }
                if (!controlItem.minValue.isEmpty)
                    child.m_MinValue = controlItem.minValue;
                if (!controlItem.maxValue.isEmpty)
                    child.m_MaxValue = controlItem.maxValue;

                ////TODO: other modifications
            }

            // Apply layout change.
            ////REVIEW: not sure what's better here; trigger this immediately means we may trigger
            ////        it a number of times on the same parent but doing it as a final pass would
            ////        require either collecting the necessary parents or doing another pass through
            ////        the list of control layouts
            if (haveChangedLayoutOfParent && !ReferenceEquals(child.parent, parent))
                ComputeStateLayout(child.parent);
        }

        private InputControl InsertChildControl(InputControlLayout layout, InternedString variant, InputControl parent,
            ref bool haveChildrenUsingStateFromOtherControls,
            ref InputControlLayout.ControlItem controlItem)
        {
            var path = controlItem.name.ToString();

            // First we need to find the immediate parent from the given path.
            var indexOfSlash = path.LastIndexOf('/');
            if (indexOfSlash == -1)
                throw new ArgumentException("InsertChildControl has to be called with a slash-separated path", "path");
            Debug.Assert(indexOfSlash != 0);
            var immediateParentPath = path.Substring(0, indexOfSlash);
            var immediateParent = InputControlPath.TryFindChild(parent, immediateParentPath);
            if (immediateParent == null)
                throw new Exception(
                    $"Cannot find parent '{immediateParentPath}' of control '{controlItem.name}' in layout '{layout.name}'");

            var controlName = path.Substring(indexOfSlash + 1);
            if (controlName.Length == 0)
                throw new Exception(
                    $"Path cannot end in '/' (control '{controlItem.name}' in layout '{layout.name}')");

            // Make room in the device's child array.
            var childStartIndex = immediateParent.m_ChildrenReadOnly.m_StartIndex;
            var childIndex = childStartIndex + immediateParent.m_ChildrenReadOnly.m_Length;
            ArrayHelpers.InsertAt(ref m_Device.m_ChildrenForEachControl, childIndex, null);
            ++immediateParent.m_ChildrenReadOnly.m_Length;

            // Insert the child.
            var control = AddChildControl(layout, variant, immediateParent, null,
                ref haveChildrenUsingStateFromOtherControls, ref controlItem, ref childIndex, controlName);

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

        // NOTE: We can only do this once we've initialized the names on the parent control. I.e. it has to be
        //       done in the second pass we do over the control hierarchy.
        private void SetDisplayName(InputControl control, string displayNameFromLayout, bool shortName)
        {
            // Display name may not be set in layout.
            if (string.IsNullOrEmpty(displayNameFromLayout))
            {
                // For short names, we leave it unassigned if there's nothing in the layout.
                if (shortName)
                {
                    control.m_ShortDisplayNameFromLayout = null;
                    return;
                }

                ////REVIEW: automatically uppercase or prettify this?
                // For long names, we default to the control's name.
                displayNameFromLayout = control.name;
            }

            // If it's a nested control, synthesize a path that includes parents.
            if (control.parent != null && control.parent != control.device)
            {
                if (m_StringBuilder == null)
                    m_StringBuilder = new StringBuilder();
                m_StringBuilder.Length = 0;
                AddParentDisplayNameRecursive(control.parent, m_StringBuilder, shortName);
                m_StringBuilder.Append(displayNameFromLayout);
                displayNameFromLayout = m_StringBuilder.ToString();
            }

            // Assign.
            if (shortName)
                control.m_ShortDisplayNameFromLayout = displayNameFromLayout;
            else
                control.m_DisplayNameFromLayout = displayNameFromLayout;
        }

        private static void AddParentDisplayNameRecursive(InputControl control, StringBuilder stringBuilder,
            bool shortName)
        {
            if (control.parent != null && control.parent != control.device)
                AddParentDisplayNameRecursive(control.parent, stringBuilder, shortName);

            if (shortName)
            {
                var text = control.shortDisplayName;
                if (string.IsNullOrEmpty(text))
                    text = control.displayName;

                stringBuilder.Append(text);
            }
            else
            {
                stringBuilder.Append(control.displayName);
            }
            stringBuilder.Append(' ');
        }

        private static void AddProcessors(InputControl control, ref InputControlLayout.ControlItem controlItem, string layoutName)
        {
            var processorCount = controlItem.processors.Count;
            for (var n = 0; n < processorCount; ++n)
            {
                var name = controlItem.processors[n].name;
                var type = InputProcessor.s_Processors.LookupTypeRegistration(name);
                if (type == null)
                    throw new Exception(
                        $"Cannot find processor '{name}' referenced by control '{controlItem.name}' in layout '{layoutName}'");

                var processor = Activator.CreateInstance(type);

                var parameters = controlItem.processors[n].parameters;
                if (parameters.Count > 0)
                    NamedValue.ApplyAllToObject(processor, parameters);

                control.AddProcessor(processor);
            }
        }

        private static void SetFormat(InputControl control, InputControlLayout.ControlItem controlItem)
        {
            control.m_StateBlock.format = controlItem.format;
            if (controlItem.sizeInBits == 0)
            {
                var primitiveFormatSize = InputStateBlock.GetSizeOfPrimitiveFormatInBits(controlItem.format);
                if (primitiveFormatSize != -1)
                    control.m_StateBlock.sizeInBits = (uint)primitiveFormatSize;
            }
        }

        private InputControlLayout FindOrLoadLayout(string name)
        {
            return m_LayoutCache.FindOrLoadLayout(name);
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
                    $"Control '{control.path}' with layout '{control.layout}' has no size set and has no children to compute size from");
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
                if (childSizeInBits == 0 || childSizeInBits == InputStateBlock.InvalidOffset)
                    throw new Exception(
                        $"Child '{child.name}' of '{control.name}' has no size set!");

                // Skip children that don't have fixed offsets.
                if (child.m_StateBlock.byteOffset == InputStateBlock.InvalidOffset ||
                    child.m_StateBlock.byteOffset == InputStateBlock.AutomaticOffset)
                    continue;

                // At this point, if the child has no valid bit offset, put it at #0 now.
                if (child.m_StateBlock.bitOffset == InputStateBlock.InvalidOffset)
                    child.m_StateBlock.bitOffset = 0;

                // See if the control bumps our fixed layout size.
                var endOffset =
                    MemoryHelpers.ComputeFollowingByteOffset(child.m_StateBlock.byteOffset, child.m_StateBlock.bitOffset + childSizeInBits);
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
                if (child.m_StateBlock.byteOffset != InputStateBlock.InvalidOffset &&
                    child.m_StateBlock.byteOffset != InputStateBlock.AutomaticOffset)
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
                    if (child.m_StateBlock.bitOffset == InputStateBlock.InvalidOffset ||
                        child.m_StateBlock.bitOffset == InputStateBlock.AutomaticOffset)
                    {
                        // Put child at current bit offset.
                        child.m_StateBlock.bitOffset = bitfieldSizeInBits;

                        bitfieldSizeInBits += child.m_StateBlock.sizeInBits;
                    }
                    else
                    {
                        // Child already has bit offset. Keep it but make sure we're accounting for it
                        // in the bitfield size.
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
                        runningByteOffset = MemoryHelpers.ComputeFollowingByteOffset(runningByteOffset, bitfieldSizeInBits);
                        firstBitAddressingChild = null;
                    }

                    if (child.m_StateBlock.bitOffset == InputStateBlock.InvalidOffset)
                        child.m_StateBlock.bitOffset = 0;
                }

                ////FIXME: seems like this should take bitOffset into account
                child.m_StateBlock.byteOffset = runningByteOffset;

                if (!isBitAddressingChild)
                    runningByteOffset =
                        MemoryHelpers.ComputeFollowingByteOffset(runningByteOffset, child.m_StateBlock.sizeInBits);
            }

            // Compute total size.
            // If we ended on a bitfield, account for its size.
            if (firstBitAddressingChild != null)
                runningByteOffset = MemoryHelpers.ComputeFollowingByteOffset(runningByteOffset, bitfieldSizeInBits);
            var totalSizeInBytes = runningByteOffset;

            // Set size. We force all parents to the combined size of their children.
            control.m_StateBlock.sizeInBits = totalSizeInBytes * 8;
        }

        // Finalize array references in the control hierarchy and make all state offsets relative to the
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
            // Set display names.
            SetDisplayName(control, control.m_DisplayNameFromLayout, false);
            SetDisplayName(control, control.m_ShortDisplayNameFromLayout, true);

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
