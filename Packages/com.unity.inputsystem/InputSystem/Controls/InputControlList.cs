using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: this would *really* profit from having a global ordering of InputControls that can be indexed

////TODO: add a device setup version to InputManager and add version check here to ensure we're not going out of sync

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Keep a list of <see cref="InputControl">input controls</see> without allocating
    /// managed memory.
    /// </summary>
    /// <remarks>
    /// Requires the control setup in the system to not change. If devices are removed from the system,
    /// the list will no be valid. Also, only works with controls of devices that have been added to
    /// the system.
    ///
    /// Allocates unmanaged memory. Must be disposed or will leak memory.
    /// </remarks>
    public unsafe struct InputControlList<TControl> : IEnumerable<TControl>, IDisposable
        where TControl : InputControl
    {
        public int Count
        {
            get { return m_Count; }
        }

        public TControl this[int index]
        {
            get
            {
                if (index >= m_Count)
                    throw new ArgumentOutOfRangeException(
                        string.Format("Index {0} is out of range in list with {1} entries", index, m_Count), "index");

                return FromIndex(m_Indices[index]);
            }
        }

        public InputControlList(Allocator allocator)
        {
            m_Allocator = allocator;
            m_Indices = new NativeArray<ulong>();
            m_Count = 0;
        }

        public void Add(TControl control)
        {
            if (control == null)
                throw new ArgumentNullException("control");

            var index = ToIndex(control);
            var allocator = m_Allocator != Allocator.Invalid ? m_Allocator : Allocator.Persistent;
            ArrayHelpers.AppendWithCapacity(ref m_Indices, ref m_Count, index, allocator: allocator);
        }

        public void Add(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            throw new NotImplementedException();
        }

        public void Remove(TControl control)
        {
            if (control == null)
                throw new ArgumentNullException("control");

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

        public void Remove(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            throw new NotImplementedException();
        }

        public void Clear()
        {
            m_Count = 0;
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

        private static ulong ToIndex(TControl control)
        {
            var device = control.device;
            var deviceIndex = device.m_DeviceIndex;
            var controlIndex = !ReferenceEquals(device, control)
                ? ArrayHelpers.IndexOfReference(device.m_ChildrenForEachControl, control) + 1
                : 0;

            return ((ulong)deviceIndex << 32) | (ulong)controlIndex;
        }

        private static TControl FromIndex(ulong index)
        {
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
}
