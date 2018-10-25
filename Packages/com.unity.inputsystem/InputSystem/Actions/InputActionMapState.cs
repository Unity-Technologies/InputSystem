using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Profiling;

////TODO: add a serialized form of this and take it across domain reloads

////TODO: remove direct references to InputManager

////TODO: make sure controls in per-action and per-map control arrays are unique (the internal arrays are probably okay to have duplicates)

////REVIEW: can we pack all the state that is blittable into a single chunk of unmanaged memory instead of into several managed arrays?
////        (this also means we can update more data with direct pointers)

////REVIEW: can we have a global array of InputControls?

////REVIEW: rename to just InputActionState?

////REVIEW: allow setup where state monitor is enabled but action is disabled?

namespace UnityEngine.Experimental.Input
{
    using InputActionListener = Action<InputAction.CallbackContext>;

    /// <summary>
    /// Dynamic execution state of one or more <see cref="InputActionMap">action maps</see>.
    /// </summary>
    /// <remarks>
    /// The aim of this class is to both put all the dynamic execution state into one place as well
    /// as to organize state in tight, GC-optimized arrays. Also, by moving state out of individual
    /// <see cref="InputActionMap">action maps</see>, we can combine the state of several maps
    /// into one single object with a single set of arrays. Ideally, if you have a single action
    /// asset in the game, you get a single InputActionMapState that contains the entire dynamic
    /// execution state for your game's actions.
    /// </remarks>
    internal class InputActionMapState : IInputStateChangeMonitor
    {
        public const int kInvalidIndex = -1;

        public InputActionMap[] maps;
        public ActionMapIndices[] mapIndices;

        /// <summary>
        /// List of all resolved controls.
        /// </summary>
        /// <remarks>
        /// As we don't know in advance how many controls a binding may match (if any), we bump the size of
        /// this array in increments during resolution. This means it may be end up being larger than the total
        /// number of used controls and have empty entries at the end. Use <see cref="totalControlCount"/> and not
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
        /// Indices match between this and <see cref="interactionStates"/>.
        /// </remarks>
        public IInputInteraction[] interactions;

        public object[] processors;

        /// <summary>
        /// Array of instantiated composite objects.
        /// </summary>
        public object[] composites;

        public int totalMapCount;

        public int totalActionCount;

        public int totalBindingCount;

        /// <summary>
        /// Total number of controls resolved from bindings of all action maps
        /// added to the state.
        /// </summary>
        public int totalControlCount;

        public int totalDeviceCount;

        public int totalInteractionCount;

        public int totalProcessorCount;

        public int totalCompositeCount;

        /// <summary>
        /// State of all bindings in the action map.
        /// </summary>
        /// <remarks>
        /// This array should not require GC scanning.
        /// </remarks>
        public BindingState[] bindingStates;

        /// <summary>
        /// State of all interactions on bindings in the action map.
        /// </summary>
        /// <remarks>
        /// Any interaction mentioned on any of the bindings gets its own execution state record
        /// in here. The interactions for any one binding are grouped together.
        ///
        /// This array should not require GC scanning.
        /// </remarks>
        public InteractionState[] interactionStates;

        /// <summary>
        /// Trigger state of each action in the map. Indices correspond to <see cref="InputActionMap.m_Actions"/>.
        /// </summary>
        /// <remarks>
        /// This may end up being <c>null</c> if there's binding but either no associated actions
        /// or no actions that could be found.
        ///
        /// This array also tells which actions are enabled or disabled. Any action with phase
        /// <see cref="InputActionPhase.Disabled"/> is disabled.
        ///
        /// This array should not require GC scanning.
        /// </remarks>
        public TriggerState[] triggerStates;

        public int[] controlIndexToBindingIndex;

        /// <summary>
        /// Initialize execution state with given resolved binding information.
        /// </summary>
        /// <param name="resolver"></param>
        public void Initialize(InputBindingResolver resolver)
        {
            ClaimDataFrom(resolver);
            AddToGlobaList();
        }

        internal void ClaimDataFrom(InputBindingResolver resolver)
        {
            totalMapCount = resolver.totalMapCount;
            totalActionCount = resolver.totalActionCount;
            totalBindingCount = resolver.totalBindingCount;
            totalInteractionCount = resolver.totalInteractionCount;
            totalProcessorCount = resolver.totalProcessorCount;
            totalCompositeCount = resolver.totalCompositeCount;
            totalControlCount = resolver.totalControlCount;
            totalDeviceCount = resolver.totalDeviceCount;

            maps = resolver.maps;
            mapIndices = resolver.mapIndices;
            triggerStates = resolver.actionStates;
            bindingStates = resolver.bindingStates;
            interactionStates = resolver.interactionStates;
            interactions = resolver.interactions;
            processors = resolver.processors;
            composites = resolver.composites;
            controls = resolver.controls;
            controlIndexToBindingIndex = resolver.controlIndexToBindingIndex;
        }

        public void Destroy()
        {
            for (var i = 0; i < totalMapCount; ++i)
            {
                var map = maps[i];
                Debug.Assert(!map.enabled);
                map.m_State = null;
                map.m_MapIndexInState = kInvalidIndex;

                var actions = map.m_Actions;
                if (actions != null)
                {
                    for (var n = 0; n < actions.Length; ++n)
                        actions[n].m_ActionIndex = kInvalidIndex;
                }
            }

            RemoveMapFromGlobalList();
        }

        ////TODO: switch to ref returns once we're on C#7
        public TriggerState FetchTriggerState(InputAction action)
        {
            Debug.Assert(action != null);
            Debug.Assert(action.m_ActionMap != null);
            Debug.Assert(action.m_ActionMap.m_MapIndexInState != kInvalidIndex);
            Debug.Assert(maps.Contains(action.m_ActionMap));
            Debug.Assert(action.m_ActionIndex >= 0 && action.m_ActionIndex < totalActionCount);

            return triggerStates[action.m_ActionIndex];
        }

        public ActionMapIndices FetchMapIndices(InputActionMap map)
        {
            Debug.Assert(map != null);
            Debug.Assert(maps.Contains(map));
            return mapIndices[map.m_MapIndexInState];
        }

        public void EnableAllActions(InputActionMap map)
        {
            Debug.Assert(map != null);
            Debug.Assert(map.m_Actions != null);
            Debug.Assert(maps.Contains(map));

            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount);

