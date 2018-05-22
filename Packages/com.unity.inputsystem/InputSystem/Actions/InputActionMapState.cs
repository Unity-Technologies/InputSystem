using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Profiling;

// The current form of this is a half-way step. It successfully pulls execution state together. But it still also
// pulls in response code. We need to separate binding processing from action response.

////TODO: separate resolution from enabling such that it is possible to query controls yet not have actions/maps enabled

////TODO: add a serialized form of this and take it across domain reloads

////TODO: remove direct references to InputManager

////REVIEW: can we pack all the state that is blittable into a single chunk of unmanaged memory instead of into several managed arrays?
////        (this also means we can update more data with direct pointers)

////REVIEW: can we have a global array of InputControls?

////REVIEW: XmlDoc syntax is totally stupid in its verbosity; is there a more compact, alternative syntax we can use?

////REVIEW: can we move this from being per-actionmap to being per-action asset? I.e. work for a list of maps?

//allow setup where state monitor is enabled but action is disabled?

namespace UnityEngine.Experimental.Input
{
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
        /// Array of instantiated modifier objects.
        /// </summary>
        /// <remarks>
        /// Every binding that has modifiers corresponds to a slice of this array.
        ///
        /// Indices match between this and <see cref="modifierStates"/>.
        /// </remarks>
        public IInputBindingModifier[] modifiers;

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

        public int totalModifierCount;

        public int totalCompositeCount;

        /// <summary>
        /// State of all bindings in the action map.
        /// </summary>
        /// <remarks>
        /// This array should not require GC scanning.
        /// </remarks>
        public BindingState[] bindingStates;

        /// <summary>
        /// State of all modifiers on bindings in the action map.
        /// </summary>
        /// <remarks>
        /// Any modifier mentioned on any of the bindings gets its own execution state record
        /// in here. The modifiers for any one binding are grouped together.
        ///
        /// This array should not require GC scanning.
        /// </remarks>
        public ModifierState[] modifierStates;

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
        public TriggerState[] actionStates;

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

        private void ClaimDataFrom(InputBindingResolver resolver)
        {
            totalMapCount = resolver.totalMapCount;
            totalActionCount = resolver.totalActionCount;
            totalBindingCount = resolver.totalBindingCount;
            totalModifierCount = resolver.totalModifierCount;
            totalCompositeCount = resolver.totalCompositeCount;
            totalControlCount = resolver.totalControlCount;

            maps = resolver.maps;
            mapIndices = resolver.mapIndices;
            actionStates = resolver.actionStates;
            bindingStates = resolver.bindingStates;
            modifierStates = resolver.modifierStates;
            modifiers = resolver.modifiers;
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
                map.m_MapIndex = kInvalidIndex;

                var actions = map.m_Actions;
                if (actions != null)
                {
                    for (var n = 0; n < actions.Length; ++n)
                        actions[n].m_ActionIndex = kInvalidIndex;
                }
            }

