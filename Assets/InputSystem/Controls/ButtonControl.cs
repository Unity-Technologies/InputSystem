using System;

namespace ISX
{
    ////REVIEW: shouldn't this still have a float value type?
    public class ButtonControl : InputControl<bool>
    {
        public ButtonControl()
        {
            m_StateBlock.sizeInBits = 1;
        }

        public override bool value
        {
            get { return GetValue(currentValuePtr); }
        }

        public bool wasPressedThisFrame
        {
            get { return value != GetValue(previousValuePtr); }
        }

        protected unsafe bool GetValue(IntPtr valuePtr)
        {
            ////TODO: currently this is not actually enforced...
            // The layout code makes sure that bitfields are either 8bit or multiples
            // of 32bits. So we always safely read either a byte or int. Handling
            // the 8bit and 32bit case directly will lead to nicely aligned memory
            // accesses if the state has been laid out that way.

            int bits;
            var bitOffset = m_StateBlock.bitOffset;

            if (bitOffset < 8)
            {
                bits = *((byte*)valuePtr);
            }
            else if (bitOffset < 32)
            {
                bits = *((int*)valuePtr);
            }
            else
            {
                // Long bitfield. Compute an offset to the byte we need and fetch
                // only that byte. Adjust the bit offset to be for that byte.
                // On this path, we may end up doing memory accesses that the CPU
                // doesn't like much.

                var byteOffset = bitOffset / 8;
                bitOffset = bitOffset % 8;

                bits = *((byte*)valuePtr + byteOffset);
            }

            var value = (bits & bitOffset) == bitOffset;

            return Process(value);
        }
    }
}
