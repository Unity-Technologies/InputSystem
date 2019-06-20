using System;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

////REVIEW: Can we change this into a setup where the buffering depth isn't fixed to 2 but rather
////        can be set on a per device basis?

namespace UnityEngine.InputSystem.LowLevel
{
    // The raw memory blocks which are indexed by InputStateBlocks.
    //
    // Internally, we perform only a single combined unmanaged allocation for all state
    // buffers needed by the system. Externally, we expose them as if they are each separate
    // buffers.
    internal unsafe struct InputStateBuffers
    {
        // State buffers are set up in a double buffering scheme where the "back buffer"
        // represents the previous state of devices and the "front buffer" represents
        // the current state.
        //
        // Edit mode and play mode each get their own double buffering. Updates to them
        // are tied to focus and only one mode will actually receive state events while the
        // other mode is dormant. In the player, we only get play mode buffers, of course.

        ////TODO: need to clear the current buffers when switching between edit and play mode
        ////      (i.e. if you click an editor window while in play mode, the play mode
        ////      device states will all go back to default)
        ////      actually, if we really reset on mode change, can't we just keep a single set buffers?

        public uint sizePerBuffer;
        public uint totalSize;

        /// <summary>
        /// Buffer that has state for each device initialized with default values.
        /// </summary>
        public void* defaultStateBuffer;

        /// <summary>
        /// Buffer that contains bitflags for noisy and non-noisy controls, to identify significant device changes.
        /// </summary>
        public void* noiseMaskBuffer;

        // Secretly we perform only a single allocation.
        // This allocation also contains the device-to-state mappings.
        private void* m_AllBuffers;

        // Contains information about a double buffer setup.
        [Serializable]
        internal struct DoubleBuffers
        {
            ////REVIEW: store timestamps along with each device-to-buffer mapping?
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

        internal DoubleBuffers m_PlayerStateBuffers;

#if UNITY_EDITOR
        internal DoubleBuffers m_EditorStateBuffers;
#endif

        public DoubleBuffers GetDoubleBuffersFor(InputUpdateType updateType)
        {
            switch (updateType)
            {
                case InputUpdateType.BeforeRender:
                case InputUpdateType.Fixed:
                case InputUpdateType.Dynamic:
                case InputUpdateType.Manual:
                    return m_PlayerStateBuffers;
#if UNITY_EDITOR
                case InputUpdateType.Editor:
                    return m_EditorStateBuffers;
#endif
            }

            throw new ArgumentException("Unrecognized InputUpdateType: " + updateType, nameof(updateType));
        }

        internal static void* s_DefaultStateBuffer;
        internal static void* s_NoiseMaskBuffer;
        internal static DoubleBuffers s_CurrentBuffers;

        public static void* GetFrontBufferForDevice(int deviceIndex)
        {
            return s_CurrentBuffers.GetFrontBuffer(deviceIndex);
        }

        public static void* GetBackBufferForDevice(int deviceIndex)
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
        public uint[] AllocateAll(InputDevice[] devices, int deviceCount)
        {
            uint[] newDeviceOffsets = null;
            sizePerBuffer = ComputeSizeOfSingleBufferAndOffsetForEachDevice(devices, deviceCount, ref newDeviceOffsets);
            if (sizePerBuffer == 0)
                return null;
            sizePerBuffer = sizePerBuffer.AlignToMultipleOf(4);

            // Determine how much memory we need.
            var mappingTableSizePerBuffer = (uint)(deviceCount * sizeof(void*) * 2);

            totalSize = 0;

            totalSize += sizePerBuffer * 2;
            totalSize += mappingTableSizePerBuffer;

            #if UNITY_EDITOR
            totalSize += sizePerBuffer * 2;
            totalSize += mappingTableSizePerBuffer;
            #endif

            // Plus 2 more buffers (1 for default states, and one for noise masks).
            totalSize += sizePerBuffer * 2;

            // Allocate.
            m_AllBuffers = UnsafeUtility.Malloc(totalSize, 4, Allocator.Persistent);
            UnsafeUtility.MemClear(m_AllBuffers, totalSize);

            // Set up device to buffer mappings.
            var ptr = (byte*)m_AllBuffers;
            m_PlayerStateBuffers =
                SetUpDeviceToBufferMappings(devices, deviceCount, ref ptr, sizePerBuffer,
                    mappingTableSizePerBuffer);

            #if UNITY_EDITOR
            m_EditorStateBuffers =
                SetUpDeviceToBufferMappings(devices, deviceCount, ref ptr, sizePerBuffer, mappingTableSizePerBuffer);
            #endif

            // Default state and noise filter buffers go last.
            defaultStateBuffer = ptr;
            noiseMaskBuffer = ptr + sizePerBuffer;

            return newDeviceOffsets;
        }

        private static DoubleBuffers SetUpDeviceToBufferMappings(InputDevice[] devices, int deviceCount, ref byte* bufferPtr, uint sizePerBuffer, uint mappingTableSizePerBuffer)
        {
            var front = bufferPtr;
            var back = bufferPtr + sizePerBuffer;
            var mappings = (void**)(bufferPtr + sizePerBuffer * 2);  // Put mapping table at end.
            bufferPtr += sizePerBuffer * 2 + mappingTableSizePerBuffer;

            var buffers = new DoubleBuffers {deviceToBufferMapping = mappings};

            for (var i = 0; i < deviceCount; ++i)
            {
                var deviceIndex = devices[i].m_DeviceIndex;

                buffers.SetFrontBuffer(deviceIndex, front);
                buffers.SetBackBuffer(deviceIndex, back);
            }

            return buffers;
        }

