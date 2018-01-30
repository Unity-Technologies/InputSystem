using System;
using ISX.LowLevel;
using ISX.Processors;
using ISX.Utilities;
using UnityEngine;

namespace ISX.Controls
{
    /// <summary>
    /// A floating-point axis control.
    /// </summary>
    /// <remarks>
    /// Can optionally be configured to perform normalization.
    /// Stored as either a float, a short, a byte, or a single bit.
    /// </remarks>
    public class AxisControl : InputControl<float>
    {
        // These can be added as processors but they are so common that we
        // build the functionality right into AxisControl to save us an
        // additional object and an additional virtual call.
        public bool clamp; // If true, force clamping to [min..max]
        public float clampMin;
        public float clampMax;
        public bool invert; // If true, multiply by -1.
        public bool normalize;
        public float normalizeMin;
        public float normalizeMax;
        public float normalizeZero;

        protected float Preprocess(float value)
        {
            if (clamp)
                value = Mathf.Clamp(value, clampMin, clampMax);
            if (normalize)
                value = NormalizeProcessor.Normalize(value, normalizeMin, normalizeMax, normalizeZero);
            if (invert)
                value *= -1.0f;
            return value;
        }

        public AxisControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeFloat;
        }

        // Read a floating-point value from the given state. Automatically checks
        // the state format of the control and performs conversions.
        // NOTE: Throws if the format set on 'stateBlock' is not of integer, floating-point,
        //       or bitfield type.
        protected override unsafe float ReadRawValueFrom(IntPtr statePtr)
        {
            float value;
            var valuePtr = new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);

            var format = m_StateBlock.format;
            if (format == InputStateBlock.kTypeFloat)
            {
                value = *(float*)valuePtr;
            }
            else if (format == InputStateBlock.kTypeBit)
            {
                if (m_StateBlock.sizeInBits != 1)
                    throw new NotImplementedException("Cannot yet convert multi-bit fields to floats");

                value = MemoryHelpers.ReadSingleBit(valuePtr, m_StateBlock.bitOffset) ? 1.0f : 0.0f;
            }
            // If a control with an integer-based representation does not use the full range
            // of its integer size (e.g. only goes from [0..128]), processors or the parameters
            // above have to be used to re-process the resulting float values.
            else if (format == InputStateBlock.kTypeShort)
            {
                value = *((ushort*)valuePtr) / 65535.0f;
            }
            else if (format == InputStateBlock.kTypeByte)
            {
                value = *((byte*)valuePtr) / 255.0f;
            }
            else
            {
                throw new Exception(string.Format("State format '{0}' is not supported as state for {1}",
                        m_StateBlock.format, GetType().Name));
            }

            return Preprocess(value);
        }

        protected override unsafe void WriteRawValueInto(IntPtr statePtr, float value)
        {
            var valuePtr = new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);

            var format = m_StateBlock.format;
            if (format == InputStateBlock.kTypeFloat)
            {
                *(float*)valuePtr = value;
            }
            else if (format == InputStateBlock.kTypeBit)
            {
                if (m_StateBlock.sizeInBits != 1)
                    throw new NotImplementedException("Cannot yet convert multi-bit fields to floats");

                MemoryHelpers.WriteSingleBit(valuePtr, m_StateBlock.bitOffset, value >= 0.5f);
            }
            else if (format == InputStateBlock.kTypeShort)
            {
                *(short*)valuePtr = (short)(value * 65535.0f);
            }
            else if (format == InputStateBlock.kTypeByte)
            {
                *(byte*)valuePtr = (byte)(value * 255.0f);
            }
            else
            {
                throw new Exception(string.Format("State format '{0}' is not supported as state for {1}",
                        m_StateBlock.format, GetType().Name));
            }
        }
    }
}
