using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
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

        private bool m_IsControlOnlyResolve;

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
        /// <param name="isFullResolve">If false, the only thing that is allowed to change in the re-resolution
        /// is the list of controls. In other words, devices may have been added or removed but otherwise the configuration
        /// is exactly the same as in the last resolve. If true, anything may have changed and the resolver will only reuse
        /// allocations but not contents.</param>
        public void StartWithPreviousResolve(InputActionState state, bool isFullResolve)
        {
            Debug.Assert(state != null, "Received null state");
            Debug.Assert(!state.isProcessingControlStateChange,
                "Cannot re-resolve bindings for an InputActionState that is currently executing an action callback; binding resolution must be deferred to until after the callback has completed");

            m_IsControlOnlyResolve = !isFullResolve;

            maps = state.maps;
            interactions = state.interactions;
            processors = state.processors;
            composites = state.composites;
            controls = state.controls;

            // Clear the arrays so that we don't leave references around.
            if (isFullResolve)
            {
                if (maps != null)
                    Array.Clear(maps, 0, state.totalMapCount);
                if (interactions != null)
                    Array.Clear(interactions, 0, state.totalInteractionCount);
                if (processors != null)
                    Array.Clear(processors, 0, state.totalProcessorCount);
                if (composites != null)
                    Array.Clear(composites, 0, state.totalCompositeCount);
            }
            if (controls != null) // Always clear this one as every resolve will change it.
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
        /// <param name="actionMap"></param>
        /// <remarks>
        /// This is where all binding resolution happens for actions. The method walks through the binding array
        /// in <paramref name="actionMap"/> and adds any controls, interactions, processors, and composites as it goes.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1809:AvoidExcessiveLocals", Justification = "TODO: Refactor later.")]
        public unsafe void AddActionMap(InputActionMap actionMap)
        {
            Debug.Assert(actionMap != null, "Received null map");

            InputSystem.EnsureInitialized();

            var actionsInThisMap = actionMap.m_Actions;
            var bindingsInThisMap = actionMap.m_Bindings;
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
            var bindingMaskOnThisMap = actionMap.m_BindingMask;
            var devicesForThisMap = actionMap.devices;
            var isSingletonAction = actionMap.m_SingletonAction != null;

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

                        var firstControlIndex = 0; // numControls dictates whether this is a valid index or not.
                        var firstInteractionIndex = InputActionState.kInvalidIndex;
                        var firstProcessorIndex = InputActionState.kInvalidIndex;
                        var actionIndexForBinding = InputActionState.kInvalidIndex;
                        var partIndex = InputActionState.kInvalidIndex;

                        var numControls = 0;
                        var numInteractions = 0;
                        var numProcessors = 0;

                        // Make sure that if it's part of a composite, we are actually part of a composite.
                        if (isPartOfComposite && currentCompositeBindingIndex == InputActionState.kInvalidIndex)
                            throw new InvalidOperationException(
                                $"Binding '{unresolvedBinding}' is marked as being part of a composite but the preceding binding is not a composite");

                        // Try to find action.
                        //
                        // NOTE: We ignore actions on bindings that are part of composites. We only allow
                        //       actions to be triggered from the composite itself.
                        var actionIndexInMap = InputActionState.kInvalidIndex;
                        var actionName = unresolvedBinding.action;
                        InputAction action = null;
                        if (!isPartOfComposite)
                        {
                            if (isSingletonAction)
                            {
                                // Singleton actions always ignore names.
                                actionIndexInMap = 0;
                            }
                            else if (!string.IsNullOrEmpty(actionName))
                            {
                                ////REVIEW: should we fail here if we don't manage to find the action
                                actionIndexInMap = actionMap.FindActionIndex(actionName);
                            }

                            if (actionIndexInMap != InputActionState.kInvalidIndex)
                                action = actionsInThisMap[actionIndexInMap];
                        }
                        else
                        {
                            actionIndexInMap = currentCompositeActionIndexInMap;
                            action = currentCompositeAction;
                        }

                        // If it's a composite, start a chain.
                        if (isComposite)
                        {
                            currentCompositeBindingIndex = bindingIndex;
                            currentCompositeAction = action;
                            currentCompositeActionIndexInMap = actionIndexInMap;
                        }

                        // Determine if the binding is disabled.
                        // Disabled if path is empty.
                        var path = unresolvedBinding.effectivePath;
                        var bindingIsDisabled = string.IsNullOrEmpty(path)

                            // Also, if we can't find the action to trigger for the binding, we just go and disable
                            // the binding.
                            || action == null

                            // Also, disabled if binding doesn't match with our binding mask (might be empty).
                            || (!isComposite && bindingMask != null &&
                                !bindingMask.Value.Matches(ref unresolvedBinding,
                                    InputBinding.MatchOptions.EmptyGroupMatchesAny))

                            // Also, disabled if binding doesn't match the binding mask on the map (might be empty).
                            || (!isComposite && bindingMaskOnThisMap != null &&
                                !bindingMaskOnThisMap.Value.Matches(ref unresolvedBinding,
                                    InputBinding.MatchOptions.EmptyGroupMatchesAny))

                            // Finally, also disabled if binding doesn't match the binding mask on the action (might be empty).
                            || (!isComposite && action?.m_BindingMask != null &&
                                !action.m_BindingMask.Value.Matches(ref unresolvedBinding,
                                    InputBinding.MatchOptions.EmptyGroupMatchesAny));

                        // If the binding isn't disabled, look up controls now. We do this first as we may still disable the
                        // binding if it doesn't resolve to any controls or resolves only to controls already bound to by
                        // other bindings.
                        //
                        // NOTE: We continuously add controls here to `resolvedControls`. Once we've completed our
                        //       pass over the bindings in the map, `resolvedControls` will have all the controls for
                        //       the current map.
                        if (!bindingIsDisabled && !isComposite)
                        {
                            firstControlIndex = memory.controlCount + resolvedControls.Count;
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
                        }

                        // If the binding isn't disabled, resolve its controls, processors, and interactions.
                        if (!bindingIsDisabled)
                        {
                            // NOTE: When isFullResolve==false, it is *imperative* that we do count processor and interaction
                            //       counts here come out exactly the same as in the previous full resolve.

                            // Instantiate processors.
                            var processorString = unresolvedBinding.effectiveProcessors;
                            if (!string.IsNullOrEmpty(processorString))
                            {
                                // Add processors from binding.
                                firstProcessorIndex = InstantiateWithParameters(InputProcessor.s_Processors, processorString,
                                    ref processors, ref totalProcessorCount, actionMap, ref unresolvedBinding);
                                if (firstProcessorIndex != InputActionState.kInvalidIndex)
                                    numProcessors = totalProcessorCount - firstProcessorIndex;
                            }
                            if (!string.IsNullOrEmpty(action.m_Processors))
                            {
                                // Add processors from action.
                                var index = InstantiateWithParameters(InputProcessor.s_Processors, action.m_Processors, ref processors,
                                    ref totalProcessorCount, actionMap, ref unresolvedBinding);
                                if (index != InputActionState.kInvalidIndex)
                                {
                                    if (firstProcessorIndex == InputActionState.kInvalidIndex)
                                        firstProcessorIndex = index;
                                    numProcessors += totalProcessorCount - index;
                                }
                            }

                            // Instantiate interactions.
                            if (isPartOfComposite)
                            {
                                // Composite's part use composite interactions
                                if (currentCompositeBindingIndex != InputActionState.kInvalidIndex)
                                {
                                    firstInteractionIndex = bindingStatesPtr[currentCompositeBindingIndex].interactionStartIndex;
                                    numInteractions = bindingStatesPtr[currentCompositeBindingIndex].interactionCount;
                                }
                            }
                            else
                            {
                                var interactionString = unresolvedBinding.effectiveInteractions;
                                if (!string.IsNullOrEmpty(interactionString))
                                {
                                    // Add interactions from binding.
                                    firstInteractionIndex = InstantiateWithParameters(InputInteraction.s_Interactions, interactionString,
                                        ref interactions, ref totalInteractionCount, actionMap, ref unresolvedBinding);
                                    if (firstInteractionIndex != InputActionState.kInvalidIndex)
                                        numInteractions = totalInteractionCount - firstInteractionIndex;
                                }
                                if (!string.IsNullOrEmpty(action.m_Interactions))
                                {
                                    // Add interactions from action.
                                    var index = InstantiateWithParameters(InputInteraction.s_Interactions, action.m_Interactions,
                                        ref interactions, ref totalInteractionCount, actionMap, ref unresolvedBinding);
                                    if (index != InputActionState.kInvalidIndex)
                                    {
                                        if (firstInteractionIndex == InputActionState.kInvalidIndex)
                                            firstInteractionIndex = index;
                                        numInteractions += totalInteractionCount - index;
                                    }
                                }
                            }

                            // If it's the start of a composite chain, create the composite.
                            if (isComposite)
                            {
                                // The composite binding entry itself does not resolve to any controls.
                                // It creates a composite binding object which is then populated from
                                // subsequent bindings.

                                // Instantiate. For composites, the path is the name of the composite.
                                var composite = InstantiateBindingComposite(ref unresolvedBinding, actionMap);
                                currentCompositeIndex =
                                    ArrayHelpers.AppendWithCapacity(ref composites, ref totalCompositeCount, composite);

                                // Record where the controls for parts of the composite start.
                                firstControlIndex = memory.controlCount + resolvedControls.Count;
                            }
                            else
                            {
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
                            }
                        }

                        // If the binding is part of a composite, pass the resolved controls
                        // on to the composite.
                        if (isPartOfComposite && currentCompositeBindingIndex != InputActionState.kInvalidIndex && numControls > 0)
                        {
                            // Make sure the binding is named. The name determines what in the composite
                            // to bind to.
                            if (string.IsNullOrEmpty(unresolvedBinding.name))
                                throw new InvalidOperationException(
                                    $"Binding '{unresolvedBinding}' that is part of composite '{composites[currentCompositeIndex]}' is missing a name");

                            // Assign an index to the current part of the composite which
                            // can be used by the composite to read input from this part.
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

                        // Store resolved binding.
                        *bindingState = new InputActionState.BindingState
                        {
                            controlStartIndex = firstControlIndex,
                            // For composites, this will be adjusted as we add each part.
                            controlCount = numControls,
                            interactionStartIndex = firstInteractionIndex,
                            interactionCount = numInteractions,
                            processorStartIndex = firstProcessorIndex,
                            processorCount = numProcessors,
                            isComposite = isComposite,
                            isPartOfComposite = unresolvedBinding.isPartOfComposite,
                            partIndex = partIndex,
                            actionIndex = actionIndexForBinding,
                            compositeOrCompositeBindingIndex = isComposite ? currentCompositeIndex : currentCompositeBindingIndex,
                            mapIndex = totalMapCount,
                            wantsInitialStateCheck = action?.wantsInitialStateCheck ?? false
                        };
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(
                            $"{exception.GetType().Name} while resolving binding '{unresolvedBinding}' in action map '{actionMap}'");
                        Debug.LogException(exception);

                        // Don't swallow exceptions that indicate something is wrong in the code rather than
                        // in the data.
                        if (exception.IsExceptionIndicatingBugInCode())
                            throw;
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
                {
                    ref var interactionState = ref newMemory.interactionStates[i];
                    interactionState.phase = InputActionPhase.Waiting;
                    interactionState.triggerControlIndex = InputActionState.kInvalidIndex;
                }

                // Initialize action data.
                var runningIndexInBindingIndices = memory.bindingCount;
                for (var i = 0; i < actionCountInThisMap; ++i)
                {
                    var action = actionsInThisMap[i];
                    var actionIndex = actionStartIndex + i;

                    // Correlate action with its trigger state.
                    action.m_ActionIndexInState = actionIndex;

                    Debug.Assert(runningIndexInBindingIndices < ushort.MaxValue, "Binding start index on action exceeds limit");
                    newMemory.actionBindingIndicesAndCounts[actionIndex * 2] = (ushort)runningIndexInBindingIndices;

                    // Collect bindings for action.
                    var firstBindingIndexForAction = -1;
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

                        if (firstBindingIndexForAction == -1)
                            firstBindingIndexForAction = bindingIndex;

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

                    if (firstBindingIndexForAction == -1)
                        firstBindingIndexForAction = 0;

                    Debug.Assert(bindingCountForAction < ushort.MaxValue, "Binding count on action exceeds limit");
                    newMemory.actionBindingIndicesAndCounts[actionIndex * 2 + 1] = (ushort)bindingCountForAction;

                    // See if we may need conflict resolution on this action. Never needed for pass-through actions.
                    // Otherwise, if we have more than one bound control or have several bindings and one of them
                    // is a composite, we enable it.
                    var isPassThroughAction = action.type == InputActionType.PassThrough;
                    var isButtonAction = action.type == InputActionType.Button;
                    var mayNeedConflictResolution = !isPassThroughAction && numPossibleConcurrentActuations > 1;

                    // Initialize initial trigger state.
                    newMemory.actionStates[actionIndex] =
                        new InputActionState.TriggerState
                    {
                        phase = InputActionPhase.Disabled,
                        mapIndex = mapIndex,
                        controlIndex = InputActionState.kInvalidIndex,
                        interactionIndex = InputActionState.kInvalidIndex,
                        isPassThrough = isPassThroughAction,
                        isButton = isButtonAction,
                        mayNeedConflictResolution = mayNeedConflictResolution,
                        bindingIndex = firstBindingIndexForAction
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
                actionMap.m_MapIndexInState = mapIndex;
                var finalActionMapCount = memory.mapCount;
                ArrayHelpers.AppendWithCapacity(ref maps, ref finalActionMapCount, actionMap, capacityIncrement: 4);
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

        private List<NameAndParameters> m_Parameters; // We retain this to reuse the allocation.
        private int InstantiateWithParameters<TType>(TypeTable registrations, string namesAndParameters, ref TType[] array, ref int count, InputActionMap actionMap, ref InputBinding binding)
        {
            if (!NameAndParameters.ParseMultiple(namesAndParameters, ref m_Parameters))
                return InputActionState.kInvalidIndex;

            var firstIndex = count;
            for (var i = 0; i < m_Parameters.Count; ++i)
            {
                // Look up type.
                var objectRegistrationName = m_Parameters[i].name;
                var type = registrations.LookupTypeRegistration(objectRegistrationName);
                if (type == null)
                {
                    Debug.LogError(
                        $"No {typeof(TType).Name} with name '{objectRegistrationName}' (mentioned in '{namesAndParameters}') has been registered");
                    continue;
                }

                if (!m_IsControlOnlyResolve)
                {
                    // Instantiate it.
                    if (!(Activator.CreateInstance(type) is TType instance))
                    {
                        Debug.LogError(
                            $"Type '{type.Name}' registered as '{objectRegistrationName}' (mentioned in '{namesAndParameters}') is not an {typeof(TType).Name}");
                        continue;
                    }

                    // Pass parameters to it.
                    ApplyParameters(m_Parameters[i].parameters, instance, actionMap, ref binding, objectRegistrationName,
                        namesAndParameters);

                    // Add to list.
                    ArrayHelpers.AppendWithCapacity(ref array, ref count, instance);
                }
                else
                {
                    Debug.Assert(type.IsInstanceOfType(array[count]), "Type of instance in array does not match expected type");
                    ++count;
                }
            }

            return firstIndex;
        }

        private static InputBindingComposite InstantiateBindingComposite(ref InputBinding binding, InputActionMap actionMap)
        {
            var nameAndParametersParsed = NameAndParameters.Parse(binding.effectivePath);

            // Look up.
            var type = InputBindingComposite.s_Composites.LookupTypeRegistration(nameAndParametersParsed.name);
            if (type == null)
                throw new InvalidOperationException(
                    $"No binding composite with name '{nameAndParametersParsed.name}' has been registered");

            // Instantiate.
            if (!(Activator.CreateInstance(type) is InputBindingComposite instance))
                throw new InvalidOperationException(
                    $"Registered type '{type.Name}' used for '{nameAndParametersParsed.name}' is not an InputBindingComposite");

            // Set parameters.
            ApplyParameters(nameAndParametersParsed.parameters, instance, actionMap, ref binding, nameAndParametersParsed.name,
                binding.effectivePath);

            return instance;
        }

        private static void ApplyParameters(ReadOnlyArray<NamedValue> parameters, object instance, InputActionMap actionMap, ref InputBinding binding, string objectRegistrationName, string namesAndParameters)
        {
            foreach (var parameter in parameters)
            {
                // Find field.
                var field = instance.GetType().GetField(parameter.name,
                    BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null)
                {
                    Debug.LogError(
                        $"Type '{instance.GetType().Name}' registered as '{objectRegistrationName}' (mentioned in '{namesAndParameters}') has no public field called '{parameter.name}'");
                    continue;
                }
                var fieldTypeCode = Type.GetTypeCode(field.FieldType);

                // See if we have a parameter override.
                var parameterOverride =
                    InputActionRebindingExtensions.ParameterOverride.Find(actionMap, ref binding, parameter.name, objectRegistrationName);
                var value = parameterOverride != null
                    ? parameterOverride.Value.value
                    : parameter.value;

                field.SetValue(instance, value.ConvertTo(fieldTypeCode).ToObject());
            }
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
                throw new InvalidOperationException(
                    $"Cannot find public field '{name}' used as parameter of binding composite '{composite}' of type '{type}'");

            ////REVIEW: should we wrap part numbers in a struct instead of using int?

            // Type-check.
            var fieldType = field.FieldType;
            if (fieldType != typeof(int))
                throw new InvalidOperationException(
                    $"Field '{name}' used as a parameter of binding composite '{composite}' must be of type 'int' but is of type '{type.Name}' instead");

            ////REVIEW: this creates garbage; need a better solution to get to zero garbage during re-resolving
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