        public void FreeAll()
        {
            if (m_AllBuffers != null)
            {
                UnsafeUtility.Free(m_AllBuffers, Allocator.Persistent);
                m_AllBuffers = null;
            }

            m_PlayerStateBuffers = new DoubleBuffers();

#if UNITY_EDITOR
            m_EditorStateBuffers = new DoubleBuffers();
#endif

            s_CurrentBuffers = new DoubleBuffers();

            if (s_DefaultStateBuffer == defaultStateBuffer)
                s_DefaultStateBuffer = null;

            defaultStateBuffer = null;

            if (s_NoiseMaskBuffer == noiseMaskBuffer)
                s_NoiseMaskBuffer = null;

            noiseMaskBuffer = null;

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
                MigrateDoubleBuffer(m_PlayerStateBuffers, devices, deviceCount, newStateBlockOffsets, oldBuffers.m_PlayerStateBuffers,
                    oldDeviceIndices);

#if UNITY_EDITOR
                MigrateDoubleBuffer(m_EditorStateBuffers, devices, deviceCount, newStateBlockOffsets, oldBuffers.m_EditorStateBuffers,
                    oldDeviceIndices);
#endif

                MigrateSingleBuffer(defaultStateBuffer, devices, deviceCount, newStateBlockOffsets, oldBuffers.defaultStateBuffer);
                MigrateSingleBuffer(noiseMaskBuffer, devices, deviceCount, newStateBlockOffsets, oldBuffers.noiseMaskBuffer);
            }

            // Assign state blocks.
            for (var i = 0; i < deviceCount; ++i)
            {
                var newOffset = newStateBlockOffsets[i];
                var device = devices[i];
                var oldOffset = device.m_StateBlock.byteOffset;

                if (oldOffset == InputStateBlock.InvalidOffset)
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

        private static void MigrateDoubleBuffer(DoubleBuffers newBuffer, InputDevice[] devices, int deviceCount, uint[] newStateBlockOffsets, DoubleBuffers oldBuffer, int[] oldDeviceIndices)
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
            var oldDeviceCount = oldDeviceIndices?.Length ?? newDeviceCount;
            for (var i = 0; i < newDeviceCount && i < oldDeviceCount; ++i)
            {
                var device = devices[i];
                Debug.Assert(device.m_DeviceIndex == i);

                // Skip device if it's a newly added device.
                if (device.m_StateBlock.byteOffset == InputStateBlock.InvalidOffset)
                    continue;

                ////FIXME: this is not protecting against devices that have changed their formats between domain reloads

                var oldDeviceIndex = oldDeviceIndices ? [i] ?? i;
                var newDeviceIndex = i;
                var numBytes = device.m_StateBlock.alignedSizeInBytes;

                var oldFrontPtr = (byte*)oldBuffer.GetFrontBuffer(oldDeviceIndex) + (int)device.m_StateBlock.byteOffset;
                var oldBackPtr = (byte*)oldBuffer.GetBackBuffer(oldDeviceIndex) + (int)device.m_StateBlock.byteOffset;

                var newFrontPtr = (byte*)newBuffer.GetFrontBuffer(newDeviceIndex) + (int)newStateBlockOffsets[i];
                var newBackPtr = (byte*)newBuffer.GetBackBuffer(newDeviceIndex) + (int)newStateBlockOffsets[i];

                // Copy state.
                UnsafeUtility.MemCpy(newFrontPtr, oldFrontPtr, numBytes);
                UnsafeUtility.MemCpy(newBackPtr, oldBackPtr, numBytes);
            }
        }

        private static void MigrateSingleBuffer(void* newBuffer, InputDevice[] devices, int deviceCount, uint[] newStateBlockOffsets, void* oldBuffer)
        {
            // Migrate every device that has allocated state blocks.
            var newDeviceCount = deviceCount;
            for (var i = 0; i < newDeviceCount; ++i)
            {
                var device = devices[i];
                Debug.Assert(device.m_DeviceIndex == i);

                // Skip device if it's a newly added device.
                if (device.m_StateBlock.byteOffset == InputStateBlock.InvalidOffset)
                    continue;

                var numBytes = device.m_StateBlock.alignedSizeInBytes;
                var oldStatePtr = (byte*)oldBuffer + (int)device.m_StateBlock.byteOffset;
                var newStatePtr = (byte*)newBuffer + (int)newStateBlockOffsets[i];

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
            var sizeInBytes = 0u;

            for (var i = 0; i < deviceCount; ++i)
            {
                var sizeOfDevice = devices[i].m_StateBlock.alignedSizeInBytes;
                sizeOfDevice = sizeOfDevice.AlignToMultipleOf(4);
                if (sizeOfDevice == 0) // Shouldn't happen as we don't allow empty layouts but make sure we catch this if something slips through.
                    throw new ArgumentException($"Device '{devices[i]}' has a zero-size state buffer", nameof(devices));
                result[i] = sizeInBytes;
                sizeInBytes += sizeOfDevice;
            }

            offsets = result;
            return sizeInBytes;
        }
    }
}
