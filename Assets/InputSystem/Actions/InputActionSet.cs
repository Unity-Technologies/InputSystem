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
    //
    // A common usage pattern for action sets is to use them to group action
    // "contexts". So one set could hold "menu" actions, for example, whereas
    // another set holds "gameplay" actions. This kind of splitting can be
    // made arbitrarily complex. Like, you could have separate "driving" and
    // "walking" action sets, for example, that you enable and disable depending
    // on whether the player is walking or driving around.
    [Serializable]
    public class InputActionSet : ISerializationCallbackReceiver, ICloneable
    {
        public string name => m_Name;

        public bool enabled => m_EnabledActionsCount > 0;

        public ReadOnlyArray<InputAction> actions => new ReadOnlyArray<InputAction>(m_Actions);

        public InputActionSet(string name = null)
        {
            m_Name = name;
        }

        public InputAction AddAction(string name, string binding = null, string modifiers = null, string groups = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Action must have name", nameof(name));
            if (TryGetAction(name) != null)
                throw new InvalidOperationException($"Cannot add action with duplicate name '{name}' to set '{this.name}'");

            var action = new InputAction(name);
            ArrayHelpers.Append(ref m_Actions, action);
            action.m_ActionSet = this;

            if (!string.IsNullOrEmpty(binding))
                action.AddBinding(binding, modifiers: modifiers, groups: groups);

            return action;
        }

        public InputAction TryGetAction(string name)
        {
            if (m_Actions != null)
            {
                var actionCount = m_Actions.Length;
                for (var i = 0; i < actionCount; ++i)
                    if (string.Compare(name, m_Actions[i].name, StringComparison.InvariantCultureIgnoreCase) == 0)
                        return m_Actions[i];
            }

            return null;
        }

        public InputAction GetAction(string name)
        {
            var action = TryGetAction(name);
            if (action == null)
                throw new KeyNotFoundException($"Could not find action '{name}' in set '{this.name}'");
            return action;
        }

        // Enable all the actions in the set.
        public void Enable()
        {
            if (m_Actions == null || m_EnabledActionsCount == m_Actions.Length)
                return;

            for (var i = 0; i < m_Actions.Length; ++i)
                m_Actions[i].Enable();

            Debug.Assert(m_EnabledActionsCount == m_Actions.Length);
        }

        // Disable all the actions in the set.
        public void Disable()
        {
            if (m_Actions == null || !enabled)
                return;

            for (var i = 0; i < m_Actions.Length; ++i)
                m_Actions[i].Disable();

            Debug.Assert(m_EnabledActionsCount == 0);
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

        ////REVIEW: right now the Clone() methods aren't overridable; do we want that?
        public InputActionSet Clone()
        {
            // Internal action sets from singleton actions should not be visible outside of
            // them. Cloning them is not allowed.
            if (m_SingletonAction != null)
                throw new InvalidOperationException(
                    $"Cloning internal set of singleton action '{m_SingletonAction}' is not allowed");

            var clone = new InputActionSet
            {
                m_Name = m_Name,
                m_Actions = ArrayHelpers.Clone(m_Actions)
            };

            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        [SerializeField] private string m_Name;
        [SerializeField] internal InputAction[] m_Actions;

        // These arrays hold data for all actions in the set. Each action will
        // refer to a slice of the arrays.
        [SerializeField] internal InputBinding[] m_Bindings;
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
            public Flags flags;
            public double startTime;

            [Flags]
            public enum Flags
            {
                TimerRunning = 1 << 8, // Reserve first 8 bits for phase.
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

            public InputAction.Phase phase
            {
                // We store the phase in the low 8 bits of the flags field.
                get { return (InputAction.Phase)((int)flags & 0xf); }
                set { flags = (Flags)(((uint)flags & 0xfffffff0) | (uint)value); }
            }
        }

        internal struct ResolvedBinding
        {
            [Flags]
            public enum Flags
            {
                ChainsWithNext = 1 << 0,
                EndOfChain = 1 << 1,
            }

            public ReadOnlyArray<InputControl> controls;
            public ReadWriteArray<ModifierState> modifiers;
            public Flags flags;

            public bool chainsWithNext
            {
                get { return (flags & Flags.ChainsWithNext) == Flags.ChainsWithNext; }
                set
                {
                    if (value)
                        flags |= Flags.ChainsWithNext;
                    else
                        flags &= ~Flags.ChainsWithNext;
                }
            }

            public bool isEndOfChain
            {
                get { return (flags & Flags.EndOfChain) == Flags.EndOfChain; }
                set
                {
                    if (value)
                        flags |= Flags.EndOfChain;
                    else
                        flags &= ~Flags.EndOfChain;
                }
            }

            public bool isPartOfChain => chainsWithNext || isEndOfChain;
        }

        ////TODO: when re-resolving, we need to preserve ModifierStates and not just reset them
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

            ////TODO: this codepath must be changed to not allocate! Must be possible to do .Enable() and Disable()
            ////      all the time during gameplay and not end up causing GC

            // We lazily allocate these as needed. No point allocating arrays
            // we don't use.
            List<InputControl> controls = null;
            List<ModifierState> modifiers = null;
            List<ResolvedBinding> resolvedBindings = null;

            // Resolve all source paths.
            if (m_SingletonAction != null)
            {
                ResolveBindings(m_SingletonAction, ref controls, ref modifiers, ref resolvedBindings);
            }
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
                for (var i = 0; i < m_Actions.Length; ++i)
                {
                    var action = m_Actions[i];
                    action.m_Controls.m_Array = m_Controls;
                    action.m_ResolvedBindings.m_Array = m_ResolvedBindings;
                }
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

        internal static void DisableAllEnabledActions()
        {
            for (var set = s_FirstSetInGlobalList; set != null;)
            {
                var next = set.m_NextInGlobalList;

                if (set.m_SingletonAction != null)
                    set.m_SingletonAction.Disable();
                else
                    set.Disable();

                set = next;
            }
            Debug.Assert(s_FirstSetInGlobalList == null);
        }

        internal void TellAboutActionChangingEnabledStatus(InputAction action, bool enable)
        {
            if (enable)
            {
                ++m_EnabledActionsCount;
                if (m_EnabledActionsCount == 1)
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
        public struct BindingJson
        {
            public string path;
            public string modifiers;
            public string groups;
            public bool chainWithPrevious;

            public InputBinding ToBinding()
            {
                return new InputBinding
                {
                    path = string.IsNullOrEmpty(path) ? null : path,
                    modifiers = string.IsNullOrEmpty(modifiers) ? null : modifiers,
                    group = string.IsNullOrEmpty(groups) ? null : groups,
                    chainWithPrevious = chainWithPrevious
                };
            }

            public static BindingJson FromBinding(InputBinding binding)
            {
                return new BindingJson
                {
                    path = binding.path,
                    modifiers = binding.modifiers,
                    groups = binding.group,
                    chainWithPrevious = binding.chainWithPrevious
                };
            }
        }

        [Serializable]
        private struct ActionJson
        {
            public string name;
            public BindingJson[] bindings;

            // ToAction doesn't make sense because all bindings combine on the action set and
            // thus need conversion logic that operates on the actions in bulk.

            public static ActionJson FromAction(InputAction action)
            {
                var bindings = action.bindings;
                var bindingsCount = bindings.Count;
                var bindingsJson = new BindingJson[bindingsCount];

                for (var i = 0; i < bindingsCount; ++i)
                {
                    bindingsJson[i] = BindingJson.FromBinding(bindings[i]);
                }

                return new ActionJson
                {
                    name = action.name,
                    bindings = bindingsJson
                };
            }
        }

        // A JSON represention of one or more sets of actions.
        // Contains a list of actions. Each action may specify the set it belongs to
        // as part of its name ("set/action").
        [Serializable]
        private struct ActionFileJson
        {
            public ActionJson[] actions;

            public InputActionSet[] ToSets()
            {
                var sets = new List<InputActionSet>();

                var actions = new List<List<InputAction>>();
                var bindings = new List<List<InputBinding>>();

                var actionCount = this.actions?.Length ?? 0;
                for (var i = 0; i < actionCount; ++i)
                {
                    var jsonAction = this.actions[i];

                    if (string.IsNullOrEmpty(jsonAction.name))
                        throw new Exception($"Action number {i + 1} has no name");

                    ////REVIEW: make sure all action names are unique?

                    // Determine name of action set.
                    string setName = null;
                    string actionName = jsonAction.name;
                    var indexOfFirstSlash = actionName.IndexOf('/');
                    if (indexOfFirstSlash != -1)
                    {
                        setName = actionName.Substring(0, indexOfFirstSlash);
                        actionName = actionName.Substring(indexOfFirstSlash + 1);

                        if (string.IsNullOrEmpty(actionName))
                            throw new Exception($"Invalid action name '{jsonAction.name}' (missing action name after '/')");
                    }

                    // Try to find existing set.
                    InputActionSet set = null;
                    var setIndex = 0;
                    for (; setIndex < sets.Count; ++setIndex)
                    {
                        if (string.Compare(sets[setIndex].name, setName, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            set = sets[setIndex];
                            break;
                        }
                    }

                    // Create new set if it's the first action in the set.
                    if (set == null)
                    {
                        set = new InputActionSet(setName);
                        sets.Add(set);
                        actions.Add(new List<InputAction>());
                        bindings.Add(new List<InputBinding>());
                    }

                    // Create action.
                    var action = new InputAction(actionName);
                    actions[setIndex].Add(action);

                    // Add bindings.
                    if (jsonAction.bindings != null)
                    {
                        var bindingsForSet = bindings[setIndex];
                        var bindingsStartIndex = bindingsForSet.Count;

                        for (var n = 0; n < jsonAction.bindings.Length; ++n)
                        {
                            var jsonBinding = jsonAction.bindings[n];
                            var binding = jsonBinding.ToBinding();
                            bindingsForSet.Add(binding);
                        }

                        action.m_BindingsCount = bindingsForSet.Count - bindingsStartIndex;
                        action.m_BindingsStartIndex = bindingsStartIndex;
                    }
                }

                // Finalize arrays.
                for (var i = 0; i < sets.Count; ++i)
                {
                    var actionArray = actions[i].ToArray();
                    var bindingArray = bindings[i].ToArray();

                    sets[i].m_Actions = actionArray;
                    sets[i].m_Bindings = bindingArray;

                    // Install final binding arrays on actions.
                    for (var n = 0; n < actionArray.Length; ++n)
                        actionArray[n].m_Bindings = bindingArray;
                }

                return sets.ToArray();
            }

            public static ActionFileJson FromSet(InputActionSet set)
            {
                var actions = set.actions;
                var actionCount = actions.Count;
                var actionsJson = new ActionJson[actionCount];
                var haveSetName = !string.IsNullOrEmpty(set.name);

                for (var i = 0; i < actionCount; ++i)
                {
                    actionsJson[i] = ActionJson.FromAction(actions[i]);

                    if (haveSetName)
                        actionsJson[i].name = $"{set.name}/{actions[i].name}";
                }

                return new ActionFileJson
                {
                    actions = actionsJson
                };
            }

            public static ActionFileJson FromSets(IEnumerable<InputActionSet> sets)
            {
                // Count total number of actions.
                var actionCount = 0;
                foreach (var set in sets)
                    actionCount += set.actions.Count;

                // Collect actions from all sets.
                var actionsJson = new ActionJson[actionCount];
                var actionIndex = 0;
                foreach (var set in sets)
                {
                    var haveSetName = !string.IsNullOrEmpty(set.name);
                    var actions = set.actions;

                    for (var i = 0; i < actions.Count; ++i)
                    {
                        actionsJson[actionIndex] = ActionJson.FromAction(actions[i]);

                        if (haveSetName)
                            actionsJson[actionIndex].name = $"{set.name}/{actions[i].name}";

                        ++actionIndex;
                    }
                }

                return new ActionFileJson
                {
                    actions = actionsJson
                };
            }
        }

        // Load one or more action sets from JSON.
        public static InputActionSet[] FromJson(string json)
        {
            var fileJson = JsonUtility.FromJson<ActionFileJson>(json);
            return fileJson.ToSets();
        }

        public static string ToJson(IEnumerable<InputActionSet> sets)
        {
            var fileJson = ActionFileJson.FromSets(sets);
            return JsonUtility.ToJson(fileJson);
        }

        public string ToJson()
        {
            var fileJson = ActionFileJson.FromSet(this);
            return JsonUtility.ToJson(fileJson);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Action sets created internally for singleton actions are meant to be purely transient.
            // The way we up their data, the sets won't serialize properly.
            Debug.Assert(m_SingletonAction == null, "Must not serialize internal sets of singleton actions!");

            // All actions in the set refer to our combined m_Bindings array. We don't
            // want to serialize that as part of each action so we null out all the
            // array references and re-establish them when the set comes back in from
            // serialization. We do want the index and length values from m_Bindings
            // in the actions, though.
            if (m_Actions != null)
            {
                for (var i = 0; i < m_Actions.Length; ++i)
                    m_Actions[i].m_Bindings = null;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_Actions != null)
            {
                // Re-establish links to m_Bindings and set.
                for (var i = 0; i < m_Actions.Length; ++i)
                {
                    var action = m_Actions[i];
                    action.m_Bindings = m_Bindings;
                    action.m_ActionSet = this;
                }
            }
        }
    }
}
