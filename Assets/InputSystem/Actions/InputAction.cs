using System;
using UnityEngine;
using UnityEngine.Events;

namespace ISX
{
    using ActionListener = UnityAction<InputAction, InputControl>;

    // A named input signal that can flexibly decide which input data to tap.
    // Unlike controls, actions signal value *changes* rather than the values themselves.
    // They sit on top of controls (and each single action may reference several controls
    // collectively) and monitor the system for change.
    // NOTE: Unlike InputControls, InputActions are not passive! They will actively perform
    //       processing each frame they are active whereas InputControls just sit there as
    //       long as no one is asking them directly for a value.
    // NOTE: InputActions will automatically hook themselves into the input update in which
    //       they are enabled in.
    [Serializable]
    public class InputAction : ISerializationCallbackReceiver
    {
        public enum Phase
        {
            Disabled,
            Waiting,
            Started,
            Performed,
            Cancelled
        }

        public string name
        {
            get { return m_Name; }
        }

        public Phase phase
        {
            get { return m_CurrentPhase; }
        }

        public InputActionSet actionSet
        {
            get
            {
                if (m_ActionSet == m_PrivateActionSet)
                    return null; // Don't let lose actions expose their internal action set.
                return m_ActionSet;
            }
        }

        public string sourcePath
        {
            get { return m_SourcePath; }
            ////TODO: allow setting
        }

        public ReadOnlyArray<InputControl> sourceControls
        {
            get
            {
                if (m_ActionSet == null)
                    CreatePrivateActionSet();
                if (m_ActionSet.m_Controls == null)
                    m_ActionSet.ResolveSources();
                return m_Controls;
            }
        }

        public bool enabled
        {
            get { return m_Enabled; }
        }

        public event ActionListener onStarted
        {
            add
            {
                if (m_OnStarted == null)
                    m_OnStarted = new ActionEvent();
                m_OnStarted.AddListener(value);
            }
            remove
            {
                if (m_OnStarted != null)
                    m_OnStarted.RemoveListener(value);
            }
        }

        public event ActionListener onCancelled
        {
            add
            {
                if (m_OnCancelled == null)
                    m_OnCancelled = new ActionEvent();
                m_OnCancelled.AddListener(value);
            }
            remove
            {
                if (m_OnCancelled != null)
                    m_OnCancelled.RemoveListener(value);
            }
        }

        // Listeners that are called when the action has been fully performed.
        // Passes along the control that triggered the state change and the action
        // object iself as well.
        public event ActionListener onPerformed
        {
            add
            {
                if (m_OnPerformed == null)
                    m_OnPerformed = new ActionEvent();
                m_OnPerformed.AddListener(value);
            }
            remove
            {
                if (m_OnPerformed != null)
                    m_OnPerformed.RemoveListener(value);
            }
        }

        // Construct a disabled action targeting the given sources.
        public InputAction(string name = null, string sourcePath = null, string modifiers = null)
        {
            m_Name = name;
            m_SourcePath = sourcePath;
            m_CurrentPhase = Phase.Disabled;
        }

        public void Enable()
        {
            if (m_Enabled)
                return;

            var controls = sourceControls;
            var manager = InputSystem.s_Manager;

            for (var i = 0; i < controls.Count; ++i)
                manager.AddStateChangeMonitor(controls[i], this);

            m_Enabled = true;
            m_CurrentPhase = Phase.Waiting;
        }

        public void Disable()
        {
            throw new NotImplementedException();
        }

        // The action set that owns us.
        internal InputActionSet m_ActionSet;

        [SerializeField] private string m_Name;
        [SerializeField] private string m_SourcePath;

        // For actions that are kept outside of any action set, we still a set to hold
        // our data. We create a hidden set private to the action. Unlike the case where
        // the action is part of a public set of actions, we need to serialize the set
        // as *part* of the action.
        // NOTE: If this is set, it will be the same as m_ActionSet.
        [SerializeField] internal InputActionSet m_PrivateActionSet;

        [SerializeField] private ActionEvent m_OnStarted;
        [SerializeField] private ActionEvent m_OnCancelled;
        [SerializeField] private ActionEvent m_OnPerformed;

        // State we keep for enabling/disabling. This is volatile and not put on disk.
        private bool m_Enabled;
        private Phase m_CurrentPhase;
        internal ReadOnlyArray<InputControl> m_Controls;

        private void CreatePrivateActionSet()
        {
            m_PrivateActionSet = new InputActionSet();
            m_PrivateActionSet.AddAction(this);
        }

        private void GoToPhase(Phase newPhase, InputControl triggerControl)
        {
            m_CurrentPhase = newPhase;
            switch (newPhase)
            {
                case Phase.Started:
                    if (m_OnStarted != null)
                        m_OnStarted.Invoke(this, triggerControl);
                    break;

                case Phase.Performed:
                    if (m_OnPerformed != null)
                        m_OnPerformed.Invoke(this, triggerControl);
                    break;

                case Phase.Cancelled:
                    if (m_OnCancelled != null)
                        m_OnCancelled.Invoke(this, triggerControl);
                    break;
            }
        }

        private class ActionEvent : UnityEvent<InputAction, InputControl>
        {
        }

        // Called from InputManager when one of our state change monitors
        // has fired.
        internal void NotifyControlValueChanged(InputControl control, double time)
        {
            ////TODO: if it's a bit-addressed control, make sure the value has *actually* changed
            ////      (change monitors work at the byte level only)

            ////TODO: we probably should be able to specify the various trigger values
            ////      on a per control basis; maybe that's best left to modifiers, though

            ////TODO: this is where modifiers should be able to hijack phase progression
            ////      the path below should be the fallback path when there aren't any modifiers

            ////REVIEW: the current state change logic does not take processors into account
            
            var isAtDefault = control.CheckStateIsAllZeroes();

            switch (phase)
            {
                case Phase.Waiting:
                    if (!isAtDefault)
                    {
                        GoToPhase(Phase.Performed, control);
                        m_CurrentPhase = Phase.Waiting;
                    }
                    break;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // To not create a cycle during serialization, m_PrivateActionSet will
            // remove us from serialization so add ourselves back.
            if (m_PrivateActionSet != null)
            {
                m_PrivateActionSet.m_Actions = new InputAction[1];
                m_PrivateActionSet.m_Actions[0] = this;
            }
        }
    }
}
