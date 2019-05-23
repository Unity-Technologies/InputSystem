using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////TODO: reuse interaction, processor, and composite instances from prior resolves

namespace UnityEngine.InputSystem
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
    /// The data set up by a resolver is for consumption by <see cref="InputActionState"/>.
    /// Essentially, InputBindingResolver does all the wiring and <see cref="InputActionState"/>
    /// does all the actual execution based on the resulting data.
    /// </remarks>
    /// <seealso cref="InputActionState.Initialize"/>
    internal struct InputBindingResolver : IDisposable
    {
        public int totalProcessorCount;
        public int totalCompositeCount;
        public int totalInteractionCount;
        public int totalMapCount => memory.mapCount;
        public int totalActionCount => memory.actionCount;
        public int totalBindingCount => memory.bindingCount;
        public int totalControlCount => memory.controlCount;

        public InputActionMap[] maps;
        public InputControl[] controls;
        public InputActionState.UnmanagedMemory memory;
        public IInputInteraction[] interactions;
        public InputProcessor[] processors;
        public InputBindingComposite[] composites;

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
        /// an entry in <see cref="memory"/>. Otherwise we would have to have a more complicated
        /// mapping from <see cref="InputActionMap.bindings"/> to a binding state in <see cref="memory"/>.
        /// </remarks>
        public InputBinding? bindingMask;

        private List<NameAndParameters> m_Parameters;

        /// <summary>
        /// Release native memory held by the resolver.
        /// </summary>
        public void Dispose()
        {
            memory.Dispose();
        }

        /// <summary>
        /// Steal the already allocated arrays from the given state.
        /// </summary>
        /// <param name="state">Action map state that was previously created.</param>
        /// <remarks>
        /// This is useful to avoid allocating new arrays from scratch when re-resolving bindings.
        /// </remarks>
        public void StartWithArraysFrom(InputActionState state)
        {
            Debug.Assert(state != null);

            maps = state.maps;
            interactions = state.interactions;
            processors = state.processors;
            composites = state.composites;
            controls = state.controls;

            // Clear the arrays so that we don't leave references around.
            if (maps != null)
                Array.Clear(maps, 0, state.totalMapCount);
            if (interactions != null)
                Array.Clear(interactions, 0, state.totalInteractionCount);
            if (processors != null)
                Array.Clear(processors, 0, state.totalProcessorCount);
            if (composites != null)
                Array.Clear(composites, 0, state.totalCompositeCount);
            if (controls != null)
                Array.Clear(controls, 0, state.totalControlCount);

            // Null out the arrays on the state so that there is no strange bugs with
            // the state reading from arrays that no longer belong to it.
            state.maps = null;
            state.interactions = null;
            state.processors = null;
            state.composites = null;
            state.controls = null;
        }

        /// <summary>
        /// Resolve and add all bindings and actions from the given map.
        /// </summary>
        /// <param name="map"></param>
        /// <remarks>
        /// This is where all binding resolution happens for actions. The method walks through the binding array
        /// in <paramref name="map"/> and adds any controls, interactions, processors, and composites as it goes.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1809:AvoidExcessiveLocals", Justification = "TODO: Refactor later.")]
        public unsafe void AddActionMap(InputActionMap map)
        {
            Debug.Assert(map != null);

            var actionsInThisMap = map.m_Actions;
            var bindingsInThisMap = map.m_Bindings;
            var bindingCountInThisMap = bindingsInThisMap?.Length ?? 0;
            var actionCountInThisMap = actionsInThisMap?.Length ?? 0;
            var mapIndex = totalMapCount;

            // Keep track of indices for this map.
            var actionStartIndex = totalActionCount;
            var bindingStartIndex = totalBindingCount;
            var controlStartIndex = totalControlCount;
            var interactionStartIndex = totalInteractionCount;
            var processorStartIndex = totalProcessorCount;
            var compositeStartIndex = totalCompositeCount;

            // Allocate an initial block of memory. We probably will have to re-allocate once
            // at the end to accommodate interactions and controls added from the map.
            var newMemory = new InputActionState.UnmanagedMemory();
            newMemory.Allocate(
                mapCount: totalMapCount + 1,
                actionCount: totalActionCount + actionCountInThisMap,
                bindingCount: totalBindingCount + bindingCountInThisMap,
                // We reallocate for the following once we know the final count.
                interactionCount: totalInteractionCount,
                compositeCount: totalCompositeCount,
                controlCount: totalControlCount);
            if (memory.isAllocated)
                newMemory.CopyDataFrom(memory);

            ////TODO: make sure composite objects get all the bindings they need
            ////TODO: handle case where we have bindings resolving to the same control
            ////      (not so clear cut what to do there; each binding may have a different interaction setup, for example)
            var currentCompositeBindingIndex = InputActionState.kInvalidIndex;
            var currentCompositeIndex = InputActionState.kInvalidIndex;
            var currentCompositePartCount = 0;
            var currentCompositeActionIndexInMap = InputActionState.kInvalidIndex;
            InputAction currentCompositeAction = null;
            var bindingMaskOnThisMap = map.m_BindingMask;
            var devicesForThisMap = map.devices;

            // Can't use `using` as we need to use it with `ref`.
            var resolvedControls = new InputControlList<InputControl>(Allocator.Temp);

            // We gather all controls in temporary memory and then move them over into newMemory once
            // we're done resolving.
            try
            {
                for (var n = 0; n < bindingCountInThisMap; ++n)
                {
                    var bindingStatesPtr = newMemory.bindingStates;
                    ref var unresolvedBinding = ref bindingsInThisMap[n];
                    var bindingIndex = bindingStartIndex + n;
                    var isComposite = unresolvedBinding.isComposite;
                    var isPartOfComposite = !isComposite && unresolvedBinding.isPartOfComposite;
                    var bindingState = &bindingStatesPtr[bindingIndex];

                    try
                    {
                        ////TODO: if it's a composite, check if any of the children matches our binding masks (if any) and skip composite if none do

                        // Set binding state to defaults.
                        bindingState->mapIndex = totalMapCount;
                        bindingState->compositeOrCompositeBindingIndex = InputActionState.kInvalidIndex;
                        bindingState->actionIndex = InputActionState.kInvalidIndex;

                        // Make sure that if it's part of a composite, we are actually part of a composite.
                        if (isPartOfComposite && currentCompositeBindingIndex == InputActionState.kInvalidIndex)
                            throw new Exception(
                                $"Binding '{unresolvedBinding}' is marked as being part of a composite but the preceding binding is not a composite");

                        // Skip binding if it is disabled (path is empty string).
                        var path = unresolvedBinding.effectivePath;
                        if (unresolvedBinding.path == "")
                            continue;

                        // Skip binding if it doesn't match with our binding mask (might be empty).
                        if (!isComposite && bindingMask != null && !bindingMask.Value.Matches(ref unresolvedBinding))
                            continue;

                        // Skip binding if it doesn't match the binding mask on the map (might be empty).
                        if (!isComposite && bindingMaskOnThisMap != null &&
                            !bindingMaskOnThisMap.Value.Matches(ref unresolvedBinding))
                            continue;

                        // Try to find action.
                        //
                        // NOTE: We ignore actions on bindings that are part of composites. We only allow
                        //       actions to be triggered from the composite itself.
                        var actionIndexInMap = InputActionState.kInvalidIndex;
                        var actionName = unresolvedBinding.action;
                        InputAction action = null;
                        if (!isPartOfComposite)
                        {
                            if (!string.IsNullOrEmpty(actionName))
                            {
                                ////REVIEW: should we fail here if we don't manage to find the action
                                actionIndexInMap = map.TryGetActionIndex(actionName);
                            }
                            else if (map.m_SingletonAction != null)
                            {
                                // Special-case for singleton actions that don't have names.
                                actionIndexInMap = 0;
                            }

                            if (actionIndexInMap != InputActionState.kInvalidIndex)
                                action = actionsInThisMap[actionIndexInMap];
                        }
                        else
                        {
                            actionIndexInMap = currentCompositeActionIndexInMap;
                            action = currentCompositeAction;
                        }

                        // Skip binding if it doesn't match the binding mask on the action (might be empty).
                        if (!isComposite && action?.m_BindingMask != null &&
                            !action.m_BindingMask.Value.Matches(ref unresolvedBinding))
                            continue;

                        // Instantiate processors.
                        var firstProcessorIndex = InputActionState.kInvalidIndex;
                        var numProcessors = 0;
                        var processorString = unresolvedBinding.effectiveProcessors;
                        if (!string.IsNullOrEmpty(processorString))
                        {
                            // Add processors from binding.
                            firstProcessorIndex = ResolveProcessors(processorString);
                            if (firstProcessorIndex != InputActionState.kInvalidIndex)
                                numProcessors = totalProcessorCount - firstProcessorIndex;
                        }
                        if (action != null && !string.IsNullOrEmpty(action.m_Processors))
                        {
                            // Add processors from action.
                            var index = ResolveProcessors(action.m_Processors);
                            if (index != InputActionState.kInvalidIndex)
                            {
                                if (firstProcessorIndex == InputActionState.kInvalidIndex)
                                    firstProcessorIndex = index;
                                numProcessors += totalProcessorCount - index;
                            }
                        }

                        // Instantiate interactions.
                        var firstInteractionIndex = InputActionState.kInvalidIndex;
                        var numInteractions = 0;
                        var interactionString = unresolvedBinding.effectiveInteractions;
                        if (!string.IsNullOrEmpty(interactionString))
                        {
                            // Add interactions from binding.
                            firstInteractionIndex = ResolveInteractions(interactionString);
                            if (firstInteractionIndex != InputActionState.kInvalidIndex)
                                numInteractions = totalInteractionCount - firstInteractionIndex;
                        }
                        if (action != null && !string.IsNullOrEmpty(action.m_Interactions))
                        {
                            // Add interactions from action.
                            var index = ResolveInteractions(action.m_Interactions);
                            if (index != InputActionState.kInvalidIndex)
                            {
                                if (firstInteractionIndex == InputActionState.kInvalidIndex)
                                    firstInteractionIndex = index;
                                numInteractions += totalInteractionCount - index;
                            }
                        }

                        // If it's the start of a composite chain, create the composite.
                        if (isComposite)
                        {
                            var actionIndexForComposite = actionIndexInMap != InputActionState.kInvalidIndex
                                ? actionStartIndex + actionIndexInMap
                                : InputActionState.kInvalidIndex;

                            // Instantiate. For composites, the path is the name of the composite.
                            var composite = InstantiateBindingComposite(unresolvedBinding.path);
                            currentCompositeIndex =
                                ArrayHelpers.AppendWithCapacity(ref composites, ref totalCompositeCount, composite);
                            currentCompositeBindingIndex = bindingIndex;
                            currentCompositeAction = action;
                            currentCompositeActionIndexInMap = actionIndexInMap;

                            *bindingState = new InputActionState.BindingState
                            {
                                actionIndex = actionIndexForComposite,
                                compositeOrCompositeBindingIndex = currentCompositeIndex,
                                processorStartIndex = firstProcessorIndex,
                                processorCount = numProcessors,
                                interactionCount = numInteractions,
                                interactionStartIndex = firstInteractionIndex,
                                mapIndex = totalMapCount,
                                isComposite = true,
                                // Record where the controls for parts of the composite start.
                                controlStartIndex = memory.controlCount + resolvedControls.Count,
                            };

                            // The composite binding entry itself does not resolve to any controls.
                            // It creates a composite binding object which is then populated from
                            // subsequent bindings.
                            continue;
                        }

                        // If we've reached the end of a composite chain, finish
                        // off the current composite.
                        if (!isPartOfComposite && currentCompositeBindingIndex != InputActionState.kInvalidIndex)
                        {
                            currentCompositePartCount = 0;
                            currentCompositeBindingIndex = InputActionState.kInvalidIndex;
                            currentCompositeIndex = InputActionState.kInvalidIndex;
                            currentCompositeAction = null;
                            currentCompositeActionIndexInMap = InputActionState.kInvalidIndex;
                        }

                        // Look up controls.
                        //
                        // NOTE: We continuously add controls here to `resolvedControls`. Once we've completed our
                        //       pass over the bindings in the map, `resolvedControls` will have all the controls for
                        //       the current map.
                        var firstControlIndex = memory.controlCount + resolvedControls.Count;
                        var numControls = 0;
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

                        // If the binding is part of a composite, pass the resolved controls
                        // on to the composite.
                        var partIndex = InputActionState.kInvalidIndex;
                        var actionIndexForBinding = InputActionState.kInvalidIndex;
                        if (isPartOfComposite && currentCompositeBindingIndex != InputActionState.kInvalidIndex && numControls > 0)
                        {
                            // Make sure the binding is named. The name determines what in the composite
                            // to bind to.
                            if (string.IsNullOrEmpty(unresolvedBinding.name))
                                throw new Exception(
                                    $"Binding '{unresolvedBinding}' that is part of composite '{composites[currentCompositeIndex]}' is missing a name");

                            // Give a part index for the
                            partIndex = AssignCompositePartIndex(composites[currentCompositeIndex], unresolvedBinding.name,
                                ref currentCompositePartCount);

                            // Keep track of total number of controls bound in the composite.
                            bindingStatesPtr[currentCompositeBindingIndex].controlCount += numControls;

                            // Force action index on part binding to be same as that of composite.
                            actionIndexForBinding = bindingStatesPtr[currentCompositeBindingIndex].actionIndex;
                        }
                        else if (actionIndexInMap != InputActionState.kInvalidIndex)
                        {
                            actionIndexForBinding = actionStartIndex + actionIndexInMap;
                        }

                        // Add entry for resolved binding.
                        *bindingState = new InputActionState.BindingState
                        {
                            controlStartIndex = firstControlIndex,
                            controlCount = numControls,
                            interactionStartIndex = firstInteractionIndex,
                            interactionCount = numInteractions,
                            processorStartIndex = firstProcessorIndex,
                            processorCount = numProcessors,
                            isPartOfComposite = unresolvedBinding.isPartOfComposite,
                            partIndex = partIndex,
                            actionIndex = actionIndexForBinding,
                            compositeOrCompositeBindingIndex = currentCompositeBindingIndex,
                            mapIndex = totalMapCount,
                            wantsInitialStateCheck = action?.initialStateCheck ?? false
                        };
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(
                            $"{exception.GetType().Name} while resolving binding '{unresolvedBinding}' in action map '{map}'");
                        Debug.LogException(exception);

                        // Don't swallow exceptions that indicate something is wrong in the code rather than
                        // in the data.
                        if (exception.IsExceptionIndicatingBugInCode())
                            throw exception;
                    }
                }

                // Re-allocate memory to accommodate controls and interaction states. The count for those
                // we only know once we've completed all resolution.
                var controlCountInThisMap = resolvedControls.Count;
                var newTotalControlCount = memory.controlCount + controlCountInThisMap;
                if (newMemory.interactionCount != totalInteractionCount ||
                    newMemory.compositeCount != totalCompositeCount ||
                    newMemory.controlCount != newTotalControlCount)
                {
                    var finalMemory = new InputActionState.UnmanagedMemory();

                    finalMemory.Allocate(
                        mapCount: newMemory.mapCount,
                        actionCount: newMemory.actionCount,
                        bindingCount: newMemory.bindingCount,
                        controlCount: newTotalControlCount,
                        interactionCount: totalInteractionCount,
                        compositeCount: totalCompositeCount);

                    finalMemory.CopyDataFrom(newMemory);

                    newMemory.Dispose();
                    newMemory = finalMemory;
                }

                // Add controls to array.
                var controlCountInArray = memory.controlCount;
                ArrayHelpers.AppendListWithCapacity(ref controls, ref controlCountInArray, resolvedControls);
                Debug.Assert(controlCountInArray == newTotalControlCount,
                    "Control array should have combined count of old and new controls");

                // Set up control to binding index mapping.
                for (var i = 0; i < bindingCountInThisMap; ++i)
                {
                    var bindingStatesPtr = newMemory.bindingStates;
                    var bindingState = &bindingStatesPtr[bindingStartIndex + i];
                    var numControls = bindingState->controlCount;
                    var startIndex = bindingState->controlStartIndex;
                    for (var n = 0; n < numControls; ++n)
                        newMemory.controlIndexToBindingIndex[startIndex + n] = bindingStartIndex + i;
                }

                // Initialize initial interaction states.
                for (var i = memory.interactionCount; i < newMemory.interactionCount; ++i)
                    newMemory.interactionStates[i].phase = InputActionPhase.Waiting;

                // Initialize action data.
                var runningIndexInBindingIndices = memory.bindingCount;
                for (var i = 0; i < actionCountInThisMap; ++i)
                {
                    var action = actionsInThisMap[i];
                    var actionIndex = actionStartIndex + i;

                    // Correlate action with its trigger state.
                    action.m_ActionIndex = actionIndex;

                    // Collect bindings for action.
                    var bindingStartIndexForAction = runningIndexInBindingIndices;
                    var bindingCountForAction = 0;
                    var numPossibleConcurrentActuations = 0;

                    for (var n = 0; n < bindingCountInThisMap; ++n)
                    {
                        var bindingIndex = bindingStartIndex + n;
                        var bindingState = &newMemory.bindingStates[bindingIndex];
                        if (bindingState->actionIndex != actionIndex)
                            continue;
                        if (bindingState->isPartOfComposite)
                            continue;

                        Debug.Assert(bindingIndex <= ushort.MaxValue, "Binding index exceeds limit");
                        newMemory.actionBindingIndices[runningIndexInBindingIndices] = (ushort)bindingIndex;
                        ++runningIndexInBindingIndices;
                        ++bindingCountForAction;

                        // Keep track of how many concurrent actuations we may be seeing on the action so that
                        // we know whether we need to enable conflict resolution or not.
                        if (bindingState->isComposite)
                        {
                            // Composite binding. Actuates as a whole. Check if the composite has successfully
                            // resolved any controls. If so, it adds one possible actuation.
                            if (bindingState->controlCount > 0)
                                ++numPossibleConcurrentActuations;
                        }
                        else
                        {
                            // Normal binding. Every successfully resolved control results in one possible actuation.
                            numPossibleConcurrentActuations += bindingState->controlCount;
                        }
                    }
                    Debug.Assert(bindingStartIndexForAction < ushort.MaxValue, "Binding start index on action exceeds limit");
                    Debug.Assert(bindingCountForAction < ushort.MaxValue, "Binding count on action exceeds limit");
                    newMemory.actionBindingIndicesAndCounts[i * 2] = (ushort)bindingStartIndexForAction;
                    newMemory.actionBindingIndicesAndCounts[i * 2 + 1] = (ushort)bindingCountForAction;

                    // See if we may need conflict resolution on this action. Never needed for pass-through actions.
                    // Otherwise, if we have more than one bound control or have several bindings and one of them
                    // is a composite, we enable it.
                    var isPassThroughAction = action.passThrough;
                    var mayNeedConflictResolution = !isPassThroughAction && numPossibleConcurrentActuations > 1;

                    // Initialize initial trigger state.
                    newMemory.actionStates[actionIndex] =
                        new InputActionState.TriggerState
                    {
                        phase = InputActionPhase.Disabled,
                        mapIndex = mapIndex,
                        controlIndex = InputActionState.kInvalidIndex,
                        interactionIndex = InputActionState.kInvalidIndex,
                        continuous = action.continuous,
                        passThrough = isPassThroughAction,
                        mayNeedConflictResolution = mayNeedConflictResolution,
                    };
                }

                // Store indices for map.
                newMemory.mapIndices[mapIndex] =
                    new InputActionState.ActionMapIndices
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
                };
                map.m_MapIndexInState = mapIndex;
                var finalActionMapCount = memory.mapCount;
                ArrayHelpers.AppendWithCapacity(ref maps, ref finalActionMapCount, map, capacityIncrement: 4);
                Debug.Assert(finalActionMapCount == newMemory.mapCount,
                    "Final action map count should match old action map count plus one");

                // As a final act, swap the new memory in.
                memory.Dispose();
                memory = newMemory;
            }
            catch (Exception)
            {
                // Don't leak our native memory when we throw an exception.
                newMemory.Dispose();
                throw;
            }
            finally
            {
                resolvedControls.Dispose();
            }
        }

        private int ResolveInteractions(string interactionString)
        {
            ////REVIEW: We're piggybacking off the processor parsing here as the two syntaxes are identical. Might consider
            ////        moving the logic to a shared place.
            ////        Alternatively, may split the paths. May help in getting rid of unnecessary allocations.

            if (!NameAndParameters.ParseMultiple(interactionString, ref m_Parameters))
                return InputActionState.kInvalidIndex;

            var firstInteractionIndex = totalInteractionCount;
            for (var i = 0; i < m_Parameters.Count; ++i)
            {
                // Look up interaction.
                var type = InputInteraction.s_Interactions.LookupTypeRegistration(m_Parameters[i].name);
                if (type == null)
                    throw new Exception(
                        $"No interaction with name '{m_Parameters[i].name}' (mentioned in '{interactionString}') has been registered");

                // Instantiate it.
                if (!(Activator.CreateInstance(type) is IInputInteraction interaction))
                    throw new Exception($"Interaction '{m_Parameters[i].name}' is not an IInputInteraction");

                // Pass parameters to it.
                NamedValue.ApplyAllToObject(interaction, m_Parameters[i].parameters);

                // Add to list.
                ArrayHelpers.AppendWithCapacity(ref interactions, ref totalInteractionCount, interaction);
            }

            return firstInteractionIndex;
        }

        private int ResolveProcessors(string processorString)
        {
            if (!NameAndParameters.ParseMultiple(processorString, ref m_Parameters))
                return InputActionState.kInvalidIndex;

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
                NamedValue.ApplyAllToObject(processor, m_Parameters[i].parameters);

                // Add to list.
                ArrayHelpers.AppendWithCapacity(ref processors, ref totalProcessorCount, processor);
            }

            return firstProcessorIndex;
        }

        private static InputBindingComposite InstantiateBindingComposite(string nameAndParameters)
        {
            var nameAndParametersParsed = NameAndParameters.Parse(nameAndParameters);

            // Look up.
            var type = InputBindingComposite.s_Composites.LookupTypeRegistration(nameAndParametersParsed.name);
            if (type == null)
                throw new Exception(
                    $"No binding composite with name '{nameAndParametersParsed.name}' has been registered");

            // Instantiate.
            if (!(Activator.CreateInstance(type) is InputBindingComposite instance))
                throw new Exception(
                    $"Registered type '{type.Name}' used for '{nameAndParametersParsed.name}' is not an InputBindingComposite");

            // Set parameters.
            NamedValue.ApplyAllToObject(instance, nameAndParametersParsed.parameters);

            return instance;
        }

        private static int AssignCompositePartIndex(object composite, string name, ref int currentCompositePartCount)
        {
            var type = composite.GetType();

            ////REVIEW: check for [InputControl] attribute?

            ////TODO: allow this to be a property instead
            // Look up field.
            var field = type.GetField(name,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                throw new Exception(
                    $"Cannot find public field '{name}' used as parameter of binding composite '{composite}' of type '{type}'");

            ////REVIEW: should we wrap part numbers in a struct instead of using int?

            // Type-check.
            var fieldType = field.FieldType;
            if (fieldType != typeof(int))
                throw new Exception(
                    $"Field '{name}' used as a parameter of binding composite '{composite}' must be of type 'int' but is of type '{type.Name}' instead");

            ////REVIEW: this create garbage; need a better solution to get to zero garbage during re-resolving
            // See if we've already assigned a part index. This can happen if there are multiple bindings
            // for the same named slot on the composite (e.g. multiple "Negative" bindings on an axis composite).
            var partIndex = (int)field.GetValue(composite);
            if (partIndex == 0)
            {
                // No, not assigned yet. Create new part index.
                partIndex = ++currentCompositePartCount;
                field.SetValue(composite, partIndex);
            }

            return partIndex;
        }
    }
}
