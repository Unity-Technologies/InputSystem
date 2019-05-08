using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////TODO: make Capacity work like in other containers (i.e. total capacity not "how much room is left")

////TODO: add a device setup version to InputManager and add version check here to ensure we're not going out of sync

////REVIEW: can we have a read-only version of this

////REVIEW: this would *really* profit from having a global ordering of InputControls that can be indexed

////REVIEW: move this to .LowLevel? this one is pretty peculiar to use and doesn't really work like what you'd expect given C#'s List<>

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Keep a list of <see cref="InputControl">input controls</see> without allocating
    /// managed memory.
    /// </summary>
    /// <remarks>
    /// Requires the control setup in the system to not change while the list is being used. If devices are
    /// removed from the system, the list will no longer be valid. Also, only works with controls of devices that
    /// have been added to the system (meaning that it cannot be used with devices created by <see cref="InputDeviceBuilder"/>
    /// before they have been <see cref="InputSystem.AddDevice{InputDevice}">added</see>).
    ///
    /// Allocates unmanaged memory. Must be disposed or will leak memory. By default allocates <see cref="Allocator.Persistent">
    /// persistent</see> memory. Can direct it to use another allocator with <see cref="InputControlList{Allocator}"/>.
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(InputControlListDebugView<>))]
    public unsafe struct InputControlList<TControl> : IList<TControl>, IReadOnlyList<TControl>, IDisposable
        where TControl : InputControl
    {
        /// <summary>
        /// Number of controls in the list.
        /// </summary>
        public int Count => m_Count;

        /// <summary>
        /// Number of controls that can be added before more (unmanaged) memory has to be allocated.
        /// </summary>
        public int Capacity
        {
            get
            {
                if (!m_Indices.IsCreated)
                    return 0;
                Debug.Assert(m_Indices.Length >= m_Count);
                return m_Indices.Length - m_Count;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Capacity cannot be negative", nameof(value));

                if (value == 0)
                {
                    if (m_Count != 0)
                        m_Indices.Dispose();
                    m_Count = 0;
                    return;
                }

                var newSize = Count + value;
                var allocator = m_Allocator != Allocator.Invalid ? m_Allocator : Allocator.Persistent;
                ArrayHelpers.Resize(ref m_Indices, newSize, allocator);
            }
        }

        public bool IsReadOnly => false;

        /// <summary>
        /// Return the control at the given index.
        /// </summary>
        /// <param name="index">Index of control.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than or equal to <see cref="Count"/>
        /// </exception>
        /// <remarks>
        /// Internally, the list only stores indices. Resolution to <see cref="InputControl">controls</see> happens
        /// dynamically by looking them up globally.
        /// </remarks>
        public TControl this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Count)
                    throw new ArgumentOutOfRangeException(
                        nameof(index), $"Index {index} is out of range in list with {m_Count} entries");

                return FromIndex(m_Indices[index]);
            }
            set
            {
                if (index < 0 || index >= m_Count)
                    throw new ArgumentOutOfRangeException(
                        nameof(index), $"Index {index} is out of range in list with {m_Count} entries");

                m_Indices[index] = ToIndex(value);
            }
        }

        /// <summary>
        /// Construct a list that allocates unmanaged memory from the given allocator.
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="initialCapacity"></param>
        /// <example>
        /// <code>
        /// // Create a control list that allocates from the temporary memory allocator.
        /// using (var list = new InputControlList(Allocator.Temp))
        /// {
        ///     // Add all gamepads to the list.
        ///     InputSystem.FindControls("&lt;Gamepad&gt;", list);
        /// }
        /// </code>
        /// </example>
        public InputControlList(Allocator allocator, int initialCapacity = 0)
        {
            m_Allocator = allocator;
            m_Indices = new NativeArray<ulong>();
            m_Count = 0;

            if (initialCapacity != 0)
                Capacity = initialCapacity;
        }

        public InputControlList(IEnumerable<TControl> values, Allocator allocator = Allocator.Persistent)
            : this(allocator)
        {
            foreach (var value in values)
                Add(value);
        }

        public InputControlList(params TControl[] values)
            : this()
        {
            foreach (var value in values)
                Add(value);
        }

        public void Add(TControl control)
        {
            var index = ToIndex(control);
            var allocator = m_Allocator != Allocator.Invalid ? m_Allocator : Allocator.Persistent;
            ArrayHelpers.AppendWithCapacity(ref m_Indices, ref m_Count, index, allocator: allocator);
        }

        /// <summary>
        /// Add a slice of elements taken from the given list.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="count"></param>
        /// <param name="destinationIndex"></param>
        /// <param name="sourceIndex"></param>
        /// <typeparam name="TList"></typeparam>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void AddSlice<TList>(TList list, int count = -1, int destinationIndex = -1, int sourceIndex = 0)
            where TList : IReadOnlyList<TControl>
        {
            if (count < 0)
                count = list.Count;
            if (destinationIndex < 0)
                destinationIndex = Count;

            if (count == 0)
                return;
            if (sourceIndex + count > list.Count)
                throw new ArgumentOutOfRangeException(
                    $"Count of {count} elements starting at index {sourceIndex} exceeds length of list of {list.Count}", "count");

            // Make space in the list.
            if (Capacity < count)
                Capacity = Math.Max(count, 10);
            if (destinationIndex < Count)
                NativeArray<ulong>.Copy(m_Indices, destinationIndex, m_Indices, destinationIndex + count,
                    Count - destinationIndex);

            // Add elements.
            for (var i = 0; i < count; ++i)
                m_Indices[destinationIndex + i] = ToIndex(list[sourceIndex + i]);
            m_Count += count;
        }

        public void AddRange(IEnumerable<TControl> list, int count = -1, int destinationIndex = -1)
        {
            if (count < 0)
                count = list.Count();
            if (destinationIndex < 0)
                destinationIndex = Count;

            if (count == 0)
                return;

            // Make space in the list.
            if (Capacity < count)
                Capacity = Math.Max(count, 10);
            if (destinationIndex < Count)
                NativeArray<ulong>.Copy(m_Indices, destinationIndex, m_Indices, destinationIndex + count,
                    Count - destinationIndex);

            // Add elements.
            foreach (var element in list)
            {
                m_Indices[destinationIndex++] = ToIndex(element);
                ++m_Count;
                --count;
                if (count == 0)
                    break;
            }
        }

        public bool Remove(TControl control)
        {
            if (m_Count == 0)
                return false;

            var index = ToIndex(control);
            for (var i = 0; i < m_Count; ++i)
            {
                if (m_Indices[i] == index)
                {
                    ArrayHelpers.EraseAtWithCapacity(m_Indices, ref m_Count, i);
                    return true;
                }
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= m_Count)
                throw new ArgumentException(
                    $"Index {index} is out of range in list with {m_Count} elements", nameof(index));

            ArrayHelpers.EraseAtWithCapacity(m_Indices, ref m_Count, index);
        }

        public void CopyTo(TControl[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(TControl control)
        {
            if (m_Count == 0)
                return -1;

            var index = ToIndex(control);
            var indices = (ulong*)m_Indices.GetUnsafeReadOnlyPtr();

            for (var i = 0; i < m_Count; ++i)
                if (indices[i] == index)
                    return i;

            return -1;
        }

        public void Insert(int index, TControl item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            m_Count = 0;
        }

        public bool Contains(TControl control)
        {
            return IndexOf(control) != -1;
        }

        public void SwapElements(int index1, int index2)
        {
            if (index1 < 0 || index1 >= m_Count)
                throw new ArgumentOutOfRangeException(nameof(index1));
            if (index2 < 0 || index2 >= m_Count)
                throw new ArgumentOutOfRangeException(nameof(index2));

            m_Indices.SwapElements(index1, index2);
        }

        public void Sort<TCompare>(int startIndex, int count, TCompare comparer)
            where TCompare : IComparer<TControl>
        {
            if (startIndex < 0 || startIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (startIndex + count >= Count)
                throw new ArgumentOutOfRangeException(nameof(count));

            // Simple insertion sort.
            for (var i = 1; i < count; ++i)
                for (var j = i; j > 0 && comparer.Compare(this[j - 1], this[j]) < 0; --j)
                    SwapElements(j, j - 1);
        }

        public TControl[] ToArray()
        {
            // Somewhat pointless to allocate an empty array if we have no elements instead
            // of returning null, but other ToArray() implementations work that way so we do
            // the same to avoid surprises.

            var result = new TControl[m_Count];
            for (var i = 0; i < m_Count; ++i)
                result[i] = this[i];
            return result;
        }

        internal void AppendTo(ref TControl[] array, ref int count)
        {
            for (var i = 0; i < m_Count; ++i)
                ArrayHelpers.AppendWithCapacity(ref array, ref count, this[i]);
        }

        public void Dispose()
        {
            if (m_Indices.IsCreated)
                m_Indices.Dispose();
        }

        public IEnumerator<TControl> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            if (Count == 0)
                return "()";

            var builder = new StringBuilder();
            builder.Append('(');

            for (var i = 0; i < Count; ++i)
            {
                if (i != 0)
                    builder.Append(',');
                builder.Append(this[i]);
            }

            builder.Append(')');
            return builder.ToString();
        }

        private int m_Count;
        private NativeArray<ulong> m_Indices;
        private readonly Allocator m_Allocator;

        private const ulong kInvalidIndex = 0xffffffffffffffff;

        private static ulong ToIndex(TControl control)
        {
            if (control == null)
                return kInvalidIndex;

            var device = control.device;
            var deviceIndex = device.m_DeviceIndex;
            var controlIndex = !ReferenceEquals(device, control)
                ? ArrayHelpers.IndexOfReference(device.m_ChildrenForEachControl, control) + 1
                : 0;

            // There is a known documented bug with the new Rosyln
            // compiler where it warns on casts with following line that
            // was perfectly legal in previous CSC compiler.
            // Below is silly conversion to get rid of warning, or we can pragma
            // out the warning.
            //return ((ulong)deviceIndex << 32) | (ulong)controlIndex;
            var shiftedDeviceIndex = (ulong)deviceIndex << 32;
            var unsignedControlIndex = (ulong)controlIndex;

            return shiftedDeviceIndex | unsignedControlIndex;
        }

        private static TControl FromIndex(ulong index)
        {
            if (index == kInvalidIndex)
                return null;

            var deviceIndex = (int)(index >> 32);
            var controlIndex = (int)(index & 0xFFFFFFFF);

            var device = InputSystem.devices[deviceIndex];
            if (controlIndex == 0)
                return (TControl)(InputControl)device;

            return (TControl)device.m_ChildrenForEachControl[controlIndex - 1];
        }

        private struct Enumerator : IEnumerator<TControl>
        {
            private readonly ulong* m_Indices;
            private readonly int m_Count;
            private int m_Current;

            public Enumerator(InputControlList<TControl> list)
            {
                m_Count = list.m_Count;
                m_Current = -1;
                m_Indices = m_Count > 0 ? (ulong*)list.m_Indices.GetUnsafeReadOnlyPtr() : null;
            }

            public bool MoveNext()
            {
                if (m_Current >= m_Count)
                    return false;
                ++m_Current;
                return (m_Current != m_Count);
            }

            public void Reset()
            {
                m_Current = -1;
            }

            public TControl Current
            {
                get
                {
                    if (m_Indices == null)
                        throw new InvalidOperationException("Enumerator is not valid");
                    return FromIndex(m_Indices[m_Current]);
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }

    internal struct InputControlListDebugView<TControl>
        where TControl : InputControl
    {
        private readonly TControl[] m_Controls;

        public InputControlListDebugView(InputControlList<TControl> list)
        {
            m_Controls = list.ToArray();
        }

        public TControl[] controls => m_Controls;
    }

    public static class InputControlListExtensions
    {
        public static InputControlList<TControl> ToControlList<TControl>(this IEnumerable<TControl> list)
            where TControl : InputControl
        {
            var result = new InputControlList<TControl>();
            foreach (var element in list)
                result.AddRange(list);
            return result;
        }

        public static InputControlList<TControl> ToControlList<TControl>(this IReadOnlyList<TControl> list)
            where TControl : InputControl
        {
            var result = new InputControlList<TControl>();
            foreach (var element in list)
                result.AddSlice(list);
            return result;
        }
    }
}
