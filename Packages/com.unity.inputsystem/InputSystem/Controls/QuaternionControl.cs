using System;
using ISX.LowLevel;
using UnityEngine;

namespace ISX.Controls
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

        public override unsafe Quaternion ReadRawValueFrom(IntPtr statePtr)
        {
            var valuePtr = (float*)new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            var x = valuePtr[0];
            var y = valuePtr[1];
            var z = valuePtr[2];
            var w = valuePtr[3];
            return new Quaternion(x, y, z, w);
        }

        protected override unsafe void WriteRawValueInto(IntPtr statePtr, Quaternion value)
        {
            var valuePtr = (float*)new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            valuePtr[0] = value.x;
            valuePtr[1] = value.y;
            valuePtr[2] = value.z;
            valuePtr[3] = value.w;
        }
    }
}
