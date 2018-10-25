using System;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

////REVIEW: Can we change this into a setup where the buffering depth isn't fixed to 2 but rather
////        can be set on a per device basis?

namespace UnityEngine.Experimental.Input.LowLevel
{
    // The raw memory blocks which are indexed by InputStateBlocks.
    //
    // Internally, we perform only a single combined unmanaged allocation for all state
    // buffers needed by the system. Externally, we expose them as if they are each separate
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
        ////      actually, if we really reset on mode change, can't we just keep a single set buffers?


        public uint sizePerBuffer;
        public uint totalSize;

        /// <summary>
        /// Buffer that has state for each device initialized with default values.
        /// </summary>
        public IntPtr defaultStateBuffer;

        /// <summary>
        /// Buffer that contains bitflags for noisy and non-noisy controls, to identify significant device changes.
        /// </summary>
        public IntPtr noiseBitmaskBuffer;

        // Secretly we perform only a single allocation.
        // This allocation also contains the device-to-state mappings.
#if UNITY_EDITOR
        [SerializeField]
#endif
        private IntPtr m_AllBuffers;

        // Contains information about a double buffer setup.
        [Serializable]
        internal unsafe struct DoubleBuffers
        {
            ////REVIEW: store timestamps along with each device-to-buffer mapping?
            // An array of pointers that maps devices to their respective
            // front and back buffer. Mapping is [deviceIndex*2] is front
            // buffer and [deviceIndex*2+1] is back buffer. Each device
            // has its buffers swapped individually with SwapDeviceBuffers().
            public void** deviceToBufferMapping;

            public bool valid
            {
                get { return deviceToBufferMapping != null; }
            }

            public void SetFrontBuffer(int deviceIndex, IntPtr ptr)
            {
                deviceToBufferMapping[deviceIndex * 2] = (void**)ptr;
            }

            public void SetBackBuffer(int deviceIndex, IntPtr ptr)
            {
                deviceToBufferMapping[deviceIndex * 2 + 1] = (void**)ptr;
            }

            public IntPtr GetFrontBuffer(int deviceIndex)
            {
                return new IntPtr(deviceToBufferMapping[deviceIndex * 2]);
            }

