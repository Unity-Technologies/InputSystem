using UnityEngine;

namespace ISX
{
    // A float axis control.
    // Can optionally be configured to perform normalization.
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
        }

        public override float value
        {
            get
            {
                unsafe
                {
                    var value = *(float*)currentValuePtr;
                    return Process(Preprocess(value));
                }
            }
        }
    }
}
