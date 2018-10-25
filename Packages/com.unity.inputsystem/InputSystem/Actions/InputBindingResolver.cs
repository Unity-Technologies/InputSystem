using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: what should we do if none of the actions referenced by bindings could be found?

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Heart of the binding resolution machinery. Consumes lists of bindings
    /// and spits out out a list of resolved bindings together with their needed
    /// execution state.
    /// </summary>
    /// <remarks>
    /// One or more <see cref="InputActionMap">action maps</see> can be added to the same
    /// resolver. The result is a combination of the binding state of all maps.
    ///
    /// The data set up by a resolver is for consumption by <see cref="InputActionMapState"/>.
    /// Essentially, InputBindingResolver does all the wiring and <see cref="InputActionMapState"/>
    /// does all the actual execution based on the resulting data.
    /// </remarks>
    /// <seealso cref="InputActionMapState.Initialize"/>
    internal struct InputBindingResolver
    {
        /// <summary>
        /// Total number of <see cref="InputActionMap">action maps</see> in <see cref="maps"/>.
        /// </summary>
        public int totalMapCount;

        public int totalActionCount;
        public int totalBindingCount;
        public int totalControlCount;
        public int totalDeviceCount;
        public int totalInteractionCount;
        public int totalProcessorCount;
        public int totalCompositeCount;

        ////REVIEW: make InlinedArray?
        public InputActionMap[] maps;
        public InputControl[] controls;
        public InputActionMapState.InteractionState[] interactionStates;
        public InputActionMapState.BindingState[] bindingStates;
        public InputActionMapState.TriggerState[] actionStates;
        public IInputInteraction[] interactions;
        public object[] processors;
        public object[] composites;

        public InputActionMapState.ActionMapIndices[] mapIndices;
        public int[] controlIndexToBindingIndex;

        /// <summary>
        /// Binding mask used to globally mask out bindings.
        /// </summary>
        /// <remarks>
        /// This is empty by default.
        ///
        /// The bindings of each map will be <see cref="InputBinding.Matches">matched</see> against this
        /// binding. Any bindings that don't match will get skipped and not resolved to controls.
        ///
        /// Note that regardless of whether a binding will be resolved to controls or not, it will get
        /// an entry in <see cref="bindingStates"/>. Otherwise we would have to have a more complicated
        /// mapping from <see cref="InputActionMap.bindings"/> to <see cref="bindingStates"/>.
        /// </remarks>
        public InputBinding bindingMask;

        private List<InputControlLayout.NameAndParameters> m_Parameters;

        public void ContinueWithDataFrom(InputActionMapState state)
        {
            totalMapCount = state.totalMapCount;
            totalActionCount = state.totalActionCount;
            totalBindingCount = state.totalBindingCount;
            totalInteractionCount = state.totalInteractionCount;
            totalProcessorCount = state.totalProcessorCount;
            totalCompositeCount = state.totalCompositeCount;
            totalControlCount = state.totalControlCount;
            totalDeviceCount = state.totalDeviceCount;

            maps = state.maps;
            mapIndices = state.mapIndices;
            actionStates = state.triggerStates;
            bindingStates = state.bindingStates;
            interactionStates = state.interactionStates;
            interactions = state.interactions;
            processors = state.processors;
            composites = state.composites;
            controls = state.controls;
            controlIndexToBindingIndex = state.controlIndexToBindingIndex;
        }

        /// <summary>
        /// Resolve and add all bindings and actions from the given map.
        /// </summary>
        /// <param name="map"></param>
        /// <exception cref="Exception"></exception>
        public void AddActionMap(InputActionMap map)
        {
            Debug.Assert(map != null);

            // Keep track of indices for this map.
            var bindingStartIndex = totalBindingCount;
            var controlStartIndex = totalControlCount;
            var interactionStartIndex = totalInteractionCount;
            var processorStartIndex = totalProcessorCount;
            var compositeStartIndex = totalCompositeCount;
            var actionStartIndex = totalActionCount;

            // Allocate binding states.
            var bindingsInThisMap = map.m_Bindings;
            var bindingCountInThisMap = bindingsInThisMap != null ? bindingsInThisMap.Length : 0;
            totalBindingCount += bindingCountInThisMap;
            ArrayHelpers.GrowBy(ref bindingStates, totalBindingCount);

            ////TODO: make sure composite objects get all the bindings they need
            ////TODO: handle case where we have bindings resolving to the same control
            ////      (not so clear cut what to do there; each binding may have a different interaction setup, for example)
            var currentCompositeBindingIndex = InputActionMapState.kInvalidIndex;
            var currentCompositeIndex = InputActionMapState.kInvalidIndex;
            var bindingMaskOnThisMap = map.m_BindingMask;
            var actionsInThisMap = map.m_Actions;
            var actionCountInThisMap = actionsInThisMap != null ? actionsInThisMap.Length : 0;
            var resolvedControls = new InputControlList<InputControl>(Allocator.Temp);
            try
            {
                for (var n = 0; n < bindingCountInThisMap; ++n)
                {
                    var unresolvedBinding = bindingsInThisMap[n];
                    var bindingIndex = bindingStartIndex + n;

                    // Set binding state to defaults.
                    bindingStates[bindingIndex].mapIndex = totalMapCount;
                    bindingStates[bindingIndex].compositeOrCompositeBindingIndex = InputActionMapState.kInvalidIndex;
                    bindingStates[bindingIndex].actionIndex = InputActionMapState.kInvalidIndex;

                    // Skip binding if it is disabled (path is empty string).
                    var path = unresolvedBinding.effectivePath;
                    if (unresolvedBinding.path == "")
                        continue;

                    // Skip binding if it doesn't match with our binding mask (might be empty).
                    if (!bindingMask.Matches(ref unresolvedBinding))
                        continue;

                    // Skip binding if it doesn't match the binding mask on the map (might be empty).
                    if (!bindingMaskOnThisMap.Matches(ref unresolvedBinding))
                        continue;

                    // Try to find action.
                    // NOTE: Technically, we allow individual bindings of composites to trigger actions independent
                    //       of the action triggered by the composite.
                    var actionIndex = InputActionMapState.kInvalidIndex;
                    var actionName = unresolvedBinding.action;
                    if (!string.IsNullOrEmpty(actionName))
                    {
                        actionIndex = map.TryGetActionIndex(actionName);
                    }
                    else if (map.m_SingletonAction != null)
                    {
                        // Special-case for singleton actions that don't have names.
                        actionIndex = 0;
                    }

                    // Skip binding if it doesn't match the binding mask on the action (might be empty).
                    if (actionIndex != InputActionMapState.kInvalidIndex &&
                        !map.m_Actions[actionIndex].m_BindingMask.Matches(ref unresolvedBinding))
                        continue;

                    // Instantiate processors.
                    var firstProcessorIndex = 0;
                    var numProcessors = 0;
                    var processors = unresolvedBinding.effectiveProcessors;
                    if (!string.IsNullOrEmpty(processors))
                    {
                        firstProcessorIndex = ResolveProcessors(processors);
                        if (processors != null)
                            numProcessors = totalProcessorCount - firstProcessorIndex;
                    }

                    // Instantiate interactions.
                    var firstInteractionIndex = 0;
                    var numInteractions = 0;
                    var interactions = unresolvedBinding.effectiveInteractions;
                    if (!string.IsNullOrEmpty(interactions))
                    {
                        firstInteractionIndex = ResolveInteractions(interactions);
                        if (interactionStates != null)
                            numInteractions = totalInteractionCount - firstInteractionIndex;
                    }

                    ////TODO: allow specifying parameters for composite on its path (same way as parameters work for interactions)
                    // If it's the start of a composite chain, create the composite.
                    if (unresolvedBinding.isComposite)
                    {
                        ////REVIEW: what to do about interactions on composites?

                        // Instantiate. For composites, the path is the name of the composite.
                        var composite = InstantiateBindingComposite(unresolvedBinding.path);
                        currentCompositeIndex =
                            ArrayHelpers.AppendWithCapacity(ref composites, ref totalCompositeCount, composite);
                        currentCompositeBindingIndex = bindingIndex;
                        bindingStates[bindingIndex] = new InputActionMapState.BindingState
                        {
                            actionIndex = actionIndex,
                            compositeOrCompositeBindingIndex = currentCompositeIndex,
                            processorStartIndex = firstProcessorIndex,
                            processorCount = numProcessors,
                            interactionCount = numInteractions,
                            interactionStartIndex = firstInteractionIndex,
                            mapIndex = totalMapCount,
                        };

                        // The composite binding entry itself does not resolve to any controls.
                        // It creates a composite binding object which is then populated from
                        // subsequent bindings.
                        continue;
                    }

                    // If we've reached the end of a composite chain, finish
                    // off the current composite.
                    if (!unresolvedBinding.isPartOfComposite &&
                        currentCompositeBindingIndex != InputActionMapState.kInvalidIndex)
                    {
                        currentCompositeBindingIndex = InputActionMapState.kInvalidIndex;
                        currentCompositeIndex = InputActionMapState.kInvalidIndex;
                    }

                    // Look up controls.
                    var firstControlIndex = totalControlCount;
                    var numControls = InputSystem.FindControls(path, ref resolvedControls);
                    if (numControls > 0)
                    {
                        resolvedControls.AppendTo(ref controls, ref totalControlCount);
                        resolvedControls.Clear();
                    }

                    // Add entry for resolved binding.
                    bindingStates[bindingIndex] = new InputActionMapState.BindingState
                    {
                        controlStartIndex = firstControlIndex,
                        controlCount = numControls,
                        interactionStartIndex = firstInteractionIndex,
                        interactionCount = numInteractions,
                        processorStartIndex = firstProcessorIndex,
                        processorCount = numProcessors,
                        isPartOfComposite = unresolvedBinding.isPartOfComposite,
                        actionIndex = actionIndex,
                        compositeOrCompositeBindingIndex = currentCompositeBindingIndex,
                        mapIndex = totalMapCount,
                    };

                    // If the binding is part of a composite, pass the resolve controls
                    // on to the composite.
                    if (unresolvedBinding.isPartOfComposite &&
                        currentCompositeBindingIndex != InputActionMapState.kInvalidIndex && numControls != 0)
                    {
                        ////REVIEW: what should we do when a single binding in a composite resolves to multiple controls?
                        ////        if the composite has more than one bindable control, it's not readily apparent how we would group them
                        if (numControls > 1)
                            throw new NotImplementedException(
                                "Handling case where single binding in composite resolves to multiple controls");

                        // Make sure the binding is named. The name determines what in the composite
                        // to bind to.
                        if (string.IsNullOrEmpty(unresolvedBinding.name))
                            throw new Exception(string.Format(
                                "Binding that is part of composite '{0}' is missing a name",
                                composites[currentCompositeIndex]));

                        // Install the control on the binding.
                        BindControlInComposite(composites[currentCompositeIndex], unresolvedBinding.name,
                            controls[firstControlIndex]);
                    }
                }
            }
            finally
            {
                resolvedControls.Dispose();
            }

            // Set up control to binding index mapping.
            var controlCountInThisMap = totalControlCount - controlStartIndex;
            ArrayHelpers.GrowBy(ref controlIndexToBindingIndex, controlCountInThisMap);
            for (var i = 0; i < bindingCountInThisMap; ++i)
            {
                var numControls = bindingStates[bindingStartIndex + i].controlCount;
                var startIndex = bindingStates[bindingStartIndex + i].controlStartIndex;
                for (var n = 0; n < numControls; ++n)
                    controlIndexToBindingIndex[startIndex + n] = i;
            }

            // Store indices for map.
            var numMaps = totalMapCount;
            var mapIndex = ArrayHelpers.AppendWithCapacity(ref maps, ref numMaps, map);
            ArrayHelpers.AppendWithCapacity(ref mapIndices, ref totalMapCount, new InputActionMapState.ActionMapIndices
            {
                actionStartIndex = actionStartIndex,
                actionCount = actionCountInThisMap,
                controlStartIndex = controlStartIndex,
                controlCount = controlCountInThisMap,
                bindingStartIndex = bindingStartIndex,
                bindingCount = bindingCountInThisMap,
                interactionStartIndex = interactionStartIndex,
                interactionCount = totalInteractionCount - interactionStartIndex,
                processorStartIndex = processorStartIndex,
                processorCount = totalProcessorCount - processorStartIndex,
                compositeStartIndex = compositeStartIndex,
                compositeCount = totalCompositeCount - compositeStartIndex,
            });
            map.m_MapIndexInState = mapIndex;

            // Allocate action states.
            if (actionCountInThisMap > 0)
            {
                // Assign action indices.
                var actions = map.m_Actions;
                for (var i = 0; i < actionCountInThisMap; ++i)
                    actions[i].m_ActionIndex = totalActionCount + i;

                ArrayHelpers.GrowBy(ref actionStates, actionCountInThisMap);
                totalActionCount += actionCountInThisMap;
                for (var i = 0; i < actionCountInThisMap; ++i)
                    actionStates[i].mapIndex = mapIndex;
            }
        }

        private int ResolveInteractions(string interactionString)
        {
            ////REVIEW: We're piggybacking off the processor parsing here as the two syntaxes are identical. Might consider
            ////        moving the logic to a shared place.
            ////        Alternatively, may split the paths. May help in getting rid of unnecessary allocations.

            var firstInteractionIndex = totalInteractionCount;
            if (!InputControlLayout.ParseNameAndParameterList(interactionString, ref m_Parameters))
                return firstInteractionIndex;

            for (var i = 0; i < m_Parameters.Count; ++i)
            {
                // Look up interaction.
                var type = InputInteraction.s_Interactions.LookupTypeRegistration(m_Parameters[i].name);
                if (type == null)
                    throw new Exception(string.Format(
                        "No interaction with name '{0}' (mentioned in '{1}') has been registered", m_Parameters[i].name,
                        interactionString));

                // Instantiate it.
                var interaction = Activator.CreateInstance(type) as IInputInteraction;
                if (interaction == null)
                    throw new Exception(string.Format("Interaction '{0}' is not an IInputInteraction", m_Parameters[i].name));

                // Pass parameters to it.
                InputDeviceBuilder.SetParameters(interaction, m_Parameters[i].parameters);

                // Add to list.
                var interactionStateCount = totalInteractionCount;
                ArrayHelpers.AppendWithCapacity(ref interactionStates, ref interactionStateCount,
                    new InputActionMapState.InteractionState
                    {
                        phase = InputActionPhase.Waiting
                    });
                ArrayHelpers.AppendWithCapacity(ref interactions, ref totalInteractionCount, interaction);
                Debug.Assert(interactionStateCount == totalInteractionCount);
            }

            return firstInteractionIndex;
        }

        private int ResolveProcessors(string processorString)
        {
            var firstProcessorIndex = totalProcessorCount;
            if (!InputControlLayout.ParseNameAndParameterList(processorString, ref m_Parameters))
                return firstProcessorIndex;

            for (var i = 0; i < m_Parameters.Count; ++i)
            {
                // Look up processor.
                var type = InputControlProcessor.s_Processors.LookupTypeRegistration(m_Parameters[i].name);
                if (type == null)
                    throw new Exception(string.Format(
                        "No processor with name '{0}' (mentioned in '{1}') has been registered", m_Parameters[i].name,
                        processorString));

                // Instantiate it.
                var processor = Activator.CreateInstance(type);

                // Pass parameters to it.
                InputDeviceBuilder.SetParameters(processor, m_Parameters[i].parameters);

                // Add to list.
                ArrayHelpers.AppendWithCapacity(ref processors, ref totalProcessorCount, processor);
            }

            return firstProcessorIndex;
        }

        private static object InstantiateBindingComposite(string nameAndParameters)
        {
            var nameAndParametersParsed = InputControlLayout.ParseNameAndParameters(nameAndParameters);

            // Look up.
            var type = InputBindingComposite.s_Composites.LookupTypeRegistration(nameAndParametersParsed.name);
            if (type == null)
                throw new Exception(string.Format("No binding composite with name '{0}' has been registered",
                    nameAndParametersParsed.name));

            // Instantiate.
            var instance = Activator.CreateInstance(type);
            ////REVIEW: typecheck for IInputBindingComposite? (at least in dev builds)

            // Set parameters.
            InputDeviceBuilder.SetParameters(instance, nameAndParametersParsed.parameters);

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
                throw new Exception(string.Format("Cannot find public field '{0}' in binding composite '{1}' of type '{2}'",
                    name, composite, type));

            // Typecheck.
            if (!typeof(InputControl).IsAssignableFrom(field.FieldType))
                throw new Exception(string.Format(
                    "Field '{0}' in binding composite '{1}' of type '{2}' is not an InputControl", name, composite,
                    type));

            field.SetValue(composite, control);
        }
    }
}
