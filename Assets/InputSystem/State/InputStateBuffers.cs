using System;
using UnityEngine;
using UnityEngine.Collections;

namespace ISX
{
    // The raw memory blocks which are indexed by InputStateBlocks.
    //
    // Internally, we perform only a single combined allocation for all state buffers
    // needed by the system. Externally, we expose them as if they are each separate
    // buffers.
#if UNITY_EDITOR
    [Serializable]
#endif
    internal struct InputStateBuffers
    {
        // State buffers are set up in a double buffering scheme where the "back buffer"
        // represents the previous state of devices and the "front buffer" represents
        // the current state.
        //
        // Edit mode and play mode each get their own double buffering. Updates to them
        // are tied to focus and only one mode will actually receive state events while the
        // other mode is dormant. In the player, we only get play mode buffers, of course.
        //
        // For edit mode, we only need a single set of front and back buffers.
        //
        // For play mode, things are complicated by the fact that we can have several
        // update slices (dynamic, fixed, before-render) in a single frame. Each such
        // update type has its own point in time it considers the "previous" point in time.
        // So, in the worst case where multiple update types are enabled concurrently,
        // we have to keep multiple separate buffers for play mode.
        //
        // If, however, only a single update type is enabled (e.g. either fixed or dynamic),
        // we operate the same way as edit mode with only one front and one back buffer.
        //
        // Buffer swapping happens differently than it does for graphics as we have to
        // carry forward the current state of a device. In a scheme where you simply swap
        // the meaning of front and back buffer every frame, every swap brings back the
        // state from two frames ago. In graphics, this isn't a problem as you are expected
        // to either clear or render over the entire frame. For us, it'd be okay as well
        // if we could guarantee that every device gets a state event every frame -- which,
        // however, we can't guarantee.
        //
        // We solve this by making buffer swapping *per device* rather than global. Only
        // when a device actually receives a state event will we swap the front and back
        // buffer for it. This means that what is the "current" buffer to one device may
        // be the "previous" buffer to another. This avoids having to do any copying of
        // state between the buffers.
        //
        // In play mode, when we do have multiple types of updates enabled at the same time,
        // some additional rules apply.
        //
        // Before render updates never get their own state buffers. If enabled, they will
        // process into the state buffers of the fixed and/or dynamic updates (depending
        // on whether only one or both are enabled).
        //
        // Fixed and dynamic each get their own buffers. We specifically want to *NOT*
        // optimize for this case as doing input processing from game scripts in both
        // updates is a bad setup -- a game should decide where it wants to process input
        // and then disable the update type that it does not need. This will put the
        // game in a simple double buffering configuration.


        ////TODO: need to clear the current buffers when switching between edit and play mode
        ////      (i.e. if you click an editor window while in play mode, the play mode
        ////      device states will all go back to default)


        public int sizePerBuffer;
        public int totalSize;

        // Secretely we perform only a single allocation.
        // This allocation also contains the device-to-state mappings.
#if UNITY_EDITOR
        [SerializeField]
#endif
        private IntPtr m_AllBuffers;

        // Contains information about a double buffer setup.
        [Serializable]
        internal unsafe struct DoubleBuffers
        {
            // An array of pointers that maps devices to their respective
            // front and back buffer. Mapping is [deviceIndex*2] is front
            // buffer and [deviceIndex*2+1] is back buffer. Each device
            // has its buffers swapped individually with SwapDeviceBuffers().
            public void** deviceToBufferMapping;

            public bool valid => deviceToBufferMapping != null;

            public void SetFrontBuffer(int deviceIndex, void* ptr)
            {
                deviceToBufferMapping[deviceIndex * 2] = ptr;
            }

            public void SetBackBuffer(int deviceIndex, void* ptr)
            {
                deviceToBufferMapping[deviceIndex * 2 + 1] = ptr;
            }

            public void* GetFrontBuffer(int deviceIndex)
            {
                return deviceToBufferMapping[deviceIndex * 2];
            }

            public void* GetBackBuffer(int deviceIndex)
            {
                return deviceToBufferMapping[deviceIndex * 2 + 1];
            }

            public void SwapBuffers(int deviceIndex)
            {
                // Ignore if the double buffer set has not been initialized.
                // Means the respective update type is disabled.
                if (!valid)
                    return;

                var front = GetFrontBuffer(deviceIndex);
                var back = GetBackBuffer(deviceIndex);

                SetFrontBuffer(deviceIndex, back);
                SetBackBuffer(deviceIndex, front);
            }
        }

        internal DoubleBuffers m_DynamicUpdateBuffers;
        internal DoubleBuffers m_FixedUpdateBuffers;

#if UNITY_EDITOR
        internal DoubleBuffers m_EditorUpdateBuffers;
#endif

        private static DoubleBuffers s_CurrentBuffers;

        public static unsafe IntPtr GetFrontBuffer(int deviceIndex)
        {
            return new IntPtr(s_CurrentBuffers.GetFrontBuffer(deviceIndex));
        }

