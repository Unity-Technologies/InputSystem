using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISX
{
    // A set of input actions that can be enabled/disabled in bulk.
    //
    // Also stores data for actions. All actions have to have an associated
    // action set. "Lose" actions constructed without a set will internally
    // create their own "set" to hold their data.
    [Serializable]
    public class InputActionSet : ISerializationCallbackReceiver
    {
        public string name => m_Name;

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

        public InputAction GetAction(string name)
        {
            throw new NotImplementedException();
        }

        public void EnableAll()
        {
            throw new NotImplementedException();
        }

        public void DisableAll()
        {
            throw new NotImplementedException();
        }

        //?????
        public void EnableGroup(string group)
        {
            throw new NotImplementedException();
        }

        public void DisableGroup(string group)
        {
            throw new NotImplementedException();
        }

        public void ApplyOverrides(IEnumerable<InputBindingOverride> overrides)
        {
            throw new NotImplementedException();
        }

        public void RemoveOverrides(IEnumerable<InputBindingOverride> overrides)
        {
            throw new NotImplementedException();
        }

        // Restore all bindings on all actions in the set to their defaults.
        public void RemoveAllOverrides()
        {
            throw new NotImplementedException();
        }

        public int GetOverrides(List<InputBindingOverride> overrides)
        {
            throw new NotImplementedException();
        }

        [SerializeField] private string m_Name;
        [SerializeField] internal InputAction[] m_Actions;

        // These arrays hold data for all actions in the set. Each action will
        // refer to a slice of the arrays.
        [SerializeField] InputBinding[] m_Bindings;
        [NonSerialized] internal InputControl[] m_Controls;
        [NonSerialized] internal ModifierState[] m_Modifiers;
        [NonSerialized] internal ResolvedBinding[] m_ResolvedBindings;

        // Action sets that are created internally by singleton actions to hold their data
        // are never exposed and never serialized so there is no point allocating an m_Actions
        // array.
        [NonSerialized] internal InputAction m_SingletonAction;

        internal struct ModifierState
        {
            public IInputActionModifier modifier;
            public InputControl control;
            public InputAction.Phase phase;
            public Flags flags;
            public double startTime;

            [Flags]
            public enum Flags
            {
                TimerRunning = 1 << 0,
            }

            public bool isTimerRunning
            {
                get { return (flags & Flags.TimerRunning) == Flags.TimerRunning; }
                set
                {
                    if (value)
                        flags |= Flags.TimerRunning;
                    else
                        flags &= ~Flags.TimerRunning;
                }
            }
        }

        internal struct ResolvedBinding
        {
            public ReadOnlyArray<InputControl> controls;
            public ReadWriteArray<ModifierState> modifiers;
        }

        // Resolve all bindings to their controls and also add any action modifiers
        // from the bindings. The best way is for this to happen once for each action
        // set at the beginning of the game and to then enable and disable the sets
        // as needed. However, the system will also re-resolve bindings if the control
        // setup in the system changes (i.e. if devices are added or removed or if
        // templates in the system are changed).
        internal void ResolveBindings()
        {
            if (m_Actions == null && m_SingletonAction == null)
                return;

            ////REVIEW: cache and reuse these?
            // We lazily allocate these as needed. No point allocating arrays
            // we don't use.
            List<InputControl> controls = null;
            List<ModifierState> modifiers = null;
            List<ResolvedBinding> resolvedBindings = null;

            // Resolve all source paths.
            if (m_SingletonAction != null)
                ResolveBindings(m_SingletonAction, ref controls, ref modifiers, ref resolvedBindings);
            else
            {
                for (var i = 0; i < m_Actions.Length; ++i)
                    ResolveBindings(m_Actions[i], ref controls, ref modifiers, ref resolvedBindings);
            }

            // Grab final arrays.
            m_Controls = controls != null && controls.Count > 0 ? controls.ToArray() : null;
            m_Modifiers = modifiers != null && modifiers.Count > 0 ? modifiers.ToArray() : null;

            if (resolvedBindings != null && resolvedBindings.Count > 0)
            {
                m_ResolvedBindings = resolvedBindings != null && resolvedBindings.Count > 0 ? resolvedBindings.ToArray() : null;

                for (var i = 0; i < m_ResolvedBindings.Length; ++i)
                {
                    m_ResolvedBindings[i].controls.m_Array = m_Controls;
                    m_ResolvedBindings[i].modifiers.m_Array = m_Modifiers;
                }
            }

            // Patch up all the array references in the ReadOnlyArray structs.
            if (m_SingletonAction != null)
            {
                if (m_Controls != null)
                {
                    m_SingletonAction.m_Controls.m_Array = m_Controls;
                    m_SingletonAction.m_ResolvedBindings.m_Array = m_ResolvedBindings;
                }
            }
            else
            {
                //indices in resolvedBindings and bindings should always match
                throw new NotImplementedException();
            }
        }

        // Resolve the bindings of a single action and add their data to the given lists of
        // controls, modifiers, and resolved bindings. Allocates the lists, if necessary.
        private void ResolveBindings(InputAction action, ref List<InputControl> controls,
            ref List<ModifierState> modifiers, ref List<ResolvedBinding> resolvedBindings)
        {
            var bindings = action.bindings;
            if (bindings.Count == 0)
                return;

            if (resolvedBindings == null)
                resolvedBindings = new List<ResolvedBinding>();
            if (controls == null)
                controls = new List<InputControl>();

            var controlStartIndex = controls.Count;
            var resolvedBindingsStartIndex = resolvedBindings.Count;

            for (var n = 0; n < bindings.Count; ++n)
            {
                var binding = bindings[n];
                var firstControl = controls.Count;

                // Use override path but fall back to default path if no
                // override set.
                var path = binding.overridePath ?? binding.path;

                // Look up controls.
                var numControls = InputSystem.GetControls(path, controls);
                if (numControls == 0)
                    continue;

                // Instantiate modifiers.
                var firstModifier = 0;
                var numModifiers = 0;
                if (!string.IsNullOrEmpty(binding.modifiers))
                {
                    firstModifier = ResolveModifiers(binding.modifiers, ref modifiers);
                    if (modifiers != null)
                        numModifiers = modifiers.Count - numModifiers;
                }

                // Add entry for resolved binding.
                resolvedBindings.Add(new ResolvedBinding
                {
                    controls = new ReadOnlyArray<InputControl>(null, firstControl, numControls),
                    modifiers = new ReadWriteArray<ModifierState>(null, firstModifier, numModifiers)
                });
            }

            // Let action know where its control and resolved binding entries are.
            action.m_Controls =
                new ReadOnlyArray<InputControl>(null, controlStartIndex, controls.Count - controlStartIndex);
            action.m_ResolvedBindings =
                new ReadOnlyArray<ResolvedBinding>(null, resolvedBindingsStartIndex, resolvedBindings.Count - resolvedBindingsStartIndex);
        }

        private static int ResolveModifiers(string modifierString, ref List<ModifierState> modifiers)
        {
            ////REVIEW: We're piggybacking off the processor parsing here as the two syntaxes are identical. Might consider
            ////        moving the logic to a shared place.
            ////        Alternatively, may split the paths. May help in getting rid of unnecessary allocations.

            var firstModifierIndex = modifiers?.Count ?? 0;

            ////TODO: get rid of the extra array allocations here
            var list = InputTemplate.ParseNameAndParameterList(modifierString);
            for (var i = 0; i < list.Length; ++i)
            {
                // Look up modifier.
                var type = InputSystem.TryGetModifier(list[i].name);
                if (type == null)
                    throw new Exception($"No modifier with name '{list[i].name}' (mentioned in '{modifierString}') has been registered");

                // Instantiate it.
                var modifier = Activator.CreateInstance(type) as IInputActionModifier;
                if (modifier == null)
                    throw new Exception($"Modifier '{list[i].name}' is not an IInputActionModifier");

                // Pass parameters to it.
                InputControlSetup.SetParameters(modifier, list[i].parameters);

                // Add to list.
                if (modifiers == null)
                    modifiers = new List<ModifierState>();
                modifiers.Add(new ModifierState
                {
                    modifier = modifier,
                    phase = InputAction.Phase.Waiting
                });
            }

            return firstModifierIndex;
        }

        // We don't want to explicitly keep track of enabled actions as that will most likely be bookkeeping
        // that isn't used most of the time. However, we do want to be able to find all enabled actions. So,
        // instead we just link all action sets that have enabled actions together in a list that has its link
        // embedded right here in an action set.
        private static InputActionSet s_FirstSetInGlobalList;
        [NonSerialized] private int m_EnabledActionsCount;
        [NonSerialized] internal InputActionSet m_NextInGlobalList;
        [NonSerialized] internal InputActionSet m_PreviousInGlobalList;

        #if UNITY_EDITOR
        ////REVIEW: not sure yet whether this warrants a publicly accessible callback so keeping it a private hook for now
        internal static List<Action> s_OnEnabledActionsChanged;
        #endif

        internal static void ResetGlobals()
        {
            for (var set = s_FirstSetInGlobalList; set != null;)
            {
                var next = set.m_NextInGlobalList;
                set.m_NextInGlobalList = null;
                set.m_PreviousInGlobalList = null;
                set.m_EnabledActionsCount = 0;
                if (set.m_SingletonAction != null)
                    set.m_SingletonAction.enabled = false;
                else
                {
                    for (var i = 0; i < set.m_Actions.Length; ++i)
                        set.m_Actions[i].enabled = false;
                }

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
                if (set.m_SingletonAction != null)
                {
                    actions.Add(set.m_SingletonAction);
                }
                else
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
            }
            return numFound;
        }

        internal static void RefreshEnabledActions()
        {
            for (var set = s_FirstSetInGlobalList; set != null; set = set.m_NextInGlobalList)
                set.ResolveBindings();
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
                --m_EnabledActionsCount;
                if (m_EnabledActionsCount == 0)
                {
                    if (m_NextInGlobalList != null)
                        m_NextInGlobalList.m_PreviousInGlobalList = m_PreviousInGlobalList;
                    if (m_PreviousInGlobalList != null)
                        m_PreviousInGlobalList.m_NextInGlobalList = m_NextInGlobalList;
                    if (s_FirstSetInGlobalList == this)
                        s_FirstSetInGlobalList = m_NextInGlobalList;
                    m_NextInGlobalList = null;
                    m_PreviousInGlobalList = null;
                }
            }

            #if UNITY_EDITOR
            if (s_OnEnabledActionsChanged != null)
                foreach (var listener in s_OnEnabledActionsChanged)
                    listener();
            #endif
        }

        [Serializable]
        private struct ActionJson
        {
            public string name;

            // If the action only needs a single binding, don't force
            // the user to declare an array.
            public string binding;
            public string modifiers;
            public bool combineWithPrevious;

            // Alternative, can specify multiple bindings.
            public BindingJson[] bindings;
        }

        [Serializable]
        public struct BindingJson
        {
            public string path;
            public string modifiers;
            public bool combineWithPrevious;
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

            if (parsedSets == null || parsedSets.Length == 0)
                return Array.Empty<InputActionSet>();

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
                    var action = new InputAction(parsedAction.name, parsedAction.binding, parsedAction.modifiers);
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

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Action sets created internally for singleton actions are meant to be purely transient.
            // The way we up their data, the sets won't serialize properly.
            Debug.Assert(m_SingletonAction == null, "Must not serialize internal arrays of singleton actions!");

            ////TODO: will have to restore m_Bindings elsewhere to keep the set working
            // All actions in the set refer to our combined m_Bindings array. We don't
            // want to serialize that as part of each action so we null out all the
            // array references and re-establish them when the set comes back in from
            // serialization. We do want the index and length values from m_Bindings
            // in the actions, though.
            for (var i = 0; i < m_Actions.Length; ++i)
                m_Actions[i].m_Bindings = null;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Re-establish links to m_Bindings.
            for (var i = 0; i < m_Actions.Length; ++i)
                m_Actions[i].m_Bindings = m_Bindings;
        }
    }
}
