using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

////TODO: add a device setup version to InputManager and add version check here to ensure we're not going out of sync

////REVIEW: can we have a read-only version of this

////REVIEW: this would *really* profit from having a global ordering of InputControls that can be indexed

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Keep a list of <see cref="InputControl">input controls</see> without allocating
    /// managed memory.
    /// </summary>
    /// <remarks>
    /// Requires the control setup in the system to not change while the list is being used. If devices are
    /// removed from the system, the list will no be valid. Also, only works with controls of devices that
    /// have been added to the system (meaning that it cannot be used with devices created by <see cref="InputDeviceBuilder"/>
    /// before they have been <see cref="InputSystem.AddDevice{InputDevice}">added</see>).
    ///
    /// Allocates unmanaged memory. Must be disposed or will leak memory. By default allocates <see cref="Allocator.Persistent">
    /// persistent</see> memory. Can direct it to use another allocator with <see cref="InputControlList{Allocator}"/>.
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(InputControlListDebugView<>))]
    public unsafe struct InputControlList<TControl> : IEnumerable<TControl>, IDisposable
        where TControl : InputControl
    {
        /// <summary>
        /// Number of controls in the list.
        /// </summary>
        public int Count
        {
            get { return m_Count; }
        }

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
                    throw new ArgumentException("Capacity cannot be negative", "value");

                var newSize = Count + value;
                var allocator = m_Allocator != Allocator.Invalid ? m_Allocator : Allocator.Persistent;
                ArrayHelpers.Resize(ref m_Indices, newSize, allocator);
            }
        }

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
                        string.Format("Index {0} is out of range in list with {1} entries", index, m_Count), "index");

                return FromIndex(m_Indices[index]);
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

        public void Remove(TControl control)
        {
            if (m_Count == 0)
                return;

            var index = ToIndex(control);
            for (var i = 0; i < m_Count; ++i)
            {
                if (m_Indices[i] == index)
                {
                    ArrayHelpers.EraseAtWithCapacity(ref m_Indices, ref m_Count, i);
                    break;
                }
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= m_Count)
                throw new ArgumentException(
                    string.Format("Index {0} is out of range in list with {1} elements", index, m_Count), "index");

            ArrayHelpers.EraseAtWithCapacity(ref m_Indices, ref m_Count, index);
        }

        public void Clear()
        {
            m_Count = 0;
        }

        public bool Contains(TControl control)
        {
            if (m_Count == 0)
                return false;

            var index = ToIndex(control);
            var indices = (ulong*)m_Indices.GetUnsafeReadOnlyPtr();

            for (var i = 0; i < m_Count; ++i)
                if (indices[i] == index)
                    return true;

            return false;
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

        private int m_Count;
        private NativeArray<ulong> m_Indices;
        private Allocator m_Allocator;

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

            return ((ulong)deviceIndex << 32) | (ulong)controlIndex;
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

        public struct Enumerator : IEnumerator<TControl>
        {
            private ulong* m_Indices;
            private int m_Current;
            private int m_Count;

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

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
            }
        }
    }

    internal struct InputControlListDebugView<TControl>
        where TControl : InputControl
    {
        private TControl[] m_Controls;

        public InputControlListDebugView(InputControlList<TControl> list)
        {
            m_Controls = list.ToArray();
        }

        public TControl[] controls
        {
            get { return m_Controls; }
        }
    }
}
