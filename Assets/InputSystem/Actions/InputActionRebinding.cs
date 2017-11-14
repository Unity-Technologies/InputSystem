using System;
using System.Collections.Generic;

namespace ISX
{
    // Various pieces of logic that help with rebinding actions in various ways.
    public static class InputActionRebinding
    {
        // For all bindings in the given action, if a binding matches a control in the given control
        // hiearchy, set an override on the binding to refer specifically to that control.
        //
        // Returns the number of overrides that have been applied.
        //
        // Use case: Say you have a local co-op game and a single action set that represents the
        //           actions of any single player. To end up with action sets that are specific to
        //           a certain player, you could, for example, clone the action set four times, and then
        //           take four gamepad devices and use the methods here to have bindings be overridden
        //           on each set to refer to a specific gamepad instance.
        //
        //           Another example is having two XRControllers and two action sets can be on either hand.
        //           At runtime you can dynamically override and re-override the bindings on the action sets
        //           to use them with the controllers as desired.
        public static int ApplyOverridesUsingMatchingControls(this InputAction action, InputControl control)
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
                var matchingControl = InputControlPath.FindControl(control, bindings[i].path);
                if (matchingControl == null)
                    continue;

                action.ApplyBindingOverride(i, matchingControl.path);
                ++numMatchingControls;
            }

            return numMatchingControls;
        }

        public static int ApplyOverridesUsingMatchingControls(this InputActionSet actionSet, InputControl control)
        {
            if (actionSet == null)
                throw new ArgumentNullException(nameof(actionSet));
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var actions = actionSet.actions;
            var actionCount = actions.Count;
            var numMatchingControls = 0;

            for (var i = 0; i < actionCount; ++i)
            {
                var action = actions[i];
                numMatchingControls = action.ApplyOverridesUsingMatchingControls(control);
            }

            return numMatchingControls;
        }

        // Base implementation for user rebinds. Can be derived from to customize rebinding
        // behavior.
        public class RebindOperation : IDisposable
        {
            private InputAction m_Action;
            private List<string> m_SuitableControlTemplates;

            protected virtual void DetermineSuitableControlTemplates(List<string> result)
            {
            }

            public void Start(InputAction action)
            {
            }

            // Manually cancel a pending rebind.
            public void Cancel()
            {
                throw new NotImplementedException();
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
        //       templates used in an y of the bindings of the action.
        //
        //       So, for example, if the given action has only bindings to buttons,
        //       then only buttons will be considered. If it has bindings to both button
        //       and touch controls, on the other hand, then both button and touch controls
        //       will be listened for.
        public static RebindOperation ListenForUserRebind(InputAction action, Action<InputBindingOverride> callback, List<string> suitableControlTemplates = null, InputAction cancelAction = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (action.bindings.Count == 0)
                throw new ArgumentException(
                    $"For rebinding, action must have at least one existing binding (action: {action})", nameof(action));

            throw new NotImplementedException();
        }
    }
}
