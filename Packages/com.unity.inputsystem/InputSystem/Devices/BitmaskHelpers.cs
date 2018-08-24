using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Processors;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    public unsafe struct BitmaskHelpers
    {
        static public void Whitelist(IntPtr filterBuffer, InputControl control)
        {
            MarkInBuffer(filterBuffer, control.stateBlock.byteOffset, control.stateBlock.sizeInBits, true);
        }

        static public void Blacklist(IntPtr filterBuffer, InputControl control)
        {
            MarkInBuffer(filterBuffer, control.stateBlock.byteOffset, control.stateBlock.sizeInBits, false);
        }

        static public void MarkInBuffer(IntPtr filterBuffer, uint byteOffset, uint sizeInBits, bool state)
        {
            uint sizeRemaining = sizeInBits;

            uint* filterIter = (uint*)((filterBuffer.ToInt64() + (Int64)byteOffset));
            while (sizeRemaining >= 32)
            {
                *filterIter = state ? 0xFFFFFFFF : 0;
                filterIter++;
                sizeRemaining -= 32;
            }

            uint mask = (uint)((1 << (int)sizeRemaining) - 1);
            if (state)
            {
                *filterIter |= mask;
            }
            else
            {
                *filterIter &= ~mask;
            }
        }

        static public bool CheckForMaskedValues(IntPtr eventBuffer , IntPtr maskPtr, uint offsetBytes, uint sizeInBits)
        {
            uint sizeRemaining = sizeInBits;

            uint* eventIter = (uint*)eventBuffer.ToPointer();

            uint* maskIter = (uint*)(new IntPtr(maskPtr.ToInt64() + (Int64)offsetBytes).ToPointer());

            while (sizeRemaining >= 32)
            {
                if ((*eventIter & *maskIter) != 0)
                    return true;

                eventIter++;
                maskIter++;

                sizeRemaining -= 32;
            }

            //Find the remaining bytes to check
            // Mask it in the state iterator and noise
            uint remainingState = *eventIter;
            uint remainingMask = *maskIter;

            int mask = ((1 >> (int)sizeRemaining) - 1);


            if ((remainingState & (remainingMask & mask)) != 0)
                return true;

            return false;
        }
    }
}
