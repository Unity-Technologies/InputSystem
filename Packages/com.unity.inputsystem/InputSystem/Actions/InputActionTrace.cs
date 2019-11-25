using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.LowLevel;

////REVIEW: why not switch to this being the default mechanism? seems like this could allow us to also solve
////        the actions-update-when-not-expected problem; plus give us access to easy polling

////REVIEW: should this automatically unsubscribe itself on disposal?

////TODO: make it possible to persist this same way that it should be possible to persist InputEventTrace

////TODO: make this one thread-safe

////TODO: add random access capability

////TODO: protect traces against controls changing configuration (if state layouts change, we're affected)

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// Records the triggering of actions into a sequence of events that can be replayed at will.
    /// </summary>
    /// <remarks>
    /// This is an alternate way to the callback-based responses (such as <see cref="InputAction.performed"/>)
    /// of <see cref="InputAction">input actions</see>. Instead of executing response code right away whenever
    /// an action triggers, an <see cref="RecordAction">event is recorded</see> which can then be queried on demand.
    ///
    /// The recorded data will stay valid even if the bindings on the actions are changed (e.g. by enabling a different
    /// set of bindings through altering <see cref="InputAction.bindingMask"/> or <see cref="InputActionMap.devices"/> or
    /// when modifying the paths of bindings altogether). Note, however, that when this happens, a trace will have
    /// to make a private copy of the data that stores the binding resolution state. This means that there can be
    /// GC allocation spike when reconfiguring actions that have recorded data in traces.
    ///
    /// <example>
    /// <code>
    /// var trace = new InputActionTrace();
    ///
    /// // Subscribe trace to single action.
    /// // (Use UnsubscribeFrom to unsubscribe)
    /// trace.SubscribeTo(myAction);
    ///
    /// // Subscribe trace to entire action map.
    /// // (Use UnsubscribeFrom to unsubscribe)
    /// trace.SubscribeTo(myActionMap);
    ///
    /// // Subscribe trace to all actions in the system.
    /// trace.SubscribeToAll();
    ///
    /// // Record a single triggering of an action.
    /// myAction.performed +=
    ///     ctx =>
    ///     {
    ///         if (ctx.ReadValue&lt;float&gt;() &gt; 0.5f)
    ///             trace.RecordAction(ctx);
    ///     };
    ///
    /// // Output trace to console.
    /// Debug.Log(string.Join(",\n", trace));
    ///
    /// // Walk through all recorded actions and then clear trace.
    /// foreach (var record in trace)
    /// {
    ///     Debug.Log($"{record.action} was {record.phase} by control {record.control} at {record.time}");
    ///
    ///     // To read out the value, you either have to know the value type or read the
    ///     // value out as a generic byte buffer. Here we assume that the value type is
    ///     // float.
    ///
    ///     Debug.Log("Value: " + record.ReadValue&lt;float&gt;());
    ///
    ///     // An alternative is read the value as an object. In this case, you don't have
    ///     // to know the value type but there will be a boxed object allocation.
    ///     Debug.Log("Value: " + record.ReadValueAsObject());
    /// }
    /// trace.Clear();
    ///
    /// // Unsubscribe trace from everything.
    /// trace.UnsubscribeFromAll();
    ///
    /// // Release memory held by trace.
    /// trace.Dispose();
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="InputAction.started"/>
    /// <seealso cref="InputAction.performed"/>
    /// <seealso cref="InputAction.canceled"/>
    /// <seealso cref="InputSystem.onActionChange"/>
    public sealed class InputActionTrace : IEnumerable<InputActionTrace.ActionEventPtr>, IDisposable
    {
        ////REVIEW: this is of limited use without having access to ActionEvent
        /// <summary>
        /// Directly access the underlying raw memory queue.
        /// </summary>
        public InputEventBuffer buffer => m_EventBuffer;

        public int count => m_EventBuffer.eventCount;

        public InputActionTrace()
        {
        }

        public InputActionTrace(InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            SubscribeTo(action);
        }

        public InputActionTrace(InputActionMap actionMap)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));
            SubscribeTo(actionMap);
        }

        /// <summary>
        /// Record any action getting triggered anywhere.
        /// </summary>
        /// <remarks>
        /// This does not require the trace to actually hook into every single action or action map in the system.
        /// Instead, the trace will listen to <see cref="InputSystem.onActionChange"/> and automatically record
        /// every triggered action.
        /// </remarks>
        public void SubscribeToAll()
        {
            if (m_SubscribedToAll)
                return;

            HookOnActionChange();
            m_SubscribedToAll = true;

            // Remove manually created subscriptions.
            while (m_SubscribedActions.length > 0)
                UnsubscribeFrom(m_SubscribedActions[m_SubscribedActions.length - 1]);
            while (m_SubscribedActionMaps.length > 0)
                UnsubscribeFrom(m_SubscribedActionMaps[m_SubscribedActionMaps.length - 1]);
        }

        public void UnsubscribeFromAll()
        {
            // Only unhook from OnActionChange if we don't have any recorded actions. If we do have
            // any, we still need the callback to be notified about when binding data changes.
            if (count == 0)
                UnhookOnActionChange();

            m_SubscribedToAll = false;

            while (m_SubscribedActions.length > 0)
                UnsubscribeFrom(m_SubscribedActions[m_SubscribedActions.length - 1]);
            while (m_SubscribedActionMaps.length > 0)
                UnsubscribeFrom(m_SubscribedActionMaps[m_SubscribedActionMaps.length - 1]);
        }

        public void SubscribeTo(InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (m_CallbackDelegate == null)
                m_CallbackDelegate = RecordAction;

            action.performed += m_CallbackDelegate;
            action.started += m_CallbackDelegate;
            action.canceled += m_CallbackDelegate;

            m_SubscribedActions.AppendWithCapacity(action);
        }

        public void SubscribeTo(InputActionMap actionMap)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));

            if (m_CallbackDelegate == null)
                m_CallbackDelegate = RecordAction;

            actionMap.actionTriggered += m_CallbackDelegate;

            m_SubscribedActionMaps.AppendWithCapacity(actionMap);
        }

        public void UnsubscribeFrom(InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (m_CallbackDelegate == null)
                return;

            action.performed -= m_CallbackDelegate;
            action.started -= m_CallbackDelegate;
            action.canceled -= m_CallbackDelegate;

            var index = m_SubscribedActions.IndexOfReference(action);
            if (index != -1)
                m_SubscribedActions.RemoveAtWithCapacity(index);
        }

        public void UnsubscribeFrom(InputActionMap actionMap)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));

            if (m_CallbackDelegate == null)
                return;

            actionMap.actionTriggered -= m_CallbackDelegate;

            var index = m_SubscribedActionMaps.IndexOfReference(actionMap);
            if (index != -1)
                m_SubscribedActionMaps.RemoveAtWithCapacity(index);
        }

        /// <summary>
        /// Record the triggering of an action as an <see cref="ActionEventPtr">action event</see>.
        /// </summary>
        /// <param name="context"></param>
        /// <see cref="InputAction.performed"/>
        /// <see cref="InputAction.started"/>
        /// <see cref="InputAction.canceled"/>
        /// <see cref="InputActionMap.actionTriggered"/>
        public unsafe void RecordAction(InputAction.CallbackContext context)
        {
            // Find/add state.
            var stateIndex = m_ActionMapStates.IndexOfReference(context.m_State);
            if (stateIndex == -1)
                stateIndex = m_ActionMapStates.AppendWithCapacity(context.m_State);

            // Make sure we get notified if there's a change to binding setups.
            HookOnActionChange();

            // Allocate event.
            var valueSizeInBytes = context.valueSizeInBytes;
            var eventPtr =
                (ActionEvent*)m_EventBuffer.AllocateEvent(ActionEvent.GetEventSizeWithValueSize(valueSizeInBytes));

            // Initialize event.
            ref var triggerState = ref context.m_State.actionStates[context.m_ActionIndex];
            eventPtr->baseEvent.type = ActionEvent.Type;
            eventPtr->baseEvent.time = triggerState.time;
            eventPtr->stateIndex = stateIndex;
            eventPtr->controlIndex = triggerState.controlIndex;
            eventPtr->bindingIndex = triggerState.bindingIndex;
            eventPtr->interactionIndex = triggerState.interactionIndex;
            eventPtr->startTime = triggerState.startTime;
            eventPtr->phase = triggerState.phase;

            // Store value.
            var valueBuffer = eventPtr->valueData;
            context.ReadValue(valueBuffer, valueSizeInBytes);
        }

        public void Clear()
        {
            m_EventBuffer.Reset();
            m_ActionMapStates.ClearWithCapacity();
        }

        ~InputActionTrace()
        {
            DisposeInternal();
        }

        public override string ToString()
        {
            if (count == 0)
                return "[]";

            var str = new StringBuilder();
            str.Append('[');
            var isFirst = true;
            foreach (var eventPtr in this)
            {
                if (!isFirst)
                    str.Append(",\n");
                str.Append(eventPtr.ToString());
                isFirst = false;
            }
            str.Append(']');
            return str.ToString();
        }

        public void Dispose()
        {
            UnsubscribeFromAll();
            DisposeInternal();
        }

        private void DisposeInternal()
        {
            // Nuke clones we made of InputActionMapStates.
            for (var i = 0; i < m_ActionMapStateClones.length; ++i)
                m_ActionMapStateClones[i].Dispose();

            m_EventBuffer.Dispose();
            m_ActionMapStates.Clear();
            m_ActionMapStateClones.Clear();

            if (m_ActionChangeDelegate != null)
            {
                InputSystem.onActionChange -= m_ActionChangeDelegate;
                m_ActionChangeDelegate = null;
            }
        }

        public IEnumerator<ActionEventPtr> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool m_SubscribedToAll;
        private bool m_OnActionChangeHooked;
        private InlinedArray<InputAction> m_SubscribedActions;
        private InlinedArray<InputActionMap> m_SubscribedActionMaps;
        private InputEventBuffer m_EventBuffer;
        private InlinedArray<InputActionState> m_ActionMapStates;
        private InlinedArray<InputActionState> m_ActionMapStateClones;
        private Action<InputAction.CallbackContext> m_CallbackDelegate;
        private Action<object, InputActionChange> m_ActionChangeDelegate;

        private void HookOnActionChange()
        {
            if (m_OnActionChangeHooked)
                return;

            if (m_ActionChangeDelegate == null)
                m_ActionChangeDelegate = OnActionChange;

            InputSystem.onActionChange += m_ActionChangeDelegate;
            m_OnActionChangeHooked = true;
        }

        private void UnhookOnActionChange()
        {
            if (!m_OnActionChangeHooked)
                return;

            InputSystem.onActionChange -= m_ActionChangeDelegate;
            m_OnActionChangeHooked = false;
        }

        private void OnActionChange(object actionOrMap, InputActionChange change)
        {
            // If we're subscribed to all actions, check if an action got triggered.
            if (m_SubscribedToAll)
            {
                switch (change)
                {
                    case InputActionChange.ActionStarted:
                    case InputActionChange.ActionPerformed:
                    case InputActionChange.ActionCanceled:
                        Debug.Assert(actionOrMap is InputAction, "Expected an action");
                        var triggeredAction = (InputAction)actionOrMap;
                        var actionIndex = triggeredAction.m_ActionIndexInState;
                        var stateForAction = triggeredAction.m_ActionMap.m_State;

                        var context = new InputAction.CallbackContext
                        {
                            m_State = stateForAction,
                            m_ActionIndex = actionIndex,
                        };

                        RecordAction(context);

                        return;
                }
            }

            // We're only interested in changes to the binding resolution state of actions.
            if (change != InputActionChange.BoundControlsAboutToChange)
                return;

            // Grab the associated action map.
            var action = actionOrMap as InputAction;
            InputActionMap actionMap;
            if (action != null)
                actionMap = action.m_ActionMap;
            else
            {
                actionMap = actionOrMap as InputActionMap;
                Debug.Assert(actionMap != null, "Given object is neither an InputAction nor an InputActionMap");
            }

            // Grab the state.
            var state = actionMap.m_State;
            if (state == null)
            {
                // Bindings have not been resolved yet for this action map. We shouldn't even be
                // on the notification list in this case, but just in case, ignore.
                return;
            }

            // See if we're using the given state.
            var stateIndex = m_ActionMapStates.IndexOfReference(state);
            if (stateIndex == -1)
                return;

            // Yes, we are so make our own private copy of its current state.
            // NOTE: We do not put these local InputActionMapStates on the global list.
            var clone = state.Clone();
            m_ActionMapStateClones.Append(clone);
            m_ActionMapStates[stateIndex] = clone;
        }

        /// <summary>
        /// A wrapper around <see cref="ActionEvent"/> that automatically translates all the
        /// information in events into their high-level representations.
        /// </summary>
        /// <remarks>
        /// For example, instead of returning <see cref="ActionEvent.controlIndex">control indices</see>,
        /// it automatically resolves and returns the respective <see cref="InputControl">controls</see>.
        /// </remarks>
        public unsafe struct ActionEventPtr
        {
            internal InputActionState m_State;
            internal ActionEvent* m_Ptr;

            public InputAction action => m_State.GetActionOrNull(m_Ptr->bindingIndex);

            public InputActionPhase phase => m_Ptr->phase;

            public InputControl control => m_State.controls[m_Ptr->controlIndex];

            public IInputInteraction interaction
            {
                get
                {
                    var index = m_Ptr->interactionIndex;
                    if (index == InputActionState.kInvalidIndex)
                        return null;

                    return m_State.interactions[index];
                }
            }

            public double time => m_Ptr->baseEvent.time;

            public double startTime => m_Ptr->startTime;

            public double duration => time - startTime;

            public int valueSizeInBytes => m_Ptr->valueSizeInBytes;

            public object ReadValueAsObject()
            {
                var valueSizeInBytes = m_Ptr->valueSizeInBytes;
                var valuePtr = m_Ptr->valueData;
                return control.ReadValueFromBufferAsObject(valuePtr, valueSizeInBytes);
            }

            public void ReadValue(void* buffer, int bufferSize)
            {
                var valueSizeInBytes = m_Ptr->valueSizeInBytes;

                ////REVIEW: do we want more checking than this?
                if (bufferSize < valueSizeInBytes)
                    throw new ArgumentException(
                        $"Expected buffer of at least {valueSizeInBytes} bytes but got buffer of just {bufferSize} bytes instead",
                        nameof(bufferSize));

                UnsafeUtility.MemCpy(buffer, m_Ptr->valueData, valueSizeInBytes);
            }

            public TValue ReadValue<TValue>()
                where TValue : struct
            {
                var valueSizeInBytes = m_Ptr->valueSizeInBytes;

                ////REVIEW: do we want more checking than this?
                if (UnsafeUtility.SizeOf<TValue>() != valueSizeInBytes)
                    throw new InvalidOperationException(
                        $"Cannot read a value of type '{typeof(TValue).Name}' with size {UnsafeUtility.SizeOf<TValue>()} from event on action '{action}' with value size {valueSizeInBytes}");

                var result = new TValue();
                var resultPtr = UnsafeUtility.AddressOf(ref result);
                UnsafeUtility.MemCpy(resultPtr, m_Ptr->valueData, valueSizeInBytes);

                return result;
            }

            public override string ToString()
            {
                if (m_Ptr == null)
                    return "<null>";

                var actionName = action.actionMap != null ? $"{action.actionMap.name}/{action.name}" : action.name;
                return $"{{ action={actionName} phase={phase} time={time} control={control} value={ReadValueAsObject()} interaction={interaction} duration={duration} }}";
            }
        }

        private unsafe struct Enumerator : IEnumerator<ActionEventPtr>
        {
            private readonly InputActionTrace m_Trace;
            private readonly ActionEvent* m_Buffer;
            private readonly int m_EventCount;
            private ActionEvent* m_CurrentEvent;
            private int m_CurrentIndex;

            public Enumerator(InputActionTrace trace)
            {
                m_Trace = trace;
                m_Buffer = (ActionEvent*)trace.m_EventBuffer.bufferPtr.data;
                m_EventCount = trace.m_EventBuffer.eventCount;
                m_CurrentEvent = null;
                m_CurrentIndex = 0;
            }

            public bool MoveNext()
            {
                if (m_CurrentIndex == m_EventCount)
                    return false;

                if (m_CurrentEvent == null)
                {
                    m_CurrentEvent = m_Buffer;
                    return m_CurrentEvent != null;
                }

                Debug.Assert(m_CurrentEvent != null);

                ++m_CurrentIndex;
                if (m_CurrentIndex == m_EventCount)
                    return false;

                m_CurrentEvent = (ActionEvent*)InputEvent.GetNextInMemory((InputEvent*)m_CurrentEvent);
                return true;
            }

            public void Reset()
            {
                m_CurrentEvent = null;
                m_CurrentIndex = 0;
            }

            public void Dispose()
            {
            }

            public ActionEventPtr Current
            {
                get
                {
                    var state = m_Trace.m_ActionMapStates[m_CurrentEvent->stateIndex];
                    return new ActionEventPtr
                    {
                        m_State = state,
                        m_Ptr = m_CurrentEvent,
                    };
                }
            }

            object IEnumerator.Current => Current;
        }
    }
}
