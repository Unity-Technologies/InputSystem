using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using Unity.Profiling;
using UnityEngine.InputSystem.Utilities;

using ProfilerMarker = Unity.Profiling.ProfilerMarker;

////TODO: now that we can bind to controls by display name, we need to re-resolve controls when those change (e.g. when the keyboard layout changes)

////TODO: remove direct references to InputManager

////TODO: make sure controls in per-action and per-map control arrays are unique (the internal arrays are probably okay to have duplicates)

////REVIEW: should the default interaction be an *explicit* interaction?

////REVIEW: should "pass-through" be an interaction instead of a setting on actions?

////REVIEW: allow setup where state monitor is enabled but action is disabled?

namespace UnityEngine.InputSystem
{
    using InputActionListener = Action<InputAction.CallbackContext>;

    /// <summary>
    /// Dynamic execution state of one or more <see cref="InputActionMap">action maps</see> and
    /// all the actions they contain.
    /// </summary>
    /// <remarks>
    /// The aim of this class is to both put all the dynamic execution state into one place as well
    /// as to organize state in tight, GC-optimized arrays. Also, by moving state out of individual
    /// <see cref="InputActionMap">action maps</see>, we can combine the state of several maps
    /// into one single object with a single set of arrays. Ideally, if you have a single action
    /// asset in the game, you get a single InputActionState that contains the entire dynamic
    /// execution state for your game's actions.
    ///
    /// Note that this class allocates unmanaged memory. It has to be disposed of or it will leak
    /// memory!
    ///
    /// An instance of this class is also used for singleton actions by means of the hidden action
    /// map we create for those actions. In that case, there will be both a hidden map instance
    /// as well as an action state for every separate singleton action. This makes singleton actions
    /// relatively expensive.
    /// </remarks>
    internal unsafe class InputActionState : IInputStateChangeMonitor, ICloneable, IDisposable
    {
        public const int kInvalidIndex = -1;

        /// <summary>
        /// Array of all maps added to the state.
        /// </summary>
        public InputActionMap[] maps;

        /// <summary>
        /// List of all resolved controls.
        /// </summary>
        /// <remarks>
        /// As we don't know in advance how many controls a binding may match (if any), we bump the size of
        /// this array in increments during resolution. This means it may be end up being larger than the total
        /// number of used controls and have empty entries at the end. Use <see cref="UnmanagedMemory.controlCount"/> and not
        /// <c>.Length</c> to find the actual number of controls.
        ///
        /// All bound controls are included in the array regardless of whether only a partial set of actions
        /// is currently enabled. What ultimately decides whether controls get triggered or not is whether we
        /// have installed state monitors for them or not.
        /// </remarks>
        public InputControl[] controls;

        /// <summary>
        /// Array of instantiated interaction objects.
        /// </summary>
        /// <remarks>
        /// Every binding that has interactions corresponds to a slice of this array.
        ///
        /// Indices match between this and interaction states in <see cref="memory"/>.
        /// </remarks>
        public IInputInteraction[] interactions;

        /// <summary>
        /// Processor objects instantiated for the bindings in the state.
        /// </summary>
        public InputProcessor[] processors;

        /// <summary>
        /// Array of instantiated composite objects.
        /// </summary>
        public InputBindingComposite[] composites;

        public int totalProcessorCount;
        public int totalCompositeCount => memory.compositeCount;
        public int totalMapCount => memory.mapCount;
        public int totalActionCount => memory.actionCount;
        public int totalBindingCount => memory.bindingCount;
        public int totalInteractionCount => memory.interactionCount;
        public int totalControlCount => memory.controlCount;

        /// <summary>
        /// Block of unmanaged memory that holds the dynamic execution state of the actions and their controls.
        /// </summary>
        /// <remarks>
        /// We keep several arrays of structured data in a single block of unmanaged memory.
        /// </remarks>
        public UnmanagedMemory memory;

        public ActionMapIndices* mapIndices => memory.mapIndices;
        public TriggerState* actionStates => memory.actionStates;
        public BindingState* bindingStates => memory.bindingStates;
        public InteractionState* interactionStates => memory.interactionStates;
        public int* controlIndexToBindingIndex => memory.controlIndexToBindingIndex;
        public ushort* controlGroupingAndComplexity => memory.controlGroupingAndComplexity;
        public float* controlMagnitudes => memory.controlMagnitudes;
        public uint* enabledControls => (uint*)memory.enabledControls;

        public bool isProcessingControlStateChange => m_InProcessControlStateChange;

        private bool m_OnBeforeUpdateHooked;
        private bool m_OnAfterUpdateHooked;
        private bool m_InProcessControlStateChange;
        private InputEventPtr m_CurrentlyProcessingThisEvent;
        private Action m_OnBeforeUpdateDelegate;
        private Action m_OnAfterUpdateDelegate;
        private static readonly ProfilerMarker k_InputInitialActionStateCheckMarker = new ProfilerMarker("InitialActionStateCheck");
        private static readonly ProfilerMarker k_InputActionResolveConflictMarker = new ProfilerMarker("InputActionResolveConflict");
        private static readonly ProfilerMarker k_InputActionCallbackMarker = new ProfilerMarker("InputActionCallback");
        private static readonly ProfilerMarker k_InputOnActionChangeMarker = new ProfilerMarker("InpustSystem.onActionChange");
        private static readonly ProfilerMarker k_InputOnDeviceChangeMarker = new ProfilerMarker("InpustSystem.onDeviceChange");

        /// <summary>
        /// Initialize execution state with given resolved binding information.
        /// </summary>
        /// <param name="resolver"></param>
        public void Initialize(InputBindingResolver resolver)
        {
            ClaimDataFrom(resolver);
            AddToGlobalList();
        }

        private void ComputeControlGroupingIfNecessary()
        {
            if (memory.controlGroupingInitialized)
                return;

            // If shortcut support is disabled, we simply put put all bindings at complexity=1 and
            // in their own group.
            var disableControlGrouping = !InputSystem.settings.shortcutKeysConsumeInput;

            var currentGroup = 1u;
            for (var i = 0; i < totalControlCount; ++i)
            {
                var control = controls[i];
                var bindingIndex = controlIndexToBindingIndex[i];
                ref var binding = ref bindingStates[bindingIndex];

                ////REVIEW: take processors and interactions into account??

                // Compute complexity.
                var complexity = 1;
                if (binding.isPartOfComposite && !disableControlGrouping)
                {
                    var compositeBindingIndex = binding.compositeOrCompositeBindingIndex;

                    for (var n = compositeBindingIndex + 1; n < totalBindingCount; ++n)
                    {
                        ref var partBinding = ref bindingStates[n];
                        if (!partBinding.isPartOfComposite || partBinding.compositeOrCompositeBindingIndex != compositeBindingIndex)
                            break;
                        ++complexity;
                    }
                }
                controlGroupingAndComplexity[i * 2 + 1] = (ushort)complexity;

                // Compute grouping. If already set, skip.
                if (controlGroupingAndComplexity[i * 2] == 0)
                {
                    if (!disableControlGrouping)
                    {
                        for (var n = 0; n < totalControlCount; ++n)
                        {
                            // NOTE: We could compute group numbers based on device index + control offsets
                            //       and thus make them work globally in a stable way. But we'd need a mechanism
                            //       to then determine ordering of actions globally such that it is clear which
                            //       action gets a first shot at an input.

                            var otherControl = controls[n];
                            if (control != otherControl)
                                continue;

                            controlGroupingAndComplexity[n * 2] = (ushort)currentGroup;
                        }
                    }

                    controlGroupingAndComplexity[i * 2] = (ushort)currentGroup;

                    ++currentGroup;
                }
            }

            memory.controlGroupingInitialized = true;
        }

        public void ClaimDataFrom(InputBindingResolver resolver)
        {
            totalProcessorCount = resolver.totalProcessorCount;

            maps = resolver.maps;
            interactions = resolver.interactions;
            processors = resolver.processors;
            composites = resolver.composites;
            controls = resolver.controls;

            memory = resolver.memory;
            resolver.memory = new UnmanagedMemory();

            ComputeControlGroupingIfNecessary();
        }

        ~InputActionState()
        {
            Destroy(isFinalizing: true);
        }

        public void Dispose()
        {
            Destroy();
        }

        private void Destroy(bool isFinalizing = false)
        {
            Debug.Assert(!isProcessingControlStateChange, "Must not destroy InputActionState while executing an action callback within it");

            if (!isFinalizing)
            {
                for (var i = 0; i < totalMapCount; ++i)
                {
                    var map = maps[i];

                    // Remove state change monitors.
                    if (map.enabled)
                        DisableControls(i, mapIndices[i].controlStartIndex, mapIndices[i].controlCount);

                    if (map.m_Asset != null)
                        map.m_Asset.m_SharedStateForAllMaps = null;

                    map.m_State = null;
                    map.m_MapIndexInState = kInvalidIndex;
                    map.m_EnabledActionsCount = 0;

                    // Reset action indices on the map's actions.
                    var actions = map.m_Actions;
                    if (actions != null)
                    {
                        for (var n = 0; n < actions.Length; ++n)
                            actions[n].m_ActionIndexInState = kInvalidIndex;
                    }
                }

                RemoveMapFromGlobalList();
            }
            memory.Dispose();
        }

