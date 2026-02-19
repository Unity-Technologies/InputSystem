using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Record a history of state changes applied to one or more controls.
    /// </summary>
    /// <remarks>
    /// This class makes it easy to track input values over time. It will automatically retain input state up to a given
    /// maximum history depth (<see cref="historyDepth"/>). When the history is full, it will start overwriting the oldest
    /// entry each time a new history record is received.
    ///
    /// The class listens to changes on the given controls by adding change monitors (<see cref="IInputStateChangeMonitor"/>)
    /// to each control.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Track all stick controls in the system.
    /// var history = new InputStateHistory&lt;Vector2&gt;("*/&lt;Stick&gt;");
    /// foreach (var control in history.controls)
    ///     Debug.Log("Capturing input on " + control);
    ///
    /// // Start capturing.
    /// history.StartRecording();
    ///
    /// // Perform a couple artificial value changes.
    /// Gamepad.current.leftStick.QueueValueChange(new Vector2(0.123f, 0.234f));
    /// Gamepad.current.leftStick.QueueValueChange(new Vector2(0.234f, 0.345f));
    /// Gamepad.current.leftStick.QueueValueChange(new Vector2(0.345f, 0.456f));
    /// InputSystem.Update();
    ///
    /// // Every value change will be visible in the history.
    /// foreach (var record in history)
    ///     Debug.Log($"{record.control} changed value to {record.ReadValue()}");
    ///
    /// // Histories allocate unmanaged memory and must be disposed of in order to not leak.
    /// history.Dispose();
    /// </code>
    /// </example>
    /// <seealso cref="Record"/>
    public class InputStateHistory : IDisposable, IEnumerable<InputStateHistory.Record>, IInputStateChangeMonitor
    {
        private const int kDefaultHistorySize = 128;

        /// <summary>
        /// Total number of state records currently captured in the history.
        /// </summary>
        /// <remarks>
        /// Number of records in the collection.
        ///
        /// This will always be at most <see cref="historyDepth"/>.
        /// To record a change use <see cref="RecordStateChange(InputControl,InputEventPtr)"/>.
        /// </remarks>
        public int Count => m_RecordCount;

        /// <summary>
        /// Current version stamp. Every time a record is stored in the history,
        /// this is incremented by one.
        /// </summary>
        /// <remarks>
        /// Version stamp that indicates the number of mutations.
        /// To record a change use <see cref="RecordStateChange(InputControl,InputEventPtr)"/>.
        /// </remarks>
        public uint version => m_CurrentVersion;

        /// <summary>
        /// Maximum number of records that can be recorded in the history.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="value"/> is negative.</exception>
        /// <remarks>
        /// Upper limit on number of records.
        /// A fixed size memory block of unmanaged memory will be allocated to store history
        /// records.
        /// When the history is full, it will start overwriting the oldest
        /// entry each time a new history record is received.
        /// </remarks>
        public int historyDepth
        {
            get => m_HistoryDepth;
            set
            {
                if (value < 0)
                    throw new ArgumentException("History depth cannot be negative", nameof(value));
                if (m_RecordBuffer.IsCreated)
                    throw new NotImplementedException();
                m_HistoryDepth = value;
            }
        }

        /// <summary>
        /// Size of additional data storage to allocate per record.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="value"/> is negative.</exception>
        /// <remarks>
        /// Additional custom data can be stored per record up to the size of this value.
        /// To retrieve a pointer to this memory use <see cref="Record.GetUnsafeExtraMemoryPtr"/>
        /// Used by <see cref="EnhancedTouch.Touch.history"/>
        /// </remarks>
        public int extraMemoryPerRecord
        {
            get => m_ExtraMemoryPerRecord;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Memory size cannot be negative", nameof(value));
                if (m_RecordBuffer.IsCreated)
                    throw new NotImplementedException();
                m_ExtraMemoryPerRecord = value;
            }
        }

        /// <summary>
        /// Specify which player loop positions the state history will be monitored for.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="value"/>When an invalid mask is provided (e.g. <see cref="InputUpdateType.None"/>).</exception>
        /// <remarks>
        /// The state history will only be monitored for the specified player loop positions.
        /// <see cref="InputUpdateType.Editor"/> is excluded from this list
        /// </remarks>
        public InputUpdateType updateMask
        {
            get => m_UpdateMask ?? InputSystem.s_Manager.updateMask & ~InputUpdateType.Editor;
            set
            {
                if (value == InputUpdateType.None)
                    throw new ArgumentException("'InputUpdateType.None' is not a valid update mask", nameof(value));
                m_UpdateMask = value;
            }
        }

        /// <summary>
        /// List of input controls the state history will be recording for.
        /// </summary>
        /// <remarks>
        /// The list of input controls the state history will be recording for is specified on construction of the <see cref="InputStateHistory"/>
        /// </remarks>
        public ReadOnlyArray<InputControl> controls => new ReadOnlyArray<InputControl>(m_Controls, 0, m_ControlCount);

        /// <summary>
        /// Returns an entry in the state history at the given index.
        /// </summary>
        /// <param name="index">Index into the array.</param>
        /// <remarks>
        /// Returns a <see cref="Record"/> entry from the state history at the given index.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is less than 0 or greater than <see cref="Count"/>.</exception>
        public unsafe Record this[int index]
        {
            get
            {
                if (index < 0 || index >= m_RecordCount)
                    throw new ArgumentOutOfRangeException(
                        $"Index {index} is out of range for history with {m_RecordCount} entries", nameof(index));

                var recordIndex = UserIndexToRecordIndex(index);
                return new Record(this, recordIndex, GetRecord(recordIndex));
            }
            set
            {
                if (index < 0 || index >= m_RecordCount)
                    throw new ArgumentOutOfRangeException(
                        $"Index {index} is out of range for history with {m_RecordCount} entries", nameof(index));

                var recordIndex = UserIndexToRecordIndex(index);
                new Record(this, recordIndex, GetRecord(recordIndex)).CopyFrom(value);
            }
        }

        /// <summary>
        /// Optional delegate to perform when a record is added to the history array.
        /// </summary>
        /// <remarks>
        /// Can be used to fill in the extra memory with custom data using <see cref="Record.GetUnsafeExtraMemoryPtr"/>
        /// </remarks>
        public Action<Record> onRecordAdded { get; set; }

        /// <summary>
        /// Optional delegate to decide whether the state change should be stored in the history.
        /// </summary>
        /// <remarks>
        /// Can be used to filter out some events to focus on recording the ones you are most interested in.
        ///
        /// If the callback returns <c>true</c>, a record will be added to the history
        /// If the callback returns <c>false</c>, the event will be ignored and not recorded.
        /// </remarks>
        public Func<InputControl, double, InputEventPtr, bool> onShouldRecordStateChange { get; set; }

        /// <summary>
        /// Creates a new InputStateHistory class to record all control state changes.
        /// </summary>
        /// <param name="maxStateSizeInBytes">Maximum size of control state in the record entries. Controls with larger state will not be recorded.</param>
        /// <remarks>
        /// Creates a new InputStateHistory to record a history of control state changes.
        ///
        /// New controls are automatically added into the state history if their state is smaller than the threshold.
        /// </remarks>
        public InputStateHistory(int maxStateSizeInBytes)
        {
            if (maxStateSizeInBytes <= 0)
                throw new ArgumentException("State size must be >= 0", nameof(maxStateSizeInBytes));

            m_AddNewControls = true;
            m_StateSizeInBytes = maxStateSizeInBytes.AlignToMultipleOf(4);
        }

        /// <summary>
        /// Creates a new InputStateHistory class to record state changes for a specified control.
        /// </summary>
        /// <param name="path">Control path to identify which controls to monitor.</param>
        /// <remarks>
        /// Creates a new InputStateHistory to record a history of state changes for the specified controls.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Track all stick controls in the system.
        /// var history = new InputStateHistory("*/&lt;Stick&gt;");
        /// </code>
        /// </example>
        public InputStateHistory(string path)
        {
            using (var controls = InputSystem.FindControls(path))
            {
                m_Controls = controls.ToArray();
                m_ControlCount = m_Controls.Length;
            }
        }

        /// <summary>
        /// Creates a new InputStateHistory class to record state changes for a specified control.
        /// </summary>
        /// <param name="control">Control to monitor.</param>
        /// <remarks>
        /// Creates a new InputStateHistory to record a history of state changes for the specified control.
        /// </remarks>
        public InputStateHistory(InputControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            m_Controls = new[] {control};
            m_ControlCount = 1;
        }

        /// <summary>
        /// Creates a new InputStateHistory class to record state changes for a specified controls.
        /// </summary>
        /// <param name="controls">Controls to monitor.</param>
        /// <remarks>
        /// Creates a new InputStateHistory to record a history of state changes for the specified controls.
        /// </remarks>
        public InputStateHistory(IEnumerable<InputControl> controls)
        {
            if (controls != null)
            {
                m_Controls = controls.ToArray();
                m_ControlCount = m_Controls.Length;
            }
        }

        /// <summary>
        /// InputStateHistory destructor.
        /// </summary>
        ~InputStateHistory()
        {
            Dispose();
        }

        /// <summary>
        /// Clear the history record.
        /// </summary>
        /// <remarks>
        /// Clear the history record. Resetting the list to empty.
        ///
        /// This won't clear controls that have been added on the fly.
        /// </remarks>
        public void Clear()
        {
            m_HeadIndex = 0;
            m_RecordCount = 0;
            ++m_CurrentVersion;

            // NOTE: Won't clear controls that have been added on the fly.
        }

        /// <summary>
        /// Add a record to the input state history.
        /// </summary>
        /// <param name="record">Record to add.</param>
        /// <returns>The newly added record from the history array (as a copy is made).</returns>
        /// <remarks>
        /// Add a record to the input state history.
        /// Allocates an entry in the history array and returns this copy of the original data passed to the function.
        /// </remarks>
        public unsafe Record AddRecord(Record record)
        {
            var recordPtr = AllocateRecord(out var index);
            var newRecord = new Record(this, index, recordPtr);
            newRecord.CopyFrom(record);
            return newRecord;
        }

        /// <summary>
        /// Start recording state history for the specified controls.
        /// </summary>
        /// <remarks>
        /// Start recording state history for the controls specified in the <see cref="InputStateHistory"/> constructor.
        /// </remarks>
        /// <example>
        /// <code>
        /// using (var allTouchTaps = new InputStateHistory("&lt;Touchscreen&gt;/touch*/tap"))
        /// {
        ///     allTouchTaps.StartRecording();
        ///     allTouchTaps.StopRecording();
        /// }
        /// </code>
        /// </example>
        public void StartRecording()
        {
            // We defer allocation until we actually get values on a control.

            foreach (var control in controls)
                InputState.AddChangeMonitor(control, this);
        }

        /// <summary>
        /// Stop recording state history for the specified controls.
        /// </summary>
        /// <remarks>
        /// Stop recording state history for the controls specified in the <see cref="InputStateHistory"/> constructor.
        /// </remarks>
        /// <example>
        /// <code>
        /// using (var allTouchTaps = new InputStateHistory("&lt;Touchscreen&gt;/touch*/tap"))
        /// {
        ///     allTouchTaps.StartRecording();
        ///     allTouchTaps.StopRecording();
        /// }
        /// </code>
        /// </example>
        public void StopRecording()
        {
            foreach (var control in controls)
                InputState.RemoveChangeMonitor(control, this);
        }

        /// <summary>
        /// Record a state change for a specific control.
        /// </summary>
        /// <param name="control">The control to record the state change for.</param>
        /// <param name="eventPtr">The current event data to record.</param>
        /// <returns>The newly added record.</returns>
        /// <remarks>
        /// Record a state change for a specific control.
        /// Will call the <see cref="onRecordAdded"/> delegate after adding the record.
        /// Note this does not call the <see cref="onShouldRecordStateChange"/> delegate.
        /// </remarks>
        public unsafe Record RecordStateChange(InputControl control, InputEventPtr eventPtr)
        {
            if (eventPtr.IsA<DeltaStateEvent>())
                throw new NotImplementedException();

            if (!eventPtr.IsA<StateEvent>())
                throw new ArgumentException($"Event must be a state event but is '{eventPtr}' instead",
                    nameof(eventPtr));

            var statePtr = (byte*)StateEvent.From(eventPtr)->state - control.device.stateBlock.byteOffset;
            return RecordStateChange(control, statePtr, eventPtr.time);
        }

        /// <summary>
        /// Record a state change for a specific control.
        /// </summary>
        /// <param name="control">The control to record the state change for.</param>
        /// <param name="statePtr">The current state data to record.</param>
        /// <param name="time">Time stamp to apply (overriding the event timestamp)</param>
        /// <returns>The newly added record.</returns>
        /// <remarks>
        /// Record a state change for a specific control.
        /// Will call the <see cref="onRecordAdded"/> delegate after adding the record.
        /// Note this does not call the <see cref="onShouldRecordStateChange"/> delegate.
        /// </remarks>
        public unsafe Record RecordStateChange(InputControl control, void* statePtr, double time)
        {
            var controlIndex = ArrayHelpers.IndexOfReference(m_Controls, control, m_ControlCount);
            if (controlIndex == -1)
            {
                if (m_AddNewControls)
                {
                    if (control.stateBlock.alignedSizeInBytes > m_StateSizeInBytes)
                        throw new InvalidOperationException(
                            $"Cannot add control '{control}' with state larger than {m_StateSizeInBytes} bytes");
                    controlIndex = ArrayHelpers.AppendWithCapacity(ref m_Controls, ref m_ControlCount, control);
                }
                else
                    throw new ArgumentException($"Control '{control}' is not part of InputStateHistory",
                        nameof(control));
            }

            var recordPtr = AllocateRecord(out var index);
            recordPtr->time = time;
            recordPtr->version = ++m_CurrentVersion;
            var stateBufferPtr = recordPtr->statePtrWithoutControlIndex;
            if (m_ControlCount > 1 || m_AddNewControls)
            {
                // If there's multiple controls, write index of control to which the state change
                // pertains as an int before the state memory contents following it.
                recordPtr->controlIndex = controlIndex;
                stateBufferPtr = recordPtr->statePtrWithControlIndex;
            }

            var stateSize = control.stateBlock.alignedSizeInBytes;
            var stateOffset = control.stateBlock.byteOffset;

            UnsafeUtility.MemCpy(stateBufferPtr, (byte*)statePtr + stateOffset, stateSize);

            // Trigger callback.
            var record = new Record(this, index, recordPtr);
            onRecordAdded?.Invoke(record);

            return record;
        }

        /// <summary>
        /// Enumerate all state history records.
        /// </summary>
        /// <returns>An enumerator going over the state history records.</returns>
        /// <remarks>
        /// Enumerate all state history records.
        /// </remarks>
        /// <seealso cref="GetEnumerator"/>
        public IEnumerator<Record> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Enumerate all state history records.
        /// </summary>
        /// <returns>An enumerator going over the state history records.</returns>
        /// <remarks>
        /// Enumerate all state history records.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Dispose of the state history records.
        /// </summary>
        /// <remarks>
        /// Stops recording and cleans up the state history
        /// </remarks>
        public void Dispose()
        {
            StopRecording();
            Destroy();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Destroy the state history records.
        /// </summary>
        /// <remarks>
        /// Deletes the state history records.
        /// </remarks>
        protected void Destroy()
        {
            if (m_RecordBuffer.IsCreated)
            {
                m_RecordBuffer.Dispose();
                m_RecordBuffer = new NativeArray<byte>();
            }
        }

        private void Allocate()
        {
            // Find max size of state.
            if (!m_AddNewControls)
            {
                m_StateSizeInBytes = 0;
                foreach (var control in controls)
                    m_StateSizeInBytes = (int)Math.Max((uint)m_StateSizeInBytes, control.stateBlock.alignedSizeInBytes);
            }
            else
            {
                Debug.Assert(m_StateSizeInBytes > 0, "State size must be have initialized!");
            }

            // Allocate historyDepth times state blocks of the given max size. For each one
            // add space for the RecordHeader header.
            // NOTE: If we only have a single control, we omit storing the integer control index.
            var totalSizeOfBuffer = bytesPerRecord * m_HistoryDepth;
            m_RecordBuffer = new NativeArray<byte>(totalSizeOfBuffer, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
        }

        /// <summary>
        /// Remap a records internal index to an index from the start of the recording in the circular buffer.
        /// </summary>
        /// <remarks>
        /// Remap a records internal index, which is relative to the start of the record buffer,
        /// to an index relative to the start of the recording in the circular buffer.
        /// </remarks>
        /// <param name="index">Record index (from the start of the record array).</param>
        /// <returns>An index relative to the start of the recording in the circular buffer.</returns>
        protected internal int RecordIndexToUserIndex(int index)
        {
            if (index < m_HeadIndex)
                return m_HistoryDepth - m_HeadIndex + index;
            return index - m_HeadIndex;
        }

        /// <summary>
        /// Remap an index from the start of the recording in the circular buffer to a records internal index.
        /// </summary>
        /// <remarks>
        /// Remap an index relative to the start of the recording in the circular buffer,
        /// to a records internal index, which is relative to the start of the record buffer.
        /// </remarks>
        /// <param name="index">An index relative to the start of the recording in the circular buffer.</param>
        /// <returns>Record index (from the start of the record array).</returns>
        protected internal int UserIndexToRecordIndex(int index)
        {
            return (m_HeadIndex + index) % m_HistoryDepth;
        }

        /// <summary>
        /// Retrieve a record from the input state history.
        /// </summary>
        /// <remarks>
        /// Retrieve a record from the input state history by Record index.
        /// </remarks>
        /// <param name="index">Record index into the input state history records buffer.</param>
        /// <returns>The record header for the specified index</returns>
        /// <exception cref="InvalidOperationException">When the buffer is no longer valid as it has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the index is out of range of the history depth.</exception>
        protected internal unsafe RecordHeader* GetRecord(int index)
        {
            if (!m_RecordBuffer.IsCreated)
                throw new InvalidOperationException("History buffer has been disposed");
            if (index < 0 || index >= m_HistoryDepth)
                throw new ArgumentOutOfRangeException(nameof(index));
            return GetRecordUnchecked(index);
        }

        /// <summary>
        /// Retrieve a record from the input state history, without any bounds check.
        /// </summary>
        /// <remarks>
        /// Retrieve a record from the input state history by record index, without any bounds check
        /// </remarks>
        /// <param name="index">Record index into the input state history records buffer.</param>
        /// <returns>The record header for the specified index</returns>
        internal unsafe RecordHeader* GetRecordUnchecked(int index)
        {
            return (RecordHeader*)((byte*)m_RecordBuffer.GetUnsafePtr() + index * bytesPerRecord);
        }

        /// <summary>
        /// Allocate a new record in the input state history.
        /// </summary>
        /// <remarks>
        /// Allocate a new record in the input state history.
        /// </remarks>
        /// <param name="index">The index of the newly created record</param>
        /// <returns>The header of the newly created record</returns>
        protected internal unsafe RecordHeader* AllocateRecord(out int index)
        {
            if (!m_RecordBuffer.IsCreated)
                Allocate();

            index = (m_HeadIndex + m_RecordCount) % m_HistoryDepth;

            // If we're full, advance head to make room.
            if (m_RecordCount == m_HistoryDepth)
                m_HeadIndex = (m_HeadIndex + 1) % m_HistoryDepth;
            else
            {
                // We have a fixed max size given by the history depth and will start overwriting
                // older entries once we reached max size.
                ++m_RecordCount;
            }

            return (RecordHeader*)((byte*)m_RecordBuffer.GetUnsafePtr() + bytesPerRecord * index);
        }

        /// <summary>
        /// Returns value from the control in the specified record header.
        /// </summary>
        /// <param name="data">The record header to query.</param>
        /// <typeparam name="TValue">The type of the value being read</typeparam>
        /// <returns>The value from the record.</returns>
        /// <exception cref="InvalidOperationException">When the record is no longer value or the specified type is not present.</exception>
        protected unsafe TValue ReadValue<TValue>(RecordHeader* data)
            where TValue : struct
        {
            // Get control. If we only have a single one, the index isn't stored on the data.
            var haveSingleControl = m_ControlCount == 1 && !m_AddNewControls;
            var control = haveSingleControl ? controls[0] : controls[data->controlIndex];
            if (!(control is InputControl<TValue> controlOfType))
                throw new InvalidOperationException(
                    $"Cannot read value of type '{TypeHelpers.GetNiceTypeName(typeof(TValue))}' from control '{control}' with value type '{TypeHelpers.GetNiceTypeName(control.valueType)}'");

            // Grab state memory.
            var statePtr = haveSingleControl ? data->statePtrWithoutControlIndex : data->statePtrWithControlIndex;
            statePtr -= control.stateBlock.byteOffset;
            return controlOfType.ReadValueFromState(statePtr);
        }

        /// <summary>
        /// Read the control's final, processed value from the given state and return the value as an object.
        /// </summary>
        /// <param name="data">The record header to query.</param>
        /// <returns>The value of the control associated with the record header.</returns>
        /// <remarks>
        /// This method allocates GC memory and should not be used during normal gameplay operation.
        /// </remarks>
        /// <exception cref="InvalidOperationException">When the specified value is not present.</exception>
        protected unsafe object ReadValueAsObject(RecordHeader* data)
        {
            // Get control. If we only have a single one, the index isn't stored on the data.
            var haveSingleControl = m_ControlCount == 1 && !m_AddNewControls;
            var control = haveSingleControl ? controls[0] : controls[data->controlIndex];

            // Grab state memory.
            var statePtr = haveSingleControl ? data->statePtrWithoutControlIndex : data->statePtrWithControlIndex;
            statePtr -= control.stateBlock.byteOffset;
            return control.ReadValueFromStateAsObject(statePtr);
        }

        /// <summary>
        /// Delegate to list to control state change notifications.
        /// </summary>
        /// <param name="control">Control that is being monitored by the state change monitor and that had its state memory changed.</param>
        /// <param name="time">Time on the <see cref="InputEvent.time"/> timeline at which the control state change was received.</param>
        /// <param name="eventPtr">If the state change was initiated by a state event (either a <see cref="StateEvent"/>
        /// or <see cref="DeltaStateEvent"/>), this is the pointer to that event. Otherwise, it is pointer that is still
        /// <see cref="InputEventPtr.valid"/>, but refers a "dummy" event that is not a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</param>
        /// <param name="monitorIndex">Index of the monitor as passed to <see cref="InputState.AddChangeMonitor(InputControl,IInputStateChangeMonitor,long,uint)"/></param>
        /// <remarks>
        /// Records a state change after checking the <see cref="updateMask"/> and the <see cref="onShouldRecordStateChange"/> callback.
        /// </remarks>
        unsafe void IInputStateChangeMonitor.NotifyControlStateChanged(InputControl control, double time,
            InputEventPtr eventPtr, long monitorIndex)
        {
            // Ignore state change if it's in an input update we're not interested in.
            var currentUpdateType = InputState.currentUpdateType;
            var updateTypeMask = updateMask;
            if ((currentUpdateType & updateTypeMask) == 0)
                return;

            // Ignore state change if we have a filter and the state change doesn't pass the check.
            if (onShouldRecordStateChange != null && !onShouldRecordStateChange(control, time, eventPtr))
                return;

            RecordStateChange(control, control.currentStatePtr, time);
        }

        // Unused.
        /// <summary>
        /// Called when a timeout set on a state change monitor has expired.
        /// </summary>
        /// <param name="control">Control on which the timeout expired.</param>
        /// <param name="time">Input time at which the timer expired. This is the time at which an <see cref="InputSystem.Update"/> is being
        /// run whose <see cref="InputState.currentTime"/> is past the time of expiration.</param>
        /// <param name="monitorIndex">Index of the monitor as given to <see cref="InputState.AddChangeMonitor(InputControl,IInputStateChangeMonitor,long,uint)"/>.</param>
        /// <param name="timerIndex">Index of the timer as given to <see cref="InputState.AddChangeMonitorTimeout"/>.</param>
        /// <seealso cref="InputState.AddChangeMonitorTimeout"/>
        void IInputStateChangeMonitor.NotifyTimerExpired(InputControl control, double time, long monitorIndex,
            int timerIndex)
        {
        }

        internal InputControl[] m_Controls;
        internal int m_ControlCount;
        private NativeArray<byte> m_RecordBuffer;
        private int m_StateSizeInBytes;
        private int m_RecordCount;
        private int m_HistoryDepth = kDefaultHistorySize;
        private int m_ExtraMemoryPerRecord;
        internal int m_HeadIndex;
        internal uint m_CurrentVersion;
        private InputUpdateType? m_UpdateMask;
        internal readonly bool m_AddNewControls;

        internal int bytesPerRecord =>
            (m_StateSizeInBytes +
                m_ExtraMemoryPerRecord +
                (m_ControlCount == 1 && !m_AddNewControls
                    ? RecordHeader.kSizeWithoutControlIndex
                    : RecordHeader.kSizeWithControlIndex)).AlignToMultipleOf(4);

        private struct Enumerator : IEnumerator<Record>
        {
            private readonly InputStateHistory m_History;
            private int m_Index;

            public Enumerator(InputStateHistory history)
            {
                m_History = history;
                m_Index = -1;
            }

            public bool MoveNext()
            {
                if (m_Index + 1 >= m_History.Count)
                    return false;
                ++m_Index;
                return true;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            public Record Current => m_History[m_Index];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        /// <summary>State change record header</summary>
        /// <remarks>
        /// Input State change record header containing the timestamp and other common record data.
        /// Stored in the <see cref="InputStateHistory"/>.
        /// </remarks>
        /// <seealso cref="InputStateHistory"/>
        [StructLayout(LayoutKind.Explicit)]
        protected internal unsafe struct RecordHeader
        {
            /// <summary>
            /// The time stamp of the input state record.
            /// </summary>
            /// <remarks>
            /// The time stamp of the input state record in the owning container.
            /// <see cref="IInputRuntime.currentTime"/>
            /// </remarks>
            [FieldOffset(0)] public double time;

            /// <summary>
            /// The version of the input state record.
            /// </summary>
            /// <remarks>
            /// Current version stamp. See <see cref="InputStateHistory.version"/>.
            /// </remarks>
            [FieldOffset(8)] public uint version;

            /// <summary>
            /// The index of the record.
            /// </summary>
            /// <remarks>
            /// The index of the record relative to the start of the buffer.
            /// See <see cref="InputStateHistory.RecordIndexToUserIndex"/> to remap this record index to a user index.
            /// </remarks>
            [FieldOffset(12)] public int controlIndex;

            [FieldOffset(12)] private fixed byte m_StateWithoutControlIndex[1];
            [FieldOffset(16)] private fixed byte m_StateWithControlIndex[1];

            /// <summary>
            /// The state data including the control index.
            /// </summary>
            /// <remarks>
            /// The state data including the control index.
            /// </remarks>
            public byte* statePtrWithControlIndex
            {
                get
                {
                    fixed(byte* ptr = m_StateWithControlIndex)
                    return ptr;
                }
            }

            /// <summary>
            /// The state data excluding the control index.
            /// </summary>
            /// <remarks>
            /// The state data excluding the control index.
            /// </remarks>
            public byte* statePtrWithoutControlIndex
            {
                get
                {
                    fixed(byte* ptr = m_StateWithoutControlIndex)
                    return ptr;
                }
            }

            /// <summary>
            /// Size of the state data including the control index.
            /// </summary>
            /// <remarks>
            /// Size of the data including the control index.
            /// </remarks>
            public const int kSizeWithControlIndex = 16;

            /// <summary>
            /// Size of the state data excluding the control index.
            /// </summary>
            /// <remarks>
            /// Size of the data excluding the control index.
            /// </remarks>
            public const int kSizeWithoutControlIndex = 12;
        }

        /// <summary>State change record</summary>
        /// <remarks>Input State change record stored in the <see cref="InputStateHistory"/>.</remarks>
        /// <seealso cref="InputStateHistory"/>
        public unsafe struct Record : IEquatable<Record>
        {
            // We store an index rather than a direct pointer to make this struct safer to use.
            private readonly InputStateHistory m_Owner;
            private readonly int m_IndexPlusOne; // Plus one so that default(int) works for us.
            private uint m_Version;

            internal RecordHeader* header => m_Owner.GetRecord(recordIndex);
            internal int recordIndex => m_IndexPlusOne - 1;
            internal uint version => m_Version;

            /// <summary>
            /// Identifies if the record is valid.
            /// </summary>
            /// <value>True if the record is a valid entry. False if invalid.</value>
            /// <remarks>
            /// When the history is cleared with <see cref="Clear"/> the entries become invalid.
            /// </remarks>
            public bool valid => m_Owner != default && m_IndexPlusOne != default && header->version == m_Version;

            /// <summary>
            /// Identifies the owning container for the record.
            /// </summary>
            /// <value>The owning <see cref="InputStateHistory"/> container for the record.</value>
            /// <remarks>
            /// Identifies the owning <see cref="InputStateHistory"/> container for the record.
            /// </remarks>
            public InputStateHistory owner => m_Owner;

            /// <summary>
            /// The index of the input state record in the owning container.
            /// </summary>
            /// <value>
            /// The index of the input state record in the owning container.
            /// </value>
            /// <exception cref="InvalidOperationException">When the record is no longer value.</exception>
            public int index
            {
                get
                {
                    CheckValid();
                    return m_Owner.RecordIndexToUserIndex(recordIndex);
                }
            }

            /// <summary>
            /// The time stamp of the input state record.
            /// </summary>
            /// <value>
            /// The time stamp of the input state record in the owning container.
            /// <see cref="IInputRuntime.currentTime"/>
            /// </value>
            /// <exception cref="InvalidOperationException">When the record is no longer value.</exception>
            public double time
            {
                get
                {
                    CheckValid();
                    return header->time;
                }
            }

            /// <summary>
            /// The control associated with the input state record.
            /// </summary>
            /// <value>
            /// The control associated with the input state record.
            /// </value>
            /// <exception cref="InvalidOperationException">When the record is no longer value.</exception>
            public InputControl control
            {
                get
                {
                    CheckValid();
                    var controls = m_Owner.controls;
                    if (controls.Count == 1 && !m_Owner.m_AddNewControls)
                        return controls[0];
                    return controls[header->controlIndex];
                }
            }

            /// <summary>
            /// The next input state record in the owning container.
            /// </summary>
            /// <value>
            /// The next input state record in the owning <see cref="InputStateHistory"/>container.
            /// </value>
            /// <exception cref="InvalidOperationException">When the record is no longer value.</exception>
            public Record next
            {
                get
                {
                    CheckValid();
                    var userIndex = m_Owner.RecordIndexToUserIndex(this.recordIndex);
                    if (userIndex + 1 >= m_Owner.Count)
                        return default;
                    var recordIndex = m_Owner.UserIndexToRecordIndex(userIndex + 1);
                    return new Record(m_Owner, recordIndex, m_Owner.GetRecord(recordIndex));
                }
            }

            /// <summary>
            /// The previous input state record in the owning container.
            /// </summary>
            /// <value>
            /// The previous input state record in the owning <see cref="InputStateHistory"/>container.
            /// </value>
            /// <exception cref="InvalidOperationException">When the record is no longer value.</exception>
            public Record previous
            {
                get
                {
                    CheckValid();
                    var userIndex = m_Owner.RecordIndexToUserIndex(this.recordIndex);
                    if (userIndex - 1 < 0)
                        return default;
                    var recordIndex = m_Owner.UserIndexToRecordIndex(userIndex - 1);
                    return new Record(m_Owner, recordIndex, m_Owner.GetRecord(recordIndex));
                }
            }

            internal Record(InputStateHistory owner, int index, RecordHeader* header)
            {
                m_Owner = owner;
                m_IndexPlusOne = index + 1;
                m_Version = header->version;
            }

            /// <summary>
            /// Returns value from the control in the record.
            /// </summary>
            /// <typeparam name="TValue">The type of the value being read</typeparam>
            /// <returns>Returns the value from the record.</returns>
            /// <exception cref="InvalidOperationException">When the record is no longer value or the specified type is not present.</exception>
            public TValue ReadValue<TValue>()
                where TValue : struct
            {
                CheckValid();
                return m_Owner.ReadValue<TValue>(header);
            }

            /// <summary>
            /// Read the control's final, processed value from the given state and return the value as an object.
            /// </summary>
            /// <returns>The value of the control associated with the record.</returns>
            /// <remarks>
            /// This method allocates GC memory and should not be used during normal gameplay operation.
            /// </remarks>
            /// <exception cref="InvalidOperationException">When the specified value is not present.</exception>
            public object ReadValueAsObject()
            {
                CheckValid();
                return m_Owner.ReadValueAsObject(header);
            }

            /// <summary>
            /// Read the state memory for the record.
            /// </summary>
            /// <returns>The state memory for the record.</returns>
            /// <remarks>
            /// Read the state memory for the record.
            /// </remarks>
            public void* GetUnsafeMemoryPtr()
            {
                CheckValid();
                return GetUnsafeMemoryPtrUnchecked();
            }

            internal void* GetUnsafeMemoryPtrUnchecked()
            {
                if (m_Owner.controls.Count == 1 && !m_Owner.m_AddNewControls)
                    return header->statePtrWithoutControlIndex;
                return header->statePtrWithControlIndex;
            }

            /// <summary>
            /// Read the extra memory for the record.
            /// </summary>
            /// <returns>The extra memory for the record.</returns>
            /// <remarks>
            /// Additional date can be stored in a record in the extra memory section.
            /// </remarks>
            /// <seealso cref="InputStateHistory.extraMemoryPerRecord"/>
            public void* GetUnsafeExtraMemoryPtr()
            {
                CheckValid();
                return GetUnsafeExtraMemoryPtrUnchecked();
            }

            internal void* GetUnsafeExtraMemoryPtrUnchecked()
            {
                if (m_Owner.extraMemoryPerRecord == 0)
                    throw new InvalidOperationException("No extra memory has been set up for history records; set extraMemoryPerRecord");
                return (byte*)header + m_Owner.bytesPerRecord - m_Owner.extraMemoryPerRecord;
            }

            /// <summary>Copy data from one record to another.</summary>
            /// <param name="record">Source record to copy from.</param>
            /// <remarks>
            /// Copy data from one record to another.
            /// </remarks>
            /// <exception cref="ArgumentException">When the source record history is not valid.</exception>
            /// <exception cref="InvalidOperationException">When the control is not tracked by the owning <see cref="InputStateHistory"/> container.</exception>
            public void CopyFrom(Record record)
            {
                if (!record.valid)
                    throw new ArgumentException("Given history record is not valid", nameof(record));
                CheckValid();

                // Find control.
                var control = record.control;
                var controlIndex = m_Owner.controls.IndexOfReference(control);
                if (controlIndex == -1)
                {
                    // We haven't found it. Throw if we can't add it.
                    if (!m_Owner.m_AddNewControls)
                        throw new InvalidOperationException($"Control '{record.control}' is not tracked by target history");

                    controlIndex =
                        ArrayHelpers.AppendWithCapacity(ref m_Owner.m_Controls, ref m_Owner.m_ControlCount, control);
                }

                // Make sure memory sizes match.
                var numBytesForState = m_Owner.m_StateSizeInBytes;
                if (numBytesForState != record.m_Owner.m_StateSizeInBytes)
                    throw new InvalidOperationException(
                        $"Cannot copy record from owner with state size '{record.m_Owner.m_StateSizeInBytes}' to owner with state size '{numBytesForState}'");

                // Copy and update header.
                var thisRecordPtr = header;
                var otherRecordPtr = record.header;
                UnsafeUtility.MemCpy(thisRecordPtr, otherRecordPtr, RecordHeader.kSizeWithoutControlIndex);
                thisRecordPtr->version = ++m_Owner.m_CurrentVersion;
                m_Version = thisRecordPtr->version;

                // Copy state.
                var dstPtr = thisRecordPtr->statePtrWithoutControlIndex;
                if (m_Owner.controls.Count > 1 || m_Owner.m_AddNewControls)
                {
                    thisRecordPtr->controlIndex = controlIndex;
                    dstPtr = thisRecordPtr->statePtrWithControlIndex;
                }
                var srcPtr = record.m_Owner.m_ControlCount > 1 || record.m_Owner.m_AddNewControls
                    ? otherRecordPtr->statePtrWithControlIndex
                    : otherRecordPtr->statePtrWithoutControlIndex;
                UnsafeUtility.MemCpy(dstPtr, srcPtr, numBytesForState);

                // Copy extra memory, but only if the size in the source and target
                // history are identical.
                var numBytesExtraMemory = m_Owner.m_ExtraMemoryPerRecord;
                if (numBytesExtraMemory > 0 && numBytesExtraMemory == record.m_Owner.m_ExtraMemoryPerRecord)
                    UnsafeUtility.MemCpy(GetUnsafeExtraMemoryPtr(), record.GetUnsafeExtraMemoryPtr(),
                        numBytesExtraMemory);

                // Notify.
                m_Owner.onRecordAdded?.Invoke(this);
            }

            internal void CheckValid()
            {
                if (m_Owner == default || m_IndexPlusOne == default)
                    throw new InvalidOperationException("Value not initialized");
                ////TODO: need to check whether memory has been disposed
                if (header->version != m_Version)
                    throw new InvalidOperationException("Record is no longer valid");
            }

            /// <summary>Compare two records.</summary>
            /// <remarks>Compare two records.</remarks>
            /// <param name="other">The record to compare with.</param>
            /// <returns>True if the records are the same, False if they differ.</returns>
            public bool Equals(Record other)
            {
                return ReferenceEquals(m_Owner, other.m_Owner) && m_IndexPlusOne == other.m_IndexPlusOne && m_Version == other.m_Version;
            }

            /// <summary>Compare two records.</summary>
            /// <remarks>Compare two records.</remarks>
            /// <param name="obj">The record to compare with.</param>
            /// <returns>True if the records are the same, False if they differ.</returns>
            public override bool Equals(object obj)
            {
                return obj is Record other && Equals(other);
            }

            /// <summary>Return the hash code of the record.</summary>
            /// <remarks>Return the hash code of the record.</remarks>
            /// <returns>The hash code of the record.</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = m_Owner != null ? m_Owner.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ m_IndexPlusOne;
                    hashCode = (hashCode * 397) ^ (int)m_Version;
                    return hashCode;
                }
            }

            /// <summary>Return the string representation of the record.</summary>
            /// <remarks>Includes the control, value and time of the record (or &lt;Invalid&gt; if not valid).</remarks>
            /// <returns>The string representation of the record.</returns>
            public override string ToString()
            {
                if (!valid)
                    return "<Invalid>";

                return $"{{ control={control} value={ReadValueAsObject()} time={time} }}";
            }
        }
    }

    /// <summary>
    /// Records value changes of a given control over time.
    /// </summary>
    /// <typeparam name="TValue">The type of the record being stored</typeparam>
    /// <remarks>
    /// This class makes it easy to track input values over time. It will automatically retain input state up to a given
    /// maximum history depth (<see cref="InputStateHistory.historyDepth"/>). When the history is full, it will start overwriting the oldest
    /// entry each time a new history record is received.
    ///
    /// The class listens to changes on the given controls by adding change monitors (<see cref="IInputStateChangeMonitor"/>)
    /// to each control.
    /// </remarks>
    public class InputStateHistory<TValue> : InputStateHistory, IReadOnlyList<InputStateHistory<TValue>.Record>
        where TValue : struct
    {
        /// <summary>
        /// Creates a new InputStateHistory class to record all control state changes.
        /// </summary>
        /// <param name="maxStateSizeInBytes">Maximum size of control state in the record entries. Controls with larger state will not be recorded.</param>
        /// <remarks>
        /// Creates a new InputStateHistory to record a history of control state changes.
        ///
        /// New controls are automatically added into the state history if there state is smaller than the threshold.
        /// </remarks>
        public InputStateHistory(int? maxStateSizeInBytes = null)
        // Using the size of the value here isn't quite correct but the value is used as an upper
        // bound on stored state size for which the size of the value should be a reasonable guess.
            : base(maxStateSizeInBytes ?? UnsafeUtility.SizeOf<TValue>())
        {
            if (maxStateSizeInBytes < UnsafeUtility.SizeOf<TValue>())
                throw new ArgumentException("Max state size cannot be smaller than sizeof(TValue)", nameof(maxStateSizeInBytes));
        }

        /// <summary>
        /// Creates a new InputStateHistory class to record state changes for a specified control.
        /// </summary>
        /// <param name="control">Control to monitor.</param>
        /// <remarks>
        /// Creates a new InputStateHistory to record a history of state changes for the specified control.
        /// </remarks>
        public InputStateHistory(InputControl<TValue> control)
            : base(control)
        {
        }

        /// <summary>
        /// Creates a new InputStateHistory class to record state changes for a specified control.
        /// </summary>
        /// <param name="path">Control path to identify which controls to monitor.</param>
        /// <remarks>
        /// Creates a new InputStateHistory to record a history of state changes for the specified controls.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Track all stick controls in the system.
        /// var history = new InputStateHistory&lt;Vector2&gt;("*/&lt;Stick&gt;");
        /// </code>
        /// </example>
        public InputStateHistory(string path)
            : base(path)
        {
            // Make sure that the value type of all matched controls is compatible with TValue.
            foreach (var control in controls)
                if (!typeof(TValue).IsAssignableFrom(control.valueType))
                    throw new ArgumentException(
                        $"Control '{control}' matched by '{path}' has value type '{TypeHelpers.GetNiceTypeName(control.valueType)}' which is incompatible with '{TypeHelpers.GetNiceTypeName(typeof(TValue))}'");
        }

        /// <summary>
        /// InputStateHistory destructor.
        /// </summary>
        ~InputStateHistory()
        {
            Destroy();
        }

        /// <summary>
        /// Add a record to the input state history.
        /// </summary>
        /// <param name="record">Record to add.</param>
        /// <returns>The newly added record from the history array (as a copy is made).</returns>
        /// <remarks>
        /// Add a record to the input state history.
        /// Allocates an entry in the history array and returns this copy of the original data passed to the function.
        /// </remarks>
        public unsafe Record AddRecord(Record record)
        {
            var recordPtr = AllocateRecord(out var index);
            var newRecord = new Record(this, index, recordPtr);
            newRecord.CopyFrom(record);
            return newRecord;
        }

        /// <summary>
        /// Record a state change for a specific control.
        /// </summary>
        /// <param name="control">The control to record the state change for.</param>
        /// <param name="value">The value to record.</param>
        /// <param name="time">Time stamp to apply (overriding the event timestamp)</param>
        /// <returns>The newly added record.</returns>
        /// <remarks>
        /// Record a state change for a specific control.
        /// Will call the <see cref="InputStateHistory.onRecordAdded"/> delegate after adding the record.
        /// Note this does not call the <see cref="InputStateHistory.onShouldRecordStateChange"/> delegate.
        /// </remarks>
        /// <example>
        /// <code>
        /// using (var allTouchTaps = new InputStateHistory&lt;float&gt;(Gamepad.current.leftTrigger))
        /// {
        ///     history.RecordStateChange(Gamepad.current.leftTrigger, 0.234f);
        /// }
        /// </code>
        /// </example>
        public unsafe Record RecordStateChange(InputControl<TValue> control, TValue value, double time = -1)
        {
            using (StateEvent.From(control.device, out var eventPtr))
            {
                var statePtr = (byte*)StateEvent.From(eventPtr)->state - control.device.stateBlock.byteOffset;
                control.WriteValueIntoState(value, statePtr);
                if (time >= 0)
                    eventPtr.time = time;
                var record = RecordStateChange(control, eventPtr);
                return new Record(this, record.recordIndex, record.header);
            }
        }

        /// <summary>
        /// Enumerate all state history records.
        /// </summary>
        /// <returns>An enumerator going over the state history records.</returns>
        /// <remarks>
        /// Enumerate all state history records.
        /// </remarks>
        /// <seealso cref="GetEnumerator"/>
        public new IEnumerator<Record> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Enumerate all state history records.
        /// </summary>
        /// <returns>An enumerator going over the state history records.</returns>
        /// <remarks>
        /// Enumerate all state history records.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an entry in the state history at the given index.
        /// </summary>
        /// <param name="index">Index into the array.</param>
        /// <remarks>
        /// Returns a <see cref="Record"/> entry from the state history at the given index.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is less than 0 or greater than <see cref="InputStateHistory.Count"/>.</exception>
        public new unsafe Record this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(
                        $"Index {index} is out of range for history with {Count} entries", nameof(index));

                var recordIndex = UserIndexToRecordIndex(index);
                return new Record(this, recordIndex, GetRecord(recordIndex));
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(
                        $"Index {index} is out of range for history with {Count} entries", nameof(index));
                var recordIndex = UserIndexToRecordIndex(index);
                new Record(this, recordIndex, GetRecord(recordIndex)).CopyFrom(value);
            }
        }

        private struct Enumerator : IEnumerator<Record>
        {
            private readonly InputStateHistory<TValue> m_History;
            private int m_Index;

            public Enumerator(InputStateHistory<TValue> history)
            {
                m_History = history;
                m_Index = -1;
            }

            public bool MoveNext()
            {
                if (m_Index + 1 >= m_History.Count)
                    return false;
                ++m_Index;
                return true;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            public Record Current => m_History[m_Index];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        /// <summary>State change record</summary>
        /// <remarks>Input State change record stored in the <see cref="InputStateHistory{TValue}"/></remarks>
        /// <seealso cref="InputStateHistory{TValue}"/>
        public new unsafe struct Record : IEquatable<Record>
        {
            private readonly InputStateHistory<TValue> m_Owner;
            private readonly int m_IndexPlusOne;
            private uint m_Version;

            internal RecordHeader* header => m_Owner.GetRecord(recordIndex);
            internal int recordIndex => m_IndexPlusOne - 1;

            /// <summary>
            /// Identifies if the record is valid.
            /// </summary>
            /// <value>True if the record is a valid entry. False if invalid.</value>
            /// <remarks>
            /// When the history is cleared with <see cref="InputStateHistory.Clear"/> the entries become invalid.
            /// </remarks>
            public bool valid => m_Owner != default && m_IndexPlusOne != default && header->version == m_Version;

            /// <summary>
            /// Identifies the owning container for the record.
            /// </summary>
            /// <value>The owning <see cref="InputStateHistory"/> container for the record.</value>
            /// <remarks>
            /// Identifies the owning <see cref="InputStateHistory"/> container for the record.
            /// </remarks>
            public InputStateHistory<TValue> owner => m_Owner;

            /// <summary>
            /// The index of the input state record in the owning container.
            /// </summary>
            /// <value>
            /// The index of the input state record in the owning container.
            /// </value>
            /// <exception cref="InvalidOperationException">When the record is no longer value.</exception>
            public int index
            {
                get
                {
                    CheckValid();
                    return m_Owner.RecordIndexToUserIndex(recordIndex);
                }
            }

            /// <summary>
            /// The time stamp of the input state record.
            /// </summary>
            /// <value>
            /// The time stamp of the input state record in the owning container.
            /// <see cref="IInputRuntime.currentTime"/>
            /// </value>
            /// <exception cref="InvalidOperationException">When the record is no longer value.</exception>
            public double time
            {
                get
                {
                    CheckValid();
                    return header->time;
                }
            }

            /// <summary>
            /// The control associated with the input state record.
            /// </summary>
            /// <value>
            /// The control associated with the input state record.
            /// </value>
            /// <exception cref="InvalidOperationException">When the record is no longer value.</exception>
            public InputControl<TValue> control
            {
                get
                {
                    CheckValid();
                    var controls = m_Owner.controls;
                    if (controls.Count == 1 && !m_Owner.m_AddNewControls)
                        return (InputControl<TValue>)controls[0];
                    return (InputControl<TValue>)controls[header->controlIndex];
                }
            }

            /// <summary>
            /// The next input state record in the owning container.
            /// </summary>
            /// <value>
            /// The next input state record in the owning <see cref="InputStateHistory{TValue}"/>container.
            /// </value>
            /// <exception cref="InvalidOperationException">When the record is no longer value.</exception>
            public Record next
            {
                get
                {
                    CheckValid();
                    var userIndex = m_Owner.RecordIndexToUserIndex(this.recordIndex);
                    if (userIndex + 1 >= m_Owner.Count)
                        return default;
                    var recordIndex = m_Owner.UserIndexToRecordIndex(userIndex + 1);
                    return new Record(m_Owner, recordIndex, m_Owner.GetRecord(recordIndex));
                }
            }

            /// <summary>
            /// The previous input state record in the owning container.
            /// </summary>
            /// <value>
            /// The previous input state record in the owning <see cref="InputStateHistory{TValue}"/>container.
            /// </value>
            /// <exception cref="InvalidOperationException">When the record is no longer value.</exception>
            public Record previous
            {
                get
                {
                    CheckValid();
                    var userIndex = m_Owner.RecordIndexToUserIndex(this.recordIndex);
                    if (userIndex - 1 < 0)
                        return default;
                    var recordIndex = m_Owner.UserIndexToRecordIndex(userIndex - 1);
                    return new Record(m_Owner, recordIndex, m_Owner.GetRecord(recordIndex));
                }
            }

            internal Record(InputStateHistory<TValue> owner, int index, RecordHeader* header)
            {
                m_Owner = owner;
                m_IndexPlusOne = index + 1;
                m_Version = header->version;
            }

            internal Record(InputStateHistory<TValue> owner, int index)
            {
                m_Owner = owner;
                m_IndexPlusOne = index + 1;
                m_Version = default;
            }

            /// <summary>
            /// Returns value from the control in the Record.
            /// </summary>
            /// <returns>Returns value from the Record.</returns>
            /// <exception cref="InvalidOperationException">When the record is no longer value or the specified type is not present.</exception>
            public TValue ReadValue()
            {
                CheckValid();
                return m_Owner.ReadValue<TValue>(header);
            }

            /// <summary>
            /// Read the state memory for the record.
            /// </summary>
            /// <returns>The state memory for the record.</returns>
            /// <remarks>
            /// Read the state memory for the record.
            /// </remarks>
            public void* GetUnsafeMemoryPtr()
            {
                CheckValid();
                return GetUnsafeMemoryPtrUnchecked();
            }

            internal void* GetUnsafeMemoryPtrUnchecked()
            {
                if (m_Owner.controls.Count == 1 && !m_Owner.m_AddNewControls)
                    return header->statePtrWithoutControlIndex;
                return header->statePtrWithControlIndex;
            }

            /// <summary>
            /// Read the extra memory for the record.
            /// </summary>
            /// <returns>The extra memory for the record.</returns>
            /// <remarks>
            /// Additional date can be stored in a record in the extra memory section.
            /// </remarks>
            /// <seealso cref="InputStateHistory.extraMemoryPerRecord"/>
            public void* GetUnsafeExtraMemoryPtr()
            {
                CheckValid();
                return GetUnsafeExtraMemoryPtrUnchecked();
            }

            internal void* GetUnsafeExtraMemoryPtrUnchecked()
            {
                if (m_Owner.extraMemoryPerRecord == 0)
                    throw new InvalidOperationException("No extra memory has been set up for history records; set extraMemoryPerRecord");
                return (byte*)header + m_Owner.bytesPerRecord - m_Owner.extraMemoryPerRecord;
            }

            /// <summary>Copy data from one record to another.</summary>
            /// <param name="record">Source Record to copy from.</param>
            /// <remarks>
            /// Copy data from one record to another.
            /// </remarks>
            /// <exception cref="ArgumentException">When the source record history is not valid.</exception>
            /// <exception cref="InvalidOperationException">When the control is not tracked by the owning <see cref="InputStateHistory{TValue}"/> container.</exception>
            public void CopyFrom(Record record)
            {
                CheckValid();
                if (!record.valid)
                    throw new ArgumentException("Given history record is not valid", nameof(record));
                var temp = new InputStateHistory.Record(m_Owner, recordIndex, header);
                temp.CopyFrom(new InputStateHistory.Record(record.m_Owner, record.recordIndex, record.header));
                m_Version = temp.version;
            }

            private void CheckValid()
            {
                if (m_Owner == default || m_IndexPlusOne == default)
                    throw new InvalidOperationException("Value not initialized");
                if (header->version != m_Version)
                    throw new InvalidOperationException("Record is no longer valid");
            }

            /// <summary>Compare two records.</summary>
            /// <remarks>Compare two records.</remarks>
            /// <param name="other">The record to compare with.</param>
            /// <returns>True if the records are the same, False if they differ.</returns>
            public bool Equals(Record other)
            {
                return ReferenceEquals(m_Owner, other.m_Owner) && m_IndexPlusOne == other.m_IndexPlusOne && m_Version == other.m_Version;
            }

            /// <summary>Compare two records.</summary>
            /// <remarks>Compare two records.</remarks>
            /// <param name="obj">The record to compare with.</param>
            /// <returns>True if the records are the same, False if they differ.</returns>
            public override bool Equals(object obj)
            {
                return obj is Record other && Equals(other);
            }

            /// <summary>Return the hash code of the record.</summary>
            /// <remarks>Return the hash code of the record.</remarks>
            /// <returns>The hash code of the record.</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = m_Owner != null ? m_Owner.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ m_IndexPlusOne;
                    hashCode = (hashCode * 397) ^ (int)m_Version;
                    return hashCode;
                }
            }

            /// <summary>Return the string representation of the record.</summary>
            /// <remarks>Includes the control, value and time of the record (or &lt;Invalid&gt; if not valid).</remarks>
            /// <returns>The string representation of the record.</returns>
            public override string ToString()
            {
                if (!valid)
                    return "<Invalid>";

                return $"{{ control={control} value={ReadValue()} time={time} }}";
            }
        }
    }
}
