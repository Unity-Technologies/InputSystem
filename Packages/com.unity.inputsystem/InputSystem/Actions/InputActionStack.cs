using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Utilities;

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
        public bool enabled
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// List of actions in the order they are pushed on the stack.
        /// </summary>
        /// <remarks>
        /// The topmost action on the stack is the last action in the array.
        ///
        /// Does not allocate.
        /// </remarks>
        public ReadOnlyArray<InputAction> actions
        {
            get { throw new NotImplementedException(); }
        }

        public ReadOnlyArray<InputActionMap> actionMaps
        {
            get { throw new NotImplementedException(); }
        }

        public void Push(InputActionMap actionMap)
        {
            throw new NotImplementedException();
        }

        public void Push(InputAction action)
        {
            throw new NotImplementedException();
        }

        //also disables actions, if stack is enabled

        public void Pop()
        {
            throw new NotImplementedException();
        }

        public void Pop(InputActionMap actionMap)
        {
            throw new NotImplementedException();
        }

        public void Pop(InputAction action)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clear the stack and switch to the given set of actions.
        /// </summary>
        /// <param name="actionMap"></param>
        public void SwitchTo(InputActionMap actionMap)
        {
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");

            Clear();
            Push(actionMap);
        }

        //also affects what happens with newly added actions

        /// <summary>
        /// Enable all actions in the stack.
        /// </summary>
        public void Enable()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Disable all actions in the stack.
        /// </summary>
        public void Disable()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<InputAction> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private int m_ActionCount;
        private InputAction[] m_Actions;
        private int m_ActionMapCount;
        private InputActionMap[] m_ActionMaps;
    }
}
