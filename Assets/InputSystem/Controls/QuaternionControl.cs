using System;
using UnityEngine;

namespace ISX
{
    public class QuaternionControl : InputControl<Quaternion>
    {
        // No component controls as doing individual operations on the xyzw components of a quaternion
        // doesn't really make sense as individual input controls.
        ////REVIEW: while exposing quaternion fields makes no sense, might make sense to expose euler angle subcontrols

        public QuaternionControl()
        {
            m_StateBlock.sizeInBits = sizeof(float) * 4 * 8;
            m_StateBlock.format = InputStateBlock.kTypeQuaternion;
        }

        protected override unsafe Quaternion ReadRawValueFrom(IntPtr statePtr)
        {
            var valuePtr = (float*)(statePtr + (int)m_StateBlock.byteOffset);
            var x = valuePtr[0];
            var y = valuePtr[1];
            var z = valuePtr[2];
            var w = valuePtr[3];
            return new Quaternion(x, y, z, w);
        }
    }
}
