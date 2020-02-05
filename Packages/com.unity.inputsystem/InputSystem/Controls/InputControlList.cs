using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;

////TODO: add a device setup version to InputManager and add version check here to ensure we're not going out of sync

////REVIEW: can we have a read-only version of this

////REVIEW: this would *really* profit from having a global ordering of InputControls that can be indexed

////REVIEW: move this to .LowLevel? this one is pretty peculiar to use and doesn't really work like what you'd expect given C#'s List<>

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Keep a list of <see cref="InputControl"/>s without allocating managed memory.
    /// </summary>
    /// <remarks>
    /// This struct is mainly used by methods such as <see cref="InputSystem.FindControls(string)"/>
    /// or <see cref="InputControlPath.TryFindControls{TControl}"/> to store an arbitrary length
    /// list of resulting matches without having to allocate GC heap memory.
    ///
    /// Requires the control setup in the system to not change while the list is being used. If devices are
    /// removed from the system, the list will no longer be valid. Also, only works with controls of devices that
    /// have been added to the system (<see cref="InputDevice.added"/>). The reason for these constraints is
    /// that internally, the list only stores integer indices that are translates to <see cref="InputControl"/>
    /// references on the fly. If the device setup in the system changes, the indices may become invalid.
    ///
    /// This struct allocates unmanaged memory and thus must be disposed or it will leak memory. By default
    /// allocates <c>Allocator.Persistent</c> memory. You can direct it to use another allocator by
    /// passing an <see cref="Allocator"/> value to one of the constructors.
    ///
    /// <example>
    /// <code>
    /// // Find all controls with the "Submit" usage in the system.
    /// // By wrapping it in a `using` block, the list of controls will automatically be disposed at the end.
    /// using (var controls = InputSystem.FindControls("*/{Submit}"))
    ///     /* ... */;
    /// </code>
    /// </example>
    /// </remarks>
    /// <typeparam name="TControl">Type of <see cref="InputControl"/> to store in the list.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    [DebuggerTypeProxy(typeof(InputControlListDebugView<>))]
    #endif
    public unsafe struct InputControlList<TControl> : IList<TControl>, IReadOnlyList<TControl>, IDisposable
        where TControl : InputControl
    {
        /// <summary>
        /// Current number of controls in the list.
        /// </summary>
        /// <value>Number of controls currently in the list.</value>
        public int Count => m_Count;

        /// <summary>
        /// Total number of controls that can currently be stored in the list.
        /// </summary>
        /// <value>Total size of array as currently allocated.</value>
        /// <remarks>
        /// This can be set ahead of time to avoid repeated allocations.
        ///
        /// <example>
        /// <code>
        /// // Add all keys from the keyboard to a list.
        /// var keys = Keyboard.current.allKeys;
        /// var list = new InputControlList&lt;KeyControl&gt;(keys.Count);
        /// list.AddRange(keys);
        /// </code>
        /// </example>
        /// </remarks>
        public int Capacity
        {
            get
            {
                if (!m_Indices.IsCreated)
                    return 0;
                return m_Indices.Length;
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

                var newSize = value;
                var allocator = m_Allocator != Allocator.Invalid ? m_Allocator : Allocator.Persistent;
                ArrayHelpers.Resize(ref m_Indices, newSize, allocator);
            }
        }

        /// <summary>
        /// This is always false.
        /// </summary>
        /// <value>Always false.</value>
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
        /// <param name="allocator">Allocator to use for requesting unmanaged memory.</param>
        /// <param name="initialCapacity">If greater than zero, will immediately allocate
        /// memory and set <see cref="Capacity"/> accordingly.</param>
        /// <example>
        /// <code>
        /// // Create a control list that allocates from the temporary memory allocator.
        /// using (var list = new InputControlList(Allocator.Temp))
        /// {
        ///     // Add all gamepads to the list.
        ///     InputSystem.FindControls("&lt;Gamepad&gt;", ref list);
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

        /// <summary>
        /// Construct a list and populate it with the given values.
        /// </summary>
        /// <param name="values">Sequence of values to populate the list with.</param>
        /// <param name="allocator">Allocator to use for requesting unmanaged memory.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        public InputControlList(IEnumerable<TControl> values, Allocator allocator = Allocator.Persistent)
            : this(allocator)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            foreach (var value in values)
                Add(value);
        }

        /// <summary>
        /// Construct a list and add the given values to it.
        /// </summary>
        /// <param name="values">Sequence of controls to add to the list.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        public InputControlList(params TControl[] values)
            : this()
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var count = values.Length;
            Capacity = Mathf.Max(count, 10);
            for (var i = 0; i < count; ++i)
                Add(values[i]);
        }

        /// <summary>
        /// Add a control to the list.
        /// </summary>
        /// <param name="item">Control to add. Allowed to be <c>null</c>.</param>
        /// <remarks>
        /// If necessary, <see cref="Capacity"/> will be increased.
        ///
        /// It is allowed to add nulls to the list. This can be useful, for example, when
        /// specific indices in the list correlate with specific matches and a given match
        /// needs to be marked as "matches nothing".
        /// </remarks>
        /// <seealso cref="Remove"/>
        public void Add(TControl item)
        {
            var index = ToIndex(item);
            var allocator = m_Allocator != Allocator.Invalid ? m_Allocator : Allocator.Persistent;
            ArrayHelpers.AppendWithCapacity(ref m_Indices, ref m_Count, index, allocator: allocator);
        }

        /// <summary>
        /// Add a slice of elements taken from the given list.
        /// </summary>
        /// <param name="list">List to take the slice of values from.</param>
        /// <param name="count">Number of elements to copy from <paramref name="list"/>.</param>
        /// <param name="destinationIndex">Starting index in the current control list to copy to.
        /// This can be beyond <see cref="Count"/> or even <see cref="Capacity"/>. Memory is allocated
        /// as needed.</param>
        /// <param name="sourceIndex">Source index in <paramref name="list"/> to start copying from.
        /// <paramref name="count"/> elements are copied starting at <paramref name="sourceIndex"/>.</param>
        /// <typeparam name="TList">Type of list. This is a type parameter to avoid boxing in case the
        /// given list is a struct (such as InputControlList itself).</typeparam>
        /// <exception cref="ArgumentOutOfRangeException">The range of <paramref name="count"/>
        /// and <paramref name="sourceIndex"/> is at least partially outside the range of values
        /// available in <paramref name="list"/>.</exception>
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
                throw new ArgumentOutOfRangeException(nameof(count),
                    $"Count of {count} elements starting at index {sourceIndex} exceeds length of list of {list.Count}");

            // Make space in the list.
            if (Capacity < m_Count + count)
                Capacity = Math.Max(m_Count + count, 10);
            if (destinationIndex < Count)
                NativeArray<ulong>.Copy(m_Indices, destinationIndex, m_Indices, destinationIndex + count,
                    Count - destinationIndex);

            // Add elements.
            for (var i = 0; i < count; ++i)
                m_Indices[destinationIndex + i] = ToIndex(list[sourceIndex + i]);
            m_Count += count;
        }

        /// <summary>
        /// Add a sequence of controls to the list.
        /// </summary>
        /// <param name="list">Sequence of controls to add.</param>
        /// <param name="count">Number of controls from <paramref name="list"/> to add. If negative
        /// (default), all controls from <paramref name="list"/> will be added.</param>
        /// <param name="destinationIndex">Index in the control list to start inserting controls
        /// at. If negative (default), controls will be appended to the end of the control list.</param>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <c>null</c>.</exception>
        /// <remarks>
        /// If <paramref name="count"/> is not supplied, <paramref name="list"/> will be iterated
        /// over twice.
        /// </remarks>
        public void AddRange(IEnumerable<TControl> list, int count = -1, int destinationIndex = -1)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (count < 0)
                count = list.Count();
            if (destinationIndex < 0)
                destinationIndex = Count;

            if (count == 0)
                return;

            // Make space in the list.
            if (Capacity < m_Count + count)
                Capacity = Math.Max(m_Count + count, 10);
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

        /// <summary>
        /// Remove a control from the list.
        /// </summary>
        /// <param name="item">Control to remove. Can be null.</param>
        /// <returns>True if the control was found in the list and removed, false otherwise.</returns>
        /// <seealso cref="Add"/>
        public bool Remove(TControl item)
        {
            if (m_Count == 0)
                return false;

            var index = ToIndex(item);
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

        /// <summary>
        /// Remove the control at the given index.
        /// </summary>
        /// <param name="index">Index of control to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or equal
        /// or greater than <see cref="Count"/>.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= m_Count)
                throw new ArgumentOutOfRangeException(
                    nameof(index), $"Index {index} is out of range in list with {m_Count} elements");

            ArrayHelpers.EraseAtWithCapacity(m_Indices, ref m_Count, index);
        }

        public void CopyTo(TControl[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(TControl item)
        {
            if (m_Count == 0)
                return -1;

            var index = ToIndex(item);
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

        public bool Contains(TControl item)
        {
            return IndexOf(item) != -1;
        }

        public void SwapElements(int index1, int index2)
        {
            if (index1 < 0 || index1 >= m_Count)
                throw new ArgumentOutOfRangeException(nameof(index1));
            if (index2 < 0 || index2 >= m_Count)
                throw new ArgumentOutOfRangeException(nameof(index2));

            if (index1 != index2)
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

        /// <summary>
        /// Convert the contents of the list to an array.
        /// </summary>
        /// <param name="dispose">If true, the control list will be disposed of as part of the operation, i.e.
        /// <see cref="Dispose"/> will be called as a side-effect.</param>
        /// <returns>An array mirroring the contents of the list. Not null.</returns>
        public TControl[] ToArray(bool dispose = false)
        {
            // Somewhat pointless to allocate an empty array if we have no elements instead
            // of returning null, but other ToArray() implementations work that way so we do
            // the same to avoid surprises.

            var result = new TControl[m_Count];
            for (var i = 0; i < m_Count; ++i)
                result[i] = this[i];

            if (dispose)
                Dispose();

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

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
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
    #endif
}
