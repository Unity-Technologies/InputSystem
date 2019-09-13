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

////TODO: for interactive rebinding, if no expected control layout is set, infer it from the action type

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

        public static void RemoveBindingOverride(this InputAction action, InputBinding bindingOverride)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            bindingOverride.overridePath = null;
            bindingOverride.overrideInteractions = null;
            bindingOverride.overrideProcessors = null;

            // Simply apply but with a null binding.
            ApplyBindingOverride(action, bindingOverride);
        }

        private static void RemoveBindingOverride(this InputActionMap actionMap, InputBinding bindingOverride)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));

            bindingOverride.overridePath = null;
            bindingOverride.overrideInteractions = null;
            bindingOverride.overrideProcessors = null;

            // Simply apply but with a null binding.
            ApplyBindingOverride(actionMap, bindingOverride);
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

                return this;
            }

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
            /// <remarks>
            /// Call this method to reset the effects of calling methods such as <see cref="WithAction"/>,
            /// <see cref="WithBindingGroup"/>, etc. but retain other data that the rebind operation
            /// may have allocated already. If you are reusing the same <c>RebindingOperation</c>
            /// multiple times, a good strategy is to reset and reconfigure the operation before starting
            /// it again.
            /// </remarks>
            public void Reset()
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

                    // If controls have to match a certain path, check if this one does.
                    if (m_IncludePathCount > 0 && !HavePathMatch(control, m_IncludePaths, m_IncludePathCount))
                        continue;

                    // If controls must not match certain path, make sure the control doesn't.
                    if (m_ExcludePathCount > 0 && HavePathMatch(control, m_ExcludePaths, m_ExcludePathCount))
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

                    // Skip controls that have no effective value change.
                    // NOTE: This will run the full processor stack and is more involved.
                    if (!control.HasValueChangeInState(statePtr))
                        continue;

                    // If we have a magnitude threshold, see if control passes it.
                    var magnitude = -1f;
                    if (m_MagnitudeThreshold >= 0f)
                    {
                        magnitude = control.EvaluateMagnitude(statePtr);
                        if (magnitude >= 0 && magnitude < m_MagnitudeThreshold)
                            continue; // No, so skip.
                    }

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
            }
        }

        /// <summary>
        /// Initiate an operation that interactively rebinds the given action based on received input.
        /// </summary>
        /// <param name="action">Action to perform rebinding on.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static RebindingOperation PerformInteractiveRebinding(this InputAction action)
        {
            return new RebindingOperation().WithAction(action);
        }
    }
}
