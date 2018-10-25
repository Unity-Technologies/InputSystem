using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Utilities;

////TODO: does not yet support the ability to override bindings and selectively trigger actions

////REVIEW: do we want this functionality as something dynamic or should it be built into the action map setup? (--> layers)

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A stack of <see cref="InputAction">input actions</see> or <see cref="InputActionMap">
    /// action maps</see>.
    /// </summary>
    /// <remarks>
    /// An action stack is useful for layering actions on top of each other such that there is
    /// an implicit priority ordering between the bindings. If the same binding potentially triggers two
    /// actions in the stack, only the action that is higher up in the stack will actually get
    /// triggered.
    ///
    /// An action stack will also automatically enable and disable actions as they are pushed onto and
    /// popped off the stack.
    ///
    /// Actions can be pushed onto a stack either individually or in bulk in the form of <see cref="InputActionMap">
    /// action maps</see>.
    ///
    /// An action stack impacts the callbacks that are triggered for all actions that are added to the
    /// stack.
    /// </remarks>
    public class InputActionStack : IEnumerable<InputAction>
    {
        /// <summary>
        /// Whether the actions in the stack are currently enabled or not.
        /// </summary>
        /// <remarks>
        /// While a stack is enabled, any action added to it will automatically be enabled and
        /// any action removed from it will automatically be disabled.
        /// </remarks>
        /// <see cref="InputAction.enabled"/>
        /// <see cref="InputActionMap.enabled"/>
        public bool enabled
        {
            get { return (m_Flags & Flags.Enabled) != 0; }
        }

        /// <summary>
        /// List of actions in the order they were pushed onto the stack.
        /// </summary>
        /// <remarks>
        /// The topmost action on the stack is the last action in the array.
        ///
        /// If an entire <see cref="InputActionMap"/> is pushed on a stack, all its actions
        /// will be added to the list in the order they appear in the map (<see cref="InputActionMap.actions"/>).
        ///
        /// Does not allocate.
        /// </remarks>
        public ReadOnlyArray<InputAction> actions
        {
            get { return new ReadOnlyArray<InputAction>(m_Actions, 0, m_ActionCount); }
        }

        /// <summary>
        /// Push all actions in the given map onto the stack.
        /// </summary>
        /// <param name="actionMap"></param>
        public void Push(InputActionMap actionMap)
        {
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");
            if (actionMap.enabled)
                throw new ArgumentException(
                    string.Format("Cannot add map '{0}' to stack because it is already enabled", actionMap),
                    "actionMap");

            // In case we have an empty stack and haven't yet allocated an actions array or don't have
            // one that's big enough, simply use the map's action array directly.
            // NOTE: ATM we don't support replacing actions in sets. If we add that, we need to intercept that
            //       and react to that here. Removing and actions is fine as that will switch the map to
            //       a new array and thus leave us with the old array.
            if (m_ActionCount == 0 && (m_Actions == null || m_Actions.Length < actionMap.actions.Count))
            {
                m_Actions = actionMap.m_Actions;
                m_ActionCount = m_Actions != null ? m_Actions.Length : 0;
                m_Flags |= Flags.UsingActionArrayOfMap;
            }
            else
            {
                var actions = actionMap.m_Actions;
                if (actions == null || actions.Length == 0)
                    return;

                var actionCountInMap = actions.Length;
                EnsureCapacity(actionCountInMap);

                var startIndex = m_ActionCount;
                for (var i = 0; i < actionCountInMap; ++i)
                    m_Actions[startIndex + i] = actions[i];
                m_ActionCount += actionCountInMap;
            }

            // Enable actions, if necessary. We do this in bulk here as it is more efficient
            // than calling Enable() on each individual action.
            if (enabled)
                actionMap.Enable();
        }

        /// <summary>
        /// Push a single action onto the stack.
        /// </summary>
        /// <param name="action"></param>
        public void Push(InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (action.enabled)
                throw new ArgumentException(
                    string.Format("Cannot add action '{0}' to stack as it is currently enabled", action), "action");

            // Add.
            EnsureCapacity(1);
            ArrayHelpers.AppendWithCapacity(ref m_Actions, ref m_ActionCount, action);

            // Enable action, if necessary.
            if (enabled)
                action.Enable();
        }

        /// <summary>
        /// Pop the topmost action of the stack.
        /// </summary>
        /// <remarks>
        /// Will do nothing if the stack is empty.
        ///
        /// If the stack is <see cref="enabled"/>, will disable the action.
        /// </remarks>
        public void Pop()
        {
            if (m_ActionCount == 0)
                return;

            var index = m_ActionCount - 1;
            var action = m_Actions[index];
            --m_ActionCount;

            // Null out the entry but only if we haven't loaned an action array from a map currently.
            if ((m_Flags & Flags.UsingActionArrayOfMap) == 0)
                m_Actions[index] = null;

            if (enabled)
                action.Disable();
        }

        /// <summary>
        /// Pop all actions from the given map.
        /// </summary>
        /// <param name="actionMap"></param>
        /// <remarks>
        /// Will do nothing if the stack is empty.
        /// </remarks>
        public void Pop(InputActionMap actionMap)
        {
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");
            if (m_ActionCount == 0)
                return;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Pop a specific action located anywhere in the stack off the stack.
        /// </summary>
        /// <param name="action"></param>
        /// <remarks>
        /// Does nothing if the action is not on the stack.
        /// </remarks>
        public void Pop(InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (m_ActionCount == 0)
                return;

            // Find the action on the stack.
            var index = ArrayHelpers.IndexOfReference(m_Actions, m_ActionCount, action);
            if (index == -1)
                return;

            // If we don't own the action array, duplicate.
            if ((m_Flags & Flags.UsingActionArrayOfMap) != 0)
                ArrayHelpers.DuplicateWithCapacity(ref m_Actions, m_ActionCount, 10);

            // Remove the action.
            ArrayHelpers.EraseAtWithCapacity(ref m_Actions, ref m_ActionCount, index);

            // Finally, disable action if necessary.
            if (enabled)
                action.Disable();
        }

        /// <summary>
        /// Remove all actions in the stack.
        /// </summary>
        /// <remarks>
        /// Does not release memory allocated by the stack.
        /// </remarks>
        public void Clear()
        {
            if (m_ActionCount == 0)
                return;

            if (enabled)
                EnableOrDisableActions(false);

            // If we're owning the action array, null out all action references.
            if ((m_Flags & Flags.UsingActionArrayOfMap) == 0)
            {
                Array.Clear(m_Actions, 0, m_ActionCount);
            }
            else
            {
                // Otherwise let go of the entire array as we're not owning it.
                m_Actions = null;
            }

            m_ActionCount = 0;
        }

        /// <summary>
        /// Enable all actions in the stack.
        /// </summary>
        public void Enable()
        {
            if (enabled)
                return;

            EnableOrDisableActions(true);

            m_Flags |= Flags.Enabled;
        }

        /// <summary>
        /// Disable all actions in the stack.
        /// </summary>
        public void Disable()
        {
            if (!enabled)
                return;

            EnableOrDisableActions(false);

            m_Flags &= ~Flags.Enabled;
        }

        /// <summary>
        /// Return an enumerator that traverses <see cref="actions"/>.
        /// </summary>
        /// <returns>An enumerator over <see cref="actions"/>.</returns>
        public IEnumerator<InputAction> GetEnumerator()
        {
            return actions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private int m_ActionCount;
        private InputAction[] m_Actions;
        private Flags m_Flags;

        [Flags]
        private enum Flags
        {
            Enabled = 1 << 0,
            UsingActionArrayOfMap = 1 << 1,
        }

        private void EnsureCapacity(int capacity)
        {
            Debug.Assert(capacity >= 1);

            // Make sure we have the necessary space in m_Actions. In case we're currently using m_Actions
            // from another InputActionMap, this will automatically switch to a new array as the array
            // from the map won't have any space left.
            //
            // That is, except if we popped actions off the back. In that case we need to duplicate now
            // or we'd write into the array of the action map and stomp over its actions.
            if ((m_Flags & Flags.UsingActionArrayOfMap) != 0 && m_Actions != null &&
                m_Actions.Length > m_ActionCount)
            {
                ArrayHelpers.DuplicateWithCapacity(ref m_Actions, m_ActionCount, capacity);
            }
            else
            {
                ArrayHelpers.EnsureCapacity(ref m_Actions, m_ActionCount, capacity);
            }

            // We always end up with an array of our own.
            m_Flags &= ~Flags.UsingActionArrayOfMap;
        }

        private void EnableOrDisableActions(bool enable)
        {
            // If we're only having the actions of a single map, enable/disable the actions directly
            // through the action map. This is more efficient than enabling/disabling each action individually.
            if ((m_Flags & Flags.UsingActionArrayOfMap) != 0 && m_ActionCount > 0)
            {
                var actionMap = m_Actions[0].actionMap;
                if (actionMap.m_Actions != null & actionMap.m_Actions.Length == m_ActionCount)
                {
                    if (enable)
                        actionMap.Enable();
                    else
                        actionMap.Disable();
                    return;
                }
            }

            // Enable actions one by one.
            for (var i = 0; i < m_ActionCount; ++i)
            {
                if (enable)
                    m_Actions[i].Enable();
                else
                    m_Actions[i].Disable();
            }
        }
    }
}
