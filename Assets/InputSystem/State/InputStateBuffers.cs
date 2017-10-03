using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Collections;

namespace ISX
{
    // The raw memory blocks which are indexed by InputStateBlocks.
    //
    // Internally, we perform only a single combined allocated for all state buffers
    // needed by the system. Externally, we expose them as if they are each separate
    // buffers.
#if UNITY_EDITOR
    [Serializable]
#endif
    internal struct InputStateBuffers
    {
        // There's only one buffer that represents the most recent state for devices
        // but we keep a separate buffer for each type of update to denote the state that
        // devices had in the preceding update of that type.
        //
        // If there's only one type of player update enabled, we can just constantly swap
        // the current and previous buffer. If multiple update types are enabled, we need
        // to memcpy state buffers at various points.

        public int sizePerBuffer;
        public int totalSize;

        public IntPtr sharedCurrentStateBuffer;
        public IntPtr previousDynamicUpdateStateBuffer;
        public IntPtr previousFixedUpdateStateBuffer;

        // Updates for the editor are always separate. If the game view has focus, all
        // state changes go into the buffers above. If any other view in the editor has
        // focus, state changes go into the buffers here.
#if UNITY_EDITOR
        public IntPtr currentEditorStateBuffer;
        public IntPtr previousEditorStateBuffer;
#endif

        // Secretely we perform only a single allocation.
#if UNITY_EDITOR
        [SerializeField]
#endif
        private IntPtr m_AllBuffers;

        // Switch the current set of buffers used by the system.
        public void SwitchTo(InputUpdateType update)
        {
            var current = IntPtr.Zero;
            var previous = IntPtr.Zero;

            switch (update)
            {
                case InputUpdateType.Dynamic:
                    current = sharedCurrentStateBuffer;
                    previous = previousDynamicUpdateStateBuffer;
                    break;
                case InputUpdateType.Fixed:
                    current = sharedCurrentStateBuffer;
                    previous = previousFixedUpdateStateBuffer;
                    break;
#if UNITY_EDITOR
                case InputUpdateType.Editor:
                    current = currentEditorStateBuffer;
                    previous = previousEditorStateBuffer;
                    break;
#endif
            }

            InputStateBlock.s_CurrentStatePtr = current;
            InputStateBlock.s_PreviousStatePtr = previous;
        }

        // Swap meaning of current and previous for the given update type
        // and switch to its buffers.
        public void SwapAndSwitchTo(InputUpdateType update)
        {
            ////TODO: implement swapping
            SwitchTo(update);
        }

        // Allocates all buffers to serve the given updates and comes up with a spot
        // for the state block of each device. Returns the new state blocks for the
        // devices (it will *NOT* install them on the devices).
        public uint[] AllocateAll(InputUpdateType updateMask, InputDevice[] devices)
        {
            uint[] newDeviceOffsets = null;
            sizePerBuffer = ComputeSizeOfSingleBufferAndOffsetForEachDevice(devices, ref newDeviceOffsets);

            var isDynamicUpdateEnabled = (updateMask & InputUpdateType.Dynamic) == InputUpdateType.Dynamic;
            var isFixedUpdateEnabled = (updateMask & InputUpdateType.Fixed) == InputUpdateType.Fixed;

            totalSize = sizePerBuffer; // Always want current buffer for player updates.
            if (isDynamicUpdateEnabled)
                totalSize += sizePerBuffer;
            if (isFixedUpdateEnabled)
                totalSize += sizePerBuffer;

#if UNITY_EDITOR
            totalSize += sizePerBuffer * 2;
#endif

            if (totalSize == 0)
                return null;

            m_AllBuffers = UnsafeUtility.Malloc(totalSize, 4, Allocator.Persistent);
            var ptr = m_AllBuffers;
            UnsafeUtility.MemClear(ptr, totalSize);

            sharedCurrentStateBuffer = ptr;
            if (isDynamicUpdateEnabled)
            {
                ptr = new IntPtr(ptr.ToInt64() + sizePerBuffer);
                previousDynamicUpdateStateBuffer = ptr;
            }
            if (isFixedUpdateEnabled)
            {
                ptr = new IntPtr(ptr.ToInt64() + sizePerBuffer);
                previousFixedUpdateStateBuffer = ptr;
            }

#if UNITY_EDITOR
            ptr = new IntPtr(ptr.ToInt64() + sizePerBuffer);
            currentEditorStateBuffer = ptr;

            ptr = new IntPtr(ptr.ToInt64() + sizePerBuffer);
            previousEditorStateBuffer = ptr;
#endif

            return newDeviceOffsets;
        }

