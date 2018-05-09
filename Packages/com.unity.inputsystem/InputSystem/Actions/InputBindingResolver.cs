using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: what should we do if none of the actions referenced by bindings could be found?

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Heart of the binding resolution machinery. Consumes lists of bindings
    /// and spits out out a list of resolved bindings.
    /// </summary>
    internal struct InputBindingResolver
    {
        public int controlCount;
        public int modifierCount;
        public int compositeCount;

        public InputControl[] controls;
        public InputActionMapState.ModifierState[] modifierStates;
        public InputActionMapState.BindingState[] bindingStates;
        public InputActionMapState.TriggerState[] actionStates;
        public IInputBindingModifier[] modifiers;
        public object[] composites;
        public int[] controlIndexToBindingIndex;

        private List<InputControlLayout.NameAndParameters> m_Parameters;

        public void ResolveBindings(InputBinding[] bindings, InputAction[] actions)
        {
            Debug.Assert(bindings != null);

            // Allocate/clear binding states.
            var bindingsCount = bindings.Length;
            if (bindingStates == null || bindings.Length != bindingsCount)
                bindingStates = new InputActionMapState.BindingState[bindingsCount];
            else
                Array.Clear(bindingStates, 0, bindingsCount);

            ////TODO: make sure composite objects get all the bindings they need
            ////TODO: handle case where we have bindings resolving to the same control
            ////      (not so clear cut what to do there; each binding may have a different modifier setup, for example)
            var controlStartIndex = 0;
            var currentCompositeIndex = InputActionMapState.kInvalidIndex;
            var actionCount = actions != null ? actions.Length : 0;
            for (var n = 0; n < bindings.Length; ++n)
            {
                var unresolvedBinding = bindings[n];
                var indexOfFirstControlInThisBinding = controlCount;

                // Try to find action.
                var actionIndex = -1;
                var actionName = unresolvedBinding.action;
                if (!actionName.IsEmpty())
                {
                    for (var i = 0; i < actionCount; ++i)
                    {
                        var currentAction = actions[i];
                        if (currentAction.m_Name == actionName)
                        {
                            actionIndex = i;
                            break;
                        }
                    }
                }

                ////TODO: allow specifying parameters for composite on its path (same way as parameters work for modifiers)
                // If it's the start of a composite chain, create the composite.
                if (unresolvedBinding.isComposite)
                {
                    ////REVIEW: what to do about modifiers on composites?

                    // Instantiate. For composites, the path is the name of the composite.
                    var composite = InstantiateBindingComposite(unresolvedBinding.path);
                    var compositeIndex =
                        ArrayHelpers.AppendWithCapacity(ref composites, ref compositeCount, composite);
                    bindingStates[n] = new InputActionMapState.BindingState
                    {
                        actionIndex = actionIndex,
                        compositeIndex = compositeIndex,
                    };

                    // The composite binding entry itself does not resolve to any controls.
                    // It creates a composite binding object which is then populated from
                    // subsequent bindings.
                    continue;
                }

                // If we've reached the end of a composite chain, finish
                // of the current composite.
                if (!unresolvedBinding.isPartOfComposite && currentCompositeIndex != InputActionMapState.kInvalidIndex)
                    currentCompositeIndex = InputActionMapState.kInvalidIndex;

                // Use override path but fall back to default path if no
                // override set.
                var path = unresolvedBinding.overridePath ?? unresolvedBinding.path;

                // Look up controls.
                if (controls == null)
                    controls = new InputControl[10];
                var resolvedControls = new ArrayOrListWrapper<InputControl>(controls, controlCount);
                var numControls = InputSystem.GetControls(path, ref resolvedControls);
                if (numControls == 0)
                    continue;

                controlCount = resolvedControls.count;
                controls = resolvedControls.array;

                // Instantiate modifiers.
                var firstModifier = 0;
                var numModifiers = 0;
                if (!string.IsNullOrEmpty(unresolvedBinding.modifiers))
                {
                    firstModifier = ResolveModifiers(unresolvedBinding.modifiers);
                    if (modifierStates != null)
                        numModifiers = modifierCount - firstModifier;
                }

                // Add entry for resolved binding.
                bindingStates[n] = new InputActionMapState.BindingState
                {
                    controlStartIndex = indexOfFirstControlInThisBinding,
                    controlCount = numControls,
                    modifierStartIndex = firstModifier,
                    modifierCount = numModifiers,
                    isPartOfComposite = unresolvedBinding.isPartOfComposite,
                    actionIndex = actionIndex,
                    compositeIndex = currentCompositeIndex
                };

                // If the binding is part of a composite, pass the resolve controls
                // on to the composite.
                if (unresolvedBinding.isPartOfComposite && currentCompositeIndex != InputActionMapState.kInvalidIndex)
                {
                    ////REVIEW: what should we do when a single binding in a composite resolves to multiple controls?
                    ////        if the composite has more than one bindable control, it's not readily apparent how we would group them
                    if (numControls > 1)
                        throw new NotImplementedException("Handling case where single binding in composite resolves to multiple controls");

                    // Make sure the binding is named. The name determines what in the composite
                    // to bind to.
                    if (string.IsNullOrEmpty(unresolvedBinding.name))
                        throw new Exception(string.Format(
                                "Binding that is part of composite '{0}' is missing a name",
                                composites[currentCompositeIndex]));

                    // Install the control on the binding.
                    BindControlInComposite(composites[currentCompositeIndex], unresolvedBinding.name,
                        controls[indexOfFirstControlInThisBinding]);
                }
            }

            // Finalize arrays.
            controlIndexToBindingIndex = new int[controlCount];
            for (var i = 0; i < bindingsCount; ++i)
            {
                var numControls = bindingStates[i].controlCount;
                var startIndex = bindingStates[i].controlStartIndex;
                for (var n = 0; n < numControls; ++n)
                    controlIndexToBindingIndex[startIndex + n] = i;
            }
            if (actionCount > 0)
                actionStates = new InputActionMapState.TriggerState[actionCount];
        }

        private int ResolveModifiers(string modifierString)
        {
            ////REVIEW: We're piggybacking off the processor parsing here as the two syntaxes are identical. Might consider
            ////        moving the logic to a shared place.
            ////        Alternatively, may split the paths. May help in getting rid of unnecessary allocations.

            var firstModifierIndex = modifierCount;
            if (!InputControlLayout.ParseNameAndParameterList(modifierString, ref m_Parameters))
                return firstModifierIndex;

            for (var i = 0; i < m_Parameters.Count; ++i)
            {
                // Look up modifier.
                var type = InputBindingModifier.s_Modifiers.LookupTypeRegisteration(m_Parameters[i].name);
                if (type == null)
                    throw new Exception(String.Format(
                            "No binding modifier with name '{0}' (mentioned in '{1}') has been registered", m_Parameters[i].name,
                            modifierString));

                // Instantiate it.
                var modifier = Activator.CreateInstance(type) as IInputBindingModifier;
                if (modifier == null)
                    throw new Exception(String.Format("Modifier '{0}' is not an IInputBindingModifier", m_Parameters[i].name));

                // Pass parameters to it.
                InputDeviceBuilder.SetParameters(modifier, m_Parameters[i].parameters);

                // Add to list.
                var modifierStateCount = modifierCount;
                ArrayHelpers.AppendWithCapacity(ref modifierStates, ref modifierStateCount,
                    new InputActionMapState.ModifierState
                {
                    phase = InputActionPhase.Waiting
                });
                ArrayHelpers.AppendWithCapacity(ref modifiers, ref modifierCount, modifier);
                Debug.Assert(modifierStateCount == modifierCount);
            }

            return firstModifierIndex;
        }

        private static object InstantiateBindingComposite(string name)
        {
            // Look up.
            var type = InputBindingComposite.s_Composites.LookupTypeRegisteration(name);
            if (type == null)
                throw new Exception(String.Format("No binding composite with name '{0}' has been registered",
                        name));

            // Instantiate.
            var instance = Activator.CreateInstance(type);
            ////REVIEW: typecheck for IInputBindingComposite? (at least in dev builds)

            return instance;
        }

        ////REVIEW: replace this with a method on the composite that receives the value?
        private static void BindControlInComposite(object composite, string name, InputControl control)
        {
            var type = composite.GetType();

            // Look up field.
            var field = type.GetField(name,
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                throw new Exception(String.Format("Cannot find public field '{0}' in binding composite '{1}' of type '{2}'",
                        name, composite, type));

            // Typecheck.
            if (!typeof(InputControl).IsAssignableFrom(field.FieldType))
                throw new Exception(String.Format(
                        "Field '{0}' in binding composite '{1}' of type '{2}' is not an InputControl", name, composite,
                        type));

            field.SetValue(composite, control);
        }
    }
}