            // Install state monitors for all controls.
            var controlCount = mapIndices[mapIndex].controlCount;
            var controlStartIndex = mapIndices[mapIndex].controlStartIndex;
            if (controlCount > 0)
                EnableControls(mapIndex, controlStartIndex, controlCount);

            // Put all actions into waiting state.
            var actionCount = mapIndices[mapIndex].actionCount;
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;
            for (var i = 0; i < actionCount; ++i)
                triggerStates[actionStartIndex + i].phase = InputActionPhase.Waiting;

            NotifyListenersOfActionChange(InputActionChange.ActionMapEnabled, map);
        }

        public void EnableSingleAction(InputAction action)
        {
            Debug.Assert(action != null);
            Debug.Assert(action.m_ActionMap != null);
            Debug.Assert(maps.Contains(action.m_ActionMap));

            var actionIndex = action.m_ActionIndex;
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount);

            var map = action.m_ActionMap;
            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount);

            // Go through all bindings in the map and for all that belong to the given action,
            // enable the associated controls.
            var bindingStartIndex = mapIndices[mapIndex].bindingStartIndex;
            var bindingCount = mapIndices[mapIndex].bindingCount;
            for (var i = 0; i < bindingCount; ++i)
            {
                var bindingIndex = bindingStartIndex + i;
                if (bindingStates[bindingIndex].actionIndex != actionIndex)
                    continue;

                var controlCount = bindingStates[bindingIndex].controlCount;
                if (controlCount == 0)
                    continue;

                EnableControls(mapIndex, bindingStates[bindingIndex].controlStartIndex, controlCount);
            }

            // Put action into waiting state.
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;
            triggerStates[actionStartIndex + actionIndex].phase = InputActionPhase.Waiting;

            NotifyListenersOfActionChange(InputActionChange.ActionEnabled, action);
        }

        ////TODO: need to cancel actions if they are in started state
        ////TODO: reset all interaction states

        public void DisableAllActions(InputActionMap map)
        {
            Debug.Assert(map != null);
            Debug.Assert(map.m_Actions != null);
            Debug.Assert(maps.Contains(map));

            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount);

            // Remove state monitors from all controls.
            var controlCount = mapIndices[mapIndex].controlCount;
            var controlStartIndex = mapIndices[mapIndex].controlStartIndex;
            if (controlCount > 0)
                DisableControls(mapIndex, controlStartIndex, controlCount);

            // Mark all actions as disabled.
            var actionCount = mapIndices[mapIndex].actionCount;
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;
            for (var i = 0; i < actionCount; ++i)
                triggerStates[actionStartIndex + i].phase = InputActionPhase.Disabled;

            NotifyListenersOfActionChange(InputActionChange.ActionMapDisabled, map);
        }

        public void DisableSingleAction(InputAction action)
        {
            Debug.Assert(action != null);
            Debug.Assert(action.m_ActionMap != null);
            Debug.Assert(maps.Contains(action.m_ActionMap));

            var actionIndex = action.m_ActionIndex;
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount);

            var map = action.m_ActionMap;
            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount);

            // Go through all bindings in the map and for all that belong to the given action,
            // disable the associated controls.
            var bindingStartIndex = mapIndices[mapIndex].bindingStartIndex;
            var bindingCount = mapIndices[mapIndex].bindingCount;
            for (var i = 0; i < bindingCount; ++i)
            {
                var bindingIndex = bindingStartIndex + i;
                if (bindingStates[bindingIndex].actionIndex != actionIndex)
                    continue;

                var controlCount = bindingStates[bindingIndex].controlCount;
                if (controlCount == 0)
                    continue;

                DisableControls(mapIndex, bindingStates[bindingIndex].controlStartIndex, controlCount);
            }

            // Put action into disabled state.
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;
            triggerStates[actionStartIndex + actionIndex].phase = InputActionPhase.Disabled;

            NotifyListenersOfActionChange(InputActionChange.ActionDisabled, action);
        }

        ////REVIEW: can we have a method on InputManager doing this in bulk?

        private void EnableControls(int mapIndex, int controlStartIndex, int numControls)
        {
            Debug.Assert(controls != null);
            Debug.Assert(controlStartIndex >= 0 && controlStartIndex < totalControlCount);
            Debug.Assert(controlStartIndex + numControls <= totalControlCount);

            var manager = InputSystem.s_Manager;
            for (var i = 0; i < numControls; ++i)
            {
                var controlIndex = controlStartIndex + i;
                var bindingIndex = controlIndexToBindingIndex[controlIndex];
                var mapControlAndBindingIndex = ToCombinedMapAndControlAndBindingIndex(mapIndex, controlIndex, bindingIndex);

                manager.AddStateChangeMonitor(controls[controlIndex], this, mapControlAndBindingIndex);
            }
        }

        private void DisableControls(int mapIndex, int controlStartIndex, int numControls)
        {
            Debug.Assert(controls != null);
            Debug.Assert(controlStartIndex >= 0 && controlStartIndex < totalControlCount);
            Debug.Assert(controlStartIndex + numControls <= totalControlCount);

            var manager = InputSystem.s_Manager;
            for (var i = 0; i < numControls; ++i)
            {
                var controlIndex = controlStartIndex + i;
                var bindingIndex = controlIndexToBindingIndex[controlIndex];
                var mapControlAndBindingIndex = ToCombinedMapAndControlAndBindingIndex(mapIndex, controlIndex, bindingIndex);

                manager.RemoveStateChangeMonitor(controls[controlIndex], this, mapControlAndBindingIndex);
            }
        }

        // Called from InputManager when one of our state change monitors has fired.
        // Tells us the time of the change *according to the state events coming in*.
        // Also tells us which control of the controls we are binding to triggered the
        // change and relays the binding index we gave it when we called AddStateChangeMonitor.
        void IInputStateChangeMonitor.NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long mapControlAndBindingIndex)
        {
            int controlIndex;
            int bindingIndex;
            int mapIndex;

            SplitUpMapAndControlAndBindingIndex(mapControlAndBindingIndex, out mapIndex, out controlIndex, out bindingIndex);
            ProcessControlStateChange(mapIndex, controlIndex, bindingIndex, time, eventPtr);
        }

        void IInputStateChangeMonitor.NotifyTimerExpired(InputControl control, double time, long mapControlAndBindingIndex, int interactionIndex)
        {
            int controlIndex;
            int bindingIndex;
            int mapIndex;

            SplitUpMapAndControlAndBindingIndex(mapControlAndBindingIndex, out mapIndex, out controlIndex, out bindingIndex);
            ProcessTimeout(time, mapIndex, controlIndex, bindingIndex, interactionIndex);
        }

        // We mangle the various indices we use into a single long for association with state change
        // monitors. While we could look up map and binding indices from control indices, keeping
        // all the information together avoids having to unnecessarily jump around in memory to grab
        // the various pieces of data.

        private static long ToCombinedMapAndControlAndBindingIndex(int mapIndex, int controlIndex, int bindingIndex)
        {
            var result = (long)controlIndex;
            result |= (long)bindingIndex << 32;
            result |= (long)mapIndex << 48;
            return result;
        }

        private static void SplitUpMapAndControlAndBindingIndex(long mapControlAndBindingIndex, out int mapIndex,
            out int controlIndex, out int bindingIndex)
        {
            controlIndex = (int)(mapControlAndBindingIndex & 0xffffffff);
            bindingIndex = (int)((mapControlAndBindingIndex >> 32) & 0xffff);
            mapIndex = (int)(mapControlAndBindingIndex >> 48);
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
        /// </remarks>
        private unsafe void ProcessControlStateChange(int mapIndex, int controlIndex, int bindingIndex, double time, InputEventPtr eventPtr)
        {
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount);
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount);
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);

            ////TODO: make sure we only pass through here if we have meaningful state changes (i.e. actual value changes)

            fixed(BindingState* bindingStatesPtr = &bindingStates[0])
            {
                var bindingStatePtr = &bindingStatesPtr[bindingIndex];

                // If the binding is part of a composite, check for interactions on the composite
                // itself and give them a first shot at processing the value change.
                var haveInteractionsOnComposite = false;
                if (bindingStatePtr->isPartOfComposite)
                {
                    var compositeBindingIndex = bindingStatePtr->compositeOrCompositeBindingIndex;
                    var compositeBindingPtr = &bindingStatesPtr[compositeBindingIndex];

                    // If the composite has already been triggered from the very same event, ignore it.
                    // Example: KeyboardState change that includes both A and W key state changes and we're looking
                    //          at a WASD composite binding. There's a state change monitor on both the A and the W
                    //          key and thus the manager will notify us individually of both changes. However, we
                    //          want to perform the action only once.
                    if (ShouldIgnoreStateChangeOnCompositeBinding(compositeBindingPtr, eventPtr))
                        return;

                    var interactionCountOnComposite = compositeBindingPtr->interactionCount;
                    if (interactionCountOnComposite > 0)
                    {
                        haveInteractionsOnComposite = true;
                        ProcessInteractions(mapIndex, controlIndex, bindingIndex, time,
                            compositeBindingPtr->interactionStartIndex,
                            interactionCountOnComposite);
                    }
                }

                // If we have interactions, let them do all the processing. The presence of an interaction
                // essentially bypasses the default phase progression logic of an action.
                var interactionCount = bindingStatePtr->interactionCount;
                if (interactionCount > 0)
                {
                    ProcessInteractions(mapIndex, controlIndex, bindingIndex, time,
                        bindingStatePtr->interactionStartIndex, interactionCount);
                }
                else if (!haveInteractionsOnComposite)
                {
                    // Default logic has no support for cancellations and won't ever go into started
                    // phase. Will go from waiting straight to performed and then straight to waiting
                    // again.
                    //
                    // Also, we perform the action on *any* value change. For buttons, this means that
                    // if you use the default logic without an interaction, the action will be performed
                    // both when you press and when you release the button.

                    var trigger = new TriggerState
                    {
                        phase = InputActionPhase.Performed,
                        mapIndex = mapIndex,
                        controlIndex = controlIndex,
                        bindingIndex = bindingIndex,
                        interactionIndex = kInvalidIndex,
                        time = time,
                        startTime = time,
                    };
                    ChangePhaseOfAction(InputActionPhase.Performed, ref trigger);
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
        /// To do so, we store the
        /// </remarks>
        private unsafe bool ShouldIgnoreStateChangeOnCompositeBinding(BindingState* binding, InputEvent* eventPtr)
        {
            if (eventPtr == null)
                return false;

            var eventId = eventPtr->eventId;
            if (binding->triggerEventIdForComposite == eventId)
                return true;

            binding->triggerEventIdForComposite = eventId;
            return false;
        }

        private void ProcessInteractions(int mapIndex, int controlIndex, int bindingIndex, double time, int interactionStartIndex, int interactionCount)
        {
            var context = new InputInteractionContext
            {
                m_State = this,
                m_TriggerState = new TriggerState
                {
                    time = time,
                    mapIndex = mapIndex,
                    bindingIndex = bindingIndex,
                    controlIndex = controlIndex,
                }
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
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount);
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);
            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount);

            var currentState = interactionStates[interactionIndex];

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
                    interactionIndex = interactionIndex
                },
                timerHasExpired = true,
            };

            currentState.isTimerRunning = false;
            interactionStates[interactionIndex] = currentState;

            // Let interaction handle timer expiration.
            interactions[interactionIndex].Process(ref context);
        }

        internal void StartTimeout(float seconds, ref TriggerState trigger)
        {
            Debug.Assert(trigger.mapIndex >= 0 && trigger.mapIndex < totalMapCount);
            Debug.Assert(trigger.controlIndex >= 0 && trigger.controlIndex < totalControlCount);
            Debug.Assert(trigger.interactionIndex >= 0 && trigger.interactionIndex < totalInteractionCount);

            var manager = InputSystem.s_Manager;
            var currentTime = manager.m_Runtime.currentTime;
            var control = controls[trigger.controlIndex];
            var interactionIndex = trigger.interactionIndex;
            var monitorIndex =
                ToCombinedMapAndControlAndBindingIndex(trigger.mapIndex, trigger.controlIndex, trigger.bindingIndex);

            manager.AddStateChangeMonitorTimeout(control, this, currentTime + seconds, monitorIndex,
                interactionIndex);

            // Update state.
            var interactionState = interactionStates[interactionIndex];
            interactionState.isTimerRunning = true;
            interactionStates[interactionIndex] = interactionState;
        }

        private void StopTimeout(int mapIndex, int controlIndex, int bindingIndex, int interactionIndex)
        {
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount);
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount);
            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount);

            var manager = InputSystem.s_Manager;
            var monitorIndex =
                ToCombinedMapAndControlAndBindingIndex(mapIndex, controlIndex, bindingIndex);

            manager.RemoveStateChangeMonitorTimeout(this, monitorIndex, interactionIndex);

            // Update state.
            var interactionState = interactionStates[interactionIndex];
            interactionState.isTimerRunning = false;
            interactionStates[interactionIndex] = interactionState;
        }

        /// <summary>
        /// Perform a phase change on the given interaction. Only visible to observers
        /// if it happens to change the phase of the action, too.
        /// </summary>
        /// <param name="newPhase">New phase to transition the interaction to.</param>
        /// <param name="trigger">Information about the binding and control that triggered the phase change.</param>
        /// <param name="remainStartedAfterPerformed">If true, then instead of going back to <see cref="InputActionPhase.Waiting"/>
        /// after transitioning to <see cref="InputActionPhase.Performed"/>, the interaction (and thus potentially the action)
        /// will remain in <see cref="InputActionPhase.Started"/> phase. This is useful for interactions that use
        /// <see cref="InputActionPhase.Started"/> to signal the start of a continuous interaction, then use <see
        /// cref="InputActionPhase.Performed"/> during the interaction and then <see cref="InputActionPhase.Cancelled"/> when
        /// the interaction stops.</param>
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
        internal void ChangePhaseOfInteraction(InputActionPhase newPhase, ref TriggerState trigger, bool remainStartedAfterPerformed = false)
        {
            var interactionIndex = trigger.interactionIndex;
            var bindingIndex = trigger.bindingIndex;

            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount);
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);

            ////TODO: need to make sure that performed and cancelled phase changes happen on the *same* binding&control
            ////      as the start of the phase

            // Update interaction state.
            ThrowIfPhaseTransitionIsInvalid(interactionStates[interactionIndex].phase, newPhase, ref trigger);
            interactionStates[interactionIndex].phase = newPhase;
            interactionStates[interactionIndex].triggerControlIndex = trigger.controlIndex;
            if (newPhase == InputActionPhase.Started)
                interactionStates[interactionIndex].startTime = trigger.time;

            ////REVIEW: If we want to defer triggering of actions, this is the point where we probably need to cut things off
            // See if it affects the phase of an associated action.
            var actionIndex = bindingStates[bindingIndex].actionIndex; // We already had to tap this array and entry in ProcessControlStateChange.
            if (actionIndex != -1)
            {
                if (triggerStates[actionIndex].phase == InputActionPhase.Waiting)
                {
                    // We're the first interaction to go to the start phase.
                    ChangePhaseOfAction(newPhase, ref trigger);
                }
                else if (newPhase == InputActionPhase.Cancelled && triggerStates[actionIndex].interactionIndex == trigger.interactionIndex)
                {
                    // We're cancelling but maybe there's another interaction ready
                    // to go into start phase.

                    ChangePhaseOfAction(newPhase, ref trigger);

                    var interactionStartIndex = bindingStates[bindingIndex].interactionStartIndex;
                    var numInteractions = bindingStates[bindingIndex].interactionCount;
                    for (var i = 0; i < numInteractions; ++i)
                    {
                        var index = interactionStartIndex + i;
                        if (index != trigger.interactionIndex && interactionStates[index].phase == InputActionPhase.Started)
                        {
                            var triggerForInteraction = new TriggerState
                            {
                                phase = InputActionPhase.Started,
                                controlIndex = interactionStates[index].triggerControlIndex,
                                bindingIndex = trigger.bindingIndex,
                                interactionIndex = index,
                                time = trigger.time,
                                startTime = interactionStates[index].startTime
                            };
                            ChangePhaseOfAction(InputActionPhase.Started, ref triggerForInteraction);
                            break;
                        }
                    }
                }
                else if (triggerStates[actionIndex].interactionIndex == trigger.interactionIndex)
                {
                    var phaseAfterPerformedOrCancelled = InputActionPhase.Waiting;
                    if (newPhase == InputActionPhase.Performed && remainStartedAfterPerformed)
                        phaseAfterPerformedOrCancelled = InputActionPhase.Started;

                    // Any other phase change goes to action if we're the interaction driving
                    // the current phase.
                    ChangePhaseOfAction(newPhase, ref trigger, phaseAfterPerformedOrCancelled);

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
                                ResetInteraction(trigger.mapIndex, trigger.bindingIndex, index);
                        }
                    }
                }
            }

            // If the interaction performed or cancelled, go back to waiting.
            // Exception: if it was performed and we're to remain in started state, set the interaction
            //            to started. Note that for that phase transition, there are no callbacks being
            //            triggered (i.e. we don't call 'started' every time after 'performed').
            if (newPhase == InputActionPhase.Performed && remainStartedAfterPerformed)
            {
                interactionStates[interactionIndex].phase = InputActionPhase.Started;
            }
            else if (newPhase == InputActionPhase.Performed || newPhase == InputActionPhase.Cancelled)
            {
                ResetInteraction(trigger.mapIndex, trigger.bindingIndex, trigger.interactionIndex);
            }
            ////TODO: reset entire chain
        }

        // Perform a phase change on the action. Visible to observers.
        internal void ChangePhaseOfAction(InputActionPhase newPhase, ref TriggerState trigger,
            InputActionPhase phaseAfterPerformedOrCancelled = InputActionPhase.Waiting)
        {
            Debug.Assert(trigger.mapIndex >= 0 && trigger.mapIndex < totalMapCount);
            Debug.Assert(trigger.controlIndex >= 0 && trigger.controlIndex < totalControlCount);
            Debug.Assert(trigger.bindingIndex >= 0 && trigger.bindingIndex < totalBindingCount);

            var actionIndex = bindingStates[trigger.bindingIndex].actionIndex;
            if (actionIndex == kInvalidIndex)
                return; // No action associated with binding.

            // Make sure phase progression is valid.
            var currentPhase = triggerStates[actionIndex].phase;
            ThrowIfPhaseTransitionIsInvalid(currentPhase, newPhase, ref trigger);

            // Update action state.
            triggerStates[actionIndex] = trigger;
            triggerStates[actionIndex].phase = newPhase;

            // Let listeners know.
            var map = maps[trigger.mapIndex];
            var action = map.m_Actions[actionIndex - mapIndices[trigger.mapIndex].actionStartIndex];
            switch (newPhase)
            {
                case InputActionPhase.Started:
                    CallActionListeners(map, ref action.m_OnStarted, ref trigger);
                    break;

                case InputActionPhase.Performed:
                    CallActionListeners(map, ref action.m_OnPerformed, ref trigger);
                    triggerStates[actionIndex].phase = phaseAfterPerformedOrCancelled;
                    break;

                case InputActionPhase.Cancelled:
                    CallActionListeners(map, ref action.m_OnCancelled, ref trigger);
                    triggerStates[actionIndex].phase = phaseAfterPerformedOrCancelled;
                    break;
            }
        }

        private void CallActionListeners(InputActionMap actionMap, ref InlinedArray<InputActionListener> listeners, ref TriggerState trigger)
        {
            // If there's no listeners, don't bother with anything else.
            var callbacksOnMap = actionMap.m_ActionCallbacks;
            if (listeners.length == 0 && callbacksOnMap.length == 0)
                return;

            var context = new InputAction.CallbackContext
            {
                m_State = this,
                m_Time = trigger.time,
                m_BindingIndex = trigger.bindingIndex,
                m_InteractionIndex = trigger.interactionIndex,
                m_ControlIndex = trigger.controlIndex,
            };

            Profiler.BeginSample("InputActionCallback");

            // Run callbacks (if any) directly on action.
            var listenerCount = listeners.length;
            for (var i = 0; i < listenerCount; ++i)
            {
                try
                {
                    listeners[i](context);
                }
                catch (Exception exception)
                {
                    Debug.LogError(string.Format("{0} thrown during execution of '{1}' callback on action '{2}'",
                        exception.GetType().Name, trigger.phase, GetActionOrNull(ref trigger)));
                    Debug.LogException(exception);
                }
            }

            // Run callbacks (if any) on action map.
            var listenerCountOnMap = callbacksOnMap.length;
            for (var i = 0; i < listenerCountOnMap; ++i)
            {
                try
                {
                    callbacksOnMap[i](context);
                }
                catch (Exception exception)
                {
                    Debug.LogError(string.Format("{0} thrown during execution of callback for '{1}' phase of '{2}' action in map '{3}'",
                        exception.GetType().Name, trigger.phase, GetActionOrNull(ref trigger).name, actionMap.name));
                    Debug.LogException(exception);
                }
            }

            Profiler.EndSample();
        }

        ////REVIEW: does this really add value? should we just allow whatever transitions?
        private void ThrowIfPhaseTransitionIsInvalid(InputActionPhase currentPhase, InputActionPhase newPhase, ref TriggerState trigger)
        {
            // Can only go to Started from Waiting.
            if (newPhase == InputActionPhase.Started && currentPhase != InputActionPhase.Waiting)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' (action: {3}, interaction: {4})",
                        currentPhase, InputActionPhase.Started, InputActionPhase.Waiting,
                        GetActionOrNoneString(ref trigger), GetInteractionOrNull(ref trigger)));

            // Can only go to Performed from Waiting or Started.
            if (newPhase == InputActionPhase.Performed && currentPhase != InputActionPhase.Waiting &&
                currentPhase != InputActionPhase.Started)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' or '{3}' (action: {4}, interaction: {5})",
                        currentPhase, InputActionPhase.Performed, InputActionPhase.Waiting, InputActionPhase.Started,
                        GetActionOrNoneString(ref trigger),
                        GetInteractionOrNull(ref trigger)));

            // Can only go to Cancelled from Started.
            if (newPhase == InputActionPhase.Cancelled && currentPhase != InputActionPhase.Started)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' (action: {3}, interaction: {4})",
                        currentPhase, InputActionPhase.Cancelled, InputActionPhase.Started,
                        GetActionOrNoneString(ref trigger), GetInteractionOrNull(ref trigger)));
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
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);

            var actionIndex = bindingStates[bindingIndex].actionIndex;
            if (actionIndex == kInvalidIndex)
                return null;

            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount);
            var mapIndex = bindingStates[bindingIndex].mapIndex;
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;
            return maps[mapIndex].m_Actions[actionIndex - actionStartIndex];
        }

        internal InputAction GetActionOrNull(ref TriggerState trigger)
        {
            Debug.Assert(trigger.mapIndex >= 0 && trigger.mapIndex < totalMapCount);
            Debug.Assert(trigger.bindingIndex >= 0 && trigger.bindingIndex < totalBindingCount);

            var actionIndex = bindingStates[trigger.bindingIndex].actionIndex;
            if (actionIndex == kInvalidIndex)
                return null;

            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount);
            var actionStartIndex = mapIndices[trigger.mapIndex].actionStartIndex;
            return maps[trigger.mapIndex].m_Actions[actionIndex - actionStartIndex];
        }

        internal InputControl GetControl(ref TriggerState trigger)
        {
            Debug.Assert(trigger.controlIndex != kInvalidIndex);
            Debug.Assert(trigger.controlIndex >= 0 && trigger.controlIndex < totalControlCount);
            return controls[trigger.controlIndex];
        }

        private IInputInteraction GetInteractionOrNull(ref TriggerState trigger)
        {
            if (trigger.interactionIndex == kInvalidIndex)
                return null;

            Debug.Assert(trigger.interactionIndex >= 0 && trigger.interactionIndex < totalInteractionCount);
            return interactions[trigger.interactionIndex];
        }

        internal InputBinding GetBinding(int bindingIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);
            var mapIndex = bindingStates[bindingIndex].mapIndex;
            var bindingStartIndex = mapIndices[mapIndex].bindingStartIndex;
            return maps[mapIndex].m_Bindings[bindingIndex - bindingStartIndex];
        }

        private void ResetInteraction(int mapIndex, int bindingIndex, int interactionIndex)
        {
            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount);
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);

            interactions[interactionIndex].Reset();

            if (interactionStates[interactionIndex].isTimerRunning)
            {
                var controlIndex = interactionStates[interactionIndex].triggerControlIndex;
                StopTimeout(mapIndex, controlIndex, bindingIndex, interactionIndex);
            }

            interactionStates[interactionIndex] =
                new InteractionState
            {
                phase = InputActionPhase.Waiting
            };
        }

        internal int GetValueSizeInBytes(int bindingIndex, int controlIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount);

            if (bindingStates[bindingIndex].isPartOfComposite) ////TODO: instead, just have compositeOrCompositeBindingIndex be invalid
            {
                var compositeBindingIndex = bindingStates[bindingIndex].compositeOrCompositeBindingIndex;
                var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                var compositeObject = (IInputBindingComposite)composites[compositeIndex];
                Debug.Assert(compositeObject != null);

                return compositeObject.valueSizeInBytes;
            }

            var control = controls[controlIndex];
            Debug.Assert(control != null);
            return control.valueSizeInBytes;
        }

        internal Type GetValueType(int bindingIndex, int controlIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount);

            if (bindingStates[bindingIndex].isPartOfComposite) ////TODO: instead, just have compositeOrCompositeBindingIndex be invalid
            {
                var compositeBindingIndex = bindingStates[bindingIndex].compositeOrCompositeBindingIndex;
                var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                var compositeObject = (IInputBindingComposite)composites[compositeIndex];
                Debug.Assert(compositeObject != null);

                return compositeObject.valueType;
            }

            var control = controls[controlIndex];
            Debug.Assert(control != null);
            return control.valueType;
        }

        ////REVIEW: we can unify the reading paths once we have blittable type constraints

        internal unsafe void ReadValue(int bindingIndex, int controlIndex, void* buffer, int bufferSize)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount);

            // If the binding that triggered the action is part of a composite, let
            // the composite determine the value we return.
            if (bindingStates[bindingIndex].isPartOfComposite) ////TODO: instead, just have compositeOrCompositeBindingIndex be invalid
            {
                var compositeBindingIndex = bindingStates[bindingIndex].compositeOrCompositeBindingIndex;
                var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                var compositeObject = (IInputBindingComposite)composites[compositeIndex];
                Debug.Assert(compositeObject != null);

                var context = new InputBindingCompositeContext();
                compositeObject.ReadValue(ref context, buffer, bufferSize);
            }
            else
            {
                var control = controls[controlIndex];
                Debug.Assert(control != null);
                control.WriteValueInto(buffer, bufferSize);
            }

            /*
            // Run value through processors, if any.
            var processorCount = bindingStates[bindingIndex].processorCount;
            if (processorCount > 0)
            {
                var processorStartIndex = bindingStates[bindingIndex].processorStartIndex;
                for (var i = 0; i < processorCount; ++i)
                    value = ((IInputControlProcessor<TValue>)processors[processorStartIndex + i]).Process(value, controlOfType);
            }
            */
        }

        internal TValue ReadValue<TValue>(int bindingIndex, int controlIndex)
            where TValue : struct
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount);

            var value = default(TValue);

            // In the case of a composite, this will be null.
            InputControl<TValue> controlOfType = null;

            // If the binding that triggered the action is part of a composite, let
            // the composite determine the value we return.
            if (bindingStates[bindingIndex].isPartOfComposite) ////TODO: instead, just have compositeOrCompositeBindingIndex be invalid
            {
                var compositeBindingIndex = bindingStates[bindingIndex].compositeOrCompositeBindingIndex;
                var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                var compositeObject = composites[compositeIndex];
                Debug.Assert(compositeObject != null);

                var compositeOfType = compositeObject as IInputBindingComposite<TValue>;
                if (compositeOfType == null)
                    throw new InvalidOperationException(string.Format(
                        "Cannot read value of type '{0}' from composite '{1}' bound to action '{2}' (composite is a '{3}' with value type '{4}')",
                        typeof(TValue).Name, compositeObject, GetActionOrNull(bindingIndex),
                        compositeIndex.GetType().Name,
                        TypeHelpers.GetNiceTypeName(compositeObject.GetType().GetGenericArguments()[0])));

                var context = new InputBindingCompositeContext();
                value = compositeOfType.ReadValue(ref context);
            }
            else
            {
                var control = controls[controlIndex];
                Debug.Assert(control != null);

                controlOfType = control as InputControl<TValue>;
                if (controlOfType == null)
                    throw new InvalidOperationException(string.Format(
                        "Cannot read value of type '{0}' from control '{1}' bound to action '{2}' (control is a '{3}' with value type '{4}')",
                        typeof(TValue).Name, control.path, GetActionOrNull(bindingIndex), control.GetType().Name,
                        TypeHelpers.GetNiceTypeName(control.valueType)));

                value = controlOfType.ReadValue();
            }

            // Run value through processors, if any.
            var processorCount = bindingStates[bindingIndex].processorCount;
            if (processorCount > 0)
            {
                var processorStartIndex = bindingStates[bindingIndex].processorStartIndex;
                for (var i = 0; i < processorCount; ++i)
                    value = ((IInputControlProcessor<TValue>)processors[processorStartIndex + i]).Process(value, controlOfType);
            }

            return value;
        }

        /// <summary>
        /// Records the current state of a single interaction attached to a binding.
        /// Each interaction keeps track of its own trigger control and phase progression.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 12)]
        internal struct InteractionState
        {
            [FieldOffset(0)] private ushort m_TriggerControlIndex;
            [FieldOffset(2)] private byte m_Phase;
            [FieldOffset(3)] private byte m_Flags;
            [FieldOffset(4)] private double m_StartTime;

            public int triggerControlIndex
            {
                get { return m_TriggerControlIndex; }
                set
                {
                    Debug.Assert(value >= 0 && value <= ushort.MaxValue);
                    if (value < 0 || value > ushort.MaxValue)
                        throw new NotSupportedException("Cannot have more than ushort.MaxValue controls in a single InputActionMapState");
                    m_TriggerControlIndex = (ushort)value;
                }
            }

            public double startTime
            {
                get { return m_StartTime; }
                set { m_StartTime = value; }
            }

            public bool isTimerRunning
            {
                get { return ((Flags)m_Flags & Flags.TimerRunning) == Flags.TimerRunning; }
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
                get { return (InputActionPhase)m_Phase; }
                set { m_Phase = (byte)value; }
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
        ///
        /// Keep references out of this struct. It should be blittable and require no GC scanning.
        /// </remarks>
        [StructLayout(LayoutKind.Explicit, Size = 20)]
        internal struct BindingState
        {
            [FieldOffset(0)] private byte m_ControlCount;
            [FieldOffset(1)] private byte m_InteractionCount;
            [FieldOffset(2)] private byte m_ProcessorCount;
            [FieldOffset(3)] private byte m_MapIndex;
            [FieldOffset(4)] private byte m_Flags;
            // One unused byte.
            [FieldOffset(6)] private ushort m_ActionIndex;
            [FieldOffset(8)] private ushort m_CompositeOrCompositeBindingIndex;
            [FieldOffset(10)] private ushort m_ProcessorStartIndex;
            [FieldOffset(12)] private ushort m_InteractionStartIndex;
            [FieldOffset(14)] private ushort m_ControlStartIndex;
            [FieldOffset(16)] private int m_TriggerEventIdForComposite;

            [Flags]
            public enum Flags
            {
                ChainsWithNext = 1 << 0,
                EndOfChain = 1 << 1,
                PartOfComposite = 1 << 2,
            }

            /// <summary>
            /// Index into <see cref="controls"/> of first control associated with the binding.
            /// </summary>
            public int controlStartIndex
            {
                get { return m_ControlStartIndex; }
                set
                {
                    Debug.Assert(value != kInvalidIndex);
                    if (value >= ushort.MaxValue)
                        throw new NotSupportedException("Total control count in state cannot exceed byte.MaxValue=" + ushort.MaxValue);
                    m_ControlStartIndex = (ushort)value;
                }
            }

            /// <summary>
            /// Number of controls associated with this binding.
            /// </summary>
            public int controlCount
            {
                get { return m_ControlCount; }
                set
                {
                    if (value >= byte.MaxValue)
                        throw new NotSupportedException("Control count per binding cannot exceed byte.MaxValue=" + byte.MaxValue);
                    m_ControlCount = (byte)value;
                }
            }

            /// <summary>
            /// Index into <see cref="InputActionMapState.interactionStates"/> of first interaction associated with the binding.
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
                get { return m_InteractionCount; }
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
                get { return m_ProcessorCount; }
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
            /// For bindings that don't trigger actions, this is <c>-1</c>.
            ///
            /// Note that for bindings that are part of a composite, this does not necessarily correspond to the action
            /// triggered by the composite itself.
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
                get { return m_MapIndex; }
                set
                {
                    Debug.Assert(value != kInvalidIndex);
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
                get { return m_TriggerEventIdForComposite; }
                set { m_TriggerEventIdForComposite = value; }
            }

            public Flags flags
            {
                get { return (Flags)m_Flags; }
                set { m_Flags = (byte)value; }
            }

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

            public bool isPartOfChain
            {
                get { return chainsWithNext || isEndOfChain; }
            }

            ////TODO: remove
            public bool isPartOfComposite
            {
                get { return (flags & Flags.PartOfComposite) == Flags.PartOfComposite; }
                set
                {
                    if (value)
                        flags |= Flags.PartOfComposite;
                    else
                        flags &= ~Flags.PartOfComposite;
                }
            }
        }

        /// <summary>
        /// Record of an input control change and its related data.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 28)]
        public struct TriggerState
        {
            [FieldOffset(0)] private ushort m_Phase;
            [FieldOffset(2)] private ushort m_MapIndex;
            [FieldOffset(4)] private double m_Time;
            [FieldOffset(12)] private double m_StartTime;
            [FieldOffset(20)] private int m_ControlIndex;
            [FieldOffset(24)] private ushort m_BindingIndex;
            [FieldOffset(26)] private ushort m_InteractionIndex;

            /// <summary>
            /// Phase being triggered by the control value change.
            /// </summary>
            public InputActionPhase phase
            {
                get { return (InputActionPhase)m_Phase; }
                set { m_Phase = (ushort)value; }
            }

            /// <summary>
            /// The time the binding got triggered.
            /// </summary>
            public double time
            {
                get { return m_Time; }
                set { m_Time = value; }
            }

            /// <summary>
            /// The time when the binding moved into <see cref="InputActionPhase.Started"/>.
            /// </summary>
            public double startTime
            {
                get { return m_StartTime; }
                set { m_StartTime = value; }
            }

            public int mapIndex
            {
                get { return m_MapIndex; }
                set
                {
                    Debug.Assert(value >= 0 && value <= ushort.MaxValue);
                    if (value < 0 || value > ushort.MaxValue)
                        throw new NotSupportedException("More than ushort.MaxValue InputActionMaps in a single InputActionMapState");
                    m_MapIndex = (ushort)value;
                }
            }

            public int controlIndex
            {
                get { return m_ControlIndex; }
                set { m_ControlIndex = value; }
            }

            /// <summary>
            /// Index into <see cref="bindingStates"/> for the binding that triggered.
            /// </summary>
            public int bindingIndex
            {
                get { return m_BindingIndex; }
                set
                {
                    Debug.Assert(value >= 0 && value <= ushort.MaxValue);
                    if (value < 0 || value > ushort.MaxValue)
                        throw new NotSupportedException("More than ushort.MaxValue bindings in a single InputActionMapState");
                    m_BindingIndex = (ushort)value;
                }
            }

            /// <summary>
            /// Index into <see cref="InputActionMapState.interactionStates"/> for the interaction that triggered.
            /// </summary>
            /// <remarks>
            /// Is <see cref="InputActionMapState.kInvalidIndex"/> if there is no interaction present on the binding.
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
                    Debug.Assert(value == kInvalidIndex || (value >= 0 && value < ushort.MaxValue));
                    if (value == kInvalidIndex)
                        m_InteractionIndex = ushort.MaxValue;
                    else
                    {
                        if (value < 0 || value >= ushort.MaxValue)
                            throw new NotSupportedException("More than ushort.MaxValue-1 interactions in a single InputActionMapState");
                        m_InteractionIndex = (ushort)value;
                    }
                }
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

        #region Global State

        /// <summary>
        /// List of weak references to all action map states currently in the system.
        /// </summary>
        /// <remarks>
        /// When the control setup in the system changes, we need a way for control resolution that
        /// has already been done to be invalidated and redone. We also want a way to find all
        /// currently enabled actions in the system.
        ///
        /// Both of these needs are served by this global list.
        /// </remarks>
        private static InlinedArray<GCHandle> s_GlobalList;
        internal static InlinedArray<Action<object, InputActionChange>> s_OnActionChange;

        private void AddToGlobaList()
        {
            CompactGlobalList();
            var handle = GCHandle.Alloc(this, GCHandleType.Weak);
            s_GlobalList.AppendWithCapacity(handle);
        }

        private void RemoveMapFromGlobalList()
        {
            var count = s_GlobalList.length;
            for (var i = 0; i < count; ++i)
                if (s_GlobalList[i].Target == this)
                {
                    s_GlobalList[i].Free();
                    s_GlobalList.RemoveAtByMovingTailWithCapacity(i);
                    break;
                }
        }

        /// <summary>
        /// Remove any entries for states that have been reclaimed by GC.
        /// </summary>
        private static void CompactGlobalList()
        {
            var length = s_GlobalList.length;
            var head = 0;
            for (var i = 0; i < length; ++i)
            {
                if (s_GlobalList[i].Target != null)
                {
                    if (head != i)
                        s_GlobalList[head] = s_GlobalList[i];
                    ++head;
                }
                else
                {
                    s_GlobalList[i].Free();
                }
            }
            s_GlobalList.length = head;
        }

        internal static void NotifyListenersOfActionChange(InputActionChange change, object actionOrMap)
        {
            for (var i = 0; i < s_OnActionChange.length; ++i)
                s_OnActionChange[i](actionOrMap, change);
        }

        /// <summary>
        /// Nuke global state we have to keep track of action map states.
        /// </summary>
        internal static void ResetGlobals()
        {
            for (var i = 0; i < s_GlobalList.length; ++i)
                s_GlobalList[i].Free();
            s_GlobalList.length = 0;
            s_OnActionChange.Clear();
        }

        // Walk all maps with enabled actions and add all enabled actions to the given list.
        internal static int FindAllEnabledActions(List<InputAction> result)
        {
            var numFound = 0;
            var stateCount = s_GlobalList.length;
            for (var i = 0; i < stateCount; ++i)
            {
                var state = (InputActionMapState)s_GlobalList[i].Target;
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
                            if (state.triggerStates[actionStartIndex + k].phase != InputActionPhase.Disabled)
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

        // The following things cannot change and be handled by this method:
        // - Set of maps in the state cannot change (neither order nor amount)
        // - Set of actions in the maps cannot change (neither order nor amount)
        // - Set of bindings in the maps cannot change (neither order nor amount)
        // To touch configuration data, state has to be thrown away.
        internal static void ReResolveAllEnabledActions()
        {
            ////REVIEW: for state that does not have any enabled maps, may be smarter to just nuke the state and
            ////        let the maps re-resolve should they get enabled again
            var stateCount = s_GlobalList.length;
            for (var i = 0; i < stateCount; ++i)
            {
                var state = (InputActionMapState)s_GlobalList[i].Target;
                if (state == null)
                    continue;

                var maps = state.maps;
                var mapCount = state.totalMapCount;

                ////FIXME: we can't have a bunch of garbage get created every time someone plugs in a device;
                ////       the logic here has to become smart such that if there is no change to any action map,
                ////       there is no garbage being created

                // Re-resolve all maps in the state.
                var resolver = new InputBindingResolver();
                for (var n = 0; n < mapCount; ++n)
                {
                    var map = maps[n];
                    map.m_MapIndexInState = kInvalidIndex;
                    resolver.AddActionMap(map);
                }

                // See if this changes things for the state. If so, leave the
                // state as is.
                // NOTE: The resolver will store indices in InputActionMap and InputAction but no
                //       references to anything in the state.
                if (state.DataMatches(resolver))
                    continue;

                // Otherwise, first get rid of all state change monitors currently installed.
                var mapIndices = state.mapIndices;
                for (var n = 0; n < mapCount; ++n)
                {
                    Debug.Assert(maps[n].m_MapIndexInState == n);
                    if (!maps[n].enabled)
                        continue;

                    var controlCount = mapIndices[n].controlCount;
                    if (controlCount > 0)
                        state.DisableControls(n, mapIndices[n].controlStartIndex, controlCount);
                }

                var oldActionStates = state.triggerStates;

                // Re-initialize the state.
                state.ClaimDataFrom(resolver);
                for (var n = 0; n < mapCount; ++n)
                {
                    var map = maps[n];
                    map.m_State = state;

                    // Controls for actions need to be re-computed on the map.
                    map.m_ControlsForEachAction = null;
                }

                // Restore enabled actions.
                var newActionStates = state.triggerStates;
                Debug.Assert((oldActionStates == null && newActionStates == null) || newActionStates.Length == oldActionStates.Length);
                for (var n = 0; n < state.totalActionCount; ++n)
                {
                    ////TODO: we want to preserve as much state as we can here, not just lose all current execution state of the maps
                    if (oldActionStates[n].phase != InputActionPhase.Disabled)
                        newActionStates[n].phase = InputActionPhase.Waiting;
                }

                // Restore state change monitors.
                for (var n = 0; n < state.totalBindingCount; ++n)
                {
                    // Skip if binding does not resolve to controls.
                    var controlCount = state.bindingStates[n].controlCount;
                    if (controlCount == 0)
                        continue;

                    // Skip if binding does not target action.
                    var actionIndex = state.bindingStates[n].actionIndex;
                    if (actionIndex == kInvalidIndex)
                        continue;

                    // Skip if action targeted by binding is not enabled.
                    if (newActionStates[actionIndex].phase == InputActionPhase.Disabled)
                        continue;

                    // Reenable.
                    state.EnableControls(newActionStates[actionIndex].mapIndex,
                        state.bindingStates[n].controlStartIndex,
                        controlCount);
                }

                ////REVIEW: ideally, we should know which actions have *actually* changed their set of bound controls
                // Fire change monitors.
                for (var n = 0; n < mapCount; ++n)
                {
                    var map = state.maps[n];
                    if (map.m_EnabledActionsCount == 0)
                        continue;
                    var actionCount = map.m_Actions.Length;
                    for (var k = 0; k < actionCount; ++k)
                    {
                        var action = map.m_Actions[k];
                        if (action.enabled)
                            NotifyListenersOfActionChange(InputActionChange.BoundControlsHaveChangedWhileEnabled, action);
                    }
                }
            }
        }

        internal bool DataMatches(InputBindingResolver resolver)
        {
            if (totalMapCount != resolver.totalMapCount
                || totalActionCount != resolver.totalActionCount
                || totalBindingCount != resolver.totalBindingCount
                || totalCompositeCount != resolver.totalCompositeCount
                || totalControlCount != resolver.totalControlCount
                || totalInteractionCount != resolver.totalInteractionCount)
                return false;

            if (!ArrayHelpers.HaveEqualElements(maps, resolver.maps)
                || !ArrayHelpers.HaveEqualElements(controls, resolver.controls)
                || !ArrayHelpers.HaveEqualElements(interactions, resolver.interactions)
                || !ArrayHelpers.HaveEqualElements(composites, resolver.composites))
                return false;

            return true;
        }

        internal static void DisableAllActions()
        {
            for (var i = 0; i < s_GlobalList.length; ++i)
            {
                var state = (InputActionMapState)s_GlobalList[i].Target;
                if (state == null)
                    continue;

                var mapCount = state.totalMapCount;
                var maps = state.maps;
                for (var n = 0; n < mapCount; ++n)
                {
                    maps[n].Disable();
                    Debug.Assert(!maps[n].enabled);
                }
            }
        }

        #endregion
    }
}