        public void FreeAll()
        {
            if (m_AllBuffers != IntPtr.Zero)
                UnsafeUtility.Free(m_AllBuffers, Allocator.Persistent);

            sharedCurrentStateBuffer = IntPtr.Zero;
            previousDynamicUpdateStateBuffer = IntPtr.Zero;
            previousFixedUpdateStateBuffer = IntPtr.Zero;

#if UNITY_EDITOR
            currentEditorStateBuffer = IntPtr.Zero;
            previousEditorStateBuffer = IntPtr.Zero;
#endif
        }

        public void MigrateAll(InputDevice[] devices, uint[] newStateBlockOffsets, InputStateBuffers oldBuffers)
        {
            // If we have old data, perform migration.
            // Note that the enabled update types don't need to match between the old set of buffers
            // and the new set of buffers.
            if (oldBuffers.totalSize > 0)
            {
                MigrateSingle(sharedCurrentStateBuffer, devices, newStateBlockOffsets, oldBuffers.sharedCurrentStateBuffer);
                MigrateSingle(previousDynamicUpdateStateBuffer, devices, newStateBlockOffsets, oldBuffers.previousDynamicUpdateStateBuffer);
                MigrateSingle(previousFixedUpdateStateBuffer, devices, newStateBlockOffsets, oldBuffers.previousFixedUpdateStateBuffer);

#if UNITY_EDITOR
                MigrateSingle(currentEditorStateBuffer, devices, newStateBlockOffsets, oldBuffers.currentEditorStateBuffer);
                MigrateSingle(previousEditorStateBuffer, devices, newStateBlockOffsets, oldBuffers.previousEditorStateBuffer);
#endif
            }

            // Assign state blocks.
            for (var i = 0; i < devices.Length; ++i)
            {
                var offset = newStateBlockOffsets[i];
                var device = devices[i];

                device.m_StateBufferOffset = offset;
                devices[i].BakeOffsetIntoStateBlockRecursive(offset);
            }
        }

        private void MigrateSingle(IntPtr newBuffer, InputDevice[] devices, uint[] newStateBlockOffsets, IntPtr oldBuffer)
        {
            // Nothing to migrate if we no longer keep a buffer or the corresponding type.
            if (newBuffer == IntPtr.Zero)
                return;

            // We do the same if we don't had a corresponding buffer before.
            ////REVIEW: this is less clear cut; should we copy the data from some other buffer?
            if (oldBuffer == IntPtr.Zero)
                return;

            // Migrate every device that has allocated state blocks.
            for (var i = 0; i < devices.Length; ++i)
            {
                var device = devices[i];

                // Skip if this is a device that got added after we allocated the
                // previous buffers.
                if (!device.m_StateBlock.isAllocated)
                    continue;

                var sourcePtr = new IntPtr(oldBuffer.ToInt64() + device.m_StateBlock.byteOffset);
                var destinationPtr = new IntPtr(newBuffer.ToInt64() + newStateBlockOffsets[i]);
                var numBytes = device.m_StateBlock.alignedSizeInBytes;

                // Copy state.
                UnsafeUtility.MemCpy(sourcePtr, destinationPtr, numBytes);
            }
        }

        // Compute the total size of we need for a single state buffer to encompass
        // all devices we have and also linearly assign offsets to all the devices
        // within such a buffer.
        public static int ComputeSizeOfSingleBufferAndOffsetForEachDevice(InputDevice[] devices, ref uint[] offsets)
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