        /// <summary>
        /// Create a copy of the state.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The copy is non-functional in so far as it cannot be used to keep track of changes made to
        /// any associated actions. However, it can be used to freeze the binding resolution state of
        /// a particular set of enabled actions. This is used by <see cref="InputActionTrace"/>.
        /// </remarks>
        public InputActionState Clone()
        {
            return new InputActionState
            {
                maps = ArrayHelpers.Copy(maps),
                controls = ArrayHelpers.Copy(controls),
                interactions = ArrayHelpers.Copy(interactions),
                processors = ArrayHelpers.Copy(processors),
                composites = ArrayHelpers.Copy(composites),
                totalProcessorCount = totalProcessorCount,
                memory = memory.Clone(),
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Check if the state is currently using a control from the given device.
        /// </summary>
        /// <param name="device">Any input device.</param>
        /// <returns>True if any of the maps in the state has the device in its <see cref="InputActionMap.devices"/>
        /// list or if any of the device's controls are contained in <see cref="controls"/>.</returns>
        private bool IsUsingDevice(InputDevice device)
        {
            Debug.Assert(device != null, "Device is null");

            // If all maps have device restrictions, the device must be in it
            // or we're not using it.
            var haveMapsWithoutDeviceRestrictions = false;
            for (var i = 0; i < totalMapCount; ++i)
            {
                var map = maps[i];
                var devicesForMap = map.devices;

                if (devicesForMap == null)
                    haveMapsWithoutDeviceRestrictions = true;
                else if (devicesForMap.Value.Contains(device))
                    return true;
            }

            if (!haveMapsWithoutDeviceRestrictions)
                return false;

            // Check all our controls one by one.
            for (var i = 0; i < totalControlCount; ++i)
                if (controls[i].device == device)
                    return true;

            return false;
        }

        // Check if the state would use a control from the given device.
        private bool CanUseDevice(InputDevice device)
        {
            Debug.Assert(device != null, "Device is null");

            // If all maps have device restrictions and the device isn't in them, we can't use
            // the device.
            var haveMapWithoutDeviceRestrictions = false;
            for (var i = 0; i < totalMapCount; ++i)
            {
                var map = maps[i];
                var devicesForMap = map.devices;

                if (devicesForMap == null)
                    haveMapWithoutDeviceRestrictions = true;
                else if (devicesForMap.Value.Contains(device))
                    return true;
            }

            if (!haveMapWithoutDeviceRestrictions)
                return false;

            for (var i = 0; i < totalMapCount; ++i)
            {
                var map = maps[i];
                var bindings = map.m_Bindings;
                if (bindings == null)
                    continue;

                var bindingCount = bindings.Length;
                for (var n = 0; n < bindingCount; ++n)
                {
                    if (InputControlPath.TryFindControl(device, bindings[n].effectivePath) != null)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check whether the state has any actions that are currently enabled.
        /// </summary>
        /// <returns></returns>
        public bool HasEnabledActions()
        {
            for (var i = 0; i < totalMapCount; ++i)
            {
                var map = maps[i];
                if (map.enabled)
                    return true;
            }

            return false;
        }

        private void FinishBindingCompositeSetups()
        {
            for (var i = 0; i < totalBindingCount; ++i)
            {
                ref var binding = ref bindingStates[i];
                if (!binding.isComposite || binding.compositeOrCompositeBindingIndex == -1)
                    continue;

                var composite = composites[binding.compositeOrCompositeBindingIndex];
                var context = new InputBindingCompositeContext { m_State = this, m_BindingIndex = i };
                composite.CallFinishSetup(ref context);
            }
        }

        internal void PrepareForBindingReResolution(bool needFullResolve,
            ref InputControlList<InputControl> activeControls, ref bool hasEnabledActions)
        {
            // Let listeners know we're about to modify bindings.
            var needToCloneActiveControls = false;
            for (var i = 0; i < totalMapCount; ++i)
            {
                var map = maps[i];

                if (map.enabled)
                {
                    hasEnabledActions = true;

                    if (needFullResolve)
                    {
                        // For a full-resolve, we temporarily disable all actions and then re-enable
                        // all that were enabled after bindings have been resolved (plus we also flip on
                        // initial state checks for those actions to make sure they react right away
                        // to whatever state controls are in).
                        DisableAllActions(map);
                    }
                    else
                    {
                        // Cancel any action that is driven from a control we will lose when we re-resolve.
                        // For any other on-going action, save active controls.
                        foreach (var action in map.actions)
                        {
                            if (!action.phase.IsInProgress())
                                continue;

                            // Skip action's that are in progress but whose active control is not affected
                            // by the changes that lead to re-resolution.
                            if (action.ActiveControlIsValid(action.activeControl))
                            {
                                // As part of re-resolving, we're losing m_State.controls. So, while we retain
                                // the current execution state of the method including the index of the currently
                                // active control, we lose the actual references to the control.
                                // Thus, we retain an explicit list of active controls into which we *only* copy
                                // those few controls that are currently active. Also, this list is kept in unmanaged
                                // memory so we don't add an additional GC allocation here.
                                if (needToCloneActiveControls == false)
                                {
                                    activeControls = new InputControlList<InputControl>(Allocator.Temp);
                                    activeControls.Resize(totalControlCount);
                                    needToCloneActiveControls = true;
                                }

                                ref var actionState = ref actionStates[action.m_ActionIndexInState];
                                var activeControlIndex = actionState.controlIndex;
                                activeControls[activeControlIndex] = controls[activeControlIndex];

                                // Also save active controls for other ongoing interactions.
                                var bindingState = bindingStates[actionState.bindingIndex];
                                for (var n = 0; n < bindingState.interactionCount; ++n)
                                {
                                    var interactionIndex = bindingState.interactionStartIndex + n;
                                    if (!interactionStates[interactionIndex].phase.IsInProgress())
                                        continue;

                                    activeControlIndex = interactionStates[interactionIndex]
                                        .triggerControlIndex;
                                    if (action.ActiveControlIsValid(controls[activeControlIndex]))
                                        activeControls[activeControlIndex] = controls[activeControlIndex];
                                    else
                                        ResetInteractionState(interactionIndex);
                                }
                            }
                            else
                            {
                                ResetActionState(action.m_ActionIndexInState);
                            }
                        }

                        // NOTE: Removing state monitors here also means we're terminating any pending
                        //       timeouts. However, we have information in the action state about how much
                        //       is time is remaining on each of them so we can resume them later.

                        DisableControls(map);
                    }
                }

                map.ClearCachedActionData(onlyControls: !needFullResolve);
            }

            NotifyListenersOfActionChange(InputActionChange.BoundControlsAboutToChange);
        }

        public void FinishBindingResolution(bool hasEnabledActions, UnmanagedMemory oldMemory, InputControlList<InputControl> activeControls, bool isFullResolve)
        {
            // Fire InputBindingComposite.FinishSetup() calls.
            FinishBindingCompositeSetups();

            // Sync action states between the old and the new state. This also ensures
            // that any action that was already in progress just keeps going -- except
            // if we actually lost the control that was driving it.
            if (hasEnabledActions)
                RestoreActionStatesAfterReResolvingBindings(oldMemory, activeControls, isFullResolve);
            else
                NotifyListenersOfActionChange(InputActionChange.BoundControlsChanged);
        }

        /// <summary>
        /// Synchronize the current action states based on what they were before.
        /// </summary>
        /// <param name="oldState"></param>
        /// <remarks>
        /// We do this when we have to temporarily disable actions in order to re-resolve bindings.
        ///
        /// Note that we do NOT restore action states perfectly. I.e. will we will not preserve trigger
        /// and interaction states exactly to what they were before. Given that the bound controls may change,
        /// it would be non-trivial to reliably correlate the old and the new state. Instead, we simply
        /// reenable all the actions and controls that were enabled before and then let the next update
        /// take it from there.
        /// </remarks>
        private void RestoreActionStatesAfterReResolvingBindings(UnmanagedMemory oldState, InputControlList<InputControl> activeControls, bool isFullResolve)
        {
            Debug.Assert(oldState.isAllocated, "Old state contains no memory");

            // No maps and/or actions must have been added, replaced, or removed.
            //
            // IF
            //  isFullResolve==true:
            //     - No bindings must have been added, replaced, or removed or touched in any other way.
            //     - The only thing that is allowed to have changed is the list of controls used by the actions.
            //     - Binding masks must not have changed.
            //
            //  isFullResolve==false:
            //     - Bindings may have been added, replaced, modified, and/or removed.
            //     - Also, the list of controls may have changed.
            //     - Binding masks may have changed.
            //
            // This means that when we compare UnmanagedMemory from before and after:
            //  - Map indices are identical.
            //  - Action indices are identical.
            //  - Binding indices may have changed arbitrarily.
            //  - Control indices may have changed arbitrarily (controls[] before and after need not relate at all).
            //  - Processor indices may have changed arbitrarily.
            //  - Interaction indices may have changed arbitrarily.
            //
            // HOWEVER, if isFullResolve==false, then ONLY control indices may have changed. All other
            // indices must have remained unchanged.
            Debug.Assert(oldState.actionCount == memory.actionCount, "Action count in old and new state must be the same");
            Debug.Assert(oldState.mapCount == memory.mapCount, "Map count in old and new state must be the same");
            if (!isFullResolve)
            {
                Debug.Assert(oldState.bindingCount == memory.bindingCount, "Binding count in old and new state must be the same");
                Debug.Assert(oldState.interactionCount == memory.interactionCount, "Interaction count in old and new state must be the same");
                Debug.Assert(oldState.compositeCount == memory.compositeCount, "Composite count in old and new state must be the same");
            }

            // Restore action states.
            for (var actionIndex = 0; actionIndex < totalActionCount; ++actionIndex)
            {
                ref var oldActionState = ref oldState.actionStates[actionIndex];
                ref var newActionState = ref actionStates[actionIndex];

                newActionState.lastCanceledInUpdate = oldActionState.lastCanceledInUpdate;
                newActionState.lastPerformedInUpdate = oldActionState.lastPerformedInUpdate;
                newActionState.lastCompletedInUpdate = oldActionState.lastCompletedInUpdate;
                newActionState.pressedInUpdate = oldActionState.pressedInUpdate;
                newActionState.releasedInUpdate = oldActionState.releasedInUpdate;
                newActionState.startTime = oldActionState.startTime;
                newActionState.framePerformed = oldActionState.framePerformed;
                newActionState.frameCompleted = oldActionState.frameCompleted;
                newActionState.framePressed = oldActionState.framePressed;
                newActionState.frameReleased = oldActionState.frameReleased;
                newActionState.bindingIndex = oldActionState.bindingIndex;

                if (oldActionState.phase != InputActionPhase.Disabled)
                {
                    // In this step, we only put enabled actions into Waiting phase.
                    // When isFullResolve==false, we will restore the actual phase from
                    // before when we look at bindings further down in the code.
                    newActionState.phase = InputActionPhase.Waiting;

                    // In a full resolve, we actually disable any action we find enabled.
                    // So count any action we reenable here.
                    if (isFullResolve)
                        ++maps[newActionState.mapIndex].m_EnabledActionsCount;
                }
            }

            // Restore binding (and interaction) states.
            for (var bindingIndex = 0; bindingIndex < totalBindingCount; ++bindingIndex)
            {
                ref var newBindingState = ref memory.bindingStates[bindingIndex];
                if (newBindingState.isPartOfComposite)
                {
                    // Bindings that are part of composites get enabled through the composite itself.
                    continue;
                }

                // For composites, bring magnitudes along.
                if (newBindingState.isComposite)
                {
                    var compositeIndex = newBindingState.compositeOrCompositeBindingIndex;
                    if (oldState.compositeMagnitudes != null)
                        memory.compositeMagnitudes[compositeIndex] = oldState.compositeMagnitudes[compositeIndex];
                }

                var actionIndex = newBindingState.actionIndex;
                if (actionIndex == kInvalidIndex)
                {
                    // Binding is not targeting an action.
                    continue;
                }

                // Skip if action is disabled.
                ref var newActionState = ref actionStates[actionIndex];
                if (newActionState.isDisabled)
                    continue;

                // For all bindings to actions that are enabled, we flip on initial state checks to make sure
                // we're checking the action's current state against the most up-to-date actuation state of controls.
                // NOTE: We're only restore execution state for currently active controls. So, if there were multiple
                //       concurrent actuations on an action that was in progress, we let initial state checks restore
                //       relevant state.
                newBindingState.initialStateCheckPending = newBindingState.wantsInitialStateCheck;

                // Enable all controls on the binding.
                EnableControls(newBindingState.mapIndex, newBindingState.controlStartIndex,
                    newBindingState.controlCount);

                // For the remainder of what we do, we need binding indices to be stable.
                if (isFullResolve)
                    continue;

                ref var oldBindingState = ref memory.bindingStates[bindingIndex];
                newBindingState.triggerEventIdForComposite = oldBindingState.triggerEventIdForComposite;

                // If we only re-resolved controls and the action was in progress from the binding we're currently
                // looking at and we still have the control that was driving the action, we can simply keep the
                // action going from its previous state. However, control indices may have shifted (devices may have been added
                // or removed) so we need to be careful to update those. Other indices (bindings, actions, maps, etc.)
                // are guaranteed to still match.
                ref var oldActionState = ref oldState.actionStates[actionIndex];
                if (bindingIndex == oldActionState.bindingIndex && oldActionState.phase.IsInProgress() &&
                    activeControls.Count > 0 && activeControls[oldActionState.controlIndex] != null)
                {
                    var control = activeControls[oldActionState.controlIndex];

                    // Find the new control index. Binding index is guaranteed to be the same,
                    // so we can simply look on the binding for where the control is now.
                    var newControlIndex = FindControlIndexOnBinding(bindingIndex, control);

                    // This assert is used by test: Actions_ActiveBindingsHaveCorrectBindingIndicesAfterBindingResolution
                    Debug.Assert(newControlIndex != kInvalidIndex, "Could not find active control after binding resolution");
                    if (newControlIndex != kInvalidIndex)
                    {
                        newActionState.phase = oldActionState.phase;
                        newActionState.controlIndex = newControlIndex;
                        newActionState.magnitude = oldActionState.magnitude;
                        newActionState.interactionIndex = oldActionState.interactionIndex;

                        memory.controlMagnitudes[newControlIndex] = oldActionState.magnitude;
                    }

                    // Also bring over interaction states.
                    Debug.Assert(newBindingState.interactionCount == oldBindingState.interactionCount,
                        "Interaction count on binding must not have changed when doing a control-only resolve");
                    for (var n = 0; n < newBindingState.interactionCount; ++n)
                    {
                        ref var oldInteractionState = ref oldState.interactionStates[oldBindingState.interactionStartIndex + n];
                        if (!oldInteractionState.phase.IsInProgress())
                            continue;

                        control = activeControls[oldInteractionState.triggerControlIndex];
                        if (control == null)
                            continue;

                        newControlIndex = FindControlIndexOnBinding(bindingIndex, control);
                        Debug.Assert(newControlIndex != kInvalidIndex, "Could not find active control on interaction after binding resolution");

                        ref var newInteractionState = ref interactionStates[newBindingState.interactionStartIndex + n];
                        newInteractionState.phase = oldInteractionState.phase;
                        newInteractionState.performedTime = oldInteractionState.performedTime;
                        newInteractionState.startTime = oldInteractionState.startTime;
                        newInteractionState.triggerControlIndex = newControlIndex;

                        // If there was a running timeout on the interaction, resume it now.
                        if (oldInteractionState.isTimerRunning)
                        {
                            var trigger = new TriggerState
                            {
                                mapIndex = newBindingState.mapIndex,
                                controlIndex = newControlIndex,
                                bindingIndex = bindingIndex,
                                time = oldInteractionState.timerStartTime,
                                interactionIndex = newBindingState.interactionStartIndex + n
                            };
                            StartTimeout(oldInteractionState.timerDuration, ref trigger);

                            newInteractionState.totalTimeoutCompletionDone = oldInteractionState.totalTimeoutCompletionDone;
                            newInteractionState.totalTimeoutCompletionTimeRemaining = oldInteractionState.totalTimeoutCompletionTimeRemaining;
                        }
                    }
                }
            }

            // Make sure we get an initial state check.
            HookOnBeforeUpdate();

            // Let listeners know we have changed controls.
            NotifyListenersOfActionChange(InputActionChange.BoundControlsChanged);

            // For a full resolve, we will have temporarily disabled actions and reenabled them now.
            // Let listeners now.
            if (isFullResolve && s_GlobalState.onActionChange.length > 0)
            {
                for (var i = 0; i < totalMapCount; ++i)
                {
                    var map = maps[i];
                    if (map.m_SingletonAction == null && map.m_EnabledActionsCount == map.m_Actions.LengthSafe())
                    {
                        NotifyListenersOfActionChange(InputActionChange.ActionMapEnabled, map);
                    }
                    else
                    {
                        var actions = map.actions;
                        foreach (var action in actions)
                            if (action.enabled)
                                NotifyListenersOfActionChange(InputActionChange.ActionEnabled, action);
                    }
                }
            }
        }

        // Return true if the action that bindingIndex is bound to is currently driven from the given control
        // -OR- if any of the interactions on the binding are currently driven from the control.
        private bool IsActiveControl(int bindingIndex, int controlIndex)
        {
            ref var bindingState = ref bindingStates[bindingIndex];
            var actionIndex = bindingState.actionIndex;
            if (actionIndex == kInvalidIndex)
                return false;
            if (actionStates[actionIndex].controlIndex == controlIndex)
                return true;
            for (var i = 0; i < bindingState.interactionCount; ++i)
                if (interactionStates[bindingStates->interactionStartIndex + i].triggerControlIndex == controlIndex)
                    return true;
            return false;
        }

        private int FindControlIndexOnBinding(int bindingIndex, InputControl control)
        {
            var controlStartIndex = bindingStates[bindingIndex].controlStartIndex;
            var controlCount = bindingStates[bindingIndex].controlCount;

            for (var n = 0; n < controlCount; ++n)
            {
                if (control == controls[controlStartIndex + n])
                    return controlStartIndex + n;
            }

            return kInvalidIndex;
        }

        private void ResetActionStatesDrivenBy(InputDevice device)
        {
            using (InputActionRebindingExtensions.DeferBindingResolution())
            {
                for (var actionIndex = 0; actionIndex < totalActionCount; ++actionIndex)
                {
                    var actionState = &actionStates[actionIndex];

                    // Skip actions that aren't in progress.
                    if (actionState->phase == InputActionPhase.Waiting || actionState->phase == InputActionPhase.Disabled)
                        continue;

                    // Skip actions not driven from this device.
                    if (actionState->isPassThrough)
                    {
                        // Pass-through actions are not driven from specific controls yet still benefit
                        // from being able to observe resets. So for these, we need to check all bound controls,
                        // not just the one that happen to trigger last.
                        if (!IsActionBoundToControlFromDevice(device, actionIndex))
                            continue;
                    }
                    else
                    {
                        // For button and value actions, we go by whatever is currently driving the action.

                        var controlIndex = actionState->controlIndex;
                        if (controlIndex == -1)
                            continue;
                        var control = controls[controlIndex];
                        if (control.device != device)
                            continue;
                    }

                    // Reset.
                    ResetActionState(actionIndex);
                }
            }
        }

        private bool IsActionBoundToControlFromDevice(InputDevice device, int actionIndex)
        {
            var usesControlFromDevice = false;
            var bindingStartIndex = GetActionBindingStartIndexAndCount(actionIndex, out var bindingCount);
            for (var i = 0; i < bindingCount; ++i)
            {
                var bindingIndex = memory.actionBindingIndices[bindingStartIndex + i];
                var controlCount = bindingStates[bindingIndex].controlCount;
                var controlStartIndex = bindingStates[bindingIndex].controlStartIndex;

                for (var n = 0; n < controlCount; ++n)
                {
                    var control = controls[controlStartIndex + n];
                    if (control.device == device)
                    {
                        usesControlFromDevice = true;
                        break;
                    }
                }
            }

            return usesControlFromDevice;
        }

        /// <summary>
        /// Reset the trigger state of the given action such that the action has no record of being triggered.
        /// </summary>
        /// <param name="actionIndex">Action whose state to reset.</param>
        /// <param name="toPhase">Phase to reset the action to. Must be either <see cref="InputActionPhase.Waiting"/>
        /// or <see cref="InputActionPhase.Disabled"/>. Other phases cannot be transitioned to through resets.</param>
        /// <param name="hardReset">If true, also wipe state such as for <see cref="InputAction.WasPressedThisFrame"/> which normally
        /// persists even if an action is disabled.</param>
        public void ResetActionState(int actionIndex, InputActionPhase toPhase = InputActionPhase.Waiting, bool hardReset = false)
        {
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Action index out of range when resetting action");
            Debug.Assert(toPhase == InputActionPhase.Waiting || toPhase == InputActionPhase.Disabled,
                "Phase must be Waiting or Disabled");

            // If the action in started or performed phase, cancel it first.
            var actionState = &actionStates[actionIndex];
            if (actionState->phase != InputActionPhase.Waiting && actionState->phase != InputActionPhase.Disabled)
            {
                // Cancellation calls should receive current time.
                actionState->time = InputState.currentTime;

                // If the action got triggered from an interaction, go and reset all interactions on the binding
                // that got triggered.
                if (actionState->interactionIndex != kInvalidIndex)
                {
                    var bindingIndex = actionState->bindingIndex;
                    if (bindingIndex != kInvalidIndex)
                    {
                        var mapIndex = actionState->mapIndex;
                        var interactionCount = bindingStates[bindingIndex].interactionCount;
                        var interactionStartIndex = bindingStates[bindingIndex].interactionStartIndex;

                        for (var i = 0; i < interactionCount; ++i)
                        {
                            var interactionIndex = interactionStartIndex + i;
                            ResetInteractionStateAndCancelIfNecessary(mapIndex, bindingIndex, interactionIndex, phaseAfterCanceled: toPhase);
                        }
                    }
                }
                else
                {
                    // No interactions. Cancel the action directly.

                    Debug.Assert(actionState->bindingIndex != kInvalidIndex, "Binding index on trigger state is invalid");
                    Debug.Assert(bindingStates[actionState->bindingIndex].interactionCount == 0,
                        "Action has been triggered but apparently not from an interaction yet there's interactions on the binding that got triggered?!?");

                    if (actionState->phase != InputActionPhase.Canceled)
                        ChangePhaseOfAction(InputActionPhase.Canceled, ref actionStates[actionIndex],
                            phaseAfterPerformedOrCanceled: toPhase);
                }
            }

            // Wipe state.
            actionState->phase = toPhase;
            actionState->controlIndex = kInvalidIndex;
            var idx = memory.actionBindingIndicesAndCounts[actionIndex];
            actionState->bindingIndex = memory.actionBindingIndices != null ? memory.actionBindingIndices[idx] : 0;
            actionState->interactionIndex = kInvalidIndex;
            actionState->startTime = 0;
            actionState->time = 0;
            actionState->hasMultipleConcurrentActuations = false;
            actionState->inProcessing = false;
            actionState->isPressed = false;

            // For "hard resets", wipe state we don't normally wipe. This resets things such as WasPressedThisFrame().
            if (hardReset)
            {
                actionState->lastCanceledInUpdate = default;
                actionState->lastPerformedInUpdate = default;
                actionState->lastCompletedInUpdate = default;
                actionState->pressedInUpdate = default;
                actionState->releasedInUpdate = default;
                actionState->framePerformed = default;
                actionState->frameCompleted = default;
                actionState->framePressed = default;
                actionState->frameReleased = default;
            }

            Debug.Assert(!actionState->isStarted, "Cannot reset an action to started phase");
            Debug.Assert(!actionState->isPerformed, "Cannot reset an action to performed phase");
            Debug.Assert(!actionState->isCanceled, "Cannot reset an action to canceled phase");
        }

        public ref TriggerState FetchActionState(InputAction action)
        {
            Debug.Assert(action != null, "Action must not be null");
            Debug.Assert(action.m_ActionMap != null, "Action must have an action map");
            Debug.Assert(action.m_ActionMap.m_MapIndexInState != kInvalidIndex, "Action must have index set");
            Debug.Assert(maps.Contains(action.m_ActionMap), "Action map must be contained in state");
            Debug.Assert(action.m_ActionIndexInState >= 0 && action.m_ActionIndexInState < totalActionCount, "Action index is out of range");

            return ref actionStates[action.m_ActionIndexInState];
        }

        public ActionMapIndices FetchMapIndices(InputActionMap map)
        {
            Debug.Assert(map != null, "Must must not be null");
            Debug.Assert(maps.Contains(map), "Map must be contained in state");
            return mapIndices[map.m_MapIndexInState];
        }

        public void EnableAllActions(InputActionMap map)
        {
            Debug.Assert(map != null, "Map must not be null");
            Debug.Assert(map.m_Actions != null, "Map must have actions");
            Debug.Assert(maps.Contains(map), "Map must be contained in state");

            // Enable all controls in map that aren't already enabled.
            EnableControls(map);

            // Put all actions that aren't already enabled into waiting state.
            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index on InputActionMap is out of range");
            var actionCount = mapIndices[mapIndex].actionCount;
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;
            for (var i = 0; i < actionCount; ++i)
            {
                var actionIndex = actionStartIndex + i;
                var actionState = &actionStates[actionIndex];
                if (actionState->isDisabled)
                    actionState->phase = InputActionPhase.Waiting;
                actionState->inProcessing = false;
            }
            map.m_EnabledActionsCount = actionCount;

            HookOnBeforeUpdate();

            // Make sure that if we happen to get here with one of the hidden action maps we create for singleton
            // action, we notify on the action, not the hidden map.
            if (map.m_SingletonAction != null)
                NotifyListenersOfActionChange(InputActionChange.ActionEnabled, map.m_SingletonAction);
            else
                NotifyListenersOfActionChange(InputActionChange.ActionMapEnabled, map);
        }

        private void EnableControls(InputActionMap map)
        {
            Debug.Assert(map != null, "Map must not be null");
            Debug.Assert(map.m_Actions != null, "Map must have actions");
            Debug.Assert(maps.Contains(map), "Map must be contained in state");

            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index on InputActionMap is out of range");

            // Install state monitors for all controls.
            var controlCount = mapIndices[mapIndex].controlCount;
            var controlStartIndex = mapIndices[mapIndex].controlStartIndex;
            if (controlCount > 0)
                EnableControls(mapIndex, controlStartIndex, controlCount);
        }

        public void EnableSingleAction(InputAction action)
        {
            Debug.Assert(action != null, "Action must not be null");
            Debug.Assert(action.m_ActionMap != null, "Action must have action map");
            Debug.Assert(maps.Contains(action.m_ActionMap), "Action map must be contained in state");

            EnableControls(action);

            // Put action into waiting state.
            var actionIndex = action.m_ActionIndexInState;
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount,
                "Action index out of range when enabling single action");
            actionStates[actionIndex].phase = InputActionPhase.Waiting;
            ++action.m_ActionMap.m_EnabledActionsCount;

            HookOnBeforeUpdate();
            NotifyListenersOfActionChange(InputActionChange.ActionEnabled, action);
        }

        private void EnableControls(InputAction action)
        {
            Debug.Assert(action != null, "Action must not be null");
            Debug.Assert(action.m_ActionMap != null, "Action must have action map");
            Debug.Assert(maps.Contains(action.m_ActionMap), "Map must be contained in state");

            var actionIndex = action.m_ActionIndexInState;
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount,
                "Action index out of range when enabling controls");

            var map = action.m_ActionMap;
            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range in EnableControls");

            // Go through all bindings in the map and for all that belong to the given action,
            // enable the associated controls.
            var bindingStartIndex = mapIndices[mapIndex].bindingStartIndex;
            var bindingCount = mapIndices[mapIndex].bindingCount;
            var bindingStatesPtr = memory.bindingStates;
            for (var i = 0; i < bindingCount; ++i)
            {
                var bindingIndex = bindingStartIndex + i;
                var bindingState = &bindingStatesPtr[bindingIndex];
                if (bindingState->actionIndex != actionIndex)
                    continue;

                // Composites enable en-bloc through the composite binding itself.
                if (bindingState->isPartOfComposite)
                    continue;

                var controlCount = bindingState->controlCount;
                if (controlCount == 0)
                    continue;

                EnableControls(mapIndex, bindingState->controlStartIndex, controlCount);
            }
        }

        public void DisableAllActions(InputActionMap map)
        {
            Debug.Assert(map != null, "Map must not be null");
            Debug.Assert(map.m_Actions != null, "Map must have actions");
            Debug.Assert(maps.Contains(map), "Map must be contained in state");

            DisableControls(map);

            // Mark all actions as disabled.
            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range in DisableAllActions");
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;
            var actionCount = mapIndices[mapIndex].actionCount;
            var allActionsEnabled = map.m_EnabledActionsCount == actionCount;
            for (var i = 0; i < actionCount; ++i)
            {
                var actionIndex = actionStartIndex + i;
                if (actionStates[actionIndex].phase != InputActionPhase.Disabled)
                {
                    ResetActionState(actionIndex, toPhase: InputActionPhase.Disabled);
                    if (!allActionsEnabled)
                        NotifyListenersOfActionChange(InputActionChange.ActionDisabled, map.m_Actions[i]);
                }
            }
            map.m_EnabledActionsCount = 0;

            // Make sure that if we happen to get here with one of the hidden action maps we create for singleton
            // action, we notify on the action, not the hidden map.
            if (map.m_SingletonAction != null)
                NotifyListenersOfActionChange(InputActionChange.ActionDisabled, map.m_SingletonAction);
            else if (allActionsEnabled)
                NotifyListenersOfActionChange(InputActionChange.ActionMapDisabled, map);
        }

        public void DisableControls(InputActionMap map)
        {
            Debug.Assert(map != null, "Map must not be null");
            Debug.Assert(map.m_Actions != null, "Map must have actions");
            Debug.Assert(maps.Contains(map), "Map must be contained in state");

            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range in DisableControls(InputActionMap)");

            // Remove state monitors from all controls.
            var controlCount = mapIndices[mapIndex].controlCount;
            var controlStartIndex = mapIndices[mapIndex].controlStartIndex;
            if (controlCount > 0)
                DisableControls(mapIndex, controlStartIndex, controlCount);
        }

        public void DisableSingleAction(InputAction action)
        {
            Debug.Assert(action != null, "Action must not be null");
            Debug.Assert(action.m_ActionMap != null, "Action must have action map");
            Debug.Assert(maps.Contains(action.m_ActionMap), "Action map must be contained in state");

            DisableControls(action);
            ResetActionState(action.m_ActionIndexInState, toPhase: InputActionPhase.Disabled);
            --action.m_ActionMap.m_EnabledActionsCount;

            NotifyListenersOfActionChange(InputActionChange.ActionDisabled, action);
        }

        private void DisableControls(InputAction action)
        {
            Debug.Assert(action != null, "Action must not be null");
            Debug.Assert(action.m_ActionMap != null, "Action must have action map");
            Debug.Assert(maps.Contains(action.m_ActionMap), "Action map must be contained in state");

            var actionIndex = action.m_ActionIndexInState;
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount,
                "Action index out of range when disabling controls");

            var map = action.m_ActionMap;
            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range in DisableControls(InputAction)");

            // Go through all bindings in the map and for all that belong to the given action,
            // disable the associated controls.
            var bindingStartIndex = mapIndices[mapIndex].bindingStartIndex;
            var bindingCount = mapIndices[mapIndex].bindingCount;
            var bindingStatesPtr = memory.bindingStates;
            for (var i = 0; i < bindingCount; ++i)
            {
                var bindingIndex = bindingStartIndex + i;
                var bindingState = &bindingStatesPtr[bindingIndex];
                if (bindingState->actionIndex != actionIndex)
                    continue;

                // Composites enable en-bloc through the composite binding itself.
                if (bindingState->isPartOfComposite)
                    continue;

                var controlCount = bindingState->controlCount;
                if (controlCount == 0)
                    continue;

                DisableControls(mapIndex, bindingState->controlStartIndex, controlCount);
            }
        }

        ////REVIEW: can we have a method on InputManager doing this in bulk?

        ////NOTE: This must not enable only a partial set of controls on a binding (currently we have no setup that would lead to that)
        private void EnableControls(int mapIndex, int controlStartIndex, int numControls)
        {
            Debug.Assert(controls != null, "State must have controls");
            Debug.Assert(controlStartIndex >= 0 && (controlStartIndex < totalControlCount || numControls == 0),
                "Control start index out of range");
            Debug.Assert(controlStartIndex + numControls <= totalControlCount, "Control range out of bounds");

            var manager = InputSystem.s_Manager;
            for (var i = 0; i < numControls; ++i)
            {
                var controlIndex = controlStartIndex + i;

                // We don't want to add multiple state monitors for the same control. This can happen if enabling
                // single actions is mixed with enabling actions maps containing them.
                if (IsControlEnabled(controlIndex))
                    continue;

                var bindingIndex = controlIndexToBindingIndex[controlIndex];
                var mapControlAndBindingIndex = ToCombinedMapAndControlAndBindingIndex(mapIndex, controlIndex, bindingIndex);

                var bindingStatePtr = &bindingStates[bindingIndex];
                if (bindingStatePtr->wantsInitialStateCheck)
                    SetInitialStateCheckPending(bindingStatePtr, true);
                manager.AddStateChangeMonitor(controls[controlIndex], this, mapControlAndBindingIndex, controlGroupingAndComplexity[controlIndex * 2]);

                SetControlEnabled(controlIndex, true);
            }
        }

        private void DisableControls(int mapIndex, int controlStartIndex, int numControls)
        {
            Debug.Assert(controls != null, "State must have controls");
            Debug.Assert(controlStartIndex >= 0 && (controlStartIndex < totalControlCount || numControls == 0),
                "Control start index out of range");
            Debug.Assert(controlStartIndex + numControls <= totalControlCount, "Control range out of bounds");

            var manager = InputSystem.s_Manager;
            for (var i = 0; i < numControls; ++i)
            {
                var controlIndex = controlStartIndex + i;
                ////TODO: This can be done much more efficiently by at least going byte by byte in the mask instead of just bit by bit
                if (!IsControlEnabled(controlIndex))
                    continue;

                var bindingIndex = controlIndexToBindingIndex[controlIndex];
                var mapControlAndBindingIndex = ToCombinedMapAndControlAndBindingIndex(mapIndex, controlIndex, bindingIndex);
                var bindingStatePtr = &bindingStates[bindingIndex];
                if (bindingStatePtr->wantsInitialStateCheck)
                    SetInitialStateCheckPending(bindingStatePtr, false);
                manager.RemoveStateChangeMonitor(controls[controlIndex], this, mapControlAndBindingIndex);

                // Ensure that pressTime is reset if the composite binding is reenable. ISXB-505
                bindingStatePtr->pressTime = default;

                SetControlEnabled(controlIndex, false);
            }
        }

        public void SetInitialStateCheckPending(int actionIndex, bool value = true)
        {
            var mapIndex = actionStates[actionIndex].mapIndex;
            var bindingStartIndex = mapIndices[mapIndex].bindingStartIndex;
            var bindingCount = mapIndices[mapIndex].bindingCount;
            for (var i = 0; i < bindingCount; ++i)
            {
                ref var bindingState = ref bindingStates[bindingStartIndex + i];
                if (bindingState.actionIndex == actionIndex && !bindingState.isPartOfComposite)
                    bindingState.initialStateCheckPending = value;
            }
        }

        private void SetInitialStateCheckPending(BindingState* bindingStatePtr, bool value)
        {
            if (bindingStatePtr->isPartOfComposite)
            {
                // For composites, we always flag the composite itself as wanting an initial state check. This
                // way, we don't have to worry about triggering the composite multiple times when several of its
                // controls are actuated.
                var compositeIndex = bindingStatePtr->compositeOrCompositeBindingIndex;
                bindingStates[compositeIndex].initialStateCheckPending = value;
            }
            else
            {
                bindingStatePtr->initialStateCheckPending = value;
            }
        }

        private bool IsControlEnabled(int controlIndex)
        {
            var intIndex = controlIndex / 32;
            var mask = 1U << (controlIndex % 32);
            return (enabledControls[intIndex] & mask) != 0;
        }

        private void SetControlEnabled(int controlIndex, bool state)
        {
            var intIndex = controlIndex / 32;
            var mask = 1U << (controlIndex % 32);

            if (state)
                enabledControls[intIndex] |= mask;
            else
                enabledControls[intIndex] &= ~mask;
        }

        private void HookOnBeforeUpdate()
        {
            if (m_OnBeforeUpdateHooked)
                return;

            if (m_OnBeforeUpdateDelegate == null)
                m_OnBeforeUpdateDelegate = OnBeforeInitialUpdate;
            InputSystem.s_Manager.onBeforeUpdate += m_OnBeforeUpdateDelegate;
            m_OnBeforeUpdateHooked = true;
        }

        private void UnhookOnBeforeUpdate()
        {
            if (!m_OnBeforeUpdateHooked)
                return;

            InputSystem.s_Manager.onBeforeUpdate -= m_OnBeforeUpdateDelegate;
            m_OnBeforeUpdateHooked = false;
        }

        // We hook this into InputManager.onBeforeUpdate every time actions are enabled and then take it off
        // the list after the first call. Inside here we check whether any actions we enabled already have
        // non-default state on bound controls.
        //
        // NOTE: We do this as a callback from onBeforeUpdate rather than directly when the action is enabled
        //       to ensure that the callbacks happen during input processing and not randomly from wherever
        //       an action happens to be enabled.
        private void OnBeforeInitialUpdate()
        {
            if (InputState.currentUpdateType == InputUpdateType.BeforeRender
                #if UNITY_EDITOR
                || InputState.currentUpdateType == InputUpdateType.Editor
                #endif
            )
                return;

            // Remove us from the callback as the processing we're doing here is a one-time thing.
            UnhookOnBeforeUpdate();

            k_InputInitialActionStateCheckMarker.Begin();

            // Use current time as time of control state change.
            var time = InputState.currentTime;

            ////REVIEW: should we store this data in a separate place rather than go through all bindingStates?

            // Go through all binding states and for every binding that needs an initial state check,
            // go through all bound controls and for each one that isn't in its default state, pretend
            // that the control just got actuated.
            var manager = InputSystem.s_Manager;
            for (var bindingIndex = 0; bindingIndex < totalBindingCount; ++bindingIndex)
            {
                ref var bindingState = ref bindingStates[bindingIndex];
                if (!bindingState.initialStateCheckPending)
                    continue;

                Debug.Assert(!bindingState.isPartOfComposite, "Initial state check flag must be set on composite, not on its parts");
                bindingState.initialStateCheckPending = false;

                var controlStartIndex = bindingState.controlStartIndex;
                var controlCount = bindingState.controlCount;

                var isComposite = bindingState.isComposite;
                var didFindControlToSignal = false;
                for (var n = 0; n < controlCount; ++n)
                {
                    var controlIndex = controlStartIndex + n;
                    var control = controls[controlIndex];

                    // Leave any control alone that is already driving an interaction and/or action.
                    if (IsActiveControl(bindingIndex, controlIndex))
                        continue;

                    if (!control.CheckStateIsAtDefault())
                    {
                        // Update press times.
                        if (control.IsValueConsideredPressed(control.magnitude))
                        {
                            // ReSharper disable once CompareOfFloatsByEqualityOperator
                            if (bindingState.pressTime == default || bindingState.pressTime > time)
                                bindingState.pressTime = time;
                        }

                        // For composites, any one actuated control will lead to the composite being
                        // processed as a whole so we can stop here. This also ensures that we are
                        // not triggering the composite repeatedly if there are multiple actuated
                        // controls bound to its parts.
                        if (isComposite && didFindControlToSignal)
                            continue;

                        manager.SignalStateChangeMonitor(control, this);
                        didFindControlToSignal = true;
                    }
                }
            }
            manager.FireStateChangeNotifications();

            k_InputInitialActionStateCheckMarker.End();
        }

        // Called from InputManager when one of our state change monitors has fired.
        // Tells us the time of the change *according to the state events coming in*.
        // Also tells us which control of the controls we are binding to triggered the
        // change and relays the binding index we gave it when we called AddChangeMonitor.
        void IInputStateChangeMonitor.NotifyControlStateChanged(InputControl control, double time,
            InputEventPtr eventPtr, long mapControlAndBindingIndex)
        {
            #if UNITY_EDITOR
            if (InputState.currentUpdateType == InputUpdateType.Editor)
                return;
            #endif

            SplitUpMapAndControlAndBindingIndex(mapControlAndBindingIndex, out var mapIndex, out var controlIndex, out var bindingIndex);
            ProcessControlStateChange(mapIndex, controlIndex, bindingIndex, time, eventPtr);
        }

        void IInputStateChangeMonitor.NotifyTimerExpired(InputControl control, double time,
            long mapControlAndBindingIndex, int interactionIndex)
        {
            SplitUpMapAndControlAndBindingIndex(mapControlAndBindingIndex, out var mapIndex, out var controlIndex, out var bindingIndex);
            ProcessTimeout(time, mapIndex, controlIndex, bindingIndex, interactionIndex);
        }

        /// <summary>
        /// Bit pack the mapIndex, controlIndex, bindingIndex and complexity components into a single long monitor index value.
        /// </summary>
        /// <param name="mapIndex">The mapIndex value to pack.</param>
        /// <param name="controlIndex">The controlIndex value to pack.</param>
        /// <param name="bindingIndex">The bindingIndex value to pack..</param>
        /// <remarks>
        /// We mangle the various indices we use into a single long for association with state change
        /// monitors. While we could look up map and binding indices from control indices, keeping
        /// all the information together avoids having to unnecessarily jump around in memory to grab
        /// the various pieces of data.
        /// The complexity component is implicitly derived and does not need to be passed as an argument.
        /// </remarks>
        private long ToCombinedMapAndControlAndBindingIndex(int mapIndex, int controlIndex, int bindingIndex)
        {
            // We have limits on the numbers of maps, controls, and bindings we allow in any single
            // action state (see TriggerState.kMaxNumXXX).
            var complexity = controlGroupingAndComplexity[controlIndex * 2 + 1];
            var result = (long)controlIndex;
            result |= (long)bindingIndex << 24;
            result |= (long)mapIndex << 40;
            result |= (long)complexity << 48;
            return result;
        }

        /// <summary>
        /// Extract the mapIndex, controlIndex and bindingIndex components from the provided bit packed argument (monitor index).
        /// </summary>
        /// <param name="mapControlAndBindingIndex">Represents a monitor index, which is a bit packed field containing multiple components.</param>
        /// <param name="mapIndex">Will hold the extracted mapIndex value after the function completes.</param>
        /// <param name="controlIndex">Will hold the extracted controlIndex value after the function completes.</param>
        /// <param name="bindingIndex">Will hold the extracted bindingIndex value after the function completes.</param>
        private void SplitUpMapAndControlAndBindingIndex(long mapControlAndBindingIndex, out int mapIndex,
            out int controlIndex, out int bindingIndex)
        {
            controlIndex = (int)(mapControlAndBindingIndex & 0x00ffffff);
            bindingIndex = (int)((mapControlAndBindingIndex >> 24) & 0xffff);
            mapIndex = (int)((mapControlAndBindingIndex >> 40) & 0xff);
        }

        /// <summary>
        /// Extract the 'complexity' component from the provided bit packed argument (monitor index).
        /// </summary>
        /// <param name="mapControlAndBindingIndex">Represents a monitor index, which is a bit packed field containing multiple components.</param>
        internal static int GetComplexityFromMonitorIndex(long mapControlAndBindingIndex)
        {
            return (int)((mapControlAndBindingIndex >> 48) & 0xff);
        }

        /// <summary>
        /// Process a state change that has happened in one of the controls attached
        /// to this action map state.
        /// </summary>
        /// <param name="mapIndex">Index of the action map to which the binding belongs.</param>
        /// <param name="controlIndex">Index of the control that changed state.</param>
        /// <param name="bindingIndex">Index of the binding associated with the given control.</param>
        /// <param name="time">The timestamp associated with the state change (comes from the state change event).</param>
        /// <param name="eventPtr">Event (if any) that triggered the state change.</param>
        /// <remarks>
        /// This is where we end up if one of the state monitors we've put in the system has triggered.
        /// From here we go back to the associated binding and then let it figure out what the state change
        /// means for it.
        ///
        /// Note that we get called for any change in state even if the change in state does not actually
        /// result in a change of value on the respective control.
        /// </remarks>
        private void ProcessControlStateChange(int mapIndex, int controlIndex, int bindingIndex, double time, InputEventPtr eventPtr)
        {
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range in ProcessControlStateChange");
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index out of range");
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");

            using (InputActionRebindingExtensions.DeferBindingResolution())
            {
                // Callbacks can do pretty much anything and thus trigger arbitrary state/configuration
                // changes in the system. We have to ensure that while we're executing callbacks, our
                // current InputActionState is not getting changed from under us. We dictate that while
                // m_InProcessControlStateChange is true, no binding resolution can be triggered on the state and
                // it cannot be destroyed.
                //
                // This is also why we defer binding resolution above. If there is a configuration change
                // triggered by an action callback, the state will be marked dirty and re-resolved after
                // we have completed the callback.
                m_InProcessControlStateChange = true;
                m_CurrentlyProcessingThisEvent = eventPtr;

                try
                {
                    var bindingStatePtr = &bindingStates[bindingIndex];
                    var actionIndex = bindingStatePtr->actionIndex;

                    var trigger = new TriggerState
                    {
                        mapIndex = mapIndex,
                        controlIndex = controlIndex,
                        bindingIndex = bindingIndex,
                        interactionIndex = kInvalidIndex,
                        time = time,
                        startTime = time,
                        isPassThrough = actionIndex != kInvalidIndex && actionStates[actionIndex].isPassThrough,
                        isButton = actionIndex != kInvalidIndex && actionStates[actionIndex].isButton,
                    };

                    // If we have pending initial state checks that will run in the next update,
                    // force-reset the flag on the control that just triggered. This ensures that we're
                    // not triggering an action twice from the same state change in case the initial state
                    // check happens later (see Actions_ValueActionsEnabledInOnEvent_DoNotReactToCurrentStateOfControlTwice).
                    if (m_OnBeforeUpdateHooked)
                        bindingStatePtr->initialStateCheckPending = false;

                    // Store magnitude. We do this once and then only read it from here.
                    var control = controls[controlIndex];
                    trigger.magnitude = control.CheckStateIsAtDefault() ? 0f : control.magnitude;
                    controlMagnitudes[controlIndex] = trigger.magnitude;

                    // Update press times.
                    if (control.IsValueConsideredPressed(trigger.magnitude))
                    {
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (bindingStatePtr->pressTime == default || bindingStatePtr->pressTime > trigger.time)
                            bindingStatePtr->pressTime = trigger.time;
                    }

                    // If the binding is part of a composite, check for interactions on the composite
                    // itself and give them a first shot at processing the value change.
                    var haveInteractionsOnComposite = false;
                    if (bindingStatePtr->isPartOfComposite)
                    {
                        var compositeBindingIndex = bindingStatePtr->compositeOrCompositeBindingIndex;
                        var compositeBindingPtr = &bindingStates[compositeBindingIndex];

                        // If the composite has already been triggered from the very same event, ignore it.
                        // Example: KeyboardState change that includes both A and W key state changes and we're looking
                        //          at a WASD composite binding. There's a state change monitor on both the A and the W
                        //          key and thus the manager will notify us individually of both changes. However, we
                        //          want to perform the action only once.
                        if (ShouldIgnoreInputOnCompositeBinding(compositeBindingPtr, eventPtr))
                            return;

                        // Update magnitude for composite.
                        var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                        var compositeContext = new InputBindingCompositeContext
                        {
                            m_State = this,
                            m_BindingIndex = compositeBindingIndex
                        };
                        trigger.magnitude = composites[compositeIndex].EvaluateMagnitude(ref compositeContext);
                        memory.compositeMagnitudes[compositeIndex] = trigger.magnitude;

                        // Run through interactions on composite.
                        var interactionCountOnComposite = compositeBindingPtr->interactionCount;
                        if (interactionCountOnComposite > 0)
                        {
                            haveInteractionsOnComposite = true;
                            ProcessInteractions(ref trigger,
                                compositeBindingPtr->interactionStartIndex,
                                interactionCountOnComposite);
                        }
                    }

                    // Check if we have multiple concurrent actuations on the same action. This may lead us
                    // to ignore certain inputs (e.g. when we get an input of lesser magnitude while already having
                    // one of higher magnitude) or may even lead us to switch to processing a different binding
                    // (e.g. when an input of previously greater magnitude has now fallen below the level of another
                    // ongoing input with now higher magnitude).
                    var isConflictingInput = IsConflictingInput(ref trigger, actionIndex);
                    bindingStatePtr = &bindingStates[trigger.bindingIndex]; // IsConflictingInput may switch us to a different binding.

                    // Process button presses/releases.
                    if (!isConflictingInput)
                        ProcessButtonState(ref trigger, actionIndex, bindingStatePtr);

                    // If we have interactions, let them do all the processing. The presence of an interaction
                    // essentially bypasses the default phase progression logic of an action.
                    var interactionCount = bindingStatePtr->interactionCount;
                    if (interactionCount > 0 && !bindingStatePtr->isPartOfComposite)
                    {
                        ProcessInteractions(ref trigger, bindingStatePtr->interactionStartIndex, interactionCount);
                    }
                    else if (!haveInteractionsOnComposite && !isConflictingInput)
                    {
                        ProcessDefaultInteraction(ref trigger, actionIndex);
                    }
                }
                finally
                {
                    m_InProcessControlStateChange = default;
                    m_CurrentlyProcessingThisEvent = default;
                }
            }
        }

        private void ProcessButtonState(ref TriggerState trigger, int actionIndex, BindingState* bindingStatePtr)
        {
            var control = controls[trigger.controlIndex];
            var pressPoint = control.isButton
                ? ((ButtonControl)control).pressPointOrDefault
                : ButtonControl.s_GlobalDefaultButtonPressPoint;

            // NOTE: This method relies on conflict resolution happening *first*. Otherwise, we may inadvertently
            //       detect a "release" from a control that is not actually driving the action.

            // Record release time on the binding.
            // NOTE: Explicitly look up control magnitude here instead of using trigger.magnitude
            //       as for part bindings, the trigger will have the magnitude of the whole composite.
            var controlActuation = controlMagnitudes[trigger.controlIndex];
            if (controlActuation <= pressPoint * ButtonControl.s_GlobalDefaultButtonReleaseThreshold)
                bindingStatePtr->pressTime = 0d;

            var actuation = trigger.magnitude;
            var actionState = &actionStates[actionIndex];
            if (!actionState->isPressed && actuation >= pressPoint)
            {
                actionState->framePressed = Time.frameCount;
                actionState->pressedInUpdate = InputUpdate.s_UpdateStepCount;
                actionState->isPressed = true;
            }
            else if (actionState->isPressed)
            {
                var releasePoint = pressPoint * ButtonControl.s_GlobalDefaultButtonReleaseThreshold;
                if (actuation <= releasePoint)
                {
                    actionState->frameReleased = Time.frameCount;
                    actionState->releasedInUpdate = InputUpdate.s_UpdateStepCount;
                    actionState->isPressed = false;
                }
            }
        }

        /// <summary>
        /// Whether the given state change on a composite binding should be ignored.
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="eventPtr"></param>
        /// <returns></returns>
        /// <remarks>
        /// Each state event may change the state of arbitrary many controls on a device and thus may trigger
        /// several bindings at once that are part of the same composite binding. We still want to trigger the
        /// composite binding only once for the event.
        ///
        /// To do so, we store the ID of the event on the binding and ignore events if they have the same
        /// ID as the one we've already recorded.
        /// </remarks>
        private static bool ShouldIgnoreInputOnCompositeBinding(BindingState* binding, InputEvent* eventPtr)
        {
            if (eventPtr == null)
                return false;

            var eventId = eventPtr->eventId;
            if (eventId != 0 && binding->triggerEventIdForComposite == eventId)
                return true;

            binding->triggerEventIdForComposite = eventId;
            return false;
        }

        /// <summary>
        /// Whether the given control state should be ignored.
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="actionIndex"></param>
        /// <returns></returns>
        /// <remarks>
        /// If an action has multiple controls bound to it, control state changes on the action may conflict with each other.
        /// If that happens, we resolve the conflict by always sticking to the most actuated control.
        ///
        /// Pass-through actions (<see cref="InputActionType.PassThrough"/>) will always bypass conflict resolution and respond
        /// to every value change.
        ///
        /// Actions that are resolved to only a single control will early out of conflict resolution.
        ///
        /// Actions that are bound to multiple controls but have only one control actuated will early out of conflict
        /// resolution as well.
        ///
        /// Note that conflict resolution here is entirely tied to magnitude. This ignores other qualities that the value
        /// of a control may have. For example, one 2D vector may have a similar magnitude to another yet point in an
        /// entirely different direction.
        ///
        /// There are other conflict resolution mechanisms that could be used. For example, we could average the values
        /// from all controls. However, it would not necessarily result in more useful conflict resolution and would
        /// at the same time be much more expensive.
        /// </remarks>
        private bool IsConflictingInput(ref TriggerState trigger, int actionIndex)
        {
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount,
                "Action index out of range when checking for conflicting control input");

            // The goal of this method is to provide conflict resolution but do so ONLY if it is
            // really needed. In the vast majority of cases, this method should do almost nothing and
            // simply return straight away.

            // If conflict resolution is disabled on the action, early out. This is the case for pass-through
            // actions and for actions that cannot get into an ambiguous state based on the controls they
            // are bound to.
            var actionState = &actionStates[actionIndex];
            if (!actionState->mayNeedConflictResolution)
                return false;

            // Anything past here happens only for actions that may have conflicts.
            // Anything below here we want to avoid executing whenever we can.
            Debug.Assert(actionState->mayNeedConflictResolution);

            k_InputActionResolveConflictMarker.Begin();

            // We take a local copy of this value, so we can change it to use the starting control of composites
            // for simpler conflict resolution (so composites always use the same value), but still report the actually
            // actuated control to the user.
            var triggerControlIndex = trigger.controlIndex;
            if (bindingStates[trigger.bindingIndex].isPartOfComposite)
            {
                // For actions that need conflict resolution, we force TriggerState.controlIndex to the
                // first control in a composite. Otherwise it becomes much harder to tell if the we have
                // multiple concurrent actuations or not.
                // Since composites always evaluate as a whole instead of as single controls, having
                // triggerControlIndex differ from the state monitor that fired should be fine.
                var compositeBindingIndex = bindingStates[trigger.bindingIndex].compositeOrCompositeBindingIndex;
                triggerControlIndex = bindingStates[compositeBindingIndex].controlStartIndex;
                Debug.Assert(triggerControlIndex >= 0 && triggerControlIndex < totalControlCount,
                    "Control start index on composite binding out of range");
            }

            // Determine which control to consider the one currently associated with the action.
            // We do the same thing as for the triggered control and in the case of a composite,
            // switch to the first control of the composite.
            var actionStateControlIndex = actionState->controlIndex;
            if (bindingStates[actionState->bindingIndex].isPartOfComposite)
            {
                var compositeBindingIndex = bindingStates[actionState->bindingIndex].compositeOrCompositeBindingIndex;
                actionStateControlIndex = bindingStates[compositeBindingIndex].controlStartIndex;
            }

            // Never ignore state changes for actions that aren't currently driven by
            // anything.
            if (actionStateControlIndex == kInvalidIndex)
            {
                actionState->magnitude = trigger.magnitude;
                k_InputActionResolveConflictMarker.End();
                return false;
            }

            // Find out if we get triggered from the control that is actively driving the action.
            var isControlCurrentlyDrivingTheAction = triggerControlIndex == actionStateControlIndex ||
                controls[triggerControlIndex] == controls[actionStateControlIndex];                                      // Same control, different binding.

            // If the control is actuated *more* than the current level of actuation we recorded for the
            // action, we process the state change normally. If this isn't the control that is already
            // driving the action, it will become the one now.
            //
            // NOTE: For composites, we're looking at the combined actuation of the entire binding here,
            //       not just at the actuation level of the individual control. ComputeMagnitude()
            //       automatically takes care of that for us.
            if (trigger.magnitude > actionState->magnitude)
            {
                // If this is not the control that is currently driving the action, we know
                // there are multiple controls that are concurrently actuated on the action.
                // Remember that so that when the controls are released again, we can more
                // efficiently determine whether we need to take multiple bound controls into
                // account or not.
                // NOTE: For composites, we have forced triggerControlIndex to the first control
                //       in the composite. See above.
                if (trigger.magnitude > 0 && !isControlCurrentlyDrivingTheAction && actionState->magnitude > 0)
                    actionState->hasMultipleConcurrentActuations = true;

                // Keep recorded magnitude in action state up to date.
                actionState->magnitude = trigger.magnitude;
                k_InputActionResolveConflictMarker.End();
                return false;
            }

            // If the control is actuated *less* then the current level of actuation we
            // recorded for the action *and* the control that changed is the one that is currently
            // driving the action, we have to check whether there is another actuation
            // that is now *higher* than what we're getting from the current control.
            if (trigger.magnitude < actionState->magnitude)
            {
                // If we're not currently driving the action, it's simple. Doesn't matter that we lowered
                // actuation as we didn't have the highest actuation anyway.
                if (!isControlCurrentlyDrivingTheAction)
                {
                    k_InputActionResolveConflictMarker.End();
                    ////REVIEW: should we *count* actuations instead? (problem is that then we have to reliably determine when a control
                    ////        first actuates; the current solution will occasionally run conflict resolution when it doesn't have to
                    ////        but won't require the extra bookkeeping)
                    // Do NOT let this control state change affect the action.
                    if (trigger.magnitude > 0)
                        actionState->hasMultipleConcurrentActuations = true;
                    return true;
                }

                // If we don't have multiple controls that are currently actuated, it's simple.
                if (!actionState->hasMultipleConcurrentActuations)
                {
                    // Keep recorded magnitude in action state up to date.
                    actionState->magnitude = trigger.magnitude;
                    k_InputActionResolveConflictMarker.End();
                    return false;
                }

                ////REVIEW: is there a simpler way we can do this???

                // So, now we know we are actually looking at a potential conflict. Multiple
                // controls bound to the action are actuated but we don't yet know whether
                // any of them is actuated *more* than the control that had just changed value.
                // Go through the bindings for the action and see what we've got.
                var bindingStartIndex = GetActionBindingStartIndexAndCount(actionIndex, out var bindingCount);
                var highestActuationLevel = trigger.magnitude;
                var controlWithHighestActuation = kInvalidIndex;
                var bindingWithHighestActuation = kInvalidIndex;
                var numActuations = 0;
                for (var i = 0; i < bindingCount; ++i)
                {
                    var bindingIndex = memory.actionBindingIndices[bindingStartIndex + i];
                    var binding = &memory.bindingStates[bindingIndex];

                    if (binding->isComposite)
                    {
                        // Composite bindings result in a single actuation value regardless of how
                        // many controls are bound through the parts of the composite.

                        var firstControlIndex = binding->controlStartIndex;
                        var compositeIndex = binding->compositeOrCompositeBindingIndex;

                        Debug.Assert(compositeIndex >= 0 && compositeIndex < totalCompositeCount,
                            "Composite index out of range on composite");

                        var magnitude = memory.compositeMagnitudes[compositeIndex];
                        if (magnitude > 0)
                            ++numActuations;
                        if (magnitude > highestActuationLevel)
                        {
                            Debug.Assert(firstControlIndex >= 0 && firstControlIndex < totalControlCount,
                                "Control start index out of range on composite");

                            controlWithHighestActuation = firstControlIndex;
                            bindingWithHighestActuation = controlIndexToBindingIndex[firstControlIndex];
                            highestActuationLevel = magnitude;
                        }
                    }
                    else if (!binding->isPartOfComposite)
                    {
                        // Check actuation of each control on the binding.
                        for (var n = 0; n < binding->controlCount; ++n)
                        {
                            var controlIndex = binding->controlStartIndex + n;
                            var magnitude = memory.controlMagnitudes[controlIndex];

                            if (magnitude > 0)
                                ++numActuations;

                            if (magnitude > highestActuationLevel)
                            {
                                controlWithHighestActuation = controlIndex;
                                bindingWithHighestActuation = bindingIndex;
                                highestActuationLevel = magnitude;
                            }
                        }
                    }
                }

                // Update our record of whether there are multiple concurrent actuations.
                if (numActuations <= 1)
                    actionState->hasMultipleConcurrentActuations = false;

                // If we didn't find a control with a higher actuation level, then go and process
                // the control value change.
                if (controlWithHighestActuation != kInvalidIndex)
                {
                    // We do have a control with a higher actuation level. Switch from our current
                    // control to processing the control with the now highest actuation level.
                    //
                    // NOTE: We are processing an artificial control state change here. Information
                    //       such as the timestamp will not correspond to when the control actually
                    //       changed value. However, if we skip processing this as a separate control
                    //       change here, interactions may not behave properly as they would not be
                    //       seeing that we just lowered the actuation level on the action.
                    trigger.controlIndex = controlWithHighestActuation;
                    trigger.bindingIndex = bindingWithHighestActuation;
                    trigger.magnitude = highestActuationLevel;

                    // If we're switching to a different binding, we may also have to switch to a
                    // different stack of interactions.
                    if (actionState->bindingIndex != bindingWithHighestActuation)
                    {
                        // If there's an interaction currently driving the action, reset it.
                        // NOTE: This will also cancel an ongoing timer. So, say we're currently 0.5 seconds into
                        //       a 1 second "Hold" when the user shifts to a different control, then this code here
                        //       will *cancel* the current "Hold" and restart from scratch.
                        if (actionState->interactionIndex != kInvalidIndex)
                            ResetInteractionState(actionState->interactionIndex);

                        // If there's an interaction in progress on the new binding, let
                        // it drive the action.
                        var bindingState = &bindingStates[bindingWithHighestActuation];
                        var interactionCount = bindingState->interactionCount;
                        var interactionStartIndex = bindingState->interactionStartIndex;
                        for (var i = 0; i < interactionCount; ++i)
                        {
                            if (!interactionStates[interactionStartIndex + i].phase.IsInProgress())
                                continue;

                            actionState->interactionIndex = interactionStartIndex + i;
                            trigger.interactionIndex = interactionStartIndex + i;
                            break;
                        }
                    }

                    // We're switching the action to a different control so regardless of whether
                    // the processing of the control state change results in a call to ChangePhaseOfAction,
                    // we need to record this or the disambiguation code may start ignoring valid input.
                    actionState->controlIndex = controlWithHighestActuation;
                    actionState->bindingIndex = bindingWithHighestActuation;
                    actionState->magnitude = highestActuationLevel;

                    k_InputActionResolveConflictMarker.End();
                    return false;
                }
            }

            k_InputActionResolveConflictMarker.End();

            // If we're not really effecting any change on the action, ignore the control state change.
            // NOTE: We may be looking at a control here that points in a completely direction, for example, even
            //       though it has the same magnitude. However, we require a control to *increase* absolute actuation
            //       before we let it drive the action.
            if (!isControlCurrentlyDrivingTheAction && Mathf.Approximately(trigger.magnitude, actionState->magnitude))
            {
                // If we do have an actuation on a control that isn't currently driving the action, flag the action has
                // having multiple concurrent inputs ATM.
                if (trigger.magnitude > 0)
                    actionState->hasMultipleConcurrentActuations = true;
                return true;
            }

            return false;
        }

        private ushort GetActionBindingStartIndexAndCount(int actionIndex, out ushort bindingCount)
        {
            bindingCount = memory.actionBindingIndicesAndCounts[actionIndex * 2 + 1];
            return memory.actionBindingIndicesAndCounts[actionIndex * 2];
        }

        /// <summary>
        /// When there is no interaction on an action, this method perform the default interaction logic that we
        /// run when a bound control changes value.
        /// </summary>
        /// <param name="trigger">Control trigger state.</param>
        /// <param name="actionIndex"></param>
        /// <remarks>
        /// The default interaction does not have its own <see cref="InteractionState"/>. Whatever we do in here,
        /// we store directly on the action state.
        ///
        /// The default interaction is basically a sort of optimization where we don't require having an explicit
        /// interaction object. Conceptually, it can be thought of, however, as putting this interaction on any
        /// binding that doesn't have any other interaction on it.
        /// </remarks>
        private void ProcessDefaultInteraction(ref TriggerState trigger, int actionIndex)
        {
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount,
                "Action index out of range when processing default interaction");

            var actionState = &actionStates[actionIndex];
            switch (actionState->phase)
            {
                case InputActionPhase.Waiting:
                {
                    // Pass-through actions we perform on every value change and then go back
                    // to waiting.
                    if (trigger.isPassThrough)
                    {
                        ChangePhaseOfAction(InputActionPhase.Performed, ref trigger,
                            phaseAfterPerformedOrCanceled: InputActionPhase.Waiting);
                        break;
                    }
                    // Button actions need to cross the button-press threshold.
                    if (trigger.isButton)
                    {
                        var actuation = trigger.magnitude;
                        if (actuation > 0)
                            ChangePhaseOfAction(InputActionPhase.Started, ref trigger);
                        var threshold = controls[trigger.controlIndex] is ButtonControl button ? button.pressPointOrDefault : ButtonControl.s_GlobalDefaultButtonPressPoint;
                        if (actuation >= threshold)
                        {
                            ChangePhaseOfAction(InputActionPhase.Performed, ref trigger,
                                phaseAfterPerformedOrCanceled: InputActionPhase.Performed);
                        }
                    }
                    else
                    {
                        // Value-type action.
                        // Ignore if the control has not crossed its actuation threshold.
                        if (IsActuated(ref trigger))
                        {
                            ////REVIEW: Why is it we don't stay in performed but rather go back to started all the time?

                            // Go into started, then perform and then go back to started.
                            ChangePhaseOfAction(InputActionPhase.Started, ref trigger);
                            ChangePhaseOfAction(InputActionPhase.Performed, ref trigger,
                                phaseAfterPerformedOrCanceled: InputActionPhase.Started);
                        }
                    }

                    break;
                }

                case InputActionPhase.Started:
                {
                    if (actionState->isButton)
                    {
                        var actuation = trigger.magnitude;
                        var threshold = controls[trigger.controlIndex] is ButtonControl button ? button.pressPointOrDefault : ButtonControl.s_GlobalDefaultButtonPressPoint;
                        if (actuation >= threshold)
                        {
                            // Button crossed press threshold. Perform.
                            ChangePhaseOfAction(InputActionPhase.Performed, ref trigger,
                                phaseAfterPerformedOrCanceled: InputActionPhase.Performed);
                        }
                        else if (Mathf.Approximately(actuation, 0))
                        {
                            // Button is no longer actuated. Never reached threshold to perform.
                            // Cancel.
                            ChangePhaseOfAction(InputActionPhase.Canceled, ref trigger);
                        }
                    }
                    else
                    {
                        if (!IsActuated(ref trigger))
                        {
                            // Control went back to below actuation threshold. Cancel interaction.
                            ChangePhaseOfAction(InputActionPhase.Canceled, ref trigger);
                        }
                        else
                        {
                            // Control changed value above magnitude threshold. Perform and remain started.
                            ChangePhaseOfAction(InputActionPhase.Performed, ref trigger,
                                phaseAfterPerformedOrCanceled: InputActionPhase.Started);
                        }
                    }
                    break;
                }

                case InputActionPhase.Performed:
                {
                    if (actionState->isButton)
                    {
                        var actuation = trigger.magnitude;
                        var pressPoint = controls[trigger.controlIndex] is ButtonControl button ? button.pressPointOrDefault : ButtonControl.s_GlobalDefaultButtonPressPoint;
                        if (Mathf.Approximately(0f, actuation))
                        {
                            ChangePhaseOfAction(InputActionPhase.Canceled, ref trigger);
                        }
                        else
                        {
                            var threshold = pressPoint * ButtonControl.s_GlobalDefaultButtonReleaseThreshold;
                            if (actuation <= threshold)
                            {
                                // Button released to below threshold but not fully released.
                                ChangePhaseOfAction(InputActionPhase.Started, ref trigger);
                            }
                        }
                    }
                    else if (actionState->isPassThrough)
                    {
                        ////REVIEW: even for pass-through actions, shouldn't we cancel when seeing a default value?
                        ChangePhaseOfAction(InputActionPhase.Performed, ref trigger,
                            phaseAfterPerformedOrCanceled: InputActionPhase.Performed);
                    }
                    break;
                }

                default:
                    Debug.Assert(false, "Should not get here");
                    break;
            }
        }

