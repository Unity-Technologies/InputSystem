using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISX
{
    // A set of input actions that can be enabled/disabled in bulk.
    // Also stores data for actions. All actions have to have an associated
    // action set. "Lose" actions constructed without a set will internally
    // create their own "set" to hold their data.
    [Serializable]
    public class InputActionSet : ISerializationCallbackReceiver
    {
        public string name => m_Name;

        ////TODO: allow actions in a set to leave their device root open and then allow devices to be assigned at the action set level

        public ReadOnlyArray<InputAction> actions => new ReadOnlyArray<InputAction>(m_Actions);

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

        public void EnableAll()
        {
            throw new NotImplementedException();
        }

        public void DisableAll()
        {
            throw new NotImplementedException();
        }

        // Load one or more action sets from JSON. The given JSON string may
        // either be a single set or may be an object with a property "sets"
        // that contains an array of action sets.
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

        public string ToJson()
        {
            throw new NotImplementedException();
        }

        [SerializeField] private string m_Name;
        [SerializeField] internal InputAction[] m_Actions;

        // We don't want to explicitly keep track of enabled actions as that will most likely be bookkeeping
        // that isn't used most of the time. However, we do want to be able to find all enabled actions. So,
        // instead we just link all the action sets in the system together in a list that has its link embedded
        // right here in an action set.
        // NOTE: Only sets with enabled actions will put themselves on this list.
        private static int m_EnabledActionsCount;
        private static InputActionSet s_FirstSetInGlobalList;
        [NonSerialized] internal InputActionSet m_NextInGlobalList;
        [NonSerialized] internal InputActionSet m_PreviousInGlobalList;

        internal static void ResetGlobals()
        {
            for (var set = s_FirstSetInGlobalList; set != null;)
            {
                var next = set.m_NextInGlobalList;
                set.m_NextInGlobalList = null;
                set.m_PreviousInGlobalList = null;
                for (var i = 0; i < set.m_Actions.Length; ++i)
                    set.m_Actions[i].m_Enabled = false;
                set = next;
            }
            s_FirstSetInGlobalList = null;
        }

        // Walk all sets with enabled actions and add all enabled actions to the given list.
        internal static int FindEnabledActions(List<InputAction> actions)
        {
            var numFound = 0;
            for (var set = s_FirstSetInGlobalList; set != null; set = set.m_NextInGlobalList)
            {
                for (var i = 0; i < set.m_Actions.Length; ++i)
                {
                    var action = set.m_Actions[i];
                    if (!action.enabled)
                        continue;

                    actions.Add(action);
                    ++numFound;
                }
            }
            return numFound;
        }

        internal void TellAboutActionChangingEnabledStatus(InputAction action, bool enable)
        {
            if (enable)
            {
                ++m_EnabledActionsCount;
                if (m_NextInGlobalList == null)
                {
                    if (s_FirstSetInGlobalList != null)
                        s_FirstSetInGlobalList.m_PreviousInGlobalList = this;
                    m_NextInGlobalList = s_FirstSetInGlobalList;
                    s_FirstSetInGlobalList = this;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

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
                if (string.IsNullOrEmpty(action.m_Binding))
                    continue;

                var numMatches = InputSystem.GetControls(action.m_Binding, controls);
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
