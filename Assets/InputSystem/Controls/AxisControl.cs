using System;
using UnityEngine;

namespace ISX
{
    // A float axis control.
    // Can optionally be configured to perform normalization.
    // Stored as either a float, a short, or a byte.
    public class AxisControl : InputControl<float>
    {
        // These can be added as processors but they are so common that we
        // build the functionality right into AxisControl.
        public bool clamp; // If true, force clamping to [min..max]
        public float clampMin;
        public float clampMax;
        public bool invert;

        private float Preprocess(float value)
        {
            if (invert)
                value *= -1.0f;
            if (clamp)
                value = Mathf.Clamp(value, clampMin, clampMax);
            return value;
        }

        public AxisControl()
        {
            m_StateBlock.sizeInBits = sizeof(float) * 8;
            m_StateBlock.format = InputStateBlock.kTypeFloat;
        }

        private unsafe float GetValue(IntPtr valuePtr)
        {
            float value;

            if (m_StateBlock.format == InputStateBlock.kTypeFloat)
            {
                value = *(float*)valuePtr;
            }
            // If a control with an integer-based representation does not use the full range
            // of its integer size (e.g. only goes from [0..128]), processors have to be used
            // to re-process the resulting float values.
            else if (m_StateBlock.format == InputStateBlock.kTypeShort)
            {
                value = *((short*)valuePtr) / 65535.0f;
            }
            else if (m_StateBlock.format == InputStateBlock.kTypeByte)
            {
                value = *((byte*)valuePtr) / 255.0f;
            }
            else
            {
                throw new Exception($"State format '{m_StateBlock.format}' is not supported as state for AxisControls");
            }

            return Process(Preprocess(value));
        }

        public override float value => GetValue(currentValuePtr);
        public override float previous => GetValue(previousValuePtr);
    }
}