        private void ProcessInteractions(ref TriggerState trigger, int interactionStartIndex, int interactionCount)
        {
            var context = new InputInteractionContext
            {
                m_State = this,
                m_TriggerState = trigger
            };

            for (var i = 0; i < interactionCount; ++i)
            {
                var index = interactionStartIndex + i;
                var state = interactionStates[index];
                var interaction = interactions[index];

                context.m_TriggerState.phase = state.phase;
                context.m_TriggerState.startTime = state.startTime;
                context.m_TriggerState.interactionIndex = index;

                interaction.Process(ref context);
            }
        }

        private void ProcessTimeout(double time, int mapIndex, int controlIndex, int bindingIndex, int interactionIndex)
        {
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index out of range");
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");
            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount, "Interaction index out of range");

            ref var currentState = ref interactionStates[interactionIndex];

            var context = new InputInteractionContext
            {
                m_State = this,
                m_TriggerState =
                    new TriggerState
                {
                    phase = currentState.phase,
                    time = time,
                    mapIndex = mapIndex,
                    controlIndex = controlIndex,
                    bindingIndex = bindingIndex,
                    interactionIndex = interactionIndex,
                    startTime = currentState.startTime
                },
                timerHasExpired = true,
            };

            currentState.isTimerRunning = false;
            currentState.totalTimeoutCompletionTimeRemaining =
                Mathf.Max(currentState.totalTimeoutCompletionTimeRemaining - currentState.timerDuration, 0);
            currentState.timerDuration = default;

