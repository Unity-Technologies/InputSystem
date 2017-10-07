using System;
using UnityEngine;

namespace ISX
{
    // Computes the magnitude of a Vector2.
    // You can add this as a child control of a 2D vector, for example, so as to get a magnitude
    // input control in addition to the X and Y component controls. The state is shared with the
    // vector itself.
    public class Magnitude2Control : InputControl<float>
    {
        public Magnitude2Control()
        {
            m_StateBlock.sizeInBits = sizeof(float) * 2 * 8;
            m_StateBlock.format = new FourCC('V', 'E', 'C', '2');
        }

        private unsafe float GetValue(IntPtr valuePtr)
        {
            var values = (float*)currentValuePtr;
            var x = values[0];
            var y = values[1];
            return new Vector2(x, y).magnitude;
        }

        public override float value => GetValue(currentValuePtr);
        public override float previous => GetValue(previousValuePtr);
    }
}
