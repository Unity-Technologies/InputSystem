using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

// The way target bindings for overrides are found:
// - If specified, directly by index (e.g. "apply this override to the third binding in the map")
// - By path (e.g. "search for binding to '<Gamepad>/leftStick' and override it with '<Gamepad>/rightStick'")
// - By group (e.g. "search for binding on action 'fire' with group 'keyboard&mouse' and override it with '<Keyboard>/space'")
// - By action (e.g. "bind action 'fire' from whatever it is right now to '<Gamepad>/leftStick'")

////TODO: make this work implicitly with PlayerInputs such that rebinds can be restricted to the device's of a specific player

////TODO: allow rebinding by GUIDs now that we have IDs on bindings

////FIXME: properly work with composites

////REVIEW: how well are we handling the case of rebinding to joysticks? (mostly auto-generated HID layouts)

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Extensions to help with dynamically rebinding <see cref="InputAction"/>s in
    /// various ways.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="InputActionSetupExtensions"/>, the extension methods in here are meant to be
    /// called during normal game operation, i.e. as part of screens whether the user can rebind
    /// controls.
    ///
    /// The two primary duties of these extensions are to apply binding overrides that non-destructively
    /// redirect existing bindings and to facilitate user-controlled rebinding by listening for controls
    /// actuated by the user.
    /// </remarks>
    /// <seealso cref="InputActionSetupExtensions"/>
    /// <seealso cref="InputBinding"/>
    /// <seealso cref="InputAction.bindings"/>
    public static class InputActionRebindingExtensions
    {
        /// <summary>
        /// Get the index of the first binding in <see cref="InputAction.bindings"/> on <paramref name="action"/>
        /// that matches the given binding mask.
        /// </summary>
        /// <param name="action">An input action.</param>
        /// <param name="bindingMask">Binding mask to match (see <see cref="InputBinding.Matches"/>).</param>
        /// <returns>The first binding on the action matching <paramref name="bindingMask"/> or -1 if no binding
        /// on the action matches the mask.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <seealso cref="InputBinding.Matches"/>
        public static int GetBindingIndex(this InputAction action, InputBinding bindingMask)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; ++i)
                if (bindingMask.Matches(bindings[i]))
                    return i;

            return -1;
        }

        /// <summary>
        /// Get the index of the first binding in <see cref="InputAction.bindings"/> on <paramref name="action"/>
        /// that matches the given binding group and/or path.
        /// </summary>
        /// <param name="action">An input action.</param>
        /// <param name="group">Binding group to match (see <see cref="InputBinding.groups"/>).</param>
        /// <param name="path">Binding path to match (see <see cref="InputBinding.path"/>).</param>
        /// <returns>The first binding on the action matching the given group and/or path or -1 if no binding
        /// on the action matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <seealso cref="InputBinding.Matches"/>
        public static int GetBindingIndex(this InputAction action, string group = default, string path = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return action.GetBindingIndex(new InputBinding(groups: group, path: path));
        }

        /// <summary>
        /// Return the binding that the given control resolved from.
        /// </summary>
        /// <param name="action">An input action that may be using the given control.</param>
        /// <param name="control">Control to look for a binding for.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c> -or- <paramref name="control"/>
        /// is <c>null</c>.</exception>
        /// <returns>The binding from which <paramref name="control"/> has been resolved or <c>null</c> if no such binding
        /// could be found on <paramref name="action"/>.</returns>
        public static InputBinding? GetBindingForControl(this InputAction action, InputControl control)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var bindingIndex = GetBindingIndexForControl(action, control);
            if (bindingIndex == -1)
                return null;
            return action.bindings[bindingIndex];
        }

        /// <summary>
        /// Return the index into <paramref name="action"/>'s <see cref="InputAction.bindings"/> that corresponds
        /// to <paramref name="control"/> bound to the action.
        /// </summary>
        /// <param name="action">The input action whose bindings to use.</param>
        /// <param name="control">An input control for which to look for a binding.</param>
        /// <returns>The index into the action's binding array for the binding that <paramref name="control"/> was
        /// resolved from or -1 if the control is not currently bound to the action.</returns>
        /// <remarks>
        /// Note that this method will only take currently active bindings into consideration. This means that if
        /// the given control <em>could</em> come from one of the bindings on the action but does not currently
        /// do so, the method still returns -1.
        ///
        /// In case you want to manually find out which of the bindings on the action could match the given control,
        /// you can do so using <see cref="InputControlPath.Matches"/>:
        ///
        /// <example>
        /// <code>
        /// // Find the binding on 'action' that matches the given 'control'.
        /// foreach (var binding in action.bindings)
        ///     if (InputControlPath.Matches(binding.effectivePath, control))
        ///         Debug.Log($"Binding for {control}: {binding}");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c> -or- <paramref name="control"/>
        /// is <c>null</c>.</exception>
        public static unsafe int GetBindingIndexForControl(this InputAction action, InputControl control)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var actionMap = action.GetOrCreateActionMap();
            actionMap.ResolveBindingsIfNecessary();

            var state = actionMap.m_State;
            Debug.Assert(state != null, "Bindings are expected to have been resolved at this point");

            // Find index of control in state.
            var controlIndex = Array.IndexOf(state.controls, control);
            if (controlIndex == -1)
                return -1;

            // Map to binding index.
            var actionIndex = action.m_ActionIndexInState;
            var bindingCount = state.totalBindingCount;
            for (var i = 0; i < bindingCount; ++i)
            {
                var bindingStatePtr = &state.bindingStates[i];
                if (bindingStatePtr->actionIndex == actionIndex && bindingStatePtr->controlStartIndex <= controlIndex &&
                    controlIndex < bindingStatePtr->controlStartIndex + bindingStatePtr->controlCount)
                {
                    var bindingIndexInMap = state.GetBindingIndexInMap(i);
                    return action.BindingIndexOnMapToBindingIndexOnAction(bindingIndexInMap);
                }
            }

            return -1;
        }

        /// <summary>
        /// Return a string suitable for display in UIs that shows what the given action is currently bound to.
        /// </summary>
        /// <param name="action">Action to create a display string for.</param>
        /// <param name="options">Optional set of formatting flags.</param>
        /// <param name="group">Optional binding group to restrict the operation to. If this is supplied, it effectively
        /// becomes the binding mask (see <see cref="InputBinding.Matches(InputBinding)"/>) to supply to <see
        /// cref="GetBindingDisplayString(InputAction,InputBinding,InputBinding.DisplayStringOptions)"/>.</param>
        /// <returns>A string suitable for display in rebinding UIs.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method will take into account any binding masks (such as from control schemes) in effect on the action
        /// (such as <see cref="InputAction.bindingMask"/> on the action itself, the <see cref="InputActionMap.bindingMask"/>
        /// on its action map, or the <see cref="InputActionAsset.bindingMask"/> on its asset) as well as the actual controls
        /// that the action is currently bound to (see <see cref="InputAction.controls"/>).
        ///
        /// <example>
        /// <code>
        /// var action = new InputAction();
        ///
        /// action.AddBinding("&lt;Gamepad&gt;/buttonSouth", groups: "Gamepad");
        /// action.AddBinding("&lt;Mouse&gt;/leftButton", groups: "KeyboardMouse");
        ///
        /// // Prints "A | LMB".
        /// Debug.Log(action.GetBindingDisplayString());
        ///
        /// // Prints "A".
        /// Debug.Log(action.GetBindingDisplayString(group: "Gamepad");
        ///
        /// // Prints "LMB".
        /// Debug.Log(action.GetBindingDisplayString(group: "KeyboardMouse");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.ToDisplayString(InputBinding.DisplayStringOptions,InputControl)"/>
        /// <seealso cref="InputControlPath.ToHumanReadableString(string,InputControlPath.HumanReadableStringOptions,InputControl)"/>
        public static string GetBindingDisplayString(this InputAction action, InputBinding.DisplayStringOptions options = default,
            string group = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // Default binding mask to the one found on the action or any of its
            // containers.
            InputBinding bindingMask;
            if (!string.IsNullOrEmpty(group))
            {
                bindingMask = InputBinding.MaskByGroup(group);
            }
            else
            {
                var mask = action.FindEffectiveBindingMask();
                if (mask.HasValue)
                    bindingMask = mask.Value;
                else
                    bindingMask = default;
            }

            return GetBindingDisplayString(action, bindingMask, options);
        }

        /// <summary>
        /// Return a string suitable for display in UIs that shows what the given action is currently bound to.
        /// </summary>
        /// <param name="action">Action to create a display string for.</param>
        /// <param name="bindingMask">Mask for bindings to take into account. Any binding on the action not
        /// matching (see <see cref="InputBinding.Matches(InputBinding)"/>) the mask is ignored and not included
        /// in the resulting string.</param>
        /// <param name="options">Optional set of formatting flags.</param>
        /// <returns>A string suitable for display in rebinding UIs.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method will take into account any binding masks (such as from control schemes) in effect on the action
        /// (such as <see cref="InputAction.bindingMask"/> on the action itself, the <see cref="InputActionMap.bindingMask"/>
        /// on its action map, or the <see cref="InputActionAsset.bindingMask"/> on its asset) as well as the actual controls
        /// that the action is currently bound to (see <see cref="InputAction.controls"/>).
        ///
        /// <example>
        /// <code>
        /// var action = new InputAction();
        ///
        /// action.AddBinding("&lt;Gamepad&gt;/buttonSouth", groups: "Gamepad");
        /// action.AddBinding("&lt;Mouse&gt;/leftButton", groups: "KeyboardMouse");
        ///
        /// // Prints "A".
        /// Debug.Log(action.GetBindingDisplayString(InputBinding.MaskByGroup("Gamepad"));
        ///
        /// // Prints "LMB".
        /// Debug.Log(action.GetBindingDisplayString(InputBinding.MaskByGroup("KeyboardMouse"));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.ToDisplayString(InputBinding.DisplayStringOptions,InputControl)"/>
        /// <seealso cref="InputControlPath.ToHumanReadableString(string,InputControlPath.HumanReadableStringOptions,InputControl)"/>
        public static string GetBindingDisplayString(this InputAction action, InputBinding bindingMask,
            InputBinding.DisplayStringOptions options = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var result = string.Empty;
            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; ++i)
            {
                if (!bindingMask.Matches(bindings[i]))
                    continue;

                ////REVIEW: should this filter out bindings that are not resolving to any controls?

                var text = action.GetBindingDisplayString(i, options);
                if (result != "")
                    result = $"{result} | {text}";
                else
                    result = text;
            }

            return result;
        }

        /// <summary>
        /// Return a string suitable for display in UIs that shows what the given action is currently bound to.
        /// </summary>
        /// <param name="action">Action to create a display string for.</param>
        /// <param name="bindingIndex">Index of the binding in the <see cref="InputAction.bindings"/> array of
        /// <paramref name="action"/> for which to get a display string.</param>
        /// <param name="options">Optional set of formatting flags.</param>
        /// <returns>A string suitable for display in rebinding UIs.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method will ignore active binding masks and return the display string for the given binding whether it
        /// is masked out (disabled) or not.
        ///
        /// <example>
        /// <code>
        /// var action = new InputAction();
        ///
        /// action.AddBinding("&lt;Gamepad&gt;/buttonSouth", groups: "Gamepad");
        /// action.AddBinding("&lt;Mouse&gt;/leftButton", groups: "KeyboardMouse");
        ///
        /// // Prints "A".
        /// Debug.Log(action.GetBindingDisplayString(0));
        ///
        /// // Prints "LMB".
        /// Debug.Log(action.GetBindingDisplayString(1));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.ToDisplayString(InputBinding.DisplayStringOptions,InputControl)"/>
        /// <seealso cref="InputControlPath.ToHumanReadableString(string,InputControlPath.HumanReadableStringOptions,InputControl)"/>
        public static string GetBindingDisplayString(this InputAction action, int bindingIndex, InputBinding.DisplayStringOptions options = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return action.GetBindingDisplayString(bindingIndex, out var _, out var _, options);
        }

        /// <summary>
        /// Return a string suitable for display in UIs that shows what the given action is currently bound to.
        /// </summary>
        /// <param name="action">Action to create a display string for.</param>
        /// <param name="bindingIndex">Index of the binding in the <see cref="InputAction.bindings"/> array of
        /// <paramref name="action"/> for which to get a display string.</param>
        /// <param name="deviceLayoutName">Receives the name of the <see cref="InputControlLayout"/> used for the
        /// device in the given binding, if applicable. Otherwise is set to <c>null</c>. If, for example, the binding
        /// is <c>"&lt;Gamepad&gt;/buttonSouth"</c>, the resulting value is <c>"Gamepad</c>.</param>
        /// <param name="controlPath">Receives the path to the control on the device referenced in the given binding,
        /// if applicable. Otherwise is set to <c>null</c>. If, for example, the binding is <c>"&lt;Gamepad&gt;/leftStick/x"</c>,
        /// the resulting value is <c>"leftStick/x"</c>.</param>
        /// <param name="options">Optional set of formatting flags.</param>
        /// <returns>A string suitable for display in rebinding UIs.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The information returned by <paramref name="deviceLayoutName"/> and <paramref name="controlPath"/> can be used, for example,
        /// to associate images with controls. Based on knowing which layout is used and which control on the layout is referenced, you
        /// can look up an image dynamically. For example, if the layout is based on <see cref="DualShock.DualShockGamepad"/> (use
        /// <see cref="InputSystem.IsFirstLayoutBasedOnSecond"/> to determine inheritance), you can pick a PlayStation-specific image
        /// for the control as named by <paramref name="controlPath"/>.
        ///
        /// <example>
        /// <code>
        /// var action = new InputAction();
        ///
        /// action.AddBinding("&lt;Gamepad&gt;/dpad/up", groups: "Gamepad");
        /// action.AddBinding("&lt;Mouse&gt;/leftButton", groups: "KeyboardMouse");
        ///
        /// // Prints "A", then "Gamepad", then "dpad/up".
        /// Debug.Log(action.GetBindingDisplayString(InputBinding.MaskByGroup("Gamepad", out var deviceLayoutNameA, out var controlPathA));
        /// Debug.Log(deviceLayoutNameA);
        /// Debug.Log(controlPathA);
        ///
        /// // Prints "LMB", then "Mouse", then "leftButton".
        /// Debug.Log(action.GetBindingDisplayString(InputBinding.MaskByGroup("KeyboardMouse", out var deviceLayoutNameB, out var controlPathB));
        /// Debug.Log(deviceLayoutNameB);
        /// Debug.Log(controlPathB);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.ToDisplayString(InputBinding.DisplayStringOptions,InputControl)"/>
        /// <seealso cref="InputControlPath.ToHumanReadableString(string,InputControlPath.HumanReadableStringOptions,InputControl)"/>
        public static unsafe string GetBindingDisplayString(this InputAction action, int bindingIndex,
            out string deviceLayoutName, out string controlPath,
            InputBinding.DisplayStringOptions options = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            deviceLayoutName = null;
            controlPath = null;

            var bindings = action.bindings;
            var bindingCount = bindings.Count;
            if (bindingIndex < 0 || bindingIndex >= bindingCount)
                throw new ArgumentOutOfRangeException(
                    $"Binding index {bindingIndex} is out of range on action '{action}' with {bindings.Count} bindings",
                    nameof(bindingIndex));

            // If the binding is a composite, compose a string using the display format string for
            // the composite.
            // NOTE: In this case, there won't be a deviceLayoutName returned from the method.
            if (bindings[bindingIndex].isComposite)
            {
                var compositeName = NameAndParameters.Parse(bindings[bindingIndex].effectivePath).name;

                // Determine what parts we have.
                var firstPartIndex = bindingIndex + 1;
                var lastPartIndex = firstPartIndex;
                while (lastPartIndex < bindingCount && bindings[lastPartIndex].isPartOfComposite)
                    ++lastPartIndex;
                var partCount = lastPartIndex - firstPartIndex;

                // Get the display string for each part.
                var partStrings = new string[partCount];
                for (var i = 0; i < partCount; ++i)
                    partStrings[i] = action.GetBindingDisplayString(firstPartIndex + i, options);

                // Put the parts together based on the display format string for
                // the composite.
                var displayFormatString = InputBindingComposite.GetDisplayFormatString(compositeName);
                if (string.IsNullOrEmpty(displayFormatString))
                {
                    // No display format string. Simply go and combine all part strings.
                    return StringHelpers.Join("/", partStrings);
                }

                return StringHelpers.ExpandTemplateString(displayFormatString,
                    fragment =>
                    {
                        var result = string.Empty;

                        // Go through all parts and look for one with the given name.
                        for (var i = 0; i < partCount; ++i)
                        {
                            if (!string.Equals(bindings[firstPartIndex + i].name, fragment, StringComparison.InvariantCultureIgnoreCase))
                                continue;

                            if (!string.IsNullOrEmpty(result))
                                result = $"{result}|{partStrings[i]}";
                            else
                                result = partStrings[i];
                        }

                        return result;
                    });
            }

            // See if the binding maps to controls.
            InputControl control = null;
            var actionMap = action.GetOrCreateActionMap();
            actionMap.ResolveBindingsIfNecessary();
            var actionState = actionMap.m_State;
            Debug.Assert(actionState != null, "Expecting action state to be in place at this point");
            var bindingIndexInMap = action.BindingIndexOnActionToBindingIndexOnMap(bindingIndex);
            var bindingIndexInState = actionState.GetBindingIndexInState(actionMap.m_MapIndexInState, bindingIndexInMap);
            Debug.Assert(bindingIndexInState >= 0 && bindingIndexInState < actionState.totalBindingCount,
                "Computed binding index is out of range");
            var bindingStatePtr = &actionState.bindingStates[bindingIndexInState];
            if (bindingStatePtr->controlCount > 0)
            {
                ////REVIEW: does it make sense to just take a single control here?
                control = actionState.controls[bindingStatePtr->controlStartIndex];
            }

            // Take interactions applied to the action into account.
            var binding = bindings[bindingIndex];
            if (string.IsNullOrEmpty(binding.effectiveInteractions))
                binding.overrideInteractions = action.interactions;
            else if (!string.IsNullOrEmpty(action.interactions))
                binding.overrideInteractions = $"{binding.effectiveInteractions};action.interactions";

            return binding.ToDisplayString(out deviceLayoutName, out controlPath, options, control: control);
        }

        /// <summary>
        /// Put an override on all matching bindings of <paramref name="action"/>.
        /// </summary>
        /// <param name="action">Action to apply the override to.</param>
        /// <param name="newPath">New binding path to take effect. Supply an empty string
        /// to disable the binding(s). See <see cref="InputControlPath"/> for details on
        /// the path language.</param>
        /// <param name="group">Optional list of binding groups to target the override
        /// to. For example, <c>"Keyboard;Gamepad"</c> will only apply overrides to bindings
        /// that either have the <c>"Keyboard"</c> or the <c>"Gamepad"</c> binding group
        /// listed in <see cref="InputBinding.groups"/>.</param>
        /// <param name="path">Only override bindings that have this exact path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Calling this method is equivalent to calling <see cref="ApplyBindingOverride(InputAction,InputBinding)"/>
        /// with the properties of the given <see cref="InputBinding"/> initialized accordingly.
        ///
        /// <example>
        /// <code>
        /// // Override the binding to the gamepad A button with a binding to
        /// // the Y button.
        /// fireAction.ApplyBindingOverride("&lt;Gamepad&gt;/buttonNorth",
        ///     path: "&lt;Gamepad&gt;/buttonSouth);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="ApplyBindingOverride(InputAction,InputBinding)"/>
        /// <seealso cref="InputBinding.effectivePath"/>
        /// <seealso cref="InputBinding.overridePath"/>
        /// <seealso cref="InputBinding.Matches"/>
        public static void ApplyBindingOverride(this InputAction action, string newPath, string group = null, string path = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            ApplyBindingOverride(action, new InputBinding {overridePath = newPath, groups = group, path = path});
        }

        /// <summary>
        /// Apply overrides to all bindings on <paramref name="action"/> that match <paramref name="bindingOverride"/>.
        /// The override values are taken from <see cref="InputBinding.overridePath"/>, <see cref="InputBinding.overrideProcessors"/>,
        /// and <seealso cref="InputBinding.overrideInteractions"/> on <paramref name="bindingOverride"/>.
        /// </summary>
        /// <param name="action">Action to override bindings on.</param>
        /// <param name="bindingOverride">A binding that both acts as a mask (see <see cref="InputBinding.Matches"/>)
        /// on the bindings to <paramref name="action"/> and as a container for the override values.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The method will go through all of the bindings for <paramref name="action"/> (i.e. its <see cref="InputAction.bindings"/>)
        /// and call <see cref="InputBinding.Matches"/> on them with <paramref name="bindingOverride"/>.
        /// For every binding that returns <c>true</c> from <c>Matches</c>, the override values from the
        /// binding (i.e. <see cref="InputBinding.overridePath"/>, <see cref="InputBinding.overrideProcessors"/>,
        /// and <see cref="InputBinding.overrideInteractions"/>) are copied into the binding.
        ///
        /// Binding overrides are non-destructive. They do not change the bindings set up for an action
        /// but rather apply non-destructive modifications that change the paths of existing bindings.
        /// However, this also means that for overrides to work, there have to be existing bindings that
        /// can be modified.
        ///
        /// This is achieved by setting <see cref="InputBinding.overridePath"/> which is a non-serialized
        /// property. When resolving bindings, the system will use <see cref="InputBinding.effectivePath"/>
        /// which uses <see cref="InputBinding.overridePath"/> if set or <see cref="InputBinding.path"/>
        /// otherwise. The same applies to <see cref="InputBinding.effectiveProcessors"/> and <see
        /// cref="InputBinding.effectiveInteractions"/>.
        ///
        /// <example>
        /// <code>
        /// // Override the binding in the "KeyboardMouse" group on 'fireAction'
        /// // by setting its override binding path to the space bar on the keyboard.
        /// fireAction.ApplyBindingOverride(new InputBinding
        /// {
        ///     groups = "KeyboardMouse",
        ///     overridePath = "&lt;Keyboard&gt;/space"
        /// });
        /// </code>
        /// </example>
        ///
        /// If the given action is enabled when calling this method, the effect will be immediate,
        /// i.e. binding resolution takes place and <see cref="InputAction.controls"/> are updated.
        /// If the action is not enabled, binding resolution is deferred to when controls are needed
        /// next (usually when either <see cref="InputAction.controls"/> is queried or when the
        /// action is enabled).
        /// </remarks>
        /// <seealso cref="InputAction.bindings"/>
        /// <seealso cref="InputBinding.Matches"/>
        public static void ApplyBindingOverride(this InputAction action, InputBinding bindingOverride)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            bindingOverride.action = action.name;
            var actionMap = action.GetOrCreateActionMap();
            ApplyBindingOverride(actionMap, bindingOverride);
        }

        /// <summary>
        /// Apply a binding override to the Nth binding on the given action.
        /// </summary>
        /// <param name="action">Action to apply the binding override to.</param>
        /// <param name="bindingIndex">Index of the binding in <see cref="InputAction.bindings"/> to
        /// which to apply the override to.</param>
        /// <param name="bindingOverride">A binding that specifies the overrides to apply. In particular,
        /// the <see cref="InputBinding.overridePath"/>, <see cref="InputBinding.overrideProcessors"/>, and
        /// <see cref="InputBinding.overrideInteractions"/> properties will be copied into the binding
        /// in <see cref="InputAction.bindings"/>. The remaining fields will be ignored by this method.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bindingIndex"/> is out of range.</exception>
        /// <remarks>
        /// Unlike <see cref="ApplyBindingOverride(InputAction,InputBinding)"/> this method will
        /// not use <see cref="InputBinding.Matches"/> to determine which binding to apply the
        /// override to. Instead, it will apply the override to the binding at the given index
        /// and to that binding alone.
        ///
        /// The remaining details of applying overrides are identical to <see
        /// cref="ApplyBindingOverride(InputAction,InputBinding)"/>.
        ///
        /// Note that calling this method with an empty (default-constructed) <paramref name="bindingOverride"/>
        /// is equivalent to resetting all overrides on the given binding.
        ///
        /// <example>
        /// <code>
        /// // Reset the overrides on the second binding on 'fireAction'.
        /// fireAction.ApplyBindingOverride(1, default);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="ApplyBindingOverride(InputAction,InputBinding)"/>
        public static void ApplyBindingOverride(this InputAction action, int bindingIndex, InputBinding bindingOverride)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var indexOnMap = action.BindingIndexOnActionToBindingIndexOnMap(bindingIndex);
            bindingOverride.action = action.name;
            ApplyBindingOverride(action.GetOrCreateActionMap(), indexOnMap, bindingOverride);
        }

        /// <summary>
        /// Apply a binding override to the Nth binding on the given action.
        /// </summary>
        /// <param name="action">Action to apply the binding override to.</param>
        /// <param name="bindingIndex">Index of the binding in <see cref="InputAction.bindings"/> to
        /// which to apply the override to.</param>
        /// <param name="path">Override path (<see cref="InputBinding.overridePath"/>) to set on
        /// the given binding in <see cref="InputAction.bindings"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bindingIndex"/> is out of range.</exception>
        /// <remarks>
        /// Calling this method is equivalent to calling <see cref="ApplyBindingOverride(InputAction,int,InputBinding)"/>
        /// like so:
        ///
        /// <example>
        /// <code>
        /// action.ApplyBindingOverride(new InputBinding { overridePath = path });
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="ApplyBindingOverride(InputAction,int,InputBinding)"/>
        public static void ApplyBindingOverride(this InputAction action, int bindingIndex, string path)
        {
            if (path == null)
                throw new ArgumentException("Binding path cannot be null", nameof(path));
            ApplyBindingOverride(action, bindingIndex, new InputBinding {overridePath = path});
        }

        /// <summary>
        /// Apply the given binding override to all bindings in the map that are matched by the override.
        /// </summary>
        /// <param name="actionMap"></param>
        /// <param name="bindingOverride"></param>
        /// <returns>The number of bindings overridden in the given map.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="actionMap"/> is <c>null</c>.</exception>
        public static int ApplyBindingOverride(this InputActionMap actionMap, InputBinding bindingOverride)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));

            var bindings = actionMap.m_Bindings;
            if (bindings == null)
                return 0;

            // Go through all bindings in the map and match them to the override.
            var bindingCount = bindings.Length;
            var matchCount = 0;
            for (var i = 0; i < bindingCount; ++i)
            {
                if (!bindingOverride.Matches(ref bindings[i]))
                    continue;

                // Set overrides on binding.
                bindings[i].overridePath = bindingOverride.overridePath;
                bindings[i].overrideInteractions = bindingOverride.overrideInteractions;
                bindings[i].overrideProcessors = bindingOverride.overrideProcessors;
                ++matchCount;
            }

            if (matchCount > 0)
            {
                actionMap.ClearPerActionCachedBindingData();
                actionMap.LazyResolveBindings();
            }

            return matchCount;
        }

        public static void ApplyBindingOverride(this InputActionMap actionMap, int bindingIndex, InputBinding bindingOverride)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));
            var bindingsCount = actionMap.m_Bindings?.Length ?? 0;
            if (bindingIndex < 0 || bindingIndex >= bindingsCount)
                throw new ArgumentOutOfRangeException(nameof(bindingIndex),
                    $"Cannot apply override to binding at index {bindingIndex} in map '{actionMap}' with only {bindingsCount} bindings");

            actionMap.m_Bindings[bindingIndex].overridePath = bindingOverride.overridePath;
            actionMap.m_Bindings[bindingIndex].overrideInteractions = bindingOverride.overrideInteractions;
            actionMap.m_Bindings[bindingIndex].overrideProcessors = bindingOverride.overrideProcessors;

            actionMap.ClearPerActionCachedBindingData();
            actionMap.LazyResolveBindings();
        }

        /// <summary>
        /// Remove any overrides from the binding on <paramref name="action"/> with the given index.
        /// </summary>
        /// <param name="action">Action whose bindings to modify.</param>
        /// <param name="bindingIndex">Index of the binding within <paramref name="action"/>'s <see cref="InputAction.bindings"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bindingIndex"/> is invalid.</exception>
        public static void RemoveBindingOverride(this InputAction action, int bindingIndex)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            action.ApplyBindingOverride(bindingIndex, default(InputBinding));
        }

        /// <summary>
        /// Remove any overrides from the binding on <paramref name="action"/> matching the given binding mask.
        /// </summary>
        /// <param name="action">Action whose bindings to modify.</param>
        /// <param name="bindingMask">Mask that will be matched against the bindings on <paramref name="action"/>. All bindings
        /// that match the mask (see <see cref="InputBinding.Matches"/>) will have their overrides removed. If none of the
        /// bindings on the action match the mask, no bindings will be modified.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <remarks>
        /// <example>
        /// <code>
        /// // Remove all binding overrides from bindings associated with the "Gamepad" binding group.
        /// myAction.RemoveBindingOverride(InputBinding.MaskByGroup("Gamepad"));
        /// </code>
        /// </example>
        /// </remarks>
        public static void RemoveBindingOverride(this InputAction action, InputBinding bindingMask)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            bindingMask.overridePath = null;
            bindingMask.overrideInteractions = null;
            bindingMask.overrideProcessors = null;

            // Simply apply but with a null binding.
            ApplyBindingOverride(action, bindingMask);
        }

        private static void RemoveBindingOverride(this InputActionMap actionMap, InputBinding bindingMask)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));

            bindingMask.overridePath = null;
            bindingMask.overrideInteractions = null;
            bindingMask.overrideProcessors = null;

            // Simply apply but with a null binding.
            ApplyBindingOverride(actionMap, bindingMask);
        }

        /// <summary>
        /// Remove all binding overrides on <paramref name="action"/>, i.e. clear all <see cref="InputBinding.overridePath"/>,
        /// <see cref="InputBinding.overrideProcessors"/>, and <see cref="InputBinding.overrideInteractions"/> set on bindings
        /// for the given action.
        /// </summary>
        /// <param name="action">Action to remove overrides from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        public static void RemoveAllBindingOverrides(this InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var actionName = action.name;
            var actionMap = action.GetOrCreateActionMap();
            var bindings = actionMap.m_Bindings;
            if (bindings == null)
                return;

            var bindingCount = bindings.Length;
            for (var i = 0; i < bindingCount; ++i)
            {
                if (string.Compare(bindings[i].action, actionName, StringComparison.InvariantCultureIgnoreCase) != 0)
                    continue;

                bindings[i].overridePath = null;
                bindings[i].overrideInteractions = null;
                bindings[i].overrideProcessors = null;
            }

            actionMap.ClearPerActionCachedBindingData();
            actionMap.LazyResolveBindings();
        }

        ////REVIEW: are the IEnumerable variations worth having?

        public static void ApplyBindingOverrides(this InputActionMap actionMap, IEnumerable<InputBinding> overrides)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));
            if (overrides == null)
                throw new ArgumentNullException(nameof(overrides));


            foreach (var binding in overrides)
                ApplyBindingOverride(actionMap, binding);
        }

        public static void RemoveBindingOverrides(this InputActionMap actionMap, IEnumerable<InputBinding> overrides)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));
            if (overrides == null)
                throw new ArgumentNullException(nameof(overrides));


            foreach (var binding in overrides)
                RemoveBindingOverride(actionMap, binding);
        }

        /// <summary>
        /// Restore all bindings in the map to their defaults.
        /// </summary>
        /// <param name="actionMap">Action map to remove overrides from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="actionMap"/> is <c>null</c>.</exception>
        public static void RemoveAllBindingOverrides(this InputActionMap actionMap)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));

            if (actionMap.m_Bindings == null)
                return; // No bindings in map.

            var emptyBinding = new InputBinding();
            var bindingCount = actionMap.m_Bindings.Length;
            for (var i = 0; i < bindingCount; ++i)
                ApplyBindingOverride(actionMap, i, emptyBinding);
        }

        ////REVIEW: how does this system work in combination with actual user overrides
        ////        (answer: we rebind based on the base path not the override path; thus user overrides are unaffected;
        ////        and hopefully operate on more than just the path; probably action+path or something)
        ////TODO: add option to suppress any non-matching binding by setting its override to an empty path
        ////TODO: need ability to do this with a list of controls
        // For all bindings in the given action, if a binding matches a control in the given control
        // hierarchy, set an override on the binding to refer specifically to that control.
        //
        // Returns the number of overrides that have been applied.
        //
        // Use case: Say you have a local co-op game and a single action map that represents the
        //           actions of any single player. To end up with action maps that are specific to
        //           a certain player, you could, for example, clone the action map four times, and then
        //           take four gamepad devices and use the methods here to have bindings be overridden
        //           on each map to refer to a specific gamepad instance.
        //
        //           Another example is having two XRControllers and two action maps can be on either hand.
        //           At runtime you can dynamically override and re-override the bindings on the action maps
        //           to use them with the controllers as desired.
        public static int ApplyBindingOverridesOnMatchingControls(this InputAction action, InputControl control)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var bindings = action.bindings;
            var bindingsCount = bindings.Count;
            var numMatchingControls = 0;

            for (var i = 0; i < bindingsCount; ++i)
            {
                var matchingControl = InputControlPath.TryFindControl(control, bindings[i].path);
                if (matchingControl == null)
                    continue;

                action.ApplyBindingOverride(i, matchingControl.path);
                ++numMatchingControls;
            }

            return numMatchingControls;
        }

        public static int ApplyBindingOverridesOnMatchingControls(this InputActionMap actionMap, InputControl control)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var actions = actionMap.actions;
            var actionCount = actions.Count;
            var numMatchingControls = 0;

            for (var i = 0; i < actionCount; ++i)
            {
                var action = actions[i];
                numMatchingControls = action.ApplyBindingOverridesOnMatchingControls(control);
            }

            return numMatchingControls;
        }

        ////TODO: allow overwriting magnitude with custom values; maybe turn more into an overall "score" for a control

        /// <summary>
        /// An ongoing rebinding operation.
        /// </summary>
        /// <remarks>
        /// This class acts as both a configuration interface for rebinds as well as a controller while
        /// the rebind is ongoing. An instance can be reused arbitrary many times. Doing so can avoid allocating
        /// additional GC memory (the class internally retains state that it can reuse for multiple rebinds).
        ///
        /// Note, however, that during rebinding it can be necessary to look at the <see cref="InputControlLayout"/>
        /// information registered in the system which means that layouts may have to be loaded. These will be
        /// cached for as long as the rebind operation is not disposed of.
        ///
        /// To reset the configuration of a rebind operation without releasing its memory, call <see cref="Reset"/>.
        ///
        /// <example>
        /// <code>
        /// var rebind = new RebindingOperation()
        ///     .WithAction(myAction)
        ///     .WithBindingGroup("Gamepad")
        ///     .WithCancelingThrough("&lt;Keyboard&gt;/escape");
        ///
        /// rebind.Start();
        /// </code>
        /// </example>
        ///
        /// Note that instances of this class <em>must</em> be disposed of to not leak memory on the unmanaged heap.
        /// </remarks>
        /// <seealso cref="InputActionRebindingExtensions.PerformInteractiveRebinding"/>
        public sealed class RebindingOperation : IDisposable
        {
            public const float kDefaultMagnitudeThreshold = 0.2f;

            /// <summary>
            /// The action that rebinding is being performed on.
            /// </summary>
            /// <seealso cref="WithAction"/>
            public InputAction action => m_ActionToRebind;

            /// <summary>
            /// Optional mask to determine which bindings to apply overrides to.
            /// </summary>
            /// <remarks>
            /// If this is not null, all bindings that match this mask will have overrides applied to them.
            /// </remarks>
            public InputBinding? bindingMask => m_BindingMask;

            ////REVIEW: exposing this as InputControlList is very misleading as users will not get an error when modifying the list;
            ////        however, exposing through an interface will lead to boxing...
            /// <summary>
            /// Controls that had input and were deemed potential matches to rebind to.
            /// </summary>
            /// <remarks>
            /// Controls in the list should be ordered by priority with the first element in the list being
            /// considered the best match.
            /// </remarks>
            /// <seealso cref="AddCandidate"/>
            /// <seealso cref="RemoveCandidate"/>
            public InputControlList<InputControl> candidates => m_Candidates;

            /// <summary>
            /// The matching score for each control in <see cref="candidates"/>.
            /// </summary>
            /// <remarks>
            /// </remarks>
            public ReadOnlyArray<float> scores => new ReadOnlyArray<float>(m_Scores, 0, m_Candidates.Count);

            /// <summary>
            /// The control currently deemed the best candidate.
            /// </summary>
            /// <value>Primary candidate control at this point.</value>
            /// <remarks>
            /// If there are no candidates yet, this returns <c>null</c>. If there are candidates,
            /// it returns the first element of <see cref="candidates"/> which is always the control
            /// with the highest matching score.
            /// </remarks>
            public InputControl selectedControl
            {
                get
                {
                    if (m_Candidates.Count == 0)
                        return null;

                    return m_Candidates[0];
                }
            }

            /// <summary>
            /// Whether the rebind is currently in progress.
            /// </summary>
            /// <value>Whether rebind is in progress.</value>
            /// <remarks>
            /// This is true after calling <see cref="Start"/> and set to false when
            /// <see cref="OnComplete"/> or <see cref="OnCancel"/> is called.
            /// </remarks>
            /// <seealso cref="Start"/>
            /// <seealso cref="completed"/>
            /// <seealso cref="canceled"/>
            public bool started => (m_Flags & Flags.Started) != 0;

            /// <summary>
            /// Whether the rebind has been completed.
            /// </summary>
            /// <value>True if the rebind has been completed.</value>
            /// <seealso cref="OnComplete(Action{RebindingOperation})"/>
            public bool completed => (m_Flags & Flags.Completed) != 0;

            public bool canceled => (m_Flags & Flags.Canceled) != 0;

            public double startTime => m_StartTime;

            public float timeout => m_Timeout;

            public string expectedControlType => m_ExpectedLayout;

            /// <summary>
            /// Perform rebinding on the bindings of the given action.
            /// </summary>
            /// <param name="action">Action to perform rebinding on.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// Note that by default, a rebind does not have a binding mask or any other setting
            /// that constrains which binding the rebind is applied to. This means that if the action
            /// has multiple bindings, all of them will have overrides applied to them.
            ///
            /// To target specific bindings, either set a binding index with <see cref="WithTargetBinding"/>,
            /// or set a binding mask with <see cref="WithBindingMask"/> or <see cref="WithBindingGroup"/>.
            ///
            /// If the action has an associated <see cref="InputAction.expectedControlType"/> set,
            /// it will automatically be passed to <see cref="WithExpectedControlType(string)"/>.
            /// </remarks>
            /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
            /// <exception cref="InvalidOperationException"><paramref name="action"/> is currently enabled.</exception>
            /// <seealso cref="PerformInteractiveRebinding"/>
            public RebindingOperation WithAction(InputAction action)
            {
                ThrowIfRebindInProgress();

                if (action == null)
                    throw new ArgumentNullException(nameof(action));
                if (action.enabled)
                    throw new InvalidOperationException($"Cannot rebind action '{action}' while it is enabled");

                m_ActionToRebind = action;

                // If the action has an associated expected layout, constrain ourselves by it.
                // NOTE: We do *NOT* translate this to a control type and constrain by that as a whole chain
                //       of derived layouts may share the same control type.
                if (!string.IsNullOrEmpty(action.expectedControlType))
                    WithExpectedControlType(action.expectedControlType);
                else if (action.type == InputActionType.Button)
                    WithExpectedControlType("Button");

                return this;
            }

            /// <summary>
            /// Prevent all input events that have input matching the rebind operation's configuration from reaching
            /// its targeted <see cref="InputDevice"/>s and thus taking effect.
            /// </summary>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// While rebinding interactively, it is usually for the most part undesirable for input to actually have an effect.
            /// For example, when rebind gamepad input, pressing the "A" button should not lead to a "submit" action in the UI.
            /// For this reason, a rebind can be configured to automatically swallow any input event except the ones having
            /// input on controls matching <see cref="WithControlsExcluding"/>.
            ///
            /// Not at all input necessarily should be suppressed. For example, it can be desirable to have UI that
            /// allows the user to cancel an ongoing rebind by clicking with the mouse. This means that mouse position and
            /// click input should come through. For this reason, input from controls matching <see cref="WithControlsExcluding"/>
            /// is still let through.
            /// </remarks>
            public RebindingOperation WithMatchingEventsBeingSuppressed(bool value = true)
            {
                if (value)
                    m_Flags |= Flags.SuppressMatchingEvents;
                else
                    m_Flags &= ~Flags.SuppressMatchingEvents;
                return this;
            }

            /// <summary>
            /// Set the control path that is matched against actuated controls.
            /// </summary>
            /// <param name="binding">A control path (see <see cref="InputControlPath"/>) such as <c>"&lt;Keyboard&gt;/escape"</c>.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// Note that every rebind operation has only one such path. Calling this method repeatedly will overwrite
            /// the path set from prior calls.
            ///
            /// <code>
            /// var rebind = new RebindingOperation();
            ///
            /// // Cancel from keyboard escape key.
            /// rebind
            ///     .WithCancelingThrough("&lt;Keyboard&gt;/escape");
            ///
            /// // Cancel from any control with "Cancel" usage.
            /// // NOTE: This can be dangerous. The control that the wants to bind to may have the "Cancel"
            /// //       usage assigned to it, thus making it impossible for the user to bind to the control.
            /// rebind
            ///     .WithCancelingThrough("*/{Cancel}");
            /// </code>
            /// </remarks>
            public RebindingOperation WithCancelingThrough(string binding)
            {
                m_CancelBinding = binding;
                return this;
            }

            public RebindingOperation WithCancelingThrough(InputControl control)
            {
                if (control == null)
                    throw new ArgumentNullException(nameof(control));

                return WithCancelingThrough(control.path);
            }

            public RebindingOperation WithExpectedControlType(string layoutName)
            {
                m_ExpectedLayout = new InternedString(layoutName);
                return this;
            }

            public RebindingOperation WithExpectedControlType(Type type)
            {
                if (type != null && !typeof(InputControl).IsAssignableFrom(type))
                    throw new ArgumentException($"Type '{type.Name}' is not an InputControl", "type");
                m_ControlType = type;
                return this;
            }

            public RebindingOperation WithExpectedControlType<TControl>()
                where TControl : InputControl
            {
                return WithExpectedControlType(typeof(TControl));
            }

            ////TODO: allow targeting bindings by name (i.e. be able to say WithTargetBinding("Left"))
            public RebindingOperation WithTargetBinding(int bindingIndex)
            {
                m_TargetBindingIndex = bindingIndex;
                return this;
            }

            public RebindingOperation WithBindingMask(InputBinding? bindingMask)
            {
                m_BindingMask = bindingMask;
                return this;
            }

            public RebindingOperation WithBindingGroup(string group)
            {
                return WithBindingMask(new InputBinding {groups = group});
            }

            /// <summary>
            /// Disable the default behavior of automatically generalizing the path of a selected control.
            /// </summary>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// At runtime, every <see cref="InputControl"/> has a unique path in the system (<see cref="InputControl.path"/>).
            /// However, when performing rebinds, we are not generally interested in the specific runtime path of the
            /// control -- which may depend on the number and types of devices present. In fact, most of the time we are not
            /// even interested in what particular brand of device the user is rebinding to but rather want to just bind based
            /// on the device's broad category.
            ///
            /// For example, if the user has a DualShock controller and performs an interactive rebind, we usually do not want
            /// to generate override paths that reflects that the input specifically came from a DualShock controller. Rather,
            /// we're usually interested in the fact that it came from a gamepad.
            /// </remarks>
            /// <seealso cref="InputBinding.overridePath"/>
            public RebindingOperation WithoutGeneralizingPathOfSelectedControl()
            {
                m_Flags |= Flags.DontGeneralizePathOfSelectedControl;
                return this;
            }

            public RebindingOperation WithRebindAddingNewBinding(string group = null)
            {
                m_Flags |= Flags.AddNewBinding;
                m_BindingGroupForNewBinding = group;
                return this;
            }

            /// <summary>
            /// Require actuation of controls to exceed a certain level.
            /// </summary>
            /// <param name="magnitude">Minimum magnitude threshold that has to be reached on a control
            /// for it to be considered a candidate. See <see cref="InputControl.EvaluateMagnitude()"/> for
            /// details about magnitude evaluations.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <exception cref="ArgumentException"><paramref name="magnitude"/> is negative.</exception>
            /// <remarks>
            /// Rebind operations use a default threshold of 0.2. This means that the actuation level
            /// of any control as returned by <see cref="InputControl.EvaluateMagnitude()"/> must be equal
            /// or greater than 0.2 for it to be considered a potential candidate. This helps filter out
            /// controls that are actuated incidentally as part of actuating other controls.
            ///
            /// For example, if the player wants to bind an action to the X axis of the gamepad's right
            /// stick, the player will almost unavoidably also actuate the Y axis to a certain degree.
            /// However, if actuation of the Y axis stays under 2.0, it will automatically get filtered out.
            ///
            /// Note that the magnitude threshold is not the only mechanism that helps trying to find
            /// the most actuated control. In fact, all controls will eventually be sorted by magnitude
            /// of actuation so even if both X and Y of a stick make it into the candidate list, if X
            /// is actuated more strongly than Y, it will be favored.
            ///
            /// Note that you can also use this method to <em>lower</em> the default threshold of 0.2
            /// in case you want more controls to make it through the matching process.
            /// </remarks>
            public RebindingOperation WithMagnitudeHavingToBeGreaterThan(float magnitude)
            {
                if (magnitude < 0)
                    throw new ArgumentException($"Magnitude has to be positive but was {magnitude}",
                        nameof(magnitude));
                m_MagnitudeThreshold = magnitude;
                return this;
            }

            /// <summary>
            /// Do not ignore input from noisy controls.
            /// </summary>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// By default, noisy controls are ignored for rebinds. This means that, for example, a gyro
            /// inside a gamepad will not be considered as a potential candidate control as it is hard
            /// to tell valid user interaction on the control apart from random jittering that occurs
            /// on noisy controls.
            ///
            /// By calling this method, this behavior can be disabled. This is usually only useful when
            /// implementing custom candidate selection through <see cref="OnPotentialMatch"/>.
            /// </remarks>
            /// <seealso cref="InputControl.noisy"/>
            public RebindingOperation WithoutIgnoringNoisyControls()
            {
                m_Flags |= Flags.DontIgnoreNoisyControls;
                return this;
            }

            /// <summary>
            /// Restrict candidate controls using a control path (see <see cref="InputControlPath"/>).
            /// </summary>
            /// <param name="path">A control path. See <see cref="InputControlPath"/>.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c> or empty.</exception>
            /// <remarks>
            /// This method is most useful to, for example, restrict controls to specific types of devices.
            /// If, say, you want to let the player only bind to gamepads, you can do so using
            ///
            /// <example>
            /// <code>
            /// rebind.WithControlsHavingToMatchPath("&lt;Gamepad&gt;");
            /// </code>
            /// </example>
            ///
            /// This method can be called repeatedly to add multiple paths. The effect is that candidates
            /// are accepted if <em>any</em> of the given paths matches. To reset the list, call <see
            /// cref="Reset"/>.
            /// </remarks>
            public RebindingOperation WithControlsHavingToMatchPath(string path)
            {
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentNullException(nameof(path));
                for (var i = 0; i < m_IncludePathCount; ++i)
                    if (string.Compare(m_IncludePaths[i], path, StringComparison.InvariantCultureIgnoreCase) == 0)
                        return this;
                ArrayHelpers.AppendWithCapacity(ref m_IncludePaths, ref m_IncludePathCount, path);
                return this;
            }

            /// <summary>
            /// Prevent specific controls from being considered as candidate controls.
            /// </summary>
            /// <param name="path">A control path. See <see cref="InputControlPath"/>.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c> or empty.</exception>
            /// <remarks>
            /// Some controls can be undesirable to include in the candidate selection process even
            /// though they constitute valid, non-noise user input. For example, in a desktop application,
            /// the mouse will usually be used to navigate the UI including a rebinding UI that makes
            /// use of RebindingOperation. It can thus be advisable to exclude specific pointer controls
            /// like so:
            ///
            /// <example>
            /// <code>
            /// rebind
            ///     .WithControlsExcluding("&lt;Pointer&gt;/position") // Don't bind to mouse position
            ///     .WithControlsExcluding("&lt;Pointer&gt;/delta") // Don't bind to mouse movement deltas
            ///     .WithControlsExcluding("&lt;Pointer&gt;/{PrimaryAction}") // don't bind to controls such as leftButton and taps.
            /// </code>
            /// </example>
            ///
            /// This method can be called repeatedly to add multiple exclusions. To reset the list,
            /// call <see cref="Reset"/>.
            /// </remarks>
            public RebindingOperation WithControlsExcluding(string path)
            {
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentNullException(nameof(path));
                for (var i = 0; i < m_ExcludePathCount; ++i)
                    if (string.Compare(m_ExcludePaths[i], path, StringComparison.InvariantCultureIgnoreCase) == 0)
                        return this;
                ArrayHelpers.AppendWithCapacity(ref m_ExcludePaths, ref m_ExcludePathCount, path);
                return this;
            }

            public RebindingOperation WithTimeout(float timeInSeconds)
            {
                m_Timeout = timeInSeconds;
                return this;
            }

            public RebindingOperation OnComplete(Action<RebindingOperation> callback)
            {
                m_OnComplete = callback;
                return this;
            }

            public RebindingOperation OnCancel(Action<RebindingOperation> callback)
            {
                m_OnCancel = callback;
                return this;
            }

            public RebindingOperation OnPotentialMatch(Action<RebindingOperation> callback)
            {
                m_OnPotentialMatch = callback;
                return this;
            }

            /// <summary>
            /// Set function to call when generating the final binding path for a control
            /// that has been selected.
            /// </summary>
            /// <param name="callback">Delegate to call </param>
            /// <returns></returns>
            public RebindingOperation OnGeneratePath(Func<InputControl, string> callback)
            {
                m_OnGeneratePath = callback;
                return this;
            }

            public RebindingOperation OnComputeScore(Func<InputControl, InputEventPtr, float> callback)
            {
                m_OnComputeScore = callback;
                return this;
            }

            public RebindingOperation OnApplyBinding(Action<RebindingOperation, string> callback)
            {
                m_OnApplyBinding = callback;
                return this;
            }

            public RebindingOperation OnMatchWaitForAnother(float seconds)
            {
                m_WaitSecondsAfterMatch = seconds;
                return this;
            }

            public RebindingOperation Start()
            {
                // Ignore if already started.
                if (started)
                    return this;

                // Make sure our configuration is sound.
                if (m_ActionToRebind != null && m_ActionToRebind.bindings.Count == 0 && (m_Flags & Flags.AddNewBinding) == 0)
                    throw new InvalidOperationException(
                        $"Action '{action}' must have at least one existing binding or must be used with WithRebindingAddNewBinding()");
                if (m_ActionToRebind == null && m_OnApplyBinding == null)
                    throw new InvalidOperationException(
                        "Must either have an action (call WithAction()) to apply binding to or have a custom callback to apply the binding (call OnApplyBinding())");

                m_StartTime = InputRuntime.s_Instance.currentTime;

                if (m_WaitSecondsAfterMatch > 0 || m_Timeout > 0)
                {
                    HookOnAfterUpdate();
                    m_LastMatchTime = -1;
                }

                HookOnEvent();

                m_Flags |= Flags.Started;
                m_Flags &= ~Flags.Canceled;
                m_Flags &= ~Flags.Completed;

                return this;
            }

            public void Cancel()
            {
                if (!started)
                    return;

                OnCancel();
            }

            /// <summary>
            /// Manually complete the rebinding operation.
            /// </summary>
            public void Complete()
            {
                if (!started)
                    return;

                OnComplete();
            }

            public void AddCandidate(InputControl control, float score)
            {
                if (control == null)
                    throw new ArgumentNullException(nameof(control));

                // If it's already added, update score.
                var index = m_Candidates.IndexOf(control);
                if (index != -1)
                {
                    m_Scores[index] = score;
                }
                else
                {
                    // Otherwise, add it.
                    var candidateCount = m_Candidates.Count;
                    m_Candidates.Add(control);
                    ArrayHelpers.AppendWithCapacity(ref m_Scores, ref candidateCount, score);
                }

                SortCandidatesByScore();
            }

            public void RemoveCandidate(InputControl control)
            {
                if (control == null)
                    throw new ArgumentNullException(nameof(control));

                var index = m_Candidates.IndexOf(control);
                if (index == -1)
                    return;

                var candidateCount = m_Candidates.Count;
                m_Candidates.RemoveAt(index);
                ArrayHelpers.EraseAtWithCapacity(m_Scores, ref candidateCount, index);
            }

            public void Dispose()
            {
                UnhookOnEvent();
                UnhookOnAfterUpdate();
                m_Candidates.Dispose();
                m_LayoutCache.Clear();
            }

            ~RebindingOperation()
            {
                Dispose();
            }

            /// <summary>
            /// Reset the configuration on the rebind.
            /// </summary>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// Call this method to reset the effects of calling methods such as <see cref="WithAction"/>,
            /// <see cref="WithBindingGroup"/>, etc. but retain other data that the rebind operation
            /// may have allocated already. If you are reusing the same <c>RebindingOperation</c>
            /// multiple times, a good strategy is to reset and reconfigure the operation before starting
            /// it again.
            /// </remarks>
            public RebindingOperation Reset()
            {
                m_ActionToRebind = default;
                m_BindingMask = default;
                m_ControlType = default;
                m_ExpectedLayout = default;
                m_IncludePathCount = default;
                m_ExcludePathCount = default;
                m_TargetBindingIndex = -1;
                m_BindingGroupForNewBinding = default;
                m_CancelBinding = default;
                m_MagnitudeThreshold = kDefaultMagnitudeThreshold;
                m_Timeout = default;
                m_WaitSecondsAfterMatch = default;
                m_Flags = default;
                return this;
            }

            private void HookOnEvent()
            {
                if ((m_Flags & Flags.OnEventHooked) != 0)
                    return;

                if (m_OnEventDelegate == null)
                    m_OnEventDelegate = OnEvent;

                InputSystem.onEvent += m_OnEventDelegate;
                m_Flags |= Flags.OnEventHooked;
            }

            private void UnhookOnEvent()
            {
                if ((m_Flags & Flags.OnEventHooked) == 0)
                    return;

                InputSystem.onEvent -= m_OnEventDelegate;
                m_Flags &= ~Flags.OnEventHooked;
            }

            private unsafe void OnEvent(InputEventPtr eventPtr, InputDevice device)
            {
                // Ignore if not a state event.
                if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                    return;

                // Go through controls and see if there's anything interesting in the event.
                var controls = device.allControls;
                var controlCount = controls.Count;
                var haveChangedCandidates = false;
                var suppressEvent = false;
                for (var i = 0; i < controlCount; ++i)
                {
                    var control = controls[i];

                    // Skip controls that have no state in the event.
                    var statePtr = control.GetStatePtrFromStateEvent(eventPtr);
                    if (statePtr == null)
                        continue;

                    // If the control that cancels has been actuated, abort the operation now.
                    if (!string.IsNullOrEmpty(m_CancelBinding) && InputControlPath.Matches(m_CancelBinding, control) &&
                        !control.CheckStateIsAtDefault(statePtr) && control.HasValueChangeInState(statePtr))
                    {
                        OnCancel();
                        break;
                    }

                    // Skip noisy controls.
                    if (control.noisy && (m_Flags & Flags.DontIgnoreNoisyControls) == 0)
                        continue;

                    // If controls must not match certain path, make sure the control doesn't.
                    if (m_ExcludePathCount > 0 && HavePathMatch(control, m_ExcludePaths, m_ExcludePathCount))
                        continue;

                    // The control is not explicitly excluded so we suppress the event, if that's enabled.
                    suppressEvent = true;

                    // If controls have to match a certain path, check if this one does.
                    if (m_IncludePathCount > 0 && !HavePathMatch(control, m_IncludePaths, m_IncludePathCount))
                        continue;

                    // If we're expecting controls of a certain type, skip if control isn't of
                    // the right type.
                    if (m_ControlType != null && !m_ControlType.IsInstanceOfType(control))
                        continue;

                    // If we're expecting controls to be based on a specific layout, skip if control
                    // isn't based on that layout.
                    if (!m_ExpectedLayout.IsEmpty() &&
                        m_ExpectedLayout != control.m_Layout &&
                        !InputControlLayout.s_Layouts.IsBasedOn(m_ExpectedLayout, control.m_Layout))
                        continue;

                    // Skip controls that are in their default state.
                    // NOTE: This is the cheapest check with respect to looking at actual state. So
                    //       do this first before looking further at the state.
                    if (control.CheckStateIsAtDefault(statePtr))
                        continue;

                    var magnitude = control.EvaluateMagnitude(statePtr);
                    if (magnitude >= 0 && magnitude < m_MagnitudeThreshold)
                        continue; // No, so skip.

                    // Compute score.
                    float score;
                    if (m_OnComputeScore != null)
                        score = m_OnComputeScore(control, eventPtr);
                    else
                    {
                        score = magnitude;

                        // We don't want synthetic controls to not be bindable at all but they should
                        // generally cede priority to controls that aren't synthetic. So we bump all
                        // scores of controls that aren't synthetic.
                        if (!control.synthetic)
                            score += 1f;
                    }

                    // Control is a candidate.
                    // See if we already singled the control out as a potential candidate.
                    var candidateIndex = m_Candidates.IndexOf(control);
                    if (candidateIndex != -1)
                    {
                        // Yes, we did. So just check whether it became a better candidate than before.
                        if (m_Scores[candidateIndex] < score)
                        {
                            haveChangedCandidates = true;
                            m_Scores[candidateIndex] = score;

                            if (m_WaitSecondsAfterMatch > 0)
                                m_LastMatchTime = InputRuntime.s_Instance.currentTime;
                        }
                    }
                    else
                    {
                        // No, so add it.
                        var candidateCount = m_Candidates.Count;
                        m_Candidates.Add(control);
                        ArrayHelpers.AppendWithCapacity(ref m_Scores, ref candidateCount, score);
                        haveChangedCandidates = true;

                        if (m_WaitSecondsAfterMatch > 0)
                            m_LastMatchTime = InputRuntime.s_Instance.currentTime;
                    }
                }

                // See if we should suppress the event. If so, mark it handled so that the input manager
                // will skip further processing of the event.
                if (suppressEvent && (m_Flags & Flags.SuppressMatchingEvents) != 0)
                    eventPtr.handled = true;

                if (haveChangedCandidates && !canceled)
                {
                    // If we have a callback that wants to control matching, leave it to the callback to decide
                    // whether the rebind is complete or not. Otherwise, just complete.
                    if (m_OnPotentialMatch != null)
                    {
                        SortCandidatesByScore();
                        m_OnPotentialMatch(this);
                    }
                    else if (m_WaitSecondsAfterMatch <= 0)
                    {
                        OnComplete();
                    }
                    else
                    {
                        SortCandidatesByScore();
                    }
                }
            }

            private void SortCandidatesByScore()
            {
                var candidateCount = m_Candidates.Count;
                if (candidateCount <= 1)
                    return;

                // Simple insertion sort that sorts both m_Candidates and m_Scores at the same time.
                // Note that we're sorting by *decreasing* score here, not by increasing score.
                for (var i = 1; i < candidateCount; ++i)
                {
                    for (var j = i; j > 0 && m_Scores[j - 1] < m_Scores[j]; --j)
                    {
                        m_Scores.SwapElements(j, j - 1);
                        m_Candidates.SwapElements(j, j - 1);
                    }
                }
            }

            private static bool HavePathMatch(InputControl control, string[] paths, int pathCount)
            {
                for (var i = 0; i < pathCount; ++i)
                {
                    if (InputControlPath.MatchesPrefix(paths[i], control))
                        return true;
                }

                return false;
            }

            private void HookOnAfterUpdate()
            {
                if ((m_Flags & Flags.OnAfterUpdateHooked) != 0)
                    return;

                if (m_OnAfterUpdateDelegate == null)
                    m_OnAfterUpdateDelegate = OnAfterUpdate;

                InputSystem.onAfterUpdate += m_OnAfterUpdateDelegate;
                m_Flags |= Flags.OnAfterUpdateHooked;
            }

            private void UnhookOnAfterUpdate()
            {
                if ((m_Flags & Flags.OnAfterUpdateHooked) == 0)
                    return;

                InputSystem.onAfterUpdate -= m_OnAfterUpdateDelegate;
                m_Flags &= ~Flags.OnAfterUpdateHooked;
            }

            private void OnAfterUpdate()
            {
                // If we don't have a match yet but we have a timeout and have expired it,
                // cancel the operation.
                if (m_LastMatchTime < 0 && m_Timeout > 0 &&
                    InputRuntime.s_Instance.currentTime - m_StartTime > m_Timeout)
                {
                    Cancel();
                    return;
                }

                // Sanity check to make sure we're actually waiting for completion.
                if (m_WaitSecondsAfterMatch <= 0)
                    return;

                // Can't complete if we have no match yet.
                if (m_LastMatchTime < 0)
                    return;

                // Complete if timeout has expired.
                if (InputRuntime.s_Instance.currentTime >= m_LastMatchTime + m_WaitSecondsAfterMatch)
                    Complete();
            }

            private void OnComplete()
            {
                SortCandidatesByScore();

                if (m_Candidates.Count > 0)
                {
                    // Create a path from the selected control.
                    var selectedControl = m_Candidates[0];
                    var path = selectedControl.path;
                    if (m_OnGeneratePath != null)
                    {
                        // We have a callback. Give it a shot to generate a path. If it doesn't,
                        // fall back to our default logic.
                        var newPath = m_OnGeneratePath(selectedControl);
                        if (!string.IsNullOrEmpty(newPath))
                            path = newPath;
                        else if ((m_Flags & Flags.DontGeneralizePathOfSelectedControl) == 0)
                            path = GeneratePathForControl(selectedControl);
                    }
                    else if ((m_Flags & Flags.DontGeneralizePathOfSelectedControl) == 0)
                        path = GeneratePathForControl(selectedControl);

                    // If we have a custom callback for applying the binding, let it handle
                    // everything.
                    if (m_OnApplyBinding != null)
                        m_OnApplyBinding(this, path);
                    else
                    {
                        Debug.Assert(m_ActionToRebind != null);

                        // See if we should modify an existing binding or create a new one.
                        if ((m_Flags & Flags.AddNewBinding) != 0)
                        {
                            // Create new binding.
                            m_ActionToRebind.AddBinding(path, groups: m_BindingGroupForNewBinding);
                        }
                        else
                        {
                            // Apply binding override to existing binding.
                            if (m_TargetBindingIndex >= 0)
                            {
                                if (m_TargetBindingIndex >= m_ActionToRebind.bindings.Count)
                                    throw new InvalidOperationException(
                                        $"Target binding index {m_TargetBindingIndex} out of range for action '{m_ActionToRebind}' with {m_ActionToRebind.bindings.Count} bindings");

                                m_ActionToRebind.ApplyBindingOverride(m_TargetBindingIndex, path);
                            }
                            else if (m_BindingMask != null)
                            {
                                var bindingOverride = m_BindingMask.Value;
                                bindingOverride.overridePath = path;
                                m_ActionToRebind.ApplyBindingOverride(bindingOverride);
                            }
                            else
                            {
                                m_ActionToRebind.ApplyBindingOverride(path);
                            }
                        }
                    }
                }

                // Complete.
                m_Flags |= Flags.Completed;
                m_OnComplete?.Invoke(this);

                ResetAfterMatchCompleted();
            }

            private void OnCancel()
            {
                m_Flags |= Flags.Canceled;

                m_OnCancel?.Invoke(this);

                ResetAfterMatchCompleted();
            }

            private void ResetAfterMatchCompleted()
            {
                m_Flags &= ~Flags.Started;
                m_Candidates.Clear();
                m_Candidates.Capacity = 0; // Release our unmanaged memory.
                m_StartTime = -1;

                UnhookOnEvent();
                UnhookOnAfterUpdate();
            }

            private void ThrowIfRebindInProgress()
            {
                if (started)
                    throw new InvalidOperationException("Cannot reconfigure rebinding while operation is in progress");
            }

            /// <summary>
            /// Based on the chosen control, generate an override path to rebind to.
            /// </summary>
            private string GeneratePathForControl(InputControl control)
            {
                var device = control.device;
                Debug.Assert(control != device, "Control must not be a device");

                var deviceLayoutName =
                    InputControlLayout.s_Layouts.FindLayoutThatIntroducesControl(control, m_LayoutCache);

                if (m_PathBuilder == null)
                    m_PathBuilder = new StringBuilder();
                else
                    m_PathBuilder.Length = 0;

                control.BuildPath(deviceLayoutName, m_PathBuilder);

                return m_PathBuilder.ToString();
            }

            private InputAction m_ActionToRebind;
            private InputBinding? m_BindingMask;
            private Type m_ControlType;
            private InternedString m_ExpectedLayout;
            private int m_IncludePathCount;
            private string[] m_IncludePaths;
            private int m_ExcludePathCount;
            private string[] m_ExcludePaths;
            private int m_TargetBindingIndex = -1;
            private string m_BindingGroupForNewBinding;
            private string m_CancelBinding;
            private float m_MagnitudeThreshold = kDefaultMagnitudeThreshold;
            private float[] m_Scores; // Scores for the controls in m_Candidates.
            private double m_LastMatchTime; // Last input event time we discovered a better match.
            private double m_StartTime;
            private float m_Timeout;
            private float m_WaitSecondsAfterMatch;
            private InputControlList<InputControl> m_Candidates;
            private Action<RebindingOperation> m_OnComplete;
            private Action<RebindingOperation> m_OnCancel;
            private Action<RebindingOperation> m_OnPotentialMatch;
            private Func<InputControl, string> m_OnGeneratePath;
            private Func<InputControl, InputEventPtr, float> m_OnComputeScore;
            private Action<RebindingOperation, string> m_OnApplyBinding;
            private Action<InputEventPtr, InputDevice> m_OnEventDelegate;
            private Action m_OnAfterUpdateDelegate;
            ////TODO: use global cache
            private InputControlLayout.Cache m_LayoutCache;
            private StringBuilder m_PathBuilder;
            private Flags m_Flags;

            [Flags]
            private enum Flags
            {
                Started = 1 << 0,
                Completed = 1 << 1,
                Canceled = 1 << 2,
                OnEventHooked = 1 << 3,
                OnAfterUpdateHooked = 1 << 4,
                DontIgnoreNoisyControls = 1 << 6,
                DontGeneralizePathOfSelectedControl = 1 << 7,
                AddNewBinding = 1 << 8,
                SuppressMatchingEvents = 1 << 9,
            }
        }

        /// <summary>
        /// Initiate an operation that interactively rebinds the given action based on received input.
        /// </summary>
        /// <param name="action">Action to perform rebinding on.</param>
        /// <param name="bindingIndex">Optional index (within the <see cref="InputAction.bindings"/> array of <paramref name="action"/>)
        /// of binding to perform rebinding on. Must not be a composite binding.</param>
        /// <returns>A rebind operation configured to perform the rebind.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bindingIndex"/> is not a valid index.</exception>
        /// <exception cref="InvalidOperationException">The binding at <paramref name="bindingIndex"/> is a composite binding.</exception>
        /// <remarks>
        /// This method will automatically perform a set of configuration on the <see cref="RebindingOperation"/>
        /// based on the action and, if specified, binding.
        ///
        /// TODO
        ///
        /// Note that rebind operations must be disposed of once finished in order to not leak memory.
        ///
        /// <example>
        /// <code>
        /// // Target the first binding in the gamepad scheme.
        /// var bindingIndex = myAction.GetBindingIndex(InputBinding.MaskByGroup("Gamepad"));
        /// var rebind = myAction.PerformInteractiveRebinding(bindingIndex);
        ///
        /// // Dispose the operation on completion.
        /// rebind.OnComplete(
        ///    operation =>
        ///    {
        ///        Debug.Log($"Rebound '{myAction}' to '{operation.selectedControl}'");
        ///        operation.Dispose();
        ///    };
        ///
        /// // Start the rebind. This will cause the rebind operation to start running in the
        /// // background listening for input.
        /// rebind.Start();
        /// </code>
        /// </example>
        /// </remarks>
        public static RebindingOperation PerformInteractiveRebinding(this InputAction action, int bindingIndex = -1)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var rebind = new RebindingOperation()
                .WithAction(action)
                // Give it an ever so slight delay to make sure there isn't a better match immediately
                // following the current event.
                .OnMatchWaitForAnother(0.05f)
                // It doesn't really make sense to interactively bind pointer position input as interactive
                // rebinds are usually initiated from UIs which are operated by pointers. So exclude pointer
                // position controls by default.
                .WithControlsExcluding("<Pointer>/delta")
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Touchscreen>/touch*/position")
                .WithControlsExcluding("<Touchscreen>/touch*/delta")
                .WithControlsExcluding("<Mouse>/clickCount")
                .WithMatchingEventsBeingSuppressed();

            // If we're not looking for a button, automatically add keyboard escape key to abort rebind.
            if (rebind.expectedControlType != "Button")
                rebind.WithCancelingThrough("<Keyboard>/escape");

            if (bindingIndex >= 0)
            {
                var bindings = action.bindings;
                if (bindingIndex >= bindings.Count)
                    throw new ArgumentOutOfRangeException(
                        $"Binding index {bindingIndex} is out of range for action '{action}' with {bindings.Count} bindings",
                        nameof(bindings));
                if (bindings[bindingIndex].isComposite)
                    throw new InvalidOperationException(
                        $"Cannot perform rebinding on composite binding '{bindings[bindingIndex]}' of '{action}'");

                rebind.WithTargetBinding(bindingIndex);

                // If the binding is a part binding, switch from the action's expected control type to
                // that expected by the composite's part.
                if (bindings[bindingIndex].isPartOfComposite)
                {
                    // Search for composite.
                    var compositeIndex = bindingIndex - 1;
                    while (compositeIndex > 0 && !bindings[compositeIndex].isComposite)
                        --compositeIndex;

                    if (compositeIndex >= 0 && bindings[compositeIndex].isComposite)
                    {
                        var compositeName = bindings[compositeIndex].GetNameOfComposite();
                        var controlTypeExpectedByPart = InputBindingComposite.GetExpectedControlLayoutName(compositeName, bindings[bindingIndex].name);

                        if (!string.IsNullOrEmpty(controlTypeExpectedByPart))
                            rebind.WithExpectedControlType(controlTypeExpectedByPart);
                    }
                }

                // If the binding is part of a control scheme, only accept controls
                // that also match device requirements.
                var bindingGroups = bindings[bindingIndex].groups;
                var asset = action.actionMap?.asset;
                if (asset != null && !string.IsNullOrEmpty(action.bindings[bindingIndex].groups))
                {
                    foreach (var group in bindingGroups.Split(InputBinding.Separator))
                    {
                        var controlSchemeIndex =
                            asset.controlSchemes.IndexOf(x => group.Equals(x.bindingGroup, StringComparison.InvariantCultureIgnoreCase));
                        if (controlSchemeIndex == -1)
                            continue;

                        ////TODO: make this deal with and/or requirements

                        var controlScheme = asset.controlSchemes[controlSchemeIndex];
                        foreach (var requirement in controlScheme.deviceRequirements)
                            rebind.WithControlsHavingToMatchPath(requirement.controlPath);
                    }
                }
            }

            return rebind;
        }

        /// <summary>
        /// Temporarily suspend immediate re-resolution of bindings.
        /// </summary>
        /// <remarks>
        /// When changing control setups, it may take multiple steps to get to the final setup but each individual
        /// step may trigger bindings to be resolved again in order to update controls on actions (see <see cref="InputAction.controls"/>).
        /// Using this struct, this can be avoided and binding resolution can be deferred to after the whole operation
        /// is complete and the final binding setup is in place.
        /// </remarks>
        internal static IDisposable DeferBindingResolution()
        {
            if (s_DeferBindingResolutionWrapper == null)
                s_DeferBindingResolutionWrapper = new DeferBindingResolutionWrapper();
            s_DeferBindingResolutionWrapper.Acquire();
            return s_DeferBindingResolutionWrapper;
        }

        private static DeferBindingResolutionWrapper s_DeferBindingResolutionWrapper;

        private class DeferBindingResolutionWrapper : IDisposable
        {
            public void Acquire()
            {
                ++InputActionMap.s_DeferBindingResolution;
            }

            public void Dispose()
            {
                if (InputActionMap.s_DeferBindingResolution > 0)
                    --InputActionMap.s_DeferBindingResolution;
                InputActionState.DeferredResolutionOfBindings();
            }
        }
    }
}
