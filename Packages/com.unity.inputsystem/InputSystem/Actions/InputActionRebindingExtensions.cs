using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Extensions to help with dynamically rebinding <see cref="InputAction">actions</see> in various ways.
    /// </summary>
    public static class InputActionRebindingExtensions
    {
        public static void ApplyBindingOverride(this InputAction action, string newBinding, string group = null)
        {
            action.ApplyBindingOverride(new InputBinding {path = newBinding, groups = group});
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
            action.ThrowIfModifyingBindingsIsNotAllowed();

            throw new NotImplementedException();
            /*
            if (bindingOverride.binding == string.Empty)
                bindingOverride.binding = null;

            var bindingIndex = FindBindingIndexForOverride(bindingOverride);
            if (bindingIndex == -1)
                return;

            m_SingletonActionBindings[m_BindingsStartIndex + bindingIndex].overridePath = bindingOverride.binding;
            */
        }

        public static void ApplyBindingOverride(this InputAction action, int bindingIndex, InputBinding bindingOverride)
        {
            throw new NotImplementedException();
        }

        public static void ApplyBindingOverride(this InputAction action, int bindingIndex, string path)
        {
            throw new NotImplementedException();
        }

        public static void ApplyBindingOverride(this InputActionMap actionMap, InputBinding bindingOverride)
        {
            throw new NotImplementedException();
        }

        public static void RemoveBindingOverride(this InputAction action, InputBinding bindingOverride)
        {
            throw new NotImplementedException();
            /*
            var undoBindingOverride = bindingOverride;
            undoBindingOverride.binding = null;

            // Simply apply but with a null binding.
            ApplyBindingOverride(undoBindingOverride);
            */
        }

        // Restore all bindings to their default paths.
        public static void RemoveAllBindingOverrides(this InputAction action)
        {
            throw new NotImplementedException();
            /*
            if (enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot removed overrides from action '{0}' while the action is enabled", this));

            for (var i = 0; i < m_BindingsCount; ++i)
                m_SingletonActionBindings[m_BindingsStartIndex + i].overridePath = null;
                */
        }

        // Add all overrides that have been applied to this action to the given list.
        // Returns the number of overrides found.
        public static int GetBindingOverrides(this InputAction action, List<InputBinding> overrides)
        {
            throw new NotImplementedException();
        }

        public static void ApplyOverrides(this InputActionMap actionMap, IEnumerable<InputBinding> overrides)
        {
            throw new NotImplementedException();
            /*
            if (enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot change overrides on set '{0}' while the action is enabled", this.name));

            foreach (var binding in overrides)
            {
                var action = TryGetAction(binding.action);
                if (action == null)
                    continue;
                action.ApplyBindingOverride(binding);
            }
            */
        }

        public static void RemoveOverrides(this InputActionMap actionMap, IEnumerable<InputBinding> overrides)
        {
            throw new NotImplementedException();
            /*
            if (enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot change overrides on map '{0}' while actions in the map are enabled", name));

            foreach (var binding in overrides)
            {
                var action = TryGetAction(binding.action);
                if (action == null)
                    continue;
                action.RemoveBindingOverride(binding);
            }
            */
        }

        // Restore all bindings on all actions in the set to their defaults.
        public static void RemoveAllOverrides(this InputActionMap actionMap)
        {
            throw new NotImplementedException();
            /*
            if (enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot remove overrides from map '{0}' while actions in the map are enabled", name));

            for (var i = 0; i < m_Actions.Length; ++i)
            {
                m_Actions[i].RemoveAllBindingOverrides();
            }
            */
        }

        public static int GetOverrides(this InputActionMap actionMap, List<InputBinding> overrides)
        {
            throw new NotImplementedException();
        }

        /*
         // Find the binding tha tthe given override addresses.
         // Return -1 if no corresponding binding is found.
         private static int FindBindingIndexForOverride(InputBinding bindingOverride)
         {
             var group = bindingOverride.group;
             var haveGroup = !string.IsNullOrEmpty(group);

             if (m_BindingsCount == 1)
             {
                 // Simple case where we have only a single binding on the action.

                 if (!haveGroup ||
                     string.Compare(m_SingletonActionBindings[m_BindingsStartIndex].groups, group,
                         StringComparison.InvariantCultureIgnoreCase) == 0)
                     return 0;
             }
             else if (m_BindingsCount > 1)
             {
                 // Trickier case where we need to select from a set of bindings.

                 if (!haveGroup)
                     // Group is required to disambiguate.
                     throw new InvalidOperationException(
                         string.Format(
                             "Action {0} has multiple bindings; overriding binding requires the use of binding groups so the action knows which binding to override. Set 'group' property on InputBindingOverride.",
                             this));

                 int groupStringLength;
                 var indexInGroup = bindingOverride.GetIndexInGroup(out groupStringLength);
                 var currentIndexInGroup = 0;

                 for (var i = 0; i < m_BindingsCount; ++i)
                     if (string.Compare(m_SingletonActionBindings[m_BindingsStartIndex + i].groups, 0, group, 0, groupStringLength, true) == 0)
                     {
                         if (currentIndexInGroup == indexInGroup)
                             return i;

                         ++currentIndexInGroup;
                     }
             }

             return -1;
         }
         */

        // For all bindings in the given action, if a binding matches a control in the given control
        // hiearchy, set an override on the binding to refer specifically to that control.
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
        public static int ApplyOverridesUsingMatchingControls(this InputAction action, InputControl control)
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

        public static int ApplyOverridesUsingMatchingControls(this InputActionMap actionMap, InputControl control)
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
                numMatchingControls = action.ApplyOverridesUsingMatchingControls(control);
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
        public class RebindOperation : IDisposable
        {
            private InputAction m_ActionToRebind;
            private string m_GroupsToRebind;

            private InputAction m_CancelAction;
            private InputAction m_RebindAction;

            private List<string> m_SuitableControlLayouts;

            private int m_NumInputUpdatesToAggregate;
            private int m_NumInputUpdatesReceived;

            protected virtual void DetermineSuitableControlLayouts(List<string> result)
            {
            }

            public void Start(InputAction actionToRebind, string groupsToRebind = null)
            {
            }

            // Manually cancel a pending rebind.
            public void Cancel()
            {
            }

            public virtual void Dispose()
            {
                GC.SuppressFinalize(this);
            }
        }

        // Wait for the user to actuate a control on a device to rebind the
        // given action to.
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
        public static RebindOperation PerformUserRebind(InputAction action, InputAction cancel = null)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (action.bindings.Count == 0)
                throw new ArgumentException(
                    string.Format("For rebinding, action must have at least one existing binding (action: {0})",
                        action), "action");

            throw new NotImplementedException();
        }

        public static RebindOperation PerformUserRebind(InputAction action, InputControl cancel)
        {
            throw new NotImplementedException();
        }

        public static RebindOperation PerformUserRebind(InputAction action, string cancel)
        {
            throw new NotImplementedException();
        }
    }
}