            RemoveMapFromGlobalList();
        }

        public TriggerState FetchActionState(InputAction action)
        {
            Debug.Assert(action != null);
            Debug.Assert(action.m_ActionMap != null);
            Debug.Assert(action.m_ActionMap.m_MapIndex != kInvalidIndex);
            Debug.Assert(maps.Contains(action.m_ActionMap));
            Debug.Assert(action.m_ActionIndex >= 0 && action.m_ActionIndex < totalActionCount);

            return actionStates[action.m_ActionIndex];
        }

        public ActionMapIndices FetchMapIndices(InputActionMap map)
        {
            Debug.Assert(map != null);
            Debug.Assert(maps.Contains(map));
            return mapIndices[map.m_MapIndex];
        }

        public void EnableAllActions(InputActionMap map)
        {
            Debug.Assert(map != null);
            Debug.Assert(map.m_Actions != null);
            Debug.Assert(maps.Contains(map));

            var mapIndex = map.m_MapIndex;
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
                actionStates[actionStartIndex + i].phase = InputActionPhase.Waiting;

            NotifyListenersThatEnabledActionsChanged();
        }

        public void EnableSingleAction(InputAction action)
        {
            Debug.Assert(action != null);
            Debug.Assert(action.m_ActionMap != null);
            Debug.Assert(maps.Contains(action.m_ActionMap));

            var actionIndex = action.m_ActionIndex;
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount);

            var map = action.m_ActionMap;
            var mapIndex = map.m_MapIndex;
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
            actionStates[actionStartIndex].phase = InputActionPhase.Waiting;

            NotifyListenersThatEnabledActionsChanged();
        }

        ////TODO: need to cancel actions if they are in started state
        ////TODO: reset all modifier states

        public void DisableAllActions(InputActionMap map)
        {
            Debug.Assert(map != null);
            Debug.Assert(map.m_Actions != null);
            Debug.Assert(maps.Contains(map));

            var mapIndex = map.m_MapIndex;
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
                actionStates[actionStartIndex + i].phase = InputActionPhase.Disabled;

            NotifyListenersThatEnabledActionsChanged();
        }

        public void DisableSingleAction(InputAction action)
        {
            Debug.Assert(action != null);
            Debug.Assert(action.m_ActionMap != null);
            Debug.Assert(maps.Contains(action.m_ActionMap));

            var actionIndex = action.m_ActionIndex;
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount);

            var map = action.m_ActionMap;
            var mapIndex = map.m_MapIndex;
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
            actionStates[actionStartIndex].phase = InputActionPhase.Disabled;

            NotifyListenersThatEnabledActionsChanged();
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
        void IInputStateChangeMonitor.NotifyControlValueChanged(InputControl control, double time, long mapControlAndBindingIndex)
        {
            int controlIndex;
            int bindingIndex;
            int mapIndex;

            SplitUpMapAndControlAndBindingIndex(mapControlAndBindingIndex, out mapIndex, out controlIndex, out bindingIndex);
            ProcessControlValueChange(mapIndex, controlIndex, bindingIndex, time);
        }

        void IInputStateChangeMonitor.NotifyTimerExpired(InputControl control, double time, long mapControlAndBindingIndex, int modifierIndex)
        {
            int controlIndex;
            int bindingIndex;
            int mapIndex;

            SplitUpMapAndControlAndBindingIndex(mapControlAndBindingIndex, out mapIndex, out controlIndex, out bindingIndex);
            ProcessTimeout(time, mapIndex, controlIndex, bindingIndex, modifierIndex);
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
        /// <remarks>
        /// This is where we end up if one of the state monitors we've put in the system has triggered.
        /// From here we go back to the associated binding and then let it figure out what the state change
        /// means for it.
        /// </remarks>
        private void ProcessControlValueChange(int mapIndex, int controlIndex, int bindingIndex, double time)
        {
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount);
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount);
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);

            ////TODO: this is where we should filter out state changes that do not result in value changes

            // If we have modifiers, let them do all the processing. The precense of a modifier
            // essentially bypasses the default phase progression logic of an action.
            var modifierCount = bindingStates[bindingIndex].modifierCount;
            if (modifierCount > 0)
            {
                var context = new InputBindingModifierContext
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

                var modifierStartIndex = bindingStates[bindingIndex].modifierStartIndex;
                for (var i = 0; i < modifierCount; ++i)
                {
                    var index = modifierStartIndex + i;
                    var state = modifierStates[index];
                    var modifier = modifiers[index];

                    context.m_TriggerState.phase = state.phase;
                    context.m_TriggerState.startTime = state.startTime;
                    context.m_TriggerState.modifierIndex = index;

                    modifier.Process(ref context);
                }
            }
            else
            {
                // Default logic has no support for cancellations and won't ever go into started
                // phase. Will go from waiting straight to performed and then straight to waiting
                // again.
                //
                // Also, we perform the action on *any* value change. For buttons, this means that
                // if you use the default logic without a modifier, the action will be performed
                // both when you press and when you release the button.

                var trigger = new TriggerState
                {
                    phase = InputActionPhase.Performed,
                    mapIndex = mapIndex,
                    controlIndex = controlIndex,
                    bindingIndex = bindingIndex,
                    modifierIndex = kInvalidIndex,
                    time = time,
                    startTime = time
                };
                ChangePhaseOfAction(InputActionPhase.Performed, ref trigger);
            }
        }

        private void ProcessTimeout(double time, int mapIndex, int controlIndex, int bindingIndex, int modifierIndex)
        {
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount);
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);
            Debug.Assert(modifierIndex >= 0 && modifierIndex < totalModifierCount);

            var currentState = modifierStates[modifierIndex];

            var context = new InputBindingModifierContext
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
                    modifierIndex = modifierIndex
                },
                timerHasExpired = true,
            };

            currentState.isTimerRunning = false;
            modifierStates[modifierIndex] = currentState;

            // Let modifier handle timer expiration.
            modifiers[modifierIndex].Process(ref context);
        }

        internal void StartTimeout(float seconds, ref TriggerState trigger)
        {
            Debug.Assert(trigger.mapIndex >= 0 && trigger.mapIndex < totalMapCount);
            Debug.Assert(trigger.controlIndex >= 0 && trigger.controlIndex < totalControlCount);
            Debug.Assert(trigger.modifierIndex >= 0 && trigger.modifierIndex < totalModifierCount);

            var manager = InputSystem.s_Manager;
            var currentTime = manager.m_Runtime.currentTime;
            var control = controls[trigger.controlIndex];
            var modifierIndex = trigger.modifierIndex;
            var monitorIndex =
                ToCombinedMapAndControlAndBindingIndex(trigger.mapIndex, trigger.controlIndex, trigger.bindingIndex);

            manager.AddStateChangeMonitorTimeout(control, this, currentTime + seconds, monitorIndex,
                modifierIndex);

            // Update state.
            var modifierState = modifierStates[modifierIndex];
            modifierState.isTimerRunning = true;
            modifierStates[modifierIndex] = modifierState;
        }

        private void StopTimeout(int mapIndex, int controlIndex, int bindingIndex, int modifierIndex)
        {
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount);
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount);
            Debug.Assert(modifierIndex >= 0 && modifierIndex < totalModifierCount);

            var manager = InputSystem.s_Manager;
            var monitorIndex =
                ToCombinedMapAndControlAndBindingIndex(mapIndex, controlIndex, bindingIndex);

            manager.RemoveStateChangeMonitorTimeout(this, monitorIndex, modifierIndex);

            // Update state.
            var modifierState = modifierStates[modifierIndex];
            modifierState.isTimerRunning = false;
            modifierStates[modifierIndex] = modifierState;
        }

        /// <summary>
        /// Perform a phase change on the given modifier. Only visible to observers
        /// if it happens to change the phase of the action, too.
        /// </summary>
        /// <param name="newPhase"></param>
        /// <param name="trigger"></param>
        /// <remarks>
        /// Multiple modifiers on the same binding can be started concurrently but the
        /// first modifier that starts will get to drive an action until it either cancels
        /// or performs the action.
        ///
        /// If a modifier driving an action performs it, all modifiers will reset and
        /// go back waiting.
        ///
        /// If a modifier driving an action cancels it, the next modifier in the list which
        /// has already started will get to drive the action (example: a TapModifier and a
        /// SlowTapModifier both start and the TapModifier gets to drive the action because
        /// it comes first; then the TapModifier cancels because the button is held for too
        /// long and the SlowTapModifier will get to drive the action next).
        /// </remarks>
        internal void ChangePhaseOfModifier(InputActionPhase newPhase, ref TriggerState trigger)
        {
            var modifierIndex = trigger.modifierIndex;
            var bindingIndex = trigger.bindingIndex;

            Debug.Assert(modifierIndex >= 0 && modifierIndex < totalModifierCount);
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);

            ////TODO: need to make sure that performed and cancelled phase changes happen on the *same* binding&control
            ////      as the start of the phase

            // Update modifier state.
            ThrowIfPhaseTransitionIsInvalid(modifierStates[modifierIndex].phase, newPhase, ref trigger);
            modifierStates[modifierIndex].phase = newPhase;
            modifierStates[modifierIndex].triggerControlIndex = trigger.controlIndex;
            if (newPhase == InputActionPhase.Started)
                modifierStates[modifierIndex].startTime = trigger.time;

            ////REVIEW: If we want to defer triggering of actions, this is the point where we probably need to cut things off
            // See if it affects the phase of an associated action.
            var actionIndex = bindingStates[bindingIndex].actionIndex; // We already had to tap this array and entry in ProcessControlValueChange.
            if (actionIndex != -1)
            {
                if (actionStates[actionIndex].phase == InputActionPhase.Waiting)
                {
                    // We're the first modifier to go to the start phase.
                    ChangePhaseOfAction(newPhase, ref trigger);
                }
                else if (newPhase == InputActionPhase.Cancelled && actionStates[actionIndex].modifierIndex == trigger.modifierIndex)
                {
                    // We're cancelling but maybe there's another modifier ready
                    // to go into start phase.

                    ChangePhaseOfAction(newPhase, ref trigger);

                    var modifierStartIndex = bindingStates[bindingIndex].modifierStartIndex;
                    var numModifiers = bindingStates[bindingIndex].modifierCount;
                    for (var i = 0; i < numModifiers; ++i)
                    {
                        var index = modifierStartIndex + i;
                        if (index != trigger.modifierIndex && modifierStates[index].phase == InputActionPhase.Started)
                        {
                            var triggerForModifier = new TriggerState
                            {
                                phase = InputActionPhase.Started,
                                controlIndex = modifierStates[index].triggerControlIndex,
                                bindingIndex = trigger.bindingIndex,
                                modifierIndex = index,
                                time = trigger.time,
                                startTime = modifierStates[index].startTime
                            };
                            ChangePhaseOfAction(InputActionPhase.Started, ref triggerForModifier);
                            break;
                        }
                    }
                }
                else if (actionStates[actionIndex].modifierIndex == trigger.modifierIndex)
                {
                    // Any other phase change goes to action if we're the modifier driving
                    // the current phase.
                    ChangePhaseOfAction(newPhase, ref trigger);

                    // We're the modifier driving the action and we performed the action,
                    // so reset any other modifier to waiting state.
                    if (newPhase == InputActionPhase.Performed)
                    {
                        var modifierStartIndex = bindingStates[bindingIndex].modifierStartIndex;
                        var numModifiers = bindingStates[bindingIndex].modifierCount;
                        for (var i = 0; i < numModifiers; ++i)
                        {
                            var index = modifierStartIndex + i;
                            if (index != trigger.modifierIndex)
                                ResetModifier(trigger.mapIndex, trigger.bindingIndex, index);
                        }
                    }
                }
            }

            // If the modifier performed or cancelled, go back to waiting.
            if (newPhase == InputActionPhase.Performed || newPhase == InputActionPhase.Cancelled)
                ResetModifier(trigger.mapIndex, trigger.bindingIndex, trigger.modifierIndex);
            ////TODO: reset entire chain
        }

        // Perform a phase change on the action. Visible to observers.
        internal void ChangePhaseOfAction(InputActionPhase newPhase, ref TriggerState trigger)
        {
            Debug.Assert(trigger.mapIndex >= 0 && trigger.mapIndex < totalMapCount);
            Debug.Assert(trigger.controlIndex >= 0 && trigger.controlIndex < totalControlCount);
            Debug.Assert(trigger.bindingIndex >= 0 && trigger.bindingIndex < totalBindingCount);

            var actionIndex = bindingStates[trigger.bindingIndex].actionIndex;
            if (actionIndex == kInvalidIndex)
                return; // No action associated with binding.

            // Make sure phase progression is valid.
            var currentPhase = actionStates[actionIndex].phase;
            ThrowIfPhaseTransitionIsInvalid(currentPhase, newPhase, ref trigger);

            // Update action state.
            actionStates[actionIndex] = trigger;
            actionStates[actionIndex].phase = newPhase;

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
                    actionStates[actionIndex].phase = InputActionPhase.Waiting; // Go back to waiting after performing action.
                    break;

                case InputActionPhase.Cancelled:
                    CallActionListeners(map, ref action.m_OnCancelled, ref trigger);
                    actionStates[actionIndex].phase = InputActionPhase.Waiting; // Go back to waiting after cancelling action.
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
                m_ModifierIndex = trigger.modifierIndex,
                m_ControlIndex = trigger.controlIndex,
            };

            Profiler.BeginSample("InputActionCallback");

            // Run callbacks (if any) directly on action.
            var listenerCount = listeners.length;
            for (var i = 0; i < listenerCount; ++i)
                listeners[i](context);

            // Run callbacks (if any) on action map.
            var listenerCountOnMap = callbacksOnMap.length;
            for (var i = 0; i < listenerCountOnMap; ++i)
                callbacksOnMap[i].OnActionTriggered(ref context);

            Profiler.EndSample();
        }

        private void ThrowIfPhaseTransitionIsInvalid(InputActionPhase currentPhase, InputActionPhase newPhase, ref TriggerState trigger)
        {
            // Can only go to Started from Waiting.
            if (newPhase == InputActionPhase.Started && currentPhase != InputActionPhase.Waiting)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' (action: {3}, modifier: {4})",
                        currentPhase, InputActionPhase.Started, InputActionPhase.Waiting,
                        GetActionOrNoneString(ref trigger), GetModifierOrNull(ref trigger)));

            // Can only go to Performed from Waiting or Started.
            if (newPhase == InputActionPhase.Performed && currentPhase != InputActionPhase.Waiting &&
                currentPhase != InputActionPhase.Started)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' or '{3}' (action: {4}, modifier: {5})",
                        currentPhase, InputActionPhase.Performed, InputActionPhase.Waiting, InputActionPhase.Started,
                        GetActionOrNoneString(ref trigger),
                        GetModifierOrNull(ref trigger)));

            // Can only go to Cancelled from Started.
            if (newPhase == InputActionPhase.Cancelled && currentPhase != InputActionPhase.Started)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' (action: {3}, modifier: {4})",
                        currentPhase, InputActionPhase.Cancelled, InputActionPhase.Started,
                        GetActionOrNoneString(ref trigger), GetModifierOrNull(ref trigger)));
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

        private IInputBindingModifier GetModifierOrNull(ref TriggerState trigger)
        {
            if (trigger.modifierIndex == kInvalidIndex)
                return null;

            Debug.Assert(trigger.modifierIndex >= 0 && trigger.modifierIndex < totalModifierCount);
            return modifiers[trigger.modifierIndex];
        }

        private void ResetModifier(int mapIndex, int bindingIndex, int modifierIndex)
        {
            Debug.Assert(modifierIndex >= 0 && modifierIndex < totalModifierCount);
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount);

            modifiers[modifierIndex].Reset();

            if (modifierStates[modifierIndex].isTimerRunning)
            {
                var controlIndex = modifierStates[modifierIndex].triggerControlIndex;
                StopTimeout(mapIndex, controlIndex, bindingIndex, modifierIndex);
            }

            modifierStates[modifierIndex] =
                new ModifierState
            {
                phase = InputActionPhase.Waiting
            };
        }

        /// <summary>
        /// Records the current state of a single modifier attached to a binding.
        /// Each modifier keeps track of its own trigger control and phase progression.
        /// </summary>
        internal struct ModifierState
        {
            public int triggerControlIndex;
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

            public InputActionPhase phase
            {
                // We store the phase in the low 8 bits of the flags field.
                get { return (InputActionPhase)((int)flags & 0xf); }
                set { flags = (Flags)(((uint)flags & 0xfffffff0) | (uint)value); }
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
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        internal struct BindingState
        {
            [FieldOffset(0)] private byte m_ControlCount;
            [FieldOffset(1)] private byte m_ModifierCount;
            [FieldOffset(2)] private byte m_MapIndex;
            [FieldOffset(3)] private byte m_Flags;
            [FieldOffset(4)] private ushort m_ActionIndex;
            [FieldOffset(6)] private ushort m_CompositeIndex;

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
            [FieldOffset(8)] public int controlStartIndex;

            /// <summary>
            /// Number of controls associated with this binding.
            /// </summary>
            public int controlCount
            {
                get { return m_ControlCount; }
                set { m_ControlCount = (byte)value; }
            }

            /// <summary>
            /// Number of modifiers associated with this binding.
            /// </summary>
            public int modifierCount
            {
                get { return m_ModifierCount; }
                set { m_ModifierCount = (byte)value; }
            }

            /// <summary>
            /// Index into <see cref="modifierStates"/> of first modifier associated with the binding.
            /// </summary>
            [FieldOffset(12)] public int modifierStartIndex;

            /// <summary>
            /// Index of the action being triggered by the binding (if any).
            /// </summary>
            /// <remarks>
            /// For bindings that don't trigger actions, this is <c>-1</c>.
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
            /// The composite that the binding is part of (if any).
            /// </summary>
            public int compositeIndex
            {
                get
                {
                    if (m_CompositeIndex == ushort.MaxValue)
                        return kInvalidIndex;
                    return m_CompositeIndex;
                }
                set
                {
                    if (value == kInvalidIndex)
                        m_CompositeIndex = ushort.MaxValue;
                    else
                        m_CompositeIndex = (ushort)value;
                }
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
        public struct TriggerState
        {
            private short m_Phase;
            private short m_MapIndex;

            /// <summary>
            /// Phase being triggered by the control value change.
            /// </summary>
            public InputActionPhase phase
            {
                get { return (InputActionPhase)m_Phase; }
                set { m_Phase = (short)value; }
            }

            /// <summary>
            /// The time the binding got triggered.
            /// </summary>
            public double time;

            public double startTime;

            public int mapIndex
            {
                get { return m_MapIndex; }
                set { m_MapIndex = (short)value; }
            }

            public int controlIndex;

            /// <summary>
            /// Index into <see cref="bindingStates"/> for the binding that triggered.
            /// </summary>
            public int bindingIndex;

            /// <summary>
            /// Index into <see cref="modifierStates"/> for the modifier that triggered.
            /// </summary>
            public int modifierIndex;
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
            public int modifierStartIndex;
            public int modifierCount;
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

        #if UNITY_EDITOR
        internal static InlinedArray<Action> s_OnEnabledActionsChanged;
        #endif

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

        internal static void NotifyListenersThatEnabledActionsChanged()
        {
            #if UNITY_EDITOR
            for (var i = 0; i < s_OnEnabledActionsChanged.length; ++i)
                s_OnEnabledActionsChanged[i]();
            #endif
        }

        /// <summary>
        /// Nuke global state we have to keep track of action map states.
        /// </summary>
        internal static void ResetGlobals()
        {
            for (var i = 0; i < s_GlobalList.length; ++i)
                s_GlobalList[i].Free();
            s_GlobalList.length = 0;
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
                        var actionStartIndex = state.mapIndices[map.m_MapIndex].actionStartIndex;
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

                // Re-resolve all maps in the state.
                var resolver = new InputBindingResolver();
                for (var n = 0; n < mapCount; ++n)
                {
                    var map = maps[n];
                    map.m_MapIndex = kInvalidIndex;
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
                    Debug.Assert(maps[n].m_MapIndex == n);
                    if (!maps[n].enabled)
                        continue;

                    var controlCount = mapIndices[n].controlCount;
                    if (controlCount > 0)
                        state.DisableControls(n, mapIndices[n].controlStartIndex, controlCount);
                }

                var oldActionStates = state.actionStates;

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
                var newActionStates = state.actionStates;
                for (var n = 0; n < state.totalActionCount; ++n)
                {
                    ////TODO: we want to preserve as much state as we can here, not just lose all current execution state of the maps
                    if (oldActionStates[n].phase != InputActionPhase.Disabled)
                        newActionStates[n].phase = InputActionPhase.Waiting;
                }

                // Restore state change monitors.
                for (var n = 0; n < state.totalControlCount; ++n)
                {
                    var bindingIndex = state.controlIndexToBindingIndex[n];
                    var actionIndex = state.bindingStates[bindingIndex].actionIndex;
                    if (actionIndex == kInvalidIndex)
                        continue;

                    if (oldActionStates[actionIndex].phase != InputActionPhase.Disabled)
                        state.EnableControls(oldActionStates[actionIndex].mapIndex,
                            state.bindingStates[bindingIndex].controlStartIndex,
                            state.bindingStates[bindingIndex].controlCount);
                }
            }

            NotifyListenersThatEnabledActionsChanged();
        }

        private bool DataMatches(InputBindingResolver resolver)
        {
            if (totalMapCount == resolver.totalMapCount
                || totalActionCount != resolver.totalActionCount
                || totalBindingCount != resolver.totalBindingCount
                || totalCompositeCount != resolver.totalCompositeCount
                || totalControlCount != resolver.totalControlCount
                || totalModifierCount != resolver.totalModifierCount)
                return false;

            if (!ArrayHelpers.HaveEqualElements(maps, resolver.maps)
                || !ArrayHelpers.HaveEqualElements(controls, resolver.controls)
                || !ArrayHelpers.HaveEqualElements(modifiers, resolver.modifiers)
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
                    maps[n].Disable();
            }

            NotifyListenersThatEnabledActionsChanged();
        }

        #endregion
    }
}