            // Let interaction handle timer expiration.
            interactions[interactionIndex].Process(ref context);
        }

        internal void SetTotalTimeoutCompletionTime(float seconds, ref TriggerState trigger)
        {
            Debug.Assert(trigger.interactionIndex >= 0 && trigger.interactionIndex < totalInteractionCount, "Interaction index out of range");

            ref var interactionState = ref interactionStates[trigger.interactionIndex];
            interactionState.totalTimeoutCompletionDone = 0;
            interactionState.totalTimeoutCompletionTimeRemaining = seconds;
        }

        internal void StartTimeout(float seconds, ref TriggerState trigger)
        {
            Debug.Assert(trigger.mapIndex >= 0 && trigger.mapIndex < totalMapCount, "Map index out of range");
            Debug.Assert(trigger.controlIndex >= 0 && trigger.controlIndex < totalControlCount, "Control index out of range");
            Debug.Assert(trigger.interactionIndex >= 0 && trigger.interactionIndex < totalInteractionCount, "Interaction index out of range");

            var manager = InputSystem.s_Manager;
            var currentTime = trigger.time;
            var control = controls[trigger.controlIndex];
            var interactionIndex = trigger.interactionIndex;
            var monitorIndex =
                ToCombinedMapAndControlAndBindingIndex(trigger.mapIndex, trigger.controlIndex, trigger.bindingIndex);

            // If there's already a timeout running, cancel it first.
            ref var interactionState = ref interactionStates[interactionIndex];
            if (interactionState.isTimerRunning)
                StopTimeout(interactionIndex);

            // Add new timeout.
            manager.AddStateChangeMonitorTimeout(control, this, currentTime + seconds, monitorIndex,
                interactionIndex);

            // Update state.
            interactionState.isTimerRunning = true;
            interactionState.timerStartTime = currentTime;
            interactionState.timerDuration = seconds;
            interactionState.timerMonitorIndex = monitorIndex;
        }

        private void StopTimeout(int interactionIndex)
        {
            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount, "Interaction index out of range");

            ref var interactionState = ref interactionStates[interactionIndex];

            var manager = InputSystem.s_Manager;
            manager.RemoveStateChangeMonitorTimeout(this, interactionState.timerMonitorIndex, interactionIndex);