            public IntPtr GetBackBuffer(int deviceIndex)
            {
                return new IntPtr(deviceToBufferMapping[deviceIndex * 2 + 1]);
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

        public DoubleBuffers GetDoubleBuffersFor(InputUpdateType updateType)
        {
            switch (updateType)
            {
                case InputUpdateType.Dynamic:
                    return m_DynamicUpdateBuffers;
                case InputUpdateType.Fixed:
                    return m_FixedUpdateBuffers;
                case InputUpdateType.BeforeRender:
                    if (m_DynamicUpdateBuffers.valid)
                        return m_DynamicUpdateBuffers;
                    else
                        return m_FixedUpdateBuffers;
#if UNITY_EDITOR
                case InputUpdateType.Editor:
                    return m_EditorUpdateBuffers;
#endif
            }

            throw new Exception("Unrecognized InputUpdateType: " + updateType);
        }

        internal static IntPtr s_DefaultStateBuffer;
        internal static IntPtr s_NoiseBitmaskBuffer;
        internal static DoubleBuffers s_CurrentBuffers;

        public static IntPtr GetFrontBufferForDevice(int deviceIndex)
        {
            return s_CurrentBuffers.GetFrontBuffer(deviceIndex);
        }

        public static IntPtr GetBackBufferForDevice(int deviceIndex)
        {
            return s_CurrentBuffers.GetBackBuffer(deviceIndex);
        }

        // Switch the current set of buffers used by the system.
        public static void SwitchTo(InputStateBuffers buffers, InputUpdateType update)
        {
            s_CurrentBuffers = buffers.GetDoubleBuffersFor(update);
        }

        // Allocates all buffers to serve the given updates and comes up with a spot
        // for the state block of each device. Returns the new state blocks for the
        // devices (it will *NOT* install them on the devices).
        public unsafe uint[] AllocateAll(InputUpdateType updateMask, InputDevice[] devices, int deviceCount)
        {
            uint[] newDeviceOffsets = null;
            sizePerBuffer = ComputeSizeOfSingleBufferAndOffsetForEachDevice(devices, deviceCount, ref newDeviceOffsets);
            if (sizePerBuffer == 0)
                return null;
            sizePerBuffer = NumberHelpers.AlignToMultiple(sizePerBuffer, 4);

            var isDynamicUpdateEnabled = (updateMask & InputUpdateType.Dynamic) == InputUpdateType.Dynamic;
            var isFixedUpdateEnabled = (updateMask & InputUpdateType.Fixed) == InputUpdateType.Fixed;

            // Determine how much memory we need.
            var mappingTableSizePerBuffer = (uint)(deviceCount * sizeof(void*) * 2);

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

            // Plus 2 more buffers (1 for defaults state, and one for noise filters)
            totalSize += sizePerBuffer * 2;

            // Allocate.
            m_AllBuffers = (IntPtr)UnsafeUtility.Malloc(totalSize, 4, Allocator.Persistent);
            UnsafeUtility.MemClear(m_AllBuffers.ToPointer(), totalSize);

            // Set up device to buffer mappings.
            var ptr = m_AllBuffers;
            if (isDynamicUpdateEnabled)
            {
                m_DynamicUpdateBuffers =
                    SetUpDeviceToBufferMappings(devices, deviceCount, ref ptr, sizePerBuffer, mappingTableSizePerBuffer);
            }
            if (isFixedUpdateEnabled)
            {
                m_FixedUpdateBuffers =
                    SetUpDeviceToBufferMappings(devices, deviceCount, ref ptr, sizePerBuffer, mappingTableSizePerBuffer);
            }

#if UNITY_EDITOR
            m_EditorUpdateBuffers =
                SetUpDeviceToBufferMappings(devices, deviceCount, ref ptr, sizePerBuffer, mappingTableSizePerBuffer);
#endif

            // Default state and noise filter buffers go last
            defaultStateBuffer = ptr;
            noiseBitmaskBuffer = new IntPtr(ptr.ToInt64() + sizePerBuffer);

            return newDeviceOffsets;
        }

        private unsafe DoubleBuffers SetUpDeviceToBufferMappings(InputDevice[] devices, int deviceCount, ref IntPtr bufferPtr, uint sizePerBuffer, uint mappingTableSizePerBuffer)
        {
            var front = bufferPtr;
            var back = new IntPtr(bufferPtr.ToInt64() + sizePerBuffer);
            var mappings = (void**)new IntPtr(bufferPtr.ToInt64() + sizePerBuffer * 2).ToPointer();  // Put mapping table at end.
            bufferPtr = new IntPtr(bufferPtr.ToInt64() + sizePerBuffer * 2 + mappingTableSizePerBuffer);

            var buffers = new DoubleBuffers {deviceToBufferMapping = mappings};

            for (var i = 0; i < deviceCount; ++i)
            {
                var deviceIndex = devices[i].m_DeviceIndex;

                buffers.SetFrontBuffer(deviceIndex, front);
                buffers.SetBackBuffer(deviceIndex, back);
            }

            return buffers;
        }

        public unsafe void FreeAll()
        {
            if (m_AllBuffers != IntPtr.Zero)
            {
                UnsafeUtility.Free(m_AllBuffers.ToPointer(), Allocator.Persistent);
                m_AllBuffers = IntPtr.Zero;
            }

            m_DynamicUpdateBuffers = new DoubleBuffers();
            m_FixedUpdateBuffers = new DoubleBuffers();

#if UNITY_EDITOR
            m_EditorUpdateBuffers = new DoubleBuffers();
#endif

            s_CurrentBuffers = new DoubleBuffers();

            if (s_DefaultStateBuffer == defaultStateBuffer)
                s_DefaultStateBuffer = IntPtr.Zero;

            defaultStateBuffer = IntPtr.Zero;

            if (s_NoiseBitmaskBuffer == noiseBitmaskBuffer)
                s_NoiseBitmaskBuffer = IntPtr.Zero;

            noiseBitmaskBuffer = IntPtr.Zero;

            totalSize = 0;
            sizePerBuffer = 0;
        }

        // Migrate state data for all devices from a previous set of buffers to the current set of buffers.
        // Copies all state from their old locations to their new locations and bakes the new offsets into
        // the control hierarchies of the given devices.
        // NOTE: oldDeviceIndices is only required if devices have been removed; otherwise it can be null.
        public void MigrateAll(InputDevice[] devices, int deviceCount, uint[] newStateBlockOffsets, InputStateBuffers oldBuffers, int[] oldDeviceIndices)
        {
            // If we have old data, perform migration.
            // Note that the enabled update types don't need to match between the old set of buffers
            // and the new set of buffers.
            if (oldBuffers.totalSize > 0)
            {
                MigrateDoubleBuffer(m_DynamicUpdateBuffers, devices, deviceCount, newStateBlockOffsets, oldBuffers.m_DynamicUpdateBuffers,
                    oldDeviceIndices);
                MigrateDoubleBuffer(m_FixedUpdateBuffers, devices, deviceCount, newStateBlockOffsets, oldBuffers.m_FixedUpdateBuffers,
                    oldDeviceIndices);

#if UNITY_EDITOR
                MigrateDoubleBuffer(m_EditorUpdateBuffers, devices, deviceCount, newStateBlockOffsets, oldBuffers.m_EditorUpdateBuffers,
                    oldDeviceIndices);
#endif

                MigrateSingleBuffer(defaultStateBuffer, devices, deviceCount, newStateBlockOffsets, oldBuffers.defaultStateBuffer);
                MigrateSingleBuffer(noiseBitmaskBuffer, devices, deviceCount, newStateBlockOffsets, oldBuffers.noiseBitmaskBuffer);
            }

            // Assign state blocks.
            for (var i = 0; i < deviceCount; ++i)
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

        private unsafe void MigrateDoubleBuffer(DoubleBuffers newBuffer, InputDevice[] devices, int deviceCount, uint[] newStateBlockOffsets, DoubleBuffers oldBuffer, int[] oldDeviceIndices)
        {
            // Nothing to migrate if we no longer keep a buffer of the corresponding type.
            if (!newBuffer.valid)
                return;

            // We do the same if we don't had a corresponding buffer before.
            if (!oldBuffer.valid)
                return;

            ////TOOD: if we assume linear layouts of devices in 'devices' and assume that new devices are only added
            ////      at the end and only single devices can be removed, we can copy state buffers much more efficiently
            ////      in bulk rather than device-by-device

            // Migrate every device that has allocated state blocks.
            var newDeviceCount = deviceCount;
            var oldDeviceCount = oldDeviceIndices != null ? oldDeviceIndices.Length : newDeviceCount;
            for (var i = 0; i < newDeviceCount && i < oldDeviceCount; ++i)
            {
                var device = devices[i];
                Debug.Assert(device.m_DeviceIndex == i);

                // Skip device if it's a newly added device.
                if (device.m_StateBlock.byteOffset == InputStateBlock.kInvalidOffset)
                    continue;

                ////FIXME: this is not protecting against devices that have changed their formats between domain reloads

                var oldDeviceIndex = oldDeviceIndices != null ? oldDeviceIndices[i] : i;
                var newDeviceIndex = i;
                var numBytes = device.m_StateBlock.alignedSizeInBytes;

                var oldFrontPtr = (byte*)oldBuffer.GetFrontBuffer(oldDeviceIndex).ToPointer() + (int)device.m_StateBlock.byteOffset;
                var oldBackPtr = (byte*)oldBuffer.GetBackBuffer(oldDeviceIndex).ToPointer() + (int)device.m_StateBlock.byteOffset;

                var newFrontPtr = (byte*)newBuffer.GetFrontBuffer(newDeviceIndex).ToPointer() + (int)newStateBlockOffsets[i];
                var newBackPtr = (byte*)newBuffer.GetBackBuffer(newDeviceIndex).ToPointer() + (int)newStateBlockOffsets[i];

                // Copy state.
                UnsafeUtility.MemCpy(newFrontPtr, oldFrontPtr, numBytes);
                UnsafeUtility.MemCpy(newBackPtr, oldBackPtr, numBytes);
            }
        }

        private unsafe void MigrateSingleBuffer(IntPtr newBuffer, InputDevice[] devices, int deviceCount, uint[] newStateBlockOffsets, IntPtr oldBuffer)
        {
            // Migrate every device that has allocated state blocks.
            var newDeviceCount = deviceCount;
            for (var i = 0; i < newDeviceCount; ++i)
            {
                var device = devices[i];
                Debug.Assert(device.m_DeviceIndex == i);

                // Skip device if it's a newly added device.
                if (device.m_StateBlock.byteOffset == InputStateBlock.kInvalidOffset)
                    continue;

                ////FIXME: this is not protecting against devices that have changed their formats between domain reloads

                var numBytes = device.m_StateBlock.alignedSizeInBytes;
                var oldStatePtr = (byte*)oldBuffer.ToPointer() + (int)device.m_StateBlock.byteOffset;
                var newStatePtr = (byte*)newBuffer.ToPointer() + (int)newStateBlockOffsets[i];

                UnsafeUtility.MemCpy(newStatePtr, oldStatePtr, numBytes);
            }
        }

        // Compute the total size of we need for a single state buffer to encompass
        // all devices we have and also linearly assign offsets to all the devices
        // within such a buffer.
        private static uint ComputeSizeOfSingleBufferAndOffsetForEachDevice(InputDevice[] devices, int deviceCount, ref uint[] offsets)
        {
            if (devices == null)
                return 0;

            var result = new uint[deviceCount];
            var currentOffset = 0u;
            var sizeInBytes = 0u;

            for (var i = 0; i < deviceCount; ++i)
            {
                var size = devices[i].m_StateBlock.alignedSizeInBytes;
                size = NumberHelpers.AlignToMultiple(size, 4);
                ////REVIEW: what should we do about this case? silently accept it and just give the device the current offset?
                if (size == 0)
                    throw new Exception(string.Format("Device '{0}' has a zero-size state buffer", devices[i]));
                sizeInBytes += size;
                result[i] = currentOffset;
                currentOffset += (uint)size;
            }

            offsets = result;
            return sizeInBytes;
        }
    }
}
