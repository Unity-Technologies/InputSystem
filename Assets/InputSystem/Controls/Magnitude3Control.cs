using System;
using ISX.LowLevel;
using UnityEngine;

////TODO: support vector of shorts

namespace ISX
{
    // Computes the magnitude of a Vector3.
    // You can add this as a child control of a 3D vector, for example, so as to get a magnitude
    // input control in addition to the X, Y, and Z component controls. The state is shared with the
    // vector itself.
    public class Magnitude3Control : AxisControl
    {
        public Magnitude3Control()
        {
            m_StateBlock.format = InputStateBlock.kTypeVector3;
        }

        protected override unsafe float ReadRawValueFrom(IntPtr statePtr)
        {
            var valuePtr = (float*)new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            var x = valuePtr[0];
            var y = valuePtr[1];
            var z = valuePtr[2];
            return new Vector3(x, y, z).magnitude;
        }
    }
}
