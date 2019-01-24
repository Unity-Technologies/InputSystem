using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Profiling;

////TODO: rename to just InputActionState?

////TODO: when control is actuated and initiates an interaction, lock the interaction to the control until the interaction is complete or cancelled

////TODO: add a serialized form of this and take it across domain reloads

////TODO: remove direct references to InputManager

////TODO: make sure controls in per-action and per-map control arrays are unique (the internal arrays are probably okay to have duplicates)

////REVIEW: can we pack all the state that is blittable into a single chunk of unmanaged memory instead of into several managed arrays?
////        (this also means we can update more data with direct pointers)

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
    internal class InputActionMapState : IInputStateChangeMonitor, ICloneable
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

        public InputProcessor[] processors;

        /// <summary>
        /// Array of instantiated composite objects.
        /// </summary>
        public InputBindingComposite[] composites;

        public int totalMapCount;

        public int totalActionCount;

        public int totalBindingCount;

        /// <summary>
        /// Total number of controls resolved from bindings of all action maps
        /// added to the state.
        /// </summary>
        public int totalControlCount;

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
        public TriggerState[] actionStates;

        public int[] controlIndexToBindingIndex;

        private Action<InputUpdateType> m_OnBeforeUpdateDelegate;
        private Action<InputUpdateType> m_OnAfterUpdateDelegate;
        private bool m_OnBeforeUpdateHooked;
        private bool m_OnAfterUpdateHooked;

        private int m_ContinuousActionCount;
        private int m_ContinuousActionCountFromPreviousUpdate;
        private int[] m_ContinuousActions;

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

            maps = resolver.maps;
            mapIndices = resolver.mapIndices;
            actionStates = resolver.actionStates;
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
                Debug.Assert(!map.enabled, "Cannot destroy action map state while a map in the state is still enabled");
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

        /// <summary>
        /// Create a copy of the state.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The copy is non-functional in so far as it cannot be used to keep track of changes made to
        /// any associated actions. However, it can be used to freeze the binding resolution state of
        /// a particular set of enabled actions. This is used by <see cref="InputActionTrace"/>.
        /// </remarks>
        public InputActionMapState Clone()
        {
            return new InputActionMapState
            {
                maps = ArrayHelpers.Copy(maps),
                mapIndices = ArrayHelpers.Copy(mapIndices),
                controls = ArrayHelpers.Copy(controls),
                interactions = ArrayHelpers.Copy(interactions),
                processors = ArrayHelpers.Copy(processors),
                composites = ArrayHelpers.Copy(composites),
                totalMapCount = totalMapCount,
                totalActionCount = totalActionCount,
                totalBindingCount = totalBindingCount,
                totalControlCount = totalControlCount,
                totalInteractionCount = totalInteractionCount,
                totalProcessorCount = totalProcessorCount,
                totalCompositeCount = totalCompositeCount,
                bindingStates = ArrayHelpers.Copy(bindingStates),
                interactionStates = ArrayHelpers.Copy(interactionStates),
                actionStates = ArrayHelpers.Copy(actionStates),
                controlIndexToBindingIndex = ArrayHelpers.Copy(controlIndexToBindingIndex),
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Reset the trigger state of the given action such that the action has no record of being triggered.
        /// </summary>
        /// <param name="actionIndex">Action whose state to reset.</param>
        /// <param name="toPhase">Phase to reset the action to. Must be either <see cref="InputActionPhase.Waiting"/>
        /// or <see cref="InputActionPhase.Disabled"/>. Other phases cannot be transitioned to through resets.</param>
        public void ResetActionState(int actionIndex, InputActionPhase toPhase = InputActionPhase.Waiting)
        {
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Action index out of range");
            Debug.Assert(toPhase == InputActionPhase.Waiting || toPhase == InputActionPhase.Disabled,
                "Phase must be Waiting or Disabled");

            // If the action in started or performed phase, cancel it first.
            if (actionStates[actionIndex].phase != InputActionPhase.Waiting)
            {
                // Cancellation calls should receive current time.
                actionStates[actionIndex].time = InputRuntime.s_Instance.currentTime;

                // If the action got triggered from an interaction, go and reset all interactions on the binding
                // that got triggered.
                if (actionStates[actionIndex].interactionIndex != kInvalidIndex)
                {
                    var bindingIndex = actionStates[actionIndex].bindingIndex;
                    if (bindingIndex != kInvalidIndex)
                    {
                        var mapIndex = actionStates[actionIndex].mapIndex;
                        var interactionCount = bindingStates[bindingIndex].interactionCount;
                        var interactionStartIndex = bindingStates[bindingIndex].interactionStartIndex;

                        for (var i = 0; i < interactionCount; ++i)
                        {
                            var interactionIndex = interactionStartIndex + i;
                            ResetInteractionStateAndCancelIfNecessary(mapIndex, bindingIndex, interactionIndex);
                        }
                    }
                }
                else
                {
                    // No interactions. Cancel the action directly.

                    Debug.Assert(actionStates[actionIndex].bindingIndex != kInvalidIndex, "Binding index on trigger state is invalid");
                    Debug.Assert(bindingStates[actionStates[actionIndex].bindingIndex].interactionCount == 0,
                        "Action has been triggered but apparently not from an interaction yet there's interactions on the binding that got triggered?!?");

                    ChangePhaseOfAction(InputActionPhase.Cancelled, ref actionStates[actionIndex]);
                }
            }

            // Wipe state.
            var state = actionStates[actionIndex];
            state.phase = toPhase;
            state.controlIndex = kInvalidIndex;
            state.bindingIndex = 0;
            state.interactionIndex = kInvalidIndex;
            state.startTime = 0;
            state.time = 0;
            actionStates[actionIndex] = state;

            // Remove if currently on the list of continuous actions.
            if (state.continuous)
            {
                var continuousIndex = ArrayHelpers.IndexOf(m_ContinuousActions, actionIndex, count: m_ContinuousActionCount);
                if (continuousIndex != -1)
                    ArrayHelpers.EraseAtByMovingTail(m_ContinuousActions, ref m_ContinuousActionCount, continuousIndex);
            }
        }

        public ref TriggerState FetchActionState(InputAction action)
        {
            Debug.Assert(action != null, "Action must not be null");
            Debug.Assert(action.m_ActionMap != null, "Action must have an action map");
            Debug.Assert(action.m_ActionMap.m_MapIndexInState != kInvalidIndex, "Action must have index set");
            Debug.Assert(maps.Contains(action.m_ActionMap), "Action map must be contained in state");
            Debug.Assert(action.m_ActionIndex >= 0 && action.m_ActionIndex < totalActionCount, "Action index is out of range");

            return ref actionStates[action.m_ActionIndex];
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

            EnableControls(map);

            // Put all actions into waiting state.
            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount);
            var actionCount = mapIndices[mapIndex].actionCount;
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;
            for (var i = 0; i < actionCount; ++i)
            {
                var actionIndex = actionStartIndex + i;
                actionStates[actionIndex].phase = InputActionPhase.Waiting;
            }

            HookOnBeforeUpdate();
            NotifyListenersOfActionChange(InputActionChange.ActionMapEnabled, map);
        }

        private void EnableControls(InputActionMap map)
        {
            Debug.Assert(map != null, "Map must not be null");
            Debug.Assert(map.m_Actions != null, "Map must have actions");
            Debug.Assert(maps.Contains(map), "Map must be contained in state");

            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount);

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
            var actionIndex = action.m_ActionIndex;
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Action index out of range");
            actionStates[actionIndex].phase = InputActionPhase.Waiting;

            HookOnBeforeUpdate();
            NotifyListenersOfActionChange(InputActionChange.ActionEnabled, action);
        }

        private void EnableControls(InputAction action)
        {
            Debug.Assert(action != null, "Action must not be null");
            Debug.Assert(action.m_ActionMap != null, "Action must have action map");
            Debug.Assert(maps.Contains(action.m_ActionMap), "Map must be contained in state");

            var actionIndex = action.m_ActionIndex;
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Action index out of range");

            var map = action.m_ActionMap;
            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range");

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
        }

        ////TODO: need to cancel actions if they are in started state
        ////TODO: reset all interaction states

        public void DisableAllActions(InputActionMap map)
        {
            Debug.Assert(map != null, "Map must not be null");
            Debug.Assert(map.m_Actions != null, "Map must have actions");
            Debug.Assert(maps.Contains(map), "Map must be contained in state");

            DisableControls(map);

            // Mark all actions as disabled.
            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range");
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;
            var actionCount = mapIndices[mapIndex].actionCount;
            for (var i = 0; i < actionCount; ++i)
            {
                var actionIndex = actionStartIndex + i;
                if (actionStates[actionIndex].phase != InputActionPhase.Disabled)
                    ResetActionState(actionIndex, toPhase: InputActionPhase.Disabled);
            }

            NotifyListenersOfActionChange(InputActionChange.ActionMapDisabled, map);
        }

        /// <summary>
        /// Disable all actions in the given map that are currently enabled such that they can be
        /// re-enabled with <see cref="ReenableTemporarilyDisabledActions"/>.
        /// </summary>
        /// <param name="map"></param>
        /// <remarks>
        /// When re-resolving bindings while an action map is enabled, we temporarily have to disable
        /// </remarks>
        public void TemporarilyDisableActions(InputActionMap map)
        {
            Debug.Assert(map != null, "Map must not be null");
            Debug.Assert(maps.Contains(map), "Map must be contained in state");

            // Nothing to do if there's no enabled actions in the map.
            var numEnabledActions = map.m_EnabledActionsCount;
            if (numEnabledActions <= 0)
                return;

            UnhookOnBeforeUpdate();

            var mapIndex = map.m_MapIndexInState;
            var actionCount = mapIndices[mapIndex].actionCount;
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;

            if (numEnabledActions == actionCount)
            {
                // Cancel any action that isn't waiting.
                for (var n = 0; n < actionCount; ++n)
                {
                    var actionIndex = actionStartIndex + n;
                    var phase = actionStates[actionIndex].phase;
                    if (phase == InputActionPhase.Started || phase == InputActionPhase.Performed)
                        ResetActionState(actionIndex);
                }

                DisableControls(map);
            }
            else
            {
                var actions = map.m_Actions;
                Debug.Assert(actions != null, "Map has no actions");

                for (var n = 0; n < actionCount; ++n)
                {
                    var actionIndex = actionStartIndex + n;
                    var phase = actionStates[actionIndex].phase;

                    if (phase == InputActionPhase.Disabled)
                        continue;

                    var action = actions[n];
                    action.m_NeedsReEnabling = true;

                    // Cancel action if it isn't waiting.
                    if (phase == InputActionPhase.Started || phase == InputActionPhase.Performed)
                        ResetActionState(actionIndex);

                    DisableControls(action);
                }
            }
        }

        public void ReenableTemporarilyDisabledActions(InputActionMap map)
        {
            Debug.Assert(map != null, "Map must not be null");
            Debug.Assert(maps.Contains(map), "Map must be contained in state");

            // Nothing to do if there's no enabled actions in the map.
            var numEnabledActions = map.m_EnabledActionsCount;
            if (numEnabledActions <= 0)
                return;

            var mapIndex = map.m_MapIndexInState;
            var actionCount = mapIndices[mapIndex].actionCount;
            var actionStartIndex = mapIndices[mapIndex].actionStartIndex;

            // The actions we enable controls on here need an initial state check in
            // OnBeforeInitialUpdate().
            HookOnBeforeUpdate();

            if (numEnabledActions == actionCount)
            {
                EnableControls(map);

                // Put all actions into waiting state.
                for (var n = 0; n < actionCount; ++n)
                {
                    var actionIndex = actionStartIndex + n;
                    actionStates[actionIndex].phase = InputActionPhase.Waiting;
                }
            }
            else
            {
                var actions = map.m_Actions;
                Debug.Assert(actions != null, "Map has no actions");

                for (var n = 0; n < actionCount; ++n)
                {
                    var action = actions[n];
                    if (!action.m_NeedsReEnabling)
                        continue;

                    EnableControls(action);
                    actionStates[actionStartIndex + n].phase = InputActionPhase.Waiting;
                    action.m_NeedsReEnabling = false;
                }
            }
        }

        private void DisableControls(InputActionMap map)
        {
            Debug.Assert(map != null, "Map must not be null");
            Debug.Assert(map.m_Actions != null, "Map must have actions");
            Debug.Assert(maps.Contains(map), "Map must be contained in state");

            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range");

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
            ResetActionState(action.m_ActionIndex, toPhase: InputActionPhase.Disabled);
            NotifyListenersOfActionChange(InputActionChange.ActionDisabled, action);
        }

        private void DisableControls(InputAction action)
        {
            Debug.Assert(action != null, "Action must not be null");
            Debug.Assert(action.m_ActionMap != null, "Action must have action map");
            Debug.Assert(maps.Contains(action.m_ActionMap), "Action map must be contained in state");

            var actionIndex = action.m_ActionIndex;
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Action index out of range");

            var map = action.m_ActionMap;
            var mapIndex = map.m_MapIndexInState;
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range");

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
        }

        ////REVIEW: can we have a method on InputManager doing this in bulk?

        private void EnableControls(int mapIndex, int controlStartIndex, int numControls)
        {
            Debug.Assert(controls != null, "State must have controls");
            Debug.Assert(controlStartIndex >= 0 && controlStartIndex < totalControlCount, "Control start index out of range");
            Debug.Assert(controlStartIndex + numControls <= totalControlCount, "Control range out of bounds");

            var manager = InputSystem.s_Manager;
            for (var i = 0; i < numControls; ++i)
            {
                var controlIndex = controlStartIndex + i;
                var bindingIndex = controlIndexToBindingIndex[controlIndex];
                var mapControlAndBindingIndex = ToCombinedMapAndControlAndBindingIndex(mapIndex, controlIndex, bindingIndex);

                bindingStates[bindingIndex].needsInitialStateCheck = true;
                manager.AddStateChangeMonitor(controls[controlIndex], this, mapControlAndBindingIndex);
            }
        }

        private void DisableControls(int mapIndex, int controlStartIndex, int numControls)
        {
            Debug.Assert(controls != null, "State must have controls");
            Debug.Assert(controlStartIndex >= 0 && controlStartIndex < totalControlCount, "Control start index out of range");
            Debug.Assert(controlStartIndex + numControls <= totalControlCount, "Control range out of bounds");

            var manager = InputSystem.s_Manager;
            for (var i = 0; i < numControls; ++i)
            {
                var controlIndex = controlStartIndex + i;
                var bindingIndex = controlIndexToBindingIndex[controlIndex];
                var mapControlAndBindingIndex = ToCombinedMapAndControlAndBindingIndex(mapIndex, controlIndex, bindingIndex);

                bindingStates[bindingIndex].needsInitialStateCheck = false;
                manager.RemoveStateChangeMonitor(controls[controlIndex], this, mapControlAndBindingIndex);
            }
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
        private unsafe void OnBeforeInitialUpdate(InputUpdateType type)
        {
            ////TODO: deal with update type

            // Remove us from the callback.
            UnhookOnBeforeUpdate();

            Profiler.BeginSample("InitialActionStateCheck");

            // The composite logic relies on the event ID to determine whether a composite binding should trigger again
            // when already triggered. Make up a fake event with just an ID.
            var inputEvent = new InputEvent {eventId = 1234};
            var eventPtr = new InputEventPtr(&inputEvent);

            // Use current time as time of control state change.
            var time = InputRuntime.s_Instance.currentTime;

            ////REVIEW: should we store this data in a separate place rather than go through all bindingStates?

            // Go through all binding states and for every binding that needs an initial state check,
            // go through all bound controls and for each one that isn't in its default state, pretend
            // that the control just got actuated.
            for (var bindingIndex = 0; bindingIndex < totalBindingCount; ++bindingIndex)
            {
                if (!bindingStates[bindingIndex].needsInitialStateCheck)
                    continue;

                bindingStates[bindingIndex].needsInitialStateCheck = false;

                var mapIndex = actionStates[bindingIndex].mapIndex;
                var controlStartIndex = bindingStates[bindingIndex].controlStartIndex;
                var controlCount = bindingStates[bindingIndex].controlCount;

                for (var n = 0; n < controlCount; ++n)
                {
                    var controlIndex = controlStartIndex + n;
                    var control = controls[controlIndex];

                    if (!control.CheckStateIsAtDefault())
                        ProcessControlStateChange(mapIndex, controlIndex, bindingIndex, time, eventPtr);
                }
            }

            Profiler.EndSample();
        }

        private void OnAfterUpdateProcessContinuousActions(InputUpdateType updateType)
        {
            ////TODO: handle update type

            // Everything that is still on the list of continuous actions at the end of a
            // frame either got there during the frame or is there still from the last frame
            // (meaning the action didn't get any input this frame). Continuous actions added
            // this update will all have been added to the end of the array so we know that
            // everything in between #0 and m_ContinuousActionCountFromPreviousUpdate is
            // continuous actions left from the previous update.

            var time = InputRuntime.s_Instance.currentTime;
            for (var i = 0; i < m_ContinuousActionCountFromPreviousUpdate; ++i)
            {
                var actionIndex = m_ContinuousActions[i];
                Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Action index out of range");

                var currentPhase = actionStates[actionIndex].phase;
                Debug.Assert(currentPhase == InputActionPhase.Started || currentPhase == InputActionPhase.Performed,
                    "Current phase must be Started or Performed");

                // Trigger the action and go back to its current phase (may be
                actionStates[actionIndex].time = time;
                ChangePhaseOfAction(InputActionPhase.Performed, ref actionStates[actionIndex],
                    phaseAfterPerformedOrCancelled: currentPhase);
            }

            // All actions that are currently in the list become the actions we update by default
            // on the next update. If events come in during the next update, the action will be
            // moved out of there using DontTriggerContinuousActionThisUpdate().
            m_ContinuousActionCountFromPreviousUpdate = m_ContinuousActionCount;
        }

        /// <summary>
        /// Add an action to the list of actions we trigger every frame.
        /// </summary>
        /// <param name="actionIndex">Index of the action in <see cref="actionStates"/>.</param>
        private void AddContinuousAction(int actionIndex)
        {
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Action index out of range");
            Debug.Assert(!actionStates[actionIndex].onContinuousList, "Action is already in list");
            Debug.Assert(
                ArrayHelpers.IndexOfValue(m_ContinuousActions, actionIndex, startIndex: 0, count: m_ContinuousActionCount) == -1,
                "Action is already on list of continuous actions");

            ArrayHelpers.AppendWithCapacity(ref m_ContinuousActions, ref m_ContinuousActionCount, actionIndex);
            actionStates[actionIndex].onContinuousList = true;

            // Hook into `onAfterUpdate` if we haven't already.
            if (!m_OnAfterUpdateHooked)
            {
                if (m_OnAfterUpdateDelegate == null)
                    m_OnAfterUpdateDelegate = OnAfterUpdateProcessContinuousActions;
                InputSystem.s_Manager.onAfterUpdate += m_OnAfterUpdateDelegate;
                m_OnAfterUpdateHooked = true;
            }
        }

        /// <summary>
        /// Remove an action from the list of actions we trigger every frame.
        /// </summary>
        /// <param name="actionIndex"></param>
        private void RemoveContinuousAction(int actionIndex)
        {
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Action index out of range");
            Debug.Assert(actionStates[actionIndex].onContinuousList, "Action not in list");
            Debug.Assert(
                ArrayHelpers.IndexOfValue(m_ContinuousActions, actionIndex, startIndex: 0, count: m_ContinuousActionCount) != -1,
                "Action is not currently in list of continuous actions");
            Debug.Assert(m_ContinuousActionCount > 0, "List of continuous actions is empty");

            var index = ArrayHelpers.IndexOfValue(m_ContinuousActions, actionIndex, startIndex: 0,
                count: m_ContinuousActionCount);
            Debug.Assert(index != -1, "Action not found in list of continuous actions");

            ArrayHelpers.EraseAtWithCapacity(ref m_ContinuousActions, ref m_ContinuousActionCount, index);
            actionStates[actionIndex].onContinuousList = false;

            // If the action was in the part of the list that continuous actions we have carried
            // over from the previous update, adjust for having removed a value there.
            if (index < m_ContinuousActionCountFromPreviousUpdate)
                --m_ContinuousActionCountFromPreviousUpdate;

            // Unhook from `onAfterUpdate` if we don't need it anymore.
            if (m_ContinuousActionCount == 0 && m_OnAfterUpdateHooked)
            {
                InputSystem.s_Manager.onAfterUpdate -= m_OnAfterUpdateDelegate;
                m_OnAfterUpdateHooked = false;
            }
        }

        private void DontTriggerContinuousActionThisUpdate(int actionIndex)
        {
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Index out of range");
            Debug.Assert(actionStates[actionIndex].onContinuousList, "Action not in list");
            Debug.Assert(
                ArrayHelpers.IndexOfValue(m_ContinuousActions, actionIndex, startIndex: 0, count: m_ContinuousActionCount) != -1,
                "Action is not currently in list of continuous actions");
            Debug.Assert(m_ContinuousActionCount > 0, "List of continuous actions is empty");

            // Check if the action is within the beginning section of the list of actions that we need to check at
            // the end of the current update. If so, move it out of there.
            var index = ArrayHelpers.IndexOfValue(m_ContinuousActions, actionIndex, startIndex: 0,
                count: m_ContinuousActionCount);
            if (index < m_ContinuousActionCountFromPreviousUpdate)
            {
                // Move to end of list.
                ArrayHelpers.EraseAtWithCapacity(ref m_ContinuousActions, ref m_ContinuousActionCount, index);
                --m_ContinuousActionCountFromPreviousUpdate;
                ArrayHelpers.AppendWithCapacity(ref m_ContinuousActions, ref m_ContinuousActionCount, actionIndex);
            }
        }

        // Called from InputManager when one of our state change monitors has fired.
        // Tells us the time of the change *according to the state events coming in*.
        // Also tells us which control of the controls we are binding to triggered the
        // change and relays the binding index we gave it when we called AddStateChangeMonitor.
        void IInputStateChangeMonitor.NotifyControlStateChanged(InputControl control, double time,
            InputEventPtr eventPtr, long mapControlAndBindingIndex)
        {
            SplitUpMapAndControlAndBindingIndex(mapControlAndBindingIndex, out var mapIndex, out var controlIndex, out var bindingIndex);
            ProcessControlStateChange(mapIndex, controlIndex, bindingIndex, time, eventPtr);
        }

        void IInputStateChangeMonitor.NotifyTimerExpired(InputControl control, double time,
            long mapControlAndBindingIndex, int interactionIndex)
        {
            SplitUpMapAndControlAndBindingIndex(mapControlAndBindingIndex, out var mapIndex, out var controlIndex, out var bindingIndex);
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
        ///
        /// Note that we get called for any change in state.
        /// </remarks>
        private unsafe void ProcessControlStateChange(int mapIndex, int controlIndex, int bindingIndex, double time, InputEventPtr eventPtr)
        {
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range");
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index out of range");
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");

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
                    ProcessDefaultInteraction(mapIndex, controlIndex, bindingIndex, time);
                }

                // If the associated action is continuous and is currently on the list to get triggered
                // this update, move it to the set of continuous actions we do NOT trigger this update.
                var actionIndex = bindingStatePtr->actionIndex;
                if (actionIndex != kInvalidIndex && actionStates[actionIndex].onContinuousList)
                    DontTriggerContinuousActionThisUpdate(actionIndex);
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
        private static unsafe bool ShouldIgnoreStateChangeOnCompositeBinding(BindingState* binding, InputEvent* eventPtr)
        {
            if (eventPtr == null)
                return false;

            var eventId = eventPtr->eventId;
            if (binding->triggerEventIdForComposite == eventId)
                return true;

            binding->triggerEventIdForComposite = eventId;
            return false;
        }

        /// <summary>
        /// When there is no interaction on an action, this method perform the default interaction logic that we
        /// run when a bound control changes value.
        /// </summary>
        /// <param name="mapIndex"></param>
        /// <param name="controlIndex"></param>
        /// <param name="bindingIndex"></param>
        /// <param name="time"></param>
        private void ProcessDefaultInteraction(int mapIndex, int controlIndex, int bindingIndex, double time)
        {
            var actionIndex = bindingStates[bindingIndex].actionIndex;
            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Action index out of range");

            var continuous = actionStates[actionIndex].continuous;
            var trigger = new TriggerState
            {
                mapIndex = mapIndex,
                controlIndex = controlIndex,
                bindingIndex = bindingIndex,
                interactionIndex = kInvalidIndex,
                time = time,
                startTime = time,
                continuous = continuous,
            };

            switch (actionStates[actionIndex].phase)
            {
                case InputActionPhase.Waiting:
                {
                    // Ignore if the control has crossed its actuation threshold.
                    if (!IsActuated(bindingIndex, controlIndex))
                        return;

                    // Go into started, then perform and then go back to started.
                    ChangePhaseOfAction(InputActionPhase.Started, ref trigger);
                    ChangePhaseOfAction(InputActionPhase.Performed, ref trigger,
                        phaseAfterPerformedOrCancelled: InputActionPhase.Started);
                    break;
                }

                case InputActionPhase.Started:
                {
                    if (!IsActuated(bindingIndex, controlIndex))
                    {
                        // Control went back to below actuation threshold. Cancel interaction.
                        ChangePhaseOfAction(InputActionPhase.Cancelled, ref trigger);
                    }
                    else
                    {
                        // Control changed value above magnitude threshold. Perform and remain started.
                        ChangePhaseOfAction(InputActionPhase.Performed, ref trigger,
                            phaseAfterPerformedOrCancelled: InputActionPhase.Started);
                    }
                    break;
                }

                default:
                    Debug.Assert(false, "Should not get here");
                    break;
            }
        }

        private void ProcessInteractions(int mapIndex, int controlIndex, int bindingIndex, double time, int interactionStartIndex, int interactionCount)
        {
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range");
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index out of range");
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");

            var actionIndex = bindingStates[bindingIndex].actionIndex;
            var context = new InputInteractionContext
            {
                m_State = this,
                m_TriggerState = new TriggerState
                {
                    time = time,
                    mapIndex = mapIndex,
                    bindingIndex = bindingIndex,
                    controlIndex = controlIndex,
                    continuous = actionStates[actionIndex].continuous,
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
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index out of range");
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");
            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount, "Interaction index out of range");

            var currentState = interactionStates[interactionIndex];
            var actionIndex = bindingStates[bindingIndex].actionIndex;

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
                    continuous = actionStates[actionIndex].continuous,
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
            if (interactionStates[interactionIndex].isTimerRunning)
                StopTimeout(trigger.mapIndex, trigger.controlIndex, trigger.bindingIndex, interactionIndex);

            // Add new timeout.
            manager.AddStateChangeMonitorTimeout(control, this, currentTime + seconds, monitorIndex,
                interactionIndex);

            // Update state.
            var interactionState = interactionStates[interactionIndex];
            interactionState.isTimerRunning = true;
            interactionStates[interactionIndex] = interactionState;
        }

        private void StopTimeout(int mapIndex, int controlIndex, int bindingIndex, int interactionIndex)
        {
            Debug.Assert(mapIndex >= 0 && mapIndex < totalMapCount, "Map index out of range");
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index out of range");
            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount, "Interaction index out of range");

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
        /// <param name="phaseAfterPerformed">If <paramref name="newPhase"/> is <see cref="InputActionPhase.Performed"/>,
        /// this determines which phase to transition to after the action has been performed. This would usually be
        /// <see cref="InputActionPhase.Waiting"/> (default), <see cref="InputActionPhase.Started"/> (if the action is supposed
        /// to be oscillate between started and performed), or <see cref="InputActionPhase.Performed"/> (if the action is
        /// supposed to perform over and over again until cancelled).</param>
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
            InputActionPhase phaseAfterPerformed = InputActionPhase.Waiting)
        {
            var interactionIndex = trigger.interactionIndex;
            var bindingIndex = trigger.bindingIndex;

            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount, "Interaction index out of range");
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");

            ////TODO: need to make sure that performed and cancelled phase changes happen on the *same* binding&control
            ////      as the start of the phase

            // Any time an interaction changes phase, we cancel all pending timeouts.
            if (interactionStates[interactionIndex].isTimerRunning)
                StopTimeout(trigger.mapIndex, trigger.controlIndex, trigger.bindingIndex, trigger.interactionIndex);

            // Update interaction state.
            interactionStates[interactionIndex].phase = newPhase;
            interactionStates[interactionIndex].triggerControlIndex = trigger.controlIndex;
            if (newPhase == InputActionPhase.Started)
                interactionStates[interactionIndex].startTime = trigger.time;

            ////REVIEW: If we want to defer triggering of actions, this is the point where we probably need to cut things off
            // See if it affects the phase of an associated action.
            var actionIndex = bindingStates[bindingIndex].actionIndex; // We already had to tap this array and entry in ProcessControlStateChange.
            if (actionIndex != -1)
            {
                if (actionStates[actionIndex].phase == InputActionPhase.Waiting)
                {
                    // We're the first interaction to go to the start phase.
                    ChangePhaseOfAction(newPhase, ref trigger);
                }
                else if (newPhase == InputActionPhase.Cancelled && actionStates[actionIndex].interactionIndex == trigger.interactionIndex)
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
                else if (actionStates[actionIndex].interactionIndex == trigger.interactionIndex)
                {
                    var phaseAfterPerformedOrCancelled = InputActionPhase.Waiting;
                    if (newPhase == InputActionPhase.Performed)
                        phaseAfterPerformedOrCancelled = phaseAfterPerformed;

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
                                ResetInteractionState(trigger.mapIndex, trigger.bindingIndex, index);
                        }
                    }
                }
            }

            // If the interaction performed or cancelled, go back to waiting.
            // Exception: if it was performed and we're to remain in started state, set the interaction
            //            to started. Note that for that phase transition, there are no callbacks being
            //            triggered (i.e. we don't call 'started' every time after 'performed').
            if (newPhase == InputActionPhase.Performed)
            {
                interactionStates[interactionIndex].phase = phaseAfterPerformed;
            }
            else if (newPhase == InputActionPhase.Performed || newPhase == InputActionPhase.Cancelled)
            {
                ResetInteractionState(trigger.mapIndex, trigger.bindingIndex, trigger.interactionIndex);
            }
            ////TODO: reset entire chain
        }

        // Perform a phase change on the action. Visible to observers.
        private void ChangePhaseOfAction(InputActionPhase newPhase, ref TriggerState trigger,
            InputActionPhase phaseAfterPerformedOrCancelled = InputActionPhase.Waiting)
        {
            Debug.Assert(trigger.mapIndex >= 0 && trigger.mapIndex < totalMapCount, "Map index out of range");
            Debug.Assert(trigger.controlIndex >= 0 && trigger.controlIndex < totalControlCount, "Control index out of range");
            Debug.Assert(trigger.bindingIndex >= 0 && trigger.bindingIndex < totalBindingCount, "Binding index out of range");

            var actionIndex = bindingStates[trigger.bindingIndex].actionIndex;
            if (actionIndex == kInvalidIndex)
                return; // No action associated with binding.

            // Update action state.
            Debug.Assert(trigger.mapIndex == actionStates[actionIndex].mapIndex,
                "Map index on trigger does not correspond to map index of trigger state");
            trigger.flags = actionStates[actionIndex].flags; // Preserve flags.
            actionStates[actionIndex] = trigger;
            actionStates[actionIndex].phase = newPhase;

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
                    CallActionListeners(actionIndex, map, newPhase, ref action.m_OnStarted);
                    break;
                }

                case InputActionPhase.Performed:
                {
                    CallActionListeners(actionIndex, map, newPhase, ref action.m_OnPerformed);
                    actionStates[actionIndex].phase = phaseAfterPerformedOrCancelled;

                    // If the action is continuous and remains in performed or started state, make sure the action
                    // is on the list of continuous actions that we check every update.
                    if ((phaseAfterPerformedOrCancelled == InputActionPhase.Started ||
                         phaseAfterPerformedOrCancelled == InputActionPhase.Performed) &&
                        actionStates[actionIndex].continuous &&
                        !actionStates[actionIndex].onContinuousList)
                    {
                        AddContinuousAction(actionIndex);
                    }
                    break;
                }

                case InputActionPhase.Cancelled:
                {
                    CallActionListeners(actionIndex, map, newPhase, ref action.m_OnCancelled);
                    actionStates[actionIndex].phase = phaseAfterPerformedOrCancelled;

                    // Remove from list of continuous actions, if necessary.
                    if (actionStates[actionIndex].onContinuousList)
                        RemoveContinuousAction(actionIndex);
                    break;
                }
            }
        }

        private void CallActionListeners(int actionIndex, InputActionMap actionMap, InputActionPhase phase, ref InlinedArray<InputActionListener> listeners)
        {
            // If there's no listeners, don't bother with anything else.
            var callbacksOnMap = actionMap.m_ActionCallbacks;
            if (listeners.length == 0 && callbacksOnMap.length == 0 && s_OnActionChange.length == 0)
                return;

            var context = new InputAction.CallbackContext
            {
                m_State = this,
                m_ActionIndex = actionIndex,
            };

            Profiler.BeginSample("InputActionCallback");

            // Global callback goes first.
            if (s_OnActionChange.length > 0)
            {
                var action = context.action;

                InputActionChange change;
                switch (phase)
                {
                    case InputActionPhase.Started:
                        change = InputActionChange.ActionStarted;
                        break;
                    case InputActionPhase.Performed:
                        change = InputActionChange.ActionPerformed;
                        break;
                    case InputActionPhase.Cancelled:
                        change = InputActionChange.ActionCancelled;
                        break;
                    default:
                        Debug.Assert(false, "Should not reach here");
                        return;
                }

                for (var i = 0; i < s_OnActionChange.length; ++i)
                    s_OnActionChange[i](action, change);
            }

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
                    Debug.LogError(
                        $"{exception.GetType().Name} thrown during execution of '{phase}' callback on action '{GetActionOrNull(ref actionStates[actionIndex])}'");
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
                    Debug.LogError(
                        $"{exception.GetType().Name} thrown during execution of callback for '{phase}' phase of '{GetActionOrNull(ref actionStates[actionIndex]).name}' action in map '{actionMap.name}'");
                    Debug.LogException(exception);
                }
            }

            Profiler.EndSample();
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

            Debug.Assert(actionIndex >= 0 && actionIndex < totalActionCount, "Action index out of range");
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

        internal InputBinding GetBinding(int bindingIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");
            var mapIndex = bindingStates[bindingIndex].mapIndex;
            var bindingStartIndex = mapIndices[mapIndex].bindingStartIndex;
            return maps[mapIndex].m_Bindings[bindingIndex - bindingStartIndex];
        }

        private void ResetInteractionStateAndCancelIfNecessary(int mapIndex, int bindingIndex, int interactionIndex)
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
                        ChangePhaseOfInteraction(InputActionPhase.Cancelled, ref actionStates[actionIndex]);
                        break;
                }
            }

            ResetInteractionState(mapIndex, bindingIndex, interactionIndex);
        }

        private void ResetInteractionState(int mapIndex, int bindingIndex, int interactionIndex)
        {
            Debug.Assert(interactionIndex >= 0 && interactionIndex < totalInteractionCount, "Interaction index out of range");
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");

            // Clean up internal state that the interaction may keep.
            interactions[interactionIndex].Reset();

            // Clean up timer.
            if (interactionStates[interactionIndex].isTimerRunning)
            {
                var controlIndex = interactionStates[interactionIndex].triggerControlIndex;
                StopTimeout(mapIndex, controlIndex, bindingIndex, interactionIndex);
            }

            // Reset state record.
            interactionStates[interactionIndex] =
                new InteractionState
            {
                // We never set interactions to disabled. This way we don't have to go through them
                // when we disable/enable actions.
                phase = InputActionPhase.Waiting,
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
                Debug.Assert(compositeObject != null);

                return compositeObject.valueSizeInBytes;
            }

            var control = controls[controlIndex];
            Debug.Assert(control != null);
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

        /// <summary>
        /// Return true if the given binding is currently actuated.
        /// </summary>
        /// <param name="bindingIndex"></param>
        /// <param name="controlIndex"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        /// <remarks>
        /// Unlike <see cref="InputControlExtensions.IsActuated"/> this will work on composites, too. It does so
        /// by checking the magnitude of actuation of the entire composite (<see cref="InputBindingComposite.EvaluateMagnitude"/>).
        ///
        /// This method is expensive!
        /// </remarks>
        /// <seealso cref="InputControlExtensions.IsActuated"/>
        internal bool IsActuated(int bindingIndex, int controlIndex, float threshold = 0)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index is out of range");
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index is out of range");

            if (bindingStates[bindingIndex].isPartOfComposite)
            {
                var compositeBindingIndex = bindingStates[bindingIndex].compositeOrCompositeBindingIndex;
                var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                var compositeObject = composites[compositeIndex];

                var context = new InputBindingCompositeContext
                {
                    m_State = this,
                    m_BindingIndex = compositeBindingIndex
                };

                var magnitude = compositeObject.EvaluateMagnitude(ref context);
                if (Mathf.Approximately(threshold, 0))
                    return magnitude > 0;

                return magnitude >= threshold;
            }

            var control = controls[controlIndex];
            return control.IsActuated(threshold);
        }

        ////REVIEW: we can unify the reading paths once we have blittable type constraints

        internal unsafe void ReadValue(int bindingIndex, int controlIndex, void* buffer, int bufferSize)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index out of range");
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index out of range");

            InputControl control = null;

            // If the binding that triggered the action is part of a composite, let
            // the composite determine the value we return.
            if (bindingStates[bindingIndex].isPartOfComposite)
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
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index is out of range");

            var value = default(TValue);

            // In the case of a composite, this will be null.
            InputControl<TValue> controlOfType = null;

            // If the binding that triggered the action is part of a composite, let
            // the composite determine the value we return.
            if (!ignoreComposites && bindingStates[bindingIndex].isPartOfComposite)
            {
                var compositeBindingIndex = bindingStates[bindingIndex].compositeOrCompositeBindingIndex;
                Debug.Assert(compositeBindingIndex >= 0 && compositeBindingIndex < totalBindingCount);
                var compositeIndex = bindingStates[compositeBindingIndex].compositeOrCompositeBindingIndex;
                var compositeObject = composites[compositeIndex];
                Debug.Assert(compositeObject != null, "Composite object is null");

                var compositeOfType = compositeObject as InputBindingComposite<TValue>;
                if (compositeOfType == null)
                    throw new InvalidOperationException(
                        $"Cannot read value of type '{typeof(TValue).Name}' from composite '{compositeObject}' bound to action '{GetActionOrNull(bindingIndex)}' (composite is a '{compositeIndex.GetType().Name}' with value type '{TypeHelpers.GetNiceTypeName(compositeObject.GetType().GetGenericArguments()[0])}')");

                var context = new InputBindingCompositeContext
                {
                    m_State = this,
                    m_BindingIndex = compositeBindingIndex
                };

                value = compositeOfType.ReadValue(ref context);
            }
            else
            {
                var control = controls[controlIndex];
                Debug.Assert(control != null, "Control is null");

                controlOfType = control as InputControl<TValue>;
                if (controlOfType == null)
                    throw new InvalidOperationException(
                        $"Cannot read value of type '{typeof(TValue).Name}' from control '{control.path}' bound to action '{GetActionOrNull(bindingIndex)}' (control is a '{control.GetType().Name}' with value type '{TypeHelpers.GetNiceTypeName(control.valueType)}')");

                value = controlOfType.ReadValue();
            }

            // Run value through processors, if any.
            var processorCount = bindingStates[bindingIndex].processorCount;
            if (processorCount > 0)
            {
                var processorStartIndex = bindingStates[bindingIndex].processorStartIndex;
                for (var i = 0; i < processorCount; ++i)
                    value = ((InputProcessor<TValue>)processors[processorStartIndex + i]).Process(value, controlOfType);
            }

            return value;
        }

        internal TValue ReadCompositePartValue<TValue>(int bindingIndex, int partNumber)
            where TValue : struct, IComparable<TValue>
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index is out of range");

            var result = default(TValue);
            var firstChildBindingIndex = bindingIndex + 1;
            var isFirstValue = true;
            for (var index = firstChildBindingIndex; index < totalBindingCount && bindingStates[index].isPartOfComposite; ++index)
            {
                if (bindingStates[index].partIndex != partNumber)
                    continue;

                var controlCount = bindingStates[index].controlCount;
                var controlStartIndex = bindingStates[index].controlStartIndex;
                for (var i = 0; i < controlCount; ++i)
                {
                    var controlIndex = controlStartIndex + i;
                    var value = ReadValue<TValue>(index, controlIndex, ignoreComposites: true);

                    if (isFirstValue)
                    {
                        result = value;
                        isFirstValue = false;
                    }
                    else if (value.CompareTo(result) > 0)
                    {
                        result = value;
                    }
                }
            }

            return result;
        }

        internal object ReadValueAsObject(int bindingIndex, int controlIndex)
        {
            Debug.Assert(bindingIndex >= 0 && bindingIndex < totalBindingCount, "Binding index is out of range");
            Debug.Assert(controlIndex >= 0 && controlIndex < totalControlCount, "Control index is out of range");

            InputControl control = null;
            object value;

            // If the binding that triggered the action is part of a composite, let
            // the composite determine the value we return.
            if (bindingStates[bindingIndex].isPartOfComposite) ////TODO: instead, just have compositeOrCompositeBindingIndex be invalid
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
            }
            else
            {
                control = controls[controlIndex];
                Debug.Assert(control != null, "Control is null");
                value = control.ReadValueAsObject();
            }

            // Run value through processors, if any.
            var processorCount = bindingStates[bindingIndex].processorCount;
            if (processorCount > 0)
            {
                var processorStartIndex = bindingStates[bindingIndex].processorStartIndex;
                for (var i = 0; i < processorCount; ++i)
                    value = processors[processorStartIndex + i].ProcessAsObject(value, control);
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
                get => m_TriggerControlIndex;
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
                get => m_StartTime;
                set => m_StartTime = value;
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
            [FieldOffset(5)] private byte m_PartIndex;
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
                Composite = 1 << 2,
                PartOfComposite = 1 << 3,
                NeedsInitialStateCheck = 1 << 4,
            }

            /// <summary>
            /// Index into <see cref="controls"/> of first control associated with the binding.
            /// </summary>
            public int controlStartIndex
            {
                get => m_ControlStartIndex;
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
                get => m_ControlCount;
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
                get => m_MapIndex;
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
                get => m_TriggerEventIdForComposite;
                set => m_TriggerEventIdForComposite = value;
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

            public bool needsInitialStateCheck
            {
                get => (flags & Flags.NeedsInitialStateCheck) != 0;
                set
                {
                    if (value)
                        flags |= Flags.NeedsInitialStateCheck;
                    else
                        flags &= ~Flags.NeedsInitialStateCheck;
                }
            }

            public int partIndex
            {
                get => m_PartIndex;
                set
                {
                    if (partIndex < 0)
                        throw new ArgumentOutOfRangeException("value", "Part index must not be negative");
                    if (partIndex > byte.MaxValue)
                        throw new InvalidOperationException("Part count must not exceed byte.MaxValue=" + byte.MaxValue);
                    m_PartIndex = (byte)value;
                }
            }
        }

        /// <summary>
        /// Record of an input control change and its related data.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 28)]
        public struct TriggerState
        {
            [FieldOffset(0)] private byte m_Phase;
            [FieldOffset(1)] private byte m_Flags;
            [FieldOffset(2)] private ushort m_MapIndex;
            ////REVIEW: can we condense this to floats? would save us a whopping 8 bytes
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
                get => (InputActionPhase)m_Phase;
                set => m_Phase = (byte)value;
            }

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

            public int mapIndex
            {
                get => m_MapIndex;
                set
                {
                    Debug.Assert(value >= 0 && value <= ushort.MaxValue);
                    if (value < 0 || value > ushort.MaxValue)
                        throw new NotSupportedException("More than ushort.MaxValue InputActionMaps in a single InputActionMapState");
                    m_MapIndex = (ushort)value;
                }
            }

            /// <summary>
            /// Index of the control currently driving the action or <see cref="kInvalidIndex"/> if none.
            /// </summary>
            public int controlIndex
            {
                get => m_ControlIndex;
                set => m_ControlIndex = value;
            }

            /// <summary>
            /// Index into <see cref="bindingStates"/> for the binding that triggered.
            /// </summary>
            public int bindingIndex
            {
                get => m_BindingIndex;
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

            public bool continuous
            {
                get => (flags & Flags.Continuous) != 0;
                set
                {
                    if (value)
                        flags |= Flags.Continuous;
                    else
                        flags &= ~Flags.Continuous;
                }
            }

            public bool onContinuousList
            {
                get => (flags & Flags.OnContinuousList) != 0;
                set
                {
                    if (value)
                        flags |= Flags.OnContinuousList;
                    else
                        flags &= ~Flags.OnContinuousList;
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
                /// Whether the action associated with the trigger state is continuous.
                /// </summary>
                /// <seealso cref="InputAction.continuous"/>
                Continuous = 1 << 0,

                /// <summary>
                /// Whether the action is currently on the list of actions to check continuously.
                /// </summary>
                /// <seealso cref="InputActionMapState.m_ContinuousActions"/>
                OnContinuousList = 1 << 1,
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

                // See if this changes things for the state. If so, leave the state as is.
                // NOTE: The resolver will store indices in InputActionMap and InputAction but no
                //       references to anything in the state.
                if (state.DataMatches(resolver))
                    continue;

                // Fire change monitors.
                for (var n = 0; n < mapCount; ++n)
                {
                    var map = state.maps[n];
                    if (map.m_SingletonAction != null)
                        NotifyListenersOfActionChange(InputActionChange.BoundControlsAboutToChange,
                            map.m_SingletonAction);
                    else
                        NotifyListenersOfActionChange(InputActionChange.BoundControlsAboutToChange, map);
                }

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
                    if (map.m_SingletonAction != null)
                        NotifyListenersOfActionChange(InputActionChange.BoundControlsChanged,
                            map.m_SingletonAction);
                    else
                        NotifyListenersOfActionChange(InputActionChange.BoundControlsChanged, map);
                }
            }
        }

        private bool DataMatches(InputBindingResolver resolver)
        {
            if (totalMapCount != resolver.totalMapCount
                || totalActionCount != resolver.totalActionCount
                || totalBindingCount != resolver.totalBindingCount
                || totalCompositeCount != resolver.totalCompositeCount
                || totalControlCount != resolver.totalControlCount
                || totalProcessorCount != resolver.totalProcessorCount
                || totalInteractionCount != resolver.totalInteractionCount)
                return false;

            if (!ArrayHelpers.HaveEqualElements(maps, resolver.maps)
                || !ArrayHelpers.HaveEqualElements(controls, resolver.controls)
                ////FIXME: these will never be true as we currently do not reuse instances in the resolver
                || !ArrayHelpers.HaveEqualElements(interactions, resolver.interactions)
                || !ArrayHelpers.HaveEqualElements(composites, resolver.composites)
                || !ArrayHelpers.HaveEqualElements(processors, resolver.processors))
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
