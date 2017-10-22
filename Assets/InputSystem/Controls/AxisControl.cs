using System;
using UnityEngine;

namespace ISX
{
    // A float axis control.
    // Can optionally be configured to perform normalization.
    // Stored as either a float, a short, a byte, or a single bit.
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

        private new float Process(float value)
        {
            if (clamp)
                value = Mathf.Clamp(value, clampMin, clampMax);
            if (normalize)
                value = NormalizeProcessor.Normalize(value, normalizeMin, normalizeMax);
            if (invert)
                value *= -1.0f;
            return base.Process(value);
        }

        public AxisControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeFloat;
        }

        public override float value => Process(ReadFloatValueFrom(currentValuePtr));
        public override float previous => Process(ReadFloatValueFrom(previousValuePtr));

        // Helper to read a floating-point value from the given state. Automatically checks
        // the state format of the control and performs conversions.
        // NOTE: Throws if the format set on 'stateBlock' is not of integer, floating-point,
        //       or bitfield type.
        protected unsafe float ReadFloatValueFrom(IntPtr valuePtr)
        {
            float value;

            var format = m_StateBlock.format;
            if (format == InputStateBlock.kTypeFloat)
            {
                value = *(float*)valuePtr;
            }
            else if (format == InputStateBlock.kTypeBit)
            {
                if (m_StateBlock.sizeInBits != 1)
                    throw new NotImplementedException("Cannot yet convert multi-bit fields to floats");

                value = BitfieldHelpers.ReadSingleBit(valuePtr, m_StateBlock.bitOffset) ? 1.0f : 0.0f;
            }
            // If a control with an integer-based representation does not use the full range
            // of its integer size (e.g. only goes from [0..128]), processors or the parameters
            // above have to be used to re-process the resulting float values.
            else if (format == InputStateBlock.kTypeShort)
            {
                value = *((short*)valuePtr) / 65535.0f;
            }
            else if (format == InputStateBlock.kTypeByte)
            {
                value = *((byte*)valuePtr) / 255.0f;
            }
            else
            {
                throw new Exception($"State format '{m_StateBlock.format}' is not supported as state for {GetType().Name}");
            }

            return value;
        }
    }
}
