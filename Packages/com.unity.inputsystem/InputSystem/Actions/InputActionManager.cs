using System;
using UnityEngine.Experimental.Input.Utilities;

//automatically flushes in-between updates?
//adds all its maps to a single state?

//allow putting responders on the map instead of each single action

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// An action manager owns the state of all action maps added to it.
    /// </remarks>
    public class InputActionManager : IInputActionCallbackReceiver
    {
        public ReadOnlyArray<InputActionMap> actionMaps
        {
            get { return new ReadOnlyArray<InputActionMap>(m_State.maps, 0, m_State.totalMapCount); }
        }

        public ReadOnlyArray<TriggerEvent> triggerEvents
        {
            get { throw new NotImplementedException(); }
        }

        public void AddActionMap(InputActionMap actionMap)
        {
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");
            if (actionMap.enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot add action map '{0}' to manager while it is enabled", actionMap));

            // Get rid of action map state that may already be on the map.
            if (actionMap.m_State != null)
            {
                // Ignore if action map has already been added to this manager.
                if (actionMap.m_State == m_State)
                    return;

                actionMap.m_State.Destroy();
                Debug.Assert(actionMap.m_State == null);
            }

            ////TODO: defer resolution until we have all the maps; we really want the ability to set
            ////      an InputActionMapState on a map without having to resolve yet

            // Add the map to our state.
            var resolver = new InputBindingResolver();
            if (m_State != null)
                resolver.ContinueWithDataFrom(m_State);
            else
                m_State = new InputActionMapState();
            resolver.AddActionMap(actionMap);
            m_State.Initialize(resolver);
        }

        public void RemoveActionMap(InputActionMap actionMap)
        {
            throw new NotImplementedException();
        }

        void IInputActionCallbackReceiver.OnActionTriggered(ref InputAction.CallbackContext context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Combined state of all action maps added to the manager.
        /// </summary>
        private InputActionMapState m_State;

        public struct TriggerEvent
        {
            /// <summary>
            /// The input control that triggered.
            /// </summary>
            public InputControl control
            {
                get { throw new NotImplementedException(); }
            }

            public ReadOnlyArray<InputBinding> bindings
            {
                get { throw new NotImplementedException(); }
            }

            /// <summary>
            /// The set of possible actions triggered by the control.
            /// </summary>
            public ReadOnlyArray<InputAction> actions
            {
                get { throw new NotImplementedException(); }
            }

            public ReadOnlyArray<InputActionPhase> phases
            {
                get { throw new NotImplementedException(); }
            }
        }
    }
}
