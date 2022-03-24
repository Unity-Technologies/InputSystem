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

////REVIEW: should this enumerate *backwards* in time rather than *forwards*?

////TODO: allow correlating history to frames/updates

////TODO: add ability to grow such that you can set it to e.g. record up to 4 seconds of history and it will automatically keep the buffer size bounded

////REVIEW: should we align the extra memory on a 4 byte boundary?

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
    ///
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
    /// </remarks>
    public class InputStateHistory : IDisposable, IEnumerable<InputStateHistory.Record>, IInputStateChangeMonitor
    {
        private const int kDefaultHistorySize = 128;

        /// <summary>
        /// Total number of state records currently captured in the history.
        /// </summary>
        /// <value>Number of records in the collection.</value>
        /// <remarks>
        /// This will always be at most <see cref="historyDepth"/>.
        /// </remarks>
        /// <seealso cref="historyDepth"/>
        /// <seealso cref="RecordStateChange(InputControl,InputEventPtr)"/>
        public int Count => m_RecordCount;

        /// <summary>
        /// Current version stamp. Every time a record is stored in the history,
        /// this is incremented by one.
        /// </summary>
        /// <value>Version stamp that indicates the number of mutations.</value>
        /// <seealso cref="RecordStateChange(InputControl,InputEventPtr)"/>
        public uint version => m_CurrentVersion;

        /// <summary>
        /// Maximum number of records that can be recorded in the history.
        /// </summary>
        /// <value>Upper limit on number of records.</value>
        /// <exception cref="ArgumentException"><paramref name="value"/> is negative.</exception>
        /// <remarks>
        /// A fixed size memory block of unmanaged memory will be allocated to store history
        /// records. This property determines TODO
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

        public ReadOnlyArray<InputControl> controls => new ReadOnlyArray<InputControl>(m_Controls, 0, m_ControlCount);

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

        public Action<Record> onRecordAdded { get; set; }
        public Func<InputControl, double, InputEventPtr, bool> onShouldRecordStateChange { get; set; }

        public InputStateHistory(int maxStateSizeInBytes)
        {
            if (maxStateSizeInBytes <= 0)
                throw new ArgumentException("State size must be >= 0", nameof(maxStateSizeInBytes));

            m_AddNewControls = true;
            m_StateSizeInBytes = maxStateSizeInBytes.AlignToMultipleOf(4);
        }

        public InputStateHistory(string path)
        {
            using (var controls = InputSystem.FindControls(path))
            {
                m_Controls = controls.ToArray();
                m_ControlCount = m_Controls.Length;
            }
        }

        public InputStateHistory(InputControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            m_Controls = new[] {control};
            m_ControlCount = 1;
        }

        public InputStateHistory(IEnumerable<InputControl> controls)
        {
            if (controls != null)
            {
                m_Controls = controls.ToArray();
                m_ControlCount = m_Controls.Length;
            }
        }

        ~InputStateHistory()
        {
            Dispose();
        }

        public void Clear()
        {
            m_HeadIndex = 0;
            m_RecordCount = 0;
            ++m_CurrentVersion;

            // NOTE: Won't clear controls that have been added on the fly.
        }

        public unsafe Record AddRecord(Record record)
        {
            var recordPtr = AllocateRecord(out var index);
            var newRecord = new Record(this, index, recordPtr);
            newRecord.CopyFrom(record);
            return newRecord;
        }

        public void StartRecording()
        {
            // We defer allocation until we actually get values on a control.

            foreach (var control in controls)
                InputState.AddChangeMonitor(control, this);
        }

        public void StopRecording()
        {
            foreach (var control in controls)
                InputState.RemoveChangeMonitor(control, this);
        }

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

        public IEnumerator<Record> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            StopRecording();
            Destroy();
            GC.SuppressFinalize(this);
        }

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

        protected internal int RecordIndexToUserIndex(int index)
        {
            if (index < m_HeadIndex)
                return m_HistoryDepth - m_HeadIndex + index;
            return index - m_HeadIndex;
        }

        protected internal int UserIndexToRecordIndex(int index)
        {
            return (m_HeadIndex + index) % m_HistoryDepth;
        }

        protected internal unsafe RecordHeader* GetRecord(int index)
        {
            if (!m_RecordBuffer.IsCreated)
                throw new InvalidOperationException("History buffer has been disposed");
            if (index < 0 || index >= m_HistoryDepth)
                throw new ArgumentOutOfRangeException(nameof(index));
            return GetRecordUnchecked(index);
        }

        internal unsafe RecordHeader* GetRecordUnchecked(int index)
        {
            return (RecordHeader*)((byte*)m_RecordBuffer.GetUnsafePtr() + index * bytesPerRecord);
        }

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

        [StructLayout(LayoutKind.Explicit)]
        protected internal unsafe struct RecordHeader
        {
            [FieldOffset(0)] public double time;
            [FieldOffset(8)] public uint version;
            [FieldOffset(12)] public int controlIndex;

            [FieldOffset(12)] private fixed byte m_StateWithoutControlIndex[1];
            [FieldOffset(16)] private fixed byte m_StateWithControlIndex[1];

            public byte* statePtrWithControlIndex
            {
                get
                {
                    fixed(byte* ptr = m_StateWithControlIndex)
                    return ptr;
                }
            }

            public byte* statePtrWithoutControlIndex
            {
                get
                {
                    fixed(byte* ptr = m_StateWithoutControlIndex)
                    return ptr;
                }
            }

            public const int kSizeWithControlIndex = 16;
            public const int kSizeWithoutControlIndex = 12;
        }

        public unsafe struct Record : IEquatable<Record>
        {
            // We store an index rather than a direct pointer to make this struct safer to use.
            private readonly InputStateHistory m_Owner;
            private readonly int m_IndexPlusOne; // Plus one so that default(int) works for us.
            private uint m_Version;

            internal RecordHeader* header => m_Owner.GetRecord(recordIndex);
            internal int recordIndex => m_IndexPlusOne - 1;
            internal uint version => m_Version;

            public bool valid => m_Owner != default && m_IndexPlusOne != default && header->version == m_Version;

            public InputStateHistory owner => m_Owner;

            public int index
            {
                get
                {
                    CheckValid();
                    return m_Owner.RecordIndexToUserIndex(recordIndex);
                }
            }

            public double time
            {
                get
                {
                    CheckValid();
                    return header->time;
                }
            }

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

            public TValue ReadValue<TValue>()
                where TValue : struct
            {
                CheckValid();
                return m_Owner.ReadValue<TValue>(header);
            }

            public object ReadValueAsObject()
            {
                CheckValid();
                return m_Owner.ReadValueAsObject(header);
            }

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

            public bool Equals(Record other)
            {
                return ReferenceEquals(m_Owner, other.m_Owner) && m_IndexPlusOne == other.m_IndexPlusOne && m_Version == other.m_Version;
            }

            public override bool Equals(object obj)
            {
                return obj is Record other && Equals(other);
            }

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
    /// <typeparam name="TValue"></typeparam>
    public class InputStateHistory<TValue> : InputStateHistory, IReadOnlyList<InputStateHistory<TValue>.Record>
        where TValue : struct
    {
        public InputStateHistory(int? maxStateSizeInBytes = null)
        // Using the size of the value here isn't quite correct but the value is used as an upper
        // bound on stored state size for which the size of the value should be a reasonable guess.
            : base(maxStateSizeInBytes ?? UnsafeUtility.SizeOf<TValue>())
        {
            if (maxStateSizeInBytes < UnsafeUtility.SizeOf<TValue>())
                throw new ArgumentException("Max state size cannot be smaller than sizeof(TValue)", nameof(maxStateSizeInBytes));
        }

        public InputStateHistory(InputControl<TValue> control)
            : base(control)
        {
        }

        public InputStateHistory(string path)
            : base(path)
        {
            // Make sure that the value type of all matched controls is compatible with TValue.
            foreach (var control in controls)
                if (!typeof(TValue).IsAssignableFrom(control.valueType))
                    throw new ArgumentException(
                        $"Control '{control}' matched by '{path}' has value type '{TypeHelpers.GetNiceTypeName(control.valueType)}' which is incompatible with '{TypeHelpers.GetNiceTypeName(typeof(TValue))}'");
        }

        ~InputStateHistory()
        {
            Destroy();
        }

        public unsafe Record AddRecord(Record record)
        {
            var recordPtr = AllocateRecord(out var index);
            var newRecord = new Record(this, index, recordPtr);
            newRecord.CopyFrom(record);
            return newRecord;
        }

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

        public new IEnumerator<Record> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

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

        public new unsafe struct Record : IEquatable<Record>
        {
            private readonly InputStateHistory<TValue> m_Owner;
            private readonly int m_IndexPlusOne;
            private uint m_Version;

            internal RecordHeader* header => m_Owner.GetRecord(recordIndex);
            internal int recordIndex => m_IndexPlusOne - 1;

            public bool valid => m_Owner != default && m_IndexPlusOne != default && header->version == m_Version;

            public InputStateHistory<TValue> owner => m_Owner;

            public int index
            {
                get
                {
                    CheckValid();
                    return m_Owner.RecordIndexToUserIndex(recordIndex);
                }
            }

            public double time
            {
                get
                {
                    CheckValid();
                    return header->time;
                }
            }

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

            public TValue ReadValue()
            {
                CheckValid();
                return m_Owner.ReadValue<TValue>(header);
            }

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

            public bool Equals(Record other)
            {
                return ReferenceEquals(m_Owner, other.m_Owner) && m_IndexPlusOne == other.m_IndexPlusOne && m_Version == other.m_Version;
            }

            public override bool Equals(object obj)
            {
                return obj is Record other && Equals(other);
            }

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

            public override string ToString()
            {
                if (!valid)
                    return "<Invalid>";

                return $"{{ control={control} value={ReadValue()} time={time} }}";
            }
        }
    }
}
