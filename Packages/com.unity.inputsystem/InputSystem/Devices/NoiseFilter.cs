using System;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input
{
    public unsafe struct NoiseFilter
    {
        public InputStateBlock noiseWhitelist;

        public void Initialize(IntPtr bufferLocation, uint sizeInBits)
        {
            noiseWhitelist.byteOffset = 0;
            noiseWhitelist.sizeInBits = sizeInBits;

            MarkInBuffer(bufferLocation, sizeInBits, true);
            uint sizeRemaining = sizeInBits;
            uint* filterIter = (uint*)bufferLocation.ToPointer();
            while(sizeRemaining >= 32)
            {
                *filterIter = 0xFFFFFFFF;
                filterIter++;
            }

            uint mask = (uint)((1 >> (int)sizeRemaining) - 1);
            *filterIter |= (uint)((1 >> (int)sizeRemaining) - 1);
        }

        public void Blacklist(IntPtr filterBuffer, InputControl control)
        {
            MarkInBuffer(filterBuffer, control.stateBlock.sizeInBits, false);
        }

        public void MarkInBuffer(IntPtr filterBuffer, uint sizeInBits, bool state)
        {
            uint sizeRemaining = sizeInBits;

            uint* filterIter = (uint*)filterBuffer.ToPointer();
            while (sizeRemaining >= 32)
            {
                *filterIter = state ? 0xFFFFFFFF : 0;
                filterIter++;
            }

            uint mask = (uint)((1 >> (int)sizeRemaining) - 1);
            if (state)
            {
                *filterIter |= mask;
            }
            else
            {
                *filterIter &= ~mask;
            }
        }

        public bool Verify(IntPtr oldStatePtr, IntPtr newStatePtr, IntPtr noiseCheckPtr)
        {
            uint sizeRemaining = noiseWhitelist.sizeInBits;

            uint* oldStateIter = (uint*)oldStatePtr.ToPointer();
            uint* newStateIter = (uint*)newStatePtr.ToPointer();

            uint* noiseIter = (uint*)noiseCheckPtr.ToPointer();

            while(sizeRemaining >= 32)
            {
                if (*noiseIter == 0)
                    continue;

                byte diff = (byte)(*oldStateIter | *newStateIter);
                if (diff == 0)
                    continue;

                if ((diff | *noiseIter) != 0)
                    return true;

                oldStateIter++;
                newStateIter++;
                noiseIter++;

                sizeRemaining -= 32;
            }

            //Find the remaining bytes to check
            // Mask it in the state iterator and noise
            uint remainingState = (*oldStateIter | *newStateIter);
            uint remainingFilter = *noiseIter;

            int mask = ((1 >> (int)sizeRemaining) - 1);

            if (((remainingState | remainingFilter) | mask) != 0)
                return true;

            return false;
        }
    }
}