        public static unsafe IntPtr GetBackBuffer(int deviceIndex)
        {
            return new IntPtr(s_CurrentBuffers.GetBackBuffer(deviceIndex));
        }

        // Switch the current set of buffers used by the system.
        public void SwitchTo(InputUpdateType update)
        {
            switch (update)
            {
                case InputUpdateType.Dynamic:
                    s_CurrentBuffers = m_DynamicUpdateBuffers;
                    break;
                case InputUpdateType.Fixed:
                    s_CurrentBuffers = m_FixedUpdateBuffers;
                    break;
                case InputUpdateType.BeforeRender:
                    if (m_DynamicUpdateBuffers.valid)
                        s_CurrentBuffers = m_DynamicUpdateBuffers;
                    else
                        s_CurrentBuffers = m_FixedUpdateBuffers;
                    break;
#if UNITY_EDITOR
                case InputUpdateType.Editor:
                    s_CurrentBuffers = m_EditorUpdateBuffers;
                    break;
#endif
            }
        }

        // Allocates all buffers to serve the given updates and comes up with a spot
        // for the state block of each device. Returns the new state blocks for the
        // devices (it will *NOT* install them on the devices).
        public unsafe uint[] AllocateAll(InputUpdateType updateMask, InputDevice[] devices)
        {
            uint[] newDeviceOffsets = null;
            sizePerBuffer = ComputeSizeOfSingleBufferAndOffsetForEachDevice(devices, ref newDeviceOffsets);
            if (sizePerBuffer == 0)
                return null;

            var isDynamicUpdateEnabled = (updateMask & InputUpdateType.Dynamic) == InputUpdateType.Dynamic;
            var isFixedUpdateEnabled = (updateMask & InputUpdateType.Fixed) == InputUpdateType.Fixed;

            var deviceCount = devices.Length;
            var mappingTableSizePerBuffer = deviceCount * sizeof(void*) * 2;

            if (isDynamicUpdateEnabled)
            {
                totalSize += sizePerBuffer * 2;
                totalSize += mappingTableSizePerBuffer;
            }
            if (isFixedUpdateEnabled)
            {
                totalSize += sizePerBuffer * 2;
                totalSize += mappingTableSizePerBuffer;
            }
            // Before render doesn't have its own buffers.

#if UNITY_EDITOR
            totalSize += sizePerBuffer * 2;
            totalSize += mappingTableSizePerBuffer;
#endif

            // Allocate.
            m_AllBuffers = UnsafeUtility.Malloc(totalSize, 4, Allocator.Persistent);
            UnsafeUtility.MemClear(m_AllBuffers, totalSize);

            // Set up device to buffer mappings.
            var ptr = m_AllBuffers;
            if (isDynamicUpdateEnabled)
            {
                m_DynamicUpdateBuffers =
                    SetUpDeviceToBufferMappings(devices, ref ptr, sizePerBuffer, mappingTableSizePerBuffer);
            }
            if (isFixedUpdateEnabled)
            {
                m_FixedUpdateBuffers =
                    SetUpDeviceToBufferMappings(devices, ref ptr, sizePerBuffer, mappingTableSizePerBuffer);
            }

            if (!isFixedUpdateEnabled)
                m_FixedUpdateBuffers = m_DynamicUpdateBuffers;
            if (!isDynamicUpdateEnabled)
                m_DynamicUpdateBuffers = m_FixedUpdateBuffers;

#if UNITY_EDITOR
            m_EditorUpdateBuffers =
                SetUpDeviceToBufferMappings(devices, ref ptr, sizePerBuffer, mappingTableSizePerBuffer);
#endif

            return newDeviceOffsets;
        }

        private unsafe DoubleBuffers SetUpDeviceToBufferMappings(InputDevice[] devices, ref IntPtr bufferPtr, int sizePerBuffer, int mappingTableSizePerBuffer)
        {
            var front = bufferPtr.ToPointer();
            var back = (bufferPtr + sizePerBuffer).ToPointer();
            var mappings = (void**)(bufferPtr + sizePerBuffer * 2).ToPointer();  // Put mapping table at end.
            bufferPtr += sizePerBuffer * 2 + mappingTableSizePerBuffer;

            var buffers = new DoubleBuffers {deviceToBufferMapping = mappings};

            for (var i = 0; i < devices.Length; ++i)
            {
                var deviceIndex = devices[i].m_DeviceIndex;

                buffers.SetFrontBuffer(deviceIndex, front);
                buffers.SetBackBuffer(deviceIndex, back);
            }

            return buffers;
        }

        public void FreeAll()
        {
            if (m_AllBuffers != IntPtr.Zero)
            {
                UnsafeUtility.Free(m_AllBuffers, Allocator.Persistent);
                m_AllBuffers = IntPtr.Zero;
            }

            m_DynamicUpdateBuffers = new DoubleBuffers();
            m_FixedUpdateBuffers = new DoubleBuffers();

#if UNITY_EDITOR
            m_EditorUpdateBuffers = new DoubleBuffers();
#endif
        }