            // Update state.
            interactionState.isTimerRunning = false;
            interactionState.totalTimeoutCompletionDone += interactionState.timerDuration;
            interactionState.totalTimeoutCompletionTimeRemaining =
                Mathf.Max(interactionState.totalTimeoutCompletionTimeRemaining - interactionState.timerDuration, 0);
            interactionState.timerDuration = default;
            interactionState.timerStartTime = default;
            interactionState.timerMonitorIndex = default;
        }

        /// <summary>
        /// Perform a phase change on the given interaction. Only visible to observers
        /// if it happens to change the phase of the action, too.
        /// </summary>
        /// <param name="newPhase">New phase to transition the interaction to.</param>
        /// <param name="trigger">Information about the binding and control that triggered the phase change.</param>
        /// <param name="phaseAfterPerformed">If <paramref name="newPhase"/> is <see cref="InputActionPhase.Performed"/>,
        /// this determines which phase to transition to after the action has been performed. This would usually be
        /// <see cref="InputActionPhase.Waiting"/> (default), <see cref="InputActionPhase.Started"/> (if the action is supposed
        /// to be oscillate between started and performed), or <see cref="InputActionPhase.Performed"/> (if the action is
        /// supposed to perform over and over again until canceled).</param>
        /// <param name="phaseAfterCanceled">If <paramref name="newPhase"/> is <see cref="InputActionPhase.Canceled"/>,
        /// this determines which phase to transition to after the action has been canceled.</param>
        /// <param name="processNextInteractionOnCancel">Indicates if the system should try and change the phase of other
        /// interactions on the same action that are already started or performed after cancelling this interaction. This should be
        /// false when resetting interactions.</param>
        /// <remarks>
        /// Multiple interactions on the same binding can be started concurrently but the
        /// first interaction that starts will get to drive an action until it either cancels
        /// or performs the action.
        ///
        /// If an interaction driving an action performs it, all interactions will reset and
        /// go back waiting.
        ///
        /// If an interaction driving an action cancels it, the next interaction in the list which
        /// has already started will get to drive the action (example: a TapInteraction and a
        /// SlowTapInteraction both start and the TapInteraction gets to drive the action because
        /// it comes first; then the TapInteraction cancels because the button is held for too
        /// long and the SlowTapInteraction will get to drive the action next).
        /// </remarks>
        internal void ChangePhaseOfInteraction(InputActionPhase newPhase, ref TriggerState trigger,
            InputActionPhase phaseAfterPerformed = InputActionPhase.Waiting,
            InputActionPhase phaseAfterCanceled = InputActionPhase.Waiting,
            bool processNextInteractionOnCancel = true)
        {
            var interactionIndex = trigger.interactionIndex;
            var bindingIndex = trigger.bindingIndex;

            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount, "Interaction index out of range");
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");

            ////TODO: need to make sure that performed and canceled phase changes happen on the *same* binding&control
            ////      as the start of the phase

            var phaseAfterPerformedOrCanceled = InputActionPhase.Waiting;
            if (newPhase == InputActionPhase.Performed)
                phaseAfterPerformedOrCanceled = phaseAfterPerformed;
            else if (newPhase == InputActionPhase.Canceled)
                phaseAfterPerformedOrCanceled = phaseAfterCanceled;

            // Any time an interaction changes phase, we cancel all pending timeouts.
            ref var interactionState = ref interactionStates[interactionIndex];
            if (interactionState.isTimerRunning)
                StopTimeout(trigger.interactionIndex);

            // Update interaction state.
            interactionState.phase = newPhase;
            interactionState.triggerControlIndex = trigger.controlIndex;
            interactionState.startTime = trigger.startTime;
            if (newPhase == InputActionPhase.Performed)
                interactionState.performedTime = trigger.time;

            // See if it affects the phase of an associated action.
            var actionIndex = bindingStates[bindingIndex].actionIndex; // We already had to tap this array and entry in ProcessControlStateChange.
            if (actionIndex != kInvalidIndex)
            {
                if (actionStates[actionIndex].phase == InputActionPhase.Waiting)
                {
                    // We're the first interaction to go to the start phase.
                    if (!ChangePhaseOfAction(newPhase, ref trigger, phaseAfterPerformedOrCanceled))
                        return;
                }
                else if (newPhase == InputActionPhase.Canceled && actionStates[actionIndex].interactionIndex == trigger.interactionIndex)
                {
                    // We're canceling but maybe there's another interaction ready
                    // to go into start phase. *Or* there's an interaction that has
                    // already performed.

                    if (!ChangePhaseOfAction(newPhase, ref trigger, phaseAfterPerformedOrCanceled))
                        return;

                    if (processNextInteractionOnCancel == false)
                        return;

                    var interactionStartIndex = bindingStates[bindingIndex].interactionStartIndex;
                    var numInteractions = bindingStates[bindingIndex].interactionCount;
                    for (var i = 0; i < numInteractions; ++i)
                    {
                        var index = interactionStartIndex + i;
                        if (index != trigger.interactionIndex && (interactionStates[index].phase == InputActionPhase.Started ||
                                                                  interactionStates[index].phase == InputActionPhase.Performed))
                        {
                            // Trigger start.
                            var startTime = interactionStates[index].startTime;
                            var triggerForInteraction = new TriggerState
                            {
                                phase = InputActionPhase.Started,
                                controlIndex = interactionStates[index].triggerControlIndex,
                                bindingIndex = trigger.bindingIndex,
                                interactionIndex = index,
                                mapIndex = trigger.mapIndex,
                                time = startTime,
                                startTime = startTime,
                            };
                            if (!ChangePhaseOfAction(InputActionPhase.Started, ref triggerForInteraction, phaseAfterPerformedOrCanceled))
                                return;

                            // If the interaction has already performed, trigger it now.
                            if (interactionStates[index].phase == InputActionPhase.Performed)
                            {
                                triggerForInteraction = new TriggerState
                                {
                                    phase = InputActionPhase.Performed,
                                    controlIndex = interactionStates[index].triggerControlIndex,
                                    bindingIndex = trigger.bindingIndex,
                                    interactionIndex = index,
                                    mapIndex = trigger.mapIndex,
                                    time = interactionStates[index].performedTime, // Time when the interaction performed.
                                    startTime = startTime,
                                };
                                if (!ChangePhaseOfAction(InputActionPhase.Performed, ref triggerForInteraction, phaseAfterPerformedOrCanceled))
                                    return;

                                // We performed the action,
                                // so reset remaining interaction to waiting state.
                                for (; i < numInteractions; ++i)
                                {
                                    index = interactionStartIndex + i;
                                    ResetInteractionState(index);
                                }
                            }
                            break;
                        }
                    }
                }
                else if (actionStates[actionIndex].interactionIndex == trigger.interactionIndex)
                {
                    // Any other phase change goes to action if we're the interaction driving
                    // the current phase.
                    if (!ChangePhaseOfAction(newPhase, ref trigger, phaseAfterPerformedOrCanceled))
                        return;

                    // We're the interaction driving the action and we performed the action,
                    // so reset any other interaction to waiting state.
                    if (newPhase == InputActionPhase.Performed)
                    {
                        var interactionStartIndex = bindingStates[bindingIndex].interactionStartIndex;
                        var numInteractions = bindingStates[bindingIndex].interactionCount;
                        for (var i = 0; i < numInteractions; ++i)
                        {
                            var index = interactionStartIndex + i;
                            if (index != trigger.interactionIndex)
                                ResetInteractionState(index);
                        }
                    }
                }
            }

            // If the interaction performed or canceled, go back to waiting.
            // Exception: if it was performed and we're to remain in started state, set the interaction
            //            to started. Note that for that phase transition, there are no callbacks being
            //            triggered (i.e. we don't call 'started' every time after 'performed').
            if (newPhase == InputActionPhase.Performed &&
                actionIndex != kInvalidIndex && !actionStates[actionIndex].isPerformed &&
                actionStates[actionIndex].interactionIndex != trigger.interactionIndex)
            {
                // If the action was not already performed and we performed but we're not the interaction driving the action.
                // We want to stay performed to make sure that if the interaction that is currently driving the action
                // cancels, we get to perform the action. If we go back to waiting here, then the system can't tell
                // that there's another interaction ready to perform (in fact, that has already performed).
            }
            else if (newPhase == InputActionPhase.Performed && phaseAfterPerformed != InputActionPhase.Waiting)
            {
                interactionState.phase = phaseAfterPerformed;
            }
            else if (newPhase == InputActionPhase.Performed || newPhase == InputActionPhase.Canceled)
            {
                ResetInteractionState(trigger.interactionIndex);
            }
        }

        /// <summary>
        /// Change the current phase of the action referenced by <paramref name="trigger"/> to <paramref name="newPhase"/>.
        /// </summary>
        /// <param name="newPhase">New phase to transition to.</param>
        /// <param name="trigger">Trigger that caused the change in phase.</param>
        /// <param name="phaseAfterPerformedOrCanceled">The phase to immediately transition to after <paramref name="newPhase"/>
        /// when that is <see cref="InputActionPhase.Performed"/> or <see cref="InputActionPhase.Canceled"/> (<see cref="InputActionPhase.Waiting"/>
        /// by default).</param>
        /// <remarks>
        /// The change in phase is visible to observers, i.e. on the various callbacks and notifications.
        ///
        /// If <paramref name="newPhase"/> is <see cref="InputActionPhase.Performed"/> or <see cref="InputActionPhase.Canceled"/>,
        /// the action will subsequently immediately transition to <paramref name="phaseAfterPerformedOrCanceled"/>
        /// (<see cref="InputActionPhase.Waiting"/> by default). This change is not visible to observers, i.e. there won't
        /// be another run through callbacks.
        /// </remarks>
        private bool ChangePhaseOfAction(InputActionPhase newPhase, ref TriggerState trigger,
            InputActionPhase phaseAfterPerformedOrCanceled = InputActionPhase.Waiting)
        {
            Debug.Assert(newPhase != InputActionPhase.Disabled, "Should not disable an action using this method");
            Debug.Assert(trigger.mapIndex >= 0 && trigger.mapIndex < totalMapCount, "Map index out of range");
            Debug.Assert(trigger.controlIndex >= 0 && trigger.controlIndex < totalControlCount, "Control index out of range");
            Debug.Assert(trigger.bindingIndex >= 0 && trigger.bindingIndex < totalBindingCount, "Binding index out of range");

            var actionIndex = bindingStates[trigger.bindingIndex].actionIndex;
            if (actionIndex == kInvalidIndex)
                return true; // No action associated with binding.

            // Ignore if action is disabled.
            var actionState = &actionStates[actionIndex];
            if (actionState->isDisabled)
                return true;

            // We mark the action as in-processing while we execute its phase transitions and perform
            // callbacks. The callbacks may alter system state such that the action may get disabled
            // (and potentially re-enabled) while the callback is in progress. We need to make sure that
            // if that happens, we don't go and then do more processing on the action.
            actionState->inProcessing = true;
            try
            {
                // Enforce transition constraints.
                if (actionState->isPassThrough && trigger.interactionIndex == kInvalidIndex)
                {
                    // No constraints on pass-through actions except if there are interactions driving the action.
                    ChangePhaseOfActionInternal(actionIndex, actionState, newPhase, ref trigger,
                        isDisablingAction: newPhase == InputActionPhase.Canceled && phaseAfterPerformedOrCanceled == InputActionPhase.Disabled);
                    if (!actionState->inProcessing)
                        return false;
                }
                else if (newPhase == InputActionPhase.Performed && actionState->phase == InputActionPhase.Waiting)
                {
                    // Going from waiting to performed, we make a detour via started.
                    ChangePhaseOfActionInternal(actionIndex, actionState, InputActionPhase.Started, ref trigger);
                    if (!actionState->inProcessing)
                        return false;

                    // Then we perform.
                    ChangePhaseOfActionInternal(actionIndex, actionState, newPhase, ref trigger);
                    if (!actionState->inProcessing)
                        return false;

                    // And finally, if we're going back to waiting, we make a detour via canceled.
                    if (phaseAfterPerformedOrCanceled == InputActionPhase.Waiting)
                        ChangePhaseOfActionInternal(actionIndex, actionState, InputActionPhase.Canceled, ref trigger);
                    if (!actionState->inProcessing)
                        return false;

                    actionState->phase = phaseAfterPerformedOrCanceled;
                }
                else if (actionState->phase != newPhase || newPhase == InputActionPhase.Performed) // We allow Performed to trigger repeatedly.
                {
                    ChangePhaseOfActionInternal(actionIndex, actionState, newPhase, ref trigger,
                        isDisablingAction: newPhase == InputActionPhase.Canceled && phaseAfterPerformedOrCanceled == InputActionPhase.Disabled);
                    if (!actionState->inProcessing)
                        return false;

                    if (newPhase == InputActionPhase.Performed || newPhase == InputActionPhase.Canceled)
                        actionState->phase = phaseAfterPerformedOrCanceled;
                }
            }
            finally
            {
                actionState->inProcessing = false;
            }

            // If we're now waiting, reset control state. This is important for the disambiguation code
            // to not consider whatever control actuation happened on the action last.
            if (actionState->phase == InputActionPhase.Waiting)
            {
                actionState->controlIndex = kInvalidIndex;
                actionState->flags &= ~TriggerState.Flags.HaveMagnitude;
            }

            return true;
        }

        private void ChangePhaseOfActionInternal(int actionIndex, TriggerState* actionState, InputActionPhase newPhase, ref TriggerState trigger, bool isDisablingAction = false)
        {
            Debug.Assert(trigger.mapIndex == actionState->mapIndex,
                "Map index on trigger does not correspond to map index of trigger state");

            // Update action state.
            var newState = trigger;

            // We need to make sure here that any HaveMagnitude flag we may be carrying over from actionState
            // is handled correctly (case 1239551).
            newState.flags = actionState->flags; // Preserve flags.
            if (newPhase != InputActionPhase.Canceled)
                newState.magnitude = trigger.magnitude;
            else
                newState.magnitude = 0f;

            newState.phase = newPhase;
            if (newPhase == InputActionPhase.Performed)
            {
                newState.framePerformed = Time.frameCount;
                newState.lastPerformedInUpdate = InputUpdate.s_UpdateStepCount;
                newState.lastCanceledInUpdate = actionState->lastCanceledInUpdate;

                // When we perform an action, we mark the event handled such that FireStateChangeNotifications()
                // can then reset state monitors in the same group.
                // NOTE: We don't consume for controls at binding complexity 1. Those we fire in unison.
                if (controlGroupingAndComplexity[trigger.controlIndex * 2 + 1] > 1 &&
                    // we can end up switching to performed state from an interaction with a timeout, at which point
                    // the original event will probably have been removed from memory, so make sure to check
                    // we still have one
                    m_CurrentlyProcessingThisEvent.valid)
                    m_CurrentlyProcessingThisEvent.handled = true;
            }
            else if (newPhase == InputActionPhase.Canceled)
            {
                newState.lastCanceledInUpdate = InputUpdate.s_UpdateStepCount;
                newState.lastPerformedInUpdate = actionState->lastPerformedInUpdate;
                newState.framePerformed = actionState->framePerformed;
            }
            else
            {
                newState.lastPerformedInUpdate = actionState->lastPerformedInUpdate;
                newState.framePerformed = actionState->framePerformed;
                newState.lastCanceledInUpdate = actionState->lastCanceledInUpdate;
            }

            // When we go from Performed to Disabling, we take a detour through Canceled.
            // To replicate the behavior of releasedInUpdate where it doesn't get updated when the action is disabled
            // from being performed, we skip updating lastCompletedInUpdate if Disabled is the phase after Canceled.
            if (actionState->phase == InputActionPhase.Performed && newPhase != InputActionPhase.Performed &&
                !isDisablingAction)
            {
                newState.frameCompleted = Time.frameCount;
                newState.lastCompletedInUpdate = InputUpdate.s_UpdateStepCount;
            }
            else
            {
                newState.lastCompletedInUpdate = actionState->lastCompletedInUpdate;
                newState.frameCompleted = actionState->frameCompleted;
            }

            newState.pressedInUpdate = actionState->pressedInUpdate;
            newState.framePressed = actionState->framePressed;
            newState.releasedInUpdate = actionState->releasedInUpdate;
            newState.frameReleased = actionState->frameReleased;
            if (newPhase == InputActionPhase.Started)
                newState.startTime = newState.time;
            *actionState = newState;

            // Let listeners know.
            var map = maps[trigger.mapIndex];
            Debug.Assert(actionIndex >= mapIndices[trigger.mapIndex].actionStartIndex,
                "actionIndex is below actionStartIndex for map that the action belongs to");
            var action = map.m_Actions[actionIndex - mapIndices[trigger.mapIndex].actionStartIndex];
            trigger.phase = newPhase;
            switch (newPhase)
            {
                case InputActionPhase.Started:
                {
                    Debug.Assert(trigger.controlIndex != -1, "Must have control to start an action");
                    CallActionListeners(actionIndex, map, newPhase, ref action.m_OnStarted, "started");
                    break;
                }

                case InputActionPhase.Performed:
                {
                    Debug.Assert(trigger.controlIndex != -1, "Must have control to perform an action");
                    CallActionListeners(actionIndex, map, newPhase, ref action.m_OnPerformed, "performed");
                    break;
                }

                case InputActionPhase.Canceled:
                {
                    Debug.Assert(trigger.controlIndex != -1, "When canceling, must have control that started action");
                    CallActionListeners(actionIndex, map, newPhase, ref action.m_OnCanceled, "canceled");
                    break;
                }
            }
        }

        private void CallActionListeners(int actionIndex, InputActionMap actionMap, InputActionPhase phase, ref CallbackArray<InputActionListener> listeners, string callbackName)
        {
            // If there's no listeners, don't bother with anything else.
            var callbacksOnMap = actionMap.m_ActionCallbacks;
            if (listeners.length == 0 && callbacksOnMap.length == 0 && s_GlobalState.onActionChange.length == 0)
                return;

            var context = new InputAction.CallbackContext
            {
                m_State = this,
                m_ActionIndex = actionIndex,
            };

            k_InputActionCallbackMarker.Begin();

            // Global callback goes first.
            var action = context.action;
            if (s_GlobalState.onActionChange.length > 0)
            {
                InputActionChange change;
                switch (phase)
                {
                    case InputActionPhase.Started:
                        change = InputActionChange.ActionStarted;
                        break;
                    case InputActionPhase.Performed:
                        change = InputActionChange.ActionPerformed;
                        break;
                    case InputActionPhase.Canceled:
                        change = InputActionChange.ActionCanceled;
                        break;
                    default:
                        Debug.Assert(false, "Should not reach here");
                        return;
                }

                DelegateHelpers.InvokeCallbacksSafe(ref s_GlobalState.onActionChange, action, change, k_InputOnActionChangeMarker, "InputSystem.onActionChange");
            }

            // Run callbacks (if any) directly on action.
            DelegateHelpers.InvokeCallbacksSafe(ref listeners, context, callbackName, action);

            // Run callbacks (if any) on action map.
            DelegateHelpers.InvokeCallbacksSafe(ref callbacksOnMap, context, callbackName, actionMap);

            k_InputActionCallbackMarker.End();
        }

        private object GetActionOrNoneString(ref TriggerState trigger)
        {
            var action = GetActionOrNull(ref trigger);
            if (action == null)
                return "<none>";
            return action;
        }

        internal InputAction GetActionOrNull(int bindingIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");

            var actionIndex = bindingStates[bindingIndex].actionIndex;
            if (actionIndex == kInvalidIndex)
                return null;

            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount,
                "Action index out of range when getting action");
            var mapIndex = bindingStates[bindingIndex].mapIndex;
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;
            return maps[mapIndex].m_Actions[actionIndex - actionStartIndex];
        }

        internal InputAction GetActionOrNull(ref TriggerState trigger)
        {
            Debug.Assert(trigger.mapIndex >= 0 && trigger.mapIndex < totalMapCount, "Map index out of range");
            Debug.Assert(trigger.bindingIndex >= 0 && trigger.bindingIndex < totalBindingCount, "Binding index out of range");

            var actionIndex = bindingStates[trigger.bindingIndex].actionIndex;
            if (actionIndex == kInvalidIndex)
                return null;

            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Action index out of range");
            var actionStartIndex = mapIndices[trigger.mapIndex].actionStartIndex;
            return maps[trigger.mapIndex].m_Actions[actionIndex - actionStartIndex];
        }

        internal InputControl GetControl(ref TriggerState trigger)
        {
            Debug.Assert(trigger.controlIndex != kInvalidIndex, "Control index is invalid");
            Debug.Assert(trigger.controlIndex >= 0 && trigger.controlIndex < totalControlCount, "Control index out of range");
            return controls[trigger.controlIndex];
        }

        private IInputInteraction GetInteractionOrNull(ref TriggerState trigger)
        {
            if (trigger.interactionIndex == kInvalidIndex)
                return null;

            Debug.Assert(trigger.interactionIndex >= 0 && trigger.interactionIndex < totalInteractionCount, "Interaction index out of range");
            return interactions[trigger.interactionIndex];
        }

        internal int GetBindingIndexInMap(int bindingIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");
            var mapIndex = bindingStates[bindingIndex].mapIndex;
            var bindingStartIndex = mapIndices[mapIndex].bindingStartIndex;
            return bindingIndex - bindingStartIndex;
        }

        internal int GetBindingIndexInState(int mapIndex, int bindingIndexInMap)
        {
            var bindingStartIndex = mapIndices[mapIndex].bindingStartIndex;
            return bindingStartIndex + bindingIndexInMap;
        }

        // Iterators may not use unsafe code so do the detour here.
        internal ref BindingState GetBindingState(int bindingIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");
            return ref bindingStates[bindingIndex];
        }

        internal ref InputBinding GetBinding(int bindingIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");
            var mapIndex = bindingStates[bindingIndex].mapIndex;
            var bindingStartIndex = mapIndices[mapIndex].bindingStartIndex;
            return ref maps[mapIndex].m_Bindings[bindingIndex - bindingStartIndex];
        }

        internal InputActionMap GetActionMap(int bindingIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");
            var mapIndex = bindingStates[bindingIndex].mapIndex;
            return maps[mapIndex];
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "mapIndex", Justification = "Keep this for future implementation")]
        private void ResetInteractionStateAndCancelIfNecessary(int mapIndex, int bindingIndex, int interactionIndex, InputActionPhase phaseAfterCanceled)
        {
            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount, "Interaction index out of range");
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");

            // If interaction is currently driving an action and it has been started or performed,
            // cancel it.
            //
            // NOTE: We could just blindly call ChangePhaseOfInteraction() and it would handle the case of
            //       when the interaction is currently driving the action automatically. However, doing so
            //       would give other interactions a chance to take over which is something we don't want to
            //       happen when resetting actions.
            var actionIndex = bindingStates[bindingIndex].actionIndex;
            if (actionStates[actionIndex].interactionIndex == interactionIndex)
            {
                switch (interactionStates[interactionIndex].phase)
                {
                    case InputActionPhase.Started:
                    case InputActionPhase.Performed:
                        ChangePhaseOfInteraction(InputActionPhase.Canceled, ref actionStates[actionIndex],
                            phaseAfterCanceled: phaseAfterCanceled,
                            processNextInteractionOnCancel: false);
                        break;
                }

                actionStates[actionIndex].interactionIndex = kInvalidIndex;
            }

            ResetInteractionState(interactionIndex);
        }

        private void ResetInteractionState(int interactionIndex)
        {
            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount, "Interaction index out of range");

            // Clean up internal state that the interaction may keep.
            interactions[interactionIndex].Reset();

            // Clean up timer.
            if (interactionStates[interactionIndex].isTimerRunning)
                StopTimeout(interactionIndex);

            // Reset state record.
            interactionStates[interactionIndex] =
                new InteractionState
            {
                // We never set interactions to disabled. This way we don't have to go through them
                // when we disable/enable actions.
                phase = InputActionPhase.Waiting,
                triggerControlIndex = kInvalidIndex
            };
        }

        internal int GetValueSizeInBytes(int bindingIndex, int controlIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index out of range");

            if (bindingStates[bindingIndex].isPartOfComposite) ////TODO: instead, just have compositeOrCompositeBindingIndex be invalid
            {
                var compositeBindingIndex = bindingStates[bindingIndex].compositeOrCompositeBindingIndex;
                var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                var compositeObject = composites[compositeIndex];
                Debug.Assert(compositeObject != null, "Composite object on composite state is null");

                return compositeObject.valueSizeInBytes;
            }

            var control = controls[controlIndex];
            Debug.Assert(control != null, "Control at given index is null");
            return control.valueSizeInBytes;
        }

        internal Type GetValueType(int bindingIndex, int controlIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index out of range");

            if (bindingStates[bindingIndex].isPartOfComposite) ////TODO: instead, just have compositeOrCompositeBindingIndex be invalid
            {
                var compositeBindingIndex = bindingStates[bindingIndex].compositeOrCompositeBindingIndex;
                var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                var compositeObject = composites[compositeIndex];
                Debug.Assert(compositeObject != null, "Composite object is null");

                return compositeObject.valueType;
            }

            var control = controls[controlIndex];
            Debug.Assert(control != null, "Control is null");
            return control.valueType;
        }

        internal static bool IsActuated(ref TriggerState trigger, float threshold = 0)
        {
            var magnitude = trigger.magnitude;
            if (magnitude < 0)
                return true;
            if (Mathf.Approximately(threshold, 0))
                return magnitude > 0;
            return magnitude >= threshold;
        }

        ////REVIEW: we can unify the reading paths once we have blittable type constraints

        internal void ReadValue(int bindingIndex, int controlIndex, void* buffer, int bufferSize, bool ignoreComposites = false)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index out of range");

            InputControl control = null;

            // If the binding that triggered the action is part of a composite, let
            // the composite determine the value we return.
            if (!ignoreComposites && bindingStates[bindingIndex].isPartOfComposite)
            {
                var compositeBindingIndex = bindingStates[bindingIndex].compositeOrCompositeBindingIndex;
                var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                var compositeObject = composites[compositeIndex];
                Debug.Assert(compositeObject != null, "Composite object is null");

                var context = new InputBindingCompositeContext
                {
                    m_State = this,
                    m_BindingIndex = compositeBindingIndex
                };

                compositeObject.ReadValue(ref context, buffer, bufferSize);

                // Switch bindingIndex to that of composite so that we use the right processors.
                bindingIndex = compositeBindingIndex;
            }
            else
            {
                control = controls[controlIndex];
                Debug.Assert(control != null, "Control is null");
                control.ReadValueIntoBuffer(buffer, bufferSize);
            }

            // Run value through processors, if any.
            var processorCount = bindingStates[bindingIndex].processorCount;
            if (processorCount > 0)
            {
                var processorStartIndex = bindingStates[bindingIndex].processorStartIndex;
                for (var i = 0; i < processorCount; ++i)
                    processors[processorStartIndex + i].Process(buffer, bufferSize, control);
            }
        }

        internal TValue ReadValue<TValue>(int bindingIndex, int controlIndex, bool ignoreComposites = false)
            where TValue : struct
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index is out of range");

            var value = default(TValue);

            // In the case of a composite, this will be null.
            InputControl<TValue> controlOfType = null;

            // If the binding that triggered the action is part of a composite, let
            // the composite determine the value we return.
            if (!ignoreComposites && bindingStates[bindingIndex].isPartOfComposite)
            {
                var compositeBindingIndex = bindingStates[bindingIndex].compositeOrCompositeBindingIndex;
                Debug.Assert(compositeBindingIndex >= 0 && compositeBindingIndex < totalBindingCount, "Composite binding index is out of range");
                var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                var compositeObject = composites[compositeIndex];
                Debug.Assert(compositeObject != null, "Composite object is null");

                var context = new InputBindingCompositeContext
                {
                    m_State = this,
                    m_BindingIndex = compositeBindingIndex
                };

                var compositeOfType = compositeObject as InputBindingComposite<TValue>;
                if (compositeOfType == null)
                {
                    // Composite is not derived from InputBindingComposite<TValue>. Do an explicit value
                    // type check here. Might be a composite like OneModifierComposite that dynamically
                    // determines its value type based on what its parts are bound to.
                    var valueType = compositeObject.valueType;
                    if (!valueType.IsAssignableFrom(typeof(TValue)))
                        throw new InvalidOperationException(
                            $"Cannot read value of type '{typeof(TValue).Name}' from composite '{compositeObject}' bound to action '{GetActionOrNull(bindingIndex)}' (composite is a '{compositeIndex.GetType().Name}' with value type '{TypeHelpers.GetNiceTypeName(valueType)}')");

                    compositeObject.ReadValue(ref context, UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<TValue>());
                }
                else
                {
                    value = compositeOfType.ReadValue(ref context);
                }

                // Switch bindingIndex to that of composite so that we use the right processors.
                bindingIndex = compositeBindingIndex;
            }
            else
            {
                if (controlIndex != kInvalidIndex)
                {
                    var control = controls[controlIndex];
                    Debug.Assert(control != null, "Control is null");

                    controlOfType = control as InputControl<TValue>;
                    if (controlOfType == null)
                        throw new InvalidOperationException(
                            $"Cannot read value of type '{TypeHelpers.GetNiceTypeName(typeof(TValue))}' from control '{control.path}' bound to action '{GetActionOrNull(bindingIndex)}' (control is a '{control.GetType().Name}' with value type '{TypeHelpers.GetNiceTypeName(control.valueType)}')");

                    value = controlOfType.value;
                }
            }

            // Run value through processors, if any.
            return ApplyProcessors(bindingIndex, value, controlOfType);
        }

        internal TValue ApplyProcessors<TValue>(int bindingIndex, TValue value, InputControl<TValue> controlOfType = null)
            where TValue : struct
        {
            if (totalBindingCount == 0)
                return value;

            var processorCount = bindingStates[bindingIndex].processorCount;
            if (processorCount > 0)
            {
                var processorStartIndex = bindingStates[bindingIndex].processorStartIndex;
                for (var i = 0; i < processorCount; ++i)
                {
                    if (processors[processorStartIndex + i] is InputProcessor<TValue> processor)
                        value = processor.Process(value, controlOfType);
                }
            }

            return value;
        }

        public float EvaluateCompositePartMagnitude(int bindingIndex, int partNumber)
        {
            var firstChildBindingIndex = bindingIndex + 1;
            var currentMagnitude = float.MinValue;
            for (var index = firstChildBindingIndex; index < totalBindingCount && bindingStates[index].isPartOfComposite; ++index)
            {
                if (bindingStates[index].partIndex != partNumber)
                    continue;

                var controlCount = bindingStates[index].controlCount;
                var controlStartIndex = bindingStates[index].controlStartIndex;
                for (var i = 0; i < controlCount; ++i)
                {
                    var control = controls[controlStartIndex + i];

                    // NOTE: We do *NOT* go to controlMagnitudes here. The reason is we may not yet have received the ProcessControlStateChange
                    //       call for a specific control that is part of the composite and thus controlMagnitudes may not yet have been updated
                    //       for a specific control.
                    currentMagnitude = Mathf.Max(control.magnitude, currentMagnitude);
                }
            }

            return currentMagnitude;
        }

        internal double GetCompositePartPressTime(int bindingIndex, int partNumber)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index is out of range");
            Debug.Assert(bindingStates[bindingIndex].isComposite, "Binding must be a composite");

            var firstChildBindingIndex = bindingIndex + 1;
            var pressTime = double.MaxValue;
            for (var index = firstChildBindingIndex; index < totalBindingCount && bindingStates[index].isPartOfComposite; ++index)
            {
                ref var bindingState = ref bindingStates[index];

                if (bindingState.partIndex != partNumber)
                    continue;

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (bindingState.pressTime != default && bindingState.pressTime < pressTime)
                    pressTime = bindingState.pressTime;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (pressTime == double.MaxValue)
                return -1d;

            return pressTime;
        }

        /// <summary>
        /// Read the value of the given part of a composite binding.
        /// </summary>
        /// <param name="bindingIndex">Index of the composite binding in <see cref="bindingStates"/>.</param>
        /// <param name="partNumber">Index of the part. Note that part indices start at 1!</param>
        /// <typeparam name="TValue">Value type to read. Must correspond to the value of bound controls or an exception will
        /// be thrown.</typeparam>
        /// <returns>Greatest value from among the bound controls for the given part.</returns>
        /// <remarks>
        /// Composites are composed of "parts". Each part has an associated name (e.g. "negative" or "positive") which is
        /// referenced by <see cref="InputBinding.name"/> of bindings that are part of the composite. However, multiple
        /// bindings may reference the same part (e.g. there could be a binding for "W" and another binding for "UpArrow"
        /// and both would reference the "Up" part).
        ///
        /// However, a given composite will only be interested in a single value for any given part. What we do is give
        /// a composite an integer key for every part. When it asks for a value for the given part, we go through all
        /// bindings that reference the given part and return the greatest value from among the controls of all those
        /// bindings.
        ///
        /// <example>
        /// <code>
        /// // Read a float value from the second part of the composite binding at index 3.
        /// ReadCompositePartValue&lt;float&gt;(3, 2);
        /// </code>
        /// </example>
        /// </remarks>
        internal TValue ReadCompositePartValue<TValue, TComparer>(int bindingIndex, int partNumber,
            bool* buttonValuePtr, out int controlIndex, TComparer comparer = default)
            where TValue : struct
            where TComparer : IComparer<TValue>
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index is out of range");
            Debug.Assert(bindingStates[bindingIndex].isComposite, "Binding must be a composite");

            var result = default(TValue);
            var firstChildBindingIndex = bindingIndex + 1;
            var isFirstValue = true;

            controlIndex = kInvalidIndex;

            // Find the binding in the composite that has both the given part number and
            // the greatest value.
            //
            // NOTE: It is tempting to go by control magnitudes instead as those are readily available to us (controlMagnitudes)
            //       and avoids us reading values that we're not going to use. Unfortunately, we can't do that as several controls
            //       used by a composite may all have been updated with a single event (e.g. WASD on a keyboard will usually see
            //       just one update that refreshes the entire state of the keyboard). In that case, one of the controls will
            //       see its state monitor trigger first and in turn trigger processing of the action and composite. Thus only
            //       that one single control would have its value refreshed in controlMagnitudes whereas the other control magnitudes
            //       would be stale.
            for (var index = firstChildBindingIndex; index < totalBindingCount && bindingStates[index].isPartOfComposite; ++index)
            {
                if (bindingStates[index].partIndex != partNumber)
                    continue;

                var controlCount = bindingStates[index].controlCount;
                var controlStartIndex = bindingStates[index].controlStartIndex;
                for (var i = 0; i < controlCount; ++i)
                {
                    var thisControlIndex = controlStartIndex + i;
                    var value = ReadValue<TValue>(index, thisControlIndex, ignoreComposites: true);

                    if (isFirstValue)
                    {
                        result = value;
                        controlIndex = thisControlIndex;
                        isFirstValue = false;
                    }
                    else if (comparer.Compare(value, result) > 0)
                    {
                        result = value;
                        controlIndex = thisControlIndex;
                    }

                    if (buttonValuePtr != null && controlIndex == thisControlIndex)
                    {
                        var control = controls[thisControlIndex];
                        if (control is ButtonControl button)
                        {
                            *buttonValuePtr = button.isPressed;
                        }
                        else if (control is InputControl<float>)
                        {
                            var valuePtr = UnsafeUtility.AddressOf(ref value);
                            *buttonValuePtr = *(float*)valuePtr >= ButtonControl.s_GlobalDefaultButtonPressPoint;
                        }

                        ////REVIEW: Early out here as soon as *any* button is pressed? Technically, the comparer
                        ////        could still select a different control, though...
                    }
                }
            }

            return result;
        }

        internal bool ReadCompositePartValue(int bindingIndex, int partNumber, void* buffer, int bufferSize)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index is out of range");
            Debug.Assert(bindingStates[bindingIndex].isComposite, "Binding must be a composite");

            var firstChildBindingIndex = bindingIndex + 1;

            // Find the binding in the composite that has both the given part number and
            // the greatest amount of actuation.
            var currentMagnitude = float.MinValue;
            for (var index = firstChildBindingIndex; index < totalBindingCount && bindingStates[index].isPartOfComposite; ++index)
            {
                if (bindingStates[index].partIndex != partNumber)
                    continue;

                var controlCount = bindingStates[index].controlCount;
                var controlStartIndex = bindingStates[index].controlStartIndex;
                for (var i = 0; i < controlCount; ++i)
                {
                    var thisControlIndex = controlStartIndex + i;

                    // Check if the control has greater actuation than the most actuated control
                    // we've found so far.
                    //
                    // NOTE: We cannot rely on controlMagnitudes here as several controls used by a composite may all have been updated
                    //       with a single event (e.g. WASD on a keyboard will usually see just one update that refreshes the entire state
                    //       of the keyboard). In that case, one of the controls will see its state monitor trigger first and in turn
                    //       trigger processing of the action and composite. Thus only that one single control would have its value
                    //       refreshed in controlMagnitudes whereas the other control magnitudes would be stale.
                    var control = controls[thisControlIndex];
                    var magnitude = control.magnitude;
                    if (magnitude < currentMagnitude)
                        continue;

                    // If so, read the value.
                    ReadValue(index, thisControlIndex, buffer, bufferSize, ignoreComposites: true);
                    currentMagnitude = magnitude;
                }
            }

            return currentMagnitude > float.MinValue;
        }

        internal object ReadCompositePartValueAsObject(int bindingIndex, int partNumber)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index is out of range");
            Debug.Assert(bindingStates[bindingIndex].isComposite, "Binding must be a composite");

            var firstChildBindingIndex = bindingIndex + 1;

            // Find the binding in the composite that both has the given part number and
            // the greatest amount of actuation.
            var currentMagnitude = float.MinValue;
            object currentValue = null;
            for (var index = firstChildBindingIndex; index < totalBindingCount && bindingStates[index].isPartOfComposite; ++index)
            {
                if (bindingStates[index].partIndex != partNumber)
                    continue;

                var controlCount = bindingStates[index].controlCount;
                var controlStartIndex = bindingStates[index].controlStartIndex;
                for (var i = 0; i < controlCount; ++i)
                {
                    var thisControlIndex = controlStartIndex + i;

                    // Check if the control has greater actuation than the most actuated control
                    // we've found so far.
                    //
                    // NOTE: We cannot rely on controlMagnitudes here as several controls used by a composite may all have been updated
                    //       with a single event (e.g. WASD on a keyboard will usually see just one update that refreshes the entire state
                    //       of the keyboard). In that case, one of the controls will see its state monitor trigger first and in turn
                    //       trigger processing of the action and composite. Thus only that one single control would have its value
                    //       refreshed in controlMagnitudes whereas the other control magnitudes would be stale.
                    var control = controls[thisControlIndex];
                    var magnitude = control.magnitude;
                    if (magnitude < currentMagnitude)
                        continue;

                    // If so, read the value.
                    currentValue = ReadValueAsObject(index, thisControlIndex, ignoreComposites: true);
                    currentMagnitude = magnitude;
                }
            }

            return currentValue;
        }

        internal object ReadValueAsObject(int bindingIndex, int controlIndex, bool ignoreComposites = false)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index is out of range");

            InputControl control = null;
            object value = null;

            // If the binding that triggered the action is part of a composite, let
            // the composite determine the value we return.
            if (!ignoreComposites && bindingStates[bindingIndex].isPartOfComposite) ////TODO: instead, just have compositeOrCompositeBindingIndex be invalid
            {
                var compositeBindingIndex = bindingStates[bindingIndex].compositeOrCompositeBindingIndex;
                Debug.Assert(compositeBindingIndex >= 0 && compositeBindingIndex < totalBindingCount, "Binding index is out of range");
                var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                var compositeObject = composites[compositeIndex];
                Debug.Assert(compositeObject != null, "Composite object is null");

                var context = new InputBindingCompositeContext
                {
                    m_State = this,
                    m_BindingIndex = compositeBindingIndex
                };

                value = compositeObject.ReadValueAsObject(ref context);

                // Switch bindingIndex to that of composite so that we use the right processors.
                bindingIndex = compositeBindingIndex;
            }
            else
            {
                if (controlIndex != kInvalidIndex)
                {
                    control = controls[controlIndex];
                    Debug.Assert(control != null, "Control is null");
                    value = control.ReadValueAsObject();
                }
            }

            if (value != null)
            {
                // Run value through processors, if any.
                var processorCount = bindingStates[bindingIndex].processorCount;
                if (processorCount > 0)
                {
                    var processorStartIndex = bindingStates[bindingIndex].processorStartIndex;
                    for (var i = 0; i < processorCount; ++i)
                        value = processors[processorStartIndex + i].ProcessAsObject(value, control);
                }
            }

            return value;
        }

        internal bool ReadValueAsButton(int bindingIndex, int controlIndex)
        {
            var buttonControl = default(ButtonControl);
            if (!bindingStates[bindingIndex].isPartOfComposite)
                buttonControl = controls[controlIndex] as ButtonControl;

            // Read float value.
            var floatValue = ReadValue<float>(bindingIndex, controlIndex);

            // Compare to press point.
            if (buttonControl != null)
                return floatValue >= buttonControl.pressPointOrDefault;
            return floatValue >= ButtonControl.s_GlobalDefaultButtonPressPoint;
        }

        /// <summary>
        /// Records the current state of a single interaction attached to a binding.
        /// Each interaction keeps track of its own trigger control and phase progression.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 48)]
        internal struct InteractionState
        {
            [FieldOffset(0)] private ushort m_TriggerControlIndex;
            [FieldOffset(2)] private byte m_Phase;
            [FieldOffset(3)] private byte m_Flags;
            [FieldOffset(4)] private float m_TimerDuration;
            [FieldOffset(8)] private double m_StartTime;
            [FieldOffset(16)] private double m_TimerStartTime;
            [FieldOffset(24)] private double m_PerformedTime;
            [FieldOffset(32)] private float m_TotalTimeoutCompletionTimeDone;
            [FieldOffset(36)] private float m_TotalTimeoutCompletionTimeRemaining;
            [FieldOffset(40)] private long m_TimerMonitorIndex;

            public int triggerControlIndex
            {
                get
                {
                    if (m_TriggerControlIndex == ushort.MaxValue)
                        return kInvalidIndex;
                    return m_TriggerControlIndex;
                }
                set
                {
                    if (value == kInvalidIndex)
                        m_TriggerControlIndex = ushort.MaxValue;
                    else
                    {
                        if (value < 0 || value >= ushort.MaxValue)
                            throw new NotSupportedException("More than ushort.MaxValue-1 controls in a single InputActionState");
                        m_TriggerControlIndex = (ushort)value;
                    }
                }
            }

            public double startTime
            {
                get => m_StartTime;
                set => m_StartTime = value;
            }

            public double performedTime
            {
                get => m_PerformedTime;
                set => m_PerformedTime = value;
            }

            public double timerStartTime
            {
                get => m_TimerStartTime;
                set => m_TimerStartTime = value;
            }

            public float timerDuration
            {
                get => m_TimerDuration;
                set => m_TimerDuration = value;
            }

            public float totalTimeoutCompletionDone
            {
                get => m_TotalTimeoutCompletionTimeDone;
                set => m_TotalTimeoutCompletionTimeDone = value;
            }

            public float totalTimeoutCompletionTimeRemaining
            {
                get => m_TotalTimeoutCompletionTimeRemaining;
                set => m_TotalTimeoutCompletionTimeRemaining = value;
            }

            public long timerMonitorIndex
            {
                get => m_TimerMonitorIndex;
                set => m_TimerMonitorIndex = value;
            }

            public bool isTimerRunning
            {
                get => ((Flags)m_Flags & Flags.TimerRunning) == Flags.TimerRunning;
                set
                {
                    if (value)
                        m_Flags |= (byte)Flags.TimerRunning;
                    else
                    {
                        var mask = ~Flags.TimerRunning;
                        m_Flags &= (byte)mask;
                    }
                }
            }

            public InputActionPhase phase
            {
                get => (InputActionPhase)m_Phase;
                set => m_Phase = (byte)value;
            }

            [Flags]
            private enum Flags
            {
                TimerRunning = 1 << 0,
            }
        }

        /// <summary>
        /// Runtime state for a single binding.
        /// </summary>
        /// <remarks>
        /// Correlated to the <see cref="InputBinding"/> it corresponds to by the index in the binding
        /// array.
        /// </remarks>
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        internal struct BindingState
        {
            [FieldOffset(0)] private byte m_ControlCount;
            [FieldOffset(1)] private byte m_InteractionCount;
            [FieldOffset(2)] private byte m_ProcessorCount;
            [FieldOffset(3)] private byte m_MapIndex;
            [FieldOffset(4)] private byte m_Flags;
            [FieldOffset(5)] private byte m_PartIndex;
            [FieldOffset(6)] private ushort m_ActionIndex;
            [FieldOffset(8)] private ushort m_CompositeOrCompositeBindingIndex;
            [FieldOffset(10)] private ushort m_ProcessorStartIndex;
            [FieldOffset(12)] private ushort m_InteractionStartIndex;
            [FieldOffset(14)] private ushort m_ControlStartIndex;
            [FieldOffset(16)] private double m_PressTime;
            [FieldOffset(24)] private int m_TriggerEventIdForComposite;
            [FieldOffset(28)] private int __padding; // m_PressTime double must be aligned

            [Flags]
            public enum Flags
            {
                ChainsWithNext = 1 << 0,
                EndOfChain = 1 << 1,
                Composite = 1 << 2,
                PartOfComposite = 1 << 3,
                InitialStateCheckPending = 1 << 4,
                WantsInitialStateCheck = 1 << 5,
            }

            /// <summary>
            /// Index into <see cref="controls"/> of first control associated with the binding.
            /// </summary>
            /// <remarks>
            /// For composites, this is the index of the first control that is bound by any of the parts in the composite.
            /// </remarks>
            public int controlStartIndex
            {
                get => m_ControlStartIndex;
                set
                {
                    Debug.Assert(value != kInvalidIndex, "Control state index is invalid");
                    if (value >= ushort.MaxValue)
                        throw new NotSupportedException("Total control count in state cannot exceed byte.MaxValue=" + ushort.MaxValue);
                    m_ControlStartIndex = (ushort)value;
                }
            }

            /// <summary>
            /// Number of controls associated with this binding.
            /// </summary>
            /// <remarks>
            /// For composites, this is the total number of controls bound by all parts of the composite combined.
            /// </remarks>
            public int controlCount
            {
                get => m_ControlCount;
                set
                {
                    if (value >= byte.MaxValue)
                        throw new NotSupportedException("Control count per binding cannot exceed byte.MaxValue=" + byte.MaxValue);
                    m_ControlCount = (byte)value;
                }
            }

            /// <summary>
            /// Index into <see cref="InputActionState.interactionStates"/> of first interaction associated with the binding.
            /// </summary>
            public int interactionStartIndex
            {
                get
                {
                    if (m_InteractionStartIndex == ushort.MaxValue)
                        return kInvalidIndex;
                    return m_InteractionStartIndex;
                }
                set
                {
                    if (value == kInvalidIndex)
                        m_InteractionStartIndex = ushort.MaxValue;
                    else
                    {
                        if (value >= ushort.MaxValue)
                            throw new NotSupportedException("Interaction count cannot exceed ushort.MaxValue=" + ushort.MaxValue);
                        m_InteractionStartIndex = (ushort)value;
                    }
                }
            }

            /// <summary>
            /// Number of interactions associated with this binding.
            /// </summary>
            public int interactionCount
            {
                get => m_InteractionCount;
                set
                {
                    if (value >= byte.MaxValue)
                        throw new NotSupportedException("Interaction count per binding cannot exceed byte.MaxValue=" + byte.MaxValue);
                    m_InteractionCount = (byte)value;
                }
            }

            public int processorStartIndex
            {
                get
                {
                    if (m_ProcessorStartIndex == ushort.MaxValue)
                        return kInvalidIndex;
                    return m_ProcessorStartIndex;
                }
                set
                {
                    if (value == kInvalidIndex)
                        m_ProcessorStartIndex = ushort.MaxValue;
                    else
                    {
                        if (value >= ushort.MaxValue)
                            throw new NotSupportedException("Processor count cannot exceed ushort.MaxValue=" + ushort.MaxValue);
                        m_ProcessorStartIndex = (ushort)value;
                    }
                }
            }

            public int processorCount
            {
                get => m_ProcessorCount;
                set
                {
                    if (value >= byte.MaxValue)
                        throw new NotSupportedException("Processor count per binding cannot exceed byte.MaxValue=" + byte.MaxValue);
                    m_ProcessorCount = (byte)value;
                }
            }

            /// <summary>
            /// Index of the action being triggered by the binding (if any).
            /// </summary>
            /// <remarks>
            /// For bindings that don't trigger actions, this is <see cref="kInvalidIndex"/>.
            ///
            /// For bindings that are part of a composite, we force this to be the action set on the composite itself.
            /// </remarks>
            public int actionIndex
            {
                get
                {
                    if (m_ActionIndex == ushort.MaxValue)
                        return kInvalidIndex;
                    return m_ActionIndex;
                }
                set
                {
                    if (value == kInvalidIndex)
                        m_ActionIndex = ushort.MaxValue;
                    else
                    {
                        if (value >= ushort.MaxValue)
                            throw new NotSupportedException("Action count cannot exceed ushort.MaxValue=" + ushort.MaxValue);
                        m_ActionIndex = (ushort)value;
                    }
                }
            }

            public int mapIndex
            {
                get => m_MapIndex;
                set
                {
                    Debug.Assert(value != kInvalidIndex, "Map index is invalid");
                    if (value >= byte.MaxValue)
                        throw new NotSupportedException("Map count cannot exceed byte.MaxValue=" + byte.MaxValue);
                    m_MapIndex = (byte)value;
                }
            }

            /// <summary>
            /// If this is a composite binding, this is the index of the composite in <see cref="composites"/>.
            /// If the binding is part of a composite, this is the index of the binding that is the composite.
            /// If the binding is neither a composite nor part of a composite, this is <see cref="kInvalidIndex"/>.
            /// </summary>
            public int compositeOrCompositeBindingIndex
            {
                get
                {
                    if (m_CompositeOrCompositeBindingIndex == ushort.MaxValue)
                        return kInvalidIndex;
                    return m_CompositeOrCompositeBindingIndex;
                }
                set
                {
                    if (value == kInvalidIndex)
                        m_CompositeOrCompositeBindingIndex = ushort.MaxValue;
                    else
                    {
                        if (value >= ushort.MaxValue)
                            throw new NotSupportedException("Composite count cannot exceed ushort.MaxValue=" + ushort.MaxValue);
                        m_CompositeOrCompositeBindingIndex = (ushort)value;
                    }
                }
            }

            /// <summary>
            /// <see cref="InputEvent.eventId">ID</see> of the event that last triggered the binding.
            /// </summary>
            /// <remarks>
            /// We only store this for composites ATM.
            /// </remarks>
            public int triggerEventIdForComposite
            {
                get => m_TriggerEventIdForComposite;
                set => m_TriggerEventIdForComposite = value;
            }

            // For now, we only record this for part bindings!
            public double pressTime
            {
                get => m_PressTime;
                set => m_PressTime = value;
            }

            public Flags flags
            {
                get => (Flags)m_Flags;
                set => m_Flags = (byte)value;
            }

            public bool chainsWithNext
            {
                get => (flags & Flags.ChainsWithNext) == Flags.ChainsWithNext;
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
                get => (flags & Flags.EndOfChain) == Flags.EndOfChain;
                set
                {
                    if (value)
                        flags |= Flags.EndOfChain;
                    else
                        flags &= ~Flags.EndOfChain;
                }
            }

            public bool isPartOfChain => chainsWithNext || isEndOfChain;

            public bool isComposite
            {
                get => (flags & Flags.Composite) == Flags.Composite;
                set
                {
                    if (value)
                        flags |= Flags.Composite;
                    else
                        flags &= ~Flags.Composite;
                }
            }

            public bool isPartOfComposite
            {
                get => (flags & Flags.PartOfComposite) == Flags.PartOfComposite;
                set
                {
                    if (value)
                        flags |= Flags.PartOfComposite;
                    else
                        flags &= ~Flags.PartOfComposite;
                }
            }

            public bool initialStateCheckPending
            {
                get => (flags & Flags.InitialStateCheckPending) != 0;
                set
                {
                    if (value)
                        flags |= Flags.InitialStateCheckPending;
                    else
                        flags &= ~Flags.InitialStateCheckPending;
                }
            }

            public bool wantsInitialStateCheck
            {
                get => (flags & Flags.WantsInitialStateCheck) != 0;
                set
                {
                    if (value)
                        flags |= Flags.WantsInitialStateCheck;
                    else
                        flags &= ~Flags.WantsInitialStateCheck;
                }
            }

            public int partIndex
            {
                get => m_PartIndex;
                set
                {
                    if (partIndex < 0)
                        throw new ArgumentOutOfRangeException(nameof(value), "Part index must not be negative");
                    if (partIndex > byte.MaxValue)
                        throw new InvalidOperationException("Part count must not exceed byte.MaxValue=" + byte.MaxValue);
                    m_PartIndex = (byte)value;
                }
            }
        }

        /// <summary>
        /// Record of an input control change and its related data.
        /// </summary>
        /// <remarks>
        /// This serves a dual purpose. One is, trigger states represent control actuations while we process them. The
        /// other is to represent the current actuation state of an action as a whole. The latter is stored in <see cref="actionStates"/>
        /// while the former is passed around as temporary instances on the stack.
        /// </remarks>
        [StructLayout(LayoutKind.Explicit, Size = 56)]
        public struct TriggerState
        {
            public const int kMaxNumMaps = byte.MaxValue;
            public const int kMaxNumControls = ushort.MaxValue;
            public const int kMaxNumBindings = ushort.MaxValue;

            [FieldOffset(0)] private byte m_Phase;
            [FieldOffset(1)] private byte m_Flags;
            [FieldOffset(2)] private byte m_MapIndex;
            // One byte available here.
            [FieldOffset(4)] private ushort m_ControlIndex;
            // Two bytes available here.
            ////REVIEW: can we condense these to floats? would save us a whopping 8 bytes
            [FieldOffset(8)] private double m_Time;
            [FieldOffset(16)] private double m_StartTime;
            [FieldOffset(24)] private ushort m_BindingIndex;
            [FieldOffset(26)] private ushort m_InteractionIndex;
            [FieldOffset(28)] private float m_Magnitude;
            [FieldOffset(32)] private uint m_LastPerformedInUpdate;
            [FieldOffset(36)] private uint m_LastCanceledInUpdate;
            [FieldOffset(40)] private uint m_PressedInUpdate;
            [FieldOffset(44)] private uint m_ReleasedInUpdate;
            [FieldOffset(48)] private uint m_LastCompletedInUpdate;
            [FieldOffset(52)] internal int framePerformed;
            [FieldOffset(56)] internal int framePressed;
            [FieldOffset(60)] internal int frameReleased;
            [FieldOffset(64)] internal int frameCompleted;

            /// <summary>
            /// Phase being triggered by the control value change.
            /// </summary>
            public InputActionPhase phase
            {
                get => (InputActionPhase)m_Phase;
                set => m_Phase = (byte)value;
            }

            public bool isDisabled => phase == InputActionPhase.Disabled;
            public bool isWaiting => phase == InputActionPhase.Waiting;
            public bool isStarted => phase == InputActionPhase.Started;
            public bool isPerformed => phase == InputActionPhase.Performed;
            public bool isCanceled => phase == InputActionPhase.Canceled;

            /// <summary>
            /// The time the binding got triggered.
            /// </summary>
            public double time
            {
                get => m_Time;
                set => m_Time = value;
            }

            /// <summary>
            /// The time when the binding moved into <see cref="InputActionPhase.Started"/>.
            /// </summary>
            public double startTime
            {
                get => m_StartTime;
                set => m_StartTime = value;
            }

            /// <summary>
            /// Amount of actuation on the control.
            /// </summary>
            /// <remarks>
            /// This is only valid if <see cref="haveMagnitude"/> is true.
            ///
            /// Note that this may differ from the actuation stored for <see cref="controlIndex"/> in <see
            /// cref="UnmanagedMemory.controlMagnitudes"/> if the binding is a composite.
            /// </remarks>
            public float magnitude
            {
                get => m_Magnitude;
                set
                {
                    flags |= Flags.HaveMagnitude;
                    m_Magnitude = value;
                }
            }

            /// <summary>
            /// Whether <see cref="magnitude"/> has been set.
            /// </summary>
            /// <remarks>
            /// Magnitude computation is expensive so we only want to do it once. Also, we sometimes need to compare
            /// a current magnitude to a magnitude value from a previous frame and the magnitude of the control
            /// may have already changed.
            /// </remarks>
            public bool haveMagnitude => (flags & Flags.HaveMagnitude) != 0;

            /// <summary>
            /// Index of the action map in <see cref="maps"/> that contains the binding that triggered.
            /// </summary>
            public int mapIndex
            {
                get => m_MapIndex;
                set
                {
                    if (value < 0 || value > kMaxNumMaps)
                        throw new NotSupportedException("More than byte.MaxValue InputActionMaps in a single InputActionState");
                    m_MapIndex = (byte)value;
                }
            }

            /// <summary>
            /// Index of the control currently driving the action or <see cref="kInvalidIndex"/> if none.
            /// </summary>
            public int controlIndex
            {
                get
                {
                    if (m_ControlIndex == kMaxNumControls)
                        return kInvalidIndex;
                    return m_ControlIndex;
                }
                set
                {
                    if (value == kInvalidIndex)
                        m_ControlIndex = ushort.MaxValue;
                    else
                    {
                        if (value < 0 || value >= kMaxNumControls)
                            throw new NotSupportedException("More than ushort.MaxValue-1 controls in a single InputActionState");
                        m_ControlIndex = (ushort)value;
                    }
                }
            }

            /// <summary>
            /// Index into <see cref="bindingStates"/> for the binding that triggered.
            /// </summary>
            /// <remarks>
            /// This corresponds 1:1 to an <see cref="InputBinding"/>.
            /// </remarks>
            public int bindingIndex
            {
                get => m_BindingIndex;
                set
                {
                    if (value < 0 || value > kMaxNumBindings)
                        throw new NotSupportedException("More than ushort.MaxValue bindings in a single InputActionState");
                    m_BindingIndex = (ushort)value;
                }
            }

            /// <summary>
            /// Index into <see cref="InputActionState.interactionStates"/> for the interaction that triggered.
            /// </summary>
            /// <remarks>
            /// Is <see cref="InputActionState.kInvalidIndex"/> if there is no interaction present on the binding.
            /// </remarks>
            public int interactionIndex
            {
                get
                {
                    if (m_InteractionIndex == ushort.MaxValue)
                        return kInvalidIndex;
                    return m_InteractionIndex;
                }
                set
                {
                    if (value == kInvalidIndex)
                        m_InteractionIndex = ushort.MaxValue;
                    else
                    {
                        if (value < 0 || value >= ushort.MaxValue)
                            throw new NotSupportedException("More than ushort.MaxValue-1 interactions in a single InputActionState");
                        m_InteractionIndex = (ushort)value;
                    }
                }
            }

            /// <summary>
            /// Update step count (<see cref="InputUpdate.s_UpdateStepCount"/>) in which action triggered/performed last.
            /// Zero if the action did not trigger yet. Also reset to zero when the action is hard reset.
            /// </summary>
            public uint lastPerformedInUpdate
            {
                get => m_LastPerformedInUpdate;
                set => m_LastPerformedInUpdate = value;
            }

            /// <summary>
            /// Update step count (<see cref="InputUpdate.s_UpdateStepCount"/>) in which action completed last.
            /// Zero if the action did not become completed yet. Also reset to zero when the action is hard reset.
            /// </summary>
            public uint lastCompletedInUpdate
            {
                get => m_LastCompletedInUpdate;
                set => m_LastCompletedInUpdate = value;
            }

            public uint lastCanceledInUpdate
            {
                get => m_LastCanceledInUpdate;
                set => m_LastCanceledInUpdate = value;
            }

            public uint pressedInUpdate
            {
                get => m_PressedInUpdate;
                set => m_PressedInUpdate = value;
            }

            public uint releasedInUpdate
            {
                get => m_ReleasedInUpdate;
                set => m_ReleasedInUpdate = value;
            }

            /// <summary>
            /// Whether the action associated with the trigger state is marked as pass-through.
            /// </summary>
            /// <seealso cref="InputActionType.PassThrough"/>
            public bool isPassThrough
            {
                get => (flags & Flags.PassThrough) != 0;
                set
                {
                    if (value)
                        flags |= Flags.PassThrough;
                    else
                        flags &= ~Flags.PassThrough;
                }
            }

            /// <summary>
            /// Whether the action associated with the trigger state is a button-type action.
            /// </summary>
            /// <seealso cref="InputActionType.Button"/>
            public bool isButton
            {
                get => (flags & Flags.Button) != 0;
                set
                {
                    if (value)
                        flags |= Flags.Button;
                    else
                        flags &= ~Flags.Button;
                }
            }

            public bool isPressed
            {
                get => (flags & Flags.Pressed) != 0;
                set
                {
                    if (value)
                        flags |= Flags.Pressed;
                    else
                        flags &= ~Flags.Pressed;
                }
            }

            /// <summary>
            /// Whether the action may potentially see multiple concurrent actuations from its bindings
            /// and wants them resolved automatically.
            /// </summary>
            /// <remarks>
            /// We use this to gate some of the more expensive checks that are pointless to
            /// perform if we don't have to disambiguate input from concurrent sources.
            ///
            /// Always disabled if <see cref="isPassThrough"/> is true.
            /// </remarks>
            public bool mayNeedConflictResolution
            {
                get => (flags & Flags.MayNeedConflictResolution) != 0;
                set
                {
                    if (value)
                        flags |= Flags.MayNeedConflictResolution;
                    else
                        flags &= ~Flags.MayNeedConflictResolution;
                }
            }

            /// <summary>
            /// Whether the action currently has several concurrent actuations from its bindings.
            /// </summary>
            /// <remarks>
            /// This is only used when automatic conflict resolution is enabled (<see cref="mayNeedConflictResolution"/>).
            /// </remarks>
            public bool hasMultipleConcurrentActuations
            {
                get => (flags & Flags.HasMultipleConcurrentActuations) != 0;
                set
                {
                    if (value)
                        flags |= Flags.HasMultipleConcurrentActuations;
                    else
                        flags &= ~Flags.HasMultipleConcurrentActuations;
                }
            }

            public bool inProcessing
            {
                get => (flags & Flags.InProcessing) != 0;
                set
                {
                    if (value)
                        flags |= Flags.InProcessing;
                    else
                        flags &= ~Flags.InProcessing;
                }
            }

            public Flags flags
            {
                get => (Flags)m_Flags;
                set => m_Flags = (byte)value;
            }

            [Flags]
            public enum Flags
            {
                /// <summary>
                /// Whether <see cref="magnitude"/> has been set.
                /// </summary>
                HaveMagnitude = 1 << 0,

                /// <summary>
                /// Whether the action associated with the trigger state is marked as pass-through.
                /// </summary>
                /// <seealso cref="InputActionType.PassThrough"/>
                PassThrough = 1 << 1,

                /// <summary>
                /// Whether the action has more than one control bound to it.
                /// </summary>
                /// <remarks>
                /// An action may have arbitrary many bindings yet may still resolve only to a single control
                /// at runtime. In that case, this flag is NOT set. We only set it if binding resolution for
                /// an action indeed ended up with multiple controls able to trigger the same action.
                /// </remarks>
                MayNeedConflictResolution = 1 << 2,

                /// <summary>
                /// Whether there are currently multiple bound controls that are actuated.
                /// </summary>
                /// <remarks>
                /// This is only used if <see cref="TriggerState.mayNeedConflictResolution"/> is true.
                /// </remarks>
                HasMultipleConcurrentActuations = 1 << 3,

                InProcessing = 1 << 4,

                /// <summary>
                /// Whether the action associated with the trigger state is a button-type action.
                /// </summary>
                /// <seealso cref="InputActionType.Button"/>
                Button = 1 << 5,

                Pressed = 1 << 6,
            }
        }

        /// <summary>
        /// Tells us where the data for a single action map is found in the
        /// various arrays.
        /// </summary>
        public struct ActionMapIndices
        {
            public int actionStartIndex;
            public int actionCount;
            public int controlStartIndex;
            public int controlCount;
            public int bindingStartIndex;
            public int bindingCount;
            public int interactionStartIndex;
            public int interactionCount;
            public int processorStartIndex;
            public int processorCount;
            public int compositeStartIndex;
            public int compositeCount;
        }

        /// <summary>
        /// Unmanaged memory kept for action maps.
        /// </summary>
        /// <remarks>
        /// Most of the dynamic execution state for actions we keep in a single block of unmanaged memory.
        /// Essentially, only the C# heap objects (like IInputInteraction and such) we keep in managed arrays.
        /// Aside from being able to condense the data into a single block of memory and not having to have
        /// it spread out on the GC heap, we gain the advantage of being able to freely allocate and re-allocate
        /// these blocks without creating garbage on the GC heap.
        ///
        /// The data here is set up by <see cref="InputBindingResolver"/>.
        /// </remarks>
        public struct UnmanagedMemory : IDisposable
        {
            public bool isAllocated => basePtr != null;

            public void* basePtr;

            /// <summary>
            /// Number of action maps and entries in <see cref="mapIndices"/> and <see cref="maps"/>.
            /// </summary>
            public int mapCount;

            /// <summary>
            /// Total number of actions (i.e. from all maps combined) and entries in <see cref="actionStates"/>.
            /// </summary>
            public int actionCount;

            /// <summary>
            /// Total number of interactions and entries in <see cref="interactionStates"/> and <see cref="interactions"/>.
            /// </summary>
            public int interactionCount;

            /// <summary>
            /// Total number of bindings and entries in <see cref="bindingStates"/>.
            /// </summary>
            public int bindingCount;

            /// <summary>
            /// Total number of bound controls and entries in <see cref="controls"/>.
            /// </summary>
            public int controlCount;

            /// <summary>
            /// Total number of composite bindings and entries in <see cref="composites"/>.
            /// </summary>
            public int compositeCount;

            /// <summary>
            /// Total size of allocated unmanaged memory.
            /// </summary>
            public int sizeInBytes =>
                mapCount * sizeof(ActionMapIndices) + // mapIndices
                actionCount * sizeof(TriggerState) + // actionStates
                bindingCount * sizeof(BindingState) + // bindingStates
                interactionCount * sizeof(InteractionState) + // interactionStates
                controlCount * sizeof(float) + // controlMagnitudes
                compositeCount * sizeof(float) + // compositeMagnitudes
                controlCount * sizeof(int) + // controlIndexToBindingIndex
                controlCount * sizeof(ushort) * 2 + // controlGrouping
                actionCount * sizeof(ushort) * 2 + // actionBindingIndicesAndCounts
                bindingCount * sizeof(ushort) + // actionBindingIndices
                (controlCount + 31) / 32 * sizeof(int); // enabledControlsArray

            /// <summary>
            /// Trigger state of all actions added to the state.
            /// </summary>
            /// <remarks>
            /// This array also tells which actions are enabled or disabled. Any action with phase
            /// <see cref="InputActionPhase.Disabled"/> is disabled.
            /// </remarks>
            public TriggerState* actionStates;

            /// <summary>
            /// State of all bindings added to the state.
            /// </summary>
            /// <remarks>
            /// For the most part, this is read-only information set up during resolution.
            /// </remarks>
            public BindingState* bindingStates;

            /// <summary>
            /// State of all interactions on bindings in the action map.
            /// </summary>
            /// <remarks>
            /// Any interaction mentioned on any of the bindings gets its own execution state record
            /// in here. The interactions for any one binding are grouped together.
            /// </remarks>
            public InteractionState* interactionStates;

            /// <summary>
            /// Current remembered level of actuation of each of the controls in <see cref="controls"/>.
            /// </summary>
            /// <remarks>
            /// This array is NOT kept strictly up to date. In fact, we only use it for conflict resolution
            /// between multiple bound controls at the moment. Meaning that in the majority of cases, the magnitude
            /// stored for a control here will NOT be up to date.
            ///
            /// Also note that for controls that are part of composites, this will NOT be the magnitude of the
            /// control but rather be the magnitude of the entire compound.
            /// </remarks>
            public float* controlMagnitudes;

            public float* compositeMagnitudes;

            public int* enabledControls;

            /// <summary>
            /// Array of pair of ints, one pair for each action (same index as <see cref="actionStates"/>). First int
            /// is the index into <see cref="actionBindingIndices"/> where bindings of action are found and second int
            /// is the count of bindings on action.
            /// </summary>
            public ushort* actionBindingIndicesAndCounts;

            /// <summary>
            /// Array of indices into <see cref="bindingStates"/>. The indices for every action are laid out sequentially.
            /// The array slice corresponding to each action can be determined by looking it up in <see cref="actionBindingIndicesAndCounts"/>.
            /// </summary>
            public ushort* actionBindingIndices;

            ////REVIEW: make this an array of shorts rather than ints?
            public int* controlIndexToBindingIndex;

            // Two shorts per control. First one is group number. Second one is complexity count.
            public ushort* controlGroupingAndComplexity;
            public bool controlGroupingInitialized;

            public ActionMapIndices* mapIndices;

            private static byte* AllocFromBlob(ref byte* top, int size)
            {
                if (size == 0)
                    return null;

                var allocation = top;
                top += size;
                return allocation;
            }

            public void Allocate(int mapCount, int actionCount, int bindingCount, int controlCount, int interactionCount, int compositeCount)
            {
                Debug.Assert(basePtr == null, "Memory already allocated! Free first!");
                Debug.Assert(mapCount >= 1, "Map count out of range");
                Debug.Assert(actionCount >= 0, "Action count out of range");
                Debug.Assert(bindingCount >= 0, "Binding count out of range");
                Debug.Assert(interactionCount >= 0, "Interaction count out of range");
                Debug.Assert(compositeCount >= 0, "Composite count out of range");

                this.mapCount = mapCount;
                this.actionCount = actionCount;
                this.interactionCount = interactionCount;
                this.bindingCount = bindingCount;
                this.controlCount = controlCount;
                this.compositeCount = compositeCount;

                var numBytes = sizeInBytes;
                var ptr = (byte*)UnsafeUtility.Malloc(numBytes, 8, Allocator.Persistent);
                UnsafeUtility.MemClear(ptr, numBytes);

                basePtr = ptr;

                // NOTE: This depends on the individual structs being sufficiently aligned in order to not
                //       cause any misalignment here. TriggerState, InteractionState, and BindingState all
                //       contain doubles so put them first in memory to make sure they get proper alignment.
                actionStates = (TriggerState*)AllocFromBlob(ref ptr, actionCount * sizeof(TriggerState));
                interactionStates = (InteractionState*)AllocFromBlob(ref ptr, interactionCount * sizeof(InteractionState));
                bindingStates = (BindingState*)AllocFromBlob(ref ptr, bindingCount * sizeof(BindingState));
                mapIndices = (ActionMapIndices*)AllocFromBlob(ref ptr, mapCount * sizeof(ActionMapIndices));
                controlMagnitudes = (float*)AllocFromBlob(ref ptr, controlCount * sizeof(float));
                compositeMagnitudes = (float*)AllocFromBlob(ref ptr, compositeCount * sizeof(float));
                controlIndexToBindingIndex = (int*)AllocFromBlob(ref ptr, controlCount * sizeof(int));
                controlGroupingAndComplexity = (ushort*)AllocFromBlob(ref ptr, controlCount * sizeof(ushort) * 2);
                actionBindingIndicesAndCounts = (ushort*)AllocFromBlob(ref ptr, actionCount * sizeof(ushort) * 2);
                actionBindingIndices = (ushort*)AllocFromBlob(ref ptr, bindingCount * sizeof(ushort));
                enabledControls = (int*)AllocFromBlob(ref ptr, (controlCount + 31) / 32 * sizeof(int));
            }

            public void Dispose()
            {
                if (basePtr == null)
                    return;

                UnsafeUtility.Free(basePtr, Allocator.Persistent);

                basePtr = null;
                actionStates = null;
                interactionStates = null;
                bindingStates = null;
                mapIndices = null;
                controlMagnitudes = null;
                compositeMagnitudes = null;
                controlIndexToBindingIndex = null;
                controlGroupingAndComplexity = null;
                actionBindingIndices = null;
                actionBindingIndicesAndCounts = null;

                mapCount = 0;
                actionCount = 0;
                bindingCount = 0;
                controlCount = 0;
                interactionCount = 0;
                compositeCount = 0;
            }

            public void CopyDataFrom(UnmanagedMemory memory)
            {
                Debug.Assert(memory.basePtr != null, "Given struct has no allocated data");

                // Even if a certain array is empty (e.g. we have no controls), we set the pointer
                // in Allocate() to something other than null.

                UnsafeUtility.MemCpy(mapIndices, memory.mapIndices, memory.mapCount * sizeof(ActionMapIndices));
                UnsafeUtility.MemCpy(actionStates, memory.actionStates, memory.actionCount * sizeof(TriggerState));
                UnsafeUtility.MemCpy(bindingStates, memory.bindingStates, memory.bindingCount * sizeof(BindingState));
                UnsafeUtility.MemCpy(interactionStates, memory.interactionStates, memory.interactionCount * sizeof(InteractionState));
                UnsafeUtility.MemCpy(controlMagnitudes, memory.controlMagnitudes, memory.controlCount * sizeof(float));
                UnsafeUtility.MemCpy(compositeMagnitudes, memory.compositeMagnitudes, memory.compositeCount * sizeof(float));
                UnsafeUtility.MemCpy(controlIndexToBindingIndex, memory.controlIndexToBindingIndex, memory.controlCount * sizeof(int));
                UnsafeUtility.MemCpy(controlGroupingAndComplexity, memory.controlGroupingAndComplexity, memory.controlCount * sizeof(ushort) * 2);
                UnsafeUtility.MemCpy(actionBindingIndicesAndCounts, memory.actionBindingIndicesAndCounts, memory.actionCount * sizeof(ushort) * 2);
                UnsafeUtility.MemCpy(actionBindingIndices, memory.actionBindingIndices, memory.bindingCount * sizeof(ushort));
                UnsafeUtility.MemCpy(enabledControls, memory.enabledControls, (memory.controlCount + 31) / 32 * sizeof(int));
            }

            public UnmanagedMemory Clone()
            {
                if (!isAllocated)
                    return new UnmanagedMemory();

                var clone = new UnmanagedMemory();
                clone.Allocate(
                    mapCount: mapCount,
                    actionCount: actionCount,
                    controlCount: controlCount,
                    bindingCount: bindingCount,
                    interactionCount: interactionCount,
                    compositeCount: compositeCount);
                clone.CopyDataFrom(this);

                return clone;
            }
        }

        #region Global State

        /// <summary>
        /// Global state containing a list of weak references to all action map states currently in the system.
        /// </summary>
        /// <remarks>
        /// When the control setup in the system changes, we need a way for control resolution that
        /// has already been done to be invalidated and redone. We also want a way to find all
        /// currently enabled actions in the system.
        ///
        /// Both of these needs are served by this global list.
        /// </remarks>
        internal struct GlobalState
        {
            internal InlinedArray<GCHandle> globalList;
            internal CallbackArray<Action<object, InputActionChange>> onActionChange;
            internal CallbackArray<Action<object>> onActionControlsChanged;
        }

        internal static GlobalState s_GlobalState;

        internal static ISavedState SaveAndResetState()
        {
            // Save current state
            var savedState = new SavedStructState<GlobalState>(
                ref s_GlobalState,
                (ref GlobalState state) => s_GlobalState = state, // restore
                () => ResetGlobals()); // static dispose

            // Reset global state
            s_GlobalState = default;

            return savedState;
        }

        private void AddToGlobalList()
        {
            CompactGlobalList();
            var handle = GCHandle.Alloc(this, GCHandleType.Weak);
            s_GlobalState.globalList.AppendWithCapacity(handle);
        }

        private void RemoveMapFromGlobalList()
        {
            var count = s_GlobalState.globalList.length;
            for (var i = 0; i < count; ++i)
                if (s_GlobalState.globalList[i].Target == this)
                {
                    s_GlobalState.globalList[i].Free();
                    s_GlobalState.globalList.RemoveAtByMovingTailWithCapacity(i);
                    break;
                }
        }

        /// <summary>
        /// Remove any entries for states that have been reclaimed by GC.
        /// </summary>
        private static void CompactGlobalList()
        {
            var length = s_GlobalState.globalList.length;
            var head = 0;
            for (var i = 0; i < length; ++i)
            {
                var handle = s_GlobalState.globalList[i];
                if (handle.IsAllocated && handle.Target != null)
                {
                    if (head != i)
                        s_GlobalState.globalList[head] = handle;
                    ++head;
                }
                else
                {
                    if (handle.IsAllocated)
                        s_GlobalState.globalList[i].Free();
                    s_GlobalState.globalList[i] = default;
                }
            }
            s_GlobalState.globalList.length = head;
        }

        internal void NotifyListenersOfActionChange(InputActionChange change)
        {
            for (var i = 0; i < totalMapCount; ++i)
            {
                var map = maps[i];
                if (map.m_SingletonAction != null)
                {
                    NotifyListenersOfActionChange(change, map.m_SingletonAction);
                }
                else if (map.m_Asset == null)
                {
                    NotifyListenersOfActionChange(change, map);
                }
                else
                {
                    NotifyListenersOfActionChange(change, map.m_Asset);
                    return;
                }
            }
        }

        internal static void NotifyListenersOfActionChange(InputActionChange change, object actionOrMapOrAsset)
        {
            Debug.Assert(actionOrMapOrAsset != null, "Should have action or action map or asset object to notify about");
            Debug.Assert(actionOrMapOrAsset is InputAction || (actionOrMapOrAsset as InputActionMap)?.m_SingletonAction == null,
                "Must not send notifications for changes made to hidden action maps of singleton actions");

            DelegateHelpers.InvokeCallbacksSafe(ref s_GlobalState.onActionChange, actionOrMapOrAsset, change, k_InputOnActionChangeMarker, "InputSystem.onActionChange");
            if (change == InputActionChange.BoundControlsChanged)
                DelegateHelpers.InvokeCallbacksSafe(ref s_GlobalState.onActionControlsChanged, actionOrMapOrAsset, "onActionControlsChange");
        }

        /// <summary>
        /// Nuke global state we have to keep track of action map states.
        /// </summary>
        private static void ResetGlobals()
        {
            DestroyAllActionMapStates();
            for (var i = 0; i < s_GlobalState.globalList.length; ++i)
                if (s_GlobalState.globalList[i].IsAllocated)
                    s_GlobalState.globalList[i].Free();
            s_GlobalState.globalList.length = 0;
            s_GlobalState.onActionChange.Clear();
            s_GlobalState.onActionControlsChanged.Clear();
        }

        // Walk all maps with enabled actions and add all enabled actions to the given list.
        internal static int FindAllEnabledActions(List<InputAction> result)
        {
            var numFound = 0;
            var stateCount = s_GlobalState.globalList.length;
            for (var i = 0; i < stateCount; ++i)
            {
                var handle = s_GlobalState.globalList[i];
                if (!handle.IsAllocated)
                    continue;
                var state = (InputActionState)handle.Target;
                if (state == null)
                    continue;

                var mapCount = state.totalMapCount;
                var maps = state.maps;
                for (var n = 0; n < mapCount; ++n)
                {
                    var map = maps[n];
                    if (!map.enabled)
                        continue;

                    var actions = map.m_Actions;
                    var actionCount = actions.Length;
                    if (map.m_EnabledActionsCount == actionCount)
                    {
                        result.AddRange(actions);
                        numFound += actionCount;
                    }
                    else
                    {
                        var actionStartIndex = state.mapIndices[map.m_MapIndexInState].actionStartIndex;
                        for (var k = 0; k < actionCount; ++k)
                        {
                            if (state.actionStates[actionStartIndex + k].phase != InputActionPhase.Disabled)
                            {
                                result.Add(actions[k]);
                                ++numFound;
                            }
                        }
                    }
                }
            }

            return numFound;
        }

        ////TODO: when re-resolving, we need to preserve InteractionStates and not just reset them

        /// <summary>
        /// Deal with the fact that the control setup in the system may change at any time and can affect
        /// actions that had their controls already resolved.
        /// </summary>
        /// <remarks>
        /// Note that this method can NOT deal with changes other than the control setup in the system
        /// changing. Specifically, it will NOT handle configuration changes in action maps (e.g. bindings
        /// being altered) correctly.
        ///
        /// We get called from <see cref="InputManager"/> directly rather than hooking into <see cref="InputSystem.onDeviceChange"/>
        /// so that we're not adding needless calls for device changes that are not of interest to us.
        /// </remarks>
        internal static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            Debug.Assert(device != null, "Device is null");
            ////REVIEW: should we ignore disconnected devices in InputBindingResolver?
            Debug.Assert(
                change == InputDeviceChange.Added || change == InputDeviceChange.Removed ||
                change == InputDeviceChange.UsageChanged || change == InputDeviceChange.ConfigurationChanged ||
                change == InputDeviceChange.SoftReset || change == InputDeviceChange.HardReset,
                "Should only be called for relevant changes");

            for (var i = 0; i < s_GlobalState.globalList.length; ++i)
            {
                var handle = s_GlobalState.globalList[i];
                if (!handle.IsAllocated || handle.Target == null)
                {
                    // Stale entry in the list. State has already been reclaimed by GC. Remove it.
                    if (handle.IsAllocated)
                        s_GlobalState.globalList[i].Free();
                    s_GlobalState.globalList.RemoveAtWithCapacity(i);
                    --i;
                    continue;
                }
                var state = (InputActionState)handle.Target;

                // If this state is not affected by the change, skip.
                var needsFullResolve = true;
                switch (change)
                {
                    case InputDeviceChange.Added:
                        if (!state.CanUseDevice(device))
                            continue;
                        needsFullResolve = false;
                        break;

                    case InputDeviceChange.Removed:
                        if (!state.IsUsingDevice(device))
                            continue;

                        // If the device is listed in a device mask (on either a map or an asset) in the
                        // state, remove it (see Actions_WhenDeviceIsRemoved_DeviceIsRemovedFromDeviceMask).
                        for (var n = 0; n < state.totalMapCount; ++n)
                        {
                            var map = state.maps[n];
                            map.m_Devices.Remove(device);
                            map.asset?.m_Devices.Remove(device);
                        }

                        needsFullResolve = false;
                        break;

                    // NOTE: ConfigurationChanges can affect display names of controls which may make a device usable that
                    //       we didn't find anything usable on before.
                    case InputDeviceChange.ConfigurationChanged:
                    case InputDeviceChange.UsageChanged:
                        if (!state.IsUsingDevice(device) && !state.CanUseDevice(device))
                            continue;
                        // Full resolve necessary!
                        break;

                    // On reset, cancel all actions currently in progress from the device that got reset.
                    // If we simply let change monitors trigger, we will respond to things like button releases
                    // that are in fact just resets of buttons to their default state.
                    case InputDeviceChange.SoftReset:
                    case InputDeviceChange.HardReset:
                        if (!state.IsUsingDevice(device))
                            continue;
                        state.ResetActionStatesDrivenBy(device);
                        continue; // No re-resolving necessary.
                }

                // Trigger a lazy-resolve on all action maps in the state.
                for (var n = 0; n < state.totalMapCount; ++n)
                {
                    if (state.maps[n].LazyResolveBindings(fullResolve: needsFullResolve))
                    {
                        // Map has chosen to resolve right away. This will resolve bindings for *all*
                        // maps in the state, so we're done here.
                        break;
                    }
                }
            }
        }

        internal static void DeferredResolutionOfBindings()
        {
            ++InputActionMap.s_DeferBindingResolution;
            try
            {
                if (InputActionMap.s_NeedToResolveBindings)
                {
                    for (var i = 0; i < s_GlobalState.globalList.length; ++i)
                    {
                        var handle = s_GlobalState.globalList[i];

                        var state = handle.IsAllocated ? (InputActionState)handle.Target : null;
                        if (state == null)
                        {
                            // Stale entry in the list. State has already been reclaimed by GC. Remove it.
                            if (handle.IsAllocated)
                                s_GlobalState.globalList[i].Free();
                            s_GlobalState.globalList.RemoveAtWithCapacity(i);
                            --i;
                            continue;
                        }

                        for (var n = 0; n < state.totalMapCount; ++n)
                            state.maps[n].ResolveBindingsIfNecessary();
                    }
                    InputActionMap.s_NeedToResolveBindings = false;
                }
            }
            finally
            {
                --InputActionMap.s_DeferBindingResolution;
            }
        }

        internal static void DisableAllActions()
        {
            for (var i = 0; i < s_GlobalState.globalList.length; ++i)
            {
                var handle = s_GlobalState.globalList[i];
                if (!handle.IsAllocated || handle.Target == null)
                    continue;
                var state = (InputActionState)handle.Target;

                var mapCount = state.totalMapCount;
                var maps = state.maps;
                for (var n = 0; n < mapCount; ++n)
                {
                    maps[n].Disable();
                    Debug.Assert(!maps[n].enabled, "Map is still enabled after calling Disable");
                }
            }
        }

        /// <summary>
        /// Forcibly destroy all states currently on the global list.
        /// </summary>
        /// <remarks>
        /// We do this when exiting play mode in the editor to make sure we are cleaning up our
        /// unmanaged memory allocations.
        /// </remarks>
        internal static void DestroyAllActionMapStates()
        {
            while (s_GlobalState.globalList.length > 0)
            {
                var index = s_GlobalState.globalList.length - 1;
                var handle = s_GlobalState.globalList[index];
                if (!handle.IsAllocated || handle.Target == null)
                {
                    // Already destroyed.
                    if (handle.IsAllocated)
                        s_GlobalState.globalList[index].Free();
                    s_GlobalState.globalList.RemoveAtWithCapacity(index);
                    continue;
                }

                var state = (InputActionState)handle.Target;
                state.Destroy();
            }
        }

        #endregion
    }
}
