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

////TODO: make RebindingOperation dispose its memory automatically; re-allocating is not a problem

////TODO: add simple method to RebindingOperation that will create keyboard binding paths by character rather than by key name

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
        /// Get the index of the first binding in <see cref="InputActionMap.bindings"/> on <paramref name="actionMap"/>
        /// that matches the given binding mask.
        /// </summary>
        /// <param name="actionMap">An input action map.</param>
        /// <param name="bindingMask">Binding mask to match (see <see cref="InputBinding.Matches"/>).</param>
        /// <returns>The first binding on the action matching <paramref name="bindingMask"/> or -1 if no binding
        /// on the action matches the mask.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="actionMap"/> is <c>null</c>.</exception>
        /// <seealso cref="InputBinding.Matches"/>
        public static int GetBindingIndex(this InputActionMap actionMap, InputBinding bindingMask)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));

            var bindings = actionMap.bindings;
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

            var controls = state.controls;
            var controlCount = state.totalControlCount;
            var bindingStates = state.bindingStates;
            var controlIndexToBindingIndex = state.controlIndexToBindingIndex;
            var actionIndex = action.m_ActionIndexInState;

            // Go through all controls in the state until we find our control.
            for (var i = 0; i < controlCount; ++i)
            {
                if (controls[i] != control)
                    continue;

                // The control may be the same one we're looking for but may be bound to a completely
                // different action. Skip anything that isn't related to our action.
                var bindingIndexInState = controlIndexToBindingIndex[i];
                if (bindingStates[bindingIndexInState].actionIndex != actionIndex)
                    continue;

                // Got it.
                var bindingIndexInMap = state.GetBindingIndexInMap(bindingIndexInState);
                return action.BindingIndexOnMapToBindingIndexOnAction(bindingIndexInMap);
            }

            return -1;
        }

        ////TODO: add option to make it *not* take bound controls into account when creating display strings

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
                if (bindings[i].isPartOfComposite)
                    continue;
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
        /// Debug.Log(action.GetBindingDisplayString(0, out var deviceLayoutNameA, out var controlPathA));
        /// Debug.Log(deviceLayoutNameA);
        /// Debug.Log(controlPathA);
        ///
        /// // Prints "LMB", then "Mouse", then "leftButton".
        /// Debug.Log(action.GetBindingDisplayString(1, out var deviceLayoutNameB, out var controlPathB));
        /// Debug.Log(deviceLayoutNameB);
        /// Debug.Log(controlPathB);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.ToDisplayString(InputBinding.DisplayStringOptions,InputControl)"/>
        /// <seealso cref="InputControlPath.ToHumanReadableString(string,InputControlPath.HumanReadableStringOptions,InputControl)"/>
        /// <seealso cref="InputActionRebindingExtensions.GetBindingIndex(InputAction,InputBinding)"/>
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
                {
                    var partString = action.GetBindingDisplayString(firstPartIndex + i, options);
                    if (string.IsNullOrEmpty(partString))
                        partString = " ";
                    partStrings[i] = partString;
                }

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

                        if (string.IsNullOrEmpty(result))
                            result = " ";

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

            // Take interactions applied to the action into account (except if explicitly forced off).
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
        /// <param name="actionMap">An action map. Overrides will be applied to its <see cref="InputActionMap.bindings"/>.</param>
        /// <param name="bindingOverride">Binding that is matched (see <see cref="InputBinding.Matches"/>) against
        /// the <see cref="InputActionMap.bindings"/> of <paramref name="actionMap"/>. The binding's
        /// <see cref="InputBinding.overridePath"/>, <see cref="InputBinding.overrideInteractions"/>, and
        /// <see cref="InputBinding.overrideProcessors"/> properties will be copied over to any matching binding.</param>
        /// <returns>The number of bindings overridden in the given map.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="actionMap"/> is <c>null</c>.</exception>
        /// <seealso cref="InputActionMap.bindings"/>
        /// <seealso cref="InputBinding.overridePath"/>
        /// <seealso cref="InputBinding.overrideInteractions"/>
        /// <seealso cref="InputBinding.overrideProcessors"/>
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

        /// <summary>
        /// Copy the override properties (<see cref="InputBinding.overridePath"/>, <see cref="InputBinding.overrideProcessors"/>,
        /// and <see cref="InputBinding.overrideInteractions"/>) from <paramref name="bindingOverride"/> over to the
        /// binding at index <paramref name="bindingIndex"/> in <see cref="InputActionMap.bindings"/> of <paramref name="actionMap"/>.
        /// </summary>
        /// <param name="actionMap">Action map whose bindings to modify.</param>
        /// <param name="bindingIndex">Index of the binding to modify in <see cref="InputActionMap.bindings"/> of
        /// <paramref name="actionMap"/>.</param>
        /// <param name="bindingOverride">Binding whose override properties (<see cref="InputBinding.overridePath"/>,
        /// <see cref="InputBinding.overrideProcessors"/>, and <see cref="InputBinding.overrideInteractions"/>) to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="actionMap"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bindingIndex"/> is not a valid index for
        /// <see cref="InputActionMap.bindings"/> of <paramref name="actionMap"/>.</exception>
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
        /// Restore all bindings in the map to their defaults.
        /// </summary>
        /// <param name="actions">Collection of actions to remove overrides from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="actions"/> is <c>null</c>.</exception>
        /// <seealso cref="ApplyBindingOverride(InputAction,int,InputBinding)"/>
        /// <seealso cref="InputBinding.overridePath"/>
        /// <seealso cref="InputBinding.overrideInteractions"/>
        /// <seealso cref="InputBinding.overrideProcessors"/>
        public static void RemoveAllBindingOverrides(this IInputActionCollection2 actions)
        {
            if (actions == null)
                throw new ArgumentNullException(nameof(actions));

            using (DeferBindingResolution())
            {
                // Go through all actions and then through the bindings in their action maps
                // and reset the bindings for those actions. Bit of a roundabout and inefficient
                // way but should be okay. Problem is that IInputActionCollection2 doesn't give
                // us quite the same level of access as InputActionMap and InputActionAsset do.
                foreach (var action in actions)
                {
                    var actionMap = action.GetOrCreateActionMap();
                    var bindings = actionMap.m_Bindings;
                    var numBindings = bindings.LengthSafe();

                    for (var i = 0; i < numBindings; ++i)
                    {
                        ref var binding = ref bindings[i];
                        if (!binding.TriggersAction(action))
                            continue;
                        binding.RemoveOverrides();
                    }

                    actionMap.ClearPerActionCachedBindingData();
                    actionMap.LazyResolveBindings();
                }
            }
        }

        /// <summary>
        /// Remove all binding overrides on <paramref name="action"/>, i.e. clear all <see cref="InputBinding.overridePath"/>,
        /// <see cref="InputBinding.overrideProcessors"/>, and <see cref="InputBinding.overrideInteractions"/> set on bindings
        /// for the given action.
        /// </summary>
        /// <param name="action">Action to remove overrides from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <seealso cref="ApplyBindingOverride(InputAction,int,InputBinding)"/>
        /// <seealso cref="InputBinding.overridePath"/>
        /// <seealso cref="InputBinding.overrideInteractions"/>
        /// <seealso cref="InputBinding.overrideProcessors"/>
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

        ////TODO: add option to suppress any non-matching binding by setting its override to an empty path
        ////TODO: need ability to do this with a list of controls

        /// <summary>
        /// For all bindings in the <paramref name="action"/>, if a binding matches a control in the given control
        /// hierarchy, set an override on the binding to refer specifically to that control.
        /// </summary>
        /// <param name="action">An action whose bindings to modify.</param>
        /// <param name="control">A control hierarchy or an entire <see cref="InputDevice"/>.</param>
        /// <returns>The number of binding overrides that have been applied to the given action.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c> -or- <paramref name="control"/>
        /// is <c>null</c>.</exception>
        /// <remarks>
        /// This method can be used to restrict bindings that otherwise apply to a wide set of possible
        /// controls.
        ///
        /// <example>
        /// <code>
        /// // Create two gamepads.
        /// var gamepad1 = InputSystem.AddDevice&lt;Gamepad&gt;();
        /// var gamepad2 = InputSystem.AddDevice&lt;Gamepad&gt;();
        ///
        /// // Create an action that binds to the A button on gamepads.
        /// var action = new InputAction();
        /// action.AddBinding("&lt;Gamepad&gt;/buttonSouth");
        ///
        /// // When we enable the action now, it will bind to both
        /// // gamepad1.buttonSouth and gamepad2.buttonSouth.
        /// action.Enable();
        ///
        /// // But let's say we want the action to specifically work
        /// // only with the first gamepad. One way to do it is like
        /// // this:
        /// action.ApplyBindingOverridesOnMatchingControls(gamepad1);
        ///
        /// // As "&lt;Gamepad&gt;/buttonSouth" matches the gamepad1.buttonSouth
        /// // control, an override will automatically be applied such that
        /// // the binding specifically refers to that button on that gamepad.
        /// </code>
        /// </example>
        ///
        /// Note that for actions that are part of <see cref="InputActionMap"/>s and/or
        /// <see cref="InputActionAsset"/>s, it is possible to restrict actions to
        /// specific device without having to set overrides. See <see cref="InputActionMap.bindingMask"/>
        /// and <see cref="InputActionAsset.bindingMask"/>.
        /// </remarks>
        /// <seealso cref="InputActionMap.devices"/>
        /// <seealso cref="InputActionAsset.devices"/>
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

        /// <summary>
        /// For all bindings in the <paramref name="actionMap"/>, if a binding matches a control in the given control
        /// hierarchy, set an override on the binding to refer specifically to that control.
        /// </summary>
        /// <param name="actionMap">An action map whose bindings to modify.</param>
        /// <param name="control">A control hierarchy or an entire <see cref="InputDevice"/>.</param>
        /// <returns>The number of binding overrides that have been applied to the given action.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="actionMap"/> is <c>null</c> -or- <paramref name="control"/>
        /// is <c>null</c>.</exception>
        /// <remarks>
        /// This method can be used to restrict bindings that otherwise apply to a wide set of possible
        /// controls. It will go through <see cref="InputActionMap.bindings"/> and apply overrides to
        /// <example>
        /// <code>
        /// // Create two gamepads.
        /// var gamepad1 = InputSystem.AddDevice&lt;Gamepad&gt;();
        /// var gamepad2 = InputSystem.AddDevice&lt;Gamepad&gt;();
        ///
        /// // Create an action map with an action for the A and B buttons
        /// // on gamepads.
        /// var actionMap = new InputActionMap();
        /// var aButtonAction = actionMap.AddAction("a", binding: "&lt;Gamepad&gt;/buttonSouth");
        /// var bButtonAction = actionMap.AddAction("b", binding: "&lt;Gamepad&gt;/buttonEast");
        ///
        /// // When we enable the action map now, the actions will bind
        /// // to the buttons on both gamepads.
        /// actionMap.Enable();
        ///
        /// // But let's say we want the actions to specifically work
        /// // only with the first gamepad. One way to do it is like
        /// // this:
        /// actionMap.ApplyBindingOverridesOnMatchingControls(gamepad1);
        ///
        /// // Now binding overrides on the actions will be set to specifically refer
        /// // to the controls on the first gamepad.
        /// </code>
        /// </example>
        ///
        /// Note that for actions that are part of <see cref="InputActionMap"/>s and/or
        /// <see cref="InputActionAsset"/>s, it is possible to restrict actions to
        /// specific device without having to set overrides. See <see cref="InputActionMap.bindingMask"/>
        /// and <see cref="InputActionAsset.bindingMask"/>.
        ///
        /// <example>
        /// <code>
        /// // For an InputActionMap, we could alternatively just do:
        /// actionMap.devices = new InputDevice[] { gamepad1 };
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputActionMap.devices"/>
        /// <seealso cref="InputActionAsset.devices"/>
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

        /// <summary>
        /// Return a JSON string containing all overrides applied to bindings in the given set of <paramref name="actions"/>.
        /// </summary>
        /// <param name="actions">A collection of <see cref="InputAction"/>s such as an <see cref="InputActionAsset"/> or
        /// an <see cref="InputActionMap"/>.</param>
        /// <returns>A JSON string containing a serialized version of the overrides applied to bindings in the given set of actions.</returns>
        /// <remarks>
        /// This method can be used to serialize the overrides, i.e. <see cref="InputBinding.overridePath"/>,
        /// <see cref="InputBinding.overrideProcessors"/>, and <see cref="InputBinding.overrideInteractions"/>, applied to
        /// bindings in the set of actions. Only overrides will be saved.
        ///
        /// <example>
        /// <code>
        /// void SaveUserRebinds(PlayerInput player)
        /// {
        ///     var rebinds = player.actions.SaveBindingOverridesAsJson();
        ///     PlayerPrefs.SetString("rebinds", rebinds);
        /// }
        ///
        /// void LoadUserRebinds(PlayerInput player)
        /// {
        ///     var rebinds = PlayerPrefs.GetString("rebinds");
        ///     player.actions.LoadBindingOverridesFromJson(rebinds);
        /// }
        /// </code>
        /// </example>
        ///
        /// Note that this method can also be used with C# wrapper classes generated from .inputactions assets.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="actions"/> is <c>null</c>.</exception>
        /// <seealso cref="LoadBindingOverridesFromJson(IInputActionCollection2,string,bool)"/>
        public static string SaveBindingOverridesAsJson(this IInputActionCollection2 actions)
        {
            if (actions == null)
                throw new ArgumentNullException(nameof(actions));

            var overrides = new List<InputActionMap.BindingOverrideJson>();
            foreach (var binding in actions.bindings)
                actions.AddBindingOverrideJsonTo(binding, overrides);

            if (overrides.Count == 0)
                return string.Empty;

            return JsonUtility.ToJson(new InputActionMap.BindingOverrideListJson {bindings = overrides});
        }

        /// <summary>
        /// Return a string in JSON format that contains all overrides applied <see cref="InputAction.bindings"/>
        /// of <paramref name="action"/>.
        /// </summary>
        /// <param name="action">An action for which to extract binding overrides.</param>
        /// <returns>A string in JSON format containing binding overrides for <paramref name="action"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This overrides can be restored using <seealso cref="LoadBindingOverridesFromJson(InputAction,string,bool)"/>.
        /// </remarks>
        public static string SaveBindingOverridesAsJson(this InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var isSingletonAction = action.isSingletonAction;
            var actionMap = action.GetOrCreateActionMap();
            var list = new List<InputActionMap.BindingOverrideJson>();

            foreach (var binding in action.bindings)
            {
                // If we're not looking at a singleton action, the bindings in the map may be
                // for other actions. Skip all that are.
                if (!isSingletonAction && !binding.TriggersAction(action))
                    continue;

                actionMap.AddBindingOverrideJsonTo(binding, list, isSingletonAction ? action : null);
            }

            if (list.Count == 0)
                return string.Empty;

            return JsonUtility.ToJson(new InputActionMap.BindingOverrideListJson {bindings = list});
        }

        private static void AddBindingOverrideJsonTo(this IInputActionCollection2 actions, InputBinding binding,
            List<InputActionMap.BindingOverrideJson> list, InputAction action = null)
        {
            if (!binding.hasOverrides)
                return;

            ////REVIEW: should this throw if there's no existing GUID on the binding? or should we rather have
            ////        move avenues for locating a binding on an action?

            if (action == null)
                action = actions.FindAction(binding.action);

            var @override = new InputActionMap.BindingOverrideJson
            {
                action = action != null && !action.isSingletonAction ? $"{action.actionMap.name}/{action.name}" : null,
                id = binding.id.ToString(),
                path = binding.overridePath,
                interactions = binding.overrideInteractions,
                processors = binding.overrideProcessors
            };

            list.Add(@override);
        }

        /// <summary>
        /// Restore all binding overrides stored in the given JSON string to the bindings in <paramref name="actions"/>.
        /// </summary>
        /// <param name="actions">A set of actions and their bindings, such as an <see cref="InputActionMap"/>, an
        /// <see cref="InputActionAsset"/>, or a C# wrapper class generated from an .inputactions asset.</param>
        /// <param name="json">A string persisting binding overrides in JSON format. See
        /// <see cref="SaveBindingOverridesAsJson(IInputActionCollection2)"/>.</param>
        /// <param name="removeExisting">If true (default), all existing overrides present on the bindings
        /// of <paramref name="actions"/> will be removed first. If false, existing binding overrides will be left
        /// in place but may be overwritten by overrides present in <paramref name="json"/>.</param>
        /// <remarks>
        /// <example>
        /// <code>
        /// void SaveUserRebinds(PlayerInput player)
        /// {
        ///     var rebinds = player.actions.SaveBindingOverridesAsJson();
        ///     PlayerPrefs.SetString("rebinds", rebinds);
        /// }
        ///
        /// void LoadUserRebinds(PlayerInput player)
        /// {
        ///     var rebinds = PlayerPrefs.GetString("rebinds");
        ///     player.actions.LoadBindingOverridesFromJson(rebinds);
        /// }
        /// </code>
        /// </example>
        ///
        /// Note that this method can also be used with C# wrapper classes generated from .inputactions assets.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="actions"/> is <c>null</c>.</exception>
        /// <seealso cref="SaveBindingOverridesAsJson(IInputActionCollection2)"/>
        /// <seealso cref="InputBinding.overridePath"/>
        public static void LoadBindingOverridesFromJson(this IInputActionCollection2 actions, string json, bool removeExisting = true)
        {
            if (actions == null)
                throw new ArgumentNullException(nameof(actions));

            using (DeferBindingResolution())
            {
                if (removeExisting)
                    actions.RemoveAllBindingOverrides();

                actions.LoadBindingOverridesFromJsonInternal(json);
            }
        }

        /// <summary>
        /// Restore all binding overrides stored in the given JSON string to the bindings of <paramref name="action"/>.
        /// </summary>
        /// <param name="action">Action to restore bindings on.</param>
        /// <param name="json">A string persisting binding overrides in JSON format. See
        /// <see cref="SaveBindingOverridesAsJson(InputAction)"/>.</param>
        /// <param name="removeExisting">If true (default), all existing overrides present on the bindings
        /// of <paramref name="action"/> will be removed first. If false, existing binding overrides will be left
        /// in place but may be overwritten by overrides present in <paramref name="json"/>.</param>
        /// <remarks>
        /// <example>
        /// <code>
        /// void SaveUserRebinds(PlayerInput player)
        /// {
        ///     var rebinds = player.actions.SaveBindingOverridesAsJson();
        ///     PlayerPrefs.SetString("rebinds", rebinds);
        /// }
        ///
        /// void LoadUserRebinds(PlayerInput player)
        /// {
        ///     var rebinds = PlayerPrefs.GetString("rebinds");
        ///     player.actions.LoadBindingOverridesFromJson(rebinds);
        /// }
        /// </code>
        /// </example>
        ///
        /// Note that this method can also be used with C# wrapper classes generated from .inputactions assets.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="actions"/> is <c>null</c>.</exception>
        /// <seealso cref="SaveBindingOverridesAsJson(IInputActionCollection2)"/>
        /// <seealso cref="InputBinding.overridePath"/>
        public static void LoadBindingOverridesFromJson(this InputAction action, string json, bool removeExisting = true)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (DeferBindingResolution())
            {
                if (removeExisting)
                    action.RemoveAllBindingOverrides();

                action.GetOrCreateActionMap().LoadBindingOverridesFromJsonInternal(json);
            }
        }

        private static void LoadBindingOverridesFromJsonInternal(this IInputActionCollection2 actions, string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            var overrides = JsonUtility.FromJson<InputActionMap.BindingOverrideListJson>(json);
            foreach (var entry in overrides.bindings)
            {
                // Try to find the binding by ID.
                if (!string.IsNullOrEmpty(entry.id))
                {
                    var bindingIndex = actions.FindBinding(new InputBinding { m_Id = entry.id }, out var action);
                    if (bindingIndex != -1)
                    {
                        action.ApplyBindingOverride(bindingIndex, new InputBinding
                        {
                            overridePath = entry.path,
                            overrideInteractions = entry.interactions,
                            overrideProcessors = entry.processors,
                        });
                        continue;
                    }
                }

                throw new NotImplementedException();
            }
        }

        ////TODO: allow overwriting magnitude with custom values; maybe turn more into an overall "score" for a control

        /// <summary>
        /// An ongoing rebinding operation.
        /// </summary>
        /// <remarks>
        /// <example>
        /// An example for how to use this class comes with the Input System package in the form of the "Rebinding UI" sample
        /// that can be installed from the Package Manager UI in the Unity editor. The sample comes with a reusable <c>RebindActionUI</c>
        /// component that also has a dedicated custom inspector.
        /// </example>
        ///
        /// The most convenient way to use this class is by using <see cref="InputActionRebindingExtensions.PerformInteractiveRebinding"/>.
        /// This method sets up many default behaviors based on the information found in the given action.
        ///
        /// Note that instances of this class <em>must</em> be disposed of to not leak memory on the unmanaged heap.
        ///
        /// <example>
        /// <code>
        /// // A MonoBehaviour that can be hooked up to a UI.Button control.
        /// public class RebindButton : MonoBehaviour
        /// {
        ///     public InputActionReference m_Action; // Reference to an action to rebind.
        ///     public int m_BindingIndex; // Index into m_Action.bindings for binding to rebind.
        ///     public Text m_DisplayText; // Text in UI that receives the binding display string.
        ///
        ///     public void OnEnable()
        ///     {
        ///         UpdateDisplayText();
        ///     }
        ///
        ///     public void OnDisable()
        ///     {
        ///         m_Rebind?.Dispose();
        ///     }
        ///
        ///     public void OnClick()
        ///     {
        ///         var rebind = m_Action.PerformInteractiveRebinding()
        ///             .WithTargetBinding(m_BindingIndex)
        ///             .OnComplete(_ => UpdateDisplayText())
        ///             .Start();
        ///     }
        ///
        ///     private void UpdateDisplayText()
        ///     {
        ///         m_DisplayText.text = m_Action.GetBindingDisplayString(m_BindingIndex);
        ///     }
        ///
        ///     private void RebindingOperation m_Rebind;
        /// }
        ///
        /// rebind.Start();
        /// </code>
        /// </example>
        ///
        /// The goal of a rebind is always to generate a control path (see <see cref="InputControlPath"/>) usable
        /// with a binding. By default, the generated path will be installed in <see cref="InputBinding.overridePath"/>.
        /// This is non-destructive as the original path is left intact in the form of <see cref="InputBinding.path"/>.
        ///
        /// This class acts as both a configuration interface for rebinds as well as a controller while
        /// the rebind is ongoing. An instance can be reused arbitrary many times. Doing so can avoid allocating
        /// additional GC memory (the class internally retains state that it can reuse for multiple rebinds).
        ///
        /// Note, however, that during rebinding it can be necessary to look at the <see cref="InputControlLayout"/>
        /// information registered in the system which means that layouts may have to be loaded. These will be
        /// cached for as long as the rebind operation is not disposed of.
        ///
        /// To reset the configuration of a rebind operation without releasing its memory, call <see cref="Reset"/>.
        /// Note that changing configuration while a rebind is in progress in not allowed and will throw
        /// <see cref="InvalidOperationException"/>.
        ///
        /// Note that it is also possible to use this class for selecting controls interactively without also
        /// having an <see cref="InputAction"/> or even associated <see cref="InputBinding"/>s. To set this up,
        /// configure the rebind accordingly with the respective methods (such as <see cref="WithExpectedControlType{Type}"/>)
        /// and use <see cref="OnApplyBinding"/> to intercept the binding override process and instead use custom
        /// logic to do something with the resulting path (or to even just use the control list found in <see cref="candidates"/>).
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
            /// <seealso cref="scores"/>
            /// <seealso cref="magnitudes"/>
            public InputControlList<InputControl> candidates => m_Candidates;

            /// <summary>
            /// The matching score for each control in <see cref="candidates"/>.
            /// </summary>
            /// <value>A relative floating-point score for each control in <see cref="candidates"/>.</value>
            /// <remarks>
            /// Candidates are ranked and sorted by their score. By default, a score is computed for each candidate
            /// control automatically. However, this can be overridden using <see cref="OnComputeScore"/>.
            ///
            /// Default scores are directly based on magnitudes (see <see cref="InputControl.EvaluateMagnitude()"/>).
            /// The greater the magnitude of actuation, the greater the score associated with the control. This means,
            /// for example, that if both X and Y are actuated on a gamepad stick, the axis with the greater amount
            /// of actuation will get scored higher and thus be more likely to get picked.
            ///
            /// In addition, 1 is added to each default score if the respective control is non-synthetic (see <see
            /// cref="InputControl.synthetic"/>). This will give controls that correspond to actual controls present
            /// on the device precedence over those added internally. For example, if both are actuated, the synthetic
            /// <see cref="Controls.StickControl.up"/> button on stick controls will be ranked lower than the <see
            /// cref="Gamepad.buttonSouth"/> which is an actual button on the device.
            /// </remarks>
            /// <seealso cref="OnComputeScore"/>
            /// <seealso cref="candidates"/>
            /// <seealso cref="magnitudes"/>
            public ReadOnlyArray<float> scores => new ReadOnlyArray<float>(m_Scores, 0, m_Candidates.Count);

            /// <summary>
            /// The matching control actuation level (see <see cref="InputControl.EvaluateMagnitude()"/> for each control in <see cref="candidates"/>.
            /// </summary>
            /// <value><see cref="InputControl.EvaluateMagnitude()"/> result for each <see cref="InputControl"/> in <see cref="candidates"/>.</value>
            /// <remarks>
            /// This array mirrors <see cref="candidates"/>, i.e. each entry corresponds to the entry in <see cref="candidates"/> at
            /// the same index.
            /// </remarks>
            /// <seealso cref="InputControl.EvaluateMagnitude()"/>
            /// <seealso cref="candidates"/>
            /// <seealso cref="scores"/>
            public ReadOnlyArray<float> magnitudes => new ReadOnlyArray<float>(m_Magnitudes, 0, m_Candidates.Count);

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

                ////TODO: allow setting this directly from a callback
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
            /// <seealso cref="OnComplete"/>
            public bool completed => (m_Flags & Flags.Completed) != 0;

            /// <summary>
            /// Whether the rebind has been cancelled.
            /// </summary>
            /// <seealso cref="OnCancel"/>
            public bool canceled => (m_Flags & Flags.Canceled) != 0;

            public double startTime => m_StartTime;

            public float timeout => m_Timeout;

            /// <summary>
            /// Name of the control layout that the rebind is looking for.
            /// </summary>
            /// <remarks>
            /// This is optional but in general, rebinds will be more successful when the operation knows
            /// what kind of input it is looking for.
            ///
            /// If an action is supplied with <see cref="WithAction"/> (automatically done by <see cref="InputActionRebindingExtensions.PerformInteractiveRebinding"/>),
            /// the expected control type is automatically set to <see cref="InputAction.expectedControlType"/> or, if that is
            /// not set, to <c>"Button"</c> in case the action has type <see cref="InputActionType.Button"/>.
            ///
            /// If a binding is supplied with <see cref="WithTargetBinding"/> and the binding is a part binding (see <see cref="InputBinding.isPartOfComposite"/>),
            /// the expected control type is automatically set to that expected by the respective part of the composite.
            ///
            /// If this is set, any input on controls that are not of the expected type is ignored. If this is not set,
            /// any control that matches all of the other criteria is considered for rebinding.
            /// </remarks>
            /// <seealso cref="InputControl.layout"/>
            /// <seealso cref="InputAction.expectedControlType"/>
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
                ThrowIfRebindInProgress();
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
                ThrowIfRebindInProgress();
                m_CancelBinding = binding;
                return this;
            }

            public RebindingOperation WithCancelingThrough(InputControl control)
            {
                ThrowIfRebindInProgress();
                if (control == null)
                    throw new ArgumentNullException(nameof(control));
                return WithCancelingThrough(control.path);
            }

            public RebindingOperation WithExpectedControlType(string layoutName)
            {
                ThrowIfRebindInProgress();
                m_ExpectedLayout = new InternedString(layoutName);
                return this;
            }

            public RebindingOperation WithExpectedControlType(Type type)
            {
                ThrowIfRebindInProgress();
                if (type != null && !typeof(InputControl).IsAssignableFrom(type))
                    throw new ArgumentException($"Type '{type.Name}' is not an InputControl", "type");
                m_ControlType = type;
                return this;
            }

            public RebindingOperation WithExpectedControlType<TControl>()
                where TControl : InputControl
            {
                ThrowIfRebindInProgress();
                return WithExpectedControlType(typeof(TControl));
            }

            ////TODO: allow targeting bindings by name (i.e. be able to say WithTargetBinding("Left"))
            /// <summary>
            /// Rebinding a specific <see cref="InputBinding"/> on an <see cref="InputAction"/> as identified
            /// by the given index into <see cref="InputAction.bindings"/>.
            /// </summary>
            /// <param name="bindingIndex">Index into <see cref="InputAction.bindings"/> of the action supplied
            /// by <see cref="WithAction"/>.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// Note that if the given binding is a part binding of a composite (see <see cref="InputBinding.isPartOfComposite"/>),
            /// then the expected control type (see <see cref="WithExpectedControlType(string)"/>) is implicitly changed to
            /// match the type of control expected by the given part. If, for example, the composite the part belongs to
            /// is a <see cref="Composites.Vector2Composite"/>, then the expected control type is implicitly changed to
            /// <see cref="Controls.ButtonControl"/>.
            ///
            /// <example>
            /// <code>
            /// // Create an action with a WASD setup.
            /// var moveAction = new InputAction(expectedControlType: "Vector2");
            /// moveAction.AddCompositeBinding("2DVector")
            ///     .With("Up", "&lt;Keyboard&gt;/w")
            ///     .With("Down", "&lt;Keyboard&gt;/s")
            ///     .With("Left", "&lt;Keyboard&gt;/a")
            ///     .With("Right", "&lt;Keyboard&gt;/d");
            ///
            /// // Start a rebind of the "Up" binding.
            /// moveAction.PerformInteractiveRebinding()
            ///     .WithTargetBinding(1)
            ///     .Start();
            /// </code>
            /// </example>
            /// </remarks>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="bindingIndex"/> is negative.</exception>
            /// <seealso cref="WithAction"/>
            /// <seealso cref="InputAction.bindings"/>
            /// <seealso cref="WithBindingMask"/>
            /// <seealso cref="WithBindingGroup"/>
            public RebindingOperation WithTargetBinding(int bindingIndex)
            {
                if (bindingIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(bindingIndex));

                m_TargetBindingIndex = bindingIndex;

                ////REVIEW: This works nicely with this method but doesn't work as nicely with other means of selecting bindings (by group or mask).

                if (m_ActionToRebind != null && bindingIndex < m_ActionToRebind.bindings.Count)
                {
                    var binding = m_ActionToRebind.bindings[bindingIndex];

                    // If it's a composite, this also changes the type of the control we're looking for.
                    if (binding.isPartOfComposite)
                    {
                        var composite = m_ActionToRebind.ChangeBinding(bindingIndex).PreviousCompositeBinding().binding.GetNameOfComposite();
                        var partName = binding.name;
                        var expectedLayout = InputBindingComposite.GetExpectedControlLayoutName(composite, partName);
                        if (!string.IsNullOrEmpty(expectedLayout))
                            WithExpectedControlType(expectedLayout);
                    }

                    // If the binding is part of a control scheme, only accept controls
                    // that also match device requirements.
                    var asset = action.actionMap?.asset;
                    if (asset != null && !string.IsNullOrEmpty(binding.groups))
                    {
                        foreach (var group in binding.groups.Split(InputBinding.Separator))
                        {
                            var controlSchemeIndex =
                                asset.controlSchemes.IndexOf(x => group.Equals(x.bindingGroup, StringComparison.InvariantCultureIgnoreCase));
                            if (controlSchemeIndex == -1)
                                continue;

                            ////TODO: make this deal with and/or requirements

                            var controlScheme = asset.controlSchemes[controlSchemeIndex];
                            foreach (var requirement in controlScheme.deviceRequirements)
                                WithControlsHavingToMatchPath(requirement.controlPath);
                        }
                    }
                }

                return this;
            }

            /// <summary>
            /// Apply the rebinding to all <see cref="InputAction.bindings"/> of the action given by <see cref="WithAction"/>
            /// which are match the given binding mask (see <see cref="InputBinding.Matches"/>).
            /// </summary>
            /// <param name="bindingMask">A binding mask. See <see cref="InputBinding.Matches"/>.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <seealso cref="WithBindingGroup"/>
            /// <seealso cref="WithTargetBinding"/>
            public RebindingOperation WithBindingMask(InputBinding? bindingMask)
            {
                m_BindingMask = bindingMask;
                return this;
            }

            /// <summary>
            /// Apply the rebinding to all <see cref="InputAction.bindings"/> of the action given by <see cref="WithAction"/>
            /// which are associated with the given binding group (see <see cref="InputBinding.groups"/>).
            /// </summary>
            /// <param name="group">A binding group. See <see cref="InputBinding.groups"/>. A binding matches if any of its
            /// group associates matches.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <seealso cref="WithBindingMask"/>
            /// <seealso cref="WithTargetBinding"/>
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
            /// <seealso cref="OnGeneratePath"/>
            public RebindingOperation WithoutGeneralizingPathOfSelectedControl()
            {
                m_Flags |= Flags.DontGeneralizePathOfSelectedControl;
                return this;
            }

            /// <summary>
            /// Instead of applying the generated path as an <see cref="InputBinding.overridePath"/>,
            /// create a new binding on the given action (see <see cref="WithAction"/>).
            /// </summary>
            /// <param name="group">Binding group (see <see cref="InputBinding.groups"/>) to apply to the new binding.
            /// This determines, for example, which control scheme (if any) the binding is associated with.</param>
            /// <returns></returns>
            /// <seealso cref="OnApplyBinding"/>
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
            /// <seealso cref="magnitudes"/>
            /// <seealso cref="InputControl.EvaluateMagnitude()"/>
            public RebindingOperation WithMagnitudeHavingToBeGreaterThan(float magnitude)
            {
                ThrowIfRebindInProgress();
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
                ThrowIfRebindInProgress();
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
            /// <seealso cref="InputControlPath.Matches"/>
            public RebindingOperation WithControlsHavingToMatchPath(string path)
            {
                ThrowIfRebindInProgress();
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentNullException(nameof(path));
                for (var i = 0; i < m_IncludePathCount; ++i)
                    if (string.Compare(m_IncludePaths[i], path, StringComparison.InvariantCultureIgnoreCase) == 0)
                        return this;
                ArrayHelpers.AppendWithCapacity(ref m_IncludePaths, ref m_IncludePathCount, path);
                return this;
            }

            ////REVIEW: This API has been confusing for users who usually will do something like WithControlsExcluding("Mouse"); find a more intuitive way to do this
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
            /// <seealso cref="InputControlPath.Matches"/>
            public RebindingOperation WithControlsExcluding(string path)
            {
                ThrowIfRebindInProgress();
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentNullException(nameof(path));
                for (var i = 0; i < m_ExcludePathCount; ++i)
                    if (string.Compare(m_ExcludePaths[i], path, StringComparison.InvariantCultureIgnoreCase) == 0)
                        return this;
                ArrayHelpers.AppendWithCapacity(ref m_ExcludePaths, ref m_ExcludePathCount, path);
                return this;
            }

            /// <summary>
            /// If no match materializes with <paramref name="timeInSeconds"/>, cancel the rebind automatically.
            /// </summary>
            /// <param name="timeInSeconds">Time in seconds to wait for a successful rebind. Disabled if timeout is less than or equal to 0.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// Limiting rebinds by time can be useful in situations where a rebind may potentially put the user in a situation where
            /// there is no other way to escape the rebind. For example, if <see cref="WithMatchingEventsBeingSuppressed"/> is engaged,
            /// input may be consumed by the rebind and thus not reach the UI if <see cref="WithControlsExcluding"/> has not also been
            /// configured accordingly.
            ///
            /// By default, no timeout is set.
            /// </remarks>
            /// <seealso cref="timeout"/>
            public RebindingOperation WithTimeout(float timeInSeconds)
            {
                m_Timeout = timeInSeconds;
                return this;
            }

            /// <summary>
            /// Delegate to invoke when the rebind completes successfully.
            /// </summary>
            /// <param name="callback">A delegate to invoke when the rebind is <see cref="completed"/>.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// Note that by the time this is invoked, the rebind has been fully applied, that is
            /// <see cref="OnApplyBinding"/> has been executed.
            /// </remarks>
            public RebindingOperation OnComplete(Action<RebindingOperation> callback)
            {
                m_OnComplete = callback;
                return this;
            }

            /// <summary>
            /// Delegate to invoke when the rebind is cancelled instead of completing. This happens when either an
            /// input is received from a control explicitly set up to trigger cancellation (see <see cref="WithCancelingThrough(string)"/>
            /// and <see cref="WithCancelingThrough(InputControl)"/>) or when <see cref="Cancel"/> is called
            /// explicitly.
            /// </summary>
            /// <param name="callback">Delegate to invoke when the rebind is cancelled.</param>
            /// <returns></returns>
            /// <seealso cref="WithCancelingThrough(string)"/>
            /// <seealso cref="Cancel"/>
            /// <seealso cref="canceled"/>
            public RebindingOperation OnCancel(Action<RebindingOperation> callback)
            {
                m_OnCancel = callback;
                return this;
            }

            /// <summary>
            /// Delegate to invoke when the rebind has found one or more controls that it considers
            /// potential matches. This allows modifying priority of matches or adding or removing
            /// matches altogether.
            /// </summary>
            /// <param name="callback">Callback to invoke when one or more suitable controls have been found.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// The matches will be contained in <see cref="candidates"/>. In the callback, you can,
            /// for example, alter the contents of the list in order to customize the selection process.
            /// You can remove candidates with <see cref="AddCandidate"/> and/or remove candidates
            /// with <see cref="RemoveCandidate"/>.
            /// </remarks>
            /// <seealso cref="candidates"/>
            public RebindingOperation OnPotentialMatch(Action<RebindingOperation> callback)
            {
                m_OnPotentialMatch = callback;
                return this;
            }

            /// <summary>
            /// Set function to call when generating the final binding path (see <see cref="InputBinding.path"/>) for a control
            /// that has been selected.
            /// </summary>
            /// <param name="callback">Delegate to call for when to generate a binding path.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// A rebind will by default create a path that it deems most useful for the purpose of rebinding. However, this
            /// logic may be undesirable for your use case. By supplying a custom callback you can bypass this logic and thus replace it.
            ///
            /// When a matching control is singled out, the default logic will look for the device that introduces the given
            /// control. For example, if the A button is pressed on an Xbox gamepad, the resulting path will be <c>"&lt;Gamepad&gt;/buttonSouth"</c>
            /// as it is the <see cref="Gamepad"/> device that introduces the south face button on gamepads. Thus, the binding will work
            /// with any other gamepad, not just the Xbox controller.
            ///
            /// If the delegate returns a null or empty string, the default logic will be re-engaged.
            /// </remarks>
            /// <seealso cref="InputBinding.path"/>
            /// <seealso cref="WithoutGeneralizingPathOfSelectedControl"/>
            public RebindingOperation OnGeneratePath(Func<InputControl, string> callback)
            {
                m_OnGeneratePath = callback;
                return this;
            }

            /// <summary>
            /// Delegate to invoke for compute the matching score for a candidate control.
            /// </summary>
            /// <param name="callback">A delegate that computes matching scores.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// By default, the actuation level of a control is used as its matching score. For a <see cref="Controls.StickControl"/>,
            /// for example, the vector magnitude of the control will be its score. So, a stick that is actuated just a little
            /// will have a lower score than a stick that is actuated to maximum extent in one direction.
            ///
            /// The control with the highest score will be the one appearing at index 0 in <see cref="candidates"/> and thus
            /// will be the control picked by the rebind as the top candidate.
            ///
            /// By installing a custom delegate, it is possible to customize the scoring and apply custom logic to boost
            /// or lower scores of controls.
            ///
            /// The first argument to the delegate is the control that is being added to <see cref="candidates"/> and the
            /// second argument is a pointer to the input event that contains an input on the control.
            /// </remarks>
            /// <seealso cref="scores"/>
            /// <seealso cref="candidates"/>
            public RebindingOperation OnComputeScore(Func<InputControl, InputEventPtr, float> callback)
            {
                m_OnComputeScore = callback;
                return this;
            }

            /// <summary>
            /// Apply a generated binding <see cref="InputBinding.path"/> as the final step to complete a rebind.
            /// </summary>
            /// <param name="callback">Delegate to invoke in order to the apply the generated binding path.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// Once a binding path has been generated (see <see cref="OnGeneratePath"/>) from a candidate control,
            /// the last step is to apply the path. The default logic will take the supplied action (see <see cref="WithAction"/>)
            /// and apply the path as an <see cref="InputBinding.overridePath"/> on all bindings that have been selected
            /// for rebinding with <see cref="WithTargetBinding"/>, <see cref="WithBindingMask"/>, or <see cref="WithBindingGroup"/>.
            ///
            /// To customize this process, you can supply a custom delegate via this method. If you do so, the default
            /// logic is bypassed and the step left entirely to the delegate. This also makes it possible to use
            /// rebind operations without even having an action or even <see cref="InputBinding"/>s.
            /// </remarks>
            public RebindingOperation OnApplyBinding(Action<RebindingOperation, string> callback)
            {
                m_OnApplyBinding = callback;
                return this;
            }

            /// <summary>
            /// If a successful match has been found, wait for the given time for a better match to appear before
            /// committing to the match.
            /// </summary>
            /// <param name="seconds">Time in seconds to wait until committing to a match.</param>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <remarks>
            /// While this adds a certain amount of lag to the operation, the lag is not really perceptible if the timeout
            /// is kept short.
            ///
            /// What this helps with is controls such as sticks where, when moved out of the deadzone, the initial direction
            /// that the user presses may not be the one actually intended. For example, the user may be pressing slightly
            /// more in the X direction before finally very clearly going more strongly in the Y direction. If the rebind
            /// does not wait for a bit but instead takes the first actuation as is, the rebind may appear overly brittle.
            ///
            /// An alternative to timeouts is to set higher magnitude thresholds with <see cref="WithMagnitudeHavingToBeGreaterThan"/>.
            /// The default threshold is 0.2f. By setting it to 0.6f or even higher, timeouts may be unnecessary.
            /// </remarks>
            public RebindingOperation OnMatchWaitForAnother(float seconds)
            {
                m_WaitSecondsAfterMatch = seconds;
                return this;
            }

            /// <summary>
            /// Start the rebinding. This should be invoked after the rebind operation has been fully configured.
            /// </summary>
            /// <returns>The same RebindingOperation instance.</returns>
            /// <exception cref="InvalidOperationException">The rebind has been configure incorrectly. For example, no action has
            /// been given but no <see cref="OnApplyBinding"/> callback has been installed either.</exception>
            /// <seealso cref="Cancel"/>
            /// <seealso cref="Dispose"/>
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

            /// <summary>
            /// Cancel an ongoing rebind. This will invoke the callback supplied by <see cref="OnCancel"/> (if any).
            /// </summary>
            /// <seealso cref="Start"/>
            /// <see cref="started"/>
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

            /// <summary>
            /// Add a candidate to <see cref="candidates"/>. This will also add values to <see cref="scores"/> and
            /// <see cref="magnitudes"/>. If the control has already been added, it's values are simply updated based
            /// on the given arguments.
            /// </summary>
            /// <param name="control">A control that is meant to be considered as a candidate for the rebind.</param>
            /// <param name="score">The score to associate with the control (see <see cref="scores"/>). By default, the control with the highest
            /// score will be picked by the rebind.</param>
            /// <param name="magnitude">Actuation level of the control to enter into <see cref="magnitudes"/>.</param>
            /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c>.</exception>
            /// <seealso cref="RemoveCandidate"/>
            public void AddCandidate(InputControl control, float score, float magnitude = -1)
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
                    var scoreCount = m_Candidates.Count;
                    var magnitudeCount = m_Candidates.Count;
                    m_Candidates.Add(control);
                    ArrayHelpers.AppendWithCapacity(ref m_Scores, ref scoreCount, score);
                    ArrayHelpers.AppendWithCapacity(ref m_Magnitudes, ref magnitudeCount, magnitude);
                }

                SortCandidatesByScore();
            }

            /// <summary>
            /// Remove a control from the list of <see cref="candidates"/>. This also removes its entries from
            /// <see cref="scores"/> and <see cref="magnitudes"/>.
            /// </summary>
            /// <param name="control">Control to remove from <see cref="candidates"/>.</param>
            /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c>.</exception>
            /// <seealso cref="AddCandidate"/>
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

            /// <summary>
            /// Release all memory held by the option, especially unmanaged memory which will not otherwise
            /// be freed.
            /// </summary>
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
                Cancel();
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
                m_StartingActuations?.Clear();
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
                var eventType = eventPtr.type;
                if (eventType != StateEvent.Type && eventType != DeltaStateEvent.Type)
                    return;

                ////TODO: add callback that shows the candidate *and* the event to the user (this is particularly useful when we are suppressing
                ////      and thus throwing away events)

                // Go through controls in the event and see if there's anything interesting.
                // NOTE: We go through quite a few steps and operations here. However, the chief goal here is trying to be as robust
                //       as we can in isolating the control the user really means to single out. If this code here does its job, that
                //       control should always pop up as the first entry in the candidates list (if the configuration of the rebind
                //       operation is otherwise sane).
                var haveChangedCandidates = false;
                var suppressEvent = false;
                var controlEnumerationFlags =
                    InputControlExtensions.Enumerate.IncludeNonLeafControls
                    | InputControlExtensions.Enumerate.IncludeSyntheticControls;
                if ((m_Flags & Flags.DontIgnoreNoisyControls) != 0)
                    controlEnumerationFlags |= InputControlExtensions.Enumerate.IncludeNoisyControls;
                foreach (var control in eventPtr.EnumerateControls(controlEnumerationFlags, device))
                {
                    var statePtr = control.GetStatePtrFromStateEventUnchecked(eventPtr, eventType);
                    Debug.Assert(statePtr != null, "If EnumerateControls() returns a control, GetStatePtrFromStateEvent should not return null for it");

                    // If the control that cancels has been actuated, abort the operation now.
                    if (!string.IsNullOrEmpty(m_CancelBinding) && InputControlPath.Matches(m_CancelBinding, control) &&
                        control.HasValueChangeInState(statePtr))
                    {
                        OnCancel();
                        break;
                    }

                    // If controls must not match certain paths, make sure the control doesn't.
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

                    ////REVIEW: shouldn't we generally require any already actuated control to go back to 0 actuation before considering it for a rebind?

                    // Skip controls that are in their default state.
                    // NOTE: This is the cheapest check with respect to looking at actual state. So
                    //       do this first before looking further at the state.
                    if (control.CheckStateIsAtDefault(statePtr))
                    {
                        // For controls that were already actuated when we started the rebind, we record starting actuations below.
                        // However, when such a control goes back to default state, we want to reset that recorded value. This makes
                        // sure that if, for example, a key is down when the rebind started, when the key is released and then pressed
                        // again, we don't compare to the previously recorded magnitude of 1 but rather to 0.
                        if (!m_StartingActuations.ContainsKey(control))
                            // ...but we also need to record the first time this control appears in it's default state for the case where
                            // the user is holding a discrete control when rebinding starts. On the first release, we'll record here a
                            // starting actuation of 0, then when the key is pressed again, the code below will successfully compare the
                            // starting value of 0 to the pressed value of 1. If we didn't set this to zero on release, the user would
                            // have to release the key, press and release again, and on the next press, it would register as actuated.
                            m_StartingActuations.Add(control, 0);

                        m_StartingActuations[control] = 0;

                        continue;
                    }

                    var magnitude = control.EvaluateMagnitude(statePtr);
                    if (magnitude >= 0)
                    {
                        // Determine starting actuation.
                        if (m_StartingActuations.TryGetValue(control, out var startingMagnitude) == false)
                        {
                            // Haven't seen this control changing actuation yet. Record its current actuation as its
                            // starting actuation and ignore the control if we haven't reached our actuation threshold yet.
                            startingMagnitude = control.EvaluateMagnitude();
                            m_StartingActuations.Add(control, startingMagnitude);
                        }

                        // Ignore control if it hasn't exceeded the magnitude threshold relative to its starting actuation yet.
                        if (Mathf.Abs(startingMagnitude - magnitude) < m_MagnitudeThreshold)
                            continue;
                    }

                    ////REVIEW: this would be more useful by providing the default score *to* the callback (which may alter it or just replace it altogether)
                    // Compute score.
                    float score;
                    if (m_OnComputeScore != null)
                    {
                        score = m_OnComputeScore(control, eventPtr);
                    }
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
                        var scoreCount = m_Candidates.Count;
                        var magnitudeCount = m_Candidates.Count;
                        m_Candidates.Add(control);
                        ArrayHelpers.AppendWithCapacity(ref m_Scores, ref scoreCount, score);
                        ArrayHelpers.AppendWithCapacity(ref m_Magnitudes, ref magnitudeCount, magnitude);
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
                        var k = j - 1;
                        m_Scores.SwapElements(j, k);
                        m_Candidates.SwapElements(j, k);
                        m_Magnitudes.SwapElements(j, k);
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
                m_StartingActuations.Clear();

                UnhookOnEvent();
                UnhookOnAfterUpdate();
            }

            private void ThrowIfRebindInProgress()
            {
                if (started)
                    throw new InvalidOperationException("Cannot reconfigure rebinding while operation is in progress");
            }

            ////TODO: this *must* be publicly accessible
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
            private float[] m_Magnitudes;
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

            // Controls may already be actuated by the time we start a rebind. For those, we track starting actuations
            // individually and require them to cross the actuation threshold WRT the starting actuation.
            private Dictionary<InputControl, float> m_StartingActuations = new Dictionary<InputControl, float>();

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
        /// based on the action and, if specified, binding. In particular, it will apply the following default
        /// configuration:
        ///
        /// <ul>
        /// <li><see cref="RebindingOperation.WithAction"/> will be called with <paramref name="action"/></li>
        /// <li>The default timeout will be set to 0.05f seconds with <see cref="RebindingOperation.OnMatchWaitForAnother"/>.</li>
        /// <li>Pointer <see cref="Pointer.delta"/> and <see cref="Pointer.position"/> as well as touch <see cref="Controls.TouchControl.position"/>
        /// and <see cref="Controls.TouchControl.delta"/> controls will be excluded with <see cref="RebindingOperation.WithControlsExcluding"/>.
        /// This prevents mouse movement or touch leading to rebinds as it will generally be used to operate the UI.</li>
        /// <li><see cref="RebindingOperation.WithMatchingEventsBeingSuppressed"/> will be invoked to suppress input funneled into rebinds
        /// from being picked up elsewhere.</li>
        /// <li>Except if the rebind is looking for a button, <see cref="Keyboard.escapeKey"/> will be set up to cancel the rebind
        /// using <see cref="RebindingOperation.WithCancelingThrough(string)"/>.</li>
        /// <li>If <paramref name="bindingIndex"/> is given, <see cref="RebindingOperation.WithTargetBinding"/> is invoked to
        /// target the given binding with the rebind.</li>
        /// </ul>
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
        internal static DeferBindingResolutionWrapper DeferBindingResolution()
        {
            if (s_DeferBindingResolutionWrapper == null)
                s_DeferBindingResolutionWrapper = new DeferBindingResolutionWrapper();
            s_DeferBindingResolutionWrapper.Acquire();
            return s_DeferBindingResolutionWrapper;
        }

        private static DeferBindingResolutionWrapper s_DeferBindingResolutionWrapper;

        internal class DeferBindingResolutionWrapper : IDisposable
        {
            public void Acquire()
            {
                ++InputActionMap.s_DeferBindingResolution;
            }

            public void Dispose()
            {
                if (InputActionMap.s_DeferBindingResolution > 0)
                    --InputActionMap.s_DeferBindingResolution;
                if (InputActionMap.s_DeferBindingResolution == 0)
                    InputActionState.DeferredResolutionOfBindings();
            }
        }
    }
}
