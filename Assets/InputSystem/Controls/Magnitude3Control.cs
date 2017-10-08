using System;
using UnityEngine;

namespace ISX
{
    // Computes the magnitude of a Vector3.
    // You can add this as a child control of a 3D vector, for example, so as to get a magnitude
    // input control in addition to the X, Y, and Z component controls. The state is shared with the
    // vector itself.
    public class Magnitude3Control : InputControl<float>
    {
        public Magnitude3Control()
        {
            m_StateBlock.sizeInBits = sizeof(float) * 3 * 8;
            m_StateBlock.format = InputStateBlock.kTypeVector3;
        }

        private unsafe float GetValue(IntPtr valuePtr)
        {
            var values = (float*)currentValuePtr;
            var x = values[0];
            var y = values[1];
            var z = values[2];
            return new Vector3(x, y, z).magnitude;
        }

        public override float value => GetValue(currentValuePtr);
        public override float previous => GetValue(previousValuePtr);
    }
}
