using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Input.LowLevel;

// The way target bindings for overrides are found:
// - If specified, directly by index (e.g. "apply this override to the third binding in the map")
// - By path (e.g. "search for binding to '<Gamepad>/leftStick' and override it with '<Gamepad>/rightStick'")
// - By group (e.g. "search for binding on action 'fire' with group 'keyboard&mouse' and override it with '<Keyboard>/space'")
// - By action (e.g. "bind action 'fire' from whatever it is right now to '<Gamepad>/leftStick'")

////FIXME: properly work with composites

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Extensions to help with dynamically rebinding <see cref="InputAction">actions</see> in
    /// various ways.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="InputActionSetupExtensions"/>, the extension methods in here are meant to be
    /// called during normal game operation.
    ///
    /// The two primary duties of these extensions are to apply binding overrides that non-destructively
    /// redirect existing bindings and to facilitate user-controlled rebinding by listening for controls
    /// actuated by a user.
    /// </remarks>
    public static class InputActionRebindingExtensions
    {
        public static void ApplyBindingOverride(this InputAction action, string newPath, string group = null, string path = null)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            ApplyBindingOverride(action, new InputBinding {overridePath = newPath, groups = group, path = path});
        }

        // Apply the given override to the action.
        //
        // NOTE: Ignores the action name in the override.
        // NOTE: Action must be disabled while applying overrides.
        // NOTE: If there's already an override on the respective binding, replaces the override.

        /// <summary>
        ///
        /// </summary>
        /// <param name="action"></param>
        /// <param name="bindingOverride"></param>
        /// <remarks>
        /// Binding overrides are non-destructive. They do not change the bindings set up for an action
        /// but rather apply non-destructive modifications that change the paths of existing bindings.
        /// However, this also means that for overrides to work, there have to be existing bindings that
        /// can be modified.
        /// </remarks>
        public static void ApplyBindingOverride(this InputAction action, InputBinding bindingOverride)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            bindingOverride.action = action.name;
            var actionMap = action.GetOrCreateActionMap();
            ApplyBindingOverride(actionMap, bindingOverride);
        }

        public static void ApplyBindingOverride(this InputAction action, int bindingIndex, InputBinding bindingOverride)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            // We don't want to hit InputAction.bindings here as this requires setting up per-action
            // binding info which we then nuke as part of the override process. Calling ApplyBindingOverride
            // repeatedly with an index would thus cause the same data to be computed and thrown away
            // over and over.
            // Instead we manually search through the map's bindings to find the right binding index
            // in the map.

            var actionMap = action.GetOrCreateActionMap();
            var bindingsInMap = actionMap.m_Bindings;
            var bindingCountInMap = bindingsInMap != null ? bindingsInMap.Length : 0;
            var actionName = action.name;

            var currentBindingIndexOnAction = -1;
            for (var i = 0; i < bindingCountInMap; ++i)
            {
                if (string.Compare(bindingsInMap[i].action, actionName, StringComparison.InvariantCultureIgnoreCase) != 0)
                    continue;

                ++currentBindingIndexOnAction;
                if (currentBindingIndexOnAction == bindingIndex)
                {
                    bindingOverride.action = actionName;
                    ApplyBindingOverride(actionMap, i, bindingOverride);
                    return;
                }
            }

            throw new ArgumentOutOfRangeException(
                string.Format("Binding index {0} is out of range for action '{1}' with {2} bindings", bindingIndex,
                    action, currentBindingIndexOnAction), "bindingIndex");
        }

        public static void ApplyBindingOverride(this InputAction action, int bindingIndex, string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Binding path cannot be null or empty", "path");
            ApplyBindingOverride(action, bindingIndex, new InputBinding {overridePath = path});
        }

        /// <summary>
        /// Apply the given binding override to all bindings in the map that are matched by the override.
        /// </summary>
        /// <param name="actionMap"></param>
        /// <param name="bindingOverride"></param>
        /// <returns>The number of bindings overridden in the given map.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="actionMap"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="actionMap"/> is currently enabled.</exception>
        /// <remarks>
        /// </remarks>
        public static int ApplyBindingOverride(this InputActionMap actionMap, InputBinding bindingOverride)
        {
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");
            actionMap.ThrowIfModifyingBindingsIsNotAllowed();

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
                ++matchCount;
            }

            if (matchCount > 0)
                actionMap.InvalidateResolvedData();

            return matchCount;
        }

        public static void ApplyBindingOverride(this InputActionMap actionMap, int bindingIndex, InputBinding bindingOverride)
        {
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");
            var bindingsCount = actionMap.m_Bindings != null ? actionMap.m_Bindings.Length : 0;
            if (bindingIndex < 0 || bindingIndex >= bindingsCount)
                throw new ArgumentOutOfRangeException(
                    string.Format("Cannot apply override to binding at index {0} in map '{1}' with only {2} bindings",
                        bindingIndex, actionMap, bindingsCount), "bindingIndex");

            actionMap.m_Bindings[bindingIndex].overridePath = bindingOverride.overridePath;
            actionMap.m_Bindings[bindingIndex].overrideInteractions = bindingOverride.overrideInteractions;
            actionMap.InvalidateResolvedData();
        }

        public static void RemoveBindingOverride(this InputAction action, InputBinding bindingOverride)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            action.ThrowIfModifyingBindingsIsNotAllowed();

            bindingOverride.overridePath = null;
            bindingOverride.overrideInteractions = null;

            // Simply apply but with a null binding.
            ApplyBindingOverride(action, bindingOverride);
        }

        private static void RemoveBindingOverride(this InputActionMap actionMap, InputBinding bindingOverride)
        {
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");
            actionMap.ThrowIfModifyingBindingsIsNotAllowed();

            bindingOverride.overridePath = null;
            bindingOverride.overrideInteractions = null;

            // Simply apply but with a null binding.
            ApplyBindingOverride(actionMap, bindingOverride);
        }

        // Restore all bindings to their default paths.
        public static void RemoveAllBindingOverrides(this InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            action.ThrowIfModifyingBindingsIsNotAllowed();

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
            }

            actionMap.InvalidateResolvedData();
        }

        public static IEnumerable<InputBinding> GetBindingOverrides(this InputAction action)
        {
            throw new NotImplementedException();
        }

        // Add all overrides that have been applied to this action to the given list.
        // Returns the number of overrides found.
        public static int GetBindingOverrides(this InputAction action, List<InputBinding> overrides)
        {
            throw new NotImplementedException();
        }

        ////REVIEW: are the IEnumerable variations worth having?

        public static void ApplyBindingOverrides(this InputActionMap actionMap, IEnumerable<InputBinding> overrides)
        {
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");
            actionMap.ThrowIfModifyingBindingsIsNotAllowed();

            foreach (var binding in overrides)
                ApplyBindingOverride(actionMap, binding);
        }

        public static void RemoveBindingOverrides(this InputActionMap actionMap, IEnumerable<InputBinding> overrides)
        {
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");
            actionMap.ThrowIfModifyingBindingsIsNotAllowed();

            foreach (var binding in overrides)
                RemoveBindingOverride(actionMap, binding);
        }

        /// <summary>
        /// Restore all bindings in the map to their defaults.
        /// </summary>
        /// <param name="actionMap">Action map to remove overrides from. Must not have enabled actions.</param>
        /// <exception cref="ArgumentNullException"><paramref name="actionMap"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="actionMap"/> is currently enabled.</exception>
        public static void RemoveAllBindingOverrides(this InputActionMap actionMap)
        {
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");
            actionMap.ThrowIfModifyingBindingsIsNotAllowed();

            if (actionMap.m_Bindings == null)
                return; // No bindings in map.

            var emptyBinding = new InputBinding();
            var bindingCount = actionMap.m_Bindings.Length;
            for (var i = 0; i < bindingCount; ++i)
                ApplyBindingOverride(actionMap, i, emptyBinding);
        }

        /// <summary>
        /// Get all overrides applied to bindings in the given map.
        /// </summary>
        /// <param name="actionMap"></param>
        /// <returns></returns>
        public static IEnumerable<InputBinding> GetBindingOverrides(this InputActionMap actionMap)
        {
            throw new NotImplementedException();
        }

        public static int GetBindingOverrides(this InputActionMap actionMap, List<InputBinding> overrides)
        {
            throw new NotImplementedException();
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
                throw new ArgumentNullException("action");
            if (control == null)
                throw new ArgumentNullException("control");

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
                throw new ArgumentNullException("actionMap");
            if (control == null)
                throw new ArgumentNullException("control");

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

        // Base implementation for user rebinds. Can be derived from to customize rebinding behavior.
        //
        // The best control to bind to may not be the very first control that matches selection by
        // control type. For example, when the user wants to bind to a specific axis on the left stick
        // on the gamepad, it's not enough to just grab the first axis that actuated. With the sticks
        // being as noisy as they are, we want to filter for the control that dominates input. For that,
        // it adds robustness to not just wait for input in the very first frame but rather listen for
        // input for a while after the first relevant input and then decide which the dominate axis was.
        // This way we can reliably filter out noise from other sticks, too.

        /// <summary>
        /// An ongoing rebinding operation.
        /// </summary>
        /// <remarks>
        /// This class acts as both a configuration interface for rebinds as well as a controller while
        /// the rebind is ongoing. An instance can be reused arbitrary many times. Doing so avoids allocating
        /// GC memory (the class internally retains state that it can reuse for multiple rebinds).
        /// </remarks>
        /// <seealso cref="InputActionRebindingExtensions.PerformInteractiveRebinding"/>
        public class RebindingOperation : IDisposable
        {
            /// <summary>
            /// The action that rebinding is being performed on.
            /// </summary>
            /// <seealso cref="WithAction"/>
            public InputAction action
            {
                get { return m_ActionToRebind; }
            }

            /// <summary>
            /// Controls that had input and were deemed potential matches to rebind to.
            /// </summary>
            public InputControlList<InputControl> candidates
            {
                get { throw new NotImplementedException(); }
            }

            public InputControl selectedControl
            {
                get
                {
                    if (m_Candidates.Count == 0)
                        return null;

                    return m_Candidates[0];
                }
            }

            public bool started
            {
                get { return (m_Flags & Flags.Started) != 0; }
            }

            public bool completed
            {
                get { return (m_Flags & Flags.Completed) != 0; }
            }

            public bool cancelled
            {
                get { return (m_Flags & Flags.Cancelled) != 0; }
            }

            public RebindingOperation WithAction(InputAction action)
            {
                ThrowIfRebindInProgress();

                if (action == null)
                    throw new ArgumentNullException("action");
                if (action.bindings.Count == 0)
                    throw new ArgumentException(
                        string.Format("For rebinding, action must have at least one existing binding (action: {0})",
                            action), "action");

                m_ActionToRebind = action;
                return this;
            }

            public RebindingOperation WithBindingMask(InputBinding? bindingMask)
            {
                return this;
            }

            public RebindingOperation WithRebindApplyingAsOverride()
            {
                return this;
            }

            public RebindingOperation WithRebindOverwritingCurrentPath()
            {
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
                return this;
            }

            public RebindingOperation OnMatchWaitForAnother(float seconds)
            {
                return this;
            }

            public RebindingOperation Start()
            {
                // Ignore if already started.
                if ((m_Flags & Flags.Started) != 0)
                    return this;

                HookOnEvent();
                m_Flags |= Flags.Started;
                return this;
            }

            // Manually cancel a pending rebind.
            public void Cancel()
            {
            }

            public void Dispose()
            {
                UnhookOnEvent();
                m_Candidates.Dispose();
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

            private void OnEvent(InputEventPtr eventPtr)
            {
                // Ignore if not a state event.
                if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                    return;

                // Fetch device.
                var device = InputSystem.GetDeviceById(eventPtr.deviceId);
                if (device == null)
                    return;

                // Go through controls and see if there's anything interesting in the event.
                var controls = device.allControls;
                var controlCount = controls.Count;
                for (var i = 0; i < controlCount; ++i)
                {
                    var control = controls[i];

                    ////TODO: skip noisy controls

                    // Skip controls that have no state in the event.
                    var statePtr = control.GetStatePtrFromStateEvent(eventPtr);
                    if (statePtr == IntPtr.Zero)
                        continue;

                    // Skip controls that are in their default state.
                    if (control.CheckStateIsAtDefault(statePtr))
                        continue;

                    //see if it's a type of control we're interested in
                    //see if it has an interesting change

                    m_Candidates.Add(control);
                    OnComplete();
                    break;
                }
            }

            private void OnComplete()
            {
                Debug.Assert(m_Candidates.Count > 0);

                var selectedControl = m_Candidates[0];
                m_ActionToRebind.ApplyBindingOverride(selectedControl.path);

                m_Flags |= Flags.Completed;

                if (m_OnComplete != null)
                    m_OnComplete(this);

                m_Flags &= ~Flags.Started;
                m_Candidates.Clear();
                m_Candidates.Capacity = 0; // Release our unmanaged memory.
            }

            private void OnCancel()
            {
            }

            private void ThrowIfRebindInProgress()
            {
                if (started)
                    throw new InvalidOperationException("Cannot reconfigure rebinding while operation is in progress");
            }

            private InputAction m_ActionToRebind;
            private InputAction m_CancelAction;
            private InputBinding? m_BindingMask;
            private InputControlList<InputControl> m_Candidates;
            private Action<RebindingOperation> m_OnComplete;
            private Action<RebindingOperation> m_OnCancel;
            private Action<InputEventPtr> m_OnEventDelegate;
            private Flags m_Flags;

            [Flags]
            private enum Flags
            {
                Started = 1 << 0,
                Completed = 1 << 1,
                Cancelled = 1 << 2,
                OnEventHooked = 1 << 3,
                OverwritePath = 1 << 4,
            }
        }

        //
        // Invokes the given callback when the rebinding happens. Also passes
        // a bool that tells whether the rebind operation has successfully completed
        // or whether the user aborted it.
        //
        // The optional cancel binding allows specifying which controls should be
        // allowed to cancel the rebind.
        //
        // NOTE: Suitable controls to rebind to are determined from the given action.
        //       The rebind will listen only for controls that match one of th control
        //       layouts used in an y of the bindings of the action.
        //
        //       So, for example, if the given action has only bindings to buttons,
        //       then only buttons will be considered. If it has bindings to both button
        //       and touch controls, on the other hand, then both button and touch controls
        //       will be listened for.

        /// <summary>
        /// Initiate an operation that interactively rebinds the given action based on received input.
        /// </summary>
        /// <param name="action">Action to perform rebinding on.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static RebindingOperation PerformInteractiveRebinding(this InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (action.bindings.Count == 0)
                throw new ArgumentException(
                    string.Format("For rebinding, action must have at least one existing binding (action: {0})",
                        action), "action");

            return new RebindingOperation().WithAction(action);
        }
    }
}
