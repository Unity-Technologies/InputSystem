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
        public InputProcessor[] processors;
        public InputBindingComposite[] composites;

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
        public InputBinding? bindingMask;

        private List<InputControlLayout.NameAndParameters> m_Parameters;

        /// <summary>
        /// Steal the already allocated arrays from the given state.
        /// </summary>
        /// <param name="state">Action map state that was previously created.</param>
        /// <remarks>
        /// This is useful to avoid allocating new arrays from scratch when re-resolving bindings.
        /// </remarks>
        public void StartWithArraysFrom(InputActionMapState state)
        {
            Debug.Assert(state != null);

            maps = state.maps;
            mapIndices = state.mapIndices;
            actionStates = state.actionStates;
            bindingStates = state.bindingStates;
            interactionStates = state.interactionStates;
            interactions = state.interactions;
            processors = state.processors;
            composites = state.composites;
            controls = state.controls;
            controlIndexToBindingIndex = state.controlIndexToBindingIndex;

            // Clear the arrays so that we don't leave references around.
            if (maps != null)
                Array.Clear(maps, 0, state.totalMapCount);
            if (mapIndices != null)
                Array.Clear(mapIndices, 0, state.totalMapCount);
            if (actionStates != null)
                Array.Clear(actionStates, 0, state.totalActionCount);
            if (bindingStates != null)
                Array.Clear(bindingStates, 0, state.totalBindingCount);
            if (interactionStates != null)
                Array.Clear(interactionStates, 0, state.totalInteractionCount);
            if (interactions != null)
                Array.Clear(interactions, 0, state.totalInteractionCount);
            if (processors != null)
                Array.Clear(processors, 0, state.totalProcessorCount);
            if (composites != null)
                Array.Clear(composites, 0, state.totalCompositeCount);
            if (controls != null)
                Array.Clear(controls, 0, state.totalControlCount);
            if (controlIndexToBindingIndex != null)
                Array.Clear(controlIndexToBindingIndex, 0, state.totalControlCount);

            // Null out the arrays on the state so that there is no strange bugs with
            // the state reading from arrays that no longer belong to it.
            state.maps = null;
            state.mapIndices = null;
            state.actionStates = null;
            state.bindingStates = null;
            state.interactionStates = null;
            state.interactions = null;
            state.processors = null;
            state.composites = null;
            state.controls = null;
            state.controlIndexToBindingIndex = null;
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
            var bindingCountInThisMap = bindingsInThisMap?.Length ?? 0;
            ArrayHelpers.GrowWithCapacity(ref bindingStates, ref totalBindingCount, bindingCountInThisMap);

            ////TODO: make sure composite objects get all the bindings they need
            ////TODO: handle case where we have bindings resolving to the same control
            ////      (not so clear cut what to do there; each binding may have a different interaction setup, for example)
            var currentCompositeBindingIndex = InputActionMapState.kInvalidIndex;
            var currentCompositeIndex = InputActionMapState.kInvalidIndex;
            var currentCompositePartIndex = 0;
            var bindingMaskOnThisMap = map.m_BindingMask;
            var actionsInThisMap = map.m_Actions;
            var devicesForThisMap = map.devices;
            var actionCountInThisMap = actionsInThisMap?.Length ?? 0;
            var resolvedControls = new InputControlList<InputControl>(Allocator.Temp);
            try
            {
                for (var n = 0; n < bindingCountInThisMap; ++n)
                {
                    var unresolvedBinding = bindingsInThisMap[n];
                    var bindingIndex = bindingStartIndex + n;
                    var isComposite = unresolvedBinding.isComposite;

                    try
                    {
                        ////TODO: if it's a composite, check if any of the children matches our binding masks (if any) and skip composite if none do

                        // Set binding state to defaults.
                        bindingStates[bindingIndex].mapIndex = totalMapCount;
                        bindingStates[bindingIndex].compositeOrCompositeBindingIndex = InputActionMapState.kInvalidIndex;
                        bindingStates[bindingIndex].actionIndex = InputActionMapState.kInvalidIndex;

                        // Skip binding if it is disabled (path is empty string).
                        var path = unresolvedBinding.effectivePath;
                        if (unresolvedBinding.path == "")
                            continue;

                        // Skip binding if it doesn't match with our binding mask (might be empty).
                        if (!isComposite && bindingMask != null && !bindingMask.Value.Matches(ref unresolvedBinding))
                            continue;

                        // Skip binding if it doesn't match the binding mask on the map (might be empty).
                        if (!isComposite && bindingMaskOnThisMap != null && !bindingMaskOnThisMap.Value.Matches(ref unresolvedBinding))
                            continue;

                        // Try to find action.
                        // NOTE: Technically, we allow individual bindings of composites to trigger actions independent
                        //       of the action triggered by the composite.
                        var actionIndexInMap = InputActionMapState.kInvalidIndex;
                        var actionName = unresolvedBinding.action;
                        if (!string.IsNullOrEmpty(actionName))
                        {
                            actionIndexInMap = map.TryGetActionIndex(actionName);
                        }
                        else if (map.m_SingletonAction != null)
                        {
                            // Special-case for singleton actions that don't have names.
                            actionIndexInMap = 0;
                        }
                        InputAction action = null;
                        if (actionIndexInMap != InputActionMapState.kInvalidIndex)
                            action = actionsInThisMap[actionIndexInMap];

                        // Skip binding if it doesn't match the binding mask on the action (might be empty).
                        if (!isComposite && action?.m_BindingMask != null && !action.m_BindingMask.Value.Matches(ref unresolvedBinding))
                            continue;

                        // Instantiate processors.
                        var firstProcessorIndex = InputActionMapState.kInvalidIndex;
                        var numProcessors = 0;
                        var processors = unresolvedBinding.effectiveProcessors;
                        if (!string.IsNullOrEmpty(processors))
                        {
                            // Add processors from binding.
                            firstProcessorIndex = ResolveProcessors(processors);
                            if (firstProcessorIndex != InputActionMapState.kInvalidIndex)
                                numProcessors = totalProcessorCount - firstProcessorIndex;
                        }
                        if (!string.IsNullOrEmpty(action.m_Processors))
                        {
                            // Add processors from action.
                            var index = ResolveProcessors(action.m_Processors);
                            if (index != InputActionMapState.kInvalidIndex)
                            {
                                if (firstProcessorIndex == InputActionMapState.kInvalidIndex)
                                    firstProcessorIndex = index;
                                numProcessors += totalProcessorCount - index;
                            }
                        }

                        // Instantiate interactions.
                        var firstInteractionIndex = InputActionMapState.kInvalidIndex;
                        var numInteractions = 0;
                        var interactions = unresolvedBinding.effectiveInteractions;
                        if (!string.IsNullOrEmpty(interactions))
                        {
                            // Add interactions from binding.
                            firstInteractionIndex = ResolveInteractions(interactions);
                            if (firstInteractionIndex != InputActionMapState.kInvalidIndex)
                                numInteractions = totalInteractionCount - firstInteractionIndex;
                        }

                        if (!string.IsNullOrEmpty(action.m_Interactions))
                        {
                            // Add interactions from action.
                            var index = ResolveInteractions(action.m_Interactions);
                            if (index != InputActionMapState.kInvalidIndex)
                            {
                                if (firstInteractionIndex == InputActionMapState.kInvalidIndex)
                                    firstInteractionIndex = index;
                                numInteractions += totalInteractionCount - index;
                            }
                        }

                        // If it's the start of a composite chain, create the composite.
                        if (unresolvedBinding.isComposite)
                        {
                            // Instantiate. For composites, the path is the name of the composite.
                            var composite = InstantiateBindingComposite(unresolvedBinding.path);
                            currentCompositeIndex =
                                ArrayHelpers.AppendWithCapacity(ref composites, ref totalCompositeCount, composite);
                            currentCompositeBindingIndex = bindingIndex;
                            bindingStates[bindingIndex] = new InputActionMapState.BindingState
                            {
                                actionIndex = actionStartIndex + actionIndexInMap,
                                compositeOrCompositeBindingIndex = currentCompositeIndex,
                                processorStartIndex = firstProcessorIndex,
                                processorCount = numProcessors,
                                interactionCount = numInteractions,
                                interactionStartIndex = firstInteractionIndex,
                                mapIndex = totalMapCount,
                                isComposite = true,
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
                            currentCompositePartIndex = 0;
                            currentCompositeBindingIndex = InputActionMapState.kInvalidIndex;
                            currentCompositeIndex = InputActionMapState.kInvalidIndex;
                        }

                        // Look up controls.
                        var firstControlIndex = totalControlCount;
                        int numControls = 0;
                        if (devicesForThisMap != null)
                        {
                            // Search in devices for only this map.
                            var list = devicesForThisMap.Value;
                            for (var i = 0; i < list.Count; ++i)
                            {
                                var device = list[i];
                                if (!device.added)
                                    continue; // Skip devices that have been removed.
                                numControls += InputControlPath.TryFindControls(device, path, 0, ref resolvedControls);
                            }
                        }
                        else
                        {
                            // Search globally.
                            numControls = InputSystem.FindControls(path, ref resolvedControls);
                        }
                        if (numControls > 0)
                        {
                            resolvedControls.AppendTo(ref controls, ref totalControlCount);
                            resolvedControls.Clear();
                        }

                        // If the binding is part of a composite, pass the resolved controls
                        // on to the composite.
                        if (unresolvedBinding.isPartOfComposite &&
                            currentCompositeBindingIndex != InputActionMapState.kInvalidIndex && numControls > 0)
                        {
                            // Make sure the binding is named. The name determines what in the composite
                            // to bind to.
                            if (string.IsNullOrEmpty(unresolvedBinding.name))
                                throw new Exception(
                                    $"Binding with path '{path}' that is part of composite '{composites[currentCompositeIndex]}' is missing a name");

                            // Install the controls on the binding.
                            BindControlInComposite(composites[currentCompositeIndex], unresolvedBinding.name,
                                ref currentCompositePartIndex);
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
                            partIndex = currentCompositePartIndex,
                            actionIndex = actionStartIndex + actionIndexInMap,
                            compositeOrCompositeBindingIndex = currentCompositeBindingIndex,
                            mapIndex = totalMapCount,
                        };
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"{exception.GetType().Name} while resolving binding '{unresolvedBinding}' in action map '{map}'");
                        Debug.LogException(exception);

                        if (exception.IsExceptionIndicatingBugInCode())
                            throw exception;
                    }
                }
            }
            finally
            {
                resolvedControls.Dispose();
            }

            // Set up control to binding index mapping.
            var controlCountInThisMap = totalControlCount - controlStartIndex;
            var controlIndexToBindingIndexCount = controlStartIndex;
            ArrayHelpers.GrowWithCapacity(ref controlIndexToBindingIndex, ref controlIndexToBindingIndexCount,
                controlCountInThisMap);
            for (var i = 0; i < bindingCountInThisMap; ++i)
            {
                var numControls = bindingStates[bindingStartIndex + i].controlCount;
                var startIndex = bindingStates[bindingStartIndex + i].controlStartIndex;
                for (var n = 0; n < numControls; ++n)
                    controlIndexToBindingIndex[startIndex + n] = bindingStartIndex + i;
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

            // Allocate action trigger states.
            if (actionCountInThisMap > 0)
            {
                ArrayHelpers.GrowWithCapacity(ref actionStates, ref totalActionCount, actionCountInThisMap);

                // Assign action indices.
                var actions = map.m_Actions;
                for (var i = 0; i < actionCountInThisMap; ++i)
                    actions[i].m_ActionIndex = actionStartIndex + i;

                // Set initial trigger states.
                for (var i = 0; i < actionCountInThisMap; ++i)
                {
                    var action = actions[i];
                    var actionIndex = actionStartIndex + i;

                    actionStates[actionIndex] = new InputActionMapState.TriggerState
                    {
                        phase = InputActionPhase.Disabled,
                        mapIndex = mapIndex,
                        controlIndex = InputActionMapState.kInvalidIndex,
                        interactionIndex = InputActionMapState.kInvalidIndex,
                        continuous = action.continuous,
                    };
                }
            }
        }

        private int ResolveInteractions(string interactionString)
        {
            ////REVIEW: We're piggybacking off the processor parsing here as the two syntaxes are identical. Might consider
            ////        moving the logic to a shared place.
            ////        Alternatively, may split the paths. May help in getting rid of unnecessary allocations.

            if (!InputControlLayout.ParseNameAndParameterList(interactionString, ref m_Parameters))
                return InputActionMapState.kInvalidIndex;

            var firstInteractionIndex = totalInteractionCount;
            for (var i = 0; i < m_Parameters.Count; ++i)
            {
                // Look up interaction.
                var type = InputInteraction.s_Interactions.LookupTypeRegistration(m_Parameters[i].name);
                if (type == null)
                    throw new Exception(
                        $"No interaction with name '{m_Parameters[i].name}' (mentioned in '{interactionString}') has been registered");

                // Instantiate it.
                var interaction = Activator.CreateInstance(type) as IInputInteraction;
                if (interaction == null)
                    throw new Exception($"Interaction '{m_Parameters[i].name}' is not an IInputInteraction");

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
            if (!InputControlLayout.ParseNameAndParameterList(processorString, ref m_Parameters))
                return InputActionMapState.kInvalidIndex;

            var firstProcessorIndex = totalProcessorCount;
            for (var i = 0; i < m_Parameters.Count; ++i)
            {
                // Look up processor.
                var type = InputProcessor.s_Processors.LookupTypeRegistration(m_Parameters[i].name);
                if (type == null)
                    throw new Exception(
                        $"No processor with name '{m_Parameters[i].name}' (mentioned in '{processorString}') has been registered");

                // Instantiate it.
                if (!(Activator.CreateInstance(type) is InputProcessor processor))
                    throw new Exception(
                        $"Type '{type.Name}' registered as processor called '{m_Parameters[i].name}' is not an InputProcessor");

                // Pass parameters to it.
                InputDeviceBuilder.SetParameters(processor, m_Parameters[i].parameters);

                // Add to list.
                ArrayHelpers.AppendWithCapacity(ref processors, ref totalProcessorCount, processor);
            }

            return firstProcessorIndex;
        }

        private static InputBindingComposite InstantiateBindingComposite(string nameAndParameters)
        {
            var nameAndParametersParsed = InputControlLayout.ParseNameAndParameters(nameAndParameters);

            // Look up.
            var type = InputBindingComposite.s_Composites.LookupTypeRegistration(nameAndParametersParsed.name);
            if (type == null)
                throw new Exception(
                    $"No binding composite with name '{nameAndParametersParsed.name}' has been registered");

            // Instantiate.
            var instance = Activator.CreateInstance(type) as InputBindingComposite;
            if (instance == null)
                throw new Exception(
                    $"Registered type '{type.Name}' used for '{nameAndParametersParsed.name}' is not an InputBindingComposite");

            // Set parameters.
            InputDeviceBuilder.SetParameters(instance, nameAndParametersParsed.parameters);

            return instance;
        }

        private static void BindControlInComposite(object composite, string name, ref int currentCompositePartIndex)
        {
            var type = composite.GetType();

            ////TODO: allow this to be a property instead
            // Look up field.
            var field = type.GetField(name,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                throw new Exception(
                    $"Cannot find public field '{name}' in binding composite '{composite}' of type '{type}'");

            ////REVIEW: look for [InputControl] and perform checks based on it?
            ////REVIEW: should we wrap part numbers in a struct instead of using int?

            // Type-check.
            var fieldType = field.FieldType;
            if (fieldType != typeof(int))
                throw new Exception(
                    $"Field '{name}' in binding composite '{composite}' must be of type 'int' but is of type '{type.Name}' instead");

            // See if we've already assigned a part index.
            int partIndex;
            var fieldValue = (int)field.GetValue(composite);
            if (fieldValue == 0)
            {
                partIndex = ++currentCompositePartIndex;
            }
            else
            {
                partIndex = fieldValue;
            }

            field.SetValue(composite, partIndex);
        }
    }
}
