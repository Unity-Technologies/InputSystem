using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Utilities;

#if !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

////TODO: nuke this shit and come up with a better solution

////REVIEW: the single state approach makes adding and removing maps costly; may not be flexible enough

////REVIEW: can we have a stable, global identification mechanism for actions that comes down to strings?

////TODO: need to sync when the InputActionMapState is re-resolved

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// An action manager owns the state of all action maps added to it.
    ///
    /// The data collected by an action manager is stored in unmanaged memory. If, when querying
    /// trigger events, no enumerators are used, no GC memory will be allocated during action
    /// processing.
    /// </remarks>
    public class InputActionManager : IInputActionCallbackReceiver, IDisposable
    {
        /// <summary>
        /// List of action maps added to the manager.
        /// </summary>
        public ReadOnlyArray<InputActionMap> actionMaps
        {
            get { return new ReadOnlyArray<InputActionMap>(m_State.maps, 0, m_State.totalMapCount); }
        }

        /// <summary>
        /// List of bound controls and associated actions that have triggered in the current frame.
        /// </summary>
        /// <remarks>
        ///
        /// Does not allocate.
        /// </remarks>
        public TriggerEventArray triggerEventsForCurrentFrame
        {
            get { return new TriggerEventArray(this); }
        }

        public void AddActionMap(InputActionMap actionMap)
        {
            ////TODO: throw if added to another manager
            if (actionMap == null)
                throw new ArgumentNullException("actionMap");
            if (actionMap.enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot add action map '{0}' to manager while it is enabled", actionMap));

            // Get rid of action map state that may already be on the map.
            if (actionMap.m_State != null)
            {
                // Ignore if action map has already been added to this manager.
                if (actionMap.m_State == m_State)
                    return;

                actionMap.m_State.Destroy();
                Debug.Assert(actionMap.m_State == null);
            }

            ////TODO: defer resolution until we have all the maps; we really want the ability to set
            ////      an InputActionMapState on a map without having to resolve yet

            // Add the map to our state.
            var resolver = new InputBindingResolver();
            if (m_State != null)
                resolver.ContinueWithDataFrom(m_State);
            else
                m_State = new InputActionMapState();
            resolver.AddActionMap(actionMap);
            m_State.Initialize(resolver);
            actionMap.m_State = m_State;

            // Listen for actions trigger on the map.
            actionMap.AddActionCallbackReceiver(this);

            // If it's the first map added to us, also hook into the input system to automatically
            // flush our recorded data between updates.
            if (m_State.totalMapCount == 1)
                InputSystem.onUpdate += OnBeforeInputSystemUpdate;
        }

        public void RemoveActionMap(InputActionMap actionMap)
        {
            //nuke event data
            throw new NotImplementedException();
        }

        public void Flush()
        {
            m_TriggerDataCount = 0;
            m_ActionDataCount = 0;
            m_StateDataSize = 0;

            // We keep the buffers allocated.
        }

        /// <summary>
        /// Called when an action is triggered in one of the action maps added to the manager. Records
        /// relevant trigger information to surface in event list later on.
        /// </summary>
        /// <param name="context"></param>
        unsafe void IInputActionCallbackReceiver.OnActionTriggered(ref InputAction.CallbackContext context)
        {
            var controlIndex = context.m_ControlIndex;
            var control = context.control;
            var time = context.m_Time;

            // See if already have a trigger record for the control.
            var triggerIndex = -1;
            for (var i = 0; i < m_TriggerDataCount; ++i)
            {
                var otherControlIndex = m_TriggerDataBuffer[i].controlIndex;
                if (otherControlIndex != controlIndex)
                {
                    ////REVIEW: shouldn't we make sure somehow that control indices are unique?
                    // NOTE: We're not just comparing control indices here but rather actual control references
                    //       as the same control may appear multiple times in the list.
                    var otherControl = m_State.controls[otherControlIndex];
                    if (!ReferenceEquals(otherControl, control))
                        continue;
                }

                if (!Mathf.Approximately((float)m_TriggerDataBuffer[i].time, (float)time))
                    continue;

                triggerIndex = i;
                break;
            }

            // If not, create one.
            if (triggerIndex == -1)
            {
                // Save the current state of the control. Copy full bytes only (means we may be grabbing some
                // state from other controls here but that doesn't matter).
                var stateSizeInBytes = control.m_StateBlock.alignedSizeInBytes;
                var stateOffset = control.m_StateBlock.byteOffset;
                var statePtr = (byte*)control.currentStatePtr.ToPointer() + stateOffset;
                var offsetOfSavedState = ArrayHelpers.GrowWithCapacity(ref m_StateDataBuffer, ref m_StateDataSize, (int)stateSizeInBytes, 1024);
                UnsafeUtility.MemCpy((byte*)m_StateDataBuffer.GetUnsafePtr() + offsetOfSavedState, statePtr,
                    stateSizeInBytes);

                // Append trigger data.
                var triggerData = new TriggerData
                {
                    controlIndex = controlIndex,
                    time = time,
                    stateOffset = (uint)offsetOfSavedState,
                    stateSizeInBytes = stateSizeInBytes,
                };
                triggerIndex =
                    ArrayHelpers.AppendWithCapacity(ref m_TriggerDataBuffer, ref m_TriggerDataCount, triggerData);
            }

            // Add action record.
            var data = m_TriggerDataBuffer[triggerIndex];
            ++data.actionEventCount;
            var actionIndex = ArrayHelpers.AppendWithCapacity(ref m_ActionDataBuffer, ref m_ActionDataCount,
                new ActionData
                {
                    triggerIndex = triggerIndex,
                    bindingIndex = context.m_BindingIndex,
                    interactionIndex = context.m_InteractionIndex,
                    phase = context.phase,
                });
            if (data.actionEventCount == 1)
                data.actionEventIndex = actionIndex;
            m_TriggerDataBuffer[triggerIndex] = data;
        }

        ~InputActionManager()
        {
            Destroy();
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        private void Destroy()
        {
            if (m_TriggerDataBuffer.Length > 0)
                m_TriggerDataBuffer.Dispose();
            if (m_ActionDataBuffer.Length > 0)
                m_ActionDataBuffer.Dispose();
            if (m_StateDataBuffer.Length > 0)
                m_StateDataBuffer.Dispose();

            m_TriggerDataBuffer = new NativeArray<TriggerData>();
            m_ActionDataBuffer = new NativeArray<ActionData>();
            m_StateDataBuffer = new NativeArray<byte>();

            InputSystem.onUpdate -= OnBeforeInputSystemUpdate;
        }

        /// <summary>
        /// Combined state of all action maps added to the manager.
        /// </summary>
        private InputActionMapState m_State;

        /// <summary>
        /// Unmanaged memory into which we store our event trace.
        /// </summary>
        /// <remarks>
        /// We record all events as unmanaged information which indexes into the data
        /// kept in <see cref="m_State"/>. This way we can retrieve managed objects, like
        /// <see cref="InputControl"/> objects, for example, on demand when the user is
        /// actually going through the events.
        /// </remarks>
        private int m_TriggerDataCount;
        private int m_ActionDataCount;
        private int m_StateDataSize;
        private NativeArray<TriggerData> m_TriggerDataBuffer;
        private NativeArray<ActionData> m_ActionDataBuffer;
        private NativeArray<byte> m_StateDataBuffer;

        ////TODO: replace this with having global frame-start notifications for input
        private bool m_HadFixedUpdate;

        private void OnBeforeInputSystemUpdate(InputUpdateType type)
        {
            if (type == InputUpdateType.Fixed)
                m_HadFixedUpdate = true;

            // Check if we need to flush events.
            var shouldFlush = false;
            var dynamicUpdateEnabled = (InputSystem.updateMask & InputUpdateType.Dynamic) == InputUpdateType.Dynamic;
            if (dynamicUpdateEnabled && type == InputUpdateType.Dynamic)
            {
                // If it's a dynamic update, we only want to flush if there wasn't a fixed update
                // that happened in-between the current and the last dynamic update.
                if (!m_HadFixedUpdate)
                    shouldFlush = true;
                m_HadFixedUpdate = false;
            }
            shouldFlush = shouldFlush
                || (!dynamicUpdateEnabled && type == InputUpdateType.Fixed);

            if (shouldFlush)
                Flush();
        }

        [StructLayout(LayoutKind.Explicit, Size = 20)]
        internal struct TriggerData
        {
            /// <summary>
            /// Time the control got triggered at.
            /// </summary>
            [FieldOffset(0)] public double m_Time;

            /// <summary>
            /// Index of the control that got triggered.
            /// </summary>
            [FieldOffset(8)] public ushort m_ControlIndex;
            [FieldOffset(10)] public ushort m_StateSizeInBytes;
            [FieldOffset(12)] public ushort m_ActionEventCount;
            [FieldOffset(14)] public ushort m_ActionEventIndex;
            [FieldOffset(16)] public uint m_StateOffset;

            public double time
            {
                get { return m_Time; }
                set { m_Time = value; }
            }

            public int controlIndex
            {
                get { return m_ControlIndex; }
                set
                {
                    Debug.Assert(value != InputActionMapState.kInvalidIndex);
                    if (value > ushort.MaxValue)
                        throw new NotSupportedException("Control count must not exceed ushort.MaxValue=" + ushort.MaxValue);
                    m_ControlIndex = (ushort)value;
                }
            }

            public uint stateOffset
            {
                get { return m_StateOffset; }
                set { m_StateOffset = value; }
            }

            public uint stateSizeInBytes
            {
                get { return m_StateSizeInBytes; }
                set
                {
                    if (value > ushort.MaxValue)
                        throw new NotSupportedException("State size must not exceed ushort.MaxValue=" + ushort.MaxValue);
                    m_StateSizeInBytes = (ushort)value;
                }
            }

            public int actionEventCount
            {
                get { return m_ActionEventCount; }
                set
                {
                    if (value > ushort.MaxValue)
                        throw new NotSupportedException(
                            "Action count triggered per control must not exceed ushort.MaxValue=" + ushort.MaxValue);
                    m_ActionEventCount = (ushort)value;
                }
            }

            public int actionEventIndex
            {
                get { return m_ActionEventIndex; }
                set
                {
                    if (value > ushort.MaxValue)
                        throw new NotSupportedException(
                            "Action count triggered in total must not exceed ushort.MaxValue=" + ushort.MaxValue);
                    m_ActionEventIndex = (ushort)value;
                }
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        internal struct ActionData
        {
            [FieldOffset(0)] public ushort m_TriggerIndex;

            /// <summary>
            /// Index of the binding that triggered the action.
            /// </summary>
            [FieldOffset(2)] public ushort m_BindingIndex;

            /// <summary>
            /// Index of the interaction on the binding that controlled the triggering.
            /// </summary>
            /// <remarks>
            /// <see cref="InputActionMapState.kInvalidIndex"/> if the binding triggered without an interaction.
            /// </remarks>
            [FieldOffset(4)] public ushort m_InteractionIndex;

            [FieldOffset(6)] public byte m_Phase;

            public int triggerIndex
            {
                get { return m_TriggerIndex; }
                set
                {
                    Debug.Assert(value >= 0);
                    if (value > ushort.MaxValue)
                        throw new NotSupportedException("Trigger count must not exceed ushort.MaxValue=" +
                            ushort.MaxValue);
                    m_TriggerIndex = (ushort)value;
                }
            }

            public int bindingIndex
            {
                get { return m_BindingIndex; }
                set
                {
                    Debug.Assert(value >= 0);
                    if (value > ushort.MaxValue)
                        throw new NotSupportedException("Binding count must not exceed ushort.MaxValue=" + ushort.MaxValue);
                    m_BindingIndex = (ushort)value;
                }
            }

            public int interactionIndex
            {
                get
                {
                    if (m_InteractionIndex == ushort.MaxValue)
                        return InputActionMapState.kInvalidIndex;
                    return m_InteractionIndex;
                }
                set
                {
                    if (value == InputActionMapState.kInvalidIndex)
                        m_InteractionIndex = ushort.MaxValue;
                    else
                    {
                        Debug.Assert(value >= 0);
                        if (value >= ushort.MaxValue)
                            throw new NotSupportedException("Interaction count must not exceed ushort.MaxValue=" + ushort.MaxValue);
                        m_InteractionIndex = (ushort)value;
                    }
                }
            }

            public InputActionPhase phase
            {
                get { return (InputActionPhase)m_Phase; }
                set { m_Phase = (byte)value; }
            }
        }

        public struct ActionEvent
        {
            internal InputActionManager m_Manager;
            internal int m_ActionDataIndex;
            internal ActionData m_Data;

            internal ActionEvent(InputActionManager manager, int actionDataIndex)
            {
                m_Manager = manager;
                m_ActionDataIndex = actionDataIndex;
                m_Data = manager.m_ActionDataBuffer[actionDataIndex];
            }

            public InputAction action
            {
                get
                {
                    if (m_Manager == null)
                        return null;
                    return m_Manager.m_State.GetActionOrNull(m_Data.bindingIndex);
                }
            }

            public InputActionPhase phase
            {
                get
                {
                    if (m_Manager == null)
                        return InputActionPhase.Disabled;
                    return m_Data.phase;
                }
            }

            public InputBinding binding
            {
                get
                {
                    if (m_Manager == null)
                        return default(InputBinding);
                    return m_Manager.m_State.GetBinding(m_Data.bindingIndex);
                }
            }

            public IInputInteraction interaction
            {
                get
                {
                    if (m_Manager == null)
                        return null;
                    var interactionIndex = m_Data.interactionIndex;
                    if (interactionIndex == InputActionMapState.kInvalidIndex)
                        return null;
                    return m_Manager.m_State.interactions[interactionIndex];
                }
            }
        }

        public struct ActionEventArray : IReadOnlyList<ActionEvent>
        {
            internal InputActionManager m_Manager;
            internal int m_TriggerIndex;
            internal int m_ActionEventCount;
            internal int m_ActionEventIndex;

            internal ActionEventArray(InputActionManager manager, int triggerIndex, int actionEventCount, int actionEventIndex)
            {
                m_Manager = manager;
                m_TriggerIndex = triggerIndex;
                m_ActionEventCount = actionEventCount;
                m_ActionEventIndex = actionEventIndex;
            }

            public IEnumerator<ActionEvent> GetEnumerator()
            {
                return new Enumerator(m_Manager, m_TriggerIndex, m_ActionEventIndex);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count
            {
                get { return m_ActionEventCount; }
            }

            public ActionEvent this[int index]
            {
                get
                {
                    if (m_Manager == null)
                        throw new InvalidOperationException("ActionEventArray not intialized");

                    if (index < 0 || index >= m_ActionEventCount)
                        throw new ArgumentOutOfRangeException(
                            string.Format("Index {0} is out of range for trigger event with {1} action entries", index,
                                m_ActionEventCount), "index");

                    var idx = m_ActionEventIndex;
                    for (var i = 0; i != index; ++i)
                    {
                        ++idx;
                        while (m_Manager.m_ActionDataBuffer[idx].triggerIndex != m_TriggerIndex)
                            ++idx;
                    }

                    return new ActionEvent(m_Manager, idx);
                }
            }

            internal class Enumerator : IEnumerator<ActionEvent>
            {
                internal InputActionManager m_Manager;
                internal int m_TriggerIndex;
                internal int m_ActionEventIndex;

                public Enumerator(InputActionManager manager, int triggerIndex, int actionEventIndex)
                {
                    m_Manager = manager;
                    m_TriggerIndex = triggerIndex;
                    m_ActionEventIndex = actionEventIndex - 1; // Minus one as first MoveNext() should move us to first item.
                }

                public bool MoveNext()
                {
                    if (m_Manager == null)
                        return false;
                    var actionEventCountTotal = m_Manager.m_ActionDataCount;
                    for (var index = m_ActionEventIndex + 1; index < actionEventCountTotal; ++index)
                        if (m_Manager.m_ActionDataBuffer[index].triggerIndex == m_TriggerIndex)
                        {
                            m_ActionEventIndex = index;
                            return true;
                        }

                    return false;
                }

                public void Reset()
                {
                    if (m_Manager == null)
                        return;
                    m_ActionEventIndex = m_Manager.m_TriggerDataBuffer[m_TriggerIndex].actionEventIndex;
                }

                public ActionEvent Current
                {
                    get { return new ActionEvent(m_Manager, m_ActionEventIndex); }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public void Dispose()
                {
                }
            }
        }

        /// <summary>
        /// Record of a control triggering one or more actions.
        /// </summary>
        public struct TriggerEvent
        {
            internal InputActionManager m_Manager;
            internal int m_TriggerDataIndex;
            internal TriggerData m_Data;

            internal TriggerEvent(InputActionManager manager, int triggerDataIndex)
            {
                Debug.Assert(triggerDataIndex >= 0 && triggerDataIndex < manager.m_TriggerDataCount);
                m_Manager = manager;
                m_TriggerDataIndex = triggerDataIndex;
                m_Data = m_Manager.m_TriggerDataBuffer[triggerDataIndex];
            }

            /// <summary>
            /// Time that the control got triggered at.
            /// </summary>
            public double time
            {
                get
                {
                    if (m_Manager == null)
                        return default(double);
                    return m_Data.time;
                }
            }

            /// <summary>
            /// The input control that triggered.
            /// </summary>
            public InputControl control
            {
                get
                {
                    if (m_Manager == null)
                        return null;
                    return m_Manager.m_State.controls[m_Data.controlIndex];
                }
            }

            /// <summary>
            /// The set of possible actions triggered by the control.
            /// </summary>
            public ActionEventArray actions
            {
                get
                {
                    return new ActionEventArray(m_Manager, m_TriggerDataIndex, m_Data.actionEventCount,
                        m_Data.actionEventIndex);
                }
            }

            /// <summary>
            /// Read the value the control had when it triggered.
            /// </summary>
            /// <typeparam name="TValue">Type of value to read. Must match the value type of the control.</typeparam>
            /// <returns>Value of <see cref="control"/> at the time it triggered.</returns>
            public unsafe TValue ReadValue<TValue>()
            {
                ////TODO: this here should be moved into a general helper method; "read control value from chunk of memory" is generally useful

                // Grab control and make sure it has a matching type.
                var controlOfType = control as InputControl<TValue>;
                if (controlOfType == null)
                    throw new ArgumentException(
                        string.Format("Control '{0}' does not have value type {1}", control, typeof(TValue)), "TValue");

                // Fetch state memory and account for the control wanting to
                // read from an offset into the global state buffers.
                Debug.Assert(m_Data.stateSizeInBytes == controlOfType.m_StateBlock.alignedSizeInBytes);
                var statePtr = (byte*)m_Manager.m_StateDataBuffer.GetUnsafeReadOnlyPtr() + m_Data.stateOffset;
                statePtr -= controlOfType.m_StateBlock.byteOffset;

                ////TODO: this will have to take composites as well as processors on the binding into account

                ////REVIEW: 4 vtable dispatches (InputControl<Vector2>.ReadRawValueFrom, InputControl<float>.ReadRawValueFrom for x,
                ////        same for y, DeadzoneProcess.Process; plus quite a few direct method calls on top) for a single value read
                ////        of a Vector2 really isn't awesome; can we cut that down?
                // And let the control do the rest.
                return controlOfType.ReadValueFrom(new IntPtr(statePtr));
            }

            ////TODO: must be able to read previous values
        }

        public struct TriggerEventArray : IReadOnlyList<TriggerEvent>
        {
            internal InputActionManager m_Manager;

            internal TriggerEventArray(InputActionManager manager)
            {
                m_Manager = manager;
            }

            public IEnumerator<TriggerEvent> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count
            {
                get
                {
                    if (m_Manager == null)
                        return 0;
                    return m_Manager.m_TriggerDataCount;
                }
            }

            public TriggerEvent this[int index]
            {
                get
                {
                    if (m_Manager == null)
                        throw new InvalidOperationException("TriggerEventArray not initialized properly");
                    if (index < 0 || index >= m_Manager.m_TriggerDataCount)
                        throw new ArgumentOutOfRangeException(string.Format(
                            "Index {0} is out of range; trigger event array has {1} entries", index,
                            m_Manager.m_TriggerDataCount), "index");
                    return new TriggerEvent(m_Manager, index);
                }
            }

            internal class Enumerator : IEnumerator<TriggerEvent>
            {
                public bool MoveNext()
                {
                    throw new NotImplementedException();
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }

                public TriggerEvent Current { get; private set; }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public void Dispose()
                {
                }
            }
        }
    }
}
