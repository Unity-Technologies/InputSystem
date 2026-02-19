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
        /// Buffer that contains a bit mask that masks out all noisy controls.
        /// </summary>
        public void* noiseMaskBuffer;

        /// <summary>
        /// Buffer that contains a bit mask that masks out all dontReset controls.
        /// </summary>
        public void* resetMaskBuffer;

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
            public int deviceCount;

            public bool valid => deviceToBufferMapping != null;

            public void SetFrontBuffer(int deviceIndex, void* ptr)
            {
                if (deviceIndex < deviceCount)
                    deviceToBufferMapping[deviceIndex * 2] = ptr;
            }

            public void SetBackBuffer(int deviceIndex, void* ptr)
            {
                if (deviceIndex < deviceCount)
                    deviceToBufferMapping[deviceIndex * 2 + 1] = ptr;
            }

            public void* GetFrontBuffer(int deviceIndex)
            {
                if (deviceIndex < deviceCount)
                    return deviceToBufferMapping[deviceIndex * 2];
                return null;
            }

            public void* GetBackBuffer(int deviceIndex)
            {
                if (deviceIndex < deviceCount)
                    return deviceToBufferMapping[deviceIndex * 2 + 1];
                return null;
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
        internal static void* s_ResetMaskBuffer;
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
        public void AllocateAll(InputDevice[] devices, int deviceCount)
        {
            sizePerBuffer = ComputeSizeOfSingleStateBuffer(devices, deviceCount);
            if (sizePerBuffer == 0)
                return;
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

            // Plus 3 more buffers (one for default states, one for noise masks, and one for dontReset masks).
            totalSize += sizePerBuffer * 3;

            // Allocate.
            m_AllBuffers = UnsafeUtility.Malloc(totalSize, 4, Allocator.Persistent);
            UnsafeUtility.MemClear(m_AllBuffers, totalSize);

            // Set up device to buffer mappings.
            var ptr = (byte*)m_AllBuffers;
            m_PlayerStateBuffers =
                SetUpDeviceToBufferMappings(deviceCount, ref ptr, sizePerBuffer,
                    mappingTableSizePerBuffer);

            #if UNITY_EDITOR
            m_EditorStateBuffers =
                SetUpDeviceToBufferMappings(deviceCount, ref ptr, sizePerBuffer, mappingTableSizePerBuffer);
            #endif

            // Default state and noise filter buffers go last.
            defaultStateBuffer = ptr;
            noiseMaskBuffer = ptr + sizePerBuffer;
            resetMaskBuffer = ptr + sizePerBuffer * 2;
        }

        private static DoubleBuffers SetUpDeviceToBufferMappings(int deviceCount, ref byte* bufferPtr, uint sizePerBuffer, uint mappingTableSizePerBuffer)
        {
            var front = bufferPtr;
            var back = bufferPtr + sizePerBuffer;
            var mappings = (void**)(bufferPtr + sizePerBuffer * 2);  // Put mapping table at end.
            bufferPtr += sizePerBuffer * 2 + mappingTableSizePerBuffer;

            var buffers = new DoubleBuffers
            {
                deviceToBufferMapping = mappings,
                deviceCount = deviceCount
            };

            for (var i = 0; i < deviceCount; ++i)
            {
                var deviceIndex = i;
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

            if (s_ResetMaskBuffer == resetMaskBuffer)
                s_ResetMaskBuffer = null;

            noiseMaskBuffer = null;
            resetMaskBuffer = null;

            totalSize = 0;
            sizePerBuffer = 0;
        }

        // Migrate state data for all devices from a previous set of buffers to the current set of buffers.
        // Copies all state from their old locations to their new locations and bakes the new offsets into
        // the control hierarchies of the given devices.
        // NOTE: When having oldBuffers, this method only works properly if the only alteration compared to the
        //       new buffers is that either devices have been removed or devices have been added. Cannot be
        //       a mix of the two. Also, new devices MUST be added to the end and cannot be inserted in the middle.
        // NOTE: Also, state formats MUST not change from before. A device that has changed its format must
        //       be treated as a newly device that didn't exist before.
        public void MigrateAll(InputDevice[] devices, int deviceCount, InputStateBuffers oldBuffers)
        {
            // If we have old data, perform migration.
            if (oldBuffers.totalSize > 0)
            {
                MigrateDoubleBuffer(m_PlayerStateBuffers, devices, deviceCount, oldBuffers.m_PlayerStateBuffers);

#if UNITY_EDITOR
                MigrateDoubleBuffer(m_EditorStateBuffers, devices, deviceCount, oldBuffers.m_EditorStateBuffers);
#endif

                MigrateSingleBuffer(defaultStateBuffer, devices, deviceCount, oldBuffers.defaultStateBuffer);
                MigrateSingleBuffer(noiseMaskBuffer, devices, deviceCount, oldBuffers.noiseMaskBuffer);
                MigrateSingleBuffer(resetMaskBuffer, devices, deviceCount, oldBuffers.resetMaskBuffer);
            }

            // Assign state blocks. This is where devices will receive their updates state offsets. Up
            // until now we've left any previous m_StateBlocks alone.
            var newOffset = 0u;
            for (var i = 0; i < deviceCount; ++i)
            {
                var device = devices[i];
                var oldOffset = device.m_StateBlock.byteOffset;

                if (oldOffset == InputStateBlock.InvalidOffset)
                {
                    // Device is new and has no offset yet baked into it.
                    device.m_StateBlock.byteOffset = 0;
                    if (newOffset != 0)
                        device.BakeOffsetIntoStateBlockRecursive(newOffset);
                }
                else
                {
                    // Device is not new and still has its old offset baked into it. We could first unbake the old offset
                    // and then bake the new one but instead just bake a relative offset.
                    var delta = newOffset - oldOffset;
                    if (delta != 0)
                        device.BakeOffsetIntoStateBlockRecursive(delta);
                }

                Debug.Assert(device.m_StateBlock.byteOffset == newOffset, "Device state offset not set correctly");

                newOffset = NextDeviceOffset(newOffset, device);
            }
        }

        private static void MigrateDoubleBuffer(DoubleBuffers newBuffer, InputDevice[] devices, int deviceCount, DoubleBuffers oldBuffer)
        {
            // Nothing to migrate if we no longer keep a buffer of the corresponding type.
            if (!newBuffer.valid)
                return;

            // We do the same if we don't had a corresponding buffer before.
            if (!oldBuffer.valid)
                return;

            // Migrate every device that has allocated state blocks.
            var newStateBlockOffset = 0u;
            for (var i = 0; i < deviceCount; ++i)
            {
                var device = devices[i];

                // Stop as soon as we're hitting a new device. Newly added devices *must* be *appended* to the
                // array as otherwise our computing of offsets into the old buffer may be wrong.
                // NOTE: This also means that device indices of
                if (device.m_StateBlock.byteOffset == InputStateBlock.InvalidOffset)
                {
                    #if DEVELOPMENT_BUILD || UNITY_EDITOR
                    for (var n = i + 1; n < deviceCount; ++n)
                        Debug.Assert(devices[n].m_StateBlock.byteOffset == InputStateBlock.InvalidOffset,
                            "New devices must be appended to the array; found an old device coming in the array after a newly added device");
                    #endif
                    break;
                }

                var oldDeviceIndex = device.m_DeviceIndex;
                var newDeviceIndex = i;
                var numBytes = device.m_StateBlock.alignedSizeInBytes;

                var oldFrontPtr = (byte*)oldBuffer.GetFrontBuffer(oldDeviceIndex) + (int)device.m_StateBlock.byteOffset; // m_StateBlock still refers to oldBuffer.
                var oldBackPtr = (byte*)oldBuffer.GetBackBuffer(oldDeviceIndex) + (int)device.m_StateBlock.byteOffset;

                var newFrontPtr = (byte*)newBuffer.GetFrontBuffer(newDeviceIndex) + (int)newStateBlockOffset;
                var newBackPtr = (byte*)newBuffer.GetBackBuffer(newDeviceIndex) + (int)newStateBlockOffset;

                // Copy state.
                UnsafeUtility.MemCpy(newFrontPtr, oldFrontPtr, numBytes);
                UnsafeUtility.MemCpy(newBackPtr, oldBackPtr, numBytes);

                newStateBlockOffset = NextDeviceOffset(newStateBlockOffset, device);
            }
        }

        private static void MigrateSingleBuffer(void* newBuffer, InputDevice[] devices, int deviceCount, void* oldBuffer)
        {
            // Migrate every device that has allocated state blocks.
            var newDeviceCount = deviceCount;
            var newStateBlockOffset = 0u;
            for (var i = 0; i < newDeviceCount; ++i)
            {
                var device = devices[i];

                // Stop if we've reached newly added devices.
                if (device.m_StateBlock.byteOffset == InputStateBlock.InvalidOffset)
                    break;

                var numBytes = device.m_StateBlock.alignedSizeInBytes;
                var oldStatePtr = (byte*)oldBuffer + (int)device.m_StateBlock.byteOffset;
                var newStatePtr = (byte*)newBuffer + (int)newStateBlockOffset;

                UnsafeUtility.MemCpy(newStatePtr, oldStatePtr, numBytes);

                newStateBlockOffset = NextDeviceOffset(newStateBlockOffset, device);
            }
        }

        private static uint ComputeSizeOfSingleStateBuffer(InputDevice[] devices, int deviceCount)
        {
            var sizeInBytes = 0u;
            for (var i = 0; i < deviceCount; ++i)
                sizeInBytes = NextDeviceOffset(sizeInBytes, devices[i]);
            return sizeInBytes;
        }

        private static uint NextDeviceOffset(uint currentOffset, InputDevice device)
        {
            var sizeOfDevice = device.m_StateBlock.alignedSizeInBytes;
            if (sizeOfDevice == 0) // Shouldn't happen as we don't allow empty layouts but make sure we catch this if something slips through.
                throw new ArgumentException($"Device '{device}' has a zero-size state buffer", nameof(device));
            return currentOffset + sizeOfDevice.AlignToMultipleOf(4);
        }
    }
}