        // Migrate state data for all devices from a previous set of buffers to the current set of buffers.
        // Copies all state from their old locations to their new locations and bakes the new offsets into
        // the control hierarchies of the given devices.
        // NOTE: oldDeviceIndices is only required if devices have been removed; otherwise it can be null.
        public void MigrateAll(InputDevice[] devices, uint[] newStateBlockOffsets, InputStateBuffers oldBuffers, int[] oldDeviceIndices)
        {
            // If we have old data, perform migration.
            // Note that the enabled update types don't need to match between the old set of buffers
            // and the new set of buffers.
            if (oldBuffers.totalSize > 0)
            {
                MigrateSingle(m_DynamicUpdateBuffers, devices, newStateBlockOffsets, oldBuffers.m_DynamicUpdateBuffers,
                    oldDeviceIndices);
                MigrateSingle(m_FixedUpdateBuffers, devices, newStateBlockOffsets, oldBuffers.m_FixedUpdateBuffers,
                    oldDeviceIndices);

#if UNITY_EDITOR
                MigrateSingle(m_EditorUpdateBuffers, devices, newStateBlockOffsets, oldBuffers.m_EditorUpdateBuffers,
                    oldDeviceIndices);
#endif
            }

            // Assign state blocks.
            for (var i = 0; i < devices.Length; ++i)
            {
                var newOffset = newStateBlockOffsets[i];
                var device = devices[i];
                var oldOffset = device.m_StateBlock.byteOffset;

                if (oldOffset == InputStateBlock.kInvalidOffset)
                {
                    device.m_StateBlock.byteOffset = 0;
                    if (newOffset != 0)
                        device.BakeOffsetIntoStateBlockRecursive(newOffset);
                }
                else
                {
                    var delta = newOffset - oldOffset;
                    if (delta != 0)
                        device.BakeOffsetIntoStateBlockRecursive(delta);
                }
            }
        }

        private unsafe void MigrateSingle(DoubleBuffers newBuffer, InputDevice[] devices, uint[] newStateBlockOffsets, DoubleBuffers oldBuffer, int[] oldDeviceIndices)
        {
            // Nothing to migrate if we no longer keep a buffer or the corresponding type.
            if (!newBuffer.valid)
                return;

            // We do the same if we don't had a corresponding buffer before.
            if (!oldBuffer.valid)
                return;

            ////TOOD: if we assume linear layouts of devices in 'devices' and assume that new devices are only added
            ////      at the end and only single devices can be removed, we can copy state buffers much more efficiently
            ////      in bulk rather than device-by-device

            // Migrate every device that has allocated state blocks.
            var newDeviceCount = devices.Length;
            var oldDeviceCount = oldDeviceIndices?.Length ?? newDeviceCount;
            for (var i = 0; i < newDeviceCount && i < oldDeviceCount; ++i)
            {
                var device = devices[i];
                Debug.Assert(device.m_DeviceIndex == i);

                // Skip device if it's a newly added device.
                if (device.m_StateBlock.byteOffset == InputStateBlock.kInvalidOffset)
                    continue;

                // Skip if this is a device that got added after we allocated the
                // previous buffers.
                var oldDeviceIndex = oldDeviceIndices ? [i] ?? i;
                if (oldDeviceIndex == -1)
                    continue;

                var numBytes = device.m_StateBlock.alignedSizeInBytes;

                var oldFrontPtr = new IntPtr(oldBuffer.GetFrontBuffer(oldDeviceIndex)) + (int)device.m_StateBlock.byteOffset;
                var oldBackPtr = new IntPtr(oldBuffer.GetBackBuffer(oldDeviceIndex)) + (int)device.m_StateBlock.byteOffset;

                var newFrontPtr = new IntPtr(newBuffer.GetFrontBuffer(i)) + (int)newStateBlockOffsets[i];
                var newBackPtr = new IntPtr(newBuffer.GetBackBuffer(i)) + (int)newStateBlockOffsets[i];

                // Copy state.
                UnsafeUtility.MemCpy(oldFrontPtr, newFrontPtr, numBytes);
                UnsafeUtility.MemCpy(oldBackPtr, newBackPtr, numBytes);
            }
        }

        // Compute the total size of we need for a single state buffer to encompass
        // all devices we have and also linearly assign offsets to all the devices
        // within such a buffer.
        private static int ComputeSizeOfSingleBufferAndOffsetForEachDevice(InputDevice[] devices, ref uint[] offsets)
        {
            if (devices == null)
                return 0;

            var deviceCount = devices.Length;
            var result = new uint[deviceCount];
            var currentOffset = 0u;
            var sizeInBytes = 0;

            for (var i = 0; i < devices.Length; ++i)
            {
                var size = devices[i].m_StateBlock.alignedSizeInBytes;
                sizeInBytes += size;
                result[i] = currentOffset;
                currentOffset += (uint)size;
            }

            offsets = result;
            return sizeInBytes;
        }
    }
}
