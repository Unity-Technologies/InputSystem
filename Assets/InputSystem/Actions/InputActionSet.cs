using System;
using System.Collections.Generic;
using UnityEngine;

//allow multiple action sets in a JSON file

namespace ISX
{
    // A set of input actions that can be enabled/disabled in bulk.
    // Also stores data for actions. All actions have to have an associated
    // action set. "Lose" actions constructed without a set will internally
    // create their own "set" to hold their data.
    [Serializable]
    public class InputActionSet : ISerializationCallbackReceiver
    {
        public string name
        {
            get { return m_Name; }
        }

        public ReadOnlyArray<InputAction> actions
        {
            get { return new ReadOnlyArray<InputAction>(m_Actions); }
        }

        public InputActionSet(string name = null)
        {
            m_Name = name;
        }

        public void AddAction(InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (action.m_ActionSet != null && action.m_ActionSet != this)
                throw new InvalidOperationException($"Cannot add '{action.name}' to set '{name}' because it has already been added to set '{action.actionSet.name}'");

            ArrayHelpers.Append(ref m_Actions, action);
            action.m_ActionSet = this;
        }

        public void Enable()
        {
            throw new NotImplementedException();
        }

        public void Disable()
        {
            throw new NotImplementedException();
        }

        public static InputActionSet[] FromJson(string json)
        {
            ActionSetJson[] parsedSets;

            // Allow JSON with either multiple sets or with just a single set.
            try
            {
                var parsed = JsonUtility.FromJson<ActionFileJson>(json);
                parsedSets = parsed.sets;
            }
            catch (Exception originalException)
            {
                try
                {
                    var alternate = JsonUtility.FromJson<ActionSetJson>(json);
                    parsedSets = new[] {alternate};
                }
                catch (Exception)
                {
                    throw originalException;
                }
            }

            var sets = new InputActionSet[parsedSets.Length];
            for (var i = 0; i < parsedSets.Length; ++i)
            {
                var parsedSet = parsedSets[i];
                var set = new InputActionSet(parsedSet.name);

                var actionCount = parsedSet.actions.Length;
                var actions = new InputAction[actionCount];
                
                for (var n = 0; n < parsedSet.actions.Length; ++n)
                {
                    var parsedAction = parsedSet.actions[n];
                    var action = new InputAction(parsedAction.name, parsedAction.defaultBinding, parsedAction.modifier);
                    action.m_ActionSet = set;
                    actions[n] = action;
                }

                set.m_Actions = actions;
                sets[i] = set;
            }

            return sets;
        }

        [SerializeField] private string m_Name;
        [SerializeField] internal InputAction[] m_Actions;

        // These arrays hold data for all actions in the set. Each action will
        // refer to a slice of the arrays.
        internal InputControl[] m_Controls;
        internal InputActionModifier[] m_Modifiers;

        internal void ResolveSources()
        {
            if (m_Actions == null)
                return;

            var controls = new List<InputControl>(); ////REVIEW: cache and reuse this?

            // Resolve all source paths.
            for (var i = 0; i < m_Actions.Length; ++i)
            {
                var action = m_Actions[i];
                var controlsStartIndex = controls.Count;

                // Skip actions that don't have a path set on them.
                if (string.IsNullOrEmpty(action.sourcePath))
                    continue;

                var numMatches = InputSystem.GetControls(action.sourcePath, controls);
                if (numMatches > 0)
                {
                    action.m_Controls = new ReadOnlyArray<InputControl>(null, controlsStartIndex, numMatches);
                }
            }

            // Grab final array.
            m_Controls = controls.ToArray();

            // Patch up all the array references in the ReadOnlyArray structs.
            var runningOffset = 0;
            for (var i = 0; i < m_Actions.Length; ++i)
            {
                var action = m_Actions[i];
                var numControls = action.m_Controls.Count;
                action.m_Controls = new ReadOnlyArray<InputControl>(m_Controls, runningOffset, numControls);
                runningOffset += numControls;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // For "hidden" action sets created into internally stand-alone InputActions, we
            // don't want to serialize that action as otherwise we'd have an infinite cycle --
            // it's the action keeping us alive. So for those actions, we go and remove our
            // actions array. InputAction.OnAfterDeserialize will take of getting the array
            // back after deserialization.

            if (m_Actions.Length == 1 && m_Actions[0].m_PrivateActionSet != null)
                m_Actions = null;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

        [Serializable]
        private struct ActionJson
        {
            public string name;
            public string defaultBinding;
            public string modifier;
        }

        [Serializable]
        private struct ActionSetJson
        {
            public string name;
            public ActionJson[] actions;
        }
        
        // JsonUtility can't deal with having an array or dictionary at the top so
        // we have to wrap this in a struct.
        [Serializable]
        private struct ActionFileJson
        {
            public ActionSetJson[] sets;
        }
    }
}
